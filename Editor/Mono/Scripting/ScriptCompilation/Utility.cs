// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine;

namespace UnityEditor.Scripting.ScriptCompilation
{
    static class Utility
    {
        public static char FastToLower(char c)
        {
            // ASCII non-letter characters and
            // lower case letters.
            if (c < 'A' || (c > 'Z' && c <= 'z'))
            {
                return c;
            }

            if (c >= 'A' && c <= 'Z')
            {
                return (char)(c + 32);
            }

            return Char.ToLower(c, CultureInfo.InvariantCulture);
        }

        public static string FastToLower(string str)
        {
            int length = str.Length;

            var chars = new char[length];

            for (int i = 0; i < length; ++i)
            {
                chars[i] = FastToLower(str[i]);
            }

            return new string(chars);
        }

        public static bool FastStartsWith(string str, string prefix, string prefixLowercase)
        {
            int strLength = str.Length;
            int prefixLength = prefix.Length;

            if (prefixLength > strLength)
                return false;

            int lastPrefixCharIndex = prefixLength - 1;

            // Check last char in prefix is equal. Since we are comparing
            // file paths against directory paths, the last char will be '/'.
            if (str[lastPrefixCharIndex] != prefix[lastPrefixCharIndex])
                return false;

            for (int i = 0; i < prefixLength; ++i)
            {
                if (str[i] == prefix[i])
                    continue;

                char strC = FastToLower(str[i]);

                if (strC != prefixLowercase[i])
                    return false;
            }

            return true;
        }

        public static bool IsAssetsPath(string path)
        {
            const string assetsLowerCase = "assets";
            const string assetsUpperCase = "ASSETS";

            if (path.Length < 7)
                return false;

            if (path[6] != '/')
                return false;

            for (int i = 0; i < 6; ++i)
            {
                if (path[i] != assetsLowerCase[i] && path[i] != assetsUpperCase[i])
                    return false;
            }

            return true;
        }

        public static string ReadTextAsset(string path)
        {
            if (IsAssetsPath(path))
            {
                var textAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(path);

                if (textAsset != null)
                    return textAsset.text;
            }

            // Support out of project reading of files for tests.
            return System.IO.File.ReadAllText(path);
        }

        public static string FileNameWithoutExtension(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "";
            }

            var indexOfDot = -1;
            var indexOfSlash = 0;
            for (var i = path.Length - 1; i >= 0; i--)
            {
                if (indexOfDot == -1 && path[i] == '.')
                {
                    indexOfDot = i;
                }

                if (indexOfSlash == 0 && path[i] == '/' || path[i] == '\\')
                {
                    indexOfSlash = i + 1;
                    break;
                }
            }

            if (indexOfDot == -1)
            {
                indexOfDot = path.Length;
            }

            return path.Substring(indexOfSlash, indexOfDot - indexOfSlash);
        }
    }
}
