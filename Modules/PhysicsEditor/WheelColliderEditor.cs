// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor.AnimatedValues;

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

        private SerializedProperty m_LayerOverridePriority;
        private SerializedProperty m_IncludeLayers;
        private SerializedProperty m_ExcludeLayers;

        private readonly AnimBool m_ShowLayerOverrides = new AnimBool();
        private SavedBool m_ShowLayerOverridesFoldout;

        private WheelCollider m_WheelCollider;

        private static class Styles
        {
            public static readonly GUIContent forceAppPointDistanceContent = EditorGUIUtility.TrTextContent("Force App Point Distance", "The point where the wheel forces are applied");
            public static readonly GUIContent centerContent = EditorGUIUtility.TrTextContent("Center", "The position of the Collider in the GameObject's local space.");
            public static readonly GUIContent suspensionDistanceContent = EditorGUIUtility.TrTextContent("Suspension Distance", "Maximum extension distance of wheel suspension, measured in local space. Suspension always extends downwards through the local Y-axis.");
            public static readonly GUIContent suspensionSpringContent = EditorGUIUtility.TrTextContent("Suspension Spring", "The suspension attempts to reach a Target Position by adding spring and damping forces.");
            public static readonly GUIContent forwardFrictionContent = EditorGUIUtility.TrTextContent("Forward Friction", "Tire friction properties when the wheel is rolling forward.");
            public static readonly GUIContent sidewaysFrictionContent = EditorGUIUtility.TrTextContent("Sideways Friction", "Tire friction properties when the wheel is rolling sideways.");
            public static readonly GUIContent layerOverridePriority = EditorGUIUtility.TrTextContent("Layer Override Priority", "When 2 colliders have conflicting overrides, the settings of the collider with the higher priority are taken.");
            public static readonly GUIContent includeLayers = EditorGUIUtility.TrTextContent("Include Layers", "Layers to include when producing collisions");
            public static readonly GUIContent excludeLayers = EditorGUIUtility.TrTextContent("Exclude Layers", "Layers to exclude when producing collisions");
        }

        public void OnEnable()
        {
            m_WheelCollider = (WheelCollider)target;

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

            m_ShowLayerOverrides.valueChanged.AddListener(Repaint);
            m_ShowLayerOverridesFoldout = new SavedBool($"{target.GetType() }.ShowLayerOverridesFoldout", false);
            m_ShowLayerOverrides.value = m_ShowLayerOverridesFoldout.value;

            m_LayerOverridePriority = serializedObject.FindProperty("m_LayerOverridePriority");
            m_IncludeLayers = serializedObject.FindProperty("m_IncludeLayers");
            m_ExcludeLayers = serializedObject.FindProperty("m_ExcludeLayers");
        }

        public override void OnInspectorGUI()
        {
            if (!m_WheelCollider.isSupported)
                EditorGUILayout.HelpBox("Wheel Collider not supported with the current physics engine", MessageType.Error);

            using (new EditorGUI.DisabledScope(!m_WheelCollider.isSupported))
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

                ShowLayerOverridesProperties();

                serializedObject.ApplyModifiedProperties();
            }
        }

         protected void ShowLayerOverridesProperties()
        {
            // Show Layer Overrides.
            m_ShowLayerOverridesFoldout.value = m_ShowLayerOverrides.target = EditorGUILayout.Foldout(m_ShowLayerOverrides.target, "Layer Overrides", true);
            if (EditorGUILayout.BeginFadeGroup(m_ShowLayerOverrides.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_LayerOverridePriority, Styles.layerOverridePriority);
                EditorGUILayout.PropertyField(m_IncludeLayers, Styles.includeLayers);
                EditorGUILayout.PropertyField(m_ExcludeLayers, Styles.excludeLayers);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();
        }
    }
}
