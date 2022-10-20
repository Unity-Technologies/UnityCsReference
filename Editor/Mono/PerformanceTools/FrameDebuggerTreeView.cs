// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Globalization;
using UnityEditor.IMGUI.Controls;
using Object = UnityEngine.Object;

namespace UnityEditorInternal.FrameDebuggerInternal
{
    internal class FrameDebuggerTreeView
    {
        internal readonly TreeViewController m_TreeView;
        internal FrameDebuggerTreeViewDataSource m_DataSource;
        private readonly FrameDebuggerWindow m_FrameDebugger;

        public FrameDebuggerTreeView(FrameDebuggerEvent[] frameEvents, TreeViewState treeViewState, FrameDebuggerWindow window, Rect startRect)
        {
            m_FrameDebugger = window;
            m_TreeView = new TreeViewController(window, treeViewState);
            m_DataSource = new FrameDebuggerTreeViewDataSource(m_TreeView, frameEvents);
            var gui = new FrameDebuggerTreeViewGUI(m_TreeView);
            m_TreeView.Init(startRect, m_DataSource, gui, null);
            m_TreeView.ReloadData();
            m_TreeView.selectionChangedCallback += SelectionChanged;
            m_TreeView.itemSingleClickedCallback += ItemSingleClicked;
            m_TreeView.itemDoubleClickedCallback += PingFrameEventObject;
        }

        void ItemSingleClicked(int selectedID)
        {
            if (Event.current.type == EventType.MouseDown && EditorGUI.actionKey)
                PingFrameEventObject(selectedID);
        }

        void SelectionChanged(int[] selectedIDs)
        {
            if (selectedIDs.Length < 1)
                return;

            int id = selectedIDs[0];
            int eventIndex = id;
            FrameDebuggerTreeViewItem originalItem = null;

            // For tree hierarchy nodes, their IDs are not the frame event indices;
            // fetch the ID from the node itself in that case.
            // IDs for hierarchy nodes are negative and need to stay consistently ordered so
            // that tree expanded state behaves well when something in the scene changes.
            //
            // When selecting a hierarchy node, we want it's last child event to be set as the limit,
            // so that rendered state corresponds to "everything up to and including this whole sub-tree".
            if (eventIndex <= 0)
            {
                originalItem = m_TreeView.FindItem(id) as FrameDebuggerTreeViewItem;
                FrameDebuggerTreeViewItem item = m_TreeView.FindItem(id) as FrameDebuggerTreeViewItem;
                if (item != null)
                    eventIndex = item.m_EventIndex;

                // If still has no valid ID, do nothing.
                if (eventIndex <= 0)
                    return;
            }

            m_FrameDebugger.ChangeFrameEventLimit(eventIndex, originalItem);
        }

        private void PingFrameEventObject(int selectedID)
        {
            Object obj = FrameDebuggerUtility.GetFrameEventObject(selectedID - 1);
            if (obj != null)
                EditorGUIUtility.PingObject(obj);
            m_FrameDebugger.DrawSearchField(string.Empty);
        }

        public void ReselectFrameEventIndex()
        {
            int[] selection = m_TreeView.GetSelection();
            if (selection.Length > 0)
            {
                FrameDebuggerTreeViewItem item = m_TreeView.FindItem(selection[0]) as FrameDebuggerTreeViewItem;
                if (item != null)
                    m_TreeView.SetSelection(new[] { item.m_EventIndex }, true);
            }
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
                FrameDebuggerTreeViewItem item = m_TreeView.FindItem(selection[0]) as FrameDebuggerTreeViewItem;
                if (item != null && eventIndex == item.m_EventIndex)
                    return;
            }
            m_TreeView.SetSelection(new[] { eventIndex }, true);
        }

        public void DrawTree(Rect rect)
        {
            int keyboardControlID = GUIUtility.GetControlID(FocusType.Keyboard);
            m_TreeView.OnGUI(rect, keyboardControlID);
        }

        // Item for TreeView
        // ID is different for leaf nodes (actual frame events) vs hierarchy nodes (parent profiler nodes):
        // - leaf node IDs are frame event indices (always > 0).
        // - hierarchy node IDs are always negative; to get frame event index we want we need to lookup the node and get it from m_EventIndex.
        public class FrameDebuggerTreeViewItem : TreeViewItem
        {
            public FrameDebuggerEvent m_FrameEvent;
            public int m_ChildEventCount;
            public int m_EventIndex;

            public FrameDebuggerTreeViewItem(int id, int depth, FrameDebuggerTreeViewItem parent, string displayName)
                : base(id, depth, parent, displayName)
            {
                m_EventIndex = id;
            }
        }

        // GUI for TreeView
        private class FrameDebuggerTreeViewGUI : TreeViewGUI
        {
            private const float kSmallMargin = 4;

            public FrameDebuggerTreeViewGUI(TreeViewController treeView)
                : base(treeView)
            {
            }

            protected override Texture GetIconForItem(TreeViewItem item)
            {
                return null;
            }

            private int childCounter = 0;

