// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.Audio;
using UnityEngine.Audio;

namespace UnityEditor
{
    [CustomEditor(typeof(AudioMixerController))]
    [CanEditMultipleObjects]
    internal class AudioMixerControllerInspector : Editor
    {
        static class Texts
        {
            public static GUIContent m_EnableSuspendLabel = new GUIContent("Auto Mixer Suspend", "Enables/disables suspending of processing in order to save CPU when the RMS signal level falls under the defined threshold (in dB). Mixers resume processing when an AudioSource referencing them starts playing again.");
            public static GUIContent m_SuspendThresholdLabel = new GUIContent("    Threshold Volume", "The level of the Master Group at which the mixer suspends processing in order to save CPU. Mixers resume processing when an AudioSource referencing them starts playing again.");
            public static GUIContent m_UpdateModeLabel = new GUIContent("Update Mode", "Update AudioMixer transitions with game time or unscaled realtime.");
            public static string dB = "dB";
        }

        SerializedProperty m_EnableSuspend;
        SerializedProperty m_SuspendThreshold;
        SerializedProperty m_UpdateMode;

        public void OnEnable()
        {
            m_SuspendThreshold = serializedObject.FindProperty("m_SuspendThreshold");
            m_EnableSuspend = serializedObject.FindProperty("m_EnableSuspend");
            m_UpdateMode = serializedObject.FindProperty("m_UpdateMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_EnableSuspend, Texts.m_EnableSuspendLabel);
            using (new EditorGUI.DisabledScope(!m_EnableSuspend.boolValue || m_EnableSuspend.hasMultipleDifferentValues))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.s_UnitString = Texts.dB;
                float displayValue = m_SuspendThreshold.floatValue;
                displayValue = EditorGUILayout.PowerSlider(Texts.m_SuspendThresholdLabel, displayValue, AudioMixerController.kMinVolume, AudioMixerController.GetMaxVolume(), 1.0f);
                EditorGUI.s_UnitString = null;
                if (EditorGUI.EndChangeCheck())
                    m_SuspendThreshold.floatValue = displayValue;
            }
            EditorGUILayout.PropertyField(m_UpdateMode, Texts.m_UpdateModeLabel);
            serializedObject.ApplyModifiedProperties();
        }
    }

    // Here we need an inspector for runtime objects that are loaded in the editor (via asset bundles)
    // We need to inform the user that such objects are not editable in the editor.
    [CustomEditor(typeof(AudioMixer))]
    [CanEditMultipleObjects]
    internal class AudioMixerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.HelpBox("Modification and inspection of built AudioMixer assets is disabled. Please modify the source asset and re-build.", MessageType.Info);
        }
    }
}
