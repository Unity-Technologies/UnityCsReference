// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;


namespace UnityEngine.TextCore.Text
{
    static class TextUtilities
    {
        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        internal static void ResizeArray<T>(ref T[] array)
        {
            int size = NextPowerOfTwo(array.Length);

            System.Array.Resize(ref array, size);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="size"></param>
        internal static void ResizeArray<T>(ref T[] array, int size)
        {
            size = NextPowerOfTwo(size);

            System.Array.Resize(ref array, size);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        internal static int NextPowerOfTwo(int v)
        {
            v |= v >> 16;
            v |= v >> 8;
            v |= v >> 4;
            v |= v >> 2;
            v |= v >> 1;
            return v + 1;
        }

        /// <summary>
        /// Table used to convert character to lowercase.
        /// </summary>
        const string k_LookupStringL = "-------------------------------- !-#$%&-()*+,-./0123456789:;<=>?@abcdefghijklmnopqrstuvwxyz[-]^_`abcdefghijklmnopqrstuvwxyz{|}~-";

        /// <summary>
        /// Table used to convert character to uppercase.
        /// </summary>
        const string k_LookupStringU = "-------------------------------- !-#$%&-()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[-]^_`ABCDEFGHIJKLMNOPQRSTUVWXYZ{|}~-";

        /// <summary>
        /// Get lowercase version of this ASCII character.
        /// </summary>
        internal static char ToLowerFast(char c)
        {
            if (c > k_LookupStringL.Length - 1)
                return c;

            return k_LookupStringL[c];
        }

        /// <summary>
        /// Get uppercase version of this ASCII character.
        /// </summary>
        internal static char ToUpperFast(char c)
        {
            if (c > k_LookupStringU.Length - 1)
                return c;

            return k_LookupStringU[c];
        }

        /// <summary>
        /// Get uppercase version of this ASCII character.
        /// </summary>
        internal static uint ToUpperASCIIFast(uint c)
        {
            if (c > k_LookupStringU.Length - 1)
                return c;

            return k_LookupStringU[(int)c];
        }

        /// <summary>
        /// Get lowercase version of this ASCII character.
        /// </summary>
        internal static uint ToLowerASCIIFast(uint c)
        {
            if (c > k_LookupStringL.Length - 1)
                return c;

            return k_LookupStringL[(int)c];
        }

        /// <summary>
        /// Function which returns a simple hashcode from a string.
        /// </summary>
        /// <returns></returns>
        public static int GetHashCodeCaseSensitive(string s)
        {
            int hashCode = 0;

            for (int i = 0; i < s.Length; i++)
                hashCode = (hashCode << 5) + hashCode ^ s[i];

            return hashCode;
        }

        public static int GetHashCodeCaseInSensitive(string s)
        {
            int hashCode = 0;

            for (int i = 0; i < s.Length; i++)
                hashCode = (hashCode << 5) + hashCode ^ ToUpperFast(s[i]);

            return hashCode;
        }

        /// <summary>
        /// Returns the case insensitive hashcode for the given string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static int GetHashCode(string s)
        {
            if (string.IsNullOrEmpty(s))
                return 0;

            int hashCode = 0;

            for (int i = 0; i < s.Length; i++)
                hashCode = ((hashCode << 5) + hashCode) ^ ToUpperFast(s[i]);

            return hashCode;
        }

        /// <summary>
        /// Function which returns a simple hashcode from a string.
        /// </summary>
        /// <returns></returns>
        public static int GetSimpleHashCode(string s)
        {
            int hashCode = 0;

            for (int i = 0; i < s.Length; i++)
                hashCode = ((hashCode << 5) + hashCode) ^ s[i];

            return hashCode;
        }

        /// <summary>
        /// Function which returns a simple hashcode from a string converted to lowercase.
        /// </summary>
        /// <returns></returns>
        public static uint GetSimpleHashCodeLowercase(string s)
        {
            uint hashCode = 0;

            for (int i = 0; i < s.Length; i++)
                hashCode = (hashCode << 5) + hashCode ^ ToLowerFast(s[i]);

            return hashCode;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="highSurrogate"></param>
        /// <param name="lowSurrogate"></param>
        /// <returns></returns>
        internal static uint ConvertToUTF32(uint highSurrogate, uint lowSurrogate)
        {
            return ((highSurrogate - CodePoint.HIGH_SURROGATE_START) * 0x400) + ((lowSurrogate - CodePoint.LOW_SURROGATE_START) + CodePoint.UNICODE_PLANE01_START);
        }

        /// <summary>
        /// Read the UTF16 character from uint[] at index.
        /// </summary>
        /// <param name="text">Source string</param>
        /// <param name="index">Reading index in string</param>
        /// <returns>UTF16 (uint) character</returns>
        internal static uint ReadUTF16(uint[] text, int index)
        {
            uint unicode = 0;
            unicode += HexToInt((char)text[index]) << 12;
            unicode += HexToInt((char)text[index + 1]) << 8;
            unicode += HexToInt((char)text[index + 2]) << 4;
            unicode += HexToInt((char)text[index + 3]);

            return unicode;
        }

        /// <summary>
        /// Read the UTF32 character from uint[] at index.
        /// </summary>
        /// <param name="text">Source string</param>
        /// <param name="index">Reading index in string</param>
        /// <returns>UTF32 (uint) character</returns>
        internal static uint ReadUTF32(uint[] text, int index)
        {
            uint unicode = 0;
            unicode += HexToInt((char)text[index]) << 30;
            unicode += HexToInt((char)text[index + 1]) << 24;
            unicode += HexToInt((char)text[index + 2]) << 20;
            unicode += HexToInt((char)text[index + 3]) << 16;
            unicode += HexToInt((char)text[index + 4]) << 12;
            unicode += HexToInt((char)text[index + 5]) << 8;
            unicode += HexToInt((char)text[index + 6]) << 4;
            unicode += HexToInt((char)text[index + 7]);
            return unicode;
        }

        /// <summary>
        /// Function to convert Hex to Int
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        static uint HexToInt(char hex)
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
                case 'A': return 10;
                case 'B': return 11;
                case 'C': return 12;
                case 'D': return 13;
                case 'E': return 14;
                case 'F': return 15;
                case 'a': return 10;
                case 'b': return 11;
                case 'c': return 12;
                case 'd': return 13;
                case 'e': return 14;
                case 'f': return 15;
            }
            return 15;
        }

        /// <summary>
        /// Function to convert a properly formatted string which contains an hex value to its decimal value.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static uint StringHexToInt(string s)
        {
            uint value = 0;
            int length = s.Length;

            for (int i = 0; i < length; i++)
                value += HexToInt(s[i]) * (uint)Mathf.Pow(16, length - 1 - i);

            return value;
        }

        internal static string UintToString(this List<uint> unicodes)
        {
            char[] chars = new char[unicodes.Count];

            for (int i = 0; i < unicodes.Count; i++)
            {
                chars[i] = (char)unicodes[i];
            }

            return new string(chars);
        }

        internal static int GetTextFontWeightIndex(TextFontWeight fontWeight)
        {
            switch (fontWeight)
            {
                case TextFontWeight.Thin:
                    return 1;       
                case TextFontWeight.ExtraLight:
                    return 2;   
                case TextFontWeight.Light:
                    return 3;  
                case TextFontWeight.Regular:
                    return 4; 
                case TextFontWeight.Medium:
                    return 5;
                case TextFontWeight.SemiBold:
                    return 6;
                case TextFontWeight.Bold:
                    return 7;
                case TextFontWeight.Heavy:
                    return 8;
                case TextFontWeight.Black:
                    return 9;
                default:
                    return 4;
            }
        }
    }
}
