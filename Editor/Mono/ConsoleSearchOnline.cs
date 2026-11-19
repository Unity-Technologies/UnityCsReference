// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text.RegularExpressions;

namespace UnityEditor
{
    internal static class ConsoleSearchOnline
    {
        // Order must match GeneralProperties.consoleSearchEngineOptions in PreferencesSettingsProviders
        internal enum SearchEngine
        {
            Google,
            Bing,
            DuckDuckGo,
            UnityDiscussions,
            Custom,
        }

        internal const string k_SearchEnginePrefKey = "ConsoleSearchOnline.SearchEngine";
        internal const string k_CustomUrlTemplatePrefKey = "ConsoleSearchOnline.CustomUrlTemplate";
        internal const string k_QueryPlaceholder = "{0}";
        internal const int k_MaxQueryLength = 256;

        const SearchEngine k_DefaultSearchEngine = SearchEngine.Google;
        const string k_UnityQueryPrefix = "Unity ";

        // Strips the "Assets/Foo.cs(12,5): " prefix of compiler messages
        const string k_FileLocationPattern = @"^.+?\.\w+\(\d+,\d+\):\s*";

        // Rich text that ConsoleWindow.StacktraceWithHyperlinks adds around callstack file links
        const string k_HyperlinkTagPattern = @"</?(color|link)(=[^>]*)?>";
        const string k_WhitespacePattern = @"\s+";

        internal static SearchEngine searchEngine
        {
            get
            {
                var engine = (SearchEngine)EditorPrefs.GetInt(k_SearchEnginePrefKey, (int)k_DefaultSearchEngine);
                return Enum.IsDefined(typeof(SearchEngine), engine) ? engine : k_DefaultSearchEngine;
            }
            set { EditorPrefs.SetInt(k_SearchEnginePrefKey, (int)value); }
        }

        internal static string customUrlTemplate
        {
            get { return EditorPrefs.GetString(k_CustomUrlTemplatePrefKey, string.Empty); }
            set { EditorPrefs.SetString(k_CustomUrlTemplatePrefKey, value ?? string.Empty); }
        }

        internal static bool IsValidCustomUrlTemplate(string template)
        {
            return !string.IsNullOrWhiteSpace(template) &&
                template.Contains(k_QueryPlaceholder) &&
                (template.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                    template.StartsWith("http://", StringComparison.OrdinalIgnoreCase));
        }

        internal static string GetUrlTemplate(SearchEngine engine, string customTemplate)
        {
            switch (engine)
            {
                case SearchEngine.Bing:
                    return "https://www.bing.com/search?q={0}";
                case SearchEngine.DuckDuckGo:
                    return "https://duckduckgo.com/?q={0}";
                case SearchEngine.UnityDiscussions:
                    return "https://discussions.unity.com/search?q={0}";
                case SearchEngine.Custom when IsValidCustomUrlTemplate(customTemplate):
                    return customTemplate;
                default:
                    return "https://www.google.com/search?q={0}";
            }
        }

        // Returns null when the message has nothing to search for
        internal static string BuildSearchQuery(string message, SearchEngine engine)
        {
            if (string.IsNullOrWhiteSpace(message))
                return null;

            // The lines after the first one are the callstack
            var newLineIndex = message.IndexOf('\n');
            var query = newLineIndex >= 0 ? message.Substring(0, newLineIndex) : message;
            query = Regex.Replace(query, k_FileLocationPattern, string.Empty).Trim();
            if (query.Length == 0)
                return null;

            if (query.Length > k_MaxQueryLength)
            {
                // A lone high surrogate makes Uri.EscapeDataString throw
                var length = char.IsHighSurrogate(query[k_MaxQueryLength - 1]) ? k_MaxQueryLength - 1 : k_MaxQueryLength;
                query = query.Substring(0, length).TrimEnd();
            }

            return engine == SearchEngine.UnityDiscussions ? query : k_UnityQueryPrefix + query;
        }

        internal static string BuildSearchUrl(string message, SearchEngine engine, string customTemplate)
        {
            var query = BuildSearchQuery(message, engine);
            if (query == null)
                return null;

            // Replace instead of string.Format, so other braces in a custom template do not throw
            return GetUrlTemplate(engine, customTemplate).Replace(k_QueryPlaceholder, Uri.EscapeDataString(query));
        }

        // Selected text is searched as a whole, so its lines are joined instead of keeping only the first one
        internal static string GetSelectionSearchText(string selectedText)
        {
            if (string.IsNullOrEmpty(selectedText))
                return selectedText;

            var plainText = Regex.Replace(selectedText, k_HyperlinkTagPattern, string.Empty);
            return Regex.Replace(plainText, k_WhitespacePattern, " ");
        }

        internal static bool CanSearch(string message)
        {
            return BuildSearchQuery(message, searchEngine) != null;
        }

        internal static void Search(string message)
        {
            var url = BuildSearchUrl(message, searchEngine, customUrlTemplate);
            if (url != null)
                Help.BrowseURL(url);
        }
    }
}
