// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal struct ExpressionTypeValue<TVersion> where TVersion : struct, IVersion<TVersion>
    {
        public Func<TVersion, TVersion, TVersion, bool> IsValid { get; set; }
        public string AppliedRule { get; set; }
    }

    internal class ExpressionTypeFactory<TVersion> where TVersion : struct, IVersion<TVersion>
    {
        public static Dictionary<ExpressionTypeKey, ExpressionTypeValue<TVersion>> Create()
        {
            return new Dictionary<ExpressionTypeKey, ExpressionTypeValue<TVersion>>
            {
                {
                    new ExpressionTypeKey(hasLeftVersion: true),
                    new ExpressionTypeValue<TVersion>
                    {
                        IsValid = VersionRangesEvaluators<TVersion>.MinimumVersionInclusive,
                        AppliedRule = "x >= {0}",
                    }
                },
                {
                    new ExpressionTypeKey(leftSymbol: '(', rightSymbol: ')', hasSeparator: true, hasLeftVersion: true),
                    new ExpressionTypeValue<TVersion>
                    {
                        IsValid = VersionRangesEvaluators<TVersion>.MinimumVersionExclusive,
                        AppliedRule = "x > {0}",
                    }
                },
                {
                    new ExpressionTypeKey(leftSymbol: '[', rightSymbol: ']', hasLeftVersion: true),
                    new ExpressionTypeValue<TVersion>
                    {
                        IsValid = VersionRangesEvaluators<TVersion>.ExactVersionMatch,
                        AppliedRule = "x == {0}",
                    }
                },
                {
                    new ExpressionTypeKey(leftSymbol: '(', rightSymbol: ']', hasSeparator: true, hasRightVersion: true),
                    new ExpressionTypeValue<TVersion>
                    {
                        IsValid = VersionRangesEvaluators<TVersion>.MaximumVersionInclusive,
                        AppliedRule = "x <= {1}",
                    }
                },
                {
                    new ExpressionTypeKey(leftSymbol: '(', rightSymbol: ']', hasSeparator: true, hasLeftVersion: true, hasRightVersion: true),
                    new ExpressionTypeValue<TVersion>
                    {
                        IsValid = VersionRangesEvaluators<TVersion>.MixedExclusiveMinimumAndInclusiveMaximumVersion,
                        AppliedRule = "{0} < x <= {1}",
                    }
                },
                {
                    new ExpressionTypeKey(leftSymbol: '(', rightSymbol: ')', hasSeparator: true, hasRightVersion: true),
                    new ExpressionTypeValue<TVersion>
                    {
                        IsValid = VersionRangesEvaluators<TVersion>.MaximumVersionExclusive,
                        AppliedRule = "x < {1}",
                    }
                },
                {
                    new ExpressionTypeKey(leftSymbol: '[', rightSymbol: ']', hasSeparator: true, hasLeftVersion: true, hasRightVersion: true),
                    new ExpressionTypeValue<TVersion>
                    {
                        IsValid = VersionRangesEvaluators<TVersion>.ExactRangeInclusive,
                        AppliedRule = "{0} <= x <= {1}",
                    }
                },
                {
                    new ExpressionTypeKey(leftSymbol: '(', rightSymbol: ')', hasSeparator: true, hasLeftVersion: true, hasRightVersion: true),
                    new ExpressionTypeValue<TVersion>
                    {
                        IsValid = VersionRangesEvaluators<TVersion>.ExactRangeExclusive,
                        AppliedRule = "{0} < x < {1}",
                    }
                },
                {
                    new ExpressionTypeKey(leftSymbol: '[', rightSymbol: ')', hasSeparator: true, hasLeftVersion: true, hasRightVersion: true),
                    new ExpressionTypeValue<TVersion>
                    {
                        IsValid = VersionRangesEvaluators<TVersion>.MixedInclusiveMinimumAndExclusiveMaximumVersion,
                        AppliedRule = "{0} <= x < {1}",
                    }
                },
                {
                    new ExpressionTypeKey(leftSymbol: '(', rightSymbol: ')', hasLeftVersion: true),
                    new ExpressionTypeValue<TVersion>
                    {
                        IsValid = (left, right, version) => VersionRangesEvaluators<TVersion>.Invalid($"({version})"),
                        AppliedRule = "Invalid",
                    }
                },
            };
        }
    }
}
