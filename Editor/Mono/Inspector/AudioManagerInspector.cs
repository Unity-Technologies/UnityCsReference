// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor
{
    [CustomEditor(typeof(AudioManager))]
    internal class AudioManagerInspector : ProjectSettingsBaseEditor
    {
        private class Styles
        {
            public static readonly GUIContent Volume                 = L10n.TextContent("Global Volume", "Initial volume multiplier (AudioListener.volume)", null, null);
            public static readonly GUIContent RolloffScale           = L10n.TextContent("Volume Rolloff Scale", "Global volume rolloff multiplier (applies only to logarithmic volume curves).", null, null);
            public static readonly GUIContent DopplerFactor          = L10n.TextContent("Doppler Factor", "Global Doppler speed multiplier for sounds in motion.", null, null);
            public static readonly GUIContent DefaultSpeakerMode     = L10n.TextContent("Default Speaker Mode", "Speaker mode at start of the game. This may be changed at runtime using the AudioSettings.Reset function.", null, null);
            public static readonly GUIContent SampleRate             = L10n.TextContent("System Sample Rate", "Sample rate at which the output device of the audio system runs. Individual sounds may run at different sample rates and will be slowed down/sped up accordingly to match the output rate.", null, null);
            public static readonly GUIContent DSPBufferSize          = L10n.TextContent("DSP Buffer Size", "Length of mixing buffer. This determines the output latency of the game.", null, null);
            public static readonly GUIContent AudioFoundation        = L10n.TextContent("Audio Foundation", "Low-level, platform audio layer. Classic is the same, mature platform layer from previous versions of Unity. Enhanced is the new platform audio layer and is supported on Windows, macOS, Linux, iOS, Android (8.1 and later), XBOX Series X|S, XBOX One, PlayStation®5, Nintendo Switch™, and Nintendo Switch™ 2. The benefits include asynchronous starting and stopping of devices and greater control over audio engine behavior. On platforms that don't have enhanced mode yet, the engine will fall back to using classic mode.", null, null);
            public static readonly GUIContent OutputChannelLayout    = L10n.TextContent("Output Channel Layout", "The audio engine will always run at the selected channel layout and up-mixing or down-mixing will occur to match the device's native channel layout. Alternatively, if DeviceNative is selected, the engine will always run at the device's native channel count, and will be reset if that native channel count changes (i.e. when the default device changes).", null, null);
            public static readonly GUIContent OutputSamplingRate     = L10n.TextContent("Output Sampling Rate", "The audio engine will always run at the selected sampling rate and sample-rate conversion will occur to match the device's native sampling rate. Alternatively, if DeviceNative is selected, the engine will always run at the device's native sampling rate, and will be reset if that native sampling rate changes (i.e. when the default device changes).", null, null);
            public static readonly GUIContent VirtualVoiceCount      = L10n.TextContent("Max Virtual Voices", "Maximum number of sounds managed by the system. Even though at most RealVoiceCount of the loudest sounds will be physically playing, the remaining sounds will still be updating their play position.", null, null);
            public static readonly GUIContent RealVoiceCount         = L10n.TextContent("Max Real Voices", "Maximum number of actual simultaneously playing sounds.", null, null);
            public static readonly GUIContent SpatializerPlugin      = L10n.TextContent("Spatializer Plugin", "Native audio plugin performing spatialized filtering of 3D sources.", null, null);
            public static readonly GUIContent AmbisonicDecoderPlugin = L10n.TextContent("Ambisonic Decoder Plugin", "Native audio plugin performing ambisonic-to-binaural filtering of sources.", null, null);
            public static readonly GUIContent DisableAudio           = L10n.TextContent("Disable Unity Audio", "Prevent allocating the output device in the runtime. Use this if you want to use other sound systems than the built-in one.", null, null);
            public static readonly GUIContent VirtualizeEffects      = L10n.TextContent("Virtualize Effects", "When enabled, dynamically turn off effects and spatializers on AudioSources that are culled in order to save CPU.", null, null);
            public static readonly GUIContent EnableOutputSuspension = L10n.TextContent("Enable Output Suspension (editor only)", "When enabled automatically suspends audio output after detecting that the output has been silent for a long duration (editor only). Suspending the audio system disables a mechanism in the operating system that prevents the computer from going into sleep mode.", null, null);

        }

        private class StylesNonSearchable
        {
            public static readonly GUIContent DSPBufferSizeInfo = L10n.TextContent("The requested buffer size ({0}) has been overridden to {1} by the operating system", null, null, null);
            public static readonly GUIContent EnhancedAudioFoundationInfo = L10n.TextContent("Enhanced will be used on Windows, macOS, Linux, iOS, Android (8.1 and later), XBOX Series X|S, XBOX One, PlayStation®5, Nintendo Switch™, and Nintendo Switch™ 2. Other platforms will use Classic.", null, null, null);
            public static readonly GUIContent PassthroughChannelLayoutInfo = L10n.TextContent("This layout is sent directly to the OS spatializer instead of being down-mixed on Windows, XBOX Series X|S, XBOX One devices, on Meta Quest devices and on Android devices that support spatial audio.", null, null, null);
        }

        static readonly string[] k_AdditionalSearchKeywords =
        {
            "enhanced audio foundation",
        };

        [NoAutoStaticsCleanup] // lazy one-time cache of static GUIContent search keyword strings, no user-code refs; safe to persist
        static List<string> s_SearchKeywords;

        private SerializedProperty m_Volume;
        private SerializedProperty m_RolloffScale;
        private SerializedProperty m_DopplerFactor;
        private SerializedProperty m_DefaultSpeakerMode;
        private SerializedProperty m_SampleRate;
        private SerializedProperty m_RequestedDSPBufferSize;
        private SerializedProperty m_AudioFoundation;
        private SerializedProperty m_OutputChannelLayout;
        private SerializedProperty m_OutputSamplingRate;
        private SerializedProperty m_ActualDSPBufferSize;
        private SerializedProperty m_VirtualVoiceCount;
        private SerializedProperty m_RealVoiceCount;
        private SerializedProperty m_SpatializerPlugin;
        private SerializedProperty m_AmbisonicDecoderPlugin;
        private SerializedProperty m_DisableAudio;
        private SerializedProperty m_VirtualizeEffects;
        private SerializedProperty m_EnableOutputSuspension;

        private void OnEnable()
        {
            m_Volume                    = serializedObject.FindProperty("m_Volume");
            m_RolloffScale              = serializedObject.FindProperty("Rolloff Scale");
            m_DopplerFactor             = serializedObject.FindProperty("Doppler Factor");
            m_DefaultSpeakerMode        = serializedObject.FindProperty("Default Speaker Mode");
            m_SampleRate                = serializedObject.FindProperty("m_SampleRate");
            m_RequestedDSPBufferSize    = serializedObject.FindProperty("m_RequestedDSPBufferSize");
            m_ActualDSPBufferSize       = serializedObject.FindProperty("m_DSPBufferSize");
            m_AudioFoundation           = serializedObject.FindProperty("m_AudioFoundation");
            m_OutputChannelLayout       = serializedObject.FindProperty("m_OutputChannelLayout");
            m_OutputSamplingRate        = serializedObject.FindProperty("m_OutputSamplingRate");
            m_VirtualVoiceCount         = serializedObject.FindProperty("m_VirtualVoiceCount");
            m_RealVoiceCount            = serializedObject.FindProperty("m_RealVoiceCount");
            m_SpatializerPlugin         = serializedObject.FindProperty("m_SpatializerPlugin");
            m_AmbisonicDecoderPlugin    = serializedObject.FindProperty("m_AmbisonicDecoderPlugin");
            m_DisableAudio              = serializedObject.FindProperty("m_DisableAudio");
            m_VirtualizeEffects         = serializedObject.FindProperty("m_VirtualizeEffects");
            m_EnableOutputSuspension    = serializedObject.FindProperty("m_EnableOutputSuspension");
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
            EditorGUILayout.DelayedIntField(m_SampleRate, Styles.SampleRate);
            EditorGUILayout.PropertyField(m_RequestedDSPBufferSize, Styles.DSPBufferSize);
            if (m_RequestedDSPBufferSize.intValue != m_ActualDSPBufferSize.intValue)
                EditorGUILayout.HelpBox(
                    string.Format(StylesNonSearchable.DSPBufferSizeInfo.text,
                        m_RequestedDSPBufferSize.intValue == 0 ? "default" : m_RequestedDSPBufferSize.intValue.ToString(),
                        m_ActualDSPBufferSize.intValue),
                    MessageType.Info);

            EditorGUILayout.PropertyField(m_AudioFoundation, Styles.AudioFoundation);
            if (m_AudioFoundation.intValue.Equals(1))
            {
                EditorGUILayout.HelpBox(StylesNonSearchable.EnhancedAudioFoundationInfo.text, MessageType.Info);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_OutputChannelLayout, Styles.OutputChannelLayout);
                if (m_OutputChannelLayout.intValue == (int)ChannelLayoutBehavior.Surround_7_1_4)
                    EditorGUILayout.HelpBox(StylesNonSearchable.PassthroughChannelLayoutInfo.text, MessageType.Info);
                EditorGUILayout.PropertyField(m_OutputSamplingRate, Styles.OutputSamplingRate);
                EditorGUI.indentLevel--;
            }

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

            if (EditorGUI.EndChangeCheck() && m_DisableAudio.boolValue.Equals(true))
            {
                AudioUtil.StopAllPreviewClips();
            }

            EditorGUILayout.PropertyField(m_EnableOutputSuspension, Styles.EnableOutputSuspension);

            EditorGUILayout.PropertyField(m_VirtualizeEffects, Styles.VirtualizeEffects);

            serializedObject.ApplyModifiedProperties();
        }

        [SettingsProvider]
        static SettingsProvider CreateProjectSettingsProvider()
        {
            if (s_SearchKeywords == null)
            {
                s_SearchKeywords = new List<string>();
                s_SearchKeywords.AddRange(SettingsProvider.GetSearchKeywordsFromGUIContentProperties<Styles>());
                s_SearchKeywords.AddRange(k_AdditionalSearchKeywords);
            }

            var provider = AssetSettingsProvider.CreateProviderFromAssetPath(
                "Project/Audio", "ProjectSettings/AudioManager.asset",
                s_SearchKeywords);
            return provider;
        }
    }
}
