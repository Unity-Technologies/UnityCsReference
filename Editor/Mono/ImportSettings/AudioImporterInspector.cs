// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditor.Build;

using System.Collections.Generic;
using System;
using System.Linq;
using UnityEditor.Experimental.AssetImporters;

namespace UnityEditor
{
    [CustomEditor(typeof(AudioImporter))]
    [CanEditMultipleObjects]
    internal class AudioImporterInspector : AssetImporterEditor
    {
        private static class Styles
        {
            public static readonly string[] kSampleRateStrings = new[] {"8,000 Hz", "11,025 Hz", "22,050 Hz", "44,100 Hz", "48,000 Hz", "96,000 Hz", "192,000 Hz"};
            public static readonly int[]    kSampleRateValues  = new[] {8000, 11025, 22050, 44100, 48000, 96000, 192000};
        }

        private struct MultiValueStatus
        {
            public bool multiLoadType;
            public bool multiSampleRateSetting;
            public bool multiSampleRateOverride;
            public bool multiCompressionFormat;
            public bool multiQuality;
            public bool multiConversionMode;
        }

        private struct SampleSettingProperties
        {
            public AudioImporterSampleSettings settings;

            //Override the settings for all the targets (only used for the platform overrides)
            public bool forcedOverrideState;
            public bool overrideIsForced;

            //Overridden status
            public bool loadTypeChanged;
            public bool sampleRateSettingChanged;
            public bool sampleRateOverrideChanged;
            public bool compressionFormatChanged;
            public bool qualityChanged;
            public bool conversionModeChanged;

            public bool HasModified()
            {
                return overrideIsForced || loadTypeChanged || sampleRateSettingChanged || sampleRateOverrideChanged ||
                    compressionFormatChanged || qualityChanged || conversionModeChanged;
            }

            public void ClearChangedFlags()
            {
                forcedOverrideState = false;
                overrideIsForced = false;
                loadTypeChanged = false;
                sampleRateSettingChanged = false;
                sampleRateOverrideChanged = false;
                compressionFormatChanged = false;
                qualityChanged = false;
                conversionModeChanged = false;
            }
        }

        public SerializedProperty m_ForceToMono;
        public SerializedProperty m_Normalize;
        public SerializedProperty m_PreloadAudioData;
        public SerializedProperty m_Ambisonic;
        public SerializedProperty m_LoadInBackground;
        public SerializedProperty m_OrigSize;
        public SerializedProperty m_CompSize;

        private SampleSettingProperties m_DefaultSampleSettings;
        private Dictionary<BuildTargetGroup, SampleSettingProperties> m_SampleSettingOverrides;

        private IEnumerable<AudioImporter> GetAllAudioImporterTargets()
        {
            foreach (UnityEngine.Object importer in targets)
            {
                AudioImporter audioImporter = importer as AudioImporter;
                if (audioImporter != null)
                    yield return audioImporter;
            }
        }

