// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.Connect
{
    static class PackageHelper
    {
        const string k_CorePackageName = "com.unity.services.core";

        public static bool IsCorePackageInstalled()
        {
            return IsPackageInstalled(k_CorePackageName);
        }

        public static bool IsPackageInstalled(string packageName)
        {
            return GetPackageInfo(packageName) != null;
        }

        static PackageManager.PackageInfo GetPackageInfo(string packageName)
        {
            var packageInfos = PackageManager.PackageInfo.GetAllRegisteredPackages();
            return packageInfos.FirstOrDefault(packageInfo => packageInfo.name.Equals(packageName));
        }

        public static bool HasCoreDependency(string packageName)
        {
            var packageInfo = GetPackageInfo(packageName);

            return packageInfo != null && HasDependencyToPackage(packageInfo, k_CorePackageName);
        }

        static bool HasDependencyToPackage(PackageManager.PackageInfo packageInfo, string dependencyPackageName)
        {
            return packageInfo.dependencies.Any(dependencyInfo => dependencyInfo.name.Equals(dependencyPackageName));
        }

        public static bool IsInstalledPackageAtMinimumVersionOrHigher(string packageName, string minimumVersionString)
        {
            bool installedPackageIsAtMinimumVersionOrHigher = false;

            var packageInfo = GetPackageInfo(packageName);
            if (packageInfo != null)
            {
                var packageVersion = new SemVersion().Parse(packageInfo.version);
                var minimumVersion = new SemVersion().Parse(minimumVersionString);

                installedPackageIsAtMinimumVersionOrHigher = SemVersion.Compare(packageVersion, minimumVersion) >= 0;
            }

            return installedPackageIsAtMinimumVersionOrHigher;
        }
    }
}
