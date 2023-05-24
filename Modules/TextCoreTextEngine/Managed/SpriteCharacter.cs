// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;


namespace UnityEngine.TextCore.Text
{
    /// <summary>
    /// A basic element of text representing a pictograph, image, sprite or emoji.
    /// </summary>
    [Serializable]
    public class SpriteCharacter : TextElement
    {
        /// <summary>
        /// The name of the sprite element.
        /// </summary>
        public string name
        {
            get { return m_Name; }
            set
            {
                if (value == m_Name)
                    return;

                m_Name = value;
            }
        }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        private string m_Name;

        // ********************
        // CONSTRUCTORS
        // ********************

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SpriteCharacter()
        {
            m_ElementType = TextElementType.Sprite;
        }

        /// <summary>
        /// Constructor for new sprite character.
        /// </summary>
        /// <param name="unicode">Unicode value of the sprite character.</param>
        /// <param name="glyph">Glyph used by the sprite character.</param>
        public SpriteCharacter(uint unicode, SpriteGlyph glyph)
        {
            m_ElementType = TextElementType.Sprite;

            this.unicode = unicode;
            this.glyphIndex = glyph.index;
            this.glyph = glyph;
            this.scale = 1.0f;
        }

        /// <summary>
        /// Constructor for new sprite character.
        /// </summary>
        /// <param name="unicode">Unicode value of the sprite character.</param>
        /// <param name="spriteAsset">Sprite Asset used by this sprite character.</param>
        /// <param name="glyph">Glyph used by the sprite character.</param>
        public SpriteCharacter(uint unicode, SpriteAsset spriteAsset, SpriteGlyph glyph)
        {
            m_ElementType = TextElementType.Sprite;

            this.unicode = unicode;
            this.textAsset = spriteAsset;
            this.glyph = glyph;
            this.glyphIndex = glyph.index;
            this.scale = 1.0f;
        }
    }
}
