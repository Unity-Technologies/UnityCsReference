// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.Bindings;

namespace Unity.Hierarchy
{
    [VisibleToOtherModules("UnityEditor.HierarchyModule")]
    interface IHierarchySearchQueryParser
    {
        HierarchySearchQueryDescriptor ParseQuery(string query);
    }

    class DefaultHierarchySearchQueryParser : IHierarchySearchQueryParser
    {
        static readonly Regex s_Filter = new Regex(@"([#$\w]+)(<=|<|>=|>|<|=|:)(.*)", RegexOptions.Compiled);

        static List<string> Tokenize(string s)
        {
            s = s.Trim();
            var tokens = new List<string>();
            var startToken = 0;
            var cursor = 0;
            while (cursor < s.Length)
            {
                if (char.IsWhiteSpace(s[cursor]))
                {
                    var token = s.Substring(startToken, cursor - startToken);
                    tokens.Add(token);
                    ++cursor;
                    while (cursor < s.Length && char.IsWhiteSpace(s[cursor]))
                    {
                        ++cursor;
                    }
                    if (cursor < s.Length)
                    {
                        startToken = cursor;
                    }
                }
                else if (s[cursor] == '"')
                {
                    ++cursor;
                    while (cursor < s.Length && s[cursor] != '"')
                    {
                        ++cursor;
                    }
                    if (cursor >= s.Length)
                        return null;
                    else
                        ++cursor;
                }
                else
                {
                    ++cursor;
                }
            }

            if (cursor != startToken)
            {
                var token = s.Substring(startToken, cursor - startToken);
                tokens.Add(token);
            }

            return tokens;
        }

        public HierarchySearchQueryDescriptor ParseQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return HierarchySearchQueryDescriptor.Empty;

            var tokens = Tokenize(query);
            if (tokens == null)
                return HierarchySearchQueryDescriptor.InvalidQuery;

            var textValues = new List<string>();
            var filters = new List<HierarchySearchFilter>();
            var valid = true;
            foreach (var t in tokens)
            {
                var m = s_Filter.Match(t);
                if (m.Success)
                {
                    if (m.Groups.Count < 4 || string.IsNullOrEmpty(m.Groups[1].Value) || string.IsNullOrEmpty(m.Groups[2].Value) || string.IsNullOrEmpty(m.Groups[3].Value))
                    {
                        valid = false;
                        break;
                    }
                    filters.Add(HierarchySearchFilter.CreateFilter(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value));
                }
                else
                {
                    textValues.Add(t);
                }
            }

            if (!valid)
                return HierarchySearchQueryDescriptor.InvalidQuery;

            return new HierarchySearchQueryDescriptor(filters.ToArray(), textValues.ToArray());
        }
    }
}
