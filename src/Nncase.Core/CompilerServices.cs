﻿// Copyright (c) Canaan Inc. All rights reserved.
// Licensed under the Apache license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonServiceLocator;
using Microsoft.Extensions.DependencyInjection;
using Nncase.Evaluator;
using Nncase.IR;

namespace Nncase;

/// <summary>
/// Compiler services provider.
/// </summary>
public interface ICompilerServicesProvider
{
    /// <summary>
    /// Inference type of the expression tree.
    /// </summary>
    /// <param name="expr">Expression.</param>
    /// <returns>Is fully inferenced.</returns>
    bool InferenceType(Expr expr);

    /// <summary>
    /// Inference operator.
    /// </summary>
    /// <param name="op">Target operator.</param>
    /// <param name="context">Inference context.</param>
    /// <returns>Inference result.</returns>
    IRType InferenceOp(Op op, ITypeInferenceContext context);

    /// <summary>
    /// Evaluate the expression tree.
    /// </summary>
    /// <param name="expr">Expression.</param>
    /// <param name="varsValues">Optional vars' values.</param>
    /// <returns>Evaluate result.</returns>
    IValue Evaluate(Expr expr, IReadOnlyDictionary<Var, IValue>? varsValues = null);

    /// <summary>
    /// Evaluate operator.
    /// </summary>
    /// <param name="op">Target operator.</param>
    /// <param name="context">Evaluate context.</param>
    /// <returns>Evaluate result.</returns>
    IValue EvaluateOp(Op op, IEvaluateContext context);
}

internal interface ICompilerServicesProviderInternal
{
    IDataTypeServiceProvider DataTypeService { get; }
}

internal class CompilerServicesProvider : ICompilerServicesProvider, ICompilerServicesProviderInternal
{
    private readonly IEvaluateProvider _evaluateProvider;
    private readonly ITypeInferenceProvider _typeInferenceProvider;

    public CompilerServicesProvider(
        IEvaluateProvider evaluateProvider,
        ITypeInferenceProvider typeInferenceProvider,
        IDataTypeServiceProvider dataTypeServiceProvider)
    {
        _evaluateProvider = evaluateProvider;
        _typeInferenceProvider = typeInferenceProvider;
        DataTypeService = dataTypeServiceProvider;
    }

    public IDataTypeServiceProvider DataTypeService { get; }

    /// <inheritdoc/>
    public IValue Evaluate(Expr expr, IReadOnlyDictionary<Var, IValue>? varsValues = null)
    {
        return _evaluateProvider.Evaluate(expr, varsValues);
    }

    /// <inheritdoc/>
    public IValue EvaluateOp(Op op, IEvaluateContext context)
    {
        return _evaluateProvider.EvaluateOp(op, context);
    }

    /// <inheritdoc/>
    public IRType InferenceOp(Op op, ITypeInferenceContext context)
    {
        return _typeInferenceProvider.InferenceOp(op, context);
    }

    /// <inheritdoc/>
    public bool InferenceType(Expr expr)
    {
        return _typeInferenceProvider.InferenceType(expr);
    }
}

/// <summary>
/// Compiler services.
/// </summary>
public static class CompilerServices
{
    private static ICompilerServicesProvider? _provider;

    internal static IDataTypeServiceProvider DataTypeService => ((ICompilerServicesProviderInternal)Provider).DataTypeService;

    private static ICompilerServicesProvider Provider => _provider ?? throw new InvalidOperationException("Compiler services provider must be set.");

    /// <summary>
    /// Configure compiler services.
    /// </summary>
    /// <param name="provider">Service provider.</param>
    public static void Configure(ICompilerServicesProvider provider)
    {
        _provider = provider;
    }

    /// <summary>
    /// Inference type of the expression tree.
    /// </summary>
    /// <param name="expr">Expression.</param>
    /// <returns>Is fully inferenced.</returns>
    public static bool InferenceType(this Expr expr)
    {
        return Provider.InferenceType(expr);
    }

    /// <summary>
    /// Inference operator.
    /// </summary>
    /// <param name="op">Target operator.</param>
    /// <param name="context">Inference context.</param>
    /// <returns>Inference result.</returns>
    public static IRType InferenceOp(Op op, ITypeInferenceContext context)
    {
        return Provider.InferenceOp(op, context);
    }

    /// <summary>
    /// Evaluate the expression tree.
    /// </summary>
    /// <param name="expr">Expression.</param>
    /// <param name="varsValues">Optional vars' values.</param>
    /// <returns>Evaluate result.</returns>
    public static IValue Evaluate(this Expr expr, IReadOnlyDictionary<Var, IValue>? varsValues = null)
    {
        return Provider.Evaluate(expr, varsValues);
    }

    /// <summary>
    /// Evaluate operator.
    /// </summary>
    /// <param name="op">Target operator.</param>
    /// <param name="context">Evaluate context.</param>
    /// <returns>Evaluate result.</returns>
    public static IValue EvaluateOp(Op op, IEvaluateContext context)
    {
        return Provider.EvaluateOp(op, context);
    }
}