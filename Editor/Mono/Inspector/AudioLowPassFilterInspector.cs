// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(AudioLowPassFilter))]
    [CanEditMultipleObjects]
    internal class AudioLowPassFilterInspector : Editor
    {
        SerializedProperty m_LowpassResonanceQ;
        SerializedProperty m_LowpassLevelCustomCurve;

        void OnEnable()
        {
            m_LowpassResonanceQ = serializedObject.FindProperty("m_LowpassResonanceQ");
            m_LowpassLevelCustomCurve = serializedObject.FindProperty("lowpassLevelCustomCurve");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            AudioSourceInspector.AnimProp(
                new GUIContent("Cutoff Frequency"),
                m_LowpassLevelCustomCurve,
                0.0f, AudioSourceInspector.kMaxCutoffFrequency, true);

            EditorGUILayout.PropertyField(m_LowpassResonanceQ);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
