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
    [CustomEditor(typeof(SurfaceEffector2D), true)]
    [CanEditMultipleObjects]
    internal class SurfaceEffector2DEditor : Effector2DEditor
    {
        readonly AnimBool m_ShowForceRollout = new AnimBool();
        SerializedProperty m_Speed;
        SerializedProperty m_SpeedVariation;
        SerializedProperty m_ForceScale;

        static readonly AnimBool m_ShowOptionsRollout = new AnimBool();
        SerializedProperty m_UseContactForce;
        SerializedProperty m_UseFriction;
        SerializedProperty m_UseBounce;

        public override void OnEnable()
        {
            base.OnEnable();


            m_ShowForceRollout.value = true;
            m_ShowForceRollout.valueChanged.AddListener(Repaint);
            m_Speed = serializedObject.FindProperty("m_Speed");
            m_SpeedVariation = serializedObject.FindProperty("m_SpeedVariation");
            m_ForceScale = serializedObject.FindProperty("m_ForceScale");

            m_ShowOptionsRollout.valueChanged.AddListener(Repaint);
            m_UseContactForce = serializedObject.FindProperty("m_UseContactForce");
            m_UseFriction = serializedObject.FindProperty("m_UseFriction");
            m_UseBounce = serializedObject.FindProperty("m_UseBounce");
        }

        public override void OnDisable()
        {
            base.OnDisable();

            m_ShowForceRollout.valueChanged.RemoveListener(Repaint);
            m_ShowOptionsRollout.valueChanged.RemoveListener(Repaint);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            // Force.
            m_ShowForceRollout.target = EditorGUILayout.Foldout(m_ShowForceRollout.target, "Force", true);
            if (EditorGUILayout.BeginFadeGroup(m_ShowForceRollout.faded))
            {
                EditorGUILayout.PropertyField(m_Speed);
                EditorGUILayout.PropertyField(m_SpeedVariation);
                EditorGUILayout.PropertyField(m_ForceScale);
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndFadeGroup();

            // Options.
            m_ShowOptionsRollout.target = EditorGUILayout.Foldout(m_ShowOptionsRollout.target, "Options", true);
            if (EditorGUILayout.BeginFadeGroup(m_ShowOptionsRollout.faded))
            {
                EditorGUILayout.PropertyField(m_UseContactForce);
                EditorGUILayout.PropertyField(m_UseFriction);
                EditorGUILayout.PropertyField(m_UseBounce);
            }
            EditorGUILayout.EndFadeGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
