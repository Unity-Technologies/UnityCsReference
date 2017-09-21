// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using AnimatedBool = UnityEditor.AnimatedValues.AnimBool;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor.Build;
using System;
using UnityEditor.Experimental.AssetImporters;
using Math = System.Math;
using Path = System.IO.Path;

namespace UnityEditor
{
    [CustomPreview(typeof(VideoClipImporter))]
    internal class VideoClipImporterSourcePreview : ObjectPreview
    {
        class Styles
        {
            public GUIStyle labelStyle = new GUIStyle(EditorStyles.label);

            public Styles()
            {
                Color fontColor = new Color(0.7f, 0.7f, 0.7f);
                labelStyle.padding.right += 4;
                labelStyle.normal.textColor = fontColor;
            }
        }

        private Styles m_Styles = new Styles();

        private GUIContent m_Title;

        private const float kLabelWidth = 120;
        private const float kIndentWidth = 30;
        private const float kValueWidth = 200;

        public override GUIContent GetPreviewTitle()
        {
            if (m_Title == null)
                m_Title = new GUIContent("Source Info");
            return m_Title;
        }

        public override bool HasPreviewGUI()
        {
            var importer = target as VideoClipImporter;
            return importer != null && !importer.useLegacyImporter;
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
            ShowProperty(ref labelRect, ref valueRect, "FPS", frameRate.ToString("F2"));

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
        internal struct MultiTargetSettingState
        {
            public void Init()
            {
                mixedTranscoding = false;
                mixedCodec = false;
                mixedResizeMode = false;
                mixedAspectRatio = false;
                mixedCustomWidth = false;
                mixedCustomHeight = false;
                mixedBitrateMode = false;
                mixedSpatialQuality = false;

                firstTranscoding = false;
                firstCodec = VideoCodec.Auto;
                firstResizeMode = VideoResizeMode.OriginalSize;
                firstAspectRatio = VideoEncodeAspectRatio.NoScaling;
                firstCustomWidth = -1;
                firstCustomHeight = -1;
                firstBitrateMode = VideoBitrateMode.High;
                firstSpatialQuality = VideoSpatialQuality.HighSpatialQuality;
            }

            public bool mixedTranscoding;
            public bool mixedCodec;
            public bool mixedResizeMode;
            public bool mixedAspectRatio;
            public bool mixedCustomWidth;
            public bool mixedCustomHeight;
            public bool mixedBitrateMode;
            public bool mixedSpatialQuality;

            public bool                   firstTranscoding;
            public VideoCodec             firstCodec;
            public VideoResizeMode        firstResizeMode;
            public VideoEncodeAspectRatio firstAspectRatio;
            public int                    firstCustomWidth;
            public int                    firstCustomHeight;
            public VideoBitrateMode       firstBitrateMode;
            public VideoSpatialQuality    firstSpatialQuality;
        }

        internal class InspectorTargetSettings
        {
            public bool                        overridePlatform;
            public VideoImporterTargetSettings settings;
        }

        SerializedProperty m_UseLegacyImporter;
        SerializedProperty m_Quality;
        SerializedProperty m_IsColorLinear;

        SerializedProperty m_EncodeAlpha;
        SerializedProperty m_Deinterlace;
        SerializedProperty m_FlipVertical;
        SerializedProperty m_FlipHorizontal;
        SerializedProperty m_ImportAudio;

        // An array of settings for each platform, for every target.
        InspectorTargetSettings[,]             m_TargetSettings;

        bool            m_IsPlaying             = false;
        Vector2         m_Position              = Vector2.zero;
        AnimatedBool    m_ShowResizeModeOptions = new AnimatedBool();
        bool            m_ModifiedTargetSettings;
        GUIContent      m_PreviewTitle;

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
            public GUIContent importerVersionContent = EditorGUIUtility.TextContent(
                    "Importer Version|Selects the type of video asset produced.");
            public GUIContent[] importerVersionOptions =
            {
                EditorGUIUtility.TextContent("VideoClip|Produce VideoClip asset (for use with VideoPlayer)"),
                EditorGUIUtility.TextContent("MovieTexture (Deprecated)|Produce MovieTexture asset (deprecated in factor of VideoClip)"),
            };
            public GUIContent transcodeWarning = EditorGUIUtility.TextContent(
                    "Not all platforms transcoded. Clip is not guaranteed to be compatible on platforms without transcoding.");
            public GUIContent transcodeSkippedWarning = EditorGUIUtility.TextContent(
                    "Transcode was skipped. Current clip does not match import settings. Reimport to resolve.");
            public GUIContent multipleTranscodeSkippedWarning = EditorGUIUtility.TextContent(
                    "Transcode was skipped for some clips and they don't match import settings. Reimport to resolve.");
        };

