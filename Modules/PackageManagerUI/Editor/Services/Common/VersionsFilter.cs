// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class VersionsFilter
    {
        public static UpmVersionList GetFilteredVersionList(UpmVersionList versonList, bool seeAllVersions, bool showPreRelease)
        {
            // Only filter on Lifecycle tags if is a Unity package and the `seeAllVersions` option is not checked
            if (seeAllVersions || !versonList.Any(v => v.isUnityPackage))
                return versonList;

            var packageTagsToExclude = PackageTag.PreRelease | PackageTag.Experimental;

            var installedVersion = versonList.installed;
            if (showPreRelease || installedVersion?.HasTag(PackageTag.PreRelease | PackageTag.Experimental) == true)
                packageTagsToExclude &= ~PackageTag.PreRelease;

            // should see updates to the installed experimental packages, if they exist
            if (installedVersion?.HasTag(PackageTag.Experimental) == true)
                packageTagsToExclude &= ~PackageTag.Experimental;

            var numVersionsFilteredOut = 0;
            var filteredVersions = new List<UpmPackageVersion>();
            foreach (var version in versonList.Cast<UpmPackageVersion>())
            {
                if (version.isInstalled || !version.HasTag(packageTagsToExclude))
                    filteredVersions.Add(version);
                else
                    ++numVersionsFilteredOut;
            }
            if (numVersionsFilteredOut <= 0)
                return versonList;
            return new UpmVersionList(filteredVersions, versonList.lifecycleVersionString, versonList.lifecycleNextVersion);
        }

        public static UpmVersionList UnloadVersionsIfNeeded(UpmVersionList versonList, bool loadAllVersions)
        {
            if (loadAllVersions)
                return versonList;
            var numTotalVersions = versonList.Count();
            if (numTotalVersions == 0)
                return versonList;
            var keyVersions = versonList.key.Cast<UpmPackageVersion>().ToArray();
            var numVersionsToUnload = numTotalVersions - keyVersions.Length;
            if (numVersionsToUnload <= 0)
                return versonList;
            return new UpmVersionList(keyVersions, versonList.lifecycleVersionString, versonList.lifecycleNextVersion, numVersionsToUnload);
        }
    }
}
