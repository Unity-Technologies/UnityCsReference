// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.TextCore.LowLevel;

namespace UnityEngine.TextCore.Text
{
    public partial class FontAsset
    {
        // ****************************************
        // OPENTYPE - FONT FEATURES
        // ****************************************

        void UpdateFontFeaturesForNewlyAddedGlyphs()
        {
            UpdateLigatureSubstitutionRecords();

            UpdateGlyphAdjustmentRecords();

            UpdateDiacriticalMarkAdjustmentRecords();

            // Clear newly added glyph list
            m_GlyphIndexListNewlyAdded.Clear();
        }

        void UpdateGlyphAdjustmentRecordsForNewGlyphs()
        {
            UpdateGlyphAdjustmentRecords();

            // Clear newly added glyph list
            m_GlyphIndexListNewlyAdded.Clear();
        }

        void UpdateGPOSFontFeaturesForNewlyAddedGlyphs()
        {
            UpdateGlyphAdjustmentRecords();

            UpdateDiacriticalMarkAdjustmentRecords();

            // Clear newly added glyph list
            m_GlyphIndexListNewlyAdded.Clear();
        }

        /// <summary>
        ///
        /// </summary>
        internal void ImportFontFeatures()
        {
            if (LoadFontFace() != FontEngineError.Success)
                return;

            // Get Pair Adjustment records
            GlyphPairAdjustmentRecord[] pairAdjustmentRecords = FontEngine.GetAllPairAdjustmentRecords();
            if (pairAdjustmentRecords != null)
                AddPairAdjustmentRecords(pairAdjustmentRecords);

            // Get Mark-to-Base adjustment records
            UnityEngine.TextCore.LowLevel.MarkToBaseAdjustmentRecord[] markToBaseRecords = FontEngine.GetAllMarkToBaseAdjustmentRecords();
            if (markToBaseRecords != null)
                AddMarkToBaseAdjustmentRecords(markToBaseRecords);

            // Get Mark-to-Mark adjustment records
            UnityEngine.TextCore.LowLevel.MarkToMarkAdjustmentRecord[] markToMarkRecords = FontEngine.GetAllMarkToMarkAdjustmentRecords();
            if (markToMarkRecords != null)
                AddMarkToMarkAdjustmentRecords(markToMarkRecords);

            // Get Ligature Substitution records
            UnityEngine.TextCore.LowLevel.LigatureSubstitutionRecord[] records = FontEngine.GetAllLigatureSubstitutionRecords();
            if (records != null)
                AddLigatureSubstitutionRecords(records);

            m_ShouldReimportFontFeatures = false;

            // Makes the changes to the font asset persistent.
            RegisterResourceForUpdate?.Invoke(this);
        }

        void UpdateGSUBFontFeaturesForNewGlyphIndex(uint glyphIndex)
        {
            UnityEngine.TextCore.LowLevel.LigatureSubstitutionRecord[] records = FontEngine.GetLigatureSubstitutionRecords(glyphIndex);

            if (records != null)
                AddLigatureSubstitutionRecords(records);
        }

        /// <summary>
        ///
        /// </summary>
        internal void UpdateLigatureSubstitutionRecords()
        {
            k_UpdateLigatureSubstitutionRecordsMarker.Begin();

            UnityEngine.TextCore.LowLevel.LigatureSubstitutionRecord[] records = FontEngine.GetLigatureSubstitutionRecords(m_GlyphIndexListNewlyAdded);

            if (records != null)
                AddLigatureSubstitutionRecords(records);

            k_UpdateLigatureSubstitutionRecordsMarker.End();
        }

