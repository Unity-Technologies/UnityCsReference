// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.IntegerTime;
using Unity.Timeline.Foundation.Time;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Timeline.Foundation.Widgets
{
    interface ICanvas
    {
        bool snapToFrame { get; }
        CanvasTransform canvasTransform { get; }
        TimeConverter timeConverter { get; }
        IOverlayManager overlayManager { get; }
        Rect worldBound { get; }

        Rect WorldToLocal(Rect worldRect);
        Rect LocalToWorld(Rect localRect);
        Vector2 WorldToLocal(Vector2 worldPos);
        Vector2 LocalToWorld(Vector2 localPos);
    }

    static class CanvasExtensions
    {
        public static DiscreteTime WorldPixelToTime(this ICanvas canvas, float worldPixel, bool ignoreSnapToFrame = false)
        {
            float localPixel = canvas.WorldToLocal(new Vector2(worldPixel, 0f)).x;
            DiscreteTime time = canvas.canvasTransform.PixelToTime(localPixel);
            return canvas.snapToFrame && !ignoreSnapToFrame ? canvas.timeConverter.RoundToFrame(time) : time;
        }

        public static float TimeToWorldPixel(this ICanvas canvas, DiscreteTime time)
        {
            float localPixel = canvas.canvasTransform.TimeToPixel(time);
            return canvas.LocalToWorld(new Vector2(localPixel, 0f)).x;
        }

        public static float DurationToPixelWidth(this ICanvas canvas, DiscreteTime duration)
        {
            return canvas.canvasTransform.DurationToPixelWidth(duration);
        }

        public static DiscreteTime PixelWidthToDuration(this ICanvas canvas, float width)
        {
            DiscreteTime duration = canvas.canvasTransform.PixelWidthToDuration(width);
            return canvas.snapToFrame ? canvas.timeConverter.ToDiscreteTime(canvas.timeConverter.ToFrames(duration)) : duration;
        }

        public static Rect ConvertCoordinatesFrom(this ICanvas canvas, VisualElement src, Rect rect)
        {
            return canvas.WorldToLocal(src.LocalToWorld(rect));
        }

        public static Vector2 ConvertCoordinatesFrom(this ICanvas canvas, VisualElement src, Vector2 position)
        {
            return canvas.WorldToLocal(src.LocalToWorld(position));
        }
    }
}
