// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.UIElements;

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
    public class TreeViewState<TIdentifier> where TIdentifier : unmanaged, System.IEquatable<TIdentifier>
    {
        public List<TIdentifier> selectedIDs { get { return m_SelectedIDs; } set { m_SelectedIDs = value; } }
        public TIdentifier lastClickedID { get { return m_LastClickedID; } set { m_LastClickedID = value; } }
        public List<TIdentifier> expandedIDs { get { return m_ExpandedIDs; } set { m_ExpandedIDs = value; } }
        internal RenameOverlay<TIdentifier> renameOverlay { get { return m_RenameOverlay; } set { m_RenameOverlay = value; } }

        public string searchString { get { return m_SearchString; } set { m_SearchString = value; } }
        public Vector2 scrollPos;

        // Selection state
        [SerializeField] private List<TIdentifier> m_SelectedIDs = new List<TIdentifier>();
        [SerializeField] private TIdentifier m_LastClickedID; // used for navigation

        // Expanded state (assumed sorted)
        [SerializeField] private List<TIdentifier> m_ExpandedIDs = new List<TIdentifier>();

        // Rename and create asset state
        [SerializeField] private RenameOverlay<TIdentifier> m_RenameOverlay = new RenameOverlay<TIdentifier>();

        // Search state (can be used by Datasource to filter tree when reloading)
        [SerializeField] private string m_SearchString;

        internal virtual void OnAwake()
        {
            // Clear state that should not survive closing/starting Unity (If TreeViewState is in EditorWindow that are serialized in a layout file)
            m_RenameOverlay.Clear();
        }
    }

    internal struct TreeViewSelectState<TIdentifier>
    {
        public List<TIdentifier> selectedIDs;
        public TIdentifier lastClickedID;
        public bool keepMultiSelection;
        public bool useShiftAsActionKey;
    }

    internal class TreeViewController<TIdentifier> where TIdentifier : unmanaged, System.IEquatable<TIdentifier>
    {
        public System.Action<TIdentifier[]> selectionChangedCallback { get; set; } // ids
        public System.Action<TIdentifier> itemSingleClickedCallback { get; set; } // id
        public System.Action<TIdentifier> itemDoubleClickedCallback { get; set; } // id
        public System.Action<TIdentifier[], bool> dragEndedCallback { get; set; } // dragged ids, if null then drag was not allowed, bool == true if dragging tree view items from own treeview, false if drag was started outside
        public System.Action<TIdentifier> contextClickItemCallback { get; set; } // clicked item id
        public System.Action contextClickOutsideItemsCallback { get; set; }
        public System.Action keyboardInputCallback { get; set; }
        public System.Action expandedStateChanged { get; set; }
        public System.Action<string> searchChanged { get; set; }
        public System.Action<Vector2> scrollChanged { get; set; }
        public System.Action<TIdentifier, Rect> onGUIRowCallback { get; set; }  // <id, Rect of row>

        internal System.Action<TIdentifier, Rect> onFoldoutButton { get; set; }  // <id, Rect of row>

        // Main state
        GUIView m_GUIView;                                              // Containing view for this tree: used for checking if we have focus and for requesting repaints
        public ITreeViewDataSource<TIdentifier> data { get; set; }                   // Data provider for this tree: handles data fetching
        public ITreeViewDragging<TIdentifier> dragging { get; set; }                 // Handle dragging
        public ITreeViewGUI<TIdentifier> gui { get; set; }                           // Handles GUI (input and rendering)
        public TreeViewState<TIdentifier> state { get; set; }                        // State that persists script reloads
        public GUIStyle horizontalScrollbarStyle { get; set; }
        public GUIStyle verticalScrollbarStyle { get; set; }
        public GUIStyle scrollViewStyle { get; set; }
        public TreeViewItemExpansionAnimator<TIdentifier> expansionAnimator { get { return m_ExpansionAnimator; } }
        readonly TreeViewItemExpansionAnimator<TIdentifier> m_ExpansionAnimator = new TreeViewItemExpansionAnimator<TIdentifier>();
        AnimFloat m_FramingAnimFloat;

        bool m_StopIteratingItems;

        public bool deselectOnUnhandledMouseDown { get; set; }
        public bool enableItemHovering { get; set; }

        IntegerCache m_DragSelection = new IntegerCache();
        IntegerCache m_CachedSelection = new IntegerCache();

        struct IntegerCache
        {
            List<TIdentifier> m_List;
            HashSet<TIdentifier> m_HashSet;

            public bool Contains(TIdentifier id)
            {
                if (m_HashSet == null)
                    return false;

                return m_HashSet.Contains(id);
            }

            public void Set(List<TIdentifier> list)
            {
                if (list == null)
                    throw new ArgumentNullException(nameof(list));

                if (!Equals(list))
                {
                    m_List = new List<TIdentifier>(list);
                    m_HashSet = new HashSet<TIdentifier>(list);
                }
            }

            public List<TIdentifier> Get()
            {
                return m_List;
            }

            public void Clear()
            {
                if (m_List == null)
                    return;

                m_List.Clear();
                m_HashSet.Clear();
            }

            public bool HasValues()
            {
                if (m_List == null)
                    return false;

                return m_List.Count > 0;
            }

            bool Equals(List<TIdentifier> list)
            {
                if (m_List == null || list == null)
                    return false;

                int count = m_List.Count;
                if (count != list.Count)
                    return false;

                for (int i = 0; i < count; ++i)
                {
                    if (!list[i].Equals(m_List[i]))
                        return false;
                }

                return true;
            }
        }

        bool m_UseScrollView = true;                                    // Internal scrollview can be omitted when e.g mulitple tree views in one scrollview is wanted
        bool m_ConsumeKeyDownEvents = true;
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

        const double kSlowSelectTimeout = 0.2;
        const float kSpaceForScrollBar = 16f;
        public TreeViewItem<TIdentifier> hoveredItem { get; set; }

        public TreeViewController(EditorWindow editorWindow, TreeViewState<TIdentifier> treeViewState)
        {
            m_GUIView = editorWindow ? editorWindow.m_Parent : GUIView.current;
            state = treeViewState;
        }

        public void Init(Rect rect, ITreeViewDataSource<TIdentifier> data, ITreeViewGUI<TIdentifier> gui, ITreeViewDragging<TIdentifier> dragging)
        {
            this.data = data;
            this.gui = gui;
            this.dragging = dragging;
            m_VisibleRect = m_TotalRect = rect; // We initialize the total rect because it might be needed for framing selection when reloading data the first time.

            // Allow sub systems to set up delegates etc after treeview references have been set up
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
            get { return m_DragSelection.HasValues(); }
        }

        public bool IsDraggingItem(TreeViewItem<TIdentifier> item)
        {
            return m_DragSelection.Contains(item.id);
        }

        public bool showingVerticalScrollBar
        {
            get { return m_VisibleRect.height > 0 && m_ContentRect.height > m_VisibleRect.height; }
        }

        public bool showingHorizontalScrollBar
        {
            get { return m_VisibleRect.width > 0 && m_ContentRect.width > m_VisibleRect.width; }
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

        public bool useScrollView
        {
            get { return m_UseScrollView; }
            set { m_UseScrollView = value; }
        }

        public Rect visibleRect
        {
            get { return m_VisibleRect; }
        }

        public bool IsSelected(TIdentifier id)
        {
            return state.selectedIDs.Contains(id);
        }

        public bool HasSelection()
        {
            return state.selectedIDs.Count > 0;
        }

        public TIdentifier[] GetSelection()
        {
            return state.selectedIDs.ToArray();
        }

        public TIdentifier[] GetRowIDs()
        {
            return (from item in data.GetRows() select item.id).ToArray();
        }

        public void SetSelection(TIdentifier[] selectedIDs, bool revealSelectionAndFrameLastSelected)
        {
            const bool animatedFraming = false;
            SetSelection(selectedIDs, revealSelectionAndFrameLastSelected, animatedFraming);
        }

        public void SetSelection(TIdentifier[] selectedIDs, bool revealSelectionAndFrameLastSelected, bool animatedFraming)
        {
            // Keep for debugging
            //Debug.Log ("SetSelection: new selection: " + DebugUtils.ListToString(new List<int>(selectedIDs)));

            // Init new state
            if (selectedIDs.Length > 0)
            {
                if (revealSelectionAndFrameLastSelected)
                {
                    data.RevealItems(selectedIDs);
                }

                state.selectedIDs = new List<TIdentifier>(selectedIDs);

                // Ensure that our key navigation is setup
                bool hasLastClicked = state.selectedIDs.IndexOf(state.lastClickedID) >= 0;
                if (!hasLastClicked)
                {
                    // See if we can find a valid id, we check the last selected (selectedids might contain invalid ids e.g scene objects in project browser and vice versa)
                    TIdentifier lastSelectedID = selectedIDs.Last();
                    if (data.GetRow(lastSelectedID) != -1)
                    {
                        state.lastClickedID = lastSelectedID;
                        hasLastClicked = true;
                    }
                    else
                        state.lastClickedID = default;
                }

                if (revealSelectionAndFrameLastSelected && hasLastClicked)
                    Frame(state.lastClickedID, true, false, animatedFraming);
            }
            else
            {
                state.selectedIDs.Clear();
                state.lastClickedID = default;
            }

            // Should not fire callback since this is called from outside
            // NotifyListenersThatSelectionChanged ()
        }

        public TreeViewItem<TIdentifier> FindItem(TIdentifier id)
        {
            return data.FindItem(id);
        }

        [System.Obsolete("SetUseScrollView has been deprecated. Use property useScrollView instead.")]
        public void SetUseScrollView(bool useScrollView)
        {
            m_UseScrollView = useScrollView;
        }

        public void SetConsumeKeyDownEvents(bool consume)
        {
            m_ConsumeKeyDownEvents = consume;
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

        static internal int GetItemControlID(TreeViewItem<TIdentifier> item)
        {
            return ((item != null) ? item.id.GetHashCode() : 0) + 10000000;
        }

        public void HandleUnusedMouseEventsForItem(Rect rect, TreeViewItem<TIdentifier> item, int row)
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
                                double selectStartTime = Time.realtimeSinceStartup;
                                var dragSelection = GetNewSelection(item, true, false);
                                bool dragAbortedBySlowSelect = (Time.realtimeSinceStartup - selectStartTime) > kSlowSelectTimeout;

                                bool canStartDrag = !dragAbortedBySlowSelect && dragging != null && dragSelection.Count != 0 && dragging.CanStartDrag(item, dragSelection, Event.current.mousePosition);
                                if (canStartDrag)
                                {
                                    // Prepare drag and drop delay (we start the drag after a couple of pixels mouse drag: See the case MouseDrag below)
                                    m_DragSelection.Set(dragSelection);
                                    DragAndDropDelay delay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), GetItemControlID(item));
                                    delay.mouseDownPosition = Event.current.mousePosition;
                                }
                                else
                                {
                                    // If dragging is not supported or not allowed for the drag selection then handle selection on mouse down
                                    // (when dragging is handled we handle selection on mouse up to e.g allow to drag to object fields in the inspector)
                                    m_DragSelection.Clear();

                                    if (m_AllowRenameOnMouseUp)
                                        m_AllowRenameOnMouseUp = (state.selectedIDs.Count == 1 && state.selectedIDs[0].Equals(item.id)); // If first time selection then prevent starting a rename on the following mouse up after this mouse down

                                    SelectionClick(item, false);

                                    // Notify about single click
                                    if (itemSingleClickedCallback != null)
                                        itemSingleClickedCallback(item.id);
                                }

                                GUIUtility.hotControl = GetItemControlID(item);
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
                    if (GUIUtility.hotControl == itemControlID && dragging != null && m_DragSelection.HasValues())
                    {
                        DragAndDropDelay delay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), itemControlID);
                        if (delay.CanStartDrag() && dragging.CanStartDrag(item, m_DragSelection.Get(), delay.mouseDownPosition))
                        {
                            dragging.StartDrag(item, m_DragSelection.Get());
                            GUIUtility.hotControl = 0;
                        }

                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == itemControlID)
                    {
                        // When having the temp dragging selection delay the the selection until mouse up
                        bool useMouseUpSelection = m_DragSelection.HasValues();

                        // Clear state before SelectionClick since it can ExitGUI early
                        GUIUtility.hotControl = 0;
                        m_DragSelection.Clear();
                        evt.Use();

                        // On Mouse up either start name editing or change selection (if not done on mouse down)
                        if (rect.Contains(evt.mousePosition))
                        {
                            Rect renameActivationRect = gui.GetRenameRect(rect, row, item);
                            List<TIdentifier> selected = state.selectedIDs;
                            if (m_AllowRenameOnMouseUp && selected != null && selected.Count == 1 && selected[0].Equals(item.id) && renameActivationRect.Contains(evt.mousePosition) && !EditorGUIUtility.HasHolddownKeyModifiers(evt))
                            {
                                BeginNameEditing(0.5f);
                            }
                            else if (useMouseUpSelection)
                            {
                                SelectionClick(item, false);

                                // Notify about single click
                                if (itemSingleClickedCallback != null)
                                    itemSingleClickedCallback(item.id);
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

        public void NotifyListenersThatDragEnded(TIdentifier[] draggedIDs, bool draggedItemsFromOwnTreeView)
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

        public bool IsItemDragSelectedOrSelected(TreeViewItem<TIdentifier> item)
        {
            return m_DragSelection.HasValues() ? m_DragSelection.Contains(item.id) : m_CachedSelection.Contains(item.id);
        }

        public bool animatingExpansion { get { return m_UseExpansionAnimation && m_ExpansionAnimator.isAnimating; } }

        void DoItemGUI(TreeViewItem<TIdentifier> item, int row, float rowWidth, bool hasFocus)
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
            {
                m_TotalRect = rect;
                m_CachedSelection.Set(state.selectedIDs);
            }

            m_GUIView = GUIView.current;

            // End rename if the window do not have focus
            if (m_GUIView != null && !m_GUIView.hasFocus && state.renameOverlay.IsRenaming())
            {
                EndNameEditing(true);
            }

            // Grab keyboard focus if requested
            if (m_GrabKeyboardFocus)
            {
                m_GrabKeyboardFocus = false;
                GUIUtility.keyboardControl = m_KeyboardControlID;
                Repaint(); // Ensure repaint so we can show we have keyboard focus
            }

            bool isMouseDownInTotalRect = evt.type == EventType.MouseDown && m_TotalRect.Contains(evt.mousePosition);
            if (isMouseDownInTotalRect)
            {
                m_AllowRenameOnMouseUp = true; // reset value (can be changed later in this event if the TreeView gets focus)
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
                state.scrollPos = GUI.BeginScrollView(m_TotalRect, state.scrollPos, m_ContentRect, false, false,
                    horizontalScrollbarStyle != null ? horizontalScrollbarStyle : GUI.skin.horizontalScrollbar,
                    verticalScrollbarStyle != null ? verticalScrollbarStyle : GUI.skin.verticalScrollbar, scrollViewStyle != null ? scrollViewStyle : EditorStyles.scrollViewAlt);
            }
            else
                GUI.BeginClip(m_TotalRect);

            if (evt.type == EventType.Repaint)
            {
                if (m_UseScrollView)
                {
                    m_VisibleRect = GUI.GetTopScrollView().visibleRect;
                }
                else
                {
                    // We may be inside of a scroll view.
                    var scrollView = GUI.GetTopScrollView();
                    if (scrollView != null)
                    {
                        // Calculate the visible area of the TreeView inside of the ScrollView taking into account
                        // that the TreeView may not be contained within the whole ScrollView area.
                        state.scrollPos = Vector2.Max(Vector2.zero, scrollView.scrollPosition - m_TotalRect.min - scrollView.position.min);
                        m_VisibleRect = scrollView.visibleRect;
                        m_VisibleRect.size = Vector2.Max(Vector2.zero, Vector2.Min(m_VisibleRect.size, (m_TotalRect.size - state.scrollPos)));
                    }
                    else
                    {
                        // If this is contained withing something from UI Toolkit (e.g. the Inspector window), the scroll
                        // will be controlled by UI Toolkit itself so we need to make sure to only show what we need to
                        // show, otherwise things can become really slow.
                        var container = UIElementsUtility.GetCurrentIMGUIContainer();
                        var uiScrollView = container?.GetFirstAncestorOfType<ScrollView>();
                        if (uiScrollView != null)
                        {
                            // We use the viewport of the UI Toolkit scroll view to calculate the visible area.
                            var viewport = uiScrollView.Q("unity-content-viewport");
                            var viewportWorldBound = viewport.worldBound;
                            var viewportRectWorld = GUIClip.Unclip(viewportWorldBound);
                            var treeViewRectWindow = GUIClip.UnclipToWindow(m_TotalRect);

                            float visibleHeight = (viewportRectWorld.y + viewportRectWorld.height) - treeViewRectWindow.y;
                            float visibleWidth = (viewportRectWorld.x + viewportRectWorld.width) - treeViewRectWindow.x;
                            float scrollPosY = 0f, scrollPosX = 0f;

                            if (visibleHeight > viewportRectWorld.height)
                            {
                                visibleHeight = viewportRectWorld.height;
                                scrollPosY = viewportRectWorld.y - treeViewRectWindow.y;
                            }

                            if (visibleWidth > viewportRectWorld.width)
                            {
                                visibleWidth = viewportRectWorld.width;
                                scrollPosX = viewportRectWorld.x - treeViewRectWindow.x;
                            }

                            if (scrollPosX == 0 && scrollPosY == 0)
                            {
                                // Avoids newing a Vector2 if we don't need a scroll position.
                                state.scrollPos = Vector2.zero;
                            }
                            else
                            {
                                state.scrollPos = new Vector2(scrollPosX, scrollPosY);
                            }
                            m_VisibleRect = new Rect(0f, 0f, visibleWidth, visibleHeight);
                        }
                        else
                        {
                            m_VisibleRect = m_TotalRect;
                        }
                    }
                }
            }

            gui.BeginRowGUI();

            // Iterate visible items
            int firstRow, lastRow;
            gui.GetFirstAndLastRowVisible(out firstRow, out lastRow);
            if (lastRow >= 0)
            {
                int numVisibleRows = lastRow - firstRow + 1;
                float rowWidth = Mathf.Max(GUIClip.visibleRect.width, m_ContentRect.width);

                IterateVisibleItems(firstRow, numVisibleRows, rowWidth, HasFocus());
            }

            // Call before gui.EndRowGUI() so stuff we render in EndRowGUI does not end up
            // in the the animation clip rect
            if (animatingExpansion)
                m_ExpansionAnimator.OnAfterAllRowsGUI();

            gui.EndRowGUI();

            // Keep inside clip region so callbacks that might want to get
            // rects of rows have correct context.
            KeyboardGUI();

            if (m_UseScrollView)
                GUI.EndScrollView(showingVerticalScrollBar || showingHorizontalScrollBar);
            else
                GUI.EndClip();

            HandleUnusedEvents();

            // Call after iterating rows since selecting a row takes keyboard focus
            HandleTreeViewGotFocus(isMouseDownInTotalRect);

            // Prevent controlID inconsistency for the controls following this tree view: We use the hint parameter of GetControlID to
            // fast forward to a fixed entry in the id list so the following controls always start from there regardless of the rows that have been
            // culled.
            GUIUtility.GetControlID(33243602, FocusType.Passive);

            if (Event.current.type == EventType.MouseLeaveWindow)
                hoveredItem = null;
        }

        void HandleTreeViewGotFocus(bool isMouseDownInTotalRect)
        {
            if (Event.current.type == EventType.Layout)
                return;

            // Detect if TreeView got keyboard focus (ignore layout event which gets fired infront of mousedown)
            bool hasFocus = HasFocus();
            if (hasFocus != m_HadFocusLastEvent)
            {
                m_HadFocusLastEvent = hasFocus;

                if (hasFocus && isMouseDownInTotalRect)
                {
                    // If we got focus this event by mouse down then we do not want to begin renaming
                    // if clicking on an already selected item in the up coming MouseUp event.
                    m_AllowRenameOnMouseUp = false;
                }
            }
        }

        void IterateVisibleItems(int firstRow, int numVisibleRows, float rowWidth, bool hasFocus)
        {
            // We stop iterating items if datasource state changes while iterating its items.
            // This can happen e.g when dragging items or items are expanding/collapsing.
            m_StopIteratingItems = false;

            TreeViewItem<TIdentifier> currentHoveredItem = null;

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

                if (enableItemHovering)
                {
                    Rect rowRect = gui.GetRowRect(row, showingVerticalScrollBar ? rowWidth - kSpaceForScrollBar : rowWidth);
                    if (rowRect.Contains(Event.current.mousePosition) && GUIView.mouseOverView == GUIView.current)
                        currentHoveredItem = data.GetItem(row);
                    m_GUIView.MarkHotRegion(GUIClip.UnclipToWindow(rowRect));
                }

                // Item GUI
                // Note that DoItemGUI() needs to be called right before checking m_StopIteratingItems since
                // UI in the current row can issue a reload of the TreeView data
                DoItemGUI(data.GetItem(row), row, rowWidth, hasFocus);

                if (m_StopIteratingItems)
                    break;
            }

            hoveredItem = currentHoveredItem;
        }

        private void ExpansionAnimationEnded(TreeViewAnimationInput<TIdentifier> setup)
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

        public void UserInputChangedExpandedState(TreeViewItem<TIdentifier> item, int row, bool expand)
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
                var input = new TreeViewAnimationInput<TIdentifier>
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

        internal void ChangeExpandedState(TreeViewItem<TIdentifier> item, bool expand, bool includeChildren)
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
                    bool containsMouse = m_TotalRect.Contains(Event.current.mousePosition);
                    if (containsMouse)
                    {
                        GUIUtility.keyboardControl = m_KeyboardControlID;
                        Repaint();
                    }
                    if (deselectOnUnhandledMouseDown && containsMouse && Event.current.button == 0 && state.selectedIDs.Count > 0)
                    {
                        SetSelection(new TIdentifier[0], false);
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
            TreeViewItem<TIdentifier> visibleAndSelectedItem = null;

            foreach (TIdentifier id in state.selectedIDs)
            {
                TreeViewItem<TIdentifier> item = visibleItems.FirstOrDefault(i => i.id.Equals(id));
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

        TreeViewItem<TIdentifier> GetItemAndRowIndex(TIdentifier id, out int row)
        {
            row = data.GetRow(id);
            if (row == -1)
                return null;
            return data.GetItem(row);
        }

        void HandleFastCollapse(TreeViewItem<TIdentifier> item, int row)
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

        void HandleFastExpand(TreeViewItem<TIdentifier> item, int row)
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

        private void ChangeFolding(TIdentifier[] ids, bool expand)
        {
            // Handle folding of single item and multiple items separately
            // Animation is only supported for folding of single item
            if (ids.Length == 1)
                ChangeFoldingForSingleItem(ids[0], expand);
            else if (ids.Length > 1)
                ChangeFoldingForMultipleItems(ids, expand);
        }

        private void ChangeFoldingForSingleItem(TIdentifier id, bool expand)
        {
            // Skip any ongoing animation first because it could affect the row count.
            // I.e. if the item to be collapsed is in a row that no longer exists after the animation is done and the rows refreshed in InitIfNeeded, skiping the animation later would cause an IndexOufOfBounds in HandleFastCollapse
            // if no animation is happening, this is just a null check so moving it later for performance reasons makes little sense.
            expansionAnimator.SkipAnimating();

            int row;
            TreeViewItem<TIdentifier> item = GetItemAndRowIndex(id, out row);
            if (item != null)
            {
                if (data.IsExpandable(item) && data.IsExpanded(item) != expand)
                    UserInputChangedExpandedState(item, row, expand);
                else
                {
                    if (expand)
                        HandleFastExpand(item, row); // Move selection to next parent
                    else
                        HandleFastCollapse(item, row); // Move selection to parent
                }
            }
        }

        private void ChangeFoldingForMultipleItems(TIdentifier[] ids, bool expand)
        {
            // Collect items that should be expanded/collapsed
            var parents = new HashSet<TIdentifier>();
            foreach (var id in ids)
            {
                int row;
                TreeViewItem<TIdentifier> item = GetItemAndRowIndex(id, out row);
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
                var expandedIDs = new HashSet<TIdentifier>(data.GetExpandedIDs());
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

            if (!m_ConsumeKeyDownEvents)
                return;

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
                        TreeViewItem<TIdentifier> lastClickedItem = data.FindItem(state.lastClickedID);
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
                        TreeViewItem<TIdentifier> lastClickedItem = data.FindItem(state.lastClickedID);
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

        static internal int GetIndexOfID(IList<TreeViewItem<TIdentifier>> items, TIdentifier id)
        {
            for (int i = 0; i < items.Count; ++i)
                if (items[i].id.Equals(id))
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

            Event.current?.Use();
            int index = GetIndexOfID(visibleRows, state.lastClickedID);
            int newIndex = Mathf.Clamp(index + offset, 0, visibleRows.Count - 1);
            EnsureRowIsVisible(newIndex, false);
            SelectionByKey(visibleRows[newIndex]);
        }

        public Func<TreeViewItem<TIdentifier>, bool, bool, List<TIdentifier>> getNewSelectionOverride { private get; set; }

        // Returns list of selected ids
        List<TIdentifier> GetNewSelection(TreeViewItem<TIdentifier> clickedItem, bool keepMultiSelection, bool useShiftAsActionKey)
        {
            if (getNewSelectionOverride != null)
                return getNewSelectionOverride(clickedItem, keepMultiSelection, useShiftAsActionKey);

            var selectState = new TreeViewSelectState<TIdentifier>() {
                selectedIDs = state.selectedIDs,
                lastClickedID = state.lastClickedID,
                keepMultiSelection = keepMultiSelection,
                useShiftAsActionKey = useShiftAsActionKey
            };

            return data.GetNewSelection(clickedItem, selectState);
        }

        void SelectionByKey(TreeViewItem<TIdentifier> itemSelected)
        {
            var newSelection = GetNewSelection(itemSelected, false, true);
            NewSelectionFromUserInteraction(newSelection, itemSelected.id);
        }

        public void SelectionClick(TreeViewItem<TIdentifier> itemClicked, bool keepMultiSelection)
        {
            var newSelection = GetNewSelection(itemClicked, keepMultiSelection, false);
            NewSelectionFromUserInteraction(newSelection, itemClicked != null ? itemClicked.id : default);
        }

        void NewSelectionFromUserInteraction(List<TIdentifier> newSelection, TIdentifier itemID)
        {
            state.lastClickedID = itemID;

            bool selectionChanged = !state.selectedIDs.SequenceEqual(newSelection);
            if (selectionChanged)
            {
                state.selectedIDs = newSelection;
                NotifyListenersThatSelectionChanged();
                Repaint();
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

        public void Frame(TIdentifier id, bool frame, bool ping)
        {
            const bool animated = false;
            Frame(id, frame, ping, animated);
        }

        public void Frame(TIdentifier id, bool frame, bool ping, bool animated)
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
                    TreeViewItem<TIdentifier> item = data.GetRows()[row];
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
        public List<TIdentifier> SortIDsInVisiblityOrder(IList<TIdentifier> ids)
        {
            if (ids.Count <= 1)
                return ids.ToList(); // no sorting needed

            var visibleRows = data.GetRows();
            List<TIdentifier> sorted = new List<TIdentifier>();
            for (int i = 0; i < visibleRows.Count; ++i)
            {
                var id = visibleRows[i].id;
                for (int j = 0; j < ids.Count; ++j)
                {
                    if (ids[j].Equals(id))
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
