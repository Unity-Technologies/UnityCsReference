// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine.TextCore
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

    struct RichTextTagAttribute
    {
        public int nameHashCode;
        public int valueHashCode;
        public TagValueType valueType;
        public int valueStartIndex;
        public int valueLength;
    }

    // Structure used for Word Wrapping which tracks the state of execution when the last space or carriage return character was encountered.
    struct WordWrapState
    {
        public int previousWordBreak;
        public int totalCharacterCount;
        public int visibleCharacterCount;
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
        public float previousLineAscender;

        public float xAdvance;
        public float preferredWidth;
        public float preferredHeight;
        public float previousLineScale;

        public int wordCount;
        public FontStyles fontStyle;
        public float fontScale;
        public float fontScaleMultiplier;

        public float currentFontSize;
        public float baselineOffset;
        public float lineOffset;

        public TextInfo textInfo;
        public LineInfo lineInfo;

        public Color32 vertexColor;
        public Color32 underlineColor;
        public Color32 strikethroughColor;
        public Color32 highlightColor;
        public FontStyleStack basicStyleStack;
        public RichTextTagStack<Color32> colorStack;
        public RichTextTagStack<Color32> underlineColorStack;
        public RichTextTagStack<Color32> strikethroughColorStack;
        public RichTextTagStack<Color32> highlightColorStack;
        public RichTextTagStack<TextGradientPreset> colorGradientStack;
        public RichTextTagStack<float> sizeStack;
        public RichTextTagStack<float> indentStack;
        public RichTextTagStack<FontWeight> fontWeightStack;
        public RichTextTagStack<int> styleStack;
        public RichTextTagStack<float> baselineStack;
        public RichTextTagStack<int> actionStack;
        public RichTextTagStack<MaterialReference> materialReferenceStack;
        public RichTextTagStack<TextAlignment> lineJustificationStack;
        public int spriteAnimationId;

        public FontAsset currentFontAsset;
        public TextSpriteAsset currentSpriteAsset;
        public Material currentMaterial;
        public int currentMaterialIndex;

        public Extents meshExtents;

        public bool tagNoParsing;
        public bool isNonBreakingSpace;
    }

    internal static class TextGeneratorUtilities
    {
        public static readonly Vector2 largePositiveVector2 = new Vector2(2147483647, 2147483647);
        public static readonly Vector2 largeNegativeVector2 = new Vector2(-214748364, -214748364);
        public const float largePositiveFloat = 32767;
        public const float largeNegativeFloat = -32767;

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
        public static void StringToCharArray(string sourceText, ref int[] charBuffer, ref RichTextTagStack<int> styleStack, TextGenerationSettings generationSettings)
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
                        if (ReplaceOpeningStyleTag(ref sourceText, i, out srcOffset, ref charBuffer, ref writeIndex, ref styleStack))
                        {
                            i = srcOffset;
                            continue;
                        }
                    }
                    else if (IsTagName(ref sourceText, "</STYLE>", i))
                    {
                        ReplaceClosingStyleTag(ref charBuffer, ref writeIndex, ref styleStack);

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

        /// <summary>
        ///
        /// </summary>
        static void ResizeInternalArray<T>(ref T[] array)
        {
            int size = Mathf.NextPowerOfTwo(array.Length + 1);

            Array.Resize(ref array, size);
        }

        internal static void ResizeArray<T>(T[] array)
        {
            int size = array.Length * 2;
            if (size == 0) size = 8;

            System.Array.Resize(ref array, size);
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
        static bool ReplaceOpeningStyleTag(ref int[] sourceText, int srcIndex, out int srcOffset, ref int[] charBuffer, ref int writeIndex, ref RichTextTagStack<int> styleStack)
        {
            // Validate <style> tag.
            int hashCode = GetTagHashCode(ref sourceText, srcIndex + 7, out srcOffset);

            TextStyle style = TextStyleSheet.GetStyle(hashCode);

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
                        if (ReplaceOpeningStyleTag(ref openingTagArray, i, out offset, ref charBuffer, ref writeIndex, ref styleStack))
                        {
                            i = offset;
                            continue;
                        }
                    }
                    else if (IsTagName(ref openingTagArray, "</STYLE>", i))
                    {
                        ReplaceClosingStyleTag(ref charBuffer, ref writeIndex, ref styleStack);

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
        static bool ReplaceOpeningStyleTag(ref string sourceText, int srcIndex, out int srcOffset, ref int[] charBuffer, ref int writeIndex, ref RichTextTagStack<int> styleStack)
        {
            // Validate <style> tag.
            int hashCode = GetTagHashCode(ref sourceText, srcIndex + 7, out srcOffset);

            TextStyle style = TextStyleSheet.GetStyle(hashCode);

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
                        if (ReplaceOpeningStyleTag(ref openingTagArray, i, out offset, ref charBuffer, ref writeIndex, ref styleStack))
                        {
                            i = offset;
                            continue;
                        }
                    }
                    else if (IsTagName(ref openingTagArray, "</STYLE>", i))
                    {
                        ReplaceClosingStyleTag(ref charBuffer, ref writeIndex, ref styleStack);

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
        static void ReplaceClosingStyleTag(ref int[] charBuffer, ref int writeIndex, ref RichTextTagStack<int> styleStack)
        {
            // Get style from the Style Stack
            int hashCode = styleStack.CurrentItem();
            TextStyle style = TextStyleSheet.GetStyle(hashCode);

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
                        if (ReplaceOpeningStyleTag(ref closingTagArray, i, out offset, ref charBuffer, ref writeIndex, ref styleStack))
                        {
                            i = offset;
                            continue;
                        }
                    }
                    else if (IsTagName(ref closingTagArray, "</STYLE>", i))
                    {
                        ReplaceClosingStyleTag(ref charBuffer, ref writeIndex, ref styleStack);

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
        /// Convert UTF-32 Hex to Char
        /// </summary>
        /// <returns>The Unicode hex.</returns>
        /// <param name="text"></param>
        /// <param name="i">The index.</param>
        static int GetUtf32(string text, int i)
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
        static int GetUtf16(string text, int i)
        {
            int unicode = 0;
            unicode += HexToInt(text[i]) << 12;
            unicode += HexToInt(text[i + 1]) << 8;
            unicode += HexToInt(text[i + 2]) << 4;
            unicode += HexToInt(text[i + 3]);
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
        public static void FillCharacterVertexBuffers(int i, TextGenerationSettings generationSettings, TextInfo textInfo)
        {
            int materialIndex = textInfo.textElementInfo[i].materialReferenceIndex;
            int indexX4 = textInfo.meshInfo[materialIndex].vertexCount;

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
            textInfo.meshInfo[materialIndex].colors32[0 + indexX4] = textElementInfoArray[i].vertexBottomLeft.color;
            textInfo.meshInfo[materialIndex].colors32[1 + indexX4] = textElementInfoArray[i].vertexTopLeft.color;
            textInfo.meshInfo[materialIndex].colors32[2 + indexX4] = textElementInfoArray[i].vertexTopRight.color;
            textInfo.meshInfo[materialIndex].colors32[3 + indexX4] = textElementInfoArray[i].vertexBottomRight.color;

            textInfo.meshInfo[materialIndex].vertexCount = indexX4 + 4;
        }

        /// <summary>
        /// Fill Vertex Buffers for Sprites
        /// </summary>
        /// <param name="i"></param>
        /// <param name="generationSettings"></param>
        /// <param name="textInfo"></param>
        public static void FillSpriteVertexBuffers(int i, TextGenerationSettings generationSettings, TextInfo textInfo)
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
            textInfo.meshInfo[materialIndex].colors32[0 + indexX4] = textElementInfoArray[i].vertexBottomLeft.color;
            textInfo.meshInfo[materialIndex].colors32[1 + indexX4] = textElementInfoArray[i].vertexTopLeft.color;
            textInfo.meshInfo[materialIndex].colors32[2 + indexX4] = textElementInfoArray[i].vertexTopRight.color;
            textInfo.meshInfo[materialIndex].colors32[3 + indexX4] = textElementInfoArray[i].vertexBottomRight.color;

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

        public static FontStyles LegacyStyleToNewStyle(UnityEngine.FontStyle fontStyle)
        {
            switch (fontStyle)
            {
                case UnityEngine.FontStyle.Bold:
                    return FontStyles.Bold;
                case UnityEngine.FontStyle.Italic:
                    return FontStyles.Italic;
                case UnityEngine.FontStyle.BoldAndItalic:
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
    }
}
