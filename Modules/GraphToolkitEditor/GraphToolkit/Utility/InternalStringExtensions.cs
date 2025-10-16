// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

namespace Unity.GraphToolkit
{
    static class InternalStringExtensions
    {
        static readonly Regex k_CodifyRegex = new Regex("[^a-zA-Z0-9]", RegexOptions.Compiled);
        const char k_NoDelimiter = '\0'; //invalid character

        public static string CodifyString(this string str)
        {
            return k_CodifyRegex.Replace(str, "_");
        }

        public static string ToKebabCase(this string text)
        {
            return ConvertCase(text, '-', char.ToLowerInvariant, char.ToLowerInvariant);
        }

        static readonly char[] k_WordDelimiters = { ' ', '-', '_' };

        static string ConvertCase(string text,
            char outputWordDelimiter,
            Func<char, char> startOfStringCaseHandler,
            Func<char, char> middleStringCaseHandler)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            var builder = new StringBuilder();

            bool startOfString = true;
            bool startOfWord = true;
            bool outputDelimiter = true;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (((IList)k_WordDelimiters).Contains(c))
                {
                    if (c == outputWordDelimiter)
                    {
                        builder.Append(outputWordDelimiter);
                        //we disable the delimiter insertion
                        outputDelimiter = false;
                    }
                    startOfWord = true;
                }
                else if (!char.IsLetterOrDigit(c))
                {
                    startOfString = true;
                    startOfWord = true;
                }
                else
                {
                    if (startOfWord || char.IsUpper(c))
                    {
                        if (startOfString)
                        {
                            builder.Append(startOfStringCaseHandler(c));
                        }
                        else
                        {
                            if (outputDelimiter && outputWordDelimiter != k_NoDelimiter)
                            {
                                builder.Append(outputWordDelimiter);
                            }
                            builder.Append(middleStringCaseHandler(c));
                            outputDelimiter = true;
                        }
                        startOfString = false;
                        startOfWord = false;
                    }
                    else
                    {
                        builder.Append(c);
                    }
                }
            }

            return builder.ToString();
        }
    }
}