        static Styles s_Styles;

        // Taken in MovieImporter.cpp.
        static string[] s_LegacyFileTypes = {".ogg", ".ogv", ".mov", ".asf", ".mpg", ".mpeg", ".mp4"};

        const int kNarrowLabelWidth = 42;
        const int kToggleButtonWidth = 16;
        const int kMinCustomWidth = 1;
        const int kMaxCustomWidth = 16384;
        const int kMinCustomHeight = 1;
        const int kMaxCustomHeight = 16384;

        // Don't show the imported movie as a separate editor
        public override bool showImportedObject { get { return false; } }

        private void ResetSettingsFromBackend()
        {
            m_TargetSettings = null;
            if (targets.Length > 0)
            {
                List<BuildPlatform> validPlatforms = BuildPlatforms.instance.GetValidPlatforms();
                m_TargetSettings = new InspectorTargetSettings[targets.Length, validPlatforms.Count + 1];

                for (int i = 0; i < targets.Length; i++)
                {
                    VideoClipImporter clipImporter = (VideoClipImporter)targets[i];

                    m_TargetSettings[i, 0] = new InspectorTargetSettings();
                    m_TargetSettings[i, 0].overridePlatform = true;
                    m_TargetSettings[i, 0].settings = clipImporter.defaultTargetSettings;

                    for (int j = 1; j < validPlatforms.Count + 1; j++)
                    {
                        BuildTargetGroup platformGroup = validPlatforms[j - 1].targetGroup;
                        m_TargetSettings[i, j] = new InspectorTargetSettings();
                        m_TargetSettings[i, j].settings = clipImporter.Internal_GetTargetSettings(platformGroup);

                        // We need to use this flag, and the nullity of the settings to determine if
                        // we have an override. This is because we could create an override later during a toggle
                        // and we want to keep that override even if we untoggle (to not lose the changes made)
                        m_TargetSettings[i, j].overridePlatform = m_TargetSettings[i, j].settings != null;
                    }
                }
            }

            m_ModifiedTargetSettings = false;
        }

        private void WriteSettingsToBackend()
        {
            if (m_TargetSettings != null)
            {
                List<BuildPlatform> validPlatforms = BuildPlatforms.instance.GetValidPlatforms();
                for (int i = 0; i < targets.Length; i++)
                {
                    VideoClipImporter clipImporter = (VideoClipImporter)targets[i];
                    clipImporter.defaultTargetSettings = m_TargetSettings[i, 0].settings;

                    for (int j = 1; j < validPlatforms.Count + 1; j++)
                    {
                        BuildTargetGroup platformGroup = validPlatforms[j - 1].targetGroup;
                        if (m_TargetSettings[i, j].settings != null && m_TargetSettings[i, j].overridePlatform)
                            clipImporter.Internal_SetTargetSettings(platformGroup, m_TargetSettings[i, j].settings);
                        else
                            clipImporter.Internal_ClearTargetSettings(platformGroup);
                    }
                }
            }

            m_ModifiedTargetSettings = false;
        }

