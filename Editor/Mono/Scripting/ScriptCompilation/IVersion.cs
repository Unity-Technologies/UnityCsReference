// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.Scripting.ScriptCompilation
{
    internal interface IVersionTypeTraits
    {
        bool IsAllowedFirstCharacter(char c, bool strict = false);
        bool IsAllowedLastCharacter(char c, bool strict = false);
        bool IsAllowedCharacter(char c);
    }

    internal static class VersionTypeTraitsUtils
    {
        public static bool IsCharDigit(char c)
        {
            return (c >= '0' && c <= '9');
        }

        public static bool IsCharLetter(char c)
        {
            return c >= 'a' && c <= 'z' || c >= 'A' && c <= 'Z';
        }
    }

    internal interface IVersion<TVersion> : IEquatable<TVersion>, IComparable<TVersion>, IComparable where TVersion : struct
    {
        bool IsInitialized { get; }

        TVersion Parse(string version, bool strict = false);

        IVersionTypeTraits GetVersionTypeTraits();
    }

    internal static class VersionUtils
    {
        public static string ConsumeVersionComponentFromString(string value, ref int cursor, Func<char, bool> isEnd)
        {
            int length = 0;
            for (int i = cursor; i < value.Length; i++)
            {
                if (isEnd(value[i]))
                    break;

                length++;
            }

            int newIndex = cursor;
            cursor += length;
            return value.Substring(newIndex, length);
        }
    }
}
