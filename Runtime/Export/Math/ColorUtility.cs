// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEngine
{
    public partial class ColorUtility
    {
        static ReadOnlySpan<Color32> HtmlColorValues => new Color32[]
        {
            new(0xff, 0x00, 0x00, 0xff), // red
            new(0x00, 0xff, 0xff, 0xff), // cyan
            new(0x00, 0x00, 0xff, 0xff), // blue
            new(0x00, 0x00, 0x8b, 0xff), // darkblue
            new(0xad, 0xd8, 0xe6, 0xff), // lightblue
            new(0x80, 0x00, 0x80, 0xff), // purple
            new(0xff, 0xff, 0x00, 0xff), // yellow
            new(0x00, 0xff, 0x00, 0xff), // lime
            new(0xff, 0x00, 0xff, 0xff), // fuchsia
            new(0xff, 0xff, 0xff, 0xff), // white
            new(0xc0, 0xc0, 0xc0, 0xff), // silver
            new(0x80, 0x80, 0x80, 0xff), // grey
            new(0x00, 0x00, 0x00, 0xff), // black
            new(0xff, 0xa5, 0x00, 0xff), // orange
            new(0xa5, 0x2a, 0x2a, 0xff), // brown
            new(0x80, 0x00, 0x00, 0xff), // maroon
            new(0x00, 0x80, 0x00, 0xff), // green
            new(0x80, 0x80, 0x00, 0xff), // olive
            new(0x00, 0x00, 0x80, 0xff), // navy
            new(0x00, 0x80, 0x80, 0xff), // teal
            new(0x00, 0xff, 0xff, 0xff), // aqua
            new(0xff, 0x00, 0xff, 0xff), // magenta
            new(0x00, 0x00, 0x00, 0x00), // transparent
        };

        static ReadOnlySpan<string> HtmlColorNames => new[]
        {
            "red",
            "cyan",
            "blue",
            "darkblue",
            "lightblue",
            "purple",
            "yellow",
            "lime",
            "fuchsia",
            "white",
            "silver",
            "grey",
            "black",
            "orange",
            "brown",
            "maroon",
            "green",
            "olive",
            "navy",
            "teal",
            "aqua",
            "magenta",
            "transparent"
        };


        public static bool TryParseHtmlString(string htmlString, out Color color)
        {
            Color32 c;
            bool ret = DoTryParseHtmlColor(htmlString, out c);
            color = c;
            return ret;
        }

        public static string ToHtmlStringRGB(Color color)
        {
            // Round to int to prevent precision issues that, for example cause values very close to 1 to become FE instead of FF (case 770904).
            Color32 col32 = new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.r * 255), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * 255), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * 255), 0, 255),
                1);

            return string.Format("{0:X2}{1:X2}{2:X2}", col32.r, col32.g, col32.b);
        }

        public static string ToHtmlStringRGBA(Color color)
        {
            // Round to int to prevent precision issues that, for example cause values very close to 1 to become FE instead of FF (case 770904).
            Color32 col32 = new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.r * 255), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.g * 255), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.b * 255), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(color.a * 255), 0, 255));

            return string.Format("{0:X2}{1:X2}{2:X2}{3:X2}", col32.r, col32.g, col32.b, col32.a);
        }

        [VisibleToOtherModules("UnityEngine.TextCoreTextEngineModule")]
        internal static bool TryParseHtmlString(ReadOnlySpan<char> input, out Color color)
        {
            color = Color.white;

            input = input.Trim();

            if (input.Length == 0)
                return false;

            if (input[0] == '#')
            {
                if (input.Length > 9) // Max "#RRGGBBAA"
                    return false;

                if (!IsHexString(input.Slice(1)))
                    return false;

                if (input.Length == 4 || input.Length == 5)
                {
                    Span<char> expanded = stackalloc char[(input.Length - 1) * 2];
                    for (int i = 1, j = 0; i < input.Length; i++)
                    {
                        expanded[j++] = input[i];
                        expanded[j++] = input[i];
                    }
                    return TryParseHexColor(expanded, out color);
                }
                else if (input.Length == 7 || input.Length == 9)
                {
                    return TryParseHexColor(input.Slice(1), out color);
                }
            }
            else
            {
                for (int i = 0; i < HtmlColorNames.Length; i++)
                {
                    if (input.Equals(HtmlColorNames[i], StringComparison.OrdinalIgnoreCase))
                    {
                        color = HtmlColorValues[i];
                        return true;
                    }
                }
            }

            return false;
        }

        static bool IsHexString(ReadOnlySpan<char> span)
        {
            foreach (char c in span)
            {
                if (!Uri.IsHexDigit(c)) return false;
            }
            return true;
        }

        static bool TryParseHexColor(ReadOnlySpan<char> hex, out Color color)
        {
            color = Color.white;

            if (hex.Length != 6 && hex.Length != 8)
                return false;

            if (!TryHexToByte(hex.Slice(0, 2), out byte r)) return false;
            if (!TryHexToByte(hex.Slice(2, 2), out byte g)) return false;
            if (!TryHexToByte(hex.Slice(4, 2), out byte b)) return false;

            byte a = 255;
            if (hex.Length == 8)
            {
                if (!TryHexToByte(hex.Slice(6, 2), out a)) return false;
            }

            color = new Color32(r, g, b, a);
            return true;
        }

        static bool TryHexToByte(ReadOnlySpan<char> span, out byte result)
        {
            result = 0;

            if (span.Length != 2)
                return false;

            int hi = HexDigitValue(span[0]);
            int lo = HexDigitValue(span[1]);

            if (hi == -1 || lo == -1)
                return false;

            result = (byte)((hi << 4) | lo);
            return true;
        }

        static int HexDigitValue(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            return -1;
        }
    }
}
