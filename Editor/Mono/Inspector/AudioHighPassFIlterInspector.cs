// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(AudioHighPassFilter))]
    [CanEditMultipleObjects]
    class AudioHighPassFilterEditor : Editor
    {
        SerializedProperty m_HighpassResonanceQ;
        SerializedProperty m_CutoffFrequency;

        void OnEnable()
        {
            m_HighpassResonanceQ = serializedObject.FindProperty("m_HighpassResonanceQ");
            m_CutoffFrequency = serializedObject.FindProperty("m_CutoffFrequency");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Slider(m_CutoffFrequency, 10.0f, 22000.0f, EditorGUIUtility.TrTextContent("CutOffFrequency", "Sets the cut off frequency of High Pass filter"));

            EditorGUILayout.PropertyField(m_HighpassResonanceQ);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
