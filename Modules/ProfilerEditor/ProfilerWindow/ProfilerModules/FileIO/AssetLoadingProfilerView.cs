// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Profiling;
using Unity.Profiling;
using System.IO;

namespace UnityEditor
{
    internal enum AssetLoadingFrameSetup
    {
        ThisFrame,
        AllFrames
    }
    internal enum AssetMarkerType
    {
        AsyncLoadAsset,
        SyncLoadAsset,
        LoadBundle,
        AsyncUnloadBundle,
        SyncUnloadBundle,
        LoadScene,
        LoadSceneObjects,
        ReadObject,
        SyncReadRequest,
        AsyncReadRequest,
        UnloadUnusedAssets
    };

    internal class AssetLoadMarker
    {
        // Key data displayed in tree view:
        public int index = 0;
        public string filename;
        public string readableFileName;
        public string readablePath;
        public string sourceName; // bundle name or otherwise
        public string assetName;
        public string threadName;
        public string subsystem;
        public AssetMarkerType type;
        public double startTimeMs;
        public double ms;           // Duration
        public int firstFrameIndex;
        public int lastFrameIndex;
        public int frameCount; // number of frames included in this access
        public ulong sizeBytes;

        public ulong startNs;
        public ulong endNs;
        public ulong lengthNs;

        public string markerName;
        public int markerId;
        public List<int> sampleIds;                // Unique sample Id for each frame/thread
        public int threadIndex;
        public ulong threadId;
        public int depth;

        public bool addedToTreeView = false;

        public AssetLoadMarker()
        {
            sampleIds = new List<int>();
        }
    }

    internal class AssetCaptureData
    {
        public double m_TotalTimeMs;

        public double m_StartTimeNs;
        public double m_EndTimeNs;

        public List<AssetLoadMarker> m_AssetLoadMarkers;

        public AssetCaptureData()
        {
            m_AssetLoadMarkers = new List<AssetLoadMarker>();
        }
    }

    [Serializable]
    internal class AssetLoadingProfilerView : LoadingProfilerViewBase
    {
        static ProfilerMarker s_PullData = new ProfilerMarker("AssetLoadingProfiler.PullData");
        static ProfilerMarker s_GetMarkerIDs = new ProfilerMarker("AssetLoadingProfiler.GetMarkerIDs");
        static ProfilerMarker s_AddNewMarker = new ProfilerMarker("AssetLoadingProfiler.AddNewMarkerToList");

        private AssetCaptureData m_CaptureData;

        public AssetLoadingFrameSetup GetFrameSetup() { return selectedFrameSetup; }

        Dictionary<string, AssetMarkerInfo> m_Markers = new Dictionary<string, AssetMarkerInfo>();
        Dictionary<string, int> m_MarkerToIDMap = new Dictionary<string, int>();

        private AssetLoadingFrameSetup selectedFrameSetup = AssetLoadingFrameSetup.ThisFrame;
        public bool FrameSelectionChanged { get; set; }

        [NonSerialized] private bool m_AssetMarkerTableIsCreated = false;
        [SerializeField] TreeViewState m_AssetMarkerTreeViewState;
        [SerializeField] MultiColumnHeaderState m_AssetMarkerMulticolumnHeaderState;
        AssetMarkerTreeView m_AssetMarkerTreeView;
        MultiColumnHeader m_AssetMarkerMultiColumnHeader;

        string m_FilterOption;
        bool m_FilterActive = false;

        public delegate void MetaDataFiller(ref AssetLoadMarker marker, RawFrameDataView frameData, int sampleIndex);

        public struct AssetMarkerInfo
        {
            public AssetMarkerType assetMarkerType;
            public MetaDataFiller metaDataFiller;
            public int metadataCount;

            public AssetMarkerInfo(AssetMarkerType _assetMarkerType, MetaDataFiller _metadataFunction, int _metadataCount)
            {
                assetMarkerType = _assetMarkerType;
                metaDataFiller = _metadataFunction;
                metadataCount = _metadataCount;
            }
        }

