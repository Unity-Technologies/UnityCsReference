// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;
using UnityEngine.Profiling;
using UnityEngine.TextCore.LowLevel;
using System;
using System.Text;
using System.Globalization;
using Unity.Jobs.LowLevel.Unsafe;
using System.Threading.Tasks;

namespace UnityEngine.TextCore.Text
{
    internal partial class TextGenerator
    {
        // Character codes
        const int
            k_Tab = 9,
            k_LineFeed = 10,
            k_VerticalTab = 11,
            k_CarriageReturn = 13,
            k_Space = 32,
            k_DoubleQuotes = 34,
            k_NumberSign = 35,
            k_PercentSign = 37,
            k_SingleQuote = 39,
            k_Plus = 43,
            k_Period = 46,
            k_LesserThan = 60,
            k_Equal = 61,
            k_GreaterThan = 62,
            k_Underline = 95,
            k_NoBreakSpace = 0x00A0,
            k_SoftHyphen = 0x00AD,
            k_HyphenMinus = 0x002D,
            k_FigureSpace = 0x2007,
            k_Hyphen = 0x2010,
            k_NonBreakingHyphen = 0x2011,
            k_ZeroWidthSpace = 0x200B,
            k_NarrowNoBreakSpace = 0x202F,
            k_WordJoiner = 0x2060,
            k_HorizontalEllipsis = 0x2026,
            k_LineSeparator = 0x2028,
            k_ParagraphSeparator = 0x2029,
            k_RightSingleQuote = 8217,
            k_Square = 9633,
            k_HangulJamoStart = 0x1100,
            k_HangulJamoEnd = 0x11ff,
            k_CjkStart = 0x2E80,
            k_CjkEnd = 0x9FFF,
            k_HangulJameExtendedStart = 0xA960,
            k_HangulJameExtendedEnd = 0xA97F,
            k_HangulSyllablesStart = 0xAC00,
            k_HangulSyllablesEnd = 0xD7FF,
            k_CjkIdeographsStart = 0xF900,
            k_CjkIdeographsEnd = 0xFAFF,
            k_CjkFormsStart = 0xFE30,
            k_CjkFormsEnd = 0xFE4F,
            k_CjkHalfwidthStart = 0xFF00,
            k_CjkHalfwidthEnd = 0xFFEF,
            k_EndOfText = 0x03;


        const float k_FloatUnset = -32767;
        const int k_MaxCharacters = 8; // Determines the initial allocation and size of the character array / buffer.

        static TextGenerator s_TextGenerator;

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static TextGenerator GetTextGenerator()
        {
            if (s_TextGenerator == null)
            {
                s_TextGenerator = new TextGenerator();
                s_DefaultSpriteAsset = Resources.Load<SpriteAsset>("Sprite Assets/Default Sprite Asset");
            }

            return s_TextGenerator;
        }

        public static void GenerateText(TextGenerationSettings settings, TextInfo textInfo)
        {
            bool isMainThread = !JobsUtility.IsExecutingJob;
            if (settings.fontAsset == null || settings.fontAsset.characterLookupTable == null)
            {
                Debug.LogWarning("Can't Generate Mesh, No Font Asset has been assigned.");
                return;
            }

            if (textInfo == null)
            {
                // TODO: If no textInfo is passed, we want to use an internal one instead of creating a new one everytime.
                Debug.LogError("Null TextInfo provided to TextGenerator. Cannot update its content.");
                return;
            }

            TextGenerator textGenerator = GetTextGenerator();

            Profiler.BeginSample("TextGenerator.GenerateText");
            textGenerator.Prepare(settings, textInfo);

            // Update font asset atlas textures and font features.
            if (isMainThread)
                FontAsset.UpdateFontAssetsInUpdateQueue();

            textGenerator.GenerateTextMesh(settings, textInfo);
            Profiler.EndSample();
        }

