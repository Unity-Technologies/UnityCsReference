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

        // Any number of AudioClip inspectors can be docked in addition to the object browser, and they are all showing and modifying the same shared state.
        static AudioClipInspector m_PlayingInspector;
        static AudioClip m_PlayingClip;
        static bool playing { get { return m_PlayingClip != null && AudioUtil.IsClipPlaying(m_PlayingClip); } }
        static bool m_bAutoPlay;
        static bool m_bLoop;

        Vector2 m_Position = Vector2.zero;
        Rect m_wantedRect;

        static GUIStyle s_PreButton;

        static GUIContent[] s_PlayIcons = {null, null};
        static GUIContent[] s_AutoPlayIcons = {null, null};
        static GUIContent[] s_LoopIcons = {null, null};

        static Texture2D s_DefaultIcon;

        override public void OnInspectorGUI()
        {
            // Override with inspector that doesn't show anything
        }

        static void Init()
        {
            if (s_PreButton != null)
                return;
            s_PreButton = "preButton";

            m_bAutoPlay = EditorPrefs.GetBool("AutoPlayAudio", false);

            s_AutoPlayIcons[0] = EditorGUIUtility.IconContent("preAudioAutoPlayOff", "|Turn Auto Play on");
            s_AutoPlayIcons[1] = EditorGUIUtility.IconContent("preAudioAutoPlayOn", "|Turn Auto Play off");
            s_PlayIcons[0] = EditorGUIUtility.IconContent("preAudioPlayOff", "|Play");
            s_PlayIcons[1] = EditorGUIUtility.IconContent("preAudioPlayOn", "|Stop");
            s_LoopIcons[0] = EditorGUIUtility.IconContent("preAudioLoopOff", "|Loop on");
            s_LoopIcons[1] = EditorGUIUtility.IconContent("preAudioLoopOn", "|Loop off");

            s_DefaultIcon = EditorGUIUtility.LoadIcon("Profiler.Audio");
        }

        public void OnDisable()
        {
            // This check is necessary because the order of OnEnable/OnDisable varies depending on whether the inspector is embedded in the project browser or object selector.
            if (m_PlayingInspector == this)
            {
                AudioUtil.StopAllClips();
                m_PlayingClip = null;
            }

            EditorPrefs.SetBool("AutoPlayAudio", m_bAutoPlay);
        }

        public void OnEnable()
        {
            AudioUtil.StopAllClips();
            m_PlayingClip = null;
            m_PlayingInspector = this;

            m_bAutoPlay = EditorPrefs.GetBool("AutoPlayAudio", false);
        }

        public void OnDestroy()
        {
            if (m_PreviewUtility != null)
            {
                m_PreviewUtility.Cleanup();
                m_PreviewUtility = null;
            }
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

            // We're drawing into an offscreen here which will have a resolution defined by EditorGUIUtility.pixelsPerPoint. This is different from the DoRenderPreview call below where we draw directly to the screen, so we need to take
            // the higher resolution into account when drawing into the offscreen, otherwise only the upper-left quarter of the preview texture will be drawn.
            DoRenderPreview(clip, audioImporter, new Rect(0.05f * width * EditorGUIUtility.pixelsPerPoint, 0.05f * width * EditorGUIUtility.pixelsPerPoint, 1.9f * width * EditorGUIUtility.pixelsPerPoint, 1.9f * height * EditorGUIUtility.pixelsPerPoint), 1.0f);

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

            using (new EditorGUI.DisabledScope(AudioUtil.IsMovieAudio(clip)))
            {
                bool isEditingMultipleObjects = targets.Length > 1;

                using (new EditorGUI.DisabledScope(isEditingMultipleObjects))
                {
                    bool oldAutoPlay = isEditingMultipleObjects ? false : m_bAutoPlay;
                    bool newAutoPlay = PreviewGUI.CycleButton(oldAutoPlay ? 1 : 0, s_AutoPlayIcons) != 0;
                    if (oldAutoPlay != newAutoPlay)
                    {
                        m_bAutoPlay = newAutoPlay;
                        InspectorWindow.RepaintAllInspectors();
                    }

                    bool oldLoop = isEditingMultipleObjects ? false : m_bLoop;
                    bool newLoop = PreviewGUI.CycleButton(oldLoop ? 1 : 0, s_LoopIcons) != 0;
                    if (oldLoop != newLoop)
                    {
                        m_bLoop = newLoop;
                        if (playing)
                            AudioUtil.LoopClip(clip, newLoop);
                        InspectorWindow.RepaintAllInspectors();
                    }
                }

                using (new EditorGUI.DisabledScope(isEditingMultipleObjects && !playing && m_PlayingInspector != this))
                {
                    bool curPlaying = m_PlayingInspector == this && playing;
                    bool newPlaying = PreviewGUI.CycleButton(curPlaying ? 1 : 0, s_PlayIcons) != 0;

                    if (newPlaying != curPlaying)
                    {
                        AudioUtil.StopAllClips();

                        if (newPlaying)
                        {
                            AudioUtil.PlayClip(clip, 0, m_bLoop);
                            m_PlayingClip = clip;
                            m_PlayingInspector = this;
                        }
                    }
                }
            }
        }

        // Passing in clip and importer separately as we're not completely done with the asset setup at the time we're asked to generate the preview.
        private void DoRenderPreview(AudioClip clip, AudioImporter audioImporter, Rect wantedRect, float scaleFactor)
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

                AudioCurveRendering.DrawMinMaxFilledCurve(
                    channelRect,
                    delegate(float x, out Color col, out float minValue, out float maxValue)
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
                    }
                    );
            }
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (s_DefaultIcon == null) Init();

            AudioClip clip = target as AudioClip;

            Event evt = Event.current;
            if (evt.type != EventType.Repaint && evt.type != EventType.Layout && evt.type != EventType.Used)
            {
                int px2sample = (AudioUtil.GetSampleCount(clip) / (int)r.width);

                switch (evt.type)
                {
                    case EventType.MouseDrag:
                    case EventType.MouseDown:
                    {
                        if (r.Contains(evt.mousePosition) && !AudioUtil.IsMovieAudio(clip))
                        {
                            if (m_PlayingClip != clip || !AudioUtil.IsClipPlaying(clip))
                            {
                                AudioUtil.StopAllClips();
                                AudioUtil.PlayClip(clip, 0, m_bLoop);
                                m_PlayingClip = clip;
                                m_PlayingInspector = this;
                            }
                            AudioUtil.SetClipSamplePosition(clip, px2sample * (int)evt.mousePosition.x);
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
            m_wantedRect = new Rect(r.x, r.y , r.width, r.height);
            float sec2px = ((float)m_wantedRect.width / clip.length);

            bool previewAble = AudioUtil.HasPreview(clip) || !(AudioUtil.IsTrackerFile(clip) || AudioUtil.IsMovieAudio(clip));
            if (!previewAble)
            {
                float labelY = (r.height > 150) ? r.y + (r.height / 2) - 10 :  r.y +  (r.height / 2) - 25;
                if (r.width > 64)
                {
                    if (AudioUtil.IsTrackerFile(clip))
                    {
                        EditorGUI.DropShadowLabel(new Rect(r.x, labelY, r.width, 20), string.Format("Module file with " + AudioUtil.GetMusicChannelCount(clip) + " channels."));
                    }
                    else if (AudioUtil.IsMovieAudio(clip))
                    {
                        if (r.width > 450)
                            EditorGUI.DropShadowLabel(new Rect(r.x, labelY, r.width, 20), "Audio is attached to a movie. To audition the sound, play the movie.");
                        else
                        {
                            EditorGUI.DropShadowLabel(new Rect(r.x, labelY, r.width, 20), "Audio is attached to a movie.");
                            EditorGUI.DropShadowLabel(new Rect(r.x, labelY + 10, r.width, 20), "To audition the sound, play the movie.");
                        }
                    }
                    else
                        EditorGUI.DropShadowLabel(new Rect(r.x, labelY, r.width, 20), "Can not show PCM data for this file");
                }

                if (m_PlayingInspector == this && m_PlayingClip == clip)
                {
                    float t = AudioUtil.GetClipPosition(clip);

                    System.TimeSpan ts = new System.TimeSpan(0, 0, 0, 0, (int)(t * 1000.0f));

                    EditorGUI.DropShadowLabel(new Rect(m_wantedRect.x, m_wantedRect.y, m_wantedRect.width, 20), string.Format("Playing - {0:00}:{1:00}.{2:000}", ts.Minutes, ts.Seconds, ts.Milliseconds));
                }
            }
            else
            {
                PreviewGUI.BeginScrollView(m_wantedRect, m_Position, m_wantedRect, "PreHorizontalScrollbar", "PreHorizontalScrollbarThumb");

                if (Event.current.type == EventType.Repaint)
                {
                    DoRenderPreview(clip, AudioUtil.GetImporterFromClip(clip), m_wantedRect, 1.0f);
                }

                for (int i = 0; i < c; ++i)
                {
                    if (c > 1 && r.width > 64)
                    {
                        var labelRect = new Rect(m_wantedRect.x + 5, m_wantedRect.y + (m_wantedRect.height / c) * i, 30, 20);
                        EditorGUI.DropShadowLabel(labelRect, "ch " + (i + 1).ToString());
                    }
                }

                if (m_PlayingInspector == this && m_PlayingClip == clip)
                {
                    float t = AudioUtil.GetClipPosition(clip);

                    System.TimeSpan ts = new System.TimeSpan(0, 0, 0, 0, (int)(t * 1000.0f));

                    GUI.DrawTexture(new Rect(m_wantedRect.x + (int)(sec2px * t), m_wantedRect.y, 2, m_wantedRect.height), EditorGUIUtility.whiteTexture);
                    if (r.width > 64)
                        EditorGUI.DropShadowLabel(new Rect(m_wantedRect.x, m_wantedRect.y, m_wantedRect.width, 20), string.Format("{0:00}:{1:00}.{2:000}", ts.Minutes, ts.Seconds, ts.Milliseconds));
                    else
                        EditorGUI.DropShadowLabel(new Rect(m_wantedRect.x, m_wantedRect.y, m_wantedRect.width, 20), string.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds));
                }


                PreviewGUI.EndScrollView();
            }


            // autoplay start?
            if (m_bAutoPlay && m_PlayingClip != clip && m_PlayingInspector == this)
            {
                AudioUtil.StopAllClips();
                AudioUtil.PlayClip(clip, 0, m_bLoop);
                m_PlayingClip = clip;
                m_PlayingInspector = this;
            }

            // force update GUI
            if (playing)
                GUIView.current.Repaint();
        }

        public override string GetInfoString()
        {
            AudioClip clip = target as AudioClip;
            int c = AudioUtil.GetChannelCount(clip);
            string ch = c == 1 ? "Mono" : c == 2 ? "Stereo" : (c - 1).ToString() + ".1";
            AudioCompressionFormat platformFormat = AudioUtil.GetTargetPlatformSoundCompressionFormat(clip);
            AudioCompressionFormat editorFormat = AudioUtil.GetSoundCompressionFormat(clip);
            string s = platformFormat.ToString();
            if (platformFormat != editorFormat)
                s += " (" + editorFormat.ToString() + " in editor" + ")";
            s += ", " + AudioUtil.GetFrequency(clip) + " Hz, " + ch + ", ";

            System.TimeSpan ts = new System.TimeSpan(0, 0, 0, 0, (int)AudioUtil.GetDuration(clip));

            if ((uint)AudioUtil.GetDuration(clip) == 0xffffffff)
                s += "Unlimited";
            else
                s += string.Format("{0:00}:{1:00}.{2:000}", ts.Minutes, ts.Seconds, ts.Milliseconds);

            return s;
        }
    }
}
