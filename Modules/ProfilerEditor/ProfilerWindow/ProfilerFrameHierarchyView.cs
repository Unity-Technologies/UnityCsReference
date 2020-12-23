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
        public const int invalidTreeViewId = -1;
        public const int invalidTreeViewDepth = -1;
        enum DetailedViewType
        {
            None,
            Objects,
            CallersAndCallees,
        }

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
        int m_FrameIndex = FrameDataView.invalidOrCurrentFrameIndex;

        [SerializeField]
        TreeViewState m_TreeViewState;

        [SerializeField]
        MultiColumnHeaderState m_MultiColumnHeaderState;

        [SerializeField]
        int m_ThreadIndexInThreadNames = 0;

        int threadIndexInThreadNames
        {
            set
            {
                m_ThreadIndexInThreadNames = value;
                if (Event.current == null || Event.current.type != EventType.Layout)
                {
                    m_ThreadIndexDuringLastNonLayoutEvent = value;
                }
            }
            get { return m_ThreadIndexInThreadNames; }
        }

        // threadGroupName, threadName, threadIndex
        public Action<string, string, int> userChangedThread = delegate {};

        int m_ThreadIndexDuringLastNonLayoutEvent = 0;

        [NonSerialized]
        List<ThreadInfo> m_ThreadInfoCache;

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
            get
            {
                InitIfNeeded();
                return m_TreeView;
            }
        }

        public ProfilerDetailedObjectsView detailedObjectsView
        {
            get
            {
                InitIfNeeded();
                return m_DetailedObjectsView;
            }
        }

        public ProfilerDetailedCallsView detailedCallsView
        {
            get
            {
                InitIfNeeded();
                return m_DetailedCallsView;
            }
        }

        [SerializeField]
        string m_FullThreadName;

        public string fullThreadName
        {
            get
            {
                return m_FullThreadName;
            }
            private set
            {
                m_FullThreadName = value;
            }
        }

        [SerializeField]
        string m_ThreadName;

        public string threadName
        {
            get
            {
                if (m_ThreadName == null)
                    m_ThreadName = string.Empty;
                return m_ThreadName;
            }
            private set
            {
                m_ThreadName = value;
            }
        }

        [field: SerializeField]
        public ulong threadId { get; private set; }

        [field: SerializeField]
        public int threadIndex { get; private set; }


        [SerializeField]
        string m_GroupName;

        public string groupName
        {
            get
            {
                if (m_GroupName == null)
                    m_GroupName = string.Empty;
                return m_GroupName;
            }
            private set
            {
                m_GroupName = value;
            }
        }

        // This is purely based on (potentially serialized) UI fields.
        // Use in cases where e.g. no valid Frame data is around to get Update and get all Thread data from ProfilerDriver but you still need something for the UI to show
        ThreadInfo CreateThreadInfoForCurrentlyShownThread() => new ThreadInfo() { fullName = m_FullThreadName, groupOrder = GetGroupOrder(m_FullThreadName), threadIndex = threadIndex };

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

        public event Action<ProfilerTimeSampleSelection> selectionChanged;

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
            m_FullThreadName = CPUProfilerModule.mainThreadName;
            m_ThreadName = CPUProfilerModule.mainThreadName;
            m_GroupName = CPUProfilerModule.mainThreadGroupName;
            k_SerializationPrefKeyPrefix = serializationPrefKeyPrefix;
            threadId = FrameDataView.invalidThreadId;
            threadIndex = FrameDataView.invalidThreadIndex;
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
            m_TreeView = new ProfilerFrameDataTreeView(m_TreeViewState, multiColumnHeader, cpuModule, m_ProfilerWindow);
            m_TreeView.selectionChanged += OnMainTreeViewSelectionChanged;
            m_TreeView.searchChanged += OnMainTreeViewSearchChanged;
            m_TreeView.Reload();

            m_SearchField = new SearchField();
            m_SearchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;

            if (m_DetailedObjectsView == null)
                m_DetailedObjectsView = new ProfilerDetailedObjectsView(detailedObjectsViewPrefKeyPrefix);
            m_DetailedObjectsView.gpuView = gpuView;
            m_DetailedObjectsView.frameItemEvent += FrameItem;
            m_DetailedObjectsView.OnEnable(cpuModule, this);
            if (m_DetailedCallsView == null)
            {
                m_DetailedCallsView = new ProfilerDetailedCallsView(detailedCallsViewPrefKeyPrefix);
            }
            m_DetailedCallsView.OnEnable(cpuModule, this);
            m_DetailedCallsView.frameItemEvent += FrameItem;
            if (m_DetailedViewSpliterState == null || !m_DetailedViewSpliterState.IsValid())
                m_DetailedViewSpliterState = SplitterState.FromRelative(new[] { SessionState.GetFloat(splitter0StatePrefKey, 70f), SessionState.GetFloat(splitter1StatePrefKey, 30f) }, new[] { 450f, 50f }, null);
            if (!m_Serialized)
                m_DetailedViewType = (DetailedViewType)SessionState.GetInt(detailedViewTypeStatePrefKey, (int)DetailedViewType.None);

            m_Serialized = true;
            m_Initialized = true;
        }

        public override void OnEnable(CPUOrGPUProfilerModule cpuModule, IProfilerWindowController profilerWindow, bool isGpuView)
        {
            base.OnEnable(cpuModule, profilerWindow, isGpuView);
            m_FrameIndex = FrameDataView.invalidOrCurrentFrameIndex;
            m_DetailedObjectsView?.OnEnable(cpuModule, this);
            m_DetailedCallsView?.OnEnable(cpuModule, this);
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

        public void DoGUI(HierarchyFrameDataView frameDataView, bool fetchData, ref bool updateViewLive, ProfilerViewType viewType)
        {
            using (m_DoGUIMarker.Auto())
            {
                if (Event.current.type != EventType.Layout && m_ThreadIndexDuringLastNonLayoutEvent != threadIndexInThreadNames)
                {
                    m_ThreadIndexDuringLastNonLayoutEvent = threadIndexInThreadNames;
                    EditorGUIUtility.ExitGUI();
                }
                InitIfNeeded();

                var isSearchAllowed = string.IsNullOrEmpty(treeView.searchString) || !(m_ProfilerWindow.ProfilerWindowOverheadIsAffectingProfilingRecordingData() && ProfilerDriver.deepProfiling);

                var isDataAvailable = frameDataView != null && frameDataView.valid;

                var showDetailedView = isDataAvailable && m_DetailedViewType != DetailedViewType.None;
                if (showDetailedView)
                    SplitterGUILayout.BeginHorizontalSplit(m_DetailedViewSpliterState);

                // Hierarchy view area
                GUILayout.BeginVertical();

                if (isDataAvailable && (threadIndex != frameDataView.threadIndex || threadName != frameDataView.threadName))
                    SetFrameDataView(frameDataView);

                DrawToolbar(frameDataView, showDetailedView, ref updateViewLive, viewType);

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
                    m_TreeView.OnGUI(rect, updateViewLive);

                    if (m_TreeView.HasSelection() && m_TreeView.proxySelectionInfo.hasProxySelection)
                    {
                        if (m_TreeView.proxySelectionInfo.cachedDisplayContent == null)
                        {
                            var diff = Math.Abs(m_TreeView.proxySelectionInfo.pathLengthDifferenceForProxy);
                            m_TreeView.proxySelectionInfo.cachedDisplayContent = new GUIContent(
                                BaseStyles.selectionExtraInfoHierarhcyView + string.Format(
                                    BaseStyles.proxySampleMessage,
                                    m_TreeView.proxySelectionInfo.nonProxyName, diff,
                                    diff == 1 ? BaseStyles.proxySampleMessageScopeSingular : BaseStyles.proxySampleMessageScopePlural),
                                BaseStyles.warningTriangle.image);
                        }
                        GUILayout.BeginHorizontal();
                        GUILayout.Box(m_TreeView.proxySelectionInfo.cachedDisplayContent, BaseStyles.selectionExtraInfoArea);
                        var rectForSampleStackButton = GUILayoutUtility.GetRect(BaseStyles.showDetailsDropdownContent, BaseStyles.tooltipDropdown, GUILayout.ExpandHeight(false), GUILayout.ExpandHeight(false));
                        if (GUI.Button(rectForSampleStackButton, BaseStyles.showDetailsDropdownContent, BaseStyles.tooltipDropdown))
                        {
                            var selection = m_TreeView.GetSelection();
                            var selectedId = (selection != null && selection.Count > 0) ? selection[0] : ProfilerFrameDataHierarchyView.invalidTreeViewId;
                            if (selectedId >= 0)
                            {
                                var menu = new GenericMenu();

                                // Show Sample Selection:
                                var rawSampleIndices = new List<int>(frameDataView.GetItemMergedSamplesCount(selectedId));
                                frameDataView.GetItemRawFrameDataViewIndices(selectedId, rawSampleIndices);
                                var actualMarkerIdPath = new List<int>(frameDataView.GetItemDepth(selectedId));
                                using (var iterator = new RawFrameDataView(frameDataView.frameIndex, frameDataView.threadIndex))
                                {
                                    string name = null;
                                    var rawIndex = ProfilerTimelineGUI.GetItemMarkerIdPath(iterator, cpuModule, rawSampleIndices[0], ref name, ref actualMarkerIdPath);
                                }

                                var actualMarkerPath = new List<string>(actualMarkerIdPath.Count);
                                foreach (var id in actualMarkerIdPath)
                                {
                                    if ((frameDataView.GetMarkerFlags(id) & Unity.Profiling.LowLevel.MarkerFlags.AvailabilityEditor) != 0)
                                        actualMarkerPath.Add(string.Format("EditorOnly [{0}]", frameDataView.GetMarkerName(id)));
                                    else
                                        actualMarkerPath.Add(frameDataView.GetMarkerName(id));
                                }

                                // admittedly, it'd be nice to only generate the text if sample selection option was chosen...
                                // however, that would need to happen in an OnGui call and not within the callback of the generic menu,
                                // to be able to calculate the needed window size and avoid glitches on first displaying it.
                                // at least the user already clicked on the dropdown for this...

                                string selectedSampleStackText = null;
                                var sampleStackSb = new System.Text.StringBuilder();
                                if (m_TreeView.proxySelectionInfo.nonProxySampleStack != null && m_TreeView.proxySelectionInfo.nonProxySampleStack.Count > 0)
                                {
                                    for (int i = m_TreeView.proxySelectionInfo.nonProxySampleStack.Count - 1; i >= 0; i--)
                                    {
                                        sampleStackSb.AppendLine(m_TreeView.proxySelectionInfo.nonProxySampleStack[i]);
                                    }
                                    selectedSampleStackText = sampleStackSb.ToString();
                                }
                                string actualSampleStackText = null;
                                if (actualMarkerPath != null && actualMarkerPath.Count > 0)
                                {
                                    sampleStackSb.Clear();

                                    for (int i = actualMarkerPath.Count - 1; i >= 0; i--)
                                    {
                                        sampleStackSb.AppendLine(actualMarkerPath[i]);
                                    }
                                    actualSampleStackText = sampleStackSb.ToString();
                                }

                                var selectionSampleStackContent = selectedSampleStackText != null ? new GUIContent(selectedSampleStackText) : null;
                                var actualSampleStackContent = actualSampleStackText != null ? new GUIContent(actualSampleStackText) : null;
                                var sampleStackWindowSize = SelectedSampleStackWindow.CalculateSize(selectionSampleStackContent, actualSampleStackContent);
                                menu.AddItem(BaseStyles.showSelectedSampleStacks, false, () =>
                                {
                                    SelectedSampleStackWindow.ShowSampleStackWindow(GUIUtility.GUIToScreenRect(rectForSampleStackButton).position, sampleStackWindowSize, selectionSampleStackContent, actualSampleStackContent);
                                });
                                menu.DropDown(rectForSampleStackButton);
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
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
            if (Event.current.isMouse && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
            {
                ProfilerWindowAnalytics.AddNewView(ProfilerWindowAnalytics.profilerCPUModuleSearch);
            }
            treeView.searchString = m_SearchField.OnToolbarGUI(rect, treeView.searchString);
        }

        void DrawToolbar(HierarchyFrameDataView frameDataView, bool showDetailedView, ref bool updateViewLive, ProfilerViewType viewType)
        {
            EditorGUILayout.BeginHorizontal(BaseStyles.toolbar);

            DrawViewTypePopup(viewType);

            DrawLiveUpdateToggle(ref updateViewLive);
            if (!gpuView)
            {
                DrawThreadPopup(frameDataView);
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

        class ThreadInfo : IComparable<ThreadInfo>, IEquatable<ThreadInfo>
        {
            public int groupOrder;
            public string fullName;
            public int threadIndex;

            public int CompareTo(ThreadInfo other)
            {
                if (this == other)
                    return 0;
                if (groupOrder != other.groupOrder)
                    return groupOrder - other.groupOrder;
                return EditorUtility.NaturalCompare(fullName, other.fullName);
            }

            public static bool operator==(ThreadInfo lhs, ThreadInfo rhs) => ReferenceEquals(lhs, null) && ReferenceEquals(rhs, null) || !ReferenceEquals(lhs, null) && lhs.Equals(rhs);
            public static bool operator!=(ThreadInfo lhs, ThreadInfo rhs) => !(ReferenceEquals(lhs, null) && ReferenceEquals(rhs, null)) || !ReferenceEquals(lhs, null) && !lhs.Equals(rhs);
            public override int GetHashCode() => (fullName.GetHashCode() * 21 + groupOrder.GetHashCode()) * 21 + threadIndex.GetHashCode();
            public override bool Equals(object other) => other is ThreadInfo && Equals((ThreadInfo)other);
            public bool Equals(ThreadInfo other) => !ReferenceEquals(other, null) && ReferenceEquals(this, other) || (groupOrder == other.groupOrder && fullName == other.fullName && threadIndex == other.threadIndex);
        }

        class ProfilerThreadSelectionDropdown : AdvancedDropdown
        {
            class ProfilerThreadSelectionDropDownItem : AdvancedDropdownItem
            {
                public ThreadInfo info;
                public ProfilerThreadSelectionDropDownItem(ThreadInfo info) : base(info.fullName)
                {
                    this.info = info;
                    id = info.GetHashCode();
                }
            }

            int m_SelectedThreadIndexInListOfThreadNames;
            List<ThreadInfo> m_ThreadInfos;
            Action<ThreadInfo, int> m_ItemSelectedCallback;
            public ProfilerThreadSelectionDropdown(List<ThreadInfo> threads, int selectedThreadIndexInListOfThreadNames, Action<ThreadInfo, int> itemSelected) : base(new AdvancedDropdownState())
            {
                m_ThreadInfos = threads;
                m_SelectedThreadIndexInListOfThreadNames = selectedThreadIndexInListOfThreadNames;
                m_ItemSelectedCallback = itemSelected;
            }

            protected override AdvancedDropdownItem BuildRoot()
            {
                AdvancedDropdownItem root = null;
                if (m_ThreadInfos != null && m_ThreadInfos.Count > 0)
                {
                    if (m_ThreadInfos.Count < m_SelectedThreadIndexInListOfThreadNames || m_SelectedThreadIndexInListOfThreadNames < 0)
                        m_SelectedThreadIndexInListOfThreadNames = 0;

                    root = new ProfilerThreadSelectionDropDownItem(m_ThreadInfos[m_SelectedThreadIndexInListOfThreadNames]);
                    for (int i = 0; i < m_ThreadInfos.Count; i++)
                    {
                        var child = new ProfilerThreadSelectionDropDownItem(m_ThreadInfos[i]);
                        child.elementIndex = i;
                        root.AddChild(child);

                        if (m_SelectedThreadIndexInListOfThreadNames == i)
                            m_DataSource.selectedIDs.Add(child.id);
                    }
                }
                return root;
            }

            protected override void ItemSelected(AdvancedDropdownItem item)
            {
                base.ItemSelected(item);
                m_ItemSelectedCallback?.Invoke((item as ProfilerThreadSelectionDropDownItem).info, item.elementIndex);
            }
        }

        static int GetGroupOrder(string threadName)
        {
            if (threadName.StartsWith(CPUProfilerModule.jobThreadNamePrefix, StringComparison.Ordinal))
                return 2;
            if (threadName.StartsWith(CPUProfilerModule.loadingThreadNamePrefix, StringComparison.Ordinal))
                return 3;
            if (threadName.StartsWith(CPUProfilerModule.scriptingThreadNamePrefix, StringComparison.Ordinal))
                return 4;
            if (threadName.StartsWith(CPUProfilerModule.mainThreadName, StringComparison.Ordinal))
                return 0;
            if (threadName.StartsWith(CPUProfilerModule.renderThreadName, StringComparison.Ordinal))
                return 1;
            return 10;
        }

        void UpdateThreadNamesAndThreadIndex(HierarchyFrameDataView frameDataView, bool forceUpdate = false)
        {
            if (frameDataView == null || !frameDataView.valid || (m_FrameIndex == frameDataView.frameIndex && !forceUpdate && m_ThreadInfoCache != null))
            {
                // make sure these caches and the selection have somewhat valid data
                if (m_ThreadInfoCache == null || m_ThreadInfoCache.Count <= 0)
                {
                    m_ThreadInfoCache = new List<ThreadInfo>();
                    m_ThreadInfoCache.Add(CreateThreadInfoForCurrentlyShownThread());
                }
                if (threadIndexInThreadNames < 0 || threadIndexInThreadNames > m_ThreadInfoCache.Count)
                    threadIndexInThreadNames = 0;
                return;
            }

            m_FrameIndex = frameDataView.frameIndex;
            threadIndexInThreadNames = FrameDataView.invalidThreadIndex;

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
                    m_ThreadInfoCache.Add(new ThreadInfo() { groupOrder = GetGroupOrder(name), fullName = name, threadIndex = i });
                }

                m_ThreadInfoCache.Sort();

                for (var i = 0; i < threadCount; ++i)
                {
                    if (m_FullThreadName == m_ThreadInfoCache[i].fullName)
                    {
                        // first check if we have a specific index (some names might be douplicates
                        if (threadIndex == m_ThreadInfoCache[i].threadIndex)
                        {
                            threadIndexInThreadNames = i;
                            break;
                        }
                        // otherwise, store the first fit.
                        else if (threadIndexInThreadNames == FrameDataView.invalidThreadIndex)
                            threadIndexInThreadNames = i;
                    }
                }
            }
            // security fallback to avoid index out of bounds
            if (threadIndexInThreadNames == FrameDataView.invalidThreadIndex)
                threadIndexInThreadNames = 0;
        }

        private void DrawThreadPopup(HierarchyFrameDataView frameDataView)
        {
            var style = BaseStyles.threadSelectionToolbarDropDown;
            var buttonContent = GUIContent.Temp(m_FullThreadName);
            float minWidth, maxWidth;
            BaseStyles.threadSelectionToolbarDropDown.CalcMinMaxWidth(buttonContent, out minWidth, out maxWidth);
            var position = GUILayoutUtility.GetRect(buttonContent, style, GUILayout.MinWidth(Math.Max(BaseStyles.detailedViewTypeToolbarDropDown.fixedWidth, minWidth)));

            var disabled = !(frameDataView != null && frameDataView.valid);
            using (new EditorGUI.DisabledScope(disabled))
            {
                if (EditorGUI.DropdownButton(position, buttonContent, FocusType.Keyboard, style))
                {
                    UpdateThreadNamesAndThreadIndex(frameDataView);
                    var dropdown = new ProfilerThreadSelectionDropdown(m_ThreadInfoCache, threadIndexInThreadNames, OnThreadSelectionChanged);
                    dropdown.Show(position);
                    GUIUtility.ExitGUI();
                }
            }
        }

        void OnThreadSelectionChanged(ThreadInfo info, int newThreadIndex)
        {
            if (newThreadIndex != threadIndexInThreadNames)
            {
                threadIndexInThreadNames = newThreadIndex;
                m_FullThreadName = info.fullName;
                var indexOfGroupNameSeparator = m_FullThreadName.IndexOf('.');
                if (indexOfGroupNameSeparator > 0)
                {
                    m_GroupName = m_FullThreadName.Substring(0, indexOfGroupNameSeparator);
                    m_ThreadName = m_FullThreadName.Substring(indexOfGroupNameSeparator + 1);
                }
                else
                {
                    m_GroupName = string.Empty;
                    m_ThreadName = m_FullThreadName;
                }
                var actualThreadIndex = FrameDataView.invalidThreadIndex;
                if (m_ThreadInfoCache != null && newThreadIndex < m_ThreadInfoCache.Count)
                    actualThreadIndex = m_ThreadInfoCache[newThreadIndex].threadIndex;
                userChangedThread(m_GroupName, m_ThreadName, actualThreadIndex);
                cpuModule.Repaint();
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

        void OnMainTreeViewSelectionChanged(ProfilerTimeSampleSelection selection)
        {
            if (selectionChanged != null)
                selectionChanged.Invoke(selection);
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
            m_TreeView.SetSelection(new List<int>(1) { id }, TreeViewSelectionOptions.FireSelectionChanged);
            m_TreeView.FrameItem(id);
        }

        public void SetSelection(ProfilerTimeSampleSelection selection, bool expandSelection)
        {
            if (selection.markerIdPath == null)
            {
                throw new ArgumentNullException(nameof(selection.markerIdPath));
            }
            if (selection.markerNamePath == null)
            {
                throw new ArgumentNullException(nameof(selection.markerNamePath));
            }
            if (selection.markerIdPath.Count != selection.markerPathDepth)
            {
                throw new ArgumentException($"ProfilerFrameDataHierarchyView.SetSelectionFromMarkerIDPath needs to be called with {nameof(selection)} having {nameof(selection.markerIdPath)} and {nameof(selection.markerNamePath)} with the same amount of elements.");
            }
            InitIfNeeded();
            m_TreeView.SetSelection(selection, expandSelection);
        }

        public void ClearSelection()
        {
            InitIfNeeded();
            m_TreeView.ClearSelection();
        }

        public void SetFrameDataView(HierarchyFrameDataView frameDataView)
        {
            InitIfNeeded();

            if (frameDataView.valid)
            {
                threadName = frameDataView.threadName;
                groupName = frameDataView.threadGroupName;
                threadId = frameDataView.threadId;
                threadIndex = frameDataView.threadIndex;
                fullThreadName = string.IsNullOrEmpty(groupName) ? threadName : $"{groupName}.{threadName}";
            }
            m_TreeView.SetFrameDataView(frameDataView);
            UpdateThreadNamesAndThreadIndex(frameDataView, forceUpdate: true);
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

        public override void OnDisable()
        {
            base.OnDisable();
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
