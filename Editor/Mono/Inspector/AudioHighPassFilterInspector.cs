// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(AudioHighPassFilter))]
    [CanEditMultipleObjects]
    class AudioHighPassFilterEditor : Editor
    {
        SerializedProperty m_HighpassResonanceQ;
        SerializedProperty m_CutoffFrequency;

        private static class Styles
        {
            public static readonly GUIContent HighpassResonanceQTooltip = EditorGUIUtility.TrTextContent("Highpass Resonance Q", "Determines how much the filter's self-resonance is dampened");
            public static readonly GUIContent CutoffFrequencyTooltip = EditorGUIUtility.TrTextContent("Cutoff Frequency", "Highpass cutoff frequency in Hz");
        }

        void OnEnable()
        {
            m_HighpassResonanceQ = serializedObject.FindProperty("m_HighpassResonanceQ");
            m_CutoffFrequency = serializedObject.FindProperty("m_CutoffFrequency");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Slider(m_CutoffFrequency, 10.0f, 22000.0f, Styles.CutoffFrequencyTooltip);
            EditorGUILayout.PropertyField(m_HighpassResonanceQ, Styles.HighpassResonanceQTooltip);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
