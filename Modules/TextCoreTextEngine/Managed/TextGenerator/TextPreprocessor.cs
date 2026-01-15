// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine.Bindings;
using UnityEngine.TextCore.Text;

#nullable enable

namespace UnityEngine.TextCore
{
    [VisibleToOtherModules("UnityEngine.UIElementsModule")]
    internal static class TextPreprocessor
    {
        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static void PreProcessString(ref string text, PreProcessFlags flags, TextSettings? textSettings)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            // Replace style tags first if we have a defaultStyleSheet
            if (textSettings?.defaultStyleSheet != null && RichTextTagParser.ContainsStyleTags(text))
            {
                text = ReplaceStyleTags(text, textSettings);
            }

            if (flags == PreProcessFlags.None)
            {
                return;
            }

            bool collapseWhiteSpaces = (flags & PreProcessFlags.CollapseWhiteSpaces) != 0;
            bool parseEscapeSequences = (flags & PreProcessFlags.ParseEscapeSequences) != 0;

            // Early out if no special characters are found.
            PreProcessFlags contentFlags = PreProcessFlags.None;

            if (text.IndexOfAny(new[] { ' ', '\t', '\r', '\n', '\v' }) != -1)
                contentFlags |= PreProcessFlags.CollapseWhiteSpaces;

            if (text.IndexOf('\\') != -1)
                contentFlags |= PreProcessFlags.ParseEscapeSequences;

            if ((flags & contentFlags) == 0)
                return;

            var sb = new StringBuilder(text.Length);
            int readIndex = 0;
            bool lastCharWasSpace = true;

            while (readIndex < text.Length)
            {
                string charToProcess = "";

                char c = text[readIndex];
                if (parseEscapeSequences && c == '\\' && readIndex < text.Length - 1)
                {
                    readIndex++; // Consume the backslash
                    char escapeCode = text[readIndex];
                    bool success = true;

                    switch (escapeCode)
                    {
                        case '\\': charToProcess = "\\"; break;
                        case 'n': charToProcess = "\n"; break;
                        case 'r': charToProcess = "\r"; break;
                        case 't': charToProcess = "\t"; break;
                        case 'v': charToProcess = "\v"; break;

                        case 'u': // UTF-16
                            if (readIndex + 4 < text.Length)
                            {
                                string hex = text.Substring(readIndex + 1, 4);
                                if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint val))
                                {
                                    charToProcess = Convert.ToChar(val).ToString();
                                    readIndex += 4;
                                }
                                else
                                {
                                    success = false;
                                }
                            }
                            else
                            {
                                success = false;
                            }
                            break;
                        case 'U': // UTF-32
                            if (readIndex + 8 < text.Length)
                            {
                                string hex = text.Substring(readIndex + 1, 8);
                                if (uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint val))
                                {
                                    charToProcess = char.ConvertFromUtf32((int)val);
                                    readIndex += 8;
                                }
                                else
                                {
                                    success = false;
                                }
                            }
                            else
                            {
                                success = false;
                            }
                            break;
                        default:
                            success = false;
                            break;
                    }

