// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEditorInternal;


namespace UnityEditor.IMGUI.Controls
{
    /*
     Description:

     The TreeViewController requires implementations from the following three interfaces:
        ITreeViewDataSource:    Should handle data fetching and data structure
        ITreeViewGUI:           Should handle visual representation of TreeView and mouse input on row controls
        ITreeViewDragging:      Should handle dragging, temp expansion of items, allow/disallow dropping
        The TreeViewController handles: Navigation, Item selection and initiates dragging

     Important concepts:
     1) The Item Tree: the DataSource should create the tree structure of items with parent and children references
     2) Rows: the DataSource should be able to provide the visible items; a simple list that will become the rows we render.
     3) The root item might not be visible; its up to the data source to deliver a set of visible items from the tree

     */


    [System.Serializable]
    public class TreeViewState
    {
        public List<int> selectedIDs { get { return m_SelectedIDs; } set { m_SelectedIDs = value; } }
        public int lastClickedID { get { return m_LastClickedID; } set { m_LastClickedID = value; } }
        public List<int> expandedIDs { get { return m_ExpandedIDs; } set { m_ExpandedIDs = value; } }
        internal RenameOverlay renameOverlay { get { return m_RenameOverlay; } set { m_RenameOverlay = value; } }

        public string searchString { get { return m_SearchString; } set { m_SearchString = value; } }
        public Vector2 scrollPos;

        // Selection state
        [SerializeField] private List<int> m_SelectedIDs = new List<int>();
        [SerializeField] private int m_LastClickedID; // used for navigation

        // Expanded state (assumed sorted)
        [SerializeField] private List<int> m_ExpandedIDs = new List<int>();

        // Rename and create asset state
        [SerializeField] private RenameOverlay m_RenameOverlay = new RenameOverlay();

        // Search state (can be used by Datasource to filter tree when reloading)
        [SerializeField] private string m_SearchString;

        internal virtual void OnAwake()
        {
            // Clear state that should not survive closing/starting Unity (If TreeViewState is in EditorWindow that are serialized in a layout file)
            m_RenameOverlay.Clear();
        }
    }

    internal class TreeViewController
    {
        public System.Action<int[]> selectionChangedCallback { get; set; } // ids
        public System.Action<int> itemDoubleClickedCallback { get; set; } // id
        public System.Action<int[], bool> dragEndedCallback { get; set; } // dragged ids, if null then drag was not allowed, bool == true if dragging tree view items from own treeview, false if drag was started outside
        public System.Action<int> contextClickItemCallback { get; set; } // clicked item id
        public System.Action contextClickOutsideItemsCallback { get; set; }
        public System.Action keyboardInputCallback { get; set; }
        public System.Action expandedStateChanged { get; set; }
        public System.Action<string> searchChanged { get; set; }
        public System.Action<Vector2> scrollChanged { get; set; }
        public System.Action<int, Rect> onGUIRowCallback { get; set; }  // <id, Rect of row>

        // Main state
        GUIView m_GUIView;                                              // Containing view for this tree: used for checking if we have focus and for requesting repaints
        public ITreeViewDataSource data { get; set; }                   // Data provider for this tree: handles data fetching
        public ITreeViewDragging dragging { get; set; }                 // Handle dragging
        public ITreeViewGUI gui { get; set; }                           // Handles GUI (input and rendering)
        public TreeViewState state { get; set; }                        // State that persists script reloads
        public GUIStyle horizontalScrollbarStyle { get; set; }
        public GUIStyle verticalScrollbarStyle { get; set; }
        public TreeViewItemExpansionAnimator expansionAnimator { get { return m_ExpansionAnimator; } }
        readonly TreeViewItemExpansionAnimator m_ExpansionAnimator = new TreeViewItemExpansionAnimator();
        AnimFloat m_FramingAnimFloat;

        bool m_StopIteratingItems;

        public bool deselectOnUnhandledMouseDown { get; set; }

        List<int> m_DragSelection = new List<int>();                    // Temp id state while dragging (not serialized)
        bool m_UseScrollView = true;                                    // Internal scrollview can be omitted when e.g mulitple tree views in one scrollview is wanted
        bool m_AllowRenameOnMouseUp = true;

        internal const string kExpansionAnimationPrefKey = "TreeViewExpansionAnimation";
        bool m_UseExpansionAnimation = EditorPrefs.GetBool(kExpansionAnimationPrefKey, true);
        public bool useExpansionAnimation { get { return m_UseExpansionAnimation; } set { m_UseExpansionAnimation = value; } }

        // Cached values during one event (for convenience)
        bool m_GrabKeyboardFocus;
        Rect m_TotalRect;
        Rect m_VisibleRect;
        Rect m_ContentRect;
        bool m_HadFocusLastEvent;                           // Cached from last event for keyboard focus changed event
        int m_KeyboardControlID;

        const float kSpaceForScrollBar = 16f;

        public TreeViewController(EditorWindow editorWindow, TreeViewState treeViewState)
        {
            m_GUIView = editorWindow ? editorWindow.m_Parent : GUIView.current;
            state = treeViewState;
        }

