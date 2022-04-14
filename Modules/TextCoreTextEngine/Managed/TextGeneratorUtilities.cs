// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;

namespace UnityEngine.TextCore.Text
{
    [Serializable]
    struct MeshExtents
    {
        public Vector2 min;
        public Vector2 max;

        public MeshExtents(Vector2 min, Vector2 max)
        {
            this.min = min;
            this.max = max;
        }

        public override string ToString()
        {
            string s = "Min (" + min.x.ToString("f2") + ", " + min.y.ToString("f2") + ")   Max (" + max.x.ToString("f2") + ", " + max.y.ToString("f2") + ")";
            return s;
        }
    }

    struct XmlTagAttribute
    {
        public int nameHashCode;
        public TagValueType valueType;
        public int valueStartIndex;
        public int valueLength;
        public int valueHashCode;
    }

    internal struct RichTextTagAttribute
    {
        public int nameHashCode;
        public int valueHashCode;
        public TagValueType valueType;
        public int valueStartIndex;
        public int valueLength;
        public TagUnitType unitType;
    }

    [System.Diagnostics.DebuggerDisplay("Unicode ({unicode})  '{(char)unicode}'")]
    internal struct UnicodeChar
    {
        public int unicode;
        public int stringIndex;
        public int length;
    }

    /// <summary>
    ///
    /// </summary>
    struct TextBackingContainer
    {
        public int Capacity
        {
            get { return m_Array.Length; }
        }

        public int Count
        {
            get { return m_Count; }
            set { m_Count = value; }
        }

        private uint[] m_Array;
        private int m_Count;

        public uint this[int index]
        {
            get { return m_Array[index]; }
            set
            {
                if (index >= m_Array.Length)
                    Resize(index);

                m_Array[index] = value;
            }
        }

        public TextBackingContainer(int size)
        {
            m_Array = new uint[size];
            m_Count = 0;
        }

        public void Resize(int size)
        {
            size = Mathf.NextPowerOfTwo(size + 1);

            Array.Resize(ref m_Array, size);
        }

    }

    internal struct CharacterSubstitution
    {
        public int index;
        public uint unicode;

        public CharacterSubstitution(int index, uint unicode)
        {
            this.index = index;
            this.unicode = unicode;
        }
    }

    /// <summary>
    ///
    /// </summary>
    internal struct Offset
    {
        public float left { get { return m_Left; } set { m_Left = value; } }

        public float right { get { return m_Right; } set { m_Right = value; } }

        public float top { get { return m_Top; } set { m_Top = value; } }

        public float bottom { get { return m_Bottom; } set { m_Bottom = value; } }

        public float horizontal { get { return m_Left; } set { m_Left = value; m_Right = value; } }

        public float vertical { get { return m_Top; } set { m_Top = value; m_Bottom = value; } }

        /// <summary>
        ///
        /// </summary>
        public static Offset zero { get { return k_ZeroOffset; } }

        // =============================================
        // Private backing fields for public properties.
        // =============================================

        float m_Left;
        float m_Right;
        float m_Top;
        float m_Bottom;

        static readonly Offset k_ZeroOffset = new Offset(0F, 0F, 0F, 0F);

