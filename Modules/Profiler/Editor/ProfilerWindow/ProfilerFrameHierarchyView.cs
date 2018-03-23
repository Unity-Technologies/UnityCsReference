// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

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

        static readonly GUIContent[] kDetailedViewTypeNames =
        {
            EditorGUIUtility.TextContent("No Details"),
            EditorGUIUtility.TextContent("Show Related Objects"),
            EditorGUIUtility.TextContent("Show Calls")
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
        TreeViewState m_TreeViewState;

        [SerializeField]
        MultiColumnHeaderState m_MultiColumnHeaderState;

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

        public ProfilerColumn sortedProfilerColumn
        {
            get
            {
                return m_TreeView == null ? ProfilerColumn.DontSort : m_TreeView.sortedProfilerColumn;
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

        public ProfilerFrameDataHierarchyView()
        {
            m_Initialized = false;
        }

        void InitIfNeeded()
        {
            if (m_Initialized)
                return;

            var cpuHierarchyColumns = new[]
            {
                ProfilerColumn.FunctionName, ProfilerColumn.TotalPercent, ProfilerColumn.SelfPercent, ProfilerColumn.Calls,
                ProfilerColumn.GCMemory, ProfilerColumn.TotalTime, ProfilerColumn.SelfTime, ProfilerColumn.WarningCount
            };
            var gpuHierarchyColumns = new[]
            {
                ProfilerColumn.FunctionName, ProfilerColumn.TotalGPUPercent, ProfilerColumn.DrawCalls, ProfilerColumn.TotalGPUTime
            };
            var profilerColumns = gpuView ? gpuHierarchyColumns : cpuHierarchyColumns;
            var defaultSortColumn = gpuView ? ProfilerColumn.TotalGPUTime : ProfilerColumn.TotalTime;

            var columns = CreateColumns(profilerColumns);
            var headerState = CreateDefaultMultiColumnHeaderState(columns, defaultSortColumn);
            if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_MultiColumnHeaderState, headerState))
                MultiColumnHeaderState.OverwriteSerializedFields(m_MultiColumnHeaderState, headerState);

            var firstInit = m_MultiColumnHeaderState == null;
            m_MultiColumnHeaderState = headerState;

            var multiColumnHeader = new ProfilerFrameDataMultiColumnHeader(m_MultiColumnHeaderState, columns) { height = 25 };
            if (firstInit)
                multiColumnHeader.ResizeToFit();

            // Check if it already exists (deserialized from window layout file or scriptable object)
            if (m_TreeViewState == null)
                m_TreeViewState = new TreeViewState();
            m_TreeView = new ProfilerFrameDataTreeView(m_TreeViewState, multiColumnHeader);
            m_TreeView.selectionChanged += OnMainTreeViewSelectionChanged;
            m_TreeView.searchChanged += OnMainTreeViewSearchChanged;

            m_SearchField = new SearchField();
            m_SearchField.downOrUpArrowKeyPressed += m_TreeView.SetFocusAndEnsureSelectedItem;

            if (m_DetailedObjectsView == null)
                m_DetailedObjectsView = new ProfilerDetailedObjectsView();
            m_DetailedObjectsView.gpuView = gpuView;
            m_DetailedObjectsView.frameItemEvent += FrameItem;
            if (m_DetailedCallsView == null)
                m_DetailedCallsView = new ProfilerDetailedCallsView();
            m_DetailedCallsView.frameItemEvent += FrameItem;
            if (m_DetailedViewSpliterState == null || m_DetailedViewSpliterState.relativeSizes == null || m_DetailedViewSpliterState.relativeSizes.Length == 0)
                m_DetailedViewSpliterState = new SplitterState(new[] { 70f, 30f }, new[] { 450, 50 }, null);

            m_Initialized = true;
        }

        public static ProfilerFrameDataMultiColumnHeader.Column[] CreateColumns(ProfilerColumn[] profilerColumns)
        {
            var columns = new ProfilerFrameDataMultiColumnHeader.Column[profilerColumns.Length];
            for (var i = 0; i < profilerColumns.Length; ++i)
            {
                var columnName = GetProfilerColumnName(profilerColumns[i]);
                var content = columnName.StartsWith("|")
                    ? EditorGUIUtility.IconContent("ProfilerColumn." + profilerColumns[i], columnName)
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

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(ProfilerFrameDataMultiColumnHeader.Column[] columns, ProfilerColumn defaultSortColumn)
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
                    case ProfilerColumn.FunctionName:
                        width = 200;
                        minWidth = 200;
                        autoResize = true;
                        allowToggleVisibility = false;
                        break;
                    case ProfilerColumn.WarningCount:
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
                    width = width, minWidth = minWidth, maxWidth = maxWidth,
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

        static string GetProfilerColumnName(ProfilerColumn column)
        {
            switch (column)
            {
                case ProfilerColumn.FunctionName:
                    return LocalizationDatabase.GetLocalizedString("Overview");
                case ProfilerColumn.TotalPercent:
                    return LocalizationDatabase.GetLocalizedString("Total");
                case ProfilerColumn.SelfPercent:
                    return LocalizationDatabase.GetLocalizedString("Self");
                case ProfilerColumn.Calls:
                    return LocalizationDatabase.GetLocalizedString("Calls");
                case ProfilerColumn.GCMemory:
                    return LocalizationDatabase.GetLocalizedString("GC Alloc");
                case ProfilerColumn.TotalTime:
                    return LocalizationDatabase.GetLocalizedString("Time ms");
                case ProfilerColumn.SelfTime:
                    return LocalizationDatabase.GetLocalizedString("Self ms");
                case ProfilerColumn.DrawCalls:
                    return LocalizationDatabase.GetLocalizedString("DrawCalls");
                case ProfilerColumn.TotalGPUTime:
                    return LocalizationDatabase.GetLocalizedString("GPU ms");
                case ProfilerColumn.SelfGPUTime:
                    return LocalizationDatabase.GetLocalizedString("Self ms");
                case ProfilerColumn.TotalGPUPercent:
                    return LocalizationDatabase.GetLocalizedString("Total");
                case ProfilerColumn.SelfGPUPercent:
                    return LocalizationDatabase.GetLocalizedString("Self");
                case ProfilerColumn.WarningCount:
                    return LocalizationDatabase.GetLocalizedString("|Warnings");
                case ProfilerColumn.ObjectName:
                    return LocalizationDatabase.GetLocalizedString("Name");
                default:
                    return "ProfilerColumn." + column;
            }
        }

        public void DoGUI(FrameDataView frameDataView)
        {
            InitIfNeeded();

            var collectingSamples = ProfilerDriver.enabled && (ProfilerDriver.profileEditor || EditorApplication.isPlaying);
            var isSearchAllowed = string.IsNullOrEmpty(treeView.searchString) || !(collectingSamples && ProfilerDriver.deepProfiling);

            var isDataAvailable = frameDataView != null && frameDataView.IsValid();
            if (isDataAvailable && isSearchAllowed)
                m_TreeView.SetFrameDataView(frameDataView);

            var showDetailedView = isDataAvailable && m_DetailedViewType != DetailedViewType.None;
            if (showDetailedView)
                SplitterGUILayout.BeginHorizontalSplit(m_DetailedViewSpliterState);

            // Hierarchy view area
            GUILayout.BeginVertical();

            DrawToolbar(frameDataView, showDetailedView);

            if (!isDataAvailable)
            {
                GUILayout.Label(BaseStyles.noData, BaseStyles.label);
            }
            else if (!isSearchAllowed)
            {
                GUILayout.Label(BaseStyles.disabledSearchText, BaseStyles.label);
            }
            else
            {
                var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandHeight(true), GUILayout.ExpandHeight(true));
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

        void DrawSearchBar()
        {
            var rect = GUILayoutUtility.GetRect(50f, 300f, EditorGUI.kSingleLineHeight, EditorGUI.kSingleLineHeight, EditorStyles.toolbarSearchField);
            treeView.searchString = m_SearchField.OnToolbarGUI(rect, treeView.searchString);
        }

        void DrawToolbar(FrameDataView frameDataView, bool showDetailedView)
        {
            EditorGUILayout.BeginHorizontal(BaseStyles.toolbar);

            DrawViewTypePopup(frameDataView.viewType);

            GUILayout.FlexibleSpace();

            if (frameDataView != null)
                GUILayout.Label(string.Format("CPU:{0}ms   GPU:{1}ms", frameDataView.frameTime, frameDataView.frameGpuTime), EditorStyles.miniLabel);

            GUILayout.FlexibleSpace();

            DrawSearchBar();

            if (!showDetailedView)
                DrawDetailedViewPopup();

            EditorGUILayout.EndHorizontal();
        }

        void DrawDetailedViewPopup()
        {
            m_DetailedViewType = (DetailedViewType)EditorGUILayout.IntPopup((int)m_DetailedViewType, kDetailedViewTypeNames, kDetailedViewTypes, EditorStyles.toolbarDropDown, GUILayout.Width(120));
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
                m_TreeView.Clear();
        }
    }
}
