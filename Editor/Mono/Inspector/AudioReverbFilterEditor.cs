// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(AudioReverbFilter))]
    [CanEditMultipleObjects]
    class AudioReverbFilterEditor : Editor
    {
        SerializedProperty m_ReverbPreset;

        SerializedProperty  m_DryLevel; // room effect level (at mid frequencies)
        SerializedProperty  m_Room; // room effect level (at mid frequencies)
        SerializedProperty  m_RoomHF; // relative room effect level at high frequencies
        SerializedProperty  m_RoomLF; // relative room effect level at low frequencies
        SerializedProperty  m_DecayTime; // reverberation decay time at mid frequencies
        SerializedProperty  m_DecayHFRatio; //  high-frequency to mid-frequency decay time ratio
        SerializedProperty  m_ReflectionsLevel; // early reflections level relative to room effect
        SerializedProperty  m_ReflectionsDelay; //  initial reflection delay time
        SerializedProperty  m_ReverbLevel; //  late reverberation level relative to room effect
        SerializedProperty  m_ReverbDelay; //  late reverberation delay time relative to initial reflection
        SerializedProperty  m_HFReference; // reference high frequency (hz)
        SerializedProperty  m_LFReference; // reference low frequency (hz)
        SerializedProperty  m_Diffusion; //  Value that controls the echo density in the late reverberation decay
        SerializedProperty  m_Density; // Value that controls the modal density in the late reverberation decay


        void OnEnable()
        {
            m_ReverbPreset = serializedObject.FindProperty("m_ReverbPreset");
            m_DryLevel = serializedObject.FindProperty("m_DryLevel"); // room effect level (at mid frequencies)
            m_Room = serializedObject.FindProperty("m_Room"); // room effect level (at mid frequencies)
            m_RoomHF = serializedObject.FindProperty("m_RoomHF"); // relative room effect level at high frequencies
            m_RoomLF = serializedObject.FindProperty("m_RoomLF"); // relative room effect level at low frequencies
            m_DecayTime = serializedObject.FindProperty("m_DecayTime"); // reverberation decay time at mid frequencies
            m_DecayHFRatio = serializedObject.FindProperty("m_DecayHFRatio"); //  high-frequency to mid-frequency decay time ratio
            m_ReflectionsLevel = serializedObject.FindProperty("m_ReflectionsLevel"); // early reflections level relative to room effect
            m_ReflectionsDelay = serializedObject.FindProperty("m_ReflectionsDelay"); //  initial reflection delay time
            m_ReverbLevel = serializedObject.FindProperty("m_ReverbLevel"); //  late reverberation level relative to room effect
            m_ReverbDelay = serializedObject.FindProperty("m_ReverbDelay"); //  late reverberation delay time relative to initial reflection
            m_HFReference = serializedObject.FindProperty("m_HFReference"); // reference high frequency (hz)
            m_LFReference = serializedObject.FindProperty("m_LFReference"); // reference low frequency (hz)
            m_Diffusion = serializedObject.FindProperty("m_Diffusion"); //  Value that controls the echo density in the late reverberation decay
            m_Density = serializedObject.FindProperty("m_Density"); // Value that controls the modal density in the late reverberation decay
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_ReverbPreset);
            if (EditorGUI.EndChangeCheck())
                serializedObject.SetIsDifferentCacheDirty();

            using (new EditorGUI.DisabledScope(m_ReverbPreset.enumValueIndex != 27 || m_ReverbPreset.hasMultipleDifferentValues))
            {
                EditorGUILayout.Slider(m_DryLevel, -10000, 0);
                EditorGUILayout.Slider(m_Room, -10000, 0);
                EditorGUILayout.Slider(m_RoomHF, -10000, 0);
                EditorGUILayout.Slider(m_RoomLF, -10000, 0);
                EditorGUILayout.Slider(m_DecayTime, 0.1f, 20.0f);
                EditorGUILayout.Slider(m_DecayHFRatio, 0.1f, 2.0f);
                EditorGUILayout.Slider(m_ReflectionsLevel, -10000, 1000);
                EditorGUILayout.Slider(m_ReflectionsDelay, 0.0f, 0.3f);
                EditorGUILayout.Slider(m_ReverbLevel, -10000, 2000);
                EditorGUILayout.Slider(m_ReverbDelay, 0.0f, 0.1f);
                EditorGUILayout.Slider(m_HFReference, 1000.0f, 20000.0f);
                EditorGUILayout.Slider(m_LFReference, 20.0f, 1000.0f);
                EditorGUILayout.Slider(m_Diffusion, 0.0f, 100.0f);
                EditorGUILayout.Slider(m_Density, 0.0f, 100.0f);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
