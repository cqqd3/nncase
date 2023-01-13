﻿// Copyright (c) Canaan Inc. All rights reserved.
// Licensed under the Apache license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using NetFabric.Hyperlinq;
using Nncase.IR;
using Nncase.Utilities;
using Xunit;
using static Nncase.Tests.TensorUtil;
using static Nncase.Utilities.DumpUtility;
using Tuple = Nncase.IR.Tuple;

namespace Nncase.Tests;

public static class TensorUtil
{
    public static int GetChannelAxis(Shape shape)
    {
        return GetChannelAxis(shape.ToValueArray());
    }

    public static int GetChannelAxis(int[] shape)
    {
        return Math.Max(0, 1 - (4 - shape.Length));
    }

    public static (int, int) GetShapeInfo(int[] shape, int channelAxis = 1)
    {
        var i = channelAxis + 1;
        var channels = shape[..i].Aggregate(1, (a, b) => a * b);
        var size = shape[i..].Aggregate(1, (a, b) => a * b);
        return (channels, size);
    }

    public static Tensor[] SliceByChannel(Tensor tensor)
    {
        var channelAxis = GetChannelAxis(tensor.Shape);
        var (channels, size) = GetShapeInfo(tensor.Dimensions.ToArray(), channelAxis);
        return Enumerable.Range(0, channels).Select(i =>
                SliceTensor(tensor, size * i, size, channelAxis))
            .ToArray();
    }

    private static Tensor SliceTensor(Tensor tensor, int start, int length, int channelAxis = 1)
    {
        var s = tensor.ElementType.SizeInBytes;
        return Tensor.FromBytes(
            tensor.ElementType,
            tensor.BytesBuffer.Slice(start * s, length * s).ToArray(), tensor.Dimensions[(channelAxis + 1)..]);
    }
}

public static class Comparator
{
    public static float[] CosSimilarity(IValue a, IValue b)
    {
        return CosSimilarity(a.AsTensors(), b.AsTensors());
    }

    public static float[] CosSimilarity(Tensor[] a, Tensor[] b)
    {
        return a.Zip(b).Select(CosSimilarity).ToArray();
    }

    public static float CosSimilarity((Tensor, Tensor) t)
    {
        return CosSimilarity(t.Item1, t.Item2);
    }

    public static float CosSimilarity((OriginTensor, OriginTensor) t)
    {
        return CosSimilarity(t.Item1.Tensor, t.Item2.Tensor);
    }

    public static float[][] CosSimilarity(IValue[] a, IValue[] b)
    {
        return a.Zip(b).Select(tuple => CosSimilarity(tuple.Item1, tuple.Item2)).ToArray();
    }

    public static float[][] CosSimilarity(OriginValue[] a, OriginValue[] b)
    {
        return a.Zip(b).Select(tuple => CosSimilarity(tuple.Item1.Value, tuple.Item2.Value)).ToArray();
    }

    public static float CosSimilarity(OriginTensor a, OriginTensor b) => CosSimilarity(a.Tensor, b.Tensor);

    public static float[] CosSimilarity(OriginTensor[] a, OriginTensor[] b) => a.Zip(b).Select(CosSimilarity).ToArray();

    public static float CosSimilarity(Tensor a, Tensor b)
    {
        var va = a.ToArray<float>();
        var vb = b.ToArray<float>();
        var v1 = Math.Sqrt(Prod(va, va));
        var v2 = Math.Sqrt(Prod(vb, vb));
        var sum = Prod(va, vb);
        return (float)(sum / (v1 * v2));
    }

    public static bool AllEqual(Tensor a, Tensor b, float thresh)
    {
        var va = a.ToArray<float>();
        var vb = b.ToArray<float>();
        return va.Zip(vb).All(p => (p.Item1, p.Item2) switch
        {
            (float.NaN, float.NaN) => 0.0f,
            _ => MathF.Abs(p.Item1 - p.Item2),
        }

        <= thresh);
    }

    public static bool TensorValueCompare(TensorValue pre, TensorValue post, float thresh)
    {
        var v1 = pre.AsTensor();
        var v2 = post.AsTensor();
        Assert.Equal(v1.Shape, v2.Shape);
        Assert.Equal(v1.ElementType, v2.ElementType);
        var cosSim = CosSimilarity(v1, v2);
        Assert.True(cosSim > thresh, "cosSim:" + cosSim);
        return true;
    }

    public static bool TupleValueCompare(TupleValue a, TupleValue b, float thresh = 0.99f)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        foreach (var (t1, t2) in a.AsTensors().Zip(b.AsTensors()))
        {
            if (!TensorValueCompare(t1, t2, thresh))
            {
                return false;
            }
        }

