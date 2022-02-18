// Copyright (c) Canaan Inc. All rights reserved.
// Licensed under the Apache license. See LICENSE file in the project root for full license information.

using Nncase.IR;
using Nncase.IR.Math;
using static Tensorflow.Binding;

namespace Nncase.Evaluator.Math;

/// <summary>
/// Evaluator for <see cref="CumSum"/>.
/// </summary>
public class CumSumEvaluator : IEvaluator<CumSum>, ITypeInferencer<CumSum>
{
    /// <inheritdoc/>
    public IValue Visit(IEvaluateContext context, CumSum cumSum)
    {
        var input = context.GetTFArgumentValue(cumSum, CumSum.Input);

        // in onnx, CumSum.Axis is a input tensor with one value
        var axis = context.GetArgumentValueAsTensor<int>(cumSum, CumSum.Axis)[0];
        var exclusive = context.GetArgumentValueAsScalar<bool>(cumSum, CumSum.Exclusive);
        var reverse = context.GetArgumentValueAsScalar<bool>(cumSum, CumSum.Reverse);
        return tf.cumsum(input, axis, exclusive, reverse).ToValue();
    }

    /// <inheritdoc/>
    public IRType Visit(ITypeInferenceContext context, CumSum target)
    {
        var input = context.CheckArgumentType<TensorType>(target, CumSum.Input);
        return Visit(input);
    }

    private IRType Visit(TensorType input)
    {
        return input;
    }
}