        /// <summary>
        /// Internal array containing the converted source text used in the text parsing process.
        /// </summary>
        private TextBackingContainer m_TextBackingArray = new TextBackingContainer(4);

        /// <summary>
        /// Array containing the Unicode characters to be parsed.
        /// </summary>
        internal TextProcessingElement[] m_TextProcessingArray = new TextProcessingElement[8];

        /// <summary>
        /// The number of Unicode characters that have been parsed and contained in the m_InternalParsingBuffer
        /// </summary>
        internal int m_InternalTextProcessingArraySize;

        /// <summary>
        /// Determines if the data structures allocated to contain the geometry of the text object will be reduced in size if the number of characters required to display the text is reduced by more than 256 characters.
        /// This reduction has the benefit of reducing the amount of vertex data being submitted to the graphic device but results in GC when it occurs.
        /// </summary>
        bool vertexBufferAutoSizeReduction
        {
            get { return m_VertexBufferAutoSizeReduction; }
            set { m_VertexBufferAutoSizeReduction = value; }
        }
        [SerializeField]
        protected bool m_VertexBufferAutoSizeReduction = false;

        private char[] m_HtmlTag = new char[256]; // Maximum length of rich text tag. This is pre-allocated to avoid GC.

        internal HighlightState m_HighlightState = new HighlightState(Color.white, Offset.zero);

        protected bool m_IsIgnoringAlignment;

        /// <summary>
        /// Property indicating whether the text is Truncated or using Ellipsis.
        /// </summary>
        public static bool isTextTruncated { get { return m_IsTextTruncated; } }
        static protected bool m_IsTextTruncated;

        /// <summary>
        /// Delegate for the OnMissingCharacter event called when the requested Unicode character is missing from the font asset.
        /// </summary>
        /// <param name="unicode">The Unicode of the missing character.</param>
        /// <param name="stringIndex">The index of the missing character in the source string.</param>
        /// <param name="text">The source text that contains the missing character.</param>
        /// <param name="fontAsset">The font asset that is missing the requested characters.</param>
        /// <param name="textComponent">The text component where the requested character is missing.</param>
        public delegate void MissingCharacterEventCallback(uint unicode, int stringIndex, TextInfo text, FontAsset fontAsset);

        /// <summary>
        /// Event delegate to be called when the requested Unicode character is missing from the font asset.
        /// </summary>
        public static event MissingCharacterEventCallback OnMissingCharacter;

        Vector3[] m_RectTransformCorners = new Vector3[4];
        float m_MarginWidth;
        float m_MarginHeight;

        float m_PreferredWidth;
        float m_PreferredHeight;
        FontAsset m_CurrentFontAsset;
        Material m_CurrentMaterial;
        int m_CurrentMaterialIndex;
        TextProcessingStack<MaterialReference> m_MaterialReferenceStack = new TextProcessingStack<MaterialReference>(new MaterialReference[16]);
        float m_Padding;
        SpriteAsset m_CurrentSpriteAsset;
        int m_TotalCharacterCount;
        float m_FontSize;
        float m_FontScaleMultiplier;
        float m_CurrentFontSize;
        TextProcessingStack<float> m_SizeStack = new TextProcessingStack<float>(16);

        // STYLE TAGS
        protected TextProcessingStack<int>[] m_TextStyleStacks = new TextProcessingStack<int>[8];
        protected int m_TextStyleStackDepth = 0;

        FontStyles m_FontStyleInternal = FontStyles.Normal;
        FontStyleStack m_FontStyleStack;

        TextFontWeight m_FontWeightInternal = TextFontWeight.Regular;
        TextProcessingStack<TextFontWeight> m_FontWeightStack = new TextProcessingStack<TextFontWeight>(8);

