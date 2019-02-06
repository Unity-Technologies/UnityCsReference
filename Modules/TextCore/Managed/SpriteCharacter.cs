// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;


namespace UnityEngine.TextCore
{
    /// <summary>
    /// A basic element of text representing a pictograph, image, sprite or emoji.
    /// </summary>
    [Serializable]
    internal class SpriteCharacter : TextElement
    {
        /// <summary>
        /// The name of the character sprite.
        /// </summary>
        public string name
        {
            get { return m_Name; }
            set
            {
                if (value == m_Name)
                    return;

                m_Name = value;
                m_HashCode = TextUtilities.GetHashCodeCaseInSensitive(m_Name);
            }
        }

        /// <summary>
        /// The hashcode value which is computed from the name of the sprite character.
        /// This value is read-only and updated when the name of the text sprite is changed.
        /// </summary>
        public int hashCode { get { return m_HashCode; } }


        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        private string m_Name;

        [SerializeField]
        private int m_HashCode;


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
    }
}
