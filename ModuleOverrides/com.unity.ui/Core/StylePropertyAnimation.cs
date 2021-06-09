// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
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
        bool Start(StylePropertyId id, Cursor from, Cursor to, int durationMs, int delayMs, Func<float, float> easingCurve);
        bool Start(StylePropertyId id, TextShadow from, TextShadow to, int durationMs, int delayMs, Func<float, float> easingCurve);
        bool Start(StylePropertyId id, Scale from, Scale to, int durationMs, int delayMs, Func<float, float> easingCurve);
        bool Start(StylePropertyId id, Translate from, Translate to, int durationMs, int delayMs, Func<float, float> easingCurve);
        bool Start(StylePropertyId id, Rotate from, Rotate to, int durationMs, int delayMs, Func<float, float> easingCurve);
        bool Start(StylePropertyId id, TransformOrigin from, TransformOrigin to, int durationMs, int delayMs, Func<float, float> easingCurve);


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
            if (!TryConvertLengthUnits(id, ref from, ref to))
                return false;
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

        bool IStylePropertyAnimations.Start(StylePropertyId id, Cursor from, Cursor to, int durationMs, int delayMs, Func<float, float> easingCurve)
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
            if (!TryConvertTranslateUnits(ref from, ref to))
                return false;
            return GetStylePropertyAnimationSystem().StartTransition(this, id, from, to, durationMs, delayMs, easingCurve);
        }

        bool IStylePropertyAnimations.Start(StylePropertyId id, Rotate from, Rotate to, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            return GetStylePropertyAnimationSystem().StartTransition(this, id, from, to, durationMs, delayMs, easingCurve);
        }

        bool IStylePropertyAnimations.Start(StylePropertyId id, TransformOrigin from, TransformOrigin to, int durationMs, int delayMs, Func<float, float> easingCurve)
        {
            if (!TryConvertTransformOriginUnits(ref from, ref to))
                return false;
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

        private bool TryConvertLengthUnits(StylePropertyId id, ref Length from, ref Length to)
        {
            if (from.IsAuto() || from.IsNone() || to.IsAuto() || to.IsNone())
                return false;
            if (Mathf.Approximately(from.value, 0))
                from.unit = to.unit;
            else if (from.unit != to.unit)
                return false;
            return true;
        }

        // Changes the from TransformOrigin so that it apply the same result as before, but with the unit of the "to" value
        // return false if not possible
        private bool TryConvertTransformOriginUnits(ref TransformOrigin from, ref TransformOrigin to)
        {
            if (from.x.unit != to.x.unit || from.y.unit != to.y.unit)
                return false;

            return true;
        }

        // Changes the from Translate so that it apply the same result as before, but with the unit of the "to"
        // return false if not possible
        private bool TryConvertTranslateUnits(ref Translate from, ref Translate to)
        {
            if (from.x.unit != to.x.unit || from.y.unit != to.y.unit)
                return false;

            return true;
        }
    }
}
