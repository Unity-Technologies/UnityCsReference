// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal class AddComponentGUI : AdvancedDropdownGUI
    {
        private Vector2 m_IconSize = new Vector2(16, 16);
        public override Vector2 iconSize => m_IconSize;

        public override void DrawItem(AdvancedDropdownItem item, bool selected, bool hasSearch)
        {
            var nsItem = item as NewScriptDropdownItem;
            if (nsItem == null)
            {
                base.DrawItem(item, selected, hasSearch);
                return;
            }

            GUILayout.Label("Name", EditorStyles.label);

            EditorGUI.FocusTextInControl("NewScriptName");
            GUI.SetNextControlName("NewScriptName");

            nsItem.m_ClassName = EditorGUILayout.TextField(nsItem.m_ClassName);

            EditorGUILayout.Space();

            EditorGUILayout.EnumPopup("Language", NewScriptDropdownItem.Language.CSharp);

            EditorGUILayout.Space();

            var canCreate = nsItem.CanCreate();
            if (!canCreate && nsItem.m_ClassName != "")
                GUILayout.Label(nsItem.GetError(), EditorStyles.helpBox);

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(!canCreate))
            {
                if (GUILayout.Button("Create and Add"))
                {
                    nsItem.Create(AddComponentWindow.s_AddComponentWindow.m_GameObjects);
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
