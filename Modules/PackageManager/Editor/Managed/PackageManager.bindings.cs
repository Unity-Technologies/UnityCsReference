// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System;
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

        [FreeFunction("PackageManager::AddAndRemove::StartOperation")]
        private static extern NativeStatusCode AddAndRemove([Out] out long operationId, string[] packagesToAdd, string[] packagesToRemove);

        [FreeFunction("PackageManager::AddScopedRegistry::StartOperation")]
        private static extern NativeStatusCode AddScopedRegistry([Out] out long operationId, string name, string url, string[] scopes);

        [FreeFunction("PackageManager::ClearCacheRoot::StartOperation")]
        private static extern NativeStatusCode ClearCacheRoot([Out] out long operationId);

        [FreeFunction("PackageManager::Embed::StartOperation")]
        private static extern NativeStatusCode Embed([Out] out long operationId, string packageId);

        [FreeFunction("PackageManager::GetCacheRoot::StartOperation")]
        private static extern NativeStatusCode GetCacheRoot([Out] out long operationId, ConfigSource source);

        [FreeFunction("PackageManager::GetPackageInfo::StartOperation")]
        private static extern NativeStatusCode GetPackageInfo([Out] out long operationId, string packageId, bool offlineMode);

        [FreeFunction("PackageManager::GetRegistries::StartOperation")]
        private static extern NativeStatusCode GetRegistries([Out] out long operationId);

        [FreeFunction("PackageManager::List::StartOperation")]
        private static extern NativeStatusCode List([Out] out long operationId, bool offlineMode, bool includeIndirectDependencies);

        [FreeFunction("PackageManager::ListBuiltInPackages::StartOperation")]
        private static extern NativeStatusCode ListBuiltInPackages([Out] out long operationId);

        [FreeFunction("PackageManager::Pack::StartOperation")]
        private static extern NativeStatusCode Pack([Out] out long operationId, string packageFolder, string targetFolder);

        [FreeFunction("PackageManager::Remove::StartOperation")]
        private static extern NativeStatusCode Remove([Out] out long operationId, string packageId);

        [FreeFunction("PackageManager::RemoveScopedRegistry::StartOperation")]
        private static extern NativeStatusCode RemoveScopedRegistry([Out] out long operationId, string registryId);

        [FreeFunction("PackageManager::ResetToEditorDefaults::StartOperation")]
        private static extern NativeStatusCode ResetToEditorDefaults([Out] out long operationId);

        [FreeFunction("PackageManager::Resolve")]
        private static extern void Resolve_Internal(bool force);

        public static void Resolve()
        {
            Resolve_Internal(true);
        }

        internal static void Resolve(bool force)
        {
            Resolve_Internal(force);
        }

        [FreeFunction("PackageManager::Search::StartOperation")]
        private static extern NativeStatusCode Search([Out] out long operationId, SearchOptions options);

        [FreeFunction("PackageManager::SetCacheRoot::StartOperation")]
        private static extern NativeStatusCode SetCacheRoot([Out] out long operationId, string newPath);

        [FreeFunction("PackageManager::UpdateScopedRegistry::StartOperation")]
        private static extern NativeStatusCode UpdateScopedRegistry([Out] out long operationId, string registryId, UpdateScopedRegistryOptions options);
    }

    [NativeHeader("Modules/PackageManager/Editor/Public/PackageManager.h")]
    [NativeHeader("Modules/PackageManager/Editor/PackageManagerFolders.h")]
    [StaticAccessor("PackageManager", StaticAccessorType.DoubleColon)]
    internal class Folders
    {
        [ThreadAndSerializationSafe]
        public static extern string GetPackagesPath();
        public static extern bool IsPackagedAssetPath(string path);
        public static extern string[] GetPackagesPaths();
    }

    [NativeHeader("Modules/PackageManager/Editor/Public/PackageManager.h")]
    [StaticAccessor("PackageManager", StaticAccessorType.DoubleColon)]
    public partial class PackageInfo
    {
        [NativeName("GetAllPackages")]
        public static extern PackageInfo[] GetAllRegisteredPackages();
        public static extern bool IsPackageRegistered(string name);

        [NativeName("GetPackageByAssetPathFromScript")]
        private static extern PackageInfo[] GetPackageByAssetPath(string assetPath);

        [NativeName("GetPackageByNameFromScript")]
        private static extern PackageInfo[] GetPackageByName(string name);
    }

    [NativeHeader("Modules/PackageManager/Editor/PackageManagerImmutableAssets.h")]
    [StaticAccessor("PackageManager::ImmutableAssets::GetInstance()", StaticAccessorType.Arrow)]
    internal class ImmutableAssets
    {
        public static extern void SetAssetsToBeModified(string[] assetsAllowedToBeModified);
    }
}
