// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    /// *undocumented*
    internal class ListViewGUILayout
    {
        static int layoutedListViewHash = "layoutedListView".GetHashCode();

        static ListViewState lvState = null;

        static int listViewHash = "ListView".GetHashCode();
        static int[] dummyWidths = new int[1];

        static public ListViewShared.ListViewElementsEnumerator ListView(ListViewState state, GUIStyle style, params GUILayoutOption[] options)
        {
            return ListView(state, 0, string.Empty, style, options);
        }

        static public ListViewShared.ListViewElementsEnumerator ListView(ListViewState state, string dragTitle, GUIStyle style, params GUILayoutOption[] options)
        {
            return ListView(state, 0, dragTitle, style, options);
        }

        static public ListViewShared.ListViewElementsEnumerator ListView(ListViewState state, ListViewOptions lvOptions, GUIStyle style, params GUILayoutOption[] options)
        {
            return ListView(state, lvOptions, string.Empty, style, options);
        }

        static public ListViewShared.ListViewElementsEnumerator ListView(ListViewState state, ListViewOptions lvOptions, string dragTitle, GUIStyle style, params GUILayoutOption[] options)
        {
            lvState = state;

            GUILayout.BeginHorizontal(style, options); // no good reason for this here, except drawing LVs background

            state.scrollPos = EditorGUILayout.BeginScrollView(state.scrollPos, options);
            BeginLayoutedListview(state, GUIStyle.none);

            state.draggedFrom = -1;
            state.draggedTo = -1;
            state.fileNames = null;

            if ((lvOptions & ListViewOptions.wantsReordering) != 0) state.ilvState.wantsReordering = true;
            if ((lvOptions & ListViewOptions.wantsExternalFiles) != 0) state.ilvState.wantsExternalFiles = true;
            if ((lvOptions & ListViewOptions.wantsToStartCustomDrag) != 0) state.ilvState.wantsToStartCustomDrag = true;
            if ((lvOptions & ListViewOptions.wantsToAcceptCustomDrag) != 0) state.ilvState.wantsToAcceptCustomDrag = true;

            return DoListView(state, null, dragTitle);
        }

        static Rect dummyRect = new Rect(0, 0, 1, 1);

        static private ListViewShared.ListViewElementsEnumerator DoListView(ListViewState state, int[] colWidths, string dragTitle)
        {
            Rect vRect = dummyRect;
            int invisibleRows = 0;
            int endRow = 0;

            ListViewShared.InternalLayoutedListViewState ilvState = state.ilvState;

            //GUIUtility.CheckOnGUI ();
            int id = GUIUtility.GetControlID(listViewHash, FocusType.Passive);

            state.ID = id;
            state.selectionChanged = false;
            ilvState.state = state;

            if (Event.current.type != EventType.Layout)
            {
                vRect = new Rect(0, state.scrollPos.y, GUIClip.visibleRect.width, GUIClip.visibleRect.height);

                if (vRect.width <= 0) vRect.width = 1;
                if (vRect.height <= 0) vRect.height = 1;

                state.ilvState.rect = vRect;

                invisibleRows = (int)(vRect.yMin) / state.rowHeight;
                endRow = invisibleRows + (int)System.Math.Ceiling(((vRect.yMin % state.rowHeight) + vRect.height) / state.rowHeight) - 1;

                //if (id == GUIUtility.hotControl)
                //{
                //  s = invisibleRows.ToString() + "::" + endRow.ToString();
                //}

                ilvState.invisibleRows = invisibleRows;
                ilvState.endRow = endRow;
                ilvState.rectHeight = (int)vRect.height;

                if (invisibleRows < 0)
                    invisibleRows = 0;

                if (endRow >= state.totalRows)
                    endRow = state.totalRows - 1;
            }

            if (colWidths == null)
            {
                dummyWidths[0] = (int)vRect.width;
                colWidths = dummyWidths;
            }

            return new ListViewShared.ListViewElementsEnumerator(ilvState, colWidths, invisibleRows, endRow, dragTitle, new Rect(0, invisibleRows * state.rowHeight, vRect.width, state.rowHeight));
        }

        private static void BeginLayoutedListview(ListViewState state, GUIStyle style, params GUILayoutOption[] options)
        {
            GUILayoutedListViewGroup g = (GUILayoutedListViewGroup)GUILayoutUtility.BeginLayoutGroup(style, null, typeof(GUILayoutedListViewGroup));

            g.state = state;
            state.ilvState.group = g;

            GUIUtility.GetControlID(layoutedListViewHash, FocusType.Passive);

            switch (Event.current.type)
            {
                case EventType.Layout:
                {
                    g.resetCoords = false;
                    g.isVertical = true;
                    g.ApplyOptions(options);
                    break;
                }
            }
        }

        /// *undocumented*
        internal class GUILayoutedListViewGroup : GUILayoutGroup
        {
            internal ListViewState state;

            public override void CalcWidth()
            {
                // Make LVs width independent of widths of elements inside it
                base.CalcWidth();
                minWidth = 0;
                maxWidth = 0;
                stretchWidth = 10000;
            }

            public override void CalcHeight()
            {
                minHeight = 0;
                maxHeight = 0;

                base.CalcHeight();

                margin.top = 0;
                margin.bottom = 0;

                if (minHeight == 0) // empty lv?
                {
                    minHeight = 1;
                    maxHeight = 1;
                    state.rowHeight = 1;
                }
                else
                {
                    state.rowHeight = (int)minHeight;
                    minHeight *= state.totalRows;
                    maxHeight *= state.totalRows;
                }
            }

            private void AddYRecursive(GUILayoutEntry e, float y)
            {
                // this looks kind of bad, but in 99% of cases it would only be one level depth (e.g. few labels inside BeginHorizontal)
                e.rect.y += y;

                GUILayoutGroup g = e as GUILayoutGroup;

                if (g != null)
                {
                    for (int i = 0; i < g.entries.Count; i++)
                        AddYRecursive((GUILayoutEntry)g.entries[i], y);
                }
            }

            public void AddY()
            {
                if (entries.Count > 0)
                    AddYRecursive((GUILayoutEntry)entries[0], ((GUILayoutEntry)entries[0]).minHeight);
            }

            public void AddY(float val)
            {
                if (entries.Count > 0)
                    AddYRecursive((GUILayoutEntry)entries[0], val);
            }
        }

        static public bool MultiSelection(int prevSelected, int currSelected, ref int initialSelected, ref bool[] selectedItems)
        {
            return ListViewShared.MultiSelection(lvState.ilvState, prevSelected, currSelected, ref initialSelected, ref selectedItems);
        }

        static public bool HasMouseUp(Rect r)
        {
            return ListViewShared.HasMouseUp(lvState.ilvState, r, 0);
        }

        static public bool HasMouseDown(Rect r)
        {
            return ListViewShared.HasMouseDown(lvState.ilvState, r, 0);
        }

        static public bool HasMouseDown(Rect r, int button)
        {
            return ListViewShared.HasMouseDown(lvState.ilvState, r, button);
        }
    }
}
