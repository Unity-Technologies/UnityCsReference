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

        private static class Styles
        {
            public static readonly GUIContent AgentSteeringHeader = EditorGUIUtility.TrTextContent("Steering");
            public static readonly GUIContent AgentAvoidanceHeader = EditorGUIUtility.TrTextContent("Obstacle Avoidance");
            public static readonly GUIContent AgentPathFindingHeader = EditorGUIUtility.TrTextContent("Path Finding");
            public static readonly GUIContent AgentType = EditorGUIUtility.TrTextContent("Agent Type", "The agent characteristics for which a NavMesh has been built.");
            public static readonly GUIContent BaseOffset = EditorGUIUtility.TrTextContent("Base Offset", "The relative vertical displacement of the owning GameObject.");
            public static readonly GUIContent Speed = EditorGUIUtility.TrTextContent("Speed", "Maximum movement speed when following a path.");
            public static readonly GUIContent AngularSpeed = EditorGUIUtility.TrTextContent("Angular Speed", "Maximum turning speed in (deg/s) while following a path.");
            public static readonly GUIContent Acceleration = EditorGUIUtility.TrTextContent("Acceleration", "The maximum acceleration of an agent as it follows a path, given in units / sec^2.");
            public static readonly GUIContent StoppingDistance = EditorGUIUtility.TrTextContent("Stopping Distance", "Stop within this distance from the target position.");
            public static readonly GUIContent AutoBraking = EditorGUIUtility.TrTextContent("Auto Braking", "The agent will avoid overshooting the destination point by slowing down in time.");
            public static readonly GUIContent Radius = EditorGUIUtility.TrTextContent("Radius", "The minimum distance to keep clear between the center of this agent and any other agents or obstacles nearby.");
            public static readonly GUIContent Height = EditorGUIUtility.TrTextContent("Height", "The height of the agent for purposes of passing under obstacles.");
            public static readonly GUIContent Quality = EditorGUIUtility.TrTextContent("Quality", "Higher quality avoidance reduces more the chance of agents overlapping but it is slower to compute than lower quality avoidance.");
            public static readonly GUIContent Priority = EditorGUIUtility.TrTextContent("Priority", "This agent will ignore all other agents for which this number is higher. A lower value implies higher importance.");
            public static readonly GUIContent AutoTraverseOffMeshLink = EditorGUIUtility.TrTextContent("Auto Traverse Off Mesh Link", "The agent moves across Off Mesh Links automatically.");
            public static readonly GUIContent AutoRepath = EditorGUIUtility.TrTextContent("Auto Repath", "The agent will attempt to acquire a new path if the existing path becomes invalid.");
            public static readonly GUIContent AreaMask = EditorGUIUtility.TrTextContent("Area Mask", "The agent plans a path and moves only through the selected NavMesh area types.");
        }

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
            serializedObject.Update();

            AgentTypePopupInternal(m_AgentTypeID);
            EditorGUILayout.PropertyField(m_BaseOffset, Styles.BaseOffset);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.AgentSteeringHeader, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_Speed, Styles.Speed);
            EditorGUILayout.PropertyField(m_AngularSpeed, Styles.AngularSpeed);
            EditorGUILayout.PropertyField(m_Acceleration, Styles.Acceleration);
            EditorGUILayout.PropertyField(m_StoppingDistance, Styles.StoppingDistance);
            EditorGUILayout.PropertyField(m_AutoBraking, Styles.AutoBraking);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.AgentAvoidanceHeader, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_Radius, Styles.Radius);
            EditorGUILayout.PropertyField(m_Height, Styles.Height);
            EditorGUILayout.PropertyField(m_ObstacleAvoidanceType, Styles.Quality);
            EditorGUILayout.PropertyField(m_AvoidancePriority, Styles.Priority);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Styles.AgentPathFindingHeader, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_AutoTraverseOffMeshLink, Styles.AutoTraverseOffMeshLink);
            EditorGUILayout.PropertyField(m_AutoRepath, Styles.AutoRepath);

            //Initially needed data
            var areaNames = NavMesh.GetAreaNames();
            var currentMask = m_WalkableMask.longValue;
            var compressedMask = 0;

            if (currentMask == 0xffffffff)
            {
                compressedMask = ~0;
            }
            else
            {
                //Need to find the index as the list of names will compress out empty areas
                for (var i = 0; i < areaNames.Length; i++)
                {
                    var areaIndex = NavMesh.GetAreaFromName(areaNames[i]);
                    if (((1 << areaIndex) & currentMask) != 0)
                        compressedMask = compressedMask | (1 << i);
                }
            }

            var position = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(position, GUIContent.none, m_WalkableMask);

            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = m_WalkableMask.hasMultipleDifferentValues;
            var areaMask = EditorGUI.MaskField(position, Styles.AreaMask, compressedMask, areaNames, EditorStyles.layerMaskField);
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
                            newMask = newMask | (uint)(1 << NavMesh.GetAreaFromName(areaNames[i]));
                        }
                    }
                    m_WalkableMask.longValue = newMask;
                }
            }
            EditorGUI.EndProperty();

            serializedObject.ApplyModifiedProperties();
        }

        private static void AgentTypePopupInternal(SerializedProperty agentTypeID)
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
            index = EditorGUI.Popup(rect, Styles.AgentType, index, agentTypeNames);
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
