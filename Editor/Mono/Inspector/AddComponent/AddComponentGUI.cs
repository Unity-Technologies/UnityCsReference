// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor.AdvancedDropdown
{
    internal class AddComponentGUI : AdvancedDropdownGUI
    {
        private static class Styles
        {
            public static GUIStyle itemStyle = "DD LargeItemStyle";
        }

        private Vector2 m_IconSize = new Vector2(16, 16);

        public override Vector2 iconSize => m_IconSize;
        public override GUIStyle lineStyle => Styles.itemStyle;

        public AddComponentGUI(AdvancedDropdownDataSource dataSource) : base(dataSource)
        {
        }

        public override void DrawItem(AdvancedDropdownItem item, bool selected, bool hasSearch)
        {
            var newScriptItem = item as NewScriptDropdownItem;
            if (newScriptItem == null)
            {
                base.DrawItem(item, selected, hasSearch);
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
                    newScriptItem.Create(AddComponentWindow.s_AddComponentWindow.m_GameObjects);
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
