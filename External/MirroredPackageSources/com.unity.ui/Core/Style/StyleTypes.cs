using System;
using System.Collections.Generic;
using UnityEngine.UIElements.StyleSheets;
using UnityEngine.Yoga;

namespace UnityEngine.UIElements
{
    internal interface IStyleValue<T>
    {
        T value { get; set; }
        StyleKeyword keyword { get; set; }
    }

    /// <summary>
    /// Keyword that can be used on any style value types.
    /// </summary>
    public enum StyleKeyword
    {
        /// <summary>
        /// Means that there's no keyword defined for that property.
        /// </summary>
        Undefined, // No keyword defined
        /// <summary>
        /// Means that an inline style from <see cref="IStyle"/> has no value or keyword.
        /// </summary>
        /// <remarks>
        /// This can also be used to remove an inline style previously set on a property.
        /// </remarks>
        Null, // No inline style value
        /// <summary>
        /// For style properties accepting auto.
        /// </summary>
        Auto,
        /// <summary>
        /// For style properties accepting none.
        /// </summary>
        None,
        /// <summary>
        /// The initial (or default) value of a style property.
        /// </summary>
        Initial // Default value
    }

    internal static class StyleValueExtensions
    {
        // Convert StyleLength to StyleFloat for IResolvedStyle
        internal static StyleFloat ToStyleFloat(this StyleLength styleLength)
        {
            return new StyleFloat(styleLength.value.value, styleLength.keyword);
        }

        // Convert StyleInt to StyleEnum for ComputedStyle
        internal static StyleEnum<T> ToStyleEnum<T>(this StyleInt styleInt, T value) where T : struct, IConvertible
        {
            return new StyleEnum<T>(value, styleInt.keyword);
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

        internal static YogaValue ToYogaValue(this StyleLength styleValue)
        {
            if (styleValue.keyword == StyleKeyword.Auto)
                return YogaValue.Auto();

            // For max-width and max-height
            if (styleValue.keyword == StyleKeyword.None)
                return float.NaN;

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

        internal static StyleKeyword ToStyleKeyword(this StyleValueKeyword styleValueKeyword)
        {
            switch (styleValueKeyword)
            {
                case StyleValueKeyword.Auto:
                    return StyleKeyword.Auto;
                case StyleValueKeyword.None:
                    return StyleKeyword.None;
                case StyleValueKeyword.Initial:
                    return StyleKeyword.Initial;
            }

            return StyleKeyword.Undefined;
        }
    }
}
