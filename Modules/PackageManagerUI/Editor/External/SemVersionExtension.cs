// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Text.RegularExpressions;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class SemVersionExtension
    {
        private static readonly Regex k_ExpRegex = new Regex(@"^exp(\-(?<feature>[A-Za-z0-9-]+))?(\.(?<iteration>[1-9]\d*))?$");

        public static string StripTag(this SemVersion version)
        {
            return $"{version.Major}.{version.Minor}.{version.Patch}";
        }

        public static string ShortVersion(this SemVersion version)
        {
            return $"{version.Major}.{version.Minor}";
        }

        public static bool IsEqualOrPatchOf(this SemVersion version, SemVersion? olderVersion)
        {
            return version.Major == olderVersion?.Major && version.Minor == olderVersion?.Minor && version >= olderVersion;
        }

        // The implementation here matches with the experimental version tagging check implemented in Package Validation Suite (US0017 CorrectPackageVersionTags)
        public static bool IsExperimental(this SemVersion version)
        {
            if (string.IsNullOrEmpty(version.Prerelease))
                return false;
            var match = k_ExpRegex.Match(version.Prerelease);
            return match.Success && match.Groups["iteration"].Success && match.Groups["feature"].Value.Length <= 10;
        }
    }
}
