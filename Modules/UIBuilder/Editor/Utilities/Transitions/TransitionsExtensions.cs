// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    static class TransitionsExtensions
    {
        public static bool IsTransitionId(this StylePropertyId id)
        {
            switch (id)
            {
                case StylePropertyId.Transition:
                case StylePropertyId.TransitionDelay:
                case StylePropertyId.TransitionDuration:
                case StylePropertyId.TransitionProperty:
                case StylePropertyId.TransitionTimingFunction:
                    return true;
            }

            return false;
        }

        public static int MaxCount(this TransitionData data)
        {
            return Mathf.Max(data.transitionProperty.Count,
                Mathf.Max(data.transitionDuration.Count,
                    Mathf.Max(data.transitionTimingFunction.Count,
                        data.transitionDelay.Count)));
        }

        public static Dimension ToDimension(this TimeValue timeValue)
        {
            return new Dimension(timeValue.value, timeValue.unit == TimeUnit.Millisecond
                ? Dimension.Unit.Millisecond
                : Dimension.Unit.Second);
        }

        public static bool IsTimeUnit(this Dimension.Unit unit)
        {
            return unit == Dimension.Unit.Millisecond || unit == Dimension.Unit.Second;
        }

        public static string UssName(this StylePropertyId id)
        {
            return StylePropertyUtil.s_IdToName.TryGetValue(id, out var name) ? name : id.ToString();
        }

        // TODO: Move this to codegen
        public static StylePropertyId GetShorthandProperty(this StylePropertyId id)
        {
            switch (id)
            {
                case StylePropertyId.BorderBottomColor:
                case StylePropertyId.BorderLeftColor:
                case StylePropertyId.BorderRightColor:
                case StylePropertyId.BorderTopColor:
                    return StylePropertyId.BorderColor;
                case StylePropertyId.BorderBottomLeftRadius:
                case StylePropertyId.BorderBottomRightRadius:
                case StylePropertyId.BorderTopLeftRadius:
                case StylePropertyId.BorderTopRightRadius:
                    return StylePropertyId.BorderRadius;
                case StylePropertyId.BorderBottomWidth:
                case StylePropertyId.BorderLeftWidth:
                case StylePropertyId.BorderRightWidth:
                case StylePropertyId.BorderTopWidth:
                    return StylePropertyId.BorderWidth;
                case StylePropertyId.FlexBasis:
                case StylePropertyId.FlexGrow:
                case StylePropertyId.FlexShrink:
                    return StylePropertyId.Flex;
                case StylePropertyId.MarginBottom:
                case StylePropertyId.MarginLeft:
                case StylePropertyId.MarginRight:
                case StylePropertyId.MarginTop:
                    return StylePropertyId.Margin;
                case StylePropertyId.PaddingBottom:
                case StylePropertyId.PaddingLeft:
                case StylePropertyId.PaddingRight:
                case StylePropertyId.PaddingTop:
                    return StylePropertyId.Padding;
                case StylePropertyId.TransitionProperty:
                case StylePropertyId.TransitionDuration:
                case StylePropertyId.TransitionTimingFunction:
                case StylePropertyId.TransitionDelay:
                    return StylePropertyId.Transition;
                case StylePropertyId.UnityTextOutlineColor:
                case StylePropertyId.UnityTextOutlineWidth:
                    return StylePropertyId.UnityTextOutline;
                default:
                    return StylePropertyId.Unknown;
            }
        }
    }
}
