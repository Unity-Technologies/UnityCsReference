// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;


namespace UnityEngine.TextCore.LowLevel
{
    /// <summary>
    /// The SingleSubstitutionRecord defines the substitution of a single glyph by another.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct SingleSubstitutionRecord
    {
        /// <summary>
        /// The index of the target glyph being substituted.
        /// </summary>
        public uint targetGlyphID { get { return m_TargetGlyphID; } set { m_TargetGlyphID = value; } }

        /// <summary>
        /// The index of the replacement glyph.
        /// </summary>
        public uint substituteGlyphID { get { return m_SubstituteGlyphID; } set { m_SubstituteGlyphID = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        [NativeName("targetGlyphID")]
        private uint m_TargetGlyphID;

        [SerializeField]
        [NativeName("substituteGlyphID")]
        private uint m_SubstituteGlyphID;
    }

    /// <summary>
    /// The MultipleSubstitutionRecord defines the substitution of a single glyph by multiple glyphs.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct MultipleSubstitutionRecord
    {
        /// <summary>
        /// The index of the target glyph being substituted.
        /// </summary>
        public uint targetGlyphID { get { return m_TargetGlyphID; } set { m_TargetGlyphID = value; } }

        /// <summary>
        /// Array that contains the index of the glyphs replacing the single target glyph.
        /// </summary>
        public uint[] substituteGlyphIDs { get { return m_SubstituteGlyphIDs; } set { m_SubstituteGlyphIDs = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        [NativeName("targetGlyphID")]
        private uint m_TargetGlyphID;

        [SerializeField]
        [NativeName("substituteGlyphIDs")]
        private uint[] m_SubstituteGlyphIDs;
    }

    /// <summary>
    /// The AlternateSubstitutionRecord defines the substitution of a single glyph by several potential alternative glyphs.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct AlternateSubstitutionRecord
    {
        /// <summary>
        /// The index of the target glyph being substituted.
        /// </summary>
        public uint targetGlyphID { get { return m_TargetGlyphID; } set { m_TargetGlyphID = value; } }

        /// <summary>
        /// Array that contains the index of the alternate glyphs for the target glyph.
        /// </summary>
        public uint[] substituteGlyphIDs { get { return m_SubstituteGlyphIDs; } set { m_SubstituteGlyphIDs = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        [NativeName("targetGlyphID")]
        private uint m_TargetGlyphID;

        [SerializeField]
        [NativeName("substituteGlyphIDs")]
        private uint[] m_SubstituteGlyphIDs;
    }

    /// <summary>
    /// The LigatureSubstitutionRecord defines the substitution of multiple glyphs by a single glyph.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct LigatureSubstitutionRecord
    {
        /// <summary>
        /// Array that contains the index of the glyphs being substituted.
        /// </summary>
        public uint[] componentGlyphIDs { get { return m_ComponentGlyphIDs; } set { m_ComponentGlyphIDs = value; } }

        /// <summary>
        /// The index of the replacement glyph.
        /// </summary>
        public uint ligatureGlyphID { get { return m_LigatureGlyphID; } set { m_LigatureGlyphID = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        [NativeName("componentGlyphs")]
        private uint[] m_ComponentGlyphIDs;

        [SerializeField]
        [NativeName("ligatureGlyph")]
        private uint m_LigatureGlyphID;
    }

    /// <summary>
    /// Data structure used by ContextualSubstitutionRecord and ChainedContextualSubstitutionRecord that defines a sequence of glyph IDs.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct GlyphIDSequence
    {
        /// <summary>
        /// Array of glyph IDs.
        /// </summary>
        public uint[] glyphIDs { get { return m_GlyphIDs; } set { m_GlyphIDs = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        [NativeName("glyphIDs")]
        private uint[] m_GlyphIDs;
    }

    /// <summary>
    /// Data structure used by ContextualSubstitutionRecord and ChainedContextualSubstitutionRecord.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct SequenceLookupRecord
    {
        /// <summary>
        /// Index into the input glyph sequence.
        /// </summary>
        public uint glyphSequenceIndex { get { return m_GlyphSequenceIndex; } set { m_GlyphSequenceIndex = value; } }

        /// <summary>
        /// Index into the LookupList.
        /// </summary>
        public uint lookupListIndex { get { return m_LookupListIndex; } set { m_LookupListIndex = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        [NativeName("glyphSequenceIndex")]
        private uint m_GlyphSequenceIndex;

        [SerializeField]
        [NativeName("lookupListIndex")]
        private uint m_LookupListIndex;
    }

    /// <summary>
    /// Defines the substitution of multiple glyphs by a single glyph.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct ContextualSubstitutionRecord
    {
        /// <summary>
        /// Array that contains the index of the glyphs used as input glyph sequence in the ContextualSubstitutionRecord.
        /// </summary>
        public GlyphIDSequence[] inputSequences { get { return m_InputGlyphSequences; } set { m_InputGlyphSequences = value; } }

        /// <summary>
        /// Array that contains the sequence lookup records for the ContextualSubstitutionRecord.
        /// </summary>
        public SequenceLookupRecord[] sequenceLookupRecords { get { return m_SequenceLookupRecords; } set { m_SequenceLookupRecords = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        [NativeName("inputGlyphSequences")]
        private GlyphIDSequence[] m_InputGlyphSequences;

        [SerializeField]
        [NativeName("sequenceLookupRecords")]
        private SequenceLookupRecord[] m_SequenceLookupRecords;
    }

    /// <summary>
    /// Defines the substitution record for the chained contextual substitution.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct ChainingContextualSubstitutionRecord
    {
        /// <summary>
        /// Array that contains the index of the glyphs used as the backtrack glyph sequence in the ChainingContextualSubstitutionRecord.
        /// </summary>
        public GlyphIDSequence[] backtrackGlyphSequences { get { return m_BacktrackGlyphSequences; } set { m_BacktrackGlyphSequences = value; } }

        /// <summary>
        /// Array that contains the index of the glyphs used as input glyph sequence in the ChainingContextualSubstitutionRecord.
        /// </summary>
        public GlyphIDSequence[] inputGlyphSequences { get { return m_InputGlyphSequences; } set { m_InputGlyphSequences = value; } }

        /// <summary>
        /// Array that contains the index of the glyphs used as the lookahead glyph sequence in the ChainingContextualSubstitutionRecord.
        /// </summary>
        public GlyphIDSequence[] lookaheadGlyphSequences { get { return m_LookaheadGlyphSequences; } set { m_LookaheadGlyphSequences = value; } }

        /// <summary>
        /// Array that contains the sequence lookup records for the ChainingContextualSubstitutionRecord.
        /// </summary>
        public SequenceLookupRecord[] sequenceLookupRecords { get { return m_SequenceLookupRecords; } set { m_SequenceLookupRecords = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        [NativeName("backtrackGlyphSequences")]
        private GlyphIDSequence[] m_BacktrackGlyphSequences;

        [SerializeField]
        [NativeName("inputGlyphSequences")]
        private GlyphIDSequence[] m_InputGlyphSequences;

        [SerializeField]
        [NativeName("lookaheadGlyphSequences")]
        private GlyphIDSequence[] m_LookaheadGlyphSequences;

        [SerializeField]
        [NativeName("sequenceLookupRecords")]
        private SequenceLookupRecord[] m_SequenceLookupRecords;
    }
}