        TextAlignment m_LineJustification;
        TextProcessingStack<TextAlignment> m_LineJustificationStack = new TextProcessingStack<TextAlignment>(16);
        float m_BaselineOffset;
        TextProcessingStack<float> m_BaselineOffsetStack = new TextProcessingStack<float>(new float[16]);
        Color32 m_FontColor32;
        Color32 m_HtmlColor;
        Color32 m_UnderlineColor;
        Color32 m_StrikethroughColor;
        TextProcessingStack<Color32> m_ColorStack = new TextProcessingStack<Color32>(new Color32[16]);
        TextProcessingStack<Color32> m_UnderlineColorStack = new TextProcessingStack<Color32>(new Color32[16]);
        TextProcessingStack<Color32> m_StrikethroughColorStack = new TextProcessingStack<Color32>(new Color32[16]);
        TextProcessingStack<Color32> m_HighlightColorStack = new TextProcessingStack<Color32>(new Color32[16]);
        TextProcessingStack<HighlightState> m_HighlightStateStack = new TextProcessingStack<HighlightState>(new HighlightState[16]);
        TextProcessingStack<int> m_ItalicAngleStack = new TextProcessingStack<int>(new int[16]);
        TextColorGradient m_ColorGradientPreset;
        TextProcessingStack<TextColorGradient> m_ColorGradientStack = new TextProcessingStack<TextColorGradient>(new TextColorGradient[16]);
        bool m_ColorGradientPresetIsTinted;
        TextProcessingStack<int> m_ActionStack = new TextProcessingStack<int>(new int[16]);
        float m_LineOffset;
        float m_LineHeight;
        bool m_IsDrivenLineSpacing;
        float m_CSpacing;
        float m_MonoSpacing;
        bool m_DuoSpace;
        float m_XAdvance;
        float m_TagLineIndent;
        float m_TagIndent;
        TextProcessingStack<float> m_IndentStack = new TextProcessingStack<float>(new float[16]);
        bool m_TagNoParsing;
        int m_CharacterCount;
        int m_FirstCharacterOfLine;
        int m_LastCharacterOfLine;
        int m_FirstVisibleCharacterOfLine;
        int m_LastVisibleCharacterOfLine;
        float m_MaxLineAscender;
        float m_MaxLineDescender;
        int m_LineNumber;
        int m_LineVisibleCharacterCount;
        int m_LineVisibleSpaceCount;
        int m_FirstOverflowCharacterIndex;
        int m_PageNumber;
        float m_MarginLeft;
        float m_MarginRight;
        float m_Width;
        Extents m_MeshExtents;
        float m_MaxCapHeight;
        float m_MaxAscender;
        float m_MaxDescender;
        bool m_IsNewPage;
        bool m_IsNonBreakingSpace;
        WordWrapState m_SavedWordWrapState;
        WordWrapState m_SavedLineState;
        WordWrapState m_SavedEllipsisState = new WordWrapState();
        WordWrapState m_SavedLastValidState = new WordWrapState();
        WordWrapState m_SavedSoftLineBreakState = new WordWrapState();
        TextElementType m_TextElementType;
        bool m_isTextLayoutPhase;
        int m_SpriteIndex;
        Color32 m_SpriteColor;
        TextElement m_CachedTextElement;
        Color32 m_HighlightColor;
        float m_CharWidthAdjDelta;
        float m_MaxFontSize;
        float m_MinFontSize;
        int m_AutoSizeIterationCount;
        int m_AutoSizeMaxIterationCount = 100;
        float m_StartOfLineAscender;
        float m_LineSpacingDelta;
        internal MaterialReference[] m_MaterialReferences = new MaterialReference[8];
        int m_SpriteCount = 0;
        TextProcessingStack<int> m_StyleStack = new TextProcessingStack<int>(new int[16]);
        TextProcessingStack<WordWrapState> m_EllipsisInsertionCandidateStack = new TextProcessingStack<WordWrapState>(8, 8);
        int m_SpriteAnimationId;
        int m_ItalicAngle;
        Vector3 m_FXScale;
        Quaternion m_FXRotation;