        void AddLigatureSubstitutionRecords(UnityEngine.TextCore.LowLevel.LigatureSubstitutionRecord[] records)
        {
            for (int i = 0; i < records.Length; i++)
            {
                UnityEngine.TextCore.LowLevel.LigatureSubstitutionRecord record = records[i];

                if (records[i].componentGlyphIDs == null || records[i].ligatureGlyphID == 0)
                    return;

                uint firstComponentGlyphIndex = record.componentGlyphIDs[0];

                LigatureSubstitutionRecord newRecord = new LigatureSubstitutionRecord { componentGlyphIDs = record.componentGlyphIDs, ligatureGlyphID = record.ligatureGlyphID };

                // Check if we already have a record for this new Ligature
                if (m_FontFeatureTable.m_LigatureSubstitutionRecordLookup.TryGetValue(firstComponentGlyphIndex, out List<LigatureSubstitutionRecord> existingRecords))
                {
                    foreach (LigatureSubstitutionRecord ligature in existingRecords)
                    {
                        if (newRecord == ligature)
                            return;
                    }

                    // Add new record to lookup
                    m_FontFeatureTable.m_LigatureSubstitutionRecordLookup[firstComponentGlyphIndex].Add(newRecord);
                }
                else
                {
                    m_FontFeatureTable.m_LigatureSubstitutionRecordLookup.Add(firstComponentGlyphIndex, new List<LigatureSubstitutionRecord> { newRecord });
                }

                m_FontFeatureTable.m_LigatureSubstitutionRecords.Add(newRecord);
            }
        }

        /// <summary>
        ///
        /// </summary>
        internal void UpdateGlyphAdjustmentRecords()
        {
            k_UpdateGlyphAdjustmentRecordsMarker.Begin();

            GlyphPairAdjustmentRecord[] records = FontEngine.GetPairAdjustmentRecords(m_GlyphIndexListNewlyAdded);

            if (records != null)
                AddPairAdjustmentRecords(records);

            k_UpdateGlyphAdjustmentRecordsMarker.End();
        }

        void AddPairAdjustmentRecords(GlyphPairAdjustmentRecord[] records)
        {
            float emScale = (float)m_FaceInfo.pointSize / m_FaceInfo.unitsPerEM;

            for (int i = 0; i < records.Length; i++)
            {
                GlyphPairAdjustmentRecord record = records[i];
                GlyphAdjustmentRecord first = record.firstAdjustmentRecord;
                GlyphAdjustmentRecord second = record.secondAdjustmentRecord;

                uint firstIndex = first.glyphIndex;
                uint secondIndexIndex = second.glyphIndex;

                if (firstIndex == 0 && secondIndexIndex == 0)
                    return;

                uint key = secondIndexIndex << 16 | firstIndex;

                if (m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.ContainsKey(key))
                    continue;

                // Adjust values currently in Units per EM to make them relative to Sampling Point Size.
                GlyphValueRecord valueRecord = first.glyphValueRecord;
                valueRecord.xAdvance *= emScale;
                record.firstAdjustmentRecord = new GlyphAdjustmentRecord(firstIndex, valueRecord);

                m_FontFeatureTable.m_GlyphPairAdjustmentRecords.Add(record);
                m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.Add(key, record);
            }
        }


        /// <summary>
        /// Function used for debugging and performance testing.
        /// </summary>
        /// <param name="glyphIndexes"></param>
        internal void UpdateGlyphAdjustmentRecords(uint[] glyphIndexes)
        {
            using (k_UpdateGlyphAdjustmentRecordsMarker.Auto())
            {
                // Get glyph pair adjustment records from font file.
                GlyphPairAdjustmentRecord[] pairAdjustmentRecords = FontEngine.GetGlyphPairAdjustmentTable(glyphIndexes);

                // Clear newly added glyph list
                //m_GlyphIndexListNewlyAdded.Clear();

                if (pairAdjustmentRecords == null || pairAdjustmentRecords.Length == 0)
                    return;

                if (m_FontFeatureTable == null)
                    m_FontFeatureTable = new FontFeatureTable();

                for (int i = 0; i < pairAdjustmentRecords.Length && pairAdjustmentRecords[i].firstAdjustmentRecord.glyphIndex != 0; i++)
                {
                    uint pairKey = pairAdjustmentRecords[i].secondAdjustmentRecord.glyphIndex << 16 | pairAdjustmentRecords[i].firstAdjustmentRecord.glyphIndex;

                    // Check if table already contains a pair adjustment record for this key.
                    if (m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.ContainsKey(pairKey))
                        continue;

                    GlyphPairAdjustmentRecord record = pairAdjustmentRecords[i];

                    m_FontFeatureTable.m_GlyphPairAdjustmentRecords.Add(record);
                    m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.Add(pairKey, record);
                }
            }
        }

