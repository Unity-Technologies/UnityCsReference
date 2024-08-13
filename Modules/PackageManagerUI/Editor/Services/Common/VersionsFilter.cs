// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class VersionsFilter
    {
        public static UpmVersionList GetFilteredVersionList(UpmVersionList versionList, bool seeAllVersions, bool showPreRelease)
        {
            if (seeAllVersions || versionList.Any(v => v.availableRegistry != RegistryType.UnityRegistry))
                return versionList;

            var packageTagsToExclude = PackageTag.PreRelease | PackageTag.Experimental;

            var installedVersion = versionList.installed;
            if (showPreRelease || installedVersion?.HasTag(PackageTag.PreRelease | PackageTag.Experimental) == true)
                packageTagsToExclude &= ~PackageTag.PreRelease;

            // should see updates to the installed experimental packages, if they exist
            if (installedVersion?.HasTag(PackageTag.Experimental) == true)
                packageTagsToExclude &= ~PackageTag.Experimental;

            var numVersionsFilteredOut = 0;
            var filteredVersions = new List<UpmPackageVersion>();
            foreach (var version in versionList.Cast<UpmPackageVersion>())
            {
                if (version.isInstalled || !version.HasTag(packageTagsToExclude))
                    filteredVersions.Add(version);
                else
                    ++numVersionsFilteredOut;
            }
            if (numVersionsFilteredOut <= 0)
                return versionList;
            return new UpmVersionList(filteredVersions, versionList.recommended?.versionString);
        }

        public static UpmVersionList UnloadVersionsIfNeeded(UpmVersionList versionList, bool loadAllVersions)
        {
            if (loadAllVersions)
                return versionList;
            var numTotalVersions = versionList.Count();
            if (numTotalVersions == 0)
                return versionList;
            var keyVersions = versionList.key.Cast<UpmPackageVersion>().ToArray();
            var numVersionsToUnload = numTotalVersions - keyVersions.Length;
            if (numVersionsToUnload <= 0)
                return versionList;
            return new UpmVersionList(keyVersions, versionList.recommended?.versionString, numVersionsToUnload);
        }
    }
}
