// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    /// *undocumented*
    [System.Serializable]
    internal class ListViewState
    {
        const int c_rowHeight = 16; //TODO:
        public int row;
        public int column;
        public Vector2 scrollPos;
        public int totalRows;
        public int rowHeight;

        public int ID;
        public bool selectionChanged;
        public int draggedFrom;
        public int draggedTo;
        public bool drawDropHere = false;
        public Rect dropHereRect = new Rect(0, 0, 0, 0);
        public string[] fileNames = null;
        public int customDraggedFromID = 0;

        public ListViewState() { Init(0, c_rowHeight); }
        public ListViewState(int totalRows) { Init(totalRows, c_rowHeight); }
        public ListViewState(int totalRows, int rowHeight) { Init(totalRows, rowHeight); }

        /// *undocumented*
        internal ListViewShared.InternalLayoutedListViewState ilvState = new ListViewShared.InternalLayoutedListViewState();

        private void Init(int totalRows, int rowHeight)
        {
            this.row = -1;
            this.column = 0;
            this.scrollPos = Vector2.zero;
            this.totalRows = totalRows;
            this.rowHeight = rowHeight;

            selectionChanged = false;
        }
    }
}
