﻿// Copyright (c) Canaan Inc. All rights reserved.
// Licensed under the Apache license. See LICENSE file in the project root for full license information.

using Google.OrTools.ConstraintSolver;
using NetFabric.Hyperlinq;
using Nncase.IR.Affine;
using QuikGraph;
using QuikGraph.Graphviz;

namespace Nncase.Schedule.TileGraph;

public sealed class TreeSolverInitializer : TreeSolverBase<IntExpr>, ITreeNodeVisitor<TreeSolverInitializer.Context, TreeSolverInitializer.InitResult>
{
    public TreeSolverInitializer(Dictionary<TieredTileGraph, BufferGraph> bufferGraphMemo, int topLevel, Solver solver, Dictionary<OpNode, OpNodeInfo<IntExpr>> primitiveBufferInfo, Dictionary<TileNode, TileNodeInfo<IntExpr>> levelBufferInfos, Dictionary<ITileable, DomainInfo<IntExpr>> domainDimInfos, ICpuTargetOptions targetOptions)
        : base(solver, primitiveBufferInfo, levelBufferInfos, domainDimInfos, targetOptions)
    {
        BufferGraphMemo = bufferGraphMemo;
        TopLevel = topLevel;
    }

    public int TimeStamp { get; private set; }

    public IReadOnlyDictionary<TieredTileGraph, BufferGraph> BufferGraphMemo { get; }

    public int TopLevel { get; }

    public static void Init(TileNode tree, Dictionary<TieredTileGraph, BufferGraph> bufferGraphMemo, int topLevel, ICpuTargetOptions options, out Solver solver, out Dictionary<OpNode, OpNodeInfo<IntExpr>> opNodeMemo, out Dictionary<TileNode, TileNodeInfo<IntExpr>> tileNodeMemo, out Dictionary<ITileable, DomainInfo<IntExpr>> tileableNodeMemo)
    {
        solver = new Solver("GraphSolver");
        opNodeMemo = new Dictionary<OpNode, OpNodeInfo<IntExpr>>();
        tileNodeMemo = new Dictionary<TileNode, TileNodeInfo<IntExpr>>();
        tileableNodeMemo = new Dictionary<ITileable, DomainInfo<IntExpr>>();
        var initializer = new TreeSolverInitializer(bufferGraphMemo, topLevel, solver, opNodeMemo, tileNodeMemo, tileableNodeMemo, options);
        initializer.Visit(tree, Context.Default);
    }