        int m_LastBaseGlyphIndex;
        float m_PageAscender;

        RichTextTagAttribute[] m_XmlAttribute = new RichTextTagAttribute[8];
        private float[] m_AttributeParameterValues = new float[16];

        Dictionary<int, int> m_MaterialReferenceIndexLookup = new Dictionary<int, int>();
        bool m_IsCalculatingPreferredValues;
        static SpriteAsset s_DefaultSpriteAsset;
        bool m_TintSprite;

        protected SpecialCharacter m_Ellipsis;
        protected SpecialCharacter m_Underline;

        TextElementInfo[] m_InternalTextElementInfo;

        /// <summary>
        /// This is the main function that is responsible for creating / displaying the text.
        /// </summary>
        internal void GenerateTextMesh(TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            // Early exit if no font asset was assigned. This should not be needed since LiberationSans SDF will be assigned by default.
            if (generationSettings.fontAsset == null || generationSettings.fontAsset.characterLookupTable == null)
            {
                Debug.LogWarning("Can't Generate Mesh! No Font Asset has been assigned.");
                return;
            }

            // Clear TextInfo
            if (textInfo != null)
                textInfo.Clear();

            // Early exit if we don't have any Text to generate.
            if (m_TextProcessingArray == null || m_TextProcessingArray.Length == 0 || m_TextProcessingArray[0].unicode == 0)
            {
                // Clear mesh and upload changes to the mesh.
                ClearMesh(true, textInfo);

                m_PreferredWidth = 0;
                m_PreferredHeight = 0;

                return;
            }

            float fontSizeDelta = 0;

            // *** PHASE I of Text Generation ***
            ParsingPhase(textInfo, generationSettings, out uint charCode, out float maxVisibleDescender);

            // Check Auto Sizing and increase font size to fill text container.
            #region Check Auto-Sizing (Upper Font Size Bounds)

            fontSizeDelta = m_MaxFontSize - m_MinFontSize;
            if ( /* !m_isCharacterWrappingEnabled && */ generationSettings.autoSize && fontSizeDelta > 0.051f && m_FontSize < generationSettings.fontSizeMax && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
            {
                // Reset character width adjustment delta
                if (m_CharWidthAdjDelta < generationSettings.charWidthMaxAdj / 100)
                    m_CharWidthAdjDelta = 0;

                m_MinFontSize = m_FontSize;

                float sizeDelta = Mathf.Max((m_MaxFontSize - m_FontSize) / 2, 0.05f);
                m_FontSize += sizeDelta;
                m_FontSize = Mathf.Min((int)(m_FontSize * 20 + 0.5f) / 20f, generationSettings.charWidthMaxAdj);

                return;
            }

            #endregion End Auto-sizing Check

            if (m_AutoSizeIterationCount >= m_AutoSizeMaxIterationCount)
                Debug.Log("Auto Size Iteration Count: " + m_AutoSizeIterationCount + ". Final Point Size: " + m_FontSize);

            // If there are no visible characters or only character is End of Text (0x03)... no need to continue
            if (m_CharacterCount == 0 || (m_CharacterCount == 1 && charCode == k_EndOfText))
            {
                ClearMesh(true, textInfo);
                return;
            }

            // *** PHASE II of Text Generation ***
            LayoutPhase(textInfo, generationSettings, maxVisibleDescender);

            // Phase III - Update Mesh Vertex Data
            // *** UPLOAD MESH DATA ***
            for (int i = 1; i < textInfo.materialCount; i++)
            {
                // Clear unused vertices
                textInfo.meshInfo[i].ClearUnusedVertices();

                // Sort the geometry of the sub-text objects if needed.
                if (generationSettings.geometrySortingOrder != VertexSortingOrder.Normal)
                    textInfo.meshInfo[i].SortGeometry(VertexSortingOrder.Reverse);
            }
        }
    }
}
