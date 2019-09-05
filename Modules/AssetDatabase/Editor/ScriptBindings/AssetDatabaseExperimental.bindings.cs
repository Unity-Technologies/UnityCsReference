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
        Available = 3
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
    [NativeHeader("Modules/AssetDatabase/Editor/V2/V1Compatibility.h")]
    [NativeHeader("Modules/AssetDatabase/Editor/Public/AssetDatabaseExperimental.h")]
    public sealed partial class AssetDatabaseExperimental
    {
        public enum OnDemandMode
        {
            Off = 0,
            Lazy = 1,
            Background = 2
        }

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
                public int total;
                public int delta;
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

        [FreeFunction("IsConnectedToCacheServerV2")]
        public extern static bool IsConnectedToCacheServer();
        [FreeFunction("ReconnectToCacheServerV2")]
        public extern static void ReconnectToCacheServer();
        [FreeFunction()]
        public extern static string GetCacheServerAddress();
        [FreeFunction()]
        public extern static UInt16 GetCacheServerPort();

        public extern static bool onlyUploadToCacheServer { get; set; }
        public extern static int minReliabilityIndex { get; set; }

        [FreeFunction("CacheServerCountersResetDeltas")]
        private extern static void CacheServerCountersResetDeltas();

        [FreeFunction("ImportCountersResetDeltas")]
        private extern static void ImportCountersResetDeltas();

        public extern static OnDemandMode ActiveOnDemandMode
        {
            [FreeFunction("GetOnDemandModeV2")] get;
            [FreeFunction("SetOnDemandModeV2")] set;
        }
        private extern static Hash128 GetArtifactHash_Internal_Guid_SelectImporter(string guid, ImportSyncMode mode);
        private extern static Hash128 GetArtifactHash_Internal_Guid(string guid, Type importerType, ImportSyncMode mode);
        private extern static Hash128[] GetArtifactHashes_Internal_Guids_SelectImporter(string[] guid, ImportSyncMode mode);

        [uei.ExcludeFromDocs] public static Hash128 GetArtifactHash(string guid, ImportSyncMode mode = ImportSyncMode.Block) { return GetArtifactHash(guid, null, mode); }
        public static Hash128 GetArtifactHash(string guid, [uei.DefaultValue("null")] Type importerType, ImportSyncMode mode = ImportSyncMode.Block)
        {
            if (importerType == null)
                return GetArtifactHash_Internal_Guid_SelectImporter(guid, mode);
            else
                return GetArtifactHash_Internal_Guid(guid, importerType, mode);
        }

        public static bool GetArtifactPaths(Hash128 hash, out string[] paths)
        {
            bool success;
            var p = GetArtifactPathsImpl(hash, out success);
            paths = p;
            return success;
        }

        public static Hash128[] GetArtifactHashes(string[] guids, ImportSyncMode mode = ImportSyncMode.Block)
        {
            return GetArtifactHashes_Internal_Guids_SelectImporter(guids, mode);
        }

        extern private static string[] GetArtifactPathsImpl(Hash128 hash, out bool success);

        private extern static OnDemandProgress GetOnDemandArtifactProgress_Internal_SelectImporter(string guid);
        private extern static OnDemandProgress GetOnDemandArtifactProgress_Internal(string guid, Type importerType);

        public static OnDemandProgress GetOnDemandArtifactProgress(string guid)
        {
            return GetOnDemandArtifactProgress_Internal_SelectImporter(guid);
        }

        public static OnDemandProgress GetOnDemandArtifactProgress(string guid, Type importerType)
        {
            return GetOnDemandArtifactProgress_Internal(guid, importerType);
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
    }
}
