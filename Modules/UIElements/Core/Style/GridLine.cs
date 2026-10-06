// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// How a <see cref="GridLine"/> places one edge of a CSS Grid item.
    /// </summary>
    public enum GridLinePlacement
    {
        /// <summary>The edge is resolved by the grid's auto-placement algorithm.</summary>
        Auto = 0,
        /// <summary>An explicit, 1-based grid line.</summary>
        Line = 1,
        /// <summary>A span of N tracks, measured from the item's opposite edge.</summary>
        Span = 2
    }

    /// <summary>
    /// A single placement value for <c>grid-column-start</c>, <c>grid-column-end</c>,
    /// <c>grid-row-start</c> and <c>grid-row-end</c>: <c>auto</c>, an explicit line
    /// (an integer &gt;= 1), or <c>span &lt;n&gt;</c> (n &gt;= 1).
    /// </summary>
    /// <remarks>
    /// The value is sign-encoded into a single blittable int so it mirrors the native field 1:1:
    /// <c>0</c> = auto, <c>&gt; 0</c> = line, <c>&lt; 0</c> = span(-value). This mirrors how
    /// aspect-ratio uses a sentinel value rather than a separate flag.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public readonly partial struct GridLine : IEquatable<GridLine>
    {
        // 0 = auto | > 0 = line n | < 0 = span(-n). Layout matches the native `int` field.
        readonly int m_Value;

        GridLine(int rawValue) { m_Value = rawValue; }

        // Raw sign-encoded value (0 auto | >0 line | <0 span). Used by the style pipeline to move the
        // value through the blittable int field it shares with native, bypassing the throwing ctors.
        internal int rawValue => m_Value;
        internal static GridLine FromRawValue(int raw) => new GridLine(raw);

        // ---- pretty constructors --------------------------------------------------------------

        /// <summary><c>auto</c>: the edge is resolved by grid auto-placement.</summary>
        public static GridLine Auto => new GridLine(0);

        /// <summary>An explicit 1-based grid line.</summary>
        /// <param name="line">The line index; must be &gt;= 1 (0 and negatives are invalid).</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="line"/> is less than 1.</exception>
        public static GridLine AtLine(int line)
        {
            if (line < 1)
                throw new ArgumentOutOfRangeException(nameof(line), line,
                    "A grid line must be >= 1; 0 and negative values are invalid.");
            return new GridLine(line);
        }

        /// <summary>A span of <paramref name="count"/> tracks from the opposite edge.</summary>
        /// <param name="count">The number of tracks to span; must be &gt;= 1.</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="count"/> is less than 1.</exception>
        public static GridLine Span(int count)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count), count, "A grid span must be >= 1.");
            return new GridLine(-count);
        }

        // ---- inspection -----------------------------------------------------------------------

        /// <summary>Which kind of placement this value represents.</summary>
        public GridLinePlacement placement =>
            m_Value == 0 ? GridLinePlacement.Auto :
            m_Value  > 0 ? GridLinePlacement.Line : GridLinePlacement.Span;

        /// <summary>Whether this value is <c>auto</c>.</summary>
        public bool isAuto => m_Value == 0;
        /// <summary>Whether this value is an explicit line.</summary>
        public bool isLine => m_Value > 0;
        /// <summary>Whether this value is a span.</summary>
        public bool isSpan => m_Value < 0;

        /// <summary>The 1-based line index when <see cref="isLine"/> is true; otherwise 0.</summary>
        public int line => m_Value > 0 ? m_Value : 0;
        /// <summary>The span count when <see cref="isSpan"/> is true; otherwise 0.</summary>
        public int span => m_Value < 0 ? -m_Value : 0;

        // ---- conversions ----------------------------------------------------------------------

        /// <summary>Converts a positive integer to an explicit line; 0 and negatives throw.</summary>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="line"/> is less than 1.</exception>
        public static implicit operator GridLine(int line) => AtLine(line);

        // ---- equality -------------------------------------------------------------------------

        /// <undoc/>
        public static bool operator ==(GridLine lhs, GridLine rhs) => lhs.m_Value == rhs.m_Value;

        /// <undoc/>
        public static bool operator !=(GridLine lhs, GridLine rhs) => !(lhs == rhs);

        /// <undoc/>
        public bool Equals(GridLine other) => m_Value == other.m_Value;

        /// <undoc/>
        public override bool Equals(object obj) => obj is GridLine other && Equals(other);

        /// <undoc/>
        public override int GetHashCode() => m_Value;

        // ---- text -----------------------------------------------------------------------------

        /// <summary>The USS text form: <c>auto</c>, <c>span &lt;n&gt;</c>, or the line number.</summary>
        public override string ToString() =>
            isAuto ? "auto" : isSpan ? $"span {span}" : line.ToString(CultureInfo.InvariantCulture);

        // ---- parsing --------------------------------------------------------------------------

        /// <summary>
        /// Parses a single grid line value: <c>auto</c>, <c>&lt;integer &gt;= 1&gt;</c>, <c>span</c>
        /// (defaults to 1), or <c>span &lt;integer &gt;= 1&gt;</c>. Case-insensitive. Rejects <c>0</c>,
        /// negatives, non-integers, and unknown identifiers.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule", "UnityEditor.UIElementsModule")]
        internal static bool TryParse(string s, out GridLine value)
        {
            value = Auto;
            if (string.IsNullOrWhiteSpace(s))
                return false;

            var tokens = s.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

            // auto
            if (tokens.Length == 1 && string.Equals(tokens[0], "auto", StringComparison.OrdinalIgnoreCase))
            {
                value = Auto;
                return true;
            }

            // span | span <n>
            if (string.Equals(tokens[0], "span", StringComparison.OrdinalIgnoreCase))
            {
                if (tokens.Length == 1) // "span" alone -> span 1
                {
                    value = Span(1);
                    return true;
                }
                if (tokens.Length == 2 &&
                    int.TryParse(tokens[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) &&
                    count >= 1)
                {
                    value = Span(count);
                    return true;
                }
                return false;
            }

            // <integer> line (>= 1; rejects 0 and negatives)
            if (tokens.Length == 1 &&
                int.TryParse(tokens[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var lineIndex) &&
                lineIndex >= 1)
            {
                value = AtLine(lineIndex);
                return true;
            }

            return false;
        }
    }
}
