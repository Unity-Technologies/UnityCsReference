// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    internal class LightingExplorerWindowTab
    {
        SerializedPropertyTable m_LightTable;

        public LightingExplorerWindowTab(SerializedPropertyTable lightTable)
        {
            m_LightTable = lightTable;
        }

        public void OnEnable()
        {
            if (m_LightTable != null)
                m_LightTable.OnEnable();
        }

        public void OnDisable()
        {
            if (m_LightTable != null)
                m_LightTable.OnDisable();
        }

        public void OnInspectorUpdate()
        {
            if (m_LightTable != null)
                m_LightTable.OnInspectorUpdate();
        }

        public void OnSelectionChange(int[] instanceIDs)
        {
            if (m_LightTable != null)
                m_LightTable.OnSelectionChange(instanceIDs);
        }

        public void OnSelectionChange()
        {
            if (m_LightTable != null)
                m_LightTable.OnSelectionChange();
        }

        public void OnHierarchyChange()
        {
            if (m_LightTable != null)
                m_LightTable.OnHierarchyChange();
        }

        public void OnGUI()
        {
            EditorGUI.indentLevel += 1;

            int cur_indent = EditorGUI.indentLevel;
            float cur_indent_px = EditorGUI.indent;
            EditorGUI.indentLevel = 0;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(cur_indent_px);

            using (new EditorGUILayout.VerticalScope())
            {
                if (m_LightTable != null)
                    m_LightTable.OnGUI();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            EditorGUI.indentLevel = cur_indent;

            EditorGUI.indentLevel -= 1;
        }
    }
}
