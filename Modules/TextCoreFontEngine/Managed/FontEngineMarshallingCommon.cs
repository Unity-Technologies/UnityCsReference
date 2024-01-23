// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;


namespace UnityEngine.TextCore.LowLevel
{
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct OTL_Tag
    {
        public byte c0, c1, c2, c3, c4;

        public override unsafe string ToString()
        {
            var chars = stackalloc char[4];
            chars[0] = (char)c0;
            chars[1] = (char)c1;
            chars[2] = (char)c2;
            chars[3] = (char)c3;
            return new string(chars);
        }
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct OTL_Table
    {
        public OTL_Script[] scripts;
        public OTL_Feature[] features;
        public OTL_Lookup[] lookups;
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Script = {tag},  Language Count = {languages.Length}")]
    internal struct OTL_Script
    {
        public OTL_Tag tag;
        public OTL_Language[] languages;
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Language = {tag},  Feature Count = {featureIndexes.Length}")]
    internal struct OTL_Language
    {
        public OTL_Tag tag;
        public uint[] featureIndexes;
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("Feature = {tag},  Lookup Count = {lookupIndexes.Length}")]
    internal struct OTL_Feature
    {
        public OTL_Tag tag;
        public uint[] lookupIndexes;
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [DebuggerDisplay("{(OTL_LookupType)lookupType}")]
    internal struct OTL_Lookup
    {
        public uint lookupType;
        public uint lookupFlag;
        public uint markFilteringSet;
    }

    /// <summary>
    ///  Structure used for marshalling glyphs between managed and native code.
    /// </summary>
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct GlyphMarshallingStruct
    {
        /// <summary>
        /// The index of the glyph in the source font file.
        /// </summary>
        public uint index;

        /// <summary>
        /// Metrics defining the size, positioning and spacing of a glyph when doing text layout.
        /// </summary>
        public GlyphMetrics metrics;

        /// <summary>
        /// A rectangle that defines the position of a glyph within an atlas texture.
        /// </summary>
        public GlyphRect glyphRect;

        /// <summary>
        /// The relative scale of the text element. The default value is 1.0.
        /// </summary>
        public float scale;

        /// <summary>
        /// The index of the atlas texture that contains this glyph.
        /// </summary>
        public int atlasIndex;

        /// <summary>
        /// Glyph class definition type.
        /// </summary>
        public GlyphClassDefinitionType classDefinitionType;

        /// <summary>
        /// Constructor for a new glyph
        /// </summary>
        /// <param name="glyph">Glyph whose values are copied to the new glyph.</param>
        public GlyphMarshallingStruct(Glyph glyph)
        {
            this.index = glyph.index;
            this.metrics = glyph.metrics;
            this.glyphRect = glyph.glyphRect;
            this.scale = glyph.scale;
            this.atlasIndex = glyph.atlasIndex;
            this.classDefinitionType = glyph.classDefinitionType;
        }

        /// <summary>
        /// Constructor for new glyph
        /// </summary>
        /// <param name="index">The index of the glyph in the font file.</param>
        /// <param name="metrics">The metrics of the glyph.</param>
        /// <param name="glyphRect">A rectangle defining the position of the glyph in the atlas texture.</param>
        /// <param name="scale">The relative scale of the glyph.</param>
        /// <param name="atlasIndex">The index of the atlas texture that contains the glyph.</param>
        public GlyphMarshallingStruct(uint index, GlyphMetrics metrics, GlyphRect glyphRect, float scale, int atlasIndex)
        {
            this.index = index;
            this.metrics = metrics;
            this.glyphRect = glyphRect;
            this.scale = scale;
            this.atlasIndex = atlasIndex;
            this.classDefinitionType = GlyphClassDefinitionType.Undefined;
        }

        /// <summary>
        /// Constructor for new glyph
        /// </summary>
        /// <param name="index">The index of the glyph in the font file.</param>
        /// <param name="metrics">The metrics of the glyph.</param>
        /// <param name="glyphRect">A rectangle defining the position of the glyph in the atlas texture.</param>
        /// <param name="scale">The relative scale of the glyph.</param>
        /// <param name="atlasIndex">The index of the atlas texture that contains the glyph.</param>
        /// <param name="classDefinitionType">Class definition type for the glyph.</param>
        public GlyphMarshallingStruct(uint index, GlyphMetrics metrics, GlyphRect glyphRect, float scale, int atlasIndex, GlyphClassDefinitionType classDefinitionType)
        {
            this.index = index;
            this.metrics = metrics;
            this.glyphRect = glyphRect;
            this.scale = scale;
            this.atlasIndex = atlasIndex;
            this.classDefinitionType = classDefinitionType;
        }
    }
}
