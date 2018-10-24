// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;


namespace UnityEngine.TextCore.LowLevel
{
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
        }
    }
}
