﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nncase.PatternMatch;
using static Nncase.IR.TypePatternUtility;

namespace Nncase.IR.K210;

/// <summary>
/// KPU Download.
/// </summary>
[PatternFunctionalGenerator]
public sealed record class KPUDownload : Op
{
    /// <summary>
    /// Gets input.
    /// </summary>
    public static readonly ParameterInfo Input = new(typeof(KPUDownload), 0, "input", HasRank(4));
}