        /// <summary>
        ///
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="top"></param>
        /// <param name="bottom"></param>
        public Offset(float left, float right, float top, float bottom)
        {
            m_Left = left;
            m_Right = right;
            m_Top = top;
            m_Bottom = bottom;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="horizontal"></param>
        /// <param name="vertical"></param>
        public Offset(float horizontal, float vertical)
        {
            m_Left = horizontal;
            m_Right = horizontal;
            m_Top = vertical;
            m_Bottom = vertical;
        }

        public static bool operator ==(Offset lhs, Offset rhs)
        {
            return lhs.m_Left == rhs.m_Left &&
                    lhs.m_Right == rhs.m_Right &&
                    lhs.m_Top == rhs.m_Top &&
                    lhs.m_Bottom == rhs.m_Bottom;
        }

        public static bool operator !=(Offset lhs, Offset rhs)
        {
            return !(lhs == rhs);
        }

        public static Offset operator *(Offset a, float b)
        {
            return new Offset(a.m_Left * b, a.m_Right * b, a.m_Top * b, a.m_Bottom * b);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public bool Equals(Offset other)
        {
            return base.Equals(other);
        }
    }


    /// <summary>
    ///
    /// </summary>
    internal struct HighlightState
    {
        public Color32 color;
        public Offset padding;

        public HighlightState(Color32 color, Offset padding)
        {
            this.color = color;
            this.padding = padding;
        }

        public static bool operator ==(HighlightState lhs, HighlightState rhs)
        {
            return lhs.color.r == rhs.color.r &&
                lhs.color.g == rhs.color.g &&
                lhs.color.b == rhs.color.b &&
                lhs.color.a == rhs.color.a &&
                lhs.padding == rhs.padding;
        }

        public static bool operator !=(HighlightState lhs, HighlightState rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public bool Equals(HighlightState other)
        {
            return base.Equals(other);
        }
    }

    // Structure used for Word Wrapping which tracks the state of execution when the last space or carriage return character was encountered.
    struct WordWrapState
    {
        public int previousWordBreak;
        public int totalCharacterCount;
        public int visibleCharacterCount;
        public int visibleSpaceCount;
        public int visibleSpriteCount;
        public int visibleLinkCount;
        public int firstCharacterIndex;
        public int firstVisibleCharacterIndex;
        public int lastCharacterIndex;
        public int lastVisibleCharIndex;
        public int lineNumber;

        public float maxCapHeight;
        public float maxAscender;
        public float maxDescender;
        public float maxLineAscender;
        public float maxLineDescender;
        public float startOfLineAscender;

        public float xAdvance;
        public float preferredWidth;
        public float preferredHeight;
        public float previousLineScale;
        public float pageAscender;

        public int wordCount;
        public FontStyles fontStyle;
        public float fontScale;
        public float fontScaleMultiplier;

        public int italicAngle;
        public float currentFontSize;
        public float baselineOffset;
        public float lineOffset;

        public TextInfo textInfo;
        public LineInfo lineInfo;

        public Color32 vertexColor;
        public Color32 underlineColor;
        public Color32 strikethroughColor;
        public Color32 highlightColor;
        public HighlightState highlightState;
        public FontStyleStack basicStyleStack;
        public TextProcessingStack<int> italicAngleStack;
        public TextProcessingStack<Color32> colorStack;
        public TextProcessingStack<Color32> underlineColorStack;
        public TextProcessingStack<Color32> strikethroughColorStack;
        public TextProcessingStack<Color32> highlightColorStack;
        public TextProcessingStack<HighlightState> highlightStateStack;
        public TextProcessingStack<TextColorGradient> colorGradientStack;
        public TextProcessingStack<float> sizeStack;
        public TextProcessingStack<float> indentStack;
        public TextProcessingStack<TextFontWeight> fontWeightStack;
        public TextProcessingStack<int> styleStack;
        public TextProcessingStack<float> baselineStack;
        public TextProcessingStack<int> actionStack;
        public TextProcessingStack<MaterialReference> materialReferenceStack;
        public TextProcessingStack<TextAlignment> lineJustificationStack;

        public int lastBaseGlyphIndex;
        public int spriteAnimationId;

        public FontAsset currentFontAsset;
        public SpriteAsset currentSpriteAsset;
        public Material currentMaterial;
        public int currentMaterialIndex;

        public Extents meshExtents;

        public bool tagNoParsing;
        public bool isNonBreakingSpace;
        public bool isDrivenLineSpacing;
        public Vector3 fxScale;
        public Quaternion fxRotation;
    }

    internal static class TextGeneratorUtilities
    {
        public static readonly Vector2 largePositiveVector2 = new Vector2(2147483647, 2147483647);
        public static readonly Vector2 largeNegativeVector2 = new Vector2(-214748364, -214748364);
        public const float largePositiveFloat = 32767;
        public const float largeNegativeFloat = -32767;
        const int
           k_DoubleQuotes = 34,
           k_GreaterThan = 62,
           k_ZeroWidthSpace = 0x200B;

        /// <summary>
        /// Table used to convert character to uppercase.
        /// </summary>
        const string k_LookupStringU = "-------------------------------- !-#$%&-()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[-]^_`ABCDEFGHIJKLMNOPQRSTUVWXYZ{|}~-";

        public static bool Approximately(float a, float b)
        {
            return (b - 0.0001f) < a && a < (b + 0.0001f);
        }

        /// <summary>
        /// Method to convert Hex color values to Color32
        /// </summary>
        /// <param name="hexChars"></param>
        /// <param name="tagCount"></param>
        /// <returns></returns>
        public static Color32 HexCharsToColor(char[] hexChars, int tagCount)
        {
            if (tagCount == 4)
            {
                byte r = (byte)(HexToInt(hexChars[1]) * 16 + HexToInt(hexChars[1]));
                byte g = (byte)(HexToInt(hexChars[2]) * 16 + HexToInt(hexChars[2]));
                byte b = (byte)(HexToInt(hexChars[3]) * 16 + HexToInt(hexChars[3]));

                return new Color32(r, g, b, 255);
            }
            if (tagCount == 5)
            {
                byte r = (byte)(HexToInt(hexChars[1]) * 16 + HexToInt(hexChars[1]));
                byte g = (byte)(HexToInt(hexChars[2]) * 16 + HexToInt(hexChars[2]));
                byte b = (byte)(HexToInt(hexChars[3]) * 16 + HexToInt(hexChars[3]));
                byte a = (byte)(HexToInt(hexChars[4]) * 16 + HexToInt(hexChars[4]));

                return new Color32(r, g, b, a);
            }
            if (tagCount == 7)
            {
                byte r = (byte)(HexToInt(hexChars[1]) * 16 + HexToInt(hexChars[2]));
                byte g = (byte)(HexToInt(hexChars[3]) * 16 + HexToInt(hexChars[4]));
                byte b = (byte)(HexToInt(hexChars[5]) * 16 + HexToInt(hexChars[6]));

                return new Color32(r, g, b, 255);
            }
            if (tagCount == 9)
            {
                byte r = (byte)(HexToInt(hexChars[1]) * 16 + HexToInt(hexChars[2]));
                byte g = (byte)(HexToInt(hexChars[3]) * 16 + HexToInt(hexChars[4]));
                byte b = (byte)(HexToInt(hexChars[5]) * 16 + HexToInt(hexChars[6]));
                byte a = (byte)(HexToInt(hexChars[7]) * 16 + HexToInt(hexChars[8]));

                return new Color32(r, g, b, a);
            }
            if (tagCount == 10)
            {
                byte r = (byte)(HexToInt(hexChars[7]) * 16 + HexToInt(hexChars[7]));
                byte g = (byte)(HexToInt(hexChars[8]) * 16 + HexToInt(hexChars[8]));
                byte b = (byte)(HexToInt(hexChars[9]) * 16 + HexToInt(hexChars[9]));

                return new Color32(r, g, b, 255);
            }
            if (tagCount == 11)
            {
                byte r = (byte)(HexToInt(hexChars[7]) * 16 + HexToInt(hexChars[7]));
                byte g = (byte)(HexToInt(hexChars[8]) * 16 + HexToInt(hexChars[8]));
                byte b = (byte)(HexToInt(hexChars[9]) * 16 + HexToInt(hexChars[9]));
                byte a = (byte)(HexToInt(hexChars[10]) * 16 + HexToInt(hexChars[10]));

                return new Color32(r, g, b, a);
            }
            if (tagCount == 13)
            {
                byte r = (byte)(HexToInt(hexChars[7]) * 16 + HexToInt(hexChars[8]));
                byte g = (byte)(HexToInt(hexChars[9]) * 16 + HexToInt(hexChars[10]));
                byte b = (byte)(HexToInt(hexChars[11]) * 16 + HexToInt(hexChars[12]));

                return new Color32(r, g, b, 255);
            }
            if (tagCount == 15)
            {
                byte r = (byte)(HexToInt(hexChars[7]) * 16 + HexToInt(hexChars[8]));
                byte g = (byte)(HexToInt(hexChars[9]) * 16 + HexToInt(hexChars[10]));
                byte b = (byte)(HexToInt(hexChars[11]) * 16 + HexToInt(hexChars[12]));
                byte a = (byte)(HexToInt(hexChars[13]) * 16 + HexToInt(hexChars[14]));

                return new Color32(r, g, b, a);
            }

            return new Color32(255, 255, 255, 255);
        }

        /// <summary>
        /// Method to convert Hex Color values to Color32
        /// </summary>
        /// <param name="hexChars"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static Color32 HexCharsToColor(char[] hexChars, int startIndex, int length)
        {
            if (length == 7)
            {
                byte r = (byte)(HexToInt(hexChars[startIndex + 1]) * 16 + HexToInt(hexChars[startIndex + 2]));
                byte g = (byte)(HexToInt(hexChars[startIndex + 3]) * 16 + HexToInt(hexChars[startIndex + 4]));
                byte b = (byte)(HexToInt(hexChars[startIndex + 5]) * 16 + HexToInt(hexChars[startIndex + 6]));

                return new Color32(r, g, b, 255);
            }
            if (length == 9)
            {
                byte r = (byte)(HexToInt(hexChars[startIndex + 1]) * 16 + HexToInt(hexChars[startIndex + 2]));
                byte g = (byte)(HexToInt(hexChars[startIndex + 3]) * 16 + HexToInt(hexChars[startIndex + 4]));
                byte b = (byte)(HexToInt(hexChars[startIndex + 5]) * 16 + HexToInt(hexChars[startIndex + 6]));
                byte a = (byte)(HexToInt(hexChars[startIndex + 7]) * 16 + HexToInt(hexChars[startIndex + 8]));

                return new Color32(r, g, b, a);
            }

            return new Color32(255, 255, 255, 255);
        }

        /// <summary>
        /// Method to convert Hex to Int
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static int HexToInt(char hex)
        {
            switch (hex)
            {
                case '0':
                    return 0;
                case '1':
                    return 1;
                case '2':
                    return 2;
                case '3':
                    return 3;
                case '4':
                    return 4;
                case '5':
                    return 5;
                case '6':
                    return 6;
                case '7':
                    return 7;
                case '8':
                    return 8;
                case '9':
                    return 9;
                case 'A':
                    return 10;
                case 'B':
                    return 11;
                case 'C':
                    return 12;
                case 'D':
                    return 13;
                case 'E':
                    return 14;
                case 'F':
                    return 15;
                case 'a':
                    return 10;
                case 'b':
                    return 11;
                case 'c':
                    return 12;
                case 'd':
                    return 13;
                case 'e':
                    return 14;
                case 'f':
                    return 15;
            }
            return 15;
        }

        /// <summary>
        /// Extracts a float value from char[] assuming we know the position of the start, end and decimal point.
        /// </summary>
        /// <param name="chars"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static float ConvertToFloat(char[] chars, int startIndex, int length)
        {
            int lastIndex;
            return ConvertToFloat(chars, startIndex, length, out lastIndex);
        }

        /// <summary>
        /// Extracts a float value from char[] given a start index and length.
        /// </summary>
        /// <param name="chars"></param> The Char[] containing the numerical sequence.
        /// <param name="startIndex"></param> The index of the start of the numerical sequence.
        /// <param name="length"></param> The length of the numerical sequence.
        /// <param name="lastIndex"></param> Index of the last character in the validated sequence.
        /// <returns></returns>
        public static float ConvertToFloat(char[] chars, int startIndex, int length, out int lastIndex)
        {
            if (startIndex == 0)
            {
                lastIndex = 0;
                return largeNegativeFloat;
            }
            int endIndex = startIndex + length;

            bool isIntegerValue = true;
            float decimalPointMultiplier = 0;

            // Set value multiplier checking the first character to determine if we are using '+' or '-'
            int valueSignMultiplier = 1;
            if (chars[startIndex] == '+')
            {
                valueSignMultiplier = 1;
                startIndex += 1;
            }
            else if (chars[startIndex] == '-')
            {
                valueSignMultiplier = -1;
                startIndex += 1;
            }

            float value = 0;

            for (int i = startIndex; i < endIndex; i++)
            {
                uint c = chars[i];

                if (c >= '0' && c <= '9' || c == '.')
                {
                    if (c == '.')
                    {
                        isIntegerValue = false;
                        decimalPointMultiplier = 0.1f;
                        continue;
                    }

                    //Calculate integer and floating point value
                    if (isIntegerValue)
                        value = value * 10 + (c - 48) * valueSignMultiplier;
                    else
                    {
                        value = value + (c - 48) * decimalPointMultiplier * valueSignMultiplier;
                        decimalPointMultiplier *= 0.1f;
                    }
                }
                else if (c == ',')
                {
                    if (i + 1 < endIndex && chars[i + 1] == ' ')
                        lastIndex = i + 1;
                    else
                        lastIndex = i;

                    return value;
                }
            }

            lastIndex = endIndex;
            return value;
        }

        /// <summary>
        /// Function to pack scale information in the UV2 Channel.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static Vector2 PackUV(float x, float y, float scale)
        {
            Vector2 output;

            output.x = (int)(x * 511);
            output.y = (int)(y * 511);

            output.x = (output.x * 4096) + output.y;
            output.y = scale;

            return output;
        }

        /// <summary>
        /// Method to store the content of a string into an integer array.
        /// </summary>
        /// <param name="sourceText"></param>
        /// <param name="charBuffer"></param>
        /// <param name="styleStack"></param>
        /// <param name="generationSettings"></param>
        public static void StringToCharArray(string sourceText, ref int[] charBuffer, ref TextProcessingStack<int> styleStack, TextGenerationSettings generationSettings)
        {
            if (sourceText == null)
            {
                charBuffer[0] = 0;
                return;
            }

            if (charBuffer == null)
                charBuffer = new int[8];

            // Clear the Style stack.
            styleStack.SetDefault(0);

            int writeIndex = 0;

            for (int i = 0; i < sourceText.Length; i++)
            {
                if (sourceText[i] == 92 && sourceText.Length > i + 1)
                {
                    switch ((int)sourceText[i + 1])
                    {
                        case 85: // \U00000000 for UTF-32 Unicode
                            if (sourceText.Length > i + 9)
                            {
                                if (writeIndex == charBuffer.Length)
                                    ResizeInternalArray(ref charBuffer);

                                charBuffer[writeIndex] = GetUtf32(sourceText, i + 2);
                                i += 9;
                                writeIndex += 1;
                                continue;
                            }
                            break;
                        case 92: // \ escape
                            if (!generationSettings.parseControlCharacters)
                                break;

                            if (sourceText.Length <= i + 2)
                                break;

                            if (writeIndex + 2 > charBuffer.Length)
                                ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex] = sourceText[i + 1];
                            charBuffer[writeIndex + 1] = sourceText[i + 2];
                            i += 2;
                            writeIndex += 2;
                            continue;
                        case 110: // \n LineFeed
                            if (!generationSettings.parseControlCharacters)
                                break;

                            if (writeIndex == charBuffer.Length)
                                ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex] = (char)10;
                            i += 1;
                            writeIndex += 1;
                            continue;
                        case 114: // \r
                            if (!generationSettings.parseControlCharacters)
                                break;

                            if (writeIndex == charBuffer.Length)
                                ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex] = (char)13;
                            i += 1;
                            writeIndex += 1;
                            continue;
                        case 116: // \t Tab
                            if (!generationSettings.parseControlCharacters)
                                break;

                            if (writeIndex == charBuffer.Length)
                                ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex] = (char)9;
                            i += 1;
                            writeIndex += 1;
                            continue;
                        case 117: // \u0000 for UTF-16 Unicode
                            if (sourceText.Length > i + 5)
                            {
                                if (writeIndex == charBuffer.Length)
                                    ResizeInternalArray(ref charBuffer);

                                charBuffer[writeIndex] = (char)GetUtf16(sourceText, i + 2);
                                i += 5;
                                writeIndex += 1;
                                continue;
                            }
                            break;
                    }
                }

                // Handle UTF-32 in the input text (string). // Not sure this is needed //
                if (Char.IsHighSurrogate(sourceText[i]) && Char.IsLowSurrogate(sourceText[i + 1]))
                {
                    if (writeIndex == charBuffer.Length)
                        ResizeInternalArray(ref charBuffer);

                    charBuffer[writeIndex] = Char.ConvertToUtf32(sourceText[i], sourceText[i + 1]);
                    i += 1;
                    writeIndex += 1;
                    continue;
                }

                //// Handle inline replacement of <stlye> and <br> tags.
                if (sourceText[i] == 60 && generationSettings.richText)
                {
                    if (IsTagName(ref sourceText, "<BR>", i))
                    {
                        if (writeIndex == charBuffer.Length)
                            ResizeInternalArray(ref charBuffer);

                        charBuffer[writeIndex] = 10;
                        writeIndex += 1;
                        i += 3;

                        continue;
                    }
                    if (IsTagName(ref sourceText, "<STYLE=", i))
                    {
                        int srcOffset;
                        if (ReplaceOpeningStyleTag(ref sourceText, i, out srcOffset, ref charBuffer, ref writeIndex, ref styleStack, ref generationSettings))
                        {
                            i = srcOffset;
                            continue;
                        }
                    }
                    else if (IsTagName(ref sourceText, "</STYLE>", i))
                    {
                        ReplaceClosingStyleTag(ref charBuffer, ref writeIndex, ref styleStack, ref generationSettings);

                        // Strip </style> even if style is invalid.
                        i += 7;
                        continue;
                    }
                }

                if (writeIndex == charBuffer.Length)
                    ResizeInternalArray(ref charBuffer);

                charBuffer[writeIndex] = sourceText[i];
                writeIndex += 1;
            }

