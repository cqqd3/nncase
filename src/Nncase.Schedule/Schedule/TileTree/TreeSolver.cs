// Copyright (c) Canaan Inc. All rights reserved.
// Licensed under the Apache license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reactive;
using Google.OrTools.ConstraintSolver;
using NetFabric.Hyperlinq;
using Nncase.IR;
using Nncase.IR.Affine;
using VisitorPatternGenerator;
using Isl = IntegerSetLibrary;

namespace Nncase.Schedule;

internal sealed record TreeSolverContext(int ParentOpId, IReadOnlyList<string> DomainNames)
{
    public static TreeSolverContext Default => new(-1, Array.Empty<string>());
}

internal sealed record BufferIdenitity(OpNode Op, int Index)
{
    public override string ToString() => $"Op{Op.OpId}_{Index}";
}

/// <summary>
/// Place : [create_loop,store_level], write: [create_loop], size: [create loop].
/// </summary>
internal sealed record TileNodeBufferInfo(IntVar[][] Place, IntExpr[] Write, IntExpr[] Size)
{
}

internal sealed record TileNodeInfo(Dictionary<BufferIdenitity, BufferIdenitity> DefUseMap, Dictionary<BufferIdenitity, TileNodeBufferInfo> BufferInfoMap)
{
}

/// <summary>
/// each buffer with each access Maps, note the access map domain is this node's domain.
/// </summary>
internal sealed record TreeSolverResult(BufferIdenitity[] Bids, Isl.basic_map[] AccessMaps)
{
}

/// <summary>
/// loop masks.count == buffer.dimension.
/// </summary>
internal sealed record DomainDimInfo(string[] DimNames, IntVar[] TileVars, Dictionary<BufferIdenitity, LoopMasks> BufferMasksMap)
{
}

internal abstract class TileTreeSolverBase
{
    public TileTreeSolverBase(Solver solver, IntExpr one, IntExpr zero, IntExpr elem, Dictionary<string, IntVar[]> domainTileVarBuckets, Dictionary<string, Constraint> domainTileVarConstraints, Dictionary<OpNode, (IntExpr[][] Shapes, IntExpr[] Size)> primitiveBufferInfo, Dictionary<TileNode, TileNodeInfo> levelBufferInfos, Dictionary<ITileAbleNode, DomainDimInfo> domainDimInfos)
    {
        Solver = solver;
        One = one;
        Zero = zero;
        Elem = elem;
        DomainTileVarBuckets = domainTileVarBuckets;
        DomainTileVarConstraints = domainTileVarConstraints;
        OpNodeMemo = primitiveBufferInfo;
        TileNodeMemo = levelBufferInfos;
        TileableNodeMemo = domainDimInfos;
    }

    public Solver Solver { get; }

    public IntExpr One { get; }

    public IntExpr Zero { get; }

    public IntExpr Elem { get; }

    /// <summary>
    /// gets key is the domain name, value is `n` level tile size var. eg. 'op0_d0' => [L0,L1,L2].
    /// </summary>
    public Dictionary<string, IntVar[]> DomainTileVarBuckets { get; }

    /// <summary>
    /// gets op0_d0 = L0*L1*L2.
    /// </summary>
    public Dictionary<string, Constraint> DomainTileVarConstraints { get; }

    public Dictionary<OpNode, (IntExpr[][] Shapes, IntExpr[] Size)> OpNodeMemo { get; }

    public Dictionary<TileNode, TileNodeInfo> TileNodeMemo { get; }

    public Dictionary<ITileAbleNode, DomainDimInfo> TileableNodeMemo { get; }
}

internal sealed class TileTreeSolverInit : TileTreeSolverBase, ITreeNodeVisitor<TreeSolverContext, TreeSolverResult>
{
    public TileTreeSolverInit(Solver solver, IntExpr one, IntExpr zero, IntExpr elem, Dictionary<string, IntVar[]> domainTileVarBuckets, Dictionary<string, Constraint> domainTileVarConstraints, Dictionary<OpNode, (IntExpr[][] Shapes, IntExpr[] Size)> primitiveBufferInfo, Dictionary<TileNode, TileNodeInfo> levelBufferInfos, Dictionary<ITileAbleNode, DomainDimInfo> domainDimInfos)
        : base(solver, one, zero, elem, domainTileVarBuckets, domainTileVarConstraints, primitiveBufferInfo, levelBufferInfos, domainDimInfos)
    {
    }

    public TreeSolverResult Visit(ScopeNode value, TreeSolverContext context)
    {
        var bids = new List<BufferIdenitity>();
        var maps = new List<Isl.basic_map>();
        for (int i = 0; i < value.Children.Count; i++)
        {
            var res = value.Children[i].Accept(this, context);
            bids.AddRange(res.Bids);
            maps.AddRange(res.AccessMaps);
        }

        return new(bids.ToArray(), maps.ToArray());
    }

