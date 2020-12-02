using System;
using System.Collections.Generic;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Describes a <see cref="VisualElement"/> font.
    /// </summary>
    public struct FontDefinition : IEquatable<FontDefinition>
    {
        private Font m_Font;
        /// <summary>
        /// Font to use to display text. You cannot set this and <see cref="FontDefinition.fontAsset"/> at the same time.
        /// </summary>
        public Font font
        {
            get { return m_Font; }
            set
            {
                if (value != null && fontAsset != null)
                    throw new InvalidOperationException("Cannot set both Font and FontAsset on FontDefinition");
                m_Font = value;
            }
        }

        private Object m_FontAsset;
        /// <summary>
        /// SDF font to use to display text. You cannot set this and <see cref="FontDefinition.font"/> at the same time.
        /// </summary>
        public Object fontAsset
        {
            get { return m_FontAsset; }
            set
            {
                if (value != null && font != null)
                    throw new InvalidOperationException("Cannot set both Font and FontAsset on FontDefinition");
                m_FontAsset = value;
            }
        }


        /// <summary>
        /// Create a FontDefinition from <see cref="Font"/>.
        /// </summary>
        /// <param name="f">The font to use to display text.</param>
        /// <returns>A new FontDefinition object.</returns>
        public static FontDefinition FromFont(Font f)
        {
            return new FontDefinition() { m_Font = f };
        }

        /// <summary>
        /// Create a FontDefinition from <see cref="FontAsset"/>.
        /// </summary>
        /// <param name="f">The SDF font to use to display text.</param>
        /// <returns>A new FontDefinition object.</returns>
        public static FontDefinition FromSDFFont(Object f)
        {
            if (TextDelegates.IsFontAssetSafe(f))
                return new FontDefinition() { fontAsset = f };

            Debug.LogError("Cannot create a FontDefinition, the type must be FontAsset");
            return default;
        }

        internal static FontDefinition FromObject(object obj)
        {
            var font = obj as Font;
            if (font != null)
                return FromFont(font);

            var fontAsset = obj as Object;
            if (fontAsset != null)
                return FromSDFFont(fontAsset);

            return default;
        }

        internal bool IsEmpty()
        {
            return m_Font == null && m_FontAsset == null;
        }

        public override string ToString()
        {
            if (font != null)
                return $"{font}";
            return $"{fontAsset}";
        }

        public bool Equals(FontDefinition other)
        {
            return Equals(m_Font, other.m_Font) && Equals(m_FontAsset, other.m_FontAsset);
        }

        public override bool Equals(object obj)
        {
            return obj is FontDefinition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((m_Font != null ? m_Font.GetHashCode() : 0) * 397) ^ (m_FontAsset != null ? m_FontAsset.GetHashCode() : 0);
            }
        }

        public static bool operator==(FontDefinition left, FontDefinition right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(FontDefinition left, FontDefinition right)
        {
            return !left.Equals(right);
        }
    }
}
