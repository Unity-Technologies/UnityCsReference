// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    /// *undocumented*
    internal class ListViewGUI
    {
        static int[] dummyWidths = new int[1];

        static internal ListViewShared.InternalListViewState ilvState = new ListViewShared.InternalListViewState();
        static int listViewHash = "ListView".GetHashCode();

        static public ListViewShared.ListViewElementsEnumerator ListView(Rect pos, ListViewState state)
        {
            return DoListView(pos, state, null, string.Empty);
        }

        static public ListViewShared.ListViewElementsEnumerator ListView(ListViewState state, GUIStyle style, params GUILayoutOption[] options)
        {
            return ListView(state, 0, null, string.Empty, style, options);
        }

        static public ListViewShared.ListViewElementsEnumerator ListView(ListViewState state, int[] colWidths, GUIStyle style, params GUILayoutOption[] options)
        {
            return ListView(state, 0, colWidths, string.Empty, style, options);
        }

        static public ListViewShared.ListViewElementsEnumerator ListView(ListViewState state, ListViewOptions lvOptions, GUIStyle style, params GUILayoutOption[] options)
        {
            return ListView(state, lvOptions, null, string.Empty, style, options);
        }

        static public ListViewShared.ListViewElementsEnumerator ListView(ListViewState state, ListViewOptions lvOptions, string dragTitle, GUIStyle style, params GUILayoutOption[] options)
        {
            return ListView(state, lvOptions, null, dragTitle, style, options);
        }

        static public ListViewShared.ListViewElementsEnumerator ListView(ListViewState state, ListViewOptions lvOptions, int[] colWidths, string dragTitle, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(style);
            state.scrollPos = EditorGUILayout.BeginScrollView(state.scrollPos, options);
            ilvState.beganHorizontal = true;

            state.draggedFrom = -1;
            state.draggedTo = -1;
            state.fileNames = null;

            if ((lvOptions & ListViewOptions.wantsReordering) != 0) ilvState.wantsReordering = true;
            if ((lvOptions & ListViewOptions.wantsExternalFiles) != 0) ilvState.wantsExternalFiles = true;
            if ((lvOptions & ListViewOptions.wantsToStartCustomDrag) != 0) ilvState.wantsToStartCustomDrag = true;
            if ((lvOptions & ListViewOptions.wantsToAcceptCustomDrag) != 0) ilvState.wantsToAcceptCustomDrag = true;

            return DoListView(GUILayoutUtility.GetRect(1, state.totalRows * state.rowHeight + 3), state, colWidths, string.Empty);
        }

        static public ListViewShared.ListViewElementsEnumerator DoListView(Rect pos, ListViewState state, int[] colWidths, string dragTitle)
        {
            int id = GUIUtility.GetControlID(listViewHash, FocusType.Passive);
            state.ID = id;

            state.selectionChanged = false;

            Rect vRect;

            if ((GUIClip.visibleRect.x < 0) || (GUIClip.visibleRect.y < 0)) // TODO: this is needed for simple LVs to work. we are not in a clip at all.
            {
                vRect = pos;
            }
            else
                vRect = (pos.y < 0) ? new Rect(0, 0, GUIClip.visibleRect.width, GUIClip.visibleRect.height) : new Rect(0, state.scrollPos.y, GUIClip.visibleRect.width, GUIClip.visibleRect.height); // check if this is custom scroll

            if (vRect.width <= 0) vRect.width = 1;
            if (vRect.height <= 0) vRect.height = 1;

            ilvState.rect = vRect;

            int invisibleRows = (int)((-pos.y + vRect.yMin) / state.rowHeight);
            int endRow = invisibleRows + (int)System.Math.Ceiling((((vRect.yMin - pos.y) % state.rowHeight) + vRect.height) / state.rowHeight) - 1;

            if (colWidths == null)
            {
                dummyWidths[0] = (int)vRect.width;
                colWidths = dummyWidths;
            }

            ilvState.invisibleRows = invisibleRows;
            ilvState.endRow = endRow;
            ilvState.rectHeight = (int)vRect.height;
            ilvState.state = state;

            if (invisibleRows < 0)
                invisibleRows = 0;

            if (endRow >= state.totalRows)
                endRow = state.totalRows - 1;

            return new ListViewShared.ListViewElementsEnumerator(ilvState, colWidths, invisibleRows, endRow, dragTitle, new Rect(0, invisibleRows * state.rowHeight, pos.width, state.rowHeight));
        }

        static public bool MultiSelection(int prevSelected, int currSelected, ref int initialSelected, ref bool[] selectedItems)
        {
            return ListViewShared.MultiSelection(ilvState, prevSelected, currSelected, ref initialSelected, ref selectedItems);
        }

        static public bool HasMouseUp(Rect r)
        {
            return ListViewShared.HasMouseUp(ilvState, r, 0);
        }

        static public bool HasMouseDown(Rect r)
        {
            return ListViewShared.HasMouseDown(ilvState, r, 0);
        }

        static public bool HasMouseDown(Rect r, int button)
        {
            return ListViewShared.HasMouseDown(ilvState, r, button);
        }
    }
}
