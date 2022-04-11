// Copyright (c) Canaan Inc. All rights reserved.
// Licensed under the Apache license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using NetFabric.Hyperlinq;
using Nncase.IR;
using Nncase.IR.Tensors;
using OrtKISharp;
using Range = Nncase.IR.Tensors.Range;
using static Nncase.Evaluator.TypeInference;
namespace Nncase.Evaluator.Tensors;

/// <summary>
/// Evaluator for <see cref="Range"/>.
/// </summary>
public class ReshapeEvaluator : IEvaluator<Reshape>, ITypeInferencer<Reshape>
{
    /// <inheritdoc/>
    public IValue Visit(IEvaluateContext context, Reshape reshape)
    {
        var input = context.GetOrtArgumentValue(reshape, Reshape.Input);
        var shape = context.GetInt64OrtTensorArgumentValue(reshape, Reshape.Shape);
        return OrtKI.Reshape(input, shape, 0).ToValue();
    }

    /// <inheritdoc/>
    public IRType Visit(ITypeInferenceContext context, Reshape target)
    {
        var input = context.CheckArgumentType<TensorType>(target, Reshape.Input);
        return Visit(context, target, input);
    }

    private IRType Visit(ITypeInferenceContext context, Reshape target, TensorType input)
    {
        if (context.GetArgument(target, Reshape.Shape) is TensorConst shapeConst &&
            input.Shape.IsFixed)
        {
            var shapeValue = shapeConst.Value.ToArray<int>();
            var negCount = shapeValue.Count(IsMinus1);
            var inputSize = input.Shape.Prod().FixedValue;
            var shapeSize = shapeValue.Aggregate(1, (x, y) => x * y);
            if (negCount > 1)
            {
                return new InvalidType(
                    $"Reshape at most one dimension of the new shape can be -1," +
                    $" shape:{shapeValue}");
            }
            else if (negCount < 1)
            {
                if (inputSize != shapeSize)
                {
                    return new InvalidType("Reshape input shape size and param shape size must be same," +
                                           $" shape:{shapeValue.ToArray().Aggregate("", (s, i) => s + i + " ")}, input shape${input.Shape}");
                }
                return input with { Shape = new Shape(shapeValue) };
            }
            else
            {
                shapeSize = -shapeSize;
                var negIndex = shapeValue.Select((dim, index) => (dim, index)).First(x => IsMinus1(x.dim)).index;
                if (inputSize % shapeSize != 0)
                {
                    return new InvalidType("Reshape input size must be divisible by shapeSize when has -1");
                }
                shapeValue[negIndex] = inputSize / shapeSize;
                return input with {Shape = new Shape(shapeValue)};
            }
        }

        return input with { Shape = Shape.Unranked };
    }
}