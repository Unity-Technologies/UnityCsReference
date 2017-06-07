// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.AI;


namespace UnityEditor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NavMeshAgent))]
    internal class NavMeshAgentInspector : Editor
    {
        private SerializedProperty m_AgentTypeID;
        private SerializedProperty m_Radius;
        private SerializedProperty m_Height;
        private SerializedProperty m_WalkableMask;
        private SerializedProperty m_Speed;
        private SerializedProperty m_Acceleration;
        private SerializedProperty m_AngularSpeed;
        private SerializedProperty m_StoppingDistance;
        private SerializedProperty m_AutoTraverseOffMeshLink;
        private SerializedProperty m_AutoBraking;
        private SerializedProperty m_AutoRepath;
        private SerializedProperty m_BaseOffset;
        private SerializedProperty m_ObstacleAvoidanceType;
        private SerializedProperty m_AvoidancePriority;

        private class Styles
        {
            public readonly GUIContent m_AgentSteeringHeader = new GUIContent("Steering");
            public readonly GUIContent m_AgentAvoidanceHeader = new GUIContent("Obstacle Avoidance");
            public readonly GUIContent m_AgentPathFindingHeader = new GUIContent("Path Finding");
        };

        static Styles s_Styles;

        void OnEnable()
        {
            m_AgentTypeID = serializedObject.FindProperty("m_AgentTypeID");
            m_Radius = serializedObject.FindProperty("m_Radius");
            m_Height = serializedObject.FindProperty("m_Height");
            m_WalkableMask = serializedObject.FindProperty("m_WalkableMask");
            m_Speed = serializedObject.FindProperty("m_Speed");
            m_Acceleration = serializedObject.FindProperty("m_Acceleration");
            m_AngularSpeed = serializedObject.FindProperty("m_AngularSpeed");
            m_StoppingDistance = serializedObject.FindProperty("m_StoppingDistance");
            m_AutoTraverseOffMeshLink = serializedObject.FindProperty("m_AutoTraverseOffMeshLink");
            m_AutoBraking = serializedObject.FindProperty("m_AutoBraking");
            m_AutoRepath = serializedObject.FindProperty("m_AutoRepath");
            m_BaseOffset = serializedObject.FindProperty("m_BaseOffset");
            m_ObstacleAvoidanceType = serializedObject.FindProperty("m_ObstacleAvoidanceType");
            m_AvoidancePriority = serializedObject.FindProperty("avoidancePriority");
        }

        public override void OnInspectorGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            serializedObject.Update();

            AgentTypePopupInternal("Agent Type", m_AgentTypeID);
            EditorGUILayout.PropertyField(m_BaseOffset);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_Styles.m_AgentSteeringHeader, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_Speed);
            EditorGUILayout.PropertyField(m_AngularSpeed);
            EditorGUILayout.PropertyField(m_Acceleration);
            EditorGUILayout.PropertyField(m_StoppingDistance);
            EditorGUILayout.PropertyField(m_AutoBraking);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_Styles.m_AgentAvoidanceHeader, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_Radius);
            EditorGUILayout.PropertyField(m_Height);
            EditorGUILayout.PropertyField(m_ObstacleAvoidanceType, GUIContent.Temp("Quality"));
            EditorGUILayout.PropertyField(m_AvoidancePriority, GUIContent.Temp("Priority"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_Styles.m_AgentPathFindingHeader, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_AutoTraverseOffMeshLink);
            EditorGUILayout.PropertyField(m_AutoRepath);

            //Initially needed data
            var areaNames = GameObjectUtility.GetNavMeshAreaNames();
            var currentMask = m_WalkableMask.longValue;
            var compressedMask = 0;

            //Need to find the index as the list of names will compress out empty areas
            for (var i = 0; i < areaNames.Length; i++)
            {
                var areaIndex = GameObjectUtility.GetNavMeshAreaFromName(areaNames[i]);
                if (((1 << areaIndex) & currentMask) != 0)
                    compressedMask = compressedMask | (1 << i);
            }

            //TODO: Refactor this to use the mask field that takes a label.
            const float kH = EditorGUI.kSingleLineHeight;
            var position = GUILayoutUtility.GetRect(EditorGUILayout.kLabelFloatMinW, EditorGUILayout.kLabelFloatMaxW, kH, kH, EditorStyles.layerMaskField);

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = m_WalkableMask.hasMultipleDifferentValues;
            var areaMask = EditorGUI.MaskField(position, "Area Mask", compressedMask, areaNames, EditorStyles.layerMaskField);
            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                if (areaMask == ~0)
                {
                    m_WalkableMask.longValue = 0xffffffff;
                }
                else
                {
                    uint newMask = 0;
                    for (var i = 0; i < areaNames.Length; i++)
                    {
                        //If the bit has been set in the compacted mask
                        if (((areaMask >> i) & 1) != 0)
                        {
                            //Find out the 'real' layer from the name, then set it in the new mask
                            newMask = newMask | (uint)(1 << GameObjectUtility.GetNavMeshAreaFromName(areaNames[i]));
                        }
                    }
                    m_WalkableMask.longValue = newMask;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void AgentTypePopupInternal(string labelName, SerializedProperty agentTypeID)
        {
            var index = -1;
            var count = NavMesh.GetSettingsCount();
            var agentTypeNames = new string[count + 2];
            for (var i = 0; i < count; i++)
            {
                var id = NavMesh.GetSettingsByIndex(i).agentTypeID;
                var name = NavMesh.GetSettingsNameFromID(id);
                agentTypeNames[i] = name;
                if (id == agentTypeID.intValue)
                    index = i;
            }
            agentTypeNames[count] = "";
            agentTypeNames[count + 1] = "Open Agent Settings...";

            bool validAgentType = index != -1;
            if (!validAgentType)
            {
                EditorGUILayout.HelpBox("Agent Type invalid.", MessageType.Warning);
            }

            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginProperty(rect, GUIContent.none, agentTypeID);

            EditorGUI.BeginChangeCheck();
            index = EditorGUI.Popup(rect, labelName, index, agentTypeNames);
            if (EditorGUI.EndChangeCheck())
            {
                if (index >= 0 && index < count)
                {
                    var id = NavMesh.GetSettingsByIndex(index).agentTypeID;
                    agentTypeID.intValue = id;
                }
                else if (index == count + 1)
                {
                    UnityEditor.AI.NavMeshEditorHelpers.OpenAgentSettings(-1);
                }
            }

            EditorGUI.EndProperty();
        }
    }
}
