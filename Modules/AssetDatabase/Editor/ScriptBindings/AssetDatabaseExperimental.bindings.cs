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
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

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

        public bool isValid => !guid.Empty();

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
                internal Counter fullScan;
            }

            public CacheServerCounters cacheServer;
            public ImportCounters import;

            public void ResetDeltas()
            {
                CacheServerCountersResetDeltas();
                ImportCountersResetDeltas();
            }
        }

        private extern static AssetDatabaseCounters GetCounters();
        public static AssetDatabaseCounters counters => GetCounters();

        [FreeFunction("CacheServerCountersResetDeltas")]
        private extern static void CacheServerCountersResetDeltas();

        [FreeFunction("ImportCountersResetDeltas")]
        private extern static void ImportCountersResetDeltas();

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

        [FreeFunction("AssetDatabaseExperimental::LookupArtifact")]
        private extern static ArtifactID _LookupArtifact(ArtifactKey artifactKey);
        public static ArtifactID LookupArtifact(ArtifactKey artifactKey) => _LookupArtifact(artifactKey);
        public extern static ArtifactID ProduceArtifact(ArtifactKey artifactKey);
        public extern static ArtifactID ProduceArtifactAsync(ArtifactKey artifactKey);
        public extern static ArtifactID[] ProduceArtifactsAsync(GUID[] artifactKey, [uei.DefaultValue("null")] Type importerType = null);
        public extern static ArtifactID ForceProduceArtifact(ArtifactKey artifactKey);

        extern internal static void LookupArtifacts(IntPtr guidsPtr, IntPtr hashesPtr, int len, [uei.DefaultValue("null")] Type importerType = null);
        public unsafe static void LookupArtifacts(NativeArray<GUID> guids, NativeArray<ArtifactID> hashes, Type importerType)
        {
            if (guids.Length != hashes.Length)
                throw new ArgumentException("guids and hashes size mismatch!");

            LookupArtifacts((IntPtr)guids.GetUnsafePtr(), (IntPtr)hashes.GetUnsafePtr(), guids.Length, importerType);
        }

        extern internal static void LookupPrimaryArtifacts(IntPtr guidsPtr, IntPtr hashesPtr, int len);
        public unsafe static void LookupArtifacts(NativeArray<GUID> guids, NativeArray<ArtifactID> hashesOut)
        {
            if (!guids.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(guids));

            if (!hashesOut.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(hashesOut));

            if (guids.Length != hashesOut.Length)
                throw new ArgumentException("guids and hashesOut size mismatch!");

            LookupPrimaryArtifacts((IntPtr)guids.GetUnsafePtr(), (IntPtr)hashesOut.GetUnsafePtr(), guids.Length);
        }

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

        [FreeFunction("AssetDatabase::GetArtifactStaticDependencyHash")]
        private extern static Hash128 _GetArtifactStaticDependencyHash(ArtifactID artifactId);
        internal static Hash128 GetArtifactStaticDependencyHash(ArtifactID artifactID) => _GetArtifactStaticDependencyHash(artifactID);
    }
}