        /// <summary>
        /// Function requires an update to the TextCore:FontEngine
        /// </summary>
        /// <param name="glyphIndexes"></param>
        internal void UpdateGlyphAdjustmentRecords(List<uint> glyphIndexes)
        {
            /*
            k_UpdateGlyphAdjustmentRecordsMarker.Begin();

            // Get glyph pair adjustment records from font file.
            int recordCount;
            GlyphPairAdjustmentRecord[] pairAdjustmentRecords = FontEngine.GetGlyphPairAdjustmentRecords(glyphIndexes, out recordCount);

            // Clear newly added glyph list
            m_GlyphIndexListNewlyAdded.Clear();

            if (pairAdjustmentRecords == null || pairAdjustmentRecords.Length == 0)
            {
                k_UpdateGlyphAdjustmentRecordsMarker.End();
                return;
            }

            if (m_FontFeatureTable == null)
                m_FontFeatureTable = new TMP_FontFeatureTable();

            for (int i = 0; i < pairAdjustmentRecords.Length && pairAdjustmentRecords[i].firstAdjustmentRecord.glyphIndex != 0; i++)
            {
                uint pairKey = pairAdjustmentRecords[i].secondAdjustmentRecord.glyphIndex << 16 | pairAdjustmentRecords[i].firstAdjustmentRecord.glyphIndex;

                // Check if table already contains a pair adjustment record for this key.
                if (m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookupDictionary.ContainsKey(pairKey))
                    continue;

                TMP_GlyphPairAdjustmentRecord record = new TMP_GlyphPairAdjustmentRecord(pairAdjustmentRecords[i]);

                m_FontFeatureTable.m_GlyphPairAdjustmentRecords.Add(record);
                m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookupDictionary.Add(pairKey, record);
            }

            k_UpdateGlyphAdjustmentRecordsMarker.End();

            #if UNITY_EDITOR
            m_FontFeatureTable.SortGlyphPairAdjustmentRecords();
            #endif
            */
        }

        /// <summary>
        /// Function requires an update to the TextCore:FontEngine
        /// </summary>
        /// <param name="newGlyphIndexes"></param>
        /// <param name="allGlyphIndexes"></param>
        internal void UpdateGlyphAdjustmentRecords(List<uint> newGlyphIndexes, List<uint> allGlyphIndexes)
        {
            /*
            // Get glyph pair adjustment records from font file.
            GlyphPairAdjustmentRecord[] pairAdjustmentRecords = FontEngine.GetGlyphPairAdjustmentRecords(newGlyphIndexes, allGlyphIndexes);

            // Clear newly added glyph list
            m_GlyphIndexListNewlyAdded.Clear();

            if (pairAdjustmentRecords == null || pairAdjustmentRecords.Length == 0)
            {
                return;
            }

            if (m_FontFeatureTable == null)
                m_FontFeatureTable = new TMP_FontFeatureTable();

            for (int i = 0; i < pairAdjustmentRecords.Length && pairAdjustmentRecords[i].firstAdjustmentRecord.glyphIndex != 0; i++)
            {
                uint pairKey = pairAdjustmentRecords[i].secondAdjustmentRecord.glyphIndex << 16 | pairAdjustmentRecords[i].firstAdjustmentRecord.glyphIndex;

                // Check if table already contains a pair adjustment record for this key.
                if (m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookupDictionary.ContainsKey(pairKey))
                    continue;

                TMP_GlyphPairAdjustmentRecord record = new TMP_GlyphPairAdjustmentRecord(pairAdjustmentRecords[i]);

                m_FontFeatureTable.m_GlyphPairAdjustmentRecords.Add(record);
                m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookupDictionary.Add(pairKey, record);
            }

            #if UNITY_EDITOR
            m_FontFeatureTable.SortGlyphPairAdjustmentRecords();
            #endif
            */
        }

