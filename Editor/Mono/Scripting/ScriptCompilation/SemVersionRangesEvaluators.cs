// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal static class SemVersionRangesEvaluators
    {
        public static bool MinimumVersionInclusive(SemVersion left, SemVersion right, SemVersion version)
        {
            return version >= left;
        }

        public static bool MinimumVersionExclusive(SemVersion left, SemVersion right, SemVersion version)
        {
            return version > left;
        }

        public static bool ExactVersionMatch(SemVersion left, SemVersion right, SemVersion version)
        {
            return left == version;
        }

        public static bool MaximumVersionInclusive(SemVersion left, SemVersion right, SemVersion version)
        {
            return version <= right;
        }

        public static bool MaximumVersionExclusive(SemVersion left, SemVersion right, SemVersion version)
        {
            return version < right;
        }

        public static bool ExactRangeInclusive(SemVersion left, SemVersion right, SemVersion version)
        {
            return left <= version && version <= right;
        }

        public static bool ExactRangeExclusive(SemVersion left, SemVersion right, SemVersion version)
        {
            return left < version && version < right;
        }

        public static bool MixedInclusiveMinimumAndExclusiveMaximumVersion(SemVersion left, SemVersion right, SemVersion version)
        {
            return left <= version && version < right;
        }

        public static bool MixedExclusiveMinimumAndInclusiveMaximumVersion(SemVersion left, SemVersion right, SemVersion version)
        {
            return left < version && version <= right;
        }

        public static bool Invalid(string expression)
        {
            throw new ExpressionNotValidException($"Unknown expression: {expression}");
        }
    }
}
