// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.Bindings;
using UnityEngine.Profiling;
using UnityEngine.TextCore.LowLevel;

namespace UnityEngine.TextCore
{
    internal enum TextOverflowMode { Overflow = 0, Ellipsis = 1, Masking = 2, Truncate = 3, ScrollRect = 4, Page = 5, Linked = 6 }
    enum TextureMapping { Character = 0, Line = 1, Paragraph = 2, MatchAspect = 3 }

    internal class TextGenerationSettings
    {
        public string text = null;

        public Rect screenRect;
        public Vector4 margins;
        public float scale = 1f;

        public FontAsset fontAsset;
        public Material material;
        public TextSpriteAsset spriteAsset;
        public FontStyles fontStyle = FontStyles.Normal;

        public TextAlignment textAlignment = TextAlignment.TopLeft;
        public TextOverflowMode overflowMode = TextOverflowMode.Overflow;
        public bool wordWrap = false;
        public float wordWrappingRatio;

        public Color color = Color.white;
        public TextGradientPreset fontColorGradient;
        public bool tintSprites;
        public bool overrideRichTextColors;

        public float fontSize = 18;
        public bool autoSize;
        public float fontSizeMin;
        public float fontSizeMax;

        public bool enableKerning = true;
        public bool richText;
        public bool isRightToLeft;
        public bool extraPadding;
        public bool parseControlCharacters = true;

        public float characterSpacing;
        public float wordSpacing;
        public float lineSpacing;
        public float paragraphSpacing;
        public float lineSpacingMax;

        public int maxVisibleCharacters = 99999;
        public int maxVisibleWords = 99999;
        public int maxVisibleLines = 99999;
        public int firstVisibleCharacter = 0;
        public bool useMaxVisibleDescender;

        public FontWeight fontWeight = FontWeight.Regular;
        public int pageToDisplay = 1;

        public TextureMapping horizontalMapping = TextureMapping.Character;
        public TextureMapping verticalMapping = TextureMapping.Character;
        public float uvLineOffset;
        public VertexSortingOrder geometrySortingOrder = VertexSortingOrder.Normal;
        public bool inverseYAxis;

        public float charWidthMaxAdj;