        public override void OnEnable()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            m_UseLegacyImporter = serializedObject.FindProperty("m_UseLegacyImporter");
            m_Quality           = serializedObject.FindProperty("m_Quality");
            m_IsColorLinear     = serializedObject.FindProperty("m_IsColorLinear");
            m_EncodeAlpha       = serializedObject.FindProperty("m_EncodeAlpha");
            m_Deinterlace       = serializedObject.FindProperty("m_Deinterlace");
            m_FlipVertical      = serializedObject.FindProperty("m_FlipVertical");
            m_FlipHorizontal    = serializedObject.FindProperty("m_FlipHorizontal");
            m_ImportAudio       = serializedObject.FindProperty("m_ImportAudio");

            ResetSettingsFromBackend();

            // We need to do this so we can calculate the correct fade state
            MultiTargetSettingState state = CalculateMultiTargetSettingState(0);

            m_ShowResizeModeOptions.valueChanged.AddListener(Repaint);
            m_ShowResizeModeOptions.value = state.mixedResizeMode || (state.firstResizeMode != VideoResizeMode.OriginalSize);
        }

        public override void OnDisable()
        {
            VideoClipImporter importer = target as VideoClipImporter;
            if (importer)
                importer.StopPreview();
            base.OnDisable();
        }

        private List<GUIContent> GetResizeModeList()
        {
            var resizeModeList = new List<GUIContent>();
            VideoClipImporter importer = (VideoClipImporter)target;
            foreach (VideoResizeMode mode in System.Enum.GetValues(typeof(VideoResizeMode)))
                resizeModeList.Add(EditorGUIUtility.TextContent(importer.GetResizeModeName(mode)));

            return resizeModeList;
        }

        private bool AnySettingsNotTranscoded()
        {
            if (m_TargetSettings != null)
            {
                for (int i = 0; i < m_TargetSettings.GetLength(0); i++)
                {
                    for (int j = 0; j < m_TargetSettings.GetLength(1); j++)
                    {
                        if (m_TargetSettings[i, j].settings != null && !m_TargetSettings[i, j].settings.enableTranscoding)
                            return true;
                    }
                }
            }

            return false;
        }

