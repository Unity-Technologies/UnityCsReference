// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(WheelCollider))]
    [CanEditMultipleObjects]
    internal class WheelColliderEditor : Editor
    {
        SerializedProperty m_Center;
        SerializedProperty m_Radius;
        SerializedProperty m_SuspensionDistance;
        SerializedProperty m_SuspensionSpring;
        SerializedProperty m_ForceAppPointDistance;
        SerializedProperty m_Mass;
        SerializedProperty m_WheelDampingRate;
        SerializedProperty m_ForwardFriction;
        SerializedProperty m_SidewaysFriction;

        private static class Styles
        {
            public static readonly GUIContent forceAppPointDistanceContent = EditorGUIUtility.TrTextContent("Force App Point Distance", "The point where the wheel forces are applied");
            public static readonly GUIContent centerContent = EditorGUIUtility.TrTextContent("Center", "The position of the Collider in the GameObject's local space.");
            public static readonly GUIContent suspensionDistanceContent = EditorGUIUtility.TrTextContent("Suspension Distance", "Maximum extension distance of wheel suspension, measured in local space. Suspension always extends downwards through the local Y-axis.");
            public static readonly GUIContent suspensionSpringContent = EditorGUIUtility.TrTextContent("Suspension Spring", "The suspension attempts to reach a Target Position by adding spring and damping forces.");
            public static readonly GUIContent forwardFrictionContent = EditorGUIUtility.TrTextContent("Forward Friction", "Tire friction properties when the wheel is rolling forward.");
            public static readonly GUIContent sidewaysFrictionContent = EditorGUIUtility.TrTextContent("Sideways Friction", "Tire friction properties when the wheel is rolling sideways.");
        }

        public void OnEnable()
        {
            // Wheel Collider does not serialize Collider properties, so we don't use base OnEnable like other collider types
            m_Center = serializedObject.FindProperty("m_Center");
            m_Radius = serializedObject.FindProperty("m_Radius");
            m_SuspensionDistance = serializedObject.FindProperty("m_SuspensionDistance");
            m_SuspensionSpring = serializedObject.FindProperty("m_SuspensionSpring");
            m_Mass = serializedObject.FindProperty("m_Mass");
            m_ForceAppPointDistance = serializedObject.FindProperty("m_ForceAppPointDistance");
            m_WheelDampingRate = serializedObject.FindProperty("m_WheelDampingRate");
            m_ForwardFriction = serializedObject.FindProperty("m_ForwardFriction");
            m_SidewaysFriction = serializedObject.FindProperty("m_SidewaysFriction");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Mass);
            EditorGUILayout.PropertyField(m_Radius);
            EditorGUILayout.PropertyField(m_WheelDampingRate);
            EditorGUILayout.PropertyField(m_SuspensionDistance, Styles.suspensionDistanceContent);
            EditorGUILayout.PropertyField(m_ForceAppPointDistance, Styles.forceAppPointDistanceContent);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_Center, Styles.centerContent);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_SuspensionSpring, Styles.suspensionSpringContent);
            EditorGUILayout.PropertyField(m_ForwardFriction, Styles.forwardFrictionContent);
            EditorGUILayout.PropertyField(m_SidewaysFriction, Styles.sidewaysFrictionContent);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
