// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEditor.PackageManager;

namespace Unity.Multiplayer.PlayMode.Editor
{
    static partial class PackageUtils
    {
        internal delegate bool ReadOnlyPackageCheck(string assetPath, out string packageName);

        // No project can contain a writable copy of a read-only package, so tests substitute the
        // check rather than trying to create one.
        [AutoStaticsCleanupOnCodeReload]
        internal static ReadOnlyPackageCheck MockIsAssetInReadOnlyPackageForTests { private get; set; }

        static bool IsPackageReadOnly(PackageInfo package)
            => package.source is not (PackageSource.Embedded or PackageSource.Local);

        /// <summary>
        /// Returns true when the asset belongs to a package Unity refuses to write to, which
        /// includes refusing to open its scenes. <paramref name="packageName"/> receives the
        /// package name for user-facing messages, or null when the asset is writable.
        /// </summary>
        public static bool IsAssetInReadOnlyPackage(string assetPath, out string packageName)
        {
            if (MockIsAssetInReadOnlyPackageForTests != null)
                return MockIsAssetInReadOnlyPackageForTests(assetPath, out packageName);

            packageName = null;

            // FindForAssetPath throws on an empty path and only resolves paths under "Packages/".
            if (string.IsNullOrEmpty(assetPath) || !assetPath.StartsWith("Packages/"))
                return false;

            var package = PackageInfo.FindForAssetPath(assetPath);
            if (package == null || !IsPackageReadOnly(package))
                return false;

            packageName = string.IsNullOrEmpty(package.displayName) ? package.name : package.displayName;
            return true;
        }
    }
}