        public void Init(Rect rect, ITreeViewDataSource data, ITreeViewGUI gui, ITreeViewDragging dragging)
        {
            this.data = data;
            this.gui = gui;
            this.dragging = dragging;
            m_TotalRect = rect; // We initialize the total rect because it might be needed for framing selection when reloading data the first time.

            // Allow sub systems to set up delegates etc after treeview references have been setup
            data.OnInitialize();
            gui.OnInitialize();
            if (dragging != null)
                dragging.OnInitialize();

            expandedStateChanged += ExpandedStateHasChanged;

            m_FramingAnimFloat = new AnimFloat(state.scrollPos.y, AnimatedScrollChanged);
        }

        void ExpandedStateHasChanged()
        {
            m_StopIteratingItems = true;
        }

        public bool isSearching
        {
            get { return !string.IsNullOrEmpty(state.searchString); }
        }

        public bool isDragging
        {
            get { return m_DragSelection != null && m_DragSelection.Count > 0; }
        }

        public bool showingVerticalScrollBar
        {
            get { return m_ContentRect.height > m_VisibleRect.height; }
        }

        public bool showingHorizontalScrollBar
        {
            get { return m_ContentRect.width > m_VisibleRect.width; }
        }

        public string searchString
        {
            get
            {
                return state.searchString;
            }
            set
            {
                if (string.ReferenceEquals(state.searchString, value))
                    return;

                if (state.searchString == value)
                    return;

                state.searchString = value;
                data.OnSearchChanged();
                if (searchChanged != null)
                    searchChanged(state.searchString);
            }
        }

        public bool IsSelected(int id)
        {
            return state.selectedIDs.Contains(id);
        }

        public bool HasSelection()
        {
            return state.selectedIDs.Count() > 0;
        }

        public int[] GetSelection()
        {
            return state.selectedIDs.ToArray();
        }

        public int[] GetRowIDs()
        {
            return (from item in data.GetRows() select item.id).ToArray();
        }

        public void SetSelection(int[] selectedIDs, bool revealSelectionAndFrameLastSelected)
        {
            const bool animatedFraming = false;
            SetSelection(selectedIDs, revealSelectionAndFrameLastSelected, animatedFraming);
        }

        public void SetSelection(int[] selectedIDs, bool revealSelectionAndFrameLastSelected, bool animatedFraming)
        {
            // Keep for debugging
            //Debug.Log ("SetSelection: new selection: " + DebugUtils.ListToString(new List<int>(selectedIDs)));

            // Init new state
            if (selectedIDs.Length > 0)
            {
                if (revealSelectionAndFrameLastSelected)
                {
                    foreach (int id in selectedIDs)
                        data.RevealItem(id);
                }

                state.selectedIDs = new List<int>(selectedIDs);

                // Ensure that our key navigation is setup
                bool hasLastClicked = state.selectedIDs.IndexOf(state.lastClickedID) >= 0;
                if (!hasLastClicked)
                {
                    // See if we can find a valid id, we check the last selected (selectedids might contain invalid ids e.g scene objects in project browser and vice versa)
                    int lastSelectedID = selectedIDs.Last();
                    if (data.GetRow(lastSelectedID) != -1)
                    {
                        state.lastClickedID = lastSelectedID;
                        hasLastClicked = true;
                    }
                    else
                        state.lastClickedID = 0;
                }

                if (revealSelectionAndFrameLastSelected && hasLastClicked)
                    Frame(state.lastClickedID, true, false, animatedFraming);
            }
            else
            {
                state.selectedIDs.Clear();
                state.lastClickedID = 0;
            }

            // Should not fire callback since this is called from outside
            // NotifyListenersThatSelectionChanged ()
        }

        public TreeViewItem FindItem(int id)
        {
            return data.FindItem(id);
        }

        public void SetUseScrollView(bool useScrollView)
        {
            m_UseScrollView = useScrollView;
        }

        public void Repaint()
        {
            if (m_GUIView != null)
                m_GUIView.Repaint();
        }

        public void ReloadData()
        {
            // Do not clear rename data here, we could be reloading due to assembly reload
            // and we want to let our rename session survive that

            data.ReloadData();
            Repaint();

            m_StopIteratingItems = true;
        }

        public bool HasFocus()
        {
            bool hasKeyFocus = (m_GUIView != null) ? m_GUIView.hasFocus : EditorGUIUtility.HasCurrentWindowKeyFocus();
            return hasKeyFocus && (GUIUtility.keyboardControl == m_KeyboardControlID);
        }

        static internal int GetItemControlID(TreeViewItem item)
        {
            return ((item != null) ? item.id : 0) + 10000000;
        }

