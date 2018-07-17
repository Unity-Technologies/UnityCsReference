// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    internal static class SearchUtils
    {
        internal static bool MatchSearch(string searchContext, string content)
        {
            return content != null && searchContext != null && content.IndexOf(searchContext, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool FuzzySearch(string searchContext, string content)
        {
            int dummyStart;
            int dummyEnd;
            return FuzzySearch(searchContext, content, out dummyStart, out dummyEnd);
        }

        internal static bool FuzzySearch(string searchContext, string content, out int startIndex, out int endIndex)
        {
            startIndex = endIndex = -1;
            if (searchContext == null || content == null || searchContext.Length > content.Length)
                return false;

            if (searchContext == content)
            {
                endIndex = content.Length - 1;
                return true;
            }

            // Ensure we find all letters of searchContext but not necessarily consecutively:
            var contentIndex = 0;
            var searchContextIndex = 0;
            for (; searchContextIndex < searchContext.Length && contentIndex < content.Length; ++searchContextIndex)
            {
                var ch = Char.ToLowerInvariant(searchContext[searchContextIndex]);
                while (contentIndex < content.Length)
                {
                    if (ch == Char.ToLowerInvariant(content[contentIndex++]))
                    {
                        startIndex = startIndex != -1 ? startIndex : (contentIndex - 1);
                        endIndex = searchContextIndex == searchContext.Length - 1 ? (contentIndex - 1) : endIndex;
                        break;
                    }
                }
            }

            return startIndex != -1 && endIndex != -1;
        }
    }
}
