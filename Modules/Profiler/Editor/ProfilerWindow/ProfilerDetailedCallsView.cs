// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditorInternal.Profiling
{
    [Serializable]
    internal class ProfilerDetailedCallsView : ProfilerDetailedView
    {
        [NonSerialized]
        bool m_Initialized = false;

        [NonSerialized]
        float m_TotalSelectedPropertyTime;

        [NonSerialized]
        GUIContent m_TotalSelectedPropertyTimeLabel = new GUIContent("", "Total time of all calls of the selected function in the frame.");

        [SerializeField]
        SplitterState m_VertSplit;

        [SerializeField]
        CallsTreeViewController m_CalleesTreeView;

        [SerializeField]
        CallsTreeViewController m_CallersTreeView;

        public delegate void FrameItemCallback(int id);
        public event FrameItemCallback frameItemEvent;

        struct CallsData
        {
            public List<CallInformation> calls;
            public float totalSelectedPropertyTime;
        }

        class CallInformation
        {
            public int id; // FrameDataView item id
            public string name;
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

            public event FrameItemCallback frameItemEvent;

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
                if (m_CallsData.calls != null)
                {
                    foreach (var callInfo in m_CallsData.calls)
                    {
                        callInfo.timePercent = m_Type == Type.Callees
                            ? callInfo.totalCallTimeMs / m_CallsData.totalSelectedPropertyTime
                            : callInfo.totalSelfTimeMs / callInfo.totalCallTimeMs;
                    }
                }

                OnSortingChanged(multiColumnHeader);
            }

            protected override TreeViewItem BuildRoot()
            {
                var root = new TreeViewItem { id = 0, depth = -1 };
                var allItems = new List<TreeViewItem>();

                if (m_CallsData.calls != null && m_CallsData.calls.Count != 0)
                {
                    allItems.Capacity = m_CallsData.calls.Count;
                    for (var i = 0; i < m_CallsData.calls.Count; i++)
                        allItems.Add(new TreeViewItem { id = i + 1, depth = 0, displayName = m_CallsData.calls[i].name });
                }
                else
                {
                    allItems.Add(new TreeViewItem { id = 1, depth = 0, displayName = kNoneText });
                }

                SetupParentsAndChildrenFromDepths(root, allItems);
                return root;
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                if (Event.current.rawType != EventType.Repaint)
                    return;

                for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
                {
                    CellGUI(args.GetCellRect(i), args.item, (Column)args.GetColumn(i), ref args);
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
                        DefaultGUI.Label(cellRect, value, args.selected, args.focused);
                    }
                    break;
                    case Column.GcAlloc:
                    {
                        var value = callInfo.gcAllocBytes;
                        DefaultGUI.Label(cellRect, value.ToString(), args.selected, args.focused);
                    }
                    break;
                    case Column.TimeMs:
                    {
                        var value = m_Type == Type.Callees ? callInfo.totalCallTimeMs : callInfo.totalSelfTimeMs;
                        DefaultGUI.Label(cellRect, value.ToString("f2"), args.selected, args.focused);
                    }
                    break;
                    case Column.TimePercent:
                    {
                        DefaultGUI.Label(cellRect, (callInfo.timePercent * 100f).ToString("f2"), args.selected, args.focused);
                    }
                    break;
                }
            }

            void OnSortingChanged(MultiColumnHeader header)
            {
                if (header.sortedColumnIndex == -1)
                    return; // No column to sort for (just use the order the data are in)

                if (m_CallsData.calls != null)
                {
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
                }

                Reload();
            }

            protected override void DoubleClickedItem(int id)
            {
                if (m_CallsData.calls == null || m_CallsData.calls.Count == 0)
                    return;

                if (frameItemEvent != null)
                    frameItemEvent.Invoke(m_CallsData.calls[id - 1].id);
            }
        }

        [Serializable]
        class CallsTreeViewController
        {
            [NonSerialized]
            bool m_Initialized;

            [NonSerialized]
            CallsTreeView.Type m_Type;

            [SerializeField]
            TreeViewState m_ViewState;

            [SerializeField]
            MultiColumnHeaderState m_ViewHeaderState;

            CallsTreeView m_View;

            static class Styles
            {
                public static GUIContent callersLabel = EditorGUIUtility.TrTextContent("Called From", "Parents the selected function is called from\n\n(Press 'F' for frame selection)");
                public static GUIContent calleesLabel = EditorGUIUtility.TrTextContent("Calls To", "Functions which are called from the selected function\n\n(Press 'F' for frame selection)");
                public static GUIContent callsLabel = EditorGUIUtility.TrTextContent("Calls", "Total number of calls in a selected frame");
                public static GUIContent gcAllocLabel = EditorGUIUtility.TrTextContent("GC Alloc");
                public static GUIContent timeMsCallersLabel = EditorGUIUtility.TrTextContent("Time ms", "Total time the selected function spend within a parent");
                public static GUIContent timeMsCalleesLabel = EditorGUIUtility.TrTextContent("Time ms", "Total time the child call spend within selected function");
                public static GUIContent timePctCallersLabel = EditorGUIUtility.TrTextContent("Time %", "Shows how often the selected function was called from the parent call");
                public static GUIContent timePctCalleesLabel = EditorGUIUtility.TrTextContent("Time %", "Shows how often child call was called from the selected function");
            }

            public event FrameItemCallback frameItemEvent;

            public CallsTreeViewController()
            {
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
                m_View.frameItemEvent += frameItemEvent;

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
                        headerTextAlignment = TextAlignment.Left,
                        sortedAscending = false,
                        sortingArrowAlignment = TextAlignment.Right,
                        width = 60, minWidth = 60,
                        autoResize = false, allowToggleVisibility = true
                    },
                    new MultiColumnHeaderState.Column
                    {
                        headerContent = Styles.gcAllocLabel,
                        headerTextAlignment = TextAlignment.Left,
                        sortedAscending = false,
                        sortingArrowAlignment = TextAlignment.Right,
                        width = 60, minWidth = 60,
                        autoResize = false, allowToggleVisibility = true
                    },
                    new MultiColumnHeaderState.Column
                    {
                        headerContent = (m_Type == CallsTreeView.Type.Callers ? Styles.timeMsCallersLabel : Styles.timeMsCalleesLabel),
                        headerTextAlignment = TextAlignment.Left,
                        sortedAscending = false,
                        sortingArrowAlignment = TextAlignment.Right,
                        width = 60, minWidth = 60,
                        autoResize = false, allowToggleVisibility = true
                    },
                    new MultiColumnHeaderState.Column
                    {
                        headerContent = (m_Type == CallsTreeView.Type.Callers ? Styles.timePctCallersLabel : Styles.timePctCalleesLabel),
                        headerTextAlignment = TextAlignment.Left,
                        sortedAscending = false,
                        sortingArrowAlignment = TextAlignment.Right,
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

            public void SetType(CallsTreeView.Type type)
            {
                m_Type = type;
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
            }
        }

        public ProfilerDetailedCallsView()
        {
        }

        void InitIfNeeded()
        {
            if (m_Initialized)
                return;

            if (m_VertSplit == null || m_VertSplit.relativeSizes == null || m_VertSplit.relativeSizes.Length == 0)
                m_VertSplit = new SplitterState(new[] { 40f, 60f }, new[] { 50, 50 }, null);

            if (m_CalleesTreeView == null)
                m_CalleesTreeView = new CallsTreeViewController();
            m_CalleesTreeView.SetType(CallsTreeView.Type.Callees);
            m_CalleesTreeView.frameItemEvent += frameItemEvent;

            if (m_CallersTreeView == null)
                m_CallersTreeView = new CallsTreeViewController();
            m_CallersTreeView.SetType(CallsTreeView.Type.Callers);
            m_CallersTreeView.frameItemEvent += frameItemEvent;

            m_Initialized = true;
        }

        public void DoGUI(GUIStyle headerStyle, FrameDataView frameDataView, IList<int> selection)
        {
            if (frameDataView == null || !frameDataView.IsValid() || selection.Count == 0)
            {
                DrawEmptyPane(headerStyle);
                return;
            }

            var selectedId = selection[0];

            InitIfNeeded();
            UpdateIfNeeded(frameDataView, selectedId);

            GUILayout.Label(m_TotalSelectedPropertyTimeLabel, EditorStyles.label);
            SplitterGUILayout.BeginVerticalSplit(m_VertSplit, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            // Callees
            var rect = EditorGUILayout.BeginHorizontal();
            m_CalleesTreeView.OnGUI(rect);
            EditorGUILayout.EndHorizontal();

            // Callers
            rect = EditorGUILayout.BeginHorizontal();
            m_CallersTreeView.OnGUI(rect);
            EditorGUILayout.EndHorizontal();

            SplitterGUILayout.EndVerticalSplit();
        }

        void UpdateIfNeeded(FrameDataView frameDataView, int selectedId)
        {
            var needReload = m_SelectedID != selectedId || !Equals(m_FrameDataView, frameDataView);
            if (!needReload)
                return;

            m_FrameDataView = frameDataView;
            m_SelectedID = selectedId;

            m_TotalSelectedPropertyTime = 0;

            var selectedMarkerId = m_FrameDataView.GetItemMarkerID(m_SelectedID);

            var callers = new Dictionary<int, CallInformation>();
            var callees = new Dictionary<int, CallInformation>();

            var childrenIds = new List<int>(256);
            var stack = new Stack<int>();
            stack.Push(m_FrameDataView.GetRootItemID());

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (!m_FrameDataView.HasItemChildren(current))
                    continue;

                var markerId = m_FrameDataView.GetItemMarkerID(current);
                m_FrameDataView.GetItemChildren(current, childrenIds);
                foreach (var childId in childrenIds)
                {
                    var childMarkerId = m_FrameDataView.GetItemMarkerID(childId);
                    if (childMarkerId == selectedMarkerId)
                    {
                        var totalSelfTime = m_FrameDataView.GetItemColumnDataAsSingle(childId, ProfilerColumn.TotalTime);
                        m_TotalSelectedPropertyTime += totalSelfTime;

                        if (current != 0)
                        {
                            // Add markerId to callers (except root)
                            CallInformation callInfo;
                            var totalTime = m_FrameDataView.GetItemColumnDataAsSingle(current, ProfilerColumn.TotalTime);
                            var calls = (int)m_FrameDataView.GetItemColumnDataAsSingle(current, ProfilerColumn.Calls);
                            var gcAlloc = (int)m_FrameDataView.GetItemColumnDataAsSingle(current, ProfilerColumn.GCMemory);
                            if (!callers.TryGetValue(markerId, out callInfo))
                            {
                                callers.Add(markerId, new CallInformation()
                                {
                                    id = current,
                                    name = m_FrameDataView.GetItemFunctionName(current),
                                    callsCount = calls,
                                    gcAllocBytes = gcAlloc,
                                    totalCallTimeMs = totalTime,
                                    totalSelfTimeMs = totalSelfTime
                                });
                            }
                            else
                            {
                                callInfo.callsCount += calls;
                                callInfo.gcAllocBytes += gcAlloc;
                                callInfo.totalCallTimeMs += totalTime;
                                callInfo.totalSelfTimeMs += totalSelfTime;
                            }
                        }
                    }

                    if (markerId == selectedMarkerId)
                    {
                        // Add childMarkerId to callees
                        CallInformation callInfo;
                        var totalTime = m_FrameDataView.GetItemColumnDataAsSingle(childId, ProfilerColumn.TotalTime);
                        var calls = (int)m_FrameDataView.GetItemColumnDataAsSingle(childId, ProfilerColumn.Calls);
                        var gcAlloc = (int)m_FrameDataView.GetItemColumnDataAsSingle(childId, ProfilerColumn.GCMemory);
                        if (!callees.TryGetValue(childMarkerId, out callInfo))
                        {
                            callees.Add(childMarkerId, new CallInformation()
                            {
                                id = childId,
                                name = m_FrameDataView.GetItemFunctionName(childId),
                                callsCount = calls,
                                gcAllocBytes = gcAlloc,
                                totalCallTimeMs = totalTime,
                                totalSelfTimeMs = 0
                            });
                        }
                        else
                        {
                            callInfo.callsCount += calls;
                            callInfo.gcAllocBytes += gcAlloc;
                            callInfo.totalCallTimeMs += totalTime;
                        }
                    }

                    stack.Push(childId);
                }
            }

            m_CallersTreeView.SetCallsData(new CallsData() { calls = callers.Values.ToList(), totalSelectedPropertyTime = m_TotalSelectedPropertyTime });
            m_CalleesTreeView.SetCallsData(new CallsData() { calls = callees.Values.ToList(), totalSelectedPropertyTime = m_TotalSelectedPropertyTime });

            m_TotalSelectedPropertyTimeLabel.text = m_FrameDataView.GetItemFunctionName(selectedId) + string.Format(" - Total time: {0:f2} ms", m_TotalSelectedPropertyTime);
        }

        public void Clear()
        {
            if (m_CalleesTreeView != null)
                m_CallersTreeView.SetCallsData(new CallsData() { calls = null, totalSelectedPropertyTime = 0 });
            if (m_CalleesTreeView != null)
                m_CalleesTreeView.SetCallsData(new CallsData() { calls = null, totalSelectedPropertyTime = 0 });
        }
    }
}