        public void HandleUnusedMouseEventsForItem(Rect rect, TreeViewItem item, int row)
        {
            int itemControlID = GetItemControlID(item);

            Event evt = Event.current;

            switch (evt.GetTypeForControl(itemControlID))
            {
                case EventType.MouseDown:
                    if (rect.Contains(Event.current.mousePosition))
                    {
                        // Handle mouse down on entire line
                        if (Event.current.button == 0)
                        {
                            // Grab keyboard
                            GUIUtility.keyboardControl = m_KeyboardControlID;
                            Repaint(); // Ensure repaint so we can show we have keyboard focus

                            // Let client handle double click
                            if (Event.current.clickCount == 2)
                            {
                                if (itemDoubleClickedCallback != null)
                                    itemDoubleClickedCallback(item.id);
                            }
                            else
                            {
                                var dragSelection =  GetNewSelection(item, true, false);
                                bool canStartDrag = dragging != null && dragSelection.Count != 0 && dragging.CanStartDrag(item, dragSelection, Event.current.mousePosition);
                                if (canStartDrag)
                                {
                                    // Prepare drag and drop delay (we start the drag after a couple of pixels mouse drag: See the case MouseDrag below)
                                    m_DragSelection = dragSelection;
                                    DragAndDropDelay delay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), itemControlID);
                                    delay.mouseDownPosition = Event.current.mousePosition;
                                }
                                else
                                {
                                    // If dragging is not supported or not allowed for the drag selection then handle selection on mouse down
                                    // (when dragging is handled we handle selection on mouse up to e.g allow to drag to object fields in the inspector)
                                    m_DragSelection.Clear();
                                    if (m_AllowRenameOnMouseUp)
                                        m_AllowRenameOnMouseUp = (state.selectedIDs.Count == 1 && state.selectedIDs[0] == item.id); // If first time selection then prevent starting a rename on the following mouse up after this mouse down
                                    SelectionClick(item, false);
                                }

                                GUIUtility.hotControl = itemControlID;
                            }
                            evt.Use();
                        }
                        else if (Event.current.button == 1)
                        {
                            // Right mouse down selects;
                            bool keepMultiSelection = true;
                            SelectionClick(item, keepMultiSelection);
                        }
                    }
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == itemControlID && dragging != null && m_DragSelection.Count > 0)
                    {
                        DragAndDropDelay delay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), itemControlID);
                        if (delay.CanStartDrag() && dragging.CanStartDrag(item, m_DragSelection, delay.mouseDownPosition))
                        {
                            dragging.StartDrag(item, m_DragSelection);
                            GUIUtility.hotControl = 0;
                        }

                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == itemControlID)
                    {
                        // When having the temp dragging selection delay the the selection until mouse up
                        bool useMouseUpSelection = m_DragSelection.Count > 0;

                        // Clear state before SelectionClick since it can ExitGUI early
                        GUIUtility.hotControl = 0;
                        m_DragSelection.Clear();
                        evt.Use();

                        // On Mouse up either start name editing or change selection (if not done on mouse down)
                        if (rect.Contains(evt.mousePosition))
                        {
                            Rect renameActivationRect = gui.GetRenameRect(rect, row, item);
                            List<int> selected = state.selectedIDs;
                            if (m_AllowRenameOnMouseUp && selected != null && selected.Count == 1 && selected[0] == item.id && renameActivationRect.Contains(evt.mousePosition) && !EditorGUIUtility.HasHolddownKeyModifiers(evt))
                            {
                                BeginNameEditing(0.5f);
                            }
                            else if (useMouseUpSelection)
                            {
                                SelectionClick(item, false);
                            }
                        }
                    }
                    break;

                case EventType.DragUpdated:
                case EventType.DragPerform:
                {
                    //bool firstItem = row == 0;
                    if (dragging != null && dragging.DragElement(item, rect, row))
                        GUIUtility.hotControl = 0;
                }
                break;

                case EventType.ContextClick:
                    if (rect.Contains(evt.mousePosition))
                    {
                        // Do not use the event so the client can react to the context click (here we just handled the treeview selection)
                        if (contextClickItemCallback != null)
                            contextClickItemCallback(item.id);
                    }
                    break;
            }
        }

        public void GrabKeyboardFocus()
        {
            m_GrabKeyboardFocus = true;
        }

        public void NotifyListenersThatSelectionChanged()
        {
            if (selectionChangedCallback != null)
                selectionChangedCallback(state.selectedIDs.ToArray());
        }

        public void NotifyListenersThatDragEnded(int[] draggedIDs, bool draggedItemsFromOwnTreeView)
        {
            if (dragEndedCallback != null)
                dragEndedCallback(draggedIDs, draggedItemsFromOwnTreeView);
        }

        public Vector2 GetContentSize()
        {
            return gui.GetTotalSize();
        }

        public Rect GetTotalRect()
        {
            return m_TotalRect;
        }

        public void SetTotalRect(Rect rect)
        {
            m_TotalRect = rect;
        }

        public bool IsItemDragSelectedOrSelected(TreeViewItem item)
        {
            return m_DragSelection.Count > 0 ? m_DragSelection.Contains(item.id) : state.selectedIDs.Contains(item.id);
        }

        public bool animatingExpansion { get { return m_UseExpansionAnimation && m_ExpansionAnimator.isAnimating; } }

        void DoItemGUI(TreeViewItem item, int row, float rowWidth, bool hasFocus)
        {
            // Check valid row
            if (row < 0 || row >= data.rowCount)
            {
                Debug.LogError("Invalid. Org row: " + (row) + " Num rows " + data.rowCount);
                return;
            }

            bool selected = IsItemDragSelectedOrSelected(item);

            Rect rowRect = gui.GetRowRect(row, rowWidth);

            // 1. Before row GUI
            if (animatingExpansion)
                rowRect = m_ExpansionAnimator.OnBeginRowGUI(row, rowRect);

            // 2. Do row GUI
            if (animatingExpansion)
                m_ExpansionAnimator.OnRowGUI(row);
            gui.OnRowGUI(rowRect, item, row, selected, hasFocus);

            // 3. Draw extra gui callbacks
            if (onGUIRowCallback != null)
            {
                float indent = gui.GetContentIndent(item);
                Rect indentedRect = new Rect(rowRect.x + indent, rowRect.y, rowRect.width - indent, rowRect.height);
                onGUIRowCallback(item.id, indentedRect);
            }

            // 4. After row GUI
            if (animatingExpansion)
                m_ExpansionAnimator.OnEndRowGUI(row);

            HandleUnusedMouseEventsForItem(rowRect, item, row);
        }

        public void OnGUI(Rect rect, int keyboardControlID)
        {
            m_KeyboardControlID = keyboardControlID;

            Event evt = Event.current;
            if (evt.type == EventType.Repaint)
                m_TotalRect = rect;

            m_GUIView = GUIView.current;

            // End rename if the window do not have focus
            if (m_GUIView != null && !m_GUIView.hasFocus && state.renameOverlay.IsRenaming())
            {
                EndNameEditing(true);
            }

            // Grab keyboard focus if requested or if we have a mousedown in entire rect
            if (m_GrabKeyboardFocus || (evt.type == EventType.MouseDown && m_TotalRect.Contains(evt.mousePosition)))
            {
                m_GrabKeyboardFocus = false;
                GUIUtility.keyboardControl = m_KeyboardControlID;
                m_AllowRenameOnMouseUp = true;
                Repaint(); // Ensure repaint so we can show we have keyboard focus
            }

            bool hasFocus = HasFocus();

            // Detect got focus (ignore layout event which might get fired infront of mousedown)
            if (hasFocus != m_HadFocusLastEvent && evt.type != EventType.Layout)
            {
                m_HadFocusLastEvent = hasFocus;

                // We got focus this event
                if (hasFocus)
                {
                    if (evt.type == EventType.MouseDown)
                        m_AllowRenameOnMouseUp = false; // If we got focus by mouse down then we do not want to begin renaming if clicking on an already selected item
                }
            }

            // Might change expanded state so call before InitIfNeeded (delayed collapse until animation is done)
            if (animatingExpansion)
                m_ExpansionAnimator.OnBeforeAllRowsGUI();

            data.InitIfNeeded();

            // Calc content size
            Vector2 contentSize = gui.GetTotalSize();
            m_ContentRect = new Rect(0, 0, contentSize.x, contentSize.y);

            if (m_UseScrollView)
            {
                state.scrollPos = GUI.BeginScrollView(m_TotalRect, state.scrollPos, m_ContentRect,
                        horizontalScrollbarStyle != null ? horizontalScrollbarStyle : GUI.skin.horizontalScrollbar,
                        verticalScrollbarStyle != null ? verticalScrollbarStyle : GUI.skin.verticalScrollbar);
            }
            else
                GUI.BeginClip(m_TotalRect);

            if (evt.type == EventType.Repaint)
                m_VisibleRect = m_UseScrollView ? GUI.GetTopScrollView().visibleRect : m_TotalRect;

            gui.BeginRowGUI();

            // Iterate visible items
            int firstRow, lastRow;
            gui.GetFirstAndLastRowVisible(out firstRow, out lastRow);
            if (lastRow >= 0)
            {
                int numVisibleRows = lastRow - firstRow + 1;
                float rowWidth = Mathf.Max(GUIClip.visibleRect.width, m_ContentRect.width);

                IterateVisibleItems(firstRow, numVisibleRows, rowWidth, hasFocus);
            }

            // Call before gui.EndRowGUI() so stuff we render in EndRowGUI does not end up
            // in the the animation clip rect
            if (animatingExpansion)
                m_ExpansionAnimator.OnAfterAllRowsGUI();

            gui.EndRowGUI();

            if (m_UseScrollView)
                GUI.EndScrollView(showingVerticalScrollBar);
            else
                GUI.EndClip();

            HandleUnusedEvents();
            KeyboardGUI();

            // Prevent controlID inconsistency for the controls following this tree view: We use the hint parameter of GetControlID to
            // fast forward to a fixed entry in the id list so the following controls always start from there regardless of the rows that have been
            // culled.
            GUIUtility.GetControlID(33243602, FocusType.Passive);
        }

        void IterateVisibleItems(int firstRow, int numVisibleRows, float rowWidth, bool hasFocus)
        {
            // We stop iterating items if datasource state changes while iterating its items.
            // This can happen e.g when dragging items or items are expanding/collapsing.
            m_StopIteratingItems = false;

            int rowOffset = 0;
            for (int i = 0; i < numVisibleRows; ++i)
            {
                int row = firstRow + i;

                if (animatingExpansion)
                {
                    // If we are animating expansion/collapsing then ensure items
                    // that are culled by the animation clip rect gets 'converted' into
                    // items after the expanding/collapsing items. When no more items can get culled
                    // then keep adding the offset (to not handle already handled items).
                    int endAnimRow = m_ExpansionAnimator.endRow;
                    if (m_ExpansionAnimator.CullRow(row, gui))
                    {
                        rowOffset++;
                        row = endAnimRow + rowOffset;
                    }
                    else
                    {
                        row += rowOffset;
                    }

                    // Ensure row is still valid after adding the rowOffset?
                    if (row >= data.rowCount)
                    {
                        continue;
                    }
                }
                else
                {
                    // When not animating cull rows outside scroll rect
                    float screenSpaceRowY = gui.GetRowRect(row, rowWidth).y - state.scrollPos.y;
                    if (screenSpaceRowY > m_TotalRect.height)
                    {
                        continue;
                    }
                }

                // Item GUI
                DoItemGUI(data.GetItem(row), row, rowWidth, hasFocus);

                if (m_StopIteratingItems)
                    return;
            }
        }

        List<int> GetVisibleSelectedIds()
        {
            // Do visible items
            int firstRow, lastRow;
            gui.GetFirstAndLastRowVisible(out firstRow, out lastRow);
            if (lastRow < 0)
                return new List<int>();

            List<int> ids = new List<int>(lastRow - firstRow);
            for (int row = firstRow; row < lastRow; ++row)
            {
                var item = data.GetItem(row);
                ids.Add(item.id);
            }

            List<int> selectedVisibleIDs = (from id in ids where state.selectedIDs.Contains(id) select id).ToList();
            return selectedVisibleIDs;
        }

        private void ExpansionAnimationEnded(TreeViewAnimationInput setup)
        {
            // When collapsing we delay the actual collapse until the animation is done
            if (!setup.expanding)
            {
                ChangeExpandedState(setup.item, false, setup.includeChildren);
            }
        }

        float GetAnimationDuration(float height)
        {
            // Speed up animation linearly for heights below kThreshold.
            // We have found from usability testing that for smaller height changes (e.g 3-4 rows)
            // we want a faster animation
            const float kThreshold = 60f;
            const float kMaxDuration = 0.07f;
            return (height > kThreshold) ? kMaxDuration : (height * kMaxDuration / kThreshold);
        }

        public void UserInputChangedExpandedState(TreeViewItem item, int row, bool expand)
        {
            var includeChildren = Event.current.alt;
            if (useExpansionAnimation)
            {
                // We need to expand prior to starting animation so we have the expanded state ready
                if (expand)
                    ChangeExpandedState(item, true, includeChildren);

                int rowStart = row + 1;
                int rowEnd = GetLastChildRowUnder(row);
                float rowWidth = GUIClip.visibleRect.width;
                Rect allRowsRect = GetRectForRows(rowStart, rowEnd, rowWidth);

                float duration = GetAnimationDuration(allRowsRect.height);
                var input = new TreeViewAnimationInput
                {
                    animationDuration = duration,
                    startRow = rowStart,
                    endRow = rowEnd,
                    startRowRect = gui.GetRowRect(rowStart, rowWidth),
                    rowsRect = allRowsRect,
                    expanding = expand,
                    includeChildren = includeChildren,
                    animationEnded = ExpansionAnimationEnded,
                    item = item,
                    treeView = this
                };

                expansionAnimator.BeginAnimating(input);
            }
            else
            {
                ChangeExpandedState(item, expand, includeChildren);
            }
        }

        void ChangeExpandedState(TreeViewItem item, bool expand, bool includeChildren)
        {
            if (includeChildren)
                data.SetExpandedWithChildren(item, expand);
            else
                data.SetExpanded(item, expand);
        }

        int GetLastChildRowUnder(int row)
        {
            var rows = data.GetRows();
            int rowDepth = rows[row].depth;
            for (int i = row + 1; i < rows.Count; ++i)
                if (rows[i].depth <= rowDepth)
                    return i - 1;

            return rows.Count - 1; // end row
        }

        protected virtual Rect GetRectForRows(int startRow, int endRow, float rowWidth)
        {
            Rect startRect = gui.GetRowRect(startRow, rowWidth);
            Rect endRect = gui.GetRowRect(endRow, rowWidth);
            return new Rect(startRect.x, startRect.y, rowWidth, endRect.yMax - startRect.yMin);
        }

        void HandleUnusedEvents()
        {
            switch (Event.current.type)
            {
                case EventType.DragUpdated:
                    if (dragging != null && m_TotalRect.Contains(Event.current.mousePosition))
                    {
                        dragging.DragElement(null, new Rect(), -1);
                        Repaint();
                        Event.current.Use();
                    }
                    break;

                case EventType.DragPerform:
                    if (dragging != null && m_TotalRect.Contains(Event.current.mousePosition))
                    {
                        m_DragSelection.Clear();
                        dragging.DragElement(null, new Rect(), -1);
                        Repaint();
                        Event.current.Use();
                    }
                    break;

                case EventType.DragExited:
                    if (dragging != null)
                    {
                        m_DragSelection.Clear();
                        dragging.DragCleanup(true);
                        Repaint();
                    }
                    break;

                case EventType.MouseDown:
                    if (deselectOnUnhandledMouseDown && Event.current.button == 0 && m_TotalRect.Contains(Event.current.mousePosition) &&  state.selectedIDs.Count > 0)
                    {
                        SetSelection(new int[0], false);
                        NotifyListenersThatSelectionChanged();
                    }
                    break;
                case EventType.ContextClick:
                    if (m_TotalRect.Contains(Event.current.mousePosition))
                    {
                        if (contextClickOutsideItemsCallback != null)
                            contextClickOutsideItemsCallback();
                    }
                    break;
            }
        }

        public void OnEvent()
        {
            state.renameOverlay.OnEvent();
        }

        public bool BeginNameEditing(float delay)
        {
            // No items selected for rename
            if (state.selectedIDs.Count == 0)
                return false;

            var visibleItems = data.GetRows();
            TreeViewItem visibleAndSelectedItem = null;

            foreach (int id in state.selectedIDs)
            {
                TreeViewItem item = visibleItems.FirstOrDefault(i => i.id == id);
                if (visibleAndSelectedItem == null)
                    visibleAndSelectedItem = item;
                else if (item != null)
                    return false; // Don't allow rename if more than one item is both visible and selected
            }

            if (visibleAndSelectedItem != null && data.IsRenamingItemAllowed(visibleAndSelectedItem))
                return gui.BeginRename(visibleAndSelectedItem, delay);

            return false;
        }

        // Let client end renaming from outside
        public void EndNameEditing(bool acceptChanges)
        {
            if (state.renameOverlay.IsRenaming())
            {
                state.renameOverlay.EndRename(acceptChanges);
                gui.EndRename();
            }
        }

        TreeViewItem GetItemAndRowIndex(int id, out int row)
        {
            row = data.GetRow(id);
            if (row == -1)
                return null;
            return data.GetItem(row);
        }

        void HandleFastCollapse(TreeViewItem item, int row)
        {
            if (item.depth == 0)
            {
                // At depth 0 traverse upwards until a parent is found and select that item
                for (int i = row - 1; i >= 0; --i)
                {
                    if (data.GetItem(i).hasChildren)
                    {
                        OffsetSelection(i - row);
                        return;
                    }
                }
            }
            else if (item.depth > 0)
            {
                // Traverse upwards until parent of item is found and select that parent (users want this behavior)
                for (int i = row - 1; i >= 0; --i)
                {
                    if (data.GetItem(i).depth < item.depth)
                    {
                        OffsetSelection(i - row);
                        return;
                    }
                }
            }
        }

        void HandleFastExpand(TreeViewItem item, int row)
        {
            int rowCount = data.rowCount;

            // Traverse downwards until a parent is found and select that parent
            for (int i = row + 1; i < rowCount; ++i)
            {
                if (data.GetItem(i).hasChildren)
                {
                    OffsetSelection(i - row);
                    break;
                }
            }
        }

        private void ChangeFolding(int[] ids, bool expand)
        {
            // Handle folding of single item and multiple items separately
            // Animation is only supported for folding of single item
            if (ids.Length == 1)
                ChangeFoldingForSingleItem(ids[0], expand);
            else if (ids.Length > 1)
                ChangeFoldingForMultipleItems(ids, expand);
        }

        private void ChangeFoldingForSingleItem(int id, bool expand)
        {
            int row;
            TreeViewItem item = GetItemAndRowIndex(id, out row);
            if (item != null)
            {
                if (data.IsExpandable(item) && data.IsExpanded(item) != expand)
                    UserInputChangedExpandedState(item, row, expand);
                else
                {
                    expansionAnimator.SkipAnimating();
                    if (expand)
                        HandleFastExpand(item, row); // Move selection to next parent
                    else
                        HandleFastCollapse(item, row); // Move selection to parent
                }
            }
        }

        private void ChangeFoldingForMultipleItems(int[] ids, bool expand)
        {
            // Collect items that should be expanded/collapsed
            var parents = new HashSet<int>();
            foreach (var id in ids)
            {
                int row;
                TreeViewItem item = GetItemAndRowIndex(id, out row);
                if (item != null)
                {
                    if (data.IsExpandable(item) && data.IsExpanded(item) != expand)
                        parents.Add(id);
                }
            }

            // Expand/collapse all collected items
            if (Event.current.alt)
            {
                // Also expand/collapse children of selected items
                foreach (var id in parents)
                    data.SetExpandedWithChildren(id, expand);
            }
            else
            {
                var expandedIDs = new HashSet<int>(data.GetExpandedIDs());
                if (expand)
                    expandedIDs.UnionWith(parents);
                else
                    expandedIDs.ExceptWith(parents);
                data.SetExpandedIDs(expandedIDs.ToArray());
            }
        }

        void KeyboardGUI()
        {
            if (m_KeyboardControlID != GUIUtility.keyboardControl || !GUI.enabled)
                return;

            // Let client handle keyboard first
            if (keyboardInputCallback != null)
                keyboardInputCallback();

            if (Event.current.type == EventType.KeyDown)
            {
                switch (Event.current.keyCode)
                {
                    // Fold in
                    case KeyCode.LeftArrow:
                        ChangeFolding(state.selectedIDs.ToArray(), false);
                        Event.current.Use();
                        break;

                    // Fold out
                    case KeyCode.RightArrow:
                        ChangeFolding(state.selectedIDs.ToArray(), true);
                        Event.current.Use();
                        break;

                    case KeyCode.UpArrow:
                        Event.current.Use();
                        OffsetSelection(-1);
                        break;

                    // Select next or first
                    case KeyCode.DownArrow:
                        Event.current.Use();
                        OffsetSelection(1);
                        break;

                    case KeyCode.Home:
                        Event.current.Use();
                        OffsetSelection(-1000000);
                        break;

                    case KeyCode.End:
                        Event.current.Use();
                        OffsetSelection(1000000);
                        break;

                    case KeyCode.PageUp:
                    {
                        Event.current.Use();
                        TreeViewItem lastClickedItem = data.FindItem(state.lastClickedID);
                        if (lastClickedItem != null)
                        {
                            int numRowsPageUp = gui.GetNumRowsOnPageUpDown(lastClickedItem, true, m_TotalRect.height);
                            OffsetSelection(-numRowsPageUp);
                        }
                    }
                    break;

                    case KeyCode.PageDown:
                    {
                        Event.current.Use();
                        TreeViewItem lastClickedItem = data.FindItem(state.lastClickedID);
                        if (lastClickedItem != null)
                        {
                            int numRowsPageDown = gui.GetNumRowsOnPageUpDown(lastClickedItem, true, m_TotalRect.height);
                            OffsetSelection(numRowsPageDown);
                        }
                    }
                    break;

                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                        if (Application.platform == RuntimePlatform.OSXEditor)
                            if (BeginNameEditing(0f))
                                Event.current.Use();
                        break;

                    case KeyCode.F2:
                        if (Application.platform != RuntimePlatform.OSXEditor)
                            if (BeginNameEditing(0f))
                                Event.current.Use();
                        break;

                    default:
                        if (Event.current.keyCode > KeyCode.A && Event.current.keyCode < KeyCode.Z)
                        {
                            // TODO: jump to folder with char?
                        }
                        break;
                }
            }
        }

        static internal int GetIndexOfID(IList<TreeViewItem> items, int id)
        {
            for (int i = 0; i < items.Count; ++i)
                if (items[i].id == id)
                    return i;
            return -1;
        }

        public bool IsLastClickedPartOfRows()
        {
            var visibleRows = data.GetRows();
            if (visibleRows.Count == 0)
                return false;

            return GetIndexOfID(visibleRows, state.lastClickedID) >= 0;
        }

        public void OffsetSelection(int offset)
        {
            expansionAnimator.SkipAnimating();
            var visibleRows = data.GetRows();

            if (visibleRows.Count == 0)
                return;

            Event.current.Use();
            int index = GetIndexOfID(visibleRows, state.lastClickedID);
            int newIndex = Mathf.Clamp(index + offset, 0, visibleRows.Count - 1);
            EnsureRowIsVisible(newIndex, true);
            SelectionByKey(visibleRows[newIndex]);
        }

        bool GetFirstAndLastSelected(List<TreeViewItem> items, out int firstIndex, out int lastIndex)
        {
            firstIndex = -1;
            lastIndex = -1;
            for (int i = 0; i < items.Count; ++i)
            {
                if (state.selectedIDs.Contains(items[i].id))
                {
                    if (firstIndex == -1)
                        firstIndex = i;
                    lastIndex = i; // just overwrite and we will have the last in the end...
                }
            }
            return firstIndex != -1 && lastIndex != -1;
        }

        // Returns list of selected ids
        List<int> GetNewSelection(TreeViewItem clickedItem, bool keepMultiSelection, bool useShiftAsActionKey)
        {
            // Get ids from items
            var visibleRows = data.GetRows();
            List<int> allIDs = new List<int>(visibleRows.Count);
            for (int i = 0; i < visibleRows.Count; ++i)
                allIDs.Add(visibleRows[i].id);

            List<int> selectedIDs = state.selectedIDs;
            int lastClickedID = state.lastClickedID;
            bool allowMultiselection = data.CanBeMultiSelected(clickedItem);

            return InternalEditorUtility.GetNewSelection(clickedItem.id, allIDs, selectedIDs, lastClickedID, keepMultiSelection, useShiftAsActionKey, allowMultiselection);
        }

        void SelectionByKey(TreeViewItem itemSelected)
        {
            var newSelection = GetNewSelection(itemSelected, false, true);
            NewSelectionFromUserInteraction(newSelection, itemSelected.id);
        }

        public void SelectionClick(TreeViewItem itemClicked, bool keepMultiSelection)
        {
            var newSelection = GetNewSelection(itemClicked, keepMultiSelection, false);
            NewSelectionFromUserInteraction(newSelection, itemClicked != null ? itemClicked.id : 0);
        }

        void NewSelectionFromUserInteraction(List<int> newSelection, int itemID)
        {
            state.lastClickedID = itemID;

            bool selectionChanged = !state.selectedIDs.SequenceEqual(newSelection);
            if (selectionChanged)
            {
                state.selectedIDs = newSelection;
                NotifyListenersThatSelectionChanged();
            }
        }

        public void RemoveSelection()
        {
            if (state.selectedIDs.Count > 0)
            {
                state.selectedIDs.Clear();
                NotifyListenersThatSelectionChanged();
            }
        }

        float GetTopPixelOfRow(int row)
        {
            return gui.GetRowRect(row, 1).y;
        }

        void EnsureRowIsVisible(int row, bool animated)
        {
            // We don't want to change the scroll to make the row visible,
            // if it's disabled: Causes content to be moved/rendered out of bounds.
            if (!m_UseScrollView)
                return;

            if (row >= 0)
            {
                // Adjusting for when the horizontal scrollbar is being shown. Before the TreeView has repainted
                // once the m_VisibleRect is not valid, then we use m_TotalRect which is passed when initialized
                float visibleHeight = m_VisibleRect.height > 0 ? m_VisibleRect.height : m_TotalRect.height;

                Rect frameRect = gui.GetRectForFraming(row);
                float scrollTop = frameRect.y;
                float scrollBottom = frameRect.yMax - visibleHeight;

                if (state.scrollPos.y < scrollBottom)
                    ChangeScrollValue(scrollBottom, animated);
                else if (state.scrollPos.y > scrollTop)
                    ChangeScrollValue(scrollTop, animated);
            }
        }

        void AnimatedScrollChanged()
        {
            Repaint();
            state.scrollPos.y = m_FramingAnimFloat.value;
        }

        void ChangeScrollValue(float targetScrollPos, bool animated)
        {
            if (m_UseExpansionAnimation && animated)
            {
                m_FramingAnimFloat.value = state.scrollPos.y;
                m_FramingAnimFloat.target = targetScrollPos;
                m_FramingAnimFloat.speed = 3f;
            }
            else
            {
                state.scrollPos.y = targetScrollPos;
            }
        }

        public void Frame(int id, bool frame, bool ping)
        {
            const bool animated = false;
            Frame(id, frame, ping, animated);
        }

        public void Frame(int id, bool frame, bool ping, bool animated)
        {
            float topPixelOfRow = -1f;

            if (frame)
            {
                data.RevealItem(id);

                int row = data.GetRow(id);
                if (row >= 0)
                {
                    topPixelOfRow = GetTopPixelOfRow(row);
                    EnsureRowIsVisible(row, animated);
                }
            }

            if (ping)
            {
                int row = data.GetRow(id);
                if (topPixelOfRow == -1f)
                {
                    // Was not framed first so we need to calc it here
                    if (row >= 0)
                        topPixelOfRow = GetTopPixelOfRow(row);
                }

                if (topPixelOfRow >= 0f && row >= 0 && row < data.rowCount)
                {
                    TreeViewItem item = data.GetItem(row);
                    float scrollBarOffset = GetContentSize().y > m_TotalRect.height ? -kSpaceForScrollBar : 0f;
                    gui.BeginPingItem(item, topPixelOfRow, m_TotalRect.width + scrollBarOffset);
                }
            }
        }

        public void EndPing()
        {
            gui.EndPingItem();
        }

        // Item holding most basic data for TreeView event handling
        // Extend this class to hold your specific tree data and make your DataSource
        // create items of your type. During e.g OnGUI you can the cast Item to your own type when needed.


        // Hidden items (under collapsed items) are added last
        public List<int> SortIDsInVisiblityOrder(IList<int> ids)
        {
            if (ids.Count <= 1)
                return ids.ToList(); // no sorting needed

            var visibleRows = data.GetRows();
            List<int> sorted = new List<int>();
            for (int i = 0; i < visibleRows.Count; ++i)
            {
                int id = visibleRows[i].id;
                for (int j = 0; j < ids.Count; ++j)
                {
                    if (ids[j] == id)
                    {
                        sorted.Add(id);
                        break;
                    }
                }
            }

            // Some rows with selection are collapsed (not visible) so add those to the end
            if (ids.Count != sorted.Count)
            {
                sorted.AddRange(ids.Except(sorted));
                if (ids.Count != sorted.Count)
                    Debug.LogError("SortIDsInVisiblityOrder failed: " + ids.Count + " != " + sorted.Count);
            }

            return sorted;
        }
    }
} // end namespace UnityEditor
