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
        /// <summary>An explicit grid line: positive values count from the start edge (1-based), negative values from the end edge.</summary>
        Line = 1,
        /// <summary>A span of N tracks, measured from the item's opposite edge.</summary>
        Span = 2
    }

    /// <summary>
    /// A single placement value for <c>grid-column-start</c>, <c>grid-column-end</c>,
    /// <c>grid-row-start</c> and <c>grid-row-end</c>: <c>auto</c>, an explicit line
    /// (a non-zero integer; negative lines count back from the end edge of the explicit grid),
    /// or <c>span &lt;n&gt;</c> (n &gt;= 1).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly partial struct GridLine : IEquatable<GridLine>
    {
        // Shared with the native int field and persisted in UI animation clip curves, so existing raw values keep their meaning:
        // 0 = auto | > 0 = line n | (int.MinValue, int.MinValue + k_MaxNegativeLine] = line -(raw - int.MinValue) | other < 0 = span(-raw).
        readonly int m_Value;

        internal const int k_MaxNegativeLine = 100000;
        internal const int k_MaxSpan = int.MaxValue - k_MaxNegativeLine;

        GridLine(int rawValue) { m_Value = rawValue; }

        internal int rawValue { [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")] get => m_Value; }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static GridLine FromRawValue(int raw) => new GridLine(raw);

        bool isNegativeLine => m_Value > int.MinValue && m_Value <= int.MinValue + k_MaxNegativeLine;

        // ---- pretty constructors --------------------------------------------------------------

        /// <summary><c>auto</c>: the edge is resolved by grid auto-placement.</summary>
        public static GridLine Auto => new GridLine(0);

        /// <summary>An explicit grid line.</summary>
        /// <param name="line">The 1-based line index, or a negative index counted from the end edge of the explicit grid (-1 is the last line). Values below -100000 are clamped to -100000.</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="line"/> is 0.</exception>
        public static GridLine AtLine(int line)
        {
            if (line == 0)
                throw new ArgumentOutOfRangeException(nameof(line), line, "A grid line must not be 0.");
            if (line > 0)
                return new GridLine(line);
            return new GridLine(int.MinValue + (int)Math.Min(-(long)line, k_MaxNegativeLine));
        }

        /// <summary>A span of <paramref name="count"/> tracks from the opposite edge.</summary>
        /// <param name="count">The number of tracks to span; must be &gt;= 1. Values above int.MaxValue - 100000 are clamped.</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="count"/> is less than 1.</exception>
        public static GridLine Span(int count)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count), count, "A grid span must be >= 1.");
            return new GridLine(-Math.Min(count, k_MaxSpan));
        }

        // ---- inspection -----------------------------------------------------------------------

        /// <summary>Which kind of placement this value represents.</summary>
        public GridLinePlacement placement =>
            m_Value == 0 ? GridLinePlacement.Auto :
            isLine ? GridLinePlacement.Line : GridLinePlacement.Span;

        /// <summary>Whether this value is <c>auto</c>.</summary>
        public bool isAuto => m_Value == 0;
        /// <summary>Whether this value is an explicit line.</summary>
        public bool isLine => m_Value > 0 || isNegativeLine;
        /// <summary>Whether this value is a span.</summary>
        public bool isSpan => m_Value < 0 && !isNegativeLine;

        /// <summary>The line index when <see cref="isLine"/> is true (1-based, or negative counting back from the end edge); otherwise 0.</summary>
        public int line => m_Value > 0 ? m_Value : isNegativeLine ? int.MinValue - m_Value : 0;
        /// <summary>The span count when <see cref="isSpan"/> is true; otherwise 0.</summary>
        public int span => !isSpan ? 0 : m_Value == int.MinValue ? k_MaxSpan : -m_Value;

        // ---- conversions ----------------------------------------------------------------------

        /// <summary>Converts a non-zero integer to an explicit line (see <see cref="AtLine"/>).</summary>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="line"/> is 0.</exception>
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
        /// Parses a single grid line value: <c>auto</c>, a non-zero <c>&lt;integer&gt;</c>, <c>span</c>
        /// (defaults to 1), or <c>span &lt;integer &gt;= 1&gt;</c>. Case-insensitive. Rejects <c>0</c>,
        /// non-integers, and unknown identifiers.
        /// </summary>
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule", "UnityEditor.UIElementsModule")]
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

            if (tokens.Length == 1 &&
                int.TryParse(tokens[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var lineIndex) &&
                lineIndex != 0)
            {
                value = AtLine(lineIndex);
                return true;
            }

            return false;
        }
    }
}
