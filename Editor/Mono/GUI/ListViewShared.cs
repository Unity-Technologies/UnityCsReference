// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    /// *undocumented*
    internal class ListViewShared
    {
        public static bool OSX = Application.platform == RuntimePlatform.OSXEditor;

        internal static int dragControlID = -1;
        internal static bool isDragging = false;

        /// *undocumented*
        internal class InternalListViewState
        {
            public int id = -1;
            public int invisibleRows;
            public int endRow;
            public int rectHeight;
            public ListViewState state;
            public bool beganHorizontal = false;
            public Rect rect;
            public bool wantsReordering = false;
            public bool wantsExternalFiles = false;
            public bool wantsToStartCustomDrag = false;
            public bool wantsToAcceptCustomDrag = false;
            public int dragItem;
        }

        /// *undocumented*
        internal class InternalLayoutedListViewState : InternalListViewState
        {
            public ListViewGUILayout.GUILayoutedListViewGroup group;
        }

        /// *undocumented*
        internal class Constants
        {
            // this should not be pre-setup
            // different windows (having different styles set) can draw using this
            public static string insertion = "PR Insertion";
        }

        // Returns if LV selection has changed
        /// *undocumented*
        static bool DoLVPageUpDown(InternalListViewState ilvState, ref int selectedRow, ref Vector2 scrollPos, bool up)
        {
            int visibleRows = ilvState.endRow - ilvState.invisibleRows;

            if (up)
            {
                if (OSX)
                {
                    scrollPos.y -= ilvState.state.rowHeight * visibleRows;

                    if (scrollPos.y < 0)
                        scrollPos.y = 0;
                }
                else
                {
                    selectedRow -= visibleRows;

                    if (selectedRow < 0)
                        selectedRow = 0;

                    return true;
                }
            }
            else
            {
                if (OSX)
                {
                    scrollPos.y += ilvState.state.rowHeight * visibleRows;
                    //FIXME: does this need an upper bound check?
                }
                else
                {
                    selectedRow += visibleRows;

                    if (selectedRow >= ilvState.state.totalRows)
                        selectedRow = ilvState.state.totalRows - 1;

                    return true;
                }
            }

            return false;
        }

        /// *undocumented*
        static internal bool ListViewKeyboard(InternalListViewState ilvState, int totalCols)
        {
            int totalRows = ilvState.state.totalRows;

            if ((Event.current.type != EventType.KeyDown) || (totalRows == 0))
                return false;

            if ((GUIUtility.keyboardControl != ilvState.state.ID) ||
                Event.current.GetTypeForControl(ilvState.state.ID) != EventType.KeyDown)
                return false;

            return SendKey(ilvState, Event.current.keyCode, totalCols);
        }

        internal static void SendKey(ListViewState state, KeyCode keyCode)
        {
            SendKey(state.ilvState, keyCode, 1);
        }

        internal static bool SendKey(InternalListViewState ilvState, KeyCode keyCode, int totalCols)
        {
            var state = ilvState.state;

            //ilvState.state.row, ref ilvState.state.column, ref ilvState.state.scrollPos
            switch (keyCode)
            {
                case KeyCode.UpArrow:
                {
                    if (state.row > 0)
                        state.row--;
                    break;
                }
                case KeyCode.DownArrow:
                {
                    if (state.row < state.totalRows - 1)
                        state.row++;
                    break;
                }
                case KeyCode.Home:
                {
                    state.row = 0;
                    break;
                }
                case KeyCode.End:
                {
                    state.row = state.totalRows - 1;
                    break;
                }
                case KeyCode.LeftArrow:
                    if (state.column > 0)
                        state.column--;
                    break;
                case KeyCode.RightArrow:
                    if (state.column < totalCols - 1)
                        state.column++;
                    break;

                case KeyCode.PageUp:
                {
                    if (!DoLVPageUpDown(ilvState, ref state.row, ref state.scrollPos, true))
                    {
                        Event.current.Use();
                        return false;
                    }
                    break;
                }
                case KeyCode.PageDown:
                {
                    if (!DoLVPageUpDown(ilvState, ref state.row, ref state.scrollPos, false))
                    {
                        Event.current.Use();
                        return false;
                    }
                    break;
                }
                default:
                    return false;
            }

            state.scrollPos = ListViewScrollToRow(ilvState, state.scrollPos, state.row);
            Event.current.Use();
            return true;
        }

        static internal bool HasMouseDown(InternalListViewState ilvState, Rect r)
        {
            return HasMouseDown(ilvState, r, 0);
        }

        static internal bool HasMouseDown(InternalListViewState ilvState, Rect r, int button)
        {
            if (Event.current.type == EventType.MouseDown && Event.current.button == button)
            {
                if (r.Contains(Event.current.mousePosition))
                {
                    GUIUtility.hotControl = ilvState.state.ID;
                    GUIUtility.keyboardControl = ilvState.state.ID;
                    ListViewShared.isDragging = false;
                    Event.current.Use();
                    return true;
                }
            }

            return false;
        }

        static internal bool HasMouseUp(InternalListViewState ilvState, Rect r)
        {
            return HasMouseUp(ilvState, r, 0);
        }

        static internal bool HasMouseUp(InternalListViewState ilvState, Rect r, int button)
        {
            if (Event.current.type == EventType.MouseUp && Event.current.button == button)
            {
                if (r.Contains(Event.current.mousePosition))
                {
                    GUIUtility.hotControl = 0;
                    Event.current.Use();
                    return true;
                }
            }

            return false;
        }

        // Returns if selection was actually changed
        static internal bool MultiSelection(InternalListViewState ilvState, int prevSelected, int currSelected, ref int initialSelected, ref bool[] selectedItems)
        {
            bool shiftIsDown = Event.current.shift;
            bool ctrlIsDown = EditorGUI.actionKey;
            bool selChanged = false;

            if ((shiftIsDown || ctrlIsDown) && (initialSelected == -1))
                initialSelected = prevSelected;

            // multi selection
            if (shiftIsDown)
            {
                int from = System.Math.Min(initialSelected, currSelected);
                int to = System.Math.Max(initialSelected, currSelected);

                if (!ctrlIsDown)
                {
                    for (int i = 0; i < from; i++)
                    {
                        if (selectedItems[i])
                            selChanged = true;

                        selectedItems[i] = false;
                    }

                    for (int i = to + 1; i < selectedItems.Length; i++)
                    {
                        if (selectedItems[i])
                            selChanged = true;

                        selectedItems[i] = false;
                    }
                }

                if (from < 0)
                    from = to;

                for (int i = from; i <= to; i++)
                {
                    if (!selectedItems[i])
                        selChanged = true;

                    selectedItems[i] = true;
                }
            }
            else if (ctrlIsDown)
            {
                selectedItems[currSelected] = !selectedItems[currSelected];
                initialSelected = currSelected;
                selChanged = true;
            }
            else
            {
                if (!selectedItems[currSelected])
                    selChanged = true;

                for (int i = 0; i < selectedItems.Length; i++)
                {
                    if (selectedItems[i] && currSelected != i)
                        selChanged = true;

                    selectedItems[i] = false;
                }

                initialSelected = -1;
                selectedItems[currSelected] = true;
            }

            if (ilvState != null)
                ilvState.state.scrollPos = ListViewScrollToRow(ilvState, currSelected);

            return selChanged;
        }

        static internal Vector2 ListViewScrollToRow(InternalListViewState ilvState, int row)
        {
            return ListViewScrollToRow(ilvState, ilvState.state.scrollPos, row);
        }

        static internal int ListViewScrollToRow(InternalListViewState ilvState, int currPosY, int row)
        {
            return (int)ListViewScrollToRow(ilvState, new Vector2(0, currPosY), row).y;
        }

        static internal Vector2 ListViewScrollToRow(InternalListViewState ilvState, Vector2 currPos, int row)
        {
            if ((ilvState.invisibleRows < row) && (ilvState.endRow > row))
                return currPos;

            if (row <= ilvState.invisibleRows)
                currPos.y = ilvState.state.rowHeight * row;
            else
                currPos.y = ilvState.state.rowHeight * (row + 1) - ilvState.rectHeight;

            if (currPos.y < 0)
                currPos.y = 0;
            else if (currPos.y > ilvState.state.totalRows * ilvState.state.rowHeight - ilvState.rectHeight)
                currPos.y = ilvState.state.totalRows * ilvState.state.rowHeight - ilvState.rectHeight;

            return currPos;
        }

        /// *undocumented*
        internal class ListViewElementsEnumerator : IEnumerator<ListViewElement>
        {
            private int[] colWidths;
            private int xTo;
            private int yFrom;
            private int yTo;

            private Rect firstRect;
            private Rect rect;
            private int xPos = -1;
            private int yPos = -1;
            private ListViewElement element;

            private InternalListViewState ilvState;
            private InternalLayoutedListViewState ilvStateL;

            private bool quiting;
            private bool isLayouted;

            private string dragTitle;

            internal ListViewElementsEnumerator(InternalListViewState ilvState, int[] colWidths, int yFrom, int yTo, string dragTitle, Rect firstRect)
            {
                this.colWidths = colWidths;
                this.xTo = colWidths.Length - 1;
                this.yFrom = yFrom;
                this.yTo = yTo;
                this.firstRect = firstRect;
                this.rect = firstRect;
                this.quiting = ilvState.state.totalRows == 0;

                this.ilvState = ilvState;
                this.ilvStateL = ilvState as InternalLayoutedListViewState;

                this.isLayouted = ilvStateL != null;

                this.dragTitle = dragTitle;

                ilvState.state.customDraggedFromID = 0;

                Reset();
            }

            public bool MoveNext()
            {
                if (xPos > -1)
                {
                    if (ListViewShared.HasMouseDown(ilvState, rect))
                    {
                        ilvState.state.selectionChanged = true;
                        ilvState.state.row = yPos;
                        ilvState.state.column = xPos;
                        ilvState.state.scrollPos = ListViewShared.ListViewScrollToRow(ilvState, yPos); // this is about clicking on a row that is partially visible

                        if ((ilvState.wantsReordering || ilvState.wantsToStartCustomDrag) && (GUIUtility.hotControl == ilvState.state.ID))
                        {
                            DragAndDropDelay delay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), ilvState.state.ID);
                            delay.mouseDownPosition = Event.current.mousePosition;
                            ilvState.dragItem = yPos;
                            dragControlID = ilvState.state.ID;
                        }
                    }
                    // On Mouse drag, start drag & drop
                    if (!ListViewShared.isDragging &&
                        (ilvState.wantsReordering || ilvState.wantsToStartCustomDrag) &&
                        GUIUtility.hotControl == ilvState.state.ID &&
                        Event.current.type == EventType.MouseDrag &&
                        GUIClip.visibleRect.Contains(Event.current.mousePosition))
                    {
                        DragAndDropDelay delay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), ilvState.state.ID);

                        if (delay.CanStartDrag())
                        {
                            DragAndDrop.PrepareStartDrag();

                            DragAndDrop.objectReferences = new UnityEngine.Object[] {};  // this IS required for dragging to work
                            DragAndDrop.paths = null;

                            if (ilvState.wantsReordering)
                            {
                                ilvState.state.dropHereRect = new Rect(ilvState.rect.x, 0, ilvState.rect.width, ilvState.state.rowHeight * 2);
                                DragAndDrop.StartDrag(dragTitle);
                            }
                            else if (ilvState.wantsToStartCustomDrag)
                            {
                                DragAndDrop.SetGenericData("CustomDragID", ilvState.state.ID);
                                DragAndDrop.StartDrag(dragTitle);
                            }

                            ListViewShared.isDragging = true;
                        }

                        Event.current.Use();
                    }
                }

                xPos++;

                if (xPos > xTo)
                {
                    xPos = 0;
                    yPos++;

                    rect.x = firstRect.x;
                    rect.width = colWidths[0];

                    if (yPos > yTo)
                    {
                        quiting = true;
                    }
                    else // move vertically
                    {
                        rect.y += rect.height;
                    }
                }
                else // move horizontally
                {
                    if (xPos >= 1)
                        rect.x += colWidths[xPos - 1];

                    rect.width = colWidths[xPos];
                }

                element.row = yPos;
                element.column = xPos;
                element.position = rect;

                if (element.row >= ilvState.state.totalRows)
                    quiting = true;

                if (isLayouted && Event.current.type == EventType.Layout)
                {
                    // this is just "on layout event enumerate just first row" (so we get height of single row)
                    if (yFrom + 1 == yPos)
                    {
                        quiting = true;
                    }
                }

                if (isLayouted && yPos != yFrom)
                    GUILayout.EndHorizontal();

                if (quiting)
                {
                    if (ilvState.state.drawDropHere && Event.current.GetTypeForControl(ilvState.state.ID) == EventType.Repaint)
                    {
                        GUIStyle insertion = Constants.insertion;
                        insertion.Draw(insertion.margin.Remove(ilvState.state.dropHereRect), false, false, false, false);
                    }

                    if (ListViewShared.ListViewKeyboard(ilvState, colWidths.Length))
                        ilvState.state.selectionChanged = true;

                    if (Event.current.GetTypeForControl(ilvState.state.ID) == EventType.MouseUp)
                    {
                        GUIUtility.hotControl = 0;
                    }

                    if (ilvState.wantsReordering && (GUIUtility.hotControl == ilvState.state.ID))
                    {
                        ListViewState lv = ilvState.state;

                        switch (Event.current.type)
                        {
                            case EventType.DragUpdated:
                            {
                                DragAndDrop.visualMode = ilvState.rect.Contains(Event.current.mousePosition) ?
                                    DragAndDropVisualMode.Move : DragAndDropVisualMode.None;

                                Event.current.Use();

                                if (DragAndDrop.visualMode != DragAndDropVisualMode.None)
                                {
                                    lv.dropHereRect.y = (Mathf.RoundToInt(Event.current.mousePosition.y / lv.rowHeight) - 1) * lv.rowHeight;

                                    if (lv.dropHereRect.y >= lv.rowHeight * lv.totalRows)
                                        lv.dropHereRect.y = lv.rowHeight * (lv.totalRows - 1);

                                    lv.drawDropHere = true;
                                }

                                break;
                            }
                            case EventType.DragPerform:
                            {
                                if (GUIClip.visibleRect.Contains(Event.current.mousePosition))
                                {
                                    ilvState.state.draggedFrom = ilvState.dragItem;
                                    ilvState.state.draggedTo = Mathf.RoundToInt(Event.current.mousePosition.y / lv.rowHeight);

                                    if (ilvState.state.draggedTo > ilvState.state.totalRows)
                                        ilvState.state.draggedTo = ilvState.state.totalRows;

                                    // the guy handling this would better actually rearrange items...
                                    if (ilvState.state.draggedTo > ilvState.state.draggedFrom)
                                        ilvState.state.row = ilvState.state.draggedTo - 1;
                                    else
                                        ilvState.state.row = ilvState.state.draggedTo;

                                    ilvState.state.selectionChanged = true;

                                    DragAndDrop.AcceptDrag();
                                    Event.current.Use();
                                    ilvState.wantsReordering = false;
                                    ilvState.state.drawDropHere = false;
                                }

                                GUIUtility.hotControl = 0;
                                break;
                            }
                            case EventType.DragExited:
                            {
                                ilvState.wantsReordering = false;
                                ilvState.state.drawDropHere = false;

                                GUIUtility.hotControl = 0;
                                break;
                            }
                        }
                    }
                    else if (ilvState.wantsExternalFiles)
                    {
                        switch (Event.current.type)
                        {
                            case EventType.DragUpdated:
                            {
                                if ((GUIClip.visibleRect.Contains(Event.current.mousePosition)) &&
                                    (DragAndDrop.paths != null) && (DragAndDrop.paths.Length != 0))         // dragging files from somewhere
                                {
                                    DragAndDrop.visualMode = ilvState.rect.Contains(Event.current.mousePosition) ?
                                        DragAndDropVisualMode.Copy : DragAndDropVisualMode.None;

                                    Event.current.Use();

                                    if (DragAndDrop.visualMode != DragAndDropVisualMode.None)
                                    {
                                        ilvState.state.dropHereRect = new Rect(ilvState.rect.x,
                                                (Mathf.RoundToInt(Event.current.mousePosition.y / ilvState.state.rowHeight) - 1) * ilvState.state.rowHeight,
                                                ilvState.rect.width, ilvState.state.rowHeight);

                                        if (ilvState.state.dropHereRect.y >= ilvState.state.rowHeight * ilvState.state.totalRows)
                                            ilvState.state.dropHereRect.y = ilvState.state.rowHeight * (ilvState.state.totalRows - 1);

                                        ilvState.state.drawDropHere = true;
                                    }
                                }
                                break;
                            }
                            case EventType.DragPerform:
                            {
                                if (GUIClip.visibleRect.Contains(Event.current.mousePosition))
                                {
                                    ilvState.state.fileNames = DragAndDrop.paths;
                                    DragAndDrop.AcceptDrag();
                                    Event.current.Use();
                                    ilvState.wantsExternalFiles = false;
                                    ilvState.state.drawDropHere = false;
                                    ilvState.state.draggedTo = Mathf.RoundToInt(Event.current.mousePosition.y / ilvState.state.rowHeight);
                                    if (ilvState.state.draggedTo > ilvState.state.totalRows)
                                        ilvState.state.draggedTo = ilvState.state.totalRows;
                                    ilvState.state.row = ilvState.state.draggedTo;
                                }

                                GUIUtility.hotControl = 0;

                                break;
                            }
                            case EventType.DragExited:
                            {
                                ilvState.wantsExternalFiles = false;
                                ilvState.state.drawDropHere = false;

                                GUIUtility.hotControl = 0;
                                break;
                            }
                        }
                    }
                    else if (ilvState.wantsToAcceptCustomDrag && (dragControlID != ilvState.state.ID))
                    {
                        switch (Event.current.type)
                        {
                            case EventType.DragUpdated:
                            {
                                object data = DragAndDrop.GetGenericData("CustomDragID");

                                if (GUIClip.visibleRect.Contains(Event.current.mousePosition) && data != null)
                                {
                                    DragAndDrop.visualMode = ilvState.rect.Contains(Event.current.mousePosition) ?
                                        DragAndDropVisualMode.Move : DragAndDropVisualMode.None;

                                    Event.current.Use();
                                }
                                break;
                            }
                            case EventType.DragPerform:
                            {
                                object data = DragAndDrop.GetGenericData("CustomDragID");

                                if (GUIClip.visibleRect.Contains(Event.current.mousePosition) && data != null)
                                {
                                    ilvState.state.customDraggedFromID = (int)data;
                                    DragAndDrop.AcceptDrag();
                                    Event.current.Use();
                                }

                                GUIUtility.hotControl = 0;
                                break;
                            }
                            case EventType.DragExited:
                            {
                                GUIUtility.hotControl = 0;
                                break;
                            }
                        }
                    }

                    if (ilvState.beganHorizontal)
                    {
                        EditorGUILayout.EndScrollView();
                        GUILayout.EndHorizontal();
                        ilvState.beganHorizontal = false;
                    }

                    if (isLayouted)
                    {
                        GUILayoutUtility.EndLayoutGroup();
                        EditorGUILayout.EndScrollView();
                    }

                    ilvState.wantsReordering = false;
                    ilvState.wantsExternalFiles = false;
                }
                else if (isLayouted)
                {
                    if (yPos != yFrom)
                    {
                        ilvStateL.group.ResetCursor();
                        ilvStateL.group.AddY();
                    }
                    else
                        ilvStateL.group.AddY(ilvState.invisibleRows * ilvState.state.rowHeight);
                }

                if (isLayouted)
                {
                    if (!quiting)
                        GUILayout.BeginHorizontal(GUIStyle.none); // for each row
                    else
                        GUILayout.EndHorizontal(); // the one used for drawing LVs background
                }

                return !quiting;
            }

            public void Reset()
            {
                xPos = -1;
                yPos = yFrom;
            }

            ListViewElement IEnumerator<ListViewElement>.Current { get { return element; } }
            object IEnumerator.Current { get { return element; } }

            public IEnumerator GetEnumerator() { return this; }
            public void Dispose() {}
        }
    }
}
