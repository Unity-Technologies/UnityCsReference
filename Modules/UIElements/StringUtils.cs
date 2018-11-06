// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Text;

namespace UnityEngine.UIElements
{
    internal static class StringUtilsExtensions
    {
        private static readonly char NoDelimiter = '\0'; //invalid character

        public static string ToPascalCase(this string text)
        {
            return ConvertCase(text, NoDelimiter, char.ToUpperInvariant, char.ToUpperInvariant);
        }

        public static string ToCamelCase(this string text)
        {
            return ConvertCase(text, NoDelimiter, char.ToLowerInvariant, char.ToUpperInvariant);
        }

        public static string ToKebabCase(this string text)
        {
            return ConvertCase(text, '-', char.ToLowerInvariant, char.ToLowerInvariant);
        }

        public static string ToTrainCase(this string text)
        {
            return ConvertCase(text, '-', char.ToUpperInvariant, char.ToUpperInvariant);
        }

        public static string ToSnakeCase(this string text)
        {
            return ConvertCase(text, '_', char.ToLowerInvariant, char.ToLowerInvariant);
        }

        private static readonly char[] WordDelimiters = { ' ', '-', '_' };

        private static string ConvertCase(string text,
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
                if (WordDelimiters.Contains(c))
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
                            if (outputDelimiter && outputWordDelimiter != NoDelimiter)
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
