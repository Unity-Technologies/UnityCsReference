// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class SemVersionExtension
    {
        public static string StripTag(this SemVersion version)
        {
            return $"{version.Major}.{version.Minor}.{version.Patch}";
        }

        public static string ShortVersion(this SemVersion version)
        {
            return $"{version.Major}.{version.Minor}";
        }

        public static bool IsPatchOf(this SemVersion version, SemVersion? olderVersion)
        {
            return version.Major == olderVersion?.Major && version.Minor == olderVersion?.Minor && version > olderVersion;
        }

        public static bool IsMajorMinorPatchEqualTo(this SemVersion version, SemVersion? compareVersion)
        {
            return version.Major == compareVersion?.Major && version.Minor == compareVersion?.Minor && version.Patch == compareVersion?.Patch;
        }

        public static bool IsNotPreReleaseOrExperimental(this SemVersion version)
        {
            return string.IsNullOrEmpty(version.Prerelease) && !version.IsExperimental();
        }

        public static bool HasPreReleaseVersionTag(this SemVersion version)
        {
            return version.Prerelease?.StartsWith("pre.") == true;
        }

        public static bool IsExperimental(this SemVersion version)
        {
            return version.Major == 0 || version.Prerelease?.StartsWith("exp.") == true;
        }

        public static bool IsHigherPreReleaseIterationOf(this SemVersion version, SemVersion? otherVersion)
        {
            if (string.IsNullOrEmpty(version.Prerelease) || string.IsNullOrEmpty(otherVersion?.Prerelease)
                    || otherVersion?.IsMajorMinorPatchEqualTo(version) != true)
                return false;

            var thisPrereleaseSplit = version.Prerelease.Split('.');
            var otherPrereleaseSplit = otherVersion?.Prerelease.Split('.');

            if (thisPrereleaseSplit.Length < 2 || otherPrereleaseSplit.Length < 2)
                return false;
            return int.TryParse(thisPrereleaseSplit[1], out var thisPrereleaseIteration)
                && int.TryParse(otherPrereleaseSplit[1], out var otherPrereleaseIteration)
                && thisPrereleaseIteration > otherPrereleaseIteration;
        }
    }
}
