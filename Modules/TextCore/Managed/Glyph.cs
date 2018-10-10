// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.TextCore.LowLevel;


namespace UnityEngine.TextCore
{
    /// <summary>
    /// A rectangle that defines the position of a glyph within an atlas texture.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct GlyphRect : IEquatable<GlyphRect>
    {
        /// <summary>
        /// The x position of the glyph in the font atlas texture.
        /// </summary>
        public int x { get { return m_X; } set { m_X = value; } }

        /// <summary>
        /// The y position of the glyph in the font atlas texture.
        /// </summary>
        public int y { get { return m_Y; } set { m_Y = value; } }

        /// <summary>
        /// The width of the glyph.
        /// </summary>
        public int width { get { return m_Width; } set { m_Width = value; } }

        /// <summary>
        /// The height of the glyph.
        /// </summary>
        public int height { get { return m_Height; } set { m_Height = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        [NativeName("x")]
        private int m_X;

        [SerializeField]
        [NativeName("y")]
        private int m_Y;

        [SerializeField]
        [NativeName("width")]
        private int m_Width;

        [SerializeField]
        [NativeName("height")]
        private int m_Height;

        static readonly GlyphRect s_ZeroGlyphRect = new GlyphRect(0, 0, 0, 0);

        /// <summary>
        /// A GlyphRect with all values set to zero. Shorthand for writing GlyphRect(0, 0, 0, 0).
        /// </summary>
        public static GlyphRect zero { get { return s_ZeroGlyphRect; } }

        /// <summary>
        /// Constructor for new GlyphRect.
        /// </summary>
        /// <param name="x">The x position of the glyph in the atlas texture.</param>
        /// <param name="y">The y position of the glyph in the atlas texture.</param>
        /// <param name="width">The width of the glyph.</param>
        /// <param name="height">The height of the glyph.</param>
        public GlyphRect(int x, int y, int width, int height)
        {
            m_X = x;
            m_Y = y;
            m_Width = width;
            m_Height = height;
        }

        /// <summary>
        /// Construct new GlyphRect from a Rect.
        /// </summary>
        /// <param name="rect">The Rect used to construct the new GlyphRect.</param>
        public GlyphRect(Rect rect)
        {
            m_X = (int)rect.x;
            m_Y = (int)rect.y;
            m_Width = (int)rect.width;
            m_Height = (int)rect.height;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public bool Equals(GlyphRect other)
        {
            return base.Equals(other);
        }

        public static bool operator==(GlyphRect lhs, GlyphRect rhs)
        {
            return lhs.x == rhs.x &&
                lhs.y == rhs.y &&
                lhs.width == rhs.width &&
                lhs.height == rhs.height;
        }

        public static bool operator!=(GlyphRect lhs, GlyphRect rhs)
        {
            return !(lhs == rhs);
        }
    }

    /// <summary>
    /// A set of values that define the size, position and spacing of a glyph when performing text layout.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct GlyphMetrics : IEquatable<GlyphMetrics>
    {
        /// <summary>
        /// The width of the glyph.
        /// </summary>
        public float width { get { return m_Width; } set { m_Width = value; } }

        /// <summary>
        /// The height of the glyph.
        /// </summary>
        public float height { get { return m_Height; } set { m_Height = value; } }

        /// <summary>
        /// The horizontal distance from the current drawing position (origin) relative to the element's left bounding box edge (bbox).
        /// </summary>
        public float horizontalBearingX { get { return m_HorizontalBearingX; } set { m_HorizontalBearingX = value; } }

        /// <summary>
        /// The vertical distance from the current baseline relative to the element's top bounding box edge (bbox).
        /// </summary>
        public float horizontalBearingY { get { return m_HorizontalBearingY; } set { m_HorizontalBearingY = value; } }

        /// <summary>
        /// The horizontal distance to increase (left to right) or decrease (right to left) the drawing position relative to the origin of the text element.
        /// This determines the origin position of the next text element.
        /// </summary>
        public float horizontalAdvance { get { return m_HorizontalAdvance; } set { m_HorizontalAdvance = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        [NativeName("width")]
        private float m_Width;

        [SerializeField]
        [NativeName("height")]
        private float m_Height;

        [SerializeField]
        [NativeName("horizontalBearingX")]
        private float m_HorizontalBearingX;

        [SerializeField]
        [NativeName("horizontalBearingy")]
        private float m_HorizontalBearingY;

        [SerializeField]
        [NativeName("horizontalAdvance")]
        private float m_HorizontalAdvance;

        /// <summary>
        /// Constructor for new glyph metrics.
        /// </summary>
        /// <param name="width">The width of the glyph.</param>
        /// <param name="height">The height of the glyph.</param>
        /// <param name="bearingX">The horizontal bearingX.</param>
        /// <param name="bearingY">The horizontal bearingY.</param>
        /// <param name="advance">The horizontal advance.</param>
        public GlyphMetrics(float width, float height, float bearingX, float bearingY, float advance)
        {
            m_Width = width;
            m_Height = height;
            m_HorizontalBearingX = bearingX;
            m_HorizontalBearingY = bearingY;
            m_HorizontalAdvance = advance;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public bool Equals(GlyphMetrics other)
        {
            return base.Equals(other);
        }

        public static bool operator==(GlyphMetrics lhs, GlyphMetrics rhs)
        {
            return lhs.width == rhs.width &&
                lhs.height == rhs.height &&
                lhs.horizontalBearingX == rhs.horizontalBearingX &&
                lhs.horizontalBearingY == rhs.horizontalBearingY &&
                lhs.horizontalAdvance == rhs.horizontalAdvance;
        }

        public static bool operator!=(GlyphMetrics lhs, GlyphMetrics rhs)
        {
            return !(lhs == rhs);
        }
    }

    /// <summary>
    ///  A Glyph is the visual representation of a text element or character.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public class Glyph
    {
        /// <summary>
        /// The index of the glyph in the source font file.
        /// </summary>
        public uint index { get { return m_Index; } set { m_Index = value; } }

        /// <summary>
        /// The metrics that define the size, position and spacing of a glyph when performing text layout.
        /// </summary>
        public GlyphMetrics metrics { get { return m_Metrics; } set { m_Metrics = value; } }

        /// <summary>
        /// A rectangle that defines the position of a glyph within an atlas texture.
        /// </summary>
        public GlyphRect glyphRect { get { return m_GlyphRect; } set { m_GlyphRect = value; } }

        /// <summary>
        /// The relative scale of the glyph. The default value is 1.0.
        /// </summary>
        public float scale { get { return m_Scale; } set { m_Scale = value; } }

        /// <summary>
        /// The index of the atlas texture that contains this glyph.
        /// </summary>
        public int atlasIndex { get { return m_AtlasIndex; } set { m_AtlasIndex = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        [NativeName("index")]
        private uint m_Index;

        [SerializeField]
        [NativeName("metrics")]
        private GlyphMetrics m_Metrics;

        [SerializeField]
        [NativeName("glyphRect")]
        private GlyphRect m_GlyphRect;

        [SerializeField]
        [NativeName("scale")]
        private float m_Scale;

        [SerializeField]
        [NativeName("atlasIndex")]
        private int m_AtlasIndex;

        /// <summary>
        /// Constructor for a new glyph.
        /// </summary>
        public Glyph()
        {
            m_Index = 0;
            m_Metrics = new GlyphMetrics();
            m_GlyphRect = new GlyphRect();
            m_Scale = 1;
            m_AtlasIndex = 0;
        }

        /// <summary>
        /// Constructor for a new glyph
        /// </summary>
        /// <param name="glyph">Glyph whose values are copied to the new glyph.</param>
        public Glyph(Glyph glyph)
        {
            m_Index = glyph.index;
            m_Metrics = glyph.metrics;
            m_GlyphRect = glyph.glyphRect;
            m_Scale = glyph.scale;
            m_AtlasIndex = glyph.atlasIndex;
        }

        /// <summary>
        /// Constructor for a new glyph
        /// </summary>
        /// <param name="glyphStruct">Glyph whose values are copied to the new glyph.</param>
        internal Glyph(GlyphMarshallingStruct glyphStruct)
        {
            m_Index = glyphStruct.index;
            m_Metrics = glyphStruct.metrics;
            m_GlyphRect = glyphStruct.glyphRect;
            m_Scale = glyphStruct.scale;
            m_AtlasIndex = glyphStruct.atlasIndex;
        }

        /// <summary>
        /// Constructor for new glyph.
        /// The scale will be set to a value of 1.0 and atlas index to 0.
        /// </summary>
        /// <param name="index">The index of the glyph in the font file.</param>
        /// <param name="metrics">The metrics of the glyph.</param>
        /// <param name="glyphRect">The GlyphRect defining the position of the glyph in the atlas texture.</param>
        public Glyph(uint index, GlyphMetrics metrics, GlyphRect glyphRect)
        {
            m_Index = index;
            m_Metrics = metrics;
            m_GlyphRect = glyphRect;
            m_Scale = 1;
            m_AtlasIndex = 0;
        }

        /// <summary>
        /// Constructor for new glyph.
        /// </summary>
        /// <param name="index">The index of the glyph in the font file.</param>
        /// <param name="metrics">The metrics of the glyph.</param>
        /// <param name="glyphRect">The GlyphRect defining the position of the glyph in the atlas texture.</param>
        /// <param name="scale">The relative scale of the glyph.</param>
        /// <param name="atlasIndex">The index of the atlas texture that contains the glyph.</param>
        public Glyph(uint index, GlyphMetrics metrics, GlyphRect glyphRect, float scale, int atlasIndex)
        {
            m_Index = index;
            m_Metrics = metrics;
            m_GlyphRect = glyphRect;
            m_Scale = scale;
            m_AtlasIndex = atlasIndex;
        }

        /// <summary>
        /// Compares two glyphs to determine if they have the same values.
        /// </summary>
        /// <param name="other">The glyph to compare with.</param>
        /// <returns>Returns true if the glyphs have the same values. False if not.</returns>
        public bool Compare(Glyph other)
        {
            return index == other.index &&
                metrics == other.metrics &&
                glyphRect == other.glyphRect &&
                scale == other.scale &&
                atlasIndex == other.atlasIndex;
        }
    }
}