        protected bool Equals(TextGenerationSettings other)
        {
            return string.Equals(text, other.text) && screenRect.Equals(other.screenRect) &&
                margins.Equals(other.margins) && scale.Equals(other.scale) &&
                Equals(fontAsset, other.fontAsset) && Equals(material, other.material) &&
                Equals(spriteAsset, other.spriteAsset) && fontStyle == other.fontStyle &&
                textAlignment == other.textAlignment && overflowMode == other.overflowMode &&
                wordWrap == other.wordWrap && wordWrappingRatio.Equals(other.wordWrappingRatio) &&
                color.Equals(other.color) && Equals(fontColorGradient, other.fontColorGradient) &&
                tintSprites == other.tintSprites &&
                overrideRichTextColors == other.overrideRichTextColors && fontSize.Equals(other.fontSize) &&
                autoSize == other.autoSize &&
                fontSizeMin.Equals(other.fontSizeMin) && fontSizeMax.Equals(other.fontSizeMax) &&
                enableKerning == other.enableKerning && richText == other.richText &&
                isRightToLeft == other.isRightToLeft && extraPadding == other.extraPadding &&
                parseControlCharacters == other.parseControlCharacters &&
                characterSpacing.Equals(other.characterSpacing) && wordSpacing.Equals(other.wordSpacing) &&
                lineSpacing.Equals(other.lineSpacing) &&
                paragraphSpacing.Equals(other.paragraphSpacing) && lineSpacingMax.Equals(other.lineSpacingMax) &&
                maxVisibleCharacters == other.maxVisibleCharacters &&
                maxVisibleWords == other.maxVisibleWords && maxVisibleLines == other.maxVisibleLines &&
                firstVisibleCharacter == other.firstVisibleCharacter &&
                useMaxVisibleDescender == other.useMaxVisibleDescender && fontWeight == other.fontWeight &&
                pageToDisplay == other.pageToDisplay &&
                horizontalMapping == other.horizontalMapping && verticalMapping == other.verticalMapping &&
                uvLineOffset.Equals(other.uvLineOffset) &&
                geometrySortingOrder == other.geometrySortingOrder && inverseYAxis == other.inverseYAxis &&
                charWidthMaxAdj.Equals(other.charWidthMaxAdj);
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
            unchecked
            {
                var hashCode = (text != null ? text.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ screenRect.GetHashCode();
                hashCode = (hashCode * 397) ^ margins.GetHashCode();
                hashCode = (hashCode * 397) ^ scale.GetHashCode();
                hashCode = (hashCode * 397) ^ (fontAsset != null ? fontAsset.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (material != null ? material.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (spriteAsset != null ? spriteAsset.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)fontStyle;
                hashCode = (hashCode * 397) ^ (int)textAlignment;
                hashCode = (hashCode * 397) ^ (int)overflowMode;
                hashCode = (hashCode * 397) ^ wordWrap.GetHashCode();
                hashCode = (hashCode * 397) ^ wordWrappingRatio.GetHashCode();
                hashCode = (hashCode * 397) ^ color.GetHashCode();
                hashCode = (hashCode * 397) ^ (fontColorGradient != null ? fontColorGradient.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ tintSprites.GetHashCode();
                hashCode = (hashCode * 397) ^ overrideRichTextColors.GetHashCode();
                hashCode = (hashCode * 397) ^ fontSize.GetHashCode();
                hashCode = (hashCode * 397) ^ autoSize.GetHashCode();
                hashCode = (hashCode * 397) ^ fontSizeMin.GetHashCode();
                hashCode = (hashCode * 397) ^ fontSizeMax.GetHashCode();
                hashCode = (hashCode * 397) ^ enableKerning.GetHashCode();
                hashCode = (hashCode * 397) ^ richText.GetHashCode();
                hashCode = (hashCode * 397) ^ isRightToLeft.GetHashCode();
                hashCode = (hashCode * 397) ^ extraPadding.GetHashCode();
                hashCode = (hashCode * 397) ^ parseControlCharacters.GetHashCode();
                hashCode = (hashCode * 397) ^ characterSpacing.GetHashCode();
                hashCode = (hashCode * 397) ^ wordSpacing.GetHashCode();
                hashCode = (hashCode * 397) ^ lineSpacing.GetHashCode();
                hashCode = (hashCode * 397) ^ paragraphSpacing.GetHashCode();
                hashCode = (hashCode * 397) ^ lineSpacingMax.GetHashCode();
                hashCode = (hashCode * 397) ^ maxVisibleCharacters;
                hashCode = (hashCode * 397) ^ maxVisibleWords;
                hashCode = (hashCode * 397) ^ maxVisibleLines;
                hashCode = (hashCode * 397) ^ firstVisibleCharacter;
                hashCode = (hashCode * 397) ^ useMaxVisibleDescender.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)fontWeight;
                hashCode = (hashCode * 397) ^ pageToDisplay;
                hashCode = (hashCode * 397) ^ (int)horizontalMapping;
                hashCode = (hashCode * 397) ^ (int)verticalMapping;
                hashCode = (hashCode * 397) ^ uvLineOffset.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)geometrySortingOrder;
                hashCode = (hashCode * 397) ^ inverseYAxis.GetHashCode();
                hashCode = (hashCode * 397) ^ charWidthMaxAdj.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator==(TextGenerationSettings left, TextGenerationSettings right)
        {
            return Equals(left, right);
        }

        public static bool operator!=(TextGenerationSettings left, TextGenerationSettings right)
        {
            return !Equals(left, right);
        }

        public void Copy(TextGenerationSettings other)
        {
            if (other == null)
                return;

            text = other.text;

            screenRect = other.screenRect;
            margins =  other.margins;
            scale = other.scale;

            fontAsset = other.fontAsset;
            material = other.material;
            spriteAsset = other.spriteAsset;
            fontStyle = other.fontStyle;

            textAlignment = other.textAlignment;
            overflowMode = other.overflowMode;
            wordWrap = other.wordWrap;
            wordWrappingRatio = other.wordWrappingRatio;

            color = other.color;
            fontColorGradient = other.fontColorGradient;
            tintSprites = other.tintSprites;
            overrideRichTextColors = other.overrideRichTextColors;

            fontSize = other.fontSize;
            autoSize = other.autoSize;
            fontSizeMin = other.fontSizeMin;
            fontSizeMax = other.fontSizeMax;

            enableKerning = other.enableKerning;
            richText = other.richText;
            isRightToLeft = other.isRightToLeft;
            extraPadding = other.extraPadding;
            parseControlCharacters = other.parseControlCharacters;

            characterSpacing = other.characterSpacing;
            wordSpacing = other.wordSpacing;
            lineSpacing = other.lineSpacing;
            paragraphSpacing = other.paragraphSpacing;
            lineSpacingMax = other.lineSpacingMax;

            maxVisibleCharacters = other.maxVisibleCharacters;
            maxVisibleWords = other.maxVisibleWords;
            maxVisibleLines = other.maxVisibleLines;
            firstVisibleCharacter = other.firstVisibleCharacter;
            useMaxVisibleDescender = other.useMaxVisibleDescender;

            fontWeight = other.fontWeight;
            pageToDisplay = other.pageToDisplay;

            horizontalMapping = other.horizontalMapping;
            verticalMapping = other.verticalMapping;
            uvLineOffset = other.uvLineOffset;
            geometrySortingOrder = other.geometrySortingOrder;
            inverseYAxis = other.inverseYAxis;

            charWidthMaxAdj = other.charWidthMaxAdj;
        }
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
            k_HorizontalEllipsis = 8230,
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
            k_CjkHalfwidthEnd = 0xFFEF;

        const int k_VerticesMax = 16383;
        const int k_SpritesStart = 57344;

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
                Debug.LogError("Null TextInfo provided to TextGenerator. Cannot update its content.");
                return;
            }

            TextGenerator textGenerator = GetTextGenerator();

            Profiler.BeginSample("TextGenerator.GenerateText");
            textGenerator.Prepare(settings, textInfo);
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

        public static Vector2 GetCursorPosition(TextInfo textInfo, Rect screenRect, int index)
        {
            if (textInfo.characterCount == 0)
            {
                return screenRect.position;
            }

            if (index >= textInfo.characterCount)
            {
                return new Vector2(textInfo.textElementInfo[textInfo.characterCount - 1].xAdvance + screenRect.position.x, screenRect.position.y);
            }

            return new Vector2(textInfo.textElementInfo[index].origin + screenRect.position.x, screenRect.position.y);
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

        Vector3[] m_RectTransformCorners = new Vector3[4];
        float m_MarginWidth;
        float m_MarginHeight;

        int[] m_CharBuffer = new int[k_MaxCharacters];
        float m_PreferredWidth;
        float m_PreferredHeight;
        FontAsset m_CurrentFontAsset;
        Material m_CurrentMaterial;
        int m_CurrentMaterialIndex;
        RichTextTagStack<MaterialReference> m_MaterialReferenceStack = new RichTextTagStack<MaterialReference>(new MaterialReference[16]);
        float m_Padding;
        TextSpriteAsset m_CurrentSpriteAsset;
        int m_TotalCharacterCount;
        float m_FontScale;
        float m_FontSize;
        float m_FontScaleMultiplier;
        float m_CurrentFontSize;
        RichTextTagStack<float> m_SizeStack = new RichTextTagStack<float>(16);

        FontStyles m_FontStyleInternal = FontStyles.Normal;
        FontStyleStack m_FontStyleStack;

        //FontWeight m_FontWeight = FontWeight.Regular;
        FontWeight m_FontWeightInternal = FontWeight.Regular;
        RichTextTagStack<FontWeight> m_FontWeightStack = new RichTextTagStack<FontWeight>(8);

        TextAlignment m_LineJustification;
        RichTextTagStack<TextAlignment> m_LineJustificationStack = new RichTextTagStack<TextAlignment>(16);
        float m_BaselineOffset;
        RichTextTagStack<float> m_BaselineOffsetStack = new RichTextTagStack<float>(new float[16]);
        Color32 m_FontColor32;
        Color32 m_HtmlColor;
        Color32 m_UnderlineColor;
        Color32 m_StrikethroughColor;
        RichTextTagStack<Color32> m_ColorStack = new RichTextTagStack<Color32>(new Color32[16]);
        RichTextTagStack<Color32> m_UnderlineColorStack = new RichTextTagStack<Color32>(new Color32[16]);
        RichTextTagStack<Color32> m_StrikethroughColorStack = new RichTextTagStack<Color32>(new Color32[16]);
        RichTextTagStack<Color32> m_HighlightColorStack = new RichTextTagStack<Color32>(new Color32[16]);
        TextGradientPreset m_ColorGradientPreset;
        RichTextTagStack<TextGradientPreset> m_ColorGradientStack = new RichTextTagStack<TextGradientPreset>(new TextGradientPreset[16]);
        RichTextTagStack<int> m_ActionStack = new RichTextTagStack<int>(new int[16]);
        bool m_IsFxMatrixSet;
        float m_LineOffset;
        float m_LineHeight;
        float m_CSpacing;
        float m_MonoSpacing;
        float m_XAdvance;
        float m_TagLineIndent;
        float m_TagIndent;
        RichTextTagStack<float> m_IndentStack = new RichTextTagStack<float>(new float[16]);
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
        int m_LoopCountA;
        TextElementType m_TextElementType;
        bool m_IsParsingText;
        int m_SpriteIndex;
        Color32 m_SpriteColor;
        TextElement m_CachedTextElement;
        Color32 m_HighlightColor;
        float m_CharWidthAdjDelta;
        Matrix4x4 m_FxMatrix;
        float m_MaxFontSize;
        float m_MinFontSize;
        bool m_IsCharacterWrappingEnabled;
        float m_StartOfLineAscender;
        float m_LineSpacingDelta;
        bool m_IsMaskingEnabled;
        MaterialReference[] m_MaterialReferences = new MaterialReference[16];
        int m_SpriteCount = 0;
        RichTextTagStack<int> m_StyleStack = new RichTextTagStack<int>(new int[16]);
        int m_SpriteAnimationId;

        /// <summary>
        /// Internal Array used in the parsing and layout of the text.
        /// </summary>
        uint[] m_InternalTextParsingBuffer = new uint[256];

        RichTextTagAttribute[] m_Attributes = new RichTextTagAttribute[8];

        XmlTagAttribute[] m_XmlAttribute = new XmlTagAttribute[8]; // To remove...
        char[] m_RichTextTag = new char[128];
        Dictionary<int, int> m_MaterialReferenceIndexLookup = new Dictionary<int, int>();
        bool m_IsCalculatingPreferredValues;
        TextSpriteAsset m_DefaultSpriteAsset;
        bool m_TintSprite;
        Character m_CachedEllipsisGlyphInfo;
        Character m_CachedUnderlineGlyphInfo;
        bool m_IsUsingBold;
        bool m_IsSdfShader;

        TextElementInfo[] m_InternalTextElementInfo;
        int m_RecursiveCount;

        void Prepare(TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            Profiler.BeginSample("TextGenerator.Prepare");
            // TODO: Find a way for GetPaddingForMaterial to not allocate
            // TODO: Hard coded padding value is temporary change to avoid clipping of text geometry with small point size.
            m_Padding = 6.0f; // generationSettings.extraPadding ? 5.5f : 1.5f;

            m_IsMaskingEnabled = false;

            // Find and cache Underline & Ellipsis characters.
            GetSpecialCharacters(generationSettings.fontAsset);

            ComputeMarginSize(generationSettings.screenRect, generationSettings.margins);

            //ParseInputText
            TextGeneratorUtilities.StringToCharArray(generationSettings.text, ref m_CharBuffer, ref m_StyleStack, generationSettings);
            SetArraySizes(m_CharBuffer, generationSettings, textInfo);

            // Reset Font min / max used with Auto-sizing
            if (generationSettings.autoSize)
                m_FontSize = Mathf.Clamp(generationSettings.fontSize, generationSettings.fontSizeMin, generationSettings.fontSizeMax);
            else
                m_FontSize = generationSettings.fontSize;

            m_MaxFontSize = generationSettings.fontSizeMax;
            m_MinFontSize = generationSettings.fontSizeMin;
            m_LineSpacingDelta = 0;
            m_CharWidthAdjDelta = 0;

            m_IsCharacterWrappingEnabled = false;
            Profiler.EndSample();
        }

        /// <summary>
        /// This is the main function that is responsible for creating / displaying the text.
        /// </summary>
        void GenerateTextMesh(TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            // Clear TextInfo
            if (textInfo == null)
                return;

            textInfo.Clear();

            // Early exit if we don't have any Text to generate.
            if (m_CharBuffer == null || m_CharBuffer.Length == 0 || m_CharBuffer[0] == (char)0)
            {
                // Clear mesh and upload changes to the mesh.
                ClearMesh(true, textInfo);

                m_PreferredWidth = 0;
                m_PreferredHeight = 0;

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
            float baseScale = m_FontScale = (m_FontSize / generationSettings.fontAsset.faceInfo.pointSize * generationSettings.fontAsset.faceInfo.scale);
            float currentElementScale = baseScale;
            m_FontScaleMultiplier = 1;

            m_CurrentFontSize = m_FontSize;
            m_SizeStack.SetDefault(m_CurrentFontSize);

            m_FontStyleInternal = generationSettings.fontStyle; // Set the default style.
            m_FontWeightInternal = (m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold ? FontWeight.Bold : generationSettings.fontWeight;
            m_FontWeightStack.SetDefault(m_FontWeightInternal);
            m_FontStyleStack.Clear();

            m_LineJustification = generationSettings.textAlignment; // Sets the line justification mode to match editor alignment.
            m_LineJustificationStack.SetDefault(m_LineJustification);

            float padding = 0;
            float boldXAdvanceMultiplier = 1; // Used to increase spacing between character when style is bold.

            m_BaselineOffset = 0; // Used by subscript characters.
            m_BaselineOffsetStack.Clear();

            // Underline
            bool beginUnderline = false;
            Vector3 underlineStart = Vector3.zero; // Used to track where underline starts & ends.
            Vector3 underlineEnd;

            // Strike-through
            bool beginStrikethrough = false;
            Vector3 strikethroughStart = Vector3.zero;
            Vector3 strikethroughEnd;

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
            m_HighlightColorStack.SetDefault(m_HtmlColor);

            m_ColorGradientPreset = null;
            m_ColorGradientStack.SetDefault(null);

            // Clear the Action stack.
            m_ActionStack.Clear();

            m_IsFxMatrixSet = false;

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
            m_LastCharacterOfLine = 0;
            m_FirstVisibleCharacterOfLine = 0;
            m_LastVisibleCharacterOfLine = 0;
            m_MaxLineAscender = TextGeneratorUtilities.largeNegativeFloat;
            m_MaxLineDescender = TextGeneratorUtilities.largePositiveFloat;
            m_LineNumber = 0;
            m_LineVisibleCharacterCount = 0;
            bool isStartOfNewLine = true;
            m_FirstOverflowCharacterIndex = -1;

            m_PageNumber = 0;
            int pageToDisplay = Mathf.Clamp(generationSettings.pageToDisplay - 1, 0, textInfo.pageInfo.Length - 1);
            int previousPageOverflowChar = 0;

            int ellipsisIndex = 0;

            Vector4 margins = generationSettings.margins;
            float marginWidth = m_MarginWidth;
            float marginHeight = m_MarginHeight;
            m_MarginLeft = 0;
            m_MarginRight = 0;
            m_Width = -1;
            float width = marginWidth + 0.0001f - m_MarginLeft - m_MarginRight;

            // Need to initialize these Extents structures
            m_MeshExtents.min = TextGeneratorUtilities.largePositiveVector2;
            m_MeshExtents.max = TextGeneratorUtilities.largeNegativeVector2;

            // Initialize lineInfo
            textInfo.ClearLineInfo();

            // Tracking of the highest Ascender
            m_MaxCapHeight = 0;
            m_MaxAscender = 0;
            m_MaxDescender = 0;
            float pageAscender = 0;
            float maxVisibleDescender = 0;
            bool isMaxVisibleDescenderSet = false;
            m_IsNewPage = false;

            // Word Wrapping related
            bool isFirstWord = true;
            m_IsNonBreakingSpace = false;
            bool ignoreNonBreakingSpace = false;
            bool isLastBreakingChar = false;
            int wrappingIndex = 0;

            // Save character and line state before we begin layout.
            SaveWordWrappingState(ref m_SavedWordWrapState, -1, -1, textInfo);
            SaveWordWrappingState(ref m_SavedLineState, -1, -1, textInfo);

            m_LoopCountA += 1;

            int endTagIndex;

            // Parse through Character buffer to read HTML tags and begin creating mesh.
            for (int i = 0; i < m_CharBuffer.Length && m_CharBuffer[i] != 0; i++)
            {
                var charCode = m_CharBuffer[i]; // Holds the character code of the currently being processed character.

                // Parse Rich Text Tag
                if (generationSettings.richText && charCode == k_LesserThan)
                {
                    m_IsParsingText = true;
                    m_TextElementType = TextElementType.Character;

                    // Check if Tag is valid. If valid, skip to the end of the validated tag.
                    if (ValidateHtmlTag(m_CharBuffer, i + 1, out endTagIndex, generationSettings, textInfo))
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

                int prevMaterialIndex = m_CurrentMaterialIndex;

                bool isUsingAltTypeface = textInfo.textElementInfo[m_CharacterCount].isUsingAlternateTypeface;

                m_IsParsingText = false;

                // When using Linked text, mark character as ignored and skip to next character.
                if (m_CharacterCount < generationSettings.firstVisibleCharacter)
                {
                    textInfo.textElementInfo[m_CharacterCount].isVisible = false;
                    textInfo.textElementInfo[m_CharacterCount].character = (char)k_ZeroWidthSpace;
                    m_CharacterCount += 1;
                    continue;
                }

                // Handle Font Styles like LowerCase, UpperCase and SmallCaps.

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

                // Look up Character Data from Dictionary and cache it.

                if (m_TextElementType == TextElementType.Sprite)
                {
                    // If a sprite is used as a fallback then get a reference to it and set the color to white.
                    m_CurrentSpriteAsset = textInfo.textElementInfo[m_CharacterCount].spriteAsset;
                    m_SpriteIndex = textInfo.textElementInfo[m_CharacterCount].spriteIndex;

                    SpriteCharacter sprite = m_CurrentSpriteAsset.spriteCharacterTable[m_SpriteIndex];
                    if (sprite == null)
                        continue;

                    // Sprites are assigned in the E000 Private Area + sprite Index
                    if (charCode == k_LesserThan)
                        charCode = k_SpritesStart + m_SpriteIndex;
                    else
                        m_SpriteColor = Color.white;

                    // The sprite scale calculations are based on the font asset assigned to the text object.
                    float spriteScale = (m_CurrentFontSize / m_CurrentFontAsset.faceInfo.pointSize * m_CurrentFontAsset.faceInfo.scale);
                    currentElementScale = m_CurrentFontAsset.faceInfo.ascentLine / sprite.glyph.metrics.height * sprite.scale * spriteScale;

                    m_CachedTextElement = sprite;

                    textInfo.textElementInfo[m_CharacterCount].elementType = TextElementType.Sprite;
                    textInfo.textElementInfo[m_CharacterCount].scale = spriteScale;
                    textInfo.textElementInfo[m_CharacterCount].spriteAsset = m_CurrentSpriteAsset;
                    textInfo.textElementInfo[m_CharacterCount].fontAsset = m_CurrentFontAsset;
                    textInfo.textElementInfo[m_CharacterCount].materialReferenceIndex = m_CurrentMaterialIndex;

                    m_CurrentMaterialIndex = prevMaterialIndex;

                    padding = 0;
                }
                else if (m_TextElementType == TextElementType.Character)
                {
                    m_CachedTextElement = textInfo.textElementInfo[m_CharacterCount].textElement;
                    if (m_CachedTextElement == null)
                        continue;

                    m_CurrentFontAsset = textInfo.textElementInfo[m_CharacterCount].fontAsset;
                    m_CurrentMaterial = textInfo.textElementInfo[m_CharacterCount].material;
                    m_CurrentMaterialIndex = textInfo.textElementInfo[m_CharacterCount].materialReferenceIndex;

                    // Re-calculate font scale as the font asset may have changed.
                    m_FontScale = m_CurrentFontSize * smallCapsMultiplier / m_CurrentFontAsset.faceInfo.pointSize * m_CurrentFontAsset.faceInfo.scale;

                    currentElementScale = m_FontScale * m_FontScaleMultiplier * m_CachedTextElement.scale;

                    textInfo.textElementInfo[m_CharacterCount].elementType = TextElementType.Character;
                    textInfo.textElementInfo[m_CharacterCount].scale = currentElementScale;

                    padding = m_CurrentMaterialIndex == 0 ? m_Padding : GetPaddingForMaterial(m_CurrentMaterial, generationSettings.extraPadding);
                }

                // Handle Soft Hyphen
                float oldScale = currentElementScale;
                if (charCode == k_SoftHyphen)
                {
                    currentElementScale = 0;
                }

                // Store some of the text object's information
                textInfo.textElementInfo[m_CharacterCount].character = (char)charCode;
                textInfo.textElementInfo[m_CharacterCount].pointSize = m_CurrentFontSize;
                textInfo.textElementInfo[m_CharacterCount].color = m_HtmlColor;
                textInfo.textElementInfo[m_CharacterCount].underlineColor = m_UnderlineColor;
                textInfo.textElementInfo[m_CharacterCount].strikethroughColor = m_StrikethroughColor;
                textInfo.textElementInfo[m_CharacterCount].highlightColor = m_HighlightColor;
                textInfo.textElementInfo[m_CharacterCount].style = m_FontStyleInternal;
                textInfo.textElementInfo[m_CharacterCount].index = i;

                // Handle Kerning if Enabled.
                GlyphValueRecord glyphAdjustments = new GlyphValueRecord();
                if (generationSettings.enableKerning)
                {
                    KerningPair adjustmentPair;

                    if (m_CharacterCount < totalCharacterCount - 1)
                    {
                        uint nextGlyph = textInfo.textElementInfo[m_CharacterCount + 1].character;
                        KerningPairKey keyValue = new KerningPairKey((uint)charCode, nextGlyph);

                        m_CurrentFontAsset.kerningLookupDictionary.TryGetValue((int)keyValue.key, out adjustmentPair);
                        if (adjustmentPair != null)
                            glyphAdjustments = adjustmentPair.firstGlyphAdjustments;
                    }

                    if (m_CharacterCount >= 1)
                    {
                        uint previousGlyph = textInfo.textElementInfo[m_CharacterCount - 1].character;
                        KerningPairKey keyValue = new KerningPairKey(previousGlyph, (uint)charCode);

                        m_CurrentFontAsset.kerningLookupDictionary.TryGetValue((int)keyValue.key, out adjustmentPair);
                        if (adjustmentPair != null)
                            glyphAdjustments += adjustmentPair.secondGlyphAdjustments;
                    }
                }


                // Initial Implementation for RTL support.
                if (generationSettings.isRightToLeft)
                {
                    m_XAdvance -= ((m_CachedTextElement.glyph.metrics.horizontalAdvance * boldXAdvanceMultiplier + generationSettings.characterSpacing + generationSettings.wordSpacing + m_CurrentFontAsset.regularStyleSpacing) * currentElementScale + m_CSpacing) * (1 - m_CharWidthAdjDelta);

                    if (char.IsWhiteSpace((char)charCode) || charCode == k_ZeroWidthSpace)
                        m_XAdvance -= generationSettings.wordSpacing * currentElementScale;
                }

                // Handle Mono Spacing
                float monoAdvance = 0;
                if (m_MonoSpacing != 0)
                {
                    monoAdvance = (m_MonoSpacing / 2 - (m_CachedTextElement.glyph.metrics.width / 2 + m_CachedTextElement.glyph.metrics.horizontalBearingX) * currentElementScale) * (1 - m_CharWidthAdjDelta);
                    m_XAdvance += monoAdvance;
                }

                // Set Padding based on selected font style
                float stylePadding; // Extra padding required to accommodate Bold style.
                if (m_TextElementType == TextElementType.Character && !isUsingAltTypeface && ((m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold)) // Checks for any combination of Bold Style.
                {
                    if (m_CurrentMaterial.HasProperty(ShaderUtilities.ID_GradientScale))
                    {
                        float gradientScale = m_CurrentMaterial.GetFloat(ShaderUtilities.ID_GradientScale);
                        stylePadding = m_CurrentFontAsset.boldStyleWeight / 4.0f * gradientScale * m_CurrentMaterial.GetFloat(ShaderUtilities.ID_ScaleRatio_A);

                        // Clamp overall padding to Gradient Scale size.
                        if (stylePadding + padding > gradientScale)
                            padding = gradientScale - stylePadding;
                    }
                    else
                        stylePadding = 0;

                    boldXAdvanceMultiplier = 1 + m_CurrentFontAsset.boldStyleSpacing * 0.01f;
                }
                else
                {
                    if (m_CurrentMaterial.HasProperty(ShaderUtilities.ID_GradientScale))
                    {
                        float gradientScale = m_CurrentMaterial.GetFloat(ShaderUtilities.ID_GradientScale);
                        stylePadding = m_CurrentFontAsset.regularStyleWeight / 4.0f * gradientScale * m_CurrentMaterial.GetFloat(ShaderUtilities.ID_ScaleRatio_A);

                        // Clamp overall padding to Gradient Scale size.
                        if (stylePadding + padding > gradientScale)
                            padding = gradientScale - stylePadding;
                    }
                    else
                        stylePadding = 0;

                    boldXAdvanceMultiplier = 1.0f;
                }

                // Determine the position of the vertices of the Character or Sprite.
                float fontBaseLineOffset = m_CurrentFontAsset.faceInfo.baseline * m_FontScale * m_FontScaleMultiplier * m_CurrentFontAsset.faceInfo.scale;
                Vector3 topLeft;
                topLeft.x = m_XAdvance + ((m_CachedTextElement.glyph.metrics.horizontalBearingX - padding - stylePadding + glyphAdjustments.xPlacement) * currentElementScale * (1 - m_CharWidthAdjDelta));
                topLeft.y = fontBaseLineOffset + (m_CachedTextElement.glyph.metrics.horizontalBearingY + padding + glyphAdjustments.yPlacement) * currentElementScale - m_LineOffset + m_BaselineOffset;
                topLeft.z = 0;

                Vector3 bottomLeft;
                bottomLeft.x = topLeft.x;
                bottomLeft.y = topLeft.y - ((m_CachedTextElement.glyph.metrics.height + padding * 2) * currentElementScale);
                bottomLeft.z = 0;

                Vector3 topRight;
                topRight.x = bottomLeft.x + ((m_CachedTextElement.glyph.metrics.width + padding * 2 + stylePadding * 2) * currentElementScale * (1 - m_CharWidthAdjDelta));
                topRight.y = topLeft.y;
                topRight.z = 0;

                Vector3 bottomRight;
                bottomRight.x = topRight.x;
                bottomRight.y = bottomLeft.y;
                bottomRight.z = 0;

                // Check if we need to Shear the rectangles for Italic styles
                if (m_TextElementType == TextElementType.Character && !isUsingAltTypeface && ((m_FontStyleInternal & FontStyles.Italic) == FontStyles.Italic))
                {
                    // Shift Top vertices forward by half (Shear Value * height of character) and Bottom vertices back by same amount.
                    float shearValue = m_CurrentFontAsset.italicStyleSlant * 0.01f;
                    Vector3 topShear = new Vector3(shearValue * ((m_CachedTextElement.glyph.metrics.horizontalBearingY + padding + stylePadding) * currentElementScale), 0, 0);
                    Vector3 bottomShear = new Vector3(shearValue * (((m_CachedTextElement.glyph.metrics.horizontalBearingY - m_CachedTextElement.glyph.metrics.height - padding - stylePadding)) * currentElementScale), 0, 0);

                    topLeft = topLeft + topShear;
                    bottomLeft = bottomLeft + bottomShear;
                    topRight = topRight + topShear;
                    bottomRight = bottomRight + bottomShear;
                }

                // Handle Character Rotation
                if (m_IsFxMatrixSet)
                {
                    Vector3 positionOffset = (topRight + bottomLeft) / 2;

                    topLeft = m_FxMatrix.MultiplyPoint3x4(topLeft - positionOffset) + positionOffset;
                    bottomLeft = m_FxMatrix.MultiplyPoint3x4(bottomLeft - positionOffset) + positionOffset;
                    topRight = m_FxMatrix.MultiplyPoint3x4(topRight - positionOffset) + positionOffset;
                    bottomRight = m_FxMatrix.MultiplyPoint3x4(bottomRight - positionOffset) + positionOffset;
                }

                // Store vertex information for the character or sprite.
                textInfo.textElementInfo[m_CharacterCount].bottomLeft = bottomLeft;
                textInfo.textElementInfo[m_CharacterCount].topLeft = topLeft;
                textInfo.textElementInfo[m_CharacterCount].topRight = topRight;
                textInfo.textElementInfo[m_CharacterCount].bottomRight = bottomRight;

                textInfo.textElementInfo[m_CharacterCount].origin = m_XAdvance;
                textInfo.textElementInfo[m_CharacterCount].baseLine = fontBaseLineOffset - m_LineOffset + m_BaselineOffset;
                textInfo.textElementInfo[m_CharacterCount].aspectRatio = (topRight.x - bottomLeft.x) / (topLeft.y - bottomLeft.y);


                // Compute and save text element Ascender and maximum line Ascender.
                float elementAscender = m_CurrentFontAsset.faceInfo.ascentLine * (m_TextElementType == TextElementType.Character ? currentElementScale / smallCapsMultiplier : textInfo.textElementInfo[m_CharacterCount].scale) + m_BaselineOffset;
                textInfo.textElementInfo[m_CharacterCount].ascender = elementAscender - m_LineOffset;
                m_MaxLineAscender = elementAscender > m_MaxLineAscender ? elementAscender : m_MaxLineAscender;

                // Compute and save text element Descender and maximum line Descender.
                float elementDescender = m_CurrentFontAsset.faceInfo.descentLine * (m_TextElementType == TextElementType.Character ? currentElementScale / smallCapsMultiplier : textInfo.textElementInfo[m_CharacterCount].scale) + m_BaselineOffset;
                float elementDescenderIi = textInfo.textElementInfo[m_CharacterCount].descender = elementDescender - m_LineOffset;
                m_MaxLineDescender = elementDescender < m_MaxLineDescender ? elementDescender : m_MaxLineDescender;

                // Adjust maxLineAscender and maxLineDescender if style is superscript or subscript
                if ((m_FontStyleInternal & FontStyles.Subscript) == FontStyles.Subscript || (m_FontStyleInternal & FontStyles.Superscript) == FontStyles.Superscript)
                {
                    float baseAscender = (elementAscender - m_BaselineOffset) / m_CurrentFontAsset.faceInfo.subscriptSize;
                    elementAscender = m_MaxLineAscender;
                    m_MaxLineAscender = baseAscender > m_MaxLineAscender ? baseAscender : m_MaxLineAscender;

                    float baseDescender = (elementDescender - m_BaselineOffset) / m_CurrentFontAsset.faceInfo.subscriptSize;
                    elementDescender = m_MaxLineDescender;
                    m_MaxLineDescender = baseDescender < m_MaxLineDescender ? baseDescender : m_MaxLineDescender;
                }

                if (m_LineNumber == 0 || m_IsNewPage)
                {
                    m_MaxAscender = m_MaxAscender > elementAscender ? m_MaxAscender : elementAscender;
                    m_MaxCapHeight = Mathf.Max(m_MaxCapHeight, m_CurrentFontAsset.faceInfo.capLine * currentElementScale / smallCapsMultiplier);
                }
                if (m_LineOffset == 0)
                    pageAscender = pageAscender > elementAscender ? pageAscender : elementAscender;


                // Set Characters to not visible by default.
                textInfo.textElementInfo[m_CharacterCount].isVisible = false;

                // Setup Mesh for visible text elements. ie. not a SPACE / LINEFEED / CARRIAGE RETURN.
                float lineOffsetDelta;
                if (charCode == k_Tab || charCode == k_NoBreakSpace || charCode == k_FigureSpace || (!char.IsWhiteSpace((char)charCode) && charCode != k_ZeroWidthSpace) || m_TextElementType == TextElementType.Sprite)
                {
                    textInfo.textElementInfo[m_CharacterCount].isVisible = true;

                    width = m_Width != -1 ? Mathf.Min(marginWidth + 0.0001f - m_MarginLeft - m_MarginRight, m_Width) : marginWidth + 0.0001f - m_MarginLeft - m_MarginRight;
                    textInfo.lineInfo[m_LineNumber].marginLeft = m_MarginLeft;

                    bool isJustifiedOrFlush = ((HorizontalAlignment)m_LineJustification & HorizontalAlignment.Flush) == HorizontalAlignment.Flush || ((HorizontalAlignment)m_LineJustification & HorizontalAlignment.Justified) == HorizontalAlignment.Justified;

                    // Calculate the line breaking width of the text.
                    var linebreakingWidth = Mathf.Abs(m_XAdvance) + (!generationSettings.isRightToLeft ? m_CachedTextElement.glyph.metrics.horizontalAdvance : 0) * (1 - m_CharWidthAdjDelta) * (charCode != 0xAD ? currentElementScale : oldScale);

                    // Check if Character exceeds the width of the Text Container
                    if (linebreakingWidth > width * (isJustifiedOrFlush ? 1.05f : 1.0f))
                    {
                        ellipsisIndex = m_CharacterCount - 1; // Last safely rendered character

                        // Word Wrapping
                        if (generationSettings.wordWrap && m_CharacterCount != m_FirstCharacterOfLine)
                        {
                            // Check if word wrapping is still possible
                            if (wrappingIndex == m_SavedWordWrapState.previousWordBreak || isFirstWord)
                            {
                                // Word wrapping is no longer possible. Shrink size of text if auto-sizing is enabled.
                                if (generationSettings.autoSize && m_FontSize > generationSettings.fontSizeMin)
                                {
                                    // Handle Character Width Adjustments
                                    if (m_CharWidthAdjDelta < generationSettings.charWidthMaxAdj / 100)
                                    {
                                        m_LoopCountA = 0;
                                        m_CharWidthAdjDelta += 0.01f;
                                        GenerateTextMesh(generationSettings, textInfo);

                                        return;
                                    }

                                    // Adjust Point Size
                                    m_MaxFontSize = m_FontSize;

                                    m_FontSize -= Mathf.Max((m_FontSize - m_MinFontSize) / 2, 0.05f);
                                    m_FontSize = (int)(Mathf.Max(m_FontSize, generationSettings.fontSizeMin) * 20 + 0.5f) / 20f;

                                    if (m_LoopCountA > 20)
                                    {
                                        return; // Added to debug
                                    }
                                    GenerateTextMesh(generationSettings, textInfo);

                                    return;
                                }

                                // Word wrapping is no longer possible, now breaking up individual words.
                                if (m_IsCharacterWrappingEnabled == false)
                                {
                                    if (ignoreNonBreakingSpace == false)
                                        ignoreNonBreakingSpace = true;
                                    else
                                        m_IsCharacterWrappingEnabled = true;
                                }
                                else
                                    isLastBreakingChar = true;
                            }


                            // Restore to previously stored state of last valid (space character or linefeed)
                            i = RestoreWordWrappingState(ref m_SavedWordWrapState, textInfo);
                            wrappingIndex = i; // Used to detect when line length can no longer be reduced.

                            // Handling for Soft Hyphen
                            if (m_CharBuffer[i] == k_SoftHyphen)
                            {
                                m_CharBuffer[i] = k_HyphenMinus;
                                GenerateTextMesh(generationSettings, textInfo);
                                return;
                            }

                            // Check if Line Spacing of previous line needs to be adjusted.
                            if (m_LineNumber > 0 && !TextGeneratorUtilities.Approximately(m_MaxLineAscender, m_StartOfLineAscender) && m_LineHeight == k_FloatUnset && !m_IsNewPage)
                            {
                                float offsetDelta = m_MaxLineAscender - m_StartOfLineAscender;
                                TextGeneratorUtilities.AdjustLineOffset(m_FirstCharacterOfLine, m_CharacterCount, offsetDelta, textInfo);
                                m_LineOffset += offsetDelta;
                                m_SavedWordWrapState.lineOffset = m_LineOffset;
                                m_SavedWordWrapState.previousLineAscender = m_MaxLineAscender;
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

                            // Track & Store lineInfo for the new line
                            textInfo.lineInfo[m_LineNumber].firstCharacterIndex = m_FirstCharacterOfLine;
                            textInfo.lineInfo[m_LineNumber].firstVisibleCharacterIndex = m_FirstVisibleCharacterOfLine = m_FirstCharacterOfLine > m_FirstVisibleCharacterOfLine ? m_FirstCharacterOfLine : m_FirstVisibleCharacterOfLine;
                            textInfo.lineInfo[m_LineNumber].lastCharacterIndex = m_LastCharacterOfLine = m_CharacterCount - 1 > 0 ? m_CharacterCount - 1 : 0;
                            textInfo.lineInfo[m_LineNumber].lastVisibleCharacterIndex = m_LastVisibleCharacterOfLine = m_LastVisibleCharacterOfLine < m_FirstVisibleCharacterOfLine ? m_FirstVisibleCharacterOfLine : m_LastVisibleCharacterOfLine;

                            textInfo.lineInfo[m_LineNumber].characterCount = textInfo.lineInfo[m_LineNumber].lastCharacterIndex - textInfo.lineInfo[m_LineNumber].firstCharacterIndex + 1;
                            textInfo.lineInfo[m_LineNumber].visibleCharacterCount = m_LineVisibleCharacterCount;
                            textInfo.lineInfo[m_LineNumber].lineExtents.min = new Vector2(textInfo.textElementInfo[m_FirstVisibleCharacterOfLine].bottomLeft.x, lineDescender);
                            textInfo.lineInfo[m_LineNumber].lineExtents.max = new Vector2(textInfo.textElementInfo[m_LastVisibleCharacterOfLine].topRight.x, lineAscender);
                            textInfo.lineInfo[m_LineNumber].length = textInfo.lineInfo[m_LineNumber].lineExtents.max.x;
                            textInfo.lineInfo[m_LineNumber].width = width;

                            textInfo.lineInfo[m_LineNumber].maxAdvance = textInfo.textElementInfo[m_LastVisibleCharacterOfLine].xAdvance - (generationSettings.characterSpacing + m_CurrentFontAsset.regularStyleSpacing) * currentElementScale - m_CSpacing;

                            textInfo.lineInfo[m_LineNumber].baseline = 0 - m_LineOffset;
                            textInfo.lineInfo[m_LineNumber].ascender = lineAscender;
                            textInfo.lineInfo[m_LineNumber].descender = lineDescender;
                            textInfo.lineInfo[m_LineNumber].lineHeight = lineAscender - lineDescender + lineGap * baseScale;

                            m_FirstCharacterOfLine = m_CharacterCount; // Store first character of the next line.
                            m_LineVisibleCharacterCount = 0;

                            // Store the state of the line before starting on the new line.
                            SaveWordWrappingState(ref m_SavedLineState, i, m_CharacterCount - 1, textInfo);

                            m_LineNumber += 1;
                            isStartOfNewLine = true;
                            isFirstWord = true;

                            // Check to make sure Array is large enough to hold a new line.
                            if (m_LineNumber >= textInfo.lineInfo.Length)
                                TextGeneratorUtilities.ResizeLineExtents(m_LineNumber, textInfo);

                            // Apply Line Spacing based on scale of the last character of the line.
                            if (m_LineHeight == k_FloatUnset)
                            {
                                float ascender = textInfo.textElementInfo[m_CharacterCount].ascender - textInfo.textElementInfo[m_CharacterCount].baseLine;
                                lineOffsetDelta = 0 - m_MaxLineDescender + ascender + (lineGap + generationSettings.lineSpacing + m_LineSpacingDelta) * baseScale;
                                m_LineOffset += lineOffsetDelta;

                                m_StartOfLineAscender = ascender;
                            }
                            else
                                m_LineOffset += m_LineHeight + generationSettings.lineSpacing * baseScale;

                            m_MaxLineAscender = TextGeneratorUtilities.largeNegativeFloat;
                            m_MaxLineDescender = TextGeneratorUtilities.largePositiveFloat;

                            m_XAdvance = 0 + m_TagIndent;

                            continue;
                        }

                        // Text Auto-Sizing (text exceeding Width of container.
                        if (generationSettings.autoSize && m_FontSize > generationSettings.fontSizeMin)
                        {
                            // Handle Character Width Adjustments
                            if (m_CharWidthAdjDelta < generationSettings.charWidthMaxAdj / 100)
                            {
                                m_LoopCountA = 0;
                                m_CharWidthAdjDelta += 0.01f;
                                GenerateTextMesh(generationSettings, textInfo);
                                return;
                            }

                            // Adjust Point Size
                            m_MaxFontSize = m_FontSize;

                            m_FontSize -= Mathf.Max((m_FontSize - m_MinFontSize) / 2, 0.05f);
                            m_FontSize = (int)(Mathf.Max(m_FontSize, generationSettings.fontSizeMin) * 20 + 0.5f) / 20f;

                            if (m_LoopCountA > 20)
                            {
                                return; // Added to debug
                            }
                            GenerateTextMesh(generationSettings, textInfo);
                            return;
                        }

                        // Handle Text Overflow
                        switch (generationSettings.overflowMode)
                        {
                            case TextOverflowMode.Overflow:
                                if (m_IsMaskingEnabled)
                                    DisableMasking();

                                break;
                            case TextOverflowMode.Ellipsis:
                                if (m_IsMaskingEnabled)
                                    DisableMasking();

                                if (m_CharacterCount < 1)
                                {
                                    textInfo.textElementInfo[m_CharacterCount].isVisible = false;
                                    break;
                                }

                                m_CharBuffer[i - 1] = k_HorizontalEllipsis;
                                m_CharBuffer[i] = (char)0;

                                if (m_CachedEllipsisGlyphInfo != null)
                                {
                                    textInfo.textElementInfo[ellipsisIndex].character = (char)k_HorizontalEllipsis;
                                    textInfo.textElementInfo[ellipsisIndex].textElement = m_CachedEllipsisGlyphInfo;
                                    textInfo.textElementInfo[ellipsisIndex].fontAsset = m_MaterialReferences[0].fontAsset;
                                    textInfo.textElementInfo[ellipsisIndex].material = m_MaterialReferences[0].material;
                                    textInfo.textElementInfo[ellipsisIndex].materialReferenceIndex = 0;
                                }
                                else
                                {
                                    Debug.LogWarning("Unable to use Ellipsis character since it wasn't found in the current Font Asset [" + generationSettings.fontAsset.name + "]. Consider regenerating this font asset to include the Ellipsis character (u+2026).");
                                }

                                m_TotalCharacterCount = ellipsisIndex + 1;

                                GenerateTextMesh(generationSettings, textInfo);
                                return;
                            case TextOverflowMode.Masking:
                                if (!m_IsMaskingEnabled)
                                    EnableMasking();
                                break;
                            case TextOverflowMode.ScrollRect:
                                if (!m_IsMaskingEnabled)
                                    EnableMasking();
                                break;
                            case TextOverflowMode.Truncate:
                                if (m_IsMaskingEnabled)
                                    DisableMasking();

                                textInfo.textElementInfo[m_CharacterCount].isVisible = false;
                                break;
                            case TextOverflowMode.Linked:
                                break;
                        }
                    }

                    // Special handling of characters that are not ignored at the end of a line.
                    if (charCode == k_Tab || charCode == k_NoBreakSpace || charCode == k_FigureSpace)
                    {
                        textInfo.textElementInfo[m_CharacterCount].isVisible = false;
                        m_LastVisibleCharacterOfLine = m_CharacterCount;
                        textInfo.lineInfo[m_LineNumber].spaceCount += 1;
                        textInfo.spaceCount += 1;
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
                    }

                    // Increase visible count for Characters.
                    if (textInfo.textElementInfo[m_CharacterCount].isVisible && charCode != k_SoftHyphen)
                    {
                        if (isStartOfNewLine)
                        {
                            isStartOfNewLine = false;
                            m_FirstVisibleCharacterOfLine = m_CharacterCount;
                        }

                        m_LineVisibleCharacterCount += 1;
                        m_LastVisibleCharacterOfLine = m_CharacterCount;
                    }
                }
                else
                {
                    // This is a Space, Tab, LineFeed or Carriage Return

                    // Track # of spaces per line which is used for line justification.
                    if ((charCode == k_LineFeed || char.IsSeparator((char)charCode)) && charCode != k_SoftHyphen && charCode != k_ZeroWidthSpace && charCode != k_WordJoiner)
                    {
                        textInfo.lineInfo[m_LineNumber].spaceCount += 1;
                        textInfo.spaceCount += 1;

                        if (charCode == 0xA0)
                            textInfo.lineInfo[m_LineNumber].controlCharacterCount = +1;
                    }
                }


                // Check if Line Spacing of previous line needs to be adjusted.
                if (m_LineNumber > 0 && !TextGeneratorUtilities.Approximately(m_MaxLineAscender, m_StartOfLineAscender) && m_LineHeight == k_FloatUnset && !m_IsNewPage)
                {
                    float offsetDelta = m_MaxLineAscender - m_StartOfLineAscender;
                    TextGeneratorUtilities.AdjustLineOffset(m_FirstCharacterOfLine, m_CharacterCount, offsetDelta, textInfo);
                    elementDescenderIi -= offsetDelta;
                    m_LineOffset += offsetDelta;

                    m_StartOfLineAscender += offsetDelta;
                    m_SavedWordWrapState.lineOffset = m_LineOffset;
                    m_SavedWordWrapState.previousLineAscender = m_StartOfLineAscender;
                }

                // Store Rectangle positions for each Character.
                textInfo.textElementInfo[m_CharacterCount].lineNumber = m_LineNumber;
                textInfo.textElementInfo[m_CharacterCount].pageNumber = m_PageNumber;

                if (charCode != k_LineFeed && charCode != k_CarriageReturn && charCode != k_HorizontalEllipsis || textInfo.lineInfo[m_LineNumber].characterCount == 1)
                    textInfo.lineInfo[m_LineNumber].alignment = m_LineJustification;


                // Check if text Exceeds the vertical bounds of the margin area.
                if (m_MaxAscender - elementDescenderIi > marginHeight + 0.0001f)
                {
                    // Handle Line spacing adjustments
                    if (generationSettings.autoSize && m_LineSpacingDelta > generationSettings.lineSpacingMax && m_LineNumber > 0)
                    {
                        m_LoopCountA = 0;

                        m_LineSpacingDelta -= 1;
                        GenerateTextMesh(generationSettings, textInfo);
                        return;
                    }

                    // Handle Text Auto-sizing resulting from text exceeding vertical bounds.
                    if (generationSettings.autoSize && m_FontSize > generationSettings.fontSizeMin)
                    {
                        m_MaxFontSize = m_FontSize;

                        m_FontSize -= Mathf.Max((m_FontSize - m_MinFontSize) / 2, 0.05f);
                        m_FontSize = (int)(Mathf.Max(m_FontSize, generationSettings.fontSizeMin) * 20 + 0.5f) / 20f;

                        if (m_LoopCountA > 20)
                        {
                            return; // Added to debug
                        }
                        GenerateTextMesh(generationSettings, textInfo);
                        return;
                    }

                    // Set isTextOverflowing and firstOverflowCharacterIndex
                    if (m_FirstOverflowCharacterIndex == -1)
                        m_FirstOverflowCharacterIndex = m_CharacterCount;

                    // Handle Text Overflow
                    switch (generationSettings.overflowMode)
                    {
                        case TextOverflowMode.Overflow:
                            if (m_IsMaskingEnabled)
                                DisableMasking();

                            break;
                        case TextOverflowMode.Ellipsis:
                            if (m_IsMaskingEnabled)
                                DisableMasking();

                            if (m_LineNumber > 0)
                            {
                                m_CharBuffer[textInfo.textElementInfo[ellipsisIndex].index] = k_HorizontalEllipsis;
                                m_CharBuffer[textInfo.textElementInfo[ellipsisIndex].index + 1] = (char)0;

                                if (m_CachedEllipsisGlyphInfo != null)
                                {
                                    textInfo.textElementInfo[ellipsisIndex].character = (char)k_HorizontalEllipsis;
                                    textInfo.textElementInfo[ellipsisIndex].textElement = m_CachedEllipsisGlyphInfo;
                                    textInfo.textElementInfo[ellipsisIndex].fontAsset = m_MaterialReferences[0].fontAsset;
                                    textInfo.textElementInfo[ellipsisIndex].material = m_MaterialReferences[0].material;
                                    textInfo.textElementInfo[ellipsisIndex].materialReferenceIndex = 0;
                                }
                                else
                                {
                                    Debug.LogWarning("Unable to use Ellipsis character since it wasn't found in the current Font Asset [" + generationSettings.fontAsset.name + "]. Consider regenerating this font asset to include the Ellipsis character (u+2026).");
                                }

                                m_TotalCharacterCount = ellipsisIndex + 1;

                                GenerateTextMesh(generationSettings, textInfo);
                                return;
                            }
                            else
                            {
                                ClearMesh(false, textInfo);
                                return;
                            }
                        case TextOverflowMode.Masking:
                            if (!m_IsMaskingEnabled)
                                EnableMasking();
                            break;
                        case TextOverflowMode.ScrollRect:
                            if (!m_IsMaskingEnabled)
                                EnableMasking();
                            break;
                        case TextOverflowMode.Truncate:
                            if (m_IsMaskingEnabled)
                                DisableMasking();

                            if (m_LineNumber > 0)
                            {
                                m_CharBuffer[textInfo.textElementInfo[ellipsisIndex].index + 1] = (char)0;

                                m_TotalCharacterCount = ellipsisIndex + 1;

                                GenerateTextMesh(generationSettings, textInfo);
                                return;
                            }
                            else
                            {
                                ClearMesh(false, textInfo);
                                return;
                            }
                        case TextOverflowMode.Page:
                            if (m_IsMaskingEnabled)
                                DisableMasking();

                            // Ignore Page Break, Linefeed or carriage return
                            if (charCode == k_CarriageReturn || charCode == k_LineFeed)
                                break;

                            // Return if the first character doesn't fit.
                            if (i == 0)
                            {
                                return;
                            }
                            else if (previousPageOverflowChar == i)
                            {
                                m_CharBuffer[i] = 0;
                            }

                            previousPageOverflowChar = i;

                            // Go back to previous line and re-layout
                            i = RestoreWordWrappingState(ref m_SavedLineState, textInfo);

                            m_IsNewPage = true;
                            m_XAdvance = 0 + m_TagIndent;
                            m_LineOffset = 0;
                            m_MaxAscender = 0;
                            pageAscender = 0;
                            m_LineNumber += 1;
                            m_PageNumber += 1;
                            continue;
                        case TextOverflowMode.Linked:
                            // Truncate remaining text
                            if (m_LineNumber > 0)
                            {
                                m_CharBuffer[i] = (char)0;

                                m_TotalCharacterCount = m_CharacterCount;

                                GenerateTextMesh(generationSettings, textInfo);
                                return;
                            }
                            else
                            {
                                ClearMesh(true, textInfo);
                                return;
                            }
                    }
                }

                // Handle xAdvance & Tabulation Stops. Tab stops at every 25% of Font Size.
                if (charCode == 9)
                {
                    float tabSize = m_CurrentFontAsset.faceInfo.tabWidth * m_CurrentFontAsset.tabMultiple * currentElementScale;
                    float tabs = Mathf.Ceil(m_XAdvance / tabSize) * tabSize;
                    m_XAdvance = tabs > m_XAdvance ? tabs : m_XAdvance + tabSize;
                }
                else if (m_MonoSpacing != 0)
                {
                    m_XAdvance += (m_MonoSpacing - monoAdvance + ((generationSettings.characterSpacing + m_CurrentFontAsset.regularStyleSpacing) * currentElementScale) + m_CSpacing) * (1 - m_CharWidthAdjDelta);

                    if (char.IsWhiteSpace((char)charCode) || charCode == k_ZeroWidthSpace)
                        m_XAdvance += generationSettings.wordSpacing * currentElementScale;
                }
                else if (!generationSettings.isRightToLeft)
                {
                    float scaleFxMultiplier = 1;
                    if (m_IsFxMatrixSet)
                        scaleFxMultiplier = m_FxMatrix.m00;

                    m_XAdvance += ((m_CachedTextElement.glyph.metrics.horizontalAdvance * scaleFxMultiplier * boldXAdvanceMultiplier + generationSettings.characterSpacing + m_CurrentFontAsset.regularStyleSpacing + glyphAdjustments.xAdvance) * currentElementScale + m_CSpacing) * (1 - m_CharWidthAdjDelta);

                    if (char.IsWhiteSpace((char)charCode) || charCode == k_ZeroWidthSpace)
                        m_XAdvance += generationSettings.wordSpacing * currentElementScale;
                }
                else
                {
                    m_XAdvance -= glyphAdjustments.xAdvance * currentElementScale;
                }

                // Store xAdvance information
                textInfo.textElementInfo[m_CharacterCount].xAdvance = m_XAdvance;

                // Handle Carriage Return
                if (charCode == k_CarriageReturn)
                {
                    m_XAdvance = 0 + m_TagIndent;
                }

                // Handle Line Spacing Adjustments + Word Wrapping & special case for last line.
                if (charCode == k_LineFeed || m_CharacterCount == totalCharacterCount - 1)
                {
                    // Check if Line Spacing of previous line needs to be adjusted.
                    if (m_LineNumber > 0 && !TextGeneratorUtilities.Approximately(m_MaxLineAscender, m_StartOfLineAscender) && m_LineHeight == k_FloatUnset && !m_IsNewPage)
                    {
                        //Debug.Log("Line Feed - Adjusting Line Spacing on line #" + m_lineNumber);
                        float offsetDelta = m_MaxLineAscender - m_StartOfLineAscender;
                        TextGeneratorUtilities.AdjustLineOffset(m_FirstCharacterOfLine, m_CharacterCount, offsetDelta, textInfo);
                        m_LineOffset += offsetDelta;
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
                    textInfo.lineInfo[m_LineNumber].lineExtents.min = new Vector2(textInfo.textElementInfo[m_FirstVisibleCharacterOfLine].bottomLeft.x, lineDescender);
                    textInfo.lineInfo[m_LineNumber].lineExtents.max = new Vector2(textInfo.textElementInfo[m_LastVisibleCharacterOfLine].topRight.x, lineAscender);
                    textInfo.lineInfo[m_LineNumber].length = textInfo.lineInfo[m_LineNumber].lineExtents.max.x - (padding * currentElementScale);
                    textInfo.lineInfo[m_LineNumber].width = width;

                    if (textInfo.lineInfo[m_LineNumber].characterCount == 1)
                        textInfo.lineInfo[m_LineNumber].alignment = m_LineJustification;

                    if (textInfo.textElementInfo[m_LastVisibleCharacterOfLine].isVisible)
                        textInfo.lineInfo[m_LineNumber].maxAdvance = textInfo.textElementInfo[m_LastVisibleCharacterOfLine].xAdvance - (generationSettings.characterSpacing + m_CurrentFontAsset.regularStyleSpacing) * currentElementScale - m_CSpacing;
                    else
                        textInfo.lineInfo[m_LineNumber].maxAdvance = textInfo.textElementInfo[m_LastCharacterOfLine].xAdvance - (generationSettings.characterSpacing + m_CurrentFontAsset.regularStyleSpacing) * currentElementScale - m_CSpacing;

                    textInfo.lineInfo[m_LineNumber].baseline = 0 - m_LineOffset;
                    textInfo.lineInfo[m_LineNumber].ascender = lineAscender;
                    textInfo.lineInfo[m_LineNumber].descender = lineDescender;
                    textInfo.lineInfo[m_LineNumber].lineHeight = lineAscender - lineDescender + lineGap * baseScale;

                    m_FirstCharacterOfLine = m_CharacterCount + 1;
                    m_LineVisibleCharacterCount = 0;

                    // Add new line if not last line or character.
                    if (charCode == k_LineFeed)
                    {
                        // Store the state of the line before starting on the new line.
                        SaveWordWrappingState(ref m_SavedLineState, i, m_CharacterCount, textInfo);

                        // Store the state of the last Character before the new line.
                        SaveWordWrappingState(ref m_SavedWordWrapState, i, m_CharacterCount, textInfo);

                        m_LineNumber += 1;
                        isStartOfNewLine = true;
                        ignoreNonBreakingSpace = false;
                        isFirstWord = true;

                        // Check to make sure Array is large enough to hold a new line.
                        if (m_LineNumber >= textInfo.lineInfo.Length)
                            TextGeneratorUtilities.ResizeLineExtents(m_LineNumber, textInfo);

                        // Apply Line Spacing
                        if (m_LineHeight == k_FloatUnset)
                        {
                            lineOffsetDelta = 0 - m_MaxLineDescender + elementAscender + (lineGap + generationSettings.lineSpacing + generationSettings.paragraphSpacing + m_LineSpacingDelta) * baseScale;
                            m_LineOffset += lineOffsetDelta;
                        }
                        else
                            m_LineOffset += m_LineHeight + (generationSettings.lineSpacing + generationSettings.paragraphSpacing) * baseScale;

                        m_MaxLineAscender = TextGeneratorUtilities.largeNegativeFloat;
                        m_MaxLineDescender = TextGeneratorUtilities.largePositiveFloat;
                        m_StartOfLineAscender = elementAscender;

                        m_XAdvance = 0 + m_TagLineIndent + m_TagIndent;

                        ellipsisIndex = m_CharacterCount - 1;

                        m_CharacterCount += 1;
                        continue;
                    }
                }

                // Store Rectangle positions for each Character.

                // Determine the bounds of the Mesh.
                if (textInfo.textElementInfo[m_CharacterCount].isVisible)
                {
                    m_MeshExtents.min.x = Mathf.Min(m_MeshExtents.min.x, textInfo.textElementInfo[m_CharacterCount].bottomLeft.x);
                    m_MeshExtents.min.y = Mathf.Min(m_MeshExtents.min.y, textInfo.textElementInfo[m_CharacterCount].bottomLeft.y);

                    m_MeshExtents.max.x = Mathf.Max(m_MeshExtents.max.x, textInfo.textElementInfo[m_CharacterCount].topRight.x);
                    m_MeshExtents.max.y = Mathf.Max(m_MeshExtents.max.y, textInfo.textElementInfo[m_CharacterCount].topRight.y);
                }

                // Save pageInfo Data
                if (generationSettings.overflowMode == TextOverflowMode.Page && charCode != k_CarriageReturn && charCode != k_LineFeed) // && m_pageNumber < 16)
                {
                    // Check if we need to increase allocations for the pageInfo array.
                    if (m_PageNumber + 1 > textInfo.pageInfo.Length)
                        TextInfo.Resize(ref textInfo.pageInfo, m_PageNumber + 1, true);

                    textInfo.pageInfo[m_PageNumber].ascender = pageAscender;
                    textInfo.pageInfo[m_PageNumber].descender = elementDescender < textInfo.pageInfo[m_PageNumber].descender ? elementDescender : textInfo.pageInfo[m_PageNumber].descender;

                    if (m_PageNumber == 0 && m_CharacterCount == 0)
                        textInfo.pageInfo[m_PageNumber].firstCharacterIndex = m_CharacterCount;
                    else if (m_CharacterCount > 0 && m_PageNumber != textInfo.textElementInfo[m_CharacterCount - 1].pageNumber)
                    {
                        textInfo.pageInfo[m_PageNumber - 1].lastCharacterIndex = m_CharacterCount - 1;
                        textInfo.pageInfo[m_PageNumber].firstCharacterIndex = m_CharacterCount;
                    }
                    else if (m_CharacterCount == totalCharacterCount - 1)
                        textInfo.pageInfo[m_PageNumber].lastCharacterIndex = m_CharacterCount;
                }

                // Save State of Mesh Creation for handling of Word Wrapping
                if (generationSettings.wordWrap || generationSettings.overflowMode == TextOverflowMode.Truncate || generationSettings.overflowMode == TextOverflowMode.Ellipsis)
                {
                    if ((char.IsWhiteSpace((char)charCode) || charCode == k_ZeroWidthSpace || charCode == k_HyphenMinus || charCode == k_SoftHyphen) && (!m_IsNonBreakingSpace || ignoreNonBreakingSpace) && charCode != k_NoBreakSpace && charCode != k_FigureSpace && charCode != k_NonBreakingHyphen && charCode != k_NarrowNoBreakSpace && charCode != k_WordJoiner)
                    {
                        // We store the state of numerous variables for the most recent Space, LineFeed or Carriage Return to enable them to be restored
                        // for Word Wrapping.
                        SaveWordWrappingState(ref m_SavedWordWrapState, i, m_CharacterCount, textInfo);
                        m_IsCharacterWrappingEnabled = false;
                        isFirstWord = false;
                    }
                    // Handling for East Asian languages
                    else if ((charCode > k_HangulJamoStart && charCode < k_HangulJamoEnd || /* Hangul Jamo */
                              charCode > k_CjkStart && charCode < k_CjkEnd || /* CJK */
                              charCode > k_HangulJameExtendedStart && charCode < k_HangulJameExtendedEnd || /* Hangul Jame Extended-A */
                              charCode > k_HangulSyllablesStart && charCode < k_HangulSyllablesEnd || /* Hangul Syllables */
                              charCode > k_CjkIdeographsStart && charCode < k_CjkIdeographsEnd || /* CJK Compatibility Ideographs */
                              charCode > k_CjkFormsStart && charCode < k_CjkFormsEnd || /* CJK Compatibility Forms */
                              charCode > k_CjkHalfwidthStart && charCode < k_CjkHalfwidthEnd) /* CJK Half-Width */
                             && !m_IsNonBreakingSpace)
                    {
                        if (isFirstWord || isLastBreakingChar || TextSettings.linebreakingRules.leadingCharacters.ContainsKey(charCode) == false &&
                            (m_CharacterCount < totalCharacterCount - 1 &&
                             TextSettings.linebreakingRules.followingCharacters.ContainsKey(textInfo.textElementInfo[m_CharacterCount + 1].character) == false))
                        {
                            SaveWordWrappingState(ref m_SavedWordWrapState, i, m_CharacterCount, textInfo);
                            m_IsCharacterWrappingEnabled = false;
                            isFirstWord = false;
                        }
                    }
                    else if (isFirstWord || m_IsCharacterWrappingEnabled || isLastBreakingChar)
                        SaveWordWrappingState(ref m_SavedWordWrapState, i, m_CharacterCount, textInfo);
                }

                m_CharacterCount += 1;
            }

            // Check Auto Sizing and increase font size to fill text container.
            var fontSizeDelta = m_MaxFontSize - m_MinFontSize;
            if (!m_IsCharacterWrappingEnabled && generationSettings.autoSize && fontSizeDelta > 0.051f && m_FontSize < generationSettings.fontSizeMax)
            {
                m_MinFontSize = m_FontSize;
                m_FontSize += Mathf.Max((m_MaxFontSize - m_FontSize) / 2, 0.05f);
                m_FontSize = (int)(Mathf.Min(m_FontSize, generationSettings.fontSizeMax) * 20 + 0.5f) / 20f;

                if (m_LoopCountA > 20)
                {
                    return; // Added to debug
                }
                GenerateTextMesh(generationSettings, textInfo);
                return;
            }

            m_IsCharacterWrappingEnabled = false;

            // *** PHASE II of Text Generation ***

            // If there are no visible characters... no need to continue
            if (m_CharacterCount == 0)
            {
                ClearMesh(true, textInfo);
                return;
            }

            // *** PHASE II of Text Generation ***
            int lastVertIndex = m_MaterialReferences[0].referenceCount * 4;

            // Partial clear of the vertices array to mark unused vertices as degenerate.
            textInfo.meshInfo[0].Clear(false);

            // Handle Text Alignment

            Vector3 anchorOffset = Vector3.zero;
            Vector3[] corners = m_RectTransformCorners;

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

            // Initialization for Second Pass
            Vector3 justificationOffset = Vector3.zero;

            int wordCount = 0;
            int lineCount = 0;
            int lastLine = 0;
            bool isFirstSeperator = false;

            bool isStartOfWord = false;
            int wordFirstChar = 0;

            // Second Pass : Line Justification, UV Mapping, Character & Line Visibility & more.

            Color32 underlineColor = Color.white;
            Color32 strikethroughColor = Color.white;
            Color32 highlightColor = new Color32(255, 255, 0, 64);
            float xScale = 0;
            float underlineStartScale = 0;
            float underlineMaxScale = 0;
            float underlineBaseLine = TextGeneratorUtilities.largePositiveFloat;
            int lastPage = 0;

            float strikethroughPointSize = 0;
            float strikethroughScale = 0;
            float strikethroughBaseline = 0;

            TextElementInfo[] textElementInfos = textInfo.textElementInfo;

            for (int i = 0; i < m_CharacterCount; i++)
            {
                FontAsset currentFontAsset = textElementInfos[i].fontAsset;

                char currentCharacter = textElementInfos[i].character;

                int currentLine = textElementInfos[i].lineNumber;
                LineInfo lineInfo = textInfo.lineInfo[currentLine];
                lineCount = currentLine + 1;

                TextAlignment lineAlignment = lineInfo.alignment;

                // Process Line Justification

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

                        // Skip Zero Width Characters
                        if (currentCharacter == k_SoftHyphen || currentCharacter == k_ZeroWidthSpace || currentCharacter == k_WordJoiner)
                            break;

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

                                if (char.IsSeparator(currentCharacter))
                                    isFirstSeperator = true;
                                else
                                    isFirstSeperator = false;
                            }
                            else
                            {
                                float gap = !generationSettings.isRightToLeft ? lineInfo.width - lineInfo.maxAdvance : lineInfo.width + lineInfo.maxAdvance;

                                int visibleCount = lineInfo.visibleCharacterCount - 1 + lineInfo.controlCharacterCount;

                                // Get the number of spaces for each line ignoring the last character if it is not visible (ie. a space or linefeed).
                                int spaces = (textElementInfos[lineInfo.lastCharacterIndex].isVisible ? lineInfo.spaceCount : lineInfo.spaceCount - 1) - lineInfo.controlCharacterCount;

                                if (isFirstSeperator)
                                {
                                    spaces -= 1;
                                    visibleCount += 1;
                                }

                                float ratio = spaces > 0 ? generationSettings.wordWrappingRatio : 1;

                                if (spaces < 1)
                                    spaces = 1;

                                if (currentCharacter != 0xA0 && (currentCharacter == 9 || char.IsSeparator(currentCharacter)))
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

                var offset = anchorOffset + justificationOffset;

                // Handle UV2 mapping options and packing of scale information into UV2.

                bool isCharacterVisible = textElementInfos[i].isVisible;
                if (isCharacterVisible)
                {
                    TextElementType elementType = textElementInfos[i].elementType;
                    switch (elementType)
                    {
                        // CHARACTERS
                        case TextElementType.Character:
                            Extents lineExtents = lineInfo.lineExtents;
                            float uvOffset = (generationSettings.uvLineOffset * currentLine) % 1;

                            // Setup UV2 based on Character Mapping Options Selected

                            switch (generationSettings.horizontalMapping)
                            {
                                case TextureMapping.Character:
                                    textElementInfos[i].vertexBottomLeft.uv2.x = 0;
                                    textElementInfos[i].vertexTopLeft.uv2.x = 0;
                                    textElementInfos[i].vertexTopRight.uv2.x = 1;
                                    textElementInfos[i].vertexBottomRight.uv2.x = 1;
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
                                            textElementInfos[i].vertexBottomLeft.uv2.y = 0;
                                            textElementInfos[i].vertexTopLeft.uv2.y = 1;
                                            textElementInfos[i].vertexTopRight.uv2.y = 0;
                                            textElementInfos[i].vertexBottomRight.uv2.y = 1;
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
                                    textElementInfos[i].vertexBottomLeft.uv2.y = 0;
                                    textElementInfos[i].vertexTopLeft.uv2.y = 1;
                                    textElementInfos[i].vertexTopRight.uv2.y = 1;
                                    textElementInfos[i].vertexBottomRight.uv2.y = 0;
                                    break;

                                case TextureMapping.Line:
                                    textElementInfos[i].vertexBottomLeft.uv2.y = (textElementInfos[i].vertexBottomLeft.position.y - lineInfo.descender) / (lineInfo.ascender - lineInfo.descender);
                                    textElementInfos[i].vertexTopLeft.uv2.y = (textElementInfos[i].vertexTopLeft.position.y - lineInfo.descender) / (lineInfo.ascender - lineInfo.descender); // + m_uvOffset.y;
                                    textElementInfos[i].vertexTopRight.uv2.y = textElementInfos[i].vertexTopLeft.uv2.y;
                                    textElementInfos[i].vertexBottomRight.uv2.y = textElementInfos[i].vertexBottomLeft.uv2.y;
                                    break;

                                case TextureMapping.Paragraph:
                                    textElementInfos[i].vertexBottomLeft.uv2.y = (textElementInfos[i].vertexBottomLeft.position.y - m_MeshExtents.min.y) / (m_MeshExtents.max.y - m_MeshExtents.min.y);
                                    textElementInfos[i].vertexTopLeft.uv2.y = (textElementInfos[i].vertexTopLeft.position.y - m_MeshExtents.min.y) / (m_MeshExtents.max.y - m_MeshExtents.min.y);
                                    textElementInfos[i].vertexTopRight.uv2.y = textElementInfos[i].vertexTopLeft.uv2.y;
                                    textElementInfos[i].vertexBottomRight.uv2.y = textElementInfos[i].vertexBottomLeft.uv2.y;
                                    break;

                                case TextureMapping.MatchAspect:
                                    float yDelta = (1 - ((textElementInfos[i].vertexBottomLeft.uv2.x + textElementInfos[i].vertexTopRight.uv2.x) / textElementInfos[i].aspectRatio)) / 2; // Center of Rectangle

                                    textElementInfos[i].vertexBottomLeft.uv2.y = yDelta + (textElementInfos[i].vertexBottomLeft.uv2.x / textElementInfos[i].aspectRatio);
                                    textElementInfos[i].vertexTopLeft.uv2.y = yDelta + (textElementInfos[i].vertexTopRight.uv2.x / textElementInfos[i].aspectRatio);
                                    textElementInfos[i].vertexBottomRight.uv2.y = textElementInfos[i].vertexBottomLeft.uv2.y;
                                    textElementInfos[i].vertexTopRight.uv2.y = textElementInfos[i].vertexTopLeft.uv2.y;
                                    break;
                            }

                            // Pack UV's so that we can pass Xscale needed for Shader to maintain 1:1 ratio.

                            xScale = textElementInfos[i].scale * (1 - m_CharWidthAdjDelta) * 1; // generationSettings.scale;
                            if (!textElementInfos[i].isUsingAlternateTypeface && (textElementInfos[i].style & FontStyles.Bold) == FontStyles.Bold)
                                xScale *= -1;

                            // Optimization to avoid having a vector2 returned from the Pack UV function.
                            textElementInfos[i].vertexBottomLeft.uv2.x = 1;
                            textElementInfos[i].vertexBottomLeft.uv2.y = xScale;
                            textElementInfos[i].vertexTopLeft.uv2.x = 1;
                            textElementInfos[i].vertexTopLeft.uv2.y = xScale;
                            textElementInfos[i].vertexTopRight.uv2.x = 1;
                            textElementInfos[i].vertexTopRight.uv2.y = xScale;
                            textElementInfos[i].vertexBottomRight.uv2.x = 1;
                            textElementInfos[i].vertexBottomRight.uv2.y = xScale;
                            break;

                        // SPRITES
                        case TextElementType.Sprite:

                            // Nothing right now
                            break;
                    }

                    // Handle maxVisibleCharacters, maxVisibleLines and Overflow Page Mode.

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

                    // Fill Vertex Buffers for the various types of element
                    if (elementType == TextElementType.Character)
                    {
                        TextGeneratorUtilities.FillCharacterVertexBuffers(i, generationSettings, textInfo);
                    }
                    else if (elementType == TextElementType.Sprite)
                    {
                        TextGeneratorUtilities.FillSpriteVertexBuffers(i, generationSettings, textInfo);
                    }
                }

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

                // Need to recompute lineExtent to account for the offset from justification.

                if (currentLine != lastLine || i == m_CharacterCount - 1)
                {
                    // Update the previous line's extents
                    if (currentLine != lastLine)
                    {
                        textInfo.lineInfo[lastLine].baseline += offset.y;
                        textInfo.lineInfo[lastLine].ascender += offset.y;
                        textInfo.lineInfo[lastLine].descender += offset.y;

                        textInfo.lineInfo[lastLine].lineExtents.min = new Vector2(textInfo.textElementInfo[textInfo.lineInfo[lastLine].firstCharacterIndex].bottomLeft.x, textInfo.lineInfo[lastLine].descender);
                        textInfo.lineInfo[lastLine].lineExtents.max = new Vector2(textInfo.textElementInfo[textInfo.lineInfo[lastLine].lastVisibleCharacterIndex].topRight.x, textInfo.lineInfo[lastLine].ascender);
                    }

                    // Update the current line's extents
                    if (i == m_CharacterCount - 1)
                    {
                        textInfo.lineInfo[currentLine].baseline += offset.y;
                        textInfo.lineInfo[currentLine].ascender += offset.y;
                        textInfo.lineInfo[currentLine].descender += offset.y;

                        textInfo.lineInfo[currentLine].lineExtents.min = new Vector2(textInfo.textElementInfo[textInfo.lineInfo[currentLine].firstCharacterIndex].bottomLeft.x, textInfo.lineInfo[currentLine].descender);
                        textInfo.lineInfo[currentLine].lineExtents.max = new Vector2(textInfo.textElementInfo[textInfo.lineInfo[currentLine].lastVisibleCharacterIndex].topRight.x, textInfo.lineInfo[currentLine].ascender);
                    }
                }

                // Track Word Count per line and for the object

                int wordLastChar;
                if (char.IsLetterOrDigit(currentCharacter) || currentCharacter == k_HyphenMinus || currentCharacter == k_SoftHyphen || currentCharacter == k_Hyphen || currentCharacter == k_NonBreakingHyphen)
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
                else if (isStartOfWord || i == 0 && (!char.IsPunctuation(currentCharacter) || char.IsWhiteSpace(currentCharacter) || currentCharacter == k_ZeroWidthSpace || i == m_CharacterCount - 1))
                {
                    if (!(i > 0 && i < textElementInfos.Length - 1 && i < m_CharacterCount && (currentCharacter == k_SingleQuote || currentCharacter == k_RightSingleQuote) && char.IsLetterOrDigit(textElementInfos[i - 1].character) && char.IsLetterOrDigit(textElementInfos[i + 1].character)))
                    {
                        wordLastChar = i == m_CharacterCount - 1 && char.IsLetterOrDigit(currentCharacter) ? i : i - 1;
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

                // Setup & Handle Underline

                // NOTE: Need to figure out how underline will be handled with multiple fonts and which font will be used for the underline.
                bool isUnderline = (textInfo.textElementInfo[i].style & FontStyles.Underline) == FontStyles.Underline;
                float underlineEndScale;
                if (isUnderline)
                {
                    bool isUnderlineVisible = true;
                    int currentPage = textInfo.textElementInfo[i].pageNumber;

                    if (i > generationSettings.maxVisibleCharacters || currentLine > generationSettings.maxVisibleLines || (generationSettings.overflowMode == TextOverflowMode.Page && currentPage + 1 != generationSettings.pageToDisplay))
                        isUnderlineVisible = false;

                    // We only use the scale of visible characters.
                    if (!char.IsWhiteSpace(currentCharacter) && currentCharacter != k_ZeroWidthSpace)
                    {
                        underlineMaxScale = Mathf.Max(underlineMaxScale, textInfo.textElementInfo[i].scale);
                        underlineBaseLine = Mathf.Min(currentPage == lastPage ? underlineBaseLine : TextGeneratorUtilities.largePositiveFloat, textInfo.textElementInfo[i].baseLine + generationSettings.fontAsset.faceInfo.underlineOffset * underlineMaxScale);
                        lastPage = currentPage; // Need to track pages to ensure we reset baseline for the new pages.
                    }

                    if (!beginUnderline && isUnderlineVisible && i <= lineInfo.lastVisibleCharacterIndex && currentCharacter != k_LineFeed && currentCharacter != k_CarriageReturn)
                    {
                        if (!(i == lineInfo.lastVisibleCharacterIndex && char.IsSeparator(currentCharacter)))
                        {
                            beginUnderline = true;
                            underlineStartScale = textInfo.textElementInfo[i].scale;
                            if (underlineMaxScale == 0)
                                underlineMaxScale = underlineStartScale;
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

                        DrawUnderlineMesh(underlineStart, underlineEnd, ref lastVertIndex, underlineStartScale, underlineEndScale, underlineMaxScale, xScale, underlineColor, generationSettings, textInfo);
                        underlineMaxScale = 0;
                        underlineBaseLine = TextGeneratorUtilities.largePositiveFloat;
                    }
                    else if (beginUnderline && (i == lineInfo.lastCharacterIndex || i >= lineInfo.lastVisibleCharacterIndex))
                    {
                        // Terminate underline at previous visible character if space or carriage return.
                        if (char.IsWhiteSpace(currentCharacter) || currentCharacter == 0x200B)
                        {
                            int lastVisibleCharacterIndex = lineInfo.lastVisibleCharacterIndex;
                            underlineEnd = new Vector3(textInfo.textElementInfo[lastVisibleCharacterIndex].topRight.x, underlineBaseLine, 0);
                            underlineEndScale = textInfo.textElementInfo[lastVisibleCharacterIndex].scale;
                        }
                        else
                        {
                            // End underline if last character of the line.
                            underlineEnd = new Vector3(textInfo.textElementInfo[i].topRight.x, underlineBaseLine, 0);
                            underlineEndScale = textInfo.textElementInfo[i].scale;
                        }

                        beginUnderline = false;
                        DrawUnderlineMesh(underlineStart, underlineEnd, ref lastVertIndex, underlineStartScale, underlineEndScale, underlineMaxScale, xScale, underlineColor, generationSettings, textInfo);
                        underlineMaxScale = 0;
                        underlineBaseLine = TextGeneratorUtilities.largePositiveFloat;
                    }
                    else if (beginUnderline && !isUnderlineVisible)
                    {
                        beginUnderline = false;
                        underlineEnd = new Vector3(textInfo.textElementInfo[i - 1].topRight.x, underlineBaseLine, 0);
                        underlineEndScale = textInfo.textElementInfo[i - 1].scale;

                        DrawUnderlineMesh(underlineStart, underlineEnd, ref lastVertIndex, underlineStartScale, underlineEndScale, underlineMaxScale, xScale, underlineColor, generationSettings, textInfo);
                        underlineMaxScale = 0;
                        underlineBaseLine = TextGeneratorUtilities.largePositiveFloat;
                    }
                    else if (beginUnderline && i < m_CharacterCount - 1 && !ColorUtilities.CompareColors(underlineColor, textInfo.textElementInfo[i + 1].underlineColor))
                    {
                        // End underline if underline color has changed.
                        beginUnderline = false;
                        underlineEnd = new Vector3(textInfo.textElementInfo[i].topRight.x, underlineBaseLine, 0);
                        underlineEndScale = textInfo.textElementInfo[i].scale;

                        DrawUnderlineMesh(underlineStart, underlineEnd, ref lastVertIndex, underlineStartScale, underlineEndScale, underlineMaxScale, xScale, underlineColor, generationSettings, textInfo);
                        underlineMaxScale = 0;
                        underlineBaseLine = TextGeneratorUtilities.largePositiveFloat;
                    }
                }
                else
                {
                    // End Underline
                    if (beginUnderline)
                    {
                        beginUnderline = false;
                        underlineEnd = new Vector3(textInfo.textElementInfo[i - 1].topRight.x, underlineBaseLine, 0);
                        underlineEndScale = textInfo.textElementInfo[i - 1].scale;

                        DrawUnderlineMesh(underlineStart, underlineEnd, ref lastVertIndex, underlineStartScale, underlineEndScale, underlineMaxScale, xScale, underlineColor, generationSettings, textInfo);
                        underlineMaxScale = 0;
                        underlineBaseLine = TextGeneratorUtilities.largePositiveFloat;
                    }
                }

                // Setup & Handle Strikethrough

                // NOTE: Need to figure out how underline will be handled with multiple fonts and which font will be used for the underline.
                bool isStrikethrough = (textInfo.textElementInfo[i].style & FontStyles.Strikethrough) == FontStyles.Strikethrough;
                float strikethroughOffset = currentFontAsset.faceInfo.strikethroughOffset;

                if (isStrikethrough)
                {
                    bool isStrikeThroughVisible = !(i > generationSettings.maxVisibleCharacters || currentLine > generationSettings.maxVisibleLines || (generationSettings.overflowMode == TextOverflowMode.Page && textInfo.textElementInfo[i].pageNumber + 1 != generationSettings.pageToDisplay));

                    if (beginStrikethrough == false && isStrikeThroughVisible && i <= lineInfo.lastVisibleCharacterIndex && currentCharacter != k_LineFeed && currentCharacter != k_CarriageReturn)
                    {
                        if (!(i == lineInfo.lastVisibleCharacterIndex && char.IsSeparator(currentCharacter)))
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

                        DrawUnderlineMesh(strikethroughStart, strikethroughEnd, ref lastVertIndex, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor, generationSettings, textInfo);
                    }
                    else if (beginStrikethrough && i == lineInfo.lastCharacterIndex)
                    {
                        // Terminate Strikethrough at previous visible character if space or carriage return.
                        if (char.IsWhiteSpace(currentCharacter) || currentCharacter == k_ZeroWidthSpace)
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
                        DrawUnderlineMesh(strikethroughStart, strikethroughEnd, ref lastVertIndex, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor, generationSettings, textInfo);
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

                        DrawUnderlineMesh(strikethroughStart, strikethroughEnd, ref lastVertIndex, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor, generationSettings, textInfo);
                    }
                    else if (beginStrikethrough && i < m_CharacterCount && currentFontAsset.GetInstanceID() != textElementInfos[i + 1].fontAsset.GetInstanceID())
                    {
                        // Terminate Strikethrough if font asset changes.
                        beginStrikethrough = false;
                        strikethroughEnd = new Vector3(textInfo.textElementInfo[i].topRight.x, textInfo.textElementInfo[i].baseLine + strikethroughOffset * strikethroughScale, 0);

                        DrawUnderlineMesh(strikethroughStart, strikethroughEnd, ref lastVertIndex, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor, generationSettings, textInfo);
                    }
                    else if (beginStrikethrough && !isStrikeThroughVisible)
                    {
                        // Terminate Strikethrough if character is not visible.
                        beginStrikethrough = false;
                        strikethroughEnd = new Vector3(textInfo.textElementInfo[i - 1].topRight.x, textInfo.textElementInfo[i - 1].baseLine + strikethroughOffset * strikethroughScale, 0);

                        DrawUnderlineMesh(strikethroughStart, strikethroughEnd, ref lastVertIndex, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor, generationSettings, textInfo);
                    }
                }
                else
                {
                    // End Strikethrough
                    if (beginStrikethrough)
                    {
                        beginStrikethrough = false;
                        strikethroughEnd = new Vector3(textInfo.textElementInfo[i - 1].topRight.x, textInfo.textElementInfo[i - 1].baseLine + strikethroughOffset * strikethroughScale, 0);

                        DrawUnderlineMesh(strikethroughStart, strikethroughEnd, ref lastVertIndex, strikethroughScale, strikethroughScale, strikethroughScale, xScale, strikethroughColor, generationSettings, textInfo);
                    }
                }

                // HANDLE TEXT HIGHLIGHTING

                bool isHighlight = (textInfo.textElementInfo[i].style & FontStyles.Highlight) == FontStyles.Highlight;
                if (isHighlight)
                {
                    bool isHighlightVisible = true;
                    int currentPage = textInfo.textElementInfo[i].pageNumber;

                    if (i > generationSettings.maxVisibleCharacters || currentLine > generationSettings.maxVisibleLines || (generationSettings.overflowMode == TextOverflowMode.Page && currentPage + 1 != generationSettings.pageToDisplay))
                        isHighlightVisible = false;

                    if (!beginHighlight && isHighlightVisible && i <= lineInfo.lastVisibleCharacterIndex && currentCharacter != k_LineFeed && currentCharacter != k_CarriageReturn)
                    {
                        if (!(i == lineInfo.lastVisibleCharacterIndex && char.IsSeparator(currentCharacter)))
                        {
                            beginHighlight = true;
                            highlightStart = TextGeneratorUtilities.largePositiveVector2;
                            highlightEnd = TextGeneratorUtilities.largeNegativeVector2;
                            highlightColor = textInfo.textElementInfo[i].highlightColor;
                        }
                    }

                    if (beginHighlight)
                    {
                        Color32 currentHighlightColor = textInfo.textElementInfo[i].highlightColor;
                        bool isColorTransition = false;

                        // Handle Highlight color changes
                        if (!ColorUtilities.CompareColors(highlightColor, currentHighlightColor))
                        {
                            // End drawing at the start of new highlight color to prevent a gap between highlight sections.
                            highlightEnd.x = (highlightEnd.x + textInfo.textElementInfo[i].bottomLeft.x) / 2;

                            highlightStart.y = Mathf.Min(highlightStart.y, textInfo.textElementInfo[i].descender);
                            highlightEnd.y = Mathf.Max(highlightEnd.y, textInfo.textElementInfo[i].ascender);

                            DrawTextHighlight(highlightStart, highlightEnd, ref lastVertIndex, highlightColor, generationSettings, textInfo);

                            beginHighlight = true;
                            highlightStart = highlightEnd;

                            highlightEnd = new Vector3(textInfo.textElementInfo[i].topRight.x, textInfo.textElementInfo[i].descender, 0);
                            highlightColor = textInfo.textElementInfo[i].highlightColor;

                            isColorTransition = true;
                        }

                        if (!isColorTransition)
                        {
                            // Use the Min / Max Extents of the Highlight area to handle different character sizes and fonts.
                            highlightStart.x = Mathf.Min(highlightStart.x, textInfo.textElementInfo[i].bottomLeft.x);
                            highlightStart.y = Mathf.Min(highlightStart.y, textInfo.textElementInfo[i].descender);

                            highlightEnd.x = Mathf.Max(highlightEnd.x, textInfo.textElementInfo[i].topRight.x);
                            highlightEnd.y = Mathf.Max(highlightEnd.y, textInfo.textElementInfo[i].ascender);
                        }
                    }

                    // End Highlight if text only contains one character.
                    if (beginHighlight && m_CharacterCount == 1)
                    {
                        beginHighlight = false;

                        DrawTextHighlight(highlightStart, highlightEnd, ref lastVertIndex, highlightColor, generationSettings, textInfo);
                    }
                    else if (beginHighlight && (i == lineInfo.lastCharacterIndex || i >= lineInfo.lastVisibleCharacterIndex))
                    {
                        beginHighlight = false;
                        DrawTextHighlight(highlightStart, highlightEnd, ref lastVertIndex, highlightColor, generationSettings, textInfo);
                    }
                    else if (beginHighlight && !isHighlightVisible)
                    {
                        beginHighlight = false;
                        DrawTextHighlight(highlightStart, highlightEnd, ref lastVertIndex, highlightColor, generationSettings, textInfo);
                    }
                }
                else
                {
                    // End Highlight
                    if (beginHighlight)
                    {
                        beginHighlight = false;
                        DrawTextHighlight(highlightStart, highlightEnd, ref lastVertIndex, highlightColor, generationSettings, textInfo);
                    }
                }

                lastLine = currentLine;
            }

            // METRICS ABOUT THE TEXT OBJECT
            textInfo.characterCount = m_CharacterCount;
            textInfo.spriteCount = m_SpriteCount;
            textInfo.lineCount = lineCount;
            textInfo.wordCount = wordCount != 0 && m_CharacterCount > 0 ? wordCount : 1;
            textInfo.pageCount = m_PageNumber + 1;

            // *** UPLOAD MESH DATA ***
            if (generationSettings.geometrySortingOrder != VertexSortingOrder.Normal)
                textInfo.meshInfo[0].SortGeometry(VertexSortingOrder.Reverse);

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

            state.visibleLinkCount = textInfo.linkCount;

            state.firstCharacterIndex = m_FirstCharacterOfLine;
            state.firstVisibleCharacterIndex = m_FirstVisibleCharacterOfLine;
            state.lastVisibleCharIndex = m_LastVisibleCharacterOfLine;

            state.fontStyle = m_FontStyleInternal;
            state.fontScale = m_FontScale;

            state.fontScaleMultiplier = m_FontScaleMultiplier;
            state.currentFontSize = m_CurrentFontSize;

            state.xAdvance = m_XAdvance;
            state.maxCapHeight = m_MaxCapHeight;
            state.maxAscender = m_MaxAscender;
            state.maxDescender = m_MaxDescender;
            state.maxLineAscender = m_MaxLineAscender;
            state.maxLineDescender = m_MaxLineDescender;
            state.previousLineAscender = m_StartOfLineAscender;
            state.preferredWidth = m_PreferredWidth;
            state.preferredHeight = m_PreferredHeight;
            state.meshExtents = m_MeshExtents;

            state.lineNumber = m_LineNumber;
            state.lineOffset = m_LineOffset;
            state.baselineOffset = m_BaselineOffset;

            state.vertexColor = m_HtmlColor;
            state.underlineColor = m_UnderlineColor;
            state.strikethroughColor = m_StrikethroughColor;
            state.highlightColor = m_HighlightColor;

            state.isNonBreakingSpace = m_IsNonBreakingSpace;
            state.tagNoParsing = m_TagNoParsing;

            // XML Tag Stack
            state.basicStyleStack = m_FontStyleStack;
            state.colorStack = m_ColorStack;
            state.underlineColorStack = m_UnderlineColorStack;
            state.strikethroughColorStack = m_StrikethroughColorStack;
            state.highlightColorStack = m_HighlightColorStack;
            state.colorGradientStack = m_ColorGradientStack;
            state.sizeStack = m_SizeStack;
            state.indentStack = m_IndentStack;
            state.fontWeightStack = m_FontWeightStack;
            state.styleStack = m_StyleStack;
            state.baselineStack = m_BaselineOffsetStack;
            state.actionStack = m_ActionStack;
            state.materialReferenceStack = m_MaterialReferenceStack;
            state.lineJustificationStack = m_LineJustificationStack;

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

            textInfo.linkCount = state.visibleLinkCount;

            m_FirstCharacterOfLine = state.firstCharacterIndex;
            m_FirstVisibleCharacterOfLine = state.firstVisibleCharacterIndex;
            m_LastVisibleCharacterOfLine = state.lastVisibleCharIndex;

            m_FontStyleInternal = state.fontStyle;
            m_FontScale = state.fontScale;
            m_FontScaleMultiplier = state.fontScaleMultiplier;

            m_CurrentFontSize = state.currentFontSize;

            m_XAdvance = state.xAdvance;
            m_MaxCapHeight = state.maxCapHeight;
            m_MaxAscender = state.maxAscender;
            m_MaxDescender = state.maxDescender;
            m_MaxLineAscender = state.maxLineAscender;
            m_MaxLineDescender = state.maxLineDescender;
            m_StartOfLineAscender = state.previousLineAscender;
            m_PreferredWidth = state.preferredWidth;
            m_PreferredHeight = state.preferredHeight;
            m_MeshExtents = state.meshExtents;

            m_LineNumber = state.lineNumber;
            m_LineOffset = state.lineOffset;
            m_BaselineOffset = state.baselineOffset;

            m_HtmlColor = state.vertexColor;
            m_UnderlineColor = state.underlineColor;
            m_StrikethroughColor = state.strikethroughColor;
            m_HighlightColor = state.highlightColor;

            m_IsNonBreakingSpace = state.isNonBreakingSpace;
            m_TagNoParsing = state.tagNoParsing;

            // XML Tag Stack
            m_FontStyleStack = state.basicStyleStack;
            m_ColorStack = state.colorStack;
            m_UnderlineColorStack = state.underlineColorStack;
            m_StrikethroughColorStack = state.strikethroughColorStack;
            m_HighlightColorStack = state.highlightColorStack;
            m_ColorGradientStack = state.colorGradientStack;
            m_SizeStack = state.sizeStack;
            m_IndentStack = state.indentStack;
            m_FontWeightStack = state.fontWeightStack;
            m_StyleStack = state.styleStack;
            m_BaselineOffsetStack = state.baselineStack;
            m_ActionStack = state.actionStack;
            m_MaterialReferenceStack = state.materialReferenceStack;
            m_LineJustificationStack = state.lineJustificationStack;

            m_SpriteAnimationId = state.spriteAnimationId;

            if (m_LineNumber < textInfo.lineInfo.Length)
                textInfo.lineInfo[m_LineNumber] = state.lineInfo;

            return index;
        }

        /// <summary>
        /// Improved rich text validation function.
        /// </summary>
        /// <param name="sourceText"></param>
        /// <param name="readIndex"></param>
        /// <param name="writeIndex"></param>
        /// <param name="generationSettings"></param>
        /// <param name="textInfo"></param>
        /// <returns></returns>
        bool ValidateRichTextTag(string sourceText, ref int readIndex, ref int writeIndex, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            int srcReadIndex = readIndex;
            int srcWriteIndex = writeIndex;

            int srcLength = sourceText.Length;
            bool isTagPotentiallyValid = false;

            byte readingFlag = 0;
            bool isValueInQuotes = false;

            int attributeIndex = 0;
            m_Attributes[attributeIndex].nameHashCode = 0;
            m_Attributes[attributeIndex].valueHashCode = 0;
            m_Attributes[attributeIndex].valueLength = 0;

            // TODO : Add limit on tag search
            for (; readIndex < srcLength && sourceText[readIndex] != 0 /* && tagCharCount < s_RichTextTag.Length */; readIndex++)
            {
                uint c = sourceText[readIndex];

                // Write character into internal array.
                if (writeIndex == m_InternalTextParsingBuffer.Length)
                    TextGeneratorUtilities.ResizeArray(m_InternalTextParsingBuffer);

                m_InternalTextParsingBuffer[writeIndex] = c;
                writeIndex += 1;

                if (c == '<')
                {
                    if (readIndex > srcReadIndex)
                        break;

                    continue;
                }

                // Check for closing tag
                if (c == '>')
                {
                    isTagPotentiallyValid = true;
                    break;
                }


                // Compute hashcode for Tag and Attribute names
                if (readingFlag == 0)
                {
                    // Compute hashcode value for tag and attribute names
                    if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '/' || c == '-')
                    {
                        m_Attributes[attributeIndex].nameHashCode = ((m_Attributes[attributeIndex].nameHashCode << 5) + m_Attributes[attributeIndex].nameHashCode) ^ (int)TextUtilities.ToUpperASCIIFast(c);
                        continue;
                    }

                    // Next - Determine the value and type.
                    if (c == '=')
                    {
                        readingFlag = 1;
                        continue;
                    }

                    // Special handling if using a short color tag like <#FF00FF>
                    if (c == '#')
                    {
                        m_Attributes[attributeIndex].nameHashCode = (int)TagHashCode.COLOR;

                        readingFlag = 2;
                        readIndex -= 1;
                        continue;
                    }

                    // Space separates tag names from attributes
                    // Reset next attribute before going to read it
                    if (c == ' ')
                    {
                        attributeIndex += 1;
                        m_Attributes[attributeIndex].nameHashCode = 0;
                        m_Attributes[attributeIndex].valueHashCode = 0;
                        m_Attributes[attributeIndex].valueLength = 0;
                        continue;
                    }

                    break;
                }

                // Determine value type for tag and attribute with special handling for values contained in quotes
                if (readingFlag == 1)
                {
                    isValueInQuotes = false;
                    readingFlag = 2;

                    // Check for format where all values are now enclosed in quotes.
                    if (c == '"')
                    {
                        isValueInQuotes = true;
                        continue;
                    }
                }

                // Read value
                if (readingFlag == 2)
                {
                    // We are done reading the value if we run into a quote.
                    if (c == '"')
                    {
                        if (!isValueInQuotes)
                            break;

                        readingFlag = 0;
                        continue;
                    }

                    // Also done reading the value if we run into a space when quotes are not being used.
                    if (!isValueInQuotes && c == ' ')
                    {
                        attributeIndex += 1;
                        m_Attributes[attributeIndex].nameHashCode = 0;
                        m_Attributes[attributeIndex].valueHashCode = 0;
                        m_Attributes[attributeIndex].valueLength = 0;
                        readingFlag = 0;
                        continue;
                    }

                    if (m_Attributes[attributeIndex].valueLength == 0)
                    {
                        m_Attributes[attributeIndex].valueStartIndex = readIndex;
                    }

                    // Compute Hashcode value irrespective of the value type
                    m_Attributes[attributeIndex].valueHashCode = ((m_Attributes[attributeIndex].valueHashCode << 5) + m_Attributes[attributeIndex].valueHashCode) ^ (int)TextUtilities.ToUpperASCIIFast(c);

                    m_Attributes[attributeIndex].valueLength += 1;
                }
            }

            if (isTagPotentiallyValid == false)
            {
                // Reset read and write positions to what they were when function was first called.
                readIndex = srcReadIndex;
                writeIndex = srcWriteIndex;
                return false;
            }

            //Debug.Log("Hashcode: " + m_Attributes[0].nameHashCode + "  Start Index: " + srcReadIndex + "  End Index: " + readIndex + "  Length: " + (writeIndex - srcWriteIndex));


            return false;
        }

        /// <summary>
        /// Function to identify and validate the rich tag. Returns the position of the > if the tag was valid.
        /// </summary>
        /// <param name="chars"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <param name="generationSettings"></param>
        /// <param name="textInfo"></param>
        /// <returns></returns>
        protected bool ValidateHtmlTag(int[] chars, int startIndex, out int endIndex, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            int tagCharCount = 0;
            byte attributeFlag = 0;

            TagUnitType tagUnits = TagUnitType.Pixels;
            TagValueType tagType = TagValueType.None;

            int attributeIndex = 0;
            m_XmlAttribute[attributeIndex].nameHashCode = 0;
            m_XmlAttribute[attributeIndex].valueType = TagValueType.None;
            m_XmlAttribute[attributeIndex].valueHashCode = 0;
            m_XmlAttribute[attributeIndex].valueStartIndex = 0;
            m_XmlAttribute[attributeIndex].valueLength = 0;

            // Clear attribute name hash codes
            m_XmlAttribute[1].nameHashCode = 0;
            m_XmlAttribute[2].nameHashCode = 0;
            m_XmlAttribute[3].nameHashCode = 0;
            m_XmlAttribute[4].nameHashCode = 0;

            endIndex = startIndex;
            bool isTagSet = false;
            bool isValidHtmlTag = false;

            for (int i = startIndex; i < chars.Length && chars[i] != 0 && tagCharCount < m_RichTextTag.Length && chars[i] != '<'; i++)
            {
                uint unicode = (uint)chars[i];

                if (unicode == '>') // ASCII Code of End HTML tag '>'
                {
                    isValidHtmlTag = true;
                    endIndex = i;
                    m_RichTextTag[tagCharCount] = (char)0;
                    break;
                }

                m_RichTextTag[tagCharCount] = (char)unicode;
                tagCharCount += 1;

                if (attributeFlag == 1)
                {
                    if (tagType == TagValueType.None)
                    {
                        // Check for attribute type
                        if (unicode == '+' || unicode == '-' || unicode == '.' || (unicode >= '0' && unicode <= '9'))
                        {
                            tagType = TagValueType.NumericalValue;
                            m_XmlAttribute[attributeIndex].valueType = TagValueType.NumericalValue;
                            m_XmlAttribute[attributeIndex].valueStartIndex = tagCharCount - 1;
                            m_XmlAttribute[attributeIndex].valueLength += 1;
                        }
                        else if (unicode == '#')
                        {
                            tagType = TagValueType.ColorValue;
                            m_XmlAttribute[attributeIndex].valueType = TagValueType.ColorValue;
                            m_XmlAttribute[attributeIndex].valueStartIndex = tagCharCount - 1;
                            m_XmlAttribute[attributeIndex].valueLength += 1;
                        }
                        else if (unicode == '"')
                        {
                            tagType = TagValueType.StringValue;
                            m_XmlAttribute[attributeIndex].valueType = TagValueType.StringValue;
                            m_XmlAttribute[attributeIndex].valueStartIndex = tagCharCount;
                        }
                        else
                        {
                            tagType = TagValueType.StringValue;
                            m_XmlAttribute[attributeIndex].valueType = TagValueType.StringValue;
                            m_XmlAttribute[attributeIndex].valueStartIndex = tagCharCount - 1;
                            m_XmlAttribute[attributeIndex].valueHashCode = (m_XmlAttribute[attributeIndex].valueHashCode << 5) + m_XmlAttribute[attributeIndex].valueHashCode ^ (int)TextUtilities.ToUpperASCIIFast(unicode);
                            m_XmlAttribute[attributeIndex].valueLength += 1;
                        }
                    }
                    else
                    {
                        if (tagType == TagValueType.NumericalValue)
                        {
                            // Check for termination of numerical value.
                            if (unicode == 'p' || unicode == 'e' || unicode == '%' || unicode == ' ')
                            {
                                attributeFlag = 2;
                                tagType = TagValueType.None;
                                attributeIndex += 1;
                                m_XmlAttribute[attributeIndex].nameHashCode = 0;
                                m_XmlAttribute[attributeIndex].valueType = TagValueType.None;
                                m_XmlAttribute[attributeIndex].valueHashCode = 0;
                                m_XmlAttribute[attributeIndex].valueStartIndex = 0;
                                m_XmlAttribute[attributeIndex].valueLength = 0;

                                if (unicode == 'e')
                                    tagUnits = TagUnitType.FontUnits;
                                else if (unicode == '%')
                                    tagUnits = TagUnitType.Percentage;
                            }
                            else if (attributeFlag != 2)
                            {
                                m_XmlAttribute[attributeIndex].valueLength += 1;
                            }
                        }
                        else if (tagType == TagValueType.ColorValue)
                        {
                            if (unicode != ' ')
                            {
                                m_XmlAttribute[attributeIndex].valueLength += 1;
                            }
                            else
                            {
                                attributeFlag = 2;
                                tagType = TagValueType.None;
                                attributeIndex += 1;
                                m_XmlAttribute[attributeIndex].nameHashCode = 0;
                                m_XmlAttribute[attributeIndex].valueType = TagValueType.None;
                                m_XmlAttribute[attributeIndex].valueHashCode = 0;
                                m_XmlAttribute[attributeIndex].valueStartIndex = 0;
                                m_XmlAttribute[attributeIndex].valueLength = 0;
                            }
                        }
                        else if (tagType == TagValueType.StringValue)
                        {
                            // Compute HashCode value for the named tag.
                            if (unicode != '"')
                            {
                                m_XmlAttribute[attributeIndex].valueHashCode = (m_XmlAttribute[attributeIndex].valueHashCode << 5) + m_XmlAttribute[attributeIndex].valueHashCode ^ (int)TextUtilities.ToUpperASCIIFast(unicode);
                                m_XmlAttribute[attributeIndex].valueLength += 1;
                            }
                            else
                            {
                                attributeFlag = 2;
                                tagType = TagValueType.None;
                                attributeIndex += 1;
                                m_XmlAttribute[attributeIndex].nameHashCode = 0;
                                m_XmlAttribute[attributeIndex].valueType = TagValueType.None;
                                m_XmlAttribute[attributeIndex].valueHashCode = 0;
                                m_XmlAttribute[attributeIndex].valueStartIndex = 0;
                                m_XmlAttribute[attributeIndex].valueLength = 0;
                            }
                        }
                    }
                }

                if (unicode == '=')
                    attributeFlag = 1;

                // Compute HashCode for the name of the attribute
                if (attributeFlag == 0 && unicode == ' ')
                {
                    if (isTagSet)
                        return false;

                    isTagSet = true;
                    attributeFlag = 2;

                    tagType = TagValueType.None;
                    attributeIndex += 1;
                    m_XmlAttribute[attributeIndex].nameHashCode = 0;
                    m_XmlAttribute[attributeIndex].valueType = TagValueType.None;
                    m_XmlAttribute[attributeIndex].valueHashCode = 0;
                    m_XmlAttribute[attributeIndex].valueStartIndex = 0;
                    m_XmlAttribute[attributeIndex].valueLength = 0;
                }

                if (attributeFlag == 0)
                    m_XmlAttribute[attributeIndex].nameHashCode = (m_XmlAttribute[attributeIndex].nameHashCode << 5) + m_XmlAttribute[attributeIndex].nameHashCode ^ (int)TextUtilities.ToUpperASCIIFast(unicode);

                if (attributeFlag == 2 && unicode == ' ')
                    attributeFlag = 0;
            }

            if (!isValidHtmlTag)
                return false;

            // Output rich text tag parsing results.
            //Debug.Log("Tag is [" + m_htmlTag.ArrayToString() + "].  Tag HashCode: " + m_xmlAttribute[0].nameHashCode + "  Tag Value HashCode: " + m_xmlAttribute[0].valueHashCode + "  Attribute 1 HashCode: " + m_xmlAttribute[1].nameHashCode + " Value HashCode: " + m_xmlAttribute[1].valueHashCode);
            //for (int i = 0; i < attributeIndex; i++)
            //    Debug.Log("Tag [" + i + "] with HashCode: " + m_XmlAttribute[i].nameHashCode + " has value of [" + new string(m_RichTextTag, m_XmlAttribute[i].valueStartIndex, m_XmlAttribute[i].valueLength) + "] Numerical Value: " + TextGeneratorUtilities.ConvertToFloat(m_RichTextTag, m_XmlAttribute[i].valueStartIndex, m_XmlAttribute[i].valueLength));

            // Special handling of the no parsing tag </noparse> </NOPARSE> tag
            if (m_TagNoParsing && ((TagHashCode)m_XmlAttribute[0].nameHashCode != TagHashCode.SLASH_NO_PARSE))
            {
                return false;
            }
            if ((TagHashCode)m_XmlAttribute[0].nameHashCode == TagHashCode.SLASH_NO_PARSE)
            {
                m_TagNoParsing = false;
                return true;
            }

            // Color <#FFF> 3 Hex values (short form)
            if (m_RichTextTag[0] == '#' && tagCharCount == 4)
            {
                m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_RichTextTag, tagCharCount);
                m_ColorStack.Add(m_HtmlColor);
                return true;
            }
            // Color <#FFF7> 4 Hex values with alpha (short form)
            if (m_RichTextTag[0] == '#' && tagCharCount == 5)
            {
                m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_RichTextTag, tagCharCount);
                m_ColorStack.Add(m_HtmlColor);
                return true;
            }
            // Color <#FF00FF>
            if (m_RichTextTag[0] == '#' && tagCharCount == 7) // if Tag begins with # and contains 7 characters.
            {
                m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_RichTextTag, tagCharCount);
                m_ColorStack.Add(m_HtmlColor);
                return true;
            }
            // Color <#FF00FF00> with alpha
            if (m_RichTextTag[0] == '#' && tagCharCount == 9) // if Tag begins with # and contains 9 characters.
            {
                m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_RichTextTag, tagCharCount);
                m_ColorStack.Add(m_HtmlColor);
                return true;
            }
            else
            {
                float value;

                switch ((TagHashCode)m_XmlAttribute[0].nameHashCode)
                {
                    case TagHashCode.BOLD: // <b>
                        m_FontStyleInternal |= FontStyles.Bold;
                        m_FontStyleStack.Add(FontStyles.Bold);

                        m_FontWeightInternal = FontWeight.Bold;
                        return true;

                    case TagHashCode.SLASH_BOLD: // </b>
                        if ((generationSettings.fontStyle & FontStyles.Bold) != FontStyles.Bold)
                        {
                            if (m_FontStyleStack.Remove(FontStyles.Bold) == 0)
                            {
                                m_FontStyleInternal &= ~FontStyles.Bold;
                                m_FontWeightInternal = m_FontWeightStack.Peek();
                            }
                        }
                        return true;

                    case TagHashCode.ITALIC: // <i>
                        m_FontStyleInternal |= FontStyles.Italic;
                        m_FontStyleStack.Add(FontStyles.Italic);
                        return true;

                    case TagHashCode.SLASH_ITALIC: // </i>
                        if ((generationSettings.fontStyle & FontStyles.Italic) != FontStyles.Italic)
                        {
                            if (m_FontStyleStack.Remove(FontStyles.Italic) == 0)
                                m_FontStyleInternal &= ~FontStyles.Italic;
                        }
                        return true;

                    case TagHashCode.STRIKETHROUGH: // <s>
                        m_FontStyleInternal |= FontStyles.Strikethrough;
                        m_FontStyleStack.Add(FontStyles.Strikethrough);

                        if (m_XmlAttribute[1].nameHashCode == (uint)TagHashCode.COLOR)
                        {
                            m_StrikethroughColor = TextGeneratorUtilities.HexCharsToColor(m_RichTextTag, m_XmlAttribute[1].valueStartIndex, m_XmlAttribute[1].valueLength);
                            m_StrikethroughColor.a = m_HtmlColor.a < m_StrikethroughColor.a ? m_HtmlColor.a : m_StrikethroughColor.a;
                        }
                        else
                            m_StrikethroughColor = m_HtmlColor;

                        m_StrikethroughColorStack.Add(m_StrikethroughColor);

                        return true;

                    case TagHashCode.SLASH_STRIKETHROUGH: // </s>
                        if ((generationSettings.fontStyle & FontStyles.Strikethrough) != FontStyles.Strikethrough)
                        {
                            if (m_FontStyleStack.Remove(FontStyles.Strikethrough) == 0)
                                m_FontStyleInternal &= ~FontStyles.Strikethrough;
                        }
                        return true;

                    case TagHashCode.UNDERLINE: // <u>
                        m_FontStyleInternal |= FontStyles.Underline;
                        m_FontStyleStack.Add(FontStyles.Underline);

                        if (m_XmlAttribute[1].nameHashCode == (uint)TagHashCode.COLOR)
                        {
                            m_UnderlineColor = TextGeneratorUtilities.HexCharsToColor(m_RichTextTag, m_XmlAttribute[1].valueStartIndex, m_XmlAttribute[1].valueLength);
                            m_UnderlineColor.a = m_HtmlColor.a < m_UnderlineColor.a ? m_HtmlColor.a : m_UnderlineColor.a;
                        }
                        else
                            m_UnderlineColor = m_HtmlColor;

                        m_UnderlineColorStack.Add(m_UnderlineColor);

                        return true;

                    case TagHashCode.SLASH_UNDERLINE: // </u>
                        if ((generationSettings.fontStyle & FontStyles.Underline) != FontStyles.Underline)
                        {
                            m_UnderlineColor = m_UnderlineColorStack.Remove();

                            if (m_FontStyleStack.Remove(FontStyles.Underline) == 0)
                                m_FontStyleInternal &= ~FontStyles.Underline;
                        }
                        return true;

                    case TagHashCode.MARK: // <mark=#FF00FF80>
                        m_FontStyleInternal |= FontStyles.Highlight;
                        m_FontStyleStack.Add(FontStyles.Highlight);

                        m_HighlightColor = TextGeneratorUtilities.HexCharsToColor(m_RichTextTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);
                        m_HighlightColor.a = m_HtmlColor.a < m_HighlightColor.a ? m_HtmlColor.a : m_HighlightColor.a;
                        m_HighlightColorStack.Add(m_HighlightColor);
                        return true;

                    case TagHashCode.SLASH_MARK: // </mark>
                        if ((generationSettings.fontStyle & FontStyles.Highlight) != FontStyles.Highlight)
                        {
                            m_HighlightColor = m_HighlightColorStack.Remove();

                            if (m_FontStyleStack.Remove(FontStyles.Highlight) == 0)
                                m_FontStyleInternal &= ~FontStyles.Highlight;
                        }
                        return true;

                    case TagHashCode.SUBSCRIPT: // <sub>
                        m_FontScaleMultiplier *= m_CurrentFontAsset.faceInfo.subscriptSize > 0 ? m_CurrentFontAsset.faceInfo.subscriptSize : 1;
                        m_BaselineOffsetStack.Push(m_BaselineOffset);
                        m_BaselineOffset += m_CurrentFontAsset.faceInfo.subscriptOffset * m_FontScale * m_FontScaleMultiplier;

                        m_FontStyleStack.Add(FontStyles.Subscript);
                        m_FontStyleInternal |= FontStyles.Subscript;
                        return true;

                    case TagHashCode.SLASH_SUBSCRIPT: // </sub>
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

                    case TagHashCode.SUPERSCRIPT: // <sup>
                        m_FontScaleMultiplier *= m_CurrentFontAsset.faceInfo.superscriptSize > 0 ? m_CurrentFontAsset.faceInfo.superscriptSize : 1;
                        m_BaselineOffsetStack.Push(m_BaselineOffset);
                        m_BaselineOffset += m_CurrentFontAsset.faceInfo.superscriptOffset * m_FontScale * m_FontScaleMultiplier;

                        m_FontStyleStack.Add(FontStyles.Superscript);
                        m_FontStyleInternal |= FontStyles.Superscript;
                        return true;

                    case TagHashCode.SLASH_SUPERSCRIPT: // </sup>
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

                    case TagHashCode.FONT_WEIGHT: // <font-weight>
                        value = TextGeneratorUtilities.ConvertToFloat(m_RichTextTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);

                        switch ((int)value)
                        {
                            case 100:
                                m_FontWeightInternal = FontWeight.Thin;
                                break;
                            case 200:
                                m_FontWeightInternal = FontWeight.ExtraLight;
                                break;
                            case 300:
                                m_FontWeightInternal = FontWeight.Light;
                                break;
                            case 400:
                                m_FontWeightInternal = FontWeight.Regular;
                                break;
                            case 500:
                                m_FontWeightInternal = FontWeight.Medium;
                                break;
                            case 600:
                                m_FontWeightInternal = FontWeight.SemiBold;
                                break;
                            case 700:
                                m_FontWeightInternal = FontWeight.Bold;
                                break;
                            case 800:
                                m_FontWeightInternal = FontWeight.Heavy;
                                break;
                            case 900:
                                m_FontWeightInternal = FontWeight.Black;
                                break;
                        }

                        m_FontWeightStack.Add(m_FontWeightInternal);

                        return true;

                    case TagHashCode.SLASH_FONT_WEIGHT: // </font-weight>
                        m_FontWeightStack.Remove();

                        if (m_FontStyleInternal == FontStyles.Bold)
                            m_FontWeightInternal = FontWeight.Bold;
                        else
                            m_FontWeightInternal = m_FontWeightStack.Peek();
                        return true;

                    case TagHashCode.POSITION: // <pos=000.00px> <pos=0em> <pos=50%>
                        value = TextGeneratorUtilities.ConvertToFloat(m_RichTextTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);
                        if (value == k_FloatUnset)
                            return false;

                        switch (tagUnits)
                        {
                            case TagUnitType.Pixels:
                                m_XAdvance = value;
                                return true;
                            case TagUnitType.FontUnits:
                                m_XAdvance = value * m_FontScale * generationSettings.fontAsset.faceInfo.tabWidth / generationSettings.fontAsset.tabMultiple;
                                return true;
                            case TagUnitType.Percentage:
                                m_XAdvance = m_MarginWidth * value / 100;
                                return true;
                        }
                        return false;

                    case TagHashCode.SLASH_POSITION: // </pos>
                        // No affect.
                        return true;


                    case TagHashCode.VERTICAL_OFFSET: // <voffset>
                        value = TextGeneratorUtilities.ConvertToFloat(m_RichTextTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);
                        if (value == k_FloatUnset)
                            return false;

                        switch (tagUnits)
                        {
                            case TagUnitType.Pixels:
                                m_BaselineOffset = value;
                                return true;
                            case TagUnitType.FontUnits:
                                m_BaselineOffset = value * m_FontScale * generationSettings.fontAsset.faceInfo.ascentLine;
                                return true;
                            case TagUnitType.Percentage:
                                return false;
                        }
                        return false;

                    case TagHashCode.SLASH_VERTICAL_OFFSET: // </voffset>
                        m_BaselineOffset = 0;
                        return true;

                    case TagHashCode.PAGE: // <page>
                        // This tag only works when Overflow - Page mode is used.
                        if (generationSettings.overflowMode == TextOverflowMode.Page)
                        {
                            m_XAdvance = 0 + m_TagLineIndent + m_TagIndent;
                            m_LineOffset = 0;
                            m_PageNumber += 1;
                            m_IsNewPage = true;
                        }
                        return true;

                    // <BR> tag is now handled inline where it is replaced by a linefeed or \n.

                    case TagHashCode.NO_BREAK: // <nobr>
                        m_IsNonBreakingSpace = true;
                        return true;

                    case TagHashCode.SLASH_NO_BREAK: // </nobr>
                        m_IsNonBreakingSpace = false;
                        return true;

                    case TagHashCode.SIZE: // <size=>
                        value = TextGeneratorUtilities.ConvertToFloat(m_RichTextTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);
                        if (value == k_FloatUnset)
                            return false;

                        switch (tagUnits)
                        {
                            case TagUnitType.Pixels:
                                if (m_RichTextTag[5] == '+') // <size=+00>
                                {
                                    m_CurrentFontSize = m_FontSize + value;
                                    m_SizeStack.Add(m_CurrentFontSize);
                                    m_FontScale = (m_CurrentFontSize / m_CurrentFontAsset.faceInfo.pointSize * m_CurrentFontAsset.faceInfo.scale);
                                    return true;
                                }
                                else if (m_RichTextTag[5] == '-') // <size=-00>
                                {
                                    m_CurrentFontSize = m_FontSize + value;
                                    m_SizeStack.Add(m_CurrentFontSize);
                                    m_FontScale = (m_CurrentFontSize / m_CurrentFontAsset.faceInfo.pointSize * m_CurrentFontAsset.faceInfo.scale);
                                    return true;
                                }
                                else // <size=00.0>
                                {
                                    m_CurrentFontSize = value;
                                    m_SizeStack.Add(m_CurrentFontSize);
                                    m_FontScale = (m_CurrentFontSize / m_CurrentFontAsset.faceInfo.pointSize * m_CurrentFontAsset.faceInfo.scale);
                                    return true;
                                }
                            case TagUnitType.FontUnits:
                                m_CurrentFontSize = m_FontSize * value;
                                m_SizeStack.Add(m_CurrentFontSize);
                                m_FontScale = (m_CurrentFontSize / m_CurrentFontAsset.faceInfo.pointSize * m_CurrentFontAsset.faceInfo.scale);
                                return true;
                            case TagUnitType.Percentage:
                                m_CurrentFontSize = m_FontSize * value / 100;
                                m_SizeStack.Add(m_CurrentFontSize);
                                m_FontScale = (m_CurrentFontSize / m_CurrentFontAsset.faceInfo.pointSize * m_CurrentFontAsset.faceInfo.scale);
                                return true;
                        }
                        return false;

                    case TagHashCode.SLASH_SIZE: // </size>
                        m_CurrentFontSize = m_SizeStack.Remove();
                        m_FontScale = (m_CurrentFontSize / m_CurrentFontAsset.faceInfo.pointSize * m_CurrentFontAsset.faceInfo.scale);
                        return true;

                    case TagHashCode.FONT: // <font=xx>
                        int fontHashCode = m_XmlAttribute[0].valueHashCode;
                        int materialAttributeHashCode = m_XmlAttribute[1].nameHashCode;
                        int materialHashCode = m_XmlAttribute[1].valueHashCode;

                        // Special handling for <font=default> or <font=Default>
                        if (fontHashCode == (int)TagHashCode.DEFAULT)
                        {
                            m_CurrentFontAsset = m_MaterialReferences[0].fontAsset;
                            m_CurrentMaterial = m_MaterialReferences[0].material;
                            m_CurrentMaterialIndex = 0;

                            m_FontScale = (m_CurrentFontSize / m_CurrentFontAsset.faceInfo.pointSize * m_CurrentFontAsset.faceInfo.scale);

                            m_MaterialReferenceStack.Add(m_MaterialReferences[0]);

                            return true;
                        }

                        FontAsset tempFont;
                        Material tempMaterial;

                        // HANDLE NEW FONT ASSET
                        if (!MaterialReferenceManager.TryGetFontAsset(fontHashCode, out tempFont))
                        {
                            // Load Font Asset
                            tempFont = Resources.Load<FontAsset>(TextSettings.defaultFontAssetPath + new string(m_RichTextTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength));

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

                            m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentMaterial, tempFont, m_MaterialReferences, m_MaterialReferenceIndexLookup);

                            m_MaterialReferenceStack.Add(m_MaterialReferences[m_CurrentMaterialIndex]);
                        }
                        else if (materialAttributeHashCode == (uint)TagHashCode.MATERIAL) // using material attribute
                        {
                            if (MaterialReferenceManager.TryGetMaterial(materialHashCode, out tempMaterial))
                            {
                                m_CurrentMaterial = tempMaterial;

                                m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentMaterial, tempFont, m_MaterialReferences, m_MaterialReferenceIndexLookup);

                                m_MaterialReferenceStack.Add(m_MaterialReferences[m_CurrentMaterialIndex]);
                            }
                            else
                            {
                                // Load new material
                                tempMaterial = Resources.Load<Material>(TextSettings.defaultFontAssetPath + new string(m_RichTextTag, m_XmlAttribute[1].valueStartIndex, m_XmlAttribute[1].valueLength));

                                if (tempMaterial == null)
                                    return false;

                                // Add new reference to this material in the MaterialReferenceManager
                                MaterialReferenceManager.AddFontMaterial(materialHashCode, tempMaterial);

                                m_CurrentMaterial = tempMaterial;

                                m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentMaterial, tempFont, m_MaterialReferences, m_MaterialReferenceIndexLookup);

                                m_MaterialReferenceStack.Add(m_MaterialReferences[m_CurrentMaterialIndex]);
                            }
                        }
                        else
                            return false;

                        m_CurrentFontAsset = tempFont;
                        m_FontScale = (m_CurrentFontSize / m_CurrentFontAsset.faceInfo.pointSize * m_CurrentFontAsset.faceInfo.scale);

                        return true;

                    case TagHashCode.SLASH_FONT: // </font>
                    {
                        MaterialReference materialReference = m_MaterialReferenceStack.Remove();

                        m_CurrentFontAsset = materialReference.fontAsset;
                        m_CurrentMaterial = materialReference.material;
                        m_CurrentMaterialIndex = materialReference.index;

                        m_FontScale = (m_CurrentFontSize / m_CurrentFontAsset.faceInfo.pointSize * m_CurrentFontAsset.faceInfo.scale);

                        return true;
                    }

                    case TagHashCode.MATERIAL: // <material="material name">
                        materialHashCode = m_XmlAttribute[0].valueHashCode;

                        // Special handling for <material=default> or <material=Default>
                        if (materialHashCode == (int)TagHashCode.DEFAULT)
                        {
                            m_CurrentMaterial = m_MaterialReferences[0].material;
                            m_CurrentMaterialIndex = 0;

                            m_MaterialReferenceStack.Add(m_MaterialReferences[0]);

                            return true;
                        }

                        // Check if material
                        if (MaterialReferenceManager.TryGetMaterial(materialHashCode, out tempMaterial))
                        {
                            m_CurrentMaterial = tempMaterial;

                            m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentMaterial, m_CurrentFontAsset, m_MaterialReferences, m_MaterialReferenceIndexLookup);

                            m_MaterialReferenceStack.Add(m_MaterialReferences[m_CurrentMaterialIndex]);
                        }
                        else
                        {
                            // Load new material
                            tempMaterial = Resources.Load<Material>(TextSettings.defaultFontAssetPath + new string(m_RichTextTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength));

                            if (tempMaterial == null)
                                return false;

                            // Add new reference to this material in the MaterialReferenceManager
                            MaterialReferenceManager.AddFontMaterial(materialHashCode, tempMaterial);

                            m_CurrentMaterial = tempMaterial;

                            m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentMaterial, m_CurrentFontAsset, m_MaterialReferences, m_MaterialReferenceIndexLookup);

                            m_MaterialReferenceStack.Add(m_MaterialReferences[m_CurrentMaterialIndex]);
                        }
                        return true;

                    case TagHashCode.SLASH_MATERIAL: // </material>
                    {
                        MaterialReference materialReference = m_MaterialReferenceStack.Remove();

                        m_CurrentMaterial = materialReference.material;
                        m_CurrentMaterialIndex = materialReference.index;

                        return true;
                    }

                    case TagHashCode.SPACE: // <space=000.00>
                        value = TextGeneratorUtilities.ConvertToFloat(m_RichTextTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);
                        if (value == k_FloatUnset)
                            return false;

                        switch (tagUnits)
                        {
                            case TagUnitType.Pixels:
                                m_XAdvance += value;
                                return true;
                            case TagUnitType.FontUnits:
                                m_XAdvance += value * m_FontScale * generationSettings.fontAsset.faceInfo.tabWidth / generationSettings.fontAsset.tabMultiple;
                                return true;
                            case TagUnitType.Percentage:

                                // Not applicable
                                return false;
                        }
                        return false;

                    case TagHashCode.ALPHA: // <alpha=#FF>
                        if (m_XmlAttribute[0].valueLength != 3)
                            return false;

                        m_HtmlColor.a = (byte)(TextGeneratorUtilities.HexToInt(m_RichTextTag[7]) * 16 + TextGeneratorUtilities.HexToInt(m_RichTextTag[8]));
                        return true;

                    case TagHashCode.A: // <a href=" ">
                        return false;

                    case TagHashCode.SLASH_A: // </a>
                        return true;

                    case TagHashCode.LINK: // <link="name">
                        if (m_IsParsingText && !m_IsCalculatingPreferredValues)
                        {
                            int index = textInfo.linkCount;

                            if (index + 1 > textInfo.linkInfo.Length)
                                TextInfo.Resize(ref textInfo.linkInfo, index + 1);

                            textInfo.linkInfo[index].hashCode = m_XmlAttribute[0].valueHashCode;
                            textInfo.linkInfo[index].linkTextfirstCharacterIndex = m_CharacterCount;

                            textInfo.linkInfo[index].linkIdFirstCharacterIndex = startIndex + m_XmlAttribute[0].valueStartIndex;
                            textInfo.linkInfo[index].linkIdLength = m_XmlAttribute[0].valueLength;
                            textInfo.linkInfo[index].SetLinkId(m_RichTextTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);
                        }
                        return true;

                    case TagHashCode.SLASH_LINK: // </link>
                        if (m_IsParsingText && !m_IsCalculatingPreferredValues)
                        {
                            if (textInfo.linkCount < textInfo.linkInfo.Length)
                            {
                                textInfo.linkInfo[textInfo.linkCount].linkTextLength = m_CharacterCount - textInfo.linkInfo[textInfo.linkCount].linkTextfirstCharacterIndex;

                                textInfo.linkCount += 1;
                            }
                        }
                        return true;

                    case TagHashCode.ALIGN: // <align=>
                        switch ((TagHashCode)m_XmlAttribute[0].valueHashCode)
                        {
                            case TagHashCode.LEFT: // <align=left>
                                m_LineJustification = TextAlignment.MiddleLeft;
                                m_LineJustificationStack.Add(m_LineJustification);
                                return true;
                            case TagHashCode.RIGHT: // <align=right>
                                m_LineJustification = TextAlignment.MiddleRight;
                                m_LineJustificationStack.Add(m_LineJustification);
                                return true;
                            case TagHashCode.CENTER: // <align=center>
                                m_LineJustification = TextAlignment.MiddleCenter;
                                m_LineJustificationStack.Add(m_LineJustification);
                                return true;
                            case TagHashCode.JUSTIFIED: // <align=justified>
                                m_LineJustification = TextAlignment.MiddleJustified;
                                m_LineJustificationStack.Add(m_LineJustification);
                                return true;
                            case TagHashCode.FLUSH: // <align=flush>
                                m_LineJustification = TextAlignment.MiddleFlush;
                                m_LineJustificationStack.Add(m_LineJustification);
                                return true;
                        }
                        return false;

                    case TagHashCode.SLASH_ALIGN: // </align>
                        m_LineJustification = m_LineJustificationStack.Remove();
                        return true;

                    case TagHashCode.WIDTH: // <width=xx>
                        value = TextGeneratorUtilities.ConvertToFloat(m_RichTextTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);
                        if (value == k_FloatUnset)
                            return false;

                        switch (tagUnits)
                        {
                            case TagUnitType.Pixels:
                                m_Width = value;
                                break;
                            case TagUnitType.FontUnits:
                                return false;

                            //break;
                            case TagUnitType.Percentage:
                                m_Width = m_MarginWidth * value / 100;
                                break;
                        }
                        return true;

                    case TagHashCode.SLASH_WIDTH: // </width>
                        m_Width = -1;
                        return true;

                    case TagHashCode.COLOR: // <color> <color=#FF00FF> or <color=#FF00FF00>
                        // <color=#FFF> 3 Hex (short hand)
                        if (m_RichTextTag[6] == '#' && tagCharCount == 10)
                        {
                            m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_RichTextTag, tagCharCount);
                            m_ColorStack.Add(m_HtmlColor);
                            return true;
                        }
                        // <color=#FFF7> 4 Hex (short hand)
                        else if (m_RichTextTag[6] == '#' && tagCharCount == 11)
                        {
                            m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_RichTextTag, tagCharCount);
                            m_ColorStack.Add(m_HtmlColor);
                            return true;
                        }

                        // <color=#FF00FF> 3 Hex pairs
                        if (m_RichTextTag[6] == '#' && tagCharCount == 13)
                        {
                            m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_RichTextTag, tagCharCount);
                            m_ColorStack.Add(m_HtmlColor);
                            return true;
                        }
                        // <color=#FF00FF00> 4 Hex pairs
                        else if (m_RichTextTag[6] == '#' && tagCharCount == 15)
                        {
                            m_HtmlColor = TextGeneratorUtilities.HexCharsToColor(m_RichTextTag, tagCharCount);
                            m_ColorStack.Add(m_HtmlColor);
                            return true;
                        }

                        // <color=name>
                        switch ((TagHashCode)m_XmlAttribute[0].valueHashCode)
                        {
                            case TagHashCode.RED: // <color=red>
                                m_HtmlColor = Color.red;
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case TagHashCode.BLUE: // <color=blue>
                                m_HtmlColor = Color.blue;
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case TagHashCode.BLACK: // <color=black>
                                m_HtmlColor = Color.black;
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case TagHashCode.GREEN: // <color=green>
                                m_HtmlColor = Color.green;
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case TagHashCode.WHITE: // <color=white>
                                m_HtmlColor = Color.white;
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case TagHashCode.ORANGE: // <color=orange>
                                m_HtmlColor = new Color32(255, 128, 0, 255);
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case TagHashCode.PURPLE: // <color=purple>
                                m_HtmlColor = new Color32(160, 32, 240, 255);
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                            case TagHashCode.YELLOW: // <color=yellow>
                                m_HtmlColor = Color.yellow;
                                m_ColorStack.Add(m_HtmlColor);
                                return true;
                        }
                        return false;

                    case TagHashCode.SLASH_COLOR: // </color>
                        m_HtmlColor = m_ColorStack.Remove();
                        return true;

                    case TagHashCode.GRADIENT: //<gradient>
                        int gradientPresetHashCode = m_XmlAttribute[0].valueHashCode;
                        TextGradientPreset tempColorGradientPreset;

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
                                tempColorGradientPreset = Resources.Load<TextGradientPreset>(TextSettings.defaultColorGradientPresetsPath + new string(m_RichTextTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength));
                            }

                            if (tempColorGradientPreset == null)
                                return false;

                            MaterialReferenceManager.AddColorGradientPreset(gradientPresetHashCode, tempColorGradientPreset);
                            m_ColorGradientPreset = tempColorGradientPreset;
                        }

                        m_ColorGradientStack.Add(m_ColorGradientPreset);

                        return true;

                    case TagHashCode.SLASH_GRADIENT: // </gradient>
                        m_ColorGradientPreset = m_ColorGradientStack.Remove();
                        return true;

                    case TagHashCode.CHARACTER_SPACE: // <cspace=xx.x>
                        value = TextGeneratorUtilities.ConvertToFloat(m_RichTextTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);
                        if (value == k_FloatUnset)
                            return false;

                        switch (tagUnits)
                        {
                            case TagUnitType.Pixels:
                                m_CSpacing = value;
                                break;
                            case TagUnitType.FontUnits:
                                m_CSpacing = value;
                                m_CSpacing *= m_FontScale * generationSettings.fontAsset.faceInfo.tabWidth / generationSettings.fontAsset.tabMultiple;
                                break;
                            case TagUnitType.Percentage:
                                return false;
                        }
                        return true;

                    case TagHashCode.SLASH_CHARACTER_SPACE: // </cspace>
                        if (!m_IsParsingText)
                            return true;

                        // Adjust xAdvance to remove extra space from last character.
                        if (m_CharacterCount > 0)
                        {
                            m_XAdvance -= m_CSpacing;
                            textInfo.textElementInfo[m_CharacterCount - 1].xAdvance = m_XAdvance;
                        }
                        m_CSpacing = 0;
                        return true;

                    case TagHashCode.MONOSPACE: // <mspace=xx.x>
                        value = TextGeneratorUtilities.ConvertToFloat(m_RichTextTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);
                        if (value == k_FloatUnset)
                            return false;

                        switch (tagUnits)
                        {
                            case TagUnitType.Pixels:
                                m_MonoSpacing = value;
                                break;
                            case TagUnitType.FontUnits:
                                m_MonoSpacing = value;
                                m_MonoSpacing *= m_FontScale * generationSettings.fontAsset.faceInfo.tabWidth / generationSettings.fontAsset.tabMultiple;
                                break;
                            case TagUnitType.Percentage:
                                return false;
                        }
                        return true;

                    case TagHashCode.SLASH_MONOSPACE: // </mspace>
                        m_MonoSpacing = 0;
                        return true;

                    case TagHashCode.CLASS: // <class="name">
                        return false;

                    case TagHashCode.INDENT: // <indent=10px> <indent=10em> <indent=50%>
                        value = TextGeneratorUtilities.ConvertToFloat(m_RichTextTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);
                        if (value == k_FloatUnset)
                            return false;

                        switch (tagUnits)
                        {
                            case TagUnitType.Pixels:
                                m_TagIndent = value;
                                break;
                            case TagUnitType.FontUnits:
                                m_TagIndent = value;
                                m_TagIndent *= m_FontScale * generationSettings.fontAsset.faceInfo.tabWidth / generationSettings.fontAsset.tabMultiple;
                                break;
                            case TagUnitType.Percentage:
                                m_TagIndent = m_MarginWidth * value / 100;
                                break;
                        }
                        m_IndentStack.Add(m_TagIndent);

                        m_XAdvance = m_TagIndent;
                        return true;

                    case TagHashCode.SLASH_INDENT: // </indent>
                        m_TagIndent = m_IndentStack.Remove();
                        return true;

                    case TagHashCode.LINE_INDENT: // <line-indent>
                        value = TextGeneratorUtilities.ConvertToFloat(m_RichTextTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);
                        if (value == k_FloatUnset)
                            return false;

                        switch (tagUnits)
                        {
                            case TagUnitType.Pixels:
                                m_TagLineIndent = value;
                                break;
                            case TagUnitType.FontUnits:
                                m_TagLineIndent = value;
                                m_TagLineIndent *= m_FontScale * generationSettings.fontAsset.faceInfo.tabWidth / generationSettings.fontAsset.tabMultiple;
                                break;
                            case TagUnitType.Percentage:
                                m_TagLineIndent = m_MarginWidth * value / 100;
                                break;
                        }

                        m_XAdvance += m_TagLineIndent;
                        return true;

                    case TagHashCode.SLASH_LINE_INDENT: // </line-indent>
                        m_TagLineIndent = 0;
                        return true;

                    case TagHashCode.SPRITE: // <sprite=x>
                        int spriteAssetHashCode = m_XmlAttribute[0].valueHashCode;
                        m_SpriteIndex = -1;

                        // CHECK TAG FORMAT
                        if (m_XmlAttribute[0].valueType == TagValueType.None || m_XmlAttribute[0].valueType == TagValueType.NumericalValue)
                        {
                            // No Sprite Asset is assigned to the text object
                            if (generationSettings.spriteAsset != null)
                            {
                                m_CurrentSpriteAsset = generationSettings.spriteAsset;
                            }
                            else if (m_DefaultSpriteAsset != null)
                            {
                                m_CurrentSpriteAsset = m_DefaultSpriteAsset;
                            }
                            else if (m_DefaultSpriteAsset == null)
                            {
                                if (TextSettings.defaultSpriteAsset != null)
                                    m_DefaultSpriteAsset = TextSettings.defaultSpriteAsset;
                                else
                                    m_DefaultSpriteAsset = Resources.Load<TextSpriteAsset>("Sprite Assets/Default Sprite Asset");

                                m_CurrentSpriteAsset = m_DefaultSpriteAsset;
                            }

                            // No valid sprite asset available
                            if (m_CurrentSpriteAsset == null)
                                return false;
                        }
                        else
                        {
                            TextSpriteAsset tempSpriteAsset;
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
                                    tempSpriteAsset = Resources.Load<TextSpriteAsset>(TextSettings.defaultSpriteAssetPath + new string(m_RichTextTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength));
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
                            int index = (int)TextGeneratorUtilities.ConvertToFloat(m_RichTextTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);
                            if (index == k_FloatUnset)
                                return false;

                            // Check to make sure sprite index is valid
                            if (index > m_CurrentSpriteAsset.spriteCharacterTable.Count - 1)
                                return false;

                            m_SpriteIndex = index;
                        }

                        m_SpriteColor = Color.white;
                        m_TintSprite = false;

                        // Handle Sprite Tag Attributes
                        for (int i = 0; i < m_XmlAttribute.Length && m_XmlAttribute[i].nameHashCode != 0; i++)
                        {
                            int nameHashCode = m_XmlAttribute[i].nameHashCode;
                            int index;

                            switch ((TagHashCode)nameHashCode)
                            {
                                case TagHashCode.NAME: // <sprite name="">
                                    m_CurrentSpriteAsset = TextSpriteAsset.SearchForSpriteByHashCode(m_CurrentSpriteAsset, m_XmlAttribute[i].valueHashCode, true, out index);
                                    if (index == -1)
                                        return false;

                                    m_SpriteIndex = index;
                                    break;
                                case TagHashCode.INDEX: // <sprite index=>
                                    index = (int)TextGeneratorUtilities.ConvertToFloat(m_RichTextTag, m_XmlAttribute[1].valueStartIndex, m_XmlAttribute[1].valueLength);
                                    if (index == k_FloatUnset)
                                        return false;

                                    // Check to make sure sprite index is valid
                                    if (index > m_CurrentSpriteAsset.spriteCharacterTable.Count - 1)
                                        return false;

                                    m_SpriteIndex = index;
                                    break;
                                case TagHashCode.TINT: // tint
                                    m_TintSprite = TextGeneratorUtilities.ConvertToFloat(m_RichTextTag, m_XmlAttribute[i].valueStartIndex, m_XmlAttribute[i].valueLength) != 0;
                                    break;
                                case TagHashCode.COLOR: // color=#FF00FF80
                                    m_SpriteColor = TextGeneratorUtilities.HexCharsToColor(m_RichTextTag, m_XmlAttribute[i].valueStartIndex, m_XmlAttribute[i].valueLength);
                                    break;
                                case TagHashCode.ANIM: // anim="0,16,12"  start, end, fps
                                    Debug.LogWarning("Sprite animations are not currently supported in TextCore");
                                    break;

                                //case 45545: // size
                                //case 32745: // SIZE

                                //    break;
                                default:
                                    if (nameHashCode != (int)TagHashCode.SPRITE)
                                        return false;
                                    break;
                            }
                        }

                        if (m_SpriteIndex == -1)
                            return false;

                        // Material HashCode for the Sprite Asset is the Sprite Asset Hash Code
                        m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentSpriteAsset.material, m_CurrentSpriteAsset, m_MaterialReferences, m_MaterialReferenceIndexLookup);

