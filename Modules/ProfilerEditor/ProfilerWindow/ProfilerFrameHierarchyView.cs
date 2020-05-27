// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Profiling;
using Unity.Profiling;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal class ProfilerFrameDataHierarchyView : ProfilerFrameDataViewBase
    {
        enum DetailedViewType
        {
            None,
            Objects,
            CallersAndCallees,
        }

        static readonly string kMainThreadName = "Main Thread";
        static readonly string kRenderThreadName = "Render Thread";

        static readonly GUIContent[] kDetailedViewTypeNames =
        {
            EditorGUIUtility.TrTextContent("No Details"),
            EditorGUIUtility.TrTextContent("Related Data"),
            EditorGUIUtility.TrTextContent("Calls")
        };
        static readonly int[] kDetailedViewTypes = new[]
        {
            (int)DetailedViewType.None,
            (int)DetailedViewType.Objects,
            (int)DetailedViewType.CallersAndCallees,
        };

        [NonSerialized]
        bool m_Initialized;
        [SerializeField]
        bool m_Serialized;

        [NonSerialized]
        int m_FrameIndex = -1;

        [SerializeField]
        TreeViewState m_TreeViewState;

        [SerializeField]
        MultiColumnHeaderState m_MultiColumnHeaderState;

        [SerializeField]
        int m_ThreadIndex = 0;

        int threadIndex
        {
            set
            {
                m_ThreadIndex = value;
                if (Event.current.type != EventType.Layout)
                {
                    m_ThreadIndexDuringLastNonLayoutEvent = value;
                }
            }
            get { return m_ThreadIndex; }
        }

        int m_ThreadIndexDuringLastNonLayoutEvent = 0;

        [NonSerialized]
        List<ThreadInfo> m_ThreadInfoCache;
        [NonSerialized]
        string[] m_ThreadNames;

        [SerializeField]
        DetailedViewType m_DetailedViewType = DetailedViewType.None;

        [SerializeField]
        SplitterState m_DetailedViewSpliterState;

        SearchField m_SearchField;
        ProfilerFrameDataTreeView m_TreeView;

        [SerializeField]
        ProfilerDetailedObjectsView m_DetailedObjectsView;

        [SerializeField]
        ProfilerDetailedCallsView m_DetailedCallsView;

        public ProfilerFrameDataTreeView treeView
        {
            get { return m_TreeView; }
        }

        public ProfilerDetailedObjectsView detailedObjectsView
        {
            get { return m_DetailedObjectsView; }
        }

        public ProfilerDetailedCallsView detailedCallsView
        {
            get { return m_DetailedCallsView; }
        }

        [SerializeField]
        string m_ThreadName;

        public string threadName
        {
            get
            {
                return m_ThreadName;
            }
            set
            {
                m_ThreadName = value;
            }
        }

        public int sortedProfilerColumn
        {
            get
            {
                return m_TreeView == null ? HierarchyFrameDataView.columnDontSort : m_TreeView.sortedProfilerColumn;
            }
        }

        public bool sortedProfilerColumnAscending
        {
            get
            {
                return m_TreeView == null ? false : m_TreeView.sortedProfilerColumnAscending;
            }
        }

        public delegate void SelectionChangedCallback(int id);
        public event SelectionChangedCallback selectionChanged;

        public delegate void SearchChangedCallback(string newSearch);
        public event SearchChangedCallback searchChanged;

        static readonly ProfilerMarker m_DoGUIMarker = new ProfilerMarker(nameof(ProfilerFrameDataHierarchyView) + ".DoGUI");

        readonly string k_SerializationPrefKeyPrefix;
        string multiColumnHeaderStatePrefKey => k_SerializationPrefKeyPrefix + "MultiColumnHeaderState";
        string splitter0StatePrefKey => k_SerializationPrefKeyPrefix + "Splitter.Relative[0]";
        string splitter1StatePrefKey => k_SerializationPrefKeyPrefix + "Splitter.Relative[1]";
        string detailedViewTypeStatePrefKey => k_SerializationPrefKeyPrefix + "DetailedViewTypeState";

        string detailedObjectsViewPrefKeyPrefix => k_SerializationPrefKeyPrefix + "DetailedObjectsView.";
        string detailedCallsViewPrefKeyPrefix => k_SerializationPrefKeyPrefix + "DetailedCallsView.";

        public ProfilerFrameDataHierarchyView(string serializationPrefKeyPrefix)
        {
            m_Initialized = false;
            m_ThreadName = kMainThreadName;
            k_SerializationPrefKeyPrefix = serializationPrefKeyPrefix;
        }

        void InitIfNeeded()
        {
            if (m_Initialized)
                return;

            var cpuHierarchyColumns = new[]
            {
                HierarchyFrameDataView.columnName,
                HierarchyFrameDataView.columnTotalPercent,
                HierarchyFrameDataView.columnSelfPercent,
                HierarchyFrameDataView.columnCalls,
                HierarchyFrameDataView.columnGcMemory,
                HierarchyFrameDataView.columnTotalTime,
                HierarchyFrameDataView.columnSelfTime,
                HierarchyFrameDataView.columnWarningCount
            };
            var gpuHierarchyColumns = new[]
            {
                HierarchyFrameDataView.columnName,
                HierarchyFrameDataView.columnTotalGpuPercent,
                HierarchyFrameDataView.columnDrawCalls,
                HierarchyFrameDataView.columnTotalGpuTime
            };
            var profilerColumns = gpuView ? gpuHierarchyColumns : cpuHierarchyColumns;
            var defaultSortColumn = gpuView ? HierarchyFrameDataView.columnTotalGpuTime : HierarchyFrameDataView.columnTotalTime;

            var columns = CreateColumns(profilerColumns);

            var multiColumnHeaderStateData = SessionState.GetString(multiColumnHeaderStatePrefKey, "");
            if (!string.IsNullOrEmpty(multiColumnHeaderStateData))
            {
                try
                {
                    var restoredHeaderState = JsonUtility.FromJson<MultiColumnHeaderState>(multiColumnHeaderStateData);
                    if (restoredHeaderState != null)
                        m_MultiColumnHeaderState = restoredHeaderState;
                }
                catch{} // Nevermind, we'll just fall back to the default
            }
            var headerState = CreateDefaultMultiColumnHeaderState(columns, defaultSortColumn);
            if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_MultiColumnHeaderState, headerState))
                MultiColumnHeaderState.OverwriteSerializedFields(m_MultiColumnHeaderState, headerState);

            var firstInit = m_MultiColumnHeaderState == null;
            m_MultiColumnHeaderState = headerState;

            var multiColumnHeader = new ProfilerFrameDataMultiColumnHeader(m_MultiColumnHeaderState, columns) { height = 25 };
            if (firstInit)
                multiColumnHeader.ResizeToFit();

            multiColumnHeader.visibleColumnsChanged += OnMultiColumnHeaderChanged;
            multiColumnHeader.sortingChanged += OnMultiColumnHeaderChanged;

            // Check if it already exists (deserialized from window layout file or scriptable object)
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();
            m_TreeView = new ProfilerFrameDataTreeView(m_TreeViewState, multiColumnHeader, cpuModule);
            m_TreeView.selectionChanged += OnMainTreeViewSelectionChanged;
            m_TreeView.searchChanged += OnMainTreeViewSearchChanged;
            m_TreeView.Reload();

            m_SearchField = new SearchField();
            m_SearchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;

            if (m_DetailedObjectsView == null)
                m_DetailedObjectsView = new ProfilerDetailedObjectsView(detailedObjectsViewPrefKeyPrefix);
            m_DetailedObjectsView.gpuView = gpuView;
            m_DetailedObjectsView.frameItemEvent += FrameItem;
            if (m_DetailedCallsView == null)
            {
                m_DetailedCallsView = new ProfilerDetailedCallsView(detailedCallsViewPrefKeyPrefix);
                m_DetailedCallsView.profilerSampleNameProvider = cpuModule;
            }
            m_DetailedCallsView.frameItemEvent += FrameItem;
            if (m_DetailedViewSpliterState == null || !m_DetailedViewSpliterState.IsValid())
                m_DetailedViewSpliterState = SplitterState.FromRelative(new[] { SessionState.GetFloat(splitter0StatePrefKey, 70f), SessionState.GetFloat(splitter1StatePrefKey, 30f) }, new[] { 450f, 50f }, null);
            if (!m_Serialized)
                m_DetailedViewType = (DetailedViewType)SessionState.GetInt(detailedViewTypeStatePrefKey, (int)DetailedViewType.None);

            m_Serialized = true;
            m_Initialized = true;
        }

        public override void OnEnable(CPUorGPUProfilerModule cpuModule, IProfilerWindowController profilerWindow, bool isGpuView)
        {
            base.OnEnable(cpuModule, profilerWindow, isGpuView);
            m_DetailedObjectsView?.OnEnable(cpuModule);
            m_DetailedCallsView?.OnEnable(cpuModule);
        }

        void OnMultiColumnHeaderChanged(MultiColumnHeader header)
        {
            SessionState.SetString(multiColumnHeaderStatePrefKey, JsonUtility.ToJson(header.state));
        }

        public static ProfilerFrameDataMultiColumnHeader.Column[] CreateColumns(int[] profilerColumns)
        {
            var columns = new ProfilerFrameDataMultiColumnHeader.Column[profilerColumns.Length];
            for (var i = 0; i < profilerColumns.Length; ++i)
            {
                var columnName = GetProfilerColumnName(profilerColumns[i]);
                var content = profilerColumns[i] == HierarchyFrameDataView.columnWarningCount
                    ? EditorGUIUtility.IconContent("ProfilerColumn.WarningCount", columnName)
                    : new GUIContent(columnName);
                var column = new ProfilerFrameDataMultiColumnHeader.Column
                {
                    profilerColumn = profilerColumns[i],
                    headerLabel = content
                };
                columns[i] = column;
            }

            return columns;
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(ProfilerFrameDataMultiColumnHeader.Column[] columns, int defaultSortColumn)
        {
            var headerColumns = new MultiColumnHeaderState.Column[columns.Length];
            for (var i = 0; i < columns.Length; ++i)
            {
                var width = 80;
                var minWidth = 50;
                var maxWidth = 1000000f;
                var autoResize = false;
                var allowToggleVisibility = true;
                switch (columns[i].profilerColumn)
                {
                    case HierarchyFrameDataView.columnName:
                        width = 200;
                        minWidth = 200;
                        autoResize = true;
                        allowToggleVisibility = false;
                        break;
                    case HierarchyFrameDataView.columnWarningCount:
                        width = 25;
                        minWidth = 25;
                        maxWidth = 25;
                        break;
                }

                var headerColumn = new MultiColumnHeaderState.Column
                {
                    headerContent = columns[i].headerLabel,
                    headerTextAlignment = TextAlignment.Left,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = width,
                    minWidth = minWidth,
                    maxWidth = maxWidth,
                    autoResize = autoResize,
                    allowToggleVisibility = allowToggleVisibility,
                    sortedAscending = i == 0
                };
                headerColumns[i] = headerColumn;
            }

            var state = new MultiColumnHeaderState(headerColumns)
            {
                sortedColumnIndex = ProfilerFrameDataMultiColumnHeader.GetMultiColumnHeaderIndex(columns, defaultSortColumn),
            };
            return state;
        }

        static string GetProfilerColumnName(int column)
        {
            switch (column)
            {
                case HierarchyFrameDataView.columnName:
                    return LocalizationDatabase.GetLocalizedString("Overview");
                case HierarchyFrameDataView.columnTotalPercent:
                    return LocalizationDatabase.GetLocalizedString("Total");
                case HierarchyFrameDataView.columnSelfPercent:
                    return LocalizationDatabase.GetLocalizedString("Self");
                case HierarchyFrameDataView.columnCalls:
                    return LocalizationDatabase.GetLocalizedString("Calls");
                case HierarchyFrameDataView.columnGcMemory:
                    return LocalizationDatabase.GetLocalizedString("GC Alloc");
                case HierarchyFrameDataView.columnTotalTime:
                    return LocalizationDatabase.GetLocalizedString("Time ms");
                case HierarchyFrameDataView.columnSelfTime:
                    return LocalizationDatabase.GetLocalizedString("Self ms");
                case HierarchyFrameDataView.columnDrawCalls:
                    return LocalizationDatabase.GetLocalizedString("DrawCalls");
                case HierarchyFrameDataView.columnTotalGpuTime:
                    return LocalizationDatabase.GetLocalizedString("GPU ms");
                case HierarchyFrameDataView.columnSelfGpuTime:
                    return LocalizationDatabase.GetLocalizedString("Self ms");
                case HierarchyFrameDataView.columnTotalGpuPercent:
                    return LocalizationDatabase.GetLocalizedString("Total");
                case HierarchyFrameDataView.columnSelfGpuPercent:
                    return LocalizationDatabase.GetLocalizedString("Self");
                case HierarchyFrameDataView.columnWarningCount:
                    return LocalizationDatabase.GetLocalizedString("|Warnings");
                case HierarchyFrameDataView.columnObjectName:
                    return LocalizationDatabase.GetLocalizedString("Object Name");
                case HierarchyFrameDataView.columnStartTime:
                    return LocalizationDatabase.GetLocalizedString("Start ms");
                default:
                    return "ProfilerColumn." + column;
            }
        }

        public void DoGUI(HierarchyFrameDataView frameDataView, bool fetchData, ref bool updateViewLive)
        {
            using (m_DoGUIMarker.Auto())
            {
                if (Event.current.type != EventType.Layout && m_ThreadIndexDuringLastNonLayoutEvent != threadIndex)
                {
                    m_ThreadIndexDuringLastNonLayoutEvent = threadIndex;
                    EditorGUIUtility.ExitGUI();
                }
                InitIfNeeded();

                var collectingSamples = ProfilerDriver.enabled && (ProfilerDriver.profileEditor || EditorApplication.isPlaying);
                var isSearchAllowed = string.IsNullOrEmpty(treeView.searchString) || !(collectingSamples && ProfilerDriver.deepProfiling);

                var isDataAvailable = frameDataView != null && frameDataView.valid;

                var showDetailedView = isDataAvailable && m_DetailedViewType != DetailedViewType.None;
                if (showDetailedView)
                    SplitterGUILayout.BeginHorizontalSplit(m_DetailedViewSpliterState);

                // Hierarchy view area
                GUILayout.BeginVertical();

                DrawToolbar(frameDataView, showDetailedView, ref updateViewLive);

                if (!string.IsNullOrEmpty(dataAvailabilityMessage))
                {
                    GUILayout.Label(dataAvailabilityMessage, BaseStyles.label);
                }
                else if (!isDataAvailable)
                {
                    if (!fetchData && !updateViewLive)
                        GUILayout.Label(BaseStyles.liveUpdateMessage, BaseStyles.label);
                    else
                        GUILayout.Label(BaseStyles.noData, BaseStyles.label);
                }
                else if (!isSearchAllowed)
                {
                    GUILayout.Label(BaseStyles.disabledSearchText, BaseStyles.label);
                }
                else
                {
                    var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandHeight(true), GUILayout.ExpandHeight(true));
                    m_TreeView.SetFrameDataView(frameDataView);
                    m_TreeView.OnGUI(rect);
                }

                GUILayout.EndVertical();

                if (showDetailedView)
                {
                    GUILayout.BeginVertical();

                    // Detailed view area
                    EditorGUILayout.BeginHorizontal(BaseStyles.toolbar);

                    DrawDetailedViewPopup();
                    GUILayout.FlexibleSpace();

                    cpuModule.DrawOptionsMenuPopup();
                    EditorGUILayout.EndHorizontal();

                    switch (m_DetailedViewType)
                    {
                        case DetailedViewType.Objects:
                            detailedObjectsView.DoGUI(BaseStyles.header, frameDataView, m_TreeView.GetSelection());
                            break;
                        case DetailedViewType.CallersAndCallees:
                            detailedCallsView.DoGUI(BaseStyles.header, frameDataView, m_TreeView.GetSelection());
                            break;
                    }

                    GUILayout.EndVertical();

                    SplitterGUILayout.EndHorizontalSplit();
                }

                HandleKeyboardEvents();
            }
        }

        void DrawSearchBar()
        {
            var rect = GUILayoutUtility.GetRect(50f, 300f, EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, EditorStyles.toolbarSearchField);
            treeView.searchString = m_SearchField.OnToolbarGUI(rect, treeView.searchString);
        }

        void DrawToolbar(HierarchyFrameDataView frameDataView, bool showDetailedView, ref bool updateViewLive)
        {
            EditorGUILayout.BeginHorizontal(BaseStyles.toolbar);

            if (frameDataView != null && frameDataView.valid)
                DrawViewTypePopup((frameDataView.viewMode & HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName) != 0 ? ProfilerViewType.Hierarchy : ProfilerViewType.RawHierarchy);
            else
                DrawViewTypePopup(ProfilerViewType.Hierarchy);

            DrawLiveUpdateToggle(ref updateViewLive);
            if (!gpuView)
            {
                using (new EditorGUI.DisabledScope(!(frameDataView != null && frameDataView.valid)))
                {
                    DrawThreadPopup(frameDataView);
                }
            }


            GUILayout.FlexibleSpace();

            if (frameDataView != null && frameDataView.valid)
                DrawCPUGPUTime(frameDataView.frameTimeMs, frameDataView.frameGpuTimeMs);

            GUILayout.FlexibleSpace();

            DrawSearchBar();

            if (!showDetailedView)
            {
                DrawDetailedViewPopup();
                EditorGUILayout.Space(); // workaround: Remove double lines
                cpuModule?.DrawOptionsMenuPopup();
            }

            EditorGUILayout.EndHorizontal();
        }

        class ThreadInfo : IComparable<ThreadInfo>
        {
            public int groupOrder;
            public string fullName;

            public int CompareTo(ThreadInfo other)
            {
                if (this == other)
                    return 0;
                if (groupOrder != other.groupOrder)
                    return groupOrder - other.groupOrder;
                return EditorUtility.NaturalCompare(fullName, other.fullName);
            }
        }

        static int GetGroupOrder(string threadName)
        {
            if (threadName.StartsWith("Job", StringComparison.Ordinal))
                return 2;
            if (threadName.StartsWith("Loading", StringComparison.Ordinal))
                return 3;
            if (threadName.StartsWith("Scripting Thread", StringComparison.Ordinal))
                return 4;
            if (threadName.StartsWith(kMainThreadName, StringComparison.Ordinal))
                return 0;
            if (threadName.StartsWith(kRenderThreadName, StringComparison.Ordinal))
                return 1;
            return 10;
        }

        void UpdateThreadNamesAndThreadIndex(HierarchyFrameDataView frameDataView)
        {
            if (m_FrameIndex == frameDataView.frameIndex)
                return;

            m_FrameIndex = frameDataView.frameIndex;
            threadIndex = 0;

            using (var frameIterator = new ProfilerFrameDataIterator())
            {
                var threadCount = frameIterator.GetThreadCount(m_FrameIndex);
                if (m_ThreadInfoCache == null || m_ThreadInfoCache.Capacity < threadCount)
                    m_ThreadInfoCache = new List<ThreadInfo>(threadCount);
                m_ThreadInfoCache.Clear();

                // Fetch all thread names
                for (var i = 0; i < threadCount; ++i)
                {
                    frameIterator.SetRoot(m_FrameIndex, i);
                    var groupName = frameIterator.GetGroupName();
                    var threadName = frameIterator.GetThreadName();
                    var name = string.IsNullOrEmpty(groupName) ? threadName : groupName + "." + threadName;
                    m_ThreadInfoCache.Add(new ThreadInfo() { groupOrder = GetGroupOrder(name), fullName = name });
                }

                m_ThreadInfoCache.Sort();

                if (m_ThreadNames == null || m_ThreadNames.Length != threadCount)
                    m_ThreadNames = new string[threadCount];
                // Make a display list with a current index selected
                for (var i = 0; i < threadCount; ++i)
                {
                    m_ThreadNames[i] = m_ThreadInfoCache[i].fullName;
                    if (m_ThreadName == m_ThreadNames[i])
                        threadIndex = i;
                }
            }
        }

        UnityEditor.Tuple<int, string[]> GetThreadNamesLazy(HierarchyFrameDataView frameDataView)
        {
            UpdateThreadNamesAndThreadIndex(frameDataView);
            return new UnityEditor.Tuple<int, string[]>(threadIndex, m_ThreadNames);
        }

        private void DrawThreadPopup(HierarchyFrameDataView frameDataView)
        {
            if (!(frameDataView != null && frameDataView.valid))
            {
                var disabledValues = new string[] { m_ThreadName };
                EditorGUILayout.AdvancedPopup(0, disabledValues, BaseStyles.threadSelectionToolbarDropDown, GUILayout.MinWidth(BaseStyles.detailedViewTypeToolbarDropDown.fixedWidth));
                return;
            }

            var newThreadIndex = 0;
            if (threadIndex == 0)
            {
                newThreadIndex = EditorGUILayout.AdvancedLazyPopup(m_ThreadName, threadIndex,
                    (() => GetThreadNamesLazy(frameDataView)),
                    BaseStyles.threadSelectionToolbarDropDown, GUILayout.MinWidth(BaseStyles.detailedViewTypeToolbarDropDown.fixedWidth));
            }
            else
            {
                float minWidth, maxWidth;
                var content = new GUIContent(m_ThreadName);
                BaseStyles.threadSelectionToolbarDropDown.CalcMinMaxWidth(content, out minWidth, out maxWidth);
                UpdateThreadNamesAndThreadIndex(frameDataView);
                newThreadIndex = EditorGUILayout.AdvancedPopup(threadIndex, m_ThreadNames, BaseStyles.threadSelectionToolbarDropDown, GUILayout.MinWidth(Math.Max(BaseStyles.detailedViewTypeToolbarDropDown.fixedWidth, minWidth)));
            }

            if (newThreadIndex != threadIndex)
            {
                threadIndex = newThreadIndex;
                m_ThreadName = m_ThreadNames[threadIndex];
                cpuModule.Repaint();
                EditorGUIUtility.ExitGUI();
            }
        }

        void DrawDetailedViewPopup()
        {
            m_DetailedViewType = (DetailedViewType)EditorGUILayout.IntPopup((int)m_DetailedViewType, kDetailedViewTypeNames, kDetailedViewTypes, BaseStyles.detailedViewTypeToolbarDropDown, GUILayout.Width(BaseStyles.detailedViewTypeToolbarDropDown.fixedWidth));
        }

        void HandleKeyboardEvents()
        {
            if (!m_TreeView.HasFocus() || !m_TreeView.HasSelection())
                return;

            var evt = Event.current;
            if (evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter))
                SelectObjectsInHierarchyView();
        }

        void SelectObjectsInHierarchyView()
        {
            var instanceIds = m_TreeView.GetSelectedInstanceIds();
            if (instanceIds == null || instanceIds.Count == 0)
                return;

            var selection = new List<UnityEngine.Object>();

            foreach (int t in instanceIds)
            {
                var obj = EditorUtility.InstanceIDToObject(t);
                var com = obj as Component;
                if (com != null)
                    selection.Add(com.gameObject);
                else if (obj != null)
                    selection.Add(obj);
            }

            if (selection.Count != 0)
                Selection.objects = selection.ToArray();
        }

        void OnMainTreeViewSelectionChanged(int id)
        {
            if (selectionChanged != null)
                selectionChanged.Invoke(id);
        }

        void OnMainTreeViewSearchChanged(string newSearch)
        {
            if (searchChanged != null)
                searchChanged.Invoke(newSearch);
            if (m_TreeView.HasSelection())
            {
                var selection = m_TreeView.GetSelection();
                if (selection != null && selection.Count > 0 && selection[0] > 0)
                {
                    m_TreeView.SetSelection(selection);
                    m_TreeView.FrameItem(selection[0]);
                }
            }
        }

        void FrameItem(int id)
        {
            m_TreeView.SetFocus();
            m_TreeView.SetSelection(new List<int>(1) { id });
            m_TreeView.FrameItem(id);
        }

        public void SetSelectionFromLegacyPropertyPath(string selectedPropertyPath)
        {
            InitIfNeeded();
            m_TreeView.SetSelectionFromLegacyPropertyPath(selectedPropertyPath);
        }

        public override void Clear()
        {
            if (m_DetailedObjectsView != null)
                m_DetailedObjectsView.Clear();
            if (m_DetailedCallsView != null)
                m_DetailedCallsView.Clear();
            if (m_TreeView != null)
            {
                if (m_TreeView.multiColumnHeader != null)
                {
                    m_TreeView.multiColumnHeader.visibleColumnsChanged -= OnMultiColumnHeaderChanged;
                    m_TreeView.multiColumnHeader.sortingChanged -= OnMultiColumnHeaderChanged;
                }
                m_TreeView.Clear();
            }
        }

        public void SaveViewSettings()
        {
            if (m_DetailedViewSpliterState != null && m_DetailedViewSpliterState.relativeSizes != null && m_DetailedViewSpliterState.relativeSizes.Length >= 2)
            {
                SessionState.SetFloat(splitter0StatePrefKey, m_DetailedViewSpliterState.relativeSizes[0]);
                SessionState.SetFloat(splitter1StatePrefKey, m_DetailedViewSpliterState.relativeSizes[1]);
            }
            SessionState.SetInt(detailedViewTypeStatePrefKey, (int)m_DetailedViewType);

            if (m_DetailedObjectsView != null)
                m_DetailedObjectsView.SaveViewSettings();
            if (m_DetailedCallsView != null)
                m_DetailedCallsView.SaveViewSettings();
        }

        public void OnDisable()
        {
            SaveViewSettings();
            if (m_TreeView != null)
            {
                if (m_TreeView.multiColumnHeader != null)
                {
                    m_TreeView.multiColumnHeader.visibleColumnsChanged -= OnMultiColumnHeaderChanged;
                    m_TreeView.multiColumnHeader.sortingChanged -= OnMultiColumnHeaderChanged;
                }
            }
            if (m_DetailedObjectsView != null)
                m_DetailedObjectsView.OnDisable();
            if (m_DetailedCallsView != null)
                m_DetailedCallsView.OnDisable();
        }
    }
}
