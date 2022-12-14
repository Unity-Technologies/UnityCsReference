// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(TimeManager))]
    internal class TimeManagerEditor : Editor
    {
        class Content
        {
            public static readonly GUIContent fixedTimestepLabel =  EditorGUIUtility.TrTextContent("Fixed Timestep", "A framerate-independent interval that dictates when physics calculations and FixedUpdate() events are performed.");
            public static readonly GUIContent maxAllowedTimestepLabel =  EditorGUIUtility.TrTextContent("Maximum Allowed Timestep", "A framerate-independent interval that caps the worst-case scenario when framerate is low. Physics calculations and FixedUpdate() events will not be performed for longer time than specified.");
            public static readonly GUIContent timeScaleLabel = EditorGUIUtility.TrTextContent("Time Scale", "The speed at which time progresses. Change this value to simulate bullet-time effects. A value of 1 means real-time. A value of .5 means half speed; a value of 2 is double speed.");
            public static readonly GUIContent maxParticleTimestepLabel = EditorGUIUtility.TrTextContent("Maximum Particle Timestep", "The maximum frame delta time Unity permits for a single iteration of the Particle System update. If the delta time is larger than this value, Unity will run the Particle System update multiple times during the frame with smaller timesteps. This preserves the quality of the simulation.");
        }

        SerializedProperty m_FixedTimestepCountProperty;
        SerializedProperty m_MaxAllowedTimestepProperty;
        SerializedProperty m_TimeScaleProperty;
        SerializedProperty m_MaxParticleTimestepProperty;

        static RationalTime.TicksPerSecond m_FixedTimeTicksPerSecond;
        const float MinFixedTimeStep = 0.0001f;

        public void OnEnable()
        {
            var fixedTimestepProperty = serializedObject.FindProperty("Fixed Timestep");
            m_FixedTimestepCountProperty = fixedTimestepProperty.FindPropertyRelative("m_Count");
            m_MaxAllowedTimestepProperty = serializedObject.FindProperty("Maximum Allowed Timestep");
            m_TimeScaleProperty = serializedObject.FindProperty("m_TimeScale");
            m_MaxParticleTimestepProperty = serializedObject.FindProperty("Maximum Particle Timestep");

            if (!m_FixedTimeTicksPerSecond.Valid) // FixedTime has a constant ticks per second value
            {
                var numerator = fixedTimestepProperty.FindPropertyRelative("m_Rate.m_Numerator");
                var denominator = fixedTimestepProperty.FindPropertyRelative("m_Rate.m_Denominator");
                m_FixedTimeTicksPerSecond =
                    new RationalTime.TicksPerSecond(numerator.uintValue, denominator.uintValue);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawFixedTimeAsFloat(m_FixedTimestepCountProperty);
            EditorGUILayout.PropertyField(m_MaxAllowedTimestepProperty, Content.maxAllowedTimestepLabel);
            EditorGUILayout.PropertyField(m_TimeScaleProperty, Content.timeScaleLabel);
            EditorGUILayout.PropertyField(m_MaxParticleTimestepProperty, Content.maxParticleTimestepLabel);

            serializedObject.ApplyModifiedProperties();
        }

        static void DrawFixedTimeAsFloat(SerializedProperty prop)
        {
            var fixedTime = (float)new RationalTime(prop.longValue, m_FixedTimeTicksPerSecond).ToDouble(); // Convert a tick count to a float
            using (var c = new EditorGUI.ChangeCheckScope())
            {
                fixedTime = EditorGUILayout.FloatField(Content.fixedTimestepLabel, fixedTime);
                if (c.changed)
                {
                    var newCount = RationalTime
                        .FromDouble(MathF.Max(fixedTime, MinFixedTimeStep), m_FixedTimeTicksPerSecond)
                        .Count; // convert it back to a count to store in the property
                    prop.longValue = newCount;
                }
            }
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
