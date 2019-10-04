// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.StyleSheets;
using UnityEngine;
using Event = UnityEngine.Event;

namespace UnityEditor.IMGUI.Controls
{
    internal class AdvancedDropdownGUI
    {
        private static class Styles
        {
            public static GUIStyle itemStyle = "DD ItemStyle";
            public static GUIStyle header = "DD HeaderStyle";
            public static GUIStyle checkMark = "DD ItemCheckmark";
            public static GUIStyle lineSeparator = "DefaultLineSeparator";
            public static GUIStyle rightArrow = "ArrowNavigationRight";
            public static GUIStyle leftArrow = "ArrowNavigationLeft";
            public static GUIStyle searchFieldStyle = new GUIStyle(EditorStyles.toolbarSearchField)
            {
                margin = new RectOffset(5, 4, 4, 5)
            };
            public static SVC<Color> searchBackgroundColor = new SVC<Color>("--theme-toolbar-background-color", Color.black);

            public static GUIContent checkMarkContent = new GUIContent("✔");
        }

        //This should ideally match line height
        private Vector2 s_IconSize = new Vector2(13, 13);
        private AdvancedDropdownDataSource m_DataSource;

        internal Rect m_SearchRect;
        internal Rect m_HeaderRect;

        internal virtual float searchHeight => m_SearchRect.height;
        internal virtual float headerHeight => m_HeaderRect.height;
        internal virtual GUIStyle lineStyle => Styles.itemStyle;
        internal virtual Vector2 iconSize => s_IconSize;
        internal AdvancedDropdownState state { get; set; }

        public AdvancedDropdownGUI(AdvancedDropdownDataSource dataSource)
        {
            m_DataSource = dataSource;
        }

        internal virtual void DrawItem(AdvancedDropdownItem item, string name, Texture2D icon, bool enabled, bool drawArrow, bool selected, bool hasSearch)
        {
            var content = GUIContent.Temp(name, icon);
            var imgTemp = content.image;
            //we need to pretend we have an icon to calculate proper width in case
            if (content.image == null)
                content.image = Texture2D.whiteTexture;
            var rect = GUILayoutUtility.GetRect(content, lineStyle, GUILayout.ExpandWidth(true));
            content.image = imgTemp;

            if (Event.current.type != EventType.Repaint)
                return;

            var imageTemp = content.image;
            if (m_DataSource.selectedIDs.Any() && m_DataSource.selectedIDs.Contains(item.id))
            {
                var checkMarkRect = new Rect(rect);
                checkMarkRect.width = iconSize.x + 1;
                Styles.checkMark.Draw(checkMarkRect, Styles.checkMarkContent, false, false, selected, selected);
                rect.x += iconSize.x + 1;
                rect.width -= iconSize.x + 1;

                //don't draw the icon if the check mark is present
                content.image = null;
            }
            else if (content.image == null)
            {
                lineStyle.Draw(rect, GUIContent.none, false, false, selected, selected);
                rect.x += iconSize.x + 1;
                rect.width -= iconSize.x + 1;
            }
            EditorGUI.BeginDisabled(!enabled);
            lineStyle.Draw(rect, content, false, false, selected, selected);
            content.image = imageTemp;
            if (drawArrow)
            {
                var yOffset = (lineStyle.fixedHeight - Styles.rightArrow.fixedHeight) / 2;
                Rect arrowRect = new Rect(
                    rect.xMax - Styles.rightArrow.fixedWidth - Styles.rightArrow.margin.right,
                    rect.y + yOffset,
                    Styles.rightArrow.fixedWidth,
                    Styles.rightArrow.fixedHeight);
                Styles.rightArrow.Draw(arrowRect, false, false, false, false);
            }
            EditorGUI.EndDisabled();
        }

        internal virtual void DrawLineSeparator()
        {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, Styles.lineSeparator, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint)
                return;
            Color orgColor = GUI.color;
            Color tintColor = (EditorGUIUtility.isProSkin) ? new Color(0.12f, 0.12f, 0.12f, 1.333f) : new Color(0.6f, 0.6f, 0.6f, 1.333f);
            GUI.color = GUI.color * tintColor;
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
            GUI.color = orgColor;
        }