        return true;
    }

    public static bool Compare(IValue pre, IValue post, float thresh = 0.99f) => (pre, post) switch
    {
        (TensorValue a, TensorValue b) => TensorValueCompare(a, b, thresh),
        (TupleValue a, TupleValue b) => TupleValueCompare(a, b, thresh),
        (_, _) => throw new ArgumentOutOfRangeException(),
    };

    public static bool AllEqual(IValue pre, IValue post, float thresh = 0.001f) => (pre, post) switch
    {
        (TensorValue a, TensorValue b) => TensorValueAllEqual(a, b, thresh),
        (TupleValue a, TupleValue b) => TupleValueAllEqual(a, b, thresh),
        (_, _) => throw new ArgumentOutOfRangeException(),
    };

    public static float[][] CompareByChannel(IValue pre, IValue post, int channelAxis = 1, float thresh = 0.99f) =>
        TensorsCompareByChannel(pre.AsTensors(), post.AsTensors(), channelAxis, thresh);

    public static float[] TensorCompareByChannel(Tensor pre, Tensor post, int channelAxis = 1, float thresh = 0.99f)
    {
        // todo:broadcast type???
        var v1 = SliceByChannel(pre);
        var v2 = SliceByChannel(post);

        // Assert.Equal(v1.Length, v2.Length);
        return v1.Zip(v2).Select(data => CosSimilarity(data.Item1, data.Item2)).ToArray();
    }

    public static float[][] TensorsCompareByChannel(Tensor[] pre, Tensor[] post, int channelAxis = 1,
        float thresh = 0.99f)
    {
        return pre.Zip(post).Select(tuple =>
                TensorCompareByChannel(tuple.Item1, tuple.Item2, channelAxis, thresh))
            .ToArray();
    }

    private static float Prod(float[] data1, float[] data2)
    {
        return data1.Zip(data2).Aggregate(0f, (f, tuple) => f + (tuple.Item1 * tuple.Item2));
    }

    private static bool TupleValueAllEqual(TupleValue a, TupleValue b, float thresh)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        foreach (var (t1, t2) in a.AsTensors().Zip(b.AsTensors()))
        {
            if (!TensorValueAllEqual(t1, t2, thresh))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TensorValueAllEqual(TensorValue pre, TensorValue post, float thresh)
    {
        var v1 = pre.AsTensor();
        var v2 = post.AsTensor();
        Assert.Equal(v1.Shape, v2.Shape);
        Assert.Equal(v1.ElementType, v2.ElementType);
        Assert.True(AllEqual(v1, v2, thresh));
        return true;
    }
}

public static class DetailComparator
{
    public static void DumpCompareDetailAnalysis(CompareResultByChannel[] resultByChannels, string path, int i)
    {
        var shape = resultByChannels.Length != 0
            ? SerializeShape(resultByChannels.Head().Shape)
            : "data all ok and not shape info";
        var fileName = resultByChannels.Length != 0 ? resultByChannels[0].Losses.Head().V1Tensor.FileName : "AllOK";
        WriteResult(Path.Join(path, $"{i}_{fileName}"), resultByChannels, $"{shape}\n");
    }

    // for single file
    public static void CompareDetailAnalysis(IEnumerable<DetailCompareResult> data, float thresh = 0.99f)
    {
        var result = data.Select(dataByTensor => CompareDetailAnalysis(dataByTensor, thresh)).ToArray();
    }

    public static CompareResultByChannel[] CompareDetailAnalysis(DetailCompareResult dataByTensor, float thresh = 0.99f)
    {
        return dataByTensor.Enumerable().Where(result => result.IsOk(thresh)).ToArray();
    }

    public static IEnumerable<DetailCompareResult> CompareDetail(Tensor[] a, Tensor[] b)
    {
        Assert.Equal(a.Length, b.Length);
        return a.Zip(b).Select((t) =>
            CompareDetail(new OriginTensor(t.Item1, string.Empty), new OriginTensor(t.Item2, string.Empty), GetChannelAxis(t.Item1.Shape)));
    }

    public static DetailCompareResult CompareDetail(OriginValue a, OriginValue b, int channelAxis = 1)
    {
        var cos = Comparator.CompareByChannel(a.Value, b.Value, channelAxis);
        var LossInfo = CompareForAccuracyLoss(a, b);
        _ = a.Value.AsTensors().Length;

        // todo: fix this
        return new DetailCompareResult(new[] { new DetailCompareResultInfo(cos[0], LossInfo[0]) });
    }

    public static AccuracyLossInfo[][] CompareForAccuracyLoss(OriginValue pre, OriginValue post) =>
        TensorsCompareForAccuracyLoss(pre.AsTensors(), post.AsTensors());