        /// <summary>
        ///
        /// </summary>
        internal void UpdateDiacriticalMarkAdjustmentRecords()
        {
            using (k_UpdateDiacriticalMarkAdjustmentRecordsMarker.Auto())
            {
                // Get Mark-to-Base adjustment records
                MarkToBaseAdjustmentRecord[] markToBaseRecords = FontEngine.GetMarkToBaseAdjustmentRecords(m_GlyphIndexListNewlyAdded);
                if (markToBaseRecords != null)
                    AddMarkToBaseAdjustmentRecords(markToBaseRecords);

                // Get Mark-to-Mark adjustment records
                MarkToMarkAdjustmentRecord[] markToMarkRecords = FontEngine.GetMarkToMarkAdjustmentRecords(m_GlyphIndexListNewlyAdded);
                if (markToMarkRecords != null)
                    AddMarkToMarkAdjustmentRecords(markToMarkRecords);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="records"></param>
        void AddMarkToBaseAdjustmentRecords(MarkToBaseAdjustmentRecord[] records)
        {
            float emScale = (float)m_FaceInfo.pointSize / m_FaceInfo.unitsPerEM;

            for (int i = 0; i < records.Length; i++)
            {
                MarkToBaseAdjustmentRecord record = records[i];
                if (records[i].baseGlyphID == 0 || records[i].markGlyphID == 0)
                    return;

                uint key = record.markGlyphID << 16 | record.baseGlyphID;

                if (m_FontFeatureTable.m_MarkToBaseAdjustmentRecordLookup.ContainsKey(key))
                    continue;

                MarkToBaseAdjustmentRecord newRecord = new MarkToBaseAdjustmentRecord {
                    baseGlyphID = record.baseGlyphID,
                    baseGlyphAnchorPoint = new GlyphAnchorPoint() { xCoordinate = record.baseGlyphAnchorPoint.xCoordinate * emScale, yCoordinate = record.baseGlyphAnchorPoint.yCoordinate * emScale },
                    markGlyphID = record.markGlyphID,
                    markPositionAdjustment = new MarkPositionAdjustment(){ xPositionAdjustment = record.markPositionAdjustment.xPositionAdjustment * emScale, yPositionAdjustment = record.markPositionAdjustment.yPositionAdjustment * emScale} };

                m_FontFeatureTable.MarkToBaseAdjustmentRecords.Add(newRecord);
                m_FontFeatureTable.m_MarkToBaseAdjustmentRecordLookup.Add(key, newRecord);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="records"></param>
        void AddMarkToMarkAdjustmentRecords(MarkToMarkAdjustmentRecord[] records)
        {
            float emScale = (float)m_FaceInfo.pointSize / m_FaceInfo.unitsPerEM;

            for (int i = 0; i < records.Length; i++)
            {
                MarkToMarkAdjustmentRecord record = records[i];
                if (records[i].baseMarkGlyphID == 0 || records[i].combiningMarkGlyphID == 0)
                    return;

                uint key = record.combiningMarkGlyphID << 16 | record.baseMarkGlyphID;

                if (m_FontFeatureTable.m_MarkToMarkAdjustmentRecordLookup.ContainsKey(key))
                    continue;

                MarkToMarkAdjustmentRecord newRecord = new MarkToMarkAdjustmentRecord {
                    baseMarkGlyphID = record.baseMarkGlyphID,
                    baseMarkGlyphAnchorPoint = new GlyphAnchorPoint() { xCoordinate = record.baseMarkGlyphAnchorPoint.xCoordinate * emScale, yCoordinate = record.baseMarkGlyphAnchorPoint.yCoordinate * emScale},
                    combiningMarkGlyphID = record.combiningMarkGlyphID,
                    combiningMarkPositionAdjustment = new MarkPositionAdjustment(){ xPositionAdjustment = record.combiningMarkPositionAdjustment.xPositionAdjustment * emScale, yPositionAdjustment = record.combiningMarkPositionAdjustment.yPositionAdjustment * emScale} };

                m_FontFeatureTable.MarkToMarkAdjustmentRecords.Add(newRecord);
                m_FontFeatureTable.m_MarkToMarkAdjustmentRecordLookup.Add(key, newRecord);
            }
        }
    }
}

