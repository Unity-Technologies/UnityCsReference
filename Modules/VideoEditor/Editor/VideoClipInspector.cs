// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Video;

namespace UnityEditor
{
    [CustomEditor(typeof(VideoClip))]
    [CanEditMultipleObjects]
    internal class VideoClipInspector : Editor
    {
#pragma warning disable 649
        static readonly GUID kEmptyGUID;

        private VideoClip m_PlayingClip;
        private Texture m_Texture;
        private GUID m_PreviewID;
        Vector2 m_Position = Vector2.zero;
        private bool m_UseAssetPreview = true;

        override public void OnInspectorGUI()
        {
            // Override with inspector that doesn't show anything
        }

        static void Init()
        {
        }

        public void OnDisable()
        {
        }

        public void OnEnable()
        {
        }

        public void OnDestroy()
        {
            StopPreview();
        }

        public override bool HasPreviewGUI()
        {
            return (targets != null);
        }

        private void PlayPreview()
        {
            m_PreviewID = VideoUtil.StartPreview(m_PlayingClip);
            VideoUtil.PlayPreview(m_PreviewID, true);
        }

        private void StopPreview()
        {
            m_UseAssetPreview = true;
            if (!m_PreviewID.Empty())
                VideoUtil.StopPreview(m_PreviewID);
            m_PlayingClip = null;
            m_PreviewID = kEmptyGUID;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            VideoClip clip = target as VideoClip;

            Event evt = Event.current;
            if (evt.type != EventType.Repaint &&
                evt.type != EventType.Layout &&
                evt.type != EventType.Used)
            {
                switch (evt.type)
                {
                    case EventType.MouseDown:
                    {
                        if (r.Contains(evt.mousePosition))
                        {
                            if (m_PlayingClip != null)
                            {
                                if (m_PreviewID.Empty() || !VideoUtil.IsPreviewPlaying(m_PreviewID))
                                {
                                    PlayPreview();
                                }
                                else
                                {
                                    StopPreview();
                                }
                            }
                            evt.Use();
                        }
                    }
                    break;
                }
                return;
            }

            if (clip != m_PlayingClip)
            {
                StopPreview();
                m_PlayingClip = clip;
            }

            Texture image = null;

            if (!m_PreviewID.Empty() && VideoUtil.IsPreviewPlaying(m_PreviewID))
            {
                image = VideoUtil.GetPreviewTexture(m_PreviewID);
                if (image != null && m_UseAssetPreview)
                    m_UseAssetPreview = false;
            }
            else
                image = GetAssetPreviewTexture();

            if (image != null && image.width != 0 && image.height != 0)
                m_Texture = image;

            if (!m_Texture)
                return;

            if (Event.current.type == EventType.Repaint)
                background.Draw(r, false, false, false, false);

            float previewWidth = m_Texture.width;
            float previewHeight = m_Texture.height;

            if (m_PlayingClip.pixelAspectRatioDenominator > 0)
                previewWidth *= (float)m_PlayingClip.pixelAspectRatioNumerator /
                    (float)m_PlayingClip.pixelAspectRatioDenominator;

            float zoomLevel = 1.0f;

            if ((r.width / previewWidth * previewHeight) > r.height)
                zoomLevel = r.height / previewHeight;
            else
                zoomLevel = r.width / previewWidth;

            zoomLevel = Mathf.Clamp01(zoomLevel);

            Rect wantedRect = !m_UseAssetPreview ? new Rect(r.x, r.y, previewWidth * zoomLevel, m_Texture.height * zoomLevel) : r;

            PreviewGUI.BeginScrollView(
                r, m_Position, wantedRect, "PreHorizontalScrollbar", "PreHorizontalScrollbarThumb");

            if (!m_UseAssetPreview)
                EditorGUI.DrawTextureTransparent(wantedRect, m_Texture, ScaleMode.StretchToFill);
            else
                GUI.DrawTexture(wantedRect, m_Texture, ScaleMode.ScaleToFit);

            m_Position = PreviewGUI.EndScrollView();

            if (!m_PreviewID.Empty() &&
                VideoUtil.IsPreviewPlaying(m_PreviewID) &&
                Event.current.type == EventType.Repaint)
                GUIView.current.Repaint();
        }

        Texture GetAssetPreviewTexture()
        {
            Texture tex = null;
            bool isLoadingAssetPreview = AssetPreview.IsLoadingAssetPreview(target.GetInstanceID());
            tex = AssetPreview.GetAssetPreview(target);
            if (!tex)
            {
                // We have a static preview it just hasn't been loaded yet. Repaint until we have it loaded.
                if (isLoadingAssetPreview)
                    GUIView.current.Repaint();
                tex = AssetPreview.GetMiniThumbnail(target);
            }
            return tex;
        }

        internal override void OnHeaderIconGUI(Rect iconRect)
        {
            GUI.DrawTexture(iconRect, GetAssetPreviewTexture(), ScaleMode.StretchToFill);
        }

        public override string GetInfoString()
        {
            VideoClip clip = target as VideoClip;
            var frameCount = clip.frameCount;
            var frameRate = clip.frameRate;

            var duration = frameRate > 0
                ? TimeSpan.FromSeconds(frameCount / frameRate).ToString()
                : new TimeSpan(0).ToString();

            // TimeSpan uses 7 digits for fractional seconds.  Limit this to 3 digits.
            if (duration.IndexOf('.') != -1)
                duration = duration.Substring(0, duration.Length - 4);

            string s = duration;
            s += ", " + frameCount.ToString() + " frames";
            s += ", " + frameRate.ToString("F2") + " FPS";
            s += ", " + clip.width.ToString() + "x" + clip.height.ToString();
            return s;
        }
    }
}
