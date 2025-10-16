// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor.PackageManager;
using Unity.ProjectAuditor.Editor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Utils
{
    internal static class PackageUtils
    {
        const string k_UnknownVersion = "Unknown";

        public static int CompareVersions(string lhs, string rhs)
        {
            const string regex = "[^0-9.]";
            var leftStr = Regex.Replace(lhs, regex, "", RegexOptions.IgnoreCase);
            var rightStr = Regex.Replace(rhs, regex, "", RegexOptions.IgnoreCase);
            var leftVersion = new Version(leftStr);
            var rightVersion = new Version(rightStr);
            return leftVersion.CompareTo(rightVersion);
        }

        public static PackageInfo[] GetClientPackages()
        {
            return PackageInfo.GetAllRegisteredPackages();
        }

        public static string GetClientPackageVersion(string packageName)
        {
            var packages = GetClientPackages();
            if (packages != null)
            {
                foreach (var packageInfo in packages)
                {
                    if (packageInfo.name == packageName)
                        return packageInfo.version;
                }
            }

            Debug.LogWarning($"Can't find Package {packageName}.");

            return k_UnknownVersion;
        }

        public static string GetPackageRecommendedVersion(UnityEditor.PackageManager.PackageInfo package)
        {
            return package.versions.recommended;
        }

        public static bool IsClientPackage(string packageName)
        {
            var packages = GetClientPackages();
            if (packages != null)
            {
                foreach (var packageInfo in packages)
                {
                    if (packageInfo.name == packageName)
                        return true;
                }
            }

            Debug.LogWarning($"Can't find Package {packageName}.");

            return false;
        }
    }
}
