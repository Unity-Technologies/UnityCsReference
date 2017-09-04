// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.AnimatedValues;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using UnityEngine.Video;
using UnityEditor.Build;

namespace UnityEditor
{
    [CustomEditor(typeof(VideoPlayer))]
    [CanEditMultipleObjects]
    internal class VideoPlayerEditor : Editor
    {
        class Styles
        {
            public GUIContent dataSourceContent =
                EditorGUIUtility.TextContent("Source|Type of source the movie will be read from.");
            public GUIContent videoClipContent =
                EditorGUIUtility.TextContent("Video Clip|VideoClips can be imported using the asset pipeline.");
            public GUIContent urlContent =
                EditorGUIUtility.TextContent("URL|URLs can be http:// or file://. File URLs can be relative [file://] or absolute [file:///].  For file URLs, the prefix is optional.");
            public GUIContent browseContent = EditorGUIUtility.TextContent("Browse...|Click to set a file:// URL.  http:// URLs have to be written or copy-pasted manually.");
            public GUIContent playOnAwakeContent =
                EditorGUIUtility.TextContent("Play On Awake|Start playback as soon as the game is started.");
            public GUIContent waitForFirstFrameContent =
                EditorGUIUtility.TextContent("Wait For First Frame|Wait for first frame to be ready before starting playback. When on, player time will only start increasing when the first image is ready.  When off, the first few frames may be skipped while clip preparation is ongoing.");
            public GUIContent loopContent =
                EditorGUIUtility.TextContent("Loop|Start playback at the beginning when end is reached.");
            public GUIContent playbackSpeedContent =
                EditorGUIUtility.TextContent("Playback Speed|Increase or decrease the playback speed. 1.0 is the normal speed.");
            public GUIContent renderModeContent =
                EditorGUIUtility.TextContent("Render Mode|Type of object on which the played images will be drawn.");
            public GUIContent cameraContent =
                EditorGUIUtility.TextContent("Camera|Camera where the images will be drawn, behind (Back Plane) or in front of (Front Plane) of the scene.");
            public GUIContent textureContent =
                EditorGUIUtility.TextContent("Target Texture|RenderTexture where the images will be drawn.  RenderTextures can be created under the Assets folder and the used on other objects.");
            public GUIContent alphaContent =
                EditorGUIUtility.TextContent("Alpha|A value less than 1.0 will reveal the content behind the video.");
            public GUIContent camera3DLayout =
                EditorGUIUtility.TextContent("3D Layout|Layout of 3D content in the source video.");
            public GUIContent audioOutputModeContent =
                EditorGUIUtility.TextContent("Audio Output Mode|Where the audio in the movie will be output.");
            public GUIContent audioSourceContent =
                EditorGUIUtility.TextContent("Audio Source|AudioSource component that will receive this track's audio samples.");
            public GUIContent aspectRatioLabel = EditorGUIUtility.TextContent("Aspect Ratio");
            public GUIContent muteLabel = EditorGUIUtility.TextContent("Mute");
            public GUIContent volumeLabel = EditorGUIUtility.TextContent("Volume");
            public GUIContent controlledAudioTrackCountContent = EditorGUIUtility.TextContent(
                    "Controlled Tracks|How many audio tracks will the player control.  The actual number of tracks is only known during playback when the source is a URL.");
            public GUIContent materialRendererContent = EditorGUIUtility.TextContent(
                    "Renderer|Renderer that will receive the images. Defaults to the first renderer on the game object.");
            public GUIContent materialPropertyContent = EditorGUIUtility.TextContent(
                    "Material Property|Texture property of the current Material that will receive the images.");

