// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UnityEngine.UIElements.Experimental;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    internal interface IStylePropertyAnimations
    {
        bool Start(StylePropertyId id, float from, float to, int durationMs, int delayMs, Func<float, float> easingCurve);
        bool Start(StylePropertyId id, int from, int to, int durationMs, int delayMs, Func<float, float> easingCurve);
        bool Start(StylePropertyId id, Length from, Length to, int durationMs, int delayMs, Func<float, float> easingCurve);
        bool Start(StylePropertyId id, Color from, Color to, int durationMs, int delayMs, Func<float, float> easingCurve);
        bool StartEnum(StylePropertyId id, int from, int to, int durationMs, int delayMs, Func<float, float> easingCurve);
        bool Start(StylePropertyId id, Background from, Background to, int durationMs, int delayMs, Func<float, float> easingCurve);
        bool Start(StylePropertyId id, FontDefinition from, FontDefinition to, int durationMs, int delayMs, Func<float, float> easingCurve);
        bool Start(StylePropertyId id, Font from, Font to, int durationMs, int delayMs, Func<float, float> easingCurve);
        bool Start(StylePropertyId id, TextShadow from, TextShadow to, int durationMs, int delayMs, Func<float, float> easingCurve);
        bool Start(StylePropertyId id, Scale from, Scale to, int durationMs, int delayMs, Func<float, float> easingCurve);
        bool Start(StylePropertyId id, Translate from, Translate to, int durationMs, int delayMs, Func<float, float> easingCurve);
        bool Start(StylePropertyId id, Rotate from, Rotate to, int durationMs, int delayMs, Func<float, float> easingCurve);
        bool Start(StylePropertyId id, TransformOrigin from, TransformOrigin to, int durationMs, int delayMs, Func<float, float> easingCurve);
        bool Start(StylePropertyId id, BackgroundPosition from, BackgroundPosition to, int durationMs, int delayMs, Func<float, float> easingCurve);
        bool Start(StylePropertyId id, BackgroundRepeat from, BackgroundRepeat to, int durationMs, int delayMs, Func<float, float> easingCurve);
        bool Start(StylePropertyId id, BackgroundSize from, BackgroundSize to, int durationMs, int delayMs, Func<float, float> easingCurve);
        bool Start(StylePropertyId id, List<FilterFunction> from, List<FilterFunction> to, int durationMs, int delayMs, Func<float, float> easingCurve);

        bool HasRunningAnimation(StylePropertyId id);
        void UpdateAnimation(StylePropertyId id);
        void GetAllAnimations(List<StylePropertyId> outPropertyIds);
        void CancelAnimation(StylePropertyId id);
        void CancelAllAnimations();

        int runningAnimationCount { get; set; }
        int completedAnimationCount { get; set; }
    }

    public partial class VisualElement : IStylePropertyAnimations
    {
        internal bool hasRunningAnimations => styleAnimation.runningAnimationCount > 0;
        internal bool hasCompletedAnimations => styleAnimation.completedAnimationCount > 0;

        int IStylePropertyAnimations.runningAnimationCount { get; set; }
        int IStylePropertyAnimations.completedAnimationCount { get; set; }

        private IStylePropertyAnimationSystem GetStylePropertyAnimationSystem()
        {
            return elementPanel?.styleAnimationSystem;
        }

        internal IStylePropertyAnimations styleAnimation => this;

        bool IStylePropertyAnimations.Start(StylePropertyId id, float from, float to, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return GetStylePropertyAnimationSystem().StartTransition(this, id, from, to, durationMs, delayMs, easingCurve);
        }

        bool IStylePropertyAnimations.Start(StylePropertyId id, int from, int to, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return GetStylePropertyAnimationSystem().StartTransition(this, id, from, to, durationMs, delayMs, easingCurve);
        }

        bool IStylePropertyAnimations.Start(StylePropertyId id, Length from, Length to, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return GetStylePropertyAnimationSystem().StartTransition(this, id, from, to, durationMs, delayMs, easingCurve);
        }

        bool IStylePropertyAnimations.Start(StylePropertyId id, Color from, Color to, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return GetStylePropertyAnimationSystem().StartTransition(this, id, from, to, durationMs, delayMs, easingCurve);
        }

        bool IStylePropertyAnimations.StartEnum(StylePropertyId id, int from, int to, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return GetStylePropertyAnimationSystem().StartTransition(this, id, from, to, durationMs, delayMs, easingCurve);
        }

        bool IStylePropertyAnimations.Start(StylePropertyId id, Background from, Background to, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return GetStylePropertyAnimationSystem().StartTransition(this, id, from, to, durationMs, delayMs, easingCurve);
        }

        bool IStylePropertyAnimations.Start(StylePropertyId id, FontDefinition from, FontDefinition to, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return GetStylePropertyAnimationSystem().StartTransition(this, id, from, to, durationMs, delayMs, easingCurve);
        }

        bool IStylePropertyAnimations.Start(StylePropertyId id, Font from, Font to, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return GetStylePropertyAnimationSystem().StartTransition(this, id, from, to, durationMs, delayMs, easingCurve);
        }

        bool IStylePropertyAnimations.Start(StylePropertyId id, TextShadow from, TextShadow to, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return GetStylePropertyAnimationSystem().StartTransition(this, id, from, to, durationMs, delayMs, easingCurve);
        }

        bool IStylePropertyAnimations.Start(StylePropertyId id, Scale from, Scale to, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return GetStylePropertyAnimationSystem().StartTransition(this, id, from, to, durationMs, delayMs, easingCurve);
        }

        bool IStylePropertyAnimations.Start(StylePropertyId id, Translate from, Translate to, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return GetStylePropertyAnimationSystem().StartTransition(this, id, from, to, durationMs, delayMs, easingCurve);
        }

        bool IStylePropertyAnimations.Start(StylePropertyId id, Rotate from, Rotate to, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return GetStylePropertyAnimationSystem().StartTransition(this, id, from, to, durationMs, delayMs, easingCurve);
        }

        bool IStylePropertyAnimations.Start(StylePropertyId id, TransformOrigin from, TransformOrigin to, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return GetStylePropertyAnimationSystem().StartTransition(this, id, from, to, durationMs, delayMs, easingCurve);
        }

        bool IStylePropertyAnimations.Start(StylePropertyId id, BackgroundPosition from, BackgroundPosition to, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return GetStylePropertyAnimationSystem().StartTransition(this, id, from, to, durationMs, delayMs, easingCurve);
        }

        bool IStylePropertyAnimations.Start(StylePropertyId id, BackgroundRepeat from, BackgroundRepeat to, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return GetStylePropertyAnimationSystem().StartTransition(this, id, from, to, durationMs, delayMs, easingCurve);
        }

        bool IStylePropertyAnimations.Start(StylePropertyId id, BackgroundSize from, BackgroundSize to, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return GetStylePropertyAnimationSystem().StartTransition(this, id, from, to, durationMs, delayMs, easingCurve);
        }

        bool IStylePropertyAnimations.Start(StylePropertyId id, List<FilterFunction> from, List<FilterFunction> to, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return GetStylePropertyAnimationSystem().StartTransition(this, id, from, to, durationMs, delayMs, easingCurve);
        }

        void IStylePropertyAnimations.CancelAnimation(StylePropertyId id)
        {
            GetStylePropertyAnimationSystem()?.CancelAnimation(this, id);
        }

        void IStylePropertyAnimations.CancelAllAnimations()
        {
            if (hasRunningAnimations || hasCompletedAnimations)
                GetStylePropertyAnimationSystem()?.CancelAllAnimations(this);
        }

        bool IStylePropertyAnimations.HasRunningAnimation(StylePropertyId id)
        {
            return hasRunningAnimations && GetStylePropertyAnimationSystem().HasRunningAnimation(this, id);
        }

        void IStylePropertyAnimations.UpdateAnimation(StylePropertyId id)
        {
            GetStylePropertyAnimationSystem().UpdateAnimation(this, id);
        }

        void IStylePropertyAnimations.GetAllAnimations(List<StylePropertyId> outPropertyIds)
        {
            if (hasRunningAnimations || hasCompletedAnimations)
                GetStylePropertyAnimationSystem().GetAllAnimations(this, outPropertyIds);
        }

        internal bool TryConvertLengthUnits(StylePropertyId id, ref Length from, ref Length to, int subPropertyIndex = 0)
        {
            if (from.IsAuto() || from.IsNone() || to.IsAuto() || to.IsNone())
                return false;

            if (float.IsNaN(from.value) || float.IsNaN(to.value))
                return false;

            if (from.unit == to.unit)
                return true;

            // At the moment, LengthUnit only accepts Pixel and Percent. A slightly more complicated conversion might
            // be needed when we have multiple units to account for, e.g. from -> px -> to.
            if (to.unit == LengthUnit.Pixel)
            {
                if (Mathf.Approximately(from.value, 0))
                {
                    from = new Length(0, LengthUnit.Pixel);
                    return true;
                }

                var parentSize = GetParentSizeForLengthConversion(id, subPropertyIndex);
                if (parentSize == null || !(parentSize.Value >= 0)) // Reject NaN and negative values
                    return false;
                from = new Length(from.value * parentSize.Value / 100, LengthUnit.Pixel);
            }
            else
            {
                // When more units are supported, this Assert will make sure we remember to implement them here.
                Assert.AreEqual(LengthUnit.Percent, to.unit);

                var parentSize = GetParentSizeForLengthConversion(id, subPropertyIndex);
                if (parentSize == null || !(parentSize.Value > 0)) // Reject NaN, zero, and negative values
                    return false;
                from = new Length(from.value * 100 / parentSize.Value, LengthUnit.Percent);
            }

            return true;
        }

        // Changes the from TransformOrigin so that it apply the same result as before, but with the unit of the "to" value
        // return false if not possible
        internal bool TryConvertTransformOriginUnits(ref TransformOrigin from, ref TransformOrigin to)
        {
            Length fromX = from.x, fromY = from.y, toX = to.x, toY = to.y;
            if (!TryConvertLengthUnits(StylePropertyId.TransformOrigin, ref fromX, ref toX, 0))
                return false;
            if (!TryConvertLengthUnits(StylePropertyId.TransformOrigin, ref fromY, ref toY, 1))
                return false;

            from.x = fromX;
            from.y = fromY;
            return true;
        }

        // Changes the from Translate so that it apply the same result as before, but with the unit of the "to"
        // return false if not possible
        internal bool TryConvertTranslateUnits(ref Translate from, ref Translate to)
        {
            Length fromX = from.x, fromY = from.y, toX = to.x, toY = to.y;
            if (!TryConvertLengthUnits(StylePropertyId.Translate, ref fromX, ref toX, 0))
                return false;
            if (!TryConvertLengthUnits(StylePropertyId.Translate, ref fromY, ref toY, 1))
                return false;

            from.x = fromX;
            from.y = fromY;
            return true;
        }

        // Changes the from BackgroundPosition so that it apply the same result as before, but with the unit of the "to"
        // return false if not possible
        internal bool TryConvertBackgroundPositionUnits(ref BackgroundPosition from, ref BackgroundPosition to)
        {
            Length fromX = from.offset, toX = to.offset;
            if (!TryConvertLengthUnits(StylePropertyId.BackgroundPosition, ref fromX, ref toX, 0))
                return false;

            from.offset = fromX;
            return true;
        }

        // Changes the from BackgroundSize so that it apply the same result as before, but with the unit of the "to" value
        // return false if not possible
        internal bool TryConvertBackgroundSizeUnits(ref BackgroundSize from, ref BackgroundSize to)
        {
            Length fromX = from.x, fromY = from.y, toX = to.x, toY = to.y;
            if (!TryConvertLengthUnits(StylePropertyId.BackgroundSize, ref fromX, ref toX, 0))
                return false;
            if (!TryConvertLengthUnits(StylePropertyId.BackgroundSize, ref fromY, ref toY, 1))
                return false;

            from.x = fromX;
            from.y = fromY;
            return true;
        }

        private float? GetParentSizeForLengthConversion(StylePropertyId id, int subPropertyIndex = 0)
        {
            switch (id)
            {
                case StylePropertyId.Bottom:
                case StylePropertyId.Top:
                case StylePropertyId.Height:
                case StylePropertyId.MaxHeight:
                case StylePropertyId.MinHeight:
                    return hierarchy.parent?.resolvedStyle.height;

                case StylePropertyId.Left:
                case StylePropertyId.Right:
                case StylePropertyId.Width:
                case StylePropertyId.MaxWidth:
                case StylePropertyId.MinWidth:

                case StylePropertyId.MarginBottom:
                case StylePropertyId.MarginTop:
                case StylePropertyId.MarginLeft:
                case StylePropertyId.MarginRight:

                case StylePropertyId.PaddingBottom: //The size of the padding as a percentage, relative to the width of the containing block. Must be nonnegative.
                case StylePropertyId.PaddingTop:
                case StylePropertyId.PaddingLeft:
                case StylePropertyId.PaddingRight:
                    return hierarchy.parent?.resolvedStyle.width;

                case StylePropertyId.FlexBasis:
                    if (hierarchy.parent == null) return null;
                    switch (hierarchy.parent.resolvedStyle.flexDirection)
                    {
                        case FlexDirection.Column: case FlexDirection.ColumnReverse:
                            return hierarchy.parent.resolvedStyle.height;
                        default:
                            return hierarchy.parent.resolvedStyle.width;
                    }

                case StylePropertyId.BorderBottomLeftRadius: //Refer to the corresponding dimension of the border box
                case StylePropertyId.BorderBottomRightRadius:
                case StylePropertyId.BorderTopLeftRadius:
                case StylePropertyId.BorderTopRightRadius:
                    // Technically a border-radius is made of 2 values (Vector2) but currently we only support one value in the style
                    return resolvedStyle.width;

                case StylePropertyId.FontSize: //Specifies extra spacing as a percentage of the affected character’s advance width.
                case StylePropertyId.LetterSpacing: //No percentage values
                case StylePropertyId.UnityParagraphSpacing: //No CSS equivalent
                case StylePropertyId.WordSpacing: //Specifies extra spacing as a percentage of the affected character’s advance width.
                    return null;

                case StylePropertyId.Translate:
                case StylePropertyId.TransformOrigin:
                    return subPropertyIndex == 0 ? resolvedStyle.width : resolvedStyle.height;
            }
            return null;
        }
    }
}
