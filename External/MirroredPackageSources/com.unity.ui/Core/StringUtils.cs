using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace UnityEngine.UIElements
{
    internal static class StringUtils
    {
        public unsafe static int LevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;

            if (n == 0)
                return m;

            if (m == 0)
                return n;

            int xSize = n + 1;
            int ySize = m + 1;
            int* d = stackalloc int[xSize * ySize];

            for (int x = 0; x <= n; x++)
                d[ySize * x] = x;
            for (int y = 0; y <= m; y++)
                d[y] = y;

            for (int y = 1; y <= m; y++)
            {
                for (int x = 1; x <= n; x++)
                {
                    if (s[x - 1] == t[y - 1])
                        d[ySize * x + y] = d[ySize * (x - 1) + y - 1];  // no operation
                    else
                        d[ySize * x + y] = Math.Min(Math.Min(
                            d[ySize * (x - 1) + y] + 1,             // a deletion
                            d[ySize * x + y - 1] + 1),             // an insertion
                            d[ySize * (x - 1) + y - 1] + 1 // a substitution
                        );
                }
            }
            return d[ySize * n + m];
        }
    }

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

        // https://docs.unity3d.com/Manual/BestPracticeUnderstandingPerformanceInUnity5.html
        public static bool EndsWithIgnoreCaseFast(this string a, string b)
        {
            int ap = a.Length - 1;
            int bp = b.Length - 1;

            var culture = CultureInfo.InvariantCulture;

            while (ap >= 0 && bp >= 0 &&
                   (a[ap] == b[bp] ||
                    char.ToLower(a[ap], culture) == char.ToLower(b[bp], culture)))
            {
                ap--;
                bp--;
            }

            return (bp < 0);
        }

        public static bool StartsWithIgnoreCaseFast(this string a, string b)
        {
            int aLen = a.Length;
            int bLen = b.Length;

            int ap = 0; int bp = 0;

            var culture = CultureInfo.InvariantCulture;

            while (ap < aLen && bp < bLen &&
                   (a[ap] == b[bp] ||
                    char.ToLower(a[ap], culture) == char.ToLower(b[bp], culture)))
            {
                ap++;
                bp++;
            }

            return (bp == bLen);
        }
    }
}