        private MultiTargetSettingState CalculateMultiTargetSettingState(int platformIndex)
        {
            MultiTargetSettingState state = new MultiTargetSettingState();
            state.Init();

            if (m_TargetSettings == null || m_TargetSettings.Length == 0)
                return state;

            int firstValidIndex = -1;
            for (int i = 0; i < m_TargetSettings.GetLength(0); i++)
            {
                if (m_TargetSettings[i, platformIndex].overridePlatform)
                {
                    if (firstValidIndex == -1)
                    {
                        firstValidIndex = i;

                        state.firstTranscoding = m_TargetSettings[i, platformIndex].settings.enableTranscoding;
                        state.firstCodec = m_TargetSettings[i, platformIndex].settings.codec;
                        state.firstResizeMode = m_TargetSettings[i, platformIndex].settings.resizeMode;
                        state.firstAspectRatio = m_TargetSettings[i, platformIndex].settings.aspectRatio;
                        state.firstCustomWidth = m_TargetSettings[i, platformIndex].settings.customWidth;
                        state.firstCustomHeight = m_TargetSettings[i, platformIndex].settings.customHeight;
                        state.firstBitrateMode = m_TargetSettings[i, platformIndex].settings.bitrateMode;
                        state.firstSpatialQuality = m_TargetSettings[i, platformIndex].settings.spatialQuality;
                    }
                    else
                    {
                        state.mixedTranscoding = state.firstTranscoding != m_TargetSettings[i, platformIndex].settings.enableTranscoding;
                        state.mixedCodec = state.firstCodec != m_TargetSettings[i, platformIndex].settings.codec;
                        state.mixedResizeMode = state.firstResizeMode != m_TargetSettings[i, platformIndex].settings.resizeMode;
                        state.mixedAspectRatio = state.firstAspectRatio != m_TargetSettings[i, platformIndex].settings.aspectRatio;
                        state.mixedCustomWidth = state.firstCustomWidth != m_TargetSettings[i, platformIndex].settings.customWidth;
                        state.mixedCustomHeight = state.firstCustomHeight != m_TargetSettings[i, platformIndex].settings.customHeight;
                        state.mixedBitrateMode = state.firstBitrateMode != m_TargetSettings[i, platformIndex].settings.bitrateMode;
                        state.mixedSpatialQuality = state.firstSpatialQuality != m_TargetSettings[i, platformIndex].settings.spatialQuality;
                    }
                }
            }

            // If there are no override settings for this platform for ANY of the targets,
            // then just get the default settings and use those.
            if (firstValidIndex == -1)
            {
                // The default settings are always valid
                state.firstTranscoding = m_TargetSettings[0, 0].settings.enableTranscoding;
                state.firstCodec = m_TargetSettings[0, 0].settings.codec;
                state.firstResizeMode = m_TargetSettings[0, 0].settings.resizeMode;
                state.firstAspectRatio = m_TargetSettings[0, 0].settings.aspectRatio;
                state.firstCustomWidth = m_TargetSettings[0, 0].settings.customWidth;
                state.firstCustomHeight = m_TargetSettings[0, 0].settings.customHeight;
                state.firstBitrateMode = m_TargetSettings[0, 0].settings.bitrateMode;
                state.firstSpatialQuality = m_TargetSettings[0, 0].settings.spatialQuality;
            }

            return state;
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

            if (sourcesHaveAlpha)
                EditorGUILayout.PropertyField(m_EncodeAlpha, s_Styles.keepAlphaContent);

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

        private void FrameSettingsGUI(int platformIndex, MultiTargetSettingState multiState)
        {
            EditorGUI.showMixedValue = multiState.mixedResizeMode;
            EditorGUI.BeginChangeCheck();
            VideoResizeMode resizeMode = (VideoResizeMode)EditorGUILayout.Popup(s_Styles.dimensionsContent, (int)multiState.firstResizeMode, GetResizeModeList().ToArray());
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                for (int i = 0; i < m_TargetSettings.GetLength(0); i++)
                {
                    if (m_TargetSettings[i, platformIndex].settings != null)
                    {
                        m_TargetSettings[i, platformIndex].settings.resizeMode = resizeMode;
                        m_ModifiedTargetSettings = true;
                    }
                }
            }

            // First item is "Original".  Options appear if another resize mode is chosen.
            m_ShowResizeModeOptions.target = resizeMode != VideoResizeMode.OriginalSize;
            if (EditorGUILayout.BeginFadeGroup(m_ShowResizeModeOptions.faded))
            {
                EditorGUI.indentLevel++;

                if (resizeMode == VideoResizeMode.CustomSize)
                {
                    EditorGUI.showMixedValue = multiState.mixedCustomWidth;
                    EditorGUI.BeginChangeCheck();
                    int customWidth = EditorGUILayout.IntField(s_Styles.widthContent, multiState.firstCustomWidth);
                    customWidth = Mathf.Clamp(customWidth, kMinCustomWidth, kMaxCustomWidth);
                    EditorGUI.showMixedValue = false;
                    if (EditorGUI.EndChangeCheck())
                    {
                        for (int i = 0; i < m_TargetSettings.GetLength(0); i++)
                        {
                            if (m_TargetSettings[i, platformIndex].settings != null)
                            {
                                m_TargetSettings[i, platformIndex].settings.customWidth = customWidth;
                                m_ModifiedTargetSettings = true;
                            }
                        }
                    }

                    EditorGUI.showMixedValue = multiState.mixedCustomHeight;
                    EditorGUI.BeginChangeCheck();
                    int customHeight = EditorGUILayout.IntField(s_Styles.heightContent, multiState.firstCustomHeight);
                    customHeight = Mathf.Clamp(customHeight, kMinCustomHeight, kMaxCustomHeight);
                    EditorGUI.showMixedValue = false;
                    if (EditorGUI.EndChangeCheck())
                    {
                        for (int i = 0; i < m_TargetSettings.GetLength(0); i++)
                        {
                            if (m_TargetSettings[i, platformIndex].settings != null)
                            {
                                m_TargetSettings[i, platformIndex].settings.customHeight = customHeight;
                                m_ModifiedTargetSettings = true;
                            }
                        }
                    }
                }

                EditorGUI.showMixedValue = multiState.mixedAspectRatio;
                EditorGUI.BeginChangeCheck();
                VideoEncodeAspectRatio aspectRatio = (VideoEncodeAspectRatio)EditorGUILayout.EnumPopup(s_Styles.aspectRatioContent, multiState.firstAspectRatio);
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    for (int i = 0; i < m_TargetSettings.GetLength(0); i++)
                    {
                        if (m_TargetSettings[i, platformIndex].settings != null)
                        {
                            m_TargetSettings[i, platformIndex].settings.aspectRatio = aspectRatio;
                            m_ModifiedTargetSettings = true;
                        }
                    }
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFadeGroup();
        }

        private void EncodingSettingsGUI(int platformIndex, MultiTargetSettingState multiState)
        {
            EditorGUI.showMixedValue = multiState.mixedCodec;
            EditorGUI.BeginChangeCheck();
            VideoCodec codec = (VideoCodec)EditorGUILayout.EnumPopup(s_Styles.codecContent, multiState.firstCodec);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                for (int i = 0; i < m_TargetSettings.GetLength(0); i++)
                {
                    if (m_TargetSettings[i, platformIndex].settings != null)
                    {
                        m_TargetSettings[i, platformIndex].settings.codec = codec;
                        m_ModifiedTargetSettings = true;
                    }
                }
            }

            EditorGUI.showMixedValue = multiState.mixedBitrateMode;
            EditorGUI.BeginChangeCheck();
            VideoBitrateMode bitrateMode = (VideoBitrateMode)EditorGUILayout.EnumPopup(s_Styles.bitrateContent, multiState.firstBitrateMode);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                for (int i = 0; i < m_TargetSettings.GetLength(0); i++)
                {
                    if (m_TargetSettings[i, platformIndex].settings != null)
                    {
                        m_TargetSettings[i, platformIndex].settings.bitrateMode = bitrateMode;
                        m_ModifiedTargetSettings = true;
                    }
                }
            }

