// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(AudioReverbZone))]
    [CanEditMultipleObjects]
    class AudioReverbZoneEditor : Editor
    {
        SerializedProperty m_MinDistance;
        SerializedProperty m_MaxDistance;
        SerializedProperty m_ReverbPreset;

        SerializedProperty  m_Room; // room effect level (at mid frequencies)
        SerializedProperty  m_RoomHF; // relative room effect level at high frequencies
        SerializedProperty  m_RoomLF; // relative room effect level at low frequencies
        SerializedProperty  m_DecayTime; // reverberation decay time at mid frequencies
        SerializedProperty  m_DecayHFRatio; //  high-frequency to mid-frequency decay time ratio
        SerializedProperty  m_Reflections; // early reflections level relative to room effect
        SerializedProperty  m_ReflectionsDelay; //  initial reflection delay time
        SerializedProperty  m_Reverb; //  late reverberation level relative to room effect
        SerializedProperty  m_ReverbDelay; //  late reverberation delay time relative to initial reflection
        SerializedProperty  m_HFReference; // reference high frequency (hz)
        SerializedProperty  m_LFReference; // reference low frequency (hz)
        SerializedProperty  m_Diffusion; //  Value that controls the echo density in the late reverberation decay
        SerializedProperty  m_Density; // Value that controls the modal density in the late reverberation decay

        void OnEnable()
        {
            m_MinDistance = serializedObject.FindProperty("m_MinDistance");
            m_MaxDistance = serializedObject.FindProperty("m_MaxDistance");
            m_ReverbPreset = serializedObject.FindProperty("m_ReverbPreset");
            m_Room = serializedObject.FindProperty("m_Room"); // room effect level (at mid frequencies)
            m_RoomHF = serializedObject.FindProperty("m_RoomHF"); // relative room effect level at high frequencies
            m_RoomLF = serializedObject.FindProperty("m_RoomLF"); // relative room effect level at low frequencies
            m_DecayTime = serializedObject.FindProperty("m_DecayTime"); // reverberation decay time at mid frequencies
            m_DecayHFRatio = serializedObject.FindProperty("m_DecayHFRatio"); //  high-frequency to mid-frequency decay time ratio
            m_Reflections = serializedObject.FindProperty("m_Reflections"); // early reflections level relative to room effect
            m_ReflectionsDelay = serializedObject.FindProperty("m_ReflectionsDelay"); //  initial reflection delay time
            m_Reverb = serializedObject.FindProperty("m_Reverb"); //  late reverberation level relative to room effect
            m_ReverbDelay = serializedObject.FindProperty("m_ReverbDelay"); //  late reverberation delay time relative to initial reflection
            m_HFReference = serializedObject.FindProperty("m_HFReference"); // reference high frequency (hz)
            m_LFReference = serializedObject.FindProperty("m_LFReference"); // reference low frequency (hz)
            m_Diffusion = serializedObject.FindProperty("m_Diffusion"); //  Value that controls the echo density in the late reverberation decay
            m_Density = serializedObject.FindProperty("m_Density"); // Value that controls the modal density in the late reverberation decay
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_MinDistance);
            EditorGUILayout.PropertyField(m_MaxDistance);

            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_ReverbPreset);
            // Changing the preset changes all the other properties as well, so we need to do a full refresh afterwards
            if (EditorGUI.EndChangeCheck())
                serializedObject.SetIsDifferentCacheDirty();

            using (new EditorGUI.DisabledScope(m_ReverbPreset.enumValueIndex != 27 || m_ReverbPreset.hasMultipleDifferentValues))
            {
                EditorGUILayout.IntSlider(m_Room, -10000, 0);
                EditorGUILayout.IntSlider(m_RoomHF, -10000, 0);
                EditorGUILayout.IntSlider(m_RoomLF, -10000, 0);
                EditorGUILayout.Slider(m_DecayTime, 0.1f, 20.0f);
                EditorGUILayout.Slider(m_DecayHFRatio, 0.1f, 2.0f);
                EditorGUILayout.IntSlider(m_Reflections, -10000, 1000);
                EditorGUILayout.Slider(m_ReflectionsDelay, 0.0f, 0.3f);
                EditorGUILayout.IntSlider(m_Reverb, -10000, 2000);
                EditorGUILayout.Slider(m_ReverbDelay, 0.0f, 0.1f);
                EditorGUILayout.Slider(m_HFReference, 1000.0f, 20000.0f);
                EditorGUILayout.Slider(m_LFReference, 20.0f, 1000.0f);
                EditorGUILayout.Slider(m_Diffusion, 0.0f, 100.0f);
                EditorGUILayout.Slider(m_Density, 0.0f, 100.0f);
            }

            serializedObject.ApplyModifiedProperties();
        }

        void OnSceneGUI()
        {
            AudioReverbZone zone = (AudioReverbZone)target;

            Color tempColor = Handles.color;
            if (zone.enabled)
                Handles.color = new Color(0.50f, 0.70f, 1.00f, 0.5f);
            else
                Handles.color = new Color(0.30f, 0.40f, 0.60f, 0.5f);

            Vector3 position = zone.transform.position;

            EditorGUI.BeginChangeCheck();
            float minDistance = Handles.RadiusHandle(Quaternion.identity, position, zone.minDistance, true);
            float maxDistance = Handles.RadiusHandle(Quaternion.identity, position, zone.maxDistance, true);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(zone, "Reverb Distance");
                zone.minDistance = minDistance;
                zone.maxDistance = maxDistance;
            }

            Handles.color = tempColor;
        }
    }
}
