// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.Search
{
    class SearchQueryColumnHeader : MultiColumnHeader
    {
        static class InnerStyles
        {
            public static GUIContent createContent = EditorGUIUtility.IconContent("CreateAddNew");
            public static GUIStyle toolbarCreateAddNewDropDown = new GUIStyle("ToolbarCreateAddNewDropDown")
            {
                fixedWidth = 32f,
                fixedHeight = 0,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(4, 4, 4, 4)
            };
        }


        public SearchQueryColumnHeader()
            : base(new MultiColumnHeaderState(CreateColumns().ToArray()))
        {
            height = 22f;
            canSort = true;
        }

        private static IEnumerable<MultiColumnHeaderState.Column> CreateColumns()
        {
            yield return CreateColumn("", 200f, autoResize: true, canSort: false, toggleVisibility: false);
            yield return CreateColumn("", 80f);
        }

        static MultiColumnHeaderState.Column CreateColumn(string label, float width = 32f, bool autoResize = true, bool canSort = true, bool toggleVisibility = true)
        {
            return new MultiColumnHeaderState.Column()
            {
                width = width,
                headerContent = new GUIContent(label),
                autoResize = autoResize,
                canSort = canSort,
                sortedAscending = true,
                allowToggleVisibility = toggleVisibility,
                headerTextAlignment = TextAlignment.Left,
                sortingArrowAlignment = TextAlignment.Right,
                minWidth = 32f,
                maxWidth = 1000000f,
                contextMenuText = null
            };
        }

        public override void OnGUI(Rect columnHeaderRect, float xScroll)
        {
            if (Event.current.type == EventType.Repaint)
                EditorStyles.toolbar.Draw(columnHeaderRect, GUIContent.none, 0);
            columnHeaderRect.yMax -= 1;
            base.OnGUI(columnHeaderRect, xScroll);
        }

        protected override void ColumnHeaderClicked(MultiColumnHeaderState.Column column, int columnIndex)
        {
            if (columnIndex == 0)
                return;

            base.ColumnHeaderClicked(column, columnIndex);
        }

        protected override void ColumnHeaderGUI(MultiColumnHeaderState.Column column, Rect headerRect, int columnIndex)
        {
            if (columnIndex == 0)
                DrawSearchField(headerRect);
            else
                base.ColumnHeaderGUI(column, headerRect, columnIndex);
        }

        void DrawSearchField(Rect columnHeaderRect)
        {
            var buttonStackRect = HandleButtons(columnHeaderRect);

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.None && Event.current.character == '\r')
                return;

            columnHeaderRect.xMin = buttonStackRect.xMax + 2f;
            var searchTextRect = columnHeaderRect;
            searchTextRect = EditorStyles.toolbarSearchField.margin.Remove(searchTextRect);
            searchTextRect.xMax -= 2f;
            searchTextRect.y += Mathf.Round((columnHeaderRect.height - searchTextRect.height) / 2f - 2f);

            var hashForSearchField = "CollectionsSearchField".GetHashCode();
            var searchFieldControlID = GUIUtility.GetControlID(hashForSearchField, FocusType.Passive, searchTextRect);

            DrawButtons(buttonStackRect);
        }

        private Rect HandleButtons(Rect columnHeaderRect)
        {
            Rect rect = columnHeaderRect;
            rect = InnerStyles.toolbarCreateAddNewDropDown.margin.Remove(rect);
            rect.xMax = rect.xMin + InnerStyles.toolbarCreateAddNewDropDown.fixedWidth;
            rect.y += (columnHeaderRect.height - rect.height) / 2f - 5f;

            bool mouseOver = rect.Contains(Event.current.mousePosition);
            if (Event.current.type == EventType.MouseDown && mouseOver)
            {
                GUIUtility.hotControl = 0;

                GenericMenu menu = new GenericMenu();
                // searchView.AddCollectionMenus(menu);

                menu.ShowAsContext();
                Event.current.Use();
            }

            return rect;
        }

        void DrawButtons(Rect buttonStackRect)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            bool mouseOver = buttonStackRect.Contains(Event.current.mousePosition);
            InnerStyles.toolbarCreateAddNewDropDown.Draw(buttonStackRect, InnerStyles.createContent, mouseOver, false, false, false);
        }
    }
}
