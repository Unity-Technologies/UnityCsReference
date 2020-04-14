// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using uei = UnityEngine.Internal;

namespace UnityEditor.Experimental
{
    public enum OnDemandState
    {
        Unavailable = 0,
        Processing = 1,
        Downloading = 2,
        Available = 3,
        Failed = 4
    }

    [NativeHeader("Modules/AssetDatabase/Editor/Public/AssetDatabaseExperimental.h")]
    [StructLayout(LayoutKind.Sequential)]
    public struct ArtifactKey
    {
        public ArtifactKey(GUID g)
        {
            guid = g;
            importerType = null;
        }

        public ArtifactKey(GUID guid, Type importerType)
        {
            this.guid = guid;
            this.importerType = importerType;
        }

        public GUID guid;
        public Type importerType;
    };

    [NativeHeader("Modules/AssetDatabase/Editor/Public/AssetDatabaseTypes.h")]
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct ArtifactID
    {
        public Hash128 value;
        public bool isValid => value.isValid;
    }

    [NativeHeader("Modules/AssetDatabase/Editor/Public/AssetDatabaseTypes.h")]
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct OnDemandProgress
    {
        public OnDemandState state;
        public float progress;
    }

    [NativeHeader("Modules/AssetDatabase/Editor/V2/AssetDatabaseCounters.h")]
    [NativeHeader("Modules/AssetDatabase/Editor/V2/AssetDatabaseInternal.h")]
    [NativeHeader("Modules/AssetDatabase/Editor/Public/AssetDatabaseExperimental.h")]
    public sealed partial class AssetDatabaseExperimental
    {
        public enum OnDemandMode
        {
            Off = 0,
            Lazy = 1,
            Background = 2
        }

        [Obsolete("ImportSyncMode has been removed from the editor API", false)]
        public enum ImportSyncMode
        {
            Block = 0,
            Queue = 1,
            Poll = 2
        }

        public struct AssetDatabaseCounters
        {
            public struct Counter
            {
                public long total;
                public long delta;
            }

            public struct CacheServerCounters
            {
                public Counter metadataRequested;
                public Counter metadataDownloaded;
                public Counter metadataFailedToDownload;
                public Counter metadataUploaded;
                public Counter metadataFailedToUpload;
                public Counter metadataVersionsDownloaded;
                public Counter metadataMatched;

                public Counter artifactsDownloaded;
                public Counter artifactFilesDownloaded;
                public Counter artifactFilesFailedToDownload;
                public Counter artifactsUploaded;
                public Counter artifactFilesUploaded;
                public Counter artifactFilesFailedToUpload;

                public Counter connects;
                public Counter disconnects;
            }

            public struct ImportCounters
            {
                public Counter imported;
                public Counter importedInProcess;
                public Counter importedOutOfProcess;
                public Counter refresh;
                public Counter domainReload;
            }

            public CacheServerCounters cacheServer;
            public ImportCounters import;

            public void ResetDeltas()
            {
                CacheServerCountersResetDeltas();
                ImportCountersResetDeltas();
            }
        }

        public extern static AssetDatabaseCounters counters { get; }

        [FreeFunction("AcceleratorClientCanConnectTo")]
        public extern static bool CanConnectToCacheServer(string ip, UInt16 port);
        [FreeFunction()]
        public extern static void RefreshSettings();

        public struct CacheServerConnectionChangedParameters
        {
        }

        public static event Action<CacheServerConnectionChangedParameters> cacheServerConnectionChanged;
        [RequiredByNativeCode]
        private static void OnCacheServerConnectionChanged()
        {
            if (cacheServerConnectionChanged != null)
            {
                CacheServerConnectionChangedParameters param;
                cacheServerConnectionChanged(param);
            }
        }

        [FreeFunction("IsConnectedToCacheServerV2")]
        public extern static bool IsConnectedToCacheServer();
        [Obsolete("Has been replaced by AssetDatabaseExperimental.RefreshSettings", true)]
        public static void ReconnectToCacheServer()
        {
            throw new NotSupportedException("Please use AssetdatabaseExperimental.RefreshSettings instead.");
        }