        internal static void UnloadUnusedAssetsMetadataFiller(ref AssetLoadMarker marker, RawFrameDataView frameData, int sampleIndex)
        {
            marker.sourceName = "UnloadUnusedAssets";
            marker.subsystem = "Assets";
        }

        internal static void LoadAssetMetadataFiller(ref AssetLoadMarker marker, RawFrameDataView frameData, int sampleIndex)
        {
            // AssetBundle Name, Asset Name
            marker.sourceName = frameData.GetSampleMetadataAsString(sampleIndex, 0);
            marker.assetName = frameData.GetSampleMetadataAsString(sampleIndex, 1);
            if (string.IsNullOrWhiteSpace(marker.assetName))
                marker.assetName = "(All)";
            marker.subsystem = "Asset in AssetBundle";
        }

        internal static void LoadAssetSyncMetadataFiller(ref AssetLoadMarker marker, RawFrameDataView frameData, int sampleIndex)
        {
            // AssetBundle Name, Asset Name
            marker.sourceName = frameData.GetSampleMetadataAsString(sampleIndex, 0);
            marker.assetName = frameData.GetSampleMetadataAsString(sampleIndex, 1);
            if (string.IsNullOrWhiteSpace(marker.assetName))
                marker.assetName = "(All)";
            marker.subsystem = "Asset in AssetBundle";
        }

        internal static void LoadAllAssetsMetadataFiller(ref AssetLoadMarker marker, RawFrameDataView frameData, int sampleIndex)
        {
            // AssetBundle Name, Asset Name
            marker.sourceName = frameData.GetSampleMetadataAsString(sampleIndex, 0);
            marker.assetName = "LoadAll";
            marker.subsystem = "Asset in AssetBundle";
        }

        internal static void LoadBundleMetadataFiller(ref AssetLoadMarker marker, RawFrameDataView frameData, int sampleIndex)
        {
            // AssetBundle Name
            marker.sourceName = frameData.GetSampleMetadataAsString(sampleIndex, 0);
            marker.subsystem = "AssetBundle";
        }

        internal static void UnloadBundleMetadataFiller(ref AssetLoadMarker marker, RawFrameDataView frameData, int sampleIndex)
        {
            // AssetBundle Name
            marker.sourceName = frameData.GetSampleMetadataAsString(sampleIndex, 0);
            marker.subsystem = "AssetBundle";
        }

        internal static void LoadSceneMetadataFiller(ref AssetLoadMarker marker, RawFrameDataView frameData, int sampleIndex)
        {
            // Scene path, Scene Name
            marker.sourceName = frameData.GetSampleMetadataAsString(sampleIndex, 0);
            marker.assetName = frameData.GetSampleMetadataAsString(sampleIndex, 1);
            marker.subsystem = "Scene";
            marker.filename = marker.sourceName;
        }

        internal static void LoadSceneObjectsMetadataFiller(ref AssetLoadMarker marker, RawFrameDataView frameData, int sampleIndex)
        {
            // Path name, some kind of count
            marker.filename = frameData.GetSampleMetadataAsString(sampleIndex, 0);
            marker.sourceName = Path.GetFileName(marker.filename);
            marker.subsystem = "SceneObjects";
        }

        internal static void AsyncReadManagerMetadataFiller(ref AssetLoadMarker marker, RawFrameDataView frameData, int sampleIndex)
        {
            // Path, size, subsystem
            marker.filename = frameData.GetSampleMetadataAsString(sampleIndex, 0);
            marker.sizeBytes = (ulong)frameData.GetSampleMetadataAsLong(sampleIndex, 1);
            marker.subsystem = frameData.GetSampleMetadataAsString(sampleIndex, 2);
            marker.assetName = frameData.GetSampleMetadataAsString(sampleIndex, 3);
            marker.sourceName = Path.GetFileName(marker.filename);
        }

        internal static void ReadObjectMetadataFiller(ref AssetLoadMarker marker, RawFrameDataView frameData, int sampleIndex)
        {
            // path, offset, size bytes
            marker.filename = frameData.GetSampleMetadataAsString(sampleIndex, 0);
            marker.sizeBytes = (ulong)frameData.GetSampleMetadataAsLong(sampleIndex, 2);
            marker.subsystem = frameData.GetSampleMetadataAsString(sampleIndex, 3);
            marker.sourceName = Path.GetFileName(marker.filename);
        }

