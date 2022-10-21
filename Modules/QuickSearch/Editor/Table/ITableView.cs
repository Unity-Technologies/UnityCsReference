// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    public interface ITableView
    {
        bool readOnly { get; }
        SearchContext context { get; }
        void AddColumn(Vector2 mousePosition, int activeColumnIndex);
        void AddColumns(IEnumerable<SearchColumn> descriptors, int activeColumnIndex);
        void SetupColumns(IEnumerable<SearchItem> elements = null);
        void RemoveColumn(int activeColumnIndex);
        void SwapColumns(int columnIndex, int swappedColumnIndex);
        IEnumerable<SearchItem> GetElements();
        IEnumerable<SearchColumn> GetColumns();
        IEnumerable<SearchItem> GetRows();
        SearchTable GetSearchTable();
        void SetSelection(IEnumerable<SearchItem> items);
        void OnItemExecuted(SearchItem item);
        void SetDirty();
        bool OpenContextualMenu(Event evt, SearchItem item);

        [System.Obsolete("Search IMGUI is not supported anymore", error: false)] // 2023.1
        void UpdateColumnSettings(int columnIndex, IMGUI.Controls.MultiColumnHeaderState.Column columnSettings);

        [System.Obsolete("Search IMGUI is not supported anymore", error: false)] // 2023.1
        bool AddColumnHeaderContextMenuItems(GenericMenu menu);

        [System.Obsolete("Search IMGUI is not supported anymore", error: false)] // 2023.1
        void AddColumnHeaderContextMenuItems(GenericMenu menu, SearchColumn sourceColumn);

        /// Mainly used for test
        internal IEnumerable<object> GetValues(int columnIdx);
        internal float GetRowHeight();
        internal int GetColumnIndex(string name);
        internal SearchColumn FindColumnBySelector(string selector);
    }

    [System.Obsolete("IMGUI is not support anymore. Use ITableView interface instead.", error: false)] // 2023.1
    public class PropertyTable : UnityEditor.IMGUI.Controls.TreeView, System.IDisposable
    {
        public PropertyTable(string serializationUID, ITableView tableView) : base(new IMGUI.Controls.TreeViewState()) => throw new System.NotSupportedException();
        protected override void BeforeRowsGUI() => throw new System.NotSupportedException();
        protected override UnityEditor.IMGUI.Controls.TreeViewItem BuildRoot() => throw new System.NotSupportedException();
        protected override System.Collections.Generic.IList<UnityEditor.IMGUI.Controls.TreeViewItem> BuildRows(UnityEditor.IMGUI.Controls.TreeViewItem root) => throw new System.NotSupportedException();
        protected override bool CanStartDrag(UnityEditor.IMGUI.Controls.TreeView.CanStartDragArgs args) => throw new System.NotSupportedException();
        public void Dispose() => throw new System.NotSupportedException();
        protected override void DoubleClickedItem(int id) => throw new System.NotSupportedException();
        public void FrameColumn(int columnIndex) => throw new System.NotSupportedException();
        protected override DragAndDropVisualMode HandleDragAndDrop(UnityEditor.IMGUI.Controls.TreeView.DragAndDropArgs args) => throw new System.NotSupportedException();
        protected override void KeyEvent() => throw new System.NotSupportedException();
        public override void OnGUI(UnityEngine.Rect tableRect) => throw new System.NotSupportedException();
        protected override void RowGUI(UnityEditor.IMGUI.Controls.TreeView.RowGUIArgs args) => throw new System.NotSupportedException();
        protected override void SelectionChanged(System.Collections.Generic.IList<int> selectedIds) => throw new System.NotSupportedException();
        protected override void SetupDragAndDrop(UnityEditor.IMGUI.Controls.TreeView.SetupDragAndDropArgs args) => throw new System.NotSupportedException();
    }

}
