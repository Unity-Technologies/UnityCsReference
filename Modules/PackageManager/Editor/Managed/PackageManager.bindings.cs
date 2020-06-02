// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;

namespace UnityEditor.PackageManager
{
    [NativeHeader("Modules/PackageManager/Editor/Public/PackageManager.h")]
    public static partial class Client
    {
        [NativeHeader("Modules/PackageManager/Editor/PackageManagerLogger.h")]
        [StaticAccessor("PackageManager", StaticAccessorType.DoubleColon)]
        public static extern LogLevel LogLevel { get; set; }

        [FreeFunction("PackageManager::Add::StartOperation")]
        private static extern NativeStatusCode Add([Out] out long operationId, string packageId);

        [FreeFunction("PackageManager::Embed::StartOperation")]
        private static extern NativeStatusCode Embed([Out] out long operationId, string packageId);

        [FreeFunction("PackageManager::GetCachedPackages::StartOperation")]
        private static extern NativeStatusCode GetCachedPackages([Out] out long operationId, string registryId);

        [FreeFunction("PackageManager::GetPackageInfo::StartOperation")]
        private static extern NativeStatusCode GetPackageInfo([Out] out long operationId, string packageId, bool offlineMode);

        [FreeFunction("PackageManager::GetRegistries::StartOperation")]
        private static extern NativeStatusCode GetRegistries([Out] out long operationId);

        [FreeFunction("PackageManager::List::StartOperation")]
        private static extern NativeStatusCode List([Out] out long operationId, bool offlineMode, bool includeIndirectDependencies);

        [FreeFunction("PackageManager::Pack::StartOperation")]
        private static extern NativeStatusCode Pack([Out] out long operationId, string packageFolder, string targetFolder);

        [FreeFunction("PackageManager::Remove::StartOperation")]
        private static extern NativeStatusCode Remove([Out] out long operationId, string packageId);

        [FreeFunction("PackageManager::ResetToEditorDefaults::StartOperation")]
        private static extern NativeStatusCode ResetToEditorDefaults([Out] out long operationId);

        [FreeFunction("PackageManager::Resolve")]
        internal static extern void Resolve();

        [FreeFunction("PackageManager::Search::StartOperation")]
        private static extern NativeStatusCode Search([Out] out long operationId, SearchOptions options);
    }

    [NativeHeader("Modules/PackageManager/Editor/Public/PackageManager.h")]
    [NativeHeader("Modules/PackageManager/Editor/PackageManagerFolders.h")]
    [StaticAccessor("PackageManager", StaticAccessorType.DoubleColon)]
    internal class Folders
    {
        public static extern string GetPackagesPath();
        public static extern bool IsPackagedAssetPath(string path);
        public static extern string[] GetPackagesPaths();
    }

    [NativeHeader("Modules/PackageManager/Editor/Public/PackageManager.h")]
    [StaticAccessor("PackageManager", StaticAccessorType.DoubleColon)]
    public partial class PackageInfo
    {
        [NativeName("GetAllPackages")]
        internal static extern PackageInfo[] GetAll();

        [NativeName("GetPredefinedPackageTypes")]
        internal static extern string[] GetPredefinedPackageTypes();

        [NativeName("GetPredefinedHiddenByDefaultPackageTypes")]
        internal static extern string[] GetPredefinedHiddenByDefaultPackageTypes();

        private static extern PackageInfo GetPackageByAssetPath(string assetPath);
    }

    [NativeHeader("Modules/PackageManager/Editor/PackageManagerImmutableAssets.h")]
    [StaticAccessor("PackageManager::ImmutableAssets", StaticAccessorType.DoubleColon)]
    internal class ImmutableAssets
    {
        public static extern void SetAssetsAllowedToBeModified(string[] assetsAllowedToBeModified);
    }
}
