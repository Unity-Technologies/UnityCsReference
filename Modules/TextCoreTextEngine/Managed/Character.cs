// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;


namespace UnityEngine.TextCore.Text
{
    /// <summary>
    /// A basic element of text.
    /// </summary>
    [Serializable]
    public class Character : TextElement
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
        /// <param name="glyph">Glyph</param>
        public Character(uint unicode, Glyph glyph)
        {
            m_ElementType = TextElementType.Character;

            this.unicode = unicode;
            this.textAsset = null;
            this.glyph = glyph;
            this.glyphIndex = glyph.index;
            this.scale = 1.0f;
        }

        /// <summary>
        /// Constructor for new character
        /// </summary>
        /// <param name="unicode">Unicode value.</param>
        /// <param name="fontAsset">The font asset to which this character belongs.</param>
        /// <param name="glyph">Glyph</param>
        public Character(uint unicode, FontAsset fontAsset, Glyph glyph)
        {
            m_ElementType = TextElementType.Character;

            this.unicode = unicode;
            this.textAsset = fontAsset;
            this.glyph = glyph;
            this.glyphIndex = glyph.index;
            this.scale = 1.0f;
        }

        /// <summary>
        /// Constructor for new character
        /// </summary>
        /// <param name="unicode">Unicode value.</param>
        /// <param name="glyphIndex">Glyph index.</param>
        internal Character(uint unicode, uint glyphIndex)
        {
            m_ElementType = TextElementType.Character;

            this.unicode = unicode;
            this.textAsset = null;
            this.glyph = null;
            this.glyphIndex = glyphIndex;
            this.scale = 1.0f;
        }
    }
}
