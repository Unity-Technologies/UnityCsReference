// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.PackageManager
{
    internal class SemVersionHelper
    {
        public static string MaxSatisfying(String version, string[] versions, ResolutionStrategy resolutionStrategy = ResolutionStrategy.Highest, bool includePrereleases = false)
        {
            SemVersion baseVersion = ParseVersion(version);
            SemVersion? maxValue = null;

            if (versions.Length == 0)
            {
                throw new ArgumentException("No Semver versions to process.");
            }

            foreach (var v in versions)
            {
                SemVersion candidate = ParseVersion(v);

                if (!includePrereleases && !string.IsNullOrEmpty(candidate.Prerelease))
                {
                    continue;
                }

                if (candidate < baseVersion || maxValue > candidate)
                {
                    continue;
                }

                if (!SatisfiesResolutionStrategy(candidate, baseVersion, resolutionStrategy)) {
                    continue;
                }

                maxValue = candidate;
            }

            return maxValue?.ToString();
        }

        private static bool SatisfiesResolutionStrategy(SemVersion candidate, SemVersion version, ResolutionStrategy resolutionStrategy) {
            switch (resolutionStrategy) {
                case ResolutionStrategy.HighestPatch:
                    return candidate.Major == version.Major && candidate.Minor == version.Minor;
                case ResolutionStrategy.HighestMinor:
                    return candidate.Major == version.Major;
                default:
                    return true;
            }
        }

        private static SemVersion ParseVersion(string version)
        {
            return SemVersionParser.Parse(version);
        }
    }
}
