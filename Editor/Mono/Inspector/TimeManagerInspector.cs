// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;

namespace UnityEditor
{
    [CustomEditor(typeof(TimeManager))]
    internal class TimeManagerEditor : Editor
    {
        SerializedProperty m_FixedTimestepProperty;
        SerializedProperty m_MaxAllowedTimestepProperty;
        SerializedProperty m_TimeScaleProperty;
        SerializedProperty m_MaxParticleTimestepProperty;

        GUIContent m_FixedTimestepLabel;
        GUIContent m_MaxAllowedTimestepLabel;
        GUIContent m_TimeScaleLabel;
        GUIContent m_MaxParticleTimestepLabel;

        public void OnEnable()
        {
            m_FixedTimestepProperty = serializedObject.FindProperty("Fixed Timestep");
            m_MaxAllowedTimestepProperty = serializedObject.FindProperty("Maximum Allowed Timestep");
            m_TimeScaleProperty = serializedObject.FindProperty("m_TimeScale");
            m_MaxParticleTimestepProperty = serializedObject.FindProperty("Maximum Particle Timestep");

            m_FixedTimestepLabel =  EditorGUIUtility.TextContent("Fixed Timestep|A framerate-independent interval that dictates when physics calculations and FixedUpdate() events are performed.");
            m_MaxAllowedTimestepLabel =  EditorGUIUtility.TextContent("Maximum Allowed Timestep|A framerate-independent interval that caps the worst case scenario when framerate is low. Physics calculations and FixedUpdate() events will not be performed for longer time than specified.");
            m_TimeScaleLabel = EditorGUIUtility.TextContent("Time Scale|The speed at which time progresses. Change this value to simulate bullet-time effects. A value of 1 means real-time. A value of .5 means half speed; a value of 2 is double speed.");
            m_MaxParticleTimestepLabel = EditorGUIUtility.TextContent("Maximum Particle Timestep|The maximum time that should be allowed to process particles for a frame.");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_FixedTimestepProperty, m_FixedTimestepLabel);
            EditorGUILayout.PropertyField(m_MaxAllowedTimestepProperty, m_MaxAllowedTimestepLabel);
            EditorGUILayout.PropertyField(m_TimeScaleProperty, m_TimeScaleLabel);
            EditorGUILayout.PropertyField(m_MaxParticleTimestepProperty, m_MaxParticleTimestepLabel);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
