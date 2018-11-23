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
        }

        private Vector2 m_IconSize = new Vector2(16, 16);
        private Action<NewScriptDropdownItem> m_OnCreateNewScript;

        internal override Vector2 iconSize => m_IconSize;
        internal override GUIStyle lineStyle => Styles.itemStyle;

        public AddComponentGUI(AdvancedDropdownDataSource dataSource, Action<NewScriptDropdownItem> onCreateNewScript) : base(dataSource)
        {
            m_OnCreateNewScript = onCreateNewScript;
        }

        internal override void DrawItem(AdvancedDropdownItem item, string name, Texture2D icon, bool enabled, bool drawArrow, bool selected, bool hasSearch)
        {
            var newScriptItem = item as NewScriptDropdownItem;
            if (newScriptItem == null)
            {
                if (hasSearch && item is ComponentDropdownItem)
                {
                    name = ((ComponentDropdownItem)item).searchableName;
                }
                base.DrawItem(item, name, icon, enabled, drawArrow, selected, hasSearch);
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

        internal override string DrawSearchFieldControl(string searchString)
        {
            float padding = 8f;
            m_SearchRect = GUILayoutUtility.GetRect(0, 0);
            m_SearchRect.x += padding;
            m_SearchRect.y = 7;
            m_SearchRect.width -= padding * 2;
            m_SearchRect.height = 30;
            var newSearch = EditorGUI.SearchField(m_SearchRect, searchString);
            return newSearch;
        }
    }
}
