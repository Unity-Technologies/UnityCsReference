// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.Yoga;

namespace UnityEngine.UIElements
{
    internal interface IStyleValue<T>
    {
        T value { get; set; }
        int specificity { get; set; }
        StyleKeyword keyword { get; set; }

        bool Apply<U>(U otherValue, StylePropertyApplyMode mode) where U : IStyleValue<T>;
    }

    public enum StyleKeyword
    {
        Undefined, // No keyword defined
        Null, // No inline style value
        Auto,
        None,
        Initial // Default value
    }

    internal static class StyleValueExtensions
    {
        internal const int UndefinedSpecificity = 0;
        internal const int UnitySpecificity = -1;
        internal const int InlineSpecificity = int.MaxValue;

        // Convert StyleLength to StyleFloat for IResolvedStyle
        internal static StyleFloat ToStyleFloat(this StyleLength styleLength)
        {
            return new StyleFloat(styleLength.value.value, styleLength.keyword) { specificity = styleLength.specificity };
        }

        // Convert StyleInt to StyleEnum for ComputedStyle
        internal static StyleEnum<T> ToStyleEnum<T>(this StyleInt styleInt, T value) where T : struct, IConvertible
        {
            return new StyleEnum<T>(value, styleInt.keyword) {specificity = styleInt.specificity };
        }

        internal static StyleLength ToStyleLength(this StyleValue styleValue)
        {
            return new StyleLength(new Length(styleValue.number), styleValue.keyword);
        }

        internal static StyleFloat ToStyleFloat(this StyleValue styleValue)
        {
            return new StyleFloat(styleValue.number, styleValue.keyword);
        }

        internal static string DebugString<T>(this IStyleValue<T> styleValue)
        {
            return styleValue.keyword != StyleKeyword.Undefined ? $"{styleValue.keyword}" : $"{styleValue.value}";
        }

        internal static U GetSpecifiedValueOrDefault<T, U>(this T styleValue, U defaultValue) where T : IStyleValue<U>
        {
            if (styleValue.specificity != UndefinedSpecificity)
                return styleValue.value;

            return defaultValue;
        }

        internal static float GetSpecifiedValueOrDefault(this StyleLength styleValue, float defaultValue)
        {
            if (styleValue.specificity != UndefinedSpecificity)
                return styleValue.value.value;

            return defaultValue;
        }

        internal static YogaValue ToYogaValue(this StyleLength styleValue)
        {
            if (styleValue.keyword == StyleKeyword.Auto)
                return YogaValue.Auto();

            // For max-width and max-height
            if (styleValue.keyword == StyleKeyword.None)
                return float.NaN;

            if (styleValue.specificity != UndefinedSpecificity)
            {
                var length = styleValue.value;
                switch (length.unit)
                {
                    case LengthUnit.Pixel:
                        return YogaValue.Point(length.value);
                    case LengthUnit.Percent:
                        return YogaValue.Percent(length.value);
                    default:
                        Debug.LogAssertion($"Unexpected unit '{length.unit}'");
                        return float.NaN;
                }
            }

            return float.NaN;
        }

        internal static bool CanApply(int specificity, int otherSpecificity, StylePropertyApplyMode mode)
        {
            switch (mode)
            {
                case StylePropertyApplyMode.Copy:
                    return true;
                case StylePropertyApplyMode.CopyIfEqualOrGreaterSpecificity:
                {
                    if (specificity == UndefinedSpecificity && otherSpecificity == UnitySpecificity)
                        return true;

                    return otherSpecificity >= specificity;
                }
                case StylePropertyApplyMode.CopyIfNotInline:
                    return specificity < InlineSpecificity;
                default:
                    Debug.Assert(false, "Invalid mode " + mode);
                    return false;
            }
        }
    }
}