            EditorGUI.showMixedValue = multiState.mixedSpatialQuality;
            EditorGUI.BeginChangeCheck();
            VideoSpatialQuality spatialQuality = (VideoSpatialQuality)EditorGUILayout.EnumPopup(s_Styles.spatialQualityContent, multiState.firstSpatialQuality);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                for (int i = 0; i < m_TargetSettings.GetLength(0); i++)
                {
                    if (m_TargetSettings[i, platformIndex].settings != null)
                    {
                        m_TargetSettings[i, platformIndex].settings.spatialQuality = spatialQuality;
                        m_ModifiedTargetSettings = true;
                    }
                }
            }
        }

        private bool HasMixedOverrideStatus(int platformIndex, out bool overrideState)
        {
            overrideState = false;
            if (m_TargetSettings == null || m_TargetSettings.Length == 0)
                return false;

            overrideState = m_TargetSettings[0, platformIndex].overridePlatform;
            for (int i = 1; i < m_TargetSettings.GetLength(0); i++)
            {
                if (m_TargetSettings[i, platformIndex].overridePlatform != overrideState)
                    return true;
            }

            return false;
        }

        private VideoImporterTargetSettings CloneTargetSettings(VideoImporterTargetSettings settings)
        {
            VideoImporterTargetSettings newSettings = new VideoImporterTargetSettings();

            newSettings.enableTranscoding = settings.enableTranscoding;
            newSettings.codec = settings.codec;
            newSettings.resizeMode = settings.resizeMode;
            newSettings.aspectRatio = settings.aspectRatio;
            newSettings.customWidth = settings.customWidth;
            newSettings.customHeight = settings.customHeight;
            newSettings.bitrateMode = settings.bitrateMode;
            newSettings.spatialQuality = settings.spatialQuality;

            return newSettings;
        }

        private void OnTargetSettingsInspectorGUI(int platformIndex, MultiTargetSettingState multiState)
        {
            EditorGUI.showMixedValue = multiState.mixedTranscoding;
            EditorGUI.BeginChangeCheck();
            bool transcode = EditorGUILayout.Toggle(s_Styles.transcodeContent, multiState.firstTranscoding);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                for (int i = 0; i < m_TargetSettings.GetLength(0); i++)
                {
                    if (m_TargetSettings[i, platformIndex].settings != null)
                    {
                        m_TargetSettings[i, platformIndex].settings.enableTranscoding = transcode;
                        m_ModifiedTargetSettings = true;
                    }
                }
            }

            EditorGUI.indentLevel++;

            using (new EditorGUI.DisabledScope(!(transcode || multiState.mixedTranscoding)))
            {
                FrameSettingsGUI(platformIndex, multiState);
                EncodingSettingsGUI(platformIndex, multiState);
            }

            EditorGUI.indentLevel--;
        }

        private void OnTargetInspectorGUI(int platformIndex, string platformName)
        {
            bool enableOverrideSettings = true;
            if (platformIndex != 0)
            {
                bool overrideState;
                EditorGUI.showMixedValue = HasMixedOverrideStatus(platformIndex, out overrideState);
                EditorGUI.BeginChangeCheck();
                overrideState = EditorGUILayout.Toggle("Override for " + platformName, overrideState);
                enableOverrideSettings = overrideState || EditorGUI.showMixedValue;
                EditorGUI.showMixedValue = false;
                if (EditorGUI.EndChangeCheck())
                {
                    for (int i = 0; i < m_TargetSettings.GetLength(0); i++)
                    {
                        m_TargetSettings[i, platformIndex].overridePlatform = overrideState;
                        m_ModifiedTargetSettings = true;
                        if (m_TargetSettings[i, platformIndex].settings == null)
                            m_TargetSettings[i, platformIndex].settings = CloneTargetSettings(m_TargetSettings[i, 0].settings);
                    }
                }
            }

            EditorGUILayout.Space();

            MultiTargetSettingState multiState = CalculateMultiTargetSettingState(platformIndex);
            using (new EditorGUI.DisabledScope(!enableOverrideSettings))
            {
                OnTargetSettingsInspectorGUI(platformIndex, multiState);
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

        internal override void OnHeaderControlsGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            bool supportsLegacy = true;
            for (int i = 0; supportsLegacy && i < targets.Length; ++i)
            {
                VideoClipImporter importer = (VideoClipImporter)targets[i];
                supportsLegacy &= IsFileSupportedByLegacy(importer.assetPath);
            }

            if (!supportsLegacy)
            {
                base.OnHeaderControlsGUI();
                return;
            }

            EditorGUI.showMixedValue = m_UseLegacyImporter.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100;
            int selectionIndex = EditorGUILayout.Popup(
                    s_Styles.importerVersionContent, m_UseLegacyImporter.boolValue ? 1 : 0,
                    s_Styles.importerVersionOptions, EditorStyles.popup, GUILayout.MaxWidth(230));
            EditorGUIUtility.labelWidth = originalLabelWidth;
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
                m_UseLegacyImporter.boolValue = selectionIndex == 1;

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Open", EditorStyles.miniButton))
            {
                AssetDatabase.OpenAsset(assetEditor.targets);
                GUIUtility.ExitGUI();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            if (m_UseLegacyImporter.boolValue)
            {
                EditorGUILayout.PropertyField(
                    m_IsColorLinear, MovieImporterInspector.linearTextureContent);
                EditorGUILayout.Slider(m_Quality, 0.0f, 1.0f);
            }
            else
            {
                OnCrossTargetInspectorGUI();
                EditorGUILayout.Space();
                OnTargetsInspectorGUI();

                // Warn the user if there is no transcoding happening on at least one platform
                if (AnySettingsNotTranscoded())
                    EditorGUILayout.HelpBox(s_Styles.transcodeWarning.text, MessageType.Info);
            }

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

            ApplyRevertGUI();
        }

        public override bool HasModified()
        {
            if (base.HasModified())
                return true;

            return m_ModifiedTargetSettings;
        }

        protected override void Apply()
        {
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
            if (m_PreviewTitle != null)
                return m_PreviewTitle;

            m_PreviewTitle = new GUIContent();

            if (targets.Length == 1)
            {
                AssetImporter importer = (AssetImporter)target;
                m_PreviewTitle.text = Path.GetFileName(importer.assetPath);
            }
            else
                m_PreviewTitle.text = targets.Length + " Video Clips";

            return m_PreviewTitle;
        }

        protected override void ResetValues()
        {
            base.ResetValues();
            OnEnable();
        }

        public override void OnPreviewSettings()
        {
            VideoClipImporter importer = (VideoClipImporter)target;
            EditorGUI.BeginDisabledGroup(Application.isPlaying || HasModified() || importer.useLegacyImporter);
            m_IsPlaying = PreviewGUI.CycleButton(m_IsPlaying ? 1 : 0, s_Styles.playIcons) != 0;
            EditorGUI.EndDisabledGroup();
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            background.Draw(r, false, false, false, false);

            VideoClipImporter importer = (VideoClipImporter)target;

            if (m_IsPlaying && !importer.isPlayingPreview)
                importer.PlayPreview();
            else if (!m_IsPlaying && importer.isPlayingPreview)
                importer.StopPreview();

            Texture image = importer.GetPreviewTexture();
            if (!image || image.width == 0 || image.height == 0)
                return;

            // Compensate spatial quality zooming, if any.
            float previewWidth = image.width;
            float previewHeight = image.height;
            if (importer.defaultTargetSettings.enableTranscoding)
            {
                VideoResizeMode resizeMode = importer.defaultTargetSettings.resizeMode;
                previewWidth = importer.GetResizeWidth(resizeMode);
                previewHeight = importer.GetResizeHeight(resizeMode);
            }

            if (importer.pixelAspectRatioDenominator > 0)
                previewWidth *= (float)importer.pixelAspectRatioNumerator /
                    (float)importer.pixelAspectRatioDenominator;

            float zoomLevel = 1.0f;

            if ((r.width / previewWidth * previewHeight) > r.height)
                zoomLevel = r.height / previewHeight;
            else
                zoomLevel = r.width / previewWidth;

            zoomLevel = Mathf.Clamp01(zoomLevel);

            Rect wantedRect = new Rect(r.x, r.y, previewWidth * zoomLevel, previewHeight * zoomLevel);

            PreviewGUI.BeginScrollView(
                r, m_Position, wantedRect, "PreHorizontalScrollbar", "PreHorizontalScrollbarThumb");

            EditorGUI.DrawTextureTransparent(wantedRect, image, ScaleMode.StretchToFill);

            m_Position = PreviewGUI.EndScrollView();

            if (m_IsPlaying)
                GUIView.current.Repaint();
        }

        private bool IsFileSupportedByLegacy(string assetPath)
        {
            return System.Array.IndexOf(
                s_LegacyFileTypes, Path.GetExtension(assetPath).ToLower()) != -1;
        }
    }
}