    public TreeSolverResult Visit(TileNode value, TreeSolverContext context)
    {
        var (pid, pnames) = context;
        var dimNames = TileTreePrinter.MappingDomainDims(value, pid, pnames);
        var tileVars = dimNames.Select(n => GetTileVar(value, n)).ToArray();
        var childResult = value.Child.Accept(this, context with { ParentOpId = value.OpId, DomainNames = dimNames });

        var defUseMap = GetBufferDefUseMap(childResult);

        if (!TileableNodeMemo.TryGetValue(value, out var dimInfo))
        {
            // note each tileable node need to recompute the loopmasks.
            var bufMasks = new Dictionary<BufferIdenitity, LoopMasks>();
            for (int i = 0; i < childResult.Bids.Length; i++)
            {
                var bid = childResult.Bids[i];
                LoopMasks masks;
                if (defUseMap.TryGetValue(bid, out var sinkBid))
                {
                    masks = bufMasks[sinkBid] = GetLoopMasks(childResult.AccessMaps[childResult.Bids.IndexOf(sinkBid)]);
                }
                else
                {
                    masks = GetLoopMasks(childResult.AccessMaps[i]);
                }

                bufMasks[bid] = masks;
            }

            dimInfo = new(dimNames, tileVars, bufMasks);
            TileableNodeMemo.Add(value, dimInfo);
        }

        // each tile node have buffer place vars.
        if (!TileNodeMemo.TryGetValue(value, out var info))
        {
            var bufferInfoMap = new Dictionary<BufferIdenitity, TileNodeBufferInfo>();
            for (int i = 0; i < childResult.Bids.Length; i++)
            {
                var sourceId = childResult.Bids[i];
                if (bufferInfoMap.ContainsKey(sourceId))
                {
                    continue;
                }

                if (defUseMap.TryGetValue(sourceId, out var sinkId))
                {
                    bufferInfoMap[sourceId] = bufferInfoMap[sinkId] = CreateBufferInfo(value, sinkId);
                }
                else
                {
                    bufferInfoMap[sourceId] = CreateBufferInfo(value, sourceId);
                }
            }

            TileNodeMemo.Add(value, new(defUseMap, bufferInfoMap));
        }

        return new(childResult.Bids, childResult.AccessMaps.Select(value.DomainRelation.apply_range).ToArray());
    }

    public TreeSolverResult Visit(OpNode value, TreeSolverContext context)
    {
        var (pid, pnames) = context;
        var dimNames = TileTreePrinter.MappingDomainDims(value, pid, pnames);
        var tileVars = dimNames.Select(n => GetTileVar(value, n)).ToArray();

        // add domain constraints
        for (int i = 0; i < dimNames.Length; i++)
        {
            if (!DomainTileVarConstraints.TryGetValue(dimNames[i], out var constraint))
            {
                var bucket = DomainTileVarBuckets[dimNames[i]];
                constraint = Solver.MakeEquality(Solver.MakeProd(bucket), value.DomainBounds[i]);
                DomainTileVarConstraints.Add(dimNames[i], constraint);
            }
        }

        // cache the primitive buffer shape and sizes.
        if (!OpNodeMemo.TryGetValue(value, out var info))
        {
            var shapes = new IntExpr[value.BufferShapes.Length][];
            var sizes = new IntExpr[value.BufferShapes.Length];
            for (int a = 0; a < value.BufferShapes.Length; a++)
            {
                shapes[a] = new IntExpr[value.BufferShapes[a].Length];
                sizes[a] = Elem;
                var extentVars = tileVars;
                var converter = new AffineExprToIntExprConverter(Solver, extentVars);
                var primtiveMap = AffineMap.FromCallable((doms, syms) => Enumerable.Range(0, value.DomainBounds.Count).Select(i => new AffineRange(doms[i].Offset, new AffineMulBinary(doms[i].Extent, new AffineConstant(1)))).ToArray(), value.DomainBounds.Count);
                var composedMap = primtiveMap * value.AccessMaps[a];
                for (int i = 0; i < shapes[a].Length; i++)
                {
                    shapes[a][i] = converter.Visit(composedMap.Results[i].Extent);
                    sizes[a] *= shapes[a][i];
                }
            }

            OpNodeMemo.Add(value, (shapes, sizes));
        }

        if (!TileableNodeMemo.TryGetValue(value, out var dimInfo))
        {
            var bufMasks = new Dictionary<BufferIdenitity, LoopMasks>();
            for (int i = 0; i < value.Reads.Count; i++)
            {
                bufMasks[new(value, i)] = GetLoopMasks(value.Reads[i]);
            }

            bufMasks[new(value, value.Reads.Count)] = GetLoopMasks(value.Write);

            dimInfo = new(dimNames, tileVars, bufMasks);
            TileableNodeMemo.Add(value, dimInfo);
        }

        // perpare return infos.
        var resBids = new BufferIdenitity[value.Reads.Count + 1];
        var resRels = new Isl.basic_map[value.Reads.Count + 1];

        for (int i = 0; i < value.Reads.Count; i++)
        {
            resBids[i] = new(value, i);
            resRels[i] = value.DomainRelation.apply_range(value.Reads[i]);
        }

        resBids[value.Reads.Count] = new(value, value.Reads.Count);
        resRels[value.Reads.Count] = value.DomainRelation.apply_range(value.Write);
        return new(resBids, resRels);
    }