                        m_TextElementType = TextElementType.Sprite;
                        return true;

                    case TagHashCode.LOWERCASE: // <lowercase>
                        m_FontStyleInternal |= FontStyles.LowerCase;
                        m_FontStyleStack.Add(FontStyles.LowerCase);
                        return true;

                    case TagHashCode.SLASH_LOWERCASE: // </lowercase>
                        if ((generationSettings.fontStyle & FontStyles.LowerCase) != FontStyles.LowerCase)
                        {
                            if (m_FontStyleStack.Remove(FontStyles.LowerCase) == 0)
                                m_FontStyleInternal &= ~FontStyles.LowerCase;
                        }
                        return true;

                    case TagHashCode.ALLCAPS: // <allcaps>
                    case TagHashCode.UPPERCASE: // <uppercase>
                        m_FontStyleInternal |= FontStyles.UpperCase;
                        m_FontStyleStack.Add(FontStyles.UpperCase);
                        return true;

                    case TagHashCode.SLASH_ALLCAPS: // </allcaps>
                    case TagHashCode.SLASH_UPPERCASE: // </uppercase>
                        if ((generationSettings.fontStyle & FontStyles.UpperCase) != FontStyles.UpperCase)
                        {
                            if (m_FontStyleStack.Remove(FontStyles.UpperCase) == 0)
                                m_FontStyleInternal &= ~FontStyles.UpperCase;
                        }
                        return true;