    public static AccuracyLossInfo[][] TensorsCompareForAccuracyLoss(OriginTensor[] pre, OriginTensor[] post) =>
        pre.Zip(post).Select(tuple => TensorCompareForAccuracyLoss(tuple.Item1, tuple.Item2)).ToArray();

    public static AccuracyLossInfo[] TensorCompareForAccuracyLoss(OriginTensor pre, OriginTensor post)
    {
        var v1 = pre.Tensor.ToArray<float>();
        var v2 = post.Tensor.ToArray<float>();
        var preShape = pre.Tensor.Shape.ToValueArray();
        var index = new int[preShape.Length];
        return v1.Zip(v2).Select(data =>
        {
            index[^1] += 1;
            for (int i = preShape.Length - 1; i >= 0; i--)
            {
                if (index[i] > preShape[i])
                {
                    index[i] = 0;
                    index[i - 1] += 1;
                }
            }

            Console.WriteLine(string.Join(",", index.Select(x => x.ToString())));
            return new AccuracyLossInfo(data.Item1, data.Item2, index.ToArray(), pre, post);
        }).ToArray();
    }

    public static void DumpCompareDetail(DetailCompareResult compareResult, string resultRoot, int count)
    {
        // todo: fix this
        var (cosByChannel, LossInfo) = compareResult.Infos.Head();

        // todo: insert separator for channel or other
        WriteResult(Path.Join(resultRoot, $"cos_{count}"), cosByChannel);

        using (var stream = new StreamWriter(Path.Join(resultRoot, count.ToString())))
        {
            stream.WriteLine(LossInfo[0].V1Tensor.Path);
            stream.WriteLine(LossInfo[0].V2Tensor.Path);
            var tensorShape = LossInfo[0].Shape;
            var (channels, size) = TensorUtil.GetShapeInfo(tensorShape, TensorUtil.GetChannelAxis(tensorShape));
            stream.WriteLine(SerializeShape(tensorShape));
            for (int i = 0; i < channels; i++)
            {
                stream.WriteLine(cosByChannel[i]);
                for (int j = 0; j < size; j++)
                {
                    stream.WriteLine(LossInfo[(i * size) + j]);
                }
            }
        }
    }

    // public static void GenerateFullCompareInfo(Tensor[] a, Tensor[] b, string resultRoot)
    // {
    //     GenerateFullCompareInfo(
    //         a.Select(x => (IValue) Value.FromTensor(x)).Zip(b.Select(x => (IValue) Value.FromTensor(x))), resultRoot);
    // }
    public static void GenerateFullCompareInfo(OriginValue[] a, OriginValue[] b, string resultRoot)
    {
        if (a.Length != b.Length)
        {
            throw new InvalidOperationException($"tensor a and b should same length but a:{a.Length} b:{b.Length}");
        }

        GenerateFullCompareInfo(a.Zip(b), resultRoot);
    }

    public static void GenerateFullCompareInfo(IEnumerable<(OriginValue, OriginValue)> data, string resultRoot)
    {
        int id = 0;
        foreach (var (originD, k230D) in data)
        {
            GenerateFullCompareInfo(resultRoot, originD, k230D, id++);
        }
    }

    private static CompareResultByChannel[] GenerateFullCompareInfo(string resultRoot, OriginValue originD, OriginValue k230D,
        int count)
    {
        var result = DetailComparator.CompareDetail(originD, k230D);

        // cos_i
        DetailComparator.DumpCompareDetail(result, PathJoinByCreate(resultRoot, "detail"), count);
        var analysisResult = DetailComparator.CompareDetailAnalysis(result);

        // analysis_i
        DetailComparator.DumpCompareDetailAnalysis(analysisResult, PathJoinByCreate(resultRoot, "Analysis"), count);
        return analysisResult;
    }
}

public class LazyCompartor
{
    // count => Either<ErrorReason, CosSim>
    private readonly Dictionary<int, LanguageExt.Either<string, float>> _error = new();

    public static Option<float> Compare(TensorValue pre, TensorValue post, float thresh)
    {
        var v1 = pre.AsTensor();
        var v2 = post.AsTensor();
        var cosSim = Comparator.CosSimilarity(v1, v2);
        if (cosSim < thresh)
        {
            return Option.Some(cosSim);
        }
        else
        {
            return Option.None;
        }
    }

    public bool Compare(IValue pre, IValue post, int count, float thresh = 0.99f)
    {
        if (pre is TensorValue a &&
            post is TensorValue b)
        {
            return Compare(a, b, thresh).Match(
                cos =>
                {
                    _error[count] = cos;
                    return false;
                },
                () => true);
        }
        else
        {
            _error[count] = $"pre is {pre.GetType().Name}, post is {post.GetType().Name}";
            return false;
        }
    }