        private bool SyncSettingsToBackend()
        {
            BuildPlatform[] validPlatforms = BuildPlatforms.instance.GetValidPlatforms().ToArray();

            foreach (AudioImporter importer in GetAllAudioImporterTargets())
            {
                {
                    AudioImporterSampleSettings importerDefaults = importer.defaultSampleSettings;

                    //Importer default settings
                    if (m_DefaultSampleSettings.loadTypeChanged)
                        importerDefaults.loadType = m_DefaultSampleSettings.settings.loadType;

                    if (m_DefaultSampleSettings.sampleRateSettingChanged)
                        importerDefaults.sampleRateSetting = m_DefaultSampleSettings.settings.sampleRateSetting;

                    if (m_DefaultSampleSettings.sampleRateOverrideChanged)
                        importerDefaults.sampleRateOverride = m_DefaultSampleSettings.settings.sampleRateOverride;

                    if (m_DefaultSampleSettings.compressionFormatChanged)
                        importerDefaults.compressionFormat = m_DefaultSampleSettings.settings.compressionFormat;

                    if (m_DefaultSampleSettings.qualityChanged)
                        importerDefaults.quality = m_DefaultSampleSettings.settings.quality;

                    if (m_DefaultSampleSettings.conversionModeChanged)
                        importerDefaults.conversionMode = m_DefaultSampleSettings.settings.conversionMode;

                    //Set the default settings on the importer after the changes.
                    importer.defaultSampleSettings = importerDefaults;
                }

                //Get all the valid platforms, and write changes only for those ones
                foreach (BuildPlatform platform in validPlatforms)
                {
                    BuildTargetGroup platformGroup = platform.targetGroup;

                    if (m_SampleSettingOverrides.ContainsKey(platformGroup))
                    {
                        SampleSettingProperties overrideProperties = m_SampleSettingOverrides[platformGroup];

                        if (overrideProperties.overrideIsForced && !overrideProperties.forcedOverrideState)
                        {
                            importer.Internal_ClearSampleSettingOverride(platformGroup);
                        }
                        else if (importer.Internal_ContainsSampleSettingsOverride(platformGroup) ||
                                 (overrideProperties.overrideIsForced && overrideProperties.forcedOverrideState))
                        {
                            AudioImporterSampleSettings overrideSettings = importer.Internal_GetOverrideSampleSettings(platformGroup);

                            if (overrideProperties.loadTypeChanged)
                                overrideSettings.loadType = overrideProperties.settings.loadType;

                            if (overrideProperties.sampleRateSettingChanged)
                                overrideSettings.sampleRateSetting = overrideProperties.settings.sampleRateSetting;

                            if (overrideProperties.sampleRateOverrideChanged)
                                overrideSettings.sampleRateOverride = overrideProperties.settings.sampleRateOverride;

                            if (overrideProperties.compressionFormatChanged)
                                overrideSettings.compressionFormat = overrideProperties.settings.compressionFormat;

                            if (overrideProperties.qualityChanged)
                                overrideSettings.quality = overrideProperties.settings.quality;

                            if (overrideProperties.conversionModeChanged)
                                overrideSettings.conversionMode = overrideProperties.settings.conversionMode;

                            //Set the default settings on the importer after the changes.
                            importer.Internal_SetOverrideSampleSettings(platformGroup, overrideSettings);
                        }

                        m_SampleSettingOverrides[platformGroup] = overrideProperties;
                    }
                }
            }

            //Now that we are in sync with the backend, we need to clear the changed flags within
            //the properties
            m_DefaultSampleSettings.ClearChangedFlags();

            foreach (BuildPlatform platform in validPlatforms)
            {
                BuildTargetGroup platformGroup = platform.targetGroup;
                if (m_SampleSettingOverrides.ContainsKey(platformGroup))
                {
                    SampleSettingProperties overrideProperties = m_SampleSettingOverrides[platformGroup];
                    overrideProperties.ClearChangedFlags();
                    m_SampleSettingOverrides[platformGroup] = overrideProperties;
                }
            }

            return true;
        }

