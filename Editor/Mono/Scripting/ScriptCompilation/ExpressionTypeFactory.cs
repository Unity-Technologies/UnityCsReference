// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.VFX;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal struct ExpressionTypeValue
    {
        public Func<SemVersion, SemVersion, SemVersion, bool> IsValid { get; set; }
        public string AppliedRule { get; set; }
    }

    internal class ExpressionTypeFactory
    {
        public static Dictionary<ExpressionTypeKey, ExpressionTypeValue> Create()
        {
            return new Dictionary<ExpressionTypeKey, ExpressionTypeValue>
            {
                {
                    new ExpressionTypeKey(hasLeftSemVer: true),
                    new ExpressionTypeValue
                    {
                        IsValid = SemVersionRangesEvaluators.MinimumVersionInclusive,
                        AppliedRule = "x >= {0}",
                    }
                },
                {
                    new ExpressionTypeKey(leftSymbol: '(', rightSymbol: ')', hasSeparator: true, hasLeftSemVer: true),
                    new ExpressionTypeValue
                    {
                        IsValid = SemVersionRangesEvaluators.MinimumVersionExclusive,
                        AppliedRule = "x > {0}",
                    }
                },
                {
                    new ExpressionTypeKey(leftSymbol: '[', rightSymbol: ']', hasLeftSemVer: true),
                    new ExpressionTypeValue
                    {
                        IsValid = SemVersionRangesEvaluators.ExactVersionMatch,
                        AppliedRule = "x == {0}",
                    }
                },
                {
                    new ExpressionTypeKey(leftSymbol: '(', rightSymbol: ']', hasSeparator: true, hasRightSemVer: true),
                    new ExpressionTypeValue
                    {
                        IsValid = SemVersionRangesEvaluators.MaximumVersionInclusive,
                        AppliedRule = "x <= {1}",
                    }
                },
                {
                    new ExpressionTypeKey(leftSymbol: '(', rightSymbol: ']', hasSeparator: true, hasLeftSemVer: true, hasRightSemVer: true),
                    new ExpressionTypeValue
                    {
                        IsValid = SemVersionRangesEvaluators.MixedExclusiveMinimumAndInclusiveMaximumVersion,
                        AppliedRule = "{0} < x <= {1}",
                    }
                },
                {
                    new ExpressionTypeKey(leftSymbol: '(', rightSymbol: ')', hasSeparator: true, hasRightSemVer: true),
                    new ExpressionTypeValue
                    {
                        IsValid = SemVersionRangesEvaluators.MaximumVersionExclusive,
                        AppliedRule = "x < {1}",
                    }
                },
                {
                    new ExpressionTypeKey(leftSymbol: '[', rightSymbol: ']', hasSeparator: true, hasLeftSemVer: true, hasRightSemVer: true),
                    new ExpressionTypeValue
                    {
                        IsValid = SemVersionRangesEvaluators.ExactRangeInclusive,
                        AppliedRule = "{0} <= x <= {1}",
                    }
                },
                {
                    new ExpressionTypeKey(leftSymbol: '(', rightSymbol: ')', hasSeparator: true, hasLeftSemVer: true, hasRightSemVer: true),
                    new ExpressionTypeValue
                    {
                        IsValid = SemVersionRangesEvaluators.ExactRangeExclusive,
                        AppliedRule = "{0} < x < {1}",
                    }
                },
                {
                    new ExpressionTypeKey(leftSymbol: '[', rightSymbol: ')', hasSeparator: true, hasLeftSemVer: true, hasRightSemVer: true),
                    new ExpressionTypeValue
                    {
                        IsValid = SemVersionRangesEvaluators.MixedInclusiveMinimumAndExclusiveMaximumVersion,
                        AppliedRule = "{0} <= x < {1}",
                    }
                },
                {
                    new ExpressionTypeKey(leftSymbol: '(', rightSymbol: ')', hasLeftSemVer: true),
                    new ExpressionTypeValue
                    {
                        IsValid = (left, right, version) => SemVersionRangesEvaluators.Invalid($"({version})"),
                        AppliedRule = "Invalid",
                    }
                },
            };
        }
    }
}