            if (writeIndex == charBuffer.Length)
                ResizeInternalArray(ref charBuffer);

            charBuffer[writeIndex] = (char)0;
        }

        public static void ResizeInternalArray<T>(ref T[] array)
        {
            int size = Mathf.NextPowerOfTwo(array.Length + 1);

            Array.Resize(ref array, size);
        }

        public static void ResizeInternalArray<T>(ref T[] array, int size)
        {
            size = Mathf.NextPowerOfTwo(size + 1);

            Array.Resize(ref array, size);
        }

        /// <summary>
        /// Method to check for a matching rich text tag.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="tag"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        static bool IsTagName(ref string text, string tag, int index)
        {
            if (text.Length < index + tag.Length)
                return false;

            for (int i = 0; i < tag.Length; i++)
            {
                if (TextUtilities.ToUpperFast(text[index + i]) != tag[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Method to check for a matching rich text tag.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="tag"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        static bool IsTagName(ref int[] text, string tag, int index)
        {
            if (text.Length < index + tag.Length)
                return false;

            for (int i = 0; i < tag.Length; i++)
            {
                if (TextUtilities.ToUpperFast((char)text[index + i]) != tag[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Method to handle inline replacement of style tag by opening style definition.
        /// </summary>
        /// <param name="sourceText"></param>
        /// <param name="srcIndex"></param>
        /// <param name="srcOffset"></param>s
        /// <param name="charBuffer"></param>
        /// <param name="writeIndex"></param>
        /// <param name="styleStack"></param>
        /// <returns></returns>
        public static bool ReplaceOpeningStyleTag(ref int[] sourceText, int srcIndex, out int srcOffset, ref int[] charBuffer, ref int writeIndex, ref TextProcessingStack<int> styleStack, ref TextGenerationSettings generationSettings)
        {
            // Validate <style> tag.
            int hashCode = GetTagHashCode(ref sourceText, srcIndex + 7, out srcOffset);

            TextStyle style = GetStyle(generationSettings, hashCode);

            // Return if we don't have a valid style.
            if (style == null || srcOffset == 0)
                return false;

            styleStack.Add(style.hashCode);

            int styleLength = style.styleOpeningTagArray.Length;

            // Replace <style> tag with opening definition
            int[] openingTagArray = style.styleOpeningTagArray;

            for (int i = 0; i < styleLength; i++)
            {
                int c = openingTagArray[i];

                if (c == 60)
                {
                    if (IsTagName(ref openingTagArray, "<BR>", i))
                    {
                        if (writeIndex == charBuffer.Length)
                            ResizeInternalArray(ref charBuffer);

                        charBuffer[writeIndex] = 10;
                        writeIndex += 1;
                        i += 3;

                        continue;
                    }
                    if (IsTagName(ref openingTagArray, "<STYLE=", i))
                    {
                        int offset;
                        if (ReplaceOpeningStyleTag(ref openingTagArray, i, out offset, ref charBuffer, ref writeIndex, ref styleStack, ref generationSettings))
                        {
                            i = offset;
                            continue;
                        }
                    }
                    else if (IsTagName(ref openingTagArray, "</STYLE>", i))
                    {
                        ReplaceClosingStyleTag(ref charBuffer, ref writeIndex, ref styleStack, ref generationSettings);

                        // Strip </style> even if style is invalid.
                        i += 7;
                        continue;
                    }
                }

                if (writeIndex == charBuffer.Length)
                    ResizeInternalArray(ref charBuffer);

                charBuffer[writeIndex] = c;
                writeIndex += 1;
            }

            return true;
        }

        /// <summary>
        /// Method to handle inline replacement of style tag by opening style definition.
        /// </summary>
        /// <param name="sourceText"></param>
        /// <param name="srcIndex"></param>
        /// <param name="srcOffset"></param>
        /// <param name="charBuffer"></param>
        /// <param name="writeIndex"></param>
        /// <param name="styleStack"></param>
        /// <returns></returns>
        static bool ReplaceOpeningStyleTag(ref string sourceText, int srcIndex, out int srcOffset, ref int[] charBuffer, ref int writeIndex, ref TextProcessingStack<int> styleStack, ref TextGenerationSettings generationSettings)
        {
            // Validate <style> tag.
            int hashCode = GetTagHashCode(ref sourceText, srcIndex + 7, out srcOffset);

            TextStyle style = GetStyle(generationSettings, hashCode);

            // Return if we don't have a valid style.
            if (style == null || srcOffset == 0)
                return false;

            styleStack.Add(style.hashCode);

            int styleLength = style.styleOpeningTagArray.Length;

            // Replace <style> tag with opening definition
            int[] openingTagArray = style.styleOpeningTagArray;

            for (int i = 0; i < styleLength; i++)
            {
                int c = openingTagArray[i];

                if (c == 60)
                {
                    if (IsTagName(ref openingTagArray, "<BR>", i))
                    {
                        if (writeIndex == charBuffer.Length)
                            ResizeInternalArray(ref charBuffer);

                        charBuffer[writeIndex] = 10;
                        writeIndex += 1;
                        i += 3;

                        continue;
                    }
                    if (IsTagName(ref openingTagArray, "<STYLE=", i))
                    {
                        int offset;
                        if (ReplaceOpeningStyleTag(ref openingTagArray, i, out offset, ref charBuffer, ref writeIndex, ref styleStack, ref generationSettings))
                        {
                            i = offset;
                            continue;
                        }
                    }
                    else if (IsTagName(ref openingTagArray, "</STYLE>", i))
                    {
                        ReplaceClosingStyleTag(ref charBuffer, ref writeIndex, ref styleStack, ref generationSettings);

                        // Strip </style> even if style is invalid.
                        i += 7;
                        continue;
                    }
                }

                if (writeIndex == charBuffer.Length)
                    ResizeInternalArray(ref charBuffer);

                charBuffer[writeIndex] = c;
                writeIndex += 1;
            }

            return true;
        }

        /// <summary>
        /// Method to handle inline replacement of style tag by closing style definition.
        /// </summary>
        /// <param name="charBuffer"></param>
        /// <param name="writeIndex"></param>
        /// <param name="styleStack"></param>
        /// <returns></returns>
        public static void ReplaceClosingStyleTag(ref int[] charBuffer, ref int writeIndex, ref TextProcessingStack<int> styleStack, ref TextGenerationSettings generationSettings)
        {
            // Get style from the Style Stack
            int hashCode = styleStack.CurrentItem();
            TextStyle style = GetStyle(generationSettings, hashCode);

            styleStack.Remove();

            // Return if we don't have a valid style.
            if (style == null)
                return;

            int styleLength = style.styleClosingTagArray.Length;

            // Replace <style> tag with opening definition
            int[] closingTagArray = style.styleClosingTagArray;

            for (int i = 0; i < styleLength; i++)
            {
                int c = closingTagArray[i];

                if (c == 60)
                {
                    if (IsTagName(ref closingTagArray, "<BR>", i))
                    {
                        if (writeIndex == charBuffer.Length)
                            ResizeInternalArray(ref charBuffer);

                        charBuffer[writeIndex] = 10;
                        writeIndex += 1;
                        i += 3;

                        continue;
                    }
                    if (IsTagName(ref closingTagArray, "<STYLE=", i))
                    {
                        int offset;
                        if (ReplaceOpeningStyleTag(ref closingTagArray, i, out offset, ref charBuffer, ref writeIndex, ref styleStack, ref generationSettings))
                        {
                            i = offset;
                            continue;
                        }
                    }
                    else if (IsTagName(ref closingTagArray, "</STYLE>", i))
                    {
                        ReplaceClosingStyleTag(ref charBuffer, ref writeIndex, ref styleStack, ref generationSettings);

                        // Strip </style> even if style is invalid.
                        i += 7;
                        continue;
                    }
                }

                if (writeIndex == charBuffer.Length)
                    ResizeInternalArray(ref charBuffer);

                charBuffer[writeIndex] = c;
                writeIndex += 1;
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="style"></param>
        /// <param name="srcIndex"></param>
        /// <param name="charBuffer"></param>
        /// <param name="writeIndex"></param>
        /// <returns></returns>
        public static bool InsertOpeningStyleTag(TextStyle style, int srcIndex, ref UnicodeChar[] charBuffer, ref int writeIndex, ref int textStyleStackDepth, ref TextProcessingStack<int>[] textStyleStacks, ref TextGenerationSettings generationSettings)
        {
            // Return if we don't have a valid style.
            if (style == null) return false;

            textStyleStacks[0].Push(style.hashCode);

            int styleLength = style.styleOpeningTagArray.Length;

            // Replace <style> tag with opening definition
            int[] tagDefinition = style.styleOpeningTagArray;

            for (int i = 0; i < styleLength; i++)
            {
                int c = tagDefinition[i];

                if (c == '\\' && i + 1 < styleLength)
                {
                    switch (tagDefinition[i + 1])
                    {
                        case '\\':
                            i += 1;
                            break;
                        case 'n':
                            c = 10;
                            i += 1;
                            break;
                        case 'r':
                            break;
                        case 't':
                            break;
                        case 'u':
                            // UTF16 format is "\uFF00" or u + 2 hex pairs.
                            if (i + 5 < styleLength)
                            {
                                c = GetUTF16(tagDefinition, i + 2);

                                i += 5;
                            }
                            break;
                        case 'U':
                            // UTF32 format is "\UFF00FF00" or U + 4 hex pairs.
                            if (i + 9 < styleLength)
                            {
                                c = GetUTF32(tagDefinition, i + 2);

                                i += 9;
                            }
                            break;
                    }
                }

                if (c == '<')
                {
                    int hashCode = GetMarkupTagHashCode(tagDefinition, i + 1);

                    switch ((MarkupTag)hashCode)
                    {
                        case MarkupTag.BR:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 10;
                            writeIndex += 1;
                            i += 3;
                            continue;
                        case MarkupTag.CR:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 13;
                            writeIndex += 1;
                            i += 3;
                            continue;
                        case MarkupTag.NBSP:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 160;
                            writeIndex += 1;
                            i += 5;
                            continue;
                        case MarkupTag.ZWSP:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = k_ZeroWidthSpace;
                            writeIndex += 1;
                            i += 5;
                            continue;
                        case MarkupTag.SHY:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 0xAD;
                            writeIndex += 1;
                            i += 4;
                            continue;
                        case MarkupTag.STYLE:
                            if (ReplaceOpeningStyleTag(ref tagDefinition, i, out int offset, ref charBuffer, ref writeIndex, ref textStyleStackDepth, ref textStyleStacks, ref generationSettings))
                            {
                                i = offset;
                                continue;
                            }
                            break;
                        case MarkupTag.SLASH_STYLE:
                            ReplaceClosingStyleTag(ref tagDefinition, i, ref charBuffer, ref writeIndex, ref textStyleStackDepth, ref textStyleStacks, ref generationSettings);

                            i += 7;
                            continue;
                    }
                }

                if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                charBuffer[writeIndex].unicode = c;
                writeIndex += 1;
            }

            textStyleStackDepth = 0;

            return true;
        }

        /// <summary>
        /// Method to handle inline replacement of style tag by opening style definition.
        /// </summary>
        /// <param name="sourceText"></param>
        /// <param name="srcIndex"></param>
        /// <param name="srcOffset"></param>
        /// <param name="charBuffer"></param>
        /// <param name="writeIndex"></param>
        /// <returns></returns>
        public static bool ReplaceOpeningStyleTag(ref TextBackingContainer sourceText, int srcIndex, out int srcOffset, ref UnicodeChar[] charBuffer, ref int writeIndex, ref int textStyleStackDepth, ref TextProcessingStack<int>[] textStyleStacks, ref TextGenerationSettings generationSettings)
        {
            // Validate <style> tag.
            int styleHashCode = GetStyleHashCode(ref sourceText, srcIndex + 7, out srcOffset);
            TextStyle style = GetStyle(generationSettings, styleHashCode);

            // Return if we don't have a valid style.
            if (style == null || srcOffset == 0) return false;

            // Increase style depth
            textStyleStackDepth += 1;

            // Push style hashcode onto stack
            textStyleStacks[textStyleStackDepth].Push(style.hashCode);

            int styleLength = style.styleOpeningTagArray.Length;

            // Replace <style> tag with opening definition
            int[] tagDefinition = style.styleOpeningTagArray;

            for (int i = 0; i < styleLength; i++)
            {
                int c = tagDefinition[i];

                if (c == '\\' && i + 1 < styleLength)
                {
                    switch (tagDefinition[i + 1])
                    {
                        case '\\':
                            i += 1;
                            break;
                        case 'n':
                            c = 10;
                            i += 1;
                            break;
                        case 'r':
                            break;
                        case 't':
                            break;
                        case 'u':
                            // UTF16 format is "\uFF00" or u + 2 hex pairs.
                            if (i + 5 < styleLength)
                            {
                                c = GetUTF16(tagDefinition, i + 2);

                                i += 5;
                            }
                            break;
                        case 'U':
                            // UTF32 format is "\UFF00FF00" or U + 4 hex pairs.
                            if (i + 9 < styleLength)
                            {
                                c = GetUTF32(tagDefinition, i + 2);

                                i += 9;
                            }
                            break;
                    }
                }

                if (c == '<')
                {
                    int hashCode = GetMarkupTagHashCode(tagDefinition, i + 1);

                    switch ((MarkupTag)hashCode)
                    {
                        case MarkupTag.BR:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 10;
                            writeIndex += 1;
                            i += 3;
                            continue;
                        case MarkupTag.CR:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 13;
                            writeIndex += 1;
                            i += 3;
                            continue;
                        case MarkupTag.NBSP:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 160;
                            writeIndex += 1;
                            i += 5;
                            continue;
                        case MarkupTag.ZWSP:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = k_ZeroWidthSpace;
                            writeIndex += 1;
                            i += 5;
                            continue;
                        case MarkupTag.SHY:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 0xAD;
                            writeIndex += 1;
                            i += 4;
                            continue;
                        case MarkupTag.STYLE:
                            if (ReplaceOpeningStyleTag(ref tagDefinition, i, out int offset, ref charBuffer, ref writeIndex, ref textStyleStackDepth, ref textStyleStacks, ref generationSettings))
                            {
                                i = offset;
                                continue;
                            }
                            break;
                        case MarkupTag.SLASH_STYLE:
                            ReplaceClosingStyleTag(ref tagDefinition, i, ref charBuffer, ref writeIndex, ref textStyleStackDepth, ref textStyleStacks, ref generationSettings);

                            i += 7;
                            continue;
                    }
                }

                if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                charBuffer[writeIndex].unicode = c;
                writeIndex += 1;
            }

            textStyleStackDepth -= 1;

            return true;
        }

        /// <summary>
        /// Method to handle inline replacement of style tag by closing style definition.
        /// </summary>
        /// <param name="sourceText"></param>
        /// <param name="srcIndex"></param>
        /// <param name="charBuffer"></param>
        /// <param name="writeIndex"></param>
        /// <returns></returns>
        public static void ReplaceClosingStyleTag(ref int[] sourceText, int srcIndex, ref UnicodeChar[] charBuffer, ref int writeIndex, ref int textStyleStackDepth, ref TextProcessingStack<int>[] textStyleStacks, ref TextGenerationSettings generationSettings)
        {
            // Get style from the Style Stack
            int styleHashCode = textStyleStacks[textStyleStackDepth + 1].Pop();
            TextStyle style = GetStyle(generationSettings, styleHashCode);

            // Return if we don't have a valid style.
            if (style == null) return;

            // Increase style depth
            textStyleStackDepth += 1;

            int styleLength = style.styleClosingTagArray.Length;

            // Replace <style> tag with opening definition
            int[] tagDefinition = style.styleClosingTagArray;

            for (int i = 0; i < styleLength; i++)
            {
                int c = tagDefinition[i];

                if (c == '\\' && i + 1 < styleLength)
                {
                    switch (tagDefinition[i + 1])
                    {
                        case '\\':
                            i += 1;
                            break;
                        case 'n':
                            c = 10;
                            i += 1;
                            break;
                        case 'r':
                            break;
                        case 't':
                            break;
                        case 'u':
                            // UTF16 format is "\uFF00" or u + 2 hex pairs.
                            if (i + 5 < styleLength)
                            {
                                c = GetUTF16(tagDefinition, i + 2);

                                i += 5;
                            }
                            break;
                        case 'U':
                            // UTF32 format is "\UFF00FF00" or U + 4 hex pairs.
                            if (i + 9 < styleLength)
                            {
                                c = GetUTF32(tagDefinition, i + 2);

                                i += 9;
                            }
                            break;
                    }
                }

                if (c == '<')
                {
                    int hashCode = GetMarkupTagHashCode(tagDefinition, i + 1);

                    switch ((MarkupTag)hashCode)
                    {
                        case MarkupTag.BR:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 10;
                            writeIndex += 1;
                            i += 3;
                            continue;
                        case MarkupTag.CR:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 13;
                            writeIndex += 1;
                            i += 3;
                            continue;
                        case MarkupTag.NBSP:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 160;
                            writeIndex += 1;
                            i += 5;
                            continue;
                        case MarkupTag.ZWSP:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = k_ZeroWidthSpace;
                            writeIndex += 1;
                            i += 5;
                            continue;
                        case MarkupTag.SHY:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 0xAD;
                            writeIndex += 1;
                            i += 4;
                            continue;
                        case MarkupTag.STYLE:
                            if (ReplaceOpeningStyleTag(ref tagDefinition, i, out int offset, ref charBuffer, ref writeIndex, ref textStyleStackDepth, ref textStyleStacks, ref generationSettings))
                            {
                                i = offset;
                                continue;
                            }
                            break;
                        case MarkupTag.SLASH_STYLE:
                            ReplaceClosingStyleTag(ref tagDefinition, i, ref charBuffer, ref writeIndex, ref textStyleStackDepth, ref textStyleStacks, ref generationSettings);

                            i += 7;
                            continue;
                    }
                }

                if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                charBuffer[writeIndex].unicode = c;
                writeIndex += 1;
            }

            textStyleStackDepth -= 1;
        }

        /// <summary>
        /// Method to handle inline replacement of style tag by closing style definition.
        /// </summary>
        /// <param name="sourceText"></param>
        /// <param name="srcIndex"></param>
        /// <param name="charBuffer"></param>
        /// <param name="writeIndex"></param>
        /// <returns></returns>
        public static void ReplaceClosingStyleTag(ref TextBackingContainer sourceText, int srcIndex, ref UnicodeChar[] charBuffer, ref int writeIndex, ref int textStyleStackDepth, ref TextProcessingStack<int>[] textStyleStacks, ref TextGenerationSettings generationSettings)
        {
            // Get style from the Style Stack
            int styleHashCode = textStyleStacks[textStyleStackDepth + 1].Pop();
            TextStyle style = GetStyle(generationSettings, styleHashCode);

            // Return if we don't have a valid style.
            if (style == null) return;

            // Increase style depth
            textStyleStackDepth += 1;

            int styleLength = style.styleClosingTagArray.Length;

            // Replace <style> tag with opening definition
            int[] tagDefinition = style.styleClosingTagArray;

            for (int i = 0; i < styleLength; i++)
            {
                int c = tagDefinition[i];

                if (c == '\\' && i + 1 < styleLength)
                {
                    switch (tagDefinition[i + 1])
                    {
                        case '\\':
                            i += 1;
                            break;
                        case 'n':
                            c = 10;
                            i += 1;
                            break;
                        case 'r':
                            break;
                        case 't':
                            break;
                        case 'u':
                            // UTF16 format is "\uFF00" or u + 2 hex pairs.
                            if (i + 5 < styleLength)
                            {
                                c = GetUTF16(tagDefinition, i + 2);

                                i += 5;
                            }
                            break;
                        case 'U':
                            // UTF32 format is "\UFF00FF00" or U + 4 hex pairs.
                            if (i + 9 < styleLength)
                            {
                                c = GetUTF32(tagDefinition, i + 2);

                                i += 9;
                            }
                            break;
                    }
                }

                if (c == '<')
                {
                    int hashCode = GetMarkupTagHashCode(tagDefinition, i + 1);

                    switch ((MarkupTag)hashCode)
                    {
                        case MarkupTag.BR:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 10;
                            writeIndex += 1;
                            i += 3;
                            continue;
                        case MarkupTag.CR:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 13;
                            writeIndex += 1;
                            i += 3;
                            continue;
                        case MarkupTag.NBSP:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 160;
                            writeIndex += 1;
                            i += 5;
                            continue;
                        case MarkupTag.ZWSP:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = k_ZeroWidthSpace;
                            writeIndex += 1;
                            i += 5;
                            continue;
                        case MarkupTag.SHY:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 0xAD;
                            writeIndex += 1;
                            i += 4;
                            continue;
                        case MarkupTag.STYLE:
                            if (ReplaceOpeningStyleTag(ref tagDefinition, i, out int offset, ref charBuffer, ref writeIndex, ref textStyleStackDepth, ref textStyleStacks, ref generationSettings))
                            {
                                i = offset;
                                continue;
                            }
                            break;
                        case MarkupTag.SLASH_STYLE:
                            ReplaceClosingStyleTag(ref tagDefinition, i, ref charBuffer, ref writeIndex, ref textStyleStackDepth, ref textStyleStacks, ref generationSettings);

                            i += 7;
                            continue;
                    }
                }

                if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                charBuffer[writeIndex].unicode = c;
                writeIndex += 1;
            }

            textStyleStackDepth -= 1;
        }

        /// <summary>
        /// Method to handle inline replacement of style tag by opening style definition.
        /// </summary>
        /// <param name="sourceText"></param>
        /// <param name="srcIndex"></param>
        /// <param name="srcOffset"></param>
        /// <param name="charBuffer"></param>
        /// <param name="writeIndex"></param>
        /// <returns></returns>
        public static bool ReplaceOpeningStyleTag(ref int[] sourceText, int srcIndex, out int srcOffset, ref UnicodeChar[] charBuffer, ref int writeIndex, ref int textStyleStackDepth, ref TextProcessingStack<int>[] textStyleStacks, ref TextGenerationSettings generationSettings)
        {
            // Validate <style> tag.
            int styleHashCode = GetStyleHashCode(ref sourceText, srcIndex + 7, out srcOffset);
            TextStyle style = GetStyle(generationSettings, styleHashCode);

            // Return if we don't have a valid style.
            if (style == null || srcOffset == 0) return false;

            // Increase style depth
            textStyleStackDepth += 1;

            // Push style hashcode onto stack
            textStyleStacks[textStyleStackDepth].Push(style.hashCode);

            int styleLength = style.styleOpeningTagArray.Length;

            // Replace <style> tag with opening definition
            int[] tagDefinition = style.styleOpeningTagArray;

            for (int i = 0; i < styleLength; i++)
            {
                int c = tagDefinition[i];

                if (c == '\\' && i + 1 < styleLength)
                {
                    switch (tagDefinition[i + 1])
                    {
                        case '\\':
                            i += 1;
                            break;
                        case 'n':
                            c = 10;
                            i += 1;
                            break;
                        case 'r':
                            break;
                        case 't':
                            break;
                        case 'u':
                            // UTF16 format is "\uFF00" or u + 2 hex pairs.
                            if (i + 5 < styleLength)
                            {
                                c = GetUTF16(tagDefinition, i + 2);

                                i += 5;
                            }
                            break;
                        case 'U':
                            // UTF32 format is "\UFF00FF00" or U + 4 hex pairs.
                            if (i + 9 < styleLength)
                            {
                                c = GetUTF32(tagDefinition, i + 2);

                                i += 9;
                            }
                            break;
                    }
                }

                if (c == '<')
                {
                    int hashCode = GetMarkupTagHashCode(tagDefinition, i + 1);

                    switch ((MarkupTag)hashCode)
                    {
                        case MarkupTag.BR:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 10;
                            writeIndex += 1;
                            i += 3;
                            continue;
                        case MarkupTag.CR:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 13;
                            writeIndex += 1;
                            i += 3;
                            continue;
                        case MarkupTag.NBSP:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 160;
                            writeIndex += 1;
                            i += 5;
                            continue;
                        case MarkupTag.ZWSP:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = k_ZeroWidthSpace;
                            writeIndex += 1;
                            i += 5;
                            continue;
                        case MarkupTag.SHY:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 0xAD;
                            writeIndex += 1;
                            i += 4;
                            continue;
                        case MarkupTag.STYLE:
                            if (ReplaceOpeningStyleTag(ref tagDefinition, i, out int offset, ref charBuffer, ref writeIndex, ref textStyleStackDepth, ref textStyleStacks, ref generationSettings))
                            {
                                i = offset;
                                continue;
                            }
                            break;
                        case MarkupTag.SLASH_STYLE:
                            ReplaceClosingStyleTag(ref tagDefinition, i, ref charBuffer, ref writeIndex, ref textStyleStackDepth, ref textStyleStacks, ref generationSettings);

                            i += 7;
                            continue;
                    }
                }

                if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                charBuffer[writeIndex].unicode = c;
                writeIndex += 1;
            }

            textStyleStackDepth -= 1;

            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="charBuffer"></param>
        /// <param name="writeIndex"></param>
        public static void InsertClosingStyleTag(ref UnicodeChar[] charBuffer, ref int writeIndex, ref int textStyleStackDepth, ref TextProcessingStack<int>[] textStyleStacks, ref TextGenerationSettings generationSettings)
        {
            // Get style from the Style Stack
            int styleHashCode = textStyleStacks[0].Pop();
            TextStyle style = GetStyle(generationSettings, styleHashCode);

            int styleLength = style.styleClosingTagArray.Length;

            // Replace <style> tag with opening definition
            int[] tagDefinition = style.styleClosingTagArray;

            for (int i = 0; i < styleLength; i++)
            {
                int c = tagDefinition[i];

                if (c == '\\' && i + 1 < styleLength)
                {
                    switch (tagDefinition[i + 1])
                    {
                        case '\\':
                            i += 1;
                            break;
                        case 'n':
                            c = 10;
                            i += 1;
                            break;
                        case 'r':
                            break;
                        case 't':
                            break;
                        case 'u':
                            // UTF16 format is "\uFF00" or u + 2 hex pairs.
                            if (i + 5 < styleLength)
                            {
                                c = GetUTF16(tagDefinition, i + 2);

                                i += 5;
                            }
                            break;
                        case 'U':
                            // UTF32 format is "\UFF00FF00" or U + 4 hex pairs.
                            if (i + 9 < styleLength)
                            {
                                c = GetUTF32(tagDefinition, i + 2);

                                i += 9;
                            }
                            break;
                    }
                }

                if (c == '<')
                {
                    int hashCode = GetMarkupTagHashCode(tagDefinition, i + 1);

                    switch ((MarkupTag)hashCode)
                    {
                        case MarkupTag.BR:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 10;
                            writeIndex += 1;
                            i += 3;
                            continue;
                        case MarkupTag.CR:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 13;
                            writeIndex += 1;
                            i += 3;
                            continue;
                        case MarkupTag.NBSP:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 160;
                            writeIndex += 1;
                            i += 5;
                            continue;
                        case MarkupTag.ZWSP:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = k_ZeroWidthSpace;
                            writeIndex += 1;
                            i += 5;
                            continue;
                        case MarkupTag.SHY:
                            if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                            charBuffer[writeIndex].unicode = 0xAD;
                            writeIndex += 1;
                            i += 4;
                            continue;
                        case MarkupTag.STYLE:
                            if (ReplaceOpeningStyleTag(ref tagDefinition, i, out int offset, ref charBuffer, ref writeIndex, ref textStyleStackDepth, ref textStyleStacks, ref generationSettings))
                            {
                                i = offset;
                                continue;
                            }
                            break;
                        case MarkupTag.SLASH_STYLE:
                            ReplaceClosingStyleTag(ref tagDefinition, i, ref charBuffer, ref writeIndex, ref textStyleStackDepth, ref textStyleStacks, ref generationSettings);

                            i += 7;
                            continue;
                    }
                }

                if (writeIndex == charBuffer.Length) ResizeInternalArray(ref charBuffer);

                charBuffer[writeIndex].unicode = c;
                writeIndex += 1;
            }

            textStyleStackDepth = 0;
        }

        public static TextStyle GetStyle(TextGenerationSettings generationSetting, int hashCode)
        {
            TextStyle style = null;

            TextStyleSheet styleSheet = generationSetting.styleSheet;

            // Get Style from Style Sheet potentially assigned to text object.
            if (styleSheet != null)
            {
                style = styleSheet.GetStyle(hashCode);

                if (style != null)
                    return style;
            }

            // Use Global Style Sheet (if exists)
            styleSheet = generationSetting.textSettings.defaultStyleSheet;

            if (styleSheet != null)
                style = styleSheet.GetStyle(hashCode);

            return style;
        }

        /// <summary>
        /// Get Hashcode for a given tag.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="index"></param>
        /// <param name="closeIndex"></param>
        /// <returns></returns>
        public static int GetStyleHashCode(ref int[] text, int index, out int closeIndex)
        {
            int hashCode = 0;
            closeIndex = 0;

            for (int i = index; i < text.Length; i++)
            {
                // Skip quote '"' character
                if (text[i] == k_DoubleQuotes) continue;

                // Break at '>'
                if (text[i] == k_GreaterThan) { closeIndex = i; break; }

                hashCode = (hashCode << 5) + hashCode ^ ToUpperASCIIFast((char)text[i]);
            }

            return hashCode;
        }

        /// <summary>
        /// Get Hashcode for a given tag.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="index"></param>
        /// <param name="closeIndex"></param>
        /// <returns></returns>
        public static int GetStyleHashCode(ref TextBackingContainer text, int index, out int closeIndex)
        {
            int hashCode = 0;
            closeIndex = 0;

            for (int i = index; i < text.Capacity; i++)
            {
                // Skip quote '"' character
                if (text[i] == k_DoubleQuotes) continue;

                // Break at '>'
                if (text[i] == k_GreaterThan) { closeIndex = i; break; }

                hashCode = (hashCode << 5) + hashCode ^ ToUpperASCIIFast((char)text[i]);
            }

            return hashCode;
        }

        /// <summary>
        /// Convert UTF-32 Hex to Char
        /// </summary>
        /// <returns>The Unicode hex.</returns>
        /// <param name="text"></param>
        /// <param name="i">The index.</param>
        public static int GetUtf32(string text, int i)
        {
            int unicode = 0;
            unicode += HexToInt(text[i]) << 30;
            unicode += HexToInt(text[i + 1]) << 24;
            unicode += HexToInt(text[i + 2]) << 20;
            unicode += HexToInt(text[i + 3]) << 16;
            unicode += HexToInt(text[i + 4]) << 12;
            unicode += HexToInt(text[i + 5]) << 8;
            unicode += HexToInt(text[i + 6]) << 4;
            unicode += HexToInt(text[i + 7]);
            return unicode;
        }

        /// <summary>
        /// Convert UTF-16 Hex to Char
        /// </summary>
        /// <returns>The Unicode hex.</returns>
        /// <param name="text"></param>
        /// <param name="i">The index.</param>
        public static int GetUtf16(string text, int i)
        {
            int unicode = 0;
            unicode += HexToInt(text[i]) << 12;
            unicode += HexToInt(text[i + 1]) << 8;
            unicode += HexToInt(text[i + 2]) << 4;
            unicode += HexToInt(text[i + 3]);
            return unicode;
        }

        /// <summary>
        /// Convert UTF-16 Hex to Char
        /// </summary>
        /// <returns>The Unicode hex.</returns>
        /// <param name="i">The index.</param>
        public static int GetUTF16(string text, int i)
        {
            int unicode = 0;
            unicode += HexToInt(text[i]) << 12;
            unicode += HexToInt(text[i + 1]) << 8;
            unicode += HexToInt(text[i + 2]) << 4;
            unicode += HexToInt(text[i + 3]);
            return unicode;
        }

        public static int GetUTF16(int[] text, int i)
        {
            int unicode = 0;
            unicode += HexToInt((char)text[i]) << 12;
            unicode += HexToInt((char)text[i + 1]) << 8;
            unicode += HexToInt((char)text[i + 2]) << 4;
            unicode += HexToInt((char)text[i + 3]);
            return unicode;
        }

        public static int GetUTF16(uint[] text, int i)
        {
            int unicode = 0;
            unicode += HexToInt((char)text[i]) << 12;
            unicode += HexToInt((char)text[i + 1]) << 8;
            unicode += HexToInt((char)text[i + 2]) << 4;
            unicode += HexToInt((char)text[i + 3]);
            return unicode;
        }

        /// <summary>
        /// Convert UTF-16 Hex to Char
        /// </summary>
        /// <returns>The Unicode hex.</returns>
        /// <param name="i">The index.</param>
        public static int GetUTF16(StringBuilder text, int i)
        {
            int unicode = 0;
            unicode += HexToInt(text[i]) << 12;
            unicode += HexToInt(text[i + 1]) << 8;
            unicode += HexToInt(text[i + 2]) << 4;
            unicode += HexToInt(text[i + 3]);
            return unicode;
        }

        public static int GetUTF16(TextBackingContainer text, int i)
        {
            int unicode = 0;
            unicode += HexToInt((char)text[i]) << 12;
            unicode += HexToInt((char)text[i + 1]) << 8;
            unicode += HexToInt((char)text[i + 2]) << 4;
            unicode += HexToInt((char)text[i + 3]);
            return unicode;
        }

        /// <summary>
        /// Convert UTF-32 Hex to Char
        /// </summary>
        /// <returns>The Unicode hex.</returns>
        /// <param name="i">The index.</param>
        public static int GetUTF32(string text, int i)
        {
            int unicode = 0;
            unicode += HexToInt(text[i]) << 28;
            unicode += HexToInt(text[i + 1]) << 24;
            unicode += HexToInt(text[i + 2]) << 20;
            unicode += HexToInt(text[i + 3]) << 16;
            unicode += HexToInt(text[i + 4]) << 12;
            unicode += HexToInt(text[i + 5]) << 8;
            unicode += HexToInt(text[i + 6]) << 4;
            unicode += HexToInt(text[i + 7]);
            return unicode;
        }

        public static int GetUTF32(int[] text, int i)
        {
            int unicode = 0;
            unicode += HexToInt((char)text[i]) << 28;
            unicode += HexToInt((char)text[i + 1]) << 24;
            unicode += HexToInt((char)text[i + 2]) << 20;
            unicode += HexToInt((char)text[i + 3]) << 16;
            unicode += HexToInt((char)text[i + 4]) << 12;
            unicode += HexToInt((char)text[i + 5]) << 8;
            unicode += HexToInt((char)text[i + 6]) << 4;
            unicode += HexToInt((char)text[i + 7]);
            return unicode;
        }

        public static int GetUTF32(uint[] text, int i)
        {
            int unicode = 0;
            unicode += HexToInt((char)text[i]) << 28;
            unicode += HexToInt((char)text[i + 1]) << 24;
            unicode += HexToInt((char)text[i + 2]) << 20;
            unicode += HexToInt((char)text[i + 3]) << 16;
            unicode += HexToInt((char)text[i + 4]) << 12;
            unicode += HexToInt((char)text[i + 5]) << 8;
            unicode += HexToInt((char)text[i + 6]) << 4;
            unicode += HexToInt((char)text[i + 7]);
            return unicode;
        }

        /// <summary>
        /// Convert UTF-32 Hex to Char
        /// </summary>
        /// <returns>The Unicode hex.</returns>
        /// <param name="i">The index.</param>
        public static int GetUTF32(StringBuilder text, int i)
        {
            int unicode = 0;
            unicode += HexToInt(text[i]) << 28;
            unicode += HexToInt(text[i + 1]) << 24;
            unicode += HexToInt(text[i + 2]) << 20;
            unicode += HexToInt(text[i + 3]) << 16;
            unicode += HexToInt(text[i + 4]) << 12;
            unicode += HexToInt(text[i + 5]) << 8;
            unicode += HexToInt(text[i + 6]) << 4;
            unicode += HexToInt(text[i + 7]);
            return unicode;
        }

        public static int GetUTF32(TextBackingContainer text, int i)
        {
            int unicode = 0;
            unicode += HexToInt((char)text[i]) << 28;
            unicode += HexToInt((char)text[i + 1]) << 24;
            unicode += HexToInt((char)text[i + 2]) << 20;
            unicode += HexToInt((char)text[i + 3]) << 16;
            unicode += HexToInt((char)text[i + 4]) << 12;
            unicode += HexToInt((char)text[i + 5]) << 8;
            unicode += HexToInt((char)text[i + 6]) << 4;
            unicode += HexToInt((char)text[i + 7]);
            return unicode;
        }

        /// <summary>
        /// Get Hashcode for a given tag.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="index"></param>
        /// <param name="closeIndex"></param>
        /// <returns></returns>
        static int GetTagHashCode(ref int[] text, int index, out int closeIndex)
        {
            int hashCode = 0;
            closeIndex = 0;

            for (int i = index; i < text.Length; i++)
            {
                // Skip quote '"' character
                if (text[i] == 34)
                    continue;

                // Break at '>'
                if (text[i] == 62)
                {
                    closeIndex = i;
                    break;
                }

                hashCode = (hashCode << 5) + hashCode ^ (int)TextUtilities.ToUpperASCIIFast((char)text[i]);
            }

            return hashCode;
        }

        /// <summary>
        /// Get Hashcode for a given tag.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="index"></param>
        /// <param name="closeIndex"></param>
        /// <returns></returns>
        static int GetTagHashCode(ref string text, int index, out int closeIndex)
        {
            int hashCode = 0;
            closeIndex = 0;

            for (int i = index; i < text.Length; i++)
            {
                // Skip quote '"' character
                if (text[i] == 34)
                    continue;

                // Break at '>'
                if (text[i] == 62)
                {
                    closeIndex = i;
                    break;
                }

                hashCode = (hashCode << 5) + hashCode ^ (int)TextUtilities.ToUpperASCIIFast(text[i]);
            }

            return hashCode;
        }

        /// <summary>
        /// Store vertex attributes into the appropriate TMP_MeshInfo.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="generationSettings"></param>
        /// <param name="textInfo"></param>
        public static void FillCharacterVertexBuffers(int i, bool convertToLinearSpace, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            int materialIndex = textInfo.textElementInfo[i].materialReferenceIndex;
            int indexX4 = textInfo.meshInfo[materialIndex].vertexCount;

            // Check to make sure our current mesh buffer allocations can hold these new Quads.
            if (indexX4 >= textInfo.meshInfo[materialIndex].vertices.Length)
                textInfo.meshInfo[materialIndex].ResizeMeshInfo(Mathf.NextPowerOfTwo((indexX4 + 4) / 4));

            TextElementInfo[] textElementInfoArray = textInfo.textElementInfo;
            textInfo.textElementInfo[i].vertexIndex = indexX4;

            // Setup Vertices for Characters
            if (generationSettings.inverseYAxis)
            {
                Vector3 axisOffset;
                axisOffset.x = 0;
                axisOffset.y = generationSettings.screenRect.y + generationSettings.screenRect.height;
                axisOffset.z = 0;

                Vector3 position = textElementInfoArray[i].vertexBottomLeft.position;
                position.y *= -1;

                textInfo.meshInfo[materialIndex].vertices[0 + indexX4] = position + axisOffset;
                position = textElementInfoArray[i].vertexTopLeft.position;
                position.y *= -1;
                textInfo.meshInfo[materialIndex].vertices[1 + indexX4] = position + axisOffset;
                position = textElementInfoArray[i].vertexTopRight.position;
                position.y *= -1;
                textInfo.meshInfo[materialIndex].vertices[2 + indexX4] = position + axisOffset;
                position = textElementInfoArray[i].vertexBottomRight.position;
                position.y *= -1;
                textInfo.meshInfo[materialIndex].vertices[3 + indexX4] = position + axisOffset;
            }
            else
            {
                textInfo.meshInfo[materialIndex].vertices[0 + indexX4] = textElementInfoArray[i].vertexBottomLeft.position;
                textInfo.meshInfo[materialIndex].vertices[1 + indexX4] = textElementInfoArray[i].vertexTopLeft.position;
                textInfo.meshInfo[materialIndex].vertices[2 + indexX4] = textElementInfoArray[i].vertexTopRight.position;
                textInfo.meshInfo[materialIndex].vertices[3 + indexX4] = textElementInfoArray[i].vertexBottomRight.position;
            }

            // Setup UVS0
            textInfo.meshInfo[materialIndex].uvs0[0 + indexX4] = textElementInfoArray[i].vertexBottomLeft.uv;
            textInfo.meshInfo[materialIndex].uvs0[1 + indexX4] = textElementInfoArray[i].vertexTopLeft.uv;
            textInfo.meshInfo[materialIndex].uvs0[2 + indexX4] = textElementInfoArray[i].vertexTopRight.uv;
            textInfo.meshInfo[materialIndex].uvs0[3 + indexX4] = textElementInfoArray[i].vertexBottomRight.uv;

            // Setup UVS2
            textInfo.meshInfo[materialIndex].uvs2[0 + indexX4] = textElementInfoArray[i].vertexBottomLeft.uv2;
            textInfo.meshInfo[materialIndex].uvs2[1 + indexX4] = textElementInfoArray[i].vertexTopLeft.uv2;
            textInfo.meshInfo[materialIndex].uvs2[2 + indexX4] = textElementInfoArray[i].vertexTopRight.uv2;
            textInfo.meshInfo[materialIndex].uvs2[3 + indexX4] = textElementInfoArray[i].vertexBottomRight.uv2;

            // setup Vertex Colors
            textInfo.meshInfo[materialIndex].colors32[0 + indexX4] = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexBottomLeft.color) : textElementInfoArray[i].vertexBottomLeft.color;
            textInfo.meshInfo[materialIndex].colors32[1 + indexX4] = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexTopLeft.color) : textElementInfoArray[i].vertexTopLeft.color;
            textInfo.meshInfo[materialIndex].colors32[2 + indexX4] = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexTopRight.color) : textElementInfoArray[i].vertexTopRight.color;
            textInfo.meshInfo[materialIndex].colors32[3 + indexX4] = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexBottomRight.color) : textElementInfoArray[i].vertexBottomRight.color;

            textInfo.meshInfo[materialIndex].vertexCount = indexX4 + 4;
        }

        /// <summary>
        /// Fill Vertex Buffers for Sprites
        /// </summary>
        /// <param name="i"></param>
        /// <param name="generationSettings"></param>
        /// <param name="textInfo"></param>
        public static void FillSpriteVertexBuffers(int i, bool convertToLinearSpace, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            int materialIndex = textInfo.textElementInfo[i].materialReferenceIndex;
            int indexX4 = textInfo.meshInfo[materialIndex].vertexCount;

            TextElementInfo[] textElementInfoArray = textInfo.textElementInfo;
            textInfo.textElementInfo[i].vertexIndex = indexX4;

            // Handle potential axis inversion
            if (generationSettings.inverseYAxis)
            {
                Vector3 axisOffset;
                axisOffset.x = 0;
                axisOffset.y = generationSettings.screenRect.y + generationSettings.screenRect.height;
                axisOffset.z = 0;

                Vector3 position = textElementInfoArray[i].vertexBottomLeft.position;
                position.y *= -1;

                textInfo.meshInfo[materialIndex].vertices[0 + indexX4] = position + axisOffset;
                position = textElementInfoArray[i].vertexTopLeft.position;
                position.y *= -1;
                textInfo.meshInfo[materialIndex].vertices[1 + indexX4] = position + axisOffset;
                position = textElementInfoArray[i].vertexTopRight.position;
                position.y *= -1;
                textInfo.meshInfo[materialIndex].vertices[2 + indexX4] = position + axisOffset;
                position = textElementInfoArray[i].vertexBottomRight.position;
                position.y *= -1;
                textInfo.meshInfo[materialIndex].vertices[3 + indexX4] = position + axisOffset;
            }
            else
            {
                textInfo.meshInfo[materialIndex].vertices[0 + indexX4] = textElementInfoArray[i].vertexBottomLeft.position;
                textInfo.meshInfo[materialIndex].vertices[1 + indexX4] = textElementInfoArray[i].vertexTopLeft.position;
                textInfo.meshInfo[materialIndex].vertices[2 + indexX4] = textElementInfoArray[i].vertexTopRight.position;
                textInfo.meshInfo[materialIndex].vertices[3 + indexX4] = textElementInfoArray[i].vertexBottomRight.position;
            }

            // Setup UVS0
            textInfo.meshInfo[materialIndex].uvs0[0 + indexX4] = textElementInfoArray[i].vertexBottomLeft.uv;
            textInfo.meshInfo[materialIndex].uvs0[1 + indexX4] = textElementInfoArray[i].vertexTopLeft.uv;
            textInfo.meshInfo[materialIndex].uvs0[2 + indexX4] = textElementInfoArray[i].vertexTopRight.uv;
            textInfo.meshInfo[materialIndex].uvs0[3 + indexX4] = textElementInfoArray[i].vertexBottomRight.uv;

            // Setup UVS2
            textInfo.meshInfo[materialIndex].uvs2[0 + indexX4] = textElementInfoArray[i].vertexBottomLeft.uv2;
            textInfo.meshInfo[materialIndex].uvs2[1 + indexX4] = textElementInfoArray[i].vertexTopLeft.uv2;
            textInfo.meshInfo[materialIndex].uvs2[2 + indexX4] = textElementInfoArray[i].vertexTopRight.uv2;
            textInfo.meshInfo[materialIndex].uvs2[3 + indexX4] = textElementInfoArray[i].vertexBottomRight.uv2;

            // setup Vertex Colors
            textInfo.meshInfo[materialIndex].colors32[0 + indexX4] = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexBottomLeft.color) : textElementInfoArray[i].vertexBottomLeft.color;
            textInfo.meshInfo[materialIndex].colors32[1 + indexX4] = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexTopLeft.color) : textElementInfoArray[i].vertexTopLeft.color;
            textInfo.meshInfo[materialIndex].colors32[2 + indexX4] = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexTopRight.color) : textElementInfoArray[i].vertexTopRight.color;
            textInfo.meshInfo[materialIndex].colors32[3 + indexX4] = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexBottomRight.color) : textElementInfoArray[i].vertexBottomRight.color;

            textInfo.meshInfo[materialIndex].vertexCount = indexX4 + 4;
        }

        // Function to offset vertices position to account for line spacing changes.
        public static void AdjustLineOffset(int startIndex, int endIndex, float offset, TextInfo textInfo)
        {
            Vector3 vertexOffset = new Vector3(0, offset, 0);

            for (int i = startIndex; i <= endIndex; i++)
            {
                textInfo.textElementInfo[i].bottomLeft -= vertexOffset;
                textInfo.textElementInfo[i].topLeft -= vertexOffset;
                textInfo.textElementInfo[i].topRight -= vertexOffset;
                textInfo.textElementInfo[i].bottomRight -= vertexOffset;

                textInfo.textElementInfo[i].ascender -= vertexOffset.y;
                textInfo.textElementInfo[i].baseLine -= vertexOffset.y;
                textInfo.textElementInfo[i].descender -= vertexOffset.y;

                if (textInfo.textElementInfo[i].isVisible)
                {
                    textInfo.textElementInfo[i].vertexBottomLeft.position -= vertexOffset;
                    textInfo.textElementInfo[i].vertexTopLeft.position -= vertexOffset;
                    textInfo.textElementInfo[i].vertexTopRight.position -= vertexOffset;
                    textInfo.textElementInfo[i].vertexBottomRight.position -= vertexOffset;
                }
            }
        }

        /// <summary>
        /// Function to increase the size of the Line Extents Array.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="textInfo"></param>
        public static void ResizeLineExtents(int size, TextInfo textInfo)
        {
            size = size > 1024 ? size + 256 : Mathf.NextPowerOfTwo(size + 1);

            LineInfo[] tempLineInfo = new LineInfo[size];
            for (int i = 0; i < size; i++)
            {
                if (i < textInfo.lineInfo.Length)
                    tempLineInfo[i] = textInfo.lineInfo[i];
                else
                {
                    tempLineInfo[i].lineExtents.min = largePositiveVector2;
                    tempLineInfo[i].lineExtents.max = largeNegativeVector2;

                    tempLineInfo[i].ascender = largeNegativeFloat;
                    tempLineInfo[i].descender = largePositiveFloat;
                }
            }

            textInfo.lineInfo = tempLineInfo;
        }

        public static FontStyles LegacyStyleToNewStyle(FontStyle fontStyle)
        {
            switch (fontStyle)
            {
                case FontStyle.Bold:
                    return FontStyles.Bold;
                case FontStyle.Italic:
                    return FontStyles.Italic;
                case FontStyle.BoldAndItalic:
                    return FontStyles.Bold | FontStyles.Italic;
                default:
                    return FontStyles.Normal;
            }
        }

        public static TextAlignment LegacyAlignmentToNewAlignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft:
                    return TextAlignment.TopLeft;
                case TextAnchor.UpperCenter:
                    return TextAlignment.TopCenter;
                case TextAnchor.UpperRight:
                    return TextAlignment.TopRight;
                case TextAnchor.MiddleLeft:
                    return TextAlignment.MiddleLeft;
                case TextAnchor.MiddleCenter:
                    return TextAlignment.MiddleCenter;
                case TextAnchor.MiddleRight:
                    return TextAlignment.MiddleRight;
                case TextAnchor.LowerLeft:
                    return TextAlignment.BottomLeft;
                case TextAnchor.LowerCenter:
                    return TextAlignment.BottomCenter;
                case TextAnchor.LowerRight:
                    return TextAlignment.BottomRight;
                default:
                    return TextAlignment.TopLeft;
            }
        }

        public static uint ConvertToUTF32(uint highSurrogate, uint lowSurrogate)
        {
            return ((highSurrogate - CodePoint.HIGH_SURROGATE_START) * 0x400) + ((lowSurrogate - CodePoint.LOW_SURROGATE_START) + CodePoint.UNICODE_PLANE01_START);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="tagDefinition"></param>
        /// <param name="readIndex"></param>
        /// <returns></returns>
        public static int GetMarkupTagHashCode(TextBackingContainer tagDefinition, int readIndex)
        {
            int hashCode = 0;
            int maxReadIndex = readIndex + 16;
            int tagDefinitionLength = tagDefinition.Capacity;

            for (; readIndex < maxReadIndex && readIndex < tagDefinitionLength; readIndex++)
            {
                uint c = tagDefinition[readIndex];

                if (c == '>' || c == '=' || c == ' ')
                    return hashCode;

                hashCode = ((hashCode << 5) + hashCode) ^ (int)ToUpperASCIIFast((uint)c);
            }

            return hashCode;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="tagDefinition"></param>
        /// <param name="readIndex"></param>
        /// <returns></returns>
        public static int GetMarkupTagHashCode(int[] tagDefinition, int readIndex)
        {
            int hashCode = 0;
            int maxReadIndex = readIndex + 16;
            int tagDefinitionLength = tagDefinition.Length;

            for (; readIndex < maxReadIndex && readIndex < tagDefinitionLength; readIndex++)
            {
                int c = tagDefinition[readIndex];

                if (c == '>' || c == '=' || c == ' ')
                    return hashCode;

                hashCode = ((hashCode << 5) + hashCode) ^ (int)ToUpperASCIIFast((uint)c);
            }

            return hashCode;
        }

        /// <summary>
        /// Get uppercase version of this ASCII character.
        /// </summary>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char ToUpperASCIIFast(char c)
        {
            if (c > k_LookupStringU.Length - 1)
                return c;

            return k_LookupStringU[c];
        }


        /// <summary>
        /// Get uppercase version of this ASCII character.
        /// </summary>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUpperASCIIFast(uint c)
        {
            if (c > k_LookupStringU.Length - 1)
                return c;

            return k_LookupStringU[(int)c];
        }

        /// <summary>
        /// Get uppercase version of this ASCII character.
        /// </summary>
        public static char ToUpperFast(char c)
        {
            if (c > k_LookupStringU.Length - 1)
                return c;

            return k_LookupStringU[c];
        }

        /// <summary>
        /// Method which returns the number of parameters used in a tag attribute and populates an array with such values.
        /// </summary>
        /// <param name="chars">Char[] containing the tag attribute and data</param>
        /// <param name="startIndex">The index of the first char of the data</param>
        /// <param name="length">The length of the data</param>
        /// <param name="parameters">The number of parameters contained in the Char[]</param>
        /// <returns></returns>
        public static int GetAttributeParameters(char[] chars, int startIndex, int length, ref float[] parameters)
        {
            int endIndex = startIndex;
            int attributeCount = 0;

            while (endIndex < startIndex + length)
            {
                parameters[attributeCount] = ConvertToFloat(chars, startIndex, length, out endIndex);

                length -= (endIndex - startIndex) + 1;
                startIndex = endIndex + 1;

                attributeCount += 1;
            }

            return attributeCount;
        }

        public static bool IsBaseGlyph(uint c)
        {
            return !(c >= 0x300 && c <= 0x36F || c >= 0x1AB0 && c <= 0x1AFF || c >= 0x1DC0 && c <= 0x1DFF || c >= 0x20D0 && c <= 0x20FF || c >= 0xFE20 && c <= 0xFE2F ||
                // Thai Marks
                c == 0xE31 || c >= 0xE34 && c <= 0xE3A || c >= 0xE47 && c <= 0xE4E ||
                // Hebrew Marks
                c >= 0x591 && c <= 0x5BD || c == 0x5BF || c >= 0x5C1 && c <= 0x5C2 || c >= 0x5C4 && c <= 0x5C5 || c == 0x5C7 ||
                // Arabic Marks
                c >= 0x610 && c <= 0x61A || c >= 0x64B && c <= 0x65F || c == 0x670 || c >= 0x6D6 && c <= 0x6DC || c >= 0x6DF && c <= 0x6E4 || c >= 0x6E7 && c <= 0x6E8 || c >= 0x6EA && c <= 0x6ED ||
                c >= 0x8D3 && c <= 0x8E1 || c >= 0x8E3 && c <= 0x8FF ||
                c >= 0xFBB2 && c <= 0xFBC1
            );
        }

        public static Color MinAlpha(this Color c1, Color c2)
        {
            float a = c1.a < c2.a ? c1.a : c2.a;

            return new Color(c1.r, c1.g, c1.b, a);
        }

        internal static Color32 GammaToLinear(Color32 c)
        {
            return new Color32(GammaToLinear(c.r), GammaToLinear(c.g), GammaToLinear(c.b), c.a);
        }

        static byte GammaToLinear(byte value)
        {
            float v = value / 255f;

            if (v <= 0.04045f)
                return (byte)(v / 12.92f * 255f);

            if (v < 1.0f)
                return (byte)(Mathf.Pow((v + 0.055f) / 1.055f, 2.4f) * 255);

            if (v == 1.0f)
                return 255;

            return (byte)(Mathf.Pow(v, 2.2f) * 255);
        }

        public static bool IsValidUTF16(TextBackingContainer text, int index)
        {
            for (int i = 0; i < 4; i++)
            {
                uint c = text[index + i];
                if (!(c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F'))
                    return false;
            }

            return true;
        }

        public static bool IsValidUTF32(TextBackingContainer text, int index)
        {
            for (int i = 0; i < 8; i++)
            {
                uint c = text[index + i];
                if (!(c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F'))
                    return false;
            }

            return true;
        }
    }
}
