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

        private static class Styles
        {
            public static readonly GUIContent MinDistanceTooltip = EditorGUIUtility.TrTextContent("MinDistance", "The distance from the centerpoint that the reverb will have full effect at");
            public static readonly GUIContent MaxDistanceTooltip = EditorGUIUtility.TrTextContent("MaxDistance", "The distance from the centerpoint that the reverb will not have any effect");
            public static readonly GUIContent ReverbPresetTooltip = EditorGUIUtility.TrTextContent("ReverbPreset", "The reverb preset");
            public static readonly GUIContent RoomTooltip = EditorGUIUtility.TrTextContent("Room", "Room effect level (at mid frequencies)");
            public static readonly GUIContent RoomHFTooltip = EditorGUIUtility.TrTextContent("Room HF", "Relative room effect level at high frequencies");
            public static readonly GUIContent RoomLFTooltip = EditorGUIUtility.TrTextContent("Room LF", "Relative room effect level at low frequencies");
            public static readonly GUIContent DecayTimeTooltip = EditorGUIUtility.TrTextContent("Decay Time", "Reverberation decay time at mid frequencies");
            public static readonly GUIContent DecayHFRatioTooltip = EditorGUIUtility.TrTextContent("Decay HF Ratio", "High-frequency to mid-frequency decay time ratio");
            public static readonly GUIContent ReflectionsTooltip = EditorGUIUtility.TrTextContent("Reflections", "Early reflections level relative to room effect");
            public static readonly GUIContent ReflectionsDelayTooltip = EditorGUIUtility.TrTextContent("Reflections Delay", "Initial reflection delay time");
            public static readonly GUIContent ReverbTooltip = EditorGUIUtility.TrTextContent("Reverb", "Late reverberation level relative to room effect");
            public static readonly GUIContent ReverbDelayTooltip = EditorGUIUtility.TrTextContent("Reverb Delay", "Late reverberation delay time relative to initial reflection");
            public static readonly GUIContent HFReferenceTooltip = EditorGUIUtility.TrTextContent("HF Reference", "Reference high frequency (Hz)");
            public static readonly GUIContent LFReferenceTooltip = EditorGUIUtility.TrTextContent("LF Reference", "Reference low frequency (Hz)");
            public static readonly GUIContent DiffusionTooltip = EditorGUIUtility.TrTextContent("Diffusion", "Value that controls the echo density in the late reverberation decay");
            public static readonly GUIContent DensityTooltip = EditorGUIUtility.TrTextContent("Density", "Value that controls the modal density in the late reverberation decay");
        }


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

            EditorGUILayout.PropertyField(m_MinDistance, Styles.MinDistanceTooltip);
            EditorGUILayout.PropertyField(m_MaxDistance, Styles.MaxDistanceTooltip);

            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_ReverbPreset, Styles.ReverbPresetTooltip);
            // Changing the preset changes all the other properties as well, so we need to do a full refresh afterwards
            if (EditorGUI.EndChangeCheck())
                serializedObject.SetIsDifferentCacheDirty();

            using (new EditorGUI.DisabledScope(m_ReverbPreset.enumValueIndex != 27 || m_ReverbPreset.hasMultipleDifferentValues))
            {
                EditorGUILayout.IntSlider(m_Room, -10000, 0, Styles.RoomTooltip);
                EditorGUILayout.IntSlider(m_RoomHF, -10000, 0, Styles.RoomHFTooltip);
                EditorGUILayout.IntSlider(m_RoomLF, -10000, 0, Styles.RoomLFTooltip);
                EditorGUILayout.Slider(m_DecayTime, 0.1f, 20.0f, Styles.DecayTimeTooltip);
                EditorGUILayout.Slider(m_DecayHFRatio, 0.1f, 2.0f, Styles.DecayHFRatioTooltip);
                EditorGUILayout.IntSlider(m_Reflections, -10000, 1000, Styles.ReflectionsTooltip);
                EditorGUILayout.Slider(m_ReflectionsDelay, 0.0f, 0.3f, Styles.ReflectionsDelayTooltip);
                EditorGUILayout.IntSlider(m_Reverb, -10000, 2000, Styles.ReverbTooltip);
                EditorGUILayout.Slider(m_ReverbDelay, 0.0f, 0.1f, Styles.ReverbDelayTooltip);
                EditorGUILayout.Slider(m_HFReference, 1000.0f, 20000.0f, Styles.HFReferenceTooltip);
                EditorGUILayout.Slider(m_LFReference, 20.0f, 1000.0f, Styles.LFReferenceTooltip);
                EditorGUILayout.Slider(m_Diffusion, 0.0f, 100.0f, Styles.DiffusionTooltip);
                EditorGUILayout.Slider(m_Density, 0.0f, 100.0f, Styles.DensityTooltip);
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
