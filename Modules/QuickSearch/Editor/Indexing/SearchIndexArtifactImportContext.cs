// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace UnityEditor.Search
{
    class SearchIndexArtifactImportContext
    {
        // Declared as volatile since it can be accessed from multiple threads.
        volatile int m_ImportedCount;

        public const int DefaultBatchSize = 64;
        public static readonly TimeSpan DefaultArtifactProductionFrameTimeLimit = TimeSpan.FromMilliseconds(16);
        public static readonly TimeSpan DefaultArtifactProductionDispatcherDelay = TimeSpan.FromMilliseconds(16);
        public static readonly TimeSpan DefaultArtifactUpdateTimeout = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Number of artifacts to process in a single batch.
        /// </summary>
        public int BatchSize { get; set; } = DefaultBatchSize;

        /// <summary>
        /// Time limit for the production of artifacts in a single frame.
        /// </summary>
        public TimeSpan ArtifactProductionFrameTimeLimit { get; set; } = DefaultArtifactProductionFrameTimeLimit;

        /// <summary>
        /// If true, the artifact production will skip to the next frame if no update was received in the current frame. When false,
        /// each frame will take up to ArtifactProductionFrameTimeLimit even if no update was received.
        /// </summary>
        public bool SkipToNextFrameIfNoUpdate { get; set; } = true;

        /// <summary>
        /// Delay between artifact production dispatcher invocations. Do not put this to zero to avoid blocking the main thread.
        /// </summary>
        public TimeSpan ArtifactProductionDispatcherDelay { get; set; } = DefaultArtifactProductionDispatcherDelay;

        /// <summary>
        /// The search database associated with this import context.
        /// </summary>
        public SearchDatabase Db { get; private set; }

        /// <summary>
        /// The list of sources to import artifacts from.
        /// </summary>
        public IList<string> Sources { get; private set; }

        /// <summary>
        /// Artifact import data associated with the sources.
        /// </summary>
        public SearchIndexArtifactImportData ArtifactImportData { get; private set; }

        /// <summary>
        /// Total number of sources to import artifacts from.
        /// </summary>
        public int TotalCount => Sources.Count;

        /// <summary>
        /// Number of successfully imported artifacts (Failed or Available).
        /// </summary>
        public int ImportedCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_ImportedCount;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => m_ImportedCount = value;
        }

        /// <summary>
        /// Number of requested artifacts.
        /// </summary>
        public int RequestedCount { get; set; }

        /// <summary>
        /// Time to wait before considering an artifact request has timed out.
        /// </summary>
        /// TODO OnDemandArtifact Pause/Resume: This is only used
        /// by the SearchIndexArtifact generation to resend requests that might have been lost
        /// when a AssetDatabase.Refresh() happens. Once proper Pause/Resume mechanism is added
        /// to the OnDemandArtifact system, this can be removed.
        public TimeSpan ArtifactUpdateTimeout { get; set; } = DefaultArtifactUpdateTimeout;

        /// <summary>
        /// Timestamp of the last artifact update received.
        /// </summary>
        /// TODO OnDemandArtifact Pause/Resume: This is only used
        /// by the SearchIndexArtifact generation to resend requests that might have been lost
        /// when a AssetDatabase.Refresh() happens. Once proper Pause/Resume mechanism is added
        /// to the OnDemandArtifact system, this can be removed.
        public double LastArtifactUpdateTimeInSecond { get; set; }

        /// <summary>
        /// Indicates whether there are artifacts that have not been updated within the ArtifactUpdateTimeout. If so, the batch needs to be restarted.
        /// </summary>
        /// TODO OnDemandArtifact Pause/Resume: This is only used
        /// by the SearchIndexArtifact generation to resend requests that might have been lost
        /// when a AssetDatabase.Refresh() happens. Once proper Pause/Resume mechanism is added
        /// to the OnDemandArtifact system, this can be removed.
        public bool RestartPendingArtifacts => TimeSpan.FromSeconds(EditorApplication.timeSinceStartup - LastArtifactUpdateTimeInSecond) > ArtifactUpdateTimeout;

        public SearchIndexArtifactImportContext(SearchDatabase db, IList<string> sources)
            : this(db)
        {
            SetSources(sources);
        }

        public SearchIndexArtifactImportContext(SearchDatabase db)
        {
            Db = db;
        }

        /// <summary>
        /// Sets the sources to import artifacts from. This will reset the import data and counters.
        /// </summary>
        /// <param name="sources"></param>
        public void SetSources(IList<string> sources)
        {
            Sources = sources;
            ArtifactImportData = new SearchIndexArtifactImportData(sources.Count);
            ImportedCount = 0;
            RequestedCount = 0;
        }
    }
}