        [FreeFunction()]
        public extern static string GetCacheServerAddress();
        [FreeFunction()]
        public extern static UInt16 GetCacheServerPort();

        [FreeFunction("AssetDatabase::IsCacheServerEnabled")]
        public extern static bool IsCacheServerEnabled();
        [FreeFunction("AssetDatabase::GetCacheServerNamespacePrefix")]
        public extern static string GetCacheServerNamespacePrefix();
        [FreeFunction("AssetDatabase::GetCacheServerEnableDownload")]
        public extern static bool GetCacheServerEnableDownload();
        [FreeFunction("AssetDatabase::GetCacheServerEnableUpload")]
        public extern static bool GetCacheServerEnableUpload();

        [FreeFunction("CacheServerCountersResetDeltas")]
        private extern static void CacheServerCountersResetDeltas();

        [FreeFunction("ImportCountersResetDeltas")]
        private extern static void ImportCountersResetDeltas();

        [FreeFunction("AssetDatabase::IsDirectoryMonitoringEnabled")]
        public extern static bool IsDirectoryMonitoringEnabled();

        public extern static OnDemandMode ActiveOnDemandMode
        {
            [FreeFunction("GetOnDemandModeV2")] get;
            [FreeFunction("SetOnDemandModeV2")] set;
        }
        [NativeHeader("Modules/AssetDatabase/Editor/V2/Virtualization/Virtualization.h")]
        internal extern static bool VirtualizationEnabled
        {
            [FreeFunction("Virtualization_IsEnabled")] get;
        }

        public extern static ArtifactID LookupArtifact(ArtifactKey artifactKey);
        public extern static ArtifactID ProduceArtifact(ArtifactKey artifactKey);
        public extern static ArtifactID ProduceArtifactAsync(ArtifactKey artifactKey);
        public extern static ArtifactID[] ProduceArtifactsAsync(GUID[] artifactKey, [uei.DefaultValue("null")] Type importerType = null);
        public extern static ArtifactID ForceProduceArtifact(ArtifactKey artifactKey);


        [Obsolete("GetArtifactHash() has been removed. Use LookupArtifact(), ProduceArtifact() or ForceProduceArtifact() instead.")]
        [uei.ExcludeFromDocs]
        public static Hash128 GetArtifactHash(string guid, ImportSyncMode mode = ImportSyncMode.Block)
        {
            switch (mode)
            {
                case ImportSyncMode.Block:
                    return ProduceArtifact(new ArtifactKey(new GUID(guid))).value;
                case ImportSyncMode.Poll:
                    return LookupArtifact(new ArtifactKey(new GUID(guid))).value;
                case ImportSyncMode.Queue:
                    return ProduceArtifactAsync(new ArtifactKey(new GUID(guid))).value;
            }

            throw new Exception("Invalid ImportSyncMode " + mode);
        }

        [Obsolete("GetArtifactHash() has been removed. Use LookupArtifact(), ProduceArtifact() or ForceProduceArtifact() instead.")]
        public static Hash128 GetArtifactHash(string guid, [uei.DefaultValue("null")] Type importerType, ImportSyncMode mode = ImportSyncMode.Block)
        {
            switch (mode)
            {
                case ImportSyncMode.Block:
                    return ProduceArtifact(new ArtifactKey(new GUID(guid), importerType)).value;
                case ImportSyncMode.Poll:
                    return LookupArtifact(new ArtifactKey(new GUID(guid), importerType)).value;
                case ImportSyncMode.Queue:
                    return ProduceArtifactAsync(new ArtifactKey(new GUID(guid), importerType)).value;
            }

            throw new Exception("Invalid ImportSyncMode " + mode);
        }

        public static bool GetArtifactPaths(ArtifactID hash, out string[] paths)
        {
            bool success;
            var p = GetArtifactPathsImpl(hash, out success);
            paths = p;
            return success;
        }