                    case TagHashCode.SMALLCAPS: // <smallcaps>
                        m_FontStyleInternal |= FontStyles.SmallCaps;
                        m_FontStyleStack.Add(FontStyles.SmallCaps);
                        return true;

                    case TagHashCode.SLASH_SMALLCAPS: // </smallcaps>
                        if ((generationSettings.fontStyle & FontStyles.SmallCaps) != FontStyles.SmallCaps)
                        {
                            if (m_FontStyleStack.Remove(FontStyles.SmallCaps) == 0)
                                m_FontStyleInternal &= ~FontStyles.SmallCaps;
                        }
                        return true;

                    case TagHashCode.MARGIN: // <margin=00.0> <margin=00em> <margin=50%>
                        // TODO: Revise margin tag to make left and right variants be attribute based.
                        value = TextGeneratorUtilities.ConvertToFloat(m_RichTextTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength); // px
                        if (value == k_FloatUnset)
                            return false;

                        m_MarginLeft = value;
                        switch (tagUnits)
                        {
                            case TagUnitType.Pixels:

                                // Default behavior
                                break;
                            case TagUnitType.FontUnits:
                                m_MarginLeft *= m_FontScale * generationSettings.fontAsset.faceInfo.tabWidth / generationSettings.fontAsset.tabMultiple;
                                break;
                            case TagUnitType.Percentage:
                                m_MarginLeft = (m_MarginWidth - (m_Width != -1 ? m_Width : 0)) * m_MarginLeft / 100;
                                break;
                        }
                        m_MarginLeft = m_MarginLeft >= 0 ? m_MarginLeft : 0;
                        m_MarginRight = m_MarginLeft;

