// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;


namespace UnityEditor
{
    [CustomEditor(typeof(WindZone))]
    [CanEditMultipleObjects]
    internal class WindInspector : Editor
    {
        private class Styles
        {
            public static GUIContent Mode               = EditorGUIUtility.TextContent("Mode|The wind blows towards a direction or outwards within a sphere");
            public static GUIContent Radius             = EditorGUIUtility.TextContent("Radius|The radius of the spherical area");
            public static GUIContent WindMain           = EditorGUIUtility.TextContent("Main|Overall strength of the wind");
            public static GUIContent WindTurbulence     = EditorGUIUtility.TextContent("Turbulence|Randomness in strength");
            public static GUIContent WindPulseMagnitude = EditorGUIUtility.TextContent("Pulse Magnitude|Strength of the wind pulses");
            public static GUIContent WindPulseFrequency = EditorGUIUtility.TextContent("Pulse Frequency|Frequency of the wind pulses");
        }

        private SerializedProperty m_Mode;
        private SerializedProperty m_Radius;
        private SerializedProperty m_WindMain;
        private SerializedProperty m_WindTurbulence;
        private SerializedProperty m_WindPulseMagnitude;
        private SerializedProperty m_WindPulseFrequency;

        private readonly AnimBool m_ShowRadius = new AnimBool();

        private void OnEnable()
        {
            m_Mode = serializedObject.FindProperty("m_Mode");
            m_Radius = serializedObject.FindProperty("m_Radius");
            m_WindMain = serializedObject.FindProperty("m_WindMain");
            m_WindTurbulence = serializedObject.FindProperty("m_WindTurbulence");
            m_WindPulseMagnitude = serializedObject.FindProperty("m_WindPulseMagnitude");
            m_WindPulseFrequency = serializedObject.FindProperty("m_WindPulseFrequency");

            m_ShowRadius.value = !m_Mode.hasMultipleDifferentValues && m_Mode.intValue == (int)WindZoneMode.Spherical;
            m_ShowRadius.valueChanged.AddListener(Repaint);
        }

        private void OnDisable()
        {
            m_ShowRadius.valueChanged.RemoveListener(Repaint);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Mode, Styles.Mode);

            m_ShowRadius.target = !m_Mode.hasMultipleDifferentValues && m_Mode.intValue == (int)WindZoneMode.Spherical;
            if (EditorGUILayout.BeginFadeGroup(m_ShowRadius.faded))
                EditorGUILayout.PropertyField(m_Radius, Styles.Radius);
            EditorGUILayout.EndFadeGroup();

            EditorGUILayout.PropertyField(m_WindMain, Styles.WindMain);
            EditorGUILayout.PropertyField(m_WindTurbulence, Styles.WindTurbulence);
            EditorGUILayout.PropertyField(m_WindPulseMagnitude, Styles.WindPulseMagnitude);
            EditorGUILayout.PropertyField(m_WindPulseFrequency, Styles.WindPulseFrequency);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
