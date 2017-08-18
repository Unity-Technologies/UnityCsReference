// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    // Handles a "ping" in project/hierarchy windows. Zoom, wait and fadeoff.
    // Init by setting:
    // - m_PingStyle: Background style
    // - m_ContentDraw: Content render callback function  (is rendered on top of background ping style)
    // - m_ContentRect: Size and position of content that is rendered in m_ContentDraw (is used for calculating the ping background and is passed to m_ContentDraw)
    class PingData
    {
        public float m_TimeStart = -1f;

        public float m_ZoomTime = 0.2f;
        public float m_WaitTime = 2.5f;
        public float m_FadeOutTime = 1.5f;
        public float m_PeakScale = 1.75f;

        public System.Action<Rect> m_ContentDraw;
        public Rect m_ContentRect;                  // Rect passed to m_ContentDraw

        // How wide is the view where we are pinging. Needed for the pivot point trick where pinged content is only partly visible.
        public float m_AvailableWidth = 100f;
        public GUIStyle m_PingStyle;

        public bool isPinging
        {
            get { return m_TimeStart > -1f; }
        }

        public void HandlePing()
        {
            if (isPinging)
            {
                float totalTime = m_ZoomTime + m_WaitTime + m_FadeOutTime;
                float t = (Time.realtimeSinceStartup - m_TimeStart);

                if (t > 0.0f && t < totalTime)
                {
                    Color c = GUI.color;
                    Matrix4x4 m = GUI.matrix;
                    if (t < m_ZoomTime)
                    {
                        float peakTime = m_ZoomTime / 2f;
                        float scale = (m_PeakScale - 1f) * (((m_ZoomTime - Mathf.Abs(peakTime - t)) / peakTime) - 1f) + 1f;
                        Matrix4x4 mat = GUI.matrix;

                        // If the content is only partly visible, the zoom pivot point is moved to right border. This avoids the nasty artefacts.
                        Vector2 pivotPoint = m_ContentRect.xMax < m_AvailableWidth ? m_ContentRect.center : new Vector2(m_AvailableWidth, m_ContentRect.center.y);
                        Vector2 point = GUIClip.Unclip(pivotPoint);
                        Matrix4x4 newMat = Matrix4x4.TRS(point, Quaternion.identity, new Vector3(scale, scale, 1)) * Matrix4x4.TRS(-point, Quaternion.identity, Vector3.one);
                        GUI.matrix = newMat * mat;
                    }
                    else if (t > m_ZoomTime + m_WaitTime)
                    {
                        float alpha = (totalTime - t) / m_FadeOutTime;
                        GUI.color = new Color(c.r, c.g, c.b, c.a * alpha);
                    }

                    if (m_ContentDraw != null && Event.current.type == EventType.Repaint)
                    {
                        Rect backRect = m_ContentRect;
                        backRect.x -= m_PingStyle.padding.left;
                        backRect.y -= m_PingStyle.padding.top;
                        m_PingStyle.Draw(backRect, GUIContent.none, false, false, false, false);
                        m_ContentDraw(m_ContentRect);
                    }

                    GUI.matrix = m;
                    GUI.color = c;
                }
                else
                {
                    m_TimeStart = -1f;
                }
            }
        }
    }
}
