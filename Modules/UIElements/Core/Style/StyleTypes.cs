// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.UIElements.Layout;
using UnityEngine.UIElements.StyleSheets;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Base interface for the style properties.
    /// </summary>
    /// <typeparam name="T">The value type of the style property.</typeparam>
    public interface IStyleValue<T>
    {
        /// <summary>
        /// The style value.
        /// </summary>
        T value { get; set; }

        /// <summary>
        /// The style keyword.
        /// </summary>
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

        internal static LayoutValue ToLayoutValue(this Length length)
        {
            if (length.IsAuto())
                return LayoutValue.Auto();

            // For max-width and max-height
            if (length.IsNone())
                return float.NaN;

            switch (length.unit)
            {
                case LengthUnit.Pixel:
                    return LayoutValue.Point(length.value);
                case LengthUnit.Percent:
                    return LayoutValue.Percent(length.value);
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

        internal static Rotate ToRotate(this StyleKeyword keyword)
        {
            switch (keyword)
            {
                case StyleKeyword.None:
                    return Rotate.None();
                default:
                    Debug.LogAssertion($"Unexpected StyleKeyword '{keyword.ToString()}'");
                    return new Rotate();
            }
        }

        internal static Scale ToScale(this StyleKeyword keyword)
        {
            switch (keyword)
            {
                case StyleKeyword.None:
                    return Scale.None();
                default:
                    Debug.LogAssertion($"Unexpected StyleKeyword '{keyword.ToString()}'");
                    return new Scale();
            }
        }

        internal static Translate ToTranslate(this StyleKeyword keyword)
        {
            switch (keyword)
            {
                case StyleKeyword.None:
                    return Translate.None();
                default:
                    Debug.LogAssertion($"Unexpected StyleKeyword '{keyword.ToString()}'");
                    return new Translate();
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

        internal static void CopyFrom<T>(this List<T> list, List<T> other)
        {
            list.Clear();
            list.AddRange(other);
        }
    }
}
