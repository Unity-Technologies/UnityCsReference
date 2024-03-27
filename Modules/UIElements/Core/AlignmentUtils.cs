// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Static class containing utility methods for aligning visual elements.
    /// </summary>
    public static class AlignmentUtils
    {
        // round(-0.52 to 0.48) returns 0 => round(0.5 +/- eps) returns 1
        internal static float RoundToPixelGrid(float v, float pixelsPerPoint, float offset = 0.02f)
        {
            return Mathf.Floor(v * pixelsPerPoint + 0.5f + offset) / pixelsPerPoint;
        }

        // ceil(-0.02 to 0.98) returns 0 => ceil(0 +/- eps) returns 0
        internal static float CeilToPixelGrid(float v, float pixelsPerPoint, float offset = -0.02f)
        {
            return Mathf.Ceil(v * pixelsPerPoint + offset) / pixelsPerPoint;
        }

        // floor(-0.02 to 0.98) returns 0 => floor(0 +/- eps) returns 0
        internal static float FloorToPixelGrid(float v, float pixelsPerPoint, float offset = 0.02f)
        {
            return Mathf.Floor(v * pixelsPerPoint + offset) / pixelsPerPoint;
        }

        /// <summary>
        /// Round the value so that it is a whole number of pixels on the target when rendered.
        /// </summary>
        /// <remarks>
        /// It will only work on visualElements inside a panel.
        ///
        /// Is is used to get dimensions repesenting whole pixels used for layout values, translations or in the generation of the visual Content.
        /// It does not consider the transform of the element and its ancestors. 
        ///
        /// This method uses the scaling from [see cref="VisualElement.scaledPixelsPerPoint"/> and uses the same rounding thresholds as the layout engine.
        /// </remarks>
        public static float RoundToPanelPixelSize(this VisualElement ve, float v)
        {
            return RoundToPixelGrid(v, ve.scaledPixelsPerPoint);
        }

        /// <summary>
        /// Return the next larger value representing a whole number of pixels on the target when rendered.
        /// </summary>
        /// <remarks>
        /// It will only work on visualElements inside a panel.
        ///
        /// Is is used to get dimensions repesenting whole pixels used for layout values, translations or in the generation of the visual Content.
        /// It does not consider the transform of the element and its ancestors. 
        ///
        /// This method uses the scaling from [see cref="VisualElement.scaledPixelsPerPoint"/> and uses the same rounding thresholds as the layout engine.
        /// </remarks>
        public static float CeilToPanelPixelSize(this VisualElement ve, float v)
        {
            return CeilToPixelGrid(v, ve.scaledPixelsPerPoint);
        }

        /// <summary>
        /// Return the next smaller value representing a whole number of pixels on the target when rendered.
        /// </summary>
        /// <remarks>
        /// It will only work on visualElements inside a panel.
        ///
        /// Is is used to get dimensions repesenting whole pixels used for layout values, translations or in the generation of the visual Content.
        /// It does not consider the transform of the element and its ancestors. 
        ///
        /// This method uses the scaling from [see cref="VisualElement.scaledPixelsPerPoint"/> and uses the same rounding thresholds as the layout engine.
        /// </remarks>
        public static float FloorToPanelPixelSize(this VisualElement ve, float v)
        {
            return FloorToPixelGrid(v, ve.scaledPixelsPerPoint);
        }

    }
}
