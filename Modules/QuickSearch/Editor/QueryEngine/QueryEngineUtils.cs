// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.Search
{
    static class QueryEngineUtils
    {
        static readonly HashSet<char> k_WhiteSpaceChars = new HashSet<char>(" \f\n\r\t\v");

        public static bool IsNestedQueryToken(in StringView token)
        {
            if (token.Length < 2)
                return false;
            var startIndex = token.IndexOf('{');
            var endIndex = token.LastIndexOf('}');
            return startIndex != -1 && endIndex == token.Length - 1 && startIndex < endIndex;
        }

        public static bool IsWhiteSpaceChar(char c)
        {
            return k_WhiteSpaceChars.Contains(c);
        }
    }
}
