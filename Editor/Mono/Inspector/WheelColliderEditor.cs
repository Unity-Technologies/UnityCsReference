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
            EditorGUILayout.PropertyField(m_SuspensionDistance);
            EditorGUILayout.PropertyField(m_ForceAppPointDistance);
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_Center);
            EditorGUILayout.Space();
            StructPropertyGUILayout.GenericStruct(m_SuspensionSpring);
            StructPropertyGUILayout.GenericStruct(m_ForwardFriction);
            StructPropertyGUILayout.GenericStruct(m_SidewaysFriction);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
