// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Diagnostics;
using UnityEngine;

namespace UnityEngine.TextCore.Text
{
    internal enum TextProcessingElementType
    {
        Undefined = 0x0,
        TextCharacterElement = 0x1,
        TextMarkupElement = 0x2
    }

    internal struct MarkupAttribute
    {
        /// <summary>
        /// The hash code of the name of the Markup attribute.
        /// </summary>
        public int NameHashCode
        {
            get { return m_NameHashCode; }
            set { m_NameHashCode = value; }
        }

        /// <summary>
        /// The hash code of the value of the Markup attribute.
        /// </summary>
        public int ValueHashCode
        {
            get { return m_ValueHashCode; }
            set { m_ValueHashCode = value; }
        }

        /// <summary>
        /// The index of the value of the Markup attribute in the text backing buffer.
        /// </summary>
        public int ValueStartIndex
        {
            get { return m_ValueStartIndex; }
            set { m_ValueStartIndex = value; }
        }

        /// <summary>
        /// The length of the value of the Markup attribute in the text backing buffer.
        /// </summary>
        public int ValueLength
        {
            get { return m_ValueLength; }
            set { m_ValueLength = value; }
        }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        int m_NameHashCode;
        int m_ValueHashCode;
        int m_ValueStartIndex;
        int m_ValueLength;
    }

    internal struct MarkupElement
    {
        /// <summary>
        /// The hash code of the name of the markup element.
        /// </summary>
        public int NameHashCode
        {
            get
            {
                return m_Attributes == null ? 0 : m_Attributes[0].NameHashCode;
            }
            set
            {
                if (m_Attributes == null)
                    m_Attributes = new MarkupAttribute[8];

                m_Attributes[0].NameHashCode = value;
            }
        }

        /// <summary>
        /// The hash code of the value of the markup element.
        /// </summary>
        public int ValueHashCode
        {
            get { return m_Attributes == null ? 0 : m_Attributes[0].ValueHashCode; }
            set { m_Attributes[0].ValueHashCode = value; }
        }

        /// <summary>
        /// The index of the value of the markup element in the text backing buffer.
        /// </summary>
        public int ValueStartIndex
        {
            get { return m_Attributes == null ? 0 : m_Attributes[0].ValueStartIndex; }
            set { m_Attributes[0].ValueStartIndex = value; }
        }

        /// <summary>
        /// The length of the value of the markup element in the text backing buffer.
        /// </summary>
        public int ValueLength
        {
            get { return m_Attributes == null ? 0 : m_Attributes[0].ValueLength; }
            set { m_Attributes[0].ValueLength = value; }
        }

        /// <summary>
        ///
        /// </summary>
        public MarkupAttribute[] Attributes
        {
            get { return m_Attributes; }
            set { m_Attributes = value; }
        }

        /// <summary>
        /// Constructor for a new Markup Element
        /// </summary>
        /// <param name="nameHashCode"></param>
        public MarkupElement(int nameHashCode, int startIndex, int length)
        {
            m_Attributes = new MarkupAttribute[8];

            m_Attributes[0].NameHashCode = nameHashCode;
            m_Attributes[0].ValueStartIndex = startIndex;
            m_Attributes[0].ValueLength = length;
        }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        private MarkupAttribute[] m_Attributes;
    }

    // [DebuggerDisplay("{DebuggerDisplay()}")]
    // internal struct TextProcessingElement
    // {
    //     public TextProcessingElementType ElementType
    //     {
    //         get { return m_ElementType; }
    //         set { m_ElementType = value; }
    //     }
    //
    //     public int StartIndex
    //     {
    //         get { return m_StartIndex; }
    //         set { m_StartIndex = value; }
    //     }
    //
    //     public int Length
    //     {
    //         get { return m_Length; }
    //         set { m_Length = value; }
    //     }
    //
    //     public CharacterElement CharacterElement
    //     {
    //         get { return m_CharacterElement; }
    //     }
    //
    //     public MarkupElement MarkupElement
    //     {
    //         get { return m_MarkupElement; }
    //         set { m_MarkupElement = value; }
    //     }
    //
    //     public TextProcessingElement(TextProcessingElementType elementType, int startIndex, int length)
    //     {
    //         m_ElementType = elementType;
    //         m_StartIndex = startIndex;
    //         m_Length = length;
    //
    //         m_CharacterElement = new CharacterElement();
    //         m_MarkupElement = new MarkupElement();
    //     }
    //
    //     public TextProcessingElement(TextElement textElement, int startIndex, int length)
    //     {
    //         m_ElementType = TextProcessingElementType.TextCharacterElement;
    //         m_StartIndex = startIndex;
    //         m_Length = length;
    //
    //         m_CharacterElement = new CharacterElement(textElement);
    //         m_MarkupElement = new MarkupElement();
    //     }
    //
    //     public TextProcessingElement(CharacterElement characterElement, int startIndex, int length)
    //     {
    //         m_ElementType = TextProcessingElementType.TextCharacterElement;
    //         m_StartIndex = startIndex;
    //         m_Length = length;
    //
    //         m_CharacterElement = characterElement;
    //         m_MarkupElement = new MarkupElement();
    //     }
    //
    //     public TextProcessingElement(MarkupElement markupElement)
    //     {
    //         m_ElementType = TextProcessingElementType.TextMarkupElement;
    //         m_StartIndex = markupElement.ValueStartIndex;
    //         m_Length = markupElement.ValueLength;
    //
    //         m_CharacterElement = new CharacterElement();
    //         m_MarkupElement = markupElement;
    //     }
    //
    //     public static TextProcessingElement Undefined => new TextProcessingElement() { ElementType = TextProcessingElementType.Undefined };
    //
    //
    //     private string DebuggerDisplay()
    //     {
    //         return m_ElementType == TextProcessingElementType.TextCharacterElement ? $"Unicode ({m_CharacterElement.Unicode})   '{(char)m_CharacterElement.Unicode}' " : $"Markup = {(MarkupTag)m_MarkupElement.NameHashCode}";
    //     }
    //
    //     // =============================================
    //     // Private backing fields for public properties.
    //     // =============================================
    //
    //     TextProcessingElementType m_ElementType;
    //     int m_StartIndex;
    //     int m_Length;
    //
    //     CharacterElement m_CharacterElement;
    //     MarkupElement m_MarkupElement;
    // }
}
