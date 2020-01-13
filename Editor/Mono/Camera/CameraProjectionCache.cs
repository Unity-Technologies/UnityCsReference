// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    struct CameraProjectionCache
    {
        Matrix4x4 worldToClip;
        Rect viewport;
        float screen;

        public CameraProjectionCache(Camera camera, float screenHeight)
        {
            worldToClip = camera.projectionMatrix * camera.worldToCameraMatrix;
            viewport = camera.pixelRect;
            screen = screenHeight;
        }

        public Vector2 WorldToScreenPoint(Vector3 point)
        {
            Vector3 clip = worldToClip.MultiplyPoint(point);

            return new Vector2(
                viewport.x + (1.0f + clip.x) * viewport.width * 0.5f,
                viewport.y + (1.0f + clip.y) * viewport.height * 0.5f);
        }

        public Vector2 WorldToGUIPoint(Vector3 point)
        {
            return ScreenToGUIPoint(WorldToScreenPoint(point));
        }

        public Vector2 GUIToScreenPoint(Vector2 point)
        {
            var pixels = EditorGUIUtility.PointsToPixels(point);
            pixels.y = screen - pixels.y;
            return pixels;
        }

        public Vector2 ScreenToGUIPoint(Vector2 point)
        {
            point.y = screen - point.y;
            return GUIClip.Clip(EditorGUIUtility.PixelsToPoints(point));
        }
    }
}
