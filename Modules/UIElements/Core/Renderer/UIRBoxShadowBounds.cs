// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements.UIR
{
    // An outer box-shadow is drawn outside of the element's layout, so it isn't part of the element's bounding box.
    // The bounding box must not include it, since it affects layout-driven features like scrolling.
    static class BoxShadowBounds
    {
        // Keep in sync with kBoxShadowExtentInSigmas and kBoxShadowMinSigma in UIRTessellationBoxShadow.cpp
        const float k_ExtentInSigmas = 3.0f;
        const float k_MinSigma = 0.4f;

        // Returns bounds, in the element's local space, grown to contain the element's outer box-shadow.
        public static Rect EncapsulateOuterShadow(VisualElement ve, Rect bounds)
        {
            var boxShadow = ve.computedStyle.boxShadow;
            if (boxShadow.inset || boxShadow.color.a <= UIRUtility.k_Epsilon)
                return bounds;

            var size = ve.layoutSize;
            float spread = boxShadow.spreadRadius;
            var shape = new Rect(boxShadow.offset.x - spread, boxShadow.offset.y - spread, size.x + 2 * spread, size.y + 2 * spread);
            if (size.x <= UIRUtility.k_Epsilon || size.y <= UIRUtility.k_Epsilon || shape.width <= UIRUtility.k_Epsilon || shape.height <= UIRUtility.k_Epsilon)
                return bounds;

            float extent = k_ExtentInSigmas * Mathf.Max(k_MinSigma, boxShadow.blurRadius * 0.5f);
            var shadowBounds = new Rect(shape.x - extent, shape.y - extent, shape.width + 2 * extent, shape.height + 2 * extent);
            return UIRUtility.Encapsulate(bounds, shadowBounds);
        }
    }
}