        public AssetLoadingProfilerView()
        {
            m_CaptureData = new AssetCaptureData();

            AddMarker("GarbageCollectAssetsProfile", AssetMarkerType.UnloadUnusedAssets, UnloadUnusedAssetsMetadataFiller, 0);
            AddMarker("AssetBundle.PerformAssetLoad", AssetMarkerType.AsyncLoadAsset, LoadAssetMetadataFiller, 2);
            AddMarker("AssetBundle.LoadAsset", AssetMarkerType.SyncLoadAsset, LoadAssetSyncMetadataFiller, 2);
            AddMarker("AssetBundle.Unload", AssetMarkerType.SyncUnloadBundle, UnloadBundleMetadataFiller, 1);
            AddMarker("AssetBundle.UnloadAsync.DestroyObjectsMainThread", AssetMarkerType.AsyncUnloadBundle, UnloadBundleMetadataFiller, 1);
            AddMarker("AssetBundle.LoadAssetBundle", AssetMarkerType.LoadBundle, LoadBundleMetadataFiller, 1);
            AddMarker("LoadSceneOperation", AssetMarkerType.LoadScene, LoadSceneMetadataFiller, 2);
            AddMarker("LoadFileThreaded_LoadObjects", AssetMarkerType.LoadSceneObjects, LoadSceneObjectsMetadataFiller, 1);
            AddMarker("ReadObjectFromSerializedFile", AssetMarkerType.ReadObject, ReadObjectMetadataFiller, 4);
            AddMarker("AsyncReadManager.SyncRequest", AssetMarkerType.SyncReadRequest, AsyncReadManagerMetadataFiller, 4);
            AddMarker("AsyncReadManager.ReadFile", AssetMarkerType.AsyncReadRequest, AsyncReadManagerMetadataFiller, 4);
        }

        public void AddMarker(string markerName, AssetMarkerType markerType, MetaDataFiller metaDataFiller, int metadataCount)
        {
            m_Markers.Add(markerName, new AssetMarkerInfo(markerType, metaDataFiller, metadataCount));
            m_MarkerToIDMap.Add(markerName, FrameDataView.invalidMarkerId);
        }

        public void CreateAssetMarkerTable()
        {
            m_AssetMarkerTreeViewState = new TreeViewState();
            m_AssetMarkerMulticolumnHeaderState = AssetMarkerTreeView.CreateDefaultMultiColumnHeaderState(700);

            m_AssetMarkerMultiColumnHeader = new MultiColumnHeader(m_AssetMarkerMulticolumnHeaderState);
            m_AssetMarkerMultiColumnHeader.SetSorting((int)AssetMarkerTreeView.Columns.Index, true);
            m_AssetMarkerMultiColumnHeader.ResizeToFit();

            m_AssetMarkerTableIsCreated = true;
        }

        public void UpdateAssetMarkerTable()
        {
            if (!m_AssetMarkerTableIsCreated)
                CreateAssetMarkerTable();
            m_AssetMarkerTreeView = new AssetMarkerTreeView(m_AssetMarkerTreeViewState, m_AssetMarkerMultiColumnHeader, m_CaptureData, this);
        }

        internal void DrawUIPane(IProfilerWindowController win)
        {
            if (win.IsRecording())
                GUILayout.Label("Cannot analyze Asset Loading markers while profiler is recording.");
            else
            {
                if (!DataPulled)
                {
                    GUILayout.Label("Select 'Analyze Markers' to view detailed Asset Loading metrics for the captured Profiler data.");
                    return;
                }

                int visibleFrameIndex = win.GetActiveVisibleFrameIndex();

                if (GetSelectedFrame() != visibleFrameIndex || m_CaptureData.m_AssetLoadMarkers.Count == 0 || FrameSelectionChanged || NewData /*|| ViewSelectionChanged*/)
                {
                    SetSelectedFrame(visibleFrameIndex);
                    FrameSelectionChanged = false;
                    NewData = false;
                    UpdateAssetMarkerTable();
                }

                string text;

                if (m_AssetMarkerTreeViewState != null)
                    text = "Asset Load Markers: " + m_AssetMarkerTreeView.GetCount();
                else
                    text = "";

                GUILayout.Label(text);

                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                DrawAssetMarkers();

                GUILayout.EndHorizontal();
            }
        }

