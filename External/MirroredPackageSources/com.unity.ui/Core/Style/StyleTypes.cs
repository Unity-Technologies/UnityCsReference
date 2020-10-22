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
        internal static string DebugString<T>(this IStyleValue<T> styleValue)
        {
            return styleValue.keyword != StyleKeyword.Undefined ? $"{styleValue.keyword}" : $"{styleValue.value}";
        }

        internal static YogaValue ToYogaValue(this Length length)
        {
            if (length.IsAuto())
                return YogaValue.Auto();

            // For max-width and max-height
            if (length.IsNone())
                return float.NaN;

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

        internal static Length ToLength(this StyleKeyword keyword)
        {
            switch (keyword)
            {
                case StyleKeyword.Auto:
                    return Length.Auto();
                case StyleKeyword.None:
                    return Length.None();
                default:
                    Debug.LogAssertion($"Unexpected StyleKeyword '{keyword.ToString()}'");
                    return new Length();
            }
        }

        internal static Length ToLength(this StyleLength styleLength)
        {
            switch (styleLength.keyword)
            {
                case StyleKeyword.Auto:
                case StyleKeyword.None:
                    return styleLength.keyword.ToLength();
                default:
                    return styleLength.value;
            }
        }
    }
}
