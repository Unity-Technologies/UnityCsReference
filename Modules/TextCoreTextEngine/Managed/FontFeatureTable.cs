// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.TextCore.LowLevel;


namespace UnityEngine.TextCore.Text
{
    /// <summary>
    /// Table that contains the various font features available for the given font asset.
    /// </summary>
    [Serializable]
    public class FontFeatureTable
    {
        /// <summary>
        /// List that contains the glyph pair adjustment records.
        /// </summary>
        internal List<GlyphPairAdjustmentRecord> glyphPairAdjustmentRecords
        {
            get { return m_GlyphPairAdjustmentRecords; }
            set { m_GlyphPairAdjustmentRecords = value; }
        }
        [SerializeField]
        internal List<GlyphPairAdjustmentRecord> m_GlyphPairAdjustmentRecords;

        /// <summary>
        ///
        /// </summary>
        internal Dictionary<uint, GlyphPairAdjustmentRecord> m_GlyphPairAdjustmentRecordLookup;

        // =============================================
        // Constructor(s)
        // =============================================

        public FontFeatureTable()
        {
            m_GlyphPairAdjustmentRecords = new List<GlyphPairAdjustmentRecord>();
            m_GlyphPairAdjustmentRecordLookup = new Dictionary<uint, GlyphPairAdjustmentRecord>();
        }

        // =============================================
        // Utility Functions
        // =============================================

        /// <summary>
        /// Sort the glyph pair adjustment records by glyph index.
        /// </summary>
        public void SortGlyphPairAdjustmentRecords()
        {
            // Sort List of Kerning Info
            if (m_GlyphPairAdjustmentRecords.Count > 0)
                m_GlyphPairAdjustmentRecords = m_GlyphPairAdjustmentRecords.OrderBy(s => s.firstAdjustmentRecord.glyphIndex).ThenBy(s => s.secondAdjustmentRecord.glyphIndex).ToList();
        }
    }
}
