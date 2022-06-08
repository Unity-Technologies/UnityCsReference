// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal static class LifecycleVersonsFilter
    {
        public static UpmVersionList GetFilteredVersionList(UpmVersionList versonList, bool seeAllVersions, bool showPreRelease)
        {
            // Only filter on Lifecycle tags if is a Unity package and the `seeAllVersions` option is not checked
            if (seeAllVersions || !versonList.Any(v => v.isUnityPackage))
                return versonList;

            var packageTagsToKeep = PackageTag.Release | PackageTag.ReleaseCandidate;

            var installedVersion = versonList.installed;
            if (showPreRelease || installedVersion?.HasTag(PackageTag.PreRelease | PackageTag.Experimental) == true)
                packageTagsToKeep |= PackageTag.PreRelease;

            // should see updates to the installed experimental packages, if they exist
            if (installedVersion?.HasTag(PackageTag.Experimental) == true)
                packageTagsToKeep |= PackageTag.Experimental;

            var numVersionsFilteredOut = 0;
            var filteredVersions = new List<UpmPackageVersion>();
            foreach (var version in versonList.Cast<UpmPackageVersion>())
            {
                if (version.isInstalled || version.HasTag(packageTagsToKeep))
                    filteredVersions.Add(version);
                else
                    ++numVersionsFilteredOut;
            }
            return numVersionsFilteredOut == 0 ? versonList : new UpmVersionList(filteredVersions, versonList.lifecycleVersionString, versonList.lifecycleNextVersion);
        }
    }
}
