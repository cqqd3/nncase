// Copyright (c) Canaan Inc. All rights reserved.
// Licensed under the Apache license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Nncase.IR;
using F = Nncase.IR.F;
using TensorType = tflite.TensorType;

namespace Nncase.Importer.TFLite
{
    public partial class TFLiteImporter
    {
        private Expr VisitConv2D(in tflite.Operator op)
        {
            var (input, weights) = GetInputExprs(op, 0, 1);
            var bias = GetInputExprs(op, 2);
            var options = op.BuiltinOptionsAsConv2DOptions();
            var inH = GetInputTensor(op, 0).Shape(2);
            var inW = GetInputTensor(op, 0).Shape(3);
            var fH = GetInputTensor(op, 1).Shape(2);
            var fW = GetInputTensor(op, 1).Shape(3);
            var strideH = options.StrideH;
            var strideW = options.StrideW;
            var dilationH = options.DilationHFactor;
            var dilationW = options.DilationWFactor;
            var padH = GetWindowedPadding(inH, fH, strideH, dilationH, options.Padding == tflite.Padding.SAME);
            var padW = GetWindowedPadding(inW, fW, strideW, dilationW, options.Padding == tflite.Padding.SAME);
            var paddingValue = padH.Concat(padW).ToArray();
            var stride = Const.FromSpan<int>(new[] { strideH, strideW }, new[] { 2 });
            var dilation = Const.FromSpan<int>(new[] { dilationH, dilationW }, new[] { 2 });
            var padding = Const.FromSpan<int>(paddingValue, new[] { 2, 2 });
            var clamp = ToFloatClampRange(options.FusedActivationFunction);
            return F.Math.Clamp(
                F.NN.Conv2D(input, weights, bias, padding, stride, dilation, PadMode.Constant),
                clamp.Min, clamp.Max);
        }

        private Expr VisitDepthwiseConv2D(in tflite.Operator op)
        {
            var (input, weights) = GetInputExprs(op, 0, 1);
            var bias = GetInputExprs(op, 2);
            var options = op.BuiltinOptionsAsDepthwiseConv2DOptions();
            var inH = GetInputTensor(op, 0).Shape(2);
            var inW = GetInputTensor(op, 0).Shape(3);
            var fH = GetInputTensor(op, 1).Shape(2);
            var fW = GetInputTensor(op, 1).Shape(3);
            var strideH = options.StrideH;
            var strideW = options.StrideW;
            var dilationH = options.DilationHFactor;
            var dilationW = options.DilationWFactor;
            var padH = GetWindowedPadding(inH, fH, strideH, dilationH, options.Padding == tflite.Padding.SAME);
            var padW = GetWindowedPadding(inW, fW, strideW, dilationW, options.Padding == tflite.Padding.SAME);
            var paddingValue = padH.Concat(padW).ToArray();
            var stride = Const.FromSpan<int>(new[] { strideH, strideW }, new[] { 2 });
            var dilation = Const.FromSpan<int>(new[] { dilationH, dilationW }, new[] { 2 });
            var padding = Const.FromSpan<int>(paddingValue, new[] { 2, 2 });
            var depthMul = options.DepthMultiplier;
            if (depthMul != 1)
            {
                throw new NotSupportedException("DepthwiseConv2D with depth_multiplier:" + depthMul +
                                                " is not supported");
            }
            var clamp = ToFloatClampRange(options.FusedActivationFunction);
            return F.Math.Clamp(
                F.NN.Conv2D(input, weights, bias, padding, stride, dilation, PadMode.Constant),
                clamp.Min, clamp.Max);
        }
        
        private static ValueRange<float> ToFloatClampRange(tflite.ActivationFunctionType func) => func switch
        {
            tflite.ActivationFunctionType.NONE => ValueRange<float>.Full,
            tflite.ActivationFunctionType.RELU => (0f, float.PositiveInfinity),
            tflite.ActivationFunctionType.RELU_N1_TO_1 => (-1f, 1f),
            tflite.ActivationFunctionType.RELU6 => (0f, 6f),
            _ => throw new NotSupportedException("Unsupported Activation:" + func),
        };

        private static int GetWindowedOutputSize(int size, int filter, int stride, int dilation, bool same,
            bool ceilMode = false)
        {
            var effectiveFilterSize = ((filter - 1) * dilation) + 1;
            if (same)
            {
                return (int)(((uint)size + stride - 1) / stride);
            }
            else
            {
                if (!ceilMode)
                {
                    return (int)(((uint)size - effectiveFilterSize + stride) / stride);
                }
                else
                {
                    return (int)Math.Ceiling((float)((uint)size - effectiveFilterSize + stride) / stride);
                }
            }
        }

        private static int[] GetWindowedPadding(int inputSize, int filter, int stride, int dilation, bool same)
        {
            var outputSize = GetWindowedOutputSize(inputSize, filter, stride, dilation, same);
            return GetWindowedPadding(inputSize, outputSize, filter, stride, dilation);
        }

        private static int[] GetWindowedPadding(int inputSize, int outputSize, int filter, int stride, int dilation)
        {
            var effectiveFilterSize = ((filter - 1) * dilation) + 1;
            var padding = Math.Max(0, ((outputSize - 1) * stride) + effectiveFilterSize - inputSize);
            return new[] { padding / 2, padding - (padding / 2) };
        }
    }
}