        private void DrawAssetMarkers()
        {
            if (m_AssetMarkerTreeView != null)
            {
                Rect r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                m_AssetMarkerTreeView.OnGUI(r);
            }
        }

        public void DrawToolbar(Rect position)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (AssetLoadingFrameCountProfileToggle())
            {
                FrameSelectionChanged = true;
            }
            bool pull = GUILayout.Button("Analyze Markers", EditorStyles.toolbarButton, GUILayout.Width(150));
            if (pull)
            {
                PullFullData();
                NewData = true;
            }

            // Inspired by ConsoleWindow.cs
            Rect rect = GUILayoutUtility.GetRect(0, EditorGUILayout.kLabelFloatMaxW * 1.5f, EditorGUI.kSingleLineHeight,
                EditorGUI.kSingleLineHeight, EditorStyles.toolbarSearchField, GUILayout.MinWidth(60),
                GUILayout.MaxWidth(300));
            var filteringText = EditorGUI.ToolbarSearchField(rect, m_FilterOption, false);
            if (m_FilterOption != filteringText)
            {
                FilterBy(filteringText);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        public void FilterBy(string filteringText)
        {
            m_FilterOption = filteringText;
            m_FilterActive = !string.IsNullOrEmpty(m_FilterOption);
            NewData = true;
        }

        public void GoToMarker(AssetLoadMarker marker)
        {
            int frame, sampleId;
            if (selectedFrameSetup == AssetLoadingFrameSetup.ThisFrame)
            {
                frame = this.GetSelectedFrame();
                // Get the index into the list of sampleIds for this frame
                int sampleIdIndex = frame - marker.firstFrameIndex;
                sampleId = marker.sampleIds[sampleIdIndex];
            }
            else
            {
                frame = marker.firstFrameIndex;
                sampleId = marker.sampleIds[0];
            }

            var threadSubStr = marker.threadName.Split('.');
            if (threadSubStr.Length < 2) // Main thread and Render thread don't have a thread group
                OpenMarkerInCpuModule(frame, null, threadSubStr[0], (ulong)marker.threadId, marker.threadIndex, sampleId);
            else
                OpenMarkerInCpuModule(frame, threadSubStr[0], threadSubStr[1], (ulong)marker.threadId, marker.threadIndex, sampleId);
        }

        bool AssetLoadingFrameCountProfileToggle()
        {
            AssetLoadingFrameSetup oldFrameCount = selectedFrameSetup;
            AssetLoadingFrameSetup newFrameCount = (AssetLoadingFrameSetup)EditorGUILayout.EnumPopup(oldFrameCount, EditorStyles.toolbarDropDownLeft, GUILayout.Width(150f));
            if (oldFrameCount != newFrameCount)
            {
                selectedFrameSetup = newFrameCount;
            }
            return oldFrameCount != newFrameCount;
        }

        public bool FilterContains(string filename)
        {
            if (!m_FilterActive)
                return true;

            if (string.IsNullOrEmpty(filename))
                return false;

            return filename.IndexOf(m_FilterOption, StringComparison.CurrentCultureIgnoreCase) != -1;
        }

        public bool FilterActive()
        {
            return m_FilterActive;
        }

        void ClearData()
        {
            m_AssetMarkerTreeViewState = null;
            m_AssetMarkerMulticolumnHeaderState = null;
            m_AssetMarkerTreeView = null;
            m_AssetMarkerMultiColumnHeader = null;
            m_AssetMarkerTableIsCreated = false;
            m_CaptureData = new AssetCaptureData();
            DataPulled = false;

            // Once there is no data, this will not need to be called again
            ProfilerDriver.profileCleared -= ClearData;
        }

        public void PullFullData()
        {
            // Once the data has been pulled, it will have to be cleared when the profiler is
            ProfilerDriver.profileCleared += ClearData;

            int firstFrameIndex = ProfilerDriver.firstFrameIndex;
            int lastFrameIndex = ProfilerDriver.lastFrameIndex;

            s_PullData.Begin();
            PullDataStatic(ref m_CaptureData, m_Markers, ref m_MarkerToIDMap, firstFrameIndex, lastFrameIndex);
            s_PullData.End();

            DataPulled = true;
        }

        // returns true if the marker is a continuation from the previous frame, and adds it to the previous marker, returns false otherwise
        private static bool CheckForContinuationMarker(RawFrameDataView frameData, ref AssetCaptureData captureData, int threadIndex, int markerId, int frameIndex, int sampleIndex)
        {
            ulong markerStartTimeNs = frameData.GetSampleStartTimeNs(sampleIndex);
            ulong sampleLengthNs = frameData.GetSampleTimeNs(sampleIndex);

            // if the marker is right at the start of the frame, it could have been split from another on the previous frame
            if (frameData.frameStartTimeNs == markerStartTimeNs)
            {
                // check for a matching marker that ends at the exact point this one starts
                for (int j = 0; j < captureData.m_AssetLoadMarkers.Count; j++)
                {
                    if (captureData.m_AssetLoadMarkers[j].threadIndex == threadIndex && captureData.m_AssetLoadMarkers[j].markerId == markerId)
                    {
                        if (AbsDiff(captureData.m_AssetLoadMarkers[j].endNs, markerStartTimeNs) < 10)
                        {
                            captureData.m_AssetLoadMarkers[j].endNs += sampleLengthNs;
                            captureData.m_AssetLoadMarkers[j].lengthNs += sampleLengthNs;
                            captureData.m_AssetLoadMarkers[j].ms = captureData.m_AssetLoadMarkers[j].lengthNs * 0.000001;
                            captureData.m_AssetLoadMarkers[j].frameCount++;
                            captureData.m_AssetLoadMarkers[j].lastFrameIndex = frameIndex;
                            captureData.m_AssetLoadMarkers[j].sampleIds.Add(sampleIndex);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static void PullDataStatic(ref AssetCaptureData captureData, Dictionary<string, AssetMarkerInfo> markers, ref Dictionary<string, int> markerToIDMap, int firstFrameIndex, int lastFrameIndex)
        {
            string error = null;

            var depthStack = new Stack<int>();

            captureData.m_AssetLoadMarkers.Clear();
            captureData.m_StartTimeNs = ulong.MaxValue;
            captureData.m_EndTimeNs = ulong.MinValue;

            ProfilerFrameDataIterator frameIter = new ProfilerFrameDataIterator();
            double nsFrameStart;

            for (int frameIndex = firstFrameIndex; frameIndex <= lastFrameIndex; ++frameIndex)
            {
                frameIter.SetRoot(frameIndex, 0);

                int threadCount = frameIter.GetThreadCount(frameIndex);

                // iterate over the threads
                for (int threadIndex = 0; threadIndex < threadCount; ++threadIndex)
                {
                    using (RawFrameDataView frameData = ProfilerDriver.GetRawFrameDataView(frameIndex, threadIndex))
                    {
                        if (!frameData.valid)
                            break;

                        string fullThreadName = GetFullThreadName(frameData.threadGroupName, frameData.threadName);

                        nsFrameStart = frameData.frameStartTimeNs;
                        if (frameIndex == firstFrameIndex) // This may only need to be done for the main thread, but does the main thread have a guaranteed index?
                        {
                            if (captureData.m_StartTimeNs > nsFrameStart)
                                captureData.m_StartTimeNs = nsFrameStart; // We will assume that this will only be true for the first frame and thus not be changed, and proceed to use this value as the start time
                        }

                        GetMarkerIDs(frameData, markers, ref markerToIDMap);

                        depthStack.Clear();
                        // iterate over the samples to collect up any markers
                        int sampleCount = frameData.sampleCount;
                        for (int i = 0; i < sampleCount; ++i)
                        {
                            int markerId = frameData.GetSampleMarkerId(i);

                            if (markerId != FrameDataView.invalidMarkerId && markerToIDMap.ContainsValue(markerId))
                            {
                                s_AddNewMarker.Begin();
                                string markerName = frameData.GetMarkerName(markerId);
                                Assert.IsTrue(markers.ContainsKey(markerName), string.Format("Marker {0} is not present in requested markers.", markerName));

                                // if the marker isn't a continuation of a previous marker, add a new asset load marker
                                if (!CheckForContinuationMarker(frameData, ref captureData, threadIndex, markerId, frameIndex, i))
                                {
                                    AssetLoadMarker assetLoadMarker = new AssetLoadMarker();

                                    // fill in contexts from Metadata
                                    AssetMarkerInfo markerInfo = markers[markerName];
                                    if (markerInfo.metadataCount == frameData.GetSampleMetadataCount(i))
                                        markerInfo.metaDataFiller(ref assetLoadMarker, frameData, i);
                                    else if (string.IsNullOrEmpty(error)) // Check the error is only shown once
                                    {
                                        error = $"Some markers, such as '{markerName}', have unexpected metadata. This may be because of opening a profile captured with an older version of Unity. Certain values may be missing.";
                                        Debug.LogWarning(error);
                                    }

                                    ulong markerStartTimeNs = frameData.GetSampleStartTimeNs(i);
                                    ulong sampleLengthNs = frameData.GetSampleTimeNs(i);

                                    assetLoadMarker.markerId = markerId;

                                    if (string.IsNullOrEmpty(assetLoadMarker.filename))
                                    {
                                        assetLoadMarker.readablePath = "Unknown path";
                                        assetLoadMarker.readableFileName = "?";
                                    }
                                    else
                                    {
                                        assetLoadMarker.readablePath = assetLoadMarker.filename;
                                        assetLoadMarker.readableFileName = Path.GetFileName(assetLoadMarker.readablePath);
                                        if (assetLoadMarker.readablePath.Contains("/Analytics/"))
                                            assetLoadMarker.readableFileName += " (Analytics)";
                                    }

                                    assetLoadMarker.startNs = markerStartTimeNs;
                                    assetLoadMarker.lengthNs = sampleLengthNs;
                                    assetLoadMarker.endNs = markerStartTimeNs + sampleLengthNs;

                                    assetLoadMarker.startTimeMs = (markerStartTimeNs - captureData.m_StartTimeNs) * 0.000001; // could this go negative?
                                    assetLoadMarker.ms = assetLoadMarker.lengthNs * 0.000001;
                                    assetLoadMarker.frameCount = 1;

                                    assetLoadMarker.type = markerInfo.assetMarkerType;
                                    assetLoadMarker.markerName = markerName;
                                    assetLoadMarker.threadName = fullThreadName;
                                    assetLoadMarker.firstFrameIndex = frameIndex;
                                    assetLoadMarker.lastFrameIndex = frameIndex;
                                    assetLoadMarker.threadIndex = threadIndex;
                                    assetLoadMarker.threadId = frameData.threadId;
                                    assetLoadMarker.sampleIds.Add(i);
                                    assetLoadMarker.depth = 1 + depthStack.Count;

                                    captureData.m_AssetLoadMarkers.Add(assetLoadMarker);
                                }

                                s_AddNewMarker.End();
                            }
                            UpdateDepthStack(ref depthStack, frameData.GetSampleChildrenCount(i));
                        }
                    }
                }
            }
            frameIter.Dispose();
        }

        static void GetMarkerIDs(RawFrameDataView frameData, Dictionary<string, AssetMarkerInfo> markers, ref Dictionary<string, int> markerToIDMap)
        {
            s_GetMarkerIDs.Begin();
            // iterate over the markers we want to get their markerIDs
            foreach (var markerName in markers.Keys)
            {
                // this markerID can change for the same marker so have to do for each frame
                markerToIDMap[markerName] = frameData.GetMarkerId(markerName);
            }
            s_GetMarkerIDs.End();
        }
    }
}
