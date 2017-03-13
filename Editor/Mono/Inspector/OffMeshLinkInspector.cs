// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.AI;


namespace UnityEditor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(OffMeshLink))]
    internal class OffMeshLinkInspector : Editor
    {
        private SerializedProperty m_AreaIndex;
        private SerializedProperty m_Start;
        private SerializedProperty m_End;
        private SerializedProperty m_CostOverride;
        private SerializedProperty m_BiDirectional;
        private SerializedProperty m_Activated;
        private SerializedProperty m_AutoUpdatePositions;

        void OnEnable()
        {
            m_AreaIndex = serializedObject.FindProperty("m_AreaIndex");
            m_Start = serializedObject.FindProperty("m_Start");
            m_End = serializedObject.FindProperty("m_End");
            m_CostOverride = serializedObject.FindProperty("m_CostOverride");
            m_BiDirectional = serializedObject.FindProperty("m_BiDirectional");
            m_Activated = serializedObject.FindProperty("m_Activated");
            m_AutoUpdatePositions = serializedObject.FindProperty("m_AutoUpdatePositions");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Start);
            EditorGUILayout.PropertyField(m_End);
            EditorGUILayout.PropertyField(m_CostOverride);
            EditorGUILayout.PropertyField(m_BiDirectional);
            EditorGUILayout.PropertyField(m_Activated);
            EditorGUILayout.PropertyField(m_AutoUpdatePositions);

            SelectNavMeshArea();

            serializedObject.ApplyModifiedProperties();
        }

        private void SelectNavMeshArea()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = m_AreaIndex.hasMultipleDifferentValues;
            var areaNames = GameObjectUtility.GetNavMeshAreaNames();
            var currentAbsoluteIndex = m_AreaIndex.intValue;
            var areaIndex = -1;

            //Need to find the index as the list of names will compress out empty layers
            for (var i = 0; i < areaNames.Length; i++)
            {
                if (GameObjectUtility.GetNavMeshAreaFromName(areaNames[i]) == currentAbsoluteIndex)
                {
                    areaIndex = i;
                    break;
                }
            }

            var area = EditorGUILayout.Popup("Navigation Area", areaIndex, areaNames);
            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                var newAreaIndex = GameObjectUtility.GetNavMeshAreaFromName(areaNames[area]);
                m_AreaIndex.intValue = newAreaIndex;
            }
        }
    }
}
