// Copyright (c) Canaan Inc. All rights reserved.
// Licensed under the Apache license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Nncase.IR.Utility;

namespace Nncase.IR.Tensors
{
    /// <summary>
    /// ReduceWindow2D.
    /// </summary>
    public sealed record ReduceWindow2D(ReduceOp ReduceOp) : Op
    {
        /// <summary>
        /// Gets input.
        /// </summary>
        public static readonly ParameterInfo Input = new(typeof(ReduceWindow2D), 0, "input", HasRank(4));

        /// <summary>
        /// Get initValue.
        /// </summary>
        public static readonly ParameterInfo InitValue = new(typeof(ReduceWindow2D), 1, "initValue", IsScalar());

        /// <summary>
        /// Get filter.
        /// </summary>
        public static readonly ParameterInfo Filter = new(typeof(ReduceWindow2D), 2, "filter", HasRank(1) & IsIntegral());

        /// <summary>
        /// Gets Stride.
        /// </summary>
        public static readonly ParameterInfo Stride = new(typeof(ReduceWindow2D), 3, "stride", HasRank(1) & IsIntegral());

        /// <summary>
        /// Gets Padding.
        /// </summary>
        public static readonly ParameterInfo Padding = new(typeof(ReduceWindow2D), 4, "padding", HasRank(2) & IsIntegral());

        /// <summary>
        /// Gets CeilMode.
        /// </summary>
        public static readonly ParameterInfo CeilMode = new(typeof(ReduceWindow2D), 6, "ceilMode", IsBool());

        /// <summary>
        /// Gets CountIncludePad.
        /// </summary>
        public static readonly ParameterInfo CountIncludePad = new(typeof(ReduceWindow2D), 6, "countIncludePad", IsBool());

        /// <inheritdoc/>
        public IRType InferInvokeResultType(ITypeInferenceContext context, TensorType input, TensorType initValue, TensorType filter, TensorType stride, TensorType padding, TensorType ceilMode, TensorType countIncludePad)
        {
            var outshape = input.Shape.ToList();
            if (
            context.GetArgument(this, Filter) is Const filter_con &&
            context.GetArgument(this, Stride) is Const stride_con &&
            context.GetArgument(this, Padding) is Const padding_con &&
            context.GetArgument(this, CeilMode) is Const ceilModeValue
            )
            {
                var ts_filter = filter_con.ToTensor<int>();
                var ts_stride = stride_con.ToTensor<int>();
                var ceilModeV = ceilModeValue.ToScalar<bool>();
                var ts_padding = padding_con.ToTensor<int>();
                var padh = ts_padding[0, 0] + ts_padding[0, 1];
                var padw = ts_padding[1, 0] + ts_padding[1, 1];
                outshape[2] = input.Shape[2].IsUnknown ? Dimension.Unknown : GetWindowedOutputSize(input.Shape[2].FixedValue + padh, ts_filter[0], ts_stride[0], 1, false, ceilModeV);
                outshape[3] = input.Shape[3].IsUnknown ? Dimension.Unknown : GetWindowedOutputSize(input.Shape[3].FixedValue + padw, ts_filter[1], ts_stride[1], 1, false, ceilModeV);

                return input with { Shape = new Shape(outshape) };
            }
            return new InvalidType("Can't Infer Shape With Dynamic Input!");
        }
    }
}