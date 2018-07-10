// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;


namespace UnityEngine.TextCore
{
    /// <summary>
    ///  A Glyph is the visual representation of a text element / character.
    /// </summary>
    [Serializable]
    [NativeAsStruct]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public class Glyph
    {
        /// <summary>
        /// Constructor for a new glyph.
        /// </summary>
        public Glyph()
        {
            this.index = 0;
            this.x = 0;
            this.y = 0;
            this.width = 0;
            this.height = 0;
            this.bearingX = 0;
            this.bearingY = 0;
            this.advanceX = 0;
            this.scale = 1;
            this.atlasIndex = 0;
        }

        /// <summary>
        /// Constructor for a new glyph
        /// </summary>
        /// <param name="glyph">Glyph whose values are copied to the new glyph.</param>
        public Glyph(Glyph glyph)
        {
            this.index = glyph.index;
            this.x = glyph.x;
            this.y = glyph.y;
            this.width = glyph.width;
            this.height = glyph.height;
            this.bearingX = glyph.bearingX;
            this.bearingY = glyph.bearingY;
            this.advanceX = glyph.advanceX;
            this.scale = glyph.scale;
            this.atlasIndex = glyph.atlasIndex;
        }

        /// <summary>
        /// Constructor for a new glyph
        /// </summary>
        /// <param name="index">Index of the glyph.</param>
        /// <param name="bearingX">The bearingX of the glyph.</param>
        /// <param name="bearingY">The bearingY of the glyph.</param>
        /// <param name="width">The width of the glyph.</param>
        /// <param name="height">The height of the glyph.</param>
        /// <param name="advanceX">The advanceX of the glyph.</param>
        /// <param name="scale">The relative scale of the glyph.</param>
        public Glyph(uint index, int bearingX, int bearingY, int width, int height, int advanceX, float scale, int atlasIndex)
        {
            this.index = index;
            this.x = 0;
            this.y = 0;
            this.width = width;
            this.height = height;
            this.bearingX = bearingX;
            this.bearingY = bearingY;
            this.advanceX = advanceX;
            this.scale = scale;
            this.atlasIndex = atlasIndex;
        }

        /// <summary>
        /// The index of the glyph in the source font file.
        /// </summary>
        public uint index;

        /// <summary>
        /// The point size at which this glyph was rastered.
        /// </summary>
        //public int pointSize;

        /// <summary>
        /// The x position of the glyph in the font atlas texture
        /// </summary>
        public int x { get { return m_XMin; } set { m_XMin = value; } }
        [SerializeField]
        [NativeName("x")]
        private int m_XMin;

        /// <summary>
        /// The y position of the glyph in the font atlas texture.
        /// </summary>
        public int y { get { return m_YMin; } set { m_YMin = value; } }
        [SerializeField]
        [NativeName("y")]
        private int m_YMin;


        // =======================
        // Glyph Metrics
        // =======================

        /// <summary>
        /// The width of the glyph.
        /// </summary>
        public int width { get { return m_Width; } set { m_Width = value; } }
        [SerializeField]
        [NativeName("width")]
        private int m_Width;

        /// <summary>
        /// The height of the glyph.
        /// </summary>
        public int height { get { return m_Height; } set { m_Height = value; } }
        [SerializeField]
        [NativeName("height")]
        private int m_Height;

        /// <summary>
        /// The horizontal distance from the current drawing position (origin) relative to the elements' left bounding box edge (bbox).
        /// </summary>
        public int bearingX;

        /// <summary>
        /// The vertical distance from the current baseline relative to the elements' top bounding box edge (bbox).
        /// </summary>
        public int bearingY;

        /// <summary>
        /// The horizontal distance to increase (left to right) or decrease (right to left) the drawing position relative to the origin of the text element.
        /// This determines the origin position of the next element.
        /// </summary>
        public int advanceX;

        /// <summary>
        /// The relative scale of the text element. The default value is 1.0.
        /// </summary>
        public float scale;


        // =======================
        // Font Atlas Information
        // =======================

        /// <summary>
        /// The index of the atlas texture that contains this glyph.
        /// </summary>
        public int atlasIndex;

        /// <summary>
        /// The x position of the glyph in the font atlas texture
        /// </summary>
        //public int x { get { return m_XMin; } set { m_XMin = value; } }
        //[SerializeField]
        //[NativeName("x")]
        //private int m_XMin;

        /// <summary>
        /// The y position of the glyph in the font atlas texture.
        /// </summary>
        //public int y { get { return m_YMin; } set { m_YMin = value; } }
        //[SerializeField]
        //[NativeName("y")]
        //private int m_YMin;

        /// <summary>
        /// A Rect that contains the uv coordinates of the glyph in the atlas texture. These values correspond to the xMin, yMin, xMax, yMax positions.
        /// </summary>
        //public Vector4 uv;

        /// <summary>
        /// The UV coordinate corresponding to the bottom left of the glyph in texture space.
        /// </summary>
        //public Vector2 uv0 { get { return new Vector2(uv.xMin, uv.yMin); } }

        /// <summary>
        /// The UV coordinate corresponding to the top left of the glyph in texture space.
        /// </summary>
        //public Vector2 uv1 { get { return new Vector2(uv.xMin, uv.yMax); } }

        /// <summary>
        /// The UV coordinate corresponding to the top right of the glyph in texture space.
        /// </summary>
        //public Vector2 uv2 { get { return new Vector2(uv.xMax, uv.yMax); } }

        /// <summary>
        /// The UV coordinate corresponding to the bottom right of the glyph in texture space.
        /// </summary>
        //public Vector2 uv3 { get { return new Vector2(uv.xMax, uv.yMin); } }

        /// <summary>
        /// Set the uv coordinates of the glyph in the atlas texture.
        /// </summary>
        /// <param name="x">The x position of the glyph in the atlas texture.</param>
        /// <param name="y">The y position of the glyph in the atlas texture.</param>
        /// <param name="texWidth">The width of the atlas texture.</param>
        /// <param name="texHeight">The height of the atlas texture.</param>
        //public void SetUV(int x, int y, int texWidth, int texHeight)
        //{
        //    uv.xMin = x / texWidth;
        //    uv.xMax = uv.xMin + m_Width / texWidth;

        //    uv.xMin = y / texHeight;
        //    uv.yMax = uv.yMin + m_Height / texHeight;
        //}
    }
}
