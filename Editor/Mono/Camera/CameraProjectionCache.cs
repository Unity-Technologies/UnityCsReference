// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    public struct CameraProjectionCache
    {
        Matrix4x4 m_WorldToClip;
        Rect m_Viewport;
        float m_ScreenHeight;

        public CameraProjectionCache(Camera camera)
        {
            m_WorldToClip = camera.projectionMatrix * camera.worldToCameraMatrix;
            m_Viewport = camera.pixelRect;
            m_ScreenHeight = GUIClip.visibleRect.height * EditorGUIUtility.pixelsPerPoint;
        }

        public Vector2 WorldToScreenPoint(Vector3 worldPoint)
        {
            Vector3 clip = m_WorldToClip.MultiplyPoint(worldPoint);

            return new Vector2(
                m_Viewport.x + (1.0f + clip.x) * m_Viewport.width * 0.5f,
                m_Viewport.y + (1.0f + clip.y) * m_Viewport.height * 0.5f);
        }

        public Vector2 WorldToGUIPoint(Vector3 worldPoint)
        {
            return ScreenToGUIPoint(WorldToScreenPoint(worldPoint));
        }

        public Vector2 GUIToScreenPoint(Vector2 guiPoint)
        {
            var pixels = EditorGUIUtility.PointsToPixels(guiPoint);
            pixels.y = m_ScreenHeight - pixels.y;
            return pixels;
        }

        public Vector2 ScreenToGUIPoint(Vector2 screenPoint)
        {
            screenPoint.y = m_ScreenHeight - screenPoint.y;
            return GUIClip.Clip(EditorGUIUtility.PixelsToPoints(screenPoint));
        }
    }
}
