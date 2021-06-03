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
    internal enum FileIOViewType
    {
        Accesses,
        FileSummary
    }

    internal enum FileIOFrameSetup
    {
        ThisFrame,
        AllFrames
    }
    internal enum FileAccessType
    {
        Open,
        Read,
        Write,
        Close,
        Seek,
    };

    internal class FileAccessMarker
    {
        public int index = 0;
        public string filename;
        public string readableFileName;
        public string readablePath;

        public double startTimeMs;
        public double ms;           // Duration
        public FileAccessType type;
        public ulong sizeBytes;
        public ulong offsetBytes;
        public ulong originBytes;
        public ulong newOffsetBytes;

        public ulong startNs;
        public ulong endNs;
        public ulong lengthNs;
        public int markerId;
        public int frameCount; // number of frames included in this access

        public float averageBandwidthMBps;

        public string markerName;
        public string threadName;
        public int frameIndex;
        public int firstFrameIndex;
        public int lastFrameIndex;
        public int threadIndex;
        public ulong threadId;
        public List<int> sampleIds;                // Unique sample Id for each frame/thread

        public FileAccessMarker()
        {
            sampleIds = new List<int>();
        }

        public FileAccessMarker(int _frameIndex, int _threadIndex, int _sampleIndex)
        {
            sampleIds = new List<int>();
            sampleIds.Add(_sampleIndex);
            firstFrameIndex = _frameIndex;
            lastFrameIndex = _frameIndex;
            frameIndex = _frameIndex;
            threadIndex = _threadIndex;
            frameCount = 1;
        }
    }

    internal class FileSummary
    {
        public int index = 0;
        public string filename;
        public string readableFileName;
        public string readablePath;

        public ulong accesses;
        public ulong opened;
        public ulong closed;
        public ulong reads;
        public ulong writes;
        public ulong seeks;
        public ulong bytesRead;
        public ulong bytesWritten;
        public double totalAccessMs;
        public double openAccessMs;
        public double closeAccessMs;
        public double seekAccessMs;
        public double readAccessMs;
        public double writeAccessMs;
        public double readBandwidthMegaBytesPerSecond;
        public double writeBandwidthMegaBytesPerSecond;
        public double bandwidthMegaBytesPerSecond;

        public List<int> frameIndices;
        public int firstFrame;

        public FileSummary()
        {
            frameIndices = new List<int>();
        }
    }

    internal class FileAccessCaptureData
    {
        public double m_TotalTimeMs;

        public double m_StartTimeNs;
        public double m_EndTimeNs;

        public List<FileAccessMarker> m_FileAccessData;
        public Dictionary<string, FileSummary> m_FileSummaryData;

        public bool accessesSorted = false;
        public bool summariesSorted = false;
        public FileAccessCaptureData()
        {
            m_FileAccessData = new List<FileAccessMarker>();
            m_FileSummaryData = new Dictionary<string, FileSummary>();
        }
    }

    [Serializable]
    internal class FileIOProfilerView : LoadingProfilerViewBase
    {
        static ProfilerMarker s_PullData = new ProfilerMarker("FileIOProfiler.PullData");
        static ProfilerMarker s_SummarizeData = new ProfilerMarker("FileIOProfiler.SummarizeData");
        static ProfilerMarker s_GetMarkerIDs = new ProfilerMarker("FileIOProfiler.GetMarkerIDs");
        static ProfilerMarker s_AddNewMarker = new ProfilerMarker("FileIOProfiler.AddNewMarkerToList");
        static ProfilerMarker s_CheckSamples = new ProfilerMarker("FileIOProfiler.CheckSamples");

        private FileAccessCaptureData m_CaptureData;
        private FileSummary m_TempFileSummary;

        public FileIOFrameSetup GetFrameSetup() { return selectedFrameSetup; }
        public FileIOViewType GetViewType() { return selectedViewType; }

        Dictionary<string, FileAccessMarkerInfo> m_Markers = new Dictionary<string, FileAccessMarkerInfo>();
        Dictionary<string, int> m_MarkerToIDMap = new Dictionary<string, int>();

        private FileIOViewType selectedViewType = FileIOViewType.FileSummary;
        private FileIOFrameSetup selectedFrameSetup = FileIOFrameSetup.ThisFrame;

        public bool FrameSelectionChanged { get; set; }
        public bool ViewSelectionChanged { get; set; }

        [NonSerialized] private bool m_FileAccessTableIsCreated = false;
        [SerializeField] TreeViewState m_FileAccessTreeViewState;
        [SerializeField] MultiColumnHeaderState m_FileAccessMulticolumnHeaderState;
        FileAccessTreeView m_FileAccessTreeView;
        MultiColumnHeader m_FileAccessMultiColumnHeader;

        [NonSerialized] private bool m_FileSummaryTableIsCreated = false;
        [SerializeField] TreeViewState m_FileSummaryTreeViewState;
        [SerializeField] MultiColumnHeaderState m_FileSummaryMulticolumnHeaderState;
        FileSummaryTreeView m_FileSummaryTreeView;
        MultiColumnHeader m_FileSummaryMultiColumnHeader;

        string m_FilterOption;
        bool m_FilterActive = false;

        public delegate void MetaDataFiller(ref FileAccessMarker marker, RawFrameDataView frameData, int sampleIndex);

        public struct FileAccessMarkerInfo
        {
            public FileAccessType fileAccessType;
            public MetaDataFiller metaDataFiller;
            public int metadataCount;

            public FileAccessMarkerInfo(FileAccessType _fileAccessType, MetaDataFiller _metadataFunction, int _metadataCount)
            {
                fileAccessType = _fileAccessType;
                metaDataFiller = _metadataFunction;
                metadataCount = _metadataCount;
            }
        }

        internal static void FileOpenMetadataFiller(ref FileAccessMarker marker, RawFrameDataView frameData, int sampleIndex)
        {
            // Filename
            marker.filename = frameData.GetSampleMetadataAsString(sampleIndex, 0);
        }

        internal static void FileReadMetadataFiller(ref FileAccessMarker marker, RawFrameDataView frameData, int sampleIndex)
        {
            // Filename, Offset, Size
            marker.filename = frameData.GetSampleMetadataAsString(sampleIndex, 0);
            marker.offsetBytes = (ulong)frameData.GetSampleMetadataAsLong(sampleIndex, 1);
            marker.sizeBytes = (ulong)frameData.GetSampleMetadataAsLong(sampleIndex, 2);
        }

        internal static void FileWriteMetadataFiller(ref FileAccessMarker marker, RawFrameDataView frameData, int sampleIndex)
        {
            // Filename, Offset, Size
            marker.filename = frameData.GetSampleMetadataAsString(sampleIndex, 0);
            marker.offsetBytes = (ulong)frameData.GetSampleMetadataAsLong(sampleIndex, 1);
            marker.sizeBytes = (ulong)frameData.GetSampleMetadataAsLong(sampleIndex, 2);
        }

        internal static void FileSeekMetadataFiller(ref FileAccessMarker marker, RawFrameDataView frameData, int sampleIndex)
        {
            // Filename, Offset, Origin
            marker.filename = frameData.GetSampleMetadataAsString(sampleIndex, 0);
            marker.newOffsetBytes = (ulong)frameData.GetSampleMetadataAsLong(sampleIndex, 1);
            marker.originBytes = (ulong)frameData.GetSampleMetadataAsLong(sampleIndex, 2);
        }

        internal static void FileCloseMetadataFiller(ref FileAccessMarker marker, RawFrameDataView frameData, int sampleIndex)
        {
            // Filename
            marker.filename = frameData.GetSampleMetadataAsString(sampleIndex, 0);
        }

        public FileIOProfilerView()
        {
            m_CaptureData = new FileAccessCaptureData();
            m_Markers = new Dictionary<string, FileAccessMarkerInfo>();
            m_MarkerToIDMap = new Dictionary<string, int>();

            AddMarker("File.Open", FileAccessType.Open, FileOpenMetadataFiller, 1);
            AddMarker("File.Read", FileAccessType.Read, FileReadMetadataFiller, 3);
            AddMarker("File.Write", FileAccessType.Write, FileWriteMetadataFiller, 3);
            AddMarker("File.Seek", FileAccessType.Seek, FileSeekMetadataFiller, 3);
            AddMarker("File.Close", FileAccessType.Close, FileCloseMetadataFiller, 1);
        }

        public void AddMarker(string markerName, FileAccessType accessType, MetaDataFiller metaDataFiller, int metadataCount)
        {
            m_Markers.Add(markerName, new FileAccessMarkerInfo(accessType, metaDataFiller, metadataCount));
            m_MarkerToIDMap.Add(markerName, FrameDataView.invalidMarkerId);
        }

        public void GoToMarker(FileAccessMarker marker)
        {
            int frame, sampleId;
            if (selectedFrameSetup == FileIOFrameSetup.ThisFrame)
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

        public void CreateFileAccessTable()
        {
            m_FileAccessTreeViewState = new TreeViewState();
            m_FileAccessMulticolumnHeaderState = FileAccessTreeView.CreateDefaultMultiColumnHeaderState(700);

            m_FileAccessMultiColumnHeader = new MultiColumnHeader(m_FileAccessMulticolumnHeaderState);
            m_FileAccessMultiColumnHeader.SetSorting((int)FileAccessTreeView.FileIOColumns.Index, true);
            m_FileAccessMultiColumnHeader.ResizeToFit();

            m_FileAccessTableIsCreated = true;
        }

        public void CreateFileSummaryTable()
        {
            m_FileSummaryTreeViewState = new TreeViewState();
            m_FileSummaryMulticolumnHeaderState = FileSummaryTreeView.CreateDefaultMultiColumnHeaderState(700);

            m_FileSummaryMultiColumnHeader = new MultiColumnHeader(m_FileSummaryMulticolumnHeaderState);
            m_FileSummaryMultiColumnHeader.SetSorting((int)FileSummaryTreeView.SummaryColumns.TotalBytesRead, false);
            m_FileSummaryMultiColumnHeader.ResizeToFit();

            m_FileSummaryTableIsCreated = true;
        }

        public void UpdateFileAccessTable()
        {
            if (!m_FileAccessTableIsCreated)
                CreateFileAccessTable();
            m_FileAccessTreeView = new FileAccessTreeView(m_FileAccessTreeViewState, m_FileAccessMultiColumnHeader, m_CaptureData, this);
        }

        public void UpdateFileSummaryTable()
        {
            if (!m_FileSummaryTableIsCreated)
                CreateFileSummaryTable();
            m_FileSummaryTreeView = new FileSummaryTreeView(m_FileSummaryTreeViewState, m_FileSummaryMultiColumnHeader, m_CaptureData, this);
        }

        internal void DrawUIPane(IProfilerWindowController win)
        {
            if (win.IsRecording())
                GUILayout.Label("Cannot analyze FileIO markers while profiler is recording.");
            else
            {
                if (!DataPulled)
                {
                    GUILayout.Label("Select 'Analyze Markers' to view detailed File Access metrics for the captured Profiler data.");
                    return;
                }

                int visibleFrameIndex = win.GetActiveVisibleFrameIndex();

                if (GetSelectedFrame() != visibleFrameIndex || m_CaptureData.m_FileAccessData.Count == 0 || NewData || ViewSelectionChanged || FrameSelectionChanged)
                {
                    SetSelectedFrame(visibleFrameIndex);
                    if (selectedViewType == FileIOViewType.Accesses)
                    {
                        UpdateFileAccessTable();
                    }
                    else if (selectedViewType == FileIOViewType.FileSummary)
                    {
                        CalculateFileSummaries();
                        UpdateFileSummaryTable();
                    }
                    ViewSelectionChanged = false;
                    FrameSelectionChanged = false;
                    NewData = false;
                }

                string text;

                if (selectedViewType == FileIOViewType.Accesses)
                {
                    text = "File accesses: " + m_FileAccessTreeView.GetCount();
                }
                else
                {
                    text = "Files accessed: " + m_FileSummaryTreeView.GetCount();
                }

                GUILayout.Label(text);

                GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

                if (selectedViewType == FileIOViewType.Accesses)
                {
                    DrawFileAccesses();
                }
                else if (selectedViewType == FileIOViewType.FileSummary)
                {
                    DrawFileSummaries();
                }
                GUILayout.EndHorizontal();
            }
        }

        private void DrawFileAccesses()
        {
            if (m_FileAccessTreeView != null)
            {
                Rect r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                m_FileAccessTreeView.OnGUI(r);
            }
        }

        private void DrawFileSummaries()
        {
            if (m_FileSummaryTreeView != null)
            {
                Rect r = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
                m_FileSummaryTreeView.OnGUI(r);
            }
        }

        public void DrawToolbar(Rect position)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (FileIOViewTypeProfileToggle())
            {
                ViewSelectionChanged = true;
            }
            if (FileIOFrameCountProfileToggle())
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
            m_FilterActive = m_FilterOption != "";
            NewData = true;
        }

        bool FileIOViewTypeProfileToggle()
        {
            FileIOViewType oldViewType = selectedViewType;
            FileIOViewType newViewType = (FileIOViewType)EditorGUILayout.EnumPopup(oldViewType, EditorStyles.toolbarDropDownLeft, GUILayout.Width(150f));
            if (oldViewType != newViewType)
            {
                selectedViewType = newViewType;
            }
            return oldViewType != newViewType;
        }

        bool FileIOFrameCountProfileToggle()
        {
            FileIOFrameSetup oldFrameCount = selectedFrameSetup;
            FileIOFrameSetup newFrameCount = (FileIOFrameSetup)EditorGUILayout.EnumPopup(oldFrameCount, EditorStyles.toolbarDropDownLeft, GUILayout.Width(150f));
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
            m_FileAccessTreeViewState = null;
            m_FileAccessMulticolumnHeaderState = null;
            m_FileAccessTreeView = null;
            m_FileAccessMultiColumnHeader = null;
            m_FileAccessTableIsCreated = false;

            m_FileSummaryTreeViewState = null;
            m_FileSummaryMulticolumnHeaderState = null;
            m_FileSummaryTreeView = null;
            m_FileSummaryMultiColumnHeader = null;
            m_FileSummaryTableIsCreated = false;

            m_CaptureData = new FileAccessCaptureData();
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

            s_SummarizeData.Begin();
            CalculateFileSummaries();
            s_SummarizeData.End();
        }

        public static void PullDataStatic(ref FileAccessCaptureData captureData, Dictionary<string, FileAccessMarkerInfo> markers, ref Dictionary<string, int> markerToIDMap, int firstFrameIndex, int lastFrameIndex)
        {
            string error = null;

            captureData.m_FileAccessData.Clear();
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

                        nsFrameStart = frameData.frameStartTimeNs;
                        if (frameIndex == firstFrameIndex)
                        {
                            if (captureData.m_StartTimeNs > nsFrameStart)
                                captureData.m_StartTimeNs = nsFrameStart;
                        }

                        GetMarkerIDs(frameData, markers, ref markerToIDMap);

                        string fullThreadName = GetFullThreadName(frameData.threadGroupName, frameData.threadName);

                        // iterate over the samples to collect up any markers
                        int sampleCount = frameData.sampleCount;
                        for (int i = 0; i < sampleCount; ++i)
                        {
                            s_CheckSamples.Begin();
                            int markerId = frameData.GetSampleMarkerId(i);
                            if (markerId == FrameDataView.invalidMarkerId || !markerToIDMap.ContainsValue(markerId))
                            {
                                s_CheckSamples.End();
                                continue;
                            }

                            string markerName = frameData.GetMarkerName(markerId);
                            Assert.IsTrue(markers.ContainsKey(markerName), string.Format("Marker {0} is not present in requested markers.", markerName));

                            s_CheckSamples.End();

                            s_AddNewMarker.Begin();

                            ulong markerStartTimeNs = Math.Max(frameData.GetSampleStartTimeNs(i) - (ulong)captureData.m_StartTimeNs, 0);
                            ulong sampleLengthNs = frameData.GetSampleTimeNs(i);

                            // if the marker isn't a continuation of a previous marker, add a new access
                            if (!CheckForContinuationMarker(frameData, ref captureData, threadIndex, markerId, frameIndex, i, markerStartTimeNs, sampleLengthNs))
                            {
                                FileAccessMarker fileAccessMarker = new FileAccessMarker(frameIndex, threadIndex, i);

                                // fill in contexts from Metadata
                                FileAccessMarkerInfo markerInfo = markers[markerName];
                                if (markerInfo.metadataCount == frameData.GetSampleMetadataCount(i))
                                    markerInfo.metaDataFiller(ref fileAccessMarker, frameData, i);
                                else if (error == null) // Check the error is only shown once
                                {
                                    error = $"Some markers, such as '{markerName}', have unexpected metadata. This may be because of opening a profile captured with an older version of Unity. Certain values may be missing.";
                                    Debug.LogWarning(error);
                                }

                                if (string.IsNullOrEmpty(fileAccessMarker.filename))
                                {
                                    fileAccessMarker.readablePath = "Unknown path";
                                    fileAccessMarker.readableFileName = "?";
                                }
                                else
                                {
                                    fileAccessMarker.readablePath = fileAccessMarker.filename;
                                    fileAccessMarker.readableFileName = Path.GetFileName(fileAccessMarker.readablePath);
                                    if (fileAccessMarker.readablePath.Contains("/Analytics/"))
                                        fileAccessMarker.readableFileName += " (Analytics)";
                                }

                                fileAccessMarker.startNs = markerStartTimeNs;
                                fileAccessMarker.lengthNs = sampleLengthNs;
                                fileAccessMarker.endNs = markerStartTimeNs + sampleLengthNs;

                                fileAccessMarker.startTimeMs = markerStartTimeNs * 0.000001;
                                fileAccessMarker.ms = fileAccessMarker.lengthNs * 0.000001;

                                fileAccessMarker.markerId = markerId;
                                fileAccessMarker.type = markerInfo.fileAccessType;
                                fileAccessMarker.markerName = markerName;
                                fileAccessMarker.threadName = fullThreadName;
                                fileAccessMarker.threadId = frameData.threadId;

                                captureData.m_FileAccessData.Add(fileAccessMarker);
                            }
                            s_AddNewMarker.End();
                        }
                    }
                }
            }

            foreach (var fileAccessMarker in captureData.m_FileAccessData)
            {
                fileAccessMarker.averageBandwidthMBps = (fileAccessMarker.ms > 0) ? ((float)fileAccessMarker.sizeBytes / (float)fileAccessMarker.ms) * 0.001f : 0.0f;
            }

            frameIter.Dispose();
        }

        private static bool CheckForContinuationMarker(RawFrameDataView frameData, ref FileAccessCaptureData captureData, int threadIndex, int markerId, int frameIndex, int sampleIndex, ulong markerStartTimeNs, ulong sampleLengthNs)
        {
            // if the marker is right at the start of the frame, it could have been split from another on the previous frame
            if (frameData.frameStartTimeNs == markerStartTimeNs)
            {
                // check for a matching marker that ends at the exact point this one starts
                for (int j = 0; j < captureData.m_FileAccessData.Count; j++)
                {
                    if (captureData.m_FileAccessData[j].threadIndex == threadIndex && captureData.m_FileAccessData[j].markerId == markerId)
                    {
                        if (AbsDiff(captureData.m_FileAccessData[j].endNs, markerStartTimeNs) < 10) // Account for rounding errors
                        {
                            captureData.m_FileAccessData[j].endNs += sampleLengthNs;
                            captureData.m_FileAccessData[j].lengthNs += sampleLengthNs;
                            captureData.m_FileAccessData[j].ms = captureData.m_FileAccessData[j].lengthNs * 0.000001;
                            captureData.m_FileAccessData[j].frameCount++;
                            captureData.m_FileAccessData[j].lastFrameIndex = frameIndex;
                            captureData.m_FileAccessData[j].sampleIds.Add(sampleIndex);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static void GetMarkerIDs(RawFrameDataView frameData, Dictionary<string, FileAccessMarkerInfo> markers, ref Dictionary<string, int> markerToIDMap)
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

        private void CalculateFileSummaries()
        {
            if (selectedFrameSetup == FileIOFrameSetup.AllFrames)
                CalculateFileSummariesAllFrames();
            else if (selectedFrameSetup == FileIOFrameSetup.ThisFrame)
                CalculateFileSummariesForFrames(GetSelectedFrame(), GetSelectedFrame());
        }

        private void CalculateFileSummariesAllFrames()
        {
            m_CaptureData.m_FileSummaryData.Clear();
            FileAccessMarker thisFileAccessMarker;
            // summarize the accesses per file
            for (int i = 0; i < m_CaptureData.m_FileAccessData.Count; i++)
            {
                thisFileAccessMarker = m_CaptureData.m_FileAccessData[i];
                Summarize(thisFileAccessMarker);
            }
        }

        private void CalculateFileSummariesForFrames(int firstFrameIndex, int lastFrameIndex)
        {
            m_CaptureData.m_FileSummaryData.Clear();
            FileAccessMarker thisFileAccessMarker;
            // summarize the accesses per file if they are within the range of firstFrameIndex - lastFrameIndex
            for (int i = 0; i < m_CaptureData.m_FileAccessData.Count; i++)
            {
                thisFileAccessMarker = m_CaptureData.m_FileAccessData[i];
                if (thisFileAccessMarker.firstFrameIndex <= lastFrameIndex && thisFileAccessMarker.lastFrameIndex >= firstFrameIndex)
                {
                    Summarize(thisFileAccessMarker);
                }
            }
            CalculateBandwidths();
        }

        private void Summarize(FileAccessMarker thisFileAccessMarker)
        {
            if (m_CaptureData.m_FileSummaryData.ContainsKey(thisFileAccessMarker.filename))
                m_TempFileSummary = m_CaptureData.m_FileSummaryData[thisFileAccessMarker.filename];
            else
            {
                m_TempFileSummary = new FileSummary();
                m_TempFileSummary.filename = thisFileAccessMarker.filename;
                m_TempFileSummary.readableFileName = thisFileAccessMarker.readableFileName;
                m_TempFileSummary.readablePath = thisFileAccessMarker.readablePath;
                m_TempFileSummary.firstFrame = thisFileAccessMarker.firstFrameIndex;

                m_CaptureData.m_FileSummaryData.Add(thisFileAccessMarker.filename, m_TempFileSummary);
            }

            switch (thisFileAccessMarker.type)
            {
                case FileAccessType.Open:
                    m_TempFileSummary.opened++;
                    m_TempFileSummary.openAccessMs += thisFileAccessMarker.ms;
                    break;
                case FileAccessType.Close:
                    m_TempFileSummary.closed++;
                    m_TempFileSummary.closeAccessMs += thisFileAccessMarker.ms;
                    break;
                case FileAccessType.Read:
                    m_TempFileSummary.reads++;
                    m_TempFileSummary.bytesRead += thisFileAccessMarker.sizeBytes;
                    m_TempFileSummary.readAccessMs += thisFileAccessMarker.ms;
                    break;
                case FileAccessType.Write:
                    m_TempFileSummary.writes++;
                    m_TempFileSummary.bytesWritten += thisFileAccessMarker.sizeBytes;
                    m_TempFileSummary.writeAccessMs += thisFileAccessMarker.ms;
                    break;
                case FileAccessType.Seek:
                    m_TempFileSummary.seeks++;
                    m_TempFileSummary.seekAccessMs += thisFileAccessMarker.ms;
                    break;
                default:
                    break;
            }

            m_TempFileSummary.accesses++;
            m_TempFileSummary.totalAccessMs += thisFileAccessMarker.ms;

            if (thisFileAccessMarker.firstFrameIndex < m_TempFileSummary.firstFrame)
                m_TempFileSummary.firstFrame = thisFileAccessMarker.firstFrameIndex;

            for (int frameIndex = thisFileAccessMarker.firstFrameIndex; frameIndex <= thisFileAccessMarker.lastFrameIndex; frameIndex++)
            {
                if (!m_TempFileSummary.frameIndices.Contains(frameIndex))
                    m_TempFileSummary.frameIndices.Add(frameIndex);
            }
        }

        private void CalculateBandwidths()
        {
            foreach (var fileSummary in m_CaptureData.m_FileSummaryData.Values)
            {
                fileSummary.bandwidthMegaBytesPerSecond = fileSummary.totalAccessMs > 0 ? ((fileSummary.bytesRead + fileSummary.bytesWritten) / fileSummary.totalAccessMs) * 0.001f : 0.0f;
                fileSummary.readBandwidthMegaBytesPerSecond = fileSummary.readAccessMs > 0 ? (fileSummary.bytesRead / fileSummary.readAccessMs) * 0.001f : 0.0f;
                fileSummary.writeBandwidthMegaBytesPerSecond = fileSummary.writeAccessMs > 0 ? (fileSummary.bytesWritten / fileSummary.writeAccessMs) * 0.001f : 0.0f;
            }
        }
    }
}