    public void AddFailed(int count, string reason)
    {
        _error[count] = reason;
    }

    public void Run(Action f)
    {
        f();
        FailedAssert();
    }

    public void FailedAssert()
    {
        if (_error.Count == 0)
        {
            return;
        }

        var errList = _error.Select(v =>
        {
            var count = v.Key;
            var value = v.Value;
            var reason = value.Match(cos => $"CosSim:{cos}", s => s);
            return $"count:{count} error, reason: ${reason}";
        });
        var errInfo = string.Join("\n", errList);
        Assert.True(false, errInfo);
    }
}

public record DetailCompareResultInfo(float[] CosList, AccuracyLossInfo[] AccuracyLossInfos)
{
    public int[] Shape => AccuracyLossInfos.Head().Shape;

    public IEnumerable<CompareResultByChannel> Enumerable()
    {
        var tensorShape = Shape;
        var (channels, size) = GetShapeInfo(tensorShape, GetChannelAxis(tensorShape));
        return System.Linq.Enumerable.Range(0, channels).Select(c => new CompareResultByChannel(CosList[c], AccuracyLossInfos[(c * size)..((c + 1) * size)]));
    }
}

public record DetailCompareResult(DetailCompareResultInfo[] Infos)
{
    public bool IsTuple => Infos.Length == 1;

    public IEnumerable<CompareResultByChannel> Enumerable()
    {
        return Infos.Aggregate(
            System.Linq.Enumerable.Empty<CompareResultByChannel>(),
            (channels, info) => channels.Concat(info.Enumerable()));
    }
}

public record CompareResultByChannel(float Cos, AccuracyLossInfo[] LossInfo)
{
    public int[] Shape => LossInfo.Head().Shape;

    // todo: more analysis strategy
    public AccuracyLossInfo[] Losses => LossInfo.Where(deviation =>
        deviation.Ratio > 1.3 || deviation.Ratio < 0.7).ToArray();

    public bool IsOk(float thresh)
    {
        return Cos < thresh || Losses.Length != 0;
    }

    public override string ToString()
    {
        var err = Losses;
        var percent = (float)err.Length / new Shape(Shape).Prod().FixedValue;
        return $"CompareResultByChannel Cos:{Cos} \nLossCount/InputSize: {percent}\nLoss:\n{SerializeByColumn(err)}";
    }
}

public record AccuracyLossInfo(float V1, float V2, int[] Index, OriginValue V1Tensor, OriginValue V2Tensor)
{
    public int[] Shape => V1Tensor.Value.AsTensor().Shape.ToValueArray();

    public float Loss => Math.Abs(V1 - V2);

    // todo: v2 is 0.0000
    public float Ratio => (V1 == 0 && V2 == 0) || (Math.Abs(V1) < 1e-9 && V2 == 0) ? 1 : Math.Abs(V1 / V2);

    public override string ToString()
    {
        return
            $"AccuracyLossInfo v1 = {V1}, v2 = {V2}, index =[{string.Join(",", Index.Select(x => x.ToString()))}], devi = {Loss}, ratio = {Ratio}";
    }
}

public static class ComparatorInstance
{
    public static void MatmulOnly(string dumpResultRoot, string evaluatorDataPath, string runtimeDataPath)
    {
        var threshold = 0.96;
        var cosRoot = PathJoinByCreate(dumpResultRoot, "matmul_cos");
        var e = new TextDataExtractor();
        var originData = e.MatmulExtract(evaluatorDataPath);
        var runtimeData = e.MatmulExtract(runtimeDataPath);

        var resultRoot = PathJoinByCreate(cosRoot, "result");
        DetailComparator.GenerateFullCompareInfo(originData, runtimeData, resultRoot);
        var failedValues = originData.Zip(runtimeData).Where(tuple =>
            Comparator.CosSimilarity(tuple.Item1.Value, tuple.Item2.Value)
                .All(cos => cos < threshold));
        var cosData = originData.Zip(runtimeData).Select(tuple =>
        {
            var cos = Comparator.CosSimilarity(tuple.Item1.Value.AsTensor(), tuple.Item2.Value.AsTensor());
            return (cos, tuple.Item1.FileName, tuple.Item2.FileName);
        });
        WriteResult(Path.Join(cosRoot, "ErrorPath"), failedValues.Select(tuple => tuple.Item1.Path).ToArray());

        // var cosByTensor = Comparator.CosSimilarity(originData.Select(x => x.AsTensor()).ToArray(), runtimeData.Select(x => x.AsTensor()).ToArray());
        WriteResult(Path.Join(cosRoot, $"!cos"), cosData.ToArray());
    }
}