// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditorInternal
{
    [Serializable]
    struct AnimationWindowSearchFilter
    {
        static readonly string[] k_TypePrefixes = ["t=", "type="];
        static readonly string[] k_PropertyPrefixes = ["p=", "property="];
        static readonly char[] kFilterSeparator = new [] { ' ', '\t', ',', '*', '?'};

        [SerializeField] List<string> m_NameFilters = new();
        [SerializeField] List<string> m_PropertyNames = new ();
        [SerializeField] List<string> m_ComponentNames = new ();
        [SerializeField] string m_SearchString = String.Empty;

        public IReadOnlyList<string> nameFilters => m_NameFilters;
        public IReadOnlyList<string> propertyNames => m_PropertyNames;
        public IReadOnlyList<string> componentNames => m_ComponentNames;

        public string searchString
        {
            get => m_SearchString;
            set => SetSearchString(value);
        }

        public bool isActive =>
            m_NameFilters.Count > 0 ||
            m_PropertyNames.Count > 0 ||
            m_ComponentNames.Count > 0;

        public AnimationWindowSearchFilter()
        {
        }

        public AnimationWindowSearchFilter(string searchString)
        {
            SetSearchString(searchString);
        }

        public void ClearSearch()
        {
            m_SearchString = string.Empty;
            m_NameFilters.Clear();
            m_PropertyNames.Clear();
            m_ComponentNames.Clear();
        }


        void SetSearchString(string searchString)
        {
            ClearSearch();

            if (string.IsNullOrEmpty(searchString))
                return;

            m_SearchString = searchString;

            RemoveUnwantedWhitespaces(ref searchString);

            // Split filter into separate words with space or tab as separators

            // Skip any separators preceding the filter
            int pos = FindNextNonSeparatorIndex(searchString, kFilterSeparator);
            if (pos == -1)
                pos = 0;
            while (pos < searchString.Length)
            {
                int endpos = searchString.IndexOfAny(kFilterSeparator, pos);

                // Check if we have quotes (may be used for pathnames) inbetween start and a /filter-separator/
                int q1 = searchString.IndexOf('"', pos);
                int q2 = -1;
                if (q1 != -1)
                {
                    q2 = searchString.IndexOf('"', q1 + 1);
                    if (q2 != -1)
                        // Advance to a /filter-separator/ after the quote
                        endpos = searchString.IndexOfAny(kFilterSeparator, q2);
                    else
                        // In case we can't find another quote, consume the rest of the string
                        endpos = -1;
                }

                if (endpos == -1)
                    endpos = searchString.Length;

                if (endpos > pos)
                {
                    string token = searchString.Substring(pos, endpos - pos);
                    CheckForKeyWords(token, q1 - pos, q2 - pos);
                }
                pos = endpos + 1;
            }
        }

        void CheckForKeyWords(string searchString, int quote1, int quote2)
        {
            // Support: 't=type' syntax (e.g 't=Transform' will show Transform components)
            foreach (var typePrefix in k_TypePrefixes)
            {
                if (searchString.StartsWith(typePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    string type = searchString.Substring(typePrefix.Length);
                    m_ComponentNames.Add(type);
                    return;
                }
            }

            // Support: 'p=name' syntax (e.g 'p=Transform.m_LocalPosition' will show Position properties in components)
            foreach (var propertyPrefix in k_PropertyPrefixes)
            {
                if (searchString.StartsWith(propertyPrefix))
                {
                    m_PropertyNames.Add(StripNameFromQuotes(searchString, propertyPrefix.Length, quote1, quote2));
                    return;
                }
            }

            m_NameFilters.Add(StripNameFromQuotes(searchString, 0, quote1, quote2));
        }

        string StripNameFromQuotes(string searchString, int offset, int quote1, int quote2)
        {
            if (quote1 >= 0 && quote2 >= 0)
            {
                int startIndex = quote1 + 1;
                int count = quote2 - quote1 - 1;
                if (count < 0)
                    count = searchString.Length - startIndex;

                // Strip path from quotes
                return count == 0
                    ? String.Empty
                    : searchString.Substring(startIndex, count);
            }

            return offset > 0 ? searchString.Substring(offset) : searchString;
        }

        void RemoveUnwantedWhitespaces(ref string searchString)
        {
            // Some users add a whitespace after the colon (remove it)
            searchString = searchString.Replace("= ", "=");
        }

        static int FindNextNonSeparatorIndex(string source, char[] chars)
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (Array.IndexOf(chars, source[i]) == -1)
                    return i;
            }
            return -1;
        }
    }
}