        [Obsolete("GetArtifactPaths(Hash128, out string[]) has been removed. Use GetArtifactPaths(ArtifactID, out string[]) instead.")]
        public static bool GetArtifactPaths(Hash128 hash, out string[] paths)
        {
            return GetArtifactPaths(new ArtifactID() { value =  hash }, out paths);
        }

        [Obsolete("GetArtifactHashes() has been removed. Use LookupArtifact(), ProduceArtifact() or ForceProduceArtifact() instead.")]
        public static Hash128[] GetArtifactHashes(string[] guids, ImportSyncMode mode = ImportSyncMode.Block)
        {
            var _guids = guids.Select(a => new GUID(a)).ToArray();

            switch (mode)
            {
                case ImportSyncMode.Block:
                    List<Hash128> resultA = new List<Hash128>();
                    foreach (var g in _guids)
                        resultA.Add(ProduceArtifact(new ArtifactKey(g)).value);
                    return resultA.ToArray();
                case ImportSyncMode.Poll:
                    List<Hash128> resultB = new List<Hash128>();
                    foreach (var g in _guids)
                        resultB.Add(LookupArtifact(new ArtifactKey(g)).value);
                    return resultB.ToArray();
                case ImportSyncMode.Queue:
                    return ProduceArtifactsAsync(_guids, null).Select(a => a.value).ToArray();
            }

            throw new Exception("Invalid ImportSyncMode " + mode);
        }

        private extern static string[] GetArtifactPathsImpl(ArtifactID hash, out bool success);

        public extern static OnDemandProgress GetOnDemandArtifactProgress(ArtifactKey artifactKey);

        [Obsolete("GetOnDemandArtifactProgress(string) has been removed. Use GetOnDemandArtifactProgress(ArtifactKey) instead.")]
        public static OnDemandProgress GetOnDemandArtifactProgress(string guid)
        {
            return GetOnDemandArtifactProgress(new ArtifactKey(new GUID(guid)));
        }

        [Obsolete("GetOnDemandArtifactProgress(string,Type) has been removed. Use GetOnDemandArtifactProgress(ArtifactKey) instead.")]
        public static OnDemandProgress GetOnDemandArtifactProgress(string guid, Type importerType)
        {
            return GetOnDemandArtifactProgress(new ArtifactKey(new GUID(guid), importerType));
        }

        [RequiredByNativeCode]
        static string[] OnSourceAssetsModified(string[] changedAssets, string[] addedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var assetMoveInfo = new AssetMoveInfo[movedAssets.Length];
            Debug.Assert(movedAssets.Length == movedFromAssetPaths.Length);
            for (int i = 0; i < movedAssets.Length; i++)
                assetMoveInfo[i] = new AssetMoveInfo(movedFromAssetPaths[i], movedAssets[i]);

            var assetsReportedChanged = new HashSet<string>();

            foreach (Type type in TypeCache.GetTypesDerivedFrom<AssetsModifiedProcessor>())
            {
                var assetPostprocessor = Activator.CreateInstance(type) as AssetsModifiedProcessor;
                assetPostprocessor.assetsReportedChanged = assetsReportedChanged;
                assetPostprocessor.Internal_OnAssetsModified(changedAssets, addedAssets, deletedAssets, assetMoveInfo);
                assetPostprocessor.assetsReportedChanged = null;
            }

            return assetsReportedChanged.ToArray();
        }

        [FreeFunction("AssetDatabaseExperimental::RegisterCustomDependency")]
        public extern static void RegisterCustomDependency(string dependency, Hash128 hashOfValue);

        [FreeFunction("AssetDatabaseExperimental::UnregisterCustomDependencyPrefixFilter")]
        public extern static UInt32 UnregisterCustomDependencyPrefixFilter(string prefixFilter);

        [FreeFunction("AssetDatabase::IsAssetImportProcess")]
        public extern static bool IsAssetImportWorkerProcess();
    }
}
