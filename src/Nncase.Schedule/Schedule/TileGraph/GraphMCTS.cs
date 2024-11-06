// Copyright (c) Canaan Inc. All rights reserved.
// Licensed under the Apache license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using NetFabric.Hyperlinq;
using Nncase.IR;
using Nncase.IR.Affine;
using Nncase.Schedule.MonteCarloTreeSearch;
using QuikGraph;
using QuikGraph.Algorithms;
using QuikGraph.Algorithms.ShortestPath;
using QuikGraph.Graphviz;

namespace Nncase.Schedule.TileGraph;

public sealed class MCTState : IEnvironmentState<MergePoint>
{
    private readonly string _path = string.Empty;

    private readonly List<MergePoint> _mergePoints = new();

    private readonly List<int> _legalIndex = new();

    private readonly Dictionary<TileNode, GraphTiler.TiledFunc> _solveMemo;

    private readonly string _moduleKind;

    private readonly string _prefix;

    private readonly ICpuTargetOptions _targetOptions;

    private int _permformCount;

    private TieredTileGraph _graph;

    public MCTState(TieredTileGraph graph, string moduleKind, string prefix, string searchPath, Dictionary<TileNode, GraphTiler.TiledFunc> solveMemo, ICpuTargetOptions targetOptions)
    {
        _graph = graph;
        _moduleKind = moduleKind;
        _prefix = prefix;
        _solveMemo = solveMemo;
        _targetOptions = targetOptions;
        _mergePoints.AddRange(graph.GetMergePoints());
        _legalIndex.AddRange(Enumerable.Range(0, _mergePoints.Count));
        _path = searchPath;
        Results = new(new LeafTileGraphComparer());
    }

    public long ObjectValue { get; private set; }

    public Dictionary<TieredTileGraph, Expr> Results { get; }

    public MergePoint GetNextAction(int index)
    {
        var legalIndex = _legalIndex[index];
        _legalIndex.Remove(legalIndex);
        _permformCount++;
        return _mergePoints[legalIndex];
    }

    public int LegalActions()
    {
        return _legalIndex.Count;
    }

    public IEnvironmentState<MergePoint>? PerformAction(MergePoint mergePoint)
    {
        var newGraph = _graph.Clone();
        if (newGraph.Merge(mergePoint))
        {
            return new MCTState(newGraph, _moduleKind, _prefix, $"{_path}.{_permformCount}", _solveMemo, _targetOptions);
        }

        return null;
    }

    public double RollOut()
    {
        if (ObjectValue == 0)
        {
            using var scope = new Diagnostics.DumpScope($"RollOut{_path}");
            try
            {
                var res = GraphTiler.SolveRootGraph(_graph, _moduleKind, _prefix, _solveMemo, _targetOptions);
                ObjectValue = res.ObjectValue;
                foreach (var item in res.ResultMemo)
                {
                    Results.Add(item.Key, item.Value);
                }
            }
            catch (System.Exception)
            {
                ObjectValue = long.MaxValue;
                return ObjectValue;
            }
        }

        return ObjectValue;
    }

    private sealed class LeafTileGraphComparer : IEqualityComparer<TieredTileGraph>
    {
        public bool Equals(TieredTileGraph? x, TieredTileGraph? y) => (x, y) switch
        {
            (null, null) => true,
            (TieredTileGraph a, TieredTileGraph b) => a.OpId.Equals(b.OpId) && a.Level.Equals(b.Level),
            _ => false,
        };

        public int GetHashCode([DisallowNull] TieredTileGraph obj) => HashCode.Combine(obj.OpId, obj.Level);
    }
}

public sealed class MCTNode : SearchNode<MergePoint>
{
    public MCTNode(IEnvironmentState<MergePoint> state)
        : base(state)
    {
        Action = null;
        QualityValue = double.PositiveInfinity;
    }

    public MCTNode(SearchNode<MergePoint> parent, IEnvironmentState<MergePoint> state, MergePoint action)
        : base(parent, state)
    {
        QualityValue = double.PositiveInfinity;
        Action = action;
    }

    public MergePoint? Action { get; }

    public override void Update(double reward)
    {
        if (QualityValue > reward)
        {
            QualityValue = reward;
        }

        VisitTimes += 1;

        if (Parent is not null)
        {
            Parent.Update(reward);
        }
    }

    public void Dump(string name)
    {
        using (var file = Diagnostics.DumpScope.Current.OpenFile($"{name}.yaml"))
        {
            using var baseWriter = new StreamWriter(file);
            using var writer = new System.CodeDom.Compiler.IndentedTextWriter(baseWriter, "  ");
            Dump(writer);
        }
    }

    public void Dump(System.CodeDom.Compiler.IndentedTextWriter writer)
    {
        writer.WriteLine($"- name: {this}");
        writer.WriteLine($"  Action: {Action}");
        writer.WriteLine($"  QualityValue: {QualityValue}");
        writer.WriteLine($"  VisitTimes: {VisitTimes}");
        writer.WriteLine($"  Children:");
        writer.Indent += 1;
        foreach (var item in Children.OfType<MCTNode>())
        {
            item.Dump(writer);
        }

        writer.Indent -= 1;
    }
}

public sealed class MCTSearcher : Searcher<MergePoint>
{
    private readonly Random _random = new Random(1010);

    public MCTSearcher()
    {
        BestObjectValue = double.PositiveInfinity;
        BestMCTNode = null;
    }

    public double BestObjectValue { get; private set; }

    public MCTNode? BestMCTNode { get; private set; }

    public SearchNode<MergePoint> UCBSelectChild(SearchNode<MergePoint> node)
    {
        double coef = Math.Sqrt(2);
        double temp = 0.5;
        var ucbs = node.Children.Select(c => (-c.QualityValue / BestObjectValue) + (coef * Math.Sqrt(Math.Log(node.VisitTimes) / c.VisitTimes))).ToArray();
        var ucbs_exp = ucbs.Select(ucb => Math.Exp(ucb / temp)).ToArray();
        var sum = ucbs_exp.Sum();
        var probs = ucbs_exp.Select(e => (int)(e / sum * 30)).ToArray(); // conver ucb as prob
        var candidates = probs.Select((p, i) => Enumerable.Repeat(i, p).ToArray()).SelectMany(i => i).ToArray();
        return node.Children[candidates[_random.Next(candidates.Length)]];
    }

    public override bool Selection(SearchNode<MergePoint> node, out SearchNode<MergePoint> selected)
    {
        while (node.State.LegalActions() == 0 && node.Children.Count > 0)
        {
            node = UCBSelectChild(node);
        }

        selected = node;
        return true;
    }

    public override SearchNode<MergePoint>? Expand(SearchNode<MergePoint> node)
    {
        if (node.VisitTimes != 0 && node.State.LegalActions() > 0)
        {
            var index = _random.Next(node.State.LegalActions());
            var action = node.State.GetNextAction(index);
            var state = node.State.PerformAction(action);
            if (state is not null)
            {
                return new MCTNode(node, state, action);
            }

            return null;
        }

        return node;
    }

    public override double Simulation(SearchNode<MergePoint> node)
    {
        double value = node.State.RollOut();
        if (value < BestObjectValue)
        {
            BestObjectValue = value;
            BestMCTNode = (MCTNode)node;
        }

        return value;
    }

    public override void BackPropagate(SearchNode<MergePoint> node, double reward)
    {
        node.Update(reward);
    }
}
