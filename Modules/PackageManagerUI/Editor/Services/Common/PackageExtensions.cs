// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;

namespace UnityEditor.PackageManager.UI
{
    internal static class PackageExtensions
    {
        public static PackageState GetState(this IPackage package)
        {
            if (package.progress != PackageProgress.None)
                return PackageState.InProgress;

            if (package.errors.Any())
                return PackageState.Error;

            var primary = package.primaryVersion;
            if (primary.HasTag(PackageTag.InDevelopment))
                return PackageState.InDevelopment;

            if (primary != package.recommendedVersion)
                return PackageState.Outdated;

            if (package.versionList.importAvailable != null)
                return PackageState.ImportAvailable;

            if (package.installedVersion != null)
                return PackageState.Installed;

            return PackageState.UpToDate;
        }
    }
}
