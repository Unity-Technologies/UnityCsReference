// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Experimental
{
    [NativeHeader("Modules/AssetDatabase/Editor/V2/AssetDatabaseCounters.h")]
    [NativeHeader("Modules/AssetDatabase/Editor/V2/V1Compatibility.h")]
    [NativeHeader("Modules/AssetDatabase/Editor/Public/AssetDatabaseExperimental.h")]
    public partial class AssetDatabaseExperimental
    {
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
    }
}
