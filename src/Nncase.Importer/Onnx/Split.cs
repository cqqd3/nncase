// Copyright (c) Canaan Inc. All rights reserved.
// Licensed under the Apache license. See LICENSE file in the project root for full license information.

using System;
using Nncase.IR;
using Nncase.IR.Tensors;
using Onnx;
using F = Nncase.IR.F;

namespace Nncase.Importer
{
    public partial class OnnxImporter
    {
        private Expr VisitSplit(in NodeProto op)
        {
            return GetOpSet(op) <= 13
                ? SplitV11(op)
                : SplitV13(op);
        }

        private Expr SplitV11(in NodeProto op)
        {
            var input = GetInputExpr(op, 0);
            var axis = GetIntAttribute(op, "axis", 0);

            // inShape[axis] / outputSize
            var split = GetOptionIntsAttribute(op, "split")
                .Map(x => (Expr)Tensor.FromSpan<long>(x))
                .Or(ComputeSplit(input, op.Output.Count));
            return F.Tensors.Split(input, axis, split);
        }

        private Expr SplitV13(in NodeProto op)
        {
            var input = GetInputExpr(op, 0);
            var axis = GetIntAttribute(op, "axis", 0);
            var split = GetOptionInputExpr(op, 1)
                .Or(ComputeSplit(input, op.Output.Count));
            return F.Tensors.Split(input, axis, split);
        }

        private Expr ComputeSplit(Expr input, int outputSize)
        {
            return F.Tensors.Expand(
                F.Tensors.Rank(input) / outputSize,
                new[] { outputSize });
        }
    }
}