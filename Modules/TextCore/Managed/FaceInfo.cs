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
    /// A structure that contains information about a given typeface and for a specific point size.
    /// </summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct FaceInfo
    {
        /// <summary>
        /// The name of the font typeface also known as family name.
        /// </summary>
        public string familyName { get { return m_FamilyName; } set { m_FamilyName = value; } }

        /// <summary>
        /// The style name of the typeface which defines both the visual style and weight of the typeface.
        /// </summary>
        public string styleName { get { return m_StyleName; } set { m_StyleName = value; } }

        /// <summary>
        /// The point size used for sampling the typeface. This is also referenced as sampling point size.
        /// </summary>
        public int pointSize { get { return m_PointSize; } set { m_PointSize = value; } }

        /// <summary>
        /// The relative scale of the typeface.
        /// Default value is 1.0f.
        /// </summary>
        public float scale { get { return m_Scale; } set { m_Scale = value; } }

        // Key metrics for the typeface

        /// <summary>
        /// The line height represents the distance between consecutive lines of text.
        /// This is the distance from baseline to baseline. It is usually computed as line height = ascent - descent + line gap.
        /// </summary>
        public float lineHeight { get { return m_LineHeight; } set { m_LineHeight = value; } }

        /// <summary>
        /// The Ascent line is typically located at the top of the tallest glyph in the typeface. This represents the distance between the baseline and the tallest ascender. This value is usually positive.
        /// </summary>
        public float ascentLine { get { return m_AscentLine; } set { m_AscentLine = value; } }

        /// <summary>
        /// The Cap line is typically located at the top of capital letters. This value represents the distance between the baseline and the top of capital letters.
        /// </summary>
        public float capLine { get { return m_CapLine; } set { m_CapLine = value; } }

        /// <summary>
        /// The Mean line is typically located at the top of lowercase letters. This value represents the distance between the baseline and the top of lowercase letters.
        /// </summary>
        public float meanLine { get { return m_MeanLine; } set { m_MeanLine = value; } }

        /// <summary>
        /// The Baseline is an imaginary line upon which all glyphs appear to rest on. This value is typically zero.
        /// </summary>
        public float baseline { get { return m_Baseline; } set { m_Baseline = value; } }

        /// <summary>
        /// The Descent line is typically located at the bottom of the glyph with the lowest descender in the typeface. This represents the distance between the baseline and the lowest descender. This value is usually negative.
        /// </summary>
        public float descentLine { get { return m_DescentLine; } set { m_DescentLine = value; } }

        /// <summary>
        /// The position of characters using superscript.
        /// </summary>
        public float superscriptOffset { get { return m_SuperscriptOffset; } set { m_SuperscriptOffset = value; } }

        /// <summary>
        /// The relative size / scale of superscript characters.
        /// </summary>
        public float superscriptSize { get { return m_SuperscriptSize; } set { m_SuperscriptSize = value; } }

        /// <summary>
        /// The position of characters using subscript.
        /// </summary>
        public float subscriptOffset { get { return m_SubscriptOffset; } set { m_SubscriptOffset = value; } }

        /// <summary>
        /// The relative size / scale of subscript characters.
        /// </summary>
        public float subscriptSize { get { return m_SubscriptSize; } set { m_SubscriptSize = value; } }

        /// <summary>
        /// The position of the underline.
        /// </summary>
        public float underlineOffset { get { return m_UnderlineOffset; } set { m_UnderlineOffset = value; } }

        /// <summary>
        /// The thickness of the underline.
        /// </summary>
        public float underlineThickness { get { return m_UnderlineThickness; } set { m_UnderlineThickness = value; } }

        /// <summary>
        /// The position of the strikethrough.
        /// </summary>
        public float strikethroughOffset { get { return m_StrikethroughOffset; } set { m_StrikethroughOffset = value; } }

        /// <summary>
        /// The thickness of the strikethrough.
        /// </summary>
        public float strikethroughThickness { get { return m_StrikethroughThickness; } set { m_StrikethroughThickness = value; } }

        /// <summary>
        /// The width of the tab character. This width is typically the same as the space character.
        /// </summary>
        public float tabWidth { get { return m_TabWidth; } set { m_TabWidth = value; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        [SerializeField]
        [NativeName("familyName")]
        private string m_FamilyName;

        [SerializeField]
        [NativeName("styleName")]
        private string m_StyleName;

        [SerializeField]
        [NativeName("pointSize")]
        private int m_PointSize;

        [SerializeField]
        [NativeName("scale")]
        private float m_Scale;

        [SerializeField]
        [NativeName("lineHeight")]
        private float m_LineHeight;

        [SerializeField]
        [NativeName("ascentLine")]
        private float m_AscentLine;

        [SerializeField]
        [NativeName("capLine")]
        private float m_CapLine;

        [SerializeField]
        [NativeName("meanLine")]
        private float m_MeanLine;

        [SerializeField]
        [NativeName("baseline")]
        private float m_Baseline;

        [SerializeField]
        [NativeName("descentLine")]
        private float m_DescentLine;

        [SerializeField]
        [NativeName("superscriptOffset")]
        private float m_SuperscriptOffset;

        [SerializeField]
        [NativeName("superscriptSize")]
        private float m_SuperscriptSize;

        [SerializeField]
        [NativeName("subscriptOffset")]
        private float m_SubscriptOffset;

        [SerializeField]
        [NativeName("subscriptSize")]
        private float m_SubscriptSize;

        [SerializeField]
        [NativeName("underlineOffset")]
        private float m_UnderlineOffset;

        [SerializeField]
        [NativeName("underlineThickness")]
        private float m_UnderlineThickness;

        [SerializeField]
        [NativeName("strikethroughOffset")]
        private float m_StrikethroughOffset;

        [SerializeField]
        [NativeName("strikethroughThickness")]
        private float m_StrikethroughThickness;

        [SerializeField]
        [NativeName("tabWidth")]
        private float m_TabWidth;

        /// <summary>
        /// Constructor used for testing
        /// </summary>
        internal FaceInfo(string familyName, string styleName, int pointSize, float scale, float lineHeight, float ascentLine, float capLine, float meanLine, float baseline, float descentLine, float superscriptOffset, float superscriptSize, float subscriptOffset, float subscriptSize, float underlineOffset, float underlineThickness, float strikethroughOffset, float strikethroughThickness, float tabWidth)
        {
            m_FamilyName = familyName;
            m_StyleName = styleName;

            m_PointSize = pointSize;
            m_Scale = scale;

            m_LineHeight = lineHeight;
            m_AscentLine = ascentLine;
            m_CapLine = capLine;
            m_MeanLine = meanLine;
            m_Baseline = baseline;
            m_DescentLine = descentLine;

            m_SuperscriptOffset = superscriptOffset;
            m_SuperscriptSize = superscriptSize;
            m_SubscriptOffset = subscriptOffset;
            m_SubscriptSize = subscriptSize;

            m_UnderlineOffset = underlineOffset;
            m_UnderlineThickness = underlineThickness;

            m_StrikethroughOffset = strikethroughOffset;
            m_StrikethroughThickness = strikethroughThickness;

            m_TabWidth = tabWidth;
        }

        /// <summary>
        /// Compares the information in this FaceInfo structure with the information in the given FaceInfo structure to determine whether they have the same values.
        /// </summary>
        /// <param name="other">The FaceInfo structure to compare this FaceInfo structure with.</param>
        /// <returns>Returns true if the FaceInfo structures have the same values. False if not.</returns>
        public bool Compare(FaceInfo other)
        {
            return familyName == other.familyName &&
                styleName == other.styleName &&
                pointSize == other.pointSize &&
                FontEngineUtilities.Approximately(scale, other.scale) &&
                FontEngineUtilities.Approximately(lineHeight, other.lineHeight) &&
                FontEngineUtilities.Approximately(ascentLine, other.ascentLine) &&
                FontEngineUtilities.Approximately(capLine, other.capLine) &&
                FontEngineUtilities.Approximately(meanLine, other.meanLine) &&
                FontEngineUtilities.Approximately(baseline, other.baseline) &&
                FontEngineUtilities.Approximately(descentLine, other.descentLine) &&
                FontEngineUtilities.Approximately(superscriptOffset, other.superscriptOffset) &&
                FontEngineUtilities.Approximately(superscriptSize, other.superscriptSize) &&
                FontEngineUtilities.Approximately(subscriptOffset, other.subscriptOffset) &&
                FontEngineUtilities.Approximately(subscriptSize, other.subscriptSize) &&
                FontEngineUtilities.Approximately(underlineOffset, other.underlineOffset) &&
                FontEngineUtilities.Approximately(underlineThickness, other.underlineThickness) &&
                FontEngineUtilities.Approximately(strikethroughOffset, other.strikethroughOffset) &&
                FontEngineUtilities.Approximately(strikethroughThickness, other.strikethroughThickness) &&
                FontEngineUtilities.Approximately(tabWidth, other.tabWidth);
        }
    }
}
