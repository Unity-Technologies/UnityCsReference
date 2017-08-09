// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    internal class ColumnViewElement
    {
        public string name;
        public object value;

        public ColumnViewElement(string name, object value)
        {
            this.name = name;
            this.value = value;
        }
    }

    internal class ColumnView
    {
        public class Styles
        {
            public GUIStyle background = "OL Box";
            public GUIStyle selected = "PR Label";

            // TODO: Make a proper style.
            public Texture2D categoryArrowIcon = EditorStyles.foldout.normal.background;
        }

        private static Styles s_Styles;
        private readonly List<ListViewState> m_ListViewStates;
        private readonly List<int> m_CachedSelectedIndices;
        private Vector2 m_ScrollPosition;
        private string m_SearchText = string.Empty;

        public string searchText
        {
            get { return m_SearchText; }
        }

        public bool isSearching
        {
            get { return searchText != string.Empty; }
        }

        public float columnWidth = 150f;
        public int minimumNumberOfColumns = 1;
        private int m_ColumnToFocusKeyboard = -1;

        public delegate void ObjectColumnFunction(object value);
        public delegate object ObjectColumnGetDataFunction(object value);

        public ColumnView()
        {
            m_ListViewStates = new List<ListViewState>();
            m_CachedSelectedIndices = new List<int>();
        }

        private static void InitStyles()
        {
            if (s_Styles == null)
                s_Styles = new Styles();
        }

        public void SetSelected(int column, int selectionIndex)
        {
            if (m_ListViewStates.Count == column)
                m_ListViewStates.Add(new ListViewState());

            if (m_CachedSelectedIndices.Count == column)
                m_CachedSelectedIndices.Add(-1);

            m_CachedSelectedIndices[column] = selectionIndex;
            m_ListViewStates[column].row = selectionIndex;
        }

        public void SetKeyboardFocusColumn(int column)
        {
            m_ColumnToFocusKeyboard = column;
        }

        public void OnGUI(List<ColumnViewElement> elements, ObjectColumnFunction previewColumnFunction)
        {
            OnGUI(elements, previewColumnFunction, null, null, null);
        }

        public void OnGUI(List<ColumnViewElement> elements, ObjectColumnFunction previewColumnFunction, ObjectColumnFunction selectedSearchItemFunction, ObjectColumnFunction selectedRegularItemFunction, ObjectColumnGetDataFunction getDataForDraggingFunction)
        {
            InitStyles();
            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);
            GUILayout.BeginHorizontal();

            var columnElements = elements;
            object selectedObject;
            int listViewIndex = 0;

            do
            {
                if (m_ListViewStates.Count == listViewIndex)
                    m_ListViewStates.Add(new ListViewState());

                if (m_CachedSelectedIndices.Count == listViewIndex)
                    m_CachedSelectedIndices.Add(-1);

                var listView = m_ListViewStates[listViewIndex];
                listView.totalRows = columnElements.Count;

                if (listViewIndex == 0)
                    GUILayout.BeginVertical(GUILayout.MaxWidth(columnWidth));

                int selectedIndex = m_CachedSelectedIndices[listViewIndex];
                selectedIndex = DoListColumn(listView, columnElements, listViewIndex, selectedIndex,
                        listViewIndex == 0 ? selectedSearchItemFunction : null, selectedRegularItemFunction,
                        getDataForDraggingFunction);

                if (Event.current.type == EventType.Layout && m_ColumnToFocusKeyboard == listViewIndex)
                {
                    m_ColumnToFocusKeyboard = -1;
                    GUIUtility.keyboardControl = listView.ID;
                    if (listView.row == -1 && columnElements.Count != 0)
                        selectedIndex = listView.row = 0;
                }

                if (listViewIndex == 0)
                {
                    // pass some of the keys to the list view, even if something else is active
                    if (isSearching)
                    {
                        var keyCode = StealImportantListviewKeys();
                        if (keyCode != KeyCode.None)
                            ListViewShared.SendKey(m_ListViewStates[0], keyCode);
                    }

                    m_SearchText = EditorGUILayout.ToolbarSearchField(m_SearchText);
                    GUILayout.EndVertical();
                }

                if (selectedIndex >= columnElements.Count)
                    selectedIndex = -1;

                if (Event.current.type == EventType.Layout && m_CachedSelectedIndices[listViewIndex] != selectedIndex &&
                    m_ListViewStates.Count > listViewIndex + 1)
                {
                    int from = listViewIndex + 1;
                    int range = m_ListViewStates.Count - (listViewIndex + 1);
                    m_ListViewStates.RemoveRange(from, range);
                    m_CachedSelectedIndices.RemoveRange(from, range);
                }

                m_CachedSelectedIndices[listViewIndex] = selectedIndex;

                selectedObject = selectedIndex > -1 ? columnElements[selectedIndex].value : null;

                columnElements = selectedObject as List<ColumnViewElement>;
                listViewIndex++;
            }
            while (columnElements != null);

            for (; listViewIndex < minimumNumberOfColumns; listViewIndex++)
            {
                DoDummyColumn();
            }

            DoPreviewColumn(selectedObject, previewColumnFunction);

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }

        private static void DoItemSelectedEvent(ObjectColumnFunction selectedRegularItemFunction, object value)
        {
            if (selectedRegularItemFunction != null)
                selectedRegularItemFunction(value);
            Event.current.Use();
        }

        private void DoSearchItemSelectedEvent(ObjectColumnFunction selectedSearchItemFunction, object value)
        {
            m_SearchText = string.Empty;
            DoItemSelectedEvent(selectedSearchItemFunction, value);
        }

        private void DoDummyColumn()
        {
            // TODO: Don't know why this width needs one more pixel to match the list views. Bug in ScrollViews?
            GUILayout.Box(GUIContent.none, s_Styles.background, GUILayout.Width(columnWidth + 1));
        }

        private static void DoPreviewColumn(object selectedObject, ObjectColumnFunction previewColumnFunction)
        {
            GUILayout.BeginVertical(s_Styles.background);
            if (previewColumnFunction != null)
                previewColumnFunction(selectedObject);
            GUILayout.EndVertical();
        }

        private int DoListColumn(ListViewState listView, List<ColumnViewElement> columnViewElements, int columnIndex, int selectedIndex, ObjectColumnFunction selectedSearchItemFunction, ObjectColumnFunction selectedRegularItemFunction, ObjectColumnGetDataFunction getDataForDraggingFunction)
        {
            if (Event.current.type == EventType.KeyDown &&
                Event.current.keyCode == KeyCode.Return && listView.row > -1)
            {
                if (isSearching && selectedSearchItemFunction != null)
                    DoSearchItemSelectedEvent(selectedSearchItemFunction, columnViewElements[selectedIndex].value);
                if (!isSearching && GUIUtility.keyboardControl == listView.ID && selectedRegularItemFunction != null)
                    DoItemSelectedEvent(selectedRegularItemFunction, columnViewElements[selectedIndex].value);
            }

            if (GUIUtility.keyboardControl == listView.ID && Event.current.type == EventType.KeyDown && !isSearching)
            {
                switch (Event.current.keyCode)
                {
                    case KeyCode.LeftArrow:
                        m_ColumnToFocusKeyboard = columnIndex - 1;
                        Event.current.Use();
                        break;
                    case KeyCode.RightArrow:
                        m_ColumnToFocusKeyboard = columnIndex + 1;
                        Event.current.Use();
                        break;
                }
            }

            foreach (ListViewElement element in ListViewGUILayout.ListView(listView, s_Styles.background, GUILayout.Width(columnWidth)))
            {
                var columnViewElement = columnViewElements[element.row];

                if (element.row == listView.row)
                {
                    if (Event.current.type == EventType.Repaint)
                    {
                        // FIXME: One Pixel offset is required when using OL Box style. Use a different style.
                        var rect = element.position;
                        rect.x++;
                        rect.y++;

                        s_Styles.selected.Draw(rect, false, true, true, GUIUtility.keyboardControl == listView.ID);
                    }
                }

                GUILayout.Label(columnViewElement.name);

                if (columnViewElement.value is List<ColumnViewElement>)
                {
                    // TODO: Make a proper style.
                    var arrowRect = element.position;
                    arrowRect.x = arrowRect.xMax - s_Styles.categoryArrowIcon.width - 5;
                    arrowRect.y += 2;
                    GUI.Label(arrowRect, s_Styles.categoryArrowIcon);
                }

                DoDoubleClick(element, columnViewElement, selectedSearchItemFunction, selectedRegularItemFunction);
                DoDragAndDrop(listView, element, columnViewElements, getDataForDraggingFunction);
            }

            if (Event.current.type == EventType.Layout)
                selectedIndex = listView.row;

            return selectedIndex;
        }

        private static void DoDragAndDrop(ListViewState listView, ListViewElement element, List<ColumnViewElement> columnViewElements, ObjectColumnGetDataFunction getDataForDraggingFunction)
        {
            if (GUIUtility.hotControl == listView.ID && Event.current.type == EventType.MouseDown &&
                element.position.Contains(Event.current.mousePosition) &&
                Event.current.button == 0)
            {
                var delay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), listView.ID);
                delay.mouseDownPosition = Event.current.mousePosition;
            }

            if (GUIUtility.hotControl == listView.ID &&
                Event.current.type == EventType.MouseDrag &&
                GUIClip.visibleRect.Contains(Event.current.mousePosition))
            {
                var delay = (DragAndDropDelay)GUIUtility.GetStateObject(typeof(DragAndDropDelay), listView.ID);

                if (delay.CanStartDrag())
                {
                    var data = getDataForDraggingFunction == null ? null :
                        getDataForDraggingFunction(columnViewElements[listView.row].value);

                    if (data == null)
                        return;

                    DragAndDrop.PrepareStartDrag();

                    DragAndDrop.paths = null;

                    DragAndDrop.SetGenericData("CustomDragData", data);
                    DragAndDrop.StartDrag(columnViewElements[listView.row].name);

                    Event.current.Use();
                }
            }
        }

        private void DoDoubleClick(ListViewElement element, ColumnViewElement columnViewElement, ObjectColumnFunction selectedSearchItemFunction, ObjectColumnFunction selectedRegularItemFunction)
        {
            if (Event.current.type == EventType.MouseDown &&
                element.position.Contains(Event.current.mousePosition) &&
                Event.current.button == 0 && Event.current.clickCount == 2)
            {
                if (isSearching)
                    DoSearchItemSelectedEvent(selectedSearchItemFunction, columnViewElement.value);
                else
                    DoItemSelectedEvent(selectedRegularItemFunction, columnViewElement.value);
            }
        }

        private static KeyCode StealImportantListviewKeys()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                var key = Event.current.keyCode;
                if (key == KeyCode.UpArrow || key == KeyCode.DownArrow || key == KeyCode.PageUp || key == KeyCode.PageDown)
                {
                    Event.current.Use();
                    return key;
                }
            }

            return KeyCode.None;
        }
    }
}
