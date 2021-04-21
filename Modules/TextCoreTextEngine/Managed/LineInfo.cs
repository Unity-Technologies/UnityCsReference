// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.TextCore.Text
{
    struct Extents
    {
        public Vector2 min;
        public Vector2 max;

        public Extents(Vector2 min, Vector2 max)
        {
            this.min = min;
            this.max = max;
        }

        public override string ToString()
        {
            string s = "Min (" + min.x.ToString("f2") + ", " + min.y.ToString("f2") + ")   Max (" + max.x.ToString("f2") + ", " + max.y.ToString("f2") + ")";
            return s;
        }
    }

    /// <summary>
    /// Structure which contains information about the individual lines of text.
    /// </summary>
    struct LineInfo
    {
        internal int controlCharacterCount;

        public int characterCount;
        public int visibleCharacterCount;
        public int spaceCount;
        public int visibleSpaceCount;
        public int wordCount;
        public int firstCharacterIndex;
        public int firstVisibleCharacterIndex;
        public int lastCharacterIndex;
        public int lastVisibleCharacterIndex;

        public float length;
        public float lineHeight;
        public float ascender;
        public float baseline;
        public float descender;
        public float maxAdvance;

        public float width;
        public float marginLeft;
        public float marginRight;

        public TextAlignment alignment;
        public Extents lineExtents;
    }
}
