// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.TextCore.LowLevel;

namespace UnityEngine.TextCore.Text
{
    // ****************************************
    // OPENTYPE - FONT FEATURES
    // ****************************************
    public partial class FontAsset
    {
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
            var pairAdjustmentRecords = FontEngine.GetAllPairAdjustmentRecords();
            if (pairAdjustmentRecords != null)
                AddPairAdjustmentRecords(pairAdjustmentRecords);

            // Get Mark-to-Base adjustment records
            var markToBaseRecords = FontEngine.GetAllMarkToBaseAdjustmentRecords();
            if (markToBaseRecords != null)
                AddMarkToBaseAdjustmentRecords(markToBaseRecords);

            // Get Mark-to-Mark adjustment records
            var markToMarkRecords = FontEngine.GetAllMarkToMarkAdjustmentRecords();
            if (markToMarkRecords != null)
                AddMarkToMarkAdjustmentRecords(markToMarkRecords);

            // Get Ligature Substitution records
            var records = FontEngine.GetAllLigatureSubstitutionRecords();
            if (records != null)
                AddLigatureSubstitutionRecords(records);

            m_ShouldReimportFontFeatures = false;

            // Makes the changes to the font asset persistent.
            RegisterResourceForUpdate?.Invoke(this);
        }

        void UpdateGSUBFontFeaturesForNewGlyphIndex(uint glyphIndex)
        {
            var records = FontEngine.GetLigatureSubstitutionRecords(glyphIndex);

            if (records != null)
                AddLigatureSubstitutionRecords(records);
        }

        /// <summary>
        ///
        /// </summary>
        internal void UpdateLigatureSubstitutionRecords()
        {
            k_UpdateLigatureSubstitutionRecordsMarker.Begin();

            var records = FontEngine.GetLigatureSubstitutionRecords(m_GlyphIndexListNewlyAdded);

            if (records != null)
                AddLigatureSubstitutionRecords(records);

            k_UpdateLigatureSubstitutionRecordsMarker.End();
        }

        void AddLigatureSubstitutionRecords(LigatureSubstitutionRecord[] records)
        {
            var destinationLookup = m_FontFeatureTable.m_LigatureSubstitutionRecordLookup;
            var destinationList = m_FontFeatureTable.m_LigatureSubstitutionRecords;

            EnsureAdditionalCapacity(destinationLookup, records.Length);
            EnsureAdditionalCapacity(destinationList, records.Length);

            foreach (var record in records)
            {
                if (record.componentGlyphIDs == null || record.ligatureGlyphID == 0)
                    return;

                var firstComponentGlyphIndex = record.componentGlyphIDs[0];
                var newRecord = new LigatureSubstitutionRecord { componentGlyphIDs = record.componentGlyphIDs, ligatureGlyphID = record.ligatureGlyphID };

                // Check if we already have a record for this new Ligature
                if (destinationLookup.TryGetValue(firstComponentGlyphIndex, out var existingRecords))
                {
                    foreach (var ligature in existingRecords)
                    {
                        if (newRecord == ligature)
                            return;
                    }

                    // Add new record to lookup
                    destinationLookup[firstComponentGlyphIndex].Add(newRecord);
                }
                else
                {
                    destinationLookup.Add(firstComponentGlyphIndex, new List<LigatureSubstitutionRecord> { newRecord });
                }

                destinationList.Add(newRecord);
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
            var emScale = (float)m_FaceInfo.pointSize / m_FaceInfo.unitsPerEM;

            var destinationList = m_FontFeatureTable.glyphPairAdjustmentRecords;
            var destinationLookup = m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup;

            EnsureAdditionalCapacity(destinationLookup, records.Length);
            EnsureAdditionalCapacity(destinationList, records.Length);

            foreach (var record in records)
            {
                var first = record.firstAdjustmentRecord;
                var second = record.secondAdjustmentRecord;

                var firstIndex = first.glyphIndex;
                var secondIndexIndex = second.glyphIndex;

                if (firstIndex == 0 && secondIndexIndex == 0)
                    return;

                var key = secondIndexIndex << 16 | firstIndex;

                // Adjust values currently in Units per EM to make them relative to Sampling Point Size.
                var newRecord = record;
                var valueRecord = first.glyphValueRecord;
                valueRecord.xAdvance *= emScale;
                newRecord.firstAdjustmentRecord = new GlyphAdjustmentRecord(firstIndex, valueRecord);

                if (destinationLookup.TryAdd(key, newRecord))
                    destinationList.Add(newRecord);
            }
        }

        /// <summary>
        ///
        /// </summary>
        internal void UpdateDiacriticalMarkAdjustmentRecords()
        {
            using (k_UpdateDiacriticalMarkAdjustmentRecordsMarker.Auto())
            {
                // Get Mark-to-Base adjustment records
                var markToBaseRecords = FontEngine.GetMarkToBaseAdjustmentRecords(m_GlyphIndexListNewlyAdded);
                if (markToBaseRecords != null)
                    AddMarkToBaseAdjustmentRecords(markToBaseRecords);

                // Get Mark-to-Mark adjustment records
                var markToMarkRecords = FontEngine.GetMarkToMarkAdjustmentRecords(m_GlyphIndexListNewlyAdded);
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
            var emScale = (float)m_FaceInfo.pointSize / m_FaceInfo.unitsPerEM;

            foreach (var record in records)
            {
                if (record.baseGlyphID == 0 || record.markGlyphID == 0)
                    return;

                var key = record.markGlyphID << 16 | record.baseGlyphID;

                if (m_FontFeatureTable.m_MarkToBaseAdjustmentRecordLookup.ContainsKey(key))
                    continue;

                var newRecord = new MarkToBaseAdjustmentRecord {
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

