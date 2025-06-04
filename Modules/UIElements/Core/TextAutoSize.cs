// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEngine.UIElements
{
    /// <summary>Defines how a text element adapts its <c>font-size</c>.</summary>
    public enum TextAutoSizeMode
    {
        /// <summary>Use the explicit <c>font-size</c>.</summary>
        None,
        /// <summary>Choose a size within the range set in <see cref="TextAutoSize"/>.</summary>
        BestFit
    }

    /// <summary>Setting controls automatic font‑size adjustment.</summary>
    public partial struct TextAutoSize : IEquatable<TextAutoSize>
    {
        /// <summary>
        /// None – keep the explicit <c>font-size</c>.
        /// BestFit – ignore <c>font-size</c> and scale text between <see cref="minSize"/> and <see cref="maxSize"/>.
        /// </summary>
        public TextAutoSizeMode mode { get; set; }

        /// <summary>Lower font‑size limit used when <see cref="mode"/> is <see cref="TextAutoSizeMode.BestFit"/>.</summary>
        public Length minSize { get; set; }

        /// <summary>Upper font‑size limit used when <see cref="mode"/> is <see cref="TextAutoSizeMode.BestFit"/>.</summary>
        public Length maxSize { get; set; }

        /// <param name="mode">Auto‑size mode.</param>
        /// <param name="minSize">Lower bound.</param>
        /// <param name="maxSize">Upper bound.</param>
        public TextAutoSize(TextAutoSizeMode mode, Length minSize, Length maxSize)
        {
            this.mode = mode;
            this.minSize = minSize;
            this.maxSize = maxSize;
        }

        /// <summary>Returns a preset with auto‑sizing disabled.</summary>
        public static TextAutoSize None()
        {
            TextAutoSize tas = default;
            tas.mode = TextAutoSizeMode.None;
            tas.maxSize = 100.0f;
            tas.minSize = 10.0f;
            return tas;
        }

        /// <undoc/>
        public bool Equals(TextAutoSize other)
        {
            return mode == other.mode &&
                minSize.Equals(other.minSize) &&
                maxSize.Equals(other.maxSize);
        }

        /// <undoc/>
        public override bool Equals(object obj)
        {
            return obj is TextAutoSize other && Equals(other);
        }

        /// <undoc/>
        public override int GetHashCode()
        {
            var hashCode = 1500536833;
            hashCode = hashCode * -1521134295 + mode.GetHashCode();
            hashCode = hashCode * -1521134295 + minSize.GetHashCode();
            hashCode = hashCode * -1521134295 + maxSize.GetHashCode();
            return hashCode;
        }

        /// <undoc/>
        public static bool operator ==(TextAutoSize left, TextAutoSize right)
        {
            return left.Equals(right);
        }

        /// <undoc/>
        public static bool operator !=(TextAutoSize left, TextAutoSize right)
        {
            return !(left == right);
        }
    }
}
