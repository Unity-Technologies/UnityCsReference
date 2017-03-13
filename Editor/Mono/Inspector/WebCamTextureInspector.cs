// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    [CustomEditor(typeof(WebCamTexture))]
    internal class WebCamTextureInspector : Editor
    {
        static GUIContent[] s_PlayIcons = {null, null};
        Vector2 m_Pos;

        public override void OnInspectorGUI()
        {
            WebCamTexture t = target as WebCamTexture;
            EditorGUILayout.LabelField("Requested FPS", t.requestedFPS.ToString());
            EditorGUILayout.LabelField("Requested Width", t.requestedWidth.ToString());
            EditorGUILayout.LabelField("Requested Height", t.requestedHeight.ToString());
            EditorGUILayout.LabelField("Device Name", t.deviceName);
        }

        static void Init()
        {
            s_PlayIcons[0] = EditorGUIUtility.IconContent("preAudioPlayOff");
            s_PlayIcons[1] = EditorGUIUtility.IconContent("preAudioPlayOn");
        }

        public override bool HasPreviewGUI()
        {
            return (target != null);
        }

        public override void OnPreviewSettings()
        {
            Init();

            // Disallow playing movie previews in play mode. Better not to interfere
            // with any playback the game does.
            GUI.enabled = !Application.isPlaying;
            WebCamTexture t = target as WebCamTexture;
            bool isPlaying = PreviewGUI.CycleButton(t.isPlaying ? 1 : 0, s_PlayIcons) != 0;
            if (isPlaying != t.isPlaying)
            {
                if (isPlaying)
                {
                    t.Stop();
                    t.Play();
                }
                else
                {
                    t.Pause();
                }
            }
            GUI.enabled = true;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type == EventType.Repaint)
                background.Draw(r, false, false, false, false);

            // show texture
            WebCamTexture t = target as WebCamTexture;

            float zoomLevel = Mathf.Min(Mathf.Min(r.width / t.width, r.height / t.height), 1);
            Rect wantedRect = new Rect(r.x, r.y, t.width * zoomLevel, t.height * zoomLevel);
            PreviewGUI.BeginScrollView(r, m_Pos, wantedRect, "PreHorizontalScrollbar", "PreHorizontalScrollbarThumb");
            GUI.DrawTexture(wantedRect, t, ScaleMode.StretchToFill, false);
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

        public void OnDisable()
        {
            WebCamTexture t = target as WebCamTexture;

            //stop the camera if we started it
            if (!Application.isPlaying && t != null)
            {
                t.Stop();
            }
        }

        public override string GetInfoString()
        {
            Texture t = target as Texture;
            string info = t.width.ToString() + "x" + t.height.ToString();
            TextureFormat format = TextureUtil.GetTextureFormat(t);
            info += "  " + TextureUtil.GetTextureFormatString(format);
            return info;
        }
    }
}
