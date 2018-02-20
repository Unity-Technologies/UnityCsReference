// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using Event = UnityEngine.Event;

namespace UnityEditor
{
    internal class AdvancedDropdownGUI
    {
        protected static class Styles
        {
            public static GUIStyle header = new GUIStyle(EditorStyles.inspectorBig);
            public static GUIStyle rightArrow = "AC RightArrow";
            public static GUIStyle leftArrow = "AC LeftArrow";
            public static GUIStyle lineSeparator = new GUIStyle();

            static Styles()
            {
                header.font = EditorStyles.boldLabel.font;
                header.margin = new RectOffset(0, 0, 0, 0);

                lineSeparator.fixedHeight = 1;
                lineSeparator.margin.bottom = 2;
                lineSeparator.margin.top = 2;
            }
        }

        protected Rect m_SearchRect;
        public virtual float HeaderHeight => m_SearchRect.height;

        private const int kWindowHeight = 395 - 80;
        public virtual float WindowHeight => kWindowHeight;

        //This should ideally match line height
        private Vector2 s_IconSize = new Vector2(13, 13);
        public virtual Vector2 iconSize => s_IconSize;

        public virtual void DrawItem(AdvancedDropdownItem item, bool selected, bool hasSearch)
        {
            if (item.IsSeparator())
            {
                DrawLineSeparator();
                return;
            }

            var content = !hasSearch ? item.content : item.contentWhenSearching;
            var rect = GUILayoutUtility.GetRect(content, item.lineStyle, GUILayout.ExpandWidth(true));
            if (item.IsSeparator() || Event.current.type != EventType.Repaint)
                return;

            if (content.image == null)
            {
                item.lineStyle.Draw(rect, GUIContent.none, false, false, selected, selected);
                rect.x += iconSize.x;
                rect.width -= iconSize.x;
            }
            item.lineStyle.Draw(rect, content, false, false, selected, selected);
            if (item.drawArrow)
            {
                var size = Styles.rightArrow.lineHeight;
                var yOffset = item.lineStyle.fixedHeight / 2 - size / 2;
                Rect arrowRect = new Rect(rect.x + rect.width - size, rect.y + yOffset, size, size);
                Styles.rightArrow.Draw(arrowRect, false, false, false, false);
            }
        }

        protected virtual void DrawLineSeparator()
        {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, Styles.lineSeparator, GUILayout.ExpandWidth(true));
            if (Event.current.type != EventType.Repaint)
                return;
            EditorGUIUtility.DrawVerticalSplitter(rect);
        }

        public void DrawHeader(AdvancedDropdownItem group, Action backButtonPressed)
        {
            var content = GUIContent.Temp(group.name);
            var headerRect = GUILayoutUtility.GetRect(content, Styles.header, GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
                Styles.header.Draw(headerRect, content, false, false, false, false);

            // Back button
            if (group.parent != null)
            {
                var arrowSize = 13;
                var y = headerRect.y + (headerRect.height / 2 - arrowSize / 2);
                var arrowRect = new Rect(headerRect.x + 4, y, arrowSize, arrowSize);
                if (Event.current.type == EventType.Repaint)
                    Styles.leftArrow.Draw(arrowRect, false, false, false, false);
                if (Event.current.type == EventType.MouseDown && headerRect.Contains(Event.current.mousePosition))
                {
                    backButtonPressed();
                    Event.current.Use();
                }
            }
        }

        public void DrawSearchField(bool isSearchFieldDisabled, string searchString, Action<string> searchChanged)
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
            var paddingX = 8f;
            var paddingY = 2f;
            m_SearchRect = GUILayoutUtility.GetRect(0, 0);
            m_SearchRect.x += paddingX;
            m_SearchRect.y += paddingY;
            m_SearchRect.height = EditorStyles.toolbarSearchField.fixedHeight + paddingY * 2;
            // Adjust to the frame
            m_SearchRect.y += 1;
            m_SearchRect.height += 1;
            m_SearchRect.width -= paddingX * 2;
            var newSearch = EditorGUI.ToolbarSearchField(m_SearchRect, searchString, false);
            return newSearch;
        }

        public Rect GetAnimRect(Rect position, float anim)
        {
            // Calculate rect for animated area
            var rect = new Rect(position);
            rect.x = position.width * anim;
            rect.y = HeaderHeight;
            rect.height -= HeaderHeight;
            // Adjust to the frame
            rect.x += 1;
            rect.width -= 2;
            return rect;
        }
    }
}