                        return true;

                    case TagHashCode.SLASH_MARGIN: // </margin>
                        m_MarginLeft = 0;
                        m_MarginRight = 0;
                        return true;

                    case TagHashCode.MARGIN_LEFT: // <margin-left=xx.x>
                        value = TextGeneratorUtilities.ConvertToFloat(m_RichTextTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength); // px
                        if (value == k_FloatUnset)
                            return false;

                        m_MarginLeft = value;
                        switch (tagUnits)
                        {
                            case TagUnitType.Pixels:

                                // Default behavior
                                break;
                            case TagUnitType.FontUnits:
                                m_MarginLeft *= m_FontScale * generationSettings.fontAsset.faceInfo.tabWidth / generationSettings.fontAsset.tabMultiple;
                                break;
                            case TagUnitType.Percentage:
                                m_MarginLeft = (m_MarginWidth - (m_Width != -1 ? m_Width : 0)) * m_MarginLeft / 100;
                                break;
                        }
                        m_MarginLeft = m_MarginLeft >= 0 ? m_MarginLeft : 0;
                        return true;

                    case TagHashCode.MARGIN_RIGHT: // <margin-right=xx.x>
                        value = TextGeneratorUtilities.ConvertToFloat(m_RichTextTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength); // px
                        if (value == k_FloatUnset)
                            return false;

