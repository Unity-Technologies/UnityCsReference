// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(TimeManager))]
    internal class TimeManagerEditor : Editor
    {
        class Content
        {
            public static readonly GUIContent fixedTimestepLabel =  EditorGUIUtility.TrTextContent("Fixed Timestep", "A framerate-independent interval that dictates when physics calculations and FixedUpdate() events are performed.");
            public static readonly GUIContent maxAllowedTimestepLabel =  EditorGUIUtility.TrTextContent("Maximum Allowed Timestep", "A framerate-independent interval that caps the worst case scenario when framerate is low. Physics calculations and FixedUpdate() events will not be performed for longer time than specified.");
            public static readonly GUIContent timeScaleLabel = EditorGUIUtility.TrTextContent("Time Scale", "The speed at which time progresses. Change this value to simulate bullet-time effects. A value of 1 means real-time. A value of .5 means half speed; a value of 2 is double speed.");
            public static readonly GUIContent maxParticleTimestepLabel = EditorGUIUtility.TrTextContent("Maximum Particle Timestep", "The maximum time that should be allowed to process particles for a frame.");
        }

        SerializedProperty m_FixedTimestepProperty;
        SerializedProperty m_MaxAllowedTimestepProperty;
        SerializedProperty m_TimeScaleProperty;
        SerializedProperty m_MaxParticleTimestepProperty;

        public void OnEnable()
        {
            m_FixedTimestepProperty = serializedObject.FindProperty("Fixed Timestep");
            m_MaxAllowedTimestepProperty = serializedObject.FindProperty("Maximum Allowed Timestep");
            m_TimeScaleProperty = serializedObject.FindProperty("m_TimeScale");
            m_MaxParticleTimestepProperty = serializedObject.FindProperty("Maximum Particle Timestep");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_FixedTimestepProperty, Content.fixedTimestepLabel);
            EditorGUILayout.PropertyField(m_MaxAllowedTimestepProperty, Content.maxAllowedTimestepLabel);
            EditorGUILayout.PropertyField(m_TimeScaleProperty, Content.timeScaleLabel);
            EditorGUILayout.PropertyField(m_MaxParticleTimestepProperty, Content.maxParticleTimestepLabel);

            serializedObject.ApplyModifiedProperties();
        }

        [SettingsProvider]
        internal static SettingsProvider CreateProjectSettingsProvider()
        {
            var provider = AssetSettingsProvider.CreateProviderFromAssetPath(
                "Project/Time", "ProjectSettings/TimeManager.asset",
                SettingsProvider.GetSearchKeywordsFromGUIContentProperties<Content>());
            return provider;
        }
    }
}
