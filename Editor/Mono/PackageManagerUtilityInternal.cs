// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.PackageManager;
using UnityEngine.Scripting;

namespace UnityEditor
{
    /// <summary>
    /// PackageManager helper class.
    /// </summary>
    internal static class PackageManagerUtilityInternal
    {
        /// <summary>
        /// Returns the list of visible packages
        /// </summary>
        /// <param name="skipHiddenPackages">Whether or not to skip packages that have the property hideInEditor set to true. Default is true</param>
        /// <returns>An array of package information ordered by display name.</returns>
        public static PackageManager.PackageInfo[] GetAllVisiblePackages(bool skipHiddenPackages = true)
        {
            return PackageManager.PackageInfo.GetAllRegisteredPackages().Where(info =>
                !IsHidden(info) && (!skipHiddenPackages || !info.hideInEditor)).
                OrderBy(info => string.IsNullOrEmpty(info.displayName) ? info.name : info.displayName,
                    StringComparer.InvariantCultureIgnoreCase).ToArray();
        }

        /// <summary>
        /// Determines whether or not a path belongs to a visible package
        /// <param name="path">The path to check.</param>
        /// <returns>A boolean, true if path belongs to a visible package. False otherwise.</returns>
        /// </summary>
        public static bool IsPathInVisiblePackage(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var package = PackageManager.PackageInfo.FindForAssetPath(path);
            if (package == null)
                return true;

            return !IsHidden(package) && !package.hideInEditor;
        }

        private static int s_HiddenPackagesCount = -1;

        /// <summary>
        /// Count of hidden packages.
        /// In this case, a package is considered hidden if the hideInEditor property is true and overrides the IsHidden value.
        /// </summary>
        public static int HiddenPackagesCount
        {
            get
            {
                if (s_HiddenPackagesCount == -1)
                {
                    s_HiddenPackagesCount = PackageManager.PackageInfo.GetAllRegisteredPackages().Count(info =>
                        !IsHidden(info) && info.hideInEditor);
                }

                return s_HiddenPackagesCount;
            }
        }

        [UsedByNativeCode]
        internal static void OnPackageManagerResolve()
        {
            s_HiddenPackagesCount = -1;
        }

        // We want to hide modules and non-embedded features
        // "IsHidden" is different from "hideInEditor".
        // "hideInEditor" is an option that can be toggled by package developers.
        private static bool IsHidden(PackageManager.PackageInfo info)
        {
            return info.type == "module" || (info.type == "feature" && info.source != PackageSource.Embedded);
        }
    }
}
