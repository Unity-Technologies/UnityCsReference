// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    [CustomEditor(typeof(MovieTexture))]
    [CanEditMultipleObjects]
    internal class MovieTextureInspector : TextureInspector
    {
        static GUIContent[] s_PlayIcons = {null, null};

        static void Init()
        {
            s_PlayIcons[0] = EditorGUIUtility.IconContent("preAudioPlayOff");
            s_PlayIcons[1] = EditorGUIUtility.IconContent("preAudioPlayOn");
        }

        protected override void OnEnable() {}

        public override void OnInspectorGUI() {}

        public override void OnPreviewSettings()
        {
            Init();

            // Disallow playing movie previews in play mode. Better not to interfere
            // with any playback the game does.
            // Also disallow if more than one MovieClip selected (for now).
            using (new EditorGUI.DisabledScope(Application.isPlaying || targets.Length > 1))
            {
                MovieTexture t = target as MovieTexture;
                AudioClip  ac = t.audioClip;
                bool isPlaying = PreviewGUI.CycleButton(t.isPlaying ? 1 : 0, s_PlayIcons) != 0;
                if (isPlaying != t.isPlaying)
                {
                    if (isPlaying)
                    {
                        t.Stop();
                        t.Play();
                        if (ac != null)
                            AudioUtil.PlayClip(ac);
                    }
                    else
                    {
                        t.Pause();
                        if (ac != null)
                            AudioUtil.PauseClip(ac);
                    }
                }
            }
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type == EventType.Repaint)
                background.Draw(r, false, false, false, false);

            // show texture
            MovieTexture t = target as MovieTexture;

            float zoomLevel = Mathf.Min(Mathf.Min(r.width / t.width, r.height / t.height), 1);
            Rect wantedRect = new Rect(r.x, r.y, t.width * zoomLevel, t.height * zoomLevel);
            PreviewGUI.BeginScrollView(r, m_Pos, wantedRect, "PreHorizontalScrollbar", "PreHorizontalScrollbarThumb");
            EditorGUI.DrawPreviewTexture(wantedRect, t, null, ScaleMode.StretchToFill);
            m_Pos = PreviewGUI.EndScrollView();

            // force update GUI
            if (t.isPlaying)
                GUIView.current.Repaint();

            if (Application.isPlaying)
            {
                if (t.isPlaying)
                    EditorGUI.DropShadowLabel(new Rect(r.x, r.y + 10, r.width, 20), "Can't pause preview when in play mode");
                else
                    EditorGUI.DropShadowLabel(new Rect(r.x, r.y + 10, r.width, 20), "Can't start preview when in play mode");
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            MovieTexture t = target as MovieTexture;

            //stop movies we started.
            if (!Application.isPlaying && t != null)
            {
                AudioClip  ac = t.audioClip;
                t.Stop();
                if (ac != null)
                    AudioUtil.StopClip(ac);
            }
        }

        public override string GetInfoString()
        {
            string result = base.GetInfoString();

            MovieTexture t = target as MovieTexture;
            if (!t.isReadyToPlay)
                result += "/nNot ready to play yet.";

            return result;
        }
    }
}
