// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor.AnimatedValues;
using UnityEditor.Build;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [CustomPreview(typeof(VideoClipImporter))]
    internal class VideoClipImporterSourcePreview : ObjectPreview
    {
        class Styles
        {
            public GUIStyle labelStyle = "VideoClipImporterLabel";
        }

        private Styles m_Styles = new Styles();

        private GUIContent m_Title;

        private const float kLabelWidth = 120;
        private const float kIndentWidth = 30;
        private const float kValueWidth = 200;

        public override GUIContent GetPreviewTitle()
        {
            if (m_Title == null)
                m_Title = EditorGUIUtility.TrTextContent("Source Info");
            return m_Title;
        }

        public override bool HasPreviewGUI()
        {
            var importer = target as VideoClipImporter;
            return importer != null;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            var importer = (VideoClipImporter)target;

            RectOffset previewPadding = new RectOffset(-5, -5, -5, -5);
            r = previewPadding.Add(r);

            // Prepare rects for columns
            r.height = EditorGUIUtility.singleLineHeight;
            Rect labelRect = r;
            Rect valueRect = r;
            labelRect.width = kLabelWidth;
            valueRect.xMin += kLabelWidth;
            valueRect.width = kValueWidth;

            ShowProperty(ref labelRect, ref valueRect,
                "Original Size", EditorUtility.FormatBytes((long)importer.sourceFileSize));
            ShowProperty(ref labelRect, ref valueRect,
                "Imported Size", EditorUtility.FormatBytes((long)importer.outputFileSize));

            var frameCount = importer.frameCount;
            var frameRate = importer.frameRate;
            var duration = frameRate > 0
                ? TimeSpan.FromSeconds(frameCount / frameRate).ToString()
                : new TimeSpan(0).ToString();

            // TimeSpan uses 7 digits for fractional seconds.  Limit this to 3 digits.
            if (duration.IndexOf('.') != -1)
                duration = duration.Substring(0, duration.Length - 4);
            ShowProperty(ref labelRect, ref valueRect, "Duration", duration);
            ShowProperty(ref labelRect, ref valueRect, "Frames", frameCount.ToString());
            ShowProperty(ref labelRect, ref valueRect, "FPS", frameRate.ToString("F2", CultureInfo.InvariantCulture.NumberFormat));

            var originalWidth = importer.GetResizeWidth(VideoResizeMode.OriginalSize);
            var originalHeight = importer.GetResizeHeight(VideoResizeMode.OriginalSize);
            ShowProperty(ref labelRect, ref valueRect, "Pixels", originalWidth + "x" + originalHeight);
            ShowProperty(ref labelRect, ref valueRect, "PAR", importer.pixelAspectRatioNumerator + ":" + importer.pixelAspectRatioDenominator);
            ShowProperty(ref labelRect, ref valueRect, "Alpha", importer.sourceHasAlpha ? "Yes" : "No");

            var audioTrackCount = importer.sourceAudioTrackCount;
            ShowProperty(ref labelRect, ref valueRect, "Audio",
                audioTrackCount == 0 ? "none" : audioTrackCount == 1 ?
                GetAudioTrackDescription(importer, 0) : "");

            if (audioTrackCount <= 1)
                return;

            labelRect.xMin  += kIndentWidth;
            labelRect.width -= kIndentWidth;

            for (ushort i = 0; i < audioTrackCount; ++i)
                ShowProperty(ref labelRect, ref valueRect, "Track #" + (i + 1), GetAudioTrackDescription(importer, i));
        }

        private string GetAudioTrackDescription(VideoClipImporter importer, ushort audioTrackIdx)
        {
            var channelCount = importer.GetSourceAudioChannelCount(audioTrackIdx);
            string channelCountStr =
                channelCount == 0 ? "No channels" :
                channelCount == 1 ? "Mono" :
                channelCount == 2 ? "Stereo" :
                channelCount == 4 ? channelCount.ToString() : // Can be 3.1 or quad
                ((channelCount - 1) + ".1");
            return importer.GetSourceAudioSampleRate(audioTrackIdx) + " Hz, " + channelCountStr;
        }

        private void ShowProperty(ref Rect labelRect, ref Rect valueRect, string label, string value)
        {
            GUI.Label(labelRect, label, m_Styles.labelStyle);
            GUI.Label(valueRect, value, m_Styles.labelStyle);
            labelRect.y += EditorGUIUtility.singleLineHeight;
            valueRect.y += EditorGUIUtility.singleLineHeight;
        }
    }

    [CustomEditor(typeof(VideoClipImporter))]
    [CanEditMultipleObjects]
    internal class VideoClipImporterInspector : AssetImporterEditor
    {
        [Serializable]
        class InspectorTargetSettings
        {
            public bool overridePlatform;
            public BuildTargetGroup target;
            public VideoImporterTargetSettings settings;
        }

        class TargetSettings : ScriptableObject
        {
            public List<InspectorTargetSettings> allSettings;
        }

        SerializedProperty m_EncodeAlpha;
        SerializedProperty m_Deinterlace;
        SerializedProperty m_FlipVertical;
        SerializedProperty m_FlipHorizontal;
        SerializedProperty m_ImportAudio;
        SerializedProperty m_ColorSpace;

        bool m_IsPlaying = false;
        Vector2 m_Position = Vector2.zero;
        AnimBool m_ShowResizeModeOptions = new AnimBool();
        Texture m_Texture;
        GUIContent m_PreviewTitle;

        bool m_SourcesHaveAlpha;
        bool m_SourcesHaveAudio;

        static class Styles
        {
            public static readonly GUIContent[] playIcons =
            {
                EditorGUIUtility.TrIconContent("preAudioPlayOff"),
                EditorGUIUtility.TrIconContent("preAudioPlayOn")
            };
            public static readonly GUIContent globalTranscodeOptionsContent = EditorGUIUtility.TrTextContent(
                "* Shared setting between multiple platforms.");
            public static readonly GUIContent keepAlphaContent = EditorGUIUtility.TrTextContent(
                "Keep Alpha*", "If the source clip has alpha, it will be preserved during transcoding so that transparency is usable during render.");
            public static readonly GUIContent deinterlaceContent = EditorGUIUtility.TrTextContent(
                "Deinterlace*", "Remove interlacing on this video during transcoding.");
            public static readonly GUIContent flipHorizontalContent = EditorGUIUtility.TrTextContent(
                "Flip Horizontally*", "Flip the video horizontally during transcoding.");
            public static readonly GUIContent flipVerticalContent = EditorGUIUtility.TrTextContent(
                "Flip Vertically*", "Flip the video vertically during transcoding.");
            public static readonly GUIContent importAudioContent = EditorGUIUtility.TrTextContent(
                "Import Audio*", "Defines if the audio tracks will be imported during transcoding.");
            public static readonly GUIContent transcodeContent = EditorGUIUtility.TrTextContent(
                "Transcode", "Transcoding a clip gives more flexibility through the options below, but takes more time.");
            public static readonly GUIContent dimensionsContent = EditorGUIUtility.TrTextContent(
                "Dimensions", "Pixel size of the resulting video.");
            public static readonly GUIContent widthContent = EditorGUIUtility.TrTextContent(
                "Width", "Width in pixels of the resulting video.");
            public static readonly GUIContent heightContent = EditorGUIUtility.TrTextContent(
                "Height", "Height in pixels of the resulting video.");
            public static readonly GUIContent aspectRatioContent = EditorGUIUtility.TrTextContent(
                "Aspect Ratio", "How the original video is mapped into the target dimensions.");
            public static readonly GUIContent codecContent = EditorGUIUtility.TrTextContent(
                "Codec", "Codec for the resulting clip. Automatic will make the best choice for the target platform.");
            public static readonly GUIContent bitrateContent = EditorGUIUtility.TrTextContent(
                "Bitrate Mode", "Higher bit rates give a better quality, but impose higher load on network connections or storage.");
            public static readonly GUIContent spatialQualityContent = EditorGUIUtility.TrTextContent(
                "Spatial Quality", "Adds a downsize during import to reduce bitrate using resolution.");
            public static readonly GUIContent transcodeWarning = EditorGUIUtility.TrTextContent(
                "Not all platforms transcoded. Clip is not guaranteed to be compatible on platforms without transcoding.");
            public static readonly GUIContent transcodeOptionsWarning = EditorGUIUtility.TrTextContent(
                "Global transcode options are not applied on all platforms. You must enable \"Transcode\" for these to take effect.");
            public static readonly GUIContent transcodeSkippedWarning = EditorGUIUtility.TrTextContent(
                "Transcode was skipped. Current clip does not match import settings. Reimport to resolve.");
            public static readonly GUIContent multipleTranscodeSkippedWarning = EditorGUIUtility.TrTextContent(
                "Transcode was skipped for some clips and they don't match import settings. Reimport to resolve.");
            public static readonly GUIContent sRGBTextureContent = EditorGUIUtility.TrTextContent(
                "sRGB (Color Texture)", "Texture content is stored in gamma space.");
        }

        // Don't show the imported movie as a separate editor
        public override bool showImportedObject { get { return false; } }

        protected override Type extraDataType => typeof(TargetSettings);

        protected override void InitializeExtraDataInstance(Object extraData, int targetIndex)
        {
            var targetSettings = extraData as TargetSettings;
            var currentTarget = targets[targetIndex] as VideoClipImporter;
            if (targetSettings != null && currentTarget != null)
            {
                var validPlatforms = BuildPlatforms.instance.GetValidPlatforms();
                targetSettings.allSettings = new List<InspectorTargetSettings>(validPlatforms.Count + 1);

                var defaultSetting = new InspectorTargetSettings();
                defaultSetting.target = BuildTargetGroup.Unknown;
                defaultSetting.settings = currentTarget.defaultTargetSettings;
                defaultSetting.overridePlatform = true;
                targetSettings.allSettings.Add(defaultSetting);

                foreach (var validPlatform in validPlatforms)
                {
                    var setting = new InspectorTargetSettings();
                    setting.target = validPlatform.targetGroup;
                    setting.settings = currentTarget.Internal_GetTargetSettings(validPlatform.targetGroup);
                    setting.overridePlatform = setting.settings != null;
                    if (!setting.overridePlatform)
                    {
                        setting.settings = currentTarget.defaultTargetSettings;
                    }
                    targetSettings.allSettings.Add(setting);
                }
            }
        }

        private void WriteSettingsToBackend()
        {
            for (var i = 0; i < extraDataTargets.Length; i++)
            {
                var dataTarget = extraDataTargets[i] as TargetSettings;
                var importer = targets[i] as VideoClipImporter;

                if (dataTarget != null && importer != null)
                {
                    var defaultSettings = dataTarget.allSettings[0];
                    importer.defaultTargetSettings = defaultSettings.settings;

                    for (var j = 1; j < dataTarget.allSettings.Count; j++)
                    {
                        var setting = dataTarget.allSettings[j];
                        if (setting.overridePlatform)
                            importer.Internal_SetTargetSettings(setting.target, setting.settings);
                        else
                            importer.Internal_ClearTargetSettings(setting.target);
                    }
                }
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();

            m_EncodeAlpha = serializedObject.FindProperty("m_EncodeAlpha");
            m_Deinterlace = serializedObject.FindProperty("m_Deinterlace");
            m_FlipVertical = serializedObject.FindProperty("m_FlipVertical");
            m_FlipHorizontal = serializedObject.FindProperty("m_FlipHorizontal");
            m_ImportAudio = serializedObject.FindProperty("m_ImportAudio");
            m_ColorSpace = serializedObject.FindProperty("m_ColorSpace");

            // setup alpha and audio values
            m_SourcesHaveAlpha = true;
            m_SourcesHaveAudio = true;
            if (assetTarget != null)
            {
                for (int i = 0; i < targets.Length; ++i)
                {
                    VideoClipImporter importer = (VideoClipImporter)targets[i];
                    m_SourcesHaveAlpha &= importer.sourceHasAlpha;
                    m_SourcesHaveAudio &= (importer.sourceAudioTrackCount > 0);
                }
            }

            m_ShowResizeModeOptions.valueChanged.AddListener(Repaint);
            var defaultResizeMode = extraDataSerializedObject.FindProperty("allSettings.Array.data[0].settings.resizeMode");
            m_ShowResizeModeOptions.value = defaultResizeMode.hasMultipleDifferentValues || (VideoResizeMode)defaultResizeMode.intValue != VideoResizeMode.OriginalSize;
        }

        public override void OnDisable()
        {
            VideoClipImporter importer = target as VideoClipImporter;
            if (importer)
                importer.StopPreview();
            base.OnDisable();
        }

        private bool AnySettingsNotTranscoded()
        {
            foreach (var extraData in extraDataTargets)
            {
                var settings = extraData as TargetSettings;
                if (settings != null)
                {
                    foreach (var setting in settings.allSettings)
                    {
                        if (setting.overridePlatform && !setting.settings.enableTranscoding)
                            return true;
                    }
                }
            }

            return false;
        }

        private bool AnyUnappliedGlobalTranscodeOptions()
        {
            // Scan through all selected clips (eg. multi-select)
            for (var i = 0; i < targets.Length; i++)
            {
                // Check "global" (eg. non-platform specific options) for any that actually apply
                // non trivial processing during transcode.
                var importer = targets[i] as VideoClipImporter;
                if (importer != null &&
                    (importer.flipHorizontal ||
                     importer.flipVertical ||
                     importer.deinterlaceMode != VideoDeinterlaceMode.Off ||
                     (!importer.importAudio && importer.sourceAudioTrackCount > 0) ||
                     (!importer.keepAlpha && importer.sourceHasAlpha)))
                {
                    // Check all platform-specific options to see if any platform has transcoding
                    // disabled.
                    var settings = extraDataTargets[i] as TargetSettings;
                    if (settings != null)
                    {
                        foreach (var setting in settings.allSettings)
                        {
                            if (setting.overridePlatform && !setting.settings.enableTranscoding)
                                return true;
                        }
                    }
                }
            }
            return false;
        }

        private void OnCrossTargetInspectorGUI()
        {
            using (var horizontal = new EditorGUILayout.HorizontalScope())
            {
                using (var property = new EditorGUI.PropertyScope(horizontal.rect, Styles.sRGBTextureContent, m_ColorSpace))
                {
                    using (var changed = new EditorGUI.ChangeCheckScope())
                    {
                        var sRGB = EditorGUILayout.Toggle(property.content,
                            m_ColorSpace.enumValueIndex == (int)VideoColorSpace.sRGB);
                        if (changed.changed)
                            m_ColorSpace.enumValueIndex = (int)(sRGB ? VideoColorSpace.sRGB : VideoColorSpace.Linear);
                    }
                }
            }
        }

        private void FrameSettingsGUI(SerializedProperty videoImporterTargetSettings)
        {
            var resizeModeProperty = videoImporterTargetSettings.FindPropertyRelative("resizeMode");
            using (var horizontal = new EditorGUILayout.HorizontalScope())
            {
                using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, Styles.dimensionsContent, resizeModeProperty))
                {
                    using (var changed = new EditorGUI.ChangeCheckScope())
                    {
                        EditorGUI.showMixedValue = resizeModeProperty.hasMultipleDifferentValues;
                        var resizeMode = (VideoResizeMode)EditorGUILayout.EnumPopup(propertyScope.content, (VideoResizeMode)resizeModeProperty.intValue);
                        EditorGUI.showMixedValue = false;
                        if (changed.changed)
                        {
                            resizeModeProperty.intValue = (int)resizeMode;
                        }
                    }
                }
            }

            // First item is "Original".  Options appear if another resize mode is chosen.
            var resizeValue = (VideoResizeMode)resizeModeProperty.intValue;
            m_ShowResizeModeOptions.target = resizeModeProperty.hasMultipleDifferentValues || resizeValue != VideoResizeMode.OriginalSize;
            if (EditorGUILayout.BeginFadeGroup(m_ShowResizeModeOptions.faded))
            {
                if (!resizeModeProperty.hasMultipleDifferentValues && resizeValue == VideoResizeMode.CustomSize)
                {
                    var customWidthProperty = videoImporterTargetSettings.FindPropertyRelative("customWidth");
                    EditorGUILayout.PropertyField(customWidthProperty, Styles.widthContent);

                    var customHeightProperty = videoImporterTargetSettings.FindPropertyRelative("customHeight");
                    EditorGUILayout.PropertyField(customHeightProperty, Styles.heightContent);
                }

                using (var horizontal = new EditorGUILayout.HorizontalScope())
                {
                    var property = videoImporterTargetSettings.FindPropertyRelative("aspectRatio");
                    using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, Styles.aspectRatioContent, property))
                    {
                        using (var changed = new EditorGUI.ChangeCheckScope())
                        {
                            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
                            var newValue = (VideoEncodeAspectRatio)EditorGUILayout.EnumPopup(propertyScope.content, (VideoEncodeAspectRatio)property.intValue);
                            EditorGUI.showMixedValue = false;
                            if (changed.changed)
                            {
                                property.intValue = (int)newValue;
                            }
                        }
                    }
                }
            }

            EditorGUILayout.EndFadeGroup();
        }

        private void EncodingSettingsGUI(SerializedProperty videoImporterTargetSettings)
        {
            using (var horizontal = new EditorGUILayout.HorizontalScope())
            {
                var property = videoImporterTargetSettings.FindPropertyRelative("codec");
                using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, Styles.codecContent, property))
                {
                    using (var changed = new EditorGUI.ChangeCheckScope())
                    {
                        EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
                        var newValue = (VideoCodec)EditorGUILayout.EnumPopup(propertyScope.content, (VideoCodec)property.intValue);
                        EditorGUI.showMixedValue = false;
                        if (changed.changed)
                        {
                            property.intValue = (int)newValue;
                        }
                    }
                }
            }

            using (var horizontal = new EditorGUILayout.HorizontalScope())
            {
                var property = videoImporterTargetSettings.FindPropertyRelative("bitrateMode");
                using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, Styles.bitrateContent, property))
                {
                    using (var changed = new EditorGUI.ChangeCheckScope())
                    {
                        EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
                        var newValue = (VideoBitrateMode)EditorGUILayout.EnumPopup(propertyScope.content, (VideoBitrateMode)property.intValue);
                        EditorGUI.showMixedValue = false;
                        if (changed.changed)
                        {
                            property.intValue = (int)newValue;
                        }
                    }
                }
            }

            using (var horizontal = new EditorGUILayout.HorizontalScope())
            {
                var property = videoImporterTargetSettings.FindPropertyRelative("spatialQuality");
                using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, Styles.spatialQualityContent, property))
                {
                    using (var changed = new EditorGUI.ChangeCheckScope())
                    {
                        EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
                        var newValue = (VideoSpatialQuality)EditorGUILayout.EnumPopup(propertyScope.content, (VideoSpatialQuality)property.intValue);
                        EditorGUI.showMixedValue = false;
                        if (changed.changed)
                        {
                            property.intValue = (int)newValue;
                        }
                    }
                }
            }
        }

        private void OnTargetSettingsInspectorGUI(SerializedProperty videoImporterTargetSettings)
        {
            var enableTranscodingProperty = videoImporterTargetSettings.FindPropertyRelative("enableTranscoding");
            EditorGUILayout.PropertyField(enableTranscodingProperty, Styles.transcodeContent);

            using (new EditorGUI.DisabledScope(!enableTranscodingProperty.boolValue || enableTranscodingProperty.hasMultipleDifferentValues))
            {
                FrameSettingsGUI(videoImporterTargetSettings);
                EncodingSettingsGUI(videoImporterTargetSettings);

                using (new EditorGUI.DisabledScope(!m_SourcesHaveAlpha))
                {
                    EditorGUILayout.PropertyField(m_EncodeAlpha, Styles.keepAlphaContent);
                }

                EditorGUILayout.PropertyField(m_Deinterlace, Styles.deinterlaceContent);
                EditorGUILayout.PropertyField(m_FlipHorizontal, Styles.flipHorizontalContent);
                EditorGUILayout.PropertyField(m_FlipVertical, Styles.flipVerticalContent);

                using (new EditorGUI.DisabledScope(!m_SourcesHaveAudio))
                {
                    EditorGUILayout.PropertyField(m_ImportAudio, Styles.importAudioContent);
                }

                EditorGUILayout.LabelField(Styles.globalTranscodeOptionsContent, EditorStyles.miniLabel);
            }
        }

        private void OnTargetInspectorGUI(int platformIndex, string platformName)
        {
            var inspectorTargetSettings = extraDataSerializedObject.FindProperty($"allSettings.Array.data[{platformIndex}]");
            var overrideEnabled = inspectorTargetSettings.FindPropertyRelative("overridePlatform");
            if (platformIndex != 0)
            {
                using (var horizontal = new EditorGUILayout.HorizontalScope())
                {
                    using (var property = new EditorGUI.PropertyScope(horizontal.rect, GUIContent.Temp("Override for " + platformName), overrideEnabled))
                    {
                        using (var changed = new EditorGUI.ChangeCheckScope())
                        {
                            EditorGUI.showMixedValue = overrideEnabled.hasMultipleDifferentValues;
                            var newValue = EditorGUILayout.Toggle(property.content, overrideEnabled.boolValue);
                            EditorGUI.showMixedValue = false;
                            if (changed.changed)
                            {
                                overrideEnabled.boolValue = newValue;
                            }
                        }
                    }
                }
                EditorGUILayout.Space();
            }
            using (new EditorGUI.DisabledScope(overrideEnabled.hasMultipleDifferentValues || !overrideEnabled.boolValue))
            {
                OnTargetSettingsInspectorGUI(inspectorTargetSettings.FindPropertyRelative("settings"));
            }
        }

        private void OnTargetsInspectorGUI()
        {
            BuildPlatform[] validPlatforms = BuildPlatforms.instance.GetValidPlatforms().ToArray();
            int shownSettingsPage = EditorGUILayout.BeginPlatformGrouping(validPlatforms, GUIContent.Temp("Default"));

            string platformName = (shownSettingsPage == -1) ? "Default" : validPlatforms[shownSettingsPage].name;
            OnTargetInspectorGUI(shownSettingsPage + 1, platformName);

            EditorGUILayout.EndPlatformGrouping();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            extraDataSerializedObject.Update();

            OnCrossTargetInspectorGUI();
            EditorGUILayout.Space();
            OnTargetsInspectorGUI();

            // Warn the user if there is no transcoding happening on at least one platform
            if (AnySettingsNotTranscoded())
                EditorGUILayout.HelpBox(Styles.transcodeWarning.text, MessageType.Info);

            // Warn the user if any of the global transcode settings are on, but transcoding isn't applied
            if (AnyUnappliedGlobalTranscodeOptions())
                EditorGUILayout.HelpBox(Styles.transcodeOptionsWarning.text, MessageType.Warning);

            foreach (var t in targets)
            {
                VideoClipImporter importer = t as VideoClipImporter;
                if (importer && importer.transcodeSkipped)
                {
                    EditorGUILayout.HelpBox(
                        targets.Length == 1 ? Styles.transcodeSkippedWarning.text :
                        Styles.multipleTranscodeSkippedWarning.text,
                        MessageType.Error);
                    break;
                }
            }

            extraDataSerializedObject.ApplyModifiedProperties();
            serializedObject.ApplyModifiedProperties();

            ApplyRevertGUI();
        }

        protected override void Apply()
        {
            foreach (var t in targets)
            {
                var importer = (VideoClipImporter)t;
                if (importer.isPlayingPreview)
                    importer.StopPreview();
            }
            m_IsPlaying = false;

            base.Apply();
            WriteSettingsToBackend();

            // This is necessary to enforce redrawing the static preview icons in the project browser, as properties like ForceToMono
            // may have changed the preview completely.
            foreach (ProjectBrowser pb in ProjectBrowser.GetAllProjectBrowsers())
                pb.Repaint();
        }

        public override bool HasPreviewGUI()
        {
            return target != null;
        }

        protected override bool useAssetDrawPreview { get { return false; } }

        public override GUIContent GetPreviewTitle()
        {
            if (m_PreviewTitle == null)
                m_PreviewTitle = new GUIContent();

            if (targets.Length == 1)
                // Asset name can change over time so we have to re-evaluate constantly.
                m_PreviewTitle.text = Path.GetFileName(((AssetImporter)target).assetPath);
            else if (string.IsNullOrEmpty(m_PreviewTitle.text))
                m_PreviewTitle.text = targets.Length + " Video Clips";

            return m_PreviewTitle;
        }

        public override void OnPreviewSettings()
        {
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            m_IsPlaying = PreviewGUI.CycleButton(m_IsPlaying ? 1 : 0, Styles.playIcons) != 0;
            EditorGUI.EndDisabledGroup();
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            VideoClipImporter importer = (VideoClipImporter)target;

            if (m_IsPlaying && !importer.isPlayingPreview)
                importer.PlayPreview();
            else if (!m_IsPlaying && importer.isPlayingPreview)
                importer.StopPreview();

            Texture image = importer.GetPreviewTexture();
            if (image && image.width != 0 && image.height != 0)
                m_Texture = image;

            if (!m_Texture)
                return;

            if (Event.current.type == EventType.Repaint)
                background.Draw(r, false, false, false, false);

            // Compensate spatial quality zooming, if any.
            float previewWidth = m_Texture.width;
            float previewHeight = m_Texture.height;
            var activeSettings =
                importer.GetTargetSettings(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget).ToString());
            if (activeSettings == null)
                activeSettings = importer.defaultTargetSettings;
            if (activeSettings.enableTranscoding)
            {
                VideoResizeMode resizeMode = activeSettings.resizeMode;
                previewWidth = importer.GetResizeWidth(resizeMode);
                previewHeight = importer.GetResizeHeight(resizeMode);
            }

            if (importer.pixelAspectRatioDenominator > 0)
            {
                float pixelAspectRatio = (float)importer.pixelAspectRatioNumerator /
                    (float)importer.pixelAspectRatioDenominator;

                if (pixelAspectRatio > 1.0F)
                    previewWidth *= pixelAspectRatio;
                else
                    previewHeight /= pixelAspectRatio;
            }

            float zoomLevel = 1.0f;

            if ((r.width / previewWidth * previewHeight) > r.height)
                zoomLevel = r.height / previewHeight;
            else
                zoomLevel = r.width / previewWidth;

            zoomLevel = Mathf.Clamp01(zoomLevel);

            Rect wantedRect = new Rect(r.x, r.y, previewWidth * zoomLevel, previewHeight * zoomLevel);

            PreviewGUI.BeginScrollView(
                r, m_Position, wantedRect, "PreHorizontalScrollbar", "PreHorizontalScrollbarThumb");

            EditorGUI.DrawTextureTransparent(wantedRect, m_Texture, ScaleMode.StretchToFill);

            m_Position = PreviewGUI.EndScrollView();

            if (m_IsPlaying && Event.current.type == EventType.Repaint)
                GUIView.current.Repaint();
        }
    }
}