        internal void DrawHeader(AdvancedDropdownItem group, Action backButtonPressed, bool hasParent)
        {
            var content = GUIContent.Temp(group.displayName, group.icon);
            m_HeaderRect = GUILayoutUtility.GetRect(content, Styles.header, GUILayout.ExpandWidth(true));
            bool hovered = m_HeaderRect.Contains(Event.current.mousePosition);

            if (Event.current.type == EventType.Repaint)
                Styles.header.Draw(m_HeaderRect, content, hovered, false, false, false);

            // Back button
            if (hasParent)
            {
                var yOffset = (m_HeaderRect.height - Styles.leftArrow.fixedWidth) / 2;
                var arrowRect = new Rect(
                    m_HeaderRect.x + Styles.leftArrow.margin.left,
                    m_HeaderRect.y + yOffset,
                    Styles.leftArrow.fixedWidth,
                    Styles.leftArrow.fixedHeight);
                if (Event.current.type == EventType.Repaint)
                    Styles.leftArrow.Draw(arrowRect, false, false, false, false);
                if (Event.current.type == EventType.MouseDown && hovered)
                {
                    backButtonPressed();
                    Event.current.Use();
                }
            }
        }

        internal void DrawSearchField(bool isSearchFieldDisabled, string searchString, Action<string> searchChanged)
        {
            if (!isSearchFieldDisabled)
            {
                EditorGUI.FocusTextInControl("ComponentSearch");
            }

            using (new EditorGUI.DisabledScope(isSearchFieldDisabled))
            {
                GUI.SetNextControlName("ComponentSearch");

                var newSearch = DrawSearchFieldControl(searchString);

                if (newSearch != searchString)
                {
                    searchChanged(newSearch);
                }
            }
        }

        internal virtual string DrawSearchFieldControl(string searchString)
        {
            const float kBorderWidth = 1f;
            var controlRect = GUILayoutUtility.GetRect(0, 0, Styles.searchFieldStyle);

            controlRect.height = Styles.searchFieldStyle.fixedHeight;
            controlRect.xMin += kBorderWidth;
            controlRect.xMax -= kBorderWidth;
            controlRect.yMin += kBorderWidth;
            m_SearchRect = Styles.searchFieldStyle.margin.Add(controlRect);
            EditorGUI.DrawRect(m_SearchRect, Styles.searchBackgroundColor);
            var newSearch = EditorGUI.ToolbarSearchField(controlRect, searchString, false);

            return newSearch;
        }

        internal Rect GetAnimRect(Rect position, float anim)
        {
            // Calculate rect for animated area
            var rect = new Rect(position);
            rect.x = position.x + position.width * anim;
            rect.y += searchHeight;
            rect.height -= searchHeight;
            return rect;
        }

        internal Vector2 CalculateContentSize(AdvancedDropdownDataSource dataSource)
        {
            float maxWidth = 0;
            float maxHeight = 0;
            bool includeArrow = false;
            float arrowWidth = Styles.rightArrow.fixedWidth;

            foreach (var child in dataSource.mainTree.children)
            {
                var content = GUIContent.Temp(child.name, child.icon);
                var a = lineStyle.CalcSize(content);
                a.x += iconSize.x + 1;

                if (maxWidth < a.x)
                {
                    maxWidth = a.x + 1;
                    includeArrow |= child.children.Any();
                }
                if (child.IsSeparator())
                {
                    maxHeight += Styles.lineSeparator.CalcHeight(content, maxWidth) + Styles.lineSeparator.margin.vertical;
                }
                else
                {
                    maxHeight += lineStyle.CalcHeight(content, maxWidth);
                }
            }
            if (includeArrow)
            {
                maxWidth += arrowWidth;
            }
            return new Vector2(maxWidth, maxHeight);
        }

        internal float GetSelectionHeight(AdvancedDropdownDataSource dataSource, Rect buttonRect)
        {
            if (state.GetSelectedIndex(dataSource.mainTree) == -1)
                return 0;
            float heigth = 0;
            for (int i = 0; i < dataSource.mainTree.children.Count(); i++)
            {
                var child = dataSource.mainTree.children.ElementAt(i);
                var content = GUIContent.Temp(child.name, child.icon);
                if (state.GetSelectedIndex(dataSource.mainTree) == i)
                {
                    var diff = (lineStyle.CalcHeight(content, 0) - buttonRect.height) / 2f;
                    return heigth + diff;
                }
                if (child.IsSeparator())
                {
                    heigth += Styles.lineSeparator.CalcHeight(content, 0) + Styles.lineSeparator.margin.vertical;
                }
                else
                {
                    heigth += lineStyle.CalcHeight(content, 0);
                }
            }
            return heigth;
        }
    }
}
