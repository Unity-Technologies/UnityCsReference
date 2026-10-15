// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    ///<summary>Utility class containing helper methods for working with  <see cref="RectTransform" />.</summary>
    [NativeHeader("Runtime/Camera/Camera.h"),
     NativeHeader("Modules/UI/Canvas.h"),
     NativeHeader("Modules/UI/RectTransformUtil.h"),
     NativeHeader("Runtime/Transform/RectTransform.h"),
     StaticAccessor("UI", StaticAccessorType.DoubleColon)]
    public static partial class RectTransformUtility
    {
        ///<summary>Convert a given point in screen space into a pixel correct point.</summary>
        ///<param name="point">Point to adjust, in the local space of the element.</param>
        ///<param name="elementTransform">Transform of the element that the point belongs to.</param>
        ///<param name="canvas">Canvas that contains the element.</param>
        ///<returns>Pixel adjusted point.</returns>
        public static extern Vector2 PixelAdjustPoint(Vector2 point, Transform elementTransform, Canvas canvas);
        ///<summary>Given a rect transform, return the corner points in pixel accurate coordinates.</summary>
        ///<param name="rectTransform">RectTransform whose rect to adjust.</param>
        ///<param name="canvas">Canvas that contains the RectTransform.</param>
        ///<returns>Pixel adjusted rect.</returns>
        public static extern Rect PixelAdjustRect(RectTransform rectTransform, Canvas canvas);

        private static extern bool PointInRectangle(Vector2 screenPoint, RectTransform rect, Camera cam, Vector4 offset);
    }
}
