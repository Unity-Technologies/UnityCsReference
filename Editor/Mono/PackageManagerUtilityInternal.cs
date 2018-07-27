// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.PackageManager;

namespace UnityEditor
{
    /// <summary>
    /// PackageManager helper class.
    /// </summary>
    internal static class PackageManagerUtilityInternal
    {
        /// <summary>
        /// Returns visibles packages, it excludes modules and non-root dependencies (used in project browser)
        /// <returns>an array of package information ordererd by display name.</returns>
        /// </summary>
        public static PackageManager.PackageInfo[] GetAllVisiblePackages()
        {
            return Packages.GetAll().Where(info => info.isRootDependency && info.type != "module").
                OrderBy(info => string.IsNullOrEmpty(info.displayName) ? info.name : info.displayName,
                    StringComparer.InvariantCultureIgnoreCase).ToArray();
        }
    }
}
