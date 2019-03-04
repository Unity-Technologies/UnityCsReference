// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;


namespace UnityEngine.TextCore.LowLevel
{
    /// <summary>
    /// The OpenType Layout Tables.
    /// </summary>
    internal enum OTFLayoutTableType
    {
        BASE = 0x01000,
        GDEF = 0x02000,
        GPOS = 0x04000,
        GSUB = 0x08000,
        JSTF = 0x10000,
        MATH = 0x20000,
    }

    /// <summary>
    /// The Lookup tables referenced in OpenType Layout Tables.
    /// </summary>
    internal enum OTFLookupTableType
    {
        // GPOS
        Single_Adjustment                               = OTFLayoutTableType.GPOS | 1,
        Pair_Adjustment                                 = OTFLayoutTableType.GPOS | 2,
        Cursive_Attachment                              = OTFLayoutTableType.GPOS | 3,
        Mark_to_Base_Attachment                         = OTFLayoutTableType.GPOS | 4,
        Mark_to_Ligature_Attachment                     = OTFLayoutTableType.GPOS | 5,
        Mark_to_Mark_Attachment                         = OTFLayoutTableType.GPOS | 6,
        Contextual_Positioning                          = OTFLayoutTableType.GPOS | 7,
        Chaining_Contextual_Positioning                 = OTFLayoutTableType.GPOS | 8,
        Extension_Positioning                           = OTFLayoutTableType.GPOS | 9,

        // GSUB
        Single_Substitution                             = OTFLayoutTableType.GSUB | 1,
        Multiple_Substitution                           = OTFLayoutTableType.GSUB | 2,
        Alternate_Substitution                          = OTFLayoutTableType.GSUB | 3,
        Ligature_Substitution                           = OTFLayoutTableType.GSUB | 4,
        Contextual_Substitution                         = OTFLayoutTableType.GSUB | 5,
        Chaining_Contextual_Substitution                = OTFLayoutTableType.GSUB | 6,
        Extension_Substitution                          = OTFLayoutTableType.GSUB | 7,
        Reverse_Chaining_Contextual_Single_Substitution = OTFLayoutTableType.GSUB | 8,
    }

    /// <summary>
    /// The values used to adjust the position of a glyph or set of glyphs.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct GlyphValueRecord
    {
        /// <summary>
        /// The positional adjustment affecting the horizontal bearing X of the glyph.
        /// </summary>
        public float xPlacement { get { return m_XPlacement; } set { m_XPlacement = value; } }

        /// <summary>
        /// The positional adjustment affecting the horizontal bearing Y of the glyph.
        /// </summary>
        public float yPlacement { get { return m_YPlacement; } set { m_YPlacement = value; } }

        /// <summary>
        /// The positional adjustment affecting the horizontal advance of the glyph.
        /// </summary>
        public float xAdvance   { get { return m_XAdvance; } set { m_XAdvance = value; } }

        /// <summary>
        /// The positional adjustment affecting the vertical advance of the glyph.
        /// </summary>
        public float yAdvance   { get { return m_YAdvance; } set { m_YAdvance = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        [NativeName("xPlacement")]
        private float m_XPlacement;

        [SerializeField]
        [NativeName("yPlacement")]
        private float m_YPlacement;

        [SerializeField]
        [NativeName("xAdvance")]
        private float m_XAdvance;

        [SerializeField]
        [NativeName("yAdvance")]
        private float m_YAdvance;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="xPlacement">The positional adjustment affecting the horizontal bearing X of the glyph.</param>
        /// <param name="yPlacement">The positional adjustment affecting the horizontal bearing Y of the glyph.</param>
        /// <param name="xAdvance">The positional adjustment affecting the horizontal advance of the glyph.</param>
        /// <param name="yAdvance">The positional adjustment affecting the vertical advance of the glyph.</param>
        public GlyphValueRecord(float xPlacement, float yPlacement, float xAdvance, float yAdvance)
        {
            m_XPlacement = xPlacement;
            m_YPlacement = yPlacement;
            m_XAdvance = xAdvance;
            m_YAdvance = yAdvance;
        }

        public static GlyphValueRecord operator+(GlyphValueRecord a, GlyphValueRecord b)
        {
            GlyphValueRecord c;
            c.m_XPlacement = a.xPlacement + b.xPlacement;
            c.m_YPlacement = a.yPlacement + b.yPlacement;
            c.m_XAdvance = a.xAdvance + b.xAdvance;
            c.m_YAdvance = a.yAdvance + b.yAdvance;

            return c;
        }
    }

    /// <summary>
    /// The positional adjustment values of a glyph.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct GlyphAdjustmentRecord
    {
        /// <summary>
        /// The index of the glyph in the source font file.
        /// </summary>
        public uint glyphIndex { get { return m_GlyphIndex; } set { m_GlyphIndex = value; } }

        /// <summary>
        /// The GlyphValueRecord contains the positional adjustments of the glyph.
        /// </summary>
        public GlyphValueRecord glyphValueRecord { get { return m_GlyphValueRecord; } set { m_GlyphValueRecord = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        [NativeName("glyphIndex")]
        private uint m_GlyphIndex;

        [SerializeField]
        [NativeName("glyphValueRecord")]
        private GlyphValueRecord m_GlyphValueRecord;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="glyphIndex">The index of the glyph in the source font file.</param>
        /// <param name="glyphValueRecord">The GlyphValueRecord contains the positional adjustments of the glyph.</param>
        public GlyphAdjustmentRecord(uint glyphIndex, GlyphValueRecord glyphValueRecord)
        {
            m_GlyphIndex = glyphIndex;
            m_GlyphValueRecord = glyphValueRecord;
        }
    }

    /// <summary>
    /// The positional adjustment values for a pair of glyphs.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct GlyphPairAdjustmentRecord
    {
        /// <summary>
        /// Contains the positional adjustment values for the first glyph.
        /// </summary>
        public GlyphAdjustmentRecord firstAdjustmentRecord { get { return m_FirstAdjustmentRecord; } set { m_FirstAdjustmentRecord = value; } }

        /// <summary>
        /// Contains the positional adjustment values for the second glyph.
        /// </summary>
        public GlyphAdjustmentRecord secondAdjustmentRecord { get { return m_SecondAdjustmentRecord; } set { m_SecondAdjustmentRecord = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        [NativeName("firstAdjustmentRecord")]
        private GlyphAdjustmentRecord m_FirstAdjustmentRecord;

        [SerializeField]
        [NativeName("secondAdjustmentRecord")]
        private GlyphAdjustmentRecord m_SecondAdjustmentRecord;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="firstAdjustmentRecord">First glyph adjustment record.</param>
        /// <param name="secondAdjustmentRecord">Second glyph adjustment record.</param>
        public GlyphPairAdjustmentRecord(GlyphAdjustmentRecord firstAdjustmentRecord, GlyphAdjustmentRecord secondAdjustmentRecord)
        {
            m_FirstAdjustmentRecord = firstAdjustmentRecord;
            m_SecondAdjustmentRecord = secondAdjustmentRecord;
        }
    }
}
