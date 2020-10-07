// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    [NativeHeader("Runtime/Camera/Camera.h"),
     NativeHeader("Modules/UI/Canvas.h"),
     NativeHeader("Modules/UI/RectTransformUtil.h"),
     NativeHeader("Runtime/Transform/RectTransform.h"),
     StaticAccessor("UI", StaticAccessorType.DoubleColon)]
    partial class RectTransformUtility
    {
        public static extern Vector2 PixelAdjustPoint(Vector2 point, Transform elementTransform, Canvas canvas);
        public static extern Rect PixelAdjustRect(RectTransform rectTransform, Canvas canvas);

        private static extern bool PointInRectangle(Vector2 screenPoint, RectTransform rect, Camera cam, Vector4 offset);
    }
}
