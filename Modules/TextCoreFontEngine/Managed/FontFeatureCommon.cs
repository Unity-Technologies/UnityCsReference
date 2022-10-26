// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Internal;
using UnityEngine.Scripting;


namespace UnityEngine.TextCore.LowLevel
{
    /// <summary>
    /// The OpenType Layout Tables.
    /// </summary>
    internal enum OTL_TableType
    {
        /// <summary>
        /// Baseline Table.
        /// Provides information used to align glyphs of different scripts and sizes in a line of text,
        /// whether the glyphs are in the same font or in different fonts.
        /// </summary>
        BASE = 0x01000,

        /// <summary>
        /// The Glyph Definition (GDEF) table provides various glyph properties used in OpenType Layout processing.
        /// </summary>
        GDEF = 0x02000,

        /// <summary>
        /// The Glyph Positioning table (GPOS) provides precise control over glyph placement for sophisticated text
        /// layout and rendering in each script and language system that a font supports.
        /// </summary>
        GPOS = 0x04000,

        /// <summary>
        /// The Glyph Substitution (GSUB) table provides data for substition of glyphs for appropriate rendering of scripts,
        /// such as cursively-connecting forms in Arabic script, or for advanced typographic effects, such as ligatures.
        /// </summary>
        GSUB = 0x08000,

        /// <summary>
        /// The Justification table (JSTF) provides font developers with additional control over glyph substitution
        /// and positioning in justified text.
        /// </summary>
        JSTF = 0x10000,

        /// <summary>
        /// The Mathematical Typesetting Table.
        /// Provides font-specific information necessary for math formula layout.
        /// </summary>
        MATH = 0x20000,
    }

    /// <summary>
    /// The Lookup tables referenced in OpenType Layout Tables.
    /// </summary>
    internal enum OTL_LookupType
    {
        // =============================================
        // GSUB - https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#gsub-header
        // =============================================

        /// <summary>
        /// Single substitution subtable tells a client to replace a single glyph with another glyph.
        /// </summary>
        Single_Substitution = OTL_TableType.GSUB | 1,

        /// <summary>
        /// A Multiple Substitution subtable replaces a single glyph with more than one glyph,
        /// as when multiple glyphs replace a single ligature. 
        /// </summary>
        Multiple_Substitution = OTL_TableType.GSUB | 2,

        /// <summary>
        /// An Alternate Substitution subtable identifies any number of aesthetic alternatives
        /// from which a user can choose a glyph variant to replace the input glyph. For example, if a font contains
        /// four variants of the ampersand symbol, the 'cmap' table will specify the index of one of the four glyphs
        /// as the default glyph index, and an AlternateSubst subtable will list the indices of the other three
        /// glyphs as alternatives.
        /// </summary>
        Alternate_Substitution = OTL_TableType.GSUB | 3,

        /// <summary>
        /// A Ligature Substitution subtable identifies ligature substitutions where a single glyph
        /// replaces multiple glyphs.
        /// </summary>
        Ligature_Substitution = OTL_TableType.GSUB | 4,

        /// <summary>
        /// A Contextual Substitution subtable describes glyph substitutions in context that replace one or more
        /// glyphs within a certain pattern of glyphs.
        /// </summary>
        Contextual_Substitution = OTL_TableType.GSUB | 5,

        /// <summary>
        /// A Chained Contexts Substitution subtable describes glyph substitutions in context with an ability to
        /// look back and/or look ahead in the sequence of glyphs.
        /// </summary>
        Chaining_Contextual_Substitution = OTL_TableType.GSUB | 6,

        /// <summary>
        /// This lookup type provides a way to access lookup subtables within the GSUB table using 32-bit offsets.
        /// This is needed if the total size of the subtables exceeds the 16-bit limits of the various other offsets in the GSUB table.
        /// </summary>
        Extension_Substitution = OTL_TableType.GSUB | 7,

        /// <summary>
        /// Reverse Chaining Contextual subtable Single Substitution describes single-glyph substitutions
        /// in context with an ability to look back and/or look ahead in the sequence of glyphs.
        /// The major difference between this and other lookup types is that processing of input glyph sequence goes from end to start.
        /// </summary>
        Reverse_Chaining_Contextual_Single_Substitution = OTL_TableType.GSUB | 8,

        // =============================================
        // GPOS - https://docs.microsoft.com/en-us/typography/opentype/spec/gpos#gpos-header
        // =============================================

        /// <summary>
        /// A single adjustment positioning subtable is used to adjust the placement or advance of a single glyph, such as a subscript
        /// or superscript. In addition, a SinglePos subtable is commonly used to implement lookup data for contextual positioning.
        /// </summary>
        Single_Adjustment = OTL_TableType.GPOS | 1,

        /// <summary>
        /// A pair adjustment positioning subtable is used to adjust the placement or advances of two glyphs in relation to one another.
        /// </summary>
        Pair_Adjustment = OTL_TableType.GPOS | 2,

        /// <summary>
        /// A cursive attachment positioning subtable can describe how to connect cursive fonts so that adjacent glyphs join.
        /// /// </summary>
        Cursive_Attachment = OTL_TableType.GPOS | 3,

