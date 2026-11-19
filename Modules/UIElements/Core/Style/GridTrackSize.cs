// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Unit for a single <see cref="GridTrackSize"/> sizing value.
    /// </summary>
    public enum GridTrackSizeUnit
    {
        /// <summary>The track is sized automatically from its content.</summary>
        Auto = 0,
        /// <summary>A fixed pixel length.</summary>
        Pixel = 1,
        /// <summary>A percentage of the grid container's corresponding dimension.</summary>
        Percent = 2,
        /// <summary>A flexible fraction (`fr`) of the leftover space.</summary>
        Fraction = 3,
        /// <summary>The largest minimal content contribution of the track's items (`min-content`).</summary>
        MinContent = 4,
        /// <summary>The largest maximal content contribution of the track's items (`max-content`).</summary>
        MaxContent = 5
    }

    // How the two sizing values below combine. Kept in sync with native GridTypes.h.
    internal enum GridTrackKind
    {
        Track = 0,       // a single sizing function (min == max)
        Minmax = 1,      // minmax(min, max)
        FitContent = 2,  // fit-content(maxValue): clamp auto to a length
        AutoFill = 3,    // repeat(auto-fill, <pattern>): min/max carry the repeated track
        AutoFit = 4      // repeat(auto-fit, <pattern>): like auto-fill, empty tracks collapse
    }

    /// <summary>
    /// The size of a single CSS Grid track (a column or a row), as used by
    /// `grid-template-columns`, `grid-template-rows`, `grid-auto-columns` and `grid-auto-rows`.
    /// </summary>
    /// <remarks>
    /// A track size is a
    /// single sizing function (e.g. `100px`, `1fr`, `auto`), a `minmax(min, max)` pair, or
    /// `fit-content(length)`. The layout of this struct mirrors the native `GridTrackSize` and
    /// must stay blittable.
    /// </remarks>
    [Serializable, StructLayout(LayoutKind.Sequential)]
    public partial struct GridTrackSize : IEquatable<GridTrackSize>
    {
        [SerializeField] internal GridTrackKind m_Kind;
        [SerializeField] float m_MinValue;
        [SerializeField] GridTrackSizeUnit m_MinUnit;
        [SerializeField] float m_MaxValue;
        [SerializeField] GridTrackSizeUnit m_MaxUnit;

        internal GridTrackSize(GridTrackKind kind, float minValue, GridTrackSizeUnit minUnit, float maxValue, GridTrackSizeUnit maxUnit)
        {
            m_Kind = kind;
            m_MinValue = minValue;
            m_MinUnit = minUnit;
            m_MaxValue = maxValue;
            m_MaxUnit = maxUnit;
        }

        static GridTrackSize Single(float value, GridTrackSizeUnit unit)
            => new GridTrackSize(GridTrackKind.Track, value, unit, value, unit);

        /// <summary>A fixed pixel track.</summary>
        public static GridTrackSize Pixels(float value) => Single(value, GridTrackSizeUnit.Pixel);
        /// <summary>A percentage track.</summary>
        public static GridTrackSize Percent(float value) => Single(value, GridTrackSizeUnit.Percent);
        /// <summary>A flexible `fr` track.</summary>
        // A single <flex> is minmax(auto, <flex>) per CSS Grid; the `auto` min floors the track at
        // min-content so a bare fr never collapses below its content (fr is not a valid minimum).
        public static GridTrackSize Fraction(float value)
            => new GridTrackSize(GridTrackKind.Track, 0f, GridTrackSizeUnit.Auto, value, GridTrackSizeUnit.Fraction);
        /// <summary>An `auto` track.</summary>
        public static GridTrackSize Auto() => Single(0f, GridTrackSizeUnit.Auto);
        /// <summary>A `min-content` track.</summary>
        public static GridTrackSize MinContent() => Single(0f, GridTrackSizeUnit.MinContent);
        /// <summary>A `max-content` track.</summary>
        public static GridTrackSize MaxContent() => Single(0f, GridTrackSizeUnit.MaxContent);

        /// <summary>A `minmax(min, max)` track.</summary>
        public static GridTrackSize Minmax(GridTrackSize min, GridTrackSize max)
            => new GridTrackSize(GridTrackKind.Minmax, min.m_MaxValue, min.m_MaxUnit, max.m_MaxValue, max.m_MaxUnit);

        /// <summary>A `fit-content(length)` track.</summary>
        public static GridTrackSize FitContent(float value, GridTrackSizeUnit unit = GridTrackSizeUnit.Pixel)
            => new GridTrackSize(GridTrackKind.FitContent, 0f, GridTrackSizeUnit.Auto, value, unit);

        /// <summary>A `repeat(auto-fill, track)` track. The repeat count is resolved from the container size.</summary>
        public static GridTrackSize RepeatAutoFill(GridTrackSize track)
            => new GridTrackSize(GridTrackKind.AutoFill, track.m_MinValue, track.m_MinUnit, track.m_MaxValue, track.m_MaxUnit);
        /// <summary>A `repeat(auto-fit, track)` track: like auto-fill, but empty tracks collapse to 0.</summary>
        public static GridTrackSize RepeatAutoFit(GridTrackSize track)
            => new GridTrackSize(GridTrackKind.AutoFit, track.m_MinValue, track.m_MinUnit, track.m_MaxValue, track.m_MaxUnit);

        /// <summary>True when this track is a `minmax()` sizing function.</summary>
        public bool isMinmax => m_Kind == GridTrackKind.Minmax;
        /// <summary>True when this track is a `fit-content()` sizing function.</summary>
        public bool isFitContent => m_Kind == GridTrackKind.FitContent;
        /// <summary>True when this track is a `repeat(auto-fill, …)` track.</summary>
        public bool isAutoFill => m_Kind == GridTrackKind.AutoFill;
        /// <summary>True when this track is a `repeat(auto-fit, …)` track.</summary>
        public bool isAutoFit => m_Kind == GridTrackKind.AutoFit;

        /// <summary>The minimum sizing value.</summary>
        public float minValue => m_MinValue;
        /// <summary>The minimum sizing unit.</summary>
        public GridTrackSizeUnit minUnit => m_MinUnit;
        /// <summary>The maximum sizing value (or the single value for a plain track).</summary>
        public float maxValue => m_MaxValue;
        /// <summary>The maximum sizing unit (or the single unit for a plain track).</summary>
        public GridTrackSizeUnit maxUnit => m_MaxUnit;

        // Text round-trip for the authoring inspectors (UI Builder + Integrated Authoring). Parses a
        // space-separated track list: auto / min-content / max-content, <n>fr / <n>% / <n>px / <n>,
        // minmax(a, b), fit-content(len), repeat(<int> | auto-fill | auto-fit, tracks).
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static List<GridTrackSize> ParseList(string text)
        {
            var result = new List<GridTrackSize>();
            if (string.IsNullOrWhiteSpace(text)) return result;
            if (string.Equals(text.Trim(), "none", StringComparison.OrdinalIgnoreCase)) return result;

            foreach (var tok in TopLevelTokens(text))
            {
                var lower = tok.ToLowerInvariant();
                if (lower.StartsWith("repeat(") && tok.EndsWith(")"))
                {
                    var inner = tok.Substring(7, tok.Length - 8);
                    int comma = TopLevelIndexOf(inner, ',');
                    if (comma < 0) continue;
                    var countTok = inner.Substring(0, comma).Trim();
                    var pattern = ParseList(inner.Substring(comma + 1).Trim());
                    if (pattern.Count == 0) continue;
                    if (string.Equals(countTok, "auto-fill", StringComparison.OrdinalIgnoreCase))
                        result.Add(RepeatAutoFill(pattern[0]));
                    else if (string.Equals(countTok, "auto-fit", StringComparison.OrdinalIgnoreCase))
                        result.Add(RepeatAutoFit(pattern[0]));
                    else if (int.TryParse(countTok, out var n) && n > 0)
                        for (int r = 0; r < n; ++r) result.AddRange(pattern);
                }
                else if (lower.StartsWith("minmax(") && tok.EndsWith(")"))
                {
                    var inner = tok.Substring(7, tok.Length - 8);
                    int comma = TopLevelIndexOf(inner, ',');
                    if (comma < 0) continue;
                    result.Add(Minmax(ParseSingle(inner.Substring(0, comma).Trim()), ParseSingle(inner.Substring(comma + 1).Trim())));
                }
                else if (lower.StartsWith("fit-content(") && tok.EndsWith(")"))
                {
                    var len = ParseSingle(tok.Substring(12, tok.Length - 13).Trim());
                    result.Add(FitContent(len.maxValue, len.maxUnit));
                }
                else
                {
                    result.Add(ParseSingle(tok));
                }
            }
            return result;
        }

        // Parse a single track (used by the UXML attribute converter for StyleList<GridTrackSize>).
        [VisibleToOtherModules("UnityEditor.UIElementsModule")]
        internal static GridTrackSize Parse(string text)
        {
            var list = ParseList(text);
            return list.Count > 0 ? list[0] : Auto();
        }

        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal static string FormatList(IEnumerable<GridTrackSize> tracks)
        {
            if (tracks == null) return string.Empty;

            // The stored list is always expanded, so re-collapse runs of identical consecutive tracks into
            // `repeat(n, <track>)` for the inspector. auto-fill/auto-fit are already `repeat(...)` and are left alone.
            var list = tracks as IList<GridTrackSize> ?? new List<GridTrackSize>(tracks);
            var sb = new System.Text.StringBuilder();
            int i = 0;
            while (i < list.Count)
            {
                var t = list[i];
                int run = 1;
                if (!t.isAutoFill && !t.isAutoFit)
                    while (i + run < list.Count && list[i + run].Equals(t)) ++run;

                if (sb.Length > 0) sb.Append(' ');
                if (run >= 2)
                {
                    sb.Append("repeat(").Append(run.ToString(CultureInfo.InvariantCulture))
                        .Append(", ").Append(t.ToString()).Append(')');
                    i += run;
                }
                else
                {
                    sb.Append(t.ToString());
                    ++i;
                }
            }
            return sb.ToString();
        }

        static GridTrackSize ParseSingle(string tok)
        {
            var lower = tok.Trim().ToLowerInvariant();
            if (lower == "auto") return Auto();
            if (lower == "min-content") return MinContent();
            if (lower == "max-content") return MaxContent();
            if (lower.EndsWith("fr") && float.TryParse(lower.Substring(0, lower.Length - 2), NumberStyles.Float, CultureInfo.InvariantCulture, out var fr))
                return Fraction(fr);
            if (lower.EndsWith("%") && float.TryParse(lower.Substring(0, lower.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out var pct))
                return Percent(pct);
            if (lower.EndsWith("px") && float.TryParse(lower.Substring(0, lower.Length - 2), NumberStyles.Float, CultureInfo.InvariantCulture, out var px))
                return Pixels(px);
            if (float.TryParse(lower, NumberStyles.Float, CultureInfo.InvariantCulture, out var raw))
                return Pixels(raw);
            return Auto();
        }

        static IEnumerable<string> TopLevelTokens(string text)
        {
            int depth = 0, start = -1;
            for (int i = 0; i < text.Length; ++i)
            {
                char c = text[i];
                if (c == '(') depth++;
                else if (c == ')') depth = Math.Max(0, depth - 1);
                bool ws = char.IsWhiteSpace(c) && depth == 0;
                if (ws) { if (start >= 0) { yield return text.Substring(start, i - start); start = -1; } }
                else if (start < 0) start = i;
            }
            if (start >= 0) yield return text.Substring(start);
        }

        static int TopLevelIndexOf(string text, char target)
        {
            int depth = 0;
            for (int i = 0; i < text.Length; ++i)
            {
                if (text[i] == '(') depth++;
                else if (text[i] == ')') depth--;
                else if (text[i] == target && depth == 0) return i;
            }
            return -1;
        }

        static string PartToString(float value, GridTrackSizeUnit unit)
        {
            switch (unit)
            {
                case GridTrackSizeUnit.Auto: return "auto";
                case GridTrackSizeUnit.MinContent: return "min-content";
                case GridTrackSizeUnit.MaxContent: return "max-content";
                case GridTrackSizeUnit.Percent: return value.ToString(CultureInfo.InvariantCulture) + "%";
                case GridTrackSizeUnit.Fraction: return value.ToString(CultureInfo.InvariantCulture) + "fr";
                default: return value.ToString(CultureInfo.InvariantCulture) + "px";
            }
        }

        public override string ToString()
        {
            switch (m_Kind)
            {
                case GridTrackKind.Minmax:
                    return "minmax(" + PartToString(m_MinValue, m_MinUnit) + ", " + PartToString(m_MaxValue, m_MaxUnit) + ")";
                case GridTrackKind.FitContent:
                    return "fit-content(" + PartToString(m_MaxValue, m_MaxUnit) + ")";
                case GridTrackKind.AutoFill:
                case GridTrackKind.AutoFit:
                {
                    var pattern = (m_MinValue == m_MaxValue && m_MinUnit == m_MaxUnit)
                        ? PartToString(m_MaxValue, m_MaxUnit)
                        : "minmax(" + PartToString(m_MinValue, m_MinUnit) + ", " + PartToString(m_MaxValue, m_MaxUnit) + ")";
                    return "repeat(" + (m_Kind == GridTrackKind.AutoFill ? "auto-fill" : "auto-fit") + ", " + pattern + ")";
                }
                default:
                    return PartToString(m_MaxValue, m_MaxUnit);
            }
        }

        /// <undoc/>
        public bool Equals(GridTrackSize other)
            => m_Kind == other.m_Kind && m_MinValue == other.m_MinValue && m_MinUnit == other.m_MinUnit
               && m_MaxValue == other.m_MaxValue && m_MaxUnit == other.m_MaxUnit;

        /// <undoc/>
        public override bool Equals(object obj) => obj is GridTrackSize other && Equals(other);

        /// <undoc/>
        public override int GetHashCode()
        {
            var hashCode = (int)m_Kind;
            hashCode = (hashCode * 397) ^ m_MinValue.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)m_MinUnit;
            hashCode = (hashCode * 397) ^ m_MaxValue.GetHashCode();
            hashCode = (hashCode * 397) ^ (int)m_MaxUnit;
            return hashCode;
        }

        /// <undoc/>
        public static bool operator==(GridTrackSize a, GridTrackSize b) => a.Equals(b);
        /// <undoc/>
        public static bool operator!=(GridTrackSize a, GridTrackSize b) => !a.Equals(b);
    }
}