    private IntVar GetTileVar(ITileAbleNode node, string name)
    {
        if (!DomainTileVarBuckets.TryGetValue(name, out var bucket))
        {
            bucket = new IntVar[node.Level + 1];
            DomainTileVarBuckets.Add(name, bucket);
        }

        if (bucket[node.Level] is null)
        {
            bucket[node.Level] = Solver.MakeIntVar(1, long.MaxValue, $"{name}_L{node.Level}");
        }

        return bucket[node.Level];
    }

    /// <summary>
    /// source id => sink id.
    /// </summary>
    private Dictionary<BufferIdenitity, BufferIdenitity> GetBufferDefUseMap(TreeSolverResult childResult)
    {
        var map = new Dictionary<BufferIdenitity, BufferIdenitity>();
        for (int i = 0; i < childResult.Bids.Length; i++)
        {
            var sinkId = childResult.Bids[i];
            foreach (var dep in sinkId.Op.Dependences)
            {
                var sourceId = new BufferIdenitity(dep.Node, dep.Node.Reads.Count);
                if (childResult.Bids.IndexOf(sourceId) != -1)
                {
                    if (!map.ContainsKey(sourceId))
                    {
                        map.Add(sourceId, sinkId);
                    }
                }
            }
        }

        return map;
    }

    private TileNodeBufferInfo CreateBufferInfo(TileNode tile, BufferIdenitity bid)
    {
        var domainDims = tile.Vars.Length;
        var bufferPlaces = new IntVar[domainDims][];
        var bufferWrites = new IntExpr[domainDims];
        var bufferSizes = new IntExpr[domainDims];
        for (int i = 0; i < domainDims; i++)
        {
            var subLevelPlace = bufferPlaces[i] = new IntVar[tile.Level + 1];

            // compute buffer size at dim i.
            {
                IntExpr lastLevelSize = One;
                if (tile.Level == 1 && i == 0)
                {
                    lastLevelSize = OpNodeMemo[bid.Op].Size[bid.Index];
                }
                else
                {
                    if (i == 0)
                    {
                        // because of the diffent branch shared the same parent domain var, so we just use first tile node buffer size.
                        var walker = new TileTreeWalker();
                        tile.Child.Accept(walker, default);
                        bool find = false;
                        foreach (var child in walker.Nodes.OfType<TileNode>())
                        {
                            if (TileNodeMemo[child].BufferInfoMap.TryGetValue(bid, out var childBufferInfo))
                            {
                                lastLevelSize = childBufferInfo.Size[^1];
                                find = true;
                                break;
                            }
                        }

                        if (!find)
                        {
                            throw new NotSupportedException("can't find the child buffer info!");
                        }
                    }
                    else
                    {
                        lastLevelSize = bufferSizes[i - 1];
                    }
                }

                // todo using range map for compute shape..
                bufferSizes[i] = TileableNodeMemo[tile].BufferMasksMap[bid].IsRelated(i) ? lastLevelSize * TileableNodeMemo[tile].TileVars[i - 1] : lastLevelSize;
            }

            for (int sl = 0; sl < tile.Level + 1; sl++)
            {
                subLevelPlace[sl] = Solver.MakeBoolVar($"p[cl{tile.Level}, op{tile.OpId}, b{bid.Index}, ci{i}, {sl}]");
            }
        }

        var bufferInfo = new TileNodeBufferInfo(bufferPlaces, bufferWrites, bufferSizes);
        return bufferInfo;
    }

