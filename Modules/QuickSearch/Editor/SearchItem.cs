// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Search
{
    /// <summary>
    /// Indicates how the search item description needs to be formatted when presented to the user.
    /// </summary>
    [Flags]
    public enum SearchItemOptions
    {
        /// <summary>Use default description.</summary>
        None = 0,
        /// <summary>If the description is too long truncate it and add an ellipsis.</summary>
        Ellipsis = 1 << 0,
        /// <summary>If the description is too long keep the right part.</summary>
        RightToLeft = 1 << 1,
        /// <summary>Highlight parts of the description that match the Search Query</summary>
        Highlight = 1 << 2,
        /// <summary>Highlight parts of the description that match the Fuzzy Search Query</summary>
        FuzzyHighlight = 1 << 3,
        /// <summary>Use Label instead of description for shorter display.</summary>
        Compacted = 1 << 4
    }

    /// <summary>
    /// Search items are returned by the search provider when some results need to be shown to the user after a search is made.
    /// The search item holds all the data that will be used to sort and present the search results.
    /// </summary>
    [DebuggerDisplay("{id} | {label}")]
    public class SearchItem : IEquatable<SearchItem>, IComparable<SearchItem>
    {
        /// <summary>
        /// Unique id of this item among this provider items.
        /// </summary>
        public readonly string id;

        /// <summary>
        /// The item score can affect how the item gets sorted within the same provider.
        /// </summary>
        public int score;

        /// <summary>
        /// Display name of the item
        /// </summary>
        public string label;

        /// <summary>
        /// If no description is provided, SearchProvider.fetchDescription will be called when the item is first displayed.
        /// </summary>
        public string description;

        /// <summary>
        /// Various flags that dictates how the search item is displayed and used.
        /// </summary>
        public SearchItemOptions options;

        /// <summary>
        /// If no thumbnail are provider, SearchProvider.fetchThumbnail will be called when the item is first displayed.
        /// </summary>
        public Texture2D thumbnail;

        /// <summary>
        /// Large preview of the search item. Usually cached by fetchPreview.
        /// </summary>
        public Texture2D preview;

        /// <summary>
        /// Search provider defined content. It can be used to transport any data to custom search provider handlers (i.e. `fetchDescription`).
        /// </summary>
        public object data;

        /// <summary>
        /// Used to map value to a search item
        /// </summary>
        internal object value { get => m_Value ?? id; set => m_Value = value; }
        private object m_Value = null;

        /// <summary>
        /// Back pointer to the provider.
        /// </summary>
        public SearchProvider provider;

        /// <summary>
        /// Context used to create that item.
        /// </summary>
        public SearchContext context;

        private static readonly SearchProvider defaultProvider = new SearchProvider("default", "Default")
        {
            priority = int.MinValue,
            toObject = (item, type) => null,
            fetchLabel = (item, context) => item.label ?? item.id,
            fetchDescription = (item, context) => item.label ?? item.id,
            fetchThumbnail = (item, context) => item.thumbnail ?? Icons.logInfo,
            actions = new[] { new SearchAction("select", "select", null, null, (SearchItem item) => {}) }.ToList()
        };

        /// <summary>
        /// A search item representing none, usually used to clear the selection.
        /// </summary>
        public static readonly SearchItem none = new SearchItem(Guid.NewGuid().ToString())
        {
            label = "None",
            description = "Clear the current value",
            score = int.MinValue,
            thumbnail = Icons.clear,
            provider = defaultProvider
        };

        /// <summary>
        /// Construct a search item. Minimally a search item need to have a unique id for a given search query.
        /// </summary>
        /// <param name="_id"></param>
        public SearchItem(string _id)
        {
            id = _id;
            provider = defaultProvider;
        }

        /// <summary>
        /// Fetch and format label.
        /// </summary>
        /// <param name="context">Any search context for the item provider.</param>
        /// <param name="stripHTML">True if any HTML tags should be dropped.</param>
        /// <returns>The search item label</returns>
        public string GetLabel(SearchContext context, bool stripHTML = false)
        {
            if (label == null)
                label = provider?.fetchLabel?.Invoke(this, context);
            if (!stripHTML || string.IsNullOrEmpty(label))
                return label;
            return Utils.StripHTML(label);
        }

        /// <summary>
        /// Fetch and format description
        /// </summary>
        /// <param name="context">Any search context for the item provider.</param>
        /// <param name="stripHTML">True if any HTML tags should be dropped.</param>
        /// <returns>The search item description</returns>
        public string GetDescription(SearchContext context, bool stripHTML = false)
        {
            if (options.HasFlag(SearchItemOptions.Compacted) && provider?.fetchDescription != null)
                return provider?.fetchDescription?.Invoke(this, context);
            if (description == null)
                description = provider?.fetchDescription?.Invoke(this, context);
            if (!stripHTML || string.IsNullOrEmpty(description))
                return description;
            return Utils.StripHTML(description);
        }

        /// <summary>
        /// Check if 2 SearchItems have the same id.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns>Returns true if SearchItem have the same id.</returns>
        public int Compare(SearchItem x, SearchItem y)
        {
            if (x.id.Equals(y.id, StringComparison.Ordinal))
                return 0;
            int c = x.score.CompareTo(y.score);
            if (c == 0)
                return string.CompareOrdinal(x.id, y.id);
            return c;
        }

        /// <summary>
        /// Check if 2 SearchItems have the same id.
        /// </summary>
        /// <param name="other"></param>
        /// <returns>Returns true if SearchItem have the same id.</returns>
        public int CompareTo(SearchItem other)
        {
            return Compare(this, other);
        }

        /// <summary>
        /// Check if 2 SearchItems have the same id.
        /// </summary>
        /// <param name="other"></param>
        /// <returns>>Returns true if SearchItem have the same id.</returns>
        public override bool Equals(object other)
        {
            return other is SearchItem l && Equals(l);
        }

        /// <summary>
        /// Default Hash of a SearchItem
        /// </summary>
        /// <returns>A hash code for the current SearchItem</returns>
        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        /// <summary>
        /// Check if 2 SearchItems have the same id.
        /// </summary>
        /// <param name="other"></param>
        /// <returns>>Returns true if SearchItem have the same id.</returns>
        public bool Equals(SearchItem other)
        {
            return id.Equals(other.id);
        }
    }
}
