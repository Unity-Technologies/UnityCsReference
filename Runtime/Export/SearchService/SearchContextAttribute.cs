// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Search
{
    [Flags]
    public enum SearchViewFlags
    {
        None = 0,

        Debug = 1 << 4,
        NoIndexing = 1 << 5,
        Packages = 1 << 8,

        // First 10 bits are reserved to map editor SearchFlags values

        OpenLeftSidePanel = 1 << 11,
        OpenInspectorPreview = 1 << 12,
        Centered = 1 << 13,
        HideSearchBar = 1 << 14,
        CompactView = 1 << 15,
        ListView = 1 << 16,
        GridView = 1 << 17,
        TableView = 1 << 18,
        EnableSearchQuery = 1 << 19,
        DisableInspectorPreview = 1 << 20,
        DisableSavedSearchQuery = 1 << 21
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SearchContextAttribute : PropertyAttribute
    {
        public string query { get; private set; }
        public string[] providerIds { get; private set; }
        public Type[] instantiableProviders { get; private set; }
        public SearchViewFlags flags { get; private set; }

        public SearchContextAttribute(string query)
            : this(query, null, SearchViewFlags.None)
        {}

        public SearchContextAttribute(string query, SearchViewFlags flags)
            : this(query, null, flags)
        {}

        public SearchContextAttribute(string query, string providerIdsCommaSeparated)
            : this(query, providerIdsCommaSeparated, SearchViewFlags.None)
        {}

        public SearchContextAttribute(string query, string providerIdsCommaSeparated, SearchViewFlags flags)
            : this(query, flags, providerIdsCommaSeparated, null)
        {}

        public SearchContextAttribute(string query, params Type[] instantiableProviders)
            : this(query, SearchViewFlags.None, null, instantiableProviders)
        {}

        public SearchContextAttribute(string query, SearchViewFlags flags, params Type[] instantiableProviders)
            : this(query, flags, null, instantiableProviders)
        {}

        public SearchContextAttribute(string query, SearchViewFlags flags, string providerIdsCommaSeparated, params Type[] instantiableProviders)
        {
            this.query = string.IsNullOrEmpty(query) || query.EndsWith(" ") ? query : $"{query} ";
            this.providerIds = providerIdsCommaSeparated?.Split(',', ';') ?? new string[0];
            this.instantiableProviders = instantiableProviders ?? new Type[0];
            this.flags = flags;
        }
    }
}
