// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Search
{
    [Flags]
    enum SearchPropositionFlags
    {
        None = 0,

        FilterOnly = 1 << 0,
        IgnoreRecents = 1 << 1
    }

    static class SearchPropositionFlagsExtensions
    {
        public static bool HasAny(this SearchPropositionFlags flags, SearchPropositionFlags f) => (flags & f) != 0;
        public static bool HasAll(this SearchPropositionFlags flags, SearchPropositionFlags all) => (flags & all) == all;
    }

    readonly struct SearchProposition : IEquatable<SearchProposition>, IComparable<SearchProposition>
    {
        public readonly string label;
        public readonly string replacement;
        public readonly string help;
        public readonly int priority;
        public readonly TextCursorPlacement moveCursor;
        public readonly Texture2D icon;

        public bool valid => label != null && replacement != null;

        public SearchProposition(string label, string replacement = null, string help = null,
                                 int priority = int.MaxValue, TextCursorPlacement moveCursor = TextCursorPlacement.MoveAutoComplete,
                                 Texture2D icon = null)
        {
            var kparts = label.Split(new char[] { '|' });
            this.label = kparts[0];
            this.replacement = replacement ?? this.label;
            this.help = kparts.Length >= 2 ? kparts[1] : BuiltinPropositions.FindHelpText(this.replacement, help);
            this.priority = priority;
            this.moveCursor = moveCursor;
            this.icon = icon;
        }

        public int CompareTo(SearchProposition other)
        {
            var c = priority.CompareTo(other.priority);
            if (c != 0)
                return c;
            c = label.CompareTo(other.label);
            if (c != 0)
                return c;
            return string.Compare(help, other.help);
        }

        public bool Equals(SearchProposition other)
        {
            return label.Equals(other.label) && string.Equals(help, other.help);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                if (help != null)
                    return label.GetHashCode() ^ help.GetHashCode() ^ priority.GetHashCode();
                return label.GetHashCode() ^ priority.GetHashCode();
            }
        }

        public override bool Equals(object other)
        {
            if (other is string s)
                return label.Equals(s);
            return other is SearchProposition l && Equals(l);
        }

        public override string ToString()
        {
            return $"{label} > {replacement}";
        }

        public static SortedSet<SearchProposition> Fetch(SearchContext context, in SearchPropositionOptions options)
        {
            var propositions = new SortedSet<SearchProposition>();

            if (!options.HasAny(SearchPropositionFlags.IgnoreRecents) && !context.options.HasFlag(SearchFlags.Debug) && !Utils.IsRunningTests())
                FillBuiltInPropositions(context, propositions);
            FillProviderPropositions(context, options, propositions);
            return propositions;
        }

        private static void FillProviderPropositions(SearchContext context, in SearchPropositionOptions options, SortedSet<SearchProposition> propositions)
        {
            var providers = context.providers.Where(p => context.filterId == null || context.filterId == p.filterId).ToList();
            var queryEmpty = string.IsNullOrWhiteSpace(context.searchText) && providers.Count(p => !p.isExplicitProvider) > 1;
            foreach (var p in providers)
            {
                if (queryEmpty)
                {
                    propositions.Add(new SearchProposition($"{p.filterId}", $"{p.filterId} ", p.name, p.priority));
                }
                else
                {
                    if (p.fetchPropositions == null)
                        continue;
                    var currentPropositions = p.fetchPropositions(context, options);
                    if (currentPropositions != null)
                        propositions.UnionWith(currentPropositions);
                }
            }
        }

        private static void FillBuiltInPropositions(SearchContext context, SortedSet<SearchProposition> propositions)
        {
            int builtPriority = -50;
            foreach (var rs in SearchSettings.recentSearches.Take(5))
            {
                if (!rs.StartsWith(context.searchText, StringComparison.OrdinalIgnoreCase))
                    continue;
                propositions.Add(new SearchProposition(rs, rs, "Recent search", priority: builtPriority, moveCursor: TextCursorPlacement.MoveLineEnd));
                builtPriority += 10;
            }
        }
    }

    class SearchPropositionOptions
    {
        static readonly char[] s_Delimiters = new[] { '{', '}', '[', ']', '=', ',' };
        static readonly char[] s_ExtendedDelimiters = new[] { '{', '}', '[', ']', '=', ':' };

        public readonly string query;
        public readonly int cursor;
        public readonly SearchPropositionFlags flags;

        private string m_Word;
        private string[] m_Tokens;

        public string word
        {
            get
            {
                if (m_Word == null)
                    m_Word = GetWordAtCursorPosition(query, cursor, out _, out _);
                return m_Word;
            }
        }

        public string[] tokens
        {
            get
            {
                if (m_Tokens == null)
                {
                    m_Tokens = new[]
                    {
                        GetTokenAtCursorPosition(query, cursor, IsDelimiter),
                        GetTokenAtCursorPosition(query, cursor, IsExtendedDelimiter)
                    }.Distinct().ToArray();
                }
                return m_Tokens;
            }
        }

        public bool StartsWith(string token)
        {
            return tokens.Any(t => t.StartsWith(token, StringComparison.OrdinalIgnoreCase));
        }

        public bool StartsWith(params string[] tokens)
        {
            return tokens.Any(t => StartsWith(t));
        }

        public SearchPropositionOptions(SearchPropositionFlags flags)
            : this(string.Empty, 0, flags)
        {
        }

        public SearchPropositionOptions(string query, int cursor)
            : this(query, cursor, SearchPropositionFlags.None)
        {
        }

        public SearchPropositionOptions(string query, SearchPropositionFlags flags)
            : this(query, query != null ? query.Length : 0, flags)
        {
        }

        public SearchPropositionOptions(string query, int cursor, SearchPropositionFlags flags)
        {
            this.query = query;
            this.cursor = cursor;
            this.flags = flags;
            m_Word = null;
            m_Tokens = null;
        }

        public static bool IsExtendedDelimiter(char ch)
        {
            return char.IsWhiteSpace(ch) || Array.IndexOf(s_ExtendedDelimiters, ch) != -1;
        }

        public static bool IsDelimiter(char ch)
        {
            return char.IsWhiteSpace(ch) || Array.IndexOf(s_Delimiters, ch) != -1;
        }

        public bool HasAny(SearchPropositionFlags f) => (flags & f) != 0;
        public bool HasAll(SearchPropositionFlags all) => (flags & all) == all;

        private static string GetWordAtCursorPosition(string txt, int cursorIndex, out int startPos, out int endPos)
        {
            return GetTokenAtCursorPosition(txt, cursorIndex, out startPos, out endPos, ch => !char.IsLetterOrDigit(ch) && !(ch == '_'));
        }

        private static string GetTokenAtCursorPosition(string txt, int cursorIndex, Func<char, bool> comparer)
        {
            return GetTokenAtCursorPosition(txt, cursorIndex, out var _, out var _, comparer);
        }

        internal static void GetTokenBoundariesAtCursorPosition(string txt, int cursorIndex, out int startPos, out int endPos)
        {
            GetTokenAtCursorPosition(txt, cursorIndex, out startPos, out endPos, IsDelimiter);
        }

        private static string GetTokenAtCursorPosition(string txt, int cursorIndex, out int startPos, out int endPos, Func<char, bool> check)
        {
            if (txt.Length > 0 && (cursorIndex == txt.Length || IsDelimiter(txt[cursorIndex])))
                cursorIndex--;

            startPos = cursorIndex;
            endPos = cursorIndex;

            // Get the character's position.
            if (cursorIndex >= txt.Length || cursorIndex < 0)
                return "";

            for (; startPos >= 0; startPos--)
            {
                // Allow digits, letters, and underscores as part of the word.
                char ch = txt[startPos];
                if (check(ch)) break;
            }
            startPos++;

            // Find the end of the word.
            for (; endPos < txt.Length; endPos++)
            {
                char ch = txt[endPos];
                if (check(ch)) break;
            }
            endPos--;

            // Return the result.
            if (startPos > endPos)
                return "";
            return txt.Substring(startPos, endPos - startPos + 1);
        }
    }

    static class BuiltinPropositions
    {
        private static readonly string[] baseTypeFilters = new[]
        {
            "DefaultAsset", "AnimationClip", "AudioClip", "AudioMixer", "ComputeShader", "Font", "GUISKin", "Material", "Mesh",
            "Model", "PhysicMaterial", "Prefab", "Scene", "Script", "ScriptableObject", "Shader", "Sprite", "StyleSheet", "Texture", "VideoClip"
        };

        public static Dictionary<string, string> help = new Dictionary<string, string>
        {
            {"dir:", "Search parent folder name" },
            {"ext:", "Search by extension" },
            {"age:", "Search asset older than N days" },
            {"size:", "Search by asset file size" },
            {"ref:", "Search references" },
            {"a:assets", "Search project assets" },
            {"a:packages", "Search package assets" },
            {"t:file", "Search files" },
            {"t:folder", "Search folders" },
            {"name:", "Search by object name" },
            {"id:", "Search by unique id" },
        };

        static BuiltinPropositions()
        {
            foreach (var t in baseTypeFilters.Concat(TypeCache.GetTypesDerivedFrom<ScriptableObject>().Select(t => t.Name)))
                help[$"t:{t.ToLowerInvariant()}"] = $"Search {t} assets";

            foreach (var t in TypeCache.GetTypesDerivedFrom<Component>().Select(t => t.Name))
                help[$"t:{t.ToLowerInvariant()}"] = $"Search {t} components";
        }

        public static string FindHelpText(string replacement, string help = null)
        {
            if (string.IsNullOrEmpty(help) && BuiltinPropositions.help.TryGetValue(replacement, out var helpText))
                return helpText;
            return help;
        }
    }
}
