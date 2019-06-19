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
                ((channelCount - 1).ToString() + ".1");
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

        class Styles
        {
            public GUIContent[] playIcons =
            {
                EditorGUIUtility.IconContent("preAudioPlayOff"),
                EditorGUIUtility.IconContent("preAudioPlayOn")
            };
            public GUIContent keepAlphaContent = EditorGUIUtility.TextContent(
                "Keep Alpha|If the source clip has alpha, this will encode it in the resulting clip so that transparency is usable during render.");
            public GUIContent deinterlaceContent = EditorGUIUtility.TextContent(
                "Deinterlace|Remove interlacing on this video.");
            public GUIContent flipHorizontalContent = EditorGUIUtility.TextContent(
                "Flip Horizontally|Flip the video horizontally during transcoding.");
            public GUIContent flipVerticalContent = EditorGUIUtility.TextContent(
                "Flip Vertically|Flip the video vertically during transcoding.");
            public GUIContent importAudioContent = EditorGUIUtility.TextContent(
                "Import Audio|Defines if the audio tracks will be imported during transcoding.");
            public GUIContent transcodeContent = EditorGUIUtility.TextContent(
                "Transcode|Transcoding a clip gives more flexibility through the options below, but takes more time.");
            public GUIContent dimensionsContent = EditorGUIUtility.TextContent(
                "Dimensions|Pixel size of the resulting video.");
            public GUIContent widthContent = EditorGUIUtility.TextContent(
                "Width|Width in pixels of the resulting video.");
            public GUIContent heightContent = EditorGUIUtility.TextContent(
                "Height|Height in pixels of the resulting video.");
            public GUIContent aspectRatioContent = EditorGUIUtility.TextContent(
                "Aspect Ratio|How the original video is mapped into the target dimensions.");
            public GUIContent codecContent = EditorGUIUtility.TextContent(
                "Codec|Codec for the resulting clip. Automatic will make the best choice for the target platform.");
            public GUIContent bitrateContent = EditorGUIUtility.TextContent(
                "Bitrate Mode|Higher bit rates give a better quality, but impose higher load on network connections or storage.");
            public GUIContent spatialQualityContent = EditorGUIUtility.TextContent(
                "Spatial Quality|Adds a downsize during import to reduce bitrate using resolution.");
            public GUIContent transcodeWarning = EditorGUIUtility.TextContent(
                "Not all platforms transcoded. Clip is not guaranteed to be compatible on platforms without transcoding.");
            public GUIContent transcodeSkippedWarning = EditorGUIUtility.TextContent(
                "Transcode was skipped. Current clip does not match import settings. Reimport to resolve.");
            public GUIContent multipleTranscodeSkippedWarning = EditorGUIUtility.TextContent(
                "Transcode was skipped for some clips and they don't match import settings. Reimport to resolve.");
            public GUIContent sRGBTextureContent = EditorGUIUtility.TrTextContent(
                "sRGB (Color Texture)", "Texture content is stored in gamma space.");
        };

        static Styles s_Styles;

        const int kNarrowLabelWidth = 42;
        const int kToggleButtonWidth = 16;
        const int kMinCustomWidth = 1;
        const int kMaxCustomWidth = 16384;
        const int kMinCustomHeight = 1;
        const int kMaxCustomHeight = 16384;

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
                targetSettings.allSettings = new List<InspectorTargetSettings>(validPlatforms.Count() + 1);

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

            if (s_Styles == null)
                s_Styles = new Styles();

            m_EncodeAlpha = serializedObject.FindProperty("m_EncodeAlpha");
            m_Deinterlace = serializedObject.FindProperty("m_Deinterlace");
            m_FlipVertical = serializedObject.FindProperty("m_FlipVertical");
            m_FlipHorizontal = serializedObject.FindProperty("m_FlipHorizontal");
            m_ImportAudio = serializedObject.FindProperty("m_ImportAudio");
            m_ColorSpace = serializedObject.FindProperty("m_ColorSpace");

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

        private void OnCrossTargetInspectorGUI()
        {
            bool sourcesHaveAlpha = true;
            bool sourcesHaveAudio = true;
            for (int i = 0; i < targets.Length; ++i)
            {
                VideoClipImporter importer = (VideoClipImporter)targets[i];
                sourcesHaveAlpha &= importer.sourceHasAlpha;
                sourcesHaveAudio &= (importer.sourceAudioTrackCount > 0);
            }

            EditorGUI.BeginChangeCheck();
            var sRGB = EditorGUILayout.Toggle(s_Styles.sRGBTextureContent,
                m_ColorSpace.enumValueIndex == (int)VideoColorSpace.sRGB);
            if (EditorGUI.EndChangeCheck())
                m_ColorSpace.enumValueIndex = (int)(sRGB ? VideoColorSpace.sRGB : VideoColorSpace.Linear);

            if (sourcesHaveAlpha)
                EditorGUILayout.PropertyField(m_EncodeAlpha, s_Styles.keepAlphaContent);

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_Deinterlace, s_Styles.deinterlaceContent);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_FlipHorizontal, s_Styles.flipHorizontalContent);
            EditorGUILayout.PropertyField(m_FlipVertical, s_Styles.flipVerticalContent);
            EditorGUILayout.Space();

            using (new EditorGUI.DisabledScope(!sourcesHaveAudio))
            {
                EditorGUILayout.PropertyField(m_ImportAudio, s_Styles.importAudioContent);
            }
        }

        private void FrameSettingsGUI(SerializedProperty videoImporterTargetSettings)
        {
            var resizeModeProperty = videoImporterTargetSettings.FindPropertyRelative("resizeMode");
            using (var horizontal = new EditorGUILayout.HorizontalScope())
            {
                using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, s_Styles.dimensionsContent, resizeModeProperty))
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
                EditorGUI.indentLevel++;

                if (!resizeModeProperty.hasMultipleDifferentValues && resizeValue == VideoResizeMode.CustomSize)
                {
                    var customWidthProperty = videoImporterTargetSettings.FindPropertyRelative("customWidth");
                    EditorGUILayout.PropertyField(customWidthProperty, s_Styles.widthContent);

                    var customHeightProperty = videoImporterTargetSettings.FindPropertyRelative("customHeight");
                    EditorGUILayout.PropertyField(customHeightProperty, s_Styles.heightContent);
                }

                using (var horizontal = new EditorGUILayout.HorizontalScope())
                {
                    var property = videoImporterTargetSettings.FindPropertyRelative("aspectRatio");
                    using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, s_Styles.aspectRatioContent, property))
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

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFadeGroup();
        }

        private void EncodingSettingsGUI(SerializedProperty videoImporterTargetSettings)
        {
            using (var horizontal = new EditorGUILayout.HorizontalScope())
            {
                var property = videoImporterTargetSettings.FindPropertyRelative("codec");
                using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, s_Styles.codecContent, property))
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
                using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, s_Styles.bitrateContent, property))
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
                using (var propertyScope = new EditorGUI.PropertyScope(horizontal.rect, s_Styles.spatialQualityContent, property))
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
            EditorGUILayout.PropertyField(enableTranscodingProperty, s_Styles.transcodeContent);

            EditorGUI.indentLevel++;

            using (new EditorGUI.DisabledScope(!(enableTranscodingProperty.boolValue || enableTranscodingProperty.hasMultipleDifferentValues)))
            {
                FrameSettingsGUI(videoImporterTargetSettings);
                EncodingSettingsGUI(videoImporterTargetSettings);
            }

            EditorGUI.indentLevel--;
        }

        private void OnTargetInspectorGUI(int platformIndex, string platformName)
        {
            var inspectorTargetSettings = extraDataSerializedObject.FindProperty($"allSettings.Array.data[{platformIndex}]");
            var overrideEnabled = inspectorTargetSettings.FindPropertyRelative("overridePlatform");
            if (platformIndex != 0)
            {
                EditorGUI.showMixedValue = overrideEnabled.hasMultipleDifferentValues;
                EditorGUI.BeginChangeCheck();
                var newValue = EditorGUILayout.Toggle("Override for " + platformName, overrideEnabled.boolValue);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    overrideEnabled.boolValue = newValue;
                    if (!newValue)
                    {
                        //TODO: copy default to this platform ? or we don't care ? The AudioImporter doesn't care after all...
                    }
                }
            }

            EditorGUILayout.Space();

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
                EditorGUILayout.HelpBox(s_Styles.transcodeWarning.text, MessageType.Info);

            foreach (var t in targets)
            {
                VideoClipImporter importer = t as VideoClipImporter;
                if (importer && importer.transcodeSkipped)
                {
                    EditorGUILayout.HelpBox(
                        targets.Length == 1 ? s_Styles.transcodeSkippedWarning.text :
                        s_Styles.multipleTranscodeSkippedWarning.text,
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
            VideoClipImporter importer = (VideoClipImporter)target;
            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            m_IsPlaying = PreviewGUI.CycleButton(m_IsPlaying ? 1 : 0, s_Styles.playIcons) != 0;
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