        /// <summary>
        /// The Mark To Base attachment subtable is used to position combining mark glyphs with respect to base glyphs.
        /// For example, the Arabic, Hebrew, and Thai scripts combine vowels, diacritical marks, and tone marks with base glyphs.
        /// </summary>
        Mark_to_Base_Attachment = OTL_TableType.GPOS | 4,

        /// <summary>
        /// The Mark To Ligature attachment subtable is used to position combining mark glyphs with respect to ligature base glyphs. 
        /// </summary>
        Mark_to_Ligature_Attachment = OTL_TableType.GPOS | 5,

        /// <summary>
        ///  Mark To Mark attachment defines the position of one mark relative to another mark as when, for example, positioning
        ///  tone marks with respect to vowel diacritical marks in Vietnamese.
        /// </summary>
        Mark_to_Mark_Attachment = OTL_TableType.GPOS | 6,

        /// <summary>
        /// A Contextual Positioning subtable describes glyph positioning in context so a text-processing client can adjust the
        /// position of one or more glyphs within a certain pattern of glyphs.
        /// </summary>
        Contextual_Positioning = OTL_TableType.GPOS | 7,

        /// <summary>
        /// A Chained Contexts Positioning subtable describes glyph positioning in context with an ability to look back and/or
        /// look ahead in the sequence of glyphs. 
        /// </summary>
        Chaining_Contextual_Positioning = OTL_TableType.GPOS | 8,

        /// <summary>
        /// This lookup type provides a way to access lookup subtables within the GPOS table using 32-bit offsets.
        /// This is needed if the total size of the subtables exceeds the 16-bit limits of the various other offsets in the GPOS table.
        /// </summary>
        Extension_Positioning = OTL_TableType.GPOS | 9,
    }

    [Flags]
    public enum FontFeatureLookupFlags
    {
        None                        = 0x000,
        //RightToLeft               = 0x001,
        //IgnoreBaseGlyphs          = 0x002,
        IgnoreLigatures             = 0x004,
        //IgnoreMarks               = 0x008,
        //UseMarkFilteringSet       = 0x010,
        IgnoreSpacingAdjustments    = 0x100,
    }

    [Serializable]
    internal struct OpenTypeLayoutTable
    {
        public List<OpenTypeLayoutScript> scripts;
        public List<OpenTypeLayoutFeature> features;
        [SerializeReference] public List<OpenTypeLayoutLookup> lookups;
    }

    [Serializable]
    [DebuggerDisplay("Script = {tag},  Language Count = {languages.Count}")]
    internal struct OpenTypeLayoutScript
    {
        public string tag;
        public List<OpenTypeLayoutLanguage> languages;
    }

    [Serializable]
    [DebuggerDisplay("Language = {tag},  Feature Count = {featureIndexes.Length}")]
    internal struct OpenTypeLayoutLanguage
    {
        public string tag;
        public uint[] featureIndexes;
        //public List<OpenTypeLayoutFeature> features;
    }

    [Serializable]
    [DebuggerDisplay("Feature = {tag},  Lookup Count = {lookupIndexes.Length}")]
    internal struct OpenTypeLayoutFeature
    {
        public string tag;
        public uint[] lookupIndexes;
        //public List<OpenTypeLayoutLookup> lookups;
    }

    internal struct OpenTypeFeature { } // Required to prevent compilation errors on TMP 3.20.0 Preview 3.

    [Serializable]
    //[DebuggerDisplay("{(OTL_LookupType)lookupType}")]
    internal abstract class OpenTypeLayoutLookup
    {
        public uint lookupType;
        public uint lookupFlag;
        public uint markFilteringSet;

        public abstract void InitializeLookupDictionary();
        public virtual void UpdateRecords(int lookupIndex, uint glyphIndex) { }
        public virtual void UpdateRecords(int lookupIndex, uint glyphIndex, float emScale) { }
        public virtual void UpdateRecords(int lookupIndex, List<uint> glyphIndexes) { }
        public virtual void UpdateRecords(int lookupIndex, List<uint> glyphIndexes, float emScale) { }

        public abstract void ClearRecords();
    }

    //[Serializable]
    //internal class OpenTypeLayoutLookupSubTable
    //{

    //}