    public InitResult Visit(TileNode value, Context context)
    {
        var (pid, pvars, ptrips) = context;
        var dimsMap = GetDimsMap(value);
        if (!pvars.Any())
        {
            dimsMap.Clear();
        }

        var tileVars = Enumerable.Range(0, value.DomainRelation.Map.Results.Length).Select(n => Solver.MakeIntVar(1, int.MaxValue, $"op{value.OpId}_d{n}_L{value.Level}")).ToArray();
        var forwardExtents = tileVars.Cast<IntExpr>().ToArray();
        if (!TileableNodeMemo.TryGetValue(value, out var dimInfo))
        {
            foreach (var (k, v) in dimsMap)
            {
                forwardExtents[k] *= pvars[v];
            }

            TileableNodeMemo.Add(value, new(tileVars, forwardExtents, dimsMap));
        }

        var tripCounts = new IntExpr[tileVars.Length + 1];
        if (pvars.Any())
        {
            tripCounts[0] = ptrips;
        }
        else
        {
            tripCounts[0] = Solver.MakeIntConst(1);
        }

        for (int i = 0; i < tileVars.Length; i++)
        {
            tripCounts[1 + i] = tripCounts[i] * tileVars[i];
        }

        InitResult childResult;
        {
            var childContext = context with { ParentOpId = value.OpId, ForwardExtents = forwardExtents, TripCounts = tripCounts[^1] };

            var results = new List<BufferResult>();
            var names = new List<Dictionary<int, int>>();
            var extents = new List<IntExpr[]>();
            var childDefUseMap = new Dictionary<BufferIdentity, BufferIdentity>();
            foreach (var child in value.Children)
            {
                var res = child.Accept(this, childContext);
                results.AddRange(res.BufferResults);
                extents.AddRange(res.BackWardExtents);
                names.AddRange(res.DimsMaps);
                foreach (var (k, v) in res.DefUseMap)
                {
                    childDefUseMap.Add(k, v);
                }
            }

            childResult = new(results.ToArray(), childDefUseMap, names.ToArray(), extents.ToArray());
        }

        var backWardExtents = GetBackWardExtents(tileVars, childResult.DimsMaps, childResult.BackWardExtents);

        // {source id : target id}
        var defUseMap = BufferGraphMemo[value.Wrapped].Edges.Where(e => e.Tag == BufferEdgeKind.Outer).ToDictionary(e => e.Source, e => e.Target);
        var bufferResults = new List<BufferResult>();

        // each tile node have buffer place vars.
        if (!TileNodeMemo.TryGetValue(value, out var info))
        {
            var bufferInfoMap = new Dictionary<BufferIdentity, TileNodeBufferInfo<IntExpr>>();
            var reusedIds = new HashSet<BufferIdentity>(childResult.BufferResults.Where(r => defUseMap.ContainsKey(r.Bid)).Select(r => defUseMap[r.Bid]));
            for (int i = 0; i < childResult.BufferResults.Length; i++)
            {
                var result = childResult.BufferResults[i];
                var curId = result.Bid;
                if (reusedIds.Contains(curId))
                {
                    continue;
                }

                AffineMap currentAccessMap = result.AccessMap;
                Tuple<int, int> currentLifeness = result.Lifeness;
                if (defUseMap.TryGetValue(curId, out var sinkId))
                {
                    var sinkIndex = Array.FindIndex(childResult.BufferResults, r => r.Bid == sinkId);
                    currentAccessMap = childResult.BufferResults[sinkIndex].AccessMap;
                    currentLifeness = new(Math.Min(result.Lifeness.Item1, childResult.BufferResults[sinkIndex].Lifeness.Item1), Math.Max(result.Lifeness.Item2, childResult.BufferResults[sinkIndex].Lifeness.Item2));
                }

                if (!bufferInfoMap.TryGetValue(curId, out var bufferInfo))
                {
                    bufferInfoMap.Add(curId, GetBufferInfo(value, curId, currentAccessMap, currentLifeness, forwardExtents, backWardExtents));
                    bufferResults.Add(new(curId, currentLifeness, value.DomainRelation.Map * currentAccessMap));
                }
            }

            TileNodeMemo.Add(value, new(tripCounts, backWardExtents, defUseMap, bufferInfoMap));
        }

        return new(bufferResults.ToArray(), defUseMap, new[] { dimsMap }, new[] { backWardExtents[0] });
    }

