// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Profiling;
using UnityEngine;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal class ProfilerDetailedObjectsView : ProfilerDetailedView
    {
        static readonly string kInstancesCountFormatText = L10n.Tr("{0} instances of {1} sample:");
        static readonly string kInstancesCountTooltipText = L10n.Tr("Total count of samples which represent the selected item in the Hierarchy View.");
        static readonly string kMetadataText = LocalizationDatabase.GetLocalizedString("Metadata:");
        static readonly string kCallstackText = LocalizationDatabase.GetLocalizedString("Call Stack:");
        static readonly string kNoMetadataOrCallstackText = LocalizationDatabase.GetLocalizedString("No metadata or call stack is available for the selected sample.");

        [NonSerialized]
        bool m_Initialized;

        [NonSerialized]
        List<ulong> m_CachedCallstack = new List<ulong>();

        [NonSerialized]
        GUIContent m_InstancesLabel;

        [SerializeField]
        TreeViewState m_TreeViewState;

        [SerializeField]
        MultiColumnHeaderState m_MultiColumnHeaderState;

        [SerializeField]
        SplitterState m_VertSplit;

        [SerializeField]
        ProfilerFrameDataMultiColumnHeader m_MultiColumnHeader;
        ObjectsTreeView m_TreeView;
        Vector2 m_CallstackScrollViewPos;

        public bool gpuView { get; set; }

        public delegate void FrameItemCallback(int id);
        public event FrameItemCallback frameItemEvent;

        class ObjectInformation
        {
            public int id; // FrameDataView item id
            public int sampleIndex; // Merged sample index
            public int instanceId;
            public float[] columnValues;
            public string[] columnStrings;
        }

        class ObjectsTreeView : TreeView
        {
            ProfilerFrameDataMultiColumnHeader m_MultiColumnHeader;

            List<ObjectInformation> m_ObjectsData;
            static readonly IList<int> k_DefaultSelection = new int[] { 0 };

            public event FrameItemCallback frameItemEvent;

            public ObjectsTreeView(TreeViewState treeViewState, ProfilerFrameDataMultiColumnHeader multicolumnHeader)
                : base(treeViewState, multicolumnHeader)
            {
                m_MultiColumnHeader = multicolumnHeader;

                showBorder = true;
                showAlternatingRowBackgrounds = true;
                multicolumnHeader.sortingChanged += OnSortingChanged;

                Reload();
            }

            public int GetSelectedFrameDataViewId()
            {
                if (m_ObjectsData == null || state.selectedIDs.Count == 0)
                    return -1;

                var selectedId = state.selectedIDs[0];
                if (selectedId == -1 || selectedId >= m_ObjectsData.Count)
                    return -1;

                return m_ObjectsData[selectedId].id;
            }

            public int GetSelectedFrameDataViewMergedSampleIndex()
            {
                if (m_ObjectsData == null || state.selectedIDs.Count == 0)
                    return 0;

                var selectedId = state.selectedIDs[0];
                if (selectedId == -1 || selectedId >= m_ObjectsData.Count)
                    return 0;

                return m_ObjectsData[selectedId].sampleIndex;
            }

            public void SetData(List<ObjectInformation> objectsData)
            {
                // Reload by forcing soring of the new data
                m_ObjectsData = objectsData;
                OnSortingChanged(multiColumnHeader);

                // Ensure that we select the first item when we
                if (m_ObjectsData != null && !HasSelection())
                    SetSelection(k_DefaultSelection);
            }

            protected override TreeViewItem BuildRoot()
            {
                var root = new TreeViewItem { id = -1, depth = -1 };
                var allItems = new List<TreeViewItem>();

                if (m_ObjectsData != null && m_ObjectsData.Count != 0)
                {
                    allItems.Capacity = m_ObjectsData.Count;
                    for (var i = 0; i < m_ObjectsData.Count; i++)
                        allItems.Add(new TreeViewItem { id = i, depth = 0 });
                }
                else
                {
                    allItems.Add(new TreeViewItem { id = 0, depth = 0, displayName = kNoneText });
                }

                SetupParentsAndChildrenFromDepths(root, allItems);
                return root;
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                if (Event.current.rawType != EventType.Repaint)
                    return;

                if (m_ObjectsData == null || m_ObjectsData.Count == 0)
                {
                    base.RowGUI(args);
                    return;
                }

                var item = args.item;
                for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
                    CellGUI(args.GetCellRect(i), item, args.GetColumn(i), ref args);
            }

            void CellGUI(Rect cellRect, TreeViewItem item, int column, ref RowGUIArgs args)
            {
                var objData = m_ObjectsData[args.item.id];

                CenterRectUsingSingleLineHeight(ref cellRect);
                DefaultGUI.Label(cellRect, objData.columnStrings[column], args.selected, args.focused);
            }

            void OnSortingChanged(MultiColumnHeader header)
            {
                if (header.sortedColumnIndex == -1)
                    return; // No column to sort for (just use the order the data are in)

                if (m_ObjectsData != null)
                {
                    var columns = m_MultiColumnHeader.columns;
                    if (header.sortedColumnIndex >= columns.Length)
                        return;

                    var orderMultiplier = header.IsSortedAscending(header.sortedColumnIndex) ? 1 : -1;
                    Comparison<ObjectInformation> comparison;
                    switch (columns[header.sortedColumnIndex].profilerColumn)
                    {
                        case HierarchyFrameDataView.columnTotalTime:
                        case HierarchyFrameDataView.columnTotalPercent:
                        case HierarchyFrameDataView.columnTotalGpuTime:
                        case HierarchyFrameDataView.columnTotalGpuPercent:
                        case HierarchyFrameDataView.columnGcMemory:
                        case HierarchyFrameDataView.columnDrawCalls:
                            comparison = (objData1, objData2) => objData1.columnValues[header.sortedColumnIndex].CompareTo(objData2.columnValues[header.sortedColumnIndex]) * orderMultiplier;
                            break;
                        default:
                            comparison = (objData1, objData2) => objData1.columnStrings[header.sortedColumnIndex].CompareTo(objData2.columnStrings[header.sortedColumnIndex]) * orderMultiplier;
                            break;
                    }
                    m_ObjectsData.Sort(comparison);
                }

                Reload();
            }

            protected override void SingleClickedItem(int id)
            {
                if (m_ObjectsData == null)
                    return;

                var selectedInstanceId = m_ObjectsData[id].instanceId;
                var obj = EditorUtility.InstanceIDToObject(selectedInstanceId);
                if (obj is Component)
                    obj = ((Component)obj).gameObject;

                if (obj != null)
                    EditorGUIUtility.PingObject(obj.GetInstanceID());
            }

            protected override void DoubleClickedItem(int id)
            {
                if (m_ObjectsData == null)
                    return;

                if (frameItemEvent != null)
                    frameItemEvent.Invoke(m_ObjectsData[id].id);
            }

            protected override bool CanMultiSelect(TreeViewItem item)
            {
                return false;
            }
        }
        readonly string k_PrefKeyPrefix;
        string multiColumnHeaderStatePrefKey => k_PrefKeyPrefix + "MultiColumnHeaderState";
        string splitter0StatePrefKey => k_PrefKeyPrefix + "Splitter.Relative[0]";
        string splitter1StatePrefKey => k_PrefKeyPrefix + "Splitter.Relative[1]";

        public ProfilerDetailedObjectsView(string prefKeyPrefix)
        {
            k_PrefKeyPrefix = prefKeyPrefix;
        }

        void InitIfNeeded()
        {
            if (m_Initialized)
                return;

            if (m_CachedCallstack == null)
                m_CachedCallstack = new List<ulong>();

            if (m_InstancesLabel == null)
                m_InstancesLabel = new GUIContent();

            var cpuDetailColumns = new[]
            {
                HierarchyFrameDataView.columnObjectName,
                HierarchyFrameDataView.columnTotalPercent,
                HierarchyFrameDataView.columnGcMemory,
                HierarchyFrameDataView.columnTotalTime
            };
            var gpuDetailColumns = new[]
            {
                HierarchyFrameDataView.columnObjectName,
                HierarchyFrameDataView.columnTotalGpuPercent,
                HierarchyFrameDataView.columnDrawCalls,
                HierarchyFrameDataView.columnTotalGpuTime
            };
            var profilerColumns = gpuView ? gpuDetailColumns : cpuDetailColumns;
            var defaultSortColumn = gpuView ? HierarchyFrameDataView.columnTotalGpuTime : HierarchyFrameDataView.columnTotalTime;

            var columns = ProfilerFrameDataHierarchyView.CreateColumns(profilerColumns);
            var headerState = ProfilerFrameDataHierarchyView.CreateDefaultMultiColumnHeaderState(columns, defaultSortColumn);
            headerState.columns[0].minWidth = 60;
            headerState.columns[0].autoResize = true;
            headerState.columns[0].allowToggleVisibility = false;

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

            if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_MultiColumnHeaderState, headerState))
                MultiColumnHeaderState.OverwriteSerializedFields(m_MultiColumnHeaderState, headerState);

            var firstInit = m_MultiColumnHeaderState == null;
            m_MultiColumnHeaderState = headerState;

            m_MultiColumnHeader = new ProfilerFrameDataMultiColumnHeader(m_MultiColumnHeaderState, columns) { height = 25 };
            if (firstInit)
                m_MultiColumnHeader.ResizeToFit();

            m_MultiColumnHeader.visibleColumnsChanged += OnMultiColumnHeaderChanged;
            m_MultiColumnHeader.sortingChanged += OnMultiColumnHeaderChanged;

            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();
            m_TreeView = new ObjectsTreeView(m_TreeViewState, m_MultiColumnHeader);
            m_TreeView.frameItemEvent += frameItemEvent;

            if (m_VertSplit == null || !m_VertSplit.IsValid())
                m_VertSplit = SplitterState.FromRelative(new[] { SessionState.GetFloat(splitter0StatePrefKey, 60f), SessionState.GetFloat(splitter1StatePrefKey, 40f) }, new[] { 50f, 50f }, null);

            m_Initialized = true;
        }

        void OnMultiColumnHeaderChanged(MultiColumnHeader header)
        {
            SessionState.SetString(multiColumnHeaderStatePrefKey, JsonUtility.ToJson(header.state));
        }

        public void DoGUI(GUIStyle headerStyle, HierarchyFrameDataView frameDataView, IList<int> selection)
        {
            if (frameDataView == null || !frameDataView.valid || selection.Count == 0)
            {
                DrawEmptyPane(headerStyle);
                return;
            }

            InitIfNeeded();
            UpdateIfNeeded(frameDataView, selection[0]);

            var selectedSampleId = m_TreeView.GetSelectedFrameDataViewId();
            var selectedMergedSampleIndex = m_TreeView.GetSelectedFrameDataViewMergedSampleIndex();
            var selectedSampleMetadataCount = 0;
            if (selectedSampleId != -1)
            {
                frameDataView.GetItemMergedSampleCallstack(selectedSampleId, selectedMergedSampleIndex, m_CachedCallstack);
                selectedSampleMetadataCount = frameDataView.GetItemMergedSamplesMetadataCount(selectedSampleId, selectedMergedSampleIndex);
            }

            GUILayout.Label(m_InstancesLabel, EditorStyles.label);

            var showCallstack = m_CachedCallstack.Count > 0;
            var showMetadata = selectedSampleMetadataCount != 0;
            SplitterGUILayout.BeginVerticalSplit(m_VertSplit, Styles.expandedArea);

            // Detailed list
            var rect = EditorGUILayout.BeginVertical(Styles.expandedArea);

            m_TreeView.OnGUI(rect);

            EditorGUILayout.EndVertical();

            // Metadata area
            EditorGUILayout.BeginVertical(Styles.expandedArea);

            if (showCallstack)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
                ProfilerFrameDataViewBase.showFullDetailsForCallStacks = GUILayout.Toggle(ProfilerFrameDataViewBase.showFullDetailsForCallStacks, ProfilerFrameDataViewBase.showFullDetailsForCallStacksContent, EditorStyles.toolbarButton);
                EditorGUILayout.EndHorizontal();
            }

            // Display active text (We want word wrapped text with a vertical scrollbar)
            m_CallstackScrollViewPos = EditorGUILayout.BeginScrollView(m_CallstackScrollViewPos, Styles.callstackScroll);

            var sb = new StringBuilder();

            if (showMetadata || showCallstack)
            {
                if (showMetadata)
                {
                    var metadataInfo = frameDataView.GetMarkerMetadataInfo(frameDataView.GetItemMarkerID(selectedSampleId));

                    sb.Append(kMetadataText);
                    sb.Append('\n');
                    for (var i = 0; i < selectedSampleMetadataCount; ++i)
                    {
                        if (metadataInfo != null && i < metadataInfo.Length)
                            sb.Append(metadataInfo[i].name);
                        else
                            sb.Append(i);
                        sb.Append(": ");
                        sb.Append(frameDataView.GetItemMergedSamplesMetadata(selectedSampleId, selectedMergedSampleIndex, i));
                        sb.Append('\n');
                    }
                    sb.Append('\n');
                }

                if (showCallstack)
                {
                    m_ProfilerFrameDataHierarchyView.CompileCallStack(sb, m_CachedCallstack, frameDataView);
                }
            }
            else
            {
                sb.Append(kNoMetadataOrCallstackText);
            }

            var metadataText = sb.ToString();
            Styles.callstackTextArea.CalcMinMaxWidth(GUIContent.Temp(metadataText), out _, out var maxWidth);
            float minHeight =  Styles.callstackTextArea.CalcHeight(GUIContent.Temp(metadataText), maxWidth);
            EditorGUILayout.SelectableLabel(metadataText, Styles.callstackTextArea, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinWidth(maxWidth + 10), GUILayout.MinHeight(minHeight + 10));

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            SplitterGUILayout.EndVerticalSplit();
        }

        void UpdateIfNeeded(HierarchyFrameDataView frameDataView, int selectedId)
        {
            var needReload = m_SelectedID != selectedId || !Equals(m_FrameDataView, frameDataView);
            if (!needReload)
                return;

            m_FrameDataView = frameDataView;
            m_SelectedID = selectedId;
            m_TreeView.SetSelection(new List<int>());

            var samplesCount = m_FrameDataView.GetItemMergedSamplesCount(selectedId);

            // Collect all the data
            var instanceIDs = new List<int>(samplesCount);
            m_FrameDataView.GetItemMergedSamplesInstanceID(selectedId, instanceIDs);

            var columns = m_MultiColumnHeader.columns;
            var columnsCount = columns.Length;
            var objectsDatas = new List<string>[columnsCount];
            var objectsValueDatas = new List<float>[columnsCount];
            for (var i = 0; i < columnsCount; i++)
            {
                objectsDatas[i] = new List<string>(samplesCount);
                m_FrameDataView.GetItemMergedSamplesColumnData(selectedId, columns[i].profilerColumn, objectsDatas[i]);

                objectsValueDatas[i] = new List<float>(samplesCount);
                m_FrameDataView.GetItemMergedSamplesColumnDataAsFloats(selectedId, columns[i].profilerColumn, objectsValueDatas[i]);
            }

            // Store it per sample
            var objectsData = new List<ObjectInformation>();
            for (var i = 0; i < samplesCount; i++)
            {
                var objData = new ObjectInformation() { columnStrings = new string[columnsCount], columnValues = new float[columnsCount] };
                objData.id = selectedId;
                objData.sampleIndex = i;

                objData.instanceId = (i < instanceIDs.Count) ? instanceIDs[i] : 0;
                for (var j = 0; j < columnsCount; j++)
                {
                    objData.columnStrings[j] = (i < objectsDatas[j].Count) ? objectsDatas[j][i] : string.Empty;
                    objData.columnValues[j] = (i < objectsValueDatas[j].Count) ? objectsValueDatas[j][i] : 0.0f;
                }

                objectsData.Add(objData);
            }

            m_TreeView.SetData(objectsData);

            // Update instances label
            var sampleName = m_FrameDataView.GetItemName(selectedId);
            m_InstancesLabel.text = UnityString.Format(kInstancesCountFormatText, samplesCount, sampleName);
            m_InstancesLabel.tooltip = kInstancesCountTooltipText;
        }

        public void Clear()
        {
            if (m_TreeView != null)
            {
                if (m_TreeView.multiColumnHeader != null)
                {
                    m_TreeView.multiColumnHeader.visibleColumnsChanged -= OnMultiColumnHeaderChanged;
                    m_TreeView.multiColumnHeader.sortingChanged -= OnMultiColumnHeaderChanged;
                }
                m_TreeView.SetData(null);
            }
        }

        override public void SaveViewSettings()
        {
            if (m_VertSplit != null && m_VertSplit.relativeSizes != null && m_VertSplit.relativeSizes.Length >= 2)
            {
                SessionState.SetFloat(splitter0StatePrefKey, m_VertSplit.relativeSizes[0]);
                SessionState.SetFloat(splitter1StatePrefKey, m_VertSplit.relativeSizes[1]);
            }
        }

        public override void OnEnable(CPUOrGPUProfilerModule cpuOrGpuProfilerModule, ProfilerFrameDataHierarchyView profilerFrameDataHierarchyView)
        {
            base.OnEnable(cpuOrGpuProfilerModule, profilerFrameDataHierarchyView);
        }

        override public void OnDisable()
        {
            SaveViewSettings();
        }
    }
}
