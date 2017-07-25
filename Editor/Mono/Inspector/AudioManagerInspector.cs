// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

using System.Collections.Generic;

namespace UnityEditor
{
    [CustomEditor(typeof(AudioManager))]
    internal class AudioManagerInspector : ProjectSettingsBaseEditor
    {
        private class Styles
        {
            public static GUIContent Volume                 = EditorGUIUtility.TextContent("Global Volume|Initial volume multiplier (AudioListener.volume)");
            public static GUIContent RolloffScale           = EditorGUIUtility.TextContent("Volume Rolloff Scale|Global volume rolloff multiplier (applies only to logarithmic volume curves).");
            public static GUIContent DopplerFactor          = EditorGUIUtility.TextContent("Doppler Factor|Global Doppler speed multiplier for sounds in motion.");
            public static GUIContent DefaultSpeakerMode     = EditorGUIUtility.TextContent("Default Speaker Mode|Speaker mode at start of the game. This may be changed at runtime using the AudioSettings.Reset function.");
            public static GUIContent SampleRate             = EditorGUIUtility.TextContent("System Sample Rate|Sample rate at which the output device of the audio system runs. Individual sounds may run at different sample rates and will be slowed down/sped up accordingly to match the output rate.");
            public static GUIContent DSPBufferSize          = EditorGUIUtility.TextContent("DSP Buffer Size|Length of mixing buffer. This determines the output latency of the game.");
            public static GUIContent VirtualVoiceCount      = EditorGUIUtility.TextContent("Max Virtual Voices|Maximum number of sounds managed by the system. Even though at most RealVoiceCount of the loudest sounds will be physically playing, the remaining sounds will still be updating their play position.");
            public static GUIContent RealVoiceCount         = EditorGUIUtility.TextContent("Max Real Voices|Maximum number of actual simultanously playing sounds.");
            public static GUIContent SpatializerPlugin      = EditorGUIUtility.TextContent("Spatializer Plugin|Native audio plugin performing spatialized filtering of 3D sources.");
            public static GUIContent AmbisonicDecoderPlugin = EditorGUIUtility.TextContent("Ambisonic Decoder Plugin|Native audio plugin performing ambisonic-to-binaural filtering of sources.");
            public static GUIContent DisableAudio           = EditorGUIUtility.TextContent("Disable Unity Audio|Prevent allocating the output device in the runtime. Use this if you want to use other sound systems than the built-in one.");
            public static GUIContent VirtualizeEffects      = EditorGUIUtility.TextContent("Virtualize Effects|When enabled dynamically turn off effects and spatializers on AudioSources that are culled in order to save CPU.");
        }

        private SerializedProperty m_Volume;
        private SerializedProperty m_RolloffScale;
        private SerializedProperty m_DopplerFactor;
        private SerializedProperty m_DefaultSpeakerMode;
        private SerializedProperty m_SampleRate;
        private SerializedProperty m_DSPBufferSize;
        private SerializedProperty m_VirtualVoiceCount;
        private SerializedProperty m_RealVoiceCount;
        private SerializedProperty m_SpatializerPlugin;
        private SerializedProperty m_AmbisonicDecoderPlugin;
        private SerializedProperty m_DisableAudio;
        private SerializedProperty m_VirtualizeEffects;

        private void OnEnable()
        {
            m_Volume                    = serializedObject.FindProperty("m_Volume");
            m_RolloffScale              = serializedObject.FindProperty("Rolloff Scale");
            m_DopplerFactor             = serializedObject.FindProperty("Doppler Factor");
            m_DefaultSpeakerMode        = serializedObject.FindProperty("Default Speaker Mode");
            m_SampleRate                = serializedObject.FindProperty("m_SampleRate");
            m_DSPBufferSize             = serializedObject.FindProperty("m_DSPBufferSize");
            m_VirtualVoiceCount         = serializedObject.FindProperty("m_VirtualVoiceCount");
            m_RealVoiceCount            = serializedObject.FindProperty("m_RealVoiceCount");
            m_SpatializerPlugin         = serializedObject.FindProperty("m_SpatializerPlugin");
            m_AmbisonicDecoderPlugin    = serializedObject.FindProperty("m_AmbisonicDecoderPlugin");
            m_DisableAudio              = serializedObject.FindProperty("m_DisableAudio");
            m_VirtualizeEffects         = serializedObject.FindProperty("m_VirtualizeEffects");
        }

        //This function assumes that index 0 is None...
        private int FindPluginStringIndex(string[] strs, string element)
        {
            //Skip past the first "None" entry
            for (int i = 1; i < strs.Length; i++)
            {
                if (element == strs[i])
                    return i;
            }

            return 0;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Volume, Styles.Volume);
            EditorGUILayout.PropertyField(m_RolloffScale, Styles.RolloffScale);
            EditorGUILayout.PropertyField(m_DopplerFactor, Styles.DopplerFactor);
            EditorGUILayout.PropertyField(m_DefaultSpeakerMode, Styles.DefaultSpeakerMode);
            EditorGUILayout.PropertyField(m_SampleRate, Styles.SampleRate);
            EditorGUILayout.PropertyField(m_DSPBufferSize, Styles.DSPBufferSize);
            EditorGUILayout.PropertyField(m_VirtualVoiceCount, Styles.VirtualVoiceCount);
            EditorGUILayout.PropertyField(m_RealVoiceCount, Styles.RealVoiceCount);

            List<string> plugins = new List<string>(AudioSettings.GetSpatializerPluginNames());
            plugins.Insert(0, "None");
            string[] pluginsArray = plugins.ToArray();

            List<GUIContent> pluginsGUIContent = new List<GUIContent>();
            foreach (var s in pluginsArray)
                pluginsGUIContent.Add(new GUIContent(s));

            List<string> ambisonicDecoderPlugins = new List<string>(AudioUtil.GetAmbisonicDecoderPluginNames());
            ambisonicDecoderPlugins.Insert(0, "None");
            string[] ambisonicDecoderPluginsArray = ambisonicDecoderPlugins.ToArray();

            List<GUIContent> ambisonicDecoderPluginsGUIContent = new List<GUIContent>();
            foreach (var s in ambisonicDecoderPluginsArray)
                ambisonicDecoderPluginsGUIContent.Add(new GUIContent(s));

            EditorGUI.BeginChangeCheck();
            int pluginIndex = FindPluginStringIndex(pluginsArray, m_SpatializerPlugin.stringValue);
            pluginIndex = EditorGUILayout.Popup(Styles.SpatializerPlugin, pluginIndex, pluginsGUIContent.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                if (pluginIndex == 0)
                    m_SpatializerPlugin.stringValue = "";
                else
                    m_SpatializerPlugin.stringValue = pluginsArray[pluginIndex];
            }

            EditorGUI.BeginChangeCheck();
            pluginIndex = FindPluginStringIndex(ambisonicDecoderPluginsArray, m_AmbisonicDecoderPlugin.stringValue);
            pluginIndex = EditorGUILayout.Popup(Styles.AmbisonicDecoderPlugin, pluginIndex, ambisonicDecoderPluginsGUIContent.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                if (pluginIndex == 0)
                    m_AmbisonicDecoderPlugin.stringValue = "";
                else
                    m_AmbisonicDecoderPlugin.stringValue = ambisonicDecoderPluginsArray[pluginIndex];
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(m_DisableAudio, Styles.DisableAudio);
            EditorGUILayout.PropertyField(m_VirtualizeEffects, Styles.VirtualizeEffects);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
