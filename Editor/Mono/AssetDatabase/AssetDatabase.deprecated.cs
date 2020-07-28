// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor.AssetImporters;
using UnityEngine.Bindings;

namespace UnityEditor
{
    partial class AssetDatabase
    {
        // Gets the path to the text .meta file associated with an asset
        [Obsolete("GetTextMetaDataPathFromAssetPath has been renamed to GetTextMetaFilePathFromAssetPath (UnityUpgradable) -> GetTextMetaFilePathFromAssetPath(*)")]
        public static string GetTextMetaDataPathFromAssetPath(string path) { return null; }
    }

    // Used to be part of Asset Server, and public API for some reason.
    [Obsolete("AssetStatus enum is not used anymore (Asset Server has been removed)")]
    public enum AssetStatus
    {
        Calculating = -1,
        ClientOnly = 0,
        ServerOnly = 1,
        Unchanged = 2,
        Conflict = 3,
        Same = 4,
        NewVersionAvailable = 5,
        NewLocalVersion = 6,
        RestoredFromTrash = 7,
        Ignored = 8,
        BadState = 9
    }

    // Used to be part of Asset Server, and public API for some reason.
    [Obsolete("AssetsItem class is not used anymore (Asset Server has been removed)")]
    [StructLayout(LayoutKind.Sequential)]
    [System.Serializable]
    public sealed class AssetsItem
    {
        public string guid;
        public string pathName;
        public string message;
        public string exportedAssetPath;
        public string guidFolder;
        public int enabled;
        public int assetIsDir;
        public int changeFlags;
        public string previewPath;
        public int exists;
    }
}

namespace UnityEditor.Experimental
{
    public partial class AssetDatabaseExperimental
    {
        [FreeFunction("AssetDatabase::ClearImporterOverride")]
        [Obsolete("AssetDatabaseExperimental.ClearImporterOverride() has been deprecated. Use AssetDatabase.ClearImporterOverride() instead (UnityUpgradable) -> UnityEditor.AssetDatabase.ClearImporterOverride(*)", false)]
        extern public static void ClearImporterOverride(string path);

        [FreeFunction("AssetDatabase::IsCacheServerEnabled")]
        [Obsolete("AssetDatabaseExperimental.IsCacheServerEnabled() has been deprecated. Use AssetDatabase.IsCacheServerEnabled() instead (UnityUpgradable) -> UnityEditor.AssetDatabase.IsCacheServerEnabled(*)", false)]
        public extern static bool IsCacheServerEnabled();

        [Obsolete("AssetDatabaseExperimental.SetImporterOverride<T>() has been deprecated. Use AssetDatabase.SetImporterOverride<T>() instead (UnityUpgradable) -> UnityEditor.AssetDatabase.SetImporterOverride<T>(*)", false)]
        public static void SetImporterOverride<T>(string path)
            where T : ScriptedImporter
        {
            AssetDatabase.SetImporterOverrideInternal(path, typeof(T));
        }

        [FreeFunction("AssetDatabase::GetImporterOverride")]
        [Obsolete("AssetDatabaseExperimental.GetImporterOverride() has been deprecated. Use AssetDatabase.GetImporterOverride() instead (UnityUpgradable) -> UnityEditor.AssetDatabase.GetImporterOverride(*)", false)]
        extern public static System.Type GetImporterOverride(string path);

        [FreeFunction("AssetDatabase::GetAvailableImporterTypes")]
        [Obsolete("AssetDatabaseExperimental.GetAvailableImporterTypes() has been deprecated. Use AssetDatabase.GetAvailableImporterTypes() instead (UnityUpgradable) -> UnityEditor.AssetDatabase.GetAvailableImporterTypes(*)", false)]
        extern public static Type[] GetAvailableImporterTypes(string path);

        [FreeFunction("AcceleratorClientCanConnectTo")]
        [Obsolete("AssetDatabaseExperimental.CanConnectToCacheServer() has been deprecated. Use AssetDatabase.CanConnectToCacheServer() instead (UnityUpgradable) -> UnityEditor.AssetDatabase.CanConnectToCacheServer(*)", false)]
        public extern static bool CanConnectToCacheServer(string ip, UInt16 port);

        [FreeFunction()]
        [Obsolete("AssetDatabaseExperimental.RefreshSettings() has been deprecated. Use AssetDatabase.RefreshSettings() instead (UnityUpgradable) -> UnityEditor.AssetDatabase.RefreshSettings(*)", false)]
        public extern static void RefreshSettings();

        [Obsolete("AssetDatabaseExperimental.CacheServerConnectionChangedParameters has been deprecated. Use UnityEditor.CacheServerConnectionChangedParameters instead (UnityUpgradable) -> UnityEditor.CacheServerConnectionChangedParameters", false)]
        public struct CacheServerConnectionChangedParameters
        {
        }

#pragma warning disable 67
        [Obsolete("AssetDatabaseExperimental.cacheServerConnectionChanged has been deprecated. Use AssetDatabase.cacheServerConnectionChanged instead (UnityUpgradable) -> UnityEditor.AssetDatabase.cacheServerConnectionChanged", false)]
        public static event Action<CacheServerConnectionChangedParameters> cacheServerConnectionChanged;
#pragma warning restore 67