                        m_MarginRight = value;
                        switch (tagUnits)
                        {
                            case TagUnitType.Pixels:

                                // Default behavior
                                break;
                            case TagUnitType.FontUnits:
                                m_MarginRight *= m_FontScale * generationSettings.fontAsset.faceInfo.tabWidth / generationSettings.fontAsset.tabMultiple;
                                break;
                            case TagUnitType.Percentage:
                                m_MarginRight = (m_MarginWidth - (m_Width != -1 ? m_Width : 0)) * m_MarginRight / 100;
                                break;
                        }
                        m_MarginRight = m_MarginRight >= 0 ? m_MarginRight : 0;
                        return true;

                    case TagHashCode.LINE_HEIGHT: // <line-height=xx.x>
                        value = TextGeneratorUtilities.ConvertToFloat(m_RichTextTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);
                        if (value == k_FloatUnset || value == 0)
                            return false;

                        m_LineHeight = value;
                        switch (tagUnits)
                        {
                            case TagUnitType.Pixels:

                                //m_lineHeight = value;
                                break;
                            case TagUnitType.FontUnits:
                                m_LineHeight *= generationSettings.fontAsset.faceInfo.lineHeight * m_FontScale;
                                break;
                            case TagUnitType.Percentage:
                                m_LineHeight = generationSettings.fontAsset.faceInfo.lineHeight * m_LineHeight / 100 * m_FontScale;
                                break;
                        }
                        return true;

                    case TagHashCode.SLASH_LINE_HEIGHT: // </line-height>
                        m_LineHeight = k_FloatUnset;
                        return true;

                    case TagHashCode.NO_PARSE: // <noparse>
                        m_TagNoParsing = true;
                        return true;

                    case TagHashCode.ACTION: // <action>
                        int actionId = m_XmlAttribute[0].valueHashCode;

                        if (m_IsParsingText)
                        {
                            m_ActionStack.Add(actionId);
                        }
                        return false;

                    case TagHashCode.SLASH_ACTION: // </action>
                        m_ActionStack.Remove();
                        return false;

                    case TagHashCode.SCALE: // <scale=xx.x>
                        value = TextGeneratorUtilities.ConvertToFloat(m_RichTextTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);
                        if (value == k_FloatUnset)
                            return false;

                        m_FxMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(value, 1, 1));
                        m_IsFxMatrixSet = true;

                        return true;

                    case TagHashCode.SLASH_SCALE: // </scale>
                        m_IsFxMatrixSet = false;
                        return true;

                    case TagHashCode.ROTATE: // <rotate=xx.x>
                        value = TextGeneratorUtilities.ConvertToFloat(m_RichTextTag, m_XmlAttribute[0].valueStartIndex, m_XmlAttribute[0].valueLength);
                        if (value == k_FloatUnset)
                            return false;