            protected override void OnContentGUI(Rect rect, int row, TreeViewItem itemRaw, string label, bool selected, bool focused, bool useBoldFont, bool isPinging)
            {
                if (Event.current.type != EventType.Repaint)
                    return;

                FrameDebuggerTreeViewItem item = (FrameDebuggerTreeViewItem)itemRaw;
                string text;
                GUIContent tempContent;
                GUIStyle style;

                bool isParent = (item.hasChildren);
                FontStyle fontStyle = (isParent) ? FontStyle.Bold : FontStyle.Normal;
                childCounter = (isParent) ? 1 : (childCounter + 1);

                // Draw background
                style = FrameDebuggerStyles.Tree.s_RowText;
                tempContent = EditorGUIUtility.TempContent("");
                style.Draw(rect, tempContent, false, false, false, false);

                // indent
                float indent = GetContentIndent(item);
                rect.x += indent;
                rect.width -= indent;

                // child event count
                if (isParent)
                {
                    text = item.m_ChildEventCount.ToString(CultureInfo.InvariantCulture);
                    tempContent = EditorGUIUtility.TempContent(text);

                    style = FrameDebuggerStyles.Tree.s_RowTextRight;
                    style.fontStyle = fontStyle;

                    Rect r = rect;
                    r.width -= kSmallMargin;
                    style.Draw(r, tempContent, false, false, false, false);

                    // reduce width of available space for the name, so that it does not overlap event count
                    rect.width -= style.CalcSize(tempContent).x + kSmallMargin * 2;
                }

                style = FrameDebuggerStyles.Tree.s_RowText;
                style.fontStyle = fontStyle;

                // draw event name
                text = item.displayName;

                if (string.IsNullOrEmpty(text))
                    text = FrameDebuggerStyles.Tree.k_UnknownScopeString;

                tempContent = EditorGUIUtility.TempContent(text);
                style.Draw(rect, tempContent, false, false, false, selected && focused);
            }

            protected override void RenameEnded()
            {
            }
        }

        // Data source for TreeView

        internal class FrameDebuggerTreeViewDataSource : TreeViewDataSource
        {
            private FrameDebuggerEvent[] m_FrameEvents;

            public override bool IsRenamingItemAllowed(TreeViewItem item) => false;
            public override bool CanBeMultiSelected(TreeViewItem item) => false;

            public FrameDebuggerTreeViewDataSource(TreeViewController treeView, FrameDebuggerEvent[] frameEvents) : base(treeView)
            {
                m_FrameEvents = frameEvents;
                rootIsCollapsable = false;
                showRootItem = false;
            }

            public void SetEvents(FrameDebuggerEvent[] frameEvents)
            {
                bool wasEmpty = m_FrameEvents == null || m_FrameEvents.Length < 1;

                m_FrameEvents = frameEvents;
                m_NeedRefreshRows = true;
                ReloadData();

                // Only expand whole events tree if it was empty before.
                // If we already had something in there, we want to preserve user's expanded items.
                if (wasEmpty)
                    SetExpandedWithChildren(m_RootItem, true);
            }

            // Used while building the tree data source; represents current tree hierarchy level
            private class FrameDebuggerTreeHierarchyLevel
            {
                internal readonly FrameDebuggerTreeViewItem item;
                internal readonly List<TreeViewItem> children;
                internal FrameDebuggerTreeHierarchyLevel(int depth, int id, string name, FrameDebuggerTreeViewItem parent)
                {
                    item = new FrameDebuggerTreeViewItem(id, depth, parent, name);
                    children = new List<TreeViewItem>();
                }
            }

            private static void CloseLastHierarchyLevel(List<FrameDebuggerTreeHierarchyLevel> eventStack, int prevFrameEventIndex)
            {
                var idx = eventStack.Count - 1;
                eventStack[idx].item.children = eventStack[idx].children;
                eventStack[idx].item.m_EventIndex = prevFrameEventIndex;

                if (eventStack[idx].item.parent != null)
                    ((FrameDebuggerTreeViewItem)eventStack[idx].item.parent).m_ChildEventCount += eventStack[idx].item.m_ChildEventCount;

                eventStack.RemoveAt(idx);
            }

            public override void FetchData()
            {
                FrameDebuggerTreeHierarchyLevel rootLevel = new FrameDebuggerTreeHierarchyLevel(0, 0, string.Empty, null);

                // Hierarchy levels of a tree being built
                List<FrameDebuggerTreeHierarchyLevel> eventStack = new List<FrameDebuggerTreeHierarchyLevel>();
                eventStack.Add(rootLevel);

                int hierarchyIDCounter = -1;
                for (int i = 0; i < m_FrameEvents.Length; ++i)
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
                        CloseLastHierarchyLevel(eventStack, i);

                    if (FrameDebuggerHelper.IsAHierarchyLevelBreakEvent(m_FrameEvents[i].m_Type))
                        continue;

                    // add all further levels for current event
                    for (int j = level; j < names.Length; ++j)
                    {
                        FrameDebuggerTreeHierarchyLevel parent = eventStack[eventStack.Count - 1];
                        FrameDebuggerTreeHierarchyLevel newLevel = new FrameDebuggerTreeHierarchyLevel(eventStack.Count - 1, --hierarchyIDCounter, names[j], parent.item);
                        parent.children.Add(newLevel.item);
                        eventStack.Add(newLevel);
                    }

                    if (FrameDebuggerHelper.IsAHiddenEvent(m_FrameEvents[i].m_Type))
                        continue;

                    // add leaf event to current level
                    Object eventObj = FrameDebuggerUtility.GetFrameEventObject(i);
                    string displayName = FrameDebuggerStyles.s_FrameEventTypeNames[(int)m_FrameEvents[i].m_Type];

                    if (eventObj)
                        displayName += " " + eventObj.name;

                    FrameDebuggerTreeHierarchyLevel parentEvent = eventStack[eventStack.Count - 1];

                    int leafEventID = i + 1;
                    FrameDebuggerTreeViewItem item = new FrameDebuggerTreeViewItem(leafEventID, eventStack.Count - 1, parentEvent.item, displayName);

                    item.m_FrameEvent = m_FrameEvents[i];
                    parentEvent.children.Add(item);
                    ++parentEvent.item.m_ChildEventCount;
                }

                while (eventStack.Count > 0)
                    CloseLastHierarchyLevel(eventStack, m_FrameEvents.Length);

                m_RootItem = rootLevel.item;
            }
        }
    }
}
