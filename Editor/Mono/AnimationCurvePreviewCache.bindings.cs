// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEditor;

namespace UnityEditorInternal
{
    [NativeHeader("Editor/Src/AnimationCurvePreviewCache.bindings.h")]
    [NativeHeader("Editor/Src/Utility/SerializedProperty.h")]
    [NativeHeader("Runtime/Graphics/Texture2D.h")]
    [StaticAccessor("AnimationCurvePreviewCacheBindings", StaticAccessorType.DoubleColon)]
    internal class AnimationCurvePreviewCache
    {
        // Regions as SerializedProperty
        public static Texture2D GetPreview(int previewWidth, int previewHeight, SerializedProperty property, SerializedProperty property2, Color color, Rect curveRanges)
        {
            return GetPreview(previewWidth, previewHeight, property, property2, color, Color.clear, Color.clear);
        }

        public static Texture2D GetPreview(int previewWidth, int previewHeight, SerializedProperty property, SerializedProperty property2, Color color, Color topFillColor, Color bottomFillColor, Rect curveRanges)
        {
            if (property2 == null)
                return GetPropertyPreviewFilled(previewWidth, previewHeight, true, curveRanges, property, color, topFillColor, bottomFillColor);
            else
                return GetPropertyPreviewRegionFilled(previewWidth, previewHeight, true, curveRanges, property, property2, color, topFillColor, bottomFillColor);
        }

        public static Texture2D GetPreview(int previewWidth, int previewHeight, SerializedProperty property, SerializedProperty property2, Color color)
        {
            return GetPreview(previewWidth, previewHeight, property, property2, color, Color.clear, Color.clear);
        }

        public static Texture2D GetPreview(int previewWidth, int previewHeight, SerializedProperty property, SerializedProperty property2, Color color, Color topFillColor, Color bottomFillColor)
        {
            if (property2 == null)
                return GetPropertyPreviewFilled(previewWidth, previewHeight, false, new Rect(), property, color, topFillColor, bottomFillColor);
            else
                return GetPropertyPreviewRegionFilled(previewWidth, previewHeight, false, new Rect(), property, property2, color, topFillColor, bottomFillColor);
        }

        // Regions as AnimationCurves
        public static Texture2D GetPreview(int previewWidth, int previewHeight, AnimationCurve curve, AnimationCurve curve2, Color color, Color topFillColor, Color bottomFillColor, Rect curveRanges)
        {
            return GetCurvePreviewRegionFilled(previewWidth, previewHeight, true, curveRanges, curve, curve2, color, topFillColor, bottomFillColor);
        }

        public static Texture2D GetPreview(int previewWidth, int previewHeight, AnimationCurve curve, AnimationCurve curve2, Color color, Rect curveRanges)
        {
            return GetPreview(previewWidth, previewHeight, curve, curve2, color, Color.clear, Color.clear, curveRanges);
        }

        public static Texture2D GetPreview(int previewWidth, int previewHeight, AnimationCurve curve, AnimationCurve curve2, Color color, Color topFillColor, Color bottomFillColor)
        {
            return GetCurvePreviewRegionFilled(previewWidth, previewHeight, false, new Rect(), curve, curve2, color, topFillColor, bottomFillColor);
        }

        public static Texture2D GetPreview(int previewWidth, int previewHeight, AnimationCurve curve, AnimationCurve curve2, Color color)
        {
            return GetPreview(previewWidth, previewHeight, curve, curve2, color, Color.clear, Color.clear, new Rect());
        }

        public static Texture2D GetPreview(int previewWidth, int previewHeight, SerializedProperty property, Color color, Color topFillColor, Color bottomFillColor, Rect curveRanges)
        {
            return GetPropertyPreviewFilled(previewWidth, previewHeight, true, curveRanges, property, color, topFillColor, bottomFillColor);
        }

        public static Texture2D GetPreview(int previewWidth, int previewHeight, SerializedProperty property, Color color, Rect curveRanges)
        {
            return GetPreview(previewWidth, previewHeight, property, color, Color.clear, Color.clear, curveRanges);
        }

        public static Texture2D GetPreview(int previewWidth, int previewHeight, SerializedProperty property, Color color, Color topFillColor, Color bottomFillColor)
        {
            return GetPropertyPreviewFilled(previewWidth, previewHeight, false, new Rect(), property, color, topFillColor, bottomFillColor);
        }

        public static Texture2D GetPreview(int previewWidth, int previewHeight, SerializedProperty property, Color color)
        {
            return GetPreview(previewWidth, previewHeight, property, color, Color.clear, Color.clear, new Rect());
        }

        public static Texture2D GetPreview(int previewWidth, int previewHeight, AnimationCurve curve, Color color, Color topFillColor, Color bottomFillColor, Rect curveRanges)
        {
            return GetCurvePreviewFilled(previewWidth, previewHeight, true, curveRanges, curve, color, topFillColor, bottomFillColor);
        }

        public static Texture2D GetPreview(int previewWidth, int previewHeight, AnimationCurve curve, Color color, Rect curveRanges)
        {
            return GetPreview(previewWidth, previewHeight, curve, color, Color.clear, Color.clear, curveRanges);
        }

        public static Texture2D GetPreview(int previewWidth, int previewHeight, AnimationCurve curve, Color color, Color topFillColor, Color bottomFillColor)
        {
            return GetCurvePreviewFilled(previewWidth, previewHeight, false, new Rect(), curve, color, topFillColor, bottomFillColor);
        }

        public static Texture2D GetPreview(int previewWidth, int previewHeight, AnimationCurve curve, Color color)
        {
            return GetPreview(previewWidth, previewHeight, curve, color, Color.clear, Color.clear, new Rect());
        }

        public static extern Texture2D GenerateCurvePreview(int previewWidth, int previewHeight, Rect curveRanges, AnimationCurve curve, Color color, Texture2D existingTexture);

        internal extern static void ClearCache();

        private extern static Texture2D GetPropertyPreview(int previewWidth, int previewHeight, bool useCurveRanges, Rect curveRanges, SerializedProperty property, Color color);
        private extern static Texture2D GetPropertyPreviewFilled(int previewWidth, int previewHeight, bool useCurveRanges, Rect curveRanges, SerializedProperty property, Color color, Color topFillColor, Color bottomFillColor);
        private extern static Texture2D GetPropertyPreviewRegion(int previewWidth, int previewHeight, bool useCurveRanges, Rect curveRanges, SerializedProperty property, SerializedProperty property2, Color color);
        private extern static Texture2D GetPropertyPreviewRegionFilled(int previewWidth, int previewHeight, bool useCurveRanges, Rect curveRanges, SerializedProperty property, SerializedProperty property2, Color color, Color topFillColor, Color bottomFillColor);
        private extern static Texture2D GetCurvePreview(int previewWidth, int previewHeight, bool useCurveRanges, Rect curveRanges, AnimationCurve curve, Color color);
        private extern static Texture2D GetCurvePreviewFilled(int previewWidth, int previewHeight, bool useCurveRanges, Rect curveRanges, AnimationCurve curve, Color color, Color topFillColor, Color bottomFillColor);
        private extern static Texture2D GetCurvePreviewRegion(int previewWidth, int previewHeight, bool useCurveRanges, Rect curveRanges, AnimationCurve curve, AnimationCurve curve2, Color color);
        private extern static Texture2D GetCurvePreviewRegionFilled(int previewWidth, int previewHeight, bool useCurveRanges, Rect curveRanges, AnimationCurve curve, AnimationCurve curve2, Color color, Color topFillColor, Color bottomFillColor);
    }
}
