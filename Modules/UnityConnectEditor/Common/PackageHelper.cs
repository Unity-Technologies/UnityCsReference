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

        public static bool IsCorePackageRegistered()
        {
            return PackageManager.PackageInfo.IsPackageRegistered(k_CorePackageName);
        }

        public static bool HasCoreDependency(string packageName)
        {
            var packageInfo = PackageManager.PackageInfo.FindForPackageName(packageName);
            return packageInfo != null && HasDependencyToPackage(packageInfo, k_CorePackageName);
        }

        static bool HasDependencyToPackage(PackageManager.PackageInfo packageInfo, string dependencyPackageName)
        {
            return packageInfo.dependencies.Any(dependencyInfo => dependencyInfo.name.Equals(dependencyPackageName));
        }

        public static bool IsInstalledPackageAtMinimumVersionOrHigher(string packageName, string minimumVersionString)
        {
            bool installedPackageIsAtMinimumVersionOrHigher = false;

            var packageInfo = PackageManager.PackageInfo.FindForPackageName(packageName);
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
