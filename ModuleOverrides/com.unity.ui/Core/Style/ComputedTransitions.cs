// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements.Experimental;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    internal struct ComputedTransitionProperty
    {
        public StylePropertyId id;
        public int durationMs;
        public int delayMs;
        public Func<float, float> easingCurve;
    }

    internal static class ComputedTransitionUtils
    {
        internal static void UpdateComputedTransitions(ref ComputedStyle computedStyle)
        {
            if (computedStyle.computedTransitions == null)
            {
                computedStyle.computedTransitions = GetOrComputeTransitionPropertyData(ref computedStyle);
            }
        }

        internal static bool HasTransitionProperty(ref this ComputedStyle computedStyle, StylePropertyId id)
        {
            for (var i = computedStyle.computedTransitions.Length - 1; i >= 0; i--)
            {
                var t = computedStyle.computedTransitions[i];
                if (t.id == id || StylePropertyUtil.IsMatchingShorthand(t.id, id))
                    return true;
            }

            return false;
        }

        internal static bool GetTransitionProperty(ref this ComputedStyle computedStyle, StylePropertyId id, out ComputedTransitionProperty result)
        {
            // See https://www.w3.org/TR/css-transitions-1/#matching-transition-property-value:
            // If a property is specified multiple times in the value of transition-property (either on its own, via a
            // shorthand that contains it, or via the all value), then the transition that starts uses the duration,
            // delay, and timing function at the index corresponding to the last item in the value of
            // transition-property that calls for animating that property.

            for (var i = computedStyle.computedTransitions.Length - 1; i >= 0; i--)
            {
                var t = computedStyle.computedTransitions[i];
                if (t.id == id || StylePropertyUtil.IsMatchingShorthand(t.id, id))
                {
                    result = t;
                    return true;
                }
            }

            result = default;
            return false;
        }

        private static List<ComputedTransitionProperty> s_ComputedTransitionsBuffer = new List<ComputedTransitionProperty>();
        private static ComputedTransitionProperty[] GetOrComputeTransitionPropertyData(ref ComputedStyle computedStyle)
        {
            int hash = GetTransitionHashCode(ref computedStyle);
            if (!StyleCache.TryGetValue(hash, out ComputedTransitionProperty[] computedTransitions))
            {
                ComputeTransitionPropertyData(ref computedStyle, s_ComputedTransitionsBuffer);
                computedTransitions = new ComputedTransitionProperty[s_ComputedTransitionsBuffer.Count];
                s_ComputedTransitionsBuffer.CopyTo(computedTransitions);
                s_ComputedTransitionsBuffer.Clear();
                StyleCache.SetValue(hash, computedTransitions);
            }
            return computedTransitions;
        }

        private static int GetTransitionHashCode(ref ComputedStyle cs)
        {
            unchecked
            {
                int hashCode = 0;
                foreach (var x in cs.transitionDelay) hashCode = (hashCode * 397) ^ x.GetHashCode();
                foreach (var x in cs.transitionDuration) hashCode = (hashCode * 397) ^ x.GetHashCode();
                foreach (var x in cs.transitionProperty) hashCode = (hashCode * 397) ^ x.GetHashCode();
                foreach (var x in cs.transitionTimingFunction) hashCode = (hashCode * 397) ^ x.GetHashCode();
                return hashCode;
            }
        }

        internal static bool SameTransitionProperty(ref ComputedStyle x, ref ComputedStyle y)
        {
            if (x.computedTransitions == y.computedTransitions && x.computedTransitions != null)
                return true;

            return SameTransitionProperty(x.transitionProperty, y.transitionProperty) &&
                SameTransitionProperty(x.transitionDuration, y.transitionDuration) &&
                SameTransitionProperty(x.transitionDelay, y.transitionDelay);
        }

        private static bool SameTransitionProperty(List<StylePropertyName> a, List<StylePropertyName> b)
        {
            if (a == b) return true;
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;
            int n = a.Count;
            for (int i = 0; i < n; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }

        private static bool SameTransitionProperty(List<TimeValue> a, List<TimeValue> b)
        {
            if (a == b) return true;
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;
            int n = a.Count;
            for (int i = 0; i < n; i++)
                if (a[i] != b[i])
                    return false;
            return true;
        }

        private static void ComputeTransitionPropertyData(ref ComputedStyle computedStyle, List<ComputedTransitionProperty> outData)
        {
            var properties = computedStyle.transitionProperty;
            if (properties == null || properties.Count == 0)
                return;

            var durations = computedStyle.transitionDuration;
            var delays = computedStyle.transitionDelay;
            var timingFunctions = computedStyle.transitionTimingFunction;

            // See https://developer.mozilla.org/en-US/docs/Web/CSS/transition-duration
            // You may specify multiple durations; each duration will be applied to the corresponding property
            // as specified by the transition-property property, which acts as a master list. If there are fewer
            // durations specified than in the master list, the user agent repeat the list of durations. If there are
            // more durations, the list is truncated to the right size. In both case the CSS declaration stays valid.

            int nProperties = properties.Count;
            for (var i = 0; i < nProperties; i++)
            {
                var id = properties[i].id;

                // Remove properties that aren't animatable.
                if (id == StylePropertyId.Unknown || !StylePropertyUtil.IsAnimatable(id))
                    continue;

                // Remove properties with non-positive combined duration.
                var durationMs = ConvertTransitionTime(GetWrappingTransitionData(durations, i, new TimeValue(0)));
                var delayMs = ConvertTransitionTime(GetWrappingTransitionData(delays, i, new TimeValue(0)));

                float combinedDuration = Mathf.Max(0, durationMs) + delayMs;
                if (combinedDuration <= 0)
                    continue;

                var easingFunction = GetWrappingTransitionData(timingFunctions, i, EasingMode.Ease);

                outData.Add(new ComputedTransitionProperty
                {
                    id = id,
                    durationMs = durationMs,
                    delayMs = delayMs,
                    easingCurve = ConvertTransitionFunction(easingFunction.mode)
                });
            }
        }

        static T GetWrappingTransitionData<T>(List<T> list, int i, T defaultValue)
        {
            return list.Count == 0 ? defaultValue : list[i % list.Count];
        }

        static int ConvertTransitionTime(TimeValue time)
        {
            return Mathf.RoundToInt(time.unit == TimeUnit.Millisecond ? time.value : time.value * 1000);
        }

        static Func<float, float> ConvertTransitionFunction(EasingMode mode)
        {
            // See https://www.w3schools.com/cssref/css3_pr_transition-timing-function.asp#:~:text=The%20transition%2Dtiming%2Dfunction%20property,change%20speed%20over%20its%20duration.
            // Each of the easing function is equivalent to a cubic-bézier curve with some given P0..P3:
            // (1-t)^3*P0 + 3*(1-t)^2*t*P1 + 3*(1-t)*t^2*P2 + t^3*P3
            // Assuming P0=(0,0) and P3=(1,1), the 4 arguments are x1,y1,x2,y2, specifying P1 and P2.
            // However, we won't implement these exact curves at the moment because they aren't simple t => f(t)
            // methods (see https://github.com/gre/bezier-easing/blob/master/src/index.js for example).
            // Instead, we will use slightly different curves that have a similar feel.
            switch (mode)
            {
                default:
                case EasingMode.Ease:
                    // "Best-fit" cubic curve trying to match start/end points and derivatives (stays within 0.079 of the exact curve).
                    // y = a t^3 + b t^2 + c t + d, where a = -0.2, b = -0.6, c = 1.8, d = 0
                    // Should be equivalent to cubic-bezier(0.25,0.1,0.25,1)
                    return t => t * (1.8f + t * (-0.6f + t * -0.2f));
                case EasingMode.EaseIn:
                    // Should be equivalent to cubic-bezier(0.42,0,1,1)
                    return t => Easing.InQuad(t);
                case EasingMode.EaseOut:
                    // Should be equivalent to cubic-bezier(0,0,0.58,1)
                    return t => Easing.OutQuad(t);
                case EasingMode.EaseInOut:
                    // Should be equivalent to cubic-bezier(0.42,0,0.58,1)
                    return t => Easing.InOutQuad(t);
                case EasingMode.Linear:
                    // Should be equivalent to cubic-bezier(0,0,1,1)
                    return t => Easing.Linear(t);
                case EasingMode.EaseInSine:
                    return t => Easing.InSine(t);
                case EasingMode.EaseOutSine:
                    return t => Easing.OutSine(t);
                case EasingMode.EaseInOutSine:
                    return t => Easing.InOutSine(t);
                case EasingMode.EaseInCubic:
                    return t => Easing.InCubic(t);
                case EasingMode.EaseOutCubic:
                    return t => Easing.OutCubic(t);
                case EasingMode.EaseInOutCubic:
                    return t => Easing.InOutCubic(t);
                case EasingMode.EaseInCirc:
                    return t => Easing.InCirc(t);
                case EasingMode.EaseOutCirc:
                    return t => Easing.OutCirc(t);
                case EasingMode.EaseInOutCirc:
                    return t => Easing.InOutCirc(t);
                case EasingMode.EaseInElastic:
                    return t => Easing.InElastic(t);
                case EasingMode.EaseOutElastic:
                    return t => Easing.OutElastic(t);
                case EasingMode.EaseInOutElastic:
                    return t => Easing.InOutElastic(t);
                case EasingMode.EaseInBack:
                    return t => Easing.InBack(t);
                case EasingMode.EaseOutBack:
                    return t => Easing.OutBack(t);
                case EasingMode.EaseInOutBack:
                    return t => Easing.InOutBack(t);
                case EasingMode.EaseInBounce:
                    return t => Easing.InBounce(t);
                case EasingMode.EaseOutBounce:
                    return t => Easing.OutBounce(t);
                case EasingMode.EaseInOutBounce:
                    return t => Easing.InOutBounce(t);
            }
        }
    }
}