        [FreeFunction("IsConnectedToCacheServerV2")]
        [Obsolete("AssetDatabaseExperimental.IsConnectedToCacheServer() has been deprecated. Use AssetDatabase.IsConnectedToCacheServer() instead (UnityUpgradable) -> UnityEditor.AssetDatabase.IsConnectedToCacheServer(*)", false)]
        public extern static bool IsConnectedToCacheServer();

        [FreeFunction()]
        [Obsolete("AssetDatabaseExperimental.GetCacheServerAddress() has been deprecated. Use AssetDatabase.GetCacheServerAddress() instead (UnityUpgradable) -> UnityEditor.AssetDatabase.GetCacheServerAddress(*)", false)]
        public extern static string GetCacheServerAddress();

        [FreeFunction()]
        [Obsolete("AssetDatabaseExperimental.GetCacheServerPort() has been deprecated. Use AssetDatabase.GetCacheServerPort() instead (UnityUpgradable) -> UnityEditor.AssetDatabase.GetCacheServerPort(*)", false)]
        public extern static UInt16 GetCacheServerPort();

        [FreeFunction("AssetDatabase::GetCacheServerNamespacePrefix")]
        [Obsolete("AssetDatabaseExperimental.GetCacheServerNamespacePrefix() has been deprecated. Use AssetDatabase.GetCacheServerNamespacePrefix() instead (UnityUpgradable) -> UnityEditor.AssetDatabase.GetCacheServerNamespacePrefix(*)", false)]
        public extern static string GetCacheServerNamespacePrefix();

        [FreeFunction("AssetDatabase::GetCacheServerEnableDownload")]
        [Obsolete("AssetDatabaseExperimental.GetCacheServerEnableDownload() has been deprecated. Use AssetDatabase.GetCacheServerEnableDownload() instead (UnityUpgradable) -> UnityEditor.AssetDatabase.GetCacheServerEnableDownload(*)", false)]
        public extern static bool GetCacheServerEnableDownload();

        [FreeFunction("AssetDatabase::GetCacheServerEnableUpload")]
        [Obsolete("AssetDatabaseExperimental.GetCacheServerEnableUpload() has been deprecated. Use AssetDatabase.GetCacheServerEnableUpload() instead (UnityUpgradable) -> UnityEditor.AssetDatabase.GetCacheServerEnableUpload(*)", false)]
        public extern static bool GetCacheServerEnableUpload();

        [FreeFunction("AssetDatabase::IsDirectoryMonitoringEnabled")]
        [Obsolete("AssetDatabaseExperimental.IsDirectoryMonitoringEnabled() has been deprecated. Use AssetDatabase.IsDirectoryMonitoringEnabled() instead (UnityUpgradable) -> UnityEditor.AssetDatabase.IsDirectoryMonitoringEnabled(*)", false)]
        public extern static bool IsDirectoryMonitoringEnabled();

        [FreeFunction("AssetDatabaseExperimental::RegisterCustomDependency")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kPreventCustomDependencyChanges, PreventExecutionSeverity.PreventExecution_ManagedException, "Custom dependencies can only be removed when the assetdatabase is not importing.")]
        [Obsolete("AssetDatabaseExperimental.RegisterCustomDependency() has been deprecated. Use AssetDatabase.RegisterCustomDependency() instead (UnityUpgradable) -> UnityEditor.AssetDatabase.RegisterCustomDependency(*)", false)]
        public extern static void RegisterCustomDependency(string dependency, Hash128 hashOfValue);

        [FreeFunction("AssetDatabaseExperimental::UnregisterCustomDependencyPrefixFilter")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kPreventCustomDependencyChanges, PreventExecutionSeverity.PreventExecution_ManagedException, "Custom dependencies can only be removed when the assetdatabase is not importing.")]
        [Obsolete("AssetDatabaseExperimental.UnregisterCustomDependencyPrefixFilter() has been deprecated. Use AssetDatabase.UnregisterCustomDependencyPrefixFilter() instead (UnityUpgradable) -> UnityEditor.AssetDatabase.UnregisterCustomDependencyPrefixFilter(*)", false)]
        public extern static UInt32 UnregisterCustomDependencyPrefixFilter(string prefixFilter);

        [FreeFunction("AssetDatabase::IsAssetImportProcess")]
        [Obsolete("AssetDatabaseExperimental.IsAssetImportWorkerProcess() has been deprecated. Use AssetDatabase.IsAssetImportWorkerProcess()", false)]
        public extern static bool IsAssetImportWorkerProcess();

        [Obsolete("Has been replaced by AssetDatabaseExperimental.RefreshSettings", true)]
        public static void ReconnectToCacheServer()
        {
            throw new NotSupportedException("Please use AssetdatabaseExperimental.RefreshSettings instead.");
        }
    }
}

