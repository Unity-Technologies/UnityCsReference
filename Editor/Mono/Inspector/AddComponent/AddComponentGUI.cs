// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEditor.AddComponent
{
    internal class AddComponentGUI : AdvancedDropdownGUI
    {
        private static class Styles
        {
            public static GUIStyle itemStyle = "DD LargeItemStyle";
            internal static GUIStyle lineStyleFaint = new GUIStyle("DD LargeItemStyle");

            static Styles()
            {
                float val = EditorGUIUtility.isProSkin ? 0.5f : 0.25f;
                var color = new Color(val, val, val, 1f);
                lineStyleFaint.active.textColor = color;
                lineStyleFaint.focused.textColor = color;
                lineStyleFaint.hover.textColor = color;
                lineStyleFaint.normal.textColor = color;
            }
        }

        private Vector2 m_IconSize = new Vector2(16, 16);
        private Action<NewScriptDropdownItem> m_OnCreateNewScript;

        internal override Vector2 iconSize => m_IconSize;
        internal override GUIStyle lineStyle => Styles.itemStyle;

        public AddComponentGUI(AdvancedDropdownDataSource dataSource, Action<NewScriptDropdownItem> onCreateNewScript) : base(dataSource)
        {
            m_OnCreateNewScript = onCreateNewScript;
        }

        private void DrawSearchItem(string name, string path, Texture2D icon, bool selected)
        {
            path = string.IsNullOrEmpty(path) ? "" : $"({path})"; // if the path we're left with is equal to " ()" - scrap it
            var contentWithIcon = new GUIContent(name, path);
            contentWithIcon.image = icon;

            var rect = GUILayoutUtility.GetRect(contentWithIcon, Styles.lineStyleFaint, GUILayout.ExpandWidth(true));
            var fullRect = new Rect(rect);

            if (Event.current.type != EventType.Repaint)
                return;

            var emptySpace = Styles.lineStyleFaint.CalcSize(contentWithIcon);
            rect.x += emptySpace.x;

            lineStyle.Draw(fullRect, contentWithIcon, selected, selected, selected, selected);

            Styles.lineStyleFaint.Draw(rect, new GUIContent(path, path), selected, false, false, false);
        }

        internal override void DrawItem(AdvancedDropdownItem item, string name, Texture2D icon, bool enabled, bool drawArrow, bool selected, bool hasSearch)
        {
            bool isScript = false;
            var newScriptItem = item as NewScriptDropdownItem;
            if (newScriptItem == null)
            {
                string namespaceName = "";
                if (hasSearch && item is ComponentDropdownItem)
                {
                    var componentItem = item as ComponentDropdownItem;
                    // null check doesn't work here so comparing against "New script"
                    if (!componentItem.displayName.Equals("New script") && componentItem.menuPath.StartsWith("Component/Scripts/"))
                    {
                        namespaceName = componentItem.menuPath.Substring(AddComponentDataSource.kScriptHeader.Length);
                        var last = namespaceName.LastIndexOf("/");
                        namespaceName = last != -1 ? namespaceName.Substring(0, last) : "";
                        isScript = true;
                    }
                    else
                        name = ((ComponentDropdownItem)item).searchableNameLocalized;
                }

                if (string.IsNullOrEmpty(namespaceName))
                    base.DrawItem(item, name, icon, enabled, drawArrow, selected, hasSearch);
                else
                    DrawSearchItem(name, namespaceName, icon, selected);

                // dummy label for easy tooltips
                // this is to allow viewing full script names in cases where they are cut off
                if (Event.current.type == EventType.Repaint && isScript)
                {
                    var text = string.IsNullOrEmpty(namespaceName) ? name : $"{name} ({namespaceName})";
                    var tooltipRect = GUILayoutUtility.GetLastRect();
                    GUI.Label(tooltipRect, new GUIContent("", text));
                }

                return;
            }

            GUILayout.Label(L10n.Tr("Name"), EditorStyles.label);

            EditorGUI.FocusTextInControl("NewScriptName");
            GUI.SetNextControlName("NewScriptName");

            newScriptItem.m_ClassName = EditorGUILayout.TextField(newScriptItem.m_ClassName);

            EditorGUILayout.Space();

            var canCreate = newScriptItem.CanCreate();
            if (!canCreate && newScriptItem.m_ClassName != "")
                GUILayout.Label(newScriptItem.GetError(), EditorStyles.helpBox);

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(!canCreate))
            {
                if (GUILayout.Button(L10n.Tr("Create and Add")))
                {
                    m_OnCreateNewScript(newScriptItem);
                }
            }

            EditorGUILayout.Space();
        }
    }
}
