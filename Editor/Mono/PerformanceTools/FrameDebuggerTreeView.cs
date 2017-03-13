// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Globalization;
using UnityEditor.IMGUI.Controls;


namespace UnityEditorInternal
{
    internal class FrameDebuggerTreeView
    {
        internal readonly TreeViewController m_TreeView;
        internal FDTreeViewDataSource m_DataSource;
        private readonly FrameDebuggerWindow m_FrameDebugger;

        public FrameDebuggerTreeView(FrameDebuggerEvent[] frameEvents, TreeViewState treeViewState, FrameDebuggerWindow window, Rect startRect)
        {
            m_FrameDebugger = window;
            m_TreeView = new TreeViewController(window, treeViewState);
            m_DataSource = new FDTreeViewDataSource(m_TreeView, frameEvents);
            var gui = new FDTreeViewGUI(m_TreeView);
            m_TreeView.Init(startRect, m_DataSource, gui, null);
            m_TreeView.ReloadData();
            m_TreeView.selectionChangedCallback += SelectionChanged;
        }

        void SelectionChanged(int[] selectedIDs)
        {
            if (selectedIDs.Length < 1)
                return;
            int id = selectedIDs[0];
            int eventIndex = id;

            // For tree hierarchy nodes, their IDs are not the frame event indices;
            // fetch the ID from the node itself in that case.
            // IDs for hierarchy nodes are negative and need to stay consistently ordered so
            // that tree expanded state behaves well when something in the scene changes.
            //
            // When selecting a hierarchy node, we want it's last child event to be set as the limit,
            // so that rendered state corresponds to "everything up to and including this whole sub-tree".
            if (eventIndex <= 0)
            {
                var item = m_TreeView.FindItem(id) as FDTreeViewItem;
                if (item != null)
                    eventIndex = item.m_EventIndex;
            }
            // If still has no valid ID, do nothing.
            if (eventIndex <= 0)
                return;
            m_FrameDebugger.ChangeFrameEventLimit(eventIndex);
        }

        public void SelectFrameEventIndex(int eventIndex)
        {
            // Check if we'd end up selecting same "frame event":
            // different tree nodes could result in the same frame debugger event
            // limit, e.g. a hierarchy node sets last child event as the limit.
            // If the limit event is the same, then do not change the currently selected item.
            int[] selection = m_TreeView.GetSelection();
            if (selection.Length > 0)
            {
                var item = m_TreeView.FindItem(selection[0]) as FDTreeViewItem;
                if (item != null && eventIndex == item.m_EventIndex)
                    return;
            }

            m_TreeView.SetSelection(new[] { eventIndex }, true);
        }

        public void OnGUI(Rect rect)
        {
            var keyboardControlID = GUIUtility.GetControlID(FocusType.Keyboard);
            m_TreeView.OnGUI(rect, keyboardControlID);
        }

        // Item for TreeView
        // ID is different for leaf nodes (actual frame events) vs hierarchy nodes (parent profiler nodes):
        // - leaf node IDs are frame event indices (always > 0).
        // - hierarchy node IDs are always negative; to get frame event index we want we need to lookup the node and get it from m_EventIndex.
        private class FDTreeViewItem : TreeViewItem
        {
            public FrameDebuggerEvent m_FrameEvent;
            public int m_ChildEventCount;
            public int m_EventIndex;
            public FDTreeViewItem(int id, int depth, FDTreeViewItem parent, string displayName)
                : base(id, depth, parent, displayName)
            {
                m_EventIndex = id;
            }
        }

        // GUI for TreeView

        private class FDTreeViewGUI : TreeViewGUI
        {
            const float kSmallMargin = 4;

            public FDTreeViewGUI(TreeViewController treeView)
                : base(treeView)
            {
            }

            protected override Texture GetIconForItem(TreeViewItem item)
            {
                return null;
            }

            protected override void OnContentGUI(Rect rect, int row, TreeViewItem itemRaw, string label, bool selected, bool focused, bool useBoldFont, bool isPinging)
            {
                if (Event.current.type != EventType.Repaint)
                    return;

                var item = (FDTreeViewItem)itemRaw;

                // indent
                float indent = GetContentIndent(item);
                rect.x += indent;
                rect.width -= indent;

                string text;
                GUIContent gc;
                GUIStyle style;

                // child event count
                if (item.m_ChildEventCount > 0)
                {
                    Rect r = rect;
                    r.width -= kSmallMargin;
                    text = item.m_ChildEventCount.ToString(CultureInfo.InvariantCulture);
                    gc = EditorGUIUtility.TempContent(text);
                    style = FrameDebuggerWindow.styles.rowTextRight;
                    style.Draw(r, gc, false, false, false, false);
                    // reduce width of available space for the name, so that it does not overlap event count
                    rect.width -= style.CalcSize(gc).x + kSmallMargin * 2;
                }

                // draw event name
                if (item.id <= 0)
                    text = item.displayName; // hierarchy item
                else
                    text = FrameDebuggerWindow.s_FrameEventTypeNames[(int)item.m_FrameEvent.type] + item.displayName; // leaf event
                if (string.IsNullOrEmpty(text))
                    text = "<unknown scope>";
                gc = EditorGUIUtility.TempContent(text);
                style = FrameDebuggerWindow.styles.rowText;
                style.Draw(rect, gc, false, false, false, selected && focused);
            }

