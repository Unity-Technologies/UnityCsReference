// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.PackageManager.UI
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

        public static bool IsRelease(this SemVersion version)
        {
            return string.IsNullOrEmpty(version.Prerelease) && version.Major != 0;
        }
    }
}
