// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Bindings;

namespace UnityEditor.Analytics
{
    [RequiredByNativeCode(GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    [UnityEngine.Internal.ExcludeFromDocs]
    public class AssetDatabaseRefreshAnalytic : UnityEngine.Analytics.AnalyticsEventBase
    {
        public AssetDatabaseRefreshAnalytic() : base("assetDatabaseInitRefresh", 1){}

        [UsedByNativeCode]
        internal static AssetDatabaseRefreshAnalytic CreateAssetDatabaseRefreshAnalytic() { return new AssetDatabaseRefreshAnalytic(); }
 
        [SerializeField] public bool isV2;

        [SerializeField] public Int64 Imports_Imported;
        
        [SerializeField] public Int64 Imports_ImportedInProcess;
        [SerializeField] public Int64 Imports_ImportedOutOfProcess;
        [SerializeField] public Int64 Imports_Refresh;
        [SerializeField] public Int64 Imports_DomainReload;

        [SerializeField] public Int64 CacheServer_MetadataRequested;
        [SerializeField] public Int64 CacheServer_MetadataDownloaded;
        [SerializeField] public Int64 CacheServer_MetadataFailedToDownload;
        [SerializeField] public Int64 CacheServer_MetadataUploaded;
        [SerializeField] public Int64 CacheServer_ArtifactsFailedToUpload;
        [SerializeField] public Int64 CacheServer_MetadataVersionsDownloaded;
        [SerializeField] public Int64 CacheServer_MetadataMatched;

        [SerializeField] public Int64 CacheServer_ArtifactsDownloaded;
        [SerializeField] public Int64 CacheServer_ArtifactFilesDownloaded;
        [SerializeField] public Int64 CacheServer_ArtifactFilesFailedToDownload;
        [SerializeField] public Int64 CacheServer_ArtifactsUploaded;
        [SerializeField] public Int64 CacheServer_ArtifactFilesUploaded;
        [SerializeField] public Int64 CacheServer_ArtifactFilesFailedToUpload;
        [SerializeField] public Int64 CacheServer_Connects;
        [SerializeField] public Int64 CacheServer_Disconnects;
    }
}