    public InitResult Visit(OpNode value, Context context)
    {
        var (pid, pvars, ptrips) = context;
        var dimsMap = GetDimsMap(value);
        var tileVars = Enumerable.Range(0, value.DomainBounds.Length).Select(n => Solver.MakeIntVar(1, long.MaxValue, $"op{value.OpId}_d{n}_L{value.Level}")).ToArray();

        var kernelInfo = value.GetKernelInfo(TargetOptions);

        for (int i = 0; i < tileVars.Length; i++)
        {
            tileVars[i].SetRange(kernelInfo.Multipliers[i].Min, kernelInfo.Multipliers[i].Max);
        }

        var primtiveMap = AffineMap.FromCallable((doms, syms) => doms.Select(i => new AffineRange(i.Offset, kernelInfo.Primitives[i.Extent.Position] * i.Extent)).ToArray(), value.DomainBounds.Length);
        var accessMaps = new AffineMap[value.BufferShapes.Length];

        // cache the primitive buffer shape and sizes.
        if (!OpNodeMemo.TryGetValue(value, out var info))
        {
            var shapes = new IntExpr[value.BufferShapes.Length][];
            var sizes = new IntExpr[value.BufferShapes.Length];
            for (int a = 0; a < value.BufferShapes.Length; a++)
            {
                shapes[a] = new IntExpr[value.BufferShapes[a].Length];
                sizes[a] = Solver.MakeIntConst(value.Grid.Buffers[a].CheckedDataType.SizeInBytes);
                var extentVars = tileVars;
                var converter = new AffineExprToIntExprConverter(Solver, extentVars);
                accessMaps[a] = primtiveMap * value.Grid.AccessMaps[a];
                for (int i = 0; i < shapes[a].Length; i++)
                {
                    shapes[a][i] = converter.Visit(accessMaps[a].Results[i].Extent);
                    sizes[a] *= shapes[a][i];
                }
            }

            OpNodeMemo.Add(value, new(accessMaps, shapes, sizes));
        }

        if (!TileableNodeMemo.TryGetValue(value, out var dimInfo))
        {
            var forwardExtents = tileVars.Cast<IntExpr>().ToArray();
            foreach (var (i, j) in dimsMap)
            {
                forwardExtents[i] *= pvars[j];
            }

            for (int i = 0; i < tileVars.Length; i++)
            {
                forwardExtents[i] *= kernelInfo.Primitives[i];
            }

            TileableNodeMemo.Add(value, new(tileVars, forwardExtents, dimsMap));
        }

        // perpare return infos.
        var bufferResults = new BufferResult[value.ReadAccesses.Length + 1];
        BufferIdentity obid = new(value.Wrapped, value.ReadAccesses.Length);
        bufferResults[value.ReadAccesses.Length] = new(obid, new(TimeStamp, TimeStamp + 1), value.DomainRelation.Map * accessMaps[^1]);

        for (int i = 0; i < value.ReadAccesses.Length; i++)
        {
            BufferIdentity bid = new(value.Wrapped, i);
            bufferResults[i] = new(bid, new(TimeStamp, TimeStamp + 1), value.DomainRelation.Map * accessMaps[i]);
        }

        TimeStamp += 2;

        // todo backward extents should times primtives.
        return new(bufferResults, new(), new[] { dimsMap }, new IntExpr[][] { tileVars.Cast<IntExpr>().ToArray() });
    }

    /// <summary>
    /// Get the backward accumulated domain extents.
    /// backWardExtents[i] contains a extents[domain rank], note the extents[0:i] is not accumulated, extents[i:] is accumulated.
    /// for example. backWardExtents[2] contains extents[3], this extents[0],extents[1] is not accumulated, extents[2] is accumulated.
    /// so backWardExtents[0] means extents[0:domain rank] is accumulated.
    /// </summary>
    private IntExpr[][] GetBackWardExtents(IntVar[] tileVars, Dictionary<int, int>[] childDimsMaps, IntExpr[][] childBackWardExtents)
    {
        var backWardExtents = new IntExpr[tileVars.Length + 1][];
        bool ProductExtent(IntExpr[] extents, int i)
        {
            bool find = false;
            for (int cid = 0; cid < childDimsMaps.Length; cid++)
            {
                var cmap = childDimsMaps[cid];
                var cextents = childBackWardExtents[cid];
                foreach (var (k, v) in cmap)
                {
                    if (i == v)
                    {
                        extents[v] = extents[v] is null ? cextents[k] : extents[v] * cextents[k];
                        return find;
                    }
                }
            }

            throw new InvalidOperationException("can't find the child tile var");
        }

        for (int i = 0; i < tileVars.Length + 1; i++)
        {
            var extents = backWardExtents[i] = new IntExpr[tileVars.Length];

            // [0:i] is not accumulated.
            for (int j = 0; j < i; j++)
            {
                ProductExtent(extents, j);
            }

            // [i:domain] is accumulated
            for (int j = i; j < tileVars.Length; j++)
            {
                extents[j] = tileVars[j];
                ProductExtent(extents, j);
            }
        }

        return backWardExtents;
    }

