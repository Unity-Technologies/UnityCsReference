// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal static class VersionRangesEvaluators<TVersion> where TVersion : struct, IVersion<TVersion>
    {
        public static bool MinimumVersionInclusive(TVersion left, TVersion right, TVersion version)
        {
            return version.Equals(left) || version.CompareTo(left) > 0; // version >= left;
        }

        public static bool MinimumVersionExclusive(TVersion left, TVersion right, TVersion version)
        {
            return version.CompareTo(left) > 0; // version > left;
        }

        public static bool ExactVersionMatch(TVersion left, TVersion right, TVersion version)
        {
            return version.Equals(left); // left == version;
        }

        public static bool MaximumVersionInclusive(TVersion left, TVersion right, TVersion version)
        {
            return version.Equals(right) || version.CompareTo(right) < 0; // version <= right;
        }

        public static bool MaximumVersionExclusive(TVersion left, TVersion right, TVersion version)
        {
            return version.CompareTo(right) < 0; // version < right;
        }

        public static bool ExactRangeInclusive(TVersion left, TVersion right, TVersion version)
        {
            return MinimumVersionInclusive(left, right, version)  // left <= version
                && MaximumVersionInclusive(left, right, version); // && version <= right;
        }

        public static bool ExactRangeExclusive(TVersion left, TVersion right, TVersion version)
        {
            return MinimumVersionExclusive(left, right, version)  // left < version
                && MaximumVersionExclusive(left, right, version); // && version < right;
        }

        public static bool MixedInclusiveMinimumAndExclusiveMaximumVersion(TVersion left, TVersion right, TVersion version)
        {
            return MinimumVersionInclusive(left, right, version)  // left <= version
                && MaximumVersionExclusive(left, right, version); // && version < right;
        }

        public static bool MixedExclusiveMinimumAndInclusiveMaximumVersion(TVersion left, TVersion right, TVersion version)
        {
            return MinimumVersionExclusive(left, right, version)  // left < version
                && MaximumVersionInclusive(left, right, version); // && version <= right;
        }

        public static bool Invalid(string expression)
        {
            throw new ExpressionNotValidException($"Unknown expression: {expression}");
        }
    }
}
