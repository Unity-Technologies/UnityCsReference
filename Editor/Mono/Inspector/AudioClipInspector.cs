// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(AudioClip))]
    [CanEditMultipleObjects]
    internal class AudioClipInspector : Editor
    {
        private PreviewRenderUtility m_PreviewUtility;
        private AudioClip m_Clip;
        private bool playing => s_PlayingInstance == this && m_Clip != null && AudioUtil.IsPreviewClipPlaying();
        Vector2 m_Position = Vector2.zero;
        private bool m_MultiEditing;

        static GUIStyle s_PreButton;

        static Rect s_WantedRect;
        static bool s_AutoPlay;
        static bool s_Loop;
        static bool s_PlayFirst;
        static AudioClipInspector s_PlayingInstance;

        static GUIContent s_PlayIcon;
        static GUIContent s_AutoPlayIcon;
        static GUIContent s_LoopIcon;

        static Texture2D s_DefaultIcon;

        private Material m_HandleLinesMaterial;

        public override void OnInspectorGUI()
        {
            // We can't always check this from preview methods
            m_MultiEditing = targets.Length > 1;
            // Override with inspector that doesn't show anything
        }

        static void Init()
        {
            if (s_PreButton != null)
                return;
            s_PreButton = "preButton";

            s_AutoPlay = EditorPrefs.GetBool("AutoPlayAudio", false);
            s_Loop = false;

            s_AutoPlayIcon = EditorGUIUtility.TrIconContent("preAudioAutoPlayOff", "Turn Auto Play on/off");
            s_PlayIcon = EditorGUIUtility.TrIconContent("PlayButton", "Play");
            s_LoopIcon = EditorGUIUtility.TrIconContent("preAudioLoopOff", "Loop on/off");

            s_DefaultIcon = EditorGUIUtility.LoadIcon("Profiler.Audio");
        }

        public void OnDisable()
        {
            if (s_PlayingInstance == this)
            {
                AudioUtil.StopAllPreviewClips();
                s_PlayingInstance = null;
            }

            EditorPrefs.SetBool("AutoPlayAudio", s_AutoPlay);

            if (m_PreviewUtility != null)
            {
                m_PreviewUtility.Cleanup();
                m_PreviewUtility = null;
            }

            m_HandleLinesMaterial = null;
        }

        public void OnEnable()
        {
            s_AutoPlay = EditorPrefs.GetBool("AutoPlayAudio", false);
            if (s_AutoPlay)
                s_PlayFirst = true;

            m_HandleLinesMaterial = EditorGUIUtility.LoadRequired("SceneView/HandleLines.mat") as Material;
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            AudioClip clip = target as AudioClip;

            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            AudioImporter audioImporter = importer as AudioImporter;

            if (audioImporter == null || !ShaderUtil.hardwareSupportsRectRenderTexture)
                return null;

            if (m_PreviewUtility == null)
                m_PreviewUtility = new PreviewRenderUtility();

            m_PreviewUtility.BeginStaticPreview(new Rect(0, 0, width, height));
            m_HandleLinesMaterial.SetPass(0);

            // We're drawing into an offscreen here which will have a resolution defined by EditorGUIUtility.pixelsPerPoint. This is different from the DoRenderPreview call below where we draw directly to the screen, so we need to take
            // the higher resolution into account when drawing into the offscreen, otherwise only the upper-left quarter of the preview texture will be drawn.
            DoRenderPreview(false, clip, audioImporter, new Rect(0.05f * width * EditorGUIUtility.pixelsPerPoint, 0.05f * width * EditorGUIUtility.pixelsPerPoint, 1.9f * width * EditorGUIUtility.pixelsPerPoint, 1.9f * height * EditorGUIUtility.pixelsPerPoint), 1.0f);

            return m_PreviewUtility.EndStaticPreview();
        }

        public override bool HasPreviewGUI()
        {
            return (targets != null);
        }

        public override void OnPreviewSettings()
        {
            if (s_DefaultIcon == null) Init();

            AudioClip clip = target as AudioClip;
            m_MultiEditing = targets.Length > 1;

            {
                using (new EditorGUI.DisabledScope(m_MultiEditing && !playing))
                {
                    bool newPlaying = GUILayout.Toggle(playing, s_PlayIcon, EditorStyles.toolbarButton);

                    if (newPlaying != playing)
                    {
                        if (newPlaying)
                            PlayClip(clip, 0, s_Loop);
                        else
                        {
                            AudioUtil.StopAllPreviewClips();
                            m_Clip = null;
                        }
                    }
                }

                using (new EditorGUI.DisabledScope(m_MultiEditing))
                {
                    s_AutoPlay = s_AutoPlay && !m_MultiEditing;
                    s_AutoPlay = GUILayout.Toggle(s_AutoPlay, s_AutoPlayIcon, EditorStyles.toolbarButton);
                }

                bool loop = s_Loop;
                s_Loop = GUILayout.Toggle(s_Loop, s_LoopIcon, EditorStyles.toolbarButton);
                if ((loop != s_Loop) && playing)
                    AudioUtil.LoopPreviewClip(s_Loop);
            }
        }

        void PlayClip(AudioClip clip, int startSample = 0, bool loop = false)
        {
            AudioUtil.StopAllPreviewClips();
            AudioUtil.PlayPreviewClip(clip, startSample, loop);
            m_Clip = clip;
            s_PlayingInstance = this;
        }

        // Passing in clip and importer separately as we're not completely done with the asset setup at the time we're asked to generate the preview.
        private void DoRenderPreview(bool setMaterial, AudioClip clip, AudioImporter audioImporter, Rect wantedRect, float scaleFactor)
        {
            scaleFactor *= 0.95f; // Reduce amplitude slightly to make highly compressed signals fit.
            float[] minMaxData = (audioImporter == null) ? null : AudioUtil.GetMinMaxData(audioImporter);
            int numChannels = clip.channels;
            int numSamples = (minMaxData == null) ? 0 : (minMaxData.Length / (2 * numChannels));
            float h = (float)wantedRect.height / (float)numChannels;
            for (int channel = 0; channel < numChannels; channel++)
            {
                Rect channelRect = new Rect(wantedRect.x, wantedRect.y + h * channel, wantedRect.width, h);
                Color curveColor = new Color(1.0f, 140.0f / 255.0f, 0.0f, 1.0f);

                AudioCurveRendering.AudioMinMaxCurveAndColorEvaluator dlg = delegate(float x, out Color col, out float minValue, out float maxValue)
                {
                    col = curveColor;
                    if (numSamples <= 0)
                    {
                        minValue = 0.0f;
                        maxValue = 0.0f;
                    }
                    else
                    {
                        float p = Mathf.Clamp(x * (numSamples - 2), 0.0f, numSamples - 2);
                        int i = (int)Mathf.Floor(p);
                        int offset1 = (i * numChannels + channel) * 2;
                        int offset2 = offset1 + numChannels * 2;
                        minValue = Mathf.Min(minMaxData[offset1 + 1], minMaxData[offset2 + 1]) * scaleFactor;
                        maxValue = Mathf.Max(minMaxData[offset1 + 0], minMaxData[offset2 + 0]) * scaleFactor;
                        if (minValue > maxValue) { float tmp = minValue; minValue = maxValue; maxValue = tmp; }
                    }
                };

                if (setMaterial)
                    AudioCurveRendering.DrawMinMaxFilledCurve(channelRect, dlg);
                else
                    AudioCurveRendering.DrawMinMaxFilledCurveInternal(channelRect, dlg);
            }
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (s_DefaultIcon == null) Init();

            AudioClip clip = target as AudioClip;

            Event evt = Event.current;
            if (evt.type != EventType.Repaint && evt.type != EventType.Layout && evt.type != EventType.Used)
            {
                switch (evt.type)
                {
                    case EventType.MouseDrag:
                    case EventType.MouseDown:
                    {
                        if (r.Contains(evt.mousePosition))
                        {
                            var startSample = (int)(evt.mousePosition.x * (AudioUtil.GetSampleCount(clip) / (int)r.width));
                            if (!AudioUtil.IsPreviewClipPlaying() || clip != m_Clip)
                                PlayClip(clip, startSample, s_Loop);
                            else
                                AudioUtil.SetPreviewClipSamplePosition(clip, startSample);
                            evt.Use();
                        }
                    }
                    break;
                }
                return;
            }

            if (Event.current.type == EventType.Repaint)
                background.Draw(r, false, false, false, false);

            int c = AudioUtil.GetChannelCount(clip);
            s_WantedRect = new Rect(r.x, r.y , r.width, r.height);
            float sec2px = ((float)s_WantedRect.width / clip.length);

            bool previewAble = AudioUtil.HasPreview(clip) || !(AudioUtil.IsTrackerFile(clip));
            if (!previewAble)
            {
                float labelY = (r.height > 150) ? r.y + (r.height / 2) - 10 :  r.y +  (r.height / 2) - 25;
                if (r.width > 64)
                {
                    if (AudioUtil.IsTrackerFile(clip))
                    {
                        EditorGUI.DropShadowLabel(new Rect(r.x, labelY, r.width, 20), string.Format("Module file with " + AudioUtil.GetMusicChannelCount(clip) + " channels."));
                    }
                    else
                        EditorGUI.DropShadowLabel(new Rect(r.x, labelY, r.width, 20), "Can not show PCM data for this file");
                }

                if (m_Clip == clip)
                {
                    float t = AudioUtil.GetPreviewClipPosition();

                    System.TimeSpan ts = new System.TimeSpan(0, 0, 0, 0, (int)(t * 1000.0f));

                    EditorGUI.DropShadowLabel(new Rect(s_WantedRect.x, s_WantedRect.y, s_WantedRect.width, 20), string.Format("Playing - {0:00}:{1:00}.{2:000}", ts.Minutes, ts.Seconds, ts.Milliseconds));
                }
            }
            else
            {
                PreviewGUI.BeginScrollView(s_WantedRect, m_Position, s_WantedRect, "PreHorizontalScrollbar", "PreHorizontalScrollbarThumb");

                if (Event.current.type == EventType.Repaint)
                {
                    DoRenderPreview(true, clip, AudioUtil.GetImporterFromClip(clip), s_WantedRect, 1.0f);
                }

                for (int i = 0; i < c; ++i)
                {
                    if (c > 1 && r.width > 64)
                    {
                        var labelRect = new Rect(s_WantedRect.x + 5, s_WantedRect.y + (s_WantedRect.height / c) * i, 30, 20);
                        EditorGUI.DropShadowLabel(labelRect, "ch " + (i + 1));
                    }
                }

                if (m_Clip == clip)
                {
                    float t = AudioUtil.GetPreviewClipPosition();

                    System.TimeSpan ts = new System.TimeSpan(0, 0, 0, 0, (int)(t * 1000.0f));

                    GUI.DrawTexture(new Rect(s_WantedRect.x + (int)(sec2px * t), s_WantedRect.y, 2, s_WantedRect.height), EditorGUIUtility.whiteTexture);
                    if (r.width > 64)
                        EditorGUI.DropShadowLabel(new Rect(s_WantedRect.x, s_WantedRect.y, s_WantedRect.width, 20), string.Format("{0:00}:{1:00}.{2:000}", ts.Minutes, ts.Seconds, ts.Milliseconds));
                    else
                        EditorGUI.DropShadowLabel(new Rect(s_WantedRect.x, s_WantedRect.y, s_WantedRect.width, 20), string.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds));
                }

                PreviewGUI.EndScrollView();
            }


            if (!m_MultiEditing && (s_PlayFirst || (s_AutoPlay && m_Clip != clip)))
            {
                // Autoplay preview
                PlayClip(clip, 0, s_Loop);
                s_PlayFirst = false;
            }

            // force update GUI
            if (playing)
                GUIView.current.Repaint();
        }

        public override string GetInfoString()
        {
            AudioClip clip = target as AudioClip;
            int c = AudioUtil.GetChannelCount(clip);
            string ch = c == 1 ? "Mono" : c == 2 ? "Stereo" : (c - 1) + ".1";
            AudioCompressionFormat platformFormat = AudioUtil.GetTargetPlatformSoundCompressionFormat(clip);
            AudioCompressionFormat editorFormat = AudioUtil.GetSoundCompressionFormat(clip);
            string s = platformFormat.ToString();
            if (platformFormat != editorFormat)
                s += " (" + editorFormat + " in editor" + ")";
            s += ", " + AudioUtil.GetFrequency(clip) + " Hz, " + ch + ", ";

            System.TimeSpan ts = new System.TimeSpan(0, 0, 0, 0, (int)AudioUtil.GetDuration(clip));

            if ((uint)AudioUtil.GetDuration(clip) == 0xffffffff)
                s += "Unlimited";
            else
                s += UnityString.Format("{0:00}:{1:00}.{2:000}", ts.Minutes, ts.Seconds, ts.Milliseconds);

            return s;
        }
    }
}
