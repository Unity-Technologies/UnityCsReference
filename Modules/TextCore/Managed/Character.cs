// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.TextCore
{
    /// <summary>
    /// A basic element of text.
    /// </summary>
    [Serializable]
    internal class Character : TextElement
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Character()
        {
            m_ElementType = TextElementType.Character;
            this.scale = 1.0f;
        }

        /// <summary>
        /// Constructor for new character
        /// </summary>
        /// <param name="unicode">Unicode value.</param>
        /// <param name="glyph">Glyph used by the character.</param>
        public Character(uint unicode, Glyph glyph)
        {
            m_ElementType = TextElementType.Character;

            this.unicode = unicode;
            this.glyph = glyph;
            this.glyphIndex = glyph.index;
            this.scale = 1.0f;
        }
    }
}