            public string selectUniformVideoSourceHelp =
                "Select a uniform video source type before a video clip or URL can be selected.";
            public string rendererMaterialsHaveNoTexPropsHelp =
                "Renderer materials have no texture properties.";
            public string someRendererMaterialsHaveNoTexPropsHelp =
                "Some selected renderers have materials with no texture properties.";
            public string invalidTexPropSelectionHelp =
                "Invalid texture property selection.";
            public string oneInvalidTexPropSelectionHelp =
                "1 selected object has an invalid texture property selection.";
            public string someInvalidTexPropSelectionsHelp =
                "{0} selected objects have invalid texture property selections.";
            public string texPropInAllMaterialsHelp =
                "Texture property appears in all renderer materials.";
            public string texPropInSomeMaterialsHelp =
                "Texture property appears in {0} out of {1} renderer materials.";
            public string selectUniformVideoRenderModeHelp =
                "Select a uniform video render mode type before a target camera, render texture or material parameter can be selected.";
            public string selectUniformAudioOutputModeHelp =
                "Select a uniform audio target before audio settings can be edited.";
            public string selectUniformAudioTracksHelp =
                "Only sources with the same number of audio tracks can be edited during multi-selection.";
            public string selectMovieFile = "Select movie file.";
            public string audioControlsNotEditableHelp =
                "Audio controls not editable when using muliple selection.";
            public string enableDecodingTooltip =
                "Enable decoding for this track.  Only effective when not playing.  When playing from a URL, track details are shown only while playing back.";
        }

        internal class AudioTrackInfo
        {
            public AudioTrackInfo()
            {
                language = "";
                channelCount = 0;
            }

            public string language;
            public ushort channelCount;
            public GUIContent content;
        }

        static Styles s_Styles;

        SerializedProperty m_DataSource;
        SerializedProperty m_VideoClip;
        SerializedProperty m_Url;
        SerializedProperty m_PlayOnAwake;
        SerializedProperty m_WaitForFirstFrame;
        SerializedProperty m_Looping;
        SerializedProperty m_PlaybackSpeed;
        SerializedProperty m_RenderMode;
        SerializedProperty m_TargetTexture;
        SerializedProperty m_TargetCamera;
        SerializedProperty m_TargetMaterialRenderer;
        SerializedProperty m_TargetMaterialProperty;
        SerializedProperty m_AspectRatio;
        SerializedProperty m_TargetCameraAlpha;
        SerializedProperty m_TargetCamera3DLayout;
        SerializedProperty m_AudioOutputMode;
        SerializedProperty m_ControlledAudioTrackCount;
        SerializedProperty m_EnabledAudioTracks;
        SerializedProperty m_TargetAudioSources;
        SerializedProperty m_DirectAudioVolumes;
        SerializedProperty m_DirectAudioMutes;

        readonly AnimBool m_ShowRenderTexture = new AnimBool();
        readonly AnimBool m_ShowTargetCamera = new AnimBool();
        readonly AnimBool m_ShowRenderer = new AnimBool();
        readonly AnimBool m_ShowMaterialProperty = new AnimBool();
        readonly AnimBool m_DataSourceIsClip = new AnimBool();
        readonly AnimBool m_ShowAspectRatio = new AnimBool();
        readonly AnimBool m_ShowAudioControls = new AnimBool();

        ushort m_AudioTrackCountCached = 0;
        GUIContent m_ControlledAudioTrackCountContent;
        List<AudioTrackInfo> m_AudioTrackInfos;

        int m_MaterialPropertyPopupContentHash;
        GUIContent[] m_MaterialPropertyPopupContent;
        int m_MaterialPropertyPopupSelection, m_MaterialPropertyPopupInvalidSelections;
        string m_MultiMaterialInfo = null;

