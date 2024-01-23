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
        /// List that contains the glyph multiple substitution records.
        /// </summary>
        internal List<MultipleSubstitutionRecord> multipleSubstitutionRecords
        {
            get { return m_MultipleSubstitutionRecords; }
            set { m_MultipleSubstitutionRecords = value; }
        }

        /// <summary>
        /// List that contains the glyph ligature records.
        /// </summary>
        internal List<LigatureSubstitutionRecord> ligatureRecords
        {
            get { return m_LigatureSubstitutionRecords; }
            set { m_LigatureSubstitutionRecords = value; }
        }

        /// <summary>
        /// List that contains the glyph pair adjustment records.
        /// </summary>
        internal List<GlyphPairAdjustmentRecord> glyphPairAdjustmentRecords => m_GlyphPairAdjustmentRecords;

        /// <summary>
        ///
        /// </summary>
        internal List<MarkToBaseAdjustmentRecord> MarkToBaseAdjustmentRecords
        {
            get { return m_MarkToBaseAdjustmentRecords; }
            set { m_MarkToBaseAdjustmentRecords = value; }
        }

        /// <summary>
        ///
        /// </summary>
        internal List<MarkToMarkAdjustmentRecord> MarkToMarkAdjustmentRecords
        {
            get { return m_MarkToMarkAdjustmentRecords; }
            set { m_MarkToMarkAdjustmentRecords = value; }
        }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        internal List<MultipleSubstitutionRecord> m_MultipleSubstitutionRecords;

        [SerializeField]
        internal List<LigatureSubstitutionRecord> m_LigatureSubstitutionRecords;

        [SerializeField]
        private List<GlyphPairAdjustmentRecord> m_GlyphPairAdjustmentRecords;

        [SerializeField]
        internal List<MarkToBaseAdjustmentRecord> m_MarkToBaseAdjustmentRecords;

        [SerializeField]
        internal List<MarkToMarkAdjustmentRecord> m_MarkToMarkAdjustmentRecords;


        // =============================================
        // Lookup data structures.
        // =============================================

        internal Dictionary<uint, List<LigatureSubstitutionRecord>> m_LigatureSubstitutionRecordLookup;

        internal Dictionary<uint, GlyphPairAdjustmentRecord> m_GlyphPairAdjustmentRecordLookup;

        internal Dictionary<uint, MarkToBaseAdjustmentRecord> m_MarkToBaseAdjustmentRecordLookup;

        internal Dictionary<uint, MarkToMarkAdjustmentRecord> m_MarkToMarkAdjustmentRecordLookup;

        // =============================================
        // Constructor(s)
        // =============================================

        internal FontFeatureTable()
        {
            m_LigatureSubstitutionRecords = new List<LigatureSubstitutionRecord>();
            m_LigatureSubstitutionRecordLookup = new Dictionary<uint, List<LigatureSubstitutionRecord>>();

            m_GlyphPairAdjustmentRecords = new List<GlyphPairAdjustmentRecord>();
            m_GlyphPairAdjustmentRecordLookup = new Dictionary<uint, GlyphPairAdjustmentRecord>();

            m_MarkToBaseAdjustmentRecords = new List<MarkToBaseAdjustmentRecord>();
            m_MarkToBaseAdjustmentRecordLookup = new Dictionary<uint, MarkToBaseAdjustmentRecord>();

            m_MarkToMarkAdjustmentRecords = new List<MarkToMarkAdjustmentRecord>();
            m_MarkToMarkAdjustmentRecordLookup = new Dictionary<uint, MarkToMarkAdjustmentRecord>();
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
            if (m_GlyphPairAdjustmentRecords.Count > 1)
                m_GlyphPairAdjustmentRecords = m_GlyphPairAdjustmentRecords.OrderBy(s => s.firstAdjustmentRecord.glyphIndex).ThenBy(s => s.secondAdjustmentRecord.glyphIndex).ToList();
        }

        /// <summary>
        /// Sort the Mark-to-Base Adjustment Table records.
        /// </summary>
        public void SortMarkToBaseAdjustmentRecords()
        {
            // Sort List of Kerning Info
            if (m_MarkToBaseAdjustmentRecords.Count > 0)
                m_MarkToBaseAdjustmentRecords = m_MarkToBaseAdjustmentRecords.OrderBy(s => s.baseGlyphID).ThenBy(s => s.markGlyphID).ToList();
        }

        /// <summary>
        /// Sort the Mark-to-Mark Adjustment Table records.
        /// </summary>
        public void SortMarkToMarkAdjustmentRecords()
        {
            // Sort List of Kerning Info
            if (m_MarkToMarkAdjustmentRecords.Count > 0)
                m_MarkToMarkAdjustmentRecords = m_MarkToMarkAdjustmentRecords.OrderBy(s => s.baseMarkGlyphID).ThenBy(s => s.combiningMarkGlyphID).ToList();
        }
    }
}