    /// <summary>
    /// loop masks contain each dimension's loop mask of the buffer.
    /// </summary>
    private LoopMasks GetLoopMasks(Isl.basic_map accessRel)
    {
        var eqMat = accessRel.equalities_matrix(Isl.dim_type.in_, Isl.dim_type.out_, Isl.dim_type.cst, Isl.dim_type.param, Isl.dim_type.div);
        var masks = new LoopMask[accessRel.dim(Isl.dim_type.out_)];
        for (int col = accessRel.dim(Isl.dim_type.in_); col < accessRel.dim(Isl.dim_type.out_); col++)
        {
            uint mask = 0;
            for (int i = 0; i < eqMat.rows(); i++)
            {
                // when the out dim is not zero
                if (eqMat.element_val(i, col).is_zero())
                {
                    continue;
                }

                // check which in dim is also not zero
                for (int j = 0; j < accessRel.dim(Isl.dim_type.in_); j++)
                {
                    if (!eqMat.element_val(i, j).is_zero())
                    {
                        mask |= 1U << i;
                    }
                }
            }

            masks[col] = new(mask);
        }

        return new(masks);
    }
}

internal sealed class TileTreeSolverInitWrites : TileTreeSolverBase, ITreeNodeVisitor<Dictionary<BufferIdenitity, IntExpr[]>, Unit>
{
    public TileTreeSolverInitWrites(Solver solver, IntExpr one, IntExpr zero, IntExpr elem, Dictionary<string, IntVar[]> domainTileVarBuckets, Dictionary<string, Constraint> domainTileVarConstraints, Dictionary<OpNode, (IntExpr[][] Shapes, IntExpr[] Size)> primitiveBufferInfo, Dictionary<TileNode, TileNodeInfo> levelBufferInfos, Dictionary<ITileAbleNode, DomainDimInfo> domainDimInfos)
        : base(solver, one, zero, elem, domainTileVarBuckets, domainTileVarConstraints, primitiveBufferInfo, levelBufferInfos, domainDimInfos)
    {
    }

    public Unit Visit(ScopeNode value, Dictionary<BufferIdenitity, IntExpr[]> bufferTripCounts)
    {
        for (int i = 0; i < value.Children.Count; i++)
        {
            value.Children[i].Accept(this, bufferTripCounts);
        }

        return default;
    }

    /// <summary>
    /// buffer trip counts mean each buffer's trip count at loop i.
    /// </summary>
    public Unit Visit(TileNode value, Dictionary<BufferIdenitity, IntExpr[]> bufferTripCounts)
    {
        Dictionary<BufferIdenitity, IntExpr[]> currentTripCounts = new();
        var domainInfo = TileableNodeMemo[value];
        if (value.GetParentTileableNode() is ITileAbleNode parentTileable)
        {
            var parentDomainInfo = TileableNodeMemo[parentTileable];

            // 1. child domain map to parent domain.
            var domainRel = new Dictionary<int, int>();
            for (int i = 0; i < parentDomainInfo.DimNames.Length; i++)
            {
                for (int j = 0; j < domainInfo.DimNames.Length; j++)
                {
                    if (parentDomainInfo.DimNames[i] == domainInfo.DimNames[j])
                    {
                        domainRel.Add(j, i);
                    }
                }
            }

            foreach (var (bid, bufferInfo) in TileNodeMemo[value].BufferInfoMap)
            {
                var parentTripCounts = bufferTripCounts[bid];
                var tripCounts = new IntExpr[domainInfo.TileVars.Length];

                for (int i = 0; i < domainInfo.TileVars.Length; i++)
                {
                    IntExpr factor;
                    IntExpr parentFactor;
                    if (domainInfo.BufferMasksMap[bid].IsRelated(i))
                    {
                        factor = domainInfo.TileVars[i];
                    }
                    else
                    {
                        factor = One;
                    }

                    if (domainRel.TryGetValue(i, out var j))
                    {
                        parentFactor = parentTripCounts[j];
                    }
                    else
                    {
                        parentFactor = One;
                    }

                    tripCounts[i] = factor * parentFactor;
                    bufferInfo.Write[i] = bufferInfo.Size[i] * tripCounts[i];
                }

                currentTripCounts.Add(bid, tripCounts);
            }
        }
        else
        {
            foreach (var (bid, bufferInfo) in TileNodeMemo[value].BufferInfoMap)
            {
                var tripCounts = new IntExpr[domainInfo.TileVars.Length];

                for (int i = 0; i < domainInfo.TileVars.Length; i++)
                {
                    IntExpr factor;
                    if (domainInfo.BufferMasksMap[bid].IsRelated(i))
                    {
                        factor = domainInfo.TileVars[i];
                    }
                    else
                    {
                        factor = One;
                    }

                    tripCounts[i] = factor;
                    bufferInfo.Write[i] = bufferInfo.Size[i] * tripCounts[i];
                }

                currentTripCounts.Add(bid, tripCounts);
            }
        }

        value.Child.Accept(this, currentTripCounts);

        return default;
    }

    public Unit Visit(OpNode value, Dictionary<BufferIdenitity, IntExpr[]> bufferTripCounts)
    {
        return default;
    }
}