        private void ResetSettingsFromBackend()
        {
            if (GetAllAudioImporterTargets().Any())
            {
                AudioImporter firstImporter = GetAllAudioImporterTargets().First();
                //Just load the settings from the first importer for the default settings
                m_DefaultSampleSettings.settings = firstImporter.defaultSampleSettings;
                m_DefaultSampleSettings.ClearChangedFlags();

                m_SampleSettingOverrides = new Dictionary<BuildTargetGroup, SampleSettingProperties>();
                List<BuildPlatform> validPlatforms = BuildPlatforms.instance.GetValidPlatforms();
                foreach (BuildPlatform platform in validPlatforms)
                {
                    BuildTargetGroup platformGroup = platform.targetGroup;
                    foreach (AudioImporter importer in GetAllAudioImporterTargets())
                    {
                        if (importer.Internal_ContainsSampleSettingsOverride(platformGroup))
                        {
                            SampleSettingProperties newProperties = new SampleSettingProperties();
                            newProperties.settings = importer.Internal_GetOverrideSampleSettings(platformGroup);

                            m_SampleSettingOverrides[platformGroup] = newProperties;

                            //Just grab the first settings we find from any of the importers.
                            //This will be sorted later by checking if there are any differences between importers.
                            break;
                        }
                    }

                    //If we failed to find a valid override setting, just create a default one for use later.
                    if (!m_SampleSettingOverrides.ContainsKey(platformGroup))
                    {
                        SampleSettingProperties newProperties = new SampleSettingProperties();
                        newProperties.settings = firstImporter.Internal_GetOverrideSampleSettings(platformGroup);

                        m_SampleSettingOverrides[platformGroup] = newProperties;
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
            m_ForceToMono = serializedObject.FindProperty("m_ForceToMono");
            m_Normalize = serializedObject.FindProperty("m_Normalize");
            m_PreloadAudioData = serializedObject.FindProperty("m_PreloadAudioData");
            m_Ambisonic = serializedObject.FindProperty("m_Ambisonic");
            m_LoadInBackground = serializedObject.FindProperty("m_LoadInBackground");
            m_OrigSize = serializedObject.FindProperty("m_PreviewData.m_OrigSize");
            m_CompSize = serializedObject.FindProperty("m_PreviewData.m_CompSize");

            ResetSettingsFromBackend();
        }

        protected override void ResetValues()
        {
            base.ResetValues();
            ResetSettingsFromBackend();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            bool selectionContainsTrackerFile = false;
            foreach (AudioImporter importer in GetAllAudioImporterTargets())
            {
                string assetPath = importer.assetPath;
                string ext = FileUtil.GetPathExtension(assetPath).ToLowerInvariant();
                if (ext == "mod" || ext == "it" || ext == "s3m" || ext == "xm")
                {
                    selectionContainsTrackerFile = true;
                    break;
                }
            }

            OnAudioImporterGUI(selectionContainsTrackerFile);

            int origSize = 0, compSize = 0;
            foreach (AudioImporter importer in GetAllAudioImporterTargets())
            {
                origSize += importer.origSize;
                compSize += importer.compSize;
            }

            GUILayout.Space(10);

            EditorGUILayout.HelpBox("Original Size: \t" + EditorUtility.FormatBytes(origSize) + "\nImported Size: \t" + EditorUtility.FormatBytes(compSize) + "\n" +
                "Ratio: \t\t" + (100.0f * (float)compSize / (float)origSize).ToString("0.00") + "%", MessageType.Info);


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

            ApplyRevertGUI();
        }

        private MultiValueStatus GetMultiValueStatus(BuildTargetGroup platform)
        {
            MultiValueStatus status;
            status.multiLoadType = false;
            status.multiSampleRateSetting = false;
            status.multiSampleRateOverride = false;
            status.multiCompressionFormat = false;
            status.multiQuality = false;
            status.multiConversionMode = false;

            AudioImporterSampleSettings settings;

            if (GetAllAudioImporterTargets().Any())
            {
                //We have at least one AudioImporter
                AudioImporter firstImporter = GetAllAudioImporterTargets().First();

                if (platform == BuildTargetGroup.Unknown)
                    settings = firstImporter.defaultSampleSettings;
                else
                    settings = firstImporter.Internal_GetOverrideSampleSettings(platform);

                foreach (AudioImporter importer in GetAllAudioImporterTargets().Except(new[] {firstImporter}))
                {
                    AudioImporterSampleSettings compareSettings;
                    if (platform == BuildTargetGroup.Unknown)
                        compareSettings = importer.defaultSampleSettings;
                    else
                        compareSettings = importer.Internal_GetOverrideSampleSettings(platform);

                    status.multiLoadType           |= settings.loadType != compareSettings.loadType;
                    status.multiSampleRateSetting  |= settings.sampleRateSetting != compareSettings.sampleRateSetting;
                    status.multiSampleRateOverride |= settings.sampleRateOverride != compareSettings.sampleRateOverride;
                    status.multiCompressionFormat  |= settings.compressionFormat != compareSettings.compressionFormat;
                    status.multiQuality            |= settings.quality != compareSettings.quality;
                    status.multiConversionMode     |= settings.conversionMode != compareSettings.conversionMode;
                }
            }

            return status;
        }

        enum OverrideStatus
        {
            NoOverrides,
            MixedOverrides,
            AllOverrides
        }

        private OverrideStatus GetOverrideStatus(BuildTargetGroup platform)
        {
            bool mixedOverrides = false;
            bool containsOverride = false;

            if (GetAllAudioImporterTargets().Any())
            {
                AudioImporter firstImporter = GetAllAudioImporterTargets().First();

                containsOverride = firstImporter.Internal_ContainsSampleSettingsOverride(platform);
                foreach (AudioImporter importer in GetAllAudioImporterTargets().Except(new[] {firstImporter}))
                {
                    bool overrideState = importer.Internal_ContainsSampleSettingsOverride(platform);

                    if (overrideState != containsOverride)
                        mixedOverrides |= true;

                    containsOverride |= overrideState;
                }
            }

            if (!containsOverride)
                return OverrideStatus.NoOverrides;
            else if (mixedOverrides)
                return OverrideStatus.MixedOverrides;
            else
                return OverrideStatus.AllOverrides;
        }

        private AudioCompressionFormat[] GetFormatsForPlatform(BuildTargetGroup platform)
        {
            List<AudioCompressionFormat> allowedFormats = new List<AudioCompressionFormat>();

            //WebGL only supports AAC currently.
            if (platform == BuildTargetGroup.WebGL)
            {
                allowedFormats.Add(AudioCompressionFormat.AAC);
                return allowedFormats.ToArray();
            }

            allowedFormats.Add(AudioCompressionFormat.PCM);

            if (platform != BuildTargetGroup.PSM && // Currently Vorbis is not supported on Vita
                platform != BuildTargetGroup.PSP2)
            {
                allowedFormats.Add(AudioCompressionFormat.Vorbis);
            }

            allowedFormats.Add(AudioCompressionFormat.ADPCM);

            if (platform != BuildTargetGroup.Standalone &&
                platform != BuildTargetGroup.WSA &&
                platform != BuildTargetGroup.WiiU &&
                platform != BuildTargetGroup.XboxOne &&
                platform != BuildTargetGroup.Unknown)
            {
                allowedFormats.Add(AudioCompressionFormat.MP3);
            }

            if (platform == BuildTargetGroup.PSM)
                allowedFormats.Add(AudioCompressionFormat.VAG);

            if (platform == BuildTargetGroup.PSP2)
            {
                allowedFormats.Add(AudioCompressionFormat.HEVAG);
                allowedFormats.Add(AudioCompressionFormat.ATRAC9);
            }

            if (platform == BuildTargetGroup.PS4)
            {
                allowedFormats.Add(AudioCompressionFormat.ATRAC9);
            }

            if (platform == BuildTargetGroup.WiiU)
                allowedFormats.Add(AudioCompressionFormat.GCADPCM);

            if (platform == BuildTargetGroup.XboxOne)
                allowedFormats.Add(AudioCompressionFormat.XMA);

            return allowedFormats.ToArray();
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

        private void OnSampleSettingGUI(BuildTargetGroup platform, MultiValueStatus status, bool selectionContainsTrackerFile, ref SampleSettingProperties properties, bool disablePreloadAudioDataOption)
        {
            //Load Type
            EditorGUI.showMixedValue = status.multiLoadType && !properties.loadTypeChanged;
            EditorGUI.BeginChangeCheck();
            AudioClipLoadType newLoadType = (AudioClipLoadType)EditorGUILayout.EnumPopup("Load Type", properties.settings.loadType);
            if (EditorGUI.EndChangeCheck())
            {
                properties.settings.loadType = newLoadType;
                properties.loadTypeChanged = true;
            }

            using (new EditorGUI.DisabledScope(disablePreloadAudioDataOption))
            {
                if (disablePreloadAudioDataOption)
                    EditorGUILayout.Toggle("Preload Audio Data", false);
                else
                    EditorGUILayout.PropertyField(m_PreloadAudioData);
            }

            if (!selectionContainsTrackerFile)
            {
                //Compression format
                AudioCompressionFormat[] allowedFormats = GetFormatsForPlatform(platform);
                EditorGUI.showMixedValue = status.multiCompressionFormat && !properties.compressionFormatChanged;
                EditorGUI.BeginChangeCheck();
                AudioCompressionFormat newFormat = (AudioCompressionFormat)EditorGUILayout.IntPopup("Compression Format",
                        (int)properties.settings.compressionFormat,
                        Array.ConvertAll(allowedFormats, value => value.ToString()),
                        Array.ConvertAll(allowedFormats, value => (int)value));
                if (EditorGUI.EndChangeCheck())
                {
                    properties.settings.compressionFormat = newFormat;
                    properties.compressionFormatChanged = true;
                }

                //Quality
                if (CompressionFormatHasQuality(properties.settings.compressionFormat))
                {
                    EditorGUI.showMixedValue = status.multiQuality && !properties.qualityChanged;
                    EditorGUI.BeginChangeCheck();
                    int newQuality = EditorGUILayout.IntSlider("Quality", (int)Mathf.Clamp(properties.settings.quality * 100.0f, 1.0f, 100.0f), 1, 100);
                    if (EditorGUI.EndChangeCheck())
                    {
                        properties.settings.quality = 0.01f * newQuality;
                        properties.qualityChanged = true;
                    }
                }

                if (platform != BuildTargetGroup.WebGL)
                {
                    //Sample rate settings
                    EditorGUI.showMixedValue = status.multiSampleRateSetting && !properties.sampleRateSettingChanged;
                    EditorGUI.BeginChangeCheck();
                    AudioSampleRateSetting newSetting = (AudioSampleRateSetting)EditorGUILayout.EnumPopup("Sample Rate Setting", properties.settings.sampleRateSetting);
                    if (EditorGUI.EndChangeCheck())
                    {
                        properties.settings.sampleRateSetting = newSetting;
                        properties.sampleRateSettingChanged = true;
                    }

                    //Sample rate settings
                    if (properties.settings.sampleRateSetting == AudioSampleRateSetting.OverrideSampleRate)
                    {
                        EditorGUI.showMixedValue = status.multiSampleRateOverride && !properties.sampleRateOverrideChanged;
                        EditorGUI.BeginChangeCheck();
                        int newRate = EditorGUILayout.IntPopup("Sample Rate", (int)properties.settings.sampleRateOverride,
                                Styles.kSampleRateStrings, Styles.kSampleRateValues);
                        if (EditorGUI.EndChangeCheck())
                        {
                            properties.settings.sampleRateOverride = (uint)newRate;
                            properties.sampleRateOverrideChanged = true;
                        }
                    }
                }

                //TODO include the settings for things like HEVAG
                EditorGUI.showMixedValue = false;
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
            int shownSettingsPage = EditorGUILayout.BeginPlatformGrouping(validPlatforms, GUIContent.Temp("Default"));

            if (shownSettingsPage == -1)
            {
                // If the loadtype is streaming on the selected platform, gray out the "Preload Audio Data" option and show the checkbox as unchecked.
                bool disablePreloadAudioDataOption = (m_DefaultSampleSettings.settings.loadType == AudioClipLoadType.Streaming);

                MultiValueStatus multiStatus = GetMultiValueStatus(BuildTargetGroup.Unknown);
                OnSampleSettingGUI(BuildTargetGroup.Unknown, multiStatus, selectionContainsTrackerFile, ref m_DefaultSampleSettings, disablePreloadAudioDataOption);
            }
            else
            {
                BuildTargetGroup platform = validPlatforms[shownSettingsPage].targetGroup;
                SampleSettingProperties properties = m_SampleSettingOverrides[platform];
                OverrideStatus status = GetOverrideStatus(platform);

                // Define the UI state of the override here.
                EditorGUI.BeginChangeCheck();
                EditorGUI.showMixedValue = (status == OverrideStatus.MixedOverrides) && !properties.overrideIsForced;
                bool overrideState = (properties.overrideIsForced && properties.forcedOverrideState) ||
                    (!properties.overrideIsForced && status != OverrideStatus.NoOverrides);
                overrideState = EditorGUILayout.ToggleLeft("Override for " + validPlatforms[shownSettingsPage].title.text, overrideState);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    properties.forcedOverrideState = overrideState;
                    properties.overrideIsForced = true;
                }

                // If the loadtype is streaming on the selected platform, gray out the "Preload Audio Data" option and show the checkbox as unchecked.
                bool disablePreloadAudioDataOption = ((properties.overrideIsForced && properties.forcedOverrideState) || GetOverrideStatus(platform) == OverrideStatus.AllOverrides) && properties.settings.loadType == AudioClipLoadType.Streaming;

                MultiValueStatus multiStatus = GetMultiValueStatus(platform);
                bool platformSettingsDisabled = !((properties.overrideIsForced && properties.forcedOverrideState) || status == OverrideStatus.AllOverrides);

                using (new EditorGUI.DisabledScope(platformSettingsDisabled))
                {
                    OnSampleSettingGUI(platform, multiStatus, selectionContainsTrackerFile, ref properties, disablePreloadAudioDataOption);
                }

                m_SampleSettingOverrides[platform] = properties;
            }

            EditorGUILayout.EndPlatformGrouping();
        }

        public override bool HasModified()
        {
            if (base.HasModified())
                return true;

            if (m_DefaultSampleSettings.HasModified())
                return true;

            //Iterate over all the override settings
            Dictionary<BuildTargetGroup, SampleSettingProperties>.ValueCollection valueColl = m_SampleSettingOverrides.Values;
            foreach (SampleSettingProperties props in valueColl)
            {
                if (props.HasModified())
                    return true;
            }

            return false;
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
