// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Bindings;
using UnityEngine.TextCore.LowLevel;

namespace UnityEngine.TextCore.Text
{
    /// <summary>
    /// Helper structure to generate rendered text without string allocations.
    /// </summary>
    [VisibleToOtherModules("UnityEngine.UIElementsModule", "UnityEngine.IMGUIModule", "UnityEditor.GraphToolsFoundationModule")]
    internal readonly struct RenderedText : IEquatable<RenderedText>, IEquatable<string>
    {
        public readonly string value;
        public readonly int valueStart, valueLength;
        public readonly string suffix;

        public readonly char repeat;
        public readonly int repeatCount;

        public RenderedText(string value)
            : this(value, 0, value?.Length ?? 0)
        {}

        public RenderedText(string value, string suffix)
            : this(value, 0, value?.Length ?? 0, suffix)
        {}

        public RenderedText(string value, int start, int length, string suffix = null)
        {
            // clamp value arguments to valid ranges
            if (string.IsNullOrEmpty(value))
            {
                start = 0;
                length = 0;
            }
            else
            {
                if (start < 0) start = 0;
                else if (start >= value.Length)
                {
                    start = value.Length;
                    length = 0;
                }

                if (length < 0) length = 0;
                else if (length > value.Length - start)
                    length = value.Length - start;
            }

            this.value = value;
            this.valueStart = start;
            this.valueLength = length;
            this.suffix = suffix;
            this.repeat = (char)0;
            this.repeatCount = 0;
        }

        public RenderedText(char repeat, int repeatCount, string suffix = null)
        {
            if (repeatCount < 0)
                repeatCount = 0;

            this.value = null;
            this.valueStart = 0;
            this.valueLength = 0;
            this.suffix = suffix;
            this.repeat = repeat;
            this.repeatCount = repeatCount;
        }

        public int CharacterCount
        {
            get
            {
                int count = valueLength + repeatCount;
                if (suffix != null)
                    count += suffix.Length;
                return count;
            }
        }

        public struct Enumerator
        {
            private readonly RenderedText m_Source;
            private const int k_ValueStage = 0;
            private const int k_RepeatStage = 1;
            private const int k_SuffixStage = 2;
            private int m_Stage;
            private int m_StageIndex;
            private char m_Current;

            public char Current => m_Current;

            public Enumerator(in RenderedText source)
            {
                m_Source = source;
                m_Stage = 0;
                m_StageIndex = 0;
                m_Current = default;
            }

            public bool MoveNext()
            {
                if (m_Stage == k_ValueStage)
                {
                    if (m_Source.value != null)
                    {
                        int start = m_Source.valueStart;
                        int end = m_Source.valueStart + m_Source.valueLength;
                        if (m_StageIndex < start)
                            m_StageIndex = start;

                        if (m_StageIndex < end)
                        {
                            m_Current = m_Source.value[m_StageIndex];
                            ++m_StageIndex;
                            return true;
                        }
                    }

                    m_Stage = k_ValueStage + 1;
                    m_StageIndex = 0;
                }

                if (m_Stage == k_RepeatStage)
                {
                    if (m_StageIndex < m_Source.repeatCount)
                    {
                        m_Current = m_Source.repeat;
                        ++m_StageIndex;
                        return true;
                    }

                    m_Stage = k_RepeatStage + 1;
                    m_StageIndex = 0;
                }

                if (m_Stage == k_SuffixStage)
                {
                    if (m_Source.suffix != null && m_StageIndex < m_Source.suffix.Length)
                    {
                        m_Current = m_Source.suffix[m_StageIndex];
                        ++m_StageIndex;
                        return true;
                    }

                    m_Stage = k_SuffixStage + 1;
                    m_StageIndex = 0;
                }

                return false;
            }

            public void Reset()
            {
                m_Stage = 0;
                m_StageIndex = 0;
                m_Current = default;
            }
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        public string CreateString()
        {
            var chars = new char[CharacterCount];
            int writeIndex = 0;
            foreach (var c in this)
                chars[writeIndex++] = c;
            return new string(chars);
        }

        public bool Equals(RenderedText other)
        {
            return value == other.value && valueStart == other.valueStart && valueLength == other.valueLength && suffix == other.suffix && repeat == other.repeat && repeatCount == other.repeatCount;
        }

        public bool Equals(string other)
        {
            var otherLength = other?.Length ?? 0;
            var length = this.CharacterCount;
            if (otherLength != length)
                return false;
            if (otherLength == 0) // both empty
                return true;
            int compIndex = 0;
            foreach (var c in this)
                // ReSharper disable once PossibleNullReferenceException
                if (c != other[compIndex++])
                    return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            return (obj is string otherString && Equals(otherString)) ||
                   (obj is RenderedText otherRenderedText && Equals(otherRenderedText));
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(value, valueStart, valueLength, suffix, repeat, repeatCount);
        }
    }

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

    // TODO TextProcessingElement is defined twice in TMP...
    [System.Diagnostics.DebuggerDisplay("Unicode ({unicode})  '{(char)unicode}'")]
    internal struct TextProcessingElement
    {
        public TextProcessingElementType elementType;
        public uint unicode;
        public int stringIndex;
        public int length;
    }

    /// <summary>
    ///
    /// </summary>
    struct TextBackingContainer
    {
        public uint[] Text
        {
            get { return m_Array; }
        }

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

    [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
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
        public static Color32 HexCharsToColor(char[] hexChars, int startIndex, int tagCount)
        {
            if (tagCount == 4)
            {
                byte r = (byte)(HexToInt(hexChars[startIndex + 1]) * 16 + HexToInt(hexChars[startIndex + 1]));
                byte g = (byte)(HexToInt(hexChars[startIndex + 2]) * 16 + HexToInt(hexChars[startIndex + 2]));
                byte b = (byte)(HexToInt(hexChars[startIndex + 3]) * 16 + HexToInt(hexChars[startIndex + 3]));

                return new Color32(r, g, b, 255);
            }
            if (tagCount == 5)
            {
                byte r = (byte)(HexToInt(hexChars[startIndex + 1]) * 16 + HexToInt(hexChars[startIndex + 1]));
                byte g = (byte)(HexToInt(hexChars[startIndex + 2]) * 16 + HexToInt(hexChars[startIndex + 2]));
                byte b = (byte)(HexToInt(hexChars[startIndex + 3]) * 16 + HexToInt(hexChars[startIndex + 3]));
                byte a = (byte)(HexToInt(hexChars[startIndex + 4]) * 16 + HexToInt(hexChars[startIndex + 4]));

                return new Color32(r, g, b, a);
            }
            if (tagCount == 7)
            {
                byte r = (byte)(HexToInt(hexChars[startIndex + 1]) * 16 + HexToInt(hexChars[startIndex + 2]));
                byte g = (byte)(HexToInt(hexChars[startIndex + 3]) * 16 + HexToInt(hexChars[startIndex + 4]));
                byte b = (byte)(HexToInt(hexChars[startIndex + 5]) * 16 + HexToInt(hexChars[startIndex + 6]));

                return new Color32(r, g, b, 255);
            }
            if (tagCount == 9)
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
        public static uint HexToInt(char hex)
        {
            switch (hex)
            {
                case '0': return 0;
                case '1': return 1;
                case '2': return 2;
                case '3': return 3;
                case '4': return 4;
                case '5': return 5;
                case '6': return 6;
                case '7': return 7;
                case '8': return 8;
                case '9': return 9;
                case 'A': case 'a': return 10;
                case 'B': case 'b': return 11;
                case 'C': case 'c': return 12;
                case 'D': case 'd': return 13;
                case 'E': case 'e': return 14;
                case 'F': case 'f': return 15;
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

        // /// <summary>
        // /// Method to store the content of a string into an integer array.
        // /// </summary>
        // /// <param name="sourceText"></param>
        // /// <param name="charBuffer"></param>
        // /// <param name="styleStack"></param>
        // /// <param name="generationSettings"></param>
        // public static void StringToCharArray(string sourceText, ref int[] charBuffer, ref TextProcessingStack<int> styleStack, TextGenerationSettings generationSettings)
        // {
        //     if (sourceText == null)
        //     {
        //         charBuffer[0] = 0;
        //         return;
        //     }
        //
        //     if (charBuffer == null)
        //         charBuffer = new int[8];
        //
        //     // Clear the Style stack.
        //     styleStack.SetDefault(0);
        //
        //     int writeIndex = 0;
        //
        //     for (int i = 0; i < sourceText.Length; i++)
        //     {
        //         if (sourceText[i] == 92 && sourceText.Length > i + 1)
        //         {
        //             switch ((int)sourceText[i + 1])
        //             {
        //                 case 85: // \U00000000 for UTF-32 Unicode
        //                     if (sourceText.Length > i + 9)
        //                     {
        //                         if (writeIndex == charBuffer.Length)
        //                             ResizeInternalArray(ref charBuffer);
        //
        //                         charBuffer[writeIndex] = GetUtf32(sourceText, i + 2);
        //                         i += 9;
        //                         writeIndex += 1;
        //                         continue;
        //                     }
        //                     break;
        //                 case 92: // \ escape
        //                     if (!generationSettings.parseControlCharacters)
        //                         break;
        //
        //                     if (sourceText.Length <= i + 2)
        //                         break;
        //
        //                     if (writeIndex + 2 > charBuffer.Length)
        //                         ResizeInternalArray(ref charBuffer);
        //
        //                     charBuffer[writeIndex] = sourceText[i + 1];
        //                     charBuffer[writeIndex + 1] = sourceText[i + 2];
        //                     i += 2;
        //                     writeIndex += 2;
        //                     continue;
        //                 case 110: // \n LineFeed
        //                     if (!generationSettings.parseControlCharacters)
        //                         break;
        //
        //                     if (writeIndex == charBuffer.Length)
        //                         ResizeInternalArray(ref charBuffer);
        //
        //                     charBuffer[writeIndex] = (char)10;
        //                     i += 1;
        //                     writeIndex += 1;
        //                     continue;
        //                 case 114: // \r
        //                     if (!generationSettings.parseControlCharacters)
        //                         break;
        //
        //                     if (writeIndex == charBuffer.Length)
        //                         ResizeInternalArray(ref charBuffer);
        //
        //                     charBuffer[writeIndex] = (char)13;
        //                     i += 1;
        //                     writeIndex += 1;
        //                     continue;
        //                 case 116: // \t Tab
        //                     if (!generationSettings.parseControlCharacters)
        //                         break;
        //
        //                     if (writeIndex == charBuffer.Length)
        //                         ResizeInternalArray(ref charBuffer);
        //
        //                     charBuffer[writeIndex] = (char)9;
        //                     i += 1;
        //                     writeIndex += 1;
        //                     continue;
        //                 case 117: // \u0000 for UTF-16 Unicode
        //                     if (sourceText.Length > i + 5)
        //                     {
        //                         if (writeIndex == charBuffer.Length)
        //                             ResizeInternalArray(ref charBuffer);
        //
        //                         charBuffer[writeIndex] = (char)GetUtf16(sourceText, i + 2);
        //                         i += 5;
        //                         writeIndex += 1;
        //                         continue;
        //                     }
        //                     break;
        //             }
        //         }
        //
        //         // Handle UTF-32 in the input text (string). // Not sure this is needed //
        //         if (Char.IsHighSurrogate(sourceText[i]) && Char.IsLowSurrogate(sourceText[i + 1]))
        //         {
        //             if (writeIndex == charBuffer.Length)
        //                 ResizeInternalArray(ref charBuffer);
        //
        //             charBuffer[writeIndex] = Char.ConvertToUtf32(sourceText[i], sourceText[i + 1]);
        //             i += 1;
        //             writeIndex += 1;
        //             continue;
        //         }
        //
        //         //// Handle inline replacement of <stlye> and <br> tags.
        //         if (sourceText[i] == 60 && generationSettings.richText)
        //         {
        //             if (IsTagName(ref sourceText, "<BR>", i))
        //             {
        //                 if (writeIndex == charBuffer.Length)
        //                     ResizeInternalArray(ref charBuffer);
        //
        //                 charBuffer[writeIndex] = 10;
        //                 writeIndex += 1;
        //                 i += 3;
        //
        //                 continue;
        //             }
        //             if (IsTagName(ref sourceText, "<STYLE=", i))
        //             {
        //                 int srcOffset;
        //                 if (ReplaceOpeningStyleTag(ref sourceText, i, out srcOffset, ref charBuffer, ref writeIndex, ref styleStack, ref generationSettings))
        //                 {
        //                     i = srcOffset;
        //                     continue;
        //                 }
        //             }
        //             else if (IsTagName(ref sourceText, "</STYLE>", i))
        //             {
        //                 ReplaceClosingStyleTag(ref charBuffer, ref writeIndex, ref styleStack, ref generationSettings);
        //
        //                 // Strip </style> even if style is invalid.
        //                 i += 7;
        //                 continue;
        //             }
        //         }
        //
        //         if (writeIndex == charBuffer.Length)
        //             ResizeInternalArray(ref charBuffer);
        //
        //         charBuffer[writeIndex] = sourceText[i];
        //         writeIndex += 1;
        //     }
        //
        //     if (writeIndex == charBuffer.Length)
        //         ResizeInternalArray(ref charBuffer);
        //
        //     charBuffer[writeIndex] = (char)0;
        // }

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

        internal static void InsertOpeningTextStyle(TextStyle style, ref TextProcessingElement[] charBuffer, ref int writeIndex, ref int textStyleStackDepth, ref TextProcessingStack<int>[] textStyleStacks, ref TextGenerationSettings generationSettings)
        {
            // Return if we don't have a valid style.
            if (style == null)
                return;

            // Increase style depth
            textStyleStackDepth += 1;

            // Push style hashcode onto stack
            textStyleStacks[textStyleStackDepth].Push(style.hashCode);

            // Replace <style> tag with opening definition
            uint[] styleDefinition = style.styleOpeningTagArray;

            InsertTextStyleInTextProcessingArray(ref charBuffer, ref writeIndex, styleDefinition, ref textStyleStackDepth, ref textStyleStacks, ref generationSettings);

            textStyleStackDepth -= 1;
        }

        internal static void InsertClosingTextStyle(TextStyle style, ref TextProcessingElement[] charBuffer, ref int writeIndex, ref int textStyleStackDepth, ref TextProcessingStack<int>[] textStyleStacks, ref TextGenerationSettings generationSettings)
        {
            // Return if we don't have a valid style.
            if (style == null) return;

            // Increase style depth
            textStyleStackDepth += 1;

            // Push style hashcode onto stack
            textStyleStacks[textStyleStackDepth].Push(style.hashCode);
            uint[] styleDefinition = style.styleClosingTagArray;

            InsertTextStyleInTextProcessingArray(ref charBuffer, ref writeIndex, styleDefinition, ref textStyleStackDepth, ref textStyleStacks, ref generationSettings);

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
        public static bool ReplaceOpeningStyleTag(ref TextBackingContainer sourceText, int srcIndex, out int srcOffset, ref TextProcessingElement[] charBuffer, ref int writeIndex, ref int textStyleStackDepth, ref TextProcessingStack<int>[] textStyleStacks, ref TextGenerationSettings generationSettings)
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

            // Replace <style> tag with opening definition
            uint[] styleDefinition = style.styleOpeningTagArray;

            InsertTextStyleInTextProcessingArray(ref charBuffer, ref writeIndex, styleDefinition, ref textStyleStackDepth, ref textStyleStacks, ref generationSettings);

            textStyleStackDepth -= 1;
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
        public static void ReplaceOpeningStyleTag(ref TextProcessingElement[] charBuffer, ref int writeIndex, ref int textStyleStackDepth, ref TextProcessingStack<int>[] textStyleStacks, ref TextGenerationSettings generationSettings)
        {
            // Get style from the Style Stack
            int styleHashCode = textStyleStacks[textStyleStackDepth + 1].Pop();
            TextStyle style = GetStyle(generationSettings, styleHashCode);

            // Return if we don't have a valid style.
            if (style == null) return;

            // Increase style depth
            textStyleStackDepth += 1;

            // Replace <style> tag with opening definition
            uint[] styleDefinition = style.styleOpeningTagArray;

            InsertTextStyleInTextProcessingArray(ref charBuffer, ref writeIndex, styleDefinition, ref textStyleStackDepth, ref textStyleStacks, ref generationSettings);

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
        static bool ReplaceOpeningStyleTag(ref uint[] sourceText, int srcIndex, out int srcOffset, ref TextProcessingElement[] charBuffer, ref int writeIndex, ref int textStyleStackDepth, ref TextProcessingStack<int>[] textStyleStacks, ref TextGenerationSettings generationSettings)
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

            // Replace <style> tag with opening definition
            uint[] styleDefinition = style.styleOpeningTagArray;

            InsertTextStyleInTextProcessingArray(ref charBuffer, ref writeIndex, styleDefinition, ref textStyleStackDepth, ref textStyleStacks, ref generationSettings);

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
        public static void ReplaceClosingStyleTag(ref TextProcessingElement[] charBuffer, ref int writeIndex, ref int textStyleStackDepth, ref TextProcessingStack<int>[] textStyleStacks, ref TextGenerationSettings generationSettings)
        {
            // Get style from the Style Stack
            int styleHashCode = textStyleStacks[textStyleStackDepth + 1].Pop();
            TextStyle style = GetStyle(generationSettings, styleHashCode);

            // Return if we don't have a valid style.
            if (style == null) return;

            // Increase style depth
            textStyleStackDepth += 1;

            // Replace <style> tag with opening definition
            uint[] styleDefinition = style.styleClosingTagArray;

            InsertTextStyleInTextProcessingArray(ref charBuffer, ref writeIndex, styleDefinition, ref textStyleStackDepth, ref textStyleStacks, ref generationSettings);

            textStyleStackDepth -= 1;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="style"></param>
        /// <param name="srcIndex"></param>
        /// <param name="charBuffer"></param>
        /// <param name="writeIndex"></param>
        /// <returns></returns>
        internal static void InsertOpeningStyleTag(TextStyle style, ref TextProcessingElement[] charBuffer, ref int writeIndex, ref int textStyleStackDepth, ref TextProcessingStack<int>[] textStyleStacks, ref TextGenerationSettings generationSettings)
        {
            // Return if we don't have a valid style.
            if (style == null) return;

            textStyleStacks[0].Push(style.hashCode);

            // Replace <style> tag with opening definition
            uint[] styleDefinition = style.styleOpeningTagArray;

            InsertTextStyleInTextProcessingArray(ref charBuffer, ref writeIndex, styleDefinition, ref textStyleStackDepth, ref textStyleStacks, ref generationSettings);

            textStyleStackDepth = 0;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="charBuffer"></param>
        /// <param name="writeIndex"></param>
        internal static void InsertClosingStyleTag(ref TextProcessingElement[] charBuffer, ref int writeIndex, ref int textStyleStackDepth, ref TextProcessingStack<int>[] textStyleStacks, ref TextGenerationSettings generationSettings)
        {
            // Get style from the Style Stack
            int styleHashCode = textStyleStacks[0].Pop();
            TextStyle style = GetStyle(generationSettings, styleHashCode);

            // Replace <style> tag with opening definition
            uint[] styleDefinition = style.styleClosingTagArray;

            InsertTextStyleInTextProcessingArray(ref charBuffer, ref writeIndex, styleDefinition, ref textStyleStackDepth, ref textStyleStacks, ref generationSettings);

            textStyleStackDepth = 0;
        }

        private static void InsertTextStyleInTextProcessingArray(ref TextProcessingElement[] charBuffer, ref int writeIndex, uint[] styleDefinition, ref int textStyleStackDepth, ref TextProcessingStack<int>[] textStyleStacks, ref TextGenerationSettings generationSettings)
        {
            var tagNoParsing = generationSettings.tagNoParsing;
            int styleLength = styleDefinition.Length;

            // Make sure text processing buffer is of sufficient size
            if (writeIndex + styleLength >= charBuffer.Length)
                ResizeInternalArray(ref charBuffer, writeIndex + styleLength);

            for (int i = 0; i < styleLength; i++)
            {
                uint c = styleDefinition[i];
                if (c == '\\' && i + 1 < styleLength)
                {
                    switch (styleDefinition[i + 1])
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
                                c = GetUTF16(styleDefinition, i + 2);

                                i += 5;
                            }

                            break;
                        case 'U':
                            // UTF32 format is "\UFF00FF00" or U + 4 hex pairs.
                            if (i + 9 < styleLength)
                            {
                                c = GetUTF32(styleDefinition, i + 2);

                                i += 9;
                            }

                            break;
                    }
                }

                if (c == '<')
                {
                    int hashCode = GetMarkupTagHashCode(styleDefinition, i + 1);
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

                            charBuffer[writeIndex].unicode = 10;
                            writeIndex += 1;
                            i += 3;
                            continue;
                        case MarkupTag.CR:
                            if (tagNoParsing) break;

                            charBuffer[writeIndex].unicode = 13;
                            writeIndex += 1;
                            i += 3;
                            continue;
                        case MarkupTag.NBSP:
                            if (tagNoParsing) break;

                            charBuffer[writeIndex].unicode = 160;
                            writeIndex += 1;
                            i += 5;
                            continue;
                        case MarkupTag.ZWSP:
                            if (tagNoParsing) break;

                            charBuffer[writeIndex].unicode = 0x200B;
                            writeIndex += 1;
                            i += 5;
                            continue;
                        case MarkupTag.ZWJ:
                            if (tagNoParsing) break;

                            charBuffer[writeIndex].unicode = 0x200D;
                            writeIndex += 1;
                            i += 4;
                            continue;
                        case MarkupTag.SHY:
                            if (tagNoParsing) break;

                            charBuffer[writeIndex].unicode = 0xAD;
                            writeIndex += 1;
                            i += 4;
                            continue;
                        case MarkupTag.STYLE:
                            if (tagNoParsing) break;

                            if (ReplaceOpeningStyleTag(ref styleDefinition, i, out int offset, ref charBuffer, ref writeIndex, ref textStyleStackDepth, ref textStyleStacks, ref generationSettings))
                            {
                                i = offset;
                                continue;
                            }

                            break;
                        case MarkupTag.SLASH_STYLE:
                            if (tagNoParsing) break;

                            ReplaceClosingStyleTag(ref charBuffer, ref writeIndex, ref textStyleStackDepth, ref textStyleStacks, ref generationSettings);

                            i += 7;
                            continue;
                    }

                    // Validate potential text markup element
                    // if (TryGetTextMarkupElement(tagDefinition, ref i, out TextProcessingElement markupElement))
                    // {
                    //     m_TextProcessingArray[writeIndex] = markupElement;
                    //     writeIndex += 1;
                    //     continue;
                    // }
                }

                // Lookup character and glyph data
                // TODO: Add future implementation for character and glyph lookups

                charBuffer[writeIndex].unicode = c;
                writeIndex += 1;
            }
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
        public static int GetStyleHashCode(ref uint[] text, int index, out int closeIndex)
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

        public static uint GetUTF16(uint[] text, int i)
        {
            uint unicode = 0;
            unicode += HexToInt((char)text[i]) << 12;
            unicode += HexToInt((char)text[i + 1]) << 8;
            unicode += HexToInt((char)text[i + 2]) << 4;
            unicode += HexToInt((char)text[i + 3]);
            return unicode;
        }

        public static uint GetUTF16(TextBackingContainer text, int i)
        {
            uint unicode = 0;
            unicode += HexToInt((char)text[i]) << 12;
            unicode += HexToInt((char)text[i + 1]) << 8;
            unicode += HexToInt((char)text[i + 2]) << 4;
            unicode += HexToInt((char)text[i + 3]);
            return unicode;
        }

        public static uint GetUTF32(uint[] text, int i)
        {
            uint unicode = 0;
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

        public static uint GetUTF32(TextBackingContainer text, int i)
        {
            uint unicode = 0;
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
            if (indexX4 >= textInfo.meshInfo[materialIndex].vertexBufferSize)
                textInfo.meshInfo[materialIndex].ResizeMeshInfo(Mathf.NextPowerOfTwo((indexX4 + 4) / 4), generationSettings.isIMGUI);

            TextElementInfo[] textElementInfoArray = textInfo.textElementInfo;
            textInfo.textElementInfo[i].vertexIndex = indexX4;

            // Setup Vertices for Characters
            if (generationSettings.inverseYAxis)
            {
                Vector3 axisOffset;
                axisOffset.x = 0;
                axisOffset.y = generationSettings.screenRect.height;
                axisOffset.z = 0;

                Vector3 position = textElementInfoArray[i].vertexBottomLeft.position;
                position.y *= -1;

                if (textInfo.vertexDataLayout == VertexDataLayout.VBO)
                {
                    textInfo.meshInfo[materialIndex].vertexData[0 + indexX4].position = position + axisOffset;
                    position = textElementInfoArray[i].vertexTopLeft.position;
                    position.y *= -1;
                    textInfo.meshInfo[materialIndex].vertexData[1 + indexX4].position = position + axisOffset;
                    position = textElementInfoArray[i].vertexTopRight.position;
                    position.y *= -1;
                    textInfo.meshInfo[materialIndex].vertexData[2 + indexX4].position = position + axisOffset;
                    position = textElementInfoArray[i].vertexBottomRight.position;
                    position.y *= -1;
                    textInfo.meshInfo[materialIndex].vertexData[3 + indexX4].position = position + axisOffset;
                }
                else
                {
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
            }
            else
            {
                if (textInfo.vertexDataLayout == VertexDataLayout.VBO)
                {
                    textInfo.meshInfo[materialIndex].vertexData[0 + indexX4].position = textElementInfoArray[i].vertexBottomLeft.position;
                    textInfo.meshInfo[materialIndex].vertexData[1 + indexX4].position = textElementInfoArray[i].vertexTopLeft.position;
                    textInfo.meshInfo[materialIndex].vertexData[2 + indexX4].position = textElementInfoArray[i].vertexTopRight.position;
                    textInfo.meshInfo[materialIndex].vertexData[3 + indexX4].position = textElementInfoArray[i].vertexBottomRight.position;
                }
                else
                {
                    textInfo.meshInfo[materialIndex].vertices[0 + indexX4] = textElementInfoArray[i].vertexBottomLeft.position;
                    textInfo.meshInfo[materialIndex].vertices[1 + indexX4] = textElementInfoArray[i].vertexTopLeft.position;
                    textInfo.meshInfo[materialIndex].vertices[2 + indexX4] = textElementInfoArray[i].vertexTopRight.position;
                    textInfo.meshInfo[materialIndex].vertices[3 + indexX4] = textElementInfoArray[i].vertexBottomRight.position;
                }
            }

            // Setup UVS0
            if (textInfo.vertexDataLayout == VertexDataLayout.VBO)
            {
                textInfo.meshInfo[materialIndex].vertexData[0 + indexX4].uv0 = textElementInfoArray[i].vertexBottomLeft.uv;
                textInfo.meshInfo[materialIndex].vertexData[1 + indexX4].uv0 = textElementInfoArray[i].vertexTopLeft.uv;
                textInfo.meshInfo[materialIndex].vertexData[2 + indexX4].uv0 = textElementInfoArray[i].vertexTopRight.uv;
                textInfo.meshInfo[materialIndex].vertexData[3 + indexX4].uv0 = textElementInfoArray[i].vertexBottomRight.uv;
            }
            else
            {
                textInfo.meshInfo[materialIndex].uvs0[0 + indexX4] = textElementInfoArray[i].vertexBottomLeft.uv;
                textInfo.meshInfo[materialIndex].uvs0[1 + indexX4] = textElementInfoArray[i].vertexTopLeft.uv;
                textInfo.meshInfo[materialIndex].uvs0[2 + indexX4] = textElementInfoArray[i].vertexTopRight.uv;
                textInfo.meshInfo[materialIndex].uvs0[3 + indexX4] = textElementInfoArray[i].vertexBottomRight.uv;
            }

            // Setup UVS2
            if (textInfo.vertexDataLayout == VertexDataLayout.VBO)
            {
                textInfo.meshInfo[materialIndex].vertexData[0 + indexX4].uv2 = textElementInfoArray[i].vertexBottomLeft.uv2;
                textInfo.meshInfo[materialIndex].vertexData[1 + indexX4].uv2 = textElementInfoArray[i].vertexTopLeft.uv2;
                textInfo.meshInfo[materialIndex].vertexData[2 + indexX4].uv2 = textElementInfoArray[i].vertexTopRight.uv2;
                textInfo.meshInfo[materialIndex].vertexData[3 + indexX4].uv2 = textElementInfoArray[i].vertexBottomRight.uv2;
            }
            else
            {
                textInfo.meshInfo[materialIndex].uvs2[0 + indexX4] = textElementInfoArray[i].vertexBottomLeft.uv2;
                textInfo.meshInfo[materialIndex].uvs2[1 + indexX4] = textElementInfoArray[i].vertexTopLeft.uv2;
                textInfo.meshInfo[materialIndex].uvs2[2 + indexX4] = textElementInfoArray[i].vertexTopRight.uv2;
                textInfo.meshInfo[materialIndex].uvs2[3 + indexX4] = textElementInfoArray[i].vertexBottomRight.uv2;
            }

            // setup Vertex Colors
            if (textInfo.vertexDataLayout == VertexDataLayout.VBO)
            {
                textInfo.meshInfo[materialIndex].vertexData[0 + indexX4].color = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexBottomLeft.color) : textElementInfoArray[i].vertexBottomLeft.color;
                textInfo.meshInfo[materialIndex].vertexData[1 + indexX4].color = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexTopLeft.color) : textElementInfoArray[i].vertexTopLeft.color;
                textInfo.meshInfo[materialIndex].vertexData[2 + indexX4].color = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexTopRight.color) : textElementInfoArray[i].vertexTopRight.color;
                textInfo.meshInfo[materialIndex].vertexData[3 + indexX4].color = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexBottomRight.color) : textElementInfoArray[i].vertexBottomRight.color;
            }
            else
            {
                textInfo.meshInfo[materialIndex].colors32[0 + indexX4] = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexBottomLeft.color) : textElementInfoArray[i].vertexBottomLeft.color;
                textInfo.meshInfo[materialIndex].colors32[1 + indexX4] = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexTopLeft.color) : textElementInfoArray[i].vertexTopLeft.color;
                textInfo.meshInfo[materialIndex].colors32[2 + indexX4] = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexTopRight.color) : textElementInfoArray[i].vertexTopRight.color;
                textInfo.meshInfo[materialIndex].colors32[3 + indexX4] = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexBottomRight.color) : textElementInfoArray[i].vertexBottomRight.color;
            }

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
            textInfo.meshInfo[materialIndex].applySDF = false;

            TextElementInfo[] textElementInfoArray = textInfo.textElementInfo;
            textInfo.textElementInfo[i].vertexIndex = indexX4;

            // Handle potential axis inversion
            if (generationSettings.inverseYAxis)
            {
                Vector3 axisOffset;
                axisOffset.x = 0;
                axisOffset.y = generationSettings.screenRect.height;
                axisOffset.z = 0;

                Vector3 position = textElementInfoArray[i].vertexBottomLeft.position;
                position.y *= -1;

                if (textInfo.vertexDataLayout == VertexDataLayout.VBO)
                {
                    textInfo.meshInfo[materialIndex].vertexData[0 + indexX4].position = position + axisOffset;
                    position = textElementInfoArray[i].vertexTopLeft.position;
                    position.y *= -1;
                    textInfo.meshInfo[materialIndex].vertexData[1 + indexX4].position = position + axisOffset;
                    position = textElementInfoArray[i].vertexTopRight.position;
                    position.y *= -1;
                    textInfo.meshInfo[materialIndex].vertexData[2 + indexX4].position = position + axisOffset;
                    position = textElementInfoArray[i].vertexBottomRight.position;
                    position.y *= -1;
                    textInfo.meshInfo[materialIndex].vertexData[3 + indexX4].position = position + axisOffset;
                }
                else
                {
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
            }
            else
            {
                if (textInfo.vertexDataLayout == VertexDataLayout.VBO)
                {
                    textInfo.meshInfo[materialIndex].vertexData[0 + indexX4].position = textElementInfoArray[i].vertexBottomLeft.position;
                    textInfo.meshInfo[materialIndex].vertexData[1 + indexX4].position = textElementInfoArray[i].vertexTopLeft.position;
                    textInfo.meshInfo[materialIndex].vertexData[2 + indexX4].position = textElementInfoArray[i].vertexTopRight.position;
                    textInfo.meshInfo[materialIndex].vertexData[3 + indexX4].position = textElementInfoArray[i].vertexBottomRight.position;
                }
                else
                {
                    textInfo.meshInfo[materialIndex].vertices[0 + indexX4] = textElementInfoArray[i].vertexBottomLeft.position;
                    textInfo.meshInfo[materialIndex].vertices[1 + indexX4] = textElementInfoArray[i].vertexTopLeft.position;
                    textInfo.meshInfo[materialIndex].vertices[2 + indexX4] = textElementInfoArray[i].vertexTopRight.position;
                    textInfo.meshInfo[materialIndex].vertices[3 + indexX4] = textElementInfoArray[i].vertexBottomRight.position;
                }
            }

            // Setup UVS0
            if (textInfo.vertexDataLayout == VertexDataLayout.VBO)
            {
                textInfo.meshInfo[materialIndex].vertexData[0 + indexX4].uv0 = textElementInfoArray[i].vertexBottomLeft.uv;
                textInfo.meshInfo[materialIndex].vertexData[1 + indexX4].uv0 = textElementInfoArray[i].vertexTopLeft.uv;
                textInfo.meshInfo[materialIndex].vertexData[2 + indexX4].uv0 = textElementInfoArray[i].vertexTopRight.uv;
                textInfo.meshInfo[materialIndex].vertexData[3 + indexX4].uv0 = textElementInfoArray[i].vertexBottomRight.uv;
            }
            else
            {
                textInfo.meshInfo[materialIndex].uvs0[0 + indexX4] = textElementInfoArray[i].vertexBottomLeft.uv;
                textInfo.meshInfo[materialIndex].uvs0[1 + indexX4] = textElementInfoArray[i].vertexTopLeft.uv;
                textInfo.meshInfo[materialIndex].uvs0[2 + indexX4] = textElementInfoArray[i].vertexTopRight.uv;
                textInfo.meshInfo[materialIndex].uvs0[3 + indexX4] = textElementInfoArray[i].vertexBottomRight.uv;
            }

            // Setup UVS2
            if (textInfo.vertexDataLayout == VertexDataLayout.VBO)
            {
                textInfo.meshInfo[materialIndex].vertexData[0 + indexX4].uv2 = textElementInfoArray[i].vertexBottomLeft.uv2;
                textInfo.meshInfo[materialIndex].vertexData[1 + indexX4].uv2 = textElementInfoArray[i].vertexTopLeft.uv2;
                textInfo.meshInfo[materialIndex].vertexData[2 + indexX4].uv2 = textElementInfoArray[i].vertexTopRight.uv2;
                textInfo.meshInfo[materialIndex].vertexData[3 + indexX4].uv2 = textElementInfoArray[i].vertexBottomRight.uv2;
            }
            else
            {
                textInfo.meshInfo[materialIndex].uvs2[0 + indexX4] = textElementInfoArray[i].vertexBottomLeft.uv2;
                textInfo.meshInfo[materialIndex].uvs2[1 + indexX4] = textElementInfoArray[i].vertexTopLeft.uv2;
                textInfo.meshInfo[materialIndex].uvs2[2 + indexX4] = textElementInfoArray[i].vertexTopRight.uv2;
                textInfo.meshInfo[materialIndex].uvs2[3 + indexX4] = textElementInfoArray[i].vertexBottomRight.uv2;
            }

            // setup Vertex Colors
            if (textInfo.vertexDataLayout == VertexDataLayout.VBO)
            {
                textInfo.meshInfo[materialIndex].vertexData[0 + indexX4].color = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexBottomLeft.color) : textElementInfoArray[i].vertexBottomLeft.color;
                textInfo.meshInfo[materialIndex].vertexData[1 + indexX4].color = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexTopLeft.color) : textElementInfoArray[i].vertexTopLeft.color;
                textInfo.meshInfo[materialIndex].vertexData[2 + indexX4].color = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexTopRight.color) : textElementInfoArray[i].vertexTopRight.color;
                textInfo.meshInfo[materialIndex].vertexData[3 + indexX4].color = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexBottomRight.color) : textElementInfoArray[i].vertexBottomRight.color;
            }
            else
            {
                textInfo.meshInfo[materialIndex].colors32[0 + indexX4] = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexBottomLeft.color) : textElementInfoArray[i].vertexBottomLeft.color;
                textInfo.meshInfo[materialIndex].colors32[1 + indexX4] = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexTopLeft.color) : textElementInfoArray[i].vertexTopLeft.color;
                textInfo.meshInfo[materialIndex].colors32[2 + indexX4] = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexTopRight.color) : textElementInfoArray[i].vertexTopRight.color;
                textInfo.meshInfo[materialIndex].colors32[3 + indexX4] = convertToLinearSpace ? GammaToLinear(textElementInfoArray[i].vertexBottomRight.color) : textElementInfoArray[i].vertexBottomRight.color;
            }
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

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static UnityEngine.TextCore.HorizontalAlignment GetHorizontalAlignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.LowerLeft:
                case TextAnchor.MiddleLeft:
                case TextAnchor.UpperLeft:
                    return UnityEngine.TextCore.HorizontalAlignment.Left;
                case TextAnchor.LowerCenter:
                case TextAnchor.MiddleCenter:
                case TextAnchor.UpperCenter:
                    return UnityEngine.TextCore.HorizontalAlignment.Center;
                case TextAnchor.LowerRight:
                case TextAnchor.MiddleRight:
                case TextAnchor.UpperRight:
                    return UnityEngine.TextCore.HorizontalAlignment.Right;
                default:
                    return UnityEngine.TextCore.HorizontalAlignment.Left;
            }
        }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static UnityEngine.TextCore.VerticalAlignment GetVerticalAlignment(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.LowerLeft:
                case TextAnchor.LowerCenter:
                case TextAnchor.LowerRight:
                    return UnityEngine.TextCore.VerticalAlignment.Bottom;
                case TextAnchor.MiddleLeft:
                case TextAnchor.MiddleCenter:
                case TextAnchor.MiddleRight:
                    return UnityEngine.TextCore.VerticalAlignment.Middle;
                case TextAnchor.UpperLeft:
                case TextAnchor.UpperCenter:
                case TextAnchor.UpperRight:
                    return UnityEngine.TextCore.VerticalAlignment.Top;
                default:
                    return UnityEngine.TextCore.VerticalAlignment.Top;
            }
        }

        public static uint ConvertToUTF32(uint highSurrogate, uint lowSurrogate)
        {
            return ((highSurrogate - CodePoint.HIGH_SURROGATE_START) * 0x400) + ((lowSurrogate - CodePoint.LOW_SURROGATE_START) + CodePoint.UNICODE_PLANE01_START);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="styleDefinition"></param>
        /// <param name="readIndex"></param>
        /// <returns></returns>
        public static int GetMarkupTagHashCode(TextBackingContainer styleDefinition, int readIndex)
        {
            int hashCode = 0;
            int maxReadIndex = readIndex + 16;
            int styleDefinitionLength = styleDefinition.Capacity;

            for (; readIndex < maxReadIndex && readIndex < styleDefinitionLength; readIndex++)
            {
                uint c = styleDefinition[readIndex];

                if (c == '>' || c == '=' || c == ' ')
                    return hashCode;

                hashCode = ((hashCode << 5) + hashCode) ^ (int)ToUpperASCIIFast(c);
            }

            return hashCode;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="tagDefinition"></param>
        /// <param name="readIndex"></param>
        /// <returns></returns>
        public static int GetMarkupTagHashCode(uint[] styleDefinition, int readIndex)
        {
            int hashCode = 0;
            int maxReadIndex = readIndex + 16;
            int tagDefinitionLength = styleDefinition.Length;

            for (; readIndex < maxReadIndex && readIndex < tagDefinitionLength; readIndex++)
            {
                uint c = styleDefinition[readIndex];

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

        public static bool IsBitmapRendering(GlyphRenderMode glyphRenderMode)
        {
            return glyphRenderMode == GlyphRenderMode.RASTER || glyphRenderMode == GlyphRenderMode.RASTER_HINTED || glyphRenderMode == GlyphRenderMode.SMOOTH || glyphRenderMode == GlyphRenderMode.SMOOTH_HINTED;
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

        static readonly HashSet<uint> k_EmojiLookup = new HashSet<uint>() {
        0x0023, 0x002A, 0x0030, 0x0031, 0x0032, 0x0033, 0x0034, 0x0035, 0x0036, 0x0037, 0x0038, 0x0039, 0x00A9, 0x00AE, 0x203C, 0x2049, 0x2122, 0x2139, 0x2194, 0x2195, 0x2196, 0x2197, 0x2198, 0x2199, 0x21A9, 0x21AA, 0x231A, 0x231B, 0x2328, 0x23CF, 0x23E9, 0x23EA, 0x23EB, 0x23EC, 0x23ED, 0x23EE, 0x23EF, 0x23F0, 0x23F1, 0x23F2, 0x23F3, 0x23F8, 0x23F9, 0x23FA, 0x24C2, 0x25AA, 0x25AB, 0x25B6, 0x25C0, 0x25FB,
        0x25FC, 0x25FD, 0x25FE, 0x2600, 0x2601, 0x2602, 0x2603, 0x2604, 0x260E, 0x2611, 0x2614, 0x2615, 0x2618, 0x261D, 0x2620, 0x2622, 0x2623, 0x2626, 0x262A, 0x262E, 0x262F, 0x2638, 0x2639, 0x263A, 0x2640, 0x2642, 0x2648, 0x2649, 0x264A, 0x264B, 0x264C, 0x264D, 0x264E, 0x264F, 0x2650, 0x2651, 0x2652, 0x2653, 0x265F, 0x2660, 0x2663, 0x2665, 0x2666, 0x2668, 0x267B, 0x267E, 0x267F, 0x2692, 0x2693, 0x2694,
        0x2695, 0x2696, 0x2697, 0x2699, 0x269B, 0x269C, 0x26A0, 0x26A1, 0x26A7, 0x26AA, 0x26AB, 0x26B0, 0x26B1, 0x26BD, 0x26BE, 0x26C4, 0x26C5, 0x26C8, 0x26CE, 0x26CF, 0x26D1, 0x26D3, 0x26D4, 0x26E9, 0x26EA, 0x26F0, 0x26F1, 0x26F2, 0x26F3, 0x26F4, 0x26F5, 0x26F7, 0x26F8, 0x26F9, 0x26FA, 0x26FD, 0x2702, 0x2705, 0x2708, 0x2709, 0x270A, 0x270B, 0x270C, 0x270D, 0x270F, 0x2712, 0x2714, 0x2716, 0x271D, 0x2721,
        0x2728, 0x2733, 0x2734, 0x2744, 0x2747, 0x274C, 0x274E, 0x2753, 0x2754, 0x2755, 0x2757, 0x2763, 0x2764, 0x2795, 0x2796, 0x2797, 0x27A1, 0x27B0, 0x27BF, 0x2934, 0x2935, 0x2B05, 0x2B06, 0x2B07, 0x2B1B, 0x2B1C, 0x2B50, 0x2B55, 0x3030, 0x303D, 0x3297, 0x3299, 0x1F004, 0x1F0CF, 0x1F170, 0x1F171, 0x1F17E, 0x1F17F, 0x1F18E, 0x1F191, 0x1F192, 0x1F193, 0x1F194, 0x1F195, 0x1F196, 0x1F197, 0x1F198, 0x1F199, 0x1F19A, 0x1F1E6,
        0x1F1E7, 0x1F1E8, 0x1F1E9, 0x1F1EA, 0x1F1EB, 0x1F1EC, 0x1F1ED, 0x1F1EE, 0x1F1EF, 0x1F1F0, 0x1F1F1, 0x1F1F2, 0x1F1F3, 0x1F1F4, 0x1F1F5, 0x1F1F6, 0x1F1F7, 0x1F1F8, 0x1F1F9, 0x1F1FA, 0x1F1FB, 0x1F1FC, 0x1F1FD, 0x1F1FE, 0x1F1FF, 0x1F201, 0x1F202, 0x1F21A, 0x1F22F, 0x1F232, 0x1F233, 0x1F234, 0x1F235, 0x1F236, 0x1F237, 0x1F238, 0x1F239, 0x1F23A, 0x1F250, 0x1F251, 0x1F300, 0x1F301, 0x1F302, 0x1F303, 0x1F304, 0x1F305, 0x1F306, 0x1F307, 0x1F308, 0x1F309,
        0x1F30A, 0x1F30B, 0x1F30C, 0x1F30D, 0x1F30E, 0x1F30F, 0x1F310, 0x1F311, 0x1F312, 0x1F313, 0x1F314, 0x1F315, 0x1F316, 0x1F317, 0x1F318, 0x1F319, 0x1F31A, 0x1F31B, 0x1F31C, 0x1F31D, 0x1F31E, 0x1F31F, 0x1F320, 0x1F321, 0x1F324, 0x1F325, 0x1F326, 0x1F327, 0x1F328, 0x1F329, 0x1F32A, 0x1F32B, 0x1F32C, 0x1F32D, 0x1F32E, 0x1F32F, 0x1F330, 0x1F331, 0x1F332, 0x1F333, 0x1F334, 0x1F335, 0x1F336, 0x1F337, 0x1F338, 0x1F339, 0x1F33A, 0x1F33B, 0x1F33C, 0x1F33D,
        0x1F33E, 0x1F33F, 0x1F340, 0x1F341, 0x1F342, 0x1F343, 0x1F344, 0x1F345, 0x1F346, 0x1F347, 0x1F348, 0x1F349, 0x1F34A, 0x1F34B, 0x1F34C, 0x1F34D, 0x1F34E, 0x1F34F, 0x1F350, 0x1F351, 0x1F352, 0x1F353, 0x1F354, 0x1F355, 0x1F356, 0x1F357, 0x1F358, 0x1F359, 0x1F35A, 0x1F35B, 0x1F35C, 0x1F35D, 0x1F35E, 0x1F35F, 0x1F360, 0x1F361, 0x1F362, 0x1F363, 0x1F364, 0x1F365, 0x1F366, 0x1F367, 0x1F368, 0x1F369, 0x1F36A, 0x1F36B, 0x1F36C, 0x1F36D, 0x1F36E, 0x1F36F,
        0x1F370, 0x1F371, 0x1F372, 0x1F373, 0x1F374, 0x1F375, 0x1F376, 0x1F377, 0x1F378, 0x1F379, 0x1F37A, 0x1F37B, 0x1F37C, 0x1F37D, 0x1F37E, 0x1F37F, 0x1F380, 0x1F381, 0x1F382, 0x1F383, 0x1F384, 0x1F385, 0x1F386, 0x1F387, 0x1F388, 0x1F389, 0x1F38A, 0x1F38B, 0x1F38C, 0x1F38D, 0x1F38E, 0x1F38F, 0x1F390, 0x1F391, 0x1F392, 0x1F393, 0x1F396, 0x1F397, 0x1F399, 0x1F39A, 0x1F39B, 0x1F39E, 0x1F39F, 0x1F3A0, 0x1F3A1, 0x1F3A2, 0x1F3A3, 0x1F3A4, 0x1F3A5, 0x1F3A6,
        0x1F3A7, 0x1F3A8, 0x1F3A9, 0x1F3AA, 0x1F3AB, 0x1F3AC, 0x1F3AD, 0x1F3AE, 0x1F3AF, 0x1F3B0, 0x1F3B1, 0x1F3B2, 0x1F3B3, 0x1F3B4, 0x1F3B5, 0x1F3B6, 0x1F3B7, 0x1F3B8, 0x1F3B9, 0x1F3BA, 0x1F3BB, 0x1F3BC, 0x1F3BD, 0x1F3BE, 0x1F3BF, 0x1F3C0, 0x1F3C1, 0x1F3C2, 0x1F3C3, 0x1F3C4, 0x1F3C5, 0x1F3C6, 0x1F3C7, 0x1F3C8, 0x1F3C9, 0x1F3CA, 0x1F3CB, 0x1F3CC, 0x1F3CD, 0x1F3CE, 0x1F3CF, 0x1F3D0, 0x1F3D1, 0x1F3D2, 0x1F3D3, 0x1F3D4, 0x1F3D5, 0x1F3D6, 0x1F3D7, 0x1F3D8,
        0x1F3D9, 0x1F3DA, 0x1F3DB, 0x1F3DC, 0x1F3DD, 0x1F3DE, 0x1F3DF, 0x1F3E0, 0x1F3E1, 0x1F3E2, 0x1F3E3, 0x1F3E4, 0x1F3E5, 0x1F3E6, 0x1F3E7, 0x1F3E8, 0x1F3E9, 0x1F3EA, 0x1F3EB, 0x1F3EC, 0x1F3ED, 0x1F3EE, 0x1F3EF, 0x1F3F0, 0x1F3F3, 0x1F3F4, 0x1F3F5, 0x1F3F7, 0x1F3F8, 0x1F3F9, 0x1F3FA, 0x1F3FB, 0x1F3FC, 0x1F3FD, 0x1F3FE, 0x1F3FF, 0x1F400, 0x1F401, 0x1F402, 0x1F403, 0x1F404, 0x1F405, 0x1F406, 0x1F407, 0x1F408, 0x1F409, 0x1F40A, 0x1F40B, 0x1F40C, 0x1F40D,
        0x1F40E, 0x1F40F, 0x1F410, 0x1F411, 0x1F412, 0x1F413, 0x1F414, 0x1F415, 0x1F416, 0x1F417, 0x1F418, 0x1F419, 0x1F41A, 0x1F41B, 0x1F41C, 0x1F41D, 0x1F41E, 0x1F41F, 0x1F420, 0x1F421, 0x1F422, 0x1F423, 0x1F424, 0x1F425, 0x1F426, 0x1F427, 0x1F428, 0x1F429, 0x1F42A, 0x1F42B, 0x1F42C, 0x1F42D, 0x1F42E, 0x1F42F, 0x1F430, 0x1F431, 0x1F432, 0x1F433, 0x1F434, 0x1F435, 0x1F436, 0x1F437, 0x1F438, 0x1F439, 0x1F43A, 0x1F43B, 0x1F43C, 0x1F43D, 0x1F43E, 0x1F43F,
        0x1F440, 0x1F441, 0x1F442, 0x1F443, 0x1F444, 0x1F445, 0x1F446, 0x1F447, 0x1F448, 0x1F449, 0x1F44A, 0x1F44B, 0x1F44C, 0x1F44D, 0x1F44E, 0x1F44F, 0x1F450, 0x1F451, 0x1F452, 0x1F453, 0x1F454, 0x1F455, 0x1F456, 0x1F457, 0x1F458, 0x1F459, 0x1F45A, 0x1F45B, 0x1F45C, 0x1F45D, 0x1F45E, 0x1F45F, 0x1F460, 0x1F461, 0x1F462, 0x1F463, 0x1F464, 0x1F465, 0x1F466, 0x1F467, 0x1F468, 0x1F469, 0x1F46A, 0x1F46B, 0x1F46C, 0x1F46D, 0x1F46E, 0x1F46F, 0x1F470, 0x1F471,
        0x1F472, 0x1F473, 0x1F474, 0x1F475, 0x1F476, 0x1F477, 0x1F478, 0x1F479, 0x1F47A, 0x1F47B, 0x1F47C, 0x1F47D, 0x1F47E, 0x1F47F, 0x1F480, 0x1F481, 0x1F482, 0x1F483, 0x1F484, 0x1F485, 0x1F486, 0x1F487, 0x1F488, 0x1F489, 0x1F48A, 0x1F48B, 0x1F48C, 0x1F48D, 0x1F48E, 0x1F48F, 0x1F490, 0x1F491, 0x1F492, 0x1F493, 0x1F494, 0x1F495, 0x1F496, 0x1F497, 0x1F498, 0x1F499, 0x1F49A, 0x1F49B, 0x1F49C, 0x1F49D, 0x1F49E, 0x1F49F, 0x1F4A0, 0x1F4A1, 0x1F4A2, 0x1F4A3,
        0x1F4A4, 0x1F4A5, 0x1F4A6, 0x1F4A7, 0x1F4A8, 0x1F4A9, 0x1F4AA, 0x1F4AB, 0x1F4AC, 0x1F4AD, 0x1F4AE, 0x1F4AF, 0x1F4B0, 0x1F4B1, 0x1F4B2, 0x1F4B3, 0x1F4B4, 0x1F4B5, 0x1F4B6, 0x1F4B7, 0x1F4B8, 0x1F4B9, 0x1F4BA, 0x1F4BB, 0x1F4BC, 0x1F4BD, 0x1F4BE, 0x1F4BF, 0x1F4C0, 0x1F4C1, 0x1F4C2, 0x1F4C3, 0x1F4C4, 0x1F4C5, 0x1F4C6, 0x1F4C7, 0x1F4C8, 0x1F4C9, 0x1F4CA, 0x1F4CB, 0x1F4CC, 0x1F4CD, 0x1F4CE, 0x1F4CF, 0x1F4D0, 0x1F4D1, 0x1F4D2, 0x1F4D3, 0x1F4D4, 0x1F4D5,
        0x1F4D6, 0x1F4D7, 0x1F4D8, 0x1F4D9, 0x1F4DA, 0x1F4DB, 0x1F4DC, 0x1F4DD, 0x1F4DE, 0x1F4DF, 0x1F4E0, 0x1F4E1, 0x1F4E2, 0x1F4E3, 0x1F4E4, 0x1F4E5, 0x1F4E6, 0x1F4E7, 0x1F4E8, 0x1F4E9, 0x1F4EA, 0x1F4EB, 0x1F4EC, 0x1F4ED, 0x1F4EE, 0x1F4EF, 0x1F4F0, 0x1F4F1, 0x1F4F2, 0x1F4F3, 0x1F4F4, 0x1F4F5, 0x1F4F6, 0x1F4F7, 0x1F4F8, 0x1F4F9, 0x1F4FA, 0x1F4FB, 0x1F4FC, 0x1F4FD, 0x1F4FF, 0x1F500, 0x1F501, 0x1F502, 0x1F503, 0x1F504, 0x1F505, 0x1F506, 0x1F507, 0x1F508,
        0x1F509, 0x1F50A, 0x1F50B, 0x1F50C, 0x1F50D, 0x1F50E, 0x1F50F, 0x1F510, 0x1F511, 0x1F512, 0x1F513, 0x1F514, 0x1F515, 0x1F516, 0x1F517, 0x1F518, 0x1F519, 0x1F51A, 0x1F51B, 0x1F51C, 0x1F51D, 0x1F51E, 0x1F51F, 0x1F520, 0x1F521, 0x1F522, 0x1F523, 0x1F524, 0x1F525, 0x1F526, 0x1F527, 0x1F528, 0x1F529, 0x1F52A, 0x1F52B, 0x1F52C, 0x1F52D, 0x1F52E, 0x1F52F, 0x1F530, 0x1F531, 0x1F532, 0x1F533, 0x1F534, 0x1F535, 0x1F536, 0x1F537, 0x1F538, 0x1F539, 0x1F53A,
        0x1F53B, 0x1F53C, 0x1F53D, 0x1F549, 0x1F54A, 0x1F54B, 0x1F54C, 0x1F54D, 0x1F54E, 0x1F550, 0x1F551, 0x1F552, 0x1F553, 0x1F554, 0x1F555, 0x1F556, 0x1F557, 0x1F558, 0x1F559, 0x1F55A, 0x1F55B, 0x1F55C, 0x1F55D, 0x1F55E, 0x1F55F, 0x1F560, 0x1F561, 0x1F562, 0x1F563, 0x1F564, 0x1F565, 0x1F566, 0x1F567, 0x1F56F, 0x1F570, 0x1F573, 0x1F574, 0x1F575, 0x1F576, 0x1F577, 0x1F578, 0x1F579, 0x1F57A, 0x1F587, 0x1F58A, 0x1F58B, 0x1F58C, 0x1F58D, 0x1F590, 0x1F595,
        0x1F596, 0x1F5A4, 0x1F5A5, 0x1F5A8, 0x1F5B1, 0x1F5B2, 0x1F5BC, 0x1F5C2, 0x1F5C3, 0x1F5C4, 0x1F5D1, 0x1F5D2, 0x1F5D3, 0x1F5DC, 0x1F5DD, 0x1F5DE, 0x1F5E1, 0x1F5E3, 0x1F5E8, 0x1F5EF, 0x1F5F3, 0x1F5FA, 0x1F5FB, 0x1F5FC, 0x1F5FD, 0x1F5FE, 0x1F5FF, 0x1F600, 0x1F601, 0x1F602, 0x1F603, 0x1F604, 0x1F605, 0x1F606, 0x1F607, 0x1F608, 0x1F609, 0x1F60A, 0x1F60B, 0x1F60C, 0x1F60D, 0x1F60E, 0x1F60F, 0x1F610, 0x1F611, 0x1F612, 0x1F613, 0x1F614, 0x1F615, 0x1F616,
        0x1F617, 0x1F618, 0x1F619, 0x1F61A, 0x1F61B, 0x1F61C, 0x1F61D, 0x1F61E, 0x1F61F, 0x1F620, 0x1F621, 0x1F622, 0x1F623, 0x1F624, 0x1F625, 0x1F626, 0x1F627, 0x1F628, 0x1F629, 0x1F62A, 0x1F62B, 0x1F62C, 0x1F62D, 0x1F62E, 0x1F62F, 0x1F630, 0x1F631, 0x1F632, 0x1F633, 0x1F634, 0x1F635, 0x1F636, 0x1F637, 0x1F638, 0x1F639, 0x1F63A, 0x1F63B, 0x1F63C, 0x1F63D, 0x1F63E, 0x1F63F, 0x1F640, 0x1F641, 0x1F642, 0x1F643, 0x1F644, 0x1F645, 0x1F646, 0x1F647, 0x1F648,
        0x1F649, 0x1F64A, 0x1F64B, 0x1F64C, 0x1F64D, 0x1F64E, 0x1F64F, 0x1F680, 0x1F681, 0x1F682, 0x1F683, 0x1F684, 0x1F685, 0x1F686, 0x1F687, 0x1F688, 0x1F689, 0x1F68A, 0x1F68B, 0x1F68C, 0x1F68D, 0x1F68E, 0x1F68F, 0x1F690, 0x1F691, 0x1F692, 0x1F693, 0x1F694, 0x1F695, 0x1F696, 0x1F697, 0x1F698, 0x1F699, 0x1F69A, 0x1F69B, 0x1F69C, 0x1F69D, 0x1F69E, 0x1F69F, 0x1F6A0, 0x1F6A1, 0x1F6A2, 0x1F6A3, 0x1F6A4, 0x1F6A5, 0x1F6A6, 0x1F6A7, 0x1F6A8, 0x1F6A9, 0x1F6AA,
        0x1F6AB, 0x1F6AC, 0x1F6AD, 0x1F6AE, 0x1F6AF, 0x1F6B0, 0x1F6B1, 0x1F6B2, 0x1F6B3, 0x1F6B4, 0x1F6B5, 0x1F6B6, 0x1F6B7, 0x1F6B8, 0x1F6B9, 0x1F6BA, 0x1F6BB, 0x1F6BC, 0x1F6BD, 0x1F6BE, 0x1F6BF, 0x1F6C0, 0x1F6C1, 0x1F6C2, 0x1F6C3, 0x1F6C4, 0x1F6C5, 0x1F6CB, 0x1F6CC, 0x1F6CD, 0x1F6CE, 0x1F6CF, 0x1F6D0, 0x1F6D1, 0x1F6D2, 0x1F6D5, 0x1F6D6, 0x1F6D7, 0x1F6DC, 0x1F6DD, 0x1F6DE, 0x1F6DF, 0x1F6E0, 0x1F6E1, 0x1F6E2, 0x1F6E3, 0x1F6E4, 0x1F6E5, 0x1F6E9, 0x1F6EB,
        0x1F6EC, 0x1F6F0, 0x1F6F3, 0x1F6F4, 0x1F6F5, 0x1F6F6, 0x1F6F7, 0x1F6F8, 0x1F6F9, 0x1F6FA, 0x1F6FB, 0x1F6FC, 0x1F7E0, 0x1F7E1, 0x1F7E2, 0x1F7E3, 0x1F7E4, 0x1F7E5, 0x1F7E6, 0x1F7E7, 0x1F7E8, 0x1F7E9, 0x1F7EA, 0x1F7EB, 0x1F7F0, 0x1F90C, 0x1F90D, 0x1F90E, 0x1F90F, 0x1F910, 0x1F911, 0x1F912, 0x1F913, 0x1F914, 0x1F915, 0x1F916, 0x1F917, 0x1F918, 0x1F919, 0x1F91A, 0x1F91B, 0x1F91C, 0x1F91D, 0x1F91E, 0x1F91F, 0x1F920, 0x1F921, 0x1F922, 0x1F923, 0x1F924,
        0x1F925, 0x1F926, 0x1F927, 0x1F928, 0x1F929, 0x1F92A, 0x1F92B, 0x1F92C, 0x1F92D, 0x1F92E, 0x1F92F, 0x1F930, 0x1F931, 0x1F932, 0x1F933, 0x1F934, 0x1F935, 0x1F936, 0x1F937, 0x1F938, 0x1F939, 0x1F93A, 0x1F93C, 0x1F93D, 0x1F93E, 0x1F93F, 0x1F940, 0x1F941, 0x1F942, 0x1F943, 0x1F944, 0x1F945, 0x1F947, 0x1F948, 0x1F949, 0x1F94A, 0x1F94B, 0x1F94C, 0x1F94D, 0x1F94E, 0x1F94F, 0x1F950, 0x1F951, 0x1F952, 0x1F953, 0x1F954, 0x1F955, 0x1F956, 0x1F957, 0x1F958,
        0x1F959, 0x1F95A, 0x1F95B, 0x1F95C, 0x1F95D, 0x1F95E, 0x1F95F, 0x1F960, 0x1F961, 0x1F962, 0x1F963, 0x1F964, 0x1F965, 0x1F966, 0x1F967, 0x1F968, 0x1F969, 0x1F96A, 0x1F96B, 0x1F96C, 0x1F96D, 0x1F96E, 0x1F96F, 0x1F970, 0x1F971, 0x1F972, 0x1F973, 0x1F974, 0x1F975, 0x1F976, 0x1F977, 0x1F978, 0x1F979, 0x1F97A, 0x1F97B, 0x1F97C, 0x1F97D, 0x1F97E, 0x1F97F, 0x1F980, 0x1F981, 0x1F982, 0x1F983, 0x1F984, 0x1F985, 0x1F986, 0x1F987, 0x1F988, 0x1F989, 0x1F98A,
        0x1F98B, 0x1F98C, 0x1F98D, 0x1F98E, 0x1F98F, 0x1F990, 0x1F991, 0x1F992, 0x1F993, 0x1F994, 0x1F995, 0x1F996, 0x1F997, 0x1F998, 0x1F999, 0x1F99A, 0x1F99B, 0x1F99C, 0x1F99D, 0x1F99E, 0x1F99F, 0x1F9A0, 0x1F9A1, 0x1F9A2, 0x1F9A3, 0x1F9A4, 0x1F9A5, 0x1F9A6, 0x1F9A7, 0x1F9A8, 0x1F9A9, 0x1F9AA, 0x1F9AB, 0x1F9AC, 0x1F9AD, 0x1F9AE, 0x1F9AF, 0x1F9B0, 0x1F9B1, 0x1F9B2, 0x1F9B3, 0x1F9B4, 0x1F9B5, 0x1F9B6, 0x1F9B7, 0x1F9B8, 0x1F9B9, 0x1F9BA, 0x1F9BB, 0x1F9BC,
        0x1F9BD, 0x1F9BE, 0x1F9BF, 0x1F9C0, 0x1F9C1, 0x1F9C2, 0x1F9C3, 0x1F9C4, 0x1F9C5, 0x1F9C6, 0x1F9C7, 0x1F9C8, 0x1F9C9, 0x1F9CA, 0x1F9CB, 0x1F9CC, 0x1F9CD, 0x1F9CE, 0x1F9CF, 0x1F9D0, 0x1F9D1, 0x1F9D2, 0x1F9D3, 0x1F9D4, 0x1F9D5, 0x1F9D6, 0x1F9D7, 0x1F9D8, 0x1F9D9, 0x1F9DA, 0x1F9DB, 0x1F9DC, 0x1F9DD, 0x1F9DE, 0x1F9DF, 0x1F9E0, 0x1F9E1, 0x1F9E2, 0x1F9E3, 0x1F9E4, 0x1F9E5, 0x1F9E6, 0x1F9E7, 0x1F9E8, 0x1F9E9, 0x1F9EA, 0x1F9EB, 0x1F9EC, 0x1F9ED, 0x1F9EE,
        0x1F9EF, 0x1F9F0, 0x1F9F1, 0x1F9F2, 0x1F9F3, 0x1F9F4, 0x1F9F5, 0x1F9F6, 0x1F9F7, 0x1F9F8, 0x1F9F9, 0x1F9FA, 0x1F9FB, 0x1F9FC, 0x1F9FD, 0x1F9FE, 0x1F9FF, 0x1FA70, 0x1FA71, 0x1FA72, 0x1FA73, 0x1FA74, 0x1FA75, 0x1FA76, 0x1FA77, 0x1FA78, 0x1FA79, 0x1FA7A, 0x1FA7B, 0x1FA7C, 0x1FA80, 0x1FA81, 0x1FA82, 0x1FA83, 0x1FA84, 0x1FA85, 0x1FA86, 0x1FA87, 0x1FA88, 0x1FA89, 0x1FA8F, 0x1FA90, 0x1FA91, 0x1FA92, 0x1FA93, 0x1FA94, 0x1FA95, 0x1FA96, 0x1FA97, 0x1FA98,
        0x1FA99, 0x1FA9A, 0x1FA9B, 0x1FA9C, 0x1FA9D, 0x1FA9E, 0x1FA9F, 0x1FAA0, 0x1FAA1, 0x1FAA2, 0x1FAA3, 0x1FAA4, 0x1FAA5, 0x1FAA6, 0x1FAA7, 0x1FAA8, 0x1FAA9, 0x1FAAA, 0x1FAAB, 0x1FAAC, 0x1FAAD, 0x1FAAE, 0x1FAAF, 0x1FAB0, 0x1FAB1, 0x1FAB2, 0x1FAB3, 0x1FAB4, 0x1FAB5, 0x1FAB6, 0x1FAB7, 0x1FAB8, 0x1FAB9, 0x1FABA, 0x1FABB, 0x1FABC, 0x1FABD, 0x1FABE, 0x1FABF, 0x1FAC0, 0x1FAC1, 0x1FAC2, 0x1FAC3, 0x1FAC4, 0x1FAC5, 0x1FAC6, 0x1FACE, 0x1FACF, 0x1FAD0, 0x1FAD1,
        0x1FAD2, 0x1FAD3, 0x1FAD4, 0x1FAD5, 0x1FAD6, 0x1FAD7, 0x1FAD8, 0x1FAD9, 0x1FADA, 0x1FADB, 0x1FADC, 0x1FADF, 0x1FAE0, 0x1FAE1, 0x1FAE2, 0x1FAE3, 0x1FAE4, 0x1FAE5, 0x1FAE6, 0x1FAE7, 0x1FAE8, 0x1FAE9, 0x1FAF0, 0x1FAF1, 0x1FAF2, 0x1FAF3, 0x1FAF4, 0x1FAF5, 0x1FAF6, 0x1FAF7, 0x1FAF8 };

        static readonly HashSet<uint> k_EmojiPresentationFormLookup = new HashSet<uint>() {
        0x231A, 0x231B, 0x23E9, 0x23EA, 0x23EB, 0x23EC, 0x23F0, 0x23F3, 0x25FD, 0x25FE, 0x2614, 0x2615, 0x2648, 0x2649, 0x264A, 0x264B, 0x264C, 0x264D, 0x264E, 0x264F, 0x2650, 0x2651, 0x2652, 0x2653, 0x267F, 0x2693, 0x26A1, 0x26AA, 0x26AB, 0x26BD, 0x26BE, 0x26C4, 0x26C5, 0x26CE, 0x26D4, 0x26EA, 0x26F2, 0x26F3, 0x26F5, 0x26FA, 0x26FD, 0x2705, 0x270A, 0x270B, 0x2728, 0x274C, 0x274E, 0x2753, 0x2754, 0x2755,
        0x2757, 0x2795, 0x2796, 0x2797, 0x27B0, 0x27BF, 0x2B1B, 0x2B1C, 0x2B50, 0x2B55, 0x1F004, 0x1F0CF, 0x1F18E, 0x1F191, 0x1F192, 0x1F193, 0x1F194, 0x1F195, 0x1F196, 0x1F197, 0x1F198, 0x1F199, 0x1F19A, 0x1F1E6, 0x1F1E7, 0x1F1E8, 0x1F1E9, 0x1F1EA, 0x1F1EB, 0x1F1EC, 0x1F1ED, 0x1F1EE, 0x1F1EF, 0x1F1F0, 0x1F1F1, 0x1F1F2, 0x1F1F3, 0x1F1F4, 0x1F1F5, 0x1F1F6, 0x1F1F7, 0x1F1F8, 0x1F1F9, 0x1F1FA, 0x1F1FB, 0x1F1FC, 0x1F1FD, 0x1F1FE, 0x1F1FF, 0x1F201,
        0x1F21A, 0x1F22F, 0x1F232, 0x1F233, 0x1F234, 0x1F235, 0x1F236, 0x1F238, 0x1F239, 0x1F23A, 0x1F250, 0x1F251, 0x1F300, 0x1F301, 0x1F302, 0x1F303, 0x1F304, 0x1F305, 0x1F306, 0x1F307, 0x1F308, 0x1F309, 0x1F30A, 0x1F30B, 0x1F30C, 0x1F30D, 0x1F30E, 0x1F30F, 0x1F310, 0x1F311, 0x1F312, 0x1F313, 0x1F314, 0x1F315, 0x1F316, 0x1F317, 0x1F318, 0x1F319, 0x1F31A, 0x1F31B, 0x1F31C, 0x1F31D, 0x1F31E, 0x1F31F, 0x1F320, 0x1F32D, 0x1F32E, 0x1F32F, 0x1F330, 0x1F331,
        0x1F332, 0x1F333, 0x1F334, 0x1F335, 0x1F337, 0x1F338, 0x1F339, 0x1F33A, 0x1F33B, 0x1F33C, 0x1F33D, 0x1F33E, 0x1F33F, 0x1F340, 0x1F341, 0x1F342, 0x1F343, 0x1F344, 0x1F345, 0x1F346, 0x1F347, 0x1F348, 0x1F349, 0x1F34A, 0x1F34B, 0x1F34C, 0x1F34D, 0x1F34E, 0x1F34F, 0x1F350, 0x1F351, 0x1F352, 0x1F353, 0x1F354, 0x1F355, 0x1F356, 0x1F357, 0x1F358, 0x1F359, 0x1F35A, 0x1F35B, 0x1F35C, 0x1F35D, 0x1F35E, 0x1F35F, 0x1F360, 0x1F361, 0x1F362, 0x1F363, 0x1F364,
        0x1F365, 0x1F366, 0x1F367, 0x1F368, 0x1F369, 0x1F36A, 0x1F36B, 0x1F36C, 0x1F36D, 0x1F36E, 0x1F36F, 0x1F370, 0x1F371, 0x1F372, 0x1F373, 0x1F374, 0x1F375, 0x1F376, 0x1F377, 0x1F378, 0x1F379, 0x1F37A, 0x1F37B, 0x1F37C, 0x1F37E, 0x1F37F, 0x1F380, 0x1F381, 0x1F382, 0x1F383, 0x1F384, 0x1F385, 0x1F386, 0x1F387, 0x1F388, 0x1F389, 0x1F38A, 0x1F38B, 0x1F38C, 0x1F38D, 0x1F38E, 0x1F38F, 0x1F390, 0x1F391, 0x1F392, 0x1F393, 0x1F3A0, 0x1F3A1, 0x1F3A2, 0x1F3A3,
        0x1F3A4, 0x1F3A5, 0x1F3A6, 0x1F3A7, 0x1F3A8, 0x1F3A9, 0x1F3AA, 0x1F3AB, 0x1F3AC, 0x1F3AD, 0x1F3AE, 0x1F3AF, 0x1F3B0, 0x1F3B1, 0x1F3B2, 0x1F3B3, 0x1F3B4, 0x1F3B5, 0x1F3B6, 0x1F3B7, 0x1F3B8, 0x1F3B9, 0x1F3BA, 0x1F3BB, 0x1F3BC, 0x1F3BD, 0x1F3BE, 0x1F3BF, 0x1F3C0, 0x1F3C1, 0x1F3C2, 0x1F3C3, 0x1F3C4, 0x1F3C5, 0x1F3C6, 0x1F3C7, 0x1F3C8, 0x1F3C9, 0x1F3CA, 0x1F3CF, 0x1F3D0, 0x1F3D1, 0x1F3D2, 0x1F3D3, 0x1F3E0, 0x1F3E1, 0x1F3E2, 0x1F3E3, 0x1F3E4, 0x1F3E5,
        0x1F3E6, 0x1F3E7, 0x1F3E8, 0x1F3E9, 0x1F3EA, 0x1F3EB, 0x1F3EC, 0x1F3ED, 0x1F3EE, 0x1F3EF, 0x1F3F0, 0x1F3F4, 0x1F3F8, 0x1F3F9, 0x1F3FA, 0x1F3FB, 0x1F3FC, 0x1F3FD, 0x1F3FE, 0x1F3FF, 0x1F400, 0x1F401, 0x1F402, 0x1F403, 0x1F404, 0x1F405, 0x1F406, 0x1F407, 0x1F408, 0x1F409, 0x1F40A, 0x1F40B, 0x1F40C, 0x1F40D, 0x1F40E, 0x1F40F, 0x1F410, 0x1F411, 0x1F412, 0x1F413, 0x1F414, 0x1F415, 0x1F416, 0x1F417, 0x1F418, 0x1F419, 0x1F41A, 0x1F41B, 0x1F41C, 0x1F41D,
        0x1F41E, 0x1F41F, 0x1F420, 0x1F421, 0x1F422, 0x1F423, 0x1F424, 0x1F425, 0x1F426, 0x1F427, 0x1F428, 0x1F429, 0x1F42A, 0x1F42B, 0x1F42C, 0x1F42D, 0x1F42E, 0x1F42F, 0x1F430, 0x1F431, 0x1F432, 0x1F433, 0x1F434, 0x1F435, 0x1F436, 0x1F437, 0x1F438, 0x1F439, 0x1F43A, 0x1F43B, 0x1F43C, 0x1F43D, 0x1F43E, 0x1F440, 0x1F442, 0x1F443, 0x1F444, 0x1F445, 0x1F446, 0x1F447, 0x1F448, 0x1F449, 0x1F44A, 0x1F44B, 0x1F44C, 0x1F44D, 0x1F44E, 0x1F44F, 0x1F450, 0x1F451,
        0x1F452, 0x1F453, 0x1F454, 0x1F455, 0x1F456, 0x1F457, 0x1F458, 0x1F459, 0x1F45A, 0x1F45B, 0x1F45C, 0x1F45D, 0x1F45E, 0x1F45F, 0x1F460, 0x1F461, 0x1F462, 0x1F463, 0x1F464, 0x1F465, 0x1F466, 0x1F467, 0x1F468, 0x1F469, 0x1F46A, 0x1F46B, 0x1F46C, 0x1F46D, 0x1F46E, 0x1F46F, 0x1F470, 0x1F471, 0x1F472, 0x1F473, 0x1F474, 0x1F475, 0x1F476, 0x1F477, 0x1F478, 0x1F479, 0x1F47A, 0x1F47B, 0x1F47C, 0x1F47D, 0x1F47E, 0x1F47F, 0x1F480, 0x1F481, 0x1F482, 0x1F483,
        0x1F484, 0x1F485, 0x1F486, 0x1F487, 0x1F488, 0x1F489, 0x1F48A, 0x1F48B, 0x1F48C, 0x1F48D, 0x1F48E, 0x1F48F, 0x1F490, 0x1F491, 0x1F492, 0x1F493, 0x1F494, 0x1F495, 0x1F496, 0x1F497, 0x1F498, 0x1F499, 0x1F49A, 0x1F49B, 0x1F49C, 0x1F49D, 0x1F49E, 0x1F49F, 0x1F4A0, 0x1F4A1, 0x1F4A2, 0x1F4A3, 0x1F4A4, 0x1F4A5, 0x1F4A6, 0x1F4A7, 0x1F4A8, 0x1F4A9, 0x1F4AA, 0x1F4AB, 0x1F4AC, 0x1F4AD, 0x1F4AE, 0x1F4AF, 0x1F4B0, 0x1F4B1, 0x1F4B2, 0x1F4B3, 0x1F4B4, 0x1F4B5,
        0x1F4B6, 0x1F4B7, 0x1F4B8, 0x1F4B9, 0x1F4BA, 0x1F4BB, 0x1F4BC, 0x1F4BD, 0x1F4BE, 0x1F4BF, 0x1F4C0, 0x1F4C1, 0x1F4C2, 0x1F4C3, 0x1F4C4, 0x1F4C5, 0x1F4C6, 0x1F4C7, 0x1F4C8, 0x1F4C9, 0x1F4CA, 0x1F4CB, 0x1F4CC, 0x1F4CD, 0x1F4CE, 0x1F4CF, 0x1F4D0, 0x1F4D1, 0x1F4D2, 0x1F4D3, 0x1F4D4, 0x1F4D5, 0x1F4D6, 0x1F4D7, 0x1F4D8, 0x1F4D9, 0x1F4DA, 0x1F4DB, 0x1F4DC, 0x1F4DD, 0x1F4DE, 0x1F4DF, 0x1F4E0, 0x1F4E1, 0x1F4E2, 0x1F4E3, 0x1F4E4, 0x1F4E5, 0x1F4E6, 0x1F4E7,
        0x1F4E8, 0x1F4E9, 0x1F4EA, 0x1F4EB, 0x1F4EC, 0x1F4ED, 0x1F4EE, 0x1F4EF, 0x1F4F0, 0x1F4F1, 0x1F4F2, 0x1F4F3, 0x1F4F4, 0x1F4F5, 0x1F4F6, 0x1F4F7, 0x1F4F8, 0x1F4F9, 0x1F4FA, 0x1F4FB, 0x1F4FC, 0x1F4FF, 0x1F500, 0x1F501, 0x1F502, 0x1F503, 0x1F504, 0x1F505, 0x1F506, 0x1F507, 0x1F508, 0x1F509, 0x1F50A, 0x1F50B, 0x1F50C, 0x1F50D, 0x1F50E, 0x1F50F, 0x1F510, 0x1F511, 0x1F512, 0x1F513, 0x1F514, 0x1F515, 0x1F516, 0x1F517, 0x1F518, 0x1F519, 0x1F51A, 0x1F51B,
        0x1F51C, 0x1F51D, 0x1F51E, 0x1F51F, 0x1F520, 0x1F521, 0x1F522, 0x1F523, 0x1F524, 0x1F525, 0x1F526, 0x1F527, 0x1F528, 0x1F529, 0x1F52A, 0x1F52B, 0x1F52C, 0x1F52D, 0x1F52E, 0x1F52F, 0x1F530, 0x1F531, 0x1F532, 0x1F533, 0x1F534, 0x1F535, 0x1F536, 0x1F537, 0x1F538, 0x1F539, 0x1F53A, 0x1F53B, 0x1F53C, 0x1F53D, 0x1F54B, 0x1F54C, 0x1F54D, 0x1F54E, 0x1F550, 0x1F551, 0x1F552, 0x1F553, 0x1F554, 0x1F555, 0x1F556, 0x1F557, 0x1F558, 0x1F559, 0x1F55A, 0x1F55B,
        0x1F55C, 0x1F55D, 0x1F55E, 0x1F55F, 0x1F560, 0x1F561, 0x1F562, 0x1F563, 0x1F564, 0x1F565, 0x1F566, 0x1F567, 0x1F57A, 0x1F595, 0x1F596, 0x1F5A4, 0x1F5FB, 0x1F5FC, 0x1F5FD, 0x1F5FE, 0x1F5FF, 0x1F600, 0x1F601, 0x1F602, 0x1F603, 0x1F604, 0x1F605, 0x1F606, 0x1F607, 0x1F608, 0x1F609, 0x1F60A, 0x1F60B, 0x1F60C, 0x1F60D, 0x1F60E, 0x1F60F, 0x1F610, 0x1F611, 0x1F612, 0x1F613, 0x1F614, 0x1F615, 0x1F616, 0x1F617, 0x1F618, 0x1F619, 0x1F61A, 0x1F61B, 0x1F61C,
        0x1F61D, 0x1F61E, 0x1F61F, 0x1F620, 0x1F621, 0x1F622, 0x1F623, 0x1F624, 0x1F625, 0x1F626, 0x1F627, 0x1F628, 0x1F629, 0x1F62A, 0x1F62B, 0x1F62C, 0x1F62D, 0x1F62E, 0x1F62F, 0x1F630, 0x1F631, 0x1F632, 0x1F633, 0x1F634, 0x1F635, 0x1F636, 0x1F637, 0x1F638, 0x1F639, 0x1F63A, 0x1F63B, 0x1F63C, 0x1F63D, 0x1F63E, 0x1F63F, 0x1F640, 0x1F641, 0x1F642, 0x1F643, 0x1F644, 0x1F645, 0x1F646, 0x1F647, 0x1F648, 0x1F649, 0x1F64A, 0x1F64B, 0x1F64C, 0x1F64D, 0x1F64E,
        0x1F64F, 0x1F680, 0x1F681, 0x1F682, 0x1F683, 0x1F684, 0x1F685, 0x1F686, 0x1F687, 0x1F688, 0x1F689, 0x1F68A, 0x1F68B, 0x1F68C, 0x1F68D, 0x1F68E, 0x1F68F, 0x1F690, 0x1F691, 0x1F692, 0x1F693, 0x1F694, 0x1F695, 0x1F696, 0x1F697, 0x1F698, 0x1F699, 0x1F69A, 0x1F69B, 0x1F69C, 0x1F69D, 0x1F69E, 0x1F69F, 0x1F6A0, 0x1F6A1, 0x1F6A2, 0x1F6A3, 0x1F6A4, 0x1F6A5, 0x1F6A6, 0x1F6A7, 0x1F6A8, 0x1F6A9, 0x1F6AA, 0x1F6AB, 0x1F6AC, 0x1F6AD, 0x1F6AE, 0x1F6AF, 0x1F6B0,
        0x1F6B1, 0x1F6B2, 0x1F6B3, 0x1F6B4, 0x1F6B5, 0x1F6B6, 0x1F6B7, 0x1F6B8, 0x1F6B9, 0x1F6BA, 0x1F6BB, 0x1F6BC, 0x1F6BD, 0x1F6BE, 0x1F6BF, 0x1F6C0, 0x1F6C1, 0x1F6C2, 0x1F6C3, 0x1F6C4, 0x1F6C5, 0x1F6CC, 0x1F6D0, 0x1F6D1, 0x1F6D2, 0x1F6D5, 0x1F6D6, 0x1F6D7, 0x1F6DC, 0x1F6DD, 0x1F6DE, 0x1F6DF, 0x1F6EB, 0x1F6EC, 0x1F6F4, 0x1F6F5, 0x1F6F6, 0x1F6F7, 0x1F6F8, 0x1F6F9, 0x1F6FA, 0x1F6FB, 0x1F6FC, 0x1F7E0, 0x1F7E1, 0x1F7E2, 0x1F7E3, 0x1F7E4, 0x1F7E5, 0x1F7E6,
        0x1F7E7, 0x1F7E8, 0x1F7E9, 0x1F7EA, 0x1F7EB, 0x1F7F0, 0x1F90C, 0x1F90D, 0x1F90E, 0x1F90F, 0x1F910, 0x1F911, 0x1F912, 0x1F913, 0x1F914, 0x1F915, 0x1F916, 0x1F917, 0x1F918, 0x1F919, 0x1F91A, 0x1F91B, 0x1F91C, 0x1F91D, 0x1F91E, 0x1F91F, 0x1F920, 0x1F921, 0x1F922, 0x1F923, 0x1F924, 0x1F925, 0x1F926, 0x1F927, 0x1F928, 0x1F929, 0x1F92A, 0x1F92B, 0x1F92C, 0x1F92D, 0x1F92E, 0x1F92F, 0x1F930, 0x1F931, 0x1F932, 0x1F933, 0x1F934, 0x1F935, 0x1F936, 0x1F937,
        0x1F938, 0x1F939, 0x1F93A, 0x1F93C, 0x1F93D, 0x1F93E, 0x1F93F, 0x1F940, 0x1F941, 0x1F942, 0x1F943, 0x1F944, 0x1F945, 0x1F947, 0x1F948, 0x1F949, 0x1F94A, 0x1F94B, 0x1F94C, 0x1F94D, 0x1F94E, 0x1F94F, 0x1F950, 0x1F951, 0x1F952, 0x1F953, 0x1F954, 0x1F955, 0x1F956, 0x1F957, 0x1F958, 0x1F959, 0x1F95A, 0x1F95B, 0x1F95C, 0x1F95D, 0x1F95E, 0x1F95F, 0x1F960, 0x1F961, 0x1F962, 0x1F963, 0x1F964, 0x1F965, 0x1F966, 0x1F967, 0x1F968, 0x1F969, 0x1F96A, 0x1F96B,
        0x1F96C, 0x1F96D, 0x1F96E, 0x1F96F, 0x1F970, 0x1F971, 0x1F972, 0x1F973, 0x1F974, 0x1F975, 0x1F976, 0x1F977, 0x1F978, 0x1F979, 0x1F97A, 0x1F97B, 0x1F97C, 0x1F97D, 0x1F97E, 0x1F97F, 0x1F980, 0x1F981, 0x1F982, 0x1F983, 0x1F984, 0x1F985, 0x1F986, 0x1F987, 0x1F988, 0x1F989, 0x1F98A, 0x1F98B, 0x1F98C, 0x1F98D, 0x1F98E, 0x1F98F, 0x1F990, 0x1F991, 0x1F992, 0x1F993, 0x1F994, 0x1F995, 0x1F996, 0x1F997, 0x1F998, 0x1F999, 0x1F99A, 0x1F99B, 0x1F99C, 0x1F99D,
        0x1F99E, 0x1F99F, 0x1F9A0, 0x1F9A1, 0x1F9A2, 0x1F9A3, 0x1F9A4, 0x1F9A5, 0x1F9A6, 0x1F9A7, 0x1F9A8, 0x1F9A9, 0x1F9AA, 0x1F9AB, 0x1F9AC, 0x1F9AD, 0x1F9AE, 0x1F9AF, 0x1F9B0, 0x1F9B1, 0x1F9B2, 0x1F9B3, 0x1F9B4, 0x1F9B5, 0x1F9B6, 0x1F9B7, 0x1F9B8, 0x1F9B9, 0x1F9BA, 0x1F9BB, 0x1F9BC, 0x1F9BD, 0x1F9BE, 0x1F9BF, 0x1F9C0, 0x1F9C1, 0x1F9C2, 0x1F9C3, 0x1F9C4, 0x1F9C5, 0x1F9C6, 0x1F9C7, 0x1F9C8, 0x1F9C9, 0x1F9CA, 0x1F9CB, 0x1F9CC, 0x1F9CD, 0x1F9CE, 0x1F9CF,
        0x1F9D0, 0x1F9D1, 0x1F9D2, 0x1F9D3, 0x1F9D4, 0x1F9D5, 0x1F9D6, 0x1F9D7, 0x1F9D8, 0x1F9D9, 0x1F9DA, 0x1F9DB, 0x1F9DC, 0x1F9DD, 0x1F9DE, 0x1F9DF, 0x1F9E0, 0x1F9E1, 0x1F9E2, 0x1F9E3, 0x1F9E4, 0x1F9E5, 0x1F9E6, 0x1F9E7, 0x1F9E8, 0x1F9E9, 0x1F9EA, 0x1F9EB, 0x1F9EC, 0x1F9ED, 0x1F9EE, 0x1F9EF, 0x1F9F0, 0x1F9F1, 0x1F9F2, 0x1F9F3, 0x1F9F4, 0x1F9F5, 0x1F9F6, 0x1F9F7, 0x1F9F8, 0x1F9F9, 0x1F9FA, 0x1F9FB, 0x1F9FC, 0x1F9FD, 0x1F9FE, 0x1F9FF, 0x1FA70, 0x1FA71,
        0x1FA72, 0x1FA73, 0x1FA74, 0x1FA75, 0x1FA76, 0x1FA77, 0x1FA78, 0x1FA79, 0x1FA7A, 0x1FA7B, 0x1FA7C, 0x1FA80, 0x1FA81, 0x1FA82, 0x1FA83, 0x1FA84, 0x1FA85, 0x1FA86, 0x1FA87, 0x1FA88, 0x1FA89, 0x1FA8F, 0x1FA90, 0x1FA91, 0x1FA92, 0x1FA93, 0x1FA94, 0x1FA95, 0x1FA96, 0x1FA97, 0x1FA98, 0x1FA99, 0x1FA9A, 0x1FA9B, 0x1FA9C, 0x1FA9D, 0x1FA9E, 0x1FA9F, 0x1FAA0, 0x1FAA1, 0x1FAA2, 0x1FAA3, 0x1FAA4, 0x1FAA5, 0x1FAA6, 0x1FAA7, 0x1FAA8, 0x1FAA9, 0x1FAAA, 0x1FAAB,
        0x1FAAC, 0x1FAAD, 0x1FAAE, 0x1FAAF, 0x1FAB0, 0x1FAB1, 0x1FAB2, 0x1FAB3, 0x1FAB4, 0x1FAB5, 0x1FAB6, 0x1FAB7, 0x1FAB8, 0x1FAB9, 0x1FABA, 0x1FABB, 0x1FABC, 0x1FABD, 0x1FABE, 0x1FABF, 0x1FAC0, 0x1FAC1, 0x1FAC2, 0x1FAC3, 0x1FAC4, 0x1FAC5, 0x1FAC6, 0x1FACE, 0x1FACF, 0x1FAD0, 0x1FAD1, 0x1FAD2, 0x1FAD3, 0x1FAD4, 0x1FAD5, 0x1FAD6, 0x1FAD7, 0x1FAD8, 0x1FAD9, 0x1FADA, 0x1FADB, 0x1FADC, 0x1FADF, 0x1FAE0, 0x1FAE1, 0x1FAE2, 0x1FAE3, 0x1FAE4, 0x1FAE5, 0x1FAE6,
        0x1FAE7, 0x1FAE8, 0x1FAE9, 0x1FAF0, 0x1FAF1, 0x1FAF2, 0x1FAF3, 0x1FAF4, 0x1FAF5, 0x1FAF6, 0x1FAF7, 0x1FAF8 };

        /// <summary>
        ///
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        internal static bool IsEmoji(uint c)
        {
            return k_EmojiLookup.Contains(c);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        internal static bool IsEmojiPresentationForm(uint c)
        {
            return k_EmojiPresentationFormLookup.Contains(c);
        }

        internal static bool IsHangul(uint c)
        {
            return c >= 0x1100 && c <= 0x11ff || /* Hangul Jamo */
                   c >= 0xA960 && c <= 0xA97F || /* Hangul Jamo Extended-A */
                   c >= 0xD7B0 && c <= 0xD7FF || /* Hangul Jamo Extended-B */
                   c >= 0x3130 && c <= 0x318F || /* Hangul Compatibility Jamo */
                   c >= 0xFFA0 && c <= 0xFFDC || /* Halfwidth Jamo */
                   c >= 0xAC00 && c <= 0xD7AF;   /* Hangul Syllables */
        }

        internal static bool IsCJK(uint c)
        {
            return
                    c >= 0x3000  && c <= 0x303F  || /* CJK Symbols and Punctuation */
                    c >= 0x16FE0 && c <= 0x16FF  || /* CJK Ideographic Symbols and Punctuation */
                    c >= 0x3100  && c <= 0x312F  || /* Bopomofo */
                    c >= 0x31A0  && c <= 0x31BF  || /* Bopomofo Extended */
                    c >= 0x4E00  && c <= 0x9FFF  || /* CJK Unified Ideographs (Han) */
                    c >= 0x3400  && c <= 0x4DBF  || /* CJK Extension A */
                    c >= 0x20000 && c <= 0x2A6DF || /* CJK Extension B */
                    c >= 0x2A700 && c <= 0x2B73F || /* CJK Extension C */
                    c >= 0x2B740 && c <= 0x2B81F || /* CJK Extension D */
                    c >= 0x2B820 && c <= 0x2CEAF || /* CJK Extension E */
                    c >= 0x2CEB0 && c <= 0x2EBE0 || /* CJK Extension F */
                    c >= 0x30000 && c <= 0x3134A || /* CJK Extension G */
                    c >= 0xF900  && c <= 0xFAFF  || /* CJK Compatibility Ideographs */
                    c >= 0x2F800 && c <= 0x2FA1F || /* CJK Compatibility Ideographs Supplement */
                    c >= 0x2F00  && c <= 0x2FDF  || /* CJK Radicals / Kangxi Radicals */
                    c >= 0x2E80  && c <= 0x2EFF  || /* CJK Radicals Supplement */
                    c >= 0x31C0  && c <= 0x31EF  || /* CJK Strokes */
                    c >= 0x2FF0  && c <= 0x2FFF  || /* Ideographic Description Characters */
                    c >= 0x3040  && c <= 0x309F  || /* Hiragana */
                    c >= 0x1B100 && c <= 0x1B12F || /* Kana Extended-A */
                    c >= 0x1AFF0 && c <= 0x1AFFF || /* Kana Extended-B */
                    c >= 0x1B000 && c <= 0x1B0FF || /* Kana Supplement */
                    c >= 0x1B130 && c <= 0x1B16F || /* Small Kana Extension */
                    c >= 0x3190  && c <= 0x319F  || /* Kanbun */
                    c >= 0x30A0  && c <= 0x30FF  || /* Katakana */
                    c >= 0x31F0  && c <= 0x31FF  || /* Katakana Phonetic Extensions */
                    c >= 0xFF65  && c <= 0xFF9F;    /* Halfwidth Katakana */
        }
    }
}
