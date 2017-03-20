// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditorInternal
{
    internal class ProfilerDetailedCallsView : ProfilerDetailedView
    {
        [NonSerialized]
        bool m_Initialized = false;

        [NonSerialized]
        float m_TotalSelectedPropertyTime;

        [NonSerialized]
        GUIContent m_TotalSelectedPropertyTimeLabel = new GUIContent("", "Total time of all calls of the selected function in the frame.");

        [SerializeField]
        SplitterState m_VertSplit = new SplitterState(new[] { 40f, 60f }, new[] { 50, 50 }, null);

        [SerializeField]
        CallsTreeViewController m_CallersTreeView;

        [SerializeField]
        CallsTreeViewController m_CalleesTreeView;

        struct CallsData
        {
            public List<CallInformation> calls;
            public float totalSelectedPropertyTime;
        }

        class CallInformation
        {
            public string name;
            public string path;
            public int callsCount;
            public int gcAllocBytes;
            public double totalCallTimeMs;
            public double totalSelfTimeMs;
            public double timePercent; // Cached value - calculated based on view type
        }

        class CallsTreeView : TreeView
        {
            public enum Type
            {
                Callers,
                Callees
            }

            public enum Column
            {
                Name,
                Calls,
                GcAlloc,
                TimeMs,
                TimePercent,

                Count
            }

            internal CallsData m_CallsData;
            Type m_Type;

            static string s_NoneText = LocalizationDatabase.GetLocalizedString("None");

            public CallsTreeView(Type type, TreeViewState treeViewState, MultiColumnHeader multicolumnHeader)
                : base(treeViewState, multicolumnHeader)
            {
                m_Type = type;

                showBorder = true;
                showAlternatingRowBackgrounds = true;

                multicolumnHeader.sortingChanged += OnSortingChanged;

                Reload();
            }

            public void SetCallsData(CallsData callsData)
            {
                m_CallsData = callsData;

                // Cache Time % value
                foreach (var callInfo in m_CallsData.calls)
                {
                    callInfo.timePercent = m_Type == Type.Callees
                        ? callInfo.totalCallTimeMs / m_CallsData.totalSelectedPropertyTime
                        : callInfo.totalSelfTimeMs / callInfo.totalCallTimeMs;
                }

                OnSortingChanged(multiColumnHeader);
            }

            protected override TreeViewItem BuildRoot()
            {
                var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
                var allItems = new List<TreeViewItem>();

                if (m_CallsData.calls != null && m_CallsData.calls.Count != 0)
                {
                    allItems.Capacity = m_CallsData.calls.Count;
                    for (var i = 0; i < m_CallsData.calls.Count; i++)
                        allItems.Add(new TreeViewItem { id = i + 1, depth = 0, displayName = m_CallsData.calls[i].name });
                }
                else
                {
                    allItems.Add(new TreeViewItem { id = 1, depth = 0, displayName = s_NoneText });
                }

                SetupParentsAndChildrenFromDepths(root, allItems);
                return root;
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                var item = args.item;

                for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
                {
                    CellGUI(args.GetCellRect(i), item, (Column)args.GetColumn(i), ref args);
                }
            }

            void CellGUI(Rect cellRect, TreeViewItem item, Column column, ref RowGUIArgs args)
            {
                if (m_CallsData.calls.Count == 0)
                {
                    base.RowGUI(args);
                    return;
                }

                var callInfo = m_CallsData.calls[args.item.id - 1];

                CenterRectUsingSingleLineHeight(ref cellRect);
                switch (column)
                {
                    case Column.Name:
                    {
                        DefaultGUI.Label(cellRect, callInfo.name, args.selected, args.focused);
                    }
                    break;
                    case Column.Calls:
                    {
                        var value = callInfo.callsCount.ToString();
                        DefaultGUI.LabelRightAligned(cellRect, value, args.selected, args.focused);
                    }
                    break;
                    case Column.GcAlloc:
                    {
                        var value = callInfo.gcAllocBytes;
                        DefaultGUI.LabelRightAligned(cellRect, value.ToString(), args.selected, args.focused);
                    }
                    break;
                    case Column.TimeMs:
                    {
                        var value = m_Type == Type.Callees ? callInfo.totalCallTimeMs : callInfo.totalSelfTimeMs;
                        DefaultGUI.LabelRightAligned(cellRect, value.ToString("f2"), args.selected, args.focused);
                    }
                    break;
                    case Column.TimePercent:
                    {
                        DefaultGUI.LabelRightAligned(cellRect, (callInfo.timePercent * 100f).ToString("f2"), args.selected, args.focused);
                    }
                    break;
                }
            }

            void OnSortingChanged(MultiColumnHeader header)
            {
                if (header.sortedColumnIndex == -1)
                    return; // No column to sort for (just use the order the data are in)

                var orderMultiplier = header.IsSortedAscending(header.sortedColumnIndex) ? 1 : -1;
                Comparison<CallInformation> comparison;
                switch ((Column)header.sortedColumnIndex)
                {
                    case Column.Name:
                        comparison = (callInfo1, callInfo2) => callInfo1.name.CompareTo(callInfo2.name) * orderMultiplier;
                        break;
                    case Column.Calls:
                        comparison = (callInfo1, callInfo2) => callInfo1.callsCount.CompareTo(callInfo2.callsCount) * orderMultiplier;
                        break;
                    case Column.GcAlloc:
                        comparison = (callInfo1, callInfo2) => callInfo1.gcAllocBytes.CompareTo(callInfo2.gcAllocBytes) * orderMultiplier;
                        break;
                    case Column.TimeMs:
                        comparison = (callInfo1, callInfo2) => callInfo1.totalCallTimeMs.CompareTo(callInfo2.totalCallTimeMs) * orderMultiplier;
                        break;
                    case Column.TimePercent:
                        comparison = (callInfo1, callInfo2) => callInfo1.timePercent.CompareTo(callInfo2.timePercent) * orderMultiplier;
                        break;
                    case Column.Count:
                        comparison = (callInfo1, callInfo2) => callInfo1.callsCount.CompareTo(callInfo2.callsCount) * orderMultiplier;
                        break;
                    default:
                        return;
                }

                m_CallsData.calls.Sort(comparison);
                Reload();
            }
        }

        [Serializable]
        class CallsTreeViewController
        {
            [NonSerialized]
            CallsTreeView m_View;

            [NonSerialized]
            bool m_Initialized;

            [SerializeField]
            TreeViewState m_ViewState;

            [SerializeField]
            MultiColumnHeaderState m_ViewHeaderState;

            [SerializeField]
            CallsTreeView.Type m_Type;

            static class Styles
            {
                public static GUIContent callersLabel = new GUIContent("Called From", "Parents the selected function is called from\n\n(Press 'F' for frame selection)");
                public static GUIContent calleesLabel = new GUIContent("Calls To", "Functions which are called from the selected function\n\n(Press 'F' for frame selection)");
                public static GUIContent callsLabel = new GUIContent("Calls", "Total number of calls in a selected frame");
                public static GUIContent gcAllocLabel = new GUIContent("GC Alloc");
                public static GUIContent timeMsCallersLabel = new GUIContent("Time ms", "Total time the selected function spend within a parent");
                public static GUIContent timeMsCalleesLabel = new GUIContent("Time ms", "Total time the child call spend within selected function");
                public static GUIContent timePctCallersLabel = new GUIContent("Time %", "Shows how often the selected function was called from the parent call");
                public static GUIContent timePctCalleesLabel = new GUIContent("Time %", "Shows how often child call was called from the selected function");
            }

            public delegate void CallSelectedCallback(string path, Event evt);

            public event CallSelectedCallback callSelected;

            public CallsTreeViewController(CallsTreeView.Type type)
            {
                m_Type = type;
            }

            void InitIfNeeded()
            {
                if (m_Initialized)
                    return;

                if (m_ViewState == null)
                    m_ViewState = new TreeViewState();

                var firstInit = m_ViewHeaderState == null;
                var headerState = CreateDefaultMultiColumnHeaderState();

                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_ViewHeaderState, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(m_ViewHeaderState, headerState);
                m_ViewHeaderState = headerState;

                var multiColumnHeader = new MultiColumnHeader(m_ViewHeaderState) { height = 25 };

                if (firstInit)
                {
                    multiColumnHeader.state.visibleColumns = new[]
                    {
                        (int)CallsTreeView.Column.Name, (int)CallsTreeView.Column.Calls, (int)CallsTreeView.Column.TimeMs, (int)CallsTreeView.Column.TimePercent,
                    };
                    multiColumnHeader.ResizeToFit();
                }

                m_View = new CallsTreeView(m_Type, m_ViewState, multiColumnHeader);

                m_Initialized = true;
            }

            MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
            {
                var columns = new[]
                {
                    new MultiColumnHeaderState.Column
                    {
                        headerContent = (m_Type == CallsTreeView.Type.Callers ? Styles.callersLabel : Styles.calleesLabel),
                        headerTextAlignment = TextAlignment.Left,
                        sortedAscending = true,
                        sortingArrowAlignment = TextAlignment.Center,
                        width = 150, minWidth = 150,
                        autoResize = true, allowToggleVisibility = false
                    },
                    new MultiColumnHeaderState.Column
                    {
                        headerContent = Styles.callsLabel,
                        headerTextAlignment = TextAlignment.Right,
                        sortedAscending = false,
                        sortingArrowAlignment = TextAlignment.Center,
                        width = 60, minWidth = 60,
                        autoResize = false, allowToggleVisibility = true
                    },
                    new MultiColumnHeaderState.Column
                    {
                        headerContent = Styles.gcAllocLabel,
                        headerTextAlignment = TextAlignment.Right,
                        sortedAscending = false,
                        sortingArrowAlignment = TextAlignment.Center,
                        width = 60, minWidth = 60,
                        autoResize = false, allowToggleVisibility = true
                    },
                    new MultiColumnHeaderState.Column
                    {
                        headerContent = (m_Type == CallsTreeView.Type.Callers ? Styles.timeMsCallersLabel : Styles.timeMsCalleesLabel),
                        headerTextAlignment = TextAlignment.Right,
                        sortedAscending = false,
                        sortingArrowAlignment = TextAlignment.Center,
                        width = 60, minWidth = 60,
                        autoResize = false, allowToggleVisibility = true
                    },
                    new MultiColumnHeaderState.Column
                    {
                        headerContent = (m_Type == CallsTreeView.Type.Callers ? Styles.timePctCallersLabel : Styles.timePctCalleesLabel),
                        headerTextAlignment = TextAlignment.Right,
                        sortedAscending = false,
                        sortingArrowAlignment = TextAlignment.Center,
                        width = 60, minWidth = 60,
                        autoResize = false, allowToggleVisibility = true
                    },
                };

                var state = new MultiColumnHeaderState(columns)
                {
                    sortedColumnIndex = (int)CallsTreeView.Column.TimeMs
                };
                return state;
            }

            public void SetCallsData(CallsData callsData)
            {
                InitIfNeeded();

                m_View.SetCallsData(callsData);
            }

            public void OnGUI(Rect r)
            {
                InitIfNeeded();

                m_View.OnGUI(r);
                HandleCommandEvents();
            }

            void HandleCommandEvents()
            {
                if (GUIUtility.keyboardControl != m_View.treeViewControlID)
                    return;

                if (m_ViewState.selectedIDs.Count == 0)
                    return;
                var callInfoIndex = m_ViewState.selectedIDs.First() - 1;
                if (callInfoIndex >= m_View.m_CallsData.calls.Count)
                    return;
                var callInfo = m_View.m_CallsData.calls[callInfoIndex];
                if (callSelected != null)
                {
                    var evt = Event.current;
                    callSelected.Invoke(callInfo.path, evt);
                }
            }
        }

        struct ParentCallInfo
        {
            public string name;
            public string path;
            public float timeMs;
        }

        public ProfilerDetailedCallsView(ProfilerHierarchyGUI mainProfilerHierarchyGUI)
            : base(mainProfilerHierarchyGUI) {}

        void InitIfNeeded()
        {
            if (m_Initialized)
                return;

            if (m_CallersTreeView == null)
                m_CallersTreeView = new CallsTreeViewController(CallsTreeView.Type.Callers);
            m_CallersTreeView.callSelected += OnCallSelected;

            if (m_CalleesTreeView == null)
                m_CalleesTreeView = new CallsTreeViewController(CallsTreeView.Type.Callees);
            m_CalleesTreeView.callSelected += OnCallSelected;

            m_Initialized = true;
        }

        public void DoGUI(GUIStyle headerStyle, int frameIndex, ProfilerViewType viewType)
        {
            var selectedPropertyPath = ProfilerDriver.selectedPropertyPath;
            if (string.IsNullOrEmpty(selectedPropertyPath))
            {
                DrawEmptyPane(headerStyle);
                return;
            }

            InitIfNeeded();
            UpdateIfNeeded(frameIndex, viewType, selectedPropertyPath);

            GUILayout.BeginVertical();
            GUILayout.Label(m_TotalSelectedPropertyTimeLabel, EditorStyles.label);
            SplitterGUILayout.BeginVerticalSplit(m_VertSplit, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            // Callees
            var rect = EditorGUILayout.BeginVertical();
            m_CalleesTreeView.OnGUI(rect);
            EditorGUILayout.EndVertical();

            // Callers
            rect = EditorGUILayout.BeginVertical();
            m_CallersTreeView.OnGUI(rect);
            EditorGUILayout.EndVertical();

            SplitterGUILayout.EndHorizontalSplit();
            GUILayout.EndVertical();
        }

        void UpdateIfNeeded(int frameIndex, ProfilerViewType viewType, string selectedPropertyPath)
        {
            if (m_CachedProfilerPropertyConfig.EqualsTo(frameIndex, viewType, ProfilerColumn.DontSort))
                return;

            var property = m_MainProfilerHierarchyGUI.GetRootProperty();
            var selectedPropertyName = GetProfilerPropertyName(selectedPropertyPath);

            m_TotalSelectedPropertyTime = 0;

            var callers = new Dictionary<string, CallInformation>();
            var callees = new Dictionary<string, CallInformation>();

            var parentCalls = new Stack<ParentCallInfo>();
            var parentIsSelected = false;
            while (property.Next(true))
            {
                var propertyName = property.propertyName;
                var propertyDepth = property.depth;
                float propertyTime;

                if (parentCalls.Count + 1 != propertyDepth)
                {
                    while (parentCalls.Count + 1 > propertyDepth)
                        parentCalls.Pop();
                    parentIsSelected = parentCalls.Count != 0 && selectedPropertyName == parentCalls.Peek().name;
                }

                if (parentCalls.Count != 0)
                {
                    var parent = parentCalls.Peek();

                    // Add caller
                    CallInformation callInfo;
                    int propertyCalls;
                    int propertyGcAlloc;
                    if (selectedPropertyName == propertyName)
                    {
                        propertyTime = property.GetColumnAsSingle(ProfilerColumn.TotalTime);
                        propertyCalls = (int)property.GetColumnAsSingle(ProfilerColumn.Calls);
                        propertyGcAlloc = (int)property.GetColumnAsSingle(ProfilerColumn.GCMemory);
                        if (!callers.TryGetValue(parent.name, out callInfo))
                        {
                            callers.Add(parent.name, new CallInformation()
                            {
                                name = parent.name, path = parent.path, callsCount = propertyCalls, gcAllocBytes = propertyGcAlloc, totalCallTimeMs = parent.timeMs, totalSelfTimeMs = propertyTime
                            });
                        }
                        else
                        {
                            callInfo.callsCount += propertyCalls;
                            callInfo.gcAllocBytes += propertyGcAlloc;
                            callInfo.totalCallTimeMs += parent.timeMs;
                            callInfo.totalSelfTimeMs += propertyTime;
                        }
                        m_TotalSelectedPropertyTime += propertyTime;
                    }

                    // Add callee
                    if (parentIsSelected)
                    {
                        propertyTime = property.GetColumnAsSingle(ProfilerColumn.TotalTime);
                        propertyCalls = (int)property.GetColumnAsSingle(ProfilerColumn.Calls);
                        propertyGcAlloc = (int)property.GetColumnAsSingle(ProfilerColumn.GCMemory);
                        if (!callees.TryGetValue(propertyName, out callInfo))
                        {
                            callees.Add(propertyName, new CallInformation()
                            {
                                name = propertyName, path = property.propertyPath, callsCount = propertyCalls, gcAllocBytes = propertyGcAlloc, totalCallTimeMs = propertyTime, totalSelfTimeMs = 0
                            });
                        }
                        else
                        {
                            callInfo.callsCount += propertyCalls;
                            callInfo.gcAllocBytes += propertyGcAlloc;
                            callInfo.totalCallTimeMs += propertyTime;
                        }
                    }
                }
                else
                {
                    if (selectedPropertyName == propertyName)
                    {
                        propertyTime = property.GetColumnAsSingle(ProfilerColumn.TotalTime);
                        m_TotalSelectedPropertyTime += propertyTime;
                    }
                }

                if (property.HasChildren)
                {
                    propertyTime = property.GetColumnAsSingle(ProfilerColumn.TotalTime);
                    parentCalls.Push(new ParentCallInfo() { name = propertyName, path = property.propertyPath, timeMs = propertyTime });

                    parentIsSelected = selectedPropertyName == propertyName;
                }
            }

            m_CallersTreeView.SetCallsData(new CallsData() { calls = callers.Values.ToList(), totalSelectedPropertyTime = m_TotalSelectedPropertyTime });
            m_CalleesTreeView.SetCallsData(new CallsData() { calls = callees.Values.ToList(), totalSelectedPropertyTime = m_TotalSelectedPropertyTime });

            m_TotalSelectedPropertyTimeLabel.text = selectedPropertyName + string.Format(" - Total time: {0:f2} ms", m_TotalSelectedPropertyTime);

            m_CachedProfilerPropertyConfig.Set(frameIndex, viewType, ProfilerColumn.TotalTime);
        }

        static string GetProfilerPropertyName(string propertyPath)
        {
            var pathDelimiterPos = propertyPath.LastIndexOf('/');
            return pathDelimiterPos == -1
                ? propertyPath
                : propertyPath.Substring(pathDelimiterPos + 1);
        }

        void OnCallSelected(string path, Event evt)
        {
            var eventType = evt.type;
            if (eventType != EventType.ExecuteCommand && eventType != EventType.ValidateCommand)
                return;

            // Avoid GC alloc through .commandName for other commands
            if (evt.commandName != "FrameSelected")
                return;

            if (eventType == EventType.ExecuteCommand)
                m_MainProfilerHierarchyGUI.SelectPath(path);

            evt.Use();
        }
    }
}