        void OnEnable()
        {
            m_ShowRenderTexture.valueChanged.AddListener(Repaint);
            m_ShowTargetCamera.valueChanged.AddListener(Repaint);
            m_ShowRenderer.valueChanged.AddListener(Repaint);
            m_ShowMaterialProperty.valueChanged.AddListener(Repaint);
            m_DataSourceIsClip.valueChanged.AddListener(Repaint);
            m_ShowAspectRatio.valueChanged.AddListener(Repaint);
            m_ShowAudioControls.valueChanged.AddListener(Repaint);

            m_DataSource = serializedObject.FindProperty("m_DataSource");
            m_VideoClip = serializedObject.FindProperty("m_VideoClip");
            m_Url = serializedObject.FindProperty("m_Url");
            m_PlayOnAwake = serializedObject.FindProperty("m_PlayOnAwake");
            m_WaitForFirstFrame = serializedObject.FindProperty("m_WaitForFirstFrame");
            m_Looping = serializedObject.FindProperty("m_Looping");
            m_PlaybackSpeed = serializedObject.FindProperty("m_PlaybackSpeed");
            m_RenderMode = serializedObject.FindProperty("m_RenderMode");
            m_TargetTexture = serializedObject.FindProperty("m_TargetTexture");
            m_TargetCamera = serializedObject.FindProperty("m_TargetCamera");
            m_TargetMaterialRenderer = serializedObject.FindProperty("m_TargetMaterialRenderer");
            m_TargetMaterialProperty = serializedObject.FindProperty("m_TargetMaterialProperty");
            m_AspectRatio = serializedObject.FindProperty("m_AspectRatio");
            m_TargetCameraAlpha = serializedObject.FindProperty("m_TargetCameraAlpha");
            m_TargetCamera3DLayout = serializedObject.FindProperty("m_TargetCamera3DLayout");
            m_AudioOutputMode = serializedObject.FindProperty("m_AudioOutputMode");
            m_ControlledAudioTrackCount = serializedObject.FindProperty("m_ControlledAudioTrackCount");
            m_EnabledAudioTracks = serializedObject.FindProperty("m_EnabledAudioTracks");
            m_TargetAudioSources = serializedObject.FindProperty("m_TargetAudioSources");
            m_DirectAudioVolumes = serializedObject.FindProperty("m_DirectAudioVolumes");
            m_DirectAudioMutes = serializedObject.FindProperty("m_DirectAudioMutes");

            m_ShowRenderTexture.value = m_RenderMode.intValue == (int)VideoRenderMode.RenderTexture;
            m_ShowTargetCamera.value =
                m_RenderMode.intValue == (int)VideoRenderMode.CameraFarPlane ||
                m_RenderMode.intValue == (int)VideoRenderMode.CameraNearPlane;

            m_ShowRenderer.value = m_RenderMode.intValue == (int)VideoRenderMode.MaterialOverride;
            m_MaterialPropertyPopupContent = BuildPopupEntries(targets, GetMaterialPropertyNames, out m_MaterialPropertyPopupSelection, out m_MaterialPropertyPopupInvalidSelections);
            m_MaterialPropertyPopupContentHash = GetMaterialPropertyPopupHash(targets);
            m_ShowMaterialProperty.value =
                targets.Count() > 1 || (m_MaterialPropertyPopupSelection >= 0 && m_MaterialPropertyPopupContent.Length > 0);

            m_DataSourceIsClip.value = m_DataSource.intValue == (int)VideoSource.VideoClip;
            m_ShowAspectRatio.value = (m_RenderMode.intValue != (int)VideoRenderMode.MaterialOverride) &&
                (m_RenderMode.intValue != (int)VideoRenderMode.APIOnly);
            m_ShowAudioControls.value = m_AudioOutputMode.intValue != (int)VideoAudioOutputMode.None;
            VideoPlayer vp = target as VideoPlayer;
            vp.prepareCompleted += PrepareCompleted;

            m_AudioTrackInfos = new List<AudioTrackInfo>();
        }

        void OnDisable()
        {
            m_ShowRenderTexture.valueChanged.RemoveListener(Repaint);
            m_ShowTargetCamera.valueChanged.RemoveListener(Repaint);
            m_ShowRenderer.valueChanged.RemoveListener(Repaint);
            m_ShowMaterialProperty.valueChanged.RemoveListener(Repaint);
            m_DataSourceIsClip.valueChanged.RemoveListener(Repaint);
            m_ShowAspectRatio.valueChanged.RemoveListener(Repaint);
            m_ShowAudioControls.valueChanged.RemoveListener(Repaint);
        }

