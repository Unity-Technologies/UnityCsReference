// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditorInternal;

using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.Build;

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
            public static readonly GUIContent AudioFoundation        = L10n.TextContent("Enhanced Audio Foundation", "Use the new low-level platform audio layer, with asynchronous starting and stopping of devices and greater control over audio engine behavior. When disabled, the classic platform layer from previous versions of Unity is used. Platforms that don't support the enhanced foundation will fall back to the classic one.", null, null);
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
            public static readonly GUIContent EnhancedAudioFoundationInfo = L10n.TextContent("Classic Audio Settings will be used on WebGL, tvOS, visionOS, Universal Windows Platform, PlayStation®4, Embedded Linux, QNX, and Android (below 8.1), as Enhanced is not supported on these platforms.", null, null, null);
            public static readonly GUIContent PassthroughChannelLayoutInfo = L10n.TextContent("This layout is sent directly to the OS spatializer instead of being down-mixed on macOS, Windows, XBOX Series X|S, XBOX One devices, on Meta Quest devices and on Android devices that support spatial audio.", null, null, null);
            public static readonly GUIContent ClassicAudioHeader = L10n.TextContent("Classic Audio Foundation", null, null, null);
            public static readonly GUIContent StandalonePassthroughChannelLayoutInfo = L10n.TextContent("This layout is sent directly to the OS spatializer instead of being down-mixed on macOS and Windows.", null, null, null);
            public static readonly GUIContent XboxPassthroughChannelLayoutInfo = L10n.TextContent("This layout is sent directly to the OS spatializer instead of being down-mixed.", null, null, null);
            public static readonly GUIContent AndroidPassthroughChannelLayoutInfo = L10n.TextContent("This layout is sent directly to the OS spatializer instead of being down-mixed on Meta Quest devices and on Android devices that support spatial audio.", null, null, null);
            public static readonly GUIContent DefaultPlatformTab = L10n.TextContent("Default", null, null, null);
            public static readonly GUIContent AndroidFallbackClassicInfo = L10n.TextContent("Classic Audio Settings will be used on Android devices below 8.1, as Enhanced is not supported on these devices.", null, null, null);
            public static readonly GUIContent EnhancedAudioFoundationNotSupportedInfo = L10n.TextContent("Classic Audio Settings will be used on {0}, as Enhanced is not supported on this platform.", null, null, null);
            public static readonly GUIContent ResetToDefault = L10n.TextContent("Reset to Default", "Replace this platform's settings with the current Default settings.", null, null);
            public static readonly GUIContent OverrideForPlatform = L10n.TextContent("Override for {0}", "When enabled, these settings replace the Default settings when building for this platform, and in Play mode when it is the active build target.", null, null);
        }

        enum Setting
        {
            Volume,
            RolloffScale,
            DopplerFactor,
            SpeakerMode,
            SampleRate,
            RequestedDSPBufferSize,
            AudioFoundation,
            OutputChannelLayout,
            OutputSamplingRate,
            VirtualVoiceCount,
            RealVoiceCount,
            SpatializerPlugin,
            AmbisonicDecoderPlugin,
            DisableAudio,
            VirtualizeEffects,
        }

        static readonly (string shared, string platform)[] k_SettingNames =
        {
            ("m_Volume", "volume"),
            ("Rolloff Scale", "rolloffScale"),
            ("Doppler Factor", "dopplerFactor"),
            ("Default Speaker Mode", "speakerMode"),
            ("m_SampleRate", "sampleRate"),
            ("m_RequestedDSPBufferSize", "requestedDSPBufferSize"),
            ("m_AudioFoundation", "audioFoundation"),
            ("m_OutputChannelLayout", "outputChannelLayout"),
            ("m_OutputSamplingRate", "outputSamplingRate"),
            ("m_VirtualVoiceCount", "virtualVoiceCount"),
            ("m_RealVoiceCount", "realVoiceCount"),
            ("m_SpatializerPlugin", "spatializerPlugin"),
            ("m_AmbisonicDecoderPlugin", "ambisonicDecoderPlugin"),
            ("m_DisableAudio", "disableAudio"),
            ("m_VirtualizeEffects", "virtualizeEffects"),
        };

        static readonly string[] k_AdditionalSearchKeywords =
        {
            "enhanced audio foundation",
        };

        [NoAutoStaticsCleanup] // lazy one-time cache of static GUIContent search keyword strings, no user-code refs; safe to persist
        static List<string> s_SearchKeywords;

        private SerializedProperty[] m_SharedSettings;
        private SerializedProperty m_EnableOutputSuspension;
        private SerializedProperty m_ActualDSPBufferSize;
        private SerializedProperty m_PerPlatformAudioSettings;

        private BuildPlatform[] m_ValidPlatforms;
        private string[] m_SpatializerPluginNames;
        private GUIContent[] m_SpatializerPluginContents;
        private string[] m_AmbisonicDecoderPluginNames;
        private GUIContent[] m_AmbisonicDecoderPluginContents;

        private void OnEnable()
        {
            m_SharedSettings = new SerializedProperty[k_SettingNames.Length];
            for (int i = 0; i < k_SettingNames.Length; i++)
                m_SharedSettings[i] = serializedObject.FindProperty(k_SettingNames[i].shared);

            m_EnableOutputSuspension    = serializedObject.FindProperty("m_EnableOutputSuspension");
            m_ActualDSPBufferSize       = serializedObject.FindProperty("m_DSPBufferSize");
            m_PerPlatformAudioSettings  = serializedObject.FindProperty("m_PerPlatformAudioSettings");

            m_ValidPlatforms = BuildPlatforms.instance.GetValidPlatforms().ToArray();
        }

        //This function assumes that index 0 is None...
        private static int FindPluginStringIndex(string[] strs, string element)
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

            UpdatePluginNames();

            int selectedPlatform = EditorGUILayout.BeginPlatformGrouping(m_ValidPlatforms, StylesNonSearchable.DefaultPlatformTab, EditorStyles.frameBox, IsPlatformOverridden);
            if (selectedPlatform == -1)
                DrawSettingsForPlatformTab(m_SharedSettings, null);
            else
                DrawPlatformSettings(m_ValidPlatforms[selectedPlatform]);
            EditorGUILayout.EndPlatformGrouping();

            serializedObject.ApplyModifiedProperties();
        }

        private void UpdatePluginNames()
        {
            List<string> plugins = new List<string>(AudioSettings.GetSpatializerPluginNames());
            plugins.Insert(0, "None");
            m_SpatializerPluginNames = plugins.ToArray();

            List<GUIContent> pluginsGUIContent = new List<GUIContent>();
            foreach (var s in m_SpatializerPluginNames)
                pluginsGUIContent.Add(new GUIContent(s));
            m_SpatializerPluginContents = pluginsGUIContent.ToArray();

            List<string> ambisonicDecoderPlugins = new List<string>(AudioUtil.GetAmbisonicDecoderPluginNames());
            ambisonicDecoderPlugins.Insert(0, "None");
            m_AmbisonicDecoderPluginNames = ambisonicDecoderPlugins.ToArray();

            List<GUIContent> ambisonicDecoderPluginsGUIContent = new List<GUIContent>();
            foreach (var s in m_AmbisonicDecoderPluginNames)
                ambisonicDecoderPluginsGUIContent.Add(new GUIContent(s));
            m_AmbisonicDecoderPluginContents = ambisonicDecoderPluginsGUIContent.ToArray();
        }

        private static void PluginPopup(GUIContent label, SerializedProperty property, string[] pluginNames, GUIContent[] pluginContents)
        {
            EditorGUI.BeginChangeCheck();
            int pluginIndex = FindPluginStringIndex(pluginNames, property.stringValue);
            pluginIndex = EditorGUILayout.Popup(label, pluginIndex, pluginContents);
            if (EditorGUI.EndChangeCheck())
            {
                if (pluginIndex == 0)
                    property.stringValue = "";
                else
                    property.stringValue = pluginNames[pluginIndex];
            }
        }

        private void DrawSettingsForPlatformTab(SerializedProperty[] settings, BuildPlatform platform)
        {
            bool isShared = platform == null;
            bool supportsEnhancedAudioFoundation = isShared || AudioUtil.SupportsEnhancedAudioFoundation(platform.defaultTarget);

            SerializedProperty audioFoundation = settings[(int)Setting.AudioFoundation];
            SerializedProperty outputChannelLayout = settings[(int)Setting.OutputChannelLayout];
            SerializedProperty requestedDSPBufferSize = settings[(int)Setting.RequestedDSPBufferSize];
            SerializedProperty disableAudio = settings[(int)Setting.DisableAudio];

            bool isEnhanced = supportsEnhancedAudioFoundation && audioFoundation.intValue == (int)AudioFoundation.Enhanced;

            // A platform that runs Enhanced has no fallback to configure, except Android devices below 8.1
            bool isAndroid = !isShared && platform.defaultTarget == BuildTarget.Android;
            bool classicSettingsApply = isShared || !isEnhanced || isAndroid;

            if (supportsEnhancedAudioFoundation)
            {
                EditorGUI.BeginChangeCheck();
                bool enhanced = EditorGUILayout.Toggle(Styles.AudioFoundation, isEnhanced);
                if (EditorGUI.EndChangeCheck())
                    audioFoundation.intValue = (int)(enhanced ? AudioFoundation.Enhanced : AudioFoundation.Classic);

                using (new EditorGUI.DisabledScope(!isEnhanced))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(outputChannelLayout, Styles.OutputChannelLayout);
                    if (outputChannelLayout.intValue == (int)ChannelLayoutBehavior.Surround_7_1_4)
                    {
                        GUIContent passthroughInfo = GetPassthroughChannelLayoutInfo(platform);
                        if (passthroughInfo != null)
                            EditorGUILayout.HelpBox(passthroughInfo.text, MessageType.Info);
                    }
                    EditorGUILayout.PropertyField(settings[(int)Setting.OutputSamplingRate], Styles.OutputSamplingRate);
                    EditorGUI.indentLevel--;
                }
            }

            using (new EditorGUI.DisabledScope(!classicSettingsApply))
            {
                EditorGUILayout.LabelField(StylesNonSearchable.ClassicAudioHeader);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(settings[(int)Setting.SpeakerMode], Styles.DefaultSpeakerMode);
                EditorGUILayout.DelayedIntField(settings[(int)Setting.SampleRate], Styles.SampleRate);
                if (!supportsEnhancedAudioFoundation)
                    EditorGUILayout.HelpBox(string.Format(StylesNonSearchable.EnhancedAudioFoundationNotSupportedInfo.text, platform.title.text), MessageType.Info);
                else if (isEnhanced && isShared)
                    EditorGUILayout.HelpBox(StylesNonSearchable.EnhancedAudioFoundationInfo.text, MessageType.Info);
                else if (isEnhanced && isAndroid)
                    EditorGUILayout.HelpBox(StylesNonSearchable.AndroidFallbackClassicInfo.text, MessageType.Info);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(settings[(int)Setting.Volume], Styles.Volume);
            EditorGUILayout.PropertyField(settings[(int)Setting.RolloffScale], Styles.RolloffScale);
            EditorGUILayout.PropertyField(settings[(int)Setting.DopplerFactor], Styles.DopplerFactor);
            EditorGUILayout.PropertyField(requestedDSPBufferSize, Styles.DSPBufferSize);

            // The actual buffer size only reflects the running engine, which uses the shared settings
            if (isShared && requestedDSPBufferSize.intValue != m_ActualDSPBufferSize.intValue)
                EditorGUILayout.HelpBox(
                    string.Format(StylesNonSearchable.DSPBufferSizeInfo.text,
                        requestedDSPBufferSize.intValue == 0 ? "default" : requestedDSPBufferSize.intValue.ToString(),
                        m_ActualDSPBufferSize.intValue),
                    MessageType.Info);

            EditorGUILayout.PropertyField(settings[(int)Setting.VirtualVoiceCount], Styles.VirtualVoiceCount);
            EditorGUILayout.PropertyField(settings[(int)Setting.RealVoiceCount], Styles.RealVoiceCount);

            PluginPopup(Styles.SpatializerPlugin, settings[(int)Setting.SpatializerPlugin], m_SpatializerPluginNames, m_SpatializerPluginContents);
            PluginPopup(Styles.AmbisonicDecoderPlugin, settings[(int)Setting.AmbisonicDecoderPlugin], m_AmbisonicDecoderPluginNames, m_AmbisonicDecoderPluginContents);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(disableAudio, Styles.DisableAudio);

            if (EditorGUI.EndChangeCheck() && isShared && disableAudio.boolValue.Equals(true))
            {
                AudioUtil.StopAllPreviewClips();
            }

            if (isShared)
                EditorGUILayout.PropertyField(m_EnableOutputSuspension, Styles.EnableOutputSuspension);

            EditorGUILayout.PropertyField(settings[(int)Setting.VirtualizeEffects], Styles.VirtualizeEffects);
        }

        // Returns null for platforms that always down-mix the layout
        private static GUIContent GetPassthroughChannelLayoutInfo(BuildPlatform platform)
        {
            if (platform == null)
                return StylesNonSearchable.PassthroughChannelLayoutInfo;

            if (platform.namedBuildTarget == NamedBuildTarget.Server)
                return null;

            switch (platform.namedBuildTarget.ToBuildTargetGroup())
            {
                case BuildTargetGroup.Standalone:
                    return StylesNonSearchable.StandalonePassthroughChannelLayoutInfo;
                case BuildTargetGroup.GameCoreXboxSeries:
                case BuildTargetGroup.GameCoreXboxOne:
                    return StylesNonSearchable.XboxPassthroughChannelLayoutInfo;
                case BuildTargetGroup.Android:
                    return StylesNonSearchable.AndroidPassthroughChannelLayoutInfo;
                default:
                    return null;
            }
        }

        private void DrawPlatformSettings(BuildPlatform platform)
        {
            string platformName = platform.namedBuildTarget.TargetName;
            SerializedProperty platformOverride = FindPlatformOverride(platformName);
            bool isOverridden = platformOverride != null && platformOverride.FindPropertyRelative("overridden").boolValue;

            var overrideLabel = new GUIContent(string.Format(StylesNonSearchable.OverrideForPlatform.text, platform.title.text), StylesNonSearchable.OverrideForPlatform.tooltip);
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            isOverridden = EditorGUILayout.ToggleLeft(overrideLabel, isOverridden);
            if (EditorGUI.EndChangeCheck())
            {
                if (platformOverride == null)
                    platformOverride = AddPlatformOverride(platform);
                platformOverride.FindPropertyRelative("overridden").boolValue = isOverridden;
            }

            using (new EditorGUI.DisabledScope(!isOverridden))
            {
                if (GUILayout.Button(StylesNonSearchable.ResetToDefault, GUILayout.ExpandWidth(false)))
                    CopySharedSettings(platformOverride, platform);
            }
            EditorGUILayout.EndHorizontal();

            // Show the Default settings, read-only, until the platform is overridden
            using (new EditorGUI.DisabledScope(!isOverridden))
                DrawSettingsForPlatformTab(isOverridden ? GetPlatformSettings(platformOverride) : m_SharedSettings, platform);
        }

        private static SerializedProperty[] GetPlatformSettings(SerializedProperty platformOverride)
        {
            var settings = new SerializedProperty[k_SettingNames.Length];
            for (int i = 0; i < k_SettingNames.Length; i++)
                settings[i] = platformOverride.FindPropertyRelative(k_SettingNames[i].platform);
            return settings;
        }

        private static void CopyValue(SerializedProperty source, SerializedProperty destination)
        {
            switch (destination.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    destination.boolValue = source.boolValue;
                    break;
                case SerializedPropertyType.String:
                    destination.stringValue = source.stringValue;
                    break;
                case SerializedPropertyType.Float:
                    destination.floatValue = source.floatValue;
                    break;
                default:
                    destination.intValue = source.intValue;
                    break;
            }
        }

        private SerializedProperty FindPlatformOverride(string platformName)
        {
            for (int i = 0; i < m_PerPlatformAudioSettings.arraySize; i++)
            {
                SerializedProperty entry = m_PerPlatformAudioSettings.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("first").stringValue == platformName)
                    return entry.FindPropertyRelative("second");
            }

            return null;
        }

        // New platform settings start as a copy of the Default settings
        private SerializedProperty AddPlatformOverride(BuildPlatform platform)
        {
            int index = m_PerPlatformAudioSettings.arraySize;
            m_PerPlatformAudioSettings.InsertArrayElementAtIndex(index);
            SerializedProperty entry = m_PerPlatformAudioSettings.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("first").stringValue = platform.namedBuildTarget.TargetName;

            SerializedProperty platformOverride = entry.FindPropertyRelative("second");
            CopySharedSettings(platformOverride, platform);
            return platformOverride;
        }

        private void CopySharedSettings(SerializedProperty platformOverride, BuildPlatform platform)
        {
            SerializedProperty[] platformSettings = GetPlatformSettings(platformOverride);
            for (int i = 0; i < platformSettings.Length; i++)
                CopyValue(m_SharedSettings[i], platformSettings[i]);

            // The Default settings may select Enhanced on a platform that does not support it
            if (!AudioUtil.SupportsEnhancedAudioFoundation(platform.defaultTarget))
                platformSettings[(int)Setting.AudioFoundation].intValue = (int)AudioFoundation.Classic;
        }

        private bool IsPlatformOverridden(int platformIndex)
        {
            if (platformIndex < 0)
                return false;

            SerializedProperty platformOverride = FindPlatformOverride(m_ValidPlatforms[platformIndex].namedBuildTarget.TargetName);
            return platformOverride != null && platformOverride.FindPropertyRelative("overridden").boolValue;
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