            protected override void RenameEnded()
            {
            }
        }

        // Data source for TreeView

        internal class FDTreeViewDataSource : TreeViewDataSource
        {
            private FrameDebuggerEvent[] m_FrameEvents;

            public FDTreeViewDataSource(TreeViewController treeView, FrameDebuggerEvent[] frameEvents)
                : base(treeView)
            {
                m_FrameEvents = frameEvents;
                rootIsCollapsable = false;
                showRootItem = false;
            }

            public void SetEvents(FrameDebuggerEvent[] frameEvents)
            {
                var wasEmpty = m_FrameEvents == null || m_FrameEvents.Length < 1;

                m_FrameEvents = frameEvents;
                m_NeedRefreshRows = true;
                ReloadData();

                // Only expand whole events tree if it was empty before.
                // If we already had something in there, we want to preserve user's expanded items.
                if (wasEmpty)
                    SetExpandedWithChildren(m_RootItem, true);
            }

            public override bool IsRenamingItemAllowed(TreeViewItem item)
            {
                return false;
            }

            public override bool CanBeMultiSelected(TreeViewItem item)
            {
                return false;
            }

            // Used while building the tree data source; represents current tree hierarchy level
            private class FDTreeHierarchyLevel
            {
                internal readonly FDTreeViewItem item;
                internal readonly List<TreeViewItem> children;
                internal FDTreeHierarchyLevel(int depth, int id, string name, FDTreeViewItem parent)
                {
                    item = new FDTreeViewItem(id, depth, parent, name);
                    children = new List<TreeViewItem>();
                }
            }
            private static void CloseLastHierarchyLevel(List<FDTreeHierarchyLevel> eventStack, int prevFrameEventIndex)
            {
                var idx = eventStack.Count - 1;
                eventStack[idx].item.children = eventStack[idx].children;
                eventStack[idx].item.m_EventIndex = prevFrameEventIndex;
                if (eventStack[idx].item.parent != null)
                    ((FDTreeViewItem)eventStack[idx].item.parent).m_ChildEventCount += eventStack[idx].item.m_ChildEventCount;
                eventStack.RemoveAt(idx);
            }

            public override void FetchData()
            {
                var rootLevel = new FDTreeHierarchyLevel(0, 0, string.Empty, null);

                // Hierarchy levels of a tree being built
                var eventStack = new List<FDTreeHierarchyLevel>();
                eventStack.Add(rootLevel);

                int hierarchyIDCounter = -1;
                for (var i = 0; i < m_FrameEvents.Length; ++i)
                {
                    // This will be a slash-delimited string, e.g. Foo/Bar/Baz.
                    // Add "/" in front to account for the single (invisible) root item
                    // that the TreeView always has.
                    string context = "/" + (FrameDebuggerUtility.GetFrameEventInfoName(i) ?? string.Empty);
                    string[] names = context.Split('/');
                    // find matching hierarchy level
                    int level = 0;
                    while (level < eventStack.Count && level < names.Length)
                    {
                        if (names[level] != eventStack[level].item.displayName)
                            break;
                        ++level;
                    }
                    // close all the further levels from previous events in the stack
                    while (eventStack.Count > 0 && eventStack.Count > level)
                    {
                        CloseLastHierarchyLevel(eventStack, i);
                    }
                    // add all further levels for current event
                    for (var j = level; j < names.Length; ++j)
                    {
                        var parent = eventStack[eventStack.Count - 1];
                        var newLevel = new FDTreeHierarchyLevel(eventStack.Count - 1, --hierarchyIDCounter, names[j], parent.item);
                        parent.children.Add(newLevel.item);
                        eventStack.Add(newLevel);
                    }
                    // add leaf event to current level
                    var eventGo = FrameDebuggerUtility.GetFrameEventGameObject(i);
                    var displayName = eventGo ? " " + eventGo.name : string.Empty;
                    FDTreeHierarchyLevel parentEvent = eventStack[eventStack.Count - 1];
                    var leafEventID = i + 1;
                    var item = new FDTreeViewItem(leafEventID, eventStack.Count - 1, parentEvent.item, displayName);
                    item.m_FrameEvent = m_FrameEvents[i];
                    parentEvent.children.Add(item);
                    ++parentEvent.item.m_ChildEventCount;
                }
                while (eventStack.Count > 0)
                {
                    CloseLastHierarchyLevel(eventStack, m_FrameEvents.Length);
                }
                m_RootItem = rootLevel.item;
            }
        }
    }
}
