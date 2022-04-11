// Copyright (c) Canaan Inc. All rights reserved.
// Licensed under the Apache license. See LICENSE file in the project root for full license information.

using Nncase.IR;
using Nncase.IR.Math;
using OrtKISharp;

namespace Nncase.Evaluator.Math;

/// <summary>
/// Evaluator for <see cref="MatMul"/>.
/// </summary>
public class MatMulEvaluator : IEvaluator<MatMul>, ITypeInferencer<MatMul>
{
    /// <inheritdoc/>
    public IValue Visit(IEvaluateContext context, MatMul matMul)
    {
        var input = context.GetOrtArgumentValue(matMul, MatMul.Lhs);
        var other = context.GetOrtArgumentValue(matMul, MatMul.Rhs);
        return OrtKI.MatMul(input, other).ToValue();
    }

    /// <inheritdoc/>
    public IRType Visit(ITypeInferenceContext context, MatMul target)
    {
        var lhs = context.CheckArgumentType<TensorType>(target, MatMul.Lhs);
        var rhs = context.CheckArgumentType<TensorType>(target, MatMul.Rhs);
        return Visit(lhs, rhs);
    }

    private IRType Visit(TensorType lhs, TensorType rhs)
    {
        if (lhs.Shape.Rank != 2)
        {
            return new InvalidType("MatMul lhs shape rank is not 2");
        }

        if (rhs.Shape.Rank != 2)
        {
            return new InvalidType("MatMul rhs shape rank is not 2");
        }

        if (lhs.Shape[1].IsUnknown || rhs.Shape[0].IsUnknown)
        {
            return new InvalidType("MatMul lhs or rhs shape is unknown");
        }

        if (lhs.Shape[1] != rhs.Shape[0])
        {
            return new InvalidType("MatMul lhs shape[1] != rhs shape[0]");
        }

        return new TensorType(lhs.DType, new[] { lhs.Shape[0], rhs.Shape[1] });
    }
}