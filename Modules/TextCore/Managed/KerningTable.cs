// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.TextCore.LowLevel;

namespace UnityEngine.TextCore
{
    struct KerningPairKey
    {
        public uint ascii_Left;
        public uint ascii_Right;
        public uint key;

        public KerningPairKey(uint ascii_left, uint ascii_right)
        {
            ascii_Left = ascii_left;
            ascii_Right = ascii_right;
            key = (ascii_right << 16) + ascii_left;
        }
    }

    [Serializable]
    class KerningPair
    {
        /// <summary>
        /// The first glyph part of a kerning pair.
        /// </summary>
        public uint firstGlyph
        {
            get { return m_FirstGlyph; }
            set { m_FirstGlyph = value; }
        }
        [FormerlySerializedAs("AscII_Left")]
        [SerializeField]
        uint m_FirstGlyph;

        /// <summary>
        /// The positional adjustment of the first glyph.
        /// </summary>
        public GlyphValueRecord firstGlyphAdjustments
        {
            get { return m_FirstGlyphAdjustments; }
        }
        [SerializeField]
        GlyphValueRecord m_FirstGlyphAdjustments;

        /// <summary>
        /// The second glyph part of a kerning pair.
        /// </summary>
        public uint secondGlyph
        {
            get { return m_SecondGlyph; }
            set { m_SecondGlyph = value; }
        }
        [FormerlySerializedAs("AscII_Right")]
        [SerializeField]
        uint m_SecondGlyph;

        /// <summary>
        /// The positional adjustment of the second glyph.
        /// </summary>
        public GlyphValueRecord secondGlyphAdjustments
        {
            get { return m_SecondGlyphAdjustments; }
        }
        [SerializeField]
        GlyphValueRecord m_SecondGlyphAdjustments;

        [FormerlySerializedAs("XadvanceOffset")]
        public float xOffset;


        public KerningPair()
        {
            m_FirstGlyph = 0;
            m_FirstGlyphAdjustments = new GlyphValueRecord();

            m_SecondGlyph = 0;
            m_SecondGlyphAdjustments = new GlyphValueRecord();
        }

        public KerningPair(uint left, uint right, float offset)
        {
            firstGlyph = left;
            m_SecondGlyph = right;
            xOffset = offset;
        }

        public KerningPair(uint firstGlyph, GlyphValueRecord firstGlyphAdjustments, uint secondGlyph, GlyphValueRecord secondGlyphAdjustments)
        {
            m_FirstGlyph = firstGlyph;
            m_FirstGlyphAdjustments = firstGlyphAdjustments;
            m_SecondGlyph = secondGlyph;
            m_SecondGlyphAdjustments = secondGlyphAdjustments;
        }

        internal void ConvertLegacyKerningData()
        {
            m_FirstGlyphAdjustments.xAdvance = xOffset;
            //xOffset = 0;
        }
    }


    [Serializable]
    class KerningTable
    {
        public List<KerningPair> kerningPairs;

        public KerningTable()
        {
            kerningPairs = new List<KerningPair>();
        }

        /// <summary>
        /// Add Glyph pair adjustment record
        /// </summary>
        /// <param name="first">The first glyph</param>
        /// <param name="firstAdjustments">Adjustment record for the first glyph</param>
        /// <param name="second">The second glyph</param>
        /// <param name="secondAdjustments">Adjustment record for the second glyph</param>
        /// <returns></returns>
        public int AddGlyphPairAdjustmentRecord(uint first, GlyphValueRecord firstAdjustments, uint second, GlyphValueRecord secondAdjustments)
        {
            int index = kerningPairs.FindIndex(item => item.firstGlyph == first && item.secondGlyph == second);

            if (index == -1)
            {
                kerningPairs.Add(new KerningPair(first, firstAdjustments, second, secondAdjustments));
                return 0;
            }

            // Return -1 if Kerning Pair already exists.
            return -1;
        }

        public void RemoveKerningPair(int left, int right)
        {
            int index = kerningPairs.FindIndex(item => item.firstGlyph == left && item.secondGlyph == right);

            if (index != -1)
                kerningPairs.RemoveAt(index);
        }

        public void RemoveKerningPair(int index)
        {
            kerningPairs.RemoveAt(index);
        }

        public void SortKerningPairs()
        {
            // Sort List of Kerning Info
            if (kerningPairs.Count > 0)
                kerningPairs = kerningPairs.OrderBy(s => s.firstGlyph).ThenBy(s => s.secondGlyph).ToList();
        }
    }
}