                        m_FxMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, value), Vector3.one);
                        m_IsFxMatrixSet = true;

                        return true;

                    case TagHashCode.SLASH_ROTATE: // </rotate>
                        m_IsFxMatrixSet = false;
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Store vertex information for each character.
        /// </summary>
        /// <param name="padding"></param>
        /// <param name="stylePadding">Style_padding.</param>
        /// <param name="vertexColor">Vertex color.</param>
        /// <param name="generationSettings"></param>
        /// <param name="textInfo"></param>
        void SaveGlyphVertexInfo(float padding, float stylePadding, Color32 vertexColor, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            // Save the Vertex Position for the Character
            textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.position = textInfo.textElementInfo[m_CharacterCount].bottomLeft;
            textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.position = textInfo.textElementInfo[m_CharacterCount].topLeft;
            textInfo.textElementInfo[m_CharacterCount].vertexTopRight.position = textInfo.textElementInfo[m_CharacterCount].topRight;
            textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.position = textInfo.textElementInfo[m_CharacterCount].bottomRight;

            // Alpha is the lower of the vertex color or tag color alpha used.
            vertexColor.a = m_FontColor32.a < vertexColor.a ? m_FontColor32.a : vertexColor.a;

            // Handle Vertex Colors & Vertex Color Gradient
            if (generationSettings.fontColorGradient == null)
            {
                textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.color = vertexColor;
                textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.color = vertexColor;
                textInfo.textElementInfo[m_CharacterCount].vertexTopRight.color = vertexColor;
                textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.color = vertexColor;
            }
            else
            {
                if (!generationSettings.overrideRichTextColors && m_ColorStack.m_Index > 1)
                {
                    textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.color = vertexColor;
                    textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.color = vertexColor;
                    textInfo.textElementInfo[m_CharacterCount].vertexTopRight.color = vertexColor;
                    textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.color = vertexColor;
                }
                else // Handle Vertex Color Gradient
                {
                    textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.color = generationSettings.fontColorGradient.bottomLeft * vertexColor;
                    textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.color = generationSettings.fontColorGradient.topLeft * vertexColor;
                    textInfo.textElementInfo[m_CharacterCount].vertexTopRight.color = generationSettings.fontColorGradient.topRight * vertexColor;
                    textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.color = generationSettings.fontColorGradient.bottomRight * vertexColor;
                }
            }

            if (m_ColorGradientPreset != null)
            {
                textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.color *= m_ColorGradientPreset.bottomLeft;
                textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.color *= m_ColorGradientPreset.topLeft;
                textInfo.textElementInfo[m_CharacterCount].vertexTopRight.color *= m_ColorGradientPreset.topRight;
                textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.color *= m_ColorGradientPreset.bottomRight;
            }

            // Apply style_padding only if this is a SDF Shader.
            if (!m_IsSdfShader)
                stylePadding = 0;

            // Setup UVs for the Character
            Vector2 uv0;
            uv0.x = (m_CachedTextElement.glyph.glyphRect.x - padding - stylePadding) / m_CurrentFontAsset.atlasWidth;
            uv0.y = (m_CachedTextElement.glyph.glyphRect.y - padding - stylePadding) / m_CurrentFontAsset.atlasHeight;

            Vector2 uv1;
            uv1.x = uv0.x;
            uv1.y = (m_CachedTextElement.glyph.glyphRect.y + padding + stylePadding + m_CachedTextElement.glyph.glyphRect.height) / m_CurrentFontAsset.atlasHeight;

            Vector2 uv2;
            uv2.x = (m_CachedTextElement.glyph.glyphRect.x + padding + stylePadding + m_CachedTextElement.glyph.glyphRect.width) / m_CurrentFontAsset.atlasWidth;
            uv2.y = uv1.y;

            Vector2 uv3;
            uv3.x = uv2.x;
            uv3.y = uv0.y;

            // Store UV Information
            textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.uv = uv0;
            textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.uv = uv1;
            textInfo.textElementInfo[m_CharacterCount].vertexTopRight.uv = uv2;
            textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.uv = uv3;
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

            textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.position = textInfo.textElementInfo[m_CharacterCount].bottomLeft;
            textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.position = textInfo.textElementInfo[m_CharacterCount].topLeft;
            textInfo.textElementInfo[m_CharacterCount].vertexTopRight.position = textInfo.textElementInfo[m_CharacterCount].topRight;
            textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.position = textInfo.textElementInfo[m_CharacterCount].bottomRight;

            // Vertex Color Alpha
            if (generationSettings.tintSprites)
                m_TintSprite = true;

            Color32 spriteColor = m_TintSprite ? ColorUtilities.MultiplyColors(m_SpriteColor, vertexColor) : m_SpriteColor;
            spriteColor.a = spriteColor.a < m_FontColor32.a ? spriteColor.a = spriteColor.a < vertexColor.a ? spriteColor.a : vertexColor.a : m_FontColor32.a;

            Color32 c0 = spriteColor;
            Color32 c1 = spriteColor;
            Color32 c2 = spriteColor;
            Color32 c3 = spriteColor;

            if (generationSettings.fontColorGradient != null)
            {
                c0 = m_TintSprite ? ColorUtilities.MultiplyColors(c0, generationSettings.fontColorGradient.bottomLeft) : c0;
                c1 = m_TintSprite ? ColorUtilities.MultiplyColors(c1, generationSettings.fontColorGradient.topLeft) : c1;
                c2 = m_TintSprite ? ColorUtilities.MultiplyColors(c2, generationSettings.fontColorGradient.topRight) : c2;
                c3 = m_TintSprite ? ColorUtilities.MultiplyColors(c3, generationSettings.fontColorGradient.bottomRight) : c3;
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
            Vector2 uv0 = new Vector2((float)m_CachedTextElement.glyph.glyphRect.x / m_CurrentSpriteAsset.spriteSheet.width, (float)m_CachedTextElement.glyph.glyphRect.y / m_CurrentSpriteAsset.spriteSheet.height); // bottom left
            Vector2 uv1 = new Vector2(uv0.x, (float)(m_CachedTextElement.glyph.glyphRect.y + m_CachedTextElement.glyph.glyphRect.height) / m_CurrentSpriteAsset.spriteSheet.height); // top left
            Vector2 uv2 = new Vector2((float)(m_CachedTextElement.glyph.glyphRect.x + m_CachedTextElement.glyph.glyphRect.width) / m_CurrentSpriteAsset.spriteSheet.width, uv1.y); // top right
            Vector2 uv3 = new Vector2(uv2.x, uv0.y); // bottom right

            // Store UV Information
            textInfo.textElementInfo[m_CharacterCount].vertexBottomLeft.uv = uv0;
            textInfo.textElementInfo[m_CharacterCount].vertexTopLeft.uv = uv1;
            textInfo.textElementInfo[m_CharacterCount].vertexTopRight.uv = uv2;
            textInfo.textElementInfo[m_CharacterCount].vertexBottomRight.uv = uv3;
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
        void DrawUnderlineMesh(Vector3 start, Vector3 end, ref int index, float startScale, float endScale, float maxScale, float sdfScale, Color32 underlineColor, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            if (m_CachedUnderlineGlyphInfo == null)
            {
                if (!TextSettings.warningsDisabled)
                    Debug.LogWarning("Unable to add underline since the Font Asset doesn't contain the underline character.");
                return;
            }

            int verticesCount = index + 12;

            // Check to make sure our current mesh buffer allocations can hold these new Quads.
            if (verticesCount > textInfo.meshInfo[0].vertices.Length)
            {
                // Resize Mesh Buffers
                textInfo.meshInfo[0].ResizeMeshInfo(verticesCount / 4);
            }

            // Adjust the position of the underline based on the lowest character. This matters for subscript character.
            start.y = Mathf.Min(start.y, end.y);
            end.y = Mathf.Min(start.y, end.y);

            float segmentWidth = m_CachedUnderlineGlyphInfo.glyph.metrics.width / 2 * maxScale;

            if (end.x - start.x < m_CachedUnderlineGlyphInfo.glyph.metrics.width * maxScale)
            {
                segmentWidth = (end.x - start.x) / 2f;
            }

            float startPadding = m_Padding * startScale / maxScale;
            float endPadding = m_Padding * endScale / maxScale;

            float underlineThickness = m_CachedUnderlineGlyphInfo.glyph.metrics.height;

            // UNDERLINE VERTICES FOR (3) LINE SEGMENTS
            Vector3[] vertices = textInfo.meshInfo[0].vertices;

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
            Vector2[] uvs0 = textInfo.meshInfo[0].uvs0;

            // Calculate UV required to setup the 3 Quads for the Underline.
            Vector2 uv0 = new Vector2((m_CachedUnderlineGlyphInfo.glyph.glyphRect.x - startPadding) / generationSettings.fontAsset.atlasWidth, (m_CachedUnderlineGlyphInfo.glyph.glyphRect.y - m_Padding) / generationSettings.fontAsset.atlasHeight); // bottom left
            Vector2 uv1 = new Vector2(uv0.x, (m_CachedUnderlineGlyphInfo.glyph.glyphRect.y + m_CachedUnderlineGlyphInfo.glyph.glyphRect.height + m_Padding) / generationSettings.fontAsset.atlasHeight);  // top left
            Vector2 uv2 = new Vector2((m_CachedUnderlineGlyphInfo.glyph.glyphRect.x - startPadding + (float)m_CachedUnderlineGlyphInfo.glyph.glyphRect.width / 2) / generationSettings.fontAsset.atlasWidth, uv1.y); // Mid Top Left
            Vector2 uv3 = new Vector2(uv2.x, uv0.y); // Mid Bottom Left
            Vector2 uv4 = new Vector2((m_CachedUnderlineGlyphInfo.glyph.glyphRect.x + endPadding + (float)m_CachedUnderlineGlyphInfo.glyph.glyphRect.width / 2) / generationSettings.fontAsset.atlasWidth, uv1.y); // Mid Top Right
            Vector2 uv5 = new Vector2(uv4.x, uv0.y); // Mid Bottom right
            Vector2 uv6 = new Vector2((m_CachedUnderlineGlyphInfo.glyph.glyphRect.x + endPadding + m_CachedUnderlineGlyphInfo.glyph.glyphRect.width) / generationSettings.fontAsset.atlasWidth, uv1.y); // End Part - Bottom Right
            Vector2 uv7 = new Vector2(uv6.x, uv0.y); // End Part - Top Right

            // Left Part of the Underline
            uvs0[0 + index] = uv0; // BL
            uvs0[1 + index] = uv1; // TL
            uvs0[2 + index] = uv2; // TR
            uvs0[3 + index] = uv3; // BR

            // Middle Part of the Underline
            uvs0[4 + index] = new Vector2(uv2.x - uv2.x * 0.001f, uv0.y);
            uvs0[5 + index] = new Vector2(uv2.x - uv2.x * 0.001f, uv1.y);
            uvs0[6 + index] = new Vector2(uv2.x + uv2.x * 0.001f, uv1.y);
            uvs0[7 + index] = new Vector2(uv2.x + uv2.x * 0.001f, uv0.y);

            // Right Part of the Underline
            uvs0[8 + index] = uv5;
            uvs0[9 + index] = uv4;
            uvs0[10 + index] = uv6;
            uvs0[11 + index] = uv7;

            // UNDERLINE UV2

            // UV1 contains Face / Border UV layout.
            float minUvX;
            float maxUvX = (vertices[index + 2].x - start.x) / (end.x - start.x);

            //Calculate the xScale or how much the UV's are getting stretched on the X axis for the middle section of the underline.
            float xScale = Mathf.Abs(sdfScale);

            Vector2[] uvs2 = textInfo.meshInfo[0].uvs2;

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

            // UNDERLINE VERTEX COLORS

            // Alpha is the lower of the vertex color or tag color alpha used.
            underlineColor.a = m_FontColor32.a < underlineColor.a ? m_FontColor32.a : underlineColor.a;

            Color32[] colors32 = textInfo.meshInfo[0].colors32;
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

            index += 12;
        }

        void DrawTextHighlight(Vector3 start, Vector3 end, ref int index, Color32 highlightColor, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            if (m_CachedUnderlineGlyphInfo == null)
            {
                if (!TextSettings.warningsDisabled)
                    Debug.LogWarning("Unable to add underline since the Font Asset doesn't contain the underline character.");
                return;
            }

            int verticesCount = index + 4;

            // Check to make sure our current mesh buffer allocations can hold these new Quads.
            if (verticesCount > textInfo.meshInfo[0].vertices.Length)
            {
                // Resize Mesh Buffers
                textInfo.meshInfo[0].ResizeMeshInfo(verticesCount / 4);
            }

            // UNDERLINE VERTICES FOR (3) LINE SEGMENTS
            Vector3[] vertices = textInfo.meshInfo[0].vertices;

            // Front Part of the Underline
            vertices[index + 0] = start; // BL
            vertices[index + 1] = new Vector3(start.x, end.y, 0); // TL
            vertices[index + 2] = end; // TR
            vertices[index + 3] = new Vector3(end.x, start.y, 0); // BR

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
            }


            // UNDERLINE UV0
            Vector2[] uvs0 = textInfo.meshInfo[0].uvs0;

            // Calculate UV required to setup the 3 Quads for the Underline.
            Vector2 uv0 = new Vector2(((float)m_CachedUnderlineGlyphInfo.glyph.glyphRect.x + m_CachedUnderlineGlyphInfo.glyph.glyphRect.width / 2) / generationSettings.fontAsset.atlasWidth, (m_CachedUnderlineGlyphInfo.glyph.glyphRect.y + (float)m_CachedUnderlineGlyphInfo.glyph.glyphRect.height / 2) / generationSettings.fontAsset.atlasHeight);  // bottom left

            // Left Part of the Underline
            uvs0[0 + index] = uv0; // BL
            uvs0[1 + index] = uv0; // TL
            uvs0[2 + index] = uv0; // TR
            uvs0[3 + index] = uv0; // BR

            // UNDERLINE UV2

            Vector2[] uvs2 = textInfo.meshInfo[0].uvs2;
            Vector2 customUv = new Vector2(0, 1);
            uvs2[0 + index] = customUv; // PackUV(-0.2f, -0.2f, xScale);
            uvs2[1 + index] = customUv; // PackUV(-0.2f, -0.1f, xScale);
            uvs2[2 + index] = customUv; // PackUV(-0.1f, -0.1f, xScale);
            uvs2[3 + index] = customUv; // PackUV(-0.1f, -0.2f, xScale);

            // HIGHLIGHT VERTEX COLORS

            // Alpha is the lower of the vertex color or tag color alpha used.
            highlightColor.a = m_FontColor32.a < highlightColor.a ? m_FontColor32.a : highlightColor.a;

            Color32[] colors32 = textInfo.meshInfo[0].colors32;
            colors32[0 + index] = highlightColor;
            colors32[1 + index] = highlightColor;
            colors32[2 + index] = highlightColor;
            colors32[3 + index] = highlightColor;

            index += 4;
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

        // This function parses through the Char[] to determine how many characters will be visible. It then makes sure the arrays are large enough for all those characters.
        // TODO: Generation settings should be a Struct.
        void SetArraySizes(int[] chars, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            int spriteCount = 0;

            m_TotalCharacterCount = 0;
            m_IsUsingBold = false;
            m_IsParsingText = false;
            m_TagNoParsing = false;
            m_FontStyleInternal = generationSettings.fontStyle;

            m_FontWeightInternal = (m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold ? FontWeight.Bold : generationSettings.fontWeight;
            m_FontWeightStack.SetDefault(m_FontWeightInternal);

            m_CurrentFontAsset = generationSettings.fontAsset;
            m_CurrentMaterial = generationSettings.material;
            m_CurrentMaterialIndex = 0;

            m_MaterialReferenceStack.SetDefault(new MaterialReference(m_CurrentMaterialIndex, m_CurrentFontAsset, null, m_CurrentMaterial, m_Padding));

            m_MaterialReferenceIndexLookup.Clear();
            MaterialReference.AddMaterialReference(m_CurrentMaterial, m_CurrentFontAsset, m_MaterialReferences, m_MaterialReferenceIndexLookup);

            if (textInfo == null)
                textInfo = new TextInfo();
            m_TextElementType = TextElementType.Character;

            // Parsing XML tags in the text
            for (int i = 0; i < chars.Length && chars[i] != 0; i++)
            {
                //Make sure the text element info array can hold the next text element.
                if (textInfo.textElementInfo == null || m_TotalCharacterCount >= textInfo.textElementInfo.Length)
                    TextInfo.Resize(ref textInfo.textElementInfo, m_TotalCharacterCount + 1, true);

                int unicode = chars[i];

                // PARSE XML TAGS
                if (generationSettings.richText && unicode == k_LesserThan) // if Char '<'
                {
                    int prevMatIndex = m_CurrentMaterialIndex;

                    // Check if Tag is Valid
                    int tagEnd;
                    if (ValidateHtmlTag(chars, i + 1, out tagEnd, generationSettings, textInfo))
                    {
                        i = tagEnd;

                        if ((m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold)
                            m_IsUsingBold = true;

                        if (m_TextElementType == TextElementType.Sprite)
                        {
                            m_MaterialReferences[m_CurrentMaterialIndex].referenceCount += 1;

                            textInfo.textElementInfo[m_TotalCharacterCount].character = (char)(k_SpritesStart + m_SpriteIndex);
                            textInfo.textElementInfo[m_TotalCharacterCount].spriteIndex = m_SpriteIndex;
                            textInfo.textElementInfo[m_TotalCharacterCount].fontAsset = m_CurrentFontAsset;
                            textInfo.textElementInfo[m_TotalCharacterCount].spriteAsset = m_CurrentSpriteAsset;
                            textInfo.textElementInfo[m_TotalCharacterCount].materialReferenceIndex = m_CurrentMaterialIndex;
                            textInfo.textElementInfo[m_TotalCharacterCount].elementType = m_TextElementType;

                            // Restore element type and material index to previous values.
                            m_TextElementType = TextElementType.Character;
                            m_CurrentMaterialIndex = prevMatIndex;

                            spriteCount += 1;
                            m_TotalCharacterCount += 1;
                        }

                        continue;
                    }
                }

                bool isUsingAlternativeTypeface = false;
                bool isUsingFallbackOrAlternativeTypeface = false;

                Character character;
                FontAsset tempFontAsset;
                FontAsset prevFontAsset = m_CurrentFontAsset;
                Material prevMaterial = m_CurrentMaterial;
                int prevMaterialIndex = m_CurrentMaterialIndex;

                // Handle Font Styles like LowerCase, UpperCase and SmallCaps.

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

                // Lookup the Glyph data for each character and cache it.
                character = FontUtilities.GetCharacterFromFontAsset((uint)unicode, m_CurrentFontAsset, false, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface, out tempFontAsset);

                // Search for the glyph in the list of fallback assigned to the primary font asset.
                if (character == null)
                {
                    if (m_CurrentFontAsset.fallbackFontAssetTable != null && m_CurrentFontAsset.fallbackFontAssetTable.Count > 0)
                        character = FontUtilities.GetCharacterFromFontAssets((uint)unicode, m_CurrentFontAsset.fallbackFontAssetTable, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface, out tempFontAsset);
                }

                // Search for the glyph in the Sprite Asset assigned to the text object.
                if (character == null)
                {
                    TextSpriteAsset spriteAsset = generationSettings.spriteAsset;

                    if (spriteAsset != null)
                    {
                        int spriteIndex = -1;

                        // Check Default Sprite Asset and its Fallbacks
                        spriteAsset = TextSpriteAsset.SearchForSpriteByUnicode(spriteAsset, (uint)unicode, true, out spriteIndex);

                        if (spriteIndex != -1)
                        {
                            m_TextElementType = TextElementType.Sprite;
                            textInfo.textElementInfo[m_TotalCharacterCount].elementType = m_TextElementType;

                            m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(spriteAsset.material, spriteAsset, m_MaterialReferences, m_MaterialReferenceIndexLookup);
                            m_MaterialReferences[m_CurrentMaterialIndex].referenceCount += 1;

                            textInfo.textElementInfo[m_TotalCharacterCount].character = (char)unicode;
                            textInfo.textElementInfo[m_TotalCharacterCount].spriteIndex = spriteIndex;
                            textInfo.textElementInfo[m_TotalCharacterCount].fontAsset = m_CurrentFontAsset;
                            textInfo.textElementInfo[m_TotalCharacterCount].spriteAsset = spriteAsset;
                            textInfo.textElementInfo[m_TotalCharacterCount].materialReferenceIndex = m_CurrentMaterialIndex;

                            // Restore element type and material index to previous values.
                            m_TextElementType = TextElementType.Character;
                            m_CurrentMaterialIndex = prevMaterialIndex;

                            spriteCount += 1;
                            m_TotalCharacterCount += 1;

                            continue;
                        }
                    }
                }

                // Search for the glyph in the list of fallback assigned in the TMP Settings (General Fallbacks).
                if (character == null)
                {
                    if (TextSettings.fallbackFontAssets != null && TextSettings.fallbackFontAssets.Count > 0)
                        character = FontUtilities.GetCharacterFromFontAssets((uint)unicode, TextSettings.fallbackFontAssets, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface, out tempFontAsset);
                }

                // Search for the glyph in the Default Font Asset assigned in the TMP Settings file.
                if (character == null)
                {
                    if (TextSettings.defaultFontAsset != null)
                        character = FontUtilities.GetCharacterFromFontAsset((uint)unicode, TextSettings.defaultFontAsset, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface, out tempFontAsset);
                }

                // This would kind of mirror native Emoji support.
                if (character == null)
                {
                    TextSpriteAsset spriteAsset = TextSettings.defaultSpriteAsset;

                    if (spriteAsset != null)
                    {
                        int spriteIndex = -1;

                        // Check Default Sprite Asset and its Fallbacks
                        spriteAsset = TextSpriteAsset.SearchForSpriteByUnicode(spriteAsset, (uint)unicode, true, out spriteIndex);

                        if (spriteIndex != -1)
                        {
                            m_TextElementType = TextElementType.Sprite;
                            textInfo.textElementInfo[m_TotalCharacterCount].elementType = m_TextElementType;

                            m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(spriteAsset.material, spriteAsset, m_MaterialReferences, m_MaterialReferenceIndexLookup);
                            m_MaterialReferences[m_CurrentMaterialIndex].referenceCount += 1;

                            textInfo.textElementInfo[m_TotalCharacterCount].character = (char)unicode;
                            textInfo.textElementInfo[m_TotalCharacterCount].spriteIndex = spriteIndex;
                            textInfo.textElementInfo[m_TotalCharacterCount].fontAsset = m_CurrentFontAsset;
                            textInfo.textElementInfo[m_TotalCharacterCount].spriteAsset = spriteAsset;
                            textInfo.textElementInfo[m_TotalCharacterCount].materialReferenceIndex = m_CurrentMaterialIndex;

                            // Restore element type and material index to previous values.
                            m_TextElementType = TextElementType.Character;
                            m_CurrentMaterialIndex = prevMaterialIndex;

                            spriteCount += 1;
                            m_TotalCharacterCount += 1;

                            continue;
                        }
                    }
                }

                // Replace missing glyph by the Square (9633) glyph or possibly the Space (32) glyph.
                if (character == null)
                {
                    // Save the original unicode character
                    int srcGlyph = unicode;

                    // Try replacing the missing glyph character by TMP Settings Missing Glyph or Square (9633) character.
                    unicode = chars[i] = TextSettings.missingGlyphCharacter == 0 ? 9633 : TextSettings.missingGlyphCharacter;

                    // Check for the missing glyph character in the currently assigned font asset and its fallbacks
                    character = FontUtilities.GetCharacterFromFontAsset((uint)unicode, m_CurrentFontAsset, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface, out tempFontAsset);

                    if (character == null)
                    {
                        // Search for the missing glyph character in the TMP Settings Fallback list.
                        if (TextSettings.fallbackFontAssets != null && TextSettings.fallbackFontAssets.Count > 0)
                            character = FontUtilities.GetCharacterFromFontAssets((uint)unicode, TextSettings.fallbackFontAssets, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface, out tempFontAsset);
                    }

                    if (character == null)
                    {
                        // Search for the missing glyph in the TMP Settings Default Font Asset.
                        if (TextSettings.defaultFontAsset != null)
                            character = FontUtilities.GetCharacterFromFontAsset((uint)unicode, TextSettings.defaultFontAsset, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface, out tempFontAsset);
                    }

                    if (character == null)
                    {
                        // Use Space (32) Glyph from the currently assigned font asset.
                        unicode = chars[i] = 32;
                        character = FontUtilities.GetCharacterFromFontAsset((uint)unicode, m_CurrentFontAsset, true, m_FontStyleInternal, m_FontWeightInternal, out isUsingAlternativeTypeface, out tempFontAsset);
                        if (!TextSettings.warningsDisabled)
                            Debug.LogWarning("Character with ASCII value of " + srcGlyph + " was not found in the Font Asset Glyph Table. It was replaced by a space.");
                    }
                }

                // Determine if the font asset is still the current font asset or a fallback.
                if (tempFontAsset != null)
                {
                    if (tempFontAsset.GetInstanceID() != m_CurrentFontAsset.GetInstanceID())
                    {
                        isUsingFallbackOrAlternativeTypeface = true;
                        m_CurrentFontAsset = tempFontAsset;
                    }
                }

                textInfo.textElementInfo[m_TotalCharacterCount].elementType = TextElementType.Character;
                textInfo.textElementInfo[m_TotalCharacterCount].textElement = character;
                textInfo.textElementInfo[m_TotalCharacterCount].isUsingAlternateTypeface = isUsingAlternativeTypeface;
                textInfo.textElementInfo[m_TotalCharacterCount].character = (char)unicode;
                textInfo.textElementInfo[m_TotalCharacterCount].fontAsset = m_CurrentFontAsset;

                if (isUsingFallbackOrAlternativeTypeface)
                {
                    // Create Fallback material instance matching current material preset if necessary
                    if (TextSettings.matchMaterialPreset)
                        m_CurrentMaterial = MaterialManager.GetFallbackMaterial(m_CurrentMaterial, m_CurrentFontAsset.material);
                    else
                        m_CurrentMaterial = m_CurrentFontAsset.material;

                    m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(m_CurrentMaterial, m_CurrentFontAsset, m_MaterialReferences, m_MaterialReferenceIndexLookup);
                }

                if (!char.IsWhiteSpace((char)unicode) && unicode != k_ZeroWidthSpace)
                {
                    // Limit the mesh of the main text object to 65535 vertices and use sub objects for the overflow.
                    if (m_MaterialReferences[m_CurrentMaterialIndex].referenceCount < k_VerticesMax)
                        m_MaterialReferences[m_CurrentMaterialIndex].referenceCount += 1;
                    else
                    {
                        m_CurrentMaterialIndex = MaterialReference.AddMaterialReference(new Material(m_CurrentMaterial), m_CurrentFontAsset, m_MaterialReferences, m_MaterialReferenceIndexLookup);
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
                return;
            }

            // Save material and sprite count.
            textInfo.spriteCount = spriteCount;
            int materialCount = textInfo.materialCount = m_MaterialReferenceIndexLookup.Count;

            // Check if we need to resize the MeshInfo array for handling different materials.
            if (materialCount > textInfo.meshInfo.Length)
                TextInfo.Resize(ref textInfo.meshInfo, materialCount, false);

            // Resize textElementInfo[] if allocations are excessive
            if (textInfo.textElementInfo.Length - m_TotalCharacterCount > 256)
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

        void GetSpecialCharacters(FontAsset fontAsset)
        {
            // Check & Assign Underline Character for use with the Underline tag.
            fontAsset.characterLookupTable.TryGetValue(k_Underline, out m_CachedUnderlineGlyphInfo);

            // Check & Assign Underline Character for use with the Underline tag.
            fontAsset.characterLookupTable.TryGetValue(k_HorizontalEllipsis, out m_CachedEllipsisGlyphInfo);
        }

        /// <summary>
        /// Get the padding value for the currently assigned material.
        /// </summary>
        /// <returns></returns>
        float GetPaddingForMaterial(Material material, bool extraPadding)
        {
            ShaderUtilities.GetShaderPropertyIDs();

            if (material == null)
                return 0;

            m_Padding = ShaderUtilities.GetPadding(material, extraPadding, m_IsUsingBold);
            m_IsMaskingEnabled = ShaderUtilities.IsMaskingEnabled(material);
            m_IsSdfShader = material.HasProperty(ShaderUtilities.ID_WeightNormal);

            return m_Padding;
        }

        /// <summary>
        /// Method to calculate the preferred width of a text object.
        /// </summary>
        /// <returns></returns>
        float GetPreferredWidthInternal(TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            if (TextSettings.instance == null)
                return 0;

            float fontSize = generationSettings.autoSize ? generationSettings.fontSizeMax : m_FontSize;

            // Reset auto sizing point size bounds
            m_MinFontSize = generationSettings.fontSizeMin;
            m_MaxFontSize = generationSettings.fontSizeMax;
            m_CharWidthAdjDelta = 0;

            // Set Margins to Infinity
            Vector2 margin = TextGeneratorUtilities.largePositiveVector2;

            m_RecursiveCount = 0;
            float preferredWidth = CalculatePreferredValues(fontSize, margin, true, generationSettings, textInfo).x;

            return preferredWidth;
        }

        /// <summary>
        /// Method to calculate the preferred height of a text object.
        /// </summary>
        /// <returns></returns>
        float GetPreferredHeightInternal(TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            if (TextSettings.instance == null)
                return 0;

            float fontSize = generationSettings.autoSize ? generationSettings.fontSizeMax : m_FontSize;

            // Reset auto sizing point size bounds
            m_MinFontSize = generationSettings.fontSizeMin;
            m_MaxFontSize = generationSettings.fontSizeMax;
            m_CharWidthAdjDelta = 0;

            Vector2 margin = new Vector2(m_MarginWidth != 0 ? m_MarginWidth : TextGeneratorUtilities.largePositiveFloat, TextGeneratorUtilities.largePositiveFloat);

            m_RecursiveCount = 0;
            float preferredHeight = CalculatePreferredValues(fontSize, margin, !generationSettings.autoSize, generationSettings, textInfo).y;

            return preferredHeight;
        }

        Vector2 GetPreferredValuesInternal(TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            if (TextSettings.instance == null)
                return Vector2.zero;

            float fontSize = generationSettings.autoSize ? generationSettings.fontSizeMax : m_FontSize;

            // Reset auto sizing point size bounds
            m_MinFontSize = generationSettings.fontSizeMin;
            m_MaxFontSize = generationSettings.fontSizeMax;
            m_CharWidthAdjDelta = 0;

            Vector2 margin = new Vector2(m_MarginWidth != 0 ? m_MarginWidth : TextGeneratorUtilities.largePositiveFloat, TextGeneratorUtilities.largePositiveFloat);

            m_RecursiveCount = 0;
            return CalculatePreferredValues(fontSize, margin, !generationSettings.autoSize, generationSettings, textInfo);
        }

        /// <summary>
        /// Method to calculate the preferred width and height of the text object.
        /// </summary>
        /// <returns></returns>
        protected virtual Vector2 CalculatePreferredValues(float defaultFontSize, Vector2 marginSize, bool ignoreTextAutoSizing, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            Profiler.BeginSample("TextGenerator.CalculatePreferredValues");
            // Early exit if no font asset was assigned. This should not be needed since LiberationSans SDF will be assigned by default.
            if (generationSettings.fontAsset == null || generationSettings.fontAsset.characterLookupTable == null)
            {
                return Vector2.zero;
            }

            // Early exit if we don't have any Text to generate.
            if (m_CharBuffer == null || m_CharBuffer.Length == 0 || m_CharBuffer[0] == (char)0)
            {
                return Vector2.zero;
            }

            m_CurrentFontAsset = generationSettings.fontAsset;
            m_CurrentMaterial = generationSettings.material;
            m_CurrentMaterialIndex = 0;
            m_MaterialReferenceStack.SetDefault(new MaterialReference(0, m_CurrentFontAsset, null, m_CurrentMaterial, m_Padding));

            // Total character count is computed when the text is parsed.
            int totalCharacterCount = m_TotalCharacterCount;

            if (m_InternalTextElementInfo == null || totalCharacterCount > m_InternalTextElementInfo.Length)
            {
                m_InternalTextElementInfo = new TextElementInfo[totalCharacterCount > 1024 ? totalCharacterCount + 256 : Mathf.NextPowerOfTwo(totalCharacterCount)];
            }

            // Calculate the scale of the font based on selected font size and sampling point size.
            // baseScale is calculated using the font asset assigned to the text object.
            float baseScale = m_FontScale = (defaultFontSize / generationSettings.fontAsset.faceInfo.pointSize * generationSettings.fontAsset.faceInfo.scale);
            float currentElementScale = baseScale;
            m_FontScaleMultiplier = 1;

            m_CurrentFontSize = defaultFontSize;
            m_SizeStack.SetDefault(m_CurrentFontSize);

            int charCode; // Holds the character code of the currently being processed character.

            m_FontStyleInternal = generationSettings.fontStyle; // Set the default style.

            m_LineJustification = generationSettings.textAlignment; // Sets the line justification mode to match editor alignment.
            m_LineJustificationStack.SetDefault(m_LineJustification);

            float boldXAdvanceMultiplier; // Used to increase spacing between character when style is bold.

            m_BaselineOffset = 0; // Used by subscript characters.
            m_BaselineOffsetStack.Clear();

            m_LineOffset = 0; // Amount of space between lines (font line spacing + m_linespacing).
            m_LineHeight = k_FloatUnset;
            float lineGap = m_CurrentFontAsset.faceInfo.lineHeight - (m_CurrentFontAsset.faceInfo.ascentLine - m_CurrentFontAsset.faceInfo.descentLine);

            m_CSpacing = 0; // Amount of space added between characters as a result of the use of the <cspace> tag.
            m_MonoSpacing = 0;
            float lineOffsetDelta;
            m_XAdvance = 0; // Used to track the position of each character.
            float maxXAdvance = 0; // Used to determine Preferred Width.

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

            float marginWidth = marginSize.x;
            m_MarginLeft = 0;
            m_MarginRight = 0;
            m_Width = -1;

            // Used by Unity's Auto Layout system.
            float renderedWidth = 0;
            float renderedHeight = 0;
            float linebreakingWidth = 0;
            m_IsCalculatingPreferredValues = true;

            // Tracking of the highest Ascender
            m_MaxAscender = 0;
            m_MaxDescender = 0;

            // Initialize struct to track states of word wrapping
            bool isFirstWord = true;
            bool isLastBreakingChar = false;
            WordWrapState savedLineState = new WordWrapState();
            SaveWordWrappingState(ref savedLineState, 0, 0, textInfo);
            WordWrapState savedWordWrapState = new WordWrapState();
            int wrappingIndex = 0;

            // Counter to prevent recursive lockup when computing preferred values.
            m_RecursiveCount += 1;

            int endTagIndex;
            // Parse through Character buffer to read HTML tags and begin creating mesh.
            for (int i = 0; m_CharBuffer[i] != 0; i++)
            {
                charCode = m_CharBuffer[i];
                m_TextElementType = textInfo.textElementInfo[m_CharacterCount].elementType;

                m_CurrentMaterialIndex = textInfo.textElementInfo[m_CharacterCount].materialReferenceIndex;
                m_CurrentFontAsset = m_MaterialReferences[m_CurrentMaterialIndex].fontAsset;

                int prevMaterialIndex = m_CurrentMaterialIndex;

                // Parse Rich Text Tag
                if (generationSettings.richText && charCode == k_LesserThan)  // '<'
                {
                    m_IsParsingText = true;
                    m_TextElementType = TextElementType.Character;

                    // Check if Tag is valid. If valid, skip to the end of the validated tag.
                    if (ValidateHtmlTag(m_CharBuffer, i + 1, out endTagIndex, generationSettings, textInfo))
                    {
                        i = endTagIndex;

                        // Continue to next character or handle the sprite element
                        if (m_TextElementType == TextElementType.Character)
                            continue;
                    }
                }

                m_IsParsingText = false;

                bool isUsingAltTypeface = textInfo.textElementInfo[m_CharacterCount].isUsingAlternateTypeface;

                // Handle Font Styles like LowerCase, UpperCase and SmallCaps.

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

                // Look up Character Data from Dictionary and cache it.
                if (m_TextElementType == TextElementType.Sprite)
                {
                    // If a sprite is used as a fallback then get a reference to it and set the color to white.
                    m_CurrentSpriteAsset = textInfo.textElementInfo[m_CharacterCount].spriteAsset;
                    m_SpriteIndex = textInfo.textElementInfo[m_CharacterCount].spriteIndex;

                    SpriteCharacter sprite = m_CurrentSpriteAsset.spriteCharacterTable[m_SpriteIndex];
                    if (sprite == null)
                        continue;

                    // Sprites are assigned in the E000 Private Area + sprite Index
                    if (charCode == k_LesserThan)
                        charCode = k_SpritesStart + m_SpriteIndex;

                    m_CurrentFontAsset = generationSettings.fontAsset;

                    // The sprite scale calculations are based on the font asset assigned to the text object.
                    // Sprite scale is used to determine line height
                    // Current element scale represents a modified scale to normalize the sprite based on the font baseline to ascender.
                    float spriteScale = (m_CurrentFontSize / generationSettings.fontAsset.faceInfo.pointSize * generationSettings.fontAsset.faceInfo.scale);
                    currentElementScale = generationSettings.fontAsset.faceInfo.ascentLine / sprite.glyph.metrics.height * sprite.scale * spriteScale;

                    m_CachedTextElement = sprite;

                    m_InternalTextElementInfo[m_CharacterCount].elementType = TextElementType.Sprite;
                    m_InternalTextElementInfo[m_CharacterCount].scale = spriteScale;

                    m_CurrentMaterialIndex = prevMaterialIndex;
                }
                else if (m_TextElementType == TextElementType.Character)
                {
                    m_CachedTextElement = textInfo.textElementInfo[m_CharacterCount].textElement;
                    if (m_CachedTextElement == null)
                        continue;

                    m_CurrentMaterialIndex = textInfo.textElementInfo[m_CharacterCount].materialReferenceIndex;

                    // Re-calculate font scale as the font asset may have changed.
                    m_FontScale = m_CurrentFontSize * smallCapsMultiplier / m_CurrentFontAsset.faceInfo.pointSize * m_CurrentFontAsset.faceInfo.scale;

                    currentElementScale = m_FontScale * m_FontScaleMultiplier * m_CachedTextElement.scale;

                    m_InternalTextElementInfo[m_CharacterCount].elementType = TextElementType.Character;
                }

                // Handle Soft Hyphen
                float oldScale = currentElementScale;
                if (charCode == k_SoftHyphen)
                {
                    currentElementScale = 0;
                }

                // Store some of the text object's information
                m_InternalTextElementInfo[m_CharacterCount].character = (char)charCode;

                // Handle Kerning if Enabled.
                GlyphValueRecord glyphAdjustments = new GlyphValueRecord();
                if (generationSettings.enableKerning)
                {
                    KerningPair adjustmentPair;

                    if (m_CharacterCount < totalCharacterCount - 1)
                    {
                        uint nextGlyph = textInfo.textElementInfo[m_CharacterCount + 1].character;
                        KerningPairKey keyValue = new KerningPairKey((uint)charCode, nextGlyph);

                        m_CurrentFontAsset.kerningLookupDictionary.TryGetValue((int)keyValue.key, out adjustmentPair);
                        if (adjustmentPair != null)
                            glyphAdjustments = adjustmentPair.firstGlyphAdjustments;
                    }

                    if (m_CharacterCount >= 1)
                    {
                        uint previousGlyph = textInfo.textElementInfo[m_CharacterCount - 1].character;
                        KerningPairKey keyValue = new KerningPairKey(previousGlyph, (uint)charCode);

                        m_CurrentFontAsset.kerningLookupDictionary.TryGetValue((int)keyValue.key, out adjustmentPair);
                        if (adjustmentPair != null)
                            glyphAdjustments += adjustmentPair.secondGlyphAdjustments;
                    }
                }

                // Handle Mono Spacing
                float monoAdvance = 0;
                if (m_MonoSpacing != 0)
                {
                    monoAdvance = (m_MonoSpacing / 2 - (m_CachedTextElement.glyph.metrics.width / 2 + m_CachedTextElement.glyph.metrics.horizontalBearingX) * currentElementScale);
                    m_XAdvance += monoAdvance;
                }

                // Set Padding based on selected font style
                if (m_TextElementType == TextElementType.Character && !isUsingAltTypeface && ((m_FontStyleInternal & FontStyles.Bold) == FontStyles.Bold)) // Checks for any combination of Bold Style.
                {
                    boldXAdvanceMultiplier = 1 + m_CurrentFontAsset.boldStyleSpacing * 0.01f;
                }
                else
                {
                    boldXAdvanceMultiplier = 1.0f;
                }

                m_InternalTextElementInfo[m_CharacterCount].baseLine = 0 - m_LineOffset + m_BaselineOffset;

                // Compute and save text element Ascender and maximum line Ascender.
                float elementAscender = m_CurrentFontAsset.faceInfo.ascentLine * (m_TextElementType == TextElementType.Character ? currentElementScale / smallCapsMultiplier : m_InternalTextElementInfo[m_CharacterCount].scale) + m_BaselineOffset;
                m_InternalTextElementInfo[m_CharacterCount].ascender = elementAscender - m_LineOffset;
                m_MaxLineAscender = elementAscender > m_MaxLineAscender ? elementAscender : m_MaxLineAscender;

                // Compute and save text element Descender and maximum line Descender.
                float elementDescender = m_CurrentFontAsset.faceInfo.descentLine * (m_TextElementType == TextElementType.Character ? currentElementScale / smallCapsMultiplier : m_InternalTextElementInfo[m_CharacterCount].scale) + m_BaselineOffset;
                m_MaxLineDescender = elementDescender < m_MaxLineDescender ? elementDescender : m_MaxLineDescender;

                // Adjust maxLineAscender and maxLineDescender if style is superscript or subscript
                if ((m_FontStyleInternal & FontStyles.Subscript) == FontStyles.Subscript || (m_FontStyleInternal & FontStyles.Superscript) == FontStyles.Superscript)
                {
                    float baseAscender = (elementAscender - m_BaselineOffset) / m_CurrentFontAsset.faceInfo.subscriptSize;
                    elementAscender = m_MaxLineAscender;
                    m_MaxLineAscender = baseAscender > m_MaxLineAscender ? baseAscender : m_MaxLineAscender;

                    float baseDescender = (elementDescender - m_BaselineOffset) / m_CurrentFontAsset.faceInfo.subscriptSize;
                    m_MaxLineDescender = baseDescender < m_MaxLineDescender ? baseDescender : m_MaxLineDescender;
                }

                if (m_LineNumber == 0)
                    m_MaxAscender = m_MaxAscender > elementAscender ? m_MaxAscender : elementAscender;

                // Setup Mesh for visible text elements. ie. not a SPACE / LINEFEED / CARRIAGE RETURN.
                if (charCode == 9 || (!char.IsWhiteSpace((char)charCode) && charCode != 0x200B) || m_TextElementType == TextElementType.Sprite)
                {
                    // Check if Character exceeds the width of the Text Container
                    float width = m_Width != -1 ? Mathf.Min(marginWidth + 0.0001f - m_MarginLeft - m_MarginRight, m_Width) : marginWidth + 0.0001f - m_MarginLeft - m_MarginRight;

                    bool isJustifiedOrFlush = ((HorizontalAlignment)m_LineJustification & HorizontalAlignment.Flush) == HorizontalAlignment.Flush || ((HorizontalAlignment)m_LineJustification & HorizontalAlignment.Justified) == HorizontalAlignment.Justified;

                    // Calculate the line breaking width of the text.
                    linebreakingWidth = m_XAdvance + m_CachedTextElement.glyph.metrics.horizontalAdvance * (1 - m_CharWidthAdjDelta) * (charCode != k_SoftHyphen ? currentElementScale : oldScale);

                    // Check if Character exceeds the width of the Text Container
                    if (linebreakingWidth > width * (isJustifiedOrFlush ? 1.05f : 1.0f))
                    {
                        // Word Wrapping
                        if (generationSettings.wordWrap && m_CharacterCount != m_FirstCharacterOfLine)
                        {
                            // Check if word wrapping is still possible
                            if (wrappingIndex == savedWordWrapState.previousWordBreak || isFirstWord)
                            {
                                // Word wrapping is no longer possible. Shrink size of text if auto-sizing is enabled.
                                if (ignoreTextAutoSizing == false && m_CurrentFontSize > generationSettings.fontSizeMin)
                                {
                                    // Handle Character Width Adjustments
                                    if (m_CharWidthAdjDelta < generationSettings.charWidthMaxAdj / 100)
                                    {
                                        m_RecursiveCount = 0;
                                        m_CharWidthAdjDelta += 0.01f;
                                        return CalculatePreferredValues(defaultFontSize, marginSize, false, generationSettings, textInfo);
                                    }

                                    // Adjust Point Size
                                    m_MaxFontSize = defaultFontSize;

                                    defaultFontSize -= Mathf.Max((defaultFontSize - m_MinFontSize) / 2, 0.05f);
                                    defaultFontSize = (int)(Mathf.Max(defaultFontSize, generationSettings.fontSizeMin) * 20 + 0.5f) / 20f;

                                    if (m_RecursiveCount > 20)
                                        return new Vector2(renderedWidth, renderedHeight);
                                    return CalculatePreferredValues(defaultFontSize, marginSize, false, generationSettings, textInfo);
                                }

                                // Word wrapping is no longer possible, now breaking up individual words.
                                if (m_IsCharacterWrappingEnabled == false)
                                {
                                    m_IsCharacterWrappingEnabled = true;
                                }
                                else
                                    isLastBreakingChar = true;
                            }


                            // Restore to previously stored state of last valid (space character or linefeed)
                            i = RestoreWordWrappingState(ref savedWordWrapState, textInfo);
                            wrappingIndex = i;  // Used to detect when line length can no longer be reduced.

                            // Handling for Soft Hyphen
                            if (m_CharBuffer[i] == k_SoftHyphen)
                            {
                                m_CharBuffer[i] = k_HyphenMinus;
                                return CalculatePreferredValues(defaultFontSize, marginSize, true, generationSettings, textInfo);
                            }

                            // Check if Line Spacing of previous line needs to be adjusted.
                            if (m_LineNumber > 0 && !TextGeneratorUtilities.Approximately(m_MaxLineAscender, m_StartOfLineAscender) && m_LineHeight == k_FloatUnset)
                            {
                                float offsetDelta = m_MaxLineAscender - m_StartOfLineAscender;
                                m_LineOffset += offsetDelta;
                                savedWordWrapState.lineOffset = m_LineOffset;
                                savedWordWrapState.previousLineAscender = m_MaxLineAscender;
                            }

                            // Calculate lineAscender & make sure if last character is superscript or subscript that we check that as well.
                            float lineAscender = m_MaxLineAscender - m_LineOffset;
                            float lineDescender = m_MaxLineDescender - m_LineOffset;

                            // Update maxDescender and maxVisibleDescender
                            m_MaxDescender = m_MaxDescender < lineDescender ? m_MaxDescender : lineDescender;

                            m_FirstCharacterOfLine = m_CharacterCount; // Store first character of the next line.

                            // Compute Preferred Width & Height
                            renderedWidth += m_XAdvance;

                            if (generationSettings.wordWrap)
                                renderedHeight = m_MaxAscender - m_MaxDescender;
                            else
                                renderedHeight = Mathf.Max(renderedHeight, lineAscender - lineDescender);

                            // Store the state of the line before starting on the new line.
                            SaveWordWrappingState(ref savedLineState, i, m_CharacterCount - 1, textInfo);

                            m_LineNumber += 1;

                            // Apply Line Spacing based on scale of the last character of the line.
                            if (m_LineHeight == k_FloatUnset)
                            {
                                float ascender = m_InternalTextElementInfo[m_CharacterCount].ascender - m_InternalTextElementInfo[m_CharacterCount].baseLine;
                                lineOffsetDelta = 0 - m_MaxLineDescender + ascender + (lineGap + generationSettings.lineSpacing + m_LineSpacingDelta) * baseScale;
                                m_LineOffset += lineOffsetDelta;

                                m_StartOfLineAscender = ascender;
                            }
                            else
                                m_LineOffset += m_LineHeight + generationSettings.lineSpacing * baseScale;

                            m_MaxLineAscender = TextGeneratorUtilities.largeNegativeFloat;
                            m_MaxLineDescender = TextGeneratorUtilities.largePositiveFloat;

                            m_XAdvance = 0 + m_TagIndent;

                            continue;
                        }

                        // Text Auto-Sizing (text exceeding Width of container.
                        if (ignoreTextAutoSizing == false && defaultFontSize > generationSettings.fontSizeMin)
                        {
                            // Handle Character Width Adjustments
                            if (m_CharWidthAdjDelta < generationSettings.charWidthMaxAdj / 100)
                            {
                                m_RecursiveCount = 0;
                                m_CharWidthAdjDelta += 0.01f;
                                return CalculatePreferredValues(defaultFontSize, marginSize, false, generationSettings, textInfo);
                            }

                            // Adjust Point Size
                            m_MaxFontSize = defaultFontSize;

                            defaultFontSize -= Mathf.Max((defaultFontSize - m_MinFontSize) / 2, 0.05f);
                            defaultFontSize = (int)(Mathf.Max(defaultFontSize, generationSettings.fontSizeMin) * 20 + 0.5f) / 20f;

                            if (m_RecursiveCount > 20)
                                return new Vector2(renderedWidth, renderedHeight);
                            return CalculatePreferredValues(defaultFontSize, marginSize, false, generationSettings, textInfo);
                        }
                    }
                }

                // Check if Line Spacing of previous line needs to be adjusted.
                if (m_LineNumber > 0 && !TextGeneratorUtilities.Approximately(m_MaxLineAscender, m_StartOfLineAscender) && m_LineHeight == k_FloatUnset && !m_IsNewPage)
                {
                    float offsetDelta = m_MaxLineAscender - m_StartOfLineAscender;
                    m_LineOffset += offsetDelta;

                    m_StartOfLineAscender += offsetDelta;
                    savedWordWrapState.lineOffset = m_LineOffset;
                    savedWordWrapState.previousLineAscender = m_StartOfLineAscender;
                }


                // Handle xAdvance & Tabulation Stops. Tab stops at every 25% of Font Size.
                if (charCode == 9)
                {
                    float tabSize = m_CurrentFontAsset.faceInfo.tabWidth * m_CurrentFontAsset.tabMultiple * currentElementScale;
                    float tabs = Mathf.Ceil(m_XAdvance / tabSize) * tabSize;
                    m_XAdvance = tabs > m_XAdvance ? tabs : m_XAdvance + tabSize;
                }
                else if (m_MonoSpacing != 0)
                {
                    m_XAdvance += (m_MonoSpacing - monoAdvance + ((generationSettings.characterSpacing + m_CurrentFontAsset.regularStyleSpacing) * currentElementScale) + m_CSpacing) * (1 - m_CharWidthAdjDelta);

                    if (char.IsWhiteSpace((char)charCode) || charCode == k_ZeroWidthSpace)
                        m_XAdvance += generationSettings.wordSpacing * currentElementScale;
                }
                else
                {
                    m_XAdvance += ((m_CachedTextElement.glyph.metrics.horizontalAdvance * boldXAdvanceMultiplier + generationSettings.characterSpacing + m_CurrentFontAsset.regularStyleSpacing + glyphAdjustments.xAdvance) * currentElementScale + m_CSpacing) * (1 - m_CharWidthAdjDelta);

                    if (char.IsWhiteSpace((char)charCode) || charCode == k_ZeroWidthSpace)
                        m_XAdvance += generationSettings.wordSpacing * currentElementScale;
                }

                // Handle Carriage Return
                if (charCode == k_CarriageReturn)
                {
                    maxXAdvance = Mathf.Max(maxXAdvance, renderedWidth + m_XAdvance);
                    renderedWidth = 0;
                    m_XAdvance = 0 + m_TagIndent;
                }

                // Handle Line Spacing Adjustments + Word Wrapping & special case for last line.
                if (charCode == k_LineFeed || m_CharacterCount == totalCharacterCount - 1)
                {
                    // Check if Line Spacing of previous line needs to be adjusted.
                    if (m_LineNumber > 0 && !TextGeneratorUtilities.Approximately(m_MaxLineAscender, m_StartOfLineAscender) && m_LineHeight == k_FloatUnset)
                    {
                        float offsetDelta = m_MaxLineAscender - m_StartOfLineAscender;
                        m_LineOffset += offsetDelta;
                    }

                    // Calculate lineAscender & make sure if last character is superscript or subscript that we check that as well.
                    float lineDescender = m_MaxLineDescender - m_LineOffset;

                    // Update maxDescender and maxVisibleDescender
                    m_MaxDescender = m_MaxDescender < lineDescender ? m_MaxDescender : lineDescender;

                    m_FirstCharacterOfLine = m_CharacterCount + 1;

                    // Store PreferredWidth paying attention to linefeed and last character of text.
                    if (charCode == k_LineFeed && m_CharacterCount != totalCharacterCount - 1)
                    {
                        maxXAdvance = Mathf.Max(maxXAdvance, renderedWidth + linebreakingWidth);
                        renderedWidth = 0;
                    }
                    else
                        renderedWidth = Mathf.Max(maxXAdvance, renderedWidth + linebreakingWidth);

                    renderedHeight = m_MaxAscender - m_MaxDescender;

                    // Add new line if not last lines or character.
                    if (charCode == k_LineFeed)
                    {
                        // Store the state of the line before starting on the new line.
                        SaveWordWrappingState(ref savedLineState, i, m_CharacterCount, textInfo);
                        // Store the state of the last Character before the new line.
                        SaveWordWrappingState(ref savedWordWrapState, i, m_CharacterCount, textInfo);

                        m_LineNumber += 1;

                        // Apply Line Spacing
                        if (m_LineHeight == k_FloatUnset)
                        {
                            lineOffsetDelta = 0 - m_MaxLineDescender + elementAscender + (lineGap + generationSettings.lineSpacing + generationSettings.paragraphSpacing + m_LineSpacingDelta) * baseScale;
                            m_LineOffset += lineOffsetDelta;
                        }
                        else
                            m_LineOffset += m_LineHeight + (generationSettings.lineSpacing + generationSettings.paragraphSpacing) * baseScale;

                        m_MaxLineAscender = TextGeneratorUtilities.largeNegativeFloat;
                        m_MaxLineDescender = TextGeneratorUtilities.largePositiveFloat;
                        m_StartOfLineAscender = elementAscender;

                        m_XAdvance = 0 + m_TagLineIndent + m_TagIndent;

                        m_CharacterCount += 1;
                        continue;
                    }
                }

                // Save State of Mesh Creation for handling of Word Wrapping
                if (generationSettings.wordWrap || generationSettings.overflowMode == TextOverflowMode.Truncate || generationSettings.overflowMode == TextOverflowMode.Ellipsis)
                {
                    if ((char.IsWhiteSpace((char)charCode) || charCode == k_ZeroWidthSpace || charCode == k_HyphenMinus || charCode == k_SoftHyphen) && !m_IsNonBreakingSpace && charCode != k_NoBreakSpace && charCode != k_NonBreakingHyphen && charCode != k_NarrowNoBreakSpace && charCode != k_WordJoiner)
                    {
                        // We store the state of numerous variables for the most recent Space, LineFeed or Carriage Return to enable them to be restored
                        // for Word Wrapping.
                        SaveWordWrappingState(ref savedWordWrapState, i, m_CharacterCount, textInfo);
                        m_IsCharacterWrappingEnabled = false;
                        isFirstWord = false;
                    }
                    // Handling for East Asian languages
                    else if ((charCode > k_HangulJamoStart && charCode < k_HangulJamoEnd || /* Hangul Jamo */
                              charCode > k_CjkStart && charCode < k_CjkEnd ||   /* CJK */
                              charCode > k_HangulJameExtendedStart && charCode < k_HangulJameExtendedEnd ||   /* Hangul Jame Extended-A */
                              charCode > k_HangulSyllablesStart && charCode < k_HangulSyllablesEnd ||   /* Hangul Syllables */
                              charCode > k_CjkIdeographsStart && charCode < k_CjkIdeographsEnd ||   /* CJK Compatibility Ideographs */
                              charCode > k_CjkFormsStart && charCode < k_CjkFormsEnd ||   /* CJK Compatibility Forms */
                              charCode > k_CjkHalfwidthStart && charCode < k_CjkHalfwidthEnd)     /* CJK Halfwidth */
                             && !m_IsNonBreakingSpace)
                    {
                        if (isFirstWord || isLastBreakingChar || TextSettings.linebreakingRules.leadingCharacters.ContainsKey(charCode) == false &&
                            (m_CharacterCount < totalCharacterCount - 1 &&
                             TextSettings.linebreakingRules.followingCharacters.ContainsKey(m_InternalTextElementInfo[m_CharacterCount + 1].character) == false))
                        {
                            SaveWordWrappingState(ref savedWordWrapState, i, m_CharacterCount, textInfo);
                            m_IsCharacterWrappingEnabled = false;
                            isFirstWord = false;
                        }
                    }
                    else if (isFirstWord || m_IsCharacterWrappingEnabled || isLastBreakingChar)
                        SaveWordWrappingState(ref savedWordWrapState, i, m_CharacterCount, textInfo);
                }

                m_CharacterCount += 1;
            }

            // Check Auto Sizing and increase font size to fill text container.
            var fontSizeDelta = m_MaxFontSize - m_MinFontSize;
            if (!m_IsCharacterWrappingEnabled && !ignoreTextAutoSizing && fontSizeDelta > 0.051f && defaultFontSize < generationSettings.fontSizeMax)
            {
                m_MinFontSize = defaultFontSize;
                defaultFontSize += Mathf.Max((m_MaxFontSize - defaultFontSize) / 2, 0.05f);
                defaultFontSize = (int)(Mathf.Min(defaultFontSize, generationSettings.fontSizeMax) * 20 + 0.5f) / 20f;

                if (m_RecursiveCount > 20)
                    return new Vector2(renderedWidth, renderedHeight);
                return CalculatePreferredValues(defaultFontSize, marginSize, false, generationSettings, textInfo);
            }

            m_IsCharacterWrappingEnabled = false;
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
    }
}
