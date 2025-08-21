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
    /// A GlyphAnchorPoint defines the position of an anchor point relative to the origin of a glyph.
    /// This data structure is used by the Mark-to-Base, Mark-to-Mark and Mark-to-Ligature OpenType font features.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
    internal struct GlyphAnchorPoint
    {
        /// <summary>
        /// The x coordinate of the anchor point relative to the glyph.
        /// </summary>
        public float xCoordinate { get { return m_XCoordinate; } set { m_XCoordinate = value; } }

        /// <summary>
        /// The y coordinate of the anchor point relative to the glyph.
        /// </summary>
        public float yCoordinate { get { return m_YCoordinate; } set { m_YCoordinate = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        [NativeName("xPositionAdjustment")]
        private float m_XCoordinate;

        [SerializeField]
        [NativeName("yPositionAdjustment")]
        private float m_YCoordinate;
    }

    /// <summary>
    /// A MarkPositionAdjustment defines the positional adjustment of a glyph relative to the anchor point of base glyph.
    /// This data structure is used by the Mark-to-Base, Mark-to-Mark and Mark-to-Ligature OpenType font features.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
    internal struct MarkPositionAdjustment
    {
        /// <summary>
        /// The horizontal positional adjustment of the glyph relative to the anchor point of its parent base glyph.
        /// </summary>
        public float xPositionAdjustment { get { return m_XPositionAdjustment; } set { m_XPositionAdjustment = value; } }

        /// <summary>
        /// The vertical positional adjustment of the glyph relative to the anchor point of its parent base glyph.
        /// </summary>
        public float yPositionAdjustment { get { return m_YPositionAdjustment; } set { m_YPositionAdjustment = value; } }

        /// <summary>
        /// Constructor for a new MarkPositionAdjustment.
        /// </summary>
        /// <param name="x">The horizontal positional adjustment.</param>
        /// <param name="y">The vertical positional adjustment.</param>
        public MarkPositionAdjustment(float x, float y)
        {
            m_XPositionAdjustment = x;
            m_YPositionAdjustment = y;
        }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        [NativeName("xCoordinate")]
        private float m_XPositionAdjustment;

        [SerializeField]
        [NativeName("yCoordinate")]
        private float m_YPositionAdjustment;
    };

    /// <summary>
    /// A MarkAdjustmentRecord defines the position of an anchor point relative to the origin of a base glyph as well as the positional adjustment of a mark glyph relative to this anchor point.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct MarkAdjustmentRecord
    {
        /// <summary>
        /// The anchor point position relative to the base glyph's origin.
        /// </summary>
        public GlyphAnchorPoint anchorPosition { get { return m_AnchorPoint; } set { m_AnchorPoint = value; } }

        /// <summary>
        /// The positional adjustment of the mark glyph relative to the anchor point.
        /// </summary>
        public MarkPositionAdjustment markPositionAdjustment { get { return m_MarkPositionAdjustment; } set { m_MarkPositionAdjustment = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        [NativeName("anchorPoint")]
        private GlyphAnchorPoint m_AnchorPoint;

        [SerializeField]
        [NativeName("markPositionAdjustment")]
        private MarkPositionAdjustment m_MarkPositionAdjustment;
    }

    /// <summary>
    /// A MarkToBaseAdjustmentRecord defines the positional adjustment between a base glyph and mark glyph.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
    internal struct MarkToBaseAdjustmentRecord
    {
        /// <summary>
        /// The index of the base glyph.
        /// </summary>
        public uint baseGlyphID { get { return m_BaseGlyphID; } set { m_BaseGlyphID = value; } }

        /// <summary>
        /// The position of the anchor point of the base glyph.
        /// </summary>
        public GlyphAnchorPoint baseGlyphAnchorPoint { get { return m_BaseGlyphAnchorPoint; } set { m_BaseGlyphAnchorPoint = value; } }

        /// <summary>
        /// The index of the mark glyph.
        /// </summary>
        public uint markGlyphID { get { return m_MarkGlyphID; } set { m_MarkGlyphID = value; } }

        /// <summary>
        /// The positional adjustment of the mark glyph relative to the anchor point of the base glyph.
        /// </summary>
        public MarkPositionAdjustment markPositionAdjustment { get { return m_MarkPositionAdjustment; } set { m_MarkPositionAdjustment = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        [NativeName("baseGlyphID")]
        private uint m_BaseGlyphID;

        [SerializeField]
        [NativeName("baseAnchor")]
        private GlyphAnchorPoint m_BaseGlyphAnchorPoint;

        [SerializeField]
        [NativeName("markGlyphID")]
        private uint m_MarkGlyphID;

        [SerializeField]
        [NativeName("markPositionAdjustment")]
        private MarkPositionAdjustment m_MarkPositionAdjustment;
    }

    /// <summary>
    /// A MarkToMarkAdjustmentRecord defines the positional adjustment between two mark glyphs.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
    internal struct MarkToMarkAdjustmentRecord
    {
        /// <summary>
        /// The index of the base glyph.
        /// </summary>
        public uint baseMarkGlyphID { get { return m_BaseMarkGlyphID; } set { m_BaseMarkGlyphID = value; } }

        /// <summary>
        /// The position of the anchor point of the base mark glyph.
        /// </summary>
        public GlyphAnchorPoint baseMarkGlyphAnchorPoint { get { return m_BaseMarkGlyphAnchorPoint; } set { m_BaseMarkGlyphAnchorPoint = value; } }

        /// <summary>
        /// The index of the mark glyph.
        /// </summary>
        public uint combiningMarkGlyphID { get { return m_CombiningMarkGlyphID; } set { m_CombiningMarkGlyphID = value; } }

        /// <summary>
        /// The positional adjustment of the combining mark glyph relative to the anchor point of the base mark glyph.
        /// </summary>
        public MarkPositionAdjustment combiningMarkPositionAdjustment { get { return m_CombiningMarkPositionAdjustment; } set { m_CombiningMarkPositionAdjustment = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        [NativeName("baseMarkGlyphID")]
        private uint m_BaseMarkGlyphID;

        [SerializeField]
        [NativeName("baseMarkAnchor")]
        private GlyphAnchorPoint m_BaseMarkGlyphAnchorPoint;

        [SerializeField]
        [NativeName("combiningMarkGlyphID")]
        private uint m_CombiningMarkGlyphID;

        [SerializeField]
        [NativeName("combiningMarkPositionAdjustment")]
        private MarkPositionAdjustment m_CombiningMarkPositionAdjustment;
    }

    /// <summary>
    /// A MarkToLigatureAdjustmentRecord defines the potential positional adjustments for a mark relative to a ligature glyph.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    internal struct MarkToLigatureAdjustmentRecord
    {
        /// <summary>
        /// The index of the ligature glyph.
        /// </summary>
        public uint ligatureGlyphID { get { return m_LigatureGlyphID; } set { m_LigatureGlyphID = value; } }

        /// <summary>
        /// The index of the mark glyph.
        /// </summary>
        public uint combiningMarkGlyphID { get { return m_CombiningMarkGlyphID; } set { m_CombiningMarkGlyphID = value; } }

        /// <summary>
        /// Array that contains the positional adjustment records of the combining mark glyph relative to the ligature glyph.
        /// </summary>
        public MarkAdjustmentRecord[] combiningMarkAdjustmentRecords { get { return m_CombiningMarkAdjustmentRecords; } set { m_CombiningMarkAdjustmentRecords = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        [NativeName("ligatureGlyphID")]
        private uint m_LigatureGlyphID;

        [SerializeField]
        [NativeName("combiningMarkGlyphID")]
        private uint m_CombiningMarkGlyphID;

        [SerializeField]
        [NativeName("adjustmentRecords")]
        private MarkAdjustmentRecord[] m_CombiningMarkAdjustmentRecords;
    }
}
