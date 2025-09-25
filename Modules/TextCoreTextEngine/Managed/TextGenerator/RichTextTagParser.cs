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
        internal static void PreProcessString(ref string text, PreProcessFlags flags)
        {
            if (string.IsNullOrEmpty(text) || flags == PreProcessFlags.None)
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
    }
}