                    if (!success)
                    {
                        sb.Append('\\');
                        charToProcess = escapeCode.ToString();
                    }
                }
                else
                {
                    charToProcess = c.ToString();
                }

                bool isWhitespace = charToProcess.Length == 1 && char.IsWhiteSpace(charToProcess[0]);

                if (collapseWhiteSpaces && isWhitespace)
                {
                    if (charToProcess == "\n")
                    {
                        // If the last character we added was a normalized space,
                        // remove it before adding the newline.
                        if (sb.Length > 0 && sb[sb.Length - 1] == ' ')
                        {
                            sb.Length--;
                        }
                        sb.Append('\n');
                        lastCharWasSpace = true;
                    }
                    else if (!lastCharWasSpace)
                    {
                        sb.Append(' ');
                        lastCharWasSpace = true;
                    }
                }
                else
                {
                    sb.Append(charToProcess);
                    lastCharWasSpace = isWhitespace;
                }

                readIndex++;
            }

            if (collapseWhiteSpaces && sb.Length > 0)
            {
                int lastCharToKeep = sb.Length - 1;

                while (lastCharToKeep >= 0 && char.IsWhiteSpace(sb[lastCharToKeep]) && sb[lastCharToKeep] != '\n')
                {
                    lastCharToKeep--;
                }

                sb.Length = lastCharToKeep + 1;
            }

            text = sb.ToString();
        }

        const char k_DoubleQuotes = '"';
        const char k_GreaterThan = '>';
        const char k_LessThan = '<';
        const string k_StyleOpenTag = "<style=\"";
        const string k_StyleCloseTag = "</style>";

        static int GetStyleHashCode(ReadOnlySpan<char> text)
        {
            int hashCode = 0;

            for (int i = 0; i < text.Length; i++)
                hashCode = (hashCode << 5) + hashCode ^ TextUtilities.ToUpperFast(text[i]);

            return hashCode;
        }

        static TextStyle? GetStyle(TextSettings textSettings, int hashCode)
        {
            var styleSheet = textSettings.defaultStyleSheet;
            if (styleSheet == null)
                return null;

            return styleSheet.GetStyle(hashCode);
        }

        internal static string ReplaceStyleTags(string text, TextSettings textSettings)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            ReadOnlySpan<char> source = text.AsSpan();
            ReadOnlySpan<char> styleOpen = k_StyleOpenTag.AsSpan();
            ReadOnlySpan<char> styleClose = k_StyleCloseTag.AsSpan();

            StringBuilder sb = new StringBuilder(text.Length);
            List<TextStyle> styleStack = new List<TextStyle>(4);

            int readIndex = 0;

            while (true)
            {
                // Find the next '<' relative to current position
                ReadOnlySpan<char> remaining = source.Slice(readIndex);
                int nextTagRelative = remaining.IndexOf(k_LessThan);

                // If no more tags, append the rest and break
                if (nextTagRelative == -1)
                {
                    sb.Append(remaining);
                    break;
                }

                // Append text occurring before the tag
                if (nextTagRelative > 0)
                {
                    sb.Append(remaining.Slice(0, nextTagRelative));
                }

                // Advance absolute index to the '<'
                readIndex += nextTagRelative;
                remaining = source.Slice(readIndex);

                if (remaining.StartsWith(styleOpen))
                {
                    // We are at '<', opening tag len is 8.
                    // We need to find the closing quote '"' and the closing bracket '>'
                    int contentStart = styleOpen.Length;
                    int closingQuoteRelative = remaining.Slice(contentStart).IndexOf(k_DoubleQuotes);

                    if (closingQuoteRelative != -1)
                    {
                        // Calculate hash of the name between quotes
                        int nameHash = GetStyleHashCode(remaining.Slice(contentStart, closingQuoteRelative));

                        TextStyle? style = GetStyle(textSettings, nameHash);

                        if (style != null)
                        {
                            // Now find the closing '>' AFTER the quote
                            int afterQuoteIndex = contentStart + closingQuoteRelative + 1;
                            int closingBracketRelative = remaining.Slice(afterQuoteIndex).IndexOf(k_GreaterThan);

                            if (closingBracketRelative != -1)
                            {
                                // Valid Tag Found!
                                sb.Append(style.styleOpeningDefinition);
                                styleStack.Add(style);
                                readIndex += afterQuoteIndex + closingBracketRelative + 1;
                                continue;
                            }
                        }
                    }
                }
                else if (remaining.StartsWith(styleClose))
                {
                    if (styleStack.Count > 0)
                    {
                        // Pop the last style
                        int lastIndex = styleStack.Count - 1;
                        TextStyle style = styleStack[lastIndex];
                        styleStack.RemoveAt(lastIndex);

                        sb.Append(style.styleClosingDefinition);

                        readIndex += styleClose.Length;
                        continue;
                    }
                }

                // If we got here, it was a '<' but not a valid style tag.
                // Append the '<' and move forward by 1 so we don't process it again.
                sb.Append(k_LessThan);
                readIndex++;
            }

            return sb.ToString();
        }
    }
}
