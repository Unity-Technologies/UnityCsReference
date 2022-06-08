// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.AnimatedValues;

namespace UnityEditor
{
    /// <summary>
    /// Prompts the end-user to add 2D colliders if non exist for 2D effector to work with.
    /// </summary>
    [CustomEditor(typeof(PointEffector2D), true)]
    [CanEditMultipleObjects]
    internal class PointEffector2DEditor : Effector2DEditor
    {
        readonly AnimBool m_ShowForceRollout = new AnimBool();
        SerializedProperty m_ForceMagnitude;
        SerializedProperty m_ForceVariation;
        SerializedProperty m_ForceSource;
        SerializedProperty m_ForceTarget;
        SerializedProperty m_ForceMode;
        SerializedProperty m_DistanceScale;

        static readonly AnimBool m_ShowDampingRollout = new AnimBool();
        SerializedProperty m_Drag;
        SerializedProperty m_AngularDrag;

        public override void OnEnable()
        {
            base.OnEnable();

            m_ShowForceRollout.value = true;
            m_ShowForceRollout.valueChanged.AddListener(Repaint);
            m_ForceMagnitude = serializedObject.FindProperty("m_ForceMagnitude");
            m_ForceVariation = serializedObject.FindProperty("m_ForceVariation");
            m_ForceSource = serializedObject.FindProperty("m_ForceSource");
            m_ForceTarget = serializedObject.FindProperty("m_ForceTarget");
            m_ForceMode = serializedObject.FindProperty("m_ForceMode");
            m_DistanceScale = serializedObject.FindProperty("m_DistanceScale");

            m_ShowDampingRollout.valueChanged.AddListener(Repaint);
            m_Drag = serializedObject.FindProperty("m_Drag");
            m_AngularDrag = serializedObject.FindProperty("m_AngularDrag");
        }

        public override void OnDisable()
        {
            base.OnDisable();

            m_ShowForceRollout.valueChanged.RemoveListener(Repaint);
            m_ShowDampingRollout.valueChanged.RemoveListener(Repaint);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            // Force.
            m_ShowForceRollout.target = EditorGUILayout.Foldout(m_ShowForceRollout.target, "Force", true);
            if (EditorGUILayout.BeginFadeGroup(m_ShowForceRollout.faded))
            {
                EditorGUILayout.PropertyField(m_ForceMagnitude);
                EditorGUILayout.PropertyField(m_ForceVariation);
                EditorGUILayout.PropertyField(m_DistanceScale);
                EditorGUILayout.PropertyField(m_ForceSource);
                EditorGUILayout.PropertyField(m_ForceTarget);
                EditorGUILayout.PropertyField(m_ForceMode);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFadeGroup();

            // Drag.
            m_ShowDampingRollout.target = EditorGUILayout.Foldout(m_ShowDampingRollout.target, "Damping", true);
            if (EditorGUILayout.BeginFadeGroup(m_ShowDampingRollout.faded))
            {
                EditorGUILayout.PropertyField(m_Drag);
                EditorGUILayout.PropertyField(m_AngularDrag);
            }
            EditorGUILayout.EndFadeGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
