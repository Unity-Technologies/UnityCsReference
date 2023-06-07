// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.AI;

namespace UnityEditor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(OffMeshLink))]
    [Obsolete("OffMeshLink has been deprecated and replaced by NavMeshLink.")]
    internal class OffMeshLinkInspector : Editor
    {
        private SerializedProperty m_AreaIndex;
        private SerializedProperty m_Start;
        private SerializedProperty m_End;
        private SerializedProperty m_CostOverride;
        private SerializedProperty m_BiDirectional;
        private SerializedProperty m_Activated;
        private SerializedProperty m_AutoUpdatePositions;

        static class Styles
        {
            public static readonly GUIContent Start = EditorGUIUtility.TrTextContent("Start", "The transform representing the start position of the link.");
            public static readonly GUIContent End = EditorGUIUtility.TrTextContent("End", "The transform representing the end position of the link.");
            public static readonly GUIContent CostOverride = EditorGUIUtility.TrTextContent("Cost Override", "A positive value here modifies the cost of the link that is normally given by Navigation Area.");
            public static readonly GUIContent BiDirectional = EditorGUIUtility.TrTextContent("Bidirectional", "When selected, agents can traverse the link also from End to Start, otherwise only from Start to End.");
            public static readonly GUIContent Activated = EditorGUIUtility.TrTextContent("Activated", "Makes the link available for pathfinding.");
            public static readonly GUIContent AutoUpdatePositions = EditorGUIUtility.TrTextContent("Auto Update Positions", "Automatically update the link's endpoints to match the positions of the Start and End transforms.");
            public static readonly GUIContent NavigationArea = EditorGUIUtility.TrTextContent("Navigation Area", "It assigns a specific cost to the link. Only NavMeshAgents with this area type in their Area Mask are allowed to pass through it.");
        }

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

            EditorGUILayout.PropertyField(m_Start, Styles.Start);
            EditorGUILayout.PropertyField(m_End, Styles.End);
            EditorGUILayout.PropertyField(m_CostOverride, Styles.CostOverride);
            EditorGUILayout.PropertyField(m_BiDirectional, Styles.BiDirectional);
            EditorGUILayout.PropertyField(m_Activated, Styles.Activated);
            EditorGUILayout.PropertyField(m_AutoUpdatePositions, Styles.AutoUpdatePositions);

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

            var area = EditorGUILayout.Popup(Styles.NavigationArea, areaIndex, areaNames);
            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                var newAreaIndex = GameObjectUtility.GetNavMeshAreaFromName(areaNames[area]);
                m_AreaIndex.intValue = newAreaIndex;
            }
        }
    }
}