    private TileNodeBufferInfo<IntExpr> GetBufferInfo(TileNode tileNode, BufferIdentity bid, AffineMap accessMap, Tuple<int, int> lifeness, IntExpr[] forwardExtents, IntExpr[][] backWardExtents)
    {
        var rank = tileNode.DomainRelation.Map.Results.Length + 1;
        var bufferPlaces = Enumerable.Range(0, rank).Select(i => Array.Empty<IntExpr>()).ToArray();
        var bufferShapes = Enumerable.Range(0, rank).Select(i => Array.Empty<IntExpr>()).ToArray();
        var bufferSizes = new IntExpr[rank];
        var bufferSizeVars = new IntExpr[rank];
        var bufferTrips = new IntExpr[rank];
        var bufferMasks = new LoopMask[rank];

        var resultStr = accessMap.ToString().Split("->")[1];
        for (int i = tileNode.Level == TopLevel ? 0 : 1; i < rank; i++)
        {
            var subLevelPlace = bufferPlaces[i] = new IntVar[tileNode.Level];
            for (int sl = 0; sl < subLevelPlace.Length; sl++)
            {
                subLevelPlace[sl] = Solver.MakeBoolVar($"p[cl{tileNode.Level}, op{tileNode.OpId}, b{bid.Index}, ci{i}, sl{sl}]");
            }

            var subDomainShapes = bufferShapes[i] = new IntExpr[accessMap.Results.Length];
            var converter = new AffineExprToIntExprConverter(Solver, backWardExtents[i]);
            for (int j = 0; j < accessMap.Results.Length; j++)
            {
                subDomainShapes[j] = converter.Visit(accessMap.Results[j].Extent);
            }

            bufferSizes[i] = subDomainShapes.Aggregate((IntExpr)Solver.MakeIntConst(bid.Node.Grid.Buffers[bid.Index].CheckedDataType.SizeInBytes), Solver.MakeProd);
            bufferSizeVars[i] = Solver.MakeIntVar(1, int.MaxValue, $"size[cl{tileNode.Level}, op{tileNode.OpId}, b{bid.Index}, ci{i}]");
            Solver.Add(Solver.MakeEquality(bufferSizeVars[i], bufferSizes[i]));

            var mask = 0U;
            bufferTrips[i] = Solver.MakeIntConst(1);
            for (int j = 0; j < i; j++)
            {
                if (resultStr.Contains($"d{j}", StringComparison.CurrentCulture))
                {
                    mask |= 1U << j;
                    bufferTrips[i] = bufferTrips[i] * forwardExtents[j];
                }
            }

            bufferMasks[i] = new(mask);

            // note update writes in second visitor.
        }

        var bufferInfo = new TileNodeBufferInfo<IntExpr>(lifeness, accessMap, bufferPlaces, bufferShapes, bufferSizeVars, bufferSizes, bufferTrips, bufferMasks);
        return bufferInfo;
    }

    /// <summary>
    /// each buffer with each access Maps, note the access map domain is this node's domain. extents also mapping to current node's domain.
    /// </summary>
    /// <param name="BufferResults">buffer info.</param>
    /// <param name="DefUseMap">the defuse map is used to record cache buffer in the top memory level. </param>
    /// <param name="DimsMaps">dims map.</param>
    /// <param name="BackWardExtents"> backward extents for cout the buffer size. </param>
    public sealed record InitResult(BufferResult[] BufferResults, Dictionary<BufferIdentity, BufferIdentity> DefUseMap, Dictionary<int, int>[] DimsMaps, IntExpr[][] BackWardExtents)
    {
    }

    /// <summary>
    /// buffer init result.
    /// </summary>
    /// <param name="Bid">buffer id.</param>
    /// <param name="Lifeness">buffer's lifetime.</param>
    /// <param name="AccessMap">access buffer relation from current node's domain, e.g. node.DomainRelation * buffer.AccessMap.</param>
    public sealed record BufferResult(BufferIdentity Bid, Tuple<int, int> Lifeness, AffineMap AccessMap)
    {
    }

    public sealed record Context(int ParentOpId, IReadOnlyList<IntExpr> ForwardExtents, IntExpr TripCounts)
    {
        public static Context Default => new(-1, Array.Empty<IntVar>(), null!);
    }
}
