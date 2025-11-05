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
using UnityEditor;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

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

    [RequiredByNativeCode]
    [NativeHeader("Modules/AssetDatabase/Editor/Public/AssetDatabaseExperimental.h")]
    [StructLayout(LayoutKind.Sequential)]
    internal struct ImporterID : IEquatable<ImporterID>
    {
        private Int32 persistentTypeID;
        private Hash128 scriptedImportTypeHash;

        public bool IsPrimary => persistentTypeID == -1;

        public bool Equals(ImporterID other)
        {
            return persistentTypeID == other.persistentTypeID && scriptedImportTypeHash == other.scriptedImportTypeHash;
        }
    }

    [NativeHeader("Modules/AssetDatabase/Editor/Public/AssetDatabaseExperimental.h")]
    [StructLayout(LayoutKind.Sequential)]
    public struct ArtifactKey
    {
        public ArtifactKey(GUID guid)
        {
            this = AssetDatabaseExperimental.CreateArtifactKey(guid);
        }

        public ArtifactKey(GUID guid, Type importerType)
        {
            this = AssetDatabaseExperimental.CreateArtifactKey(guid, importerType);
        }

        public bool isValid => !guid.Empty();

        public Type importerType
        {
            get => AssetDatabaseExperimental.ImporterIDToImporterType(importerID);
            set => importerID = AssetDatabaseExperimental.GetImporterID(value);
        }

        public GUID guid;
        private ImporterID importerID;
        private uint hash;
    };
    

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

        [Obsolete("ImportSyncMode has been removed from the editor API", true)]
        public enum ImportSyncMode
        {
            Block = 0,
            Queue = 1,
            Poll = 2
        }

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, AssetDatabase.kPreventExecutionDuringCodeReloadHowToFixMsg)]
        internal static extern ImporterID GetImporterID(Type importerType);
        
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, AssetDatabase.kPreventExecutionDuringCodeReloadHowToFixMsg)]
        internal static extern Type ImporterIDToImporterType(ImporterID importerID);

        internal static ArtifactKey CreateArtifactKey(GUID guid) { return CreateImportAddress_Primary(guid); }

        internal static ArtifactKey CreateArtifactKey(GUID guid, Type importerType) { return CreateImportAddress_Full(guid, importerType); }

        // Needed because bindings don't support overloads 
        // https://internaldocs.unity.com/editor_and_runtime_development_guide/Runtime/Core/marshalling/faq/#do-overloaded-methods-work
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, AssetDatabase.kPreventExecutionDuringCodeReloadHowToFixMsg)]
        private static extern ArtifactKey CreateImportAddress_Primary(GUID guid);
        
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, AssetDatabase.kPreventExecutionDuringCodeReloadHowToFixMsg)]
        private static extern ArtifactKey CreateImportAddress_Full(GUID guid, Type importerType);

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

                public Counter batchesUsedForDownload;
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

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, AssetDatabase.kPreventExecutionDuringCodeReloadHowToFixMsg)]
        private extern static AssetDatabaseCounters GetCounters();
        public static AssetDatabaseCounters counters => GetCounters();

        [FreeFunction("CacheServerCountersResetDeltas")]
        private extern static void CacheServerCountersResetDeltas();

        [FreeFunction("ImportCountersResetDeltas")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, AssetDatabase.kPreventExecutionDuringCodeReloadHowToFixMsg)]
        private extern static void ImportCountersResetDeltas();

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, AssetDatabase.kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public extern static OnDemandMode ActiveOnDemandMode
        {
            [FreeFunction("GetOnDemandModeV2")] 
            [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, AssetDatabase.kPreventExecutionDuringCodeReloadHowToFixMsg)]
            get;
            [FreeFunction("SetOnDemandModeV2")]
            [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, AssetDatabase.kPreventExecutionDuringCodeReloadHowToFixMsg)]
            set;
        }

        [NativeHeader("Modules/AssetDatabase/Editor/V2/Virtualization/Virtualization.h")]
        internal extern static bool VirtualizationEnabled
        {
            [FreeFunction("Virtualization_IsEnabled")] 
            get;
        }

        [FreeFunction("AssetDatabaseExperimental::LookupArtifact")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, AssetDatabase.kPreventExecutionDuringCodeReloadHowToFixMsg)]
        private extern static ImportResultID _LookupArtifact(ArtifactKey artifactKey);
        public static ImportResultID LookupArtifact(ArtifactKey artifactKey) => _LookupArtifact(artifactKey);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, AssetDatabase.kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public extern static ImportResultID ProduceArtifact(ArtifactKey artifactKey);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, AssetDatabase.kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public extern static ImportResultID ProduceArtifactAsync(ArtifactKey artifactKey);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, AssetDatabase.kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public extern static ImportResultID[] ProduceArtifactsAsync(GUID[] artifactKey, [uei.DefaultValue("null")] Type importerType = null);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, AssetDatabase.kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public extern static ImportResultID ForceProduceArtifact(ArtifactKey artifactKey);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, AssetDatabase.kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static void LookupArtifacts(IntPtr guidsPtr, IntPtr hashesPtr, int len, [uei.DefaultValue("null")] Type importerType = null);
        public unsafe static void LookupArtifacts(NativeArray<GUID> guids, NativeArray<ImportResultID> hashes, Type importerType)
        {
            if (guids.Length != hashes.Length)
                throw new ArgumentException("guids and hashes size mismatch!");

            LookupArtifacts((IntPtr)guids.GetUnsafePtr(), (IntPtr)hashes.GetUnsafePtr(), guids.Length, importerType);
        }

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, AssetDatabase.kPreventExecutionDuringCodeReloadHowToFixMsg)]
        extern internal static void LookupPrimaryArtifacts(IntPtr guidsPtr, IntPtr hashesPtr, int len);
        public unsafe static void LookupArtifacts(NativeArray<GUID> guids, NativeArray<ImportResultID> hashesOut)
        {
            if (!guids.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(guids));

            if (!hashesOut.IsCreated)
                throw new ArgumentException("NativeArray is uninitialized", nameof(hashesOut));

            if (guids.Length != hashesOut.Length)
                throw new ArgumentException("guids and hashesOut size mismatch!");

            LookupPrimaryArtifacts((IntPtr)guids.GetUnsafePtr(), (IntPtr)hashesOut.GetUnsafePtr(), guids.Length);
        }

        [Obsolete("GetArtifactHash() has been removed. Use LookupArtifact(), ProduceArtifact() or ForceProduceArtifact() instead.", true)]
        [uei.ExcludeFromDocs]
        public static Hash128 GetArtifactHash(string guid, ImportSyncMode mode = ImportSyncMode.Block)
        {
            throw new NotImplementedException();
        }

        [Obsolete("GetArtifactHash() has been removed. Use LookupArtifact(), ProduceArtifact() or ForceProduceArtifact() instead.", true)]
        public static Hash128 GetArtifactHash(string guid, [uei.DefaultValue("null")] Type importerType, ImportSyncMode mode = ImportSyncMode.Block)
        {
            throw new NotImplementedException();
        }

        public static bool GetArtifactPaths(ImportResultID hash, out string[] paths)
        {
            bool success;
            var p = GetArtifactPathsImpl(hash, out success);
            paths = p;
            return success;
        }

        [Obsolete("GetArtifactPaths(Hash128, out string[]) has been removed. Use GetArtifactPaths(ImportResultID, out string[]) instead.", true)]
        public static bool GetArtifactPaths(Hash128 hash, out string[] paths)
        {
            throw new NotImplementedException();
        }

        [Obsolete("GetArtifactHashes() has been removed. Use LookupArtifact(), ProduceArtifact() or ForceProduceArtifact() instead.", true)]
        public static Hash128[] GetArtifactHashes(string[] guids, ImportSyncMode mode = ImportSyncMode.Block)
        {
            throw new NotImplementedException();
        }

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, AssetDatabase.kPreventExecutionDuringCodeReloadHowToFixMsg)]
        private extern static string[] GetArtifactPathsImpl(ImportResultID hash, out bool success);

        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, AssetDatabase.kPreventExecutionDuringCodeReloadHowToFixMsg)]
        public extern static OnDemandProgress GetOnDemandArtifactProgress(ArtifactKey artifactKey);

        [Obsolete("GetOnDemandArtifactProgress(string) has been removed. Use GetOnDemandArtifactProgress(ArtifactKey) instead.", true)]
        public static OnDemandProgress GetOnDemandArtifactProgress(string guid)
        {
            return GetOnDemandArtifactProgress(CreateArtifactKey(new GUID(guid)));
        }

        [Obsolete("GetOnDemandArtifactProgress(string,Type) has been removed. Use GetOnDemandArtifactProgress(ArtifactKey) instead.", true)]
        public static OnDemandProgress GetOnDemandArtifactProgress(string guid, Type importerType)
        {
            return GetOnDemandArtifactProgress(CreateArtifactKey(new GUID(guid), importerType));
        }

        [FreeFunction("AssetDatabase::GetArtifactStaticDependencyHash")]
        [PreventExecutionInState(AssetDatabasePreventExecution.kCodeReload, PreventExecutionSeverity.PreventExecution_ManagedException, AssetDatabase.kPreventExecutionDuringCodeReloadHowToFixMsg)]
        private extern static Hash128 _GetArtifactStaticDependencyHash(ImportResultID importResultID);
        
        internal static Hash128 GetArtifactStaticDependencyHash(ImportResultID importResultID) => _GetArtifactStaticDependencyHash(importResultID);
    }
}