    /// <summary>
    /// The values used to adjust the position of a glyph or set of glyphs.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct GlyphValueRecord : IEquatable<GlyphValueRecord>
    {
        /// <summary>
        /// The positional adjustment that affects the horizontal bearing X of the glyph.
        /// </summary>
        public float xPlacement { get { return m_XPlacement; } set { m_XPlacement = value; } }

        /// <summary>
        /// The positional adjustment that affects the horizontal bearing Y of the glyph.
        /// </summary>
        public float yPlacement { get { return m_YPlacement; } set { m_YPlacement = value; } }

        /// <summary>
        /// The positional adjustment that affects the horizontal advance of the glyph.
        /// </summary>
        public float xAdvance   { get { return m_XAdvance; } set { m_XAdvance = value; } }

        /// <summary>
        /// The positional adjustment that affects the vertical advance of the glyph.
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
        /// <param name="xPlacement">The positional adjustment that affects the horizontal bearing X of the glyph.</param>
        /// <param name="yPlacement">The positional adjustment that affects the horizontal bearing Y of the glyph.</param>
        /// <param name="xAdvance">The positional adjustment that affects the horizontal advance of the glyph.</param>
        /// <param name="yAdvance">The positional adjustment that affects the vertical advance of the glyph.</param>
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

        [ExcludeFromDocs]
        public static GlyphValueRecord operator *(GlyphValueRecord a, float emScale)
        {
            a.m_XPlacement = a.xPlacement * emScale;
            a.m_YPlacement = a.yPlacement * emScale;
            a.m_XAdvance = a.xAdvance * emScale;
            a.m_YAdvance = a.yAdvance * emScale;

            return a;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public bool Equals(GlyphValueRecord other)
        {
            return base.Equals(other);
        }

        public static bool operator==(GlyphValueRecord lhs, GlyphValueRecord rhs)
        {
            return lhs.m_XPlacement == rhs.m_XPlacement &&
                lhs.m_YPlacement == rhs.m_YPlacement &&
                lhs.m_XAdvance == rhs.m_XAdvance &&
                lhs.m_YAdvance == rhs.m_YAdvance;
        }

        public static bool operator!=(GlyphValueRecord lhs, GlyphValueRecord rhs)
        {
            return !(lhs == rhs);
        }
    }

    /// <summary>
    /// The positional adjustment values of a glyph.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct GlyphAdjustmentRecord : IEquatable<GlyphAdjustmentRecord>
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

        [ExcludeFromDocs]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        [ExcludeFromDocs]
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        [ExcludeFromDocs]
        public bool Equals(GlyphAdjustmentRecord other)
        {
            return base.Equals(other);
        }

        [ExcludeFromDocs]
        public static bool operator ==(GlyphAdjustmentRecord lhs, GlyphAdjustmentRecord rhs)
        {
            return lhs.m_GlyphIndex == rhs.m_GlyphIndex &&
                lhs.m_GlyphValueRecord == rhs.m_GlyphValueRecord;
        }

        [ExcludeFromDocs]
        public static bool operator !=(GlyphAdjustmentRecord lhs, GlyphAdjustmentRecord rhs)
        {
            return !(lhs == rhs);
        }
    }

    /// <summary>
    /// The positional adjustment values for a pair of glyphs.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("First glyphIndex = {m_FirstAdjustmentRecord.m_GlyphIndex},  Second glyphIndex = {m_SecondAdjustmentRecord.m_GlyphIndex}")]
    public struct GlyphPairAdjustmentRecord : IEquatable<GlyphPairAdjustmentRecord>
    {
        /// <summary>
        /// Contains the positional adjustment values for the first glyph.
        /// </summary>
        public GlyphAdjustmentRecord firstAdjustmentRecord { get { return m_FirstAdjustmentRecord; } set { m_FirstAdjustmentRecord = value; } }

        /// <summary>
        /// Contains the positional adjustment values for the second glyph.
        /// </summary>
        public GlyphAdjustmentRecord secondAdjustmentRecord { get { return m_SecondAdjustmentRecord; } set { m_SecondAdjustmentRecord = value; } }

        /// <summary>
        ///
        /// </summary>
        public FontFeatureLookupFlags featureLookupFlags { get { return m_FeatureLookupFlags; } set { m_FeatureLookupFlags = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        [NativeName("firstAdjustmentRecord")]
        private GlyphAdjustmentRecord m_FirstAdjustmentRecord;

        [SerializeField]
        [NativeName("secondAdjustmentRecord")]
        private GlyphAdjustmentRecord m_SecondAdjustmentRecord;

        [SerializeField]
        private FontFeatureLookupFlags m_FeatureLookupFlags;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="firstAdjustmentRecord">First glyph adjustment record.</param>
        /// <param name="secondAdjustmentRecord">Second glyph adjustment record.</param>
        public GlyphPairAdjustmentRecord(GlyphAdjustmentRecord firstAdjustmentRecord, GlyphAdjustmentRecord secondAdjustmentRecord)
        {
            m_FirstAdjustmentRecord = firstAdjustmentRecord;
            m_SecondAdjustmentRecord = secondAdjustmentRecord;
            m_FeatureLookupFlags = FontFeatureLookupFlags.None;
        }

        [ExcludeFromDocs]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        [ExcludeFromDocs]
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        [ExcludeFromDocs]
        public bool Equals(GlyphPairAdjustmentRecord other)
        {
            return base.Equals(other);
        }

        [ExcludeFromDocs]
        public static bool operator ==(GlyphPairAdjustmentRecord lhs, GlyphPairAdjustmentRecord rhs)
        {
            return lhs.m_FirstAdjustmentRecord == rhs.m_FirstAdjustmentRecord &&
                lhs.m_SecondAdjustmentRecord == rhs.m_SecondAdjustmentRecord;
        }

        [ExcludeFromDocs]
        public static bool operator !=(GlyphPairAdjustmentRecord lhs, GlyphPairAdjustmentRecord rhs)
        {
            return !(lhs == rhs);
        }
    }
}
