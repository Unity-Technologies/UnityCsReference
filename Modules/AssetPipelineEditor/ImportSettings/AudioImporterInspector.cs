// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.Build;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEditor.Experimental.AssetImporters;
using System.Globalization;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [CustomEditor(typeof(AudioImporter))]
    [CanEditMultipleObjects]
    internal class AudioImporterInspector : AssetImporterEditor
    {
        static class Style
        {
            public static readonly GUIContent[] kSampleRateStrings = new[] {"8,000 Hz", "11,025 Hz", "22,050 Hz", "44,100 Hz", "48,000 Hz", "96,000 Hz", "192,000 Hz"}.Select(s => new GUIContent(s)).ToArray();
            public static readonly int[] kSampleRateValues = {8000, 11025, 22050, 44100, 48000, 96000, 192000};

            public static GUIContent LoadType = EditorGUIUtility.TrTextContent("Load Type");
            public static GUIContent PreloadAudioData = EditorGUIUtility.TrTextContent("Preload Audio Data*");
            public static GUIContent CompressionFormat = EditorGUIUtility.TrTextContent("Compression Format");
            public static GUIContent Quality = EditorGUIUtility.TrTextContent("Quality");
            public static GUIContent SampleRateSetting = EditorGUIUtility.TrTextContent("Sample Rate Setting");
            public static GUIContent SampleRate = EditorGUIUtility.TrTextContent("Sample Rate");
            public static GUIContent DefaultPlatform = EditorGUIUtility.TrTextContent("Default");
            public static GUIContent SharedSettingInformation = EditorGUIUtility.TrTextContent("* Shared setting between multiple platforms.");
        }

        public SerializedProperty m_ForceToMono;
        public SerializedProperty m_Normalize;
        public SerializedProperty m_PreloadAudioData;
        public SerializedProperty m_Ambisonic;
        public SerializedProperty m_LoadInBackground;
        public SerializedProperty m_OrigSize;
        public SerializedProperty m_CompSize;
        public SerializedProperty m_DefaultSampleSettings;

        bool m_SelectionContainsTrackerFile;

        [Serializable]
        class AudioImporterPlatformSettings
        {
            public BuildTargetGroup platform;
            public bool isOverridden;
            public AudioImporterSampleSettings settings;
        }

        class PlatformSettings : ScriptableObject
        {
            public List<AudioImporterPlatformSettings> sampleSettingOverrides;
        }

        protected override Type extraDataType => typeof(PlatformSettings);

        protected override void InitializeExtraDataInstance(Object extraData, int targetIndex)
        {
            var settings = extraData as PlatformSettings;
            var audioImporter = targets[targetIndex] as AudioImporter;
            if (settings != null && audioImporter != null)
            {
                // We need to sort them so every extraDataTarget have them ordered correctly and we can use serializedProperties.
                var validPlatforms = BuildPlatforms.instance.GetValidPlatforms().OrderBy(platform => platform.targetGroup);
                settings.sampleSettingOverrides = new List<AudioImporterPlatformSettings>(validPlatforms.Count());
                foreach (BuildPlatform platform in validPlatforms)
                {
                    var groupName = platform.targetGroup.ToString();
                    var sample = audioImporter.GetOverrideSampleSettings(groupName);
                    settings.sampleSettingOverrides.Add(new AudioImporterPlatformSettings()
                    {
                        platform = platform.targetGroup,
                        isOverridden = audioImporter.ContainsSampleSettingsOverride(groupName),
                        settings = sample
                    });
                }
            }
        }

        private IEnumerable<AudioImporter> GetAllAudioImporterTargets()
        {
            foreach (Object importer in targets)
            {
                AudioImporter audioImporter = importer as AudioImporter;
                if (audioImporter != null)
                    yield return audioImporter;
            }
        }

        private void SyncSettingsToBackend()
        {
            for (var index = 0; index < targets.Length; index++)
            {
                var audioImporter = targets[index] as AudioImporter;
                var settings = extraDataTargets[index] as PlatformSettings;
                if (settings != null && audioImporter != null)
                {
                    foreach (var setting in settings.sampleSettingOverrides)
                    {
                        if (setting.isOverridden)
                        {
                            audioImporter.SetOverrideSampleSettings(setting.platform.ToString(), setting.settings);
                        }
                        else if (audioImporter.ContainsSampleSettingsOverride(setting.platform.ToString()))
                        {
                            audioImporter.ClearSampleSettingOverride(setting.platform.ToString());
                        }
                    }
                }
            }
        }

        public bool CurrentPlatformHasAutoTranslatedCompression()
        {
            BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);

            foreach (AudioImporter importer in GetAllAudioImporterTargets())
            {
                AudioCompressionFormat defaultCompressionFormat = importer.defaultSampleSettings.compressionFormat;
                // Because we only want to query if the importer does not have an override.
                if (!importer.Internal_ContainsSampleSettingsOverride(targetGroup))
                {
                    AudioImporterSampleSettings overrideSettings = importer.Internal_GetOverrideSampleSettings(targetGroup);
                    AudioCompressionFormat overrideCompressionFormat = overrideSettings.compressionFormat;

                    // If we dont have an override, but the translated compression format is different,
                    // this means we have audio translate happening.
                    if (defaultCompressionFormat != overrideCompressionFormat)
                        return true;
                }
            }

            return false;
        }

        public bool IsHardwareSound(AudioCompressionFormat format)
        {
            switch (format)
            {
                case AudioCompressionFormat.HEVAG:
                case AudioCompressionFormat.VAG:
                case AudioCompressionFormat.XMA:
                case AudioCompressionFormat.GCADPCM:
                    return true;
                default:
                    return false;
            }
        }

        public bool CurrentSelectionContainsHardwareSounds()
        {
            BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);

            foreach (AudioImporter importer in GetAllAudioImporterTargets())
            {
                AudioImporterSampleSettings overrideSettings = importer.Internal_GetOverrideSampleSettings(targetGroup);
                if (IsHardwareSound(overrideSettings.compressionFormat))
                    return true;
            }

            return false;
        }

        public override void OnEnable()
        {
            base.OnEnable();

            m_ForceToMono = serializedObject.FindProperty("m_ForceToMono");
            m_Normalize = serializedObject.FindProperty("m_Normalize");
            m_PreloadAudioData = serializedObject.FindProperty("m_PreloadAudioData");
            m_Ambisonic = serializedObject.FindProperty("m_Ambisonic");
            m_LoadInBackground = serializedObject.FindProperty("m_LoadInBackground");
            m_OrigSize = serializedObject.FindProperty("m_PreviewData.m_OrigSize");
            m_CompSize = serializedObject.FindProperty("m_PreviewData.m_CompSize");

            m_DefaultSampleSettings = serializedObject.FindProperty("m_DefaultSettings");

            m_SelectionContainsTrackerFile = false;
            foreach (AudioImporter importer in GetAllAudioImporterTargets())
            {
                string assetPath = importer.assetPath;
                string ext = FileUtil.GetPathExtension(assetPath).ToLowerInvariant();
                if (ext == "mod" || ext == "it" || ext == "s3m" || ext == "xm")
                {
                    m_SelectionContainsTrackerFile = true;
                    break;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            extraDataSerializedObject.Update();

            OnAudioImporterGUI(m_SelectionContainsTrackerFile);

            int origSize = 0, compSize = 0;
            foreach (AudioImporter importer in GetAllAudioImporterTargets())
            {
                origSize += importer.origSize;
                compSize += importer.compSize;
            }

            GUILayout.Space(10);

            EditorGUILayout.HelpBox("Original Size: \t" + EditorUtility.FormatBytes(origSize) + "\nImported Size: \t" + EditorUtility.FormatBytes(compSize) + "\n" +
                "Ratio: \t\t" + (100.0f * (float)compSize / (float)origSize).ToString("0.00", CultureInfo.InvariantCulture.NumberFormat) + "%", MessageType.Info);

            if (CurrentPlatformHasAutoTranslatedCompression())
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("The selection contains different compression formats to the default settings for the current build platform.", MessageType.Info);
            }

            if (CurrentSelectionContainsHardwareSounds())
            {
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("The selection contains sounds that are decompressed in hardware. Advanced mixing is not available for these sounds.", MessageType.Info);
            }

            extraDataSerializedObject.ApplyModifiedProperties();
            serializedObject.ApplyModifiedProperties();

            ApplyRevertGUI();
        }

        private List<AudioCompressionFormat> GetFormatsForPlatform(BuildTargetGroup platform)
        {
            List<AudioCompressionFormat> allowedFormats = new List<AudioCompressionFormat>();

            //WebGL only supports AAC currently.
            if (platform == BuildTargetGroup.WebGL)
            {
                allowedFormats.Add(AudioCompressionFormat.AAC);
                return allowedFormats;
            }

            allowedFormats.Add(AudioCompressionFormat.PCM);

            allowedFormats.Add(AudioCompressionFormat.Vorbis);

            allowedFormats.Add(AudioCompressionFormat.ADPCM);

            if (platform != BuildTargetGroup.Standalone &&
                platform != BuildTargetGroup.WSA &&
                platform != BuildTargetGroup.XboxOne &&
                platform != BuildTargetGroup.Unknown)
            {
                allowedFormats.Add(AudioCompressionFormat.MP3);
            }

            if (platform == BuildTargetGroup.PS4)
            {
                allowedFormats.Add(AudioCompressionFormat.ATRAC9);
            }

            if (platform == BuildTargetGroup.XboxOne)
                allowedFormats.Add(AudioCompressionFormat.XMA);

            return allowedFormats;
        }

        private bool CompressionFormatHasQuality(AudioCompressionFormat format)
        {
            switch (format)
            {
                case AudioCompressionFormat.Vorbis:
                case AudioCompressionFormat.MP3:
                case AudioCompressionFormat.XMA:
                case AudioCompressionFormat.AAC:
                case AudioCompressionFormat.ATRAC9:
                    return true;
                default:
                    return false;
            }
        }

        private void OnSampleSettingGUI(BuildTargetGroup platform, SerializedProperty audioImporterSampleSettings, bool selectionContainsTrackerFile)
        {
            //Load Type
            var loadTypeProperty = audioImporterSampleSettings.FindPropertyRelative("loadType");
            using (var horizontal = new EditorGUILayout.HorizontalScope())
            {
                using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, Style.LoadType, loadTypeProperty))
                {
                    EditorGUI.showMixedValue = loadTypeProperty.hasMultipleDifferentValues;
                    using (var changed = new EditorGUI.ChangeCheckScope())
                    {
                        var newValue = (AudioClipLoadType)EditorGUILayout.EnumPopup(propertyScope.content, (AudioClipLoadType)loadTypeProperty.intValue);
                        if (changed.changed)
                        {
                            loadTypeProperty.intValue = (int)newValue;
                        }
                    }

                    EditorGUI.showMixedValue = false;
                }
            }

            //Preload Audio Data
            // If the loadtype is streaming on the selected platform, gray out the "Preload Audio Data" option and show the checkbox as unchecked.
            bool disablePreloadAudioDataOption = (AudioClipLoadType)loadTypeProperty.intValue == AudioClipLoadType.Streaming;
            using (new EditorGUI.DisabledScope(disablePreloadAudioDataOption))
            {
                if (disablePreloadAudioDataOption)
                    EditorGUILayout.Toggle("Preload Audio Data", false);
                else
                    EditorGUILayout.PropertyField(m_PreloadAudioData, Style.PreloadAudioData);
            }

            if (!selectionContainsTrackerFile)
            {
                //Compression format
                var compressionFormatProperty = audioImporterSampleSettings.FindPropertyRelative("compressionFormat");
                var allowedFormats = GetFormatsForPlatform(platform);
                using (var horizontal = new EditorGUILayout.HorizontalScope())
                {
                    using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, Style.CompressionFormat, compressionFormatProperty))
                    {
                        EditorGUI.showMixedValue = compressionFormatProperty.hasMultipleDifferentValues;
                        using (var changed = new EditorGUI.ChangeCheckScope())
                        {
                            var newValue = (AudioCompressionFormat)EditorGUILayout.IntPopup(
                                propertyScope.content,
                                compressionFormatProperty.intValue,
                                allowedFormats.Select(a => new GUIContent(a.ToString())).ToArray(),
                                allowedFormats.Select(a => (int)a).ToArray());
                            if (changed.changed)
                            {
                                compressionFormatProperty.intValue = (int)newValue;
                            }
                        }

                        EditorGUI.showMixedValue = false;
                    }
                }

                //Quality
                if (!compressionFormatProperty.hasMultipleDifferentValues && CompressionFormatHasQuality((AudioCompressionFormat)compressionFormatProperty.intValue))
                {
                    using (var horizontal = new EditorGUILayout.HorizontalScope())
                    {
                        var property = audioImporterSampleSettings.FindPropertyRelative("quality");
                        using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, Style.Quality, property))
                        {
                            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
                            using (var changed = new EditorGUI.ChangeCheckScope())
                            {
                                var newValue = EditorGUILayout.IntSlider(propertyScope.content, (int)Mathf.Clamp(property.floatValue * 100.0f + 0.5f, 1.0f, 100.0f), 1, 100);
                                if (changed.changed)
                                {
                                    property.floatValue = 0.01f * newValue;
                                }
                            }

                            EditorGUI.showMixedValue = false;
                        }
                    }
                }

                if (platform != BuildTargetGroup.WebGL)
                {
                    //Sample rate settings
                    var sampleRateSettingProperty = audioImporterSampleSettings.FindPropertyRelative("sampleRateSetting");
                    using (var horizontal = new EditorGUILayout.HorizontalScope())
                    {
                        using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, Style.SampleRateSetting, sampleRateSettingProperty))
                        {
                            EditorGUI.showMixedValue = sampleRateSettingProperty.hasMultipleDifferentValues;
                            using (var changed = new EditorGUI.ChangeCheckScope())
                            {
                                var newValue = (AudioSampleRateSetting)EditorGUILayout.EnumPopup(propertyScope.content, (AudioSampleRateSetting)sampleRateSettingProperty.intValue);
                                if (changed.changed)
                                {
                                    sampleRateSettingProperty.intValue = (int)newValue;
                                }
                            }

                            EditorGUI.showMixedValue = false;
                        }
                    }

                    //Sample rate override settings
                    if (!sampleRateSettingProperty.hasMultipleDifferentValues && (AudioSampleRateSetting)sampleRateSettingProperty.intValue == AudioSampleRateSetting.OverrideSampleRate)
                    {
                        using (var horizontal = new EditorGUILayout.HorizontalScope())
                        {
                            var property = audioImporterSampleSettings.FindPropertyRelative("sampleRateOverride");
                            using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, Style.SampleRate, property))
                            {
                                EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
                                using (var changed = new EditorGUI.ChangeCheckScope())
                                {
                                    var newValue = EditorGUILayout.IntPopup(propertyScope.content, property.intValue,
                                        Style.kSampleRateStrings, Style.kSampleRateValues);
                                    if (changed.changed)
                                    {
                                        property.intValue = newValue;
                                    }
                                }

                                EditorGUI.showMixedValue = false;
                            }
                        }
                    }
                }

                //TODO include the settings for things like HEVAG

                EditorGUILayout.LabelField(Style.SharedSettingInformation, EditorStyles.miniLabel);
            }
        }

        private void OnAudioImporterGUI(bool selectionContainsTrackerFile)
        {
            if (!selectionContainsTrackerFile)
            {
                EditorGUILayout.PropertyField(m_ForceToMono);
                EditorGUI.indentLevel++;
                using (new EditorGUI.DisabledScope(!m_ForceToMono.boolValue))
                {
                    EditorGUILayout.PropertyField(m_Normalize);
                }
                EditorGUI.indentLevel--;
                EditorGUILayout.PropertyField(m_LoadInBackground);
                EditorGUILayout.PropertyField(m_Ambisonic);
            }

            BuildPlatform[] validPlatforms = BuildPlatforms.instance.GetValidPlatforms().ToArray();
            GUILayout.Space(10);
            int shownSettingsPage = EditorGUILayout.BeginPlatformGrouping(validPlatforms, Style.DefaultPlatform);

            if (shownSettingsPage == -1)
            {
                OnSampleSettingGUI(BuildTargetGroup.Unknown, m_DefaultSampleSettings, selectionContainsTrackerFile);
            }
            else
            {
                BuildTargetGroup platform = validPlatforms[shownSettingsPage].targetGroup;
                SerializedProperty platformProperty = extraDataSerializedObject.FindProperty($"sampleSettingOverrides.Array.data[{shownSettingsPage}]");
                var isOverriddenProperty = platformProperty.FindPropertyRelative("isOverridden");

                // Define the UI state of the override here.
                using (var horizontal = new EditorGUILayout.HorizontalScope())
                {
                    var label = EditorGUIUtility.TrTempContent("Override for " + validPlatforms[shownSettingsPage].title.text);
                    using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, label, isOverriddenProperty))
                    {
                        EditorGUI.showMixedValue = isOverriddenProperty.hasMultipleDifferentValues;
                        using (var changed = new EditorGUI.ChangeCheckScope())
                        {
                            var newValue = EditorGUILayout.ToggleLeft(propertyScope.content, isOverriddenProperty.boolValue);
                            if (changed.changed)
                            {
                                isOverriddenProperty.boolValue = newValue;
                            }
                        }

                        EditorGUI.showMixedValue = false;
                    }
                }

                using (new EditorGUI.DisabledScope(isOverriddenProperty.hasMultipleDifferentValues || !isOverriddenProperty.boolValue))
                {
                    OnSampleSettingGUI(platform, platformProperty.FindPropertyRelative("settings"), selectionContainsTrackerFile);
                }
            }

            EditorGUILayout.EndPlatformGrouping();
        }

        protected override void Apply()
        {
            base.Apply();

            SyncSettingsToBackend();

            // This is necessary to enforce redrawing the static preview icons in the project browser, as properties like ForceToMono
            // may have changed the preview completely.
            foreach (ProjectBrowser pb in ProjectBrowser.GetAllProjectBrowsers())
                pb.Repaint();
        }
    }
}