        public override void OnInspectorGUI()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            serializedObject.Update();

            EditorGUILayout.PropertyField(m_DataSource, s_Styles.dataSourceContent);
            HandleDataSourceField();
            EditorGUILayout.PropertyField(m_PlayOnAwake, s_Styles.playOnAwakeContent);
            EditorGUILayout.PropertyField(m_WaitForFirstFrame, s_Styles.waitForFirstFrameContent);
            EditorGUILayout.PropertyField(m_Looping, s_Styles.loopContent);
            EditorGUILayout.Slider(m_PlaybackSpeed, 0.0f, 10.0f, s_Styles.playbackSpeedContent);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_RenderMode, s_Styles.renderModeContent);
            if (m_RenderMode.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox(s_Styles.selectUniformVideoRenderModeHelp, MessageType.Warning, false);
            }
            else
            {
                VideoRenderMode currentRenderMode = (VideoRenderMode)m_RenderMode.intValue;
                HandleTargetField(currentRenderMode);
            }
            HandleAudio();

            serializedObject.ApplyModifiedProperties();
        }

        private void HandleDataSourceField()
        {
            m_DataSourceIsClip.target = m_DataSource.intValue == (int)VideoSource.VideoClip;
            if (m_DataSource.hasMultipleDifferentValues)
                EditorGUILayout.HelpBox(s_Styles.selectUniformVideoSourceHelp, MessageType.Warning, false);
            else if (EditorGUILayout.BeginFadeGroup(m_DataSourceIsClip.faded))
                EditorGUILayout.PropertyField(m_VideoClip, s_Styles.videoClipContent);
            else
            {
                EditorGUILayout.PropertyField(m_Url, s_Styles.urlContent);
                Rect browseRect = EditorGUILayout.GetControlRect(true, EditorGUI.kSingleLineHeight);
                browseRect.xMin += EditorGUIUtility.labelWidth;
                browseRect.xMax = browseRect.xMin + GUI.skin.label.CalcSize(s_Styles.browseContent).x + 10;
                if (EditorGUI.DropdownButton(
                        browseRect, s_Styles.browseContent, FocusType.Passive, GUISkin.current.button))
                {
                    string[] filter =
                    {
                        // FIXME: Array should come from the player.
                        "Movie files", "dv,mp4,mpg,mpeg,m4v,ogv,vp8,webm",
                        "All files", "*"
                    };

                    string path = EditorUtility.OpenFilePanelWithFilters(
                            s_Styles.selectMovieFile, "", filter);
                    if (!string.IsNullOrEmpty(path))
                        m_Url.stringValue = "file://" + path;
                }
            }

            EditorGUILayout.EndFadeGroup();
        }

        private static int GetMaterialPropertyPopupHash(UnityEngine.Object[] objects)
        {
            int hash = 0;
            foreach (VideoPlayer vp in objects)
            {
                if (!vp)
                    continue;
                Renderer renderer = GetTargetRenderer(vp);
                if (!renderer)
                    continue;

                hash ^= vp.targetMaterialProperty.GetHashCode();
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (!material)
                        continue;
                    hash ^= material.name.GetHashCode();
                    for (int i = 0, e = ShaderUtil.GetPropertyCount(material.shader); i < e; ++i)
                    {
                        if (ShaderUtil.GetPropertyType(material.shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                            hash ^= ShaderUtil.GetPropertyName(material.shader, i).GetHashCode();
                    }
                }
            }
            return hash;
        }

        private static List<string> GetMaterialPropertyNames(UnityEngine.Object obj, bool multiSelect, out int selection, out bool invalidSelection)
        {
            selection = -1;
            invalidSelection = true;
            List<string> properties = new List<string>();
            VideoPlayer vp = obj as VideoPlayer;
            if (!vp)
                return properties;

            Renderer renderer = GetTargetRenderer(vp);
            if (!renderer)
                return properties;

            foreach (Material material in renderer.sharedMaterials)
            {
                if (material)
                {
                    for (int i = 0, e = ShaderUtil.GetPropertyCount(material.shader); i < e; ++i)
                    {
                        if (ShaderUtil.GetPropertyType(material.shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                        {
                            string propertyName = ShaderUtil.GetPropertyName(material.shader, i);
                            if (!properties.Contains(propertyName))
                                properties.Add(propertyName);
                        }
                    }
                    selection = properties.IndexOf(vp.targetMaterialProperty);
                    invalidSelection = selection < 0 && properties.Count() > 0;
                    if (invalidSelection && !multiSelect)
                    {
                        selection = properties.Count();
                        properties.Add(vp.targetMaterialProperty);
                    }
                }
            }
            return properties;
        }

        private delegate List<string> EntryGenerator(UnityEngine.Object obj, bool multiSelect, out int selection, out bool invalidSelection);
        private static GUIContent[] BuildPopupEntries(UnityEngine.Object[] objects, EntryGenerator func, out int selection, out int invalidSelections)
        {
            selection = -1;
            invalidSelections = 0;
            List<string> entries = null;
            foreach (UnityEngine.Object o in objects)
            {
                int newSelection;
                bool invalidSelection;
                List<string> newEntries = func(o, objects.Count() > 1, out newSelection, out invalidSelection);
                if (newEntries != null)
                {
                    if (invalidSelection)
                        ++invalidSelections;
                    List<string> mergedEntries =
                        entries == null ? newEntries : new List<string>(entries.Intersect(newEntries));
                    selection = entries == null ? newSelection : selection < 0 || newSelection < 0 || entries[selection] != newEntries[newSelection] ? -1 : mergedEntries.IndexOf(entries[selection]);
                    entries = mergedEntries;
                }
            }
            if (entries == null)
                entries = new List<string>();
            return entries.Select(x => new GUIContent(x)).ToArray();
        }

        private static void HandlePopup(GUIContent content, SerializedProperty property, GUIContent[] entries, int selection)
        {
            Rect pos = EditorGUILayout.GetControlRect(true, EditorGUI.kSingleLineHeight);
            GUIContent label = EditorGUI.BeginProperty(pos, content, property);
            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginDisabledGroup(entries.Count() == 0);
            selection = EditorGUI.Popup(pos, label, selection, entries);
            EditorGUI.EndDisabledGroup();
            if (EditorGUI.EndChangeCheck())
                property.stringValue = entries[selection].text;
            EditorGUI.EndProperty();
        }

        private void HandleTargetField(VideoRenderMode currentRenderMode)
        {
            m_ShowRenderTexture.target = currentRenderMode == VideoRenderMode.RenderTexture;
            if (EditorGUILayout.BeginFadeGroup(m_ShowRenderTexture.faded))
                EditorGUILayout.PropertyField(m_TargetTexture, s_Styles.textureContent);
            EditorGUILayout.EndFadeGroup();

            m_ShowTargetCamera.target = (currentRenderMode == VideoRenderMode.CameraFarPlane) ||
                (currentRenderMode == VideoRenderMode.CameraNearPlane);
            if (EditorGUILayout.BeginFadeGroup(m_ShowTargetCamera.faded))
            {
                EditorGUILayout.PropertyField(m_TargetCamera, s_Styles.cameraContent);
                EditorGUILayout.Slider(m_TargetCameraAlpha, 0.0f, 1.0f, s_Styles.alphaContent);
                // If VR is enabled in PlayerSettings on ANY platform, show the 3D layout option
                foreach (BuildPlatform cur in BuildPlatforms.instance.buildPlatforms)
                {
                    if (UnityEditorInternal.VR.VREditor.GetVREnabledOnTargetGroup(cur.targetGroup))
                    {
                        EditorGUILayout.PropertyField(m_TargetCamera3DLayout, s_Styles.camera3DLayout);
                        break;
                    }
                }
            }
            EditorGUILayout.EndFadeGroup();

            m_ShowRenderer.target = currentRenderMode == VideoRenderMode.MaterialOverride;
            if (EditorGUILayout.BeginFadeGroup(m_ShowRenderer.faded))
            {
                bool hasMultipleSelection = targets.Count() > 1;
                if (hasMultipleSelection)
                    EditorGUILayout.PropertyField(m_TargetMaterialRenderer, s_Styles.materialRendererContent);
                else
                {
                    Rect rect = EditorGUILayout.GetControlRect(true, EditorGUI.kSingleLineHeight);
                    GUIContent label = EditorGUI.BeginProperty(
                            rect, s_Styles.materialRendererContent, m_TargetMaterialRenderer);
                    EditorGUI.BeginChangeCheck();
                    var newRenderer = EditorGUI.ObjectField(
                            rect, label, GetTargetRenderer((VideoPlayer)target), typeof(Renderer), true);
                    if (EditorGUI.EndChangeCheck())
                        m_TargetMaterialRenderer.objectReferenceValue = newRenderer;
                    EditorGUI.EndProperty();
                }

                int curHash = GetMaterialPropertyPopupHash(targets);
                if (m_MaterialPropertyPopupContentHash != curHash)
                    m_MaterialPropertyPopupContent = BuildPopupEntries(
                            targets, GetMaterialPropertyNames, out m_MaterialPropertyPopupSelection,
                            out m_MaterialPropertyPopupInvalidSelections);
                HandlePopup(s_Styles.materialPropertyContent, m_TargetMaterialProperty, m_MaterialPropertyPopupContent, m_MaterialPropertyPopupSelection);
                if (m_MaterialPropertyPopupInvalidSelections > 0 || m_MaterialPropertyPopupContent.Length == 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(EditorGUIUtility.labelWidth);
                    if (m_MaterialPropertyPopupContent.Length == 0)
                    {
                        if (!hasMultipleSelection)
                            EditorGUILayout.HelpBox(s_Styles.rendererMaterialsHaveNoTexPropsHelp, MessageType.Warning);
                        else
                            EditorGUILayout.HelpBox(s_Styles.someRendererMaterialsHaveNoTexPropsHelp, MessageType.Warning);
                    }
                    else if (!hasMultipleSelection)
                        EditorGUILayout.HelpBox(s_Styles.invalidTexPropSelectionHelp, MessageType.Warning);
                    else if (m_MaterialPropertyPopupInvalidSelections == 1)
                        EditorGUILayout.HelpBox(s_Styles.oneInvalidTexPropSelectionHelp, MessageType.Warning);
                    else
                        EditorGUILayout.HelpBox(
                            string.Format(s_Styles.someInvalidTexPropSelectionsHelp, m_MaterialPropertyPopupInvalidSelections),
                            MessageType.Warning);
                    GUILayout.EndHorizontal();
                }
                else
                    DisplayMultiMaterialInformation(m_MaterialPropertyPopupContentHash != curHash);

                m_MaterialPropertyPopupContentHash = curHash;
            }
            EditorGUILayout.EndFadeGroup();

            m_ShowAspectRatio.target =
                currentRenderMode != VideoRenderMode.MaterialOverride &&
                currentRenderMode != VideoRenderMode.APIOnly;
            if (EditorGUILayout.BeginFadeGroup(m_ShowAspectRatio.faded))
                EditorGUILayout.PropertyField(m_AspectRatio, s_Styles.aspectRatioLabel);
            EditorGUILayout.EndFadeGroup();
        }

        private void DisplayMultiMaterialInformation(bool refreshInfo)
        {
            if (refreshInfo || m_MultiMaterialInfo == null)
                m_MultiMaterialInfo = GenerateMultiMaterialinformation();

            if (string.IsNullOrEmpty(m_MultiMaterialInfo))
                return;

            GUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            EditorGUILayout.HelpBox(m_MultiMaterialInfo, MessageType.Info);
            GUILayout.EndHorizontal();
        }

        private string GenerateMultiMaterialinformation()
        {
            if (targets.Count() > 1)
                return "";

            VideoPlayer vp = target as VideoPlayer;
            if (!vp)
                return "";

            Renderer renderer = GetTargetRenderer(vp);
            if (!renderer)
                return "";

            var sharedMaterials = renderer.sharedMaterials;
            if (sharedMaterials == null || sharedMaterials.Count() <= 1)
                return "";

            var targetMaterials = new List<string>();

            foreach (Material material in sharedMaterials)
            {
                if (!material)
                    continue;
                for (int i = 0, e = ShaderUtil.GetPropertyCount(material.shader); i < e; ++i)
                {
                    if ((ShaderUtil.GetPropertyType(material.shader, i) ==
                         ShaderUtil.ShaderPropertyType.TexEnv) &&
                        (ShaderUtil.GetPropertyName(material.shader, i) ==
                         m_TargetMaterialProperty.stringValue))
                    {
                        targetMaterials.Add(material.name);
                        break;
                    }
                }
            }

            if (targetMaterials.Count() == sharedMaterials.Count())
                return s_Styles.texPropInAllMaterialsHelp;

            return string.Format(
                s_Styles.texPropInSomeMaterialsHelp,
                targetMaterials.Count(), sharedMaterials.Count()) + ": " +
                string.Join(", ", targetMaterials.ToArray());
        }

        private void HandleAudio()
        {
            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_AudioOutputMode, s_Styles.audioOutputModeContent);

            m_ShowAudioControls.target = (VideoAudioOutputMode)m_AudioOutputMode.intValue != VideoAudioOutputMode.None;
            if (EditorGUILayout.BeginFadeGroup(m_ShowAudioControls.faded))
            {
                // FIXME: Due to a bug in the behaviour of the widgets used in
                // this multi-selection-capable code, we are disabling
                // multi-select editing for now.  The array of widgets being
                // constructed ends up being garbled (no crash, just incorrect
                // content).  After discussing with @shawn, it was agreed to
                // handle this bug separately and disable multi-editing here for
                // the time being.
                if (serializedObject.isEditingMultipleObjects)
                    EditorGUILayout.HelpBox(s_Styles.audioControlsNotEditableHelp, MessageType.Warning, false);
                else if (m_AudioOutputMode.hasMultipleDifferentValues)
                    EditorGUILayout.HelpBox(s_Styles.selectUniformAudioOutputModeHelp, MessageType.Warning, false);
                else
                {
                    ushort trackCountBefore = (ushort)m_ControlledAudioTrackCount.intValue;
                    HandleControlledAudioTrackCount();
                    if (m_ControlledAudioTrackCount.hasMultipleDifferentValues)
                        EditorGUILayout.HelpBox(s_Styles.selectUniformAudioTracksHelp, MessageType.Warning, false);
                    else
                    {
                        VideoAudioOutputMode audioOutputMode = (VideoAudioOutputMode)m_AudioOutputMode.intValue;

                        // VideoPlayer::CheckConsistency keeps the array sizes in
                        // sync with the (possible) change done in
                        // HandleControlledAudioTrackCount().  But this adjustment is
                        // only done later so we conservatively only iterate over the
                        // smallest known number of tracks we know are initialized.
                        ushort trackCount = (ushort)Math.Min(
                                (ushort)m_ControlledAudioTrackCount.intValue, trackCountBefore);
                        trackCount = (ushort)Math.Min(trackCount, m_EnabledAudioTracks.arraySize);

                        for (ushort trackIdx = 0; trackIdx < trackCount; ++trackIdx)
                        {
                            EditorGUILayout.PropertyField(
                                m_EnabledAudioTracks.GetArrayElementAtIndex(trackIdx),
                                GetAudioTrackEnabledContent(trackIdx));

                            EditorGUI.indentLevel++;
                            if (audioOutputMode == VideoAudioOutputMode.AudioSource)
                            {
                                EditorGUILayout.PropertyField(
                                    m_TargetAudioSources.GetArrayElementAtIndex(trackIdx),
                                    s_Styles.audioSourceContent);
                            }
                            else if (audioOutputMode == VideoAudioOutputMode.Direct)
                            {
                                EditorGUILayout.PropertyField(
                                    m_DirectAudioMutes.GetArrayElementAtIndex(trackIdx),
                                    s_Styles.muteLabel);
                                EditorGUILayout.Slider(
                                    m_DirectAudioVolumes.GetArrayElementAtIndex(trackIdx), 0.0f, 1.0f,
                                    s_Styles.volumeLabel);
                            }
                            EditorGUI.indentLevel--;
                        }
                    }
                }
            }
            EditorGUILayout.EndFadeGroup();
        }

        GUIContent GetAudioTrackEnabledContent(ushort trackIdx)
        {
            while (m_AudioTrackInfos.Count <= trackIdx)
                m_AudioTrackInfos.Add(new AudioTrackInfo());

            AudioTrackInfo info = m_AudioTrackInfos[trackIdx];

            VideoPlayer player = null;
            if (!serializedObject.isEditingMultipleObjects)
                player = (VideoPlayer)target;

            // Only produce a decorated track label with single-selection.  No
            // point trying to come up with a label that makes the average of
            // the current track params...
            string language = player ? player.GetAudioLanguageCode(trackIdx) : "";
            ushort channelCount = player ? player.GetAudioChannelCount(trackIdx) : (ushort)0;

            if (language != info.language || channelCount != info.channelCount || info.content == null)
            {
                string trackDetails = "";
                if (language.Length > 0)
                    trackDetails += language;

                if (channelCount > 0)
                {
                    if (trackDetails.Length > 0)
                        trackDetails += ", ";
                    trackDetails += channelCount.ToString() + " ch";
                }

                if (trackDetails.Length > 0)
                    trackDetails = " [" + trackDetails + "]";

                info.content = EditorGUIUtility.TextContent("Track " + trackIdx + trackDetails);
                info.content.tooltip = s_Styles.enableDecodingTooltip;
            }

            return info.content;
        }

        private void HandleControlledAudioTrackCount()
        {
            // Won't show the widget for number of controlled tracks if we're
            // just using VideoClips (for which editing this property doesn't
            // make sense) or mixing VideoClips and URLs.
            if (m_DataSourceIsClip.value || m_DataSource.hasMultipleDifferentValues)
                return;

            VideoPlayer player = (VideoPlayer)target;
            ushort audioTrackCount = serializedObject.isEditingMultipleObjects ? (ushort)0 : player.audioTrackCount;
            GUIContent controlledAudioTrackCountContent;
            if (audioTrackCount == 0)
            {
                // Use the simple undecorated label (without number of existing tracks)
                // when editing multiple objects so we don't need fancy logic
                // to explain that the number of discovered tracks is not uniform
                // across multi-selection.
                controlledAudioTrackCountContent = s_Styles.controlledAudioTrackCountContent;
            }
            else
            {
                // Manage a cached decorated GUIContent where we show how many
                // existing tracks there are in the URL being played.  Doing
                // this to avoid repeatedly construct this string.
                if (audioTrackCount != m_AudioTrackCountCached)
                {
                    m_AudioTrackCountCached = audioTrackCount;
                    m_ControlledAudioTrackCountContent = EditorGUIUtility.TextContent(
                            s_Styles.controlledAudioTrackCountContent.text + " [" + audioTrackCount + " found]");
                    m_ControlledAudioTrackCountContent.tooltip = s_Styles.controlledAudioTrackCountContent.tooltip;
                }
                controlledAudioTrackCountContent = m_ControlledAudioTrackCountContent;
            }

            EditorGUILayout.PropertyField(m_ControlledAudioTrackCount, controlledAudioTrackCountContent);
        }

        private void PrepareCompleted(VideoPlayer vp)
        {
            Repaint();
        }

        static private Renderer GetTargetRenderer(VideoPlayer vp)
        {
            Renderer renderer = vp.targetMaterialRenderer;
            if (renderer)
                return renderer;
            return vp.gameObject.GetComponent<Renderer>();
        }
    }
}

