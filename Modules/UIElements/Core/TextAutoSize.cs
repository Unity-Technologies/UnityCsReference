// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
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
    [StructLayout(LayoutKind.Sequential)]
    public partial struct TextAutoSize : IEquatable<TextAutoSize>
    {
        private Length m_MinSize;
        private Length m_MaxSize;
        private TextAutoSizeMode m_Mode;

        /// <summary>
        /// None – keep the explicit <c>font-size</c>.
        /// BestFit – ignore <c>font-size</c> and scale text between <see cref="minSize"/> and <see cref="maxSize"/>.
        /// </summary>
        public TextAutoSizeMode mode
        {
            readonly get => m_Mode;
            set => m_Mode = value;
        }

        /// <summary>Lower font‑size limit used when <see cref="mode"/> is <see cref="TextAutoSizeMode.BestFit"/>.</summary>
        public Length minSize
        {
            readonly get => m_MinSize;
            set => m_MinSize = value;
        }

        /// <summary>Upper font‑size limit used when <see cref="mode"/> is <see cref="TextAutoSizeMode.BestFit"/>.</summary>
        public Length maxSize
        {
            readonly get => m_MaxSize;
            set => m_MaxSize = value;
        }

        /// <param name="mode">Auto‑size mode.</param>
        /// <param name="minSize">Lower bound.</param>
        /// <param name="maxSize">Upper bound.</param>
        public TextAutoSize(TextAutoSizeMode mode, Length minSize, Length maxSize)
        {
            m_Mode = mode;
            m_MinSize = minSize;
            m_MaxSize = maxSize;
        }

        /// <summary>Returns a preset with auto‑sizing disabled.</summary>
        public static TextAutoSize None() => new(TextAutoSizeMode.None, 10.0f, 100.0f);

        /// <undoc/>
        public bool Equals(TextAutoSize other)
        {
            return m_Mode == other.m_Mode &&
                   m_MinSize.Equals(other.m_MinSize) &&
                   m_MaxSize.Equals(other.m_MaxSize);
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
            hashCode = hashCode * -1521134295 + m_Mode.GetHashCode();
            hashCode = hashCode * -1521134295 + m_MinSize.GetHashCode();
            hashCode = hashCode * -1521134295 + m_MaxSize.GetHashCode();
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
