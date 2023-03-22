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

namespace UnityEngine.TextCore.Text
{
    internal class TextGenerationSettings : IEquatable<TextGenerationSettings>
    {
        public string text;

        public Rect screenRect;
        public Vector4 margins;
        public float scale = 1f;

        public FontAsset fontAsset;
        public Material material;
        public SpriteAsset spriteAsset;
        public TextStyleSheet styleSheet;
        public FontStyles fontStyle = FontStyles.Normal;

        public TextSettings textSettings;

        public TextAlignment textAlignment = TextAlignment.TopLeft;
        public TextOverflowMode overflowMode = TextOverflowMode.Overflow;
        public bool wordWrap = false;
        public float wordWrappingRatio;

        public Color color = Color.white;
        public TextColorGradient fontColorGradient;
        public TextColorGradient fontColorGradientPreset;
        public bool tintSprites;
        public bool overrideRichTextColors;
        public bool shouldConvertToLinearSpace = true;

        public float fontSize = 18;
        public bool autoSize;
        public float fontSizeMin;
        public float fontSizeMax;

        public bool enableKerning = true;
        public bool richText;
        public bool isRightToLeft;
        public bool extraPadding;
        public bool parseControlCharacters = true;
        public bool isOrthographic = true;
        public bool tagNoParsing = false;

        public float characterSpacing;
        public float wordSpacing;
        public float lineSpacing;
        public float paragraphSpacing;
        public float lineSpacingMax;
        public TextWrappingMode textWrappingMode = TextWrappingMode.Normal;

        public int maxVisibleCharacters = 99999;
        public int maxVisibleWords = 99999;
        public int maxVisibleLines = 99999;
        public int firstVisibleCharacter = 0;
        public bool useMaxVisibleDescender;

        public TextFontWeight fontWeight = TextFontWeight.Regular;
        public int pageToDisplay = 1;

        public TextureMapping horizontalMapping = TextureMapping.Character;
        public TextureMapping verticalMapping = TextureMapping.Character;
        public float uvLineOffset;
        public VertexSortingOrder geometrySortingOrder = VertexSortingOrder.Normal;
        public bool inverseYAxis;

        public float charWidthMaxAdj;
        internal TextInputSource inputSource = TextInputSource.TextString;

        public bool Equals(TextGenerationSettings other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return text == other.text && screenRect.Equals(other.screenRect) && margins.Equals(other.margins) &&
                scale.Equals(other.scale) && Equals(fontAsset, other.fontAsset) && Equals(material, other.material) &&
                Equals(spriteAsset, other.spriteAsset) && Equals(styleSheet, other.styleSheet) &&
                fontStyle == other.fontStyle && Equals(textSettings, other.textSettings) &&
                textAlignment == other.textAlignment && overflowMode == other.overflowMode &&
                wordWrap == other.wordWrap && wordWrappingRatio.Equals(other.wordWrappingRatio) &&
                color.Equals(other.color) && Equals(fontColorGradient, other.fontColorGradient) &&
                Equals(fontColorGradientPreset, other.fontColorGradientPreset) && tintSprites == other.tintSprites &&
                overrideRichTextColors == other.overrideRichTextColors &&
                shouldConvertToLinearSpace == other.shouldConvertToLinearSpace && fontSize.Equals(other.fontSize) &&
                autoSize == other.autoSize && fontSizeMin.Equals(other.fontSizeMin) &&
                fontSizeMax.Equals(other.fontSizeMax) && enableKerning == other.enableKerning &&
                richText == other.richText && isRightToLeft == other.isRightToLeft &&
                extraPadding == other.extraPadding && parseControlCharacters == other.parseControlCharacters &&
                isOrthographic == other.isOrthographic && tagNoParsing == other.tagNoParsing &&
                characterSpacing.Equals(other.characterSpacing) && wordSpacing.Equals(other.wordSpacing) &&
                lineSpacing.Equals(other.lineSpacing) && paragraphSpacing.Equals(other.paragraphSpacing) &&
                lineSpacingMax.Equals(other.lineSpacingMax) && textWrappingMode == other.textWrappingMode &&
                maxVisibleCharacters == other.maxVisibleCharacters && maxVisibleWords == other.maxVisibleWords &&
                maxVisibleLines == other.maxVisibleLines && firstVisibleCharacter == other.firstVisibleCharacter &&
                useMaxVisibleDescender == other.useMaxVisibleDescender && fontWeight == other.fontWeight &&
                pageToDisplay == other.pageToDisplay && horizontalMapping == other.horizontalMapping &&
                verticalMapping == other.verticalMapping && uvLineOffset.Equals(other.uvLineOffset) &&
                geometrySortingOrder == other.geometrySortingOrder && inverseYAxis == other.inverseYAxis &&
                charWidthMaxAdj.Equals(other.charWidthMaxAdj) && inputSource == other.inputSource;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TextGenerationSettings)obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(text);
            hashCode.Add(screenRect);
            hashCode.Add(margins);
            hashCode.Add(scale);
            hashCode.Add(fontAsset);
            hashCode.Add(material);
            hashCode.Add(spriteAsset);
            hashCode.Add(styleSheet);
            hashCode.Add((int)fontStyle);
            hashCode.Add(textSettings);
            hashCode.Add((int)textAlignment);
            hashCode.Add((int)overflowMode);
            hashCode.Add(wordWrap);
            hashCode.Add(wordWrappingRatio);
            hashCode.Add(color);
            hashCode.Add(fontColorGradient);
            hashCode.Add(fontColorGradientPreset);
            hashCode.Add(tintSprites);
            hashCode.Add(overrideRichTextColors);
            hashCode.Add(shouldConvertToLinearSpace);
            hashCode.Add(fontSize);
            hashCode.Add(autoSize);
            hashCode.Add(fontSizeMin);
            hashCode.Add(fontSizeMax);
            hashCode.Add(enableKerning);
            hashCode.Add(richText);
            hashCode.Add(isRightToLeft);
            hashCode.Add(extraPadding);
            hashCode.Add(parseControlCharacters);
            hashCode.Add(isOrthographic);
            hashCode.Add(tagNoParsing);
            hashCode.Add(characterSpacing);
            hashCode.Add(wordSpacing);
            hashCode.Add(lineSpacing);
            hashCode.Add(paragraphSpacing);
            hashCode.Add(lineSpacingMax);
            hashCode.Add((int)textWrappingMode);
            hashCode.Add(maxVisibleCharacters);
            hashCode.Add(maxVisibleWords);
            hashCode.Add(maxVisibleLines);
            hashCode.Add(firstVisibleCharacter);
            hashCode.Add(useMaxVisibleDescender);
            hashCode.Add((int)fontWeight);
            hashCode.Add(pageToDisplay);
            hashCode.Add((int)horizontalMapping);
            hashCode.Add((int)verticalMapping);
            hashCode.Add(uvLineOffset);
            hashCode.Add((int)geometrySortingOrder);
            hashCode.Add(inverseYAxis);
            hashCode.Add(charWidthMaxAdj);
            hashCode.Add((int)inputSource);
            return hashCode.ToHashCode();
        }

        public static bool operator ==(TextGenerationSettings left, TextGenerationSettings right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TextGenerationSettings left, TextGenerationSettings right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return $"{nameof(text)}: {text}\n {nameof(screenRect)}: {screenRect}\n {nameof(margins)}: {margins}\n {nameof(scale)}: {scale}\n {nameof(fontAsset)}: {fontAsset}\n {nameof(material)}: {material}\n {nameof(spriteAsset)}: {spriteAsset}\n {nameof(styleSheet)}: {styleSheet}\n {nameof(fontStyle)}: {fontStyle}\n {nameof(textSettings)}: {textSettings}\n {nameof(textAlignment)}: {textAlignment}\n {nameof(overflowMode)}: {overflowMode}\n {nameof(wordWrap)}: {wordWrap}\n {nameof(wordWrappingRatio)}: {wordWrappingRatio}\n {nameof(color)}: {color}\n {nameof(fontColorGradient)}: {fontColorGradient}\n {nameof(fontColorGradientPreset)}: {fontColorGradientPreset}\n {nameof(tintSprites)}: {tintSprites}\n {nameof(overrideRichTextColors)}: {overrideRichTextColors}\n {nameof(shouldConvertToLinearSpace)}: {shouldConvertToLinearSpace}\n {nameof(fontSize)}: {fontSize}\n {nameof(autoSize)}: {autoSize}\n {nameof(fontSizeMin)}: {fontSizeMin}\n {nameof(fontSizeMax)}: {fontSizeMax}\n {nameof(enableKerning)}: {enableKerning}\n {nameof(richText)}: {richText}\n {nameof(isRightToLeft)}: {isRightToLeft}\n {nameof(extraPadding)}: {extraPadding}\n {nameof(parseControlCharacters)}: {parseControlCharacters}\n {nameof(isOrthographic)}: {isOrthographic}\n {nameof(tagNoParsing)}: {tagNoParsing}\n {nameof(characterSpacing)}: {characterSpacing}\n {nameof(wordSpacing)}: {wordSpacing}\n {nameof(lineSpacing)}: {lineSpacing}\n {nameof(paragraphSpacing)}: {paragraphSpacing}\n {nameof(lineSpacingMax)}: {lineSpacingMax}\n {nameof(textWrappingMode)}: {textWrappingMode}\n {nameof(maxVisibleCharacters)}: {maxVisibleCharacters}\n {nameof(maxVisibleWords)}: {maxVisibleWords}\n {nameof(maxVisibleLines)}: {maxVisibleLines}\n {nameof(firstVisibleCharacter)}: {firstVisibleCharacter}\n {nameof(useMaxVisibleDescender)}: {useMaxVisibleDescender}\n {nameof(fontWeight)}: {fontWeight}\n {nameof(pageToDisplay)}: {pageToDisplay}\n {nameof(horizontalMapping)}: {horizontalMapping}\n {nameof(verticalMapping)}: {verticalMapping}\n {nameof(uvLineOffset)}: {uvLineOffset}\n {nameof(geometrySortingOrder)}: {geometrySortingOrder}\n {nameof(inverseYAxis)}: {inverseYAxis}\n {nameof(charWidthMaxAdj)}: {charWidthMaxAdj}\n {nameof(inputSource)}: {inputSource}";        }
        }

    internal class TextGenerator
    {
        // Character codes
        const int
            k_Tab = 9,
            k_LineFeed = 10,
            k_CarriageReturn = 13,
            k_Space = 32,
            k_DoubleQuotes = 34,
            k_NumberSign = 35,
            k_PercentSign = 37,
            k_SingleQuote = 39,
            k_Plus = 43,
            k_Minus = 45,
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
        static TextGenerator GetTextGenerator()
        {
            if (s_TextGenerator == null)
            {
                s_TextGenerator = new TextGenerator();
            }

            return s_TextGenerator;
        }

        public static void GenerateText(TextGenerationSettings settings, TextInfo textInfo)
        {
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
            FontAsset.UpdateFontAssetsInUpdateQueue();

            textGenerator.GenerateTextMesh(settings, textInfo);
            Profiler.EndSample();
        }

        public static Vector2 GetCursorPosition(TextGenerationSettings settings, int index)
        {
            if (settings.fontAsset == null || settings.fontAsset.characterLookupTable == null)
            {
                Debug.LogWarning("Can't Generate Mesh, No Font Asset has been assigned.");
                return Vector2.zero;
            }

            TextInfo textInfo = new TextInfo();
            GenerateText(settings, textInfo);

            return GetCursorPosition(textInfo, settings.screenRect, index);
        }

        public static Vector2 GetCursorPosition(TextInfo textInfo, Rect screenRect, int index, bool inverseYAxis = true)
        {
            var result = screenRect.position;
            if (textInfo.characterCount == 0)
                return result;

            var character = textInfo.textElementInfo[textInfo.characterCount - 1];
            var line = textInfo.lineInfo[character.lineNumber];
            var lineGap = line.lineHeight - (line.ascender - line.descender);
            if (index >= textInfo.characterCount)
            {
                result += inverseYAxis ?
                    new Vector2(character.xAdvance, screenRect.height - line.ascender - lineGap) :
                    new Vector2(character.xAdvance , line.descender);
                return result;
            }

            character = textInfo.textElementInfo[index];
            line = textInfo.lineInfo[character.lineNumber];
            lineGap = line.lineHeight - (line.ascender - line.descender);

            result += inverseYAxis ?
                new Vector2(character.origin , screenRect.height - line.ascender - lineGap) :
                new Vector2(character.origin , line.descender);

            return result;
        }

        public static float GetPreferredWidth(TextGenerationSettings settings, TextInfo textInfo)
        {
            if (settings.fontAsset == null || settings.fontAsset.characterLookupTable == null)
            {
                Debug.LogWarning("Can't Generate Mesh, No Font Asset has been assigned.");
                return 0f;
            }

            TextGenerator textGenerator = GetTextGenerator();

            textGenerator.Prepare(settings, textInfo);
            return textGenerator.GetPreferredWidthInternal(settings, textInfo);
        }

        public static float GetPreferredHeight(TextGenerationSettings settings, TextInfo textInfo)
        {
            if (settings.fontAsset == null || settings.fontAsset.characterLookupTable == null)
            {
                Debug.LogWarning("Can't Generate Mesh, No Font Asset has been assigned.");
                return 0f;
            }

            TextGenerator textGenerator = GetTextGenerator();

            textGenerator.Prepare(settings, textInfo);
            return textGenerator.GetPreferredHeightInternal(settings, textInfo);
        }

        public static Vector2 GetPreferredValues(TextGenerationSettings settings, TextInfo textInfo)
        {
            if (settings.fontAsset == null || settings.fontAsset.characterLookupTable == null)
            {
                Debug.LogWarning("Can't Generate Mesh, No Font Asset has been assigned.");
                return Vector2.zero;
            }

            TextGenerator textGenerator = GetTextGenerator();

            textGenerator.Prepare(settings, textInfo);
            return textGenerator.GetPreferredValuesInternal(settings, textInfo);
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

        private char[] m_HtmlTag = new char[128]; // Maximum length of rich text tag. This is pre-allocated to avoid GC.

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
        bool m_IsAutoSizePointSizeSet;
        float m_StartOfLineAscender;
        float m_LineSpacingDelta;
        bool m_IsMaskingEnabled;
        MaterialReference[] m_MaterialReferences = new MaterialReference[8];
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
        SpriteAsset m_DefaultSpriteAsset;
        bool m_TintSprite;

        protected SpecialCharacter m_Ellipsis;
        protected SpecialCharacter m_Underline;

        bool m_IsUsingBold;
        bool m_IsSdfShader;

        TextElementInfo[] m_InternalTextElementInfo;

        void Prepare(TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            Profiler.BeginSample("TextGenerator.Prepare");
            // TODO: Find a way for GetPaddingForMaterial to not allocate
            // TODO: Hard coded padding value is temporary change to avoid clipping of text geometry with small point size.
            m_Padding = 6.0f; // generationSettings.extraPadding ? 5.5f : 1.5f;

            m_IsMaskingEnabled = false;

            // Set the font style that is assigned by the builder
            m_FontStyleInternal = generationSettings.fontStyle;
            m_FontWeightInternal = (m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold ? TextFontWeight.Bold : generationSettings.fontWeight;

            // Find and cache Underline & Ellipsis characters.
            GetSpecialCharacters(generationSettings);

            ComputeMarginSize(generationSettings.screenRect, generationSettings.margins);

            //ParseInputText
            PopulateTextBackingArray(generationSettings.text);
            PopulateTextProcessingArray(generationSettings);
            SetArraySizes(m_TextProcessingArray, generationSettings, textInfo);

            // Reset Font min / max used with Auto-sizing
            if (generationSettings.autoSize)
                m_FontSize = Mathf.Clamp(generationSettings.fontSize, generationSettings.fontSizeMin, generationSettings.fontSizeMax);
            else
                m_FontSize = generationSettings.fontSize;

            m_MaxFontSize = generationSettings.fontSizeMax;
            m_MinFontSize = generationSettings.fontSizeMin;
            m_LineSpacingDelta = 0;
            m_CharWidthAdjDelta = 0;

            Profiler.EndSample();
        }

        /// <summary>
        /// This is the main function that is responsible for creating / displaying the text.
        /// </summary>
        void GenerateTextMesh(TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            // Early exit if no font asset was assigned. This should not be needed since LiberationSans SDF will be assigned by default.
            if (generationSettings.fontAsset == null || generationSettings.fontAsset.characterLookupTable == null)
            {
                Debug.LogWarning("Can't Generate Mesh! No Font Asset has been assigned.");
                m_IsAutoSizePointSizeSet = true;
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
                m_IsAutoSizePointSizeSet = true;

                return;
            }

            m_CurrentFontAsset = generationSettings.fontAsset;
            m_CurrentMaterial = generationSettings.material;
            m_CurrentMaterialIndex = 0;
            m_MaterialReferenceStack.SetDefault(new MaterialReference(m_CurrentMaterialIndex, m_CurrentFontAsset, null, m_CurrentMaterial, m_Padding));

            m_CurrentSpriteAsset = generationSettings.spriteAsset;

            // Total character count is computed when the text is parsed.
            int totalCharacterCount = m_TotalCharacterCount;

            // Calculate the scale of the font based on selected font size and sampling point size.
            // baseScale is calculated using the font asset assigned to the text object.
            float baseScale = (m_FontSize / generationSettings.fontAsset.m_FaceInfo.pointSize * generationSettings.fontAsset.m_FaceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f));
            float currentElementScale = baseScale;
            float currentEmScale = m_FontSize * 0.01f * (generationSettings.isOrthographic ? 1 : 0.1f);
            m_FontScaleMultiplier = 1;

            m_CurrentFontSize = m_FontSize;
            m_SizeStack.SetDefault(m_CurrentFontSize);
            float fontSizeDelta = 0;

            uint charCode = 0; // Holds the character code of the currently being processed character.

            m_FontStyleInternal = generationSettings.fontStyle; // Set the default style.
            m_FontWeightInternal = (m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold ? TextFontWeight.Bold : generationSettings.fontWeight;
            m_FontWeightStack.SetDefault(m_FontWeightInternal);
            m_FontStyleStack.Clear();

            m_LineJustification = generationSettings.textAlignment;// m_textAlignment; // Sets the line justification mode to match editor alignment.
            m_LineJustificationStack.SetDefault(m_LineJustification);

            float padding = 0;
            //float boldXAdvanceMultiplier = 1; // Used to increase spacing between character when style is bold.

            m_BaselineOffset = 0; // Used by subscript characters.
            m_BaselineOffsetStack.Clear();

            // Underline
            bool beginUnderline = false;
            Vector3 underlineStart = Vector3.zero; // Used to track where underline starts & ends.
            Vector3 underlineEnd = Vector3.zero;

            // Strike-through
            bool beginStrikethrough = false;
            Vector3 strikethroughStart = Vector3.zero;
            Vector3 strikethroughEnd = Vector3.zero;

            // Text Highlight
            bool beginHighlight = false;
            Vector3 highlightStart = Vector3.zero;
            Vector3 highlightEnd = Vector3.zero;

            m_FontColor32 = generationSettings.color;
            m_HtmlColor = m_FontColor32;
            m_UnderlineColor = m_HtmlColor;
            m_StrikethroughColor = m_HtmlColor;

            m_ColorStack.SetDefault(m_HtmlColor);
            m_UnderlineColorStack.SetDefault(m_HtmlColor);
            m_StrikethroughColorStack.SetDefault(m_HtmlColor);
            m_HighlightStateStack.SetDefault(new HighlightState(m_HtmlColor, Offset.zero));

            m_ColorGradientPreset = null;
            m_ColorGradientStack.SetDefault(null);

            m_ItalicAngle = m_CurrentFontAsset.italicStyleSlant;
            m_ItalicAngleStack.SetDefault(m_ItalicAngle);

            // Clear the Action stack.
            m_ActionStack.Clear();

            m_FXScale = Vector3.one;
            m_FXRotation = Quaternion.identity;

            m_LineOffset = 0; // Amount of space between lines (font line spacing + m_linespacing).
            m_LineHeight = k_FloatUnset;
            float lineGap = m_CurrentFontAsset.faceInfo.lineHeight - (m_CurrentFontAsset.m_FaceInfo.ascentLine - m_CurrentFontAsset.m_FaceInfo.descentLine);

            m_CSpacing = 0; // Amount of space added between characters as a result of the use of the <cspace> tag.
            m_MonoSpacing = 0;
            m_XAdvance = 0; // Used to track the position of each character.

            m_TagLineIndent = 0; // Used for indentation of text.
            m_TagIndent = 0;
            m_IndentStack.SetDefault(0);
            m_TagNoParsing = false;

            m_CharacterCount = 0; // Total characters in the char[]

            // Tracking of line information
            m_FirstCharacterOfLine = 0;
            m_LastCharacterOfLine = 0;
            m_FirstVisibleCharacterOfLine = 0;
            m_LastVisibleCharacterOfLine = 0;
            m_MaxLineAscender = TextGeneratorUtilities.largeNegativeFloat;
            m_MaxLineDescender = TextGeneratorUtilities.largePositiveFloat;
            m_LineNumber = 0;
            m_StartOfLineAscender = 0;
            m_LineVisibleCharacterCount = 0;
            m_LineVisibleSpaceCount = 0;
            bool isStartOfNewLine = true;
            m_IsDrivenLineSpacing = false;
            m_FirstOverflowCharacterIndex = -1;
            m_LastBaseGlyphIndex = int.MinValue;

            m_PageNumber = 0;
            int pageToDisplay = Mathf.Clamp(generationSettings.pageToDisplay - 1, 0, textInfo.pageInfo.Length - 1);
            textInfo.ClearPageInfo();

            Vector4 margins = generationSettings.margins;
            float marginWidth = m_MarginWidth > 0 ? m_MarginWidth : 0;
            float marginHeight = m_MarginHeight > 0 ? m_MarginHeight : 0;
            m_MarginLeft = 0;
            m_MarginRight = 0;
            m_Width = -1;
            float widthOfTextArea = marginWidth + 0.0001f - m_MarginLeft - m_MarginRight;

            // Need to initialize these Extents structures
            m_MeshExtents.min = TextGeneratorUtilities.largePositiveVector2;
            m_MeshExtents.max = TextGeneratorUtilities.largeNegativeVector2;

            // Initialize lineInfo
            textInfo.ClearLineInfo();

            // Tracking of the highest Ascender
            m_MaxCapHeight = 0;
            m_MaxAscender = 0;
            m_MaxDescender = 0;
            m_PageAscender = 0;
            float maxVisibleDescender = 0;
            bool isMaxVisibleDescenderSet = false;
            m_IsNewPage = false;

            // Initialize struct to track states of word wrapping
            bool isFirstWordOfLine = true;
            m_IsNonBreakingSpace = false;
            bool ignoreNonBreakingSpace = false;
            int lastSoftLineBreak = 0;

            CharacterSubstitution characterToSubstitute = new CharacterSubstitution(-1, 0);
            bool isSoftHyphenIgnored = false;
            var wordWrap = generationSettings.wordWrap ? TextWrappingMode.Normal : TextWrappingMode.NoWrap;

            // Save character and line state before we begin layout.
            SaveWordWrappingState(ref m_SavedWordWrapState, -1, -1, textInfo);
            SaveWordWrappingState(ref m_SavedLineState, -1, -1, textInfo);
            SaveWordWrappingState(ref m_SavedEllipsisState, -1, -1, textInfo);
            SaveWordWrappingState(ref m_SavedLastValidState, -1, -1, textInfo);
            SaveWordWrappingState(ref m_SavedSoftLineBreakState, -1, -1, textInfo);

            m_EllipsisInsertionCandidateStack.Clear();
            // Clear the previous truncated / ellipsed state
            m_IsTextTruncated = false;

            TextSettings textSettings = generationSettings.textSettings;

            // Safety Tracker
            int restoreCount = 0;

            // Parse through Character buffer to read HTML tags and begin creating mesh.
            for (int i = 0; i < m_TextProcessingArray.Length && m_TextProcessingArray[i].unicode != 0; i++)
            {
                charCode = m_TextProcessingArray[i].unicode;

                if (restoreCount > 5)
                {
                    Debug.LogError("Line breaking recursion max threshold hit... Character [" + charCode + "] index: " + i);
                    characterToSubstitute.index = m_CharacterCount;
                    characterToSubstitute.unicode = k_EndOfText;
                }

                // Skip characters that have been substituted.
                if (charCode == 0x1A)
                    continue;

                // Parse Rich Text Tag
                #region Parse Rich Text Tag
                if (generationSettings.richText && charCode == '<')
                {
                    m_isTextLayoutPhase = true;
                    m_TextElementType = TextElementType.Character;
                    int endTagIndex;

                    // Check if Tag is valid. If valid, skip to the end of the validated tag.
                    if (ValidateHtmlTag(m_TextProcessingArray, i + 1, out endTagIndex, generationSettings, textInfo))
                    {
                        i = endTagIndex;

                        // Continue to next character or handle the sprite element
                        if (m_TextElementType == TextElementType.Character)
                        {
                            continue;
                        }
                    }
                }
                else
                {
                    m_TextElementType = textInfo.textElementInfo[m_CharacterCount].elementType;
                    m_CurrentMaterialIndex = textInfo.textElementInfo[m_CharacterCount].materialReferenceIndex;
                    m_CurrentFontAsset = textInfo.textElementInfo[m_CharacterCount].fontAsset;
                }
                #endregion End Parse Rich Text Tag

                int previousMaterialIndex = m_CurrentMaterialIndex;
                bool isUsingAltTypeface = textInfo.textElementInfo[m_CharacterCount].isUsingAlternateTypeface;

                m_isTextLayoutPhase = false;

                // Handle potential character substitutions
                #region Character Substitutions
                bool isInjectedCharacter = false;

                if (characterToSubstitute.index == m_CharacterCount)
                {
                    charCode = characterToSubstitute.unicode;
                    m_TextElementType = TextElementType.Character;
                    isInjectedCharacter = true;

                    switch (charCode)
                    {
                        case k_EndOfText:
                            textInfo.textElementInfo[m_CharacterCount].textElement = m_CurrentFontAsset.characterLookupTable[k_EndOfText];
                            m_IsTextTruncated = true;
                            break;
                        case 0x2D:
                            //
                            break;
                        case k_HorizontalEllipsis:
                            textInfo.textElementInfo[m_CharacterCount].textElement = m_Ellipsis.character;
                            textInfo.textElementInfo[m_CharacterCount].elementType = TextElementType.Character;
                            textInfo.textElementInfo[m_CharacterCount].fontAsset = m_Ellipsis.fontAsset;
                            textInfo.textElementInfo[m_CharacterCount].material = m_Ellipsis.material;
                            textInfo.textElementInfo[m_CharacterCount].materialReferenceIndex = m_Ellipsis.materialIndex;

                            // Indicates the source parsing data has been modified.
                            m_IsTextTruncated = true;

                            // End Of Text
                            characterToSubstitute.index = m_CharacterCount + 1;
                            characterToSubstitute.unicode = k_EndOfText;
                            break;
                    }
                }
                #endregion


                // When using Linked text, mark character as ignored and skip to next character.
                #region Linked Text
                if (m_CharacterCount < generationSettings.firstVisibleCharacter && charCode != k_EndOfText)
                {
                    textInfo.textElementInfo[m_CharacterCount].isVisible = false;
                    textInfo.textElementInfo[m_CharacterCount].character = (char)k_ZeroWidthSpace;
                    textInfo.textElementInfo[m_CharacterCount].lineNumber = 0;
                    m_CharacterCount += 1;
                    continue;
                }
                #endregion


                // Handle Font Styles like LowerCase, UpperCase and SmallCaps.
                #region Handling of LowerCase, UpperCase and SmallCaps Font Styles

                float smallCapsMultiplier = 1.0f;

                if (m_TextElementType == TextElementType.Character)
                {
                    if ((m_FontStyleInternal & FontStyles.UpperCase) == FontStyles.UpperCase)
                    {
                        // If this character is lowercase, switch to uppercase.
                        if (char.IsLower((char)charCode))
                            charCode = char.ToUpper((char)charCode);

                    }
                    else if ((m_FontStyleInternal & FontStyles.LowerCase) == FontStyles.LowerCase)
                    {
                        // If this character is uppercase, switch to lowercase.
                        if (char.IsUpper((char)charCode))
                            charCode = char.ToLower((char)charCode);
                    }
                    else if ((m_FontStyleInternal & FontStyles.SmallCaps) == FontStyles.SmallCaps)
                    {
                        if (char.IsLower((char)charCode))
                        {
                            smallCapsMultiplier = 0.8f;
                            charCode = char.ToUpper((char)charCode);
                        }
                    }
                }
                #endregion


                // Look up Character Data from Dictionary and cache it.
                #region Look up Character Data

                float baselineOffset = 0;
                float elementAscentLine = 0;
                float elementDescentLine = 0;
                if (m_TextElementType == TextElementType.Sprite)
                {
                    // If a sprite is used as a fallback then get a reference to it and set the color to white.
                    m_CurrentSpriteAsset = textInfo.textElementInfo[m_CharacterCount].textElement.textAsset as SpriteAsset;
                    m_SpriteIndex = (int)textInfo.textElementInfo[m_CharacterCount].textElement.glyphIndex;

                    SpriteCharacter sprite = m_CurrentSpriteAsset.spriteCharacterTable[m_SpriteIndex];
                    if (sprite == null)
                    {
                        continue;
                    }

                    // Sprites are assigned in the E000 Private Area + sprite Index
                    if (charCode == '<')
                        charCode = 57344 + (uint)m_SpriteIndex;
                    else
                        m_SpriteColor = Color.white;

                    float fontScale = (m_CurrentFontSize / m_CurrentFontAsset.faceInfo.pointSize * m_CurrentFontAsset.faceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f));

                    // The sprite scale calculations are based on the font asset assigned to the text object.
                    if (m_CurrentSpriteAsset.m_FaceInfo.pointSize > 0)
                    {
                        float spriteScale = m_CurrentFontSize / m_CurrentSpriteAsset.m_FaceInfo.pointSize * m_CurrentSpriteAsset.m_FaceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f);
                        currentElementScale = sprite.m_Scale * sprite.m_Glyph.scale * spriteScale;
                        elementAscentLine = m_CurrentSpriteAsset.m_FaceInfo.ascentLine;
                        baselineOffset = m_CurrentSpriteAsset.m_FaceInfo.baseline * fontScale * m_FontScaleMultiplier * m_CurrentSpriteAsset.m_FaceInfo.scale;
                        elementDescentLine = m_CurrentSpriteAsset.m_FaceInfo.descentLine;
                    }
                    else
                    {
                        float spriteScale = m_CurrentFontSize / m_CurrentFontAsset.m_FaceInfo.pointSize * m_CurrentFontAsset.m_FaceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f);
                        currentElementScale = m_CurrentFontAsset.m_FaceInfo.ascentLine / sprite.m_Glyph.metrics.height * sprite.m_Scale * sprite.m_Glyph.scale * spriteScale;
                        float scaleDelta = spriteScale / currentElementScale;
                        elementAscentLine = m_CurrentFontAsset.m_FaceInfo.ascentLine * scaleDelta;
                        baselineOffset = m_CurrentFontAsset.m_FaceInfo.baseline * fontScale * m_FontScaleMultiplier * m_CurrentFontAsset.m_FaceInfo.scale;
                        elementDescentLine = m_CurrentFontAsset.m_FaceInfo.descentLine * scaleDelta;
                    }

                    m_CachedTextElement = sprite;

                    textInfo.textElementInfo[m_CharacterCount].elementType = TextElementType.Sprite;
                    textInfo.textElementInfo[m_CharacterCount].scale = currentElementScale;
                    textInfo.textElementInfo[m_CharacterCount].spriteAsset = m_CurrentSpriteAsset;
                    textInfo.textElementInfo[m_CharacterCount].fontAsset = m_CurrentFontAsset;
                    textInfo.textElementInfo[m_CharacterCount].materialReferenceIndex = m_CurrentMaterialIndex;

                    m_CurrentMaterialIndex = previousMaterialIndex;

                    padding = 0;
                }
                else if (m_TextElementType == TextElementType.Character)
                {
                    m_CachedTextElement = textInfo.textElementInfo[m_CharacterCount].textElement;
                    if (m_CachedTextElement == null)
                    {
                        continue;
                    }

                    m_CurrentFontAsset = textInfo.textElementInfo[m_CharacterCount].fontAsset;
                    m_CurrentMaterial = textInfo.textElementInfo[m_CharacterCount].material;
                    m_CurrentMaterialIndex = textInfo.textElementInfo[m_CharacterCount].materialReferenceIndex;

                    // Special handling if replaced character was a line feed where in this case we have to use the scale of the previous character.
                    float adjustedScale;
                    if (isInjectedCharacter && m_TextProcessingArray[i].unicode == 0x0A && m_CharacterCount != m_FirstCharacterOfLine)
                        adjustedScale = textInfo.textElementInfo[m_CharacterCount - 1].pointSize * smallCapsMultiplier / m_CurrentFontAsset.m_FaceInfo.pointSize * m_CurrentFontAsset.m_FaceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f);
                    else
                        adjustedScale = m_CurrentFontSize * smallCapsMultiplier / m_CurrentFontAsset.m_FaceInfo.pointSize * m_CurrentFontAsset.m_FaceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f);

                    // Special handling for injected Ellipsis
                    if (isInjectedCharacter && charCode == k_HorizontalEllipsis)
                    {
                        elementAscentLine = 0;
                        elementDescentLine = 0;
                    }
                    else
                    {
                        elementAscentLine = m_CurrentFontAsset.m_FaceInfo.ascentLine;
                        elementDescentLine = m_CurrentFontAsset.m_FaceInfo.descentLine;
                    }

                    currentElementScale = adjustedScale * m_FontScaleMultiplier * m_CachedTextElement.m_Scale * m_CachedTextElement.m_Glyph.scale;
                    baselineOffset = m_CurrentFontAsset.m_FaceInfo.baseline * adjustedScale * m_FontScaleMultiplier * m_CurrentFontAsset.m_FaceInfo.scale;

                    textInfo.textElementInfo[m_CharacterCount].elementType = TextElementType.Character;
                    textInfo.textElementInfo[m_CharacterCount].scale = currentElementScale;

                    padding = m_Padding;
                }
                #endregion


                // Handle Soft Hyphen
                #region Handle Soft Hyphen
                float currentElementUnmodifiedScale = currentElementScale;
                if (charCode == 0xAD || charCode == k_EndOfText)
                    currentElementScale = 0;
                #endregion


                // Store some of the text object's information
                textInfo.textElementInfo[m_CharacterCount].character = (char)charCode;
                textInfo.textElementInfo[m_CharacterCount].pointSize = m_CurrentFontSize;
                textInfo.textElementInfo[m_CharacterCount].color = m_HtmlColor;
                textInfo.textElementInfo[m_CharacterCount].underlineColor = m_UnderlineColor;
                textInfo.textElementInfo[m_CharacterCount].strikethroughColor = m_StrikethroughColor;
                textInfo.textElementInfo[m_CharacterCount].highlightState = m_HighlightState;
                textInfo.textElementInfo[m_CharacterCount].style = m_FontStyleInternal;

                // Cache glyph metrics
                Glyph altGlyph = textInfo.textElementInfo[m_CharacterCount].alternativeGlyph;
                GlyphMetrics currentGlyphMetrics = altGlyph == null ? m_CachedTextElement.m_Glyph.metrics : altGlyph.metrics;

                // Optimization to avoid calling this more than once per character.
                bool isWhiteSpace = charCode <= 0xFFFF && char.IsWhiteSpace((char)charCode);

                // Handle Kerning if Enabled.
                #region Handle Kerning
                GlyphValueRecord glyphAdjustments = new GlyphValueRecord();
                float characterSpacingAdjustment = generationSettings.characterSpacing;
                if (generationSettings.enableKerning)
                {
                    GlyphPairAdjustmentRecord adjustmentPair;
                    uint baseGlyphIndex = m_CachedTextElement.m_GlyphIndex;

                    if (m_CharacterCount < totalCharacterCount - 1)
                    {
                        uint nextGlyphIndex = textInfo.textElementInfo[m_CharacterCount + 1].textElement.m_GlyphIndex;
                        uint key = nextGlyphIndex << 16 | baseGlyphIndex;

                        if (m_CurrentFontAsset.m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.TryGetValue(key, out adjustmentPair))
                        {
                            glyphAdjustments = adjustmentPair.firstAdjustmentRecord.glyphValueRecord;
                            characterSpacingAdjustment = (adjustmentPair.featureLookupFlags & FontFeatureLookupFlags.IgnoreSpacingAdjustments) == FontFeatureLookupFlags.IgnoreSpacingAdjustments ? 0 : characterSpacingAdjustment;
                        }
                    }

                    if (m_CharacterCount >= 1)
                    {
                        uint previousGlyphIndex = textInfo.textElementInfo[m_CharacterCount - 1].textElement.m_GlyphIndex;
                        uint key = baseGlyphIndex << 16 | previousGlyphIndex;

                        if (m_CurrentFontAsset.m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.TryGetValue(key, out adjustmentPair))
                        {
                            glyphAdjustments += adjustmentPair.secondAdjustmentRecord.glyphValueRecord;
                            characterSpacingAdjustment = (adjustmentPair.featureLookupFlags & FontFeatureLookupFlags.IgnoreSpacingAdjustments) == FontFeatureLookupFlags.IgnoreSpacingAdjustments ? 0 : characterSpacingAdjustment;
                        }
                    }
                }

                textInfo.textElementInfo[m_CharacterCount].adjustedHorizontalAdvance = glyphAdjustments.xAdvance;
                #endregion


                // Handle Diacritical Marks
                #region Handle Diacritical Marks
                bool isBaseGlyph = TextGeneratorUtilities.IsBaseGlyph((uint)charCode);

                if (isBaseGlyph)
                    m_LastBaseGlyphIndex = m_CharacterCount;

                if (m_CharacterCount > 0 && !isBaseGlyph)
                {
                    // Check for potential Mark-to-Base lookup if previous glyph was a base glyph
                    if (m_LastBaseGlyphIndex != int.MinValue && m_LastBaseGlyphIndex == m_CharacterCount - 1)
                    {
                        Glyph baseGlyph = textInfo.textElementInfo[m_LastBaseGlyphIndex].textElement.glyph;
                        uint baseGlyphIndex = baseGlyph.index;
                        uint markGlyphIndex = m_CachedTextElement.glyphIndex;
                        uint key = markGlyphIndex << 16 | baseGlyphIndex;

                        if (m_CurrentFontAsset.fontFeatureTable.m_MarkToBaseAdjustmentRecordLookup.TryGetValue(key, out MarkToBaseAdjustmentRecord glyphAdjustmentRecord))
                        {
                            float advanceOffset = (textInfo.textElementInfo[m_LastBaseGlyphIndex].origin - m_XAdvance) / currentElementScale;

                            glyphAdjustments.xPlacement = advanceOffset + glyphAdjustmentRecord.baseGlyphAnchorPoint.xCoordinate - glyphAdjustmentRecord.markPositionAdjustment.xPositionAdjustment;
                            glyphAdjustments.yPlacement = glyphAdjustmentRecord.baseGlyphAnchorPoint.yCoordinate - glyphAdjustmentRecord.markPositionAdjustment.yPositionAdjustment;

                            characterSpacingAdjustment = 0;
                        }
                    }
                    else
                    {
                        // Iterate from previous glyph to last base glyph checking for any potential Mark-to-Mark lookups to apply. Otherwise check for potential Mark-to-Base lookup between the current glyph and last base glyph
                        bool wasLookupApplied = false;

                        // Check for any potential Mark-to-Mark lookups
                        for (int characterLookupIndex = m_CharacterCount - 1; characterLookupIndex >= 0 && characterLookupIndex != m_LastBaseGlyphIndex; characterLookupIndex--)
                        {
                            // Handle any potential Mark-to-Mark lookup
                            Glyph baseMarkGlyph = textInfo.textElementInfo[characterLookupIndex].textElement.glyph;
                            uint baseGlyphIndex = baseMarkGlyph.index;
                            uint combiningMarkGlyphIndex = m_CachedTextElement.glyphIndex;
                            uint key = combiningMarkGlyphIndex << 16 | baseGlyphIndex;

                            if (m_CurrentFontAsset.fontFeatureTable.m_MarkToMarkAdjustmentRecordLookup.TryGetValue(key, out MarkToMarkAdjustmentRecord glyphAdjustmentRecord))
                            {
                                float baseMarkOrigin = (textInfo.textElementInfo[characterLookupIndex].origin - m_XAdvance) / currentElementScale;
                                float currentBaseline = baselineOffset - m_LineOffset + m_BaselineOffset;
                                float baseMarkBaseline = (textInfo.textElementInfo[characterLookupIndex].baseLine - currentBaseline) / currentElementScale;

                                glyphAdjustments.xPlacement = baseMarkOrigin + glyphAdjustmentRecord.baseMarkGlyphAnchorPoint.xCoordinate - glyphAdjustmentRecord.combiningMarkPositionAdjustment.xPositionAdjustment;
                                glyphAdjustments.yPlacement = baseMarkBaseline + glyphAdjustmentRecord.baseMarkGlyphAnchorPoint.yCoordinate - glyphAdjustmentRecord.combiningMarkPositionAdjustment.yPositionAdjustment;

                                characterSpacingAdjustment = 0;
                                wasLookupApplied = true;
                                break;
                            }
                        }

                        // If no Mark-to-Mark lookups were applied, check for potential Mark-to-Base lookup.
                        if (m_LastBaseGlyphIndex != int.MinValue && !wasLookupApplied)
                        {
                            // Handle lookup for Mark-to-Base
                            Glyph baseGlyph = textInfo.textElementInfo[m_LastBaseGlyphIndex].textElement.glyph;
                            uint baseGlyphIndex = baseGlyph.index;
                            uint markGlyphIndex = m_CachedTextElement.glyphIndex;
                            uint key = markGlyphIndex << 16 | baseGlyphIndex;

                            if (m_CurrentFontAsset.fontFeatureTable.m_MarkToBaseAdjustmentRecordLookup.TryGetValue(key, out MarkToBaseAdjustmentRecord glyphAdjustmentRecord))
                            {
                                float advanceOffset = (textInfo.textElementInfo[m_LastBaseGlyphIndex].origin - m_XAdvance) / currentElementScale;

                                glyphAdjustments.xPlacement = advanceOffset + glyphAdjustmentRecord.baseGlyphAnchorPoint.xCoordinate - glyphAdjustmentRecord.markPositionAdjustment.xPositionAdjustment;
                                glyphAdjustments.yPlacement = glyphAdjustmentRecord.baseGlyphAnchorPoint.yCoordinate - glyphAdjustmentRecord.markPositionAdjustment.yPositionAdjustment;

                                characterSpacingAdjustment = 0;
                            }
                        }
                    }
                }

                // Adjust relevant text metrics
                elementAscentLine += glyphAdjustments.yPlacement;
                elementDescentLine += glyphAdjustments.yPlacement;
                #endregion


                // Initial Implementation for RTL support.
                #region Handle Right-to-Left
                if (generationSettings.isRightToLeft)
                {
                    m_XAdvance -= currentGlyphMetrics.horizontalAdvance * (1 - m_CharWidthAdjDelta) * currentElementScale;

                    if (isWhiteSpace || charCode == k_ZeroWidthSpace)
                        m_XAdvance -= generationSettings.wordSpacing * currentEmScale;
                }
                #endregion


                // Handle Mono Spacing
                #region Handle Mono Spacing
                float monoAdvance = 0;
                if (m_MonoSpacing != 0)
                {
                    monoAdvance = (m_MonoSpacing / 2 - (currentGlyphMetrics.width / 2 + currentGlyphMetrics.horizontalBearingX) * currentElementScale) * (1 - m_CharWidthAdjDelta);
                    m_XAdvance += monoAdvance;
                }
                #endregion


                // Set Padding based on selected font style
                #region Handle Style Padding
                float boldSpacingAdjustment;
                float stylePadding;
                if (m_TextElementType == TextElementType.Character && !isUsingAltTypeface && ((m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold)) // Checks for any combination of Bold Style.
                {
                    if (m_CurrentMaterial != null && m_CurrentMaterial.HasProperty(TextShaderUtilities.ID_GradientScale))
                    {
                        float gradientScale = m_CurrentMaterial.GetFloat(TextShaderUtilities.ID_GradientScale);
                        stylePadding = m_CurrentFontAsset.boldStyleWeight / 4.0f * gradientScale * m_CurrentMaterial.GetFloat(TextShaderUtilities.ID_ScaleRatio_A);

                        // Clamp overall padding to Gradient Scale size.
                        if (stylePadding + padding > gradientScale)
                            padding = gradientScale - stylePadding;
                    }
                    else
                        stylePadding = 0;

                    boldSpacingAdjustment = m_CurrentFontAsset.boldStyleSpacing;
                }
                else
                {
                    if (m_CurrentMaterial != null && m_CurrentMaterial.HasProperty(TextShaderUtilities.ID_GradientScale) && m_CurrentMaterial.HasProperty(TextShaderUtilities.ID_ScaleRatio_A))
                    {
                        float gradientScale = m_CurrentMaterial.GetFloat(TextShaderUtilities.ID_GradientScale);
                        stylePadding = m_CurrentFontAsset.m_RegularStyleWeight / 4.0f * gradientScale * m_CurrentMaterial.GetFloat(TextShaderUtilities.ID_ScaleRatio_A);

                        // Clamp overall padding to Gradient Scale size.
                        if (stylePadding + padding > gradientScale)
                            padding = gradientScale - stylePadding;
                    }
                    else
                        stylePadding = 0;

                    boldSpacingAdjustment = 0;
                }
                #endregion Handle Style Padding


                // Determine the position of the vertices of the Character or Sprite.
                #region Calculate Vertices Position
                Vector3 topLeft;
                topLeft.x = m_XAdvance + ((currentGlyphMetrics.horizontalBearingX * m_FXScale.x - padding - stylePadding + glyphAdjustments.xPlacement) * currentElementScale * (1 - m_CharWidthAdjDelta));
                topLeft.y = baselineOffset + (currentGlyphMetrics.horizontalBearingY + padding + glyphAdjustments.yPlacement) * currentElementScale - m_LineOffset + m_BaselineOffset;
                topLeft.z = 0;

                Vector3 bottomLeft;
                bottomLeft.x = topLeft.x;
                bottomLeft.y = topLeft.y - ((currentGlyphMetrics.height + padding * 2) * currentElementScale);
                bottomLeft.z = 0;

                Vector3 topRight;
                topRight.x = bottomLeft.x + ((currentGlyphMetrics.width * m_FXScale.x + padding * 2 + stylePadding * 2) * currentElementScale * (1 - m_CharWidthAdjDelta));
                topRight.y = topLeft.y;
                topRight.z = 0;

                Vector3 bottomRight;
                bottomRight.x = topRight.x;
                bottomRight.y = bottomLeft.y;
                bottomRight.z = 0;
                #endregion


                // Check if we need to Shear the rectangles for Italic styles
                #region Handle Italic & Shearing
                if (m_TextElementType == TextElementType.Character && !isUsingAltTypeface && ((m_FontStyleInternal & FontStyles.Italic) == FontStyles.Italic))
                {
                    // Shift Top vertices forward by half (Shear Value * height of character) and Bottom vertices back by same amount.
                    float shearValue = m_ItalicAngle * 0.01f;
                    float midPoint = ((m_CurrentFontAsset.m_FaceInfo.capLine - (m_CurrentFontAsset.m_FaceInfo.baseline + m_BaselineOffset)) / 2) * m_FontScaleMultiplier * m_CurrentFontAsset.m_FaceInfo.scale;
                    Vector3 topShear = new Vector3(shearValue * ((currentGlyphMetrics.horizontalBearingY + padding + stylePadding - midPoint) * currentElementScale), 0, 0);
                    Vector3 bottomShear = new Vector3(shearValue * (((currentGlyphMetrics.horizontalBearingY - currentGlyphMetrics.height - padding - stylePadding - midPoint)) * currentElementScale), 0, 0);

                    topLeft += topShear;
                    bottomLeft += bottomShear;
                    topRight += topShear;
                    bottomRight += bottomShear;
                }
                #endregion Handle Italics & Shearing


                // Handle Character FX Rotation
                #region Handle Character FX Rotation
                if (m_FXRotation != Quaternion.identity)
                {
                    Matrix4x4 rotationMatrix = Matrix4x4.Rotate(m_FXRotation);
                    Vector3 positionOffset = (topRight + bottomLeft) / 2;

                    topLeft = rotationMatrix.MultiplyPoint3x4(topLeft - positionOffset) + positionOffset;
                    bottomLeft = rotationMatrix.MultiplyPoint3x4(bottomLeft - positionOffset) + positionOffset;
                    topRight = rotationMatrix.MultiplyPoint3x4(topRight - positionOffset) + positionOffset;
                    bottomRight = rotationMatrix.MultiplyPoint3x4(bottomRight - positionOffset) + positionOffset;
                }
                #endregion


                // Store vertex information for the character or sprite.
                textInfo.textElementInfo[m_CharacterCount].bottomLeft = bottomLeft;
                textInfo.textElementInfo[m_CharacterCount].topLeft = topLeft;
                textInfo.textElementInfo[m_CharacterCount].topRight = topRight;
                textInfo.textElementInfo[m_CharacterCount].bottomRight = bottomRight;

                textInfo.textElementInfo[m_CharacterCount].origin = m_XAdvance + glyphAdjustments.xPlacement * currentElementScale;
                textInfo.textElementInfo[m_CharacterCount].baseLine = (baselineOffset - m_LineOffset + m_BaselineOffset) + glyphAdjustments.yPlacement * currentElementScale;
                textInfo.textElementInfo[m_CharacterCount].aspectRatio = (topRight.x - bottomLeft.x) / (topLeft.y - bottomLeft.y);


                // Compute text metrics
                #region Compute Ascender & Descender values
                // Element Ascender in line space
                float elementAscender = m_TextElementType == TextElementType.Character
                    ? elementAscentLine * currentElementScale / smallCapsMultiplier + m_BaselineOffset
                    : elementAscentLine * currentElementScale + m_BaselineOffset;

                // Element Descender in line space
                float elementDescender = m_TextElementType == TextElementType.Character
                    ? elementDescentLine * currentElementScale / smallCapsMultiplier + m_BaselineOffset
                    : elementDescentLine * currentElementScale + m_BaselineOffset;

                float adjustedAscender = elementAscender;
                float adjustedDescender = elementDescender;

                // Max line ascender and descender in line space
                bool isFirstCharacterOfLine = m_CharacterCount == m_FirstCharacterOfLine;
                if (isFirstCharacterOfLine || isWhiteSpace == false)
                {
                    // Special handling for Superscript and Subscript where we use the unadjusted line ascender and descender
                    if (m_BaselineOffset != 0)
                    {
                        adjustedAscender = Mathf.Max((elementAscender - m_BaselineOffset) / m_FontScaleMultiplier, adjustedAscender);
                        adjustedDescender = Mathf.Min((elementDescender - m_BaselineOffset) / m_FontScaleMultiplier, adjustedDescender);
                    }

                    m_MaxLineAscender = Mathf.Max(adjustedAscender, m_MaxLineAscender);
                    m_MaxLineDescender = Mathf.Min(adjustedDescender, m_MaxLineDescender);
                }

                // Element Ascender and Descender in object space
                if (isFirstCharacterOfLine || isWhiteSpace == false)
                {
                    textInfo.textElementInfo[m_CharacterCount].adjustedAscender = adjustedAscender;
                    textInfo.textElementInfo[m_CharacterCount].adjustedDescender = adjustedDescender;

                    textInfo.textElementInfo[m_CharacterCount].ascender = elementAscender - m_LineOffset;
                    m_MaxDescender = textInfo.textElementInfo[m_CharacterCount].descender = elementDescender - m_LineOffset;
                }
                else
                {
                    textInfo.textElementInfo[m_CharacterCount].adjustedAscender = m_MaxLineAscender;
                    textInfo.textElementInfo[m_CharacterCount].adjustedDescender = m_MaxLineDescender;

                    textInfo.textElementInfo[m_CharacterCount].ascender = m_MaxLineAscender - m_LineOffset;
                    m_MaxDescender = textInfo.textElementInfo[m_CharacterCount].descender = m_MaxLineDescender - m_LineOffset;
                }

                // Max text object ascender and cap height
                if (m_LineNumber == 0 || m_IsNewPage)
                {
                    if (isFirstCharacterOfLine || isWhiteSpace == false)
                    {
                        m_MaxAscender = m_MaxLineAscender;
                        m_MaxCapHeight = Mathf.Max(m_MaxCapHeight, m_CurrentFontAsset.m_FaceInfo.capLine * currentElementScale / smallCapsMultiplier);
                    }
                }

                // Page ascender
                if (m_LineOffset == 0)
                {
                    if (isFirstCharacterOfLine || isWhiteSpace == false)
                        m_PageAscender = m_PageAscender > elementAscender ? m_PageAscender : elementAscender;
                }
                #endregion


                // Set Characters to not visible by default.
                textInfo.textElementInfo[m_CharacterCount].isVisible = false;

                bool isJustifiedOrFlush = ((HorizontalAlignment)m_LineJustification & HorizontalAlignment.Flush) == HorizontalAlignment.Flush || ((HorizontalAlignment)m_LineJustification & HorizontalAlignment.Justified) == HorizontalAlignment.Justified;

                // Setup Mesh for visible text elements. ie. not a SPACE / LINEFEED / CARRIAGE RETURN.
                #region Handle Visible Characters
                if (charCode == k_Tab || ((wordWrap == TextWrappingMode.PreserveWhitespace || wordWrap == TextWrappingMode.PreserveWhitespaceNoWrap) && (isWhiteSpace || charCode == k_ZeroWidthSpace)) || (isWhiteSpace == false && charCode != k_ZeroWidthSpace && charCode != 0xAD && charCode != k_EndOfText) || (charCode == 0xAD && isSoftHyphenIgnored == false) || m_TextElementType == TextElementType.Sprite)
                {
                    textInfo.textElementInfo[m_CharacterCount].isVisible = true;

                    #region Experimental Margin Shaper
                    //Vector2 shapedMargins;
                    //if (marginShaper)
                    //{HorizontalAlignmentOption
                    //    shapedMargins = m_marginShaper.GetShapedMargins(textInfo.textElementInfo[m_CharacterCount].baseLine);
                    //    if (shapedMargins.x < margins.x)
                    //    {
                    //        shapedMargins.x = m_MarginLeft;
                    //    }
                    //    else
                    //    {
                    //        shapedMargins.x += m_MarginLeft - margins.x;
                    //    }
                    //    if (shapedMargins.y < margins.z)
                    //    {
                    //        shapedMargins.y = m_MarginRight;
                    //    }
                    //    else
                    //    {
                    //        shapedMargins.y += m_MarginRight - margins.z;
                    //    }
                    //}
                    //else
                    //{
                    //    shapedMargins.x = m_MarginLeft;
                    //    shapedMargins.y = m_MarginRight;
                    //}
                    //width = marginWidth + 0.0001f - shapedMargins.x - shapedMargins.y;
                    //if (m_Width != -1 && m_Width < width)
                    //{
                    //    width = m_Width;
                    //}
                    //textInfo.lineInfo[m_LineNumber].marginLeft = shapedMargins.x;
                    #endregion

                    float marginLeft = m_MarginLeft;
                    float marginRight = m_MarginRight;

                    // Injected characters do not override margins
                    if (isInjectedCharacter)
                    {
                        marginLeft = textInfo.lineInfo[m_LineNumber].marginLeft;
                        marginRight = textInfo.lineInfo[m_LineNumber].marginRight;
                    }

                    widthOfTextArea = m_Width != -1 ? Mathf.Min(marginWidth + 0.0001f - marginLeft - marginRight, m_Width) : marginWidth + 0.0001f - marginLeft - marginRight;

                    // Calculate the line breaking width of the text.
                    float textWidth = Mathf.Abs(m_XAdvance) + (!generationSettings.isRightToLeft ? currentGlyphMetrics.horizontalAdvance : 0) * (1 - m_CharWidthAdjDelta) * (charCode == 0xAD ? currentElementUnmodifiedScale : currentElementScale);
                    float textHeight = m_MaxAscender - (m_MaxLineDescender - m_LineOffset) + (m_LineOffset > 0 && m_IsDrivenLineSpacing == false ? m_MaxLineAscender - m_StartOfLineAscender : 0);

                    int testedCharacterCount = m_CharacterCount;

                    // Handling of current line Vertical Bounds
                    #region Current Line Vertical Bounds Check
                    if (textHeight > marginHeight + 0.0001f)
                    {
                        // Set isTextOverflowing and firstOverflowCharacterIndex
                        if (m_FirstOverflowCharacterIndex == -1)
                            m_FirstOverflowCharacterIndex = m_CharacterCount;

                        // Check if Auto-Size is enabled
                        if (generationSettings.autoSize)
                        {
                            // Handle Line spacing adjustments
                            #region Line Spacing Adjustments
                            if (m_LineSpacingDelta > generationSettings.lineSpacingMax && m_LineOffset > 0 && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                            {
                                float adjustmentDelta = (marginHeight - textHeight) / m_LineNumber;

                                m_LineSpacingDelta = Mathf.Max(m_LineSpacingDelta + adjustmentDelta / baseScale, generationSettings.lineSpacingMax);

                                return;
                            }
                            #endregion


                            // Handle Text Auto-sizing resulting from text exceeding vertical bounds.
                            #region Text Auto-Sizing (Text greater than vertical bounds)
                            if (m_FontSize > generationSettings.fontSizeMin && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                            {
                                m_MaxFontSize = m_FontSize;

                                float sizeDelta = Mathf.Max((m_FontSize - m_MinFontSize) / 2, 0.05f);
                                m_FontSize -= sizeDelta;
                                m_FontSize = Mathf.Max((int)(m_FontSize * 20 + 0.5f) / 20f, generationSettings.fontSizeMin);

                                return;
                            }
                            #endregion Text Auto-Sizing
                        }

                        // Handle Vertical Overflow on current line
                        switch (generationSettings.overflowMode)
                        {
                            case TextOverflowMode.Overflow:
                            case TextOverflowMode.ScrollRect:
                            case TextOverflowMode.Masking:
                                // Nothing happens as vertical bounds are ignored in this mode.
                                break;

                            case TextOverflowMode.Truncate:
                                i = RestoreWordWrappingState(ref m_SavedLastValidState, textInfo);

                                characterToSubstitute.index = testedCharacterCount;
                                //characterToSubstitute.unicode = k_EndOfText;
                                continue;

                            case TextOverflowMode.Ellipsis:
                                if (m_LineNumber > 0)
                                {
                                    if (m_EllipsisInsertionCandidateStack.Count == 0)
                                    {
                                        i = -1;
                                        m_CharacterCount = 0;
                                        characterToSubstitute.index = 0;
                                        characterToSubstitute.unicode = k_EndOfText;
                                        m_FirstCharacterOfLine = 0;
                                        continue;
                                    }

                                    var ellipsisState = m_EllipsisInsertionCandidateStack.Pop();
                                    i = RestoreWordWrappingState(ref ellipsisState, textInfo);

                                    i -= 1;
                                    m_CharacterCount -= 1;
                                    characterToSubstitute.index = m_CharacterCount;
                                    characterToSubstitute.unicode = k_HorizontalEllipsis;

                                    restoreCount += 1;
                                    continue;
                                }
                                break;
                            case TextOverflowMode.Linked:
                                i = RestoreWordWrappingState(ref m_SavedLastValidState, textInfo);

                                // Truncate remaining text
                                characterToSubstitute.index = testedCharacterCount;
                                characterToSubstitute.unicode = k_EndOfText;
                                continue;

                            case TextOverflowMode.Page:
                                // End layout of text if first character / page doesn't fit.
                                if (i < 0 || testedCharacterCount == 0)
                                {
                                    i = -1;
                                    m_CharacterCount = 0;
                                    characterToSubstitute.index = 0;
                                    characterToSubstitute.unicode = k_EndOfText;
                                    continue;
                                }
                                else if (m_MaxLineAscender - m_MaxLineDescender > marginHeight + 0.0001f)
                                {
                                    // Current line exceeds the height of the text container
                                    // as such we stop on the previous line.
                                    i = RestoreWordWrappingState(ref m_SavedLineState, textInfo);

                                    characterToSubstitute.index = testedCharacterCount;
                                    characterToSubstitute.unicode = k_EndOfText;
                                    continue;
                                }

                                // Go back to previous line and re-layout
                                i = RestoreWordWrappingState(ref m_SavedLineState, textInfo);

                                m_IsNewPage = true;
                                m_FirstCharacterOfLine = m_CharacterCount;
                                m_MaxLineAscender = TextGeneratorUtilities.largeNegativeFloat;
                                m_MaxLineDescender = TextGeneratorUtilities.largePositiveFloat;
                                m_StartOfLineAscender = 0;

                                m_XAdvance = 0 + m_TagIndent;
                                m_LineOffset = 0;
                                m_MaxAscender = 0;
                                m_PageAscender = 0;
                                m_LineNumber += 1;
                                m_PageNumber += 1;

                                // Should consider saving page data here
                                continue;
                        }
                    }
                    #endregion

                    // Handling of Horizontal Bounds
                    #region Current Line Horizontal Bounds Check
                    if (isBaseGlyph && textWidth > widthOfTextArea * (isJustifiedOrFlush ? 1.05f : 1.0f))
                    {
                        // Handle Line Breaking (if still possible)
                        if (wordWrap != TextWrappingMode.NoWrap && wordWrap != TextWrappingMode.PreserveWhitespaceNoWrap && m_CharacterCount != m_FirstCharacterOfLine)
                        {
                            // Restore state to previous safe line breaking
                            i = RestoreWordWrappingState(ref m_SavedWordWrapState, textInfo);

                            // Compute potential new line offset in the event a line break is needed.
                            float lineOffsetDelta = 0;
                            if (m_LineHeight == k_FloatUnset)
                            {
                                float ascender = textInfo.textElementInfo[m_CharacterCount].adjustedAscender;
                                lineOffsetDelta = (m_LineOffset > 0 && m_IsDrivenLineSpacing == false ? m_MaxLineAscender - m_StartOfLineAscender : 0) - m_MaxLineDescender + ascender + (lineGap + m_LineSpacingDelta) * baseScale + generationSettings.lineSpacing * currentEmScale;
                            }
                            else
                            {
                                lineOffsetDelta = m_LineHeight + generationSettings.lineSpacing * currentEmScale;
                                m_IsDrivenLineSpacing = true;
                            }

                            // Calculate new text height
                            float newTextHeight = m_MaxAscender + lineOffsetDelta + m_LineOffset - textInfo.textElementInfo[m_CharacterCount].adjustedDescender;

                            // Replace Soft Hyphen by Hyphen Minus 0x2D
                            #region Handle Soft Hyphenation
                            if (textInfo.textElementInfo[m_CharacterCount - 1].character == 0xAD && isSoftHyphenIgnored == false)
                            {
                                // Only inject Hyphen Minus if new line is possible
                                if (generationSettings.overflowMode == TextOverflowMode.Overflow || newTextHeight < marginHeight + 0.0001f)
                                {
                                    characterToSubstitute.index = m_CharacterCount - 1;
                                    characterToSubstitute.unicode = (uint)0x2D;

                                    i -= 1;
                                    m_CharacterCount -= 1;
                                    continue;
                                }
                            }

                            isSoftHyphenIgnored = false;

                            // Ignore Soft Hyphen to prevent it from wrapping
                            if (textInfo.textElementInfo[m_CharacterCount].character == 0xAD)
                            {
                                isSoftHyphenIgnored = true;
                                continue;
                            }
                            #endregion

                            // Adjust character spacing before breaking up word if auto size is enabled
                            if (generationSettings.autoSize && isFirstWordOfLine)
                            {
                                // Handle Character Width Adjustments
                                #region Character Width Adjustments
                                if (m_CharWidthAdjDelta < generationSettings.charWidthMaxAdj / 100 && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                {
                                    float adjustedTextWidth = textWidth;

                                    // Determine full width of the text
                                    if (m_CharWidthAdjDelta > 0)
                                        adjustedTextWidth /= 1f - m_CharWidthAdjDelta;

                                    float adjustmentDelta = textWidth - (widthOfTextArea - 0.0001f) * (isJustifiedOrFlush ? 1.05f : 1.0f);
                                    m_CharWidthAdjDelta += adjustmentDelta / adjustedTextWidth;
                                    m_CharWidthAdjDelta = Mathf.Min(m_CharWidthAdjDelta, generationSettings.charWidthMaxAdj / 100);

                                    return;
                                }
                                #endregion

                                // Handle Text Auto-sizing resulting from text exceeding vertical bounds.
                                #region Text Auto-Sizing (Text greater than vertical bounds)
                                if (m_FontSize > generationSettings.fontSizeMin && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                {
                                    m_MaxFontSize = m_FontSize;

                                    float sizeDelta = Mathf.Max((m_FontSize - m_MinFontSize) / 2, 0.05f);
                                    m_FontSize -= sizeDelta;
                                    m_FontSize = Mathf.Max((int)(m_FontSize * 20 + 0.5f) / 20f, generationSettings.fontSizeMin);

                                    return;
                                }
                                #endregion Text Auto-Sizing
                            }


                            // Special handling if first word of line and non breaking space
                            int savedSoftLineBreakingSpace = m_SavedSoftLineBreakState.previousWordBreak;
                            if (isFirstWordOfLine && savedSoftLineBreakingSpace != -1)
                            {
                                if (savedSoftLineBreakingSpace != lastSoftLineBreak)
                                {
                                    i = RestoreWordWrappingState(ref m_SavedSoftLineBreakState, textInfo);
                                    lastSoftLineBreak = savedSoftLineBreakingSpace;

                                    // check if soft hyphen
                                    if (textInfo.textElementInfo[m_CharacterCount - 1].character == 0xAD)
                                    {
                                        characterToSubstitute.index = m_CharacterCount - 1;
                                        characterToSubstitute.unicode = 0x2D;

                                        i -= 1;
                                        m_CharacterCount -= 1;
                                        continue;
                                    }
                                }
                            }

                            // Determine if new line of text would exceed the vertical bounds of text container
                            if (newTextHeight > marginHeight + 0.0001f)
                            {
                                // Set isTextOverflowing and firstOverflowCharacterIndex
                                if (m_FirstOverflowCharacterIndex == -1)
                                    m_FirstOverflowCharacterIndex = m_CharacterCount;

                                // Check if Auto-Size is enabled
                                if (generationSettings.autoSize)
                                {
                                    // Handle Line spacing adjustments
                                    #region Line Spacing Adjustments
                                    if (m_LineSpacingDelta > generationSettings.lineSpacingMax && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                    {
                                        float adjustmentDelta = (marginHeight - newTextHeight) / (m_LineNumber + 1);

                                        m_LineSpacingDelta = Mathf.Max(m_LineSpacingDelta + adjustmentDelta / baseScale, generationSettings.lineSpacingMax);
                                        return;
                                    }
                                    #endregion

                                    // Handle Character Width Adjustments
                                    #region Character Width Adjustments
                                    if (m_CharWidthAdjDelta < generationSettings.charWidthMaxAdj / 100 && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                    {
                                        float adjustedTextWidth = textWidth;

                                        // Determine full width of the text
                                        if (m_CharWidthAdjDelta > 0)
                                            adjustedTextWidth /= 1f - m_CharWidthAdjDelta;

                                        float adjustmentDelta = textWidth - (widthOfTextArea - 0.0001f) * (isJustifiedOrFlush ? 1.05f : 1.0f);
                                        m_CharWidthAdjDelta += adjustmentDelta / adjustedTextWidth;
                                        m_CharWidthAdjDelta = Mathf.Min(m_CharWidthAdjDelta, generationSettings.charWidthMaxAdj / 100);

                                        return;
                                    }
                                    #endregion

                                    // Handle Text Auto-sizing resulting from text exceeding vertical bounds.
                                    #region Text Auto-Sizing (Text greater than vertical bounds)
                                    if (m_FontSize > generationSettings.fontSizeMin && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                    {
                                        m_MaxFontSize = m_FontSize;

                                        float sizeDelta = Mathf.Max((m_FontSize - m_MinFontSize) / 2, 0.05f);
                                        m_FontSize -= sizeDelta;
                                        m_FontSize = Mathf.Max((int)(m_FontSize * 20 + 0.5f) / 20f, generationSettings.fontSizeMin);

                                        return;
                                    }
                                    #endregion Text Auto-Sizing
                                }

                                // Check Text Overflow Modes
                                switch (generationSettings.overflowMode)
                                {
                                    case TextOverflowMode.Overflow:
                                    case TextOverflowMode.ScrollRect:
                                    case TextOverflowMode.Masking:
                                        InsertNewLine(i, baseScale, currentElementScale, currentEmScale, boldSpacingAdjustment, characterSpacingAdjustment, widthOfTextArea, lineGap, ref isMaxVisibleDescenderSet, ref maxVisibleDescender, generationSettings, textInfo);
                                        isStartOfNewLine = true;
                                        isFirstWordOfLine = true;
                                        continue;

                                    case TextOverflowMode.Truncate:
                                        i = RestoreWordWrappingState(ref m_SavedLastValidState, textInfo);

                                        characterToSubstitute.index = testedCharacterCount;
                                        characterToSubstitute.unicode = k_EndOfText;
                                        continue;

                                    case TextOverflowMode.Ellipsis:
                                        if (m_EllipsisInsertionCandidateStack.Count == 0)
                                        {
                                            i = -1;
                                            m_CharacterCount = 0;
                                            characterToSubstitute.index = 0;
                                            characterToSubstitute.unicode = k_EndOfText;
                                            m_FirstCharacterOfLine = 0;
                                            continue;
                                        }

                                        var ellipsisState = m_EllipsisInsertionCandidateStack.Pop();
                                        i = RestoreWordWrappingState(ref ellipsisState, textInfo);

                                        i -= 1;
                                        m_CharacterCount -= 1;
                                        characterToSubstitute.index = m_CharacterCount;
                                        characterToSubstitute.unicode = k_HorizontalEllipsis;

                                        restoreCount += 1;
                                        continue;
                                    case TextOverflowMode.Linked:
                                        // Truncate remaining text
                                        characterToSubstitute.index = m_CharacterCount;
                                        characterToSubstitute.unicode = k_EndOfText;
                                        continue;

                                    case TextOverflowMode.Page:
                                        // Add new page
                                        m_IsNewPage = true;

                                        InsertNewLine(i, baseScale, currentElementScale, currentEmScale, boldSpacingAdjustment, characterSpacingAdjustment, widthOfTextArea, lineGap, ref isMaxVisibleDescenderSet, ref maxVisibleDescender, generationSettings, textInfo);

                                        m_StartOfLineAscender = 0;
                                        m_LineOffset = 0;
                                        m_MaxAscender = 0;
                                        m_PageAscender = 0;
                                        m_PageNumber += 1;

                                        isStartOfNewLine = true;
                                        isFirstWordOfLine = true;
                                        continue;
                                }
                            }
                            else
                            {
                                //if (generationSettings.autoSize && isFirstWordOfLine)
                                //{
                                //    // Handle Character Width Adjustments
                                //    #region Character Width Adjustments
                                //    if (m_CharWidthAdjDelta < m_CharWidthMaxAdj / 100 && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                //    {
                                //        //m_AutoSizeIterationCount = 0;
                                //        float adjustedTextWidth = textWidth;

                                //        // Determine full width of the text
                                //        if (m_CharWidthAdjDelta > 0)
                                //            adjustedTextWidth /= 1f - m_CharWidthAdjDelta;

                                //        float adjustmentDelta = textWidth - (widthOfTextArea - 0.0001f) * (isJustifiedOrFlush ? 1.05f : 1.0f);
                                //        m_CharWidthAdjDelta += adjustmentDelta / adjustedTextWidth;
                                //        m_CharWidthAdjDelta = Mathf.Min(m_CharWidthAdjDelta, m_CharWidthMaxAdj / 100);

                                //        //Debug.Log("[" + m_AutoSizeIterationCount + "] Reducing Character Width by " + (m_CharWidthAdjDelta * 100) + "%");

                                //        GenerateTextMesh();
                                //        return;
                                //    }
                                //    #endregion
                                //}

                                // New line of text does not exceed vertical bounds of text container
                                InsertNewLine(i, baseScale, currentElementScale, currentEmScale, boldSpacingAdjustment, characterSpacingAdjustment, widthOfTextArea, lineGap, ref isMaxVisibleDescenderSet, ref maxVisibleDescender, generationSettings, textInfo);
                                isStartOfNewLine = true;
                                isFirstWordOfLine = true;
                                continue;
                            }
                        }
                        else
                        {
                            if (generationSettings.autoSize && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                            {
                                // Handle Character Width Adjustments
                                #region Character Width Adjustments
                                if (m_CharWidthAdjDelta < generationSettings.charWidthMaxAdj / 100)
                                {
                                    float adjustedTextWidth = textWidth;

                                    // Determine full width of the text
                                    if (m_CharWidthAdjDelta > 0)
                                        adjustedTextWidth /= 1f - m_CharWidthAdjDelta;

                                    float adjustmentDelta = textWidth - (widthOfTextArea - 0.0001f) * (isJustifiedOrFlush ? 1.05f : 1.0f);
                                    m_CharWidthAdjDelta += adjustmentDelta / adjustedTextWidth;
                                    m_CharWidthAdjDelta = Mathf.Min(m_CharWidthAdjDelta, generationSettings.charWidthMaxAdj / 100);

                                    return;
                                }
                                #endregion

                                // Handle Text Auto-sizing resulting from text exceeding horizontal bounds.
                                #region Text Exceeds Horizontal Bounds - Reducing Point Size
                                if (m_FontSize > generationSettings.fontSizeMin)
                                {
                                    // Adjust Point Size
                                    m_MaxFontSize = m_FontSize;

                                    float sizeDelta = Mathf.Max((m_FontSize - m_MinFontSize) / 2, 0.05f);
                                    m_FontSize -= sizeDelta;
                                    m_FontSize = Mathf.Max((int)(m_FontSize * 20 + 0.5f) / 20f, generationSettings.fontSizeMin);

                                    return;
                                }
                                #endregion
                            }

                            // Check Text Overflow Modes
                            switch (generationSettings.overflowMode)
                            {
                                case TextOverflowMode.Overflow:
                                case TextOverflowMode.ScrollRect:
                                case TextOverflowMode.Masking:
                                    // Nothing happens as horizontal bounds are ignored in this mode.
                                    break;

                                case TextOverflowMode.Truncate:
                                    i = RestoreWordWrappingState(ref m_SavedWordWrapState, textInfo);

                                    characterToSubstitute.index = testedCharacterCount;
                                    characterToSubstitute.unicode = k_EndOfText;
                                    continue;

                                case TextOverflowMode.Ellipsis:
                                    if (m_EllipsisInsertionCandidateStack.Count == 0)
                                    {
                                        i = -1;
                                        m_CharacterCount = 0;
                                        characterToSubstitute.index = 0;
                                        characterToSubstitute.unicode = k_EndOfText;
                                        m_FirstCharacterOfLine = 0;
                                        continue;
                                    }

                                    var ellipsisState = m_EllipsisInsertionCandidateStack.Pop();
                                    i = RestoreWordWrappingState(ref ellipsisState, textInfo);

                                    i -= 1;
                                    m_CharacterCount -= 1;
                                    characterToSubstitute.index = m_CharacterCount;
                                    characterToSubstitute.unicode = k_HorizontalEllipsis;

                                    restoreCount += 1;
                                    continue;
                                case TextOverflowMode.Linked:
                                    i = RestoreWordWrappingState(ref m_SavedWordWrapState, textInfo);

                                    // Truncate text the overflows the vertical bounds
                                    characterToSubstitute.index = m_CharacterCount;
                                    characterToSubstitute.unicode = k_EndOfText;
                                    continue;
                            }

                        }
                    }
                    #endregion


                    // Special handling of characters that are not ignored at the end of a line.
                    if (isWhiteSpace)
                    {
                        textInfo.textElementInfo[m_CharacterCount].isVisible = false;
                        m_LastVisibleCharacterOfLine = m_CharacterCount;
                        m_LineVisibleSpaceCount = textInfo.lineInfo[m_LineNumber].spaceCount += 1;
                        textInfo.lineInfo[m_LineNumber].marginLeft = marginLeft;
                        textInfo.lineInfo[m_LineNumber].marginRight = marginRight;
                        textInfo.spaceCount += 1;
                    }
                    else if (charCode == 0xAD)
                    {
                        textInfo.textElementInfo[m_CharacterCount].isVisible = false;
                    }
                    else
                    {
                        // Determine Vertex Color
                        Color32 vertexColor;
                        if (generationSettings.overrideRichTextColors)
                            vertexColor = m_FontColor32;
                        else
                            vertexColor = m_HtmlColor;

                        // Store Character & Sprite Vertex Information
                        if (m_TextElementType == TextElementType.Character)
                        {
                            // Save Character Vertex Data
                            SaveGlyphVertexInfo(padding, stylePadding, vertexColor, generationSettings, textInfo);
                        }
                        else if (m_TextElementType == TextElementType.Sprite)
                        {
                            SaveSpriteVertexInfo(vertexColor, generationSettings, textInfo);
                        }

                        if (isStartOfNewLine)
                        {
                            isStartOfNewLine = false;
                            m_FirstVisibleCharacterOfLine = m_CharacterCount;
                        }

                        m_LineVisibleCharacterCount += 1;
                        m_LastVisibleCharacterOfLine = m_CharacterCount;
                        textInfo.lineInfo[m_LineNumber].marginLeft = marginLeft;
                        textInfo.lineInfo[m_LineNumber].marginRight = marginRight;
                    }
                }
                else
                {
                    // Special handling for text overflow linked mode
                    #region Check Vertical Bounds
                    if (generationSettings.overflowMode == TextOverflowMode.Linked && (charCode == k_LineFeed || charCode == 11))
                    {
                        float textHeight = m_MaxAscender - (m_MaxLineDescender - m_LineOffset) + (m_LineOffset > 0 && m_IsDrivenLineSpacing == false ? m_MaxLineAscender - m_StartOfLineAscender : 0);

                        int testedCharacterCount = m_CharacterCount;

                        if (textHeight > marginHeight + 0.0001f)
                        {
                            // Set isTextOverflowing and firstOverflowCharacterIndex
                            if (m_FirstOverflowCharacterIndex == -1)
                                m_FirstOverflowCharacterIndex = m_CharacterCount;

                            i = RestoreWordWrappingState(ref m_SavedLastValidState, textInfo);

                            // Truncate remaining text
                            characterToSubstitute.index = testedCharacterCount;
                            characterToSubstitute.unicode = k_EndOfText;
                            continue;
                        }
                    }
                    #endregion

                    // Track # of spaces per line which is used for line justification.
                    if ((charCode == k_LineFeed || charCode == 11 || charCode == 0xA0 || charCode == k_FigureSpace || charCode == 0x2028 || charCode == 0x2029 || char.IsSeparator((char)charCode)) && charCode != 0xAD && charCode != k_ZeroWidthSpace && charCode != k_WordJoiner)
                    {
                        textInfo.lineInfo[m_LineNumber].spaceCount += 1;
                        textInfo.spaceCount += 1;
                    }

                    // Special handling for control characters like <NBSP>
                    if (charCode == 0xA0)
                        textInfo.lineInfo[m_LineNumber].controlCharacterCount += 1;

                }
                #endregion Handle Visible Characters


                // Tracking of potential insertion positions for Ellipsis character
                #region Track Potential Insertion Location for Ellipsis
                if (generationSettings.overflowMode == TextOverflowMode.Ellipsis && (isInjectedCharacter == false || charCode == 0x2D))
                {
                    float fontScale = m_CurrentFontSize / m_Ellipsis.fontAsset.m_FaceInfo.pointSize * m_Ellipsis.fontAsset.m_FaceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f);
                    float scale = fontScale * m_FontScaleMultiplier * m_Ellipsis.character.m_Scale * m_Ellipsis.character.m_Glyph.scale;
                    float marginLeft = m_MarginLeft;
                    float marginRight = m_MarginRight;

                    // Use the scale and margins of the previous character if Line Feed (LF) is not the first character of a line.
                    if (charCode == 0x0A && m_CharacterCount != m_FirstCharacterOfLine)
                    {
                        fontScale = textInfo.textElementInfo[m_CharacterCount - 1].pointSize / m_Ellipsis.fontAsset.m_FaceInfo.pointSize * m_Ellipsis.fontAsset.m_FaceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f);
                        scale = fontScale * m_FontScaleMultiplier * m_Ellipsis.character.m_Scale * m_Ellipsis.character.m_Glyph.scale;
                        marginLeft = textInfo.lineInfo[m_LineNumber].marginLeft;
                        marginRight = textInfo.lineInfo[m_LineNumber].marginRight;
                    }

                    float textHeight = m_MaxAscender - (m_MaxLineDescender - m_LineOffset) + (m_LineOffset > 0 && m_IsDrivenLineSpacing == false ? m_MaxLineAscender - m_StartOfLineAscender : 0);
                    float textWidth = Mathf.Abs(m_XAdvance) + (!generationSettings.isRightToLeft ? m_Ellipsis.character.m_Glyph.metrics.horizontalAdvance : 0) * (1 - m_CharWidthAdjDelta) * scale;
                    float widthOfTextAreaForEllipsis = m_Width != -1 ? Mathf.Min(marginWidth + 0.0001f - marginLeft - marginRight, m_Width) : marginWidth + 0.0001f - marginLeft - marginRight;

                    if (textWidth < widthOfTextAreaForEllipsis * (isJustifiedOrFlush ? 1.05f : 1.0f))
                    {
                        SaveWordWrappingState(ref m_SavedEllipsisState, i, m_CharacterCount, textInfo);
                        m_EllipsisInsertionCandidateStack.Push(m_SavedEllipsisState);
                    }
                }
                #endregion


                // Store Rectangle positions for each Character.
                #region Store Character Data
                textInfo.textElementInfo[m_CharacterCount].lineNumber = m_LineNumber;
                textInfo.textElementInfo[m_CharacterCount].pageNumber = m_PageNumber;

                if (charCode != 10 && charCode != 11 && charCode != 13 && isInjectedCharacter == false /* && charCode != k_HorizontalEllipsis */ || textInfo.lineInfo[m_LineNumber].characterCount == 1)
                    textInfo.lineInfo[m_LineNumber].alignment = m_LineJustification;
                #endregion Store Character Data


                // Handle xAdvance & Tabulation Stops. Tab stops at every 25% of Font Size.
                #region XAdvance, Tabulation & Stops
                if (charCode == k_Tab)
                {
                    float tabSize = m_CurrentFontAsset.m_FaceInfo.tabWidth * m_CurrentFontAsset.tabMultiple * currentElementScale;
                    float tabs = Mathf.Ceil(m_XAdvance / tabSize) * tabSize;
                    m_XAdvance = tabs > m_XAdvance ? tabs : m_XAdvance + tabSize;
                }
                else if (m_MonoSpacing != 0)
                {
                    m_XAdvance += (m_MonoSpacing - monoAdvance + ((m_CurrentFontAsset.regularStyleSpacing + characterSpacingAdjustment) * currentEmScale) + m_CSpacing) * (1 - m_CharWidthAdjDelta);

                    if (isWhiteSpace || charCode == k_ZeroWidthSpace)
                        m_XAdvance += generationSettings.wordSpacing * currentEmScale;
                }
                else if (generationSettings.isRightToLeft)
                {
                    m_XAdvance -= ((glyphAdjustments.xAdvance * currentElementScale + (m_CurrentFontAsset.regularStyleSpacing + characterSpacingAdjustment + boldSpacingAdjustment) * currentEmScale + m_CSpacing) * (1 - m_CharWidthAdjDelta));

                    if (isWhiteSpace || charCode == k_ZeroWidthSpace)
                        m_XAdvance -= generationSettings.wordSpacing * currentEmScale;
                }
                else
                {
                    m_XAdvance += ((currentGlyphMetrics.horizontalAdvance * m_FXScale.x + glyphAdjustments.xAdvance) * currentElementScale + (m_CurrentFontAsset.regularStyleSpacing + characterSpacingAdjustment + boldSpacingAdjustment) * currentEmScale + m_CSpacing) * (1 - m_CharWidthAdjDelta);

                    if (isWhiteSpace || charCode == k_ZeroWidthSpace)
                        m_XAdvance += generationSettings.wordSpacing * currentEmScale;
                }

                // Store xAdvance information
                textInfo.textElementInfo[m_CharacterCount].xAdvance = m_XAdvance;
                #endregion Tabulation & Stops


                // Handle Carriage Return
                #region Carriage Return
                if (charCode == k_CarriageReturn)
                {
                    m_XAdvance = 0 + m_TagIndent;
                }
                #endregion Carriage Return


                // Tracking of text overflow page mode
                #region Save PageInfo
                if (generationSettings.overflowMode == TextOverflowMode.Page && charCode != 10 && charCode != 11 && charCode != 13 && charCode != 0x2028 && charCode != 0x2029)
                {
                    // Check if we need to increase allocations for the pageInfo array.
                    if (m_PageNumber + 1 > textInfo.pageInfo.Length)
                        TextInfo.Resize(ref textInfo.pageInfo, m_PageNumber + 1, true);

                    textInfo.pageInfo[m_PageNumber].ascender = m_PageAscender;
                    textInfo.pageInfo[m_PageNumber].descender = m_MaxDescender < textInfo.pageInfo[m_PageNumber].descender
                        ? m_MaxDescender
                        : textInfo.pageInfo[m_PageNumber].descender;

                    if (m_IsNewPage)
                    {
                        m_IsNewPage = false;
                        textInfo.pageInfo[m_PageNumber].firstCharacterIndex = m_CharacterCount;
                    }

                    // Last index
                    textInfo.pageInfo[m_PageNumber].lastCharacterIndex = m_CharacterCount;
                }
                #endregion Save PageInfo


                // Handle Line Spacing Adjustments + Word Wrapping & special case for last line.
                #region Check for Line Feed and Last Character
                if (charCode == k_LineFeed || charCode == 11 || charCode == k_EndOfText || charCode == 0x2028 || charCode == 0x2029 || (charCode == 0x2D && isInjectedCharacter) || m_CharacterCount == totalCharacterCount - 1)
                {
                    // Adjust current line spacing (if necessary) before inserting new line
                    float baselineAdjustmentDelta = m_MaxLineAscender - m_StartOfLineAscender;
                    if (m_LineOffset > 0 && Math.Abs(baselineAdjustmentDelta) > 0.01f && m_IsDrivenLineSpacing == false && !m_IsNewPage)
                    {
                        TextGeneratorUtilities.AdjustLineOffset(m_FirstCharacterOfLine, m_CharacterCount, baselineAdjustmentDelta, textInfo);
                        m_MaxDescender -= baselineAdjustmentDelta;
                        m_LineOffset += baselineAdjustmentDelta;

                        // Adjust saved ellipsis state only if we are adjusting the same line number
                        if (m_SavedEllipsisState.lineNumber == m_LineNumber)
                        {
                            m_SavedEllipsisState = m_EllipsisInsertionCandidateStack.Pop();
                            m_SavedEllipsisState.startOfLineAscender += baselineAdjustmentDelta;
                            m_SavedEllipsisState.lineOffset += baselineAdjustmentDelta;
                            m_EllipsisInsertionCandidateStack.Push(m_SavedEllipsisState);
                        }
                    }
                    m_IsNewPage = false;

                    // Calculate lineAscender & make sure if last character is superscript or subscript that we check that as well.
                    float lineAscender = m_MaxLineAscender - m_LineOffset;
                    float lineDescender = m_MaxLineDescender - m_LineOffset;

                    // Update maxDescender and maxVisibleDescender
                    m_MaxDescender = m_MaxDescender < lineDescender ? m_MaxDescender : lineDescender;
                    if (!isMaxVisibleDescenderSet)
                        maxVisibleDescender = m_MaxDescender;

                    if (generationSettings.useMaxVisibleDescender && (m_CharacterCount >= generationSettings.maxVisibleCharacters || m_LineNumber >= generationSettings.maxVisibleLines))
                        isMaxVisibleDescenderSet = true;

                    // Save Line Information
                    textInfo.lineInfo[m_LineNumber].firstCharacterIndex = m_FirstCharacterOfLine;
                    textInfo.lineInfo[m_LineNumber].firstVisibleCharacterIndex = m_FirstVisibleCharacterOfLine = m_FirstCharacterOfLine > m_FirstVisibleCharacterOfLine ? m_FirstCharacterOfLine : m_FirstVisibleCharacterOfLine;
                    textInfo.lineInfo[m_LineNumber].lastCharacterIndex = m_LastCharacterOfLine = m_CharacterCount;
                    textInfo.lineInfo[m_LineNumber].lastVisibleCharacterIndex = m_LastVisibleCharacterOfLine = m_LastVisibleCharacterOfLine < m_FirstVisibleCharacterOfLine ? m_FirstVisibleCharacterOfLine : m_LastVisibleCharacterOfLine;

                    textInfo.lineInfo[m_LineNumber].characterCount = textInfo.lineInfo[m_LineNumber].lastCharacterIndex - textInfo.lineInfo[m_LineNumber].firstCharacterIndex + 1;
                    textInfo.lineInfo[m_LineNumber].visibleCharacterCount = m_LineVisibleCharacterCount;
                    textInfo.lineInfo[m_LineNumber].visibleSpaceCount = m_LineVisibleSpaceCount;
                    textInfo.lineInfo[m_LineNumber].lineExtents.min = new Vector2(textInfo.textElementInfo[m_FirstVisibleCharacterOfLine].bottomLeft.x, lineDescender);
                    textInfo.lineInfo[m_LineNumber].lineExtents.max = new Vector2(textInfo.textElementInfo[m_LastVisibleCharacterOfLine].topRight.x, lineAscender);
                    textInfo.lineInfo[m_LineNumber].length = textInfo.lineInfo[m_LineNumber].lineExtents.max.x - (padding * currentElementScale);
                    textInfo.lineInfo[m_LineNumber].width = widthOfTextArea;

                    if (textInfo.lineInfo[m_LineNumber].characterCount == 1)
                        textInfo.lineInfo[m_LineNumber].alignment = m_LineJustification;

                    float maxAdvanceOffset = ((m_CurrentFontAsset.regularStyleSpacing + characterSpacingAdjustment + boldSpacingAdjustment) * currentEmScale + m_CSpacing) * (1 - m_CharWidthAdjDelta);
                    if (textInfo.textElementInfo[m_LastVisibleCharacterOfLine].isVisible)
                        textInfo.lineInfo[m_LineNumber].maxAdvance = textInfo.textElementInfo[m_LastVisibleCharacterOfLine].xAdvance + (generationSettings.isRightToLeft ? maxAdvanceOffset : -maxAdvanceOffset);
                    else
                        textInfo.lineInfo[m_LineNumber].maxAdvance = textInfo.textElementInfo[m_LastCharacterOfLine].xAdvance + (generationSettings.isRightToLeft ? maxAdvanceOffset : -maxAdvanceOffset);

                    textInfo.lineInfo[m_LineNumber].baseline = 0 - m_LineOffset;
                    textInfo.lineInfo[m_LineNumber].ascender = lineAscender;
                    textInfo.lineInfo[m_LineNumber].descender = lineDescender;
                    textInfo.lineInfo[m_LineNumber].lineHeight = lineAscender - lineDescender + lineGap * baseScale;

                    // Add new line if not last line or character.
                    if (charCode == k_LineFeed || charCode == 11 || charCode == 0x2D || charCode == 0x2028 || charCode == 0x2029)
                    {
                        // Store the state of the line before starting on the new line.
                        SaveWordWrappingState(ref m_SavedLineState, i, m_CharacterCount, textInfo);

                        m_LineNumber += 1;
                        isStartOfNewLine = true;
                        ignoreNonBreakingSpace = false;
                        isFirstWordOfLine = true;

                        m_FirstCharacterOfLine = m_CharacterCount + 1;
                        m_LineVisibleCharacterCount = 0;
                        m_LineVisibleSpaceCount = 0;

                        // Check to make sure Array is large enough to hold a new line.
                        if (m_LineNumber >= textInfo.lineInfo.Length)
                            TextGeneratorUtilities.ResizeLineExtents(m_LineNumber, textInfo);

                        float lastVisibleAscender = textInfo.textElementInfo[m_CharacterCount].adjustedAscender;

                        // Apply Line Spacing with special handling for VT char(11)
                        if (m_LineHeight == k_FloatUnset)
                        {
                            float lineOffsetDelta = 0 - m_MaxLineDescender + lastVisibleAscender + (lineGap + m_LineSpacingDelta) * baseScale + (generationSettings.lineSpacing + (charCode == k_LineFeed || charCode == 0x2029 ? generationSettings.paragraphSpacing : 0)) * currentEmScale;
                            m_LineOffset += lineOffsetDelta;
                            m_IsDrivenLineSpacing = false;
                        }
                        else
                        {
                            m_LineOffset += m_LineHeight + (generationSettings.lineSpacing + (charCode == k_LineFeed || charCode == 0x2029 ? generationSettings.paragraphSpacing : 0)) * currentEmScale;
                            m_IsDrivenLineSpacing = true;
                        }

                        m_MaxLineAscender = TextGeneratorUtilities.largeNegativeFloat;
                        m_MaxLineDescender = TextGeneratorUtilities.largePositiveFloat;
                        m_StartOfLineAscender = lastVisibleAscender;

                        m_XAdvance = 0 + m_TagLineIndent + m_TagIndent;

                        SaveWordWrappingState(ref m_SavedWordWrapState, i, m_CharacterCount, textInfo);
                        SaveWordWrappingState(ref m_SavedLastValidState, i, m_CharacterCount, textInfo);

                        m_CharacterCount += 1;

                        continue;
                    }

                    // If End of Text
                    if (charCode == k_EndOfText)
                        i = m_TextProcessingArray.Length;
                }
                #endregion Check for Linefeed or Last Character


                // Track extents of the text
                #region Track Text Extents
                // Determine the bounds of the Mesh.
                if (textInfo.textElementInfo[m_CharacterCount].isVisible)
                {
                    m_MeshExtents.min.x = Mathf.Min(m_MeshExtents.min.x, textInfo.textElementInfo[m_CharacterCount].bottomLeft.x);
                    m_MeshExtents.min.y = Mathf.Min(m_MeshExtents.min.y, textInfo.textElementInfo[m_CharacterCount].bottomLeft.y);

                    m_MeshExtents.max.x = Mathf.Max(m_MeshExtents.max.x, textInfo.textElementInfo[m_CharacterCount].topRight.x);
                    m_MeshExtents.max.y = Mathf.Max(m_MeshExtents.max.y, textInfo.textElementInfo[m_CharacterCount].topRight.y);

                    //m_MeshExtents.min = new Vector2(Mathf.Min(m_MeshExtents.min.x, textInfo.textElementInfo[m_CharacterCount].bottomLeft.x), Mathf.Min(m_MeshExtents.min.y, textInfo.textElementInfo[m_CharacterCount].bottomLeft.y));
                    //m_MeshExtents.max = new Vector2(Mathf.Max(m_MeshExtents.max.x, textInfo.textElementInfo[m_CharacterCount].topRight.x), Mathf.Max(m_MeshExtents.max.y, textInfo.textElementInfo[m_CharacterCount].topRight.y));
                }
                #endregion Track Text Extents


                // Save State of Mesh Creation for handling of Word Wrapping
                #region Save Word Wrapping State
                if ((wordWrap != TextWrappingMode.NoWrap && wordWrap != TextWrappingMode.PreserveWhitespaceNoWrap) || generationSettings.overflowMode == TextOverflowMode.Truncate || generationSettings.overflowMode == TextOverflowMode.Ellipsis || generationSettings.overflowMode == TextOverflowMode.Linked)
                {
                    if ((isWhiteSpace || charCode == k_ZeroWidthSpace || charCode == 0x2D || charCode == 0xAD) && (!m_IsNonBreakingSpace || ignoreNonBreakingSpace) && charCode != 0xA0 && charCode != k_FigureSpace && charCode != k_NonBreakingHyphen && charCode != k_NarrowNoBreakSpace && charCode != k_WordJoiner)
                    {
                        // We store the state of numerous variables for the most recent Space, LineFeed or Carriage Return to enable them to be restored
                        // for Word Wrapping.
                        SaveWordWrappingState(ref m_SavedWordWrapState, i, m_CharacterCount, textInfo);
                        isFirstWordOfLine = false;

                        // Reset soft line breaking point since we now have a valid hard break point.
                        m_SavedSoftLineBreakState.previousWordBreak = -1;
                    }
                    // Handling for East Asian characters
                    else if (m_IsNonBreakingSpace == false &&
                             ((charCode > k_HangulJamoStart && charCode < k_HangulJamoEnd || /* Hangul Jamo */
                               charCode > k_HangulJameExtendedStart && charCode < k_HangulJameExtendedEnd || /* Hangul Jamo Extended-A */
                               charCode > k_HangulSyllablesStart && charCode < k_HangulSyllablesEnd) && /* Hangul Syllables */
                              generationSettings.textSettings.lineBreakingRules.useModernHangulLineBreakingRules == false ||

                              (charCode > k_CjkStart && charCode < k_CjkEnd || /* CJK */
                               charCode > k_CjkIdeographsStart && charCode < k_CjkIdeographsEnd || /* CJK Compatibility Ideographs */
                               charCode > k_CjkFormsStart && charCode < k_CjkFormsEnd || /* CJK Compatibility Forms */
                               charCode > k_CjkHalfwidthStart && charCode < k_CjkHalfwidthEnd))) /* CJK Halfwidth */
                    {
                        bool isCurrentLeadingCharacter = textSettings.lineBreakingRules.leadingCharactersLookup.Contains((uint)charCode);
                        bool isNextFollowingCharacter = m_CharacterCount < totalCharacterCount - 1 && textSettings.lineBreakingRules.followingCharactersLookup.Contains((uint)textInfo.textElementInfo[m_CharacterCount + 1].character);

                        if (isCurrentLeadingCharacter == false)
                        {
                            if (isNextFollowingCharacter == false)
                            {
                                SaveWordWrappingState(ref m_SavedWordWrapState, i, m_CharacterCount, textInfo);
                                isFirstWordOfLine = false;
                            }

                            if (isFirstWordOfLine)
                            {
                                // Special handling for non-breaking space and soft line breaks
                                if (isWhiteSpace)
                                    SaveWordWrappingState(ref m_SavedSoftLineBreakState, i, m_CharacterCount, textInfo);

                                SaveWordWrappingState(ref m_SavedWordWrapState, i, m_CharacterCount, textInfo);
                            }
                        }
                        else
                        {
                            if (isFirstWordOfLine && isFirstCharacterOfLine)
                            {
                                // Special handling for non-breaking space and soft line breaks
                                if (isWhiteSpace)
                                    SaveWordWrappingState(ref m_SavedSoftLineBreakState, i, m_CharacterCount, textInfo);

                                SaveWordWrappingState(ref m_SavedWordWrapState, i, m_CharacterCount, textInfo);
                            }
                        }
                    }
                    else if (isFirstWordOfLine)
                    {
                        // Special handling for non-breaking space and soft line breaks
                        if (isWhiteSpace && charCode != 0xA0 || (charCode == 0xAD && isSoftHyphenIgnored == false))
                            SaveWordWrappingState(ref m_SavedSoftLineBreakState, i, m_CharacterCount, textInfo);

                        SaveWordWrappingState(ref m_SavedWordWrapState, i, m_CharacterCount, textInfo);
                    }
                }
                #endregion Save Word Wrapping State

                // Consider only saving state on base glyphs
                SaveWordWrappingState(ref m_SavedLastValidState, i, m_CharacterCount, textInfo);

                m_CharacterCount += 1;
            }

            // Check Auto Sizing and increase font size to fill text container.
            #region Check Auto-Sizing (Upper Font Size Bounds)
            fontSizeDelta = m_MaxFontSize - m_MinFontSize;
            if (/* !m_isCharacterWrappingEnabled && */ generationSettings.autoSize && fontSizeDelta > 0.051f && m_FontSize < generationSettings.fontSizeMax && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
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

            m_IsAutoSizePointSizeSet = true;

            if (m_AutoSizeIterationCount >= m_AutoSizeMaxIterationCount)
                Debug.Log("Auto Size Iteration Count: " + m_AutoSizeIterationCount + ". Final Point Size: " + m_FontSize);

            // If there are no visible characters or only character is End of Text (0x03)... no need to continue
            if (m_CharacterCount == 0 || (m_CharacterCount == 1 && charCode == k_EndOfText))
            {
                ClearMesh(true, textInfo);
                return;
            }

            // *** PHASE II of Text Generation ***

            // Partial clear of the vertices array to mark unused vertices as degenerate.
            textInfo.meshInfo[m_CurrentMaterialIndex].Clear(false);

            // Handle Text Alignment
            #region Text Vertical Alignment
            Vector3 anchorOffset = Vector3.zero;
            Vector3[] corners = m_RectTransformCorners; // GetTextContainerLocalCorners();

            // Handle Vertical Text Alignment
            switch (generationSettings.textAlignment)
            {
                // Top Vertically
                case TextAlignment.TopCenter:
                case TextAlignment.TopLeft:
                case TextAlignment.TopRight:
                case TextAlignment.TopJustified:
                case TextAlignment.TopFlush:
                case TextAlignment.TopGeoAligned:
                    if (generationSettings.overflowMode != TextOverflowMode.Page)
                        anchorOffset = corners[1] + new Vector3(0 + margins.x, 0 - m_MaxAscender - margins.y, 0);
                    else
                        anchorOffset = corners[1] + new Vector3(0 + margins.x, 0 - textInfo.pageInfo[pageToDisplay].ascender - margins.y, 0);
                    break;

                // Middle Vertically
                case TextAlignment.MiddleLeft:
                case TextAlignment.MiddleRight:
                case TextAlignment.MiddleCenter:
                case TextAlignment.MiddleJustified:
                case TextAlignment.MiddleFlush:
                case TextAlignment.MiddleGeoAligned:
                    if (generationSettings.overflowMode != TextOverflowMode.Page)
                        anchorOffset = (corners[0] + corners[1]) / 2 + new Vector3(0 + margins.x, 0 - (m_MaxAscender + margins.y + maxVisibleDescender - margins.w) / 2, 0);
                    else
                        anchorOffset = (corners[0] + corners[1]) / 2 + new Vector3(0 + margins.x, 0 - (textInfo.pageInfo[pageToDisplay].ascender + margins.y + textInfo.pageInfo[pageToDisplay].descender - margins.w) / 2, 0);
                    break;

                // Bottom Vertically
                case TextAlignment.BottomCenter:
                case TextAlignment.BottomLeft:
                case TextAlignment.BottomRight:
                case TextAlignment.BottomJustified:
                case TextAlignment.BottomFlush:
                case TextAlignment.BottomGeoAligned:
                    if (generationSettings.overflowMode != TextOverflowMode.Page)
                        anchorOffset = corners[0] + new Vector3(0 + margins.x, 0 - maxVisibleDescender + margins.w, 0);
                    else
                        anchorOffset = corners[0] + new Vector3(0 + margins.x, 0 - textInfo.pageInfo[pageToDisplay].descender + margins.w, 0);
                    break;

                // Baseline Vertically
                case TextAlignment.BaselineCenter:
                case TextAlignment.BaselineLeft:
                case TextAlignment.BaselineRight:
                case TextAlignment.BaselineJustified:
                case TextAlignment.BaselineFlush:
                case TextAlignment.BaselineGeoAligned:
                    anchorOffset = (corners[0] + corners[1]) / 2 + new Vector3(0 + margins.x, 0, 0);
                    break;

                // Midline Vertically
                case TextAlignment.MidlineLeft:
                case TextAlignment.MidlineCenter:
                case TextAlignment.MidlineRight:
                case TextAlignment.MidlineJustified:
                case TextAlignment.MidlineFlush:
                case TextAlignment.MidlineGeoAligned:
                    anchorOffset = (corners[0] + corners[1]) / 2 + new Vector3(0 + margins.x, 0 - (m_MeshExtents.max.y + margins.y + m_MeshExtents.min.y - margins.w) / 2, 0);
                    break;

                // Capline Vertically
                case TextAlignment.CaplineLeft:
                case TextAlignment.CaplineCenter:
                case TextAlignment.CaplineRight:
                case TextAlignment.CaplineJustified:
                case TextAlignment.CaplineFlush:
                case TextAlignment.CaplineGeoAligned:
                    anchorOffset = (corners[0] + corners[1]) / 2 + new Vector3(0 + margins.x, 0 - (m_MaxCapHeight - margins.y - margins.w) / 2, 0);
                    break;
            }
            #endregion

            // Initialization for Second Pass
            Vector3 justificationOffset = Vector3.zero;
            Vector3 offset = Vector3.zero;

            int wordCount = 0;
            int lineCount = 0;
            int lastLine = 0;
            bool isFirstSeperator = false;

            bool isStartOfWord = false;
            int wordFirstChar = 0;
            int wordLastChar = 0;

            // Second Pass : Line Justification, UV Mapping, Character & Line Visibility & more.
            Color32 underlineColor = Color.white;
            Color32 strikethroughColor = Color.white;
            HighlightState highlightState = new HighlightState(new Color32(255, 255, 0, 64), Offset.zero);
            float xScale = 0;
            float xScaleMax = 0;
            float underlineStartScale = 0;
            float underlineEndScale = 0;
            float underlineMaxScale = 0;
            float underlineBaseLine = TextGeneratorUtilities.largePositiveFloat;
            int lastPage = 0;

            float strikethroughPointSize = 0;
            float strikethroughScale = 0;
            float strikethroughBaseline = 0;

            TextElementInfo[] textElementInfos = textInfo.textElementInfo;
            #region Handle Line Justification & UV Mapping & Character Visibility & More
            for (int i = 0; i < m_CharacterCount; i++)
            {
                FontAsset currentFontAsset = textElementInfos[i].fontAsset;

                char unicode = textElementInfos[i].character;
                bool isWhiteSpace = char.IsWhiteSpace(unicode);

                int currentLine = textElementInfos[i].lineNumber;
                LineInfo lineInfo = textInfo.lineInfo[currentLine];
                lineCount = currentLine + 1;

                TextAlignment lineAlignment = lineInfo.alignment;

                // Process Line Justification
                #region Handle Line Justification
                switch (lineAlignment)
                {
                    case TextAlignment.TopLeft:
                    case TextAlignment.MiddleLeft:
                    case TextAlignment.BottomLeft:
                    case TextAlignment.BaselineLeft:
                    case TextAlignment.MidlineLeft:
                    case TextAlignment.CaplineLeft:
                        if (!generationSettings.isRightToLeft)
                            justificationOffset = new Vector3(0 + lineInfo.marginLeft, 0, 0);
                        else
                            justificationOffset = new Vector3(0 - lineInfo.maxAdvance, 0, 0);
                        break;

                    case TextAlignment.TopCenter:
                    case TextAlignment.MiddleCenter:
                    case TextAlignment.BottomCenter:
                    case TextAlignment.BaselineCenter:
                    case TextAlignment.MidlineCenter:
                    case TextAlignment.CaplineCenter:
                        justificationOffset = new Vector3(lineInfo.marginLeft + lineInfo.width / 2 - lineInfo.maxAdvance / 2, 0, 0);
                        break;

                    case TextAlignment.TopGeoAligned:
                    case TextAlignment.MiddleGeoAligned:
                    case TextAlignment.BottomGeoAligned:
                    case TextAlignment.BaselineGeoAligned:
                    case TextAlignment.MidlineGeoAligned:
                    case TextAlignment.CaplineGeoAligned:
                        justificationOffset = new Vector3(lineInfo.marginLeft + lineInfo.width / 2 - (lineInfo.lineExtents.min.x + lineInfo.lineExtents.max.x) / 2, 0, 0);
                        break;

                    case TextAlignment.TopRight:
                    case TextAlignment.MiddleRight:
                    case TextAlignment.BottomRight:
                    case TextAlignment.BaselineRight:
                    case TextAlignment.MidlineRight:
                    case TextAlignment.CaplineRight:
                        if (!generationSettings.isRightToLeft)
                            justificationOffset = new Vector3(lineInfo.marginLeft + lineInfo.width - lineInfo.maxAdvance, 0, 0);
                        else
                            justificationOffset = new Vector3(lineInfo.marginLeft + lineInfo.width, 0, 0);
                        break;

                    case TextAlignment.TopJustified:
                    case TextAlignment.MiddleJustified:
                    case TextAlignment.BottomJustified:
                    case TextAlignment.BaselineJustified:
                    case TextAlignment.MidlineJustified:
                    case TextAlignment.CaplineJustified:
                    case TextAlignment.TopFlush:
                    case TextAlignment.MiddleFlush:
                    case TextAlignment.BottomFlush:
                    case TextAlignment.BaselineFlush:
                    case TextAlignment.MidlineFlush:
                    case TextAlignment.CaplineFlush:
                        // Skip Zero Width Characters and spaces outside of the margins.
                        if (i > lineInfo.lastVisibleCharacterIndex || unicode == 0x0A || unicode == 0xAD || unicode == k_ZeroWidthSpace || unicode == k_WordJoiner || unicode == k_EndOfText) break;

                        char lastCharOfCurrentLine = textElementInfos[lineInfo.lastCharacterIndex].character;

                        bool isFlush = ((HorizontalAlignment)lineAlignment & HorizontalAlignment.Flush) == HorizontalAlignment.Flush;

                        // In Justified mode, all lines are justified except the last one.
                        // In Flush mode, all lines are justified.
                        if (char.IsControl(lastCharOfCurrentLine) == false && currentLine < m_LineNumber || isFlush || lineInfo.maxAdvance > lineInfo.width)
                        {
                            // First character of each line.
                            if (currentLine != lastLine || i == 0 || i == generationSettings.firstVisibleCharacter)
                            {
                                if (!generationSettings.isRightToLeft)
                                    justificationOffset = new Vector3(lineInfo.marginLeft, 0, 0);
                                else
                                    justificationOffset = new Vector3(lineInfo.marginLeft + lineInfo.width, 0, 0);

                                if (char.IsSeparator(unicode))
                                    isFirstSeperator = true;
                                else
                                    isFirstSeperator = false;
                            }
                            else
                            {
                                float gap = !generationSettings.isRightToLeft ? lineInfo.width - lineInfo.maxAdvance : lineInfo.width + lineInfo.maxAdvance;
                                int visibleCount = lineInfo.visibleCharacterCount - 1 + lineInfo.controlCharacterCount;
                                int spaces = lineInfo.visibleSpaceCount - lineInfo.controlCharacterCount;

                                if (isFirstSeperator) { spaces -= 1; visibleCount += 1; }

                                float ratio = spaces > 0 ? generationSettings.wordWrappingRatio : 1;

                                if (spaces < 1) spaces = 1;

                                if (unicode != 0xA0 && (unicode == k_Tab || char.IsSeparator(unicode)))
                                {
                                    if (!generationSettings.isRightToLeft)
                                        justificationOffset += new Vector3(gap * (1 - ratio) / spaces, 0, 0);
                                    else
                                        justificationOffset -= new Vector3(gap * (1 - ratio) / spaces, 0, 0);
                                }
                                else
                                {
                                    if (!generationSettings.isRightToLeft)
                                        justificationOffset += new Vector3(gap * ratio / visibleCount, 0, 0);
                                    else
                                        justificationOffset -= new Vector3(gap * ratio / visibleCount, 0, 0);
                                }
                            }
                        }
                        else
                        {
                            if (!generationSettings.isRightToLeft)
                                justificationOffset = new Vector3(lineInfo.marginLeft, 0, 0); // Keep last line left justified.
                            else
                                justificationOffset = new Vector3(lineInfo.marginLeft + lineInfo.width, 0, 0); // Keep last line right justified.
                        }
                        break;
                }
                #endregion End Text Justification

                offset = anchorOffset + justificationOffset;

                // Handle UV2 mapping options and packing of scale information into UV2.
                #region Handling of UV2 mapping & Scale packing
                bool isCharacterVisible = textElementInfos[i].isVisible;
                if (isCharacterVisible)
                {
                    TextElementType elementType = textElementInfos[i].elementType;
                    switch (elementType)
                    {
                        // CHARACTERS
                        case TextElementType.Character:
                            Extents lineExtents = lineInfo.lineExtents;
                            float uvOffset = (generationSettings.uvLineOffset * currentLine) % 1; // + m_uvOffset.x;

                            // Setup UV2 based on Character Mapping Options Selected
                            #region Handle UV Mapping Options
                            switch (generationSettings.horizontalMapping)
                            {
                                case TextureMapping.Character:
                                    textElementInfos[i].vertexBottomLeft.uv2.x = 0; //+ m_uvOffset.x;
                                    textElementInfos[i].vertexTopLeft.uv2.x = 0; //+ m_uvOffset.x;
                                    textElementInfos[i].vertexTopRight.uv2.x = 1; //+ m_uvOffset.x;
                                    textElementInfos[i].vertexBottomRight.uv2.x = 1; //+ m_uvOffset.x;
                                    break;

                                case TextureMapping.Line:
                                    if (generationSettings.textAlignment != TextAlignment.MiddleJustified)
                                    {
                                        textElementInfos[i].vertexBottomLeft.uv2.x = (textElementInfos[i].vertexBottomLeft.position.x - lineExtents.min.x) / (lineExtents.max.x - lineExtents.min.x) + uvOffset;
                                        textElementInfos[i].vertexTopLeft.uv2.x = (textElementInfos[i].vertexTopLeft.position.x - lineExtents.min.x) / (lineExtents.max.x - lineExtents.min.x) + uvOffset;
                                        textElementInfos[i].vertexTopRight.uv2.x = (textElementInfos[i].vertexTopRight.position.x - lineExtents.min.x) / (lineExtents.max.x - lineExtents.min.x) + uvOffset;
                                        textElementInfos[i].vertexBottomRight.uv2.x = (textElementInfos[i].vertexBottomRight.position.x - lineExtents.min.x) / (lineExtents.max.x - lineExtents.min.x) + uvOffset;
                                        break;
                                    }
                                    else // Special Case if Justified is used in Line Mode.
                                    {
                                        textElementInfos[i].vertexBottomLeft.uv2.x = (textElementInfos[i].vertexBottomLeft.position.x + justificationOffset.x - m_MeshExtents.min.x) / (m_MeshExtents.max.x - m_MeshExtents.min.x) + uvOffset;
                                        textElementInfos[i].vertexTopLeft.uv2.x = (textElementInfos[i].vertexTopLeft.position.x + justificationOffset.x - m_MeshExtents.min.x) / (m_MeshExtents.max.x - m_MeshExtents.min.x) + uvOffset;
                                        textElementInfos[i].vertexTopRight.uv2.x = (textElementInfos[i].vertexTopRight.position.x + justificationOffset.x - m_MeshExtents.min.x) / (m_MeshExtents.max.x - m_MeshExtents.min.x) + uvOffset;
                                        textElementInfos[i].vertexBottomRight.uv2.x = (textElementInfos[i].vertexBottomRight.position.x + justificationOffset.x - m_MeshExtents.min.x) / (m_MeshExtents.max.x - m_MeshExtents.min.x) + uvOffset;
                                        break;
                                    }

                                case TextureMapping.Paragraph:
                                    textElementInfos[i].vertexBottomLeft.uv2.x = (textElementInfos[i].vertexBottomLeft.position.x + justificationOffset.x - m_MeshExtents.min.x) / (m_MeshExtents.max.x - m_MeshExtents.min.x) + uvOffset;
                                    textElementInfos[i].vertexTopLeft.uv2.x = (textElementInfos[i].vertexTopLeft.position.x + justificationOffset.x - m_MeshExtents.min.x) / (m_MeshExtents.max.x - m_MeshExtents.min.x) + uvOffset;
                                    textElementInfos[i].vertexTopRight.uv2.x = (textElementInfos[i].vertexTopRight.position.x + justificationOffset.x - m_MeshExtents.min.x) / (m_MeshExtents.max.x - m_MeshExtents.min.x) + uvOffset;
                                    textElementInfos[i].vertexBottomRight.uv2.x = (textElementInfos[i].vertexBottomRight.position.x + justificationOffset.x - m_MeshExtents.min.x) / (m_MeshExtents.max.x - m_MeshExtents.min.x) + uvOffset;
                                    break;

                                case TextureMapping.MatchAspect:

                                    switch (generationSettings.verticalMapping)
                                    {
                                        case TextureMapping.Character:
                                            textElementInfos[i].vertexBottomLeft.uv2.y = 0; // + m_uvOffset.y;
                                            textElementInfos[i].vertexTopLeft.uv2.y = 1; // + m_uvOffset.y;
                                            textElementInfos[i].vertexTopRight.uv2.y = 0; // + m_uvOffset.y;
                                            textElementInfos[i].vertexBottomRight.uv2.y = 1; // + m_uvOffset.y;
                                            break;

                                        case TextureMapping.Line:
                                            textElementInfos[i].vertexBottomLeft.uv2.y = (textElementInfos[i].vertexBottomLeft.position.y - lineExtents.min.y) / (lineExtents.max.y - lineExtents.min.y) + uvOffset;
                                            textElementInfos[i].vertexTopLeft.uv2.y = (textElementInfos[i].vertexTopLeft.position.y - lineExtents.min.y) / (lineExtents.max.y - lineExtents.min.y) + uvOffset;
                                            textElementInfos[i].vertexTopRight.uv2.y = textElementInfos[i].vertexBottomLeft.uv2.y;
                                            textElementInfos[i].vertexBottomRight.uv2.y = textElementInfos[i].vertexTopLeft.uv2.y;
                                            break;

                                        case TextureMapping.Paragraph:
                                            textElementInfos[i].vertexBottomLeft.uv2.y = (textElementInfos[i].vertexBottomLeft.position.y - m_MeshExtents.min.y) / (m_MeshExtents.max.y - m_MeshExtents.min.y) + uvOffset;
                                            textElementInfos[i].vertexTopLeft.uv2.y = (textElementInfos[i].vertexTopLeft.position.y - m_MeshExtents.min.y) / (m_MeshExtents.max.y - m_MeshExtents.min.y) + uvOffset;
                                            textElementInfos[i].vertexTopRight.uv2.y = textElementInfos[i].vertexBottomLeft.uv2.y;
                                            textElementInfos[i].vertexBottomRight.uv2.y = textElementInfos[i].vertexTopLeft.uv2.y;
                                            break;

                                        case TextureMapping.MatchAspect:
                                            Debug.Log("ERROR: Cannot Match both Vertical & Horizontal.");
                                            break;
                                    }

                                    //float xDelta = 1 - (_uv2s[vert_index + 0].y * textMeshCharacterInfo[i].AspectRatio); // Left aligned
                                    float xDelta = (1 - ((textElementInfos[i].vertexBottomLeft.uv2.y + textElementInfos[i].vertexTopLeft.uv2.y) * textElementInfos[i].aspectRatio)) / 2; // Center of Rectangle

                                    textElementInfos[i].vertexBottomLeft.uv2.x = (textElementInfos[i].vertexBottomLeft.uv2.y * textElementInfos[i].aspectRatio) + xDelta + uvOffset;
                                    textElementInfos[i].vertexTopLeft.uv2.x = textElementInfos[i].vertexBottomLeft.uv2.x;
                                    textElementInfos[i].vertexTopRight.uv2.x = (textElementInfos[i].vertexTopLeft.uv2.y * textElementInfos[i].aspectRatio) + xDelta + uvOffset;
                                    textElementInfos[i].vertexBottomRight.uv2.x = textElementInfos[i].vertexTopRight.uv2.x;
                                    break;
                            }

                            switch (generationSettings.verticalMapping)
                            {
                                case TextureMapping.Character:
                                    textElementInfos[i].vertexBottomLeft.uv2.y = 0; // + m_uvOffset.y;
                                    textElementInfos[i].vertexTopLeft.uv2.y = 1; // + m_uvOffset.y;
                                    textElementInfos[i].vertexTopRight.uv2.y = 1; // + m_uvOffset.y;
                                    textElementInfos[i].vertexBottomRight.uv2.y = 0; // + m_uvOffset.y;
                                    break;

                                case TextureMapping.Line:
                                    textElementInfos[i].vertexBottomLeft.uv2.y = (textElementInfos[i].vertexBottomLeft.position.y - lineInfo.descender) / (lineInfo.ascender - lineInfo.descender); // + m_uvOffset.y;
                                    textElementInfos[i].vertexTopLeft.uv2.y = (textElementInfos[i].vertexTopLeft.position.y - lineInfo.descender) / (lineInfo.ascender - lineInfo.descender); // + m_uvOffset.y;
                                    textElementInfos[i].vertexTopRight.uv2.y = textElementInfos[i].vertexTopLeft.uv2.y;
                                    textElementInfos[i].vertexBottomRight.uv2.y = textElementInfos[i].vertexBottomLeft.uv2.y;
                                    break;

                                case TextureMapping.Paragraph:
                                    textElementInfos[i].vertexBottomLeft.uv2.y = (textElementInfos[i].vertexBottomLeft.position.y - m_MeshExtents.min.y) / (m_MeshExtents.max.y - m_MeshExtents.min.y); // + m_uvOffset.y;
                                    textElementInfos[i].vertexTopLeft.uv2.y = (textElementInfos[i].vertexTopLeft.position.y - m_MeshExtents.min.y) / (m_MeshExtents.max.y - m_MeshExtents.min.y); // + m_uvOffset.y;
                                    textElementInfos[i].vertexTopRight.uv2.y = textElementInfos[i].vertexTopLeft.uv2.y;
                                    textElementInfos[i].vertexBottomRight.uv2.y = textElementInfos[i].vertexBottomLeft.uv2.y;
                                    break;

                                case TextureMapping.MatchAspect:
                                    float yDelta = (1 - ((textElementInfos[i].vertexBottomLeft.uv2.x + textElementInfos[i].vertexTopRight.uv2.x) / textElementInfos[i].aspectRatio)) / 2; // Center of Rectangle

                                    textElementInfos[i].vertexBottomLeft.uv2.y = yDelta + (textElementInfos[i].vertexBottomLeft.uv2.x / textElementInfos[i].aspectRatio); // + m_uvOffset.y;
                                    textElementInfos[i].vertexTopLeft.uv2.y = yDelta + (textElementInfos[i].vertexTopRight.uv2.x / textElementInfos[i].aspectRatio); // + m_uvOffset.y;
                                    textElementInfos[i].vertexBottomRight.uv2.y = textElementInfos[i].vertexBottomLeft.uv2.y;
                                    textElementInfos[i].vertexTopRight.uv2.y = textElementInfos[i].vertexTopLeft.uv2.y;
                                    break;
                            }
                            #endregion

                            // Pack UV's so that we can pass Xscale needed for Shader to maintain 1:1 ratio.
                            #region Pack Scale into UV2
                            xScale = textElementInfos[i].scale * (1 - m_CharWidthAdjDelta) * 1; // generationSettings.scale;
                            if (!textElementInfos[i].isUsingAlternateTypeface && (textElementInfos[i].style & FontStyles.Bold) == FontStyles.Bold) xScale *= -1;

                            // Set SDF Scale
                            textElementInfos[i].vertexBottomLeft.uv.w = xScale;
                            textElementInfos[i].vertexTopLeft.uv.w = xScale;
                            textElementInfos[i].vertexTopRight.uv.w = xScale;
                            textElementInfos[i].vertexBottomRight.uv.w = xScale;

                            // TODO: To revise the code below. Right now, it is required by UITK to handle bold styling, while in TMP is not necessary.
                            // Optimization to avoid having a vector2 returned from the Pack UV function.
                            textElementInfos[i].vertexBottomLeft.uv2.x = 1;
                            textElementInfos[i].vertexBottomLeft.uv2.y = xScale;
                            textElementInfos[i].vertexTopLeft.uv2.x = 1;
                            textElementInfos[i].vertexTopLeft.uv2.y = xScale;
                            textElementInfos[i].vertexTopRight.uv2.x = 1;
                            textElementInfos[i].vertexTopRight.uv2.y = xScale;
                            textElementInfos[i].vertexBottomRight.uv2.x = 1;
                            textElementInfos[i].vertexBottomRight.uv2.y = xScale;
                            #endregion
                            break;

                        // SPRITES
                        case TextElementType.Sprite:
                            // Nothing right now
                            break;
                    }

                    // Handle maxVisibleCharacters, maxVisibleLines and Overflow Page Mode.
                    #region Handle maxVisibleCharacters / maxVisibleLines / Page Mode
                    if (i < generationSettings.maxVisibleCharacters && wordCount < generationSettings.maxVisibleWords && currentLine < generationSettings.maxVisibleLines && generationSettings.overflowMode != TextOverflowMode.Page)
                    {
                        textElementInfos[i].vertexBottomLeft.position += offset;
                        textElementInfos[i].vertexTopLeft.position += offset;
                        textElementInfos[i].vertexTopRight.position += offset;
                        textElementInfos[i].vertexBottomRight.position += offset;
                    }
                    else if (i < generationSettings.maxVisibleCharacters && wordCount < generationSettings.maxVisibleWords && currentLine < generationSettings.maxVisibleLines && generationSettings.overflowMode == TextOverflowMode.Page && textElementInfos[i].pageNumber == pageToDisplay)
                    {
                        textElementInfos[i].vertexBottomLeft.position += offset;
                        textElementInfos[i].vertexTopLeft.position += offset;
                        textElementInfos[i].vertexTopRight.position += offset;
                        textElementInfos[i].vertexBottomRight.position += offset;
                    }
                    else
                    {
                        textElementInfos[i].vertexBottomLeft.position = Vector3.zero;
                        textElementInfos[i].vertexTopLeft.position = Vector3.zero;
                        textElementInfos[i].vertexTopRight.position = Vector3.zero;
                        textElementInfos[i].vertexBottomRight.position = Vector3.zero;
                        textElementInfos[i].isVisible = false;
                    }
                    #endregion

                    bool convertToLinearSpace = QualitySettings.activeColorSpace == ColorSpace.Linear
                                                && generationSettings.shouldConvertToLinearSpace;

                    // Fill Vertex Buffers for the various types of element
                    if (elementType == TextElementType.Character)
                    {
                        TextGeneratorUtilities.FillCharacterVertexBuffers(i, convertToLinearSpace, generationSettings, textInfo);
                    }
                    else if (elementType == TextElementType.Sprite)
                    {
                        TextGeneratorUtilities.FillSpriteVertexBuffers(i, convertToLinearSpace, generationSettings, textInfo);
                    }
                }
                #endregion

                // Apply Alignment and Justification Offset
                textInfo.textElementInfo[i].bottomLeft += offset;
                textInfo.textElementInfo[i].topLeft += offset;
                textInfo.textElementInfo[i].topRight += offset;
                textInfo.textElementInfo[i].bottomRight += offset;

                textInfo.textElementInfo[i].origin += offset.x;
                textInfo.textElementInfo[i].xAdvance += offset.x;

                textInfo.textElementInfo[i].ascender += offset.y;
                textInfo.textElementInfo[i].descender += offset.y;
                textInfo.textElementInfo[i].baseLine += offset.y;

                // Update MeshExtents
                if (isCharacterVisible)
                {
                    //m_MeshExtents.min = new Vector2(Mathf.Min(m_MeshExtents.min.x, textInfo.textElementInfo[i].bottomLeft.x), Mathf.Min(m_MeshExtents.min.y, textInfo.textElementInfo[i].bottomLeft.y));
                    //m_MeshExtents.max = new Vector2(Mathf.Max(m_MeshExtents.max.x, textInfo.textElementInfo[i].topRight.x), Mathf.Max(m_MeshExtents.max.y, textInfo.textElementInfo[i].topLeft.y));
                }

                // Need to recompute lineExtent to account for the offset from justification.
                #region Adjust lineExtents resulting from alignment offset
                if (currentLine != lastLine || i == m_CharacterCount - 1)
                {
                    // Update the previous line's extents
                    if (currentLine != lastLine)
                    {
                        textInfo.lineInfo[lastLine].baseline += offset.y;
                        textInfo.lineInfo[lastLine].ascender += offset.y;
                        textInfo.lineInfo[lastLine].descender += offset.y;

                        textInfo.lineInfo[lastLine].maxAdvance += offset.x;

                        textInfo.lineInfo[lastLine].lineExtents.min = new Vector2(textInfo.textElementInfo[textInfo.lineInfo[lastLine].firstCharacterIndex].bottomLeft.x, textInfo.lineInfo[lastLine].descender);
                        textInfo.lineInfo[lastLine].lineExtents.max = new Vector2(textInfo.textElementInfo[textInfo.lineInfo[lastLine].lastVisibleCharacterIndex].topRight.x, textInfo.lineInfo[lastLine].ascender);
                    }

                    // Update the current line's extents
                    if (i == m_CharacterCount - 1)
                    {
                        textInfo.lineInfo[currentLine].baseline += offset.y;
                        textInfo.lineInfo[currentLine].ascender += offset.y;
                        textInfo.lineInfo[currentLine].descender += offset.y;

                        textInfo.lineInfo[currentLine].maxAdvance += offset.x;

                        textInfo.lineInfo[currentLine].lineExtents.min = new Vector2(textInfo.textElementInfo[textInfo.lineInfo[currentLine].firstCharacterIndex].bottomLeft.x, textInfo.lineInfo[currentLine].descender);
                        textInfo.lineInfo[currentLine].lineExtents.max = new Vector2(textInfo.textElementInfo[textInfo.lineInfo[currentLine].lastVisibleCharacterIndex].topRight.x, textInfo.lineInfo[currentLine].ascender);
                    }
                }
                #endregion


                // Track Word Count per line and for the object
                #region Track Word Count
                if (char.IsLetterOrDigit(unicode) || unicode == 0x2D || unicode == 0xAD || unicode == k_Hyphen || unicode == k_NonBreakingHyphen)
                {
                    if (isStartOfWord == false)
                    {
                        isStartOfWord = true;
                        wordFirstChar = i;
                    }

                    // If last character is a word
                    if (isStartOfWord && i == m_CharacterCount - 1)
                    {
                        int size = textInfo.wordInfo.Length;
                        int index = textInfo.wordCount;

                        if (textInfo.wordCount + 1 > size)
                            TextInfo.Resize(ref textInfo.wordInfo, size + 1);

                        wordLastChar = i;

                        textInfo.wordInfo[index].firstCharacterIndex = wordFirstChar;
                        textInfo.wordInfo[index].lastCharacterIndex = wordLastChar;
                        textInfo.wordInfo[index].characterCount = wordLastChar - wordFirstChar + 1;

                        wordCount += 1;
                        textInfo.wordCount += 1;
                        textInfo.lineInfo[currentLine].wordCount += 1;
                    }
                }
                else if (isStartOfWord || i == 0 && (!char.IsPunctuation(unicode) || isWhiteSpace || unicode == k_ZeroWidthSpace || i == m_CharacterCount - 1))
                {
                    if (i > 0 && i < textElementInfos.Length - 1 && i < m_CharacterCount && (unicode == k_SingleQuote || unicode == k_RightSingleQuote) && char.IsLetterOrDigit(textElementInfos[i - 1].character) && char.IsLetterOrDigit(textElementInfos[i + 1].character))
                    {

                    }
                    else
                    {
                        wordLastChar = i == m_CharacterCount - 1 && char.IsLetterOrDigit(unicode) ? i : i - 1;
                        isStartOfWord = false;

                        int size = textInfo.wordInfo.Length;
                        int index = textInfo.wordCount;

                        if (textInfo.wordCount + 1 > size)
                            TextInfo.Resize(ref textInfo.wordInfo, size + 1);

                        textInfo.wordInfo[index].firstCharacterIndex = wordFirstChar;
                        textInfo.wordInfo[index].lastCharacterIndex = wordLastChar;
                        textInfo.wordInfo[index].characterCount = wordLastChar - wordFirstChar + 1;

                        wordCount += 1;
                        textInfo.wordCount += 1;
                        textInfo.lineInfo[currentLine].wordCount += 1;
                    }
                }
                #endregion


                // Setup & Handle Underline
                #region Underline
                // NOTE: Need to figure out how underline will be handled with multiple fonts and which font will be used for the underline.
                bool isUnderline = (textInfo.textElementInfo[i].style & FontStyles.Underline) == FontStyles.Underline;
                if (isUnderline)
                {
                    bool isUnderlineVisible = true;
                    int currentPage = textInfo.textElementInfo[i].pageNumber;
                    textInfo.textElementInfo[i].underlineVertexIndex = m_MaterialReferences[m_Underline.materialIndex].referenceCount * 4;

                    if (i > generationSettings.maxVisibleCharacters || currentLine > generationSettings.maxVisibleLines || (generationSettings.overflowMode == TextOverflowMode.Page && currentPage + 1 != generationSettings.pageToDisplay))
                        isUnderlineVisible = false;

                    // We only use the scale of visible characters.
                    if (!isWhiteSpace && unicode != k_ZeroWidthSpace)
                    {
                        underlineMaxScale = Mathf.Max(underlineMaxScale, textInfo.textElementInfo[i].scale);
                        xScaleMax = Mathf.Max(xScaleMax, Mathf.Abs(xScale));
                        underlineBaseLine = Mathf.Min(currentPage == lastPage ? underlineBaseLine : TextGeneratorUtilities.largePositiveFloat, textInfo.textElementInfo[i].baseLine + currentFontAsset.faceInfo.underlineOffset * underlineMaxScale);
                        lastPage = currentPage; // Need to track pages to ensure we reset baseline for the new pages.
                    }

                    if (beginUnderline == false && isUnderlineVisible == true && i <= lineInfo.lastVisibleCharacterIndex && unicode != 10 && unicode != 11 && unicode != 13)
                    {
                        if (i == lineInfo.lastVisibleCharacterIndex && char.IsSeparator(unicode))
                        { }
                        else
                        {
                            beginUnderline = true;
                            underlineStartScale = textInfo.textElementInfo[i].scale;
                            if (underlineMaxScale == 0)
                            {
                                underlineMaxScale = underlineStartScale;
                                xScaleMax = xScale;
                            }
                            underlineStart = new Vector3(textInfo.textElementInfo[i].bottomLeft.x, underlineBaseLine, 0);
                            underlineColor = textInfo.textElementInfo[i].underlineColor;
                        }
                    }

                    // End Underline if text only contains one character.
                    if (beginUnderline && m_CharacterCount == 1)
                    {
                        beginUnderline = false;
                        underlineEnd = new Vector3(textInfo.textElementInfo[i].topRight.x, underlineBaseLine, 0);
                        underlineEndScale = textInfo.textElementInfo[i].scale;

                        DrawUnderlineMesh(underlineStart, underlineEnd, underlineStartScale, underlineEndScale, underlineMaxScale, xScaleMax, underlineColor, generationSettings, textInfo);
                        underlineMaxScale = 0;
                        xScaleMax = 0;
                        underlineBaseLine = TextGeneratorUtilities.largePositiveFloat;
                    }
                    else if (beginUnderline && (i == lineInfo.lastCharacterIndex || i >= lineInfo.lastVisibleCharacterIndex))
                    {
                        // Terminate underline at previous visible character if space or carriage return.
                        if (isWhiteSpace || unicode == k_ZeroWidthSpace)
                        {
                            int lastVisibleCharacterIndex = lineInfo.lastVisibleCharacterIndex;
                            underlineEnd = new Vector3(textInfo.textElementInfo[lastVisibleCharacterIndex].topRight.x, underlineBaseLine, 0);
                            underlineEndScale = textInfo.textElementInfo[lastVisibleCharacterIndex].scale;
                        }
                        else
                        {   // End underline if last character of the line.
                            underlineEnd = new Vector3(textInfo.textElementInfo[i].topRight.x, underlineBaseLine, 0);
                            underlineEndScale = textInfo.textElementInfo[i].scale;
                        }

                        beginUnderline = false;
                        DrawUnderlineMesh(underlineStart, underlineEnd, underlineStartScale, underlineEndScale, underlineMaxScale, xScaleMax, underlineColor, generationSettings, textInfo);
                        underlineMaxScale = 0;
                        xScaleMax = 0;
                        underlineBaseLine = TextGeneratorUtilities.largePositiveFloat;
                    }
                    else if (beginUnderline && !isUnderlineVisible)
                    {
                        beginUnderline = false;
                        underlineEnd = new Vector3(textInfo.textElementInfo[i - 1].topRight.x, underlineBaseLine, 0);
                        underlineEndScale = textInfo.textElementInfo[i - 1].scale;

                        DrawUnderlineMesh(underlineStart, underlineEnd, underlineStartScale, underlineEndScale, underlineMaxScale, xScaleMax, underlineColor, generationSettings, textInfo);
                        underlineMaxScale = 0;
                        xScaleMax = 0;
                        underlineBaseLine = TextGeneratorUtilities.largePositiveFloat;
                    }
                    else if (beginUnderline && i < m_CharacterCount - 1 && !ColorUtilities.CompareColors(underlineColor, textInfo.textElementInfo[i + 1].underlineColor))
                    {
                        // End underline if underline color has changed.
                        beginUnderline = false;
                        underlineEnd = new Vector3(textInfo.textElementInfo[i].topRight.x, underlineBaseLine, 0);
                        underlineEndScale = textInfo.textElementInfo[i].scale;

                        DrawUnderlineMesh(underlineStart, underlineEnd, underlineStartScale, underlineEndScale, underlineMaxScale, xScaleMax, underlineColor, generationSettings, textInfo);
                        underlineMaxScale = 0;
                        xScaleMax = 0;
                        underlineBaseLine = TextGeneratorUtilities.largePositiveFloat;
                    }
                }
                else
                {
                    // End Underline
                    if (beginUnderline == true)
                    {
                        beginUnderline = false;
                        underlineEnd = new Vector3(textInfo.textElementInfo[i - 1].topRight.x, underlineBaseLine, 0);
                        underlineEndScale = textInfo.textElementInfo[i - 1].scale;

                        DrawUnderlineMesh(underlineStart, underlineEnd, underlineStartScale, underlineEndScale, underlineMaxScale, xScaleMax, underlineColor, generationSettings, textInfo);
                        underlineMaxScale = 0;
                        xScaleMax = 0;
                        underlineBaseLine = TextGeneratorUtilities.largePositiveFloat;
                    }
                }
                #endregion


                // Setup & Handle Strikethrough
                #region Strikethrough
                // NOTE: Need to figure out how underline will be handled with multiple fonts and which font will be used for the underline.
                bool isStrikethrough = (textInfo.textElementInfo[i].style & FontStyles.Strikethrough) == FontStyles.Strikethrough;
                float strikethroughOffset = currentFontAsset.faceInfo.strikethroughOffset;

                if (isStrikethrough)
                {
                    bool isStrikeThroughVisible = true;
                    textInfo.textElementInfo[i].strikethroughVertexIndex = m_MaterialReferences[m_Underline.materialIndex].referenceCount * 4;

                    if (i > generationSettings.maxVisibleCharacters || currentLine > generationSettings.maxVisibleLines || (generationSettings.overflowMode == TextOverflowMode.Page && textInfo.textElementInfo[i].pageNumber + 1 != generationSettings.pageToDisplay))
                        isStrikeThroughVisible = false;

                    if (beginStrikethrough == false && isStrikeThroughVisible && i <= lineInfo.lastVisibleCharacterIndex && unicode != 10 && unicode != 11 && unicode != 13)
                    {
                        if (i == lineInfo.lastVisibleCharacterIndex && char.IsSeparator(unicode))
                        { }
                        else
                        {
                            beginStrikethrough = true;
                            strikethroughPointSize = textInfo.textElementInfo[i].pointSize;
                            strikethroughScale = textInfo.textElementInfo[i].scale;
                            strikethroughStart = new Vector3(textInfo.textElementInfo[i].bottomLeft.x, textInfo.textElementInfo[i].baseLine + strikethroughOffset * strikethroughScale, 0);
                            strikethroughColor = textInfo.textElementInfo[i].strikethroughColor;
                            strikethroughBaseline = textInfo.textElementInfo[i].baseLine;
                        }
                    }

                    // End Strikethrough if text only contains one character.
                    if (beginStrikethrough && m_CharacterCount == 1)
                    {
                        beginStrikethrough = false;
                        strikethroughEnd = new Vector3(textInfo.textElementInfo[i].topRight.x, textInfo.textElementInfo[i].baseLine + strikethroughOffset * strikethroughScale, 0);

                        DrawUnderlineMesh(strikethroughStart, strikethroughEnd, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor, generationSettings, textInfo);
                    }
                    else if (beginStrikethrough && i == lineInfo.lastCharacterIndex)
                    {
                        // Terminate Strikethrough at previous visible character if space or carriage return.
                        if (isWhiteSpace || unicode == k_ZeroWidthSpace)
                        {
                            int lastVisibleCharacterIndex = lineInfo.lastVisibleCharacterIndex;
                            strikethroughEnd = new Vector3(textInfo.textElementInfo[lastVisibleCharacterIndex].topRight.x, textInfo.textElementInfo[lastVisibleCharacterIndex].baseLine + strikethroughOffset * strikethroughScale, 0);
                        }
                        else
                        {
                            // Terminate Strikethrough at last character of line.
                            strikethroughEnd = new Vector3(textInfo.textElementInfo[i].topRight.x, textInfo.textElementInfo[i].baseLine + strikethroughOffset * strikethroughScale, 0);
                        }

                        beginStrikethrough = false;
                        DrawUnderlineMesh(strikethroughStart, strikethroughEnd, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor, generationSettings, textInfo);
                    }
                    else if (beginStrikethrough && i < m_CharacterCount && (textInfo.textElementInfo[i + 1].pointSize != strikethroughPointSize || !TextGeneratorUtilities.Approximately(textInfo.textElementInfo[i + 1].baseLine + offset.y, strikethroughBaseline)))
                    {
                        // Terminate Strikethrough if scale changes.
                        beginStrikethrough = false;

                        int lastVisibleCharacterIndex = lineInfo.lastVisibleCharacterIndex;
                        if (i > lastVisibleCharacterIndex)
                            strikethroughEnd = new Vector3(textInfo.textElementInfo[lastVisibleCharacterIndex].topRight.x, textInfo.textElementInfo[lastVisibleCharacterIndex].baseLine + strikethroughOffset * strikethroughScale, 0);
                        else
                            strikethroughEnd = new Vector3(textInfo.textElementInfo[i].topRight.x, textInfo.textElementInfo[i].baseLine + strikethroughOffset * strikethroughScale, 0);

                        DrawUnderlineMesh(strikethroughStart, strikethroughEnd, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor, generationSettings, textInfo);
                    }
                    else if (beginStrikethrough && i < m_CharacterCount && currentFontAsset.GetInstanceID() != textElementInfos[i + 1].fontAsset.GetInstanceID())
                    {
                        // Terminate Strikethrough if font asset changes.
                        beginStrikethrough = false;
                        strikethroughEnd = new Vector3(textInfo.textElementInfo[i].topRight.x, textInfo.textElementInfo[i].baseLine + strikethroughOffset * strikethroughScale, 0);

                        DrawUnderlineMesh(strikethroughStart, strikethroughEnd, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor, generationSettings, textInfo);
                    }
                    else if (beginStrikethrough && !isStrikeThroughVisible)
                    {
                        // Terminate Strikethrough if character is not visible.
                        beginStrikethrough = false;
                        strikethroughEnd = new Vector3(textInfo.textElementInfo[i - 1].topRight.x, textInfo.textElementInfo[i - 1].baseLine + strikethroughOffset * strikethroughScale, 0);

                        DrawUnderlineMesh(strikethroughStart, strikethroughEnd, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor, generationSettings, textInfo);
                    }
                }
                else
                {
                    // End Strikethrough
                    if (beginStrikethrough == true)
                    {
                        beginStrikethrough = false;
                        strikethroughEnd = new Vector3(textInfo.textElementInfo[i - 1].topRight.x, textInfo.textElementInfo[i - 1].baseLine + strikethroughOffset * strikethroughScale, 0);

                        DrawUnderlineMesh(strikethroughStart, strikethroughEnd, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor, generationSettings, textInfo);
                    }
                }
                #endregion


                // HANDLE TEXT HIGHLIGHTING
                #region Text Highlighting
                bool isHighlight = (textInfo.textElementInfo[i].style & FontStyles.Highlight) == FontStyles.Highlight;
                if (isHighlight)
                {
                    bool isHighlightVisible = true;
                    int currentPage = textInfo.textElementInfo[i].pageNumber;

                    if (i > generationSettings.maxVisibleCharacters || currentLine > generationSettings.maxVisibleLines || (generationSettings.overflowMode == TextOverflowMode.Page && currentPage + 1 != generationSettings.pageToDisplay))
                        isHighlightVisible = false;

                    if (beginHighlight == false && isHighlightVisible == true && i <= lineInfo.lastVisibleCharacterIndex && unicode != 10 && unicode != 11 && unicode != 13)
                    {
                        if (i == lineInfo.lastVisibleCharacterIndex && char.IsSeparator(unicode))
                        { }
                        else
                        {
                            beginHighlight = true;
                            highlightStart = TextGeneratorUtilities.largePositiveVector2;
                            highlightEnd = TextGeneratorUtilities.largeNegativeVector2;
                            highlightState = textInfo.textElementInfo[i].highlightState;
                        }
                    }

                    if (beginHighlight)
                    {
                        TextElementInfo currentCharacter = textInfo.textElementInfo[i];
                        HighlightState currentState = currentCharacter.highlightState;

                        bool isColorTransition = false;

                        // Handle Highlight color changes
                        if (highlightState != currentState)
                        {
                            // Adjust previous highlight section to prevent a gaps between sections.
                            if (isWhiteSpace)
                                highlightEnd.x = (highlightEnd.x - highlightState.padding.right + currentCharacter.origin) / 2;
                            else
                                highlightEnd.x = (highlightEnd.x - highlightState.padding.right + currentCharacter.bottomLeft.x) / 2;

                            highlightStart.y = Mathf.Min(highlightStart.y, currentCharacter.descender);
                            highlightEnd.y = Mathf.Max(highlightEnd.y, currentCharacter.ascender);

                            DrawTextHighlight(highlightStart, highlightEnd, highlightState.color, generationSettings, textInfo);

                            beginHighlight = true;
                            highlightStart = new Vector2(highlightEnd.x, currentCharacter.descender - currentState.padding.bottom);

                            if (isWhiteSpace)
                                highlightEnd = new Vector2(currentCharacter.xAdvance + currentState.padding.right, currentCharacter.ascender + currentState.padding.top);
                            else
                                highlightEnd = new Vector2(currentCharacter.topRight.x + currentState.padding.right, currentCharacter.ascender + currentState.padding.top);

                            highlightState = currentState;

                            isColorTransition = true;
                        }

                        if (!isColorTransition)
                        {
                            if (isWhiteSpace)
                            {
                                // Use the Min / Max of glyph metrics if white space.
                                highlightStart.x = Mathf.Min(highlightStart.x, currentCharacter.origin - highlightState.padding.left);
                                highlightEnd.x = Mathf.Max(highlightEnd.x, currentCharacter.xAdvance + highlightState.padding.right);
                            }
                            else
                            {
                                // Use the Min / Max of character bounds
                                highlightStart.x = Mathf.Min(highlightStart.x, currentCharacter.bottomLeft.x - highlightState.padding.left);
                                highlightEnd.x = Mathf.Max(highlightEnd.x, currentCharacter.topRight.x + highlightState.padding.right);
                            }

                            highlightStart.y = Mathf.Min(highlightStart.y, currentCharacter.descender - highlightState.padding.bottom);
                            highlightEnd.y = Mathf.Max(highlightEnd.y, currentCharacter.ascender + highlightState.padding.top);
                        }
                    }

                    // End Highlight if text only contains one character.
                    if (beginHighlight && m_CharacterCount == 1)
                    {
                        beginHighlight = false;

                        DrawTextHighlight(highlightStart, highlightEnd, highlightState.color, generationSettings, textInfo);
                    }
                    else if (beginHighlight && (i == lineInfo.lastCharacterIndex || i >= lineInfo.lastVisibleCharacterIndex))
                    {
                        beginHighlight = false;
                        DrawTextHighlight(highlightStart, highlightEnd, highlightState.color, generationSettings, textInfo);
                    }
                    else if (beginHighlight && !isHighlightVisible)
                    {
                        beginHighlight = false;
                        DrawTextHighlight(highlightStart, highlightEnd, highlightState.color, generationSettings, textInfo);
                    }
                }
                else
                {
                    // End Highlight
                    if (beginHighlight == true)
                    {
                        beginHighlight = false;
                        DrawTextHighlight(highlightStart, highlightEnd, highlightState.color, generationSettings, textInfo);
                    }
                }
                #endregion

                lastLine = currentLine;
            }
            #endregion

            // METRICS ABOUT THE TEXT OBJECT
            textInfo.characterCount = m_CharacterCount;
            textInfo.spriteCount = m_SpriteCount;
            textInfo.lineCount = lineCount;
            textInfo.wordCount = wordCount != 0 && m_CharacterCount > 0 ? wordCount : 1;
            textInfo.pageCount = m_PageNumber + 1;

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

        void SaveWordWrappingState(ref WordWrapState state, int index, int count, TextInfo textInfo)
        {
            // Multi Font & Material support related
            state.currentFontAsset = m_CurrentFontAsset;
            state.currentSpriteAsset = m_CurrentSpriteAsset;
            state.currentMaterial = m_CurrentMaterial;
            state.currentMaterialIndex = m_CurrentMaterialIndex;

            state.previousWordBreak = index;
            state.totalCharacterCount = count;
            state.visibleCharacterCount = m_LineVisibleCharacterCount;
            state.visibleSpaceCount = m_LineVisibleSpaceCount;
            state.visibleLinkCount = textInfo.linkCount;

            state.firstCharacterIndex = m_FirstCharacterOfLine;
            state.firstVisibleCharacterIndex = m_FirstVisibleCharacterOfLine;
            state.lastVisibleCharIndex = m_LastVisibleCharacterOfLine;

            state.fontStyle = m_FontStyleInternal;
            state.italicAngle = m_ItalicAngle;

            state.fontScaleMultiplier = m_FontScaleMultiplier;
            state.currentFontSize = m_CurrentFontSize;

            state.xAdvance = m_XAdvance;
            state.maxCapHeight = m_MaxCapHeight;
            state.maxAscender = m_MaxAscender;
            state.maxDescender = m_MaxDescender;
            state.maxLineAscender = m_MaxLineAscender;
            state.maxLineDescender = m_MaxLineDescender;
            state.startOfLineAscender = m_StartOfLineAscender;
            state.preferredWidth = m_PreferredWidth;
            state.preferredHeight = m_PreferredHeight;
            state.meshExtents = m_MeshExtents;
            state.pageAscender = m_PageAscender;

            state.lineNumber = m_LineNumber;
            state.lineOffset = m_LineOffset;
            state.baselineOffset = m_BaselineOffset;
            state.isDrivenLineSpacing = m_IsDrivenLineSpacing;

            state.vertexColor = m_HtmlColor;
            state.underlineColor = m_UnderlineColor;
            state.strikethroughColor = m_StrikethroughColor;
            state.highlightColor = m_HighlightColor;
            state.highlightState = m_HighlightState;

            state.isNonBreakingSpace = m_IsNonBreakingSpace;
            state.tagNoParsing = m_TagNoParsing;
            state.fxScale = m_FXScale;
            state.fxRotation = m_FXRotation;

            // XML Tag Stack
            state.basicStyleStack = m_FontStyleStack;
            state.italicAngleStack = m_ItalicAngleStack;
            state.colorStack = m_ColorStack;
            state.underlineColorStack = m_UnderlineColorStack;
            state.strikethroughColorStack = m_StrikethroughColorStack;
            state.highlightColorStack = m_HighlightColorStack;
            state.colorGradientStack = m_ColorGradientStack;
            state.highlightStateStack = m_HighlightStateStack;
            state.sizeStack = m_SizeStack;
            state.indentStack = m_IndentStack;
            state.fontWeightStack = m_FontWeightStack;
            state.styleStack = m_StyleStack;
            state.baselineStack = m_BaselineOffsetStack;
            state.actionStack = m_ActionStack;
            state.materialReferenceStack = m_MaterialReferenceStack;
            state.lineJustificationStack = m_LineJustificationStack;

            state.lastBaseGlyphIndex = m_LastBaseGlyphIndex;
            state.spriteAnimationId = m_SpriteAnimationId;

            if (m_LineNumber < textInfo.lineInfo.Length)
                state.lineInfo = textInfo.lineInfo[m_LineNumber];
        }

        protected int RestoreWordWrappingState(ref WordWrapState state, TextInfo textInfo)
        {
            int index = state.previousWordBreak;

            // Multi Font & Material support related
            m_CurrentFontAsset = state.currentFontAsset;
            m_CurrentSpriteAsset = state.currentSpriteAsset;
            m_CurrentMaterial = state.currentMaterial;
            m_CurrentMaterialIndex = state.currentMaterialIndex;

            m_CharacterCount = state.totalCharacterCount + 1;
            m_LineVisibleCharacterCount = state.visibleCharacterCount;
            m_LineVisibleSpaceCount = state.visibleSpaceCount;

            textInfo.linkCount = state.visibleLinkCount;

            m_FirstCharacterOfLine = state.firstCharacterIndex;
            m_FirstVisibleCharacterOfLine = state.firstVisibleCharacterIndex;
            m_LastVisibleCharacterOfLine = state.lastVisibleCharIndex;

            m_FontStyleInternal = state.fontStyle;
            m_ItalicAngle = state.italicAngle;
            m_FontScaleMultiplier = state.fontScaleMultiplier;

            m_CurrentFontSize = state.currentFontSize;

            m_XAdvance = state.xAdvance;
            m_MaxCapHeight = state.maxCapHeight;
            m_MaxAscender = state.maxAscender;
            m_MaxDescender = state.maxDescender;
            m_MaxLineAscender = state.maxLineAscender;
            m_MaxLineDescender = state.maxLineDescender;
            m_StartOfLineAscender = state.startOfLineAscender;
            m_PreferredWidth = state.preferredWidth;
            m_PreferredHeight = state.preferredHeight;
            m_MeshExtents = state.meshExtents;
            m_PageAscender = state.pageAscender;

            m_LineNumber = state.lineNumber;
            m_LineOffset = state.lineOffset;
            m_BaselineOffset = state.baselineOffset;
            m_IsDrivenLineSpacing = state.isDrivenLineSpacing;

            m_HtmlColor = state.vertexColor;
            m_UnderlineColor = state.underlineColor;
            m_StrikethroughColor = state.strikethroughColor;
            m_HighlightColor = state.highlightColor;
            m_HighlightState = state.highlightState;

            m_IsNonBreakingSpace = state.isNonBreakingSpace;
            m_TagNoParsing = state.tagNoParsing;
            m_FXScale = state.fxScale;
            m_FXRotation = state.fxRotation;

            // XML Tag Stack
            m_FontStyleStack = state.basicStyleStack;
            m_ItalicAngleStack = state.italicAngleStack;
            m_ColorStack = state.colorStack;
            m_UnderlineColorStack = state.underlineColorStack;
            m_StrikethroughColorStack = state.strikethroughColorStack;
            m_HighlightColorStack = state.highlightColorStack;
            m_ColorGradientStack = state.colorGradientStack;
            m_HighlightStateStack = state.highlightStateStack;
            m_SizeStack = state.sizeStack;
            m_IndentStack = state.indentStack;
            m_FontWeightStack = state.fontWeightStack;
            m_StyleStack = state.styleStack;
            m_BaselineOffsetStack = state.baselineStack;
            m_ActionStack = state.actionStack;
            m_MaterialReferenceStack = state.materialReferenceStack;
            m_LineJustificationStack = state.lineJustificationStack;

            m_LastBaseGlyphIndex = state.lastBaseGlyphIndex;
            m_SpriteAnimationId = state.spriteAnimationId;

            if (m_LineNumber < textInfo.lineInfo.Length)
                textInfo.lineInfo[m_LineNumber] = state.lineInfo;

            return index;
        }

        protected bool ValidateHtmlTag(TextProcessingElement[] chars, int startIndex, out int endIndex, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            TextSettings textSettings = generationSettings.textSettings;

            int tagCharCount = 0;
            byte attributeFlag = 0;

            int attributeIndex = 0;
            ClearMarkupTagAttributes();
            TagValueType tagValueType = TagValueType.None;
            TagUnitType tagUnitType = TagUnitType.Pixels;

            endIndex = startIndex;
            bool isTagSet = false;
            bool isValidHtmlTag = false;

            for (int i = startIndex; i < chars.Length && chars[i].unicode != 0 && tagCharCount < m_HtmlTag.Length && chars[i].unicode != '<'; i++)
            {
                uint unicode = chars[i].unicode;

                if (unicode == '>') // ASCII Code of End HTML tag '>'
                {
                    isValidHtmlTag = true;
                    endIndex = i;
                    m_HtmlTag[tagCharCount] = (char)0;
                    break;
                }

                m_HtmlTag[tagCharCount] = (char)unicode;
                tagCharCount += 1;

                if (attributeFlag == 1)
                {
                    if (tagValueType == TagValueType.None)
                    {
                        // Check for attribute type
                        if (unicode == '+' || unicode == '-' || unicode == '.' || (unicode >= '0' && unicode <= '9'))
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = m_XmlAttribute[attributeIndex].valueType = TagValueType.NumericalValue;
                            m_XmlAttribute[attributeIndex].valueStartIndex = tagCharCount - 1;
                            m_XmlAttribute[attributeIndex].valueLength += 1;
                        }
                        else if (unicode == '#')
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = m_XmlAttribute[attributeIndex].valueType = TagValueType.ColorValue;
                            m_XmlAttribute[attributeIndex].valueStartIndex = tagCharCount - 1;
                            m_XmlAttribute[attributeIndex].valueLength += 1;
                        }
                        else if (unicode == '"')
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = m_XmlAttribute[attributeIndex].valueType = TagValueType.StringValue;
                            m_XmlAttribute[attributeIndex].valueStartIndex = tagCharCount;
                        }
                        else
                        {
                            tagUnitType = TagUnitType.Pixels;
                            tagValueType = m_XmlAttribute[attributeIndex].valueType = TagValueType.StringValue;
                            m_XmlAttribute[attributeIndex].valueStartIndex = tagCharCount - 1;
                            m_XmlAttribute[attributeIndex].valueHashCode = (m_XmlAttribute[attributeIndex].valueHashCode << 5) + m_XmlAttribute[attributeIndex].valueHashCode ^ TextGeneratorUtilities.ToUpperFast((char)unicode);
                            m_XmlAttribute[attributeIndex].valueLength += 1;
                        }
                    }
                    else
                    {
                        if (tagValueType == TagValueType.NumericalValue)
                        {
                            // Check for termination of numerical value.
                            if (unicode == 'p' || unicode == 'e' || unicode == '%' || unicode == ' ')
                            {
                                attributeFlag = 2;
                                tagValueType = TagValueType.None;

                                switch (unicode)
                                {
                                    case 'e':
                                        m_XmlAttribute[attributeIndex].unitType = tagUnitType = TagUnitType.FontUnits;
                                        break;
                                    case '%':
                                        m_XmlAttribute[attributeIndex].unitType = tagUnitType = TagUnitType.Percentage;
                                        break;
                                    default:
                                        m_XmlAttribute[attributeIndex].unitType = tagUnitType = TagUnitType.Pixels;
                                        break;
                                }

                                attributeIndex += 1;
                                m_XmlAttribute[attributeIndex].nameHashCode = 0;
                                m_XmlAttribute[attributeIndex].valueHashCode = 0;
                                m_XmlAttribute[attributeIndex].valueType = TagValueType.None;
                                m_XmlAttribute[attributeIndex].unitType = TagUnitType.Pixels;
                                m_XmlAttribute[attributeIndex].valueStartIndex = 0;
                                m_XmlAttribute[attributeIndex].valueLength = 0;

                            }
                            else
                            {
                                m_XmlAttribute[attributeIndex].valueLength += 1;
                            }
                        }
                        else if (tagValueType == TagValueType.ColorValue)
                        {
                            if (unicode != ' ')
                            {
                                m_XmlAttribute[attributeIndex].valueLength += 1;
                            }
                            else
                            {
                                attributeFlag = 2;
                                tagValueType = TagValueType.None;
                                tagUnitType = TagUnitType.Pixels;
                                attributeIndex += 1;
                                m_XmlAttribute[attributeIndex].nameHashCode = 0;
                                m_XmlAttribute[attributeIndex].valueType = TagValueType.None;
                                m_XmlAttribute[attributeIndex].unitType = TagUnitType.Pixels;
                                m_XmlAttribute[attributeIndex].valueHashCode = 0;
                                m_XmlAttribute[attributeIndex].valueStartIndex = 0;
                                m_XmlAttribute[attributeIndex].valueLength = 0;
                            }
                        }
                        else if (tagValueType == TagValueType.StringValue)
                        {
                            // Compute HashCode value for the named tag.
                            if (unicode != '"')
                            {
                                m_XmlAttribute[attributeIndex].valueHashCode = (m_XmlAttribute[attributeIndex].valueHashCode << 5) + m_XmlAttribute[attributeIndex].valueHashCode ^ TextGeneratorUtilities.ToUpperFast((char)unicode);
                                m_XmlAttribute[attributeIndex].valueLength += 1;
                            }
                            else
                            {
                                attributeFlag = 2;
                                tagValueType = TagValueType.None;
                                tagUnitType = TagUnitType.Pixels;
                                attributeIndex += 1;
                                m_XmlAttribute[attributeIndex].nameHashCode = 0;
                                m_XmlAttribute[attributeIndex].valueType = TagValueType.None;
                                m_XmlAttribute[attributeIndex].unitType = TagUnitType.Pixels;
                                m_XmlAttribute[attributeIndex].valueHashCode = 0;
                                m_XmlAttribute[attributeIndex].valueStartIndex = 0;
                                m_XmlAttribute[attributeIndex].valueLength = 0;
                            }
                        }
                    }
                }


                if (unicode == '=') // '='
                    attributeFlag = 1;

                // Compute HashCode for the name of the attribute
                if (attributeFlag == 0 && unicode == ' ')
                {
                    if (isTagSet) return false;

                    isTagSet = true;
                    attributeFlag = 2;

                    tagValueType = TagValueType.None;
                    tagUnitType = TagUnitType.Pixels;
                    attributeIndex += 1;
                    m_XmlAttribute[attributeIndex].nameHashCode = 0;
                    m_XmlAttribute[attributeIndex].valueType = TagValueType.None;
                    m_XmlAttribute[attributeIndex].unitType = TagUnitType.Pixels;
                    m_XmlAttribute[attributeIndex].valueHashCode = 0;
                    m_XmlAttribute[attributeIndex].valueStartIndex = 0;
                    m_XmlAttribute[attributeIndex].valueLength = 0;
                }

                if (attributeFlag == 0)
                    m_XmlAttribute[attributeIndex].nameHashCode = (m_XmlAttribute[attributeIndex].nameHashCode << 5) + m_XmlAttribute[attributeIndex].nameHashCode ^ TextGeneratorUtilities.ToUpperFast((char)unicode);

                if (attributeFlag == 2 && unicode == ' ')
                    attributeFlag = 0;

            }

            if (!isValidHtmlTag)
            {
                return false;
            }

            #region Rich Text Tag Processing
            // Special handling of the no parsing tag </noparse> </NOPARSE> tag
            if (m_TagNoParsing && (m_XmlAttribute[0].nameHashCode != (int)MarkupTag.SLASH_NO_PARSE))
                return false;

            if (m_XmlAttribute[0].nameHashCode == (int)MarkupTag.SLASH_NO_PARSE)
            {
                m_TagNoParsing = false;
                return true;
            }

            // Color <#FFF> 3 Hex values (short form)
            if (m_HtmlTag[0] == k_NumberSign && tagCharCount == 4)
            {
                m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, tagCharCount);
                m_ColorStack.Add(m_HtmlColor);
                return true;
            }
            // Color <#FFF7> 4 Hex values with alpha (short form)
            else if (m_HtmlTag[0] == k_NumberSign && tagCharCount == 5)
            {
                m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, tagCharCount);
                m_ColorStack.Add(m_HtmlColor);
                return true;
            }
            // Color <#FF00FF>
            else if (m_HtmlTag[0] == k_NumberSign && tagCharCount == 7) // if Tag begins with # and contains 7 characters.
            {
                m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, tagCharCount);
                m_ColorStack.Add(m_HtmlColor);
                return true;
            }
            // Color <#FF00FF00> with alpha
            else if (m_HtmlTag[0] == k_NumberSign && tagCharCount == k_Tab) // if Tag begins with # and contains 9 characters.
            {
                m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, tagCharCount);
                m_ColorStack.Add(m_HtmlColor);
                return true;
            }
            else
            {
                float value = 0;
                float fontScale;

                switch ((MarkupTag)m_XmlAttribute[0].nameHashCode)
                {
                    case MarkupTag.BOLD:
                        m_FontStyleInternal |= FontStyles.Bold;
                        m_FontStyleStack.Add(FontStyles.Bold);

                        m_FontWeightInternal = TextFontWeight.Bold;
                        return true;
                    case MarkupTag.SLASH_BOLD:
                        if ((generationSettings.fontStyle & FontStyles.Bold) != FontStyles.Bold)
                        {
                            if (m_FontStyleStack.Remove(FontStyles.Bold) == 0)
                            {
                                m_FontStyleInternal &= ~FontStyles.Bold;
                                m_FontWeightInternal = m_FontWeightStack.Peek();
                            }
                        }
                        return true;
                    case MarkupTag.ITALIC:
                        m_FontStyleInternal |= FontStyles.Italic;
                        m_FontStyleStack.Add(FontStyles.Italic);

                        if (m_XmlAttribute[1].nameHashCode == (int)MarkupTag.ANGLE)
                        {
                            m_ItalicAngle = (int)TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[1].valueStartIndex, m_XmlAttribute[1].valueLength);

                            // Make sure angle is within valid range.
                            if (m_ItalicAngle < -180 || m_ItalicAngle > 180) return false;
                        }
                        else
                            m_ItalicAngle = m_CurrentFontAsset.italicStyleSlant;

                        m_ItalicAngleStack.Add(m_ItalicAngle);

                        return true;
                    case MarkupTag.SLASH_ITALIC:
                        if ((generationSettings.fontStyle & FontStyles.Italic) != FontStyles.Italic)
                        {
                            m_ItalicAngle = m_ItalicAngleStack.Remove();

                            if (m_FontStyleStack.Remove(FontStyles.Italic) == 0)
                                m_FontStyleInternal &= ~FontStyles.Italic;
                        }
                        return true;
                    case MarkupTag.STRIKETHROUGH:
                        m_FontStyleInternal |= FontStyles.Strikethrough;
                        m_FontStyleStack.Add(FontStyles.Strikethrough);

                        if (m_XmlAttribute[1].nameHashCode == (int)MarkupTag.COLOR)
                        {
                            m_StrikethroughColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, m_XmlAttribute[1].valueStartIndex, m_XmlAttribute[1].valueLength);
                            m_StrikethroughColor.a = m_HtmlColor.a < m_StrikethroughColor.a ? (byte)(m_HtmlColor.a) : (byte)(m_StrikethroughColor.a);
                        }
                        else
                            m_StrikethroughColor = m_HtmlColor;

                        m_StrikethroughColorStack.Add(m_StrikethroughColor);

                        return true;
                    case MarkupTag.SLASH_STRIKETHROUGH:
                        if ((generationSettings.fontStyle & FontStyles.Strikethrough) != FontStyles.Strikethrough)
                        {
                            if (m_FontStyleStack.Remove(FontStyles.Strikethrough) == 0)
                                m_FontStyleInternal &= ~FontStyles.Strikethrough;
                        }

                        m_StrikethroughColor = m_StrikethroughColorStack.Remove();
                        return true;
                    case MarkupTag.UNDERLINE:
                        m_FontStyleInternal |= FontStyles.Underline;
                        m_FontStyleStack.Add(FontStyles.Underline);

                        if (m_XmlAttribute[1].nameHashCode == (int)MarkupTag.COLOR)
                        {
                            m_UnderlineColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, m_XmlAttribute[1].valueStartIndex, m_XmlAttribute[1].valueLength);
                            m_UnderlineColor.a = m_HtmlColor.a < m_UnderlineColor.a ? (m_HtmlColor.a) : (m_UnderlineColor.a);
                        }
                        else
                            m_UnderlineColor = m_HtmlColor;

                        m_UnderlineColorStack.Add(m_UnderlineColor);

                        return true;
                    case MarkupTag.SLASH_UNDERLINE:
                        if ((generationSettings.fontStyle & FontStyles.Underline) != FontStyles.Underline)
                        {
                            if (m_FontStyleStack.Remove(FontStyles.Underline) == 0)
                                m_FontStyleInternal &= ~FontStyles.Underline;
                        }

                        m_UnderlineColor = m_UnderlineColorStack.Remove();
                        return true;
                    case MarkupTag.MARK:
                        m_FontStyleInternal |= FontStyles.Highlight;
                        m_FontStyleStack.Add(FontStyles.Highlight);

                        Color32 highlightColor = new Color32(255, 255, 0, 64);
                        Offset highlightPadding = Offset.zero;

                        // Handle Mark Tag and potential attributes
                        for (int i = 0; i < m_XmlAttribute.Length && m_XmlAttribute[i].nameHashCode != 0; i++)
                        {
                            switch ((MarkupTag)m_XmlAttribute[i].nameHashCode)
                            {
                                // Mark tag
                                case MarkupTag.MARK:
                                    if (m_XmlAttribute[i].valueType == TagValueType.ColorValue)
                                        highlightColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);
                                    break;

                                // Color attribute
                                case MarkupTag.COLOR:
                                    highlightColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, m_XmlAttribute[i].valueStartIndex, m_XmlAttribute[i].valueLength);
                                    break;

                                // Padding attribute
                                case MarkupTag.PADDING:
                                    int paramCount = TextGeneratorUtilities.GetAttributeParameters(m_HtmlTag, m_XmlAttribute[i].valueStartIndex, m_XmlAttribute[i].valueLength, ref m_AttributeParameterValues);
                                    if (paramCount != 4) return false;

                                    highlightPadding = new Offset(m_AttributeParameterValues[0], m_AttributeParameterValues[1], m_AttributeParameterValues[2], m_AttributeParameterValues[3]);
                                    highlightPadding *= m_FontSize * 0.01f * (generationSettings.isOrthographic ? 1 : 0.1f);
                                    break;
                            }
                        }

                        highlightColor.a = m_HtmlColor.a < highlightColor.a ? (byte)(m_HtmlColor.a) : (byte)(highlightColor.a);

                        m_HighlightState = new HighlightState(highlightColor, highlightPadding);
                        m_HighlightStateStack.Push(m_HighlightState);

                        return true;
                    case MarkupTag.SLASH_MARK:
                        if ((generationSettings.fontStyle & FontStyles.Highlight) != FontStyles.Highlight)
                        {
                            m_HighlightStateStack.Remove();
                            m_HighlightState = m_HighlightStateStack.current;

                            if (m_FontStyleStack.Remove(FontStyles.Highlight) == 0)
                                m_FontStyleInternal &= ~FontStyles.Highlight;
                        }
                        return true;
                    case MarkupTag.SUBSCRIPT:
                        m_FontScaleMultiplier *= m_CurrentFontAsset.faceInfo.subscriptSize > 0 ? m_CurrentFontAsset.faceInfo.subscriptSize : 1;
                        m_BaselineOffsetStack.Push(m_BaselineOffset);
                        fontScale = (m_CurrentFontSize / m_CurrentFontAsset.faceInfo.pointSize * m_CurrentFontAsset.faceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f));
                        m_BaselineOffset += m_CurrentFontAsset.faceInfo.subscriptOffset * fontScale * m_FontScaleMultiplier;

                        m_FontStyleStack.Add(FontStyles.Subscript);
                        m_FontStyleInternal |= FontStyles.Subscript;
                        return true;
                    case MarkupTag.SLASH_SUBSCRIPT:
                        if ((m_FontStyleInternal & FontStyles.Subscript) == FontStyles.Subscript)
                        {
                            if (m_FontScaleMultiplier < 1)
                            {
                                m_BaselineOffset = m_BaselineOffsetStack.Pop();
                                m_FontScaleMultiplier /= m_CurrentFontAsset.faceInfo.subscriptSize > 0 ? m_CurrentFontAsset.faceInfo.subscriptSize : 1;
                            }

                            if (m_FontStyleStack.Remove(FontStyles.Subscript) == 0)
                                m_FontStyleInternal &= ~FontStyles.Subscript;
                        }
                        return true;
                    case MarkupTag.SUPERSCRIPT:
                        m_FontScaleMultiplier *= m_CurrentFontAsset.faceInfo.superscriptSize > 0 ? m_CurrentFontAsset.faceInfo.superscriptSize : 1;
                        m_BaselineOffsetStack.Push(m_BaselineOffset);
                        fontScale = (m_CurrentFontSize / m_CurrentFontAsset.faceInfo.pointSize * m_CurrentFontAsset.faceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f));
                        m_BaselineOffset += m_CurrentFontAsset.faceInfo.superscriptOffset * fontScale * m_FontScaleMultiplier;

                        m_FontStyleStack.Add(FontStyles.Superscript);
                        m_FontStyleInternal |= FontStyles.Superscript;
                        return true;
                    case MarkupTag.SLASH_SUPERSCRIPT:
                        if ((m_FontStyleInternal & FontStyles.Superscript) == FontStyles.Superscript)
                        {
                            if (m_FontScaleMultiplier < 1)
                            {
                                m_BaselineOffset = m_BaselineOffsetStack.Pop();
                                m_FontScaleMultiplier /= m_CurrentFontAsset.faceInfo.superscriptSize > 0 ? m_CurrentFontAsset.faceInfo.superscriptSize : 1;
                            }

                            if (m_FontStyleStack.Remove(FontStyles.Superscript) == 0)
                                m_FontStyleInternal &= ~FontStyles.Superscript;
                        }
                        return true;
                    case MarkupTag.FONT_WEIGHT:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch ((int)value)
                        {
                            case 100:
                                m_FontWeightInternal = TextFontWeight.Thin;
                                break;
                            case 200:
                                m_FontWeightInternal = TextFontWeight.ExtraLight;
                                break;
                            case 300:
                                m_FontWeightInternal = TextFontWeight.Light;
                                break;
                            case 400:
                                m_FontWeightInternal = TextFontWeight.Regular;
                                break;
                            case 500:
                                m_FontWeightInternal = TextFontWeight.Medium;
                                break;
                            case 600:
                                m_FontWeightInternal = TextFontWeight.SemiBold;
                                break;
                            case 700:
                                m_FontWeightInternal = TextFontWeight.Bold;
                                break;
                            case 800:
                                m_FontWeightInternal = TextFontWeight.Heavy;
                                break;
                            case 900:
                                m_FontWeightInternal = TextFontWeight.Black;
                                break;
                        }

                        m_FontWeightStack.Add(m_FontWeightInternal);

                        return true;
                    case MarkupTag.SLASH_FONT_WEIGHT:
                        m_FontWeightStack.Remove();

                        if (m_FontStyleInternal == FontStyles.Bold)
                            m_FontWeightInternal = TextFontWeight.Bold;
                        else
                            m_FontWeightInternal = m_FontWeightStack.Peek();

                        return true;
                    case MarkupTag.POSITION:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_XAdvance = value * (generationSettings.isOrthographic ? 1.0f : 0.1f);
                                //m_isIgnoringAlignment = true;
                                return true;
                            case TagUnitType.FontUnits:
                                m_XAdvance = value * m_CurrentFontSize * (generationSettings.isOrthographic ? 1.0f : 0.1f);
                                //m_isIgnoringAlignment = true;
                                return true;
                            case TagUnitType.Percentage:
                                m_XAdvance = m_MarginWidth * value / 100;
                                //m_isIgnoringAlignment = true;
                                return true;
                        }
                        return false;
                    case MarkupTag.SLASH_POSITION:
                        m_IsIgnoringAlignment = false;
                        return true;
                    case MarkupTag.VERTICAL_OFFSET:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_BaselineOffset = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                return true;
                            case TagUnitType.FontUnits:
                                m_BaselineOffset = value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                return true;
                            case TagUnitType.Percentage:
                                //m_BaselineOffset = m_MarginHeight * val / 100;
                                return false;
                        }
                        return false;
                    case MarkupTag.SLASH_VERTICAL_OFFSET:
                        m_BaselineOffset = 0;
                        return true;
                    case MarkupTag.PAGE:
                        // This tag only works when Overflow - Page mode is used.
                        if (generationSettings.overflowMode == TextOverflowMode.Page)
                        {
                            m_XAdvance = 0 + m_TagLineIndent + m_TagIndent;
                            m_LineOffset = 0;
                            m_PageNumber += 1;
                            m_IsNewPage = true;
                        }
                        return true;
                    case MarkupTag.NO_BREAK:
                        m_IsNonBreakingSpace = true;
                        return true;
                    case MarkupTag.SLASH_NO_BREAK:
                        m_IsNonBreakingSpace = false;
                        return true;
                    case MarkupTag.SIZE:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                if (m_HtmlTag[5] == k_Plus) // <size=+00>
                                {
                                    m_CurrentFontSize = m_FontSize + value;
                                    m_SizeStack.Add(m_CurrentFontSize);
                                    return true;
                                }
                                else if (m_HtmlTag[5] == k_Minus) // <size=-00>
                                {
                                    m_CurrentFontSize = m_FontSize + value;
                                    m_SizeStack.Add(m_CurrentFontSize);
                                    return true;
                                }
                                else // <size=00.0>
                                {
                                    m_CurrentFontSize = value;
                                    m_SizeStack.Add(m_CurrentFontSize);
                                    return true;
                                }
                            case TagUnitType.FontUnits:
                                m_CurrentFontSize = m_FontSize * value;
                                m_SizeStack.Add(m_CurrentFontSize);
                                return true;
                            case TagUnitType.Percentage:
                                m_CurrentFontSize = m_FontSize * value / 100;
                                m_SizeStack.Add(m_CurrentFontSize);
                                return true;
                        }
                        return false;
                    case MarkupTag.SLASH_SIZE:
                        m_CurrentFontSize = m_SizeStack.Remove();
                        return true;
                    case MarkupTag.FONT:
                        int fontHashCode = m_XmlAttribute[0].valueHashCode;
                        int materialAttributeHashCode = m_XmlAttribute[1].nameHashCode;
                        int materialHashCode = m_XmlAttribute[1].valueHashCode;

                        // Special handling for <font=default> or <font=Default>
                        if (fontHashCode == (int)MarkupTag.DEFAULT)
                        {
                            m_CurrentFontAsset = m_MaterialReferences[0].fontAsset;
                            m_CurrentMaterial = m_MaterialReferences[0].material;
                            m_CurrentMaterialIndex = 0;
                            m_MaterialReferenceStack.Add(m_MaterialReferences[0]);

                            return true;
                        }

                        FontAsset tempFont;
                        Material tempMaterial;

                        // HANDLE NEW FONT ASSET

                        // Check if we already have a reference to this font asset.
                        MaterialReferenceManager.TryGetFontAsset(fontHashCode, out tempFont);

                        // Try loading font asset from potential delegate or resources.
                        if (tempFont == null)
                        {
                            if (tempFont == null)
                            {
                                // Load Font Asset
                                tempFont = Resources.Load<FontAsset>(textSettings.defaultFontAssetPath + new string(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength));
                            }

                            if (tempFont == null)
                                return false;

                            // Add new reference to the font asset as well as default material to the MaterialReferenceManager
                            MaterialReferenceManager.AddFontAsset(tempFont);
                        }

                        // HANDLE NEW MATERIAL
                        if (materialAttributeHashCode == 0 && materialHashCode == 0)
                        {
                            // No material specified then use default font asset material.
                            m_CurrentMaterial = tempFont.material;

                            m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentMaterial, tempFont, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);

                            m_MaterialReferenceStack.Add(m_MaterialReferences[m_CurrentMaterialIndex]);
                        }
                        else if (materialAttributeHashCode == (int)MarkupTag.MATERIAL) // using material attribute
                        {
                            if (MaterialReferenceManager.TryGetMaterial(materialHashCode, out tempMaterial))
                            {
                                m_CurrentMaterial = tempMaterial;

                                m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentMaterial, tempFont, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);

                                m_MaterialReferenceStack.Add(m_MaterialReferences[m_CurrentMaterialIndex]);
                            }
                            else
                            {
                                // Load new material
                                tempMaterial = Resources.Load<Material>(textSettings.defaultFontAssetPath + new string(m_HtmlTag, m_XmlAttribute[1].valueStartIndex, m_XmlAttribute[1].valueLength));

                                if (tempMaterial == null)
                                    return false;

                                // Add new reference to this material in the MaterialReferenceManager
                                MaterialReferenceManager.AddFontMaterial(materialHashCode, tempMaterial);

                                m_CurrentMaterial = tempMaterial;

                                m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentMaterial, tempFont, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);

                                m_MaterialReferenceStack.Add(m_MaterialReferences[m_CurrentMaterialIndex]);
                            }
                        }
                        else
                            return false;

                        m_CurrentFontAsset = tempFont;

                        return true;
                    case MarkupTag.SLASH_FONT:
                        {
                            MaterialReference materialReference = m_MaterialReferenceStack.Remove();

                            m_CurrentFontAsset = materialReference.fontAsset;
                            m_CurrentMaterial = materialReference.material;
                            m_CurrentMaterialIndex = materialReference.index;

                            return true;
                        }
                    case MarkupTag.MATERIAL:
                        materialHashCode = m_XmlAttribute[0].valueHashCode;

                        // Special handling for <material=default> or <material=Default>
                        if (materialHashCode == (int)MarkupTag.DEFAULT)
                        {
                            // Check if material font atlas texture matches that of the current font asset.
                            //if (m_CurrentFontAsset.atlas.GetInstanceID() != m_CurrentMaterial.GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID()) return false;

                            m_CurrentMaterial = m_MaterialReferences[0].material;
                            m_CurrentMaterialIndex = 0;

                            m_MaterialReferenceStack.Add(m_MaterialReferences[0]);

                            return true;
                        }


                        // Check if material
                        if (MaterialReferenceManager.TryGetMaterial(materialHashCode, out tempMaterial))
                        {
                            // Check if material font atlas texture matches that of the current font asset.
                            //if (m_CurrentFontAsset.atlas.GetInstanceID() != tempMaterial.GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID()) return false;

                            m_CurrentMaterial = tempMaterial;

                            m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentMaterial, m_CurrentFontAsset, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);

                            m_MaterialReferenceStack.Add(m_MaterialReferences[m_CurrentMaterialIndex]);
                        }
                        else
                        {
                            // Load new material
                            tempMaterial = Resources.Load<Material>(textSettings.defaultFontAssetPath + new string(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength));

                            if (tempMaterial == null)
                                return false;

                            // Check if material font atlas texture matches that of the current font asset.
                            //if (m_CurrentFontAsset.atlas.GetInstanceID() != tempMaterial.GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID()) return false;

                            // Add new reference to this material in the MaterialReferenceManager
                            MaterialReferenceManager.AddFontMaterial(materialHashCode, tempMaterial);

                            m_CurrentMaterial = tempMaterial;

                            m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentMaterial, m_CurrentFontAsset, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);

                            m_MaterialReferenceStack.Add(m_MaterialReferences[m_CurrentMaterialIndex]);
                        }
                        return true;
                    case MarkupTag.SLASH_MATERIAL:
                        {
                            //if (m_CurrentMaterial.GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID() != m_MaterialReferenceStack.PreviousItem().material.GetTexture(ShaderUtilities.ID_MainTex).GetInstanceID())
                            //    return false;

                            MaterialReference materialReference = m_MaterialReferenceStack.Remove();

                            m_CurrentMaterial = materialReference.material;
                            m_CurrentMaterialIndex = materialReference.index;

                            return true;
                        }
                    case MarkupTag.SPACE:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_XAdvance += value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                return true;
                            case TagUnitType.FontUnits:
                                m_XAdvance += value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                return true;
                            case TagUnitType.Percentage:
                                // Not applicable
                                return false;
                        }
                        return false;
                    case MarkupTag.ALPHA:
                        if (m_XmlAttribute[0].valueLength != 3) return false;

                        m_HtmlColor.a = (byte)(TextGeneratorUtilities.HexToInt(m_HtmlTag[7]) * 16 + TextGeneratorUtilities.HexToInt(m_HtmlTag[8]));
                        return true;

                    case MarkupTag.A:
                        if (m_isTextLayoutPhase && !m_IsCalculatingPreferredValues)
                        {
                            if (m_XmlAttribute[1].nameHashCode == (int)MarkupTag.HREF)
                            {
                                // Make sure linkInfo array is of appropriate size.
                                int index = textInfo.linkCount;

                                if (index + 1 > textInfo.linkInfo.Length)
                                    TextInfo.Resize(ref textInfo.linkInfo, index + 1);

                                textInfo.linkInfo[index].hashCode = (int)MarkupTag.HREF;
                                textInfo.linkInfo[index].linkTextfirstCharacterIndex = m_CharacterCount;
                                textInfo.linkInfo[index].linkIdFirstCharacterIndex = startIndex + m_XmlAttribute[1].valueStartIndex;
                                textInfo.linkInfo[index].SetLinkId(m_HtmlTag, m_XmlAttribute[1].valueStartIndex, m_XmlAttribute[1].valueLength);
                            }
                        }
                        return true;

                    case MarkupTag.SLASH_A:
                        if (m_isTextLayoutPhase && !m_IsCalculatingPreferredValues)
                        {
                            int index = textInfo.linkCount;

                            textInfo.linkInfo[index].linkTextLength = m_CharacterCount - textInfo.linkInfo[index].linkTextfirstCharacterIndex;

                            textInfo.linkCount += 1;
                        }
                        return true;

                    case MarkupTag.LINK:
                        if (m_isTextLayoutPhase && !m_IsCalculatingPreferredValues)
                        {
                            int index = textInfo.linkCount;

                            if (index + 1 > textInfo.linkInfo.Length)
                                TextInfo.Resize(ref textInfo.linkInfo, index + 1);

                            textInfo.linkInfo[index].hashCode = m_XmlAttribute[0].valueHashCode;
                            textInfo.linkInfo[index].linkTextfirstCharacterIndex = m_CharacterCount;

                            textInfo.linkInfo[index].linkIdFirstCharacterIndex = startIndex + m_XmlAttribute[0].valueStartIndex;
                            textInfo.linkInfo[index].SetLinkId(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);
                        }
                        return true;
                    case MarkupTag.SLASH_LINK:
                        if (m_isTextLayoutPhase && !m_IsCalculatingPreferredValues)
                        {
                            if (textInfo.linkCount < textInfo.linkInfo.Length)
                            {
                                textInfo.linkInfo[textInfo.linkCount].linkTextLength = m_CharacterCount - textInfo.linkInfo[textInfo.linkCount].linkTextfirstCharacterIndex;

                                textInfo.linkCount += 1;
                            }
                        }
                        return true;
                    case MarkupTag.ALIGN: // <align=>
                        switch ((MarkupTag)m_XmlAttribute[0].valueHashCode)
                        {
                            case MarkupTag.LEFT: // <align=left>
                                m_LineJustification = TextAlignment.MiddleLeft;
                                m_LineJustificationStack.Add(m_LineJustification);
                                return true;
                            case MarkupTag.RIGHT: // <align=right>
                                m_LineJustification = TextAlignment.MiddleRight;
                                m_LineJustificationStack.Add(m_LineJustification);
                                return true;
                            case MarkupTag.CENTER: // <align=center>
                                m_LineJustification = TextAlignment.MiddleCenter;
                                m_LineJustificationStack.Add(m_LineJustification);
                                return true;
                            case MarkupTag.JUSTIFIED: // <align=justified>
                                m_LineJustification = TextAlignment.MiddleJustified;
                                m_LineJustificationStack.Add(m_LineJustification);
                                return true;
                            case MarkupTag.FLUSH: // <align=flush>
                                m_LineJustification = TextAlignment.MiddleFlush;
                                m_LineJustificationStack.Add(m_LineJustification);
                                return true;
                        }
                        return false;
                    case MarkupTag.SLASH_ALIGN:
                        m_LineJustification = m_LineJustificationStack.Remove();
                        return true;
                    case MarkupTag.WIDTH:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_Width = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                return false;
                            //break;
                            case TagUnitType.Percentage:
                                m_Width = m_MarginWidth * value / 100;
                                break;
                        }
                        return true;
                    case MarkupTag.SLASH_WIDTH:
                        m_Width = -1;
                        return true;
                    // STYLE tag is now handled inline and replaced by its definition.
                    //case 322689: // <style="name">
                    //case 233057: // <STYLE>
                    //    Style style = StyleSheet.GetStyle(m_XmlAttribute[0].valueHashCode);

                    //    if (style == null) return false;

                    //    m_StyleStack.Add(style.hashCode);

                    //    // Parse Style Macro
                    //    for (int i = 0; i < style.styleOpeningTagArray.Length; i++)
                    //    {
                    //        if (style.styleOpeningTagArray[i] == k_LesserThan)
                    //        {
                    //            if (ValidateHtmlTag(style.styleOpeningTagArray, i + 1, out i) == false) return false;
                    //        }
                    //    }
                    //    return true;
                    //case 1112618: // </style>
                    //case 1022986: // </STYLE>
                    //    style = StyleSheet.GetStyle(m_XmlAttribute[0].valueHashCode);

                    //    if (style == null)
                    //    {
                    //        // Get style from the Style Stack
                    //        int styleHashCode = m_StyleStack.CurrentItem();
                    //        style = StyleSheet.GetStyle(styleHashCode);

                    //        m_StyleStack.Remove();
                    //    }

                    //    if (style == null) return false;
                    //    //// Parse Style Macro
                    //    for (int i = 0; i < style.styleClosingTagArray.Length; i++)
                    //    {
                    //        if (style.styleClosingTagArray[i] == k_LesserThan)
                    //            ValidateHtmlTag(style.styleClosingTagArray, i + 1, out i);
                    //    }
                    //    return true;
                    case MarkupTag.COLOR:
                        // <color=#FFF> 3 Hex (short hand)
                        if (m_HtmlTag[6] == k_NumberSign && tagCharCount == k_LineFeed)
                        {
                            m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, tagCharCount);
                            m_ColorStack.Add(m_HtmlColor);
                            return true;
                        }
                        // <color=#FFF7> 4 Hex (short hand)
                        else if (m_HtmlTag[6] == k_NumberSign && tagCharCount == 11)
                        {
                            m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, tagCharCount);
                            m_ColorStack.Add(m_HtmlColor);
                            return true;
                        }
                        // <color=#FF00FF> 3 Hex pairs
                        if (m_HtmlTag[6] == k_NumberSign && tagCharCount == k_CarriageReturn)
                        {
                            m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, tagCharCount);
                            m_ColorStack.Add(m_HtmlColor);
                            return true;
                        }
                        // <color=#FF00FF00> 4 Hex pairs
                        else if (m_HtmlTag[6] == k_NumberSign && tagCharCount == 15)
                        {
                            m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, tagCharCount);
                            m_ColorStack.Add(m_HtmlColor);
                            return true;
                        }

                        // <color=name>
                        switch (m_XmlAttribute[0].valueHashCode)
                        {
                            case (int)MarkupTag.RED: // <color=red>
                                m_HtmlColor = Color.red;
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case -992792864: // <color=lightblue>
                                m_HtmlColor = new Color32(173, 216, 230, 255);
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case (int)MarkupTag.BLUE: // <color=blue>
                                m_HtmlColor = Color.blue;
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case 3680713: // <color=grey>
                                m_HtmlColor = new Color32(128, 128, 128, 255);
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case (int)MarkupTag.BLACK: // <color=black>
                                m_HtmlColor = Color.black;
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case (int)MarkupTag.GREEN: // <color=green>
                                m_HtmlColor = Color.green;
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case (int)MarkupTag.WHITE: // <color=white>
                                m_HtmlColor = Color.white;
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case (int)MarkupTag.ORANGE: // <color=orange>
                                m_HtmlColor = new Color32(255, 128, 0, 255);
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case (int)MarkupTag.PURPLE: // <color=purple>
                                m_HtmlColor = new Color32(160, 32, 240, 255);
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case (int)MarkupTag.YELLOW: // <color=yellow>
                                m_HtmlColor = Color.yellow;
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                        }
                        return false;

                    case MarkupTag.GRADIENT:
                        int gradientPresetHashCode = m_XmlAttribute[0].valueHashCode;
                        TextColorGradient tempColorGradientPreset;

                        // Check if Color Gradient Preset has already been loaded.
                        if (MaterialReferenceManager.TryGetColorGradientPreset(gradientPresetHashCode, out tempColorGradientPreset))
                        {
                            m_ColorGradientPreset = tempColorGradientPreset;
                        }
                        else
                        {
                            // Load Color Gradient Preset
                            if (tempColorGradientPreset == null)
                            {
                                tempColorGradientPreset = Resources.Load<TextColorGradient>(textSettings.defaultColorGradientPresetsPath + new string(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength));
                            }

                            if (tempColorGradientPreset == null)
                                return false;

                            MaterialReferenceManager.AddColorGradientPreset(gradientPresetHashCode, tempColorGradientPreset);
                            m_ColorGradientPreset = tempColorGradientPreset;
                        }

                        m_ColorGradientPresetIsTinted = false;

                        // Check Attributes
                        for (int i = 1; i < m_XmlAttribute.Length && m_XmlAttribute[i].nameHashCode != 0; i++)
                        {
                            // Get attribute name
                            int nameHashCode = m_XmlAttribute[i].nameHashCode;

                            switch ((MarkupTag)nameHashCode)
                            {
                                case MarkupTag.TINT:
                                    m_ColorGradientPresetIsTinted = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[i].valueStartIndex, m_XmlAttribute[i].valueLength) != 0;
                                    break;
                            }
                        }

                        m_ColorGradientStack.Add(m_ColorGradientPreset);

                        // TODO : Add support for defining preset in the tag itself

                        return true;

                    case MarkupTag.SLASH_GRADIENT:
                        m_ColorGradientPreset = m_ColorGradientStack.Remove();
                        return true;

                    case MarkupTag.CHARACTER_SPACE:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_CSpacing = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_CSpacing = value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                return false;
                        }
                        return true;
                    case MarkupTag.SLASH_CHARACTER_SPACE:
                        if (!m_isTextLayoutPhase) return true;

                        // Adjust xAdvance to remove extra space from last character.
                        if (m_CharacterCount > 0)
                        {
                            m_XAdvance -= m_CSpacing;
                            textInfo.textElementInfo[m_CharacterCount - 1].xAdvance = m_XAdvance;
                        }
                        m_CSpacing = 0;
                        return true;
                    case MarkupTag.MONOSPACE:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_MonoSpacing = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_MonoSpacing = value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                return false;
                        }
                        return true;
                    case MarkupTag.SLASH_MONOSPACE:
                        m_MonoSpacing = 0;
                        return true;
                    case MarkupTag.CLASS:
                        return false;
                    case MarkupTag.SLASH_COLOR:
                        m_HtmlColor = m_ColorStack.Remove();
                        return true;
                    case MarkupTag.INDENT:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_TagIndent = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_TagIndent = value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                m_TagIndent = m_MarginWidth * value / 100;
                                break;
                        }
                        m_IndentStack.Add(m_TagIndent);

                        m_XAdvance = m_TagIndent;
                        return true;
                    case MarkupTag.SLASH_INDENT:
                        m_TagIndent = m_IndentStack.Remove();
                        //m_XAdvance = m_TagIndent;
                        return true;
                    case MarkupTag.LINE_INDENT:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_TagLineIndent = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_TagLineIndent = value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                m_TagLineIndent = m_MarginWidth * value / 100;
                                break;
                        }

                        m_XAdvance += m_TagLineIndent;
                        return true;
                    case MarkupTag.SLASH_LINE_INDENT:
                        m_TagLineIndent = 0;
                        return true;
                    case MarkupTag.SPRITE:
                        int spriteAssetHashCode = m_XmlAttribute[0].valueHashCode;
                        SpriteAsset tempSpriteAsset;
                        m_SpriteIndex = -1;

                        // CHECK TAG FORMAT
                        if (m_XmlAttribute[0].valueType == TagValueType.None || m_XmlAttribute[0].valueType == TagValueType.NumericalValue)
                        {
                            // No Sprite Asset is assigned to the text object
                            if (generationSettings.spriteAsset != null)
                            {
                                m_CurrentSpriteAsset = generationSettings.spriteAsset;
                            }
                            else if (textSettings.defaultSpriteAsset != null)
                            {
                                m_CurrentSpriteAsset = textSettings.defaultSpriteAsset;
                            }
                            else if (m_DefaultSpriteAsset != null)
                            {
                                m_CurrentSpriteAsset = m_DefaultSpriteAsset;
                            }
                            else if (m_DefaultSpriteAsset == null)
                            {
                                m_DefaultSpriteAsset = Resources.Load<SpriteAsset>("Sprite Assets/Default Sprite Asset");
                                m_CurrentSpriteAsset = m_DefaultSpriteAsset;
                            }

                            // No valid sprite asset available
                            if (m_CurrentSpriteAsset == null)
                                return false;
                        }
                        else
                        {
                            // A Sprite Asset has been specified
                            if (MaterialReferenceManager.TryGetSpriteAsset(spriteAssetHashCode, out tempSpriteAsset))
                            {
                                m_CurrentSpriteAsset = tempSpriteAsset;
                            }
                            else
                            {
                                // Load Sprite Asset
                                if (tempSpriteAsset == null)
                                {
                                    if (tempSpriteAsset == null)
                                        tempSpriteAsset = Resources.Load<SpriteAsset>(textSettings.defaultSpriteAssetPath + new string(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength));
                                }

                                if (tempSpriteAsset == null)
                                    return false;

                                MaterialReferenceManager.AddSpriteAsset(spriteAssetHashCode, tempSpriteAsset);
                                m_CurrentSpriteAsset = tempSpriteAsset;
                            }
                        }

                        // Handling of <sprite=index> legacy tag format.
                        if (m_XmlAttribute[0].valueType == TagValueType.NumericalValue) // <sprite=index>
                        {
                            int index = (int)TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                            // Reject tag if value is invalid.
                            if (index == Int16.MinValue) return false;

                            // Check to make sure sprite index is valid
                            if (index > m_CurrentSpriteAsset.spriteCharacterTable.Count - 1) return false;

                            m_SpriteIndex = index;
                        }

                        m_SpriteColor = Color.white;
                        m_TintSprite = false;

                        // Handle Sprite Tag Attributes
                        for (int i = 0; i < m_XmlAttribute.Length && m_XmlAttribute[i].nameHashCode != 0; i++)
                        {
                            int nameHashCode = m_XmlAttribute[i].nameHashCode;
                            int index = 0;

                            switch ((MarkupTag)nameHashCode)
                            {
                                case MarkupTag.NAME:
                                    m_CurrentSpriteAsset = SpriteAsset.SearchForSpriteByHashCode(m_CurrentSpriteAsset, m_XmlAttribute[i].valueHashCode, true, out index);
                                    if (index == -1) return false;

                                    m_SpriteIndex = index;
                                    break;
                                case MarkupTag.INDEX:
                                    index = (int)TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[1].valueStartIndex, m_XmlAttribute[1].valueLength);

                                    // Reject tag if value is invalid.
                                    if (index == Int16.MinValue) return false;

                                    // Check to make sure sprite index is valid
                                    if (index > m_CurrentSpriteAsset.spriteCharacterTable.Count - 1) return false;

                                    m_SpriteIndex = index;
                                    break;
                                case MarkupTag.TINT:
                                    m_TintSprite = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[i].valueStartIndex, m_XmlAttribute[i].valueLength) != 0;
                                    break;
                                case MarkupTag.COLOR:
                                    m_SpriteColor = TextGeneratorUtilities.HexCharsToColor(m_HtmlTag, m_XmlAttribute[i].valueStartIndex, m_XmlAttribute[i].valueLength);
                                    break;
                                case MarkupTag.ANIM:
                                    int paramCount = TextGeneratorUtilities.GetAttributeParameters(m_HtmlTag, m_XmlAttribute[i].valueStartIndex, m_XmlAttribute[i].valueLength, ref m_AttributeParameterValues);
                                    if (paramCount != 3) return false;

                                    m_SpriteIndex = (int)m_AttributeParameterValues[0];

                                    if (m_isTextLayoutPhase)
                                    {
                                        // It is possible for a sprite to get animated when it ends up being truncated.
                                        // Should consider moving the animation of the sprite after text geometry upload.
                                        //spriteAnimator.DoSpriteAnimation(m_CharacterCount, m_CurrentSpriteAsset, m_SpriteIndex, (int)m_AttributeParameterValues[1], (int)m_AttributeParameterValues[2]);
                                    }

                                    break;
                                //case 45545: // size
                                //case 32745: // SIZE

                                //    break;
                                default:
                                    if (nameHashCode != (int)MarkupTag.SPRITE)
                                        return false;
                                    break;
                            }
                        }

                        if (m_SpriteIndex == -1) return false;

                        // Material HashCode for the Sprite Asset is the Sprite Asset Hash Code
                        m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentSpriteAsset.material, m_CurrentSpriteAsset, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);

                        m_TextElementType = TextElementType.Sprite;
                        return true;
                    case MarkupTag.LOWERCASE:
                        m_FontStyleInternal |= FontStyles.LowerCase;
                        m_FontStyleStack.Add(FontStyles.LowerCase);
                        return true;
                    case MarkupTag.SLASH_LOWERCASE:
                        if ((generationSettings.fontStyle & FontStyles.LowerCase) != FontStyles.LowerCase)
                        {
                            if (m_FontStyleStack.Remove(FontStyles.LowerCase) == 0)
                                m_FontStyleInternal &= ~FontStyles.LowerCase;
                        }
                        return true;
                    case MarkupTag.ALLCAPS:
                    case MarkupTag.UPPERCASE:
                        m_FontStyleInternal |= FontStyles.UpperCase;
                        m_FontStyleStack.Add(FontStyles.UpperCase);
                        return true;
                    case MarkupTag.SLASH_ALLCAPS:
                    case MarkupTag.SLASH_UPPERCASE:
                        if ((generationSettings.fontStyle & FontStyles.UpperCase) != FontStyles.UpperCase)
                        {
                            if (m_FontStyleStack.Remove(FontStyles.UpperCase) == 0)
                                m_FontStyleInternal &= ~FontStyles.UpperCase;
                        }
                        return true;
                    case MarkupTag.SMALLCAPS:
                        m_FontStyleInternal |= FontStyles.SmallCaps;
                        m_FontStyleStack.Add(FontStyles.SmallCaps);
                        return true;
                    case MarkupTag.SLASH_SMALLCAPS:
                        if ((generationSettings.fontStyle & FontStyles.SmallCaps) != FontStyles.SmallCaps)
                        {
                            if (m_FontStyleStack.Remove(FontStyles.SmallCaps) == 0)
                                m_FontStyleInternal &= ~FontStyles.SmallCaps;
                        }
                        return true;
                    case MarkupTag.MARGIN:
                        // Check value type
                        switch (m_XmlAttribute[0].valueType)
                        {
                            case TagValueType.NumericalValue:
                                value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength); // px

                                // Reject tag if value is invalid.
                                if (value == Int16.MinValue) return false;

                                // Determine tag unit type
                                switch (tagUnitType)
                                {
                                    case TagUnitType.Pixels:
                                        m_MarginLeft = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                        break;
                                    case TagUnitType.FontUnits:
                                        m_MarginLeft = value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                        break;
                                    case TagUnitType.Percentage:
                                        m_MarginLeft = (m_MarginWidth - (m_Width != -1 ? m_Width : 0)) * value / 100;
                                        break;
                                }
                                m_MarginLeft = m_MarginLeft >= 0 ? m_MarginLeft : 0;
                                m_MarginRight = m_MarginLeft;
                                return true;

                            case TagValueType.None:
                                for (int i = 1; i < m_XmlAttribute.Length && m_XmlAttribute[i].nameHashCode != 0; i++)
                                {
                                    // Get attribute name
                                    int nameHashCode = m_XmlAttribute[i].nameHashCode;

                                    switch ((MarkupTag)nameHashCode)
                                    {
                                        case MarkupTag.LEFT:
                                            value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[i].valueStartIndex, m_XmlAttribute[i].valueLength); // px

                                            // Reject tag if value is invalid.
                                            if (value == Int16.MinValue) return false;

                                            switch (m_XmlAttribute[i].unitType)
                                            {
                                                case TagUnitType.Pixels:
                                                    m_MarginLeft = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                                    break;
                                                case TagUnitType.FontUnits:
                                                    m_MarginLeft = value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                                    break;
                                                case TagUnitType.Percentage:
                                                    m_MarginLeft = (m_MarginWidth - (m_Width != -1 ? m_Width : 0)) * value / 100;
                                                    break;
                                            }
                                            m_MarginLeft = m_MarginLeft >= 0 ? m_MarginLeft : 0;
                                            break;

                                        case MarkupTag.RIGHT:
                                            value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[i].valueStartIndex, m_XmlAttribute[i].valueLength); // px

                                            // Reject tag if value is invalid.
                                            if (value == Int16.MinValue) return false;

                                            switch (m_XmlAttribute[i].unitType)
                                            {
                                                case TagUnitType.Pixels:
                                                    m_MarginRight = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                                    break;
                                                case TagUnitType.FontUnits:
                                                    m_MarginRight = value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                                    break;
                                                case TagUnitType.Percentage:
                                                    m_MarginRight = (m_MarginWidth - (m_Width != -1 ? m_Width : 0)) * value / 100;
                                                    break;
                                            }
                                            m_MarginRight = m_MarginRight >= 0 ? m_MarginRight : 0;
                                            break;
                                    }
                                }
                                return true;
                        }

                        return false;
                    case MarkupTag.SLASH_MARGIN:
                        m_MarginLeft = 0;
                        m_MarginRight = 0;
                        return true;
                    case MarkupTag.MARGIN_LEFT:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength); // px

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_MarginLeft = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_MarginLeft = value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                m_MarginLeft = (m_MarginWidth - (m_Width != -1 ? m_Width : 0)) * value / 100;
                                break;
                        }
                        m_MarginLeft = m_MarginLeft >= 0 ? m_MarginLeft : 0;
                        return true;
                    case MarkupTag.MARGIN_RIGHT:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength); // px

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_MarginRight = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_MarginRight = value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                m_MarginRight = (m_MarginWidth - (m_Width != -1 ? m_Width : 0)) * value / 100;
                                break;
                        }
                        m_MarginRight = m_MarginRight >= 0 ? m_MarginRight : 0;
                        return true;
                    case MarkupTag.LINE_HEIGHT:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        switch (tagUnitType)
                        {
                            case TagUnitType.Pixels:
                                m_LineHeight = value * (generationSettings.isOrthographic ? 1 : 0.1f);
                                break;
                            case TagUnitType.FontUnits:
                                m_LineHeight = value * (generationSettings.isOrthographic ? 1 : 0.1f) * m_CurrentFontSize;
                                break;
                            case TagUnitType.Percentage:
                                fontScale = (m_CurrentFontSize / m_CurrentFontAsset.faceInfo.pointSize * m_CurrentFontAsset.faceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f));
                                m_LineHeight = generationSettings.fontAsset.faceInfo.lineHeight * value / 100 * fontScale;
                                break;
                        }
                        return true;
                    case MarkupTag.SLASH_LINE_HEIGHT:
                        m_LineHeight = k_FloatUnset;
                        return true;
                    case MarkupTag.NO_PARSE:
                        m_TagNoParsing = true;
                        return true;
                    case MarkupTag.ACTION:
                        int actionID = m_XmlAttribute[0].valueHashCode;

                        if (m_isTextLayoutPhase)
                        {
                            m_ActionStack.Add(actionID);

                            Debug.Log("Action ID: [" + actionID + "] First character index: " + m_CharacterCount);
                        }
                        //if (m_isTextLayoutPhase)
                        //{
                        // TMP_Action action = TMP_Action.GetAction(m_XmlAttribute[0].valueHashCode);
                        //}
                        return true;
                    case MarkupTag.SLASH_ACTION:
                        if (m_isTextLayoutPhase)
                        {
                            Debug.Log("Action ID: [" + m_ActionStack.CurrentItem() + "] Last character index: " + (m_CharacterCount - 1));
                        }

                        m_ActionStack.Remove();
                        return true;
                    case MarkupTag.SCALE:
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        m_FXScale = new Vector3(value, 1, 1);

                        return true;
                    case MarkupTag.SLASH_SCALE:
                        m_FXScale = Vector3.one;
                        return true;
                    case MarkupTag.ROTATE:
                        // TODO: Add attribute to provide for ability to use Random Rotation
                        value = TextGeneratorUtilities.ConvertToFloat(m_HtmlTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        // Reject tag if value is invalid.
                        if (value == Int16.MinValue) return false;

                        m_FXRotation = Quaternion.Euler(0, 0, value);

                        return true;
                    case MarkupTag.SLASH_ROTATE:
                        m_FXRotation = Quaternion.identity;
                        return true;
                    case MarkupTag.TABLE:
                        //switch (m_XmlAttribute[1].nameHashCode)
                        //{
                        //    case 327550: // width
                        //        float tableWidth = ConvertToFloat(m_HtmlTag, m_XmlAttribute[1].valueStartIndex, m_XmlAttribute[1].valueLength);

                        //        // Reject tag if value is invalid.
                        //        if (tableWidth == Int16.MinValue) return false;

                        //        switch (tagUnitType)
                        //        {
                        //            case TagUnitType.Pixels:
                        //                Debug.Log("Table width = " + tableWidth + "px.");
                        //                break;
                        //            case TagUnitType.FontUnits:
                        //                Debug.Log("Table width = " + tableWidth + "em.");
                        //                break;
                        //            case TagUnitType.Percentage:
                        //                Debug.Log("Table width = " + tableWidth + "%.");
                        //                break;
                        //        }
                        //        break;
                        //}
                        return false;
                    case MarkupTag.SLASH_TABLE:
                        return false;
                    case MarkupTag.TR:
                        return false;
                    case MarkupTag.SLASH_TR:
                        return false;
                    case MarkupTag.TH:
                        // Set style to bold and center alignment
                        return false;
                    case MarkupTag.SLASH_TH:
                        return false;
                    case MarkupTag.TD:
                        // Style options
                        //for (int i = 1; i < m_XmlAttribute.Length && m_XmlAttribute[i].nameHashCode != 0; i++)
                        //{
                        //    switch (m_XmlAttribute[i].nameHashCode)
                        //    {
                        //        case 327550: // width
                        //            float tableWidth = ConvertToFloat(m_HtmlTag, m_XmlAttribute[i].valueStartIndex, m_XmlAttribute[i].valueLength);

                        //            switch (tagUnitType)
                        //            {
                        //                case TagUnitType.Pixels:
                        //                    Debug.Log("Table width = " + tableWidth + "px.");
                        //                    break;
                        //                case TagUnitType.FontUnits:
                        //                    Debug.Log("Table width = " + tableWidth + "em.");
                        //                    break;
                        //                case TagUnitType.Percentage:
                        //                    Debug.Log("Table width = " + tableWidth + "%.");
                        //                    break;
                        //            }
                        //            break;
                        //        case 275917: // align
                        //            switch (m_XmlAttribute[i].valueHashCode)
                        //            {
                        //                case 3774683: // left
                        //                    Debug.Log("TD align=\"left\".");
                        //                    break;
                        //                case 136703040: // right
                        //                    Debug.Log("TD align=\"right\".");
                        //                    break;
                        //                case -458210101: // center
                        //                    Debug.Log("TD align=\"center\".");
                        //                    break;
                        //                case -523808257: // justified
                        //                    Debug.Log("TD align=\"justified\".");
                        //                    break;
                        //            }
                        //            break;
                        //    }
                        //}

                        return false;
                    case MarkupTag.SLASH_TD:
                        return false;
                }
            }
            #endregion

            return false;
        }

        /// <summary>
        /// Store vertex information for each character.
        /// </summary>
        /// <param name="padding"></param>
        /// <param name="stylePadding">stylePadding.</param>
        /// <param name="vertexColor">Vertex color.</param>
        /// <param name="generationSettings"></param>
        /// <param name="textInfo"></param>
        void SaveGlyphVertexInfo(float padding, float stylePadding, Color32 vertexColor, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            #region Setup Mesh Vertices
            // Save the Vertex Position for the Character
            textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.position = textInfo.textElementInfo[m_CharacterCount].bottomLeft;
            textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.position = textInfo.textElementInfo[m_CharacterCount].topLeft;
            textInfo.textElementInfo[m_CharacterCount].vertexTopRight.position = textInfo.textElementInfo[m_CharacterCount].topRight;
            textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.position = textInfo.textElementInfo[m_CharacterCount].bottomRight;
            #endregion


            #region Setup Vertex Colors
            // Alpha is the lower of the vertex color or tag color alpha used.
            vertexColor.a = m_FontColor32.a < vertexColor.a ? m_FontColor32.a : vertexColor.a;

            bool isColorGlyph = false;

            // Handle Vertex Colors & Vertex Color Gradient
            if (generationSettings.fontColorGradient == null || isColorGlyph)
            {
                // Special handling for color glyphs
                vertexColor = isColorGlyph ? new Color32(255, 255, 255, vertexColor.a) : vertexColor;

                textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.color = vertexColor;
                textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.color = vertexColor;
                textInfo.textElementInfo[m_CharacterCount].vertexTopRight.color = vertexColor;
                textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.color = vertexColor;
            }
            else
            {
                if (!generationSettings.overrideRichTextColors && m_ColorStack.index > 1)
                {
                    textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.color = vertexColor;
                    textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.color = vertexColor;
                    textInfo.textElementInfo[m_CharacterCount].vertexTopRight.color = vertexColor;
                    textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.color = vertexColor;
                }
                else // Handle Vertex Color Gradient
                {
                    // Use Vertex Color Gradient Preset (if one is assigned)
                    if (generationSettings.fontColorGradientPreset != null)
                    {
                        textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.color = generationSettings.fontColorGradientPreset.bottomLeft * vertexColor;
                        textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.color = generationSettings.fontColorGradientPreset.topLeft * vertexColor;
                        textInfo.textElementInfo[m_CharacterCount].vertexTopRight.color = generationSettings.fontColorGradientPreset.topRight * vertexColor;
                        textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.color = generationSettings.fontColorGradientPreset.bottomRight * vertexColor;
                    }
                    else
                    {
                        textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.color = generationSettings.fontColorGradient.bottomLeft * vertexColor;
                        textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.color = generationSettings.fontColorGradient.topLeft * vertexColor;
                        textInfo.textElementInfo[m_CharacterCount].vertexTopRight.color = generationSettings.fontColorGradient.topRight * vertexColor;
                        textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.color = generationSettings.fontColorGradient.bottomRight * vertexColor;
                    }
                }
            }

            if (m_ColorGradientPreset != null && !isColorGlyph)
            {
                if (m_ColorGradientPresetIsTinted)
                {
                    textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.color *= m_ColorGradientPreset.bottomLeft;
                    textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.color *= m_ColorGradientPreset.topLeft;
                    textInfo.textElementInfo[m_CharacterCount].vertexTopRight.color *= m_ColorGradientPreset.topRight;
                    textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.color *= m_ColorGradientPreset.bottomRight;
                }
                else
                {
                    textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.color = TextGeneratorUtilities.MinAlpha(m_ColorGradientPreset.bottomLeft, vertexColor);
                    textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.color = TextGeneratorUtilities.MinAlpha(m_ColorGradientPreset.topLeft, vertexColor);
                    textInfo.textElementInfo[m_CharacterCount].vertexTopRight.color = TextGeneratorUtilities.MinAlpha(m_ColorGradientPreset.topRight, vertexColor);
                    textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.color = TextGeneratorUtilities.MinAlpha(m_ColorGradientPreset.bottomRight, vertexColor);
                }
            }
            #endregion

            // Apply stylePadding only if this is a SDF Shader.
            if (!m_IsSdfShader)
                stylePadding = 0;

            // Setup UVs for the Character
            #region Setup UVs
            Glyph altGlyph = textInfo.textElementInfo[m_CharacterCount].alternativeGlyph;
            GlyphRect glyphRect = altGlyph == null ? m_CachedTextElement.m_Glyph.glyphRect : altGlyph.glyphRect;

            Vector2 uv0;
            uv0.x = (glyphRect.x - padding - stylePadding) / m_CurrentFontAsset.atlasWidth;
            uv0.y = (glyphRect.y - padding - stylePadding) / m_CurrentFontAsset.atlasHeight;

            Vector2 uv1;
            uv1.x = uv0.x;
            uv1.y = (glyphRect.y + padding + stylePadding + glyphRect.height) / m_CurrentFontAsset.atlasHeight;

            Vector2 uv2;
            uv2.x = (glyphRect.x + padding + stylePadding + glyphRect.width) / m_CurrentFontAsset.atlasWidth;
            uv2.y = uv1.y;

            Vector2 uv3;
            uv3.x = uv2.x;
            uv3.y = uv0.y;

            // Store UV Information
            textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.uv = uv0;
            textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.uv = uv1;
            textInfo.textElementInfo[m_CharacterCount].vertexTopRight.uv = uv2;
            textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.uv = uv3;
            #endregion Setup UVs
        }

        /// <summary>
        /// Store vertex information for each sprite.
        /// </summary>
        /// <param name="vertexColor"></param>
        /// <param name="generationSettings"></param>
        /// <param name="textInfo"></param>
        void SaveSpriteVertexInfo(Color32 vertexColor, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            // Save the Vertex Position for the Character
            #region Setup Mesh Vertices
            textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.position = textInfo.textElementInfo[m_CharacterCount].bottomLeft;
            textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.position = textInfo.textElementInfo[m_CharacterCount].topLeft;
            textInfo.textElementInfo[m_CharacterCount].vertexTopRight.position = textInfo.textElementInfo[m_CharacterCount].topRight;
            textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.position = textInfo.textElementInfo[m_CharacterCount].bottomRight;
            #endregion

            // Vertex Color Alpha
            if (generationSettings.tintSprites)
                m_TintSprite = true;

            Color32 spriteColor = m_TintSprite ? ColorUtilities.MultiplyColors(m_SpriteColor, vertexColor) : m_SpriteColor;
            spriteColor.a = spriteColor.a < m_FontColor32.a ? spriteColor.a < vertexColor.a ? spriteColor.a : vertexColor.a : m_FontColor32.a;

            Color32 c0 = spriteColor;
            Color32 c1 = spriteColor;
            Color32 c2 = spriteColor;
            Color32 c3 = spriteColor;

            if (generationSettings.fontColorGradient != null)
            {
                if (generationSettings.fontColorGradientPreset != null) {
                    c0 = m_TintSprite ? ColorUtilities.MultiplyColors(c0, generationSettings.fontColorGradientPreset.bottomLeft) : c0;
                    c1 = m_TintSprite ? ColorUtilities.MultiplyColors(c1, generationSettings.fontColorGradientPreset.topLeft) : c1;
                    c2 = m_TintSprite ? ColorUtilities.MultiplyColors(c2, generationSettings.fontColorGradientPreset.topRight) : c2;
                    c3 = m_TintSprite ? ColorUtilities.MultiplyColors(c3, generationSettings.fontColorGradientPreset.bottomRight) : c3;
                }
                else
                {
                    c0 = m_TintSprite ? ColorUtilities.MultiplyColors(c0, generationSettings.fontColorGradient.bottomLeft) : c0;
                    c1 = m_TintSprite ? ColorUtilities.MultiplyColors(c1, generationSettings.fontColorGradient.topLeft) : c1;
                    c2 = m_TintSprite ? ColorUtilities.MultiplyColors(c2, generationSettings.fontColorGradient.topRight) : c2;
                    c3 = m_TintSprite ? ColorUtilities.MultiplyColors(c3, generationSettings.fontColorGradient.bottomRight) : c3;
                }
            }

            if (m_ColorGradientPreset != null)
            {
                c0 = m_TintSprite ? ColorUtilities.MultiplyColors(c0, m_ColorGradientPreset.bottomLeft) : c0;
                c1 = m_TintSprite ? ColorUtilities.MultiplyColors(c1, m_ColorGradientPreset.topLeft) : c1;
                c2 = m_TintSprite ? ColorUtilities.MultiplyColors(c2, m_ColorGradientPreset.topRight) : c2;
                c3 = m_TintSprite ? ColorUtilities.MultiplyColors(c3, m_ColorGradientPreset.bottomRight) : c3;
            }

            textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.color = c0;
            textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.color = c1;
            textInfo.textElementInfo[m_CharacterCount].vertexTopRight.color = c2;
            textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.color = c3;

            // Setup UVs for the Character
            #region Setup UVs
            Vector2 uv0 = new Vector2((float)m_CachedTextElement.glyph.glyphRect.x / m_CurrentSpriteAsset.spriteSheet.width, (float)m_CachedTextElement.glyph.glyphRect.y / m_CurrentSpriteAsset.spriteSheet.height); // bottom left
            Vector2 uv1 = new Vector2(uv0.x, (float)(m_CachedTextElement.glyph.glyphRect.y + m_CachedTextElement.glyph.glyphRect.height) / m_CurrentSpriteAsset.spriteSheet.height); // top left
            Vector2 uv2 = new Vector2((float)(m_CachedTextElement.glyph.glyphRect.x + m_CachedTextElement.glyph.glyphRect.width) / m_CurrentSpriteAsset.spriteSheet.width, uv1.y); // top right
            Vector2 uv3 = new Vector2(uv2.x, uv0.y); // bottom right

            // Store UV Information
            textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.uv = uv0;
            textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.uv = uv1;
            textInfo.textElementInfo[m_CharacterCount].vertexTopRight.uv = uv2;
            textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.uv = uv3;
            #endregion Setup UVs
        }

        /// <summary>
        /// Method to add the underline geometry.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="index"></param>
        /// <param name="startScale"></param>
        /// <param name="endScale"></param>
        /// <param name="maxScale"></param>
        /// <param name="sdfScale"></param>
        /// <param name="underlineColor"></param>
        /// <param name="generationSettings"></param>
        /// <param name="textInfo"></param>
        void DrawUnderlineMesh(Vector3 start, Vector3 end, float startScale, float endScale, float maxScale, float sdfScale, Color32 underlineColor, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            // Get Underline special character from the primary font asset.
            GetUnderlineSpecialCharacter(generationSettings);

            if (m_Underline.character == null)
            {
                if (generationSettings.textSettings.displayWarnings)
                    Debug.LogWarning("Unable to add underline or strikethrough since the character [0x5F] used by these features is not present in the Font Asset assigned to this text object.");
                return;
            }

            const int k_VertexIncrease = 12;
            int index = textInfo.meshInfo[m_CurrentMaterialIndex].vertexCount;
            int newVerticesCount = index + k_VertexIncrease;

            // Check to make sure our current mesh buffer allocations can hold these new Quads.
            if (newVerticesCount > textInfo.meshInfo[m_CurrentMaterialIndex].vertices.Length)
            {
                // Resize Mesh Buffers
                textInfo.meshInfo[m_CurrentMaterialIndex].ResizeMeshInfo(newVerticesCount / 4);
            }

            // Adjust the position of the underline based on the lowest character. This matters for subscript character.
            start.y = Mathf.Min(start.y, end.y);
            end.y = Mathf.Min(start.y, end.y);

            GlyphMetrics underlineGlyphMetrics = m_Underline.character.glyph.metrics;
            GlyphRect underlineGlyphRect = m_Underline.character.glyph.glyphRect;

            float segmentWidth = underlineGlyphMetrics.width / 2 * maxScale;

            if (end.x - start.x < underlineGlyphMetrics.width * maxScale)
            {
                segmentWidth = (end.x - start.x) / 2f;
            }

            float startPadding = m_Padding * startScale / maxScale;
            float endPadding = m_Padding * endScale / maxScale;

            float underlineThickness = m_Underline.fontAsset.faceInfo.underlineThickness;

            // UNDERLINE VERTICES FOR (3) LINE SEGMENTS
            #region UNDERLINE VERTICES
            Vector3[] vertices = textInfo.meshInfo[m_CurrentMaterialIndex].vertices;

            // Front Part of the Underline
            vertices[index + 0] = start + new Vector3(0, 0 - (underlineThickness + m_Padding) * maxScale, 0); // BL
            vertices[index + 1] = start + new Vector3(0, m_Padding * maxScale, 0); // TL
            vertices[index + 2] = vertices[index + 1] + new Vector3(segmentWidth, 0, 0); // TR
            vertices[index + 3] = vertices[index + 0] + new Vector3(segmentWidth, 0, 0); // BR

            // Middle Part of the Underline
            vertices[index + 4] = vertices[index + 3]; // BL
            vertices[index + 5] = vertices[index + 2]; // TL
            vertices[index + 6] = end + new Vector3(-segmentWidth, m_Padding * maxScale, 0); // TR
            vertices[index + 7] = end + new Vector3(-segmentWidth, -(underlineThickness + m_Padding) * maxScale, 0); // BR

            // End Part of the Underline
            vertices[index + 8] = vertices[index + 7]; // BL
            vertices[index + 9] = vertices[index + 6]; // TL
            vertices[index + 10] = end + new Vector3(0, m_Padding * maxScale, 0); // TR
            vertices[index + 11] = end + new Vector3(0, -(underlineThickness + m_Padding) * maxScale, 0); // BR
            #endregion

            // Handle potential axis inversion.
            if (generationSettings.inverseYAxis)
            {
                Vector3 axisOffset;
                axisOffset.x = 0;
                axisOffset.y = generationSettings.screenRect.y + generationSettings.screenRect.height;
                axisOffset.z = 0;

                //vertices[index + 0].x += axisOffset.x;
                vertices[index + 0].y = (vertices[index + 0].y * -1) + axisOffset.y;
                //vertices[index + 1].x += axisOffset.x;
                vertices[index + 1].y = (vertices[index + 1].y * -1) + axisOffset.y;
                //vertices[index + 2].x += axisOffset.x;
                vertices[index + 2].y = (vertices[index + 2].y * -1) + axisOffset.y;
                //vertices[index + 3].x += axisOffset.x;
                vertices[index + 3].y = (vertices[index + 3].y * -1) + axisOffset.y;

                //vertices[index + 4].x += axisOffset.x;
                vertices[index + 4].y = (vertices[index + 4].y * -1) + axisOffset.y;
                //vertices[index + 5].x += axisOffset.x;
                vertices[index + 5].y = (vertices[index + 5].y * -1) + axisOffset.y;
                //vertices[index + 6].x += axisOffset.x;
                vertices[index + 6].y = (vertices[index + 6].y * -1) + axisOffset.y;
                //vertices[index + 7].x += axisOffset.x;
                vertices[index + 7].y = (vertices[index + 7].y * -1) + axisOffset.y;

                //vertices[index + 8].x += axisOffset.x;
                vertices[index + 8].y = (vertices[index + 8].y * -1) + axisOffset.y;
                //vertices[index + 9].x += axisOffset.x;
                vertices[index + 9].y = (vertices[index + 9].y * -1) + axisOffset.y;
                //vertices[index + 10].x += axisOffset.x;
                vertices[index + 10].y = (vertices[index + 10].y * -1) + axisOffset.y;
                //vertices[index + 11].x += axisOffset.x;
                vertices[index + 11].y = (vertices[index + 11].y * -1) + axisOffset.y;
            }

            // UNDERLINE UV0
            #region HANDLE UV0
            Vector4[] uvs0 = textInfo.meshInfo[m_CurrentMaterialIndex].uvs0;

            int atlasWidth = m_Underline.fontAsset.atlasWidth;
            int atlasHeight = m_Underline.fontAsset.atlasHeight;

            float xScale = Mathf.Abs(sdfScale);

            // Calculate UV required to setup the 3 Quads for the Underline.
            Vector4 uv0 = new Vector4((underlineGlyphRect.x - startPadding) / atlasWidth, (underlineGlyphRect.y - m_Padding) / atlasHeight, 0, xScale);  // bottom left
            Vector4 uv1 = new Vector4(uv0.x, (underlineGlyphRect.y + underlineGlyphRect.height + m_Padding) / atlasHeight, 0, xScale);  // top left
            Vector4 uv2 = new Vector4((underlineGlyphRect.x - startPadding + (float)underlineGlyphRect.width / 2) / atlasWidth, uv1.y, 0, xScale); // Mid Top Left
            Vector4 uv3 = new Vector4(uv2.x, uv0.y, 0, xScale); // Mid Bottom Left
            Vector4 uv4 = new Vector4((underlineGlyphRect.x + endPadding + (float)underlineGlyphRect.width / 2) / atlasWidth, uv1.y, 0, xScale); // Mid Top Right
            Vector4 uv5 = new Vector4(uv4.x, uv0.y, 0, xScale); // Mid Bottom right
            Vector4 uv6 = new Vector4((underlineGlyphRect.x + endPadding + underlineGlyphRect.width) / atlasWidth, uv1.y, 0, xScale); // End Part - Bottom Right
            Vector4 uv7 = new Vector4(uv6.x, uv0.y, 0, xScale); // End Part - Top Right

            // Left Part of the Underline
            uvs0[0 + index] = uv0; // BL
            uvs0[1 + index] = uv1; // TL
            uvs0[2 + index] = uv2; // TR
            uvs0[3 + index] = uv3; // BR

            // Middle Part of the Underline
            uvs0[4 + index] = new Vector4(uv2.x - uv2.x * 0.001f, uv0.y, 0, xScale);
            uvs0[5 + index] = new Vector4(uv2.x - uv2.x * 0.001f, uv1.y, 0, xScale);
            uvs0[6 + index] = new Vector4(uv2.x + uv2.x * 0.001f, uv1.y, 0, xScale);
            uvs0[7 + index] = new Vector4(uv2.x + uv2.x * 0.001f, uv0.y, 0, xScale);

            // Right Part of the Underline
            uvs0[8 + index] = uv5;
            uvs0[9 + index] = uv4;
            uvs0[10 + index] = uv6;
            uvs0[11 + index] = uv7;
            #endregion

            // UNDERLINE UV2
            #region HANDLE UV2 - SDF SCALE
            // UV1 contains Face / Border UV layout.
            float minUvX = 0;
            float maxUvX = (vertices[index + 2].x - start.x) / (end.x - start.x);

            Vector2[] uvs2 = textInfo.meshInfo[m_CurrentMaterialIndex].uvs2;

            uvs2[0 + index] = TextGeneratorUtilities.PackUV(0, 0, xScale);
            uvs2[1 + index] = TextGeneratorUtilities.PackUV(0, 1, xScale);
            uvs2[2 + index] = TextGeneratorUtilities.PackUV(maxUvX, 1, xScale);
            uvs2[3 + index] = TextGeneratorUtilities.PackUV(maxUvX, 0, xScale);

            minUvX = (vertices[index + 4].x - start.x) / (end.x - start.x);
            maxUvX = (vertices[index + 6].x - start.x) / (end.x - start.x);

            uvs2[4 + index] = TextGeneratorUtilities.PackUV(minUvX, 0, xScale);
            uvs2[5 + index] = TextGeneratorUtilities.PackUV(minUvX, 1, xScale);
            uvs2[6 + index] = TextGeneratorUtilities.PackUV(maxUvX, 1, xScale);
            uvs2[7 + index] = TextGeneratorUtilities.PackUV(maxUvX, 0, xScale);

            minUvX = (vertices[index + 8].x - start.x) / (end.x - start.x);

            uvs2[8 + index] = TextGeneratorUtilities.PackUV(minUvX, 0, xScale);
            uvs2[9 + index] = TextGeneratorUtilities.PackUV(minUvX, 1, xScale);
            uvs2[10 + index] = TextGeneratorUtilities.PackUV(1, 1, xScale);
            uvs2[11 + index] = TextGeneratorUtilities.PackUV(1, 0, xScale);
            #endregion

            // UNDERLINE VERTEX COLORS
            #region UNDERLINE VERTEX COLORS
            // Alpha is the lower of the vertex color or tag color alpha used.
            underlineColor.a = m_FontColor32.a < underlineColor.a ? m_FontColor32.a : underlineColor.a;

            Color32[] colors32 = textInfo.meshInfo[m_CurrentMaterialIndex].colors32;
            colors32[0 + index] = underlineColor;
            colors32[1 + index] = underlineColor;
            colors32[2 + index] = underlineColor;
            colors32[3 + index] = underlineColor;

            colors32[4 + index] = underlineColor;
            colors32[5 + index] = underlineColor;
            colors32[6 + index] = underlineColor;
            colors32[7 + index] = underlineColor;

            colors32[8 + index] = underlineColor;
            colors32[9 + index] = underlineColor;
            colors32[10 + index] = underlineColor;
            colors32[11 + index] = underlineColor;
            #endregion

            textInfo.meshInfo[m_CurrentMaterialIndex].vertexCount += k_VertexIncrease;
        }

        void DrawTextHighlight(Vector3 start, Vector3 end, Color32 highlightColor, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            GetUnderlineSpecialCharacter(generationSettings);

            if (m_Underline.character == null)
            {
                if (generationSettings.textSettings.displayWarnings)
                    Debug.LogWarning("Unable to add highlight since the primary Font Asset doesn't contain the underline character.");

                return;
            }

            const int k_VertexIncrease = 4;
            int index = textInfo.meshInfo[m_CurrentMaterialIndex].vertexCount;
            int newVerticesCount = index + k_VertexIncrease;

            // Check to make sure our current mesh buffer allocations can hold these new Quads.
            if (newVerticesCount > textInfo.meshInfo[m_CurrentMaterialIndex].vertices.Length)
            {
                // Resize Mesh Buffers
                textInfo.meshInfo[m_CurrentMaterialIndex].ResizeMeshInfo(newVerticesCount / 4);
            }

            // UNDERLINE VERTICES FOR (3) LINE SEGMENTS
            #region HIGHLIGHT VERTICES
            Vector3[] vertices = textInfo.meshInfo[m_CurrentMaterialIndex].vertices;

            // Front Part of the Underline
            vertices[index + 0] = start; // BL
            vertices[index + 1] = new Vector3(start.x, end.y, 0); // TL
            vertices[index + 2] = end; // TR
            vertices[index + 3] = new Vector3(end.x, start.y, 0); // BR
            #endregion

            if (generationSettings.inverseYAxis)
            {
                Vector3 axisOffset;
                axisOffset.x = 0;
                axisOffset.y = generationSettings.screenRect.y + generationSettings.screenRect.height;
                axisOffset.z = 0;

                //vertices[index + 0].x += axisOffset.x;
                vertices[index + 0].y = (vertices[index + 0].y * -1) + axisOffset.y;
                //vertices[index + 1].x += axisOffset.x;
                vertices[index + 1].y = (vertices[index + 1].y * -1) + axisOffset.y;
                //vertices[index + 2].x += axisOffset.x;
                vertices[index + 2].y = (vertices[index + 2].y * -1) + axisOffset.y;
                //vertices[index + 3].x += axisOffset.x;
                vertices[index + 3].y = (vertices[index + 3].y * -1) + axisOffset.y;
            }

            // UNDERLINE UV0
            #region HANDLE UV0
            Vector4[] uvs0 = textInfo.meshInfo[m_CurrentMaterialIndex].uvs0;

            int atlasWidth = m_Underline.fontAsset.atlasWidth;
            int atlasHeight = m_Underline.fontAsset.atlasHeight;
            GlyphRect glyphRect = m_Underline.character.glyph.glyphRect;

            // Calculate UV
            Vector2 uvGlyphCenter = new Vector2((glyphRect.x + (float)glyphRect.width / 2) / atlasWidth, (glyphRect.y + (float)glyphRect.height / 2) / atlasHeight);
            Vector2 uvTexelSize = new Vector2(1.0f / atlasWidth, 1.0f / atlasHeight);

            // UVs for the Quad
            uvs0[0 + index] = uvGlyphCenter - uvTexelSize; // BL
            uvs0[1 + index] = uvGlyphCenter + new Vector2(-uvTexelSize.x, uvTexelSize.y); // TL
            uvs0[2 + index] = uvGlyphCenter + uvTexelSize; // TR
            uvs0[3 + index] = uvGlyphCenter + new Vector2(uvTexelSize.x, -uvTexelSize.y); // BR
            #endregion

            // HIGHLIGHT UV2
            #region HANDLE UV2 - SDF SCALE
            Vector2[] uvs2 = textInfo.meshInfo[m_CurrentMaterialIndex].uvs2;
            Vector2 customUV = new Vector2(0, 1);
            uvs2[0 + index] = customUV;
            uvs2[1 + index] = customUV;
            uvs2[2 + index] = customUV;
            uvs2[3 + index] = customUV;
            #endregion

            // HIGHLIGHT VERTEX COLORS
            #region HIGHLIGHT VERTEX COLORS
            // Alpha is the lower of the vertex color or tag color alpha used.
            highlightColor.a = m_FontColor32.a < highlightColor.a ? m_FontColor32.a : highlightColor.a;

            Color32[] colors32 = textInfo.meshInfo[m_CurrentMaterialIndex].colors32;
            colors32[0 + index] = highlightColor;
            colors32[1 + index] = highlightColor;
            colors32[2 + index] = highlightColor;
            colors32[3 + index] = highlightColor;
            #endregion

            textInfo.meshInfo[m_CurrentMaterialIndex].vertexCount += k_VertexIncrease;
        }

        /// <summary>
        /// Function to clear the geometry of the Primary and Sub Text objects.
        /// </summary>
        static void ClearMesh(bool updateMesh, TextInfo textInfo)
        {
            textInfo.ClearMeshInfo(updateMesh);
        }

        void EnableMasking()
        {
            m_IsMaskingEnabled = true;
        }

        // Enable Masking in the Shader
        void DisableMasking()
        {
            m_IsMaskingEnabled = false;
        }

        internal int SetArraySizes(TextProcessingElement[] textProcessingArray, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            TextSettings textSettings = generationSettings.textSettings;

            int spriteCount = 0;

            m_TotalCharacterCount = 0;
            m_IsUsingBold = false;
            m_isTextLayoutPhase = false;
            m_TagNoParsing = false;
            m_FontStyleInternal = generationSettings.fontStyle;
            m_FontStyleStack.Clear();

            m_FontWeightInternal = (m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold ? TextFontWeight.Bold : generationSettings.fontWeight;
            m_FontWeightStack.SetDefault(m_FontWeightInternal);

            m_CurrentFontAsset = generationSettings.fontAsset;
            m_CurrentMaterial = generationSettings.material;
            m_CurrentMaterialIndex = 0;

            m_MaterialReferenceStack.SetDefault(new MaterialReference(m_CurrentMaterialIndex, m_CurrentFontAsset, null, m_CurrentMaterial, m_Padding));

            m_MaterialReferenceIndexLookup.Clear();
            MaterialReference.AddMaterialReference(m_CurrentMaterial, m_CurrentFontAsset, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);

            if (textInfo == null)
                textInfo = new TextInfo();
            else if (textInfo.textElementInfo.Length < m_InternalTextProcessingArraySize)
                TextInfo.Resize(ref textInfo.textElementInfo, m_InternalTextProcessingArraySize, false);

            m_TextElementType = TextElementType.Character;

            // Handling for Underline special character
            #region Setup Underline Special Character
            /*
            GetUnderlineSpecialCharacter(m_CurrentFontAsset);
            if (m_Underline.character != null)
            {
                if (m_Underline.fontAsset.GetInstanceID() != m_CurrentFontAsset.GetInstanceID())
                {
                    if (generationSettings.textSettings.matchMaterialPreset && m_CurrentMaterial.GetInstanceID() != m_Underline.fontAsset.material.GetInstanceID())
                        m_Underline.material = TMP_MaterialManager.GetFallbackMaterial(m_CurrentMaterial, m_Underline.fontAsset.material);
                    else
                        m_Underline.material = m_Underline.fontAsset.material;

                    m_Underline.materialIndex = MaterialReference.AddMaterialReference(m_Underline.material, m_Underline.fontAsset, m_MaterialReferences, m_MaterialReferenceIndexLookup);
                    m_MaterialReferences[m_Underline.materialIndex].referenceCount = 0;
                }
            }
            */
            #endregion


            // Handling for Ellipsis special character
            #region Setup Ellipsis Special Character
            if (generationSettings.overflowMode == TextOverflowMode.Ellipsis)
            {
                GetEllipsisSpecialCharacter(generationSettings);

                if (m_Ellipsis.character != null)
                {
                    if (m_Ellipsis.fontAsset.GetInstanceID() != m_CurrentFontAsset.GetInstanceID())
                    {
                        if (textSettings.matchMaterialPreset && m_CurrentMaterial.GetInstanceID() != m_Ellipsis.fontAsset.material.GetInstanceID())
                            m_Ellipsis.material = MaterialManager.GetFallbackMaterial(m_CurrentMaterial, m_Ellipsis.fontAsset.material);
                        else
                            m_Ellipsis.material = m_Ellipsis.fontAsset.material;

                        m_Ellipsis.materialIndex = MaterialReference.AddMaterialReference(m_Ellipsis.material, m_Ellipsis.fontAsset, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);
                        m_MaterialReferences[m_Ellipsis.materialIndex].referenceCount = 0;
                    }
                }
                else
                {
                    generationSettings.overflowMode = TextOverflowMode.Truncate;

                    if (textSettings.displayWarnings)
                        Debug.LogWarning("The character used for Ellipsis is not available in font asset [" + m_CurrentFontAsset.name + "] or any potential fallbacks. Switching Text Overflow mode to Truncate.");
                }
            }
            #endregion

            // Clear Linked Text object if we have one
            //if (generationSettings.overflowMode == TextOverflowMode.Linked && m_linkedTextComponent != null && !m_IsCalculatingPreferredValues)
            //    m_linkedTextComponent.text = string.Empty;

            // Parsing XML tags in the text
            for (int i = 0; i < textProcessingArray.Length && textProcessingArray[i].unicode != 0; i++)
            {
                //Make sure the characterInfo array can hold the next text element.
                if (textInfo.textElementInfo == null || m_TotalCharacterCount >= textInfo.textElementInfo.Length)
                    TextInfo.Resize(ref textInfo.textElementInfo, m_TotalCharacterCount + 1, true);

                uint unicode = textProcessingArray[i].unicode;
                int prevMaterialIndex = m_CurrentMaterialIndex;

                // PARSE XML TAGS
                #region PARSE XML TAGS
                if (generationSettings.richText && unicode == '<')
                {
                    prevMaterialIndex = m_CurrentMaterialIndex;
                    int endTagIndex;

                    // Check if Tag is Valid
                    if (ValidateHtmlTag(textProcessingArray, i + 1, out endTagIndex, generationSettings, textInfo))
                    {
                        int tagStartIndex = textProcessingArray[i].stringIndex;
                        i = endTagIndex;

                        if ((m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold)
                            m_IsUsingBold = true;

                        if (m_TextElementType == TextElementType.Sprite)
                        {
                            m_MaterialReferences[m_CurrentMaterialIndex].referenceCount += 1;


                            textInfo.textElementInfo[m_TotalCharacterCount].character = (char)(57344 + m_SpriteIndex);
                            textInfo.textElementInfo[m_TotalCharacterCount].fontAsset = m_CurrentFontAsset;
                            textInfo.textElementInfo[m_TotalCharacterCount].materialReferenceIndex = m_CurrentMaterialIndex;
                            textInfo.textElementInfo[m_TotalCharacterCount].textElement = m_CurrentSpriteAsset.spriteCharacterTable[m_SpriteIndex];
                            textInfo.textElementInfo[m_TotalCharacterCount].elementType = m_TextElementType;
                            textInfo.textElementInfo[m_TotalCharacterCount].index = tagStartIndex;
                            textInfo.textElementInfo[m_TotalCharacterCount].stringLength = textProcessingArray[i].stringIndex - tagStartIndex + 1;

                            // Restore element type and material index to previous values.
                            m_TextElementType = TextElementType.Character;
                            m_CurrentMaterialIndex = prevMaterialIndex;

                            spriteCount += 1;
                            m_TotalCharacterCount += 1;
                        }

                        continue;
                    }
                }
                #endregion

                bool isUsingAlternativeTypeface;
                bool isUsingFallbackOrAlternativeTypeface = false;

                FontAsset prevFontAsset = m_CurrentFontAsset;
                Material prevMaterial = m_CurrentMaterial;
                prevMaterialIndex = m_CurrentMaterialIndex;

                // Handle Font Styles like LowerCase, UpperCase and SmallCaps.
                #region Handling of LowerCase, UpperCase and SmallCaps Font Styles
                if (m_TextElementType == TextElementType.Character)
                {
                    if ((m_FontStyleInternal & FontStyles.UpperCase) == FontStyles.UpperCase)
                    {
                        // If this character is lowercase, switch to uppercase.
                        if (char.IsLower((char)unicode))
                            unicode = char.ToUpper((char)unicode);

                    }
                    else if ((m_FontStyleInternal & FontStyles.LowerCase) == FontStyles.LowerCase)
                    {
                        // If this character is uppercase, switch to lowercase.
                        if (char.IsUpper((char)unicode))
                            unicode = char.ToLower((char)unicode);
                    }
                    else if ((m_FontStyleInternal & FontStyles.SmallCaps) == FontStyles.SmallCaps)
                    {
                        // Only convert lowercase characters to uppercase.
                        if (char.IsLower((char)unicode))
                            unicode = char.ToUpper((char)unicode);
                    }
                }
                #endregion

                // Lookup the Glyph data for each character and cache it.
                #region LOOKUP GLYPH
                TextElement character = GetTextElement(generationSettings, (uint)unicode, m_CurrentFontAsset, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);

                // Check if Lowercase or Uppercase variant of the character is available.
                /* Not sure this is necessary anyone as it is very unlikely with recursive search through fallback fonts.
                if (glyph == null)
                {
                    if (char.IsLower((char)c))
                    {
                        if (m_CurrentFontAsset.characterDictionary.TryGetValue(char.ToUpper((char)c), out glyph))
                            c = chars[i] = char.ToUpper((char)c);
                    }
                    else if (char.IsUpper((char)c))
                    {
                        if (m_CurrentFontAsset.characterDictionary.TryGetValue(char.ToLower((char)c), out glyph))
                            c = chars[i] = char.ToLower((char)c);
                    }
                }*/

                // Special handling for missing character.
                // Replace missing glyph by the Square (9633) glyph or possibly the Space (32) glyph.
                if (character == null)
                {
                    DoMissingGlyphCallback(unicode, textProcessingArray[i].stringIndex, m_CurrentFontAsset, textInfo);

                    // Save the original unicode character
                    uint srcGlyph = unicode;

                    // Try replacing the missing glyph character by the Settings file Missing Glyph or Square (9633) character.
                    unicode = textProcessingArray[i].unicode = (uint)textSettings.missingCharacterUnicode == 0 ? k_Square : (uint)textSettings.missingCharacterUnicode;

                    // Check for the missing glyph character in the currently assigned font asset and its fallbacks
                    character = FontAssetUtilities.GetCharacterFromFontAsset((uint)unicode, m_CurrentFontAsset, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);

                    if (character == null)
                    {
                        // Search for the missing glyph character in the Settings Fallback list.
                        if (textSettings.fallbackFontAssets != null && textSettings.fallbackFontAssets.Count > 0)
                            character = FontAssetUtilities.GetCharacterFromFontAssets((uint)unicode, m_CurrentFontAsset, textSettings.fallbackFontAssets, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);
                    }

                    if (character == null)
                    {
                        // Search for the missing glyph in the Settings Default Font Asset.
                        if (textSettings.defaultFontAsset != null)
                            character = FontAssetUtilities.GetCharacterFromFontAsset((uint)unicode, textSettings.defaultFontAsset, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);
                    }

                    if (character == null)
                    {
                        // Use Space (32) Glyph from the currently assigned font asset.
                        unicode = textProcessingArray[i].unicode = 32;
                        character = FontAssetUtilities.GetCharacterFromFontAsset((uint)unicode, m_CurrentFontAsset, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);
                    }

                    if (character == null)
                    {
                        // Use End of Text (0x03) Glyph from the currently assigned font asset.
                        unicode = textProcessingArray[i].unicode = k_EndOfText;
                        character = FontAssetUtilities.GetCharacterFromFontAsset((uint)unicode, m_CurrentFontAsset, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);
                    }

                    if (textSettings.displayWarnings)
                    {
                        string formattedWarning = srcGlyph > 0xFFFF
                            ? string.Format("The character with Unicode value \\U{0:X8} was not found in the [{1}] font asset or any potential fallbacks. It was replaced by Unicode character \\u{2:X4}.", srcGlyph, generationSettings.fontAsset.name, character.unicode)
                            : string.Format("The character with Unicode value \\u{0:X4} was not found in the [{1}] font asset or any potential fallbacks. It was replaced by Unicode character \\u{2:X4}.", srcGlyph, generationSettings.fontAsset.name, character.unicode);

                        Debug.LogWarning(formattedWarning);
                    }
                }

                textInfo.textElementInfo[m_TotalCharacterCount].alternativeGlyph = null;

                if (character.elementType == TextElementType.Character)
                {
                    if (character.textAsset.instanceID != m_CurrentFontAsset.instanceID)
                    {
                        isUsingFallbackOrAlternativeTypeface = true;
                        m_CurrentFontAsset = character.textAsset as FontAsset;
                    }
                    // Process potential glyph substitutions
                    if (m_CurrentFontAsset.fontFeatureTable.m_LigatureSubstitutionRecordLookup.TryGetValue(character.glyphIndex, out List<LigatureSubstitutionRecord> records))
                    {
                        if (records == null)
                            break;

                        for (int j = 0; j < records.Count; j++)
                        {
                            LigatureSubstitutionRecord record = records[j];

                            int componentCount = record.componentGlyphIDs.Length;
                            uint ligatureGlyphID = record.ligatureGlyphID;

                            //
                            for (int k = 1; k < componentCount; k++)
                            {
                                uint glyphIndex = m_CurrentFontAsset.GetGlyphIndex((uint)textProcessingArray[i + k].unicode);

                                if (glyphIndex == record.componentGlyphIDs[k])
                                    continue;

                                ligatureGlyphID = 0;
                                break;
                            }

                            if (ligatureGlyphID != 0)
                            {
                                if (m_CurrentFontAsset.TryAddGlyphInternal(ligatureGlyphID, out Glyph glyph))
                                {
                                    textInfo.textElementInfo[m_TotalCharacterCount].alternativeGlyph = glyph;

                                    // Update text processing array
                                    for (int c = 0; c < componentCount; c++)
                                    {
                                        if (c == 0)
                                        {
                                            textProcessingArray[i + c].length = componentCount;
                                            continue;
                                        }

                                        textProcessingArray[i + c].unicode = 0x1A;
                                    }

                                    i += componentCount - 1;
                                    break;
                                }
                            }
                        }
                    }

                }
                #endregion

                // Save text element data
                textInfo.textElementInfo[m_TotalCharacterCount].elementType = TextElementType.Character;
                textInfo.textElementInfo[m_TotalCharacterCount].textElement = character;
                textInfo.textElementInfo[m_TotalCharacterCount].isUsingAlternateTypeface = isUsingAlternativeTypeface;
                textInfo.textElementInfo[m_TotalCharacterCount].character = (char)unicode;
                textInfo.textElementInfo[m_TotalCharacterCount].index = textProcessingArray[i].stringIndex;
                textInfo.textElementInfo[m_TotalCharacterCount].stringLength = textProcessingArray[i].length;
                textInfo.textElementInfo[m_TotalCharacterCount].fontAsset = m_CurrentFontAsset;

                // Special handling if the character is a sprite.
                if (character.elementType == TextElementType.Sprite)
                {
                    SpriteAsset spriteAssetRef = character.textAsset as SpriteAsset;
                    m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(spriteAssetRef.material, spriteAssetRef, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);
                    m_MaterialReferences[m_CurrentMaterialIndex].referenceCount += 1;

                    textInfo.textElementInfo[m_TotalCharacterCount].elementType = TextElementType.Sprite;
                    textInfo.textElementInfo[m_TotalCharacterCount].materialReferenceIndex = m_CurrentMaterialIndex;

                    // Restore element type and material index to previous values.
                    m_TextElementType = TextElementType.Character;
                    m_CurrentMaterialIndex = prevMaterialIndex;

                    spriteCount += 1;
                    m_TotalCharacterCount += 1;

                    continue;
                }

                if (isUsingFallbackOrAlternativeTypeface && m_CurrentFontAsset.instanceID != generationSettings.fontAsset.instanceID)
                {
                    // Create Fallback material instance matching current material preset if necessary
                    if (textSettings.matchMaterialPreset)
                        m_CurrentMaterial = MaterialManager.GetFallbackMaterial(m_CurrentMaterial, m_CurrentFontAsset.material);
                    else
                        m_CurrentMaterial = m_CurrentFontAsset.material;

                    m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentMaterial, m_CurrentFontAsset, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);
                }

                // Handle Multi Atlas Texture support
                if (character != null && character.glyph.atlasIndex > 0)
                {
                    m_CurrentMaterial = MaterialManager.GetFallbackMaterial(m_CurrentFontAsset, m_CurrentMaterial, character.glyph.atlasIndex);

                    m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentMaterial, m_CurrentFontAsset, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);

                    isUsingFallbackOrAlternativeTypeface = true;
                }

                if (!char.IsWhiteSpace((char)unicode) && unicode != CodePoint.ZERO_WIDTH_SPACE)
                {
                    // Limit the mesh of the main text object to 65535 vertices and use sub objects for the overflow.
                    if (m_MaterialReferences[m_CurrentMaterialIndex].referenceCount < 16383)
                        m_MaterialReferences[m_CurrentMaterialIndex].referenceCount += 1;
                    else
                    {
                        m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(new Material(m_CurrentMaterial), m_CurrentFontAsset, ref m_MaterialReferences, m_MaterialReferenceIndexLookup);
                        m_MaterialReferences[m_CurrentMaterialIndex].referenceCount += 1;
                    }
                }

                textInfo.textElementInfo[m_TotalCharacterCount].material = m_CurrentMaterial;
                textInfo.textElementInfo[m_TotalCharacterCount].materialReferenceIndex = m_CurrentMaterialIndex;
                m_MaterialReferences[m_CurrentMaterialIndex].isFallbackMaterial = isUsingFallbackOrAlternativeTypeface;

                // Restore previous font asset and material if fallback font was used.
                if (isUsingFallbackOrAlternativeTypeface)
                {
                    m_MaterialReferences[m_CurrentMaterialIndex].fallbackMaterial = prevMaterial;
                    m_CurrentFontAsset = prevFontAsset;
                    m_CurrentMaterial = prevMaterial;
                    m_CurrentMaterialIndex = prevMaterialIndex;
                }

                m_TotalCharacterCount += 1;
            }

            // Early return if we are calculating the preferred values.
            if (m_IsCalculatingPreferredValues)
            {
                m_IsCalculatingPreferredValues = false;

                return m_TotalCharacterCount;
            }

            // Save material and sprite count.
            textInfo.spriteCount = spriteCount;
            int materialCount = textInfo.materialCount = m_MaterialReferenceIndexLookup.Count;

            // Check if we need to resize the MeshInfo array for handling different materials.
            if (materialCount > textInfo.meshInfo.Length)
                TextInfo.Resize(ref textInfo.meshInfo, materialCount, false);

            // Resize textElementInfo[] if allocations are excessive
            if (m_VertexBufferAutoSizeReduction && textInfo.textElementInfo.Length - m_TotalCharacterCount > 256)
                TextInfo.Resize(ref textInfo.textElementInfo, Mathf.Max(m_TotalCharacterCount + 1, 256), true);

            // Iterate through the material references to set the mesh buffer allocations
            for (int i = 0; i < materialCount; i++)
            {
                int referenceCount = m_MaterialReferences[i].referenceCount;

                // Check to make sure buffer allocations can accommodate the required text elements.
                if (textInfo.meshInfo[i].vertices == null || textInfo.meshInfo[i].vertices.Length < referenceCount * 4)
                {
                    if (textInfo.meshInfo[i].vertices == null)
                    {
                        textInfo.meshInfo[i] = new MeshInfo(referenceCount + 1);
                    }
                    else
                        textInfo.meshInfo[i].ResizeMeshInfo(referenceCount > 1024 ? referenceCount + 256 : Mathf.NextPowerOfTwo(referenceCount));
                }
                else if (textInfo.meshInfo[i].vertices.Length - referenceCount * 4 > 1024)
                {
                    // Resize vertex buffers if allocations are excessive.
                    textInfo.meshInfo[i].ResizeMeshInfo(referenceCount > 1024 ? referenceCount + 256 : Mathf.Max(Mathf.NextPowerOfTwo(referenceCount), 256));
                }

                // Assign material reference
                textInfo.meshInfo[i].material = m_MaterialReferences[i].material;
            }

            return m_TotalCharacterCount;
        }

        internal TextElement GetTextElement(TextGenerationSettings generationSettings, uint unicode, FontAsset fontAsset, FontStyles fontStyle, TextFontWeight fontWeight, out bool isUsingAlternativeTypeface)
        {
            //Debug.Log("Unicode: " + unicode.ToString("X8"));

            TextSettings textSettings = generationSettings.textSettings;
            // if (m_EmojiFallbackSupport && TextGeneratorUtilities.IsEmoji(unicode))
            // {
            //     if (TMP_Settings.emojiFallbackTextAssets != null && TMP_Settings.emojiFallbackTextAssets.Count > 0)
            //     {
            //         TMP_TextElement textElement = TMP_FontAssetUtilities.GetTextElementFromTextAssets(unicode, fontAsset, TMP_Settings.emojiFallbackTextAssets, true, fontStyle, fontWeight, out isUsingAlternativeTypeface);
            //
            //         if (textElement != null)
            //         {
            //             // Add character to font asset lookup cache
            //             //fontAsset.AddCharacterToLookupCache(unicode, character);
            //
            //             return textElement;
            //         }
            //     }
            // }

            Character character = FontAssetUtilities.GetCharacterFromFontAsset(unicode, fontAsset, false, fontStyle, fontWeight, out isUsingAlternativeTypeface);

            if (character != null)
                return character;

            // Search potential list of fallback font assets assigned to the font asset.
            if (fontAsset.m_FallbackFontAssetTable != null && fontAsset.m_FallbackFontAssetTable.Count > 0)
                character = FontAssetUtilities.GetCharacterFromFontAssets(unicode, fontAsset, fontAsset.m_FallbackFontAssetTable, true, fontStyle, fontWeight, out isUsingAlternativeTypeface);

            if (character != null)
            {
                // Add character to font asset lookup cache
                fontAsset.AddCharacterToLookupCache(unicode, character);

                return character;
            }

            // Search for the character in the primary font asset if not the current font asset
            if (fontAsset.instanceID != generationSettings.fontAsset.instanceID)
            {
                // Search primary font asset
                character = FontAssetUtilities.GetCharacterFromFontAsset(unicode, generationSettings.fontAsset, false, fontStyle, fontWeight, out isUsingAlternativeTypeface);

                // Use material and index of primary font asset.
                if (character != null)
                {
                    m_CurrentMaterialIndex = 0;
                    m_CurrentMaterial = m_MaterialReferences[0].material;

                    // Add character to font asset lookup cache
                    fontAsset.AddCharacterToLookupCache(unicode, character);

                    return character;
                }

                // Search list of potential fallback font assets assigned to the primary font asset.
                if (generationSettings.fontAsset.m_FallbackFontAssetTable != null && generationSettings.fontAsset.m_FallbackFontAssetTable.Count > 0)
                    character = FontAssetUtilities.GetCharacterFromFontAssets(unicode, fontAsset, generationSettings.fontAsset.m_FallbackFontAssetTable, true, fontStyle, fontWeight, out isUsingAlternativeTypeface);

                if (character != null)
                {
                    // Add character to font asset lookup cache
                    fontAsset.AddCharacterToLookupCache(unicode, character);

                    return character;
                }
            }

            // Search for the character in potential local Sprite Asset assigned to the text object.
            if (generationSettings.spriteAsset != null)
            {
                SpriteCharacter spriteCharacter = FontAssetUtilities.GetSpriteCharacterFromSpriteAsset(unicode, generationSettings.spriteAsset, true);

                if (spriteCharacter != null)
                    return spriteCharacter;
            }

            // Search for the character in the list of fallback assigned in the settings (General Fallbacks).
            if (textSettings.fallbackFontAssets != null && textSettings.fallbackFontAssets.Count > 0)
                character = FontAssetUtilities.GetCharacterFromFontAssets(unicode, fontAsset, textSettings.fallbackFontAssets, true, fontStyle, fontWeight, out isUsingAlternativeTypeface);

            if (character != null)
            {
                // Add character to font asset lookup cache
                fontAsset.AddCharacterToLookupCache(unicode, character);

                return character;
            }

            // Search for the character in the Default Font Asset assigned in the settings file.
            if (textSettings.defaultFontAsset != null)
                character = FontAssetUtilities.GetCharacterFromFontAsset(unicode, textSettings.defaultFontAsset, true, fontStyle, fontWeight, out isUsingAlternativeTypeface);

            if (character != null)
            {
                // Add character to font asset lookup cache
                fontAsset.AddCharacterToLookupCache(unicode, character);

                return character;
            }

            // Search for the character in the Default Sprite Asset assigned in the settings file.
            if (textSettings.defaultSpriteAsset != null)
            {
                SpriteCharacter spriteCharacter = FontAssetUtilities.GetSpriteCharacterFromSpriteAsset(unicode, textSettings.defaultSpriteAsset, true);

                if (spriteCharacter != null)
                    return spriteCharacter;
            }

            return null;
        }

        /// <summary>
        /// Update the margin width and height
        /// </summary>
        void ComputeMarginSize(Rect rect, Vector4 margins)
        {
            m_MarginWidth = rect.width - margins.x - margins.z;
            m_MarginHeight = rect.height - margins.y - margins.w;

            // Update the corners of the RectTransform
            m_RectTransformCorners[0].x = 0;
            m_RectTransformCorners[0].y = 0;
            m_RectTransformCorners[1].x = 0;
            m_RectTransformCorners[1].y = rect.height;
            m_RectTransformCorners[2].x = rect.width;
            m_RectTransformCorners[2].y = rect.height;
            m_RectTransformCorners[3].x = rect.width;
            m_RectTransformCorners[3].y = 0;
        }

        protected struct SpecialCharacter
        {
            public Character character;
            public FontAsset fontAsset;
            public Material material;
            public int materialIndex;

            public SpecialCharacter(Character character, int materialIndex)
            {
                this.character = character;
                this.fontAsset = character.textAsset as FontAsset;
                this.material = this.fontAsset != null ? this.fontAsset.material : null;
                this.materialIndex = materialIndex;
            }
        }

        /// <summary>
        /// Method used to find and cache references to the Underline and Ellipsis characters.
        /// </summary>
        /// <param name=""></param>
        protected void GetSpecialCharacters(TextGenerationSettings generationSettings)
        {
            GetEllipsisSpecialCharacter(generationSettings);

            GetUnderlineSpecialCharacter(generationSettings);
        }

        protected void GetEllipsisSpecialCharacter(TextGenerationSettings generationSettings)
        {
            bool isUsingAlternativeTypeface;

            FontAsset fontAsset = m_CurrentFontAsset ?? generationSettings.fontAsset;
            TextSettings textSettings = generationSettings.textSettings;

            // Search base font asset
            Character character = FontAssetUtilities.GetCharacterFromFontAsset(k_HorizontalEllipsis, fontAsset, false, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);

            if (character == null)
            {
                // Search primary fallback list
                if (fontAsset.m_FallbackFontAssetTable != null && fontAsset.m_FallbackFontAssetTable.Count > 0)
                    character = FontAssetUtilities.GetCharacterFromFontAssets(k_HorizontalEllipsis, fontAsset, fontAsset.m_FallbackFontAssetTable, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);
            }

            // Search the setting's general fallback list
            if (character == null)
            {
                if (textSettings.fallbackFontAssets != null && textSettings.fallbackFontAssets.Count > 0)
                    character = FontAssetUtilities.GetCharacterFromFontAssets(k_HorizontalEllipsis, fontAsset, textSettings.fallbackFontAssets, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);
            }

            // Search the setting's default font asset
            if (character == null)
            {
                if (textSettings.defaultFontAsset != null)
                    character = FontAssetUtilities.GetCharacterFromFontAsset(k_HorizontalEllipsis, textSettings.defaultFontAsset, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);
            }

            if (character != null)
                m_Ellipsis = new SpecialCharacter(character, 0);
        }

        protected void GetUnderlineSpecialCharacter(TextGenerationSettings generationSettings)
        {
            bool isUsingAlternativeTypeface;

            FontAsset fontAsset = m_CurrentFontAsset ?? generationSettings.fontAsset;
            TextSettings textSettings = generationSettings.textSettings;

            // Search base font asset
            Character character = FontAssetUtilities.GetCharacterFromFontAsset(0x5F, fontAsset, false, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface);

            if (character != null)
                m_Underline = new SpecialCharacter(character, m_CurrentMaterialIndex);
        }

        /// <summary>
        /// Get the padding value for the currently assigned material.
        /// </summary>
        /// <returns></returns>
        float GetPaddingForMaterial(Material material, bool extraPadding)
        {
            TextShaderUtilities.GetShaderPropertyIDs();

            if (material == null)
                return 0;

            m_Padding = TextShaderUtilities.GetPadding(material, extraPadding, m_IsUsingBold);
            m_IsMaskingEnabled = TextShaderUtilities.IsMaskingEnabled(material);
            m_IsSdfShader = material.HasProperty(TextShaderUtilities.ID_WeightNormal);

            return m_Padding;
        }

        /// <summary>
        /// Method to calculate the preferred width of a text object.
        /// </summary>
        /// <returns></returns>
        float GetPreferredWidthInternal(TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            if (generationSettings.textSettings == null)
                return 0;

            float fontSize = generationSettings.autoSize ? generationSettings.fontSizeMax : m_FontSize;

            // Reset auto sizing point size bounds
            m_MinFontSize = generationSettings.fontSizeMin;
            m_MaxFontSize = generationSettings.fontSizeMax;
            m_CharWidthAdjDelta = 0;

            // Set Margins to Infinity
            Vector2 margin = TextGeneratorUtilities.largePositiveVector2;

            //TextWrappingMode wrapMode = m_TextWrappingMode == TextWrappingMode.Normal || m_TextWrappingMode == TextWrappingMode.NoWrap ? TextWrappingMode.NoWrap : TextWrappingMode.PreserveWhitespaceNoWrap;
            TextWrappingMode wrapMode = generationSettings.wordWrap ? TextWrappingMode.NoWrap : TextWrappingMode.PreserveWhitespaceNoWrap;

            m_AutoSizeIterationCount = 0;
            float preferredWidth = CalculatePreferredValues(ref fontSize, margin, true, wrapMode, generationSettings, textInfo).x;

            return preferredWidth;
        }

        /// <summary>
        /// Method to calculate the preferred height of a text object.
        /// </summary>
        /// <returns></returns>
        float GetPreferredHeightInternal(TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            if (generationSettings.textSettings == null)
                return 0;

            float fontSize = generationSettings.autoSize ? generationSettings.fontSizeMax : m_FontSize;

            // Reset auto sizing point size bounds
            m_MinFontSize = generationSettings.fontSizeMin;
            m_MaxFontSize = generationSettings.fontSizeMax;
            m_CharWidthAdjDelta = 0;

            Vector2 margin = new Vector2(m_MarginWidth != 0 ? m_MarginWidth : TextGeneratorUtilities.largePositiveFloat, TextGeneratorUtilities.largePositiveFloat);

            // Reset Text Auto Size iteration tracking.
            m_IsAutoSizePointSizeSet = false;
            m_AutoSizeIterationCount = 0;

            float preferredHeight = 0;
            TextWrappingMode wrapMode = generationSettings.wordWrap ? TextWrappingMode.Normal : TextWrappingMode.NoWrap;

            while (m_IsAutoSizePointSizeSet == false)
            {
                preferredHeight = CalculatePreferredValues(ref fontSize, margin, generationSettings.autoSize, wrapMode, generationSettings, textInfo).y;
                m_AutoSizeIterationCount += 1;
            }

            return preferredHeight;
        }

        Vector2 GetPreferredValuesInternal(TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            if (generationSettings.textSettings == null)
                return Vector2.zero;

            float fontSize = generationSettings.autoSize ? generationSettings.fontSizeMax : m_FontSize;

            // Reset auto sizing point size bounds
            m_MinFontSize = generationSettings.fontSizeMin;
            m_MaxFontSize = generationSettings.fontSizeMax;
            m_CharWidthAdjDelta = 0;

            Vector2 margin = new Vector2(m_MarginWidth != 0 ? m_MarginWidth : TextGeneratorUtilities.largePositiveFloat, m_MarginHeight != 0 ? m_MarginHeight : TextGeneratorUtilities.largePositiveFloat);
            TextWrappingMode wrapMode = generationSettings.wordWrap ? TextWrappingMode.Normal : TextWrappingMode.NoWrap;

            m_AutoSizeIterationCount = 0;

            return CalculatePreferredValues(ref fontSize, margin, generationSettings.autoSize, wrapMode, generationSettings, textInfo);
        }


        /// <summary>
        /// Method to calculate the preferred width and height of the text object.
        /// </summary>
        /// <returns></returns>
        protected virtual Vector2 CalculatePreferredValues(ref float fontSize, Vector2 marginSize, bool isTextAutoSizingEnabled, TextWrappingMode textWrapMode, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            Profiler.BeginSample("TextGenerator.CalculatePreferredValues");

            // Early exit if no font asset was assigned. This should not be needed since LiberationSans SDF will be assigned by default.
            if (generationSettings.fontAsset == null || generationSettings.fontAsset.characterLookupTable == null)
            {
                Debug.LogWarning("Can't Generate Mesh! No Font Asset has been assigned.");

                m_IsAutoSizePointSizeSet = true;
                return Vector2.zero;
            }

            // Early exit if we don't have any Text to generate.
            if (m_TextProcessingArray == null || m_TextProcessingArray.Length == 0 || m_TextProcessingArray[0].unicode == (char)0)
            {
                m_IsAutoSizePointSizeSet = true;
                return Vector2.zero;
            }

            m_CurrentFontAsset = generationSettings.fontAsset;
            m_CurrentMaterial = generationSettings.material;
            m_CurrentMaterialIndex = 0;
            m_MaterialReferenceStack.SetDefault(new MaterialReference(0, m_CurrentFontAsset, null, m_CurrentMaterial, m_Padding));

            // Total character count is computed when the text is parsed.
            int totalCharacterCount = m_TotalCharacterCount; // m_VisibleCharacters.Count;

            if (m_InternalTextElementInfo == null || totalCharacterCount > m_InternalTextElementInfo.Length)
                m_InternalTextElementInfo = new TextElementInfo[totalCharacterCount > 1024 ? totalCharacterCount + 256 : Mathf.NextPowerOfTwo(totalCharacterCount)];

            // Calculate the scale of the font based on selected font size and sampling point size.
            // baseScale is calculated using the font asset assigned to the text object.
            float baseScale = (fontSize / generationSettings.fontAsset.faceInfo.pointSize * generationSettings.fontAsset.faceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f));
            float currentElementScale = baseScale;
            float currentEmScale = fontSize * 0.01f * (generationSettings.isOrthographic ? 1 : 0.1f);
            m_FontScaleMultiplier = 1;

            m_CurrentFontSize = fontSize;
            m_SizeStack.SetDefault(m_CurrentFontSize);
            float fontSizeDelta = 0;

            m_FontStyleInternal = generationSettings.fontStyle; // Set the default style.

            m_LineJustification = generationSettings.textAlignment;  // m_textAlignment; // Sets the line justification mode to match editor alignment.
            m_LineJustificationStack.SetDefault(m_LineJustification);

            m_BaselineOffset = 0; // Used by subscript characters.
            m_BaselineOffsetStack.Clear();

            m_FXScale = Vector3.one;

            m_LineOffset = 0; // Amount of space between lines (font line spacing + m_linespacing).
            m_LineHeight = k_FloatUnset;
            float lineGap = m_CurrentFontAsset.faceInfo.lineHeight - (m_CurrentFontAsset.faceInfo.ascentLine - m_CurrentFontAsset.faceInfo.descentLine);

            m_CSpacing = 0; // Amount of space added between characters as a result of the use of the <cspace> tag.
            m_MonoSpacing = 0;
            m_XAdvance = 0; // Used to track the position of each character.

            m_TagLineIndent = 0; // Used for indentation of text.
            m_TagIndent = 0;
            m_IndentStack.SetDefault(0);
            m_TagNoParsing = false;

            m_CharacterCount = 0; // Total characters in the char[]

            // Tracking of line information
            m_FirstCharacterOfLine = 0;
            m_MaxLineAscender = TextGeneratorUtilities.largeNegativeFloat;
            m_MaxLineDescender = TextGeneratorUtilities.largePositiveFloat;
            m_LineNumber = 0;
            m_StartOfLineAscender = 0;
            m_IsDrivenLineSpacing = false;
            m_LastBaseGlyphIndex = int.MinValue;

            TextSettings textSettings = generationSettings.textSettings;

            float marginWidth = marginSize.x;
            float marginHeight = marginSize.y;
            m_MarginLeft = 0;
            m_MarginRight = 0;

            m_Width = -1;
            float widthOfTextArea = marginWidth + 0.0001f - m_MarginLeft - m_MarginRight;

            // Used by Unity's Auto Layout system.
            float renderedWidth = 0;
            float renderedHeight = 0;
            float textWidth = 0;
            m_IsCalculatingPreferredValues = true;

            // Tracking of the highest Ascender
            m_MaxCapHeight = 0;
            m_MaxAscender = 0;
            m_MaxDescender = 0;
            float maxVisibleDescender = 0;
            bool isMaxVisibleDescenderSet = false;

            // Initialize struct to track states of word wrapping
            bool isFirstWordOfLine = true;
            m_IsNonBreakingSpace = false;
            bool ignoreNonBreakingSpace = false;

            CharacterSubstitution characterToSubstitute = new CharacterSubstitution(-1, 0);
            bool isSoftHyphenIgnored = false;

            WordWrapState internalWordWrapState = new WordWrapState();
            WordWrapState internalLineState = new WordWrapState();
            WordWrapState internalSoftLineBreak = new WordWrapState();

            // Clear the previous truncated / ellipsed state
            m_IsTextTruncated = false;

            // Counter to prevent recursive lockup when computing preferred values.
            m_AutoSizeIterationCount += 1;

            // Parse through Character buffer to read HTML tags and begin creating mesh.
            for (int i = 0; i < m_TextProcessingArray.Length && m_TextProcessingArray[i].unicode != 0; i++)
            {
                uint charCode = m_TextProcessingArray[i].unicode;

                // Skip characters that have been substituted.
                if (charCode == 0x1A)
                    continue;

                // Parse Rich Text Tag
                #region Parse Rich Text Tag
                if (generationSettings.richText && charCode == k_LesserThan)  // '<'
                {
                    m_isTextLayoutPhase = true;
                    m_TextElementType = TextElementType.Character;
                    int endTagIndex;

                    // Check if Tag is valid. If valid, skip to the end of the validated tag.
                    if (ValidateHtmlTag(m_TextProcessingArray, i + 1, out endTagIndex, generationSettings, textInfo))
                    {
                        i = endTagIndex;

                        // Continue to next character or handle the sprite element
                        if (m_TextElementType == TextElementType.Character)
                            continue;
                    }
                }
                else
                {
                    m_TextElementType = textInfo.textElementInfo[m_CharacterCount].elementType;
                    m_CurrentMaterialIndex = textInfo.textElementInfo[m_CharacterCount].materialReferenceIndex;
                    m_CurrentFontAsset = textInfo.textElementInfo[m_CharacterCount].fontAsset;
                }
                #endregion End Parse Rich Text Tag

                int prevMaterialIndex = m_CurrentMaterialIndex;
                bool isUsingAltTypeface = textInfo.textElementInfo[m_CharacterCount].isUsingAlternateTypeface;

                m_isTextLayoutPhase = false;

                // Handle potential character substitutions
                #region Character Substitutions
                bool isInjectedCharacter = false;

                if (characterToSubstitute.index == m_CharacterCount)
                {
                    charCode = characterToSubstitute.unicode;
                    m_TextElementType = TextElementType.Character;
                    isInjectedCharacter = true;

                    switch (charCode)
                    {
                        case k_EndOfText:
                            m_InternalTextElementInfo[m_CharacterCount].textElement = m_CurrentFontAsset.characterLookupTable[k_EndOfText];
                            m_IsTextTruncated = true;
                            break;
                        case 0x2D:
                            //
                            break;
                        case k_HorizontalEllipsis:
                            m_InternalTextElementInfo[m_CharacterCount].textElement = m_Ellipsis.character;
                            m_InternalTextElementInfo[m_CharacterCount].elementType = TextElementType.Character;
                            m_InternalTextElementInfo[m_CharacterCount].fontAsset = m_Ellipsis.fontAsset;
                            m_InternalTextElementInfo[m_CharacterCount].material = m_Ellipsis.material;
                            m_InternalTextElementInfo[m_CharacterCount].materialReferenceIndex = m_Ellipsis.materialIndex;

                            // Indicates the source parsing data has been modified.
                            m_IsTextTruncated = true;

                            // End Of Text
                            characterToSubstitute.index = m_CharacterCount + 1;
                            characterToSubstitute.unicode = k_EndOfText;
                            break;
                    }
                }
                #endregion


                // When using Linked text, mark character as ignored and skip to next character.
                #region Linked Text
                if (m_CharacterCount < generationSettings.firstVisibleCharacter && charCode != k_EndOfText)
                {
                    m_InternalTextElementInfo[m_CharacterCount].isVisible = false;
                    m_InternalTextElementInfo[m_CharacterCount].character = (char)k_ZeroWidthSpace;
                    m_InternalTextElementInfo[m_CharacterCount].lineNumber = 0;
                    m_CharacterCount += 1;
                    continue;
                }
                #endregion


                // Handle Font Styles like LowerCase, UpperCase and SmallCaps.
                #region Handling of LowerCase, UpperCase and SmallCaps Font Styles

                float smallCapsMultiplier = 1.0f;

                if (m_TextElementType == TextElementType.Character)
                {
                    if (/*(m_fontStyle & FontStyles.UpperCase) == FontStyles.UpperCase ||*/ (m_FontStyleInternal & FontStyles.UpperCase) == FontStyles.UpperCase)
                    {
                        // If this character is lowercase, switch to uppercase.
                        if (char.IsLower((char)charCode))
                            charCode = char.ToUpper((char)charCode);

                    }
                    else if (/*(m_fontStyle & FontStyles.LowerCase) == FontStyles.LowerCase ||*/ (m_FontStyleInternal & FontStyles.LowerCase) == FontStyles.LowerCase)
                    {
                        // If this character is uppercase, switch to lowercase.
                        if (char.IsUpper((char)charCode))
                            charCode = char.ToLower((char)charCode);
                    }
                    else if (/*(m_fontStyle & FontStyles.SmallCaps) == FontStyles.SmallCaps ||*/ (m_FontStyleInternal & FontStyles.SmallCaps) == FontStyles.SmallCaps)
                    {
                        if (char.IsLower((char)charCode))
                        {
                            smallCapsMultiplier = 0.8f;
                            charCode = char.ToUpper((char)charCode);
                        }
                    }
                }
                #endregion


                // Look up Character Data from Dictionary and cache it.
                #region Look up Character Data
                float baselineOffset = 0;
                float elementAscentLine = 0;
                float elementDescentLine = 0;
                if (m_TextElementType == TextElementType.Sprite)
                {
                    // If a sprite is used as a fallback then get a reference to it and set the color to white.
                    m_CurrentSpriteAsset = textInfo.textElementInfo[m_CharacterCount].textElement.textAsset as SpriteAsset;
                    m_SpriteIndex = (int)textInfo.textElementInfo[m_CharacterCount].textElement.glyphIndex;

                    SpriteCharacter sprite = m_CurrentSpriteAsset.spriteCharacterTable[m_SpriteIndex];
                    if (sprite == null) continue;

                    // Sprites are assigned in the E000 Private Area + sprite Index
                    if (charCode == k_LesserThan)
                        charCode = 57344 + (uint)m_SpriteIndex;

                    // The sprite scale calculations are based on the font asset assigned to the text object.
                    if (m_CurrentSpriteAsset.faceInfo.pointSize > 0)
                    {
                        float spriteScale = (m_CurrentFontSize / m_CurrentSpriteAsset.faceInfo.pointSize * m_CurrentSpriteAsset.faceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f));
                        currentElementScale = sprite.scale * sprite.glyph.scale * spriteScale;
                        elementAscentLine = m_CurrentSpriteAsset.faceInfo.ascentLine;
                        //baselineOffset = m_CurrentSpriteAsset.faceInfo.baseline * m_fontScale * m_FontScaleMultiplier * m_CurrentSpriteAsset.faceInfo.scale;
                        elementDescentLine = m_CurrentSpriteAsset.faceInfo.descentLine;
                    }
                    else
                    {
                        float spriteScale = (m_CurrentFontSize / m_CurrentFontAsset.faceInfo.pointSize * m_CurrentFontAsset.faceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f));
                        currentElementScale = m_CurrentFontAsset.faceInfo.ascentLine / sprite.glyph.metrics.height * sprite.scale * sprite.glyph.scale * spriteScale;
                        float scaleDelta = spriteScale / currentElementScale;
                        elementAscentLine = m_CurrentFontAsset.faceInfo.ascentLine * scaleDelta;
                        //baselineOffset = m_CurrentFontAsset.faceInfo.baseline * m_fontScale * m_FontScaleMultiplier * m_CurrentFontAsset.faceInfo.scale;
                        elementDescentLine = m_CurrentFontAsset.faceInfo.descentLine * scaleDelta;
                    }

                    m_CachedTextElement = sprite;

                    m_InternalTextElementInfo[m_CharacterCount].elementType = TextElementType.Sprite;
                    m_InternalTextElementInfo[m_CharacterCount].scale = currentElementScale;

                    m_CurrentMaterialIndex = prevMaterialIndex;
                }
                else if (m_TextElementType == TextElementType.Character)
                {
                    m_CachedTextElement = textInfo.textElementInfo[m_CharacterCount].textElement;
                    if (m_CachedTextElement == null) continue;

                    m_CurrentMaterialIndex = textInfo.textElementInfo[m_CharacterCount].materialReferenceIndex;

                    float adjustedScale;
                    if (isInjectedCharacter && m_TextProcessingArray[i].unicode == 0x0A && m_CharacterCount != m_FirstCharacterOfLine)
                        adjustedScale = textInfo.textElementInfo[m_CharacterCount - 1].pointSize * smallCapsMultiplier / m_CurrentFontAsset.m_FaceInfo.pointSize * m_CurrentFontAsset.m_FaceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f);
                    else
                        adjustedScale = m_CurrentFontSize * smallCapsMultiplier / m_CurrentFontAsset.m_FaceInfo.pointSize * m_CurrentFontAsset.m_FaceInfo.scale * (generationSettings.isOrthographic ? 1 : 0.1f);

                    // Special handling for injected Ellipsis
                    if (isInjectedCharacter && charCode == k_HorizontalEllipsis)
                    {
                        elementAscentLine = 0;
                        elementDescentLine = 0;
                    }
                    else
                    {
                        elementAscentLine = m_CurrentFontAsset.m_FaceInfo.ascentLine;
                        elementDescentLine = m_CurrentFontAsset.m_FaceInfo.descentLine;
                    }

                    currentElementScale = adjustedScale * m_FontScaleMultiplier * m_CachedTextElement.scale;

                    m_InternalTextElementInfo[m_CharacterCount].elementType = TextElementType.Character;
                }
                #endregion


                // Handle Soft Hyphen
                #region Handle Soft Hyphen
                float currentElementUnmodifiedScale = currentElementScale;
                if (charCode == 0xAD || charCode == k_EndOfText)
                    currentElementScale = 0;
                #endregion


                // Store some of the text object's information
                m_InternalTextElementInfo[m_CharacterCount].character = (char)charCode;

                // Cache glyph metrics
                Glyph altGlyph = textInfo.textElementInfo[m_CharacterCount].alternativeGlyph;
                GlyphMetrics currentGlyphMetrics = altGlyph == null ? m_CachedTextElement.m_Glyph.metrics : altGlyph.metrics;

                // Optimization to avoid calling this more than once per character.
                bool isWhiteSpace = charCode <= 0xFFFF && char.IsWhiteSpace((char)charCode);

                // Handle Kerning if Enabled.
                #region Handle Kerning
                GlyphValueRecord glyphAdjustments = new GlyphValueRecord();
                float characterSpacingAdjustment = generationSettings.characterSpacing;
                if (generationSettings.enableKerning)
                {
                    GlyphPairAdjustmentRecord adjustmentPair;
                    uint baseGlyphIndex = m_CachedTextElement.m_GlyphIndex;

                    if (m_CharacterCount < totalCharacterCount - 1)
                    {
                        uint nextGlyphIndex = textInfo.textElementInfo[m_CharacterCount + 1].textElement.m_GlyphIndex;
                        uint key = nextGlyphIndex << 16 | baseGlyphIndex;

                        if (m_CurrentFontAsset.m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.TryGetValue(key, out adjustmentPair))
                        {
                            glyphAdjustments = adjustmentPair.firstAdjustmentRecord.glyphValueRecord;
                            characterSpacingAdjustment = (adjustmentPair.featureLookupFlags & FontFeatureLookupFlags.IgnoreSpacingAdjustments) == FontFeatureLookupFlags.IgnoreSpacingAdjustments ? 0 : characterSpacingAdjustment;
                        }
                    }

                    if (m_CharacterCount >= 1)
                    {
                        uint previousGlyphIndex = textInfo.textElementInfo[m_CharacterCount - 1].textElement.m_GlyphIndex;
                        uint key = baseGlyphIndex << 16 | previousGlyphIndex;

                        if (m_CurrentFontAsset.m_FontFeatureTable.m_GlyphPairAdjustmentRecordLookup.TryGetValue(key, out adjustmentPair))
                        {
                            glyphAdjustments += adjustmentPair.secondAdjustmentRecord.glyphValueRecord;
                            characterSpacingAdjustment = (adjustmentPair.featureLookupFlags & FontFeatureLookupFlags.IgnoreSpacingAdjustments) == FontFeatureLookupFlags.IgnoreSpacingAdjustments ? 0 : characterSpacingAdjustment;
                        }
                    }

                    m_InternalTextElementInfo[m_CharacterCount].adjustedHorizontalAdvance = glyphAdjustments.xAdvance;
                }
                #endregion

                // Handle Diacritical Marks
                #region Handle Diacritical Marks
                bool isBaseGlyph = TextGeneratorUtilities.IsBaseGlyph((uint)charCode);

                if (isBaseGlyph)
                    m_LastBaseGlyphIndex = m_CharacterCount;

                if (m_CharacterCount > 0 && !isBaseGlyph)
                {
                    // Check for potential Mark-to-Base lookup if previous glyph was a base glyph
                    if (m_LastBaseGlyphIndex != int.MinValue && m_LastBaseGlyphIndex == m_CharacterCount - 1)
                    {
                        Glyph baseGlyph = textInfo.textElementInfo[m_LastBaseGlyphIndex].textElement.glyph;
                        uint baseGlyphIndex = baseGlyph.index;
                        uint markGlyphIndex = m_CachedTextElement.glyphIndex;
                        uint key = markGlyphIndex << 16 | baseGlyphIndex;

                        if (m_CurrentFontAsset.fontFeatureTable.m_MarkToBaseAdjustmentRecordLookup.TryGetValue(key, out MarkToBaseAdjustmentRecord glyphAdjustmentRecord))
                        {
                            float advanceOffset = (m_InternalTextElementInfo[m_LastBaseGlyphIndex].origin - m_XAdvance) / currentElementScale;

                            glyphAdjustments.xPlacement = advanceOffset + glyphAdjustmentRecord.baseGlyphAnchorPoint.xCoordinate - glyphAdjustmentRecord.markPositionAdjustment.xPositionAdjustment;
                            glyphAdjustments.yPlacement = glyphAdjustmentRecord.baseGlyphAnchorPoint.yCoordinate - glyphAdjustmentRecord.markPositionAdjustment.yPositionAdjustment;

                            characterSpacingAdjustment = 0;
                        }
                    }
                    else
                    {
                        // Iterate from previous glyph to last base glyph checking for any potential Mark-to-Mark lookups to apply. Otherwise check for potential Mark-to-Base lookup between the current glyph and last base glyph
                        bool wasLookupApplied = false;

                        // Check for any potential Mark-to-Mark lookups
                        for (int characterLookupIndex = m_CharacterCount - 1; characterLookupIndex >= 0 && characterLookupIndex != m_LastBaseGlyphIndex; characterLookupIndex--)
                        {
                            // Handle any potential Mark-to-Mark lookup
                            Glyph baseMarkGlyph = textInfo.textElementInfo[characterLookupIndex].textElement.glyph;
                            uint baseGlyphIndex = baseMarkGlyph.index;
                            uint combiningMarkGlyphIndex = m_CachedTextElement.glyphIndex;
                            uint key = combiningMarkGlyphIndex << 16 | baseGlyphIndex;

                            if (m_CurrentFontAsset.fontFeatureTable.m_MarkToMarkAdjustmentRecordLookup.TryGetValue(key, out MarkToMarkAdjustmentRecord glyphAdjustmentRecord))
                            {
                                float baseMarkOrigin = (textInfo.textElementInfo[characterLookupIndex].origin - m_XAdvance) / currentElementScale;
                                float currentBaseline = baselineOffset - m_LineOffset + m_BaselineOffset;
                                float baseMarkBaseline = (m_InternalTextElementInfo[characterLookupIndex].baseLine - currentBaseline) / currentElementScale;

                                glyphAdjustments.xPlacement = baseMarkOrigin + glyphAdjustmentRecord.baseMarkGlyphAnchorPoint.xCoordinate - glyphAdjustmentRecord.combiningMarkPositionAdjustment.xPositionAdjustment;
                                glyphAdjustments.yPlacement = baseMarkBaseline + glyphAdjustmentRecord.baseMarkGlyphAnchorPoint.yCoordinate - glyphAdjustmentRecord.combiningMarkPositionAdjustment.yPositionAdjustment;

                                characterSpacingAdjustment = 0;
                                wasLookupApplied = true;
                                break;
                            }
                        }

                        // If no Mark-to-Mark lookups were applied, check for potential Mark-to-Base lookup.
                        if (m_LastBaseGlyphIndex != int.MinValue && !wasLookupApplied)
                        {
                            // Handle lookup for Mark-to-Base
                            Glyph baseGlyph = textInfo.textElementInfo[m_LastBaseGlyphIndex].textElement.glyph;
                            uint baseGlyphIndex = baseGlyph.index;
                            uint markGlyphIndex = m_CachedTextElement.glyphIndex;
                            uint key = markGlyphIndex << 16 | baseGlyphIndex;

                            if (m_CurrentFontAsset.fontFeatureTable.m_MarkToBaseAdjustmentRecordLookup.TryGetValue(key, out MarkToBaseAdjustmentRecord glyphAdjustmentRecord))
                            {
                                float advanceOffset = (m_InternalTextElementInfo[m_LastBaseGlyphIndex].origin - m_XAdvance) / currentElementScale;

                                glyphAdjustments.xPlacement = advanceOffset + glyphAdjustmentRecord.baseGlyphAnchorPoint.xCoordinate - glyphAdjustmentRecord.markPositionAdjustment.xPositionAdjustment;
                                glyphAdjustments.yPlacement = glyphAdjustmentRecord.baseGlyphAnchorPoint.yCoordinate - glyphAdjustmentRecord.markPositionAdjustment.yPositionAdjustment;

                                characterSpacingAdjustment = 0;
                            }
                        }
                    }
                }

                // Adjust relevant text metrics
                elementAscentLine += glyphAdjustments.yPlacement;
                elementDescentLine += glyphAdjustments.yPlacement;
                #endregion


                // Initial Implementation for RTL support.
                #region Handle Right-to-Left
                //if (generationSettings.isRightToLeft)
                //{
                //    m_XAdvance -= ((m_CachedTextElement.xAdvance * boldXAdvanceMultiplier + m_characterSpacing + generationSettings.wordSpacing + m_CurrentFontAsset.regularStyleSpacing) * currentElementScale + m_CSpacing) * (1 - m_CharWidthAdjDelta);

                //    if (char.IsWhiteSpace((char)charCode) || charCode == k_ZeroWidthSpace)
                //        m_XAdvance -= generationSettings.wordSpacing * currentElementScale;
                //}
                #endregion


                // Handle Mono Spacing
                #region Handle Mono Spacing
                float monoAdvance = 0;
                if (m_MonoSpacing != 0)
                {
                    monoAdvance = (m_MonoSpacing / 2 - (m_CachedTextElement.glyph.metrics.width / 2 + m_CachedTextElement.glyph.metrics.horizontalBearingX) * currentElementScale) * (1 - m_CharWidthAdjDelta);
                    m_XAdvance += monoAdvance;
                }
                #endregion


                // Set Padding based on selected font style
                #region Handle Style Padding
                float boldSpacingAdjustment = 0;
                if (m_TextElementType == TextElementType.Character && !isUsingAltTypeface && ((m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold)) // Checks for any combination of Bold Style.
                    boldSpacingAdjustment = m_CurrentFontAsset.boldStyleSpacing;
                #endregion Handle Style Padding

                m_InternalTextElementInfo[m_CharacterCount].origin = m_XAdvance + glyphAdjustments.xPlacement * currentElementScale;
                m_InternalTextElementInfo[m_CharacterCount].baseLine = (baselineOffset - m_LineOffset + m_BaselineOffset) + glyphAdjustments.yPlacement * currentElementScale;

                // Compute text metrics
                #region Compute Ascender & Descender values
                // Element Ascender in line space
                float elementAscender = m_TextElementType == TextElementType.Character
                    ? elementAscentLine * currentElementScale / smallCapsMultiplier + m_BaselineOffset
                    : elementAscentLine * currentElementScale + m_BaselineOffset;

                // Element Descender in line space
                float elementDescender = m_TextElementType == TextElementType.Character
                    ? elementDescentLine * currentElementScale / smallCapsMultiplier + m_BaselineOffset
                    : elementDescentLine * currentElementScale + m_BaselineOffset;

                float adjustedAscender = elementAscender;
                float adjustedDescender = elementDescender;

                // Max line ascender and descender in line space
                bool isFirstCharacterOfLine = m_CharacterCount == m_FirstCharacterOfLine;
                if (isFirstCharacterOfLine || isWhiteSpace == false)
                {
                    // Special handling for Superscript and Subscript where we use the unadjusted line ascender and descender
                    if (m_BaselineOffset != 0)
                    {
                        adjustedAscender = Mathf.Max((elementAscender - m_BaselineOffset) / m_FontScaleMultiplier, adjustedAscender);
                        adjustedDescender = Mathf.Min((elementDescender - m_BaselineOffset) / m_FontScaleMultiplier, adjustedDescender);
                    }

                    m_MaxLineAscender = Mathf.Max(adjustedAscender, m_MaxLineAscender);
                    m_MaxLineDescender = Mathf.Min(adjustedDescender, m_MaxLineDescender);
                }

                // Element Ascender and Descender in object space
                if (isFirstCharacterOfLine || isWhiteSpace == false)
                {
                    m_InternalTextElementInfo[m_CharacterCount].adjustedAscender = adjustedAscender;
                    m_InternalTextElementInfo[m_CharacterCount].adjustedDescender = adjustedDescender;

                    m_InternalTextElementInfo[m_CharacterCount].ascender = elementAscender - m_LineOffset;
                    m_MaxDescender = m_InternalTextElementInfo[m_CharacterCount].descender = elementDescender - m_LineOffset;
                }
                else
                {
                    m_InternalTextElementInfo[m_CharacterCount].adjustedAscender = m_MaxLineAscender;
                    m_InternalTextElementInfo[m_CharacterCount].adjustedDescender = m_MaxLineDescender;

                    m_InternalTextElementInfo[m_CharacterCount].ascender = m_MaxLineAscender - m_LineOffset;
                    m_MaxDescender = m_InternalTextElementInfo[m_CharacterCount].descender = m_MaxLineDescender - m_LineOffset;
                }

                // Max text object ascender and cap height
                if (m_LineNumber == 0 || m_IsNewPage)
                {
                    if (isFirstCharacterOfLine || isWhiteSpace == false)
                    {
                        m_MaxAscender = m_MaxLineAscender;
                        m_MaxCapHeight = Mathf.Max(m_MaxCapHeight, m_CurrentFontAsset.m_FaceInfo.capLine * currentElementScale / smallCapsMultiplier);
                    }
                }

                // Page ascender
                if (m_LineOffset == 0)
                {
                    if (!isWhiteSpace || m_CharacterCount == m_FirstCharacterOfLine)
                        m_PageAscender = m_PageAscender > elementAscender ? m_PageAscender : elementAscender;
                }
                #endregion

                bool isJustifiedOrFlush = ((HorizontalAlignment)m_LineJustification & HorizontalAlignment.Flush) == HorizontalAlignment.Flush || ((HorizontalAlignment)m_LineJustification & HorizontalAlignment.Justified) == HorizontalAlignment.Justified;

                // Setup Mesh for visible text elements. ie. not a SPACE / LINEFEED / CARRIAGE RETURN.
                #region Handle Visible Characters
                if (charCode == k_Tab || charCode == k_ZeroWidthSpace || ((textWrapMode == TextWrappingMode.PreserveWhitespace || textWrapMode == TextWrappingMode.PreserveWhitespaceNoWrap) && (isWhiteSpace || charCode == k_ZeroWidthSpace)) || (isWhiteSpace == false && charCode != k_ZeroWidthSpace && charCode != 0xAD && charCode != k_EndOfText) || (charCode == 0xAD && isSoftHyphenIgnored == false) || m_TextElementType == TextElementType.Sprite)
                {
                    //float marginLeft = m_MarginLeft;
                    //float marginRight = m_MarginRight;

                    // Injected characters do not override margins
                    //if (isInjectedCharacter)
                    //{
                    //    marginLeft = textInfo.lineInfo[m_LineNumber].marginLeft;
                    //    marginRight = textInfo.lineInfo[m_LineNumber].marginRight;
                    //}

                    widthOfTextArea = m_Width != -1 ? Mathf.Min(marginWidth + 0.0001f - m_MarginLeft - m_MarginRight, m_Width) : marginWidth + 0.0001f - m_MarginLeft - m_MarginRight;

                    // Calculate the line breaking width of the text.
                    textWidth = Mathf.Abs(m_XAdvance) + currentGlyphMetrics.horizontalAdvance * (1 - m_CharWidthAdjDelta) * (charCode == 0xAD ? currentElementUnmodifiedScale : currentElementScale);

                    int testedCharacterCount = m_CharacterCount;

                    // Handling of Horizontal Bounds
                    #region Current Line Horizontal Bounds Check
                    if (isBaseGlyph && textWidth > widthOfTextArea * (isJustifiedOrFlush ? 1.05f : 1.0f))
                    {
                        // Handle Line Breaking (if still possible)
                        if (textWrapMode != TextWrappingMode.NoWrap && textWrapMode != TextWrappingMode.PreserveWhitespaceNoWrap && m_CharacterCount != m_FirstCharacterOfLine)
                        {
                            // Restore state to previous safe line breaking
                            i = RestoreWordWrappingState(ref internalWordWrapState, textInfo);

                            // Replace Soft Hyphen by Hyphen Minus 0x2D
                            #region Handle Soft Hyphenation
                            if (m_InternalTextElementInfo[m_CharacterCount - 1].character == 0xAD && isSoftHyphenIgnored == false && generationSettings.overflowMode == TextOverflowMode.Overflow)
                            {
                                characterToSubstitute.index = m_CharacterCount - 1;
                                characterToSubstitute.unicode = 0x2D;

                                i -= 1;
                                m_CharacterCount -= 1;
                                continue;
                            }

                            isSoftHyphenIgnored = false;

                            // Ignore Soft Hyphen to prevent it from wrapping
                            if (m_InternalTextElementInfo[m_CharacterCount].character == 0xAD)
                            {
                                isSoftHyphenIgnored = true;
                                continue;
                            }
                            #endregion

                            // Adjust character spacing before breaking up word if auto size is enabled
                            #region Handle Text Auto Size (if word wrapping is no longer possible)
                            if (isTextAutoSizingEnabled && isFirstWordOfLine)
                            {
                                // Handle Character Width Adjustments
                                #region Character Width Adjustments
                                if (m_CharWidthAdjDelta < generationSettings.charWidthMaxAdj / 100 && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                {
                                    float adjustedTextWidth = textWidth;

                                    // Determine full width of the text
                                    if (m_CharWidthAdjDelta > 0)
                                        adjustedTextWidth /= 1f - m_CharWidthAdjDelta;

                                    float adjustmentDelta = textWidth - (widthOfTextArea - 0.0001f) * (isJustifiedOrFlush ? 1.05f : 1.0f);
                                    m_CharWidthAdjDelta += adjustmentDelta / adjustedTextWidth;
                                    m_CharWidthAdjDelta = Mathf.Min(m_CharWidthAdjDelta, generationSettings.charWidthMaxAdj / 100);

                                    return Vector2.zero;
                                }
                                #endregion

                                // Handle Text Auto-sizing resulting from text exceeding vertical bounds.
                                #region Text Auto-Sizing (Text greater than vertical bounds)
                                if (fontSize > generationSettings.fontSizeMin && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
                                {
                                    m_MaxFontSize = fontSize;

                                    float sizeDelta = Mathf.Max((fontSize - m_MinFontSize) / 2, 0.05f);
                                    fontSize -= sizeDelta;
                                    fontSize = Mathf.Max((int)(fontSize * 20 + 0.5f) / 20f, generationSettings.fontSizeMin);

                                    // TODO: Need to investigate this value when moving it to a service as TMP might require this value to be zero.
                                    //       It's currently omitted as UITK expects the text height to be auto-increased if a word becomes longer
                                    //       and longer. User scenario: User enters a very long word with no spaces that spawns longer than it's container.
                                    // return Vector2.zero;
                                }
                                #endregion Text Auto-Sizing
                            }
                            #endregion

                            // Adjust line spacing if necessary
                            float baselineAdjustmentDelta = m_MaxLineAscender - m_StartOfLineAscender;
                            if (m_LineOffset > 0 && Math.Abs(baselineAdjustmentDelta) > 0.01f && m_IsDrivenLineSpacing == false && !m_IsNewPage)
                            {
                                //AdjustLineOffset(m_FirstCharacterOfLine, m_CharacterCount, baselineAdjustmentDelta);
                                m_MaxDescender -= baselineAdjustmentDelta;
                                m_LineOffset += baselineAdjustmentDelta;
                            }

                            // Calculate line ascender and make sure if last character is superscript or subscript that we check that as well.
                            float lineAscender = m_MaxLineAscender - m_LineOffset;
                            float lineDescender = m_MaxLineDescender - m_LineOffset;

                            // Update maxDescender and maxVisibleDescender
                            m_MaxDescender = m_MaxDescender < lineDescender ? m_MaxDescender : lineDescender;
                            if (!isMaxVisibleDescenderSet)
                                maxVisibleDescender = m_MaxDescender;

                            if (generationSettings.useMaxVisibleDescender && (m_CharacterCount >= generationSettings.maxVisibleCharacters || m_LineNumber >= generationSettings.maxVisibleLines))
                                isMaxVisibleDescenderSet = true;

                            // Store first character of the next line.
                            m_FirstCharacterOfLine = m_CharacterCount;
                            m_LineVisibleCharacterCount = 0;

                            // Store the state of the line before starting on the new line.
                            SaveWordWrappingState(ref internalLineState, i, m_CharacterCount - 1, textInfo);

                            m_LineNumber += 1;

                            float ascender = m_InternalTextElementInfo[m_CharacterCount].adjustedAscender;

                            // Compute potential new line offset in the event a line break is needed.
                            if (m_LineHeight == k_FloatUnset)
                            {
                                m_LineOffset += 0 - m_MaxLineDescender + ascender + (lineGap + m_LineSpacingDelta) * baseScale + generationSettings.lineSpacing * currentEmScale;
                                m_IsDrivenLineSpacing = false;
                            }
                            else
                            {
                                m_LineOffset += m_LineHeight + generationSettings.lineSpacing * currentEmScale;
                                m_IsDrivenLineSpacing = true;
                            }

                            m_MaxLineAscender = TextGeneratorUtilities.largeNegativeFloat;
                            m_MaxLineDescender = TextGeneratorUtilities.largePositiveFloat;
                            m_StartOfLineAscender = ascender;

                            m_XAdvance = 0 + m_TagIndent;
                            //isStartOfNewLine = true;
                            isFirstWordOfLine = true;
                            continue;
                        }
                    }
                    #endregion

                    // Compute Preferred Width & Height
                    renderedWidth = Mathf.Max(renderedWidth, textWidth + m_MarginLeft + m_MarginRight);
                    renderedHeight = m_MaxAscender - m_MaxDescender;

                }
                #endregion Handle Visible Characters


                // Check if Line Spacing of previous line needs to be adjusted.
                #region Adjust Line Spacing
                /*if (m_LineOffset > 0 && !TextGeneratorUtilities.Approximately(m_MaxLineAscender, m_StartOfLineAscender) && m_IsDrivenLineSpacing == false && !m_IsNewPage)
                {
                    float offsetDelta = m_MaxLineAscender - m_StartOfLineAscender;
                    //AdjustLineOffset(m_FirstCharacterOfLine, m_CharacterCount, offsetDelta);
                    m_MaxDescender -= offsetDelta;
                    m_LineOffset += offsetDelta;

                    m_StartOfLineAscender += offsetDelta;
                    internalWordWrapState.lineOffset = m_LineOffset;
                    internalWordWrapState.startOfLineAscender = m_StartOfLineAscender;
                }*/
                #endregion


                // Handle xAdvance & Tabulation Stops. Tab stops at every 25% of Font Size.
                #region XAdvance, Tabulation & Stops
                if (charCode == k_Tab)
                {
                    float tabSize = m_CurrentFontAsset.faceInfo.tabWidth * m_CurrentFontAsset.tabMultiple * currentElementScale;
                    float tabs = Mathf.Ceil(m_XAdvance / tabSize) * tabSize;
                    m_XAdvance = tabs > m_XAdvance ? tabs : m_XAdvance + tabSize;
                }
                else if (m_MonoSpacing != 0)
                {
                    m_XAdvance += (m_MonoSpacing - monoAdvance + ((m_CurrentFontAsset.regularStyleSpacing + characterSpacingAdjustment) * currentEmScale) + m_CSpacing) * (1 - m_CharWidthAdjDelta);

                    if (isWhiteSpace || charCode == k_ZeroWidthSpace)
                        m_XAdvance += generationSettings.wordSpacing * currentEmScale;
                }
                else
                {
                    m_XAdvance += ((currentGlyphMetrics.horizontalAdvance * m_FXScale.x + glyphAdjustments.xAdvance) * currentElementScale + (m_CurrentFontAsset.regularStyleSpacing + characterSpacingAdjustment + boldSpacingAdjustment) * currentEmScale + m_CSpacing) * (1 - m_CharWidthAdjDelta);

                    if (isWhiteSpace || charCode == k_ZeroWidthSpace)
                        m_XAdvance += generationSettings.wordSpacing * currentEmScale;
                }
                #endregion Tabulation & Stops


                // Handle Carriage Return
                #region Carriage Return
                if (charCode == k_CarriageReturn)
                {
                    m_XAdvance = 0 + m_TagIndent;
                }
                #endregion Carriage Return


                // Handle Line Spacing Adjustments + Word Wrapping & special case for last line.
                #region Check for Line Feed and Last Character
                if (charCode == k_LineFeed || charCode == 11 || charCode == k_EndOfText || charCode == 0x2028 || charCode == 0x2029 || m_CharacterCount == totalCharacterCount - 1)
                {
                    // Check if Line Spacing of previous line needs to be adjusted.
                    float baselineAdjustmentDelta = m_MaxLineAscender - m_StartOfLineAscender;
                    if (m_LineOffset > 0 && Math.Abs(baselineAdjustmentDelta) > 0.01f && m_IsDrivenLineSpacing == false && !m_IsNewPage)
                    {
                        m_MaxDescender -= baselineAdjustmentDelta;
                        m_LineOffset += baselineAdjustmentDelta;
                    }
                    m_IsNewPage = false;

                    // Calculate lineAscender & make sure if last character is superscript or subscript that we check that as well.
                    //float lineAscender = m_MaxLineAscender - m_LineOffset;
                    float lineDescender = m_MaxLineDescender - m_LineOffset;

                    // Update maxDescender and maxVisibleDescender
                    m_MaxDescender = m_MaxDescender < lineDescender ? m_MaxDescender : lineDescender;

                    // Add new line if not last lines or character.
                    if (charCode == k_LineFeed || charCode == 11 || charCode == 0x2D || charCode == 0x2028 || charCode == 0x2029)
                    {
                        // Store the state of the line before starting on the new line.
                        SaveWordWrappingState(ref internalLineState, i, m_CharacterCount, textInfo);
                        // Store the state of the last Character before the new line.
                        SaveWordWrappingState(ref internalWordWrapState, i, m_CharacterCount, textInfo);

                        m_LineNumber += 1;
                        m_FirstCharacterOfLine = m_CharacterCount + 1;

                        float ascender = m_InternalTextElementInfo[m_CharacterCount].adjustedAscender;

                        // Apply Line Spacing with special handling for VT char(11)
                        if (m_LineHeight == k_FloatUnset)
                        {
                            float lineOffsetDelta = 0 - m_MaxLineDescender + ascender + (lineGap + m_LineSpacingDelta) * baseScale + (generationSettings.lineSpacing + (charCode == k_LineFeed || charCode == 0x2029 ? generationSettings.paragraphSpacing : 0)) * currentEmScale;
                            m_LineOffset += lineOffsetDelta;
                            m_IsDrivenLineSpacing = false;
                        }
                        else
                        {
                            m_LineOffset += m_LineHeight + (generationSettings.lineSpacing + (charCode == k_LineFeed || charCode == 0x2029 ? generationSettings.paragraphSpacing : 0)) * currentEmScale;
                            m_IsDrivenLineSpacing = true;
                        }

                        m_MaxLineAscender = TextGeneratorUtilities.largeNegativeFloat;
                        m_MaxLineDescender = TextGeneratorUtilities.largePositiveFloat;
                        m_StartOfLineAscender = ascender;

                        m_XAdvance = 0 + m_TagLineIndent + m_TagIndent;

                        m_CharacterCount += 1;
                        continue;
                    }

                    // If End of Text
                    if (charCode == k_EndOfText)
                        i = m_TextProcessingArray.Length;
                }
                #endregion Check for Linefeed or Last Character


                // Save State of Mesh Creation for handling of Word Wrapping
                #region Save Word Wrapping State
                if ((textWrapMode != TextWrappingMode.NoWrap && textWrapMode != TextWrappingMode.PreserveWhitespaceNoWrap) || generationSettings.overflowMode == TextOverflowMode.Truncate || generationSettings.overflowMode == TextOverflowMode.Ellipsis)
                {
                    bool shouldSaveHardLineBreak = false;
                    bool shouldSaveSoftLineBreak = false;

                    if ((isWhiteSpace || charCode == k_ZeroWidthSpace || charCode == 0x2D || charCode == 0xAD) && (!m_IsNonBreakingSpace || ignoreNonBreakingSpace) && charCode != 0xA0 && charCode != k_FigureSpace && charCode != k_NonBreakingHyphen && charCode != k_NarrowNoBreakSpace && charCode != k_WordJoiner)
                    {
                        // Ignore Hyphen (0x2D) when preceded by a whitespace
                        if ((charCode == 0x2D && m_CharacterCount > 0 && char.IsWhiteSpace(textInfo.textElementInfo[m_CharacterCount - 1].character)) == false)
                        {
                            isFirstWordOfLine = false;
                            shouldSaveHardLineBreak = true;

                            // Reset soft line breaking point since we now have a valid hard break point.
                            internalSoftLineBreak.previousWordBreak = -1;
                        }
                    }
                    // Handling for East Asian scripts
                    else if (m_IsNonBreakingSpace == false && (TextGeneratorUtilities.IsHangul((uint)charCode) && textSettings.useModernHangulLineBreakingRules == false || TextGeneratorUtilities.IsCJK((uint)charCode)))
                    {
                        bool isCurrentLeadingCharacter = textSettings.lineBreakingRules.leadingCharactersLookup.Contains((uint)charCode);
                        bool isNextFollowingCharacter = m_CharacterCount < totalCharacterCount - 1 && textSettings.lineBreakingRules.leadingCharactersLookup.Contains(m_InternalTextElementInfo[m_CharacterCount + 1].character);

                        if (isCurrentLeadingCharacter == false)
                        {
                            if (isNextFollowingCharacter == false)
                            {
                                isFirstWordOfLine = false;
                                shouldSaveHardLineBreak = true;
                            }

                            if (isFirstWordOfLine)
                            {
                                // Special handling for non-breaking space and soft line breaks
                                if (isWhiteSpace)
                                    shouldSaveSoftLineBreak = true;

                                shouldSaveHardLineBreak = true;
                            }
                        }
                        else
                        {
                            if (isFirstWordOfLine && isFirstCharacterOfLine)
                            {
                                // Special handling for non-breaking space and soft line breaks
                                if (isWhiteSpace)
                                    shouldSaveSoftLineBreak = true;

                                shouldSaveHardLineBreak = true;
                            }
                        }
                    }
                    else if (isFirstWordOfLine)
                    {
                        // Special handling for non-breaking space and soft line breaks
                        if (isWhiteSpace && charCode != 0xA0 || (charCode == 0xAD && isSoftHyphenIgnored == false))
                            shouldSaveSoftLineBreak = true;

                        shouldSaveHardLineBreak = true;
                    }

                    // Save potential Hard lines break
                    if (shouldSaveHardLineBreak)
                        SaveWordWrappingState(ref internalWordWrapState, i, m_CharacterCount, textInfo);

                    // Save potential Soft line break
                    if (shouldSaveSoftLineBreak)
                        SaveWordWrappingState(ref internalSoftLineBreak, i, m_CharacterCount, textInfo);
                }
                #endregion Save Word Wrapping State

                m_CharacterCount += 1;
            }

            // Check Auto Sizing and increase font size to fill text container.
            #region Check Auto-Sizing (Upper Font Size Bounds)
            fontSizeDelta = m_MaxFontSize - m_MinFontSize;
            if (isTextAutoSizingEnabled && fontSizeDelta > 0.051f && fontSize < generationSettings.fontSizeMax && m_AutoSizeIterationCount < m_AutoSizeMaxIterationCount)
            {
                // Reset character width adjustment delta
                if (m_CharWidthAdjDelta < generationSettings.charWidthMaxAdj / 100)
                    m_CharWidthAdjDelta = 0;

                m_MinFontSize = fontSize;

                float sizeDelta = Mathf.Max((m_MaxFontSize - fontSize) / 2, 0.05f);
                fontSize += sizeDelta;
                fontSize = Mathf.Min((int)(fontSize * 20 + 0.5f) / 20f, generationSettings.fontSizeMax);

                return Vector2.zero;
            }
            #endregion End Auto-sizing Check

            m_IsAutoSizePointSizeSet = true;

            m_IsCalculatingPreferredValues = false;

            // Adjust Preferred Width and Height to account for Margins.
            renderedWidth += generationSettings.margins.x > 0 ? generationSettings.margins.x : 0;
            renderedWidth += generationSettings.margins.z > 0 ? generationSettings.margins.z : 0;

            renderedHeight += generationSettings.margins.y > 0 ? generationSettings.margins.y : 0;
            renderedHeight += generationSettings.margins.w > 0 ? generationSettings.margins.w : 0;

            // Round Preferred Values to nearest 5/100.
            renderedWidth = (int)(renderedWidth * 100 + 1f) / 100f;
            renderedHeight = (int)(renderedHeight * 100 + 1f) / 100f;

            Profiler.EndSample();

            return new Vector2(renderedWidth, renderedHeight);
        }

        /// <summary>
        /// Convert source text to Unicode (uint) and populate internal text backing array.
        /// </summary>
        /// <param name="sourceText">Source text to be converted</param>
        void PopulateTextBackingArray(string sourceText)
        {
            int srcLength = sourceText == null ? 0 : sourceText.Length;

            PopulateTextBackingArray(sourceText, 0, srcLength);
        }

        /// <summary>
        /// Convert source text to uint and populate internal text backing array.
        /// </summary>
        /// <param name="sourceText">string containing the source text to be converted</param>
        /// <param name="start">Index of the first element of the source array to be converted and copied to the internal text backing array.</param>
        /// <param name="length">Number of elements in the array to be converted and copied to the internal text backing array.</param>
        void PopulateTextBackingArray(string sourceText, int start, int length)
        {
            int readIndex;
            int writeIndex = 0;

            // Range check
            if (sourceText == null)
            {
                readIndex = 0;
                length = 0;
            }
            else
            {
                readIndex = Mathf.Clamp(start, 0, sourceText.Length);
                length = Mathf.Clamp(length, 0, start + length < sourceText.Length ? length : sourceText.Length - start);
            }

            // Make sure array size is appropriate
            if (length >= m_TextBackingArray.Capacity)
                m_TextBackingArray.Resize((length));

            int end = readIndex + length;
            for (; readIndex < end; readIndex++)
            {
                m_TextBackingArray[writeIndex] = sourceText[readIndex];
                writeIndex += 1;
            }

            // Terminate array with zero as we are not clearing the array on new invocation of this function.
            m_TextBackingArray[writeIndex] = 0;
            m_TextBackingArray.Count = writeIndex;
        }

        /// <summary>
        /// Convert source text to uint and populate internal text backing array.
        /// </summary>
        /// <param name="sourceText">char array containing the source text to be converted</param>
        /// <param name="start">Index of the first element of the source array to be converted and copied to the internal text backing array.</param>
        /// <param name="length">Number of elements in the array to be converted and copied to the internal text backing array.</param>
        void PopulateTextBackingArray(StringBuilder sourceText, int start, int length)
        {
            int readIndex;
            int writeIndex = 0;

            // Range check
            if (sourceText == null)
            {
                readIndex = 0;
                length = 0;
            }
            else
            {
                readIndex = Mathf.Clamp(start, 0, sourceText.Length);
                length = Mathf.Clamp(length, 0, start + length < sourceText.Length ? length : sourceText.Length - start);
            }

            // Make sure array size is appropriate
            if (length >= m_TextBackingArray.Capacity)
                m_TextBackingArray.Resize((length));

            int end = readIndex + length;
            for (; readIndex < end; readIndex++)
            {
                m_TextBackingArray[writeIndex] = sourceText[readIndex];
                writeIndex += 1;
            }

            // Terminate array with zero as we are not clearing the array on new invocation of this function.
            m_TextBackingArray[writeIndex] = 0;
            m_TextBackingArray.Count = writeIndex;
        }

        /// <summary>
        /// Convert source text to Unicode (uint) and populate internal text backing array.
        /// </summary>
        /// <param name="sourceText">char array containing the source text to be converted</param>
        /// <param name="start">Index of the first element of the source array to be converted and copied to the internal text backing array.</param>
        /// <param name="length">Number of elements in the array to be converted and copied to the internal text backing array.</param>
        void PopulateTextBackingArray(char[] sourceText, int start, int length)
        {
            int readIndex;
            int writeIndex = 0;

            // Range check
            if (sourceText == null)
            {
                readIndex = 0;
                length = 0;
            }
            else
            {
                readIndex = Mathf.Clamp(start, 0, sourceText.Length);
                length = Mathf.Clamp(length, 0, start + length < sourceText.Length ? length : sourceText.Length - start);
            }

            // Make sure array size is appropriate
            if (length >= m_TextBackingArray.Capacity)
                m_TextBackingArray.Resize((length));

            int end = readIndex + length;
            for (; readIndex < end; readIndex++)
            {
                m_TextBackingArray[writeIndex] = sourceText[readIndex];
                writeIndex += 1;
            }

            // Terminate array with zero as we are not clearing the array on new invocation of this function.
            m_TextBackingArray[writeIndex] = 0;
            m_TextBackingArray.Count = writeIndex;
        }

        /// <summary>
        ///
        /// </summary>
        void PopulateTextProcessingArray(TextGenerationSettings generationSettings)
        {
            int srcLength = m_TextBackingArray.Count;

            // Make sure parsing buffer is large enough to handle the required text.
            if (m_TextProcessingArray.Length < srcLength)
                TextGeneratorUtilities.ResizeInternalArray(ref m_TextProcessingArray, srcLength);

            // Reset Style stack back to default
            TextProcessingStack<int>.SetDefault(m_TextStyleStacks, 0);

            m_TextStyleStackDepth = 0;
            int writeIndex = 0;

            int styleHashCode = m_TextStyleStacks[0].Pop();
            TextStyle textStyle = TextGeneratorUtilities.GetStyle(generationSettings, styleHashCode);

            // Insert Opening Style
            if (textStyle != null && textStyle.hashCode != (int)MarkupTag.NORMAL)
                TextGeneratorUtilities.InsertOpeningStyleTag(textStyle, ref m_TextProcessingArray, ref writeIndex, ref m_TextStyleStackDepth, ref m_TextStyleStacks, ref generationSettings);

            var tagNoParsing = generationSettings.tagNoParsing;

            int readIndex = 0;
            for (; readIndex < srcLength; readIndex++)
            {
                uint c = m_TextBackingArray[readIndex];

                if (c == 0)
                    break;

                // TODO: Since we do not set the TextInputSource at this very moment, we have defaulted this conditional to check for
                //       TextInputSource.TextString whereas in TMP, it checks for TextInputSource.TextInputBox
                if (/*generationSettings.inputSource == TextInputSource.TextString && */ c == '\\' && readIndex < srcLength - 1)
                {
                    switch (m_TextBackingArray[readIndex + 1])
                    {
                        case 92: // \ escape
                            if (!generationSettings.parseControlCharacters) break;

                            readIndex += 1;
                            break;
                        case 110: // \n LineFeed
                            if (!generationSettings.parseControlCharacters) break;

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 1, unicode = 10 };

                            readIndex += 1;
                            writeIndex += 1;
                            continue;
                        case 114: // \r Carriage Return
                            if (!generationSettings.parseControlCharacters) break;

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 1, unicode = 13 };

                            readIndex += 1;
                            writeIndex += 1;
                            continue;
                        case 116: // \t Tab
                            if (!generationSettings.parseControlCharacters) break;

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 1, unicode = 9 };

                            readIndex += 1;
                            writeIndex += 1;
                            continue;
                        case 118: // \v Vertical tab used as soft line break
                            if (!generationSettings.parseControlCharacters) break;

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 1, unicode = 11 };

                            readIndex += 1;
                            writeIndex += 1;
                            continue;
                        case 117: // \u0000 for UTF-16 Unicode
                            if (srcLength > readIndex + 5 && TextGeneratorUtilities.IsValidUTF16(m_TextBackingArray, readIndex + 2))
                            {
                                m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 6, unicode = TextGeneratorUtilities.GetUTF16(m_TextBackingArray, readIndex + 2) };

                                readIndex += 5;
                                writeIndex += 1;
                                continue;
                            }
                            break;
                        case 85: // \U00000000 for UTF-32 Unicode
                            if (srcLength > readIndex + 9 && TextGeneratorUtilities.IsValidUTF32(m_TextBackingArray, readIndex + 2))
                            {
                                m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 10, unicode = TextGeneratorUtilities.GetUTF32(m_TextBackingArray, readIndex + 2) };

                                readIndex += 9;
                                writeIndex += 1;
                                continue;
                            }
                            break;
                    }
                }

                // Handle surrogate pair conversion
                if (c >= CodePoint.HIGH_SURROGATE_START && c <= CodePoint.HIGH_SURROGATE_END && srcLength > readIndex + 1 && m_TextBackingArray[readIndex + 1] >= CodePoint.LOW_SURROGATE_START && m_TextBackingArray[readIndex + 1] <= CodePoint.LOW_SURROGATE_END)
                {
                    m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 2, unicode = TextGeneratorUtilities.ConvertToUTF32(c, m_TextBackingArray[readIndex + 1]) };

                    readIndex += 1;
                    writeIndex += 1;
                    continue;
                }

                // Handle inline replacement of <style> and <br> tags.
                if (c == '<' && generationSettings.richText)
                {
                    // Read tag hash code
                    int hashCode = TextGeneratorUtilities.GetMarkupTagHashCode(m_TextBackingArray, readIndex + 1);

                    switch ((MarkupTag)hashCode)
                    {
                        case MarkupTag.NO_PARSE:
                            tagNoParsing = true;
                            break;
                        case MarkupTag.SLASH_NO_PARSE:
                            tagNoParsing = false;
                            break;
                        case MarkupTag.BR:
                            if (tagNoParsing) break;
                            if (writeIndex == m_TextProcessingArray.Length) TextGeneratorUtilities.ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 4, unicode = 10 };
                            writeIndex += 1;
                            readIndex += 3;
                            continue;
                        case MarkupTag.CR:
                            if (tagNoParsing) break;
                            if (writeIndex == m_TextProcessingArray.Length) TextGeneratorUtilities.ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 4, unicode = 13 };
                            writeIndex += 1;
                            readIndex += 3;
                            continue;
                        case MarkupTag.NBSP:
                            if (tagNoParsing) break;
                            if (writeIndex == m_TextProcessingArray.Length) TextGeneratorUtilities.ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 6, unicode = 0xA0 };
                            writeIndex += 1;
                            readIndex += 5;
                            continue;
                        case MarkupTag.ZWSP:
                            if (tagNoParsing) break;
                            if (writeIndex == m_TextProcessingArray.Length) TextGeneratorUtilities.ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 6, unicode = 0x200B };
                            writeIndex += 1;
                            readIndex += 5;
                            continue;
                        case MarkupTag.ZWJ:
                            if (tagNoParsing) break;
                            if (writeIndex == m_TextProcessingArray.Length) TextGeneratorUtilities.ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 5, unicode = 0x200D };
                            writeIndex += 1;
                            readIndex += 4;
                            continue;
                        case MarkupTag.SHY:
                            if (tagNoParsing) break;
                            if (writeIndex == m_TextProcessingArray.Length) TextGeneratorUtilities.ResizeInternalArray(ref m_TextProcessingArray);

                            m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 5, unicode = 0xAD };

                            writeIndex += 1;
                            readIndex += 4;
                            continue;
                        case MarkupTag.A:
                            // Additional check
                            if (m_TextBackingArray.Count > readIndex + 4 && m_TextBackingArray[readIndex + 3] == 'h' && m_TextBackingArray[readIndex + 4] == 'r')
                                TextGeneratorUtilities.InsertOpeningTextStyle(TextGeneratorUtilities.GetStyle(generationSettings, (int)MarkupTag.A), ref m_TextProcessingArray, ref writeIndex, ref m_TextStyleStackDepth, ref m_TextStyleStacks, ref generationSettings);
                            break;
                        case MarkupTag.STYLE:
                            if (tagNoParsing) break;

                            int openWriteIndex = writeIndex;
                            if (TextGeneratorUtilities.ReplaceOpeningStyleTag(ref m_TextBackingArray, readIndex, out int srcOffset, ref m_TextProcessingArray, ref writeIndex, ref m_TextStyleStackDepth, ref m_TextStyleStacks, ref generationSettings))
                            {
                                // Update potential text elements added by the opening style.
                                for (; openWriteIndex < writeIndex; openWriteIndex++)
                                {
                                    m_TextProcessingArray[openWriteIndex].stringIndex = readIndex;
                                    m_TextProcessingArray[openWriteIndex].length = (srcOffset - readIndex) + 1;
                                }

                                readIndex = srcOffset;
                                continue;
                            }
                            break;
                        case MarkupTag.SLASH_A:
                            TextGeneratorUtilities.InsertClosingTextStyle(TextGeneratorUtilities.GetStyle(generationSettings, (int)MarkupTag.A), ref m_TextProcessingArray, ref writeIndex, ref m_TextStyleStackDepth, ref m_TextStyleStacks, ref generationSettings);
                            break;
                        case MarkupTag.SLASH_STYLE:
                            if (tagNoParsing) break;

                            int closeWriteIndex = writeIndex;
                            TextGeneratorUtilities.ReplaceClosingStyleTag(ref m_TextProcessingArray, ref writeIndex, ref m_TextStyleStackDepth, ref m_TextStyleStacks, ref generationSettings);

                            // Update potential text elements added by the closing style.
                            for (; closeWriteIndex < writeIndex; closeWriteIndex++)
                            {
                                m_TextProcessingArray[closeWriteIndex].stringIndex = readIndex;
                                m_TextProcessingArray[closeWriteIndex].length = 8;
                            }

                            readIndex += 7;
                            continue;
                    }

                    // Validate potential text markup element
                    // if (TryGetTextMarkupElement(m_TextBackingArray.Text, ref readIndex, out TextProcessingElement markupElement))
                    // {
                    //     m_TextProcessingArray[writeIndex] = markupElement;
                    //     writeIndex += 1;
                    //     continue;
                    // }
                }

                // Lookup character and glyph data
                // TODO: Add future implementation for character and glyph lookups
                if (writeIndex == m_TextProcessingArray.Length) TextGeneratorUtilities.ResizeInternalArray(ref m_TextProcessingArray);

                m_TextProcessingArray[writeIndex] = new TextProcessingElement { elementType = TextProcessingElementType.TextCharacterElement, stringIndex = readIndex, length = 1, unicode = c };

                writeIndex += 1;
            }

            m_TextStyleStackDepth = 0;

            // Insert Closing Style
            if (textStyle != null && textStyle.hashCode != (int)MarkupTag.NORMAL)
                TextGeneratorUtilities.InsertClosingStyleTag(ref m_TextProcessingArray, ref writeIndex, ref m_TextStyleStackDepth, ref m_TextStyleStacks, ref generationSettings);

            if (writeIndex == m_TextProcessingArray.Length) TextGeneratorUtilities.ResizeInternalArray(ref m_TextProcessingArray);

            m_TextProcessingArray[writeIndex].unicode = 0;
            m_InternalTextProcessingArraySize = writeIndex;
        }

        void InsertNewLine(int i, float baseScale, float currentElementScale, float currentEmScale, float boldSpacingAdjustment, float characterSpacingAdjustment, float width, float lineGap, ref bool isMaxVisibleDescenderSet, ref float maxVisibleDescender, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            // Adjust line spacing if necessary
            float baselineAdjustmentDelta = m_MaxLineAscender - m_StartOfLineAscender;
            if (m_LineOffset > 0 && Math.Abs(baselineAdjustmentDelta) > 0.01f && m_IsDrivenLineSpacing == false && !m_IsNewPage)
            {
                TextGeneratorUtilities.AdjustLineOffset(m_FirstCharacterOfLine, m_CharacterCount, baselineAdjustmentDelta, textInfo);
                m_MaxDescender -= baselineAdjustmentDelta;
                m_LineOffset += baselineAdjustmentDelta;
            }

            // Calculate lineAscender & make sure if last character is superscript or subscript that we check that as well.
            float lineAscender = m_MaxLineAscender - m_LineOffset;
            float lineDescender = m_MaxLineDescender - m_LineOffset;

            // Update maxDescender and maxVisibleDescender
            m_MaxDescender = m_MaxDescender < lineDescender ? m_MaxDescender : lineDescender;
            if (!isMaxVisibleDescenderSet)
                maxVisibleDescender = m_MaxDescender;

            if (generationSettings.useMaxVisibleDescender && (m_CharacterCount >= generationSettings.maxVisibleCharacters || m_LineNumber >= generationSettings.maxVisibleLines))
                isMaxVisibleDescenderSet = true;

            // Track & Store lineInfo for the new line
            textInfo.lineInfo[m_LineNumber].firstCharacterIndex = m_FirstCharacterOfLine;
            textInfo.lineInfo[m_LineNumber].firstVisibleCharacterIndex = m_FirstVisibleCharacterOfLine = m_FirstCharacterOfLine > m_FirstVisibleCharacterOfLine ? m_FirstCharacterOfLine : m_FirstVisibleCharacterOfLine;
            textInfo.lineInfo[m_LineNumber].lastCharacterIndex = m_LastCharacterOfLine = m_CharacterCount - 1 > 0 ? m_CharacterCount - 1 : 0;
            textInfo.lineInfo[m_LineNumber].lastVisibleCharacterIndex = m_LastVisibleCharacterOfLine = m_LastVisibleCharacterOfLine < m_FirstVisibleCharacterOfLine ? m_FirstVisibleCharacterOfLine : m_LastVisibleCharacterOfLine;

            textInfo.lineInfo[m_LineNumber].characterCount = textInfo.lineInfo[m_LineNumber].lastCharacterIndex - textInfo.lineInfo[m_LineNumber].firstCharacterIndex + 1;
            textInfo.lineInfo[m_LineNumber].visibleCharacterCount = m_LineVisibleCharacterCount;
            textInfo.lineInfo[m_LineNumber].visibleSpaceCount = m_LineVisibleSpaceCount;
            textInfo.lineInfo[m_LineNumber].lineExtents.min = new Vector2(textInfo.textElementInfo[m_FirstVisibleCharacterOfLine].bottomLeft.x, lineDescender);
            textInfo.lineInfo[m_LineNumber].lineExtents.max = new Vector2(textInfo.textElementInfo[m_LastVisibleCharacterOfLine].topRight.x, lineAscender);
            textInfo.lineInfo[m_LineNumber].length = textInfo.lineInfo[m_LineNumber].lineExtents.max.x;
            textInfo.lineInfo[m_LineNumber].width = width;

            float glyphAdjustment = textInfo.textElementInfo[m_LastVisibleCharacterOfLine].adjustedHorizontalAdvance;
            float maxAdvanceOffset = (glyphAdjustment * currentElementScale + (m_CurrentFontAsset.regularStyleSpacing + characterSpacingAdjustment + boldSpacingAdjustment) * currentEmScale + m_CSpacing) * (1 - generationSettings.charWidthMaxAdj);
            float adjustedHorizontalAdvance = textInfo.lineInfo[m_LineNumber].maxAdvance = textInfo.textElementInfo[m_LastVisibleCharacterOfLine].xAdvance + (generationSettings.isRightToLeft ? maxAdvanceOffset : -maxAdvanceOffset);
            textInfo.textElementInfo[m_LastVisibleCharacterOfLine].xAdvance = adjustedHorizontalAdvance;

            textInfo.lineInfo[m_LineNumber].baseline = 0 - m_LineOffset;
            textInfo.lineInfo[m_LineNumber].ascender = lineAscender;
            textInfo.lineInfo[m_LineNumber].descender = lineDescender;
            textInfo.lineInfo[m_LineNumber].lineHeight = lineAscender - lineDescender + lineGap * baseScale;

            m_FirstCharacterOfLine = m_CharacterCount; // Store first character of the next line.
            m_LineVisibleCharacterCount = 0;
            m_LineVisibleSpaceCount = 0;

            // Store the state of the line before starting on the new line.
            SaveWordWrappingState(ref m_SavedLineState, i, m_CharacterCount - 1, textInfo);

            m_LineNumber += 1;

            // Check to make sure Array is large enough to hold a new line.
            if (m_LineNumber >= textInfo.lineInfo.Length)
                TextGeneratorUtilities.ResizeLineExtents(m_LineNumber, textInfo);

            // Apply Line Spacing based on scale of the last character of the line.
            if (m_LineHeight == k_FloatUnset)
            {
                float ascender = textInfo.textElementInfo[m_CharacterCount].adjustedAscender;
                float lineOffsetDelta = 0 - m_MaxLineDescender + ascender + (lineGap + m_LineSpacingDelta) * baseScale + generationSettings.lineSpacing * currentEmScale;
                m_LineOffset += lineOffsetDelta;

                m_StartOfLineAscender = ascender;
            }
            else
            {
                m_LineOffset += m_LineHeight + generationSettings.lineSpacing * currentEmScale;
            }

            m_MaxLineAscender = TextGeneratorUtilities.largeNegativeFloat;
            m_MaxLineDescender = TextGeneratorUtilities.largePositiveFloat;

            m_XAdvance = 0 + m_TagIndent;
        }

        protected void DoMissingGlyphCallback(uint unicode, int stringIndex, FontAsset fontAsset, TextInfo textInfo)
        {
            // Event to allow users to modify the content of the text info before the text is rendered.
            OnMissingCharacter?.Invoke(unicode, stringIndex, textInfo, fontAsset);
        }

        void ClearMarkupTagAttributes()
        {
            int length = m_XmlAttribute.Length;
            for (int i = 0; i < length; i++)
                m_XmlAttribute[i] = new RichTextTagAttribute();
        }
    }
}
