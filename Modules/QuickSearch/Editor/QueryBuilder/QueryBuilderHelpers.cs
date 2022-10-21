// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Search
{
    static class QueryColors
    {
        private static bool isDarkTheme => EditorGUIUtility.isProSkin;

        public static readonly Color area;
        public static readonly Color filter;
        public static readonly Color property;
        public static readonly Color type;
        public static readonly Color typeIcon;
        public static readonly Color word;
        public static readonly Color toggle;
        public static readonly Color combine;
        public static readonly Color expression;
        public static readonly Color textureBackgroundColor = new Color(0.2f, 0.2f, 0.25f, 0.95f);
        public static readonly Color selectedBorderColor = new Color(58 / 255f, 121 / 255f, 187 / 255f);
        public static readonly Color hoveredBorderColor = new Color(0.6f, 0.6f, 0.6f);
        public static readonly Color normalBorderColor = new Color(0.1f, 0.1f, 0.1f);
        public static readonly Color selectedTint = new Color(1.3f, 1.2f, 1.3f, 1f);
        public static readonly Color backgroundHoverTint = new Color(0.2f, 0.2f, 0.2f, 1f);

        static QueryColors()
        {
            ColorUtility.TryParseHtmlString("#74CBEE", out area);
            ColorUtility.TryParseHtmlString("#78CAB6", out filter);
            ColorUtility.TryParseHtmlString("#A38CD0", out property);
            ColorUtility.TryParseHtmlString("#FF6A00", out toggle);
            ColorUtility.TryParseHtmlString("#EBD05F", out type);
            ColorUtility.TryParseHtmlString("#EBD05F", out typeIcon);
            ColorUtility.TryParseHtmlString("#739CEB", out word);
            ColorUtility.TryParseHtmlString("#B7B741", out combine);
            ColorUtility.TryParseHtmlString("#8DBB65", out expression);
            if (isDarkTheme)
            {
                ColorUtility.TryParseHtmlString("#383838", out textureBackgroundColor);
                selectedBorderColor = Color.white;
            }
            else
            {
                ColorUtility.TryParseHtmlString("#CBCBCB", out textureBackgroundColor);
            }
        }
    }

    [Serializable]
    class QueryHelperSearchGroup
    {
        public static readonly Texture2D templateIcon = Utils.LoadIcon("UnityEditor/Search/SearchQueryAsset Icon");

        public enum QueryType
        {
            Template,
            Recent
        }

        public struct QueryData : IComparable<QueryData>, IEquatable<QueryData>
        {
            public QueryBuilder builder;
            public ISearchQuery query;
            public string searchText;
            public GUIContent icon;
            public GUIContent description;
            public GUIContent tooltip;
            public QueryType type;

            public int CompareTo(QueryData other)
            {
                return -query.lastUsedTime.CompareTo(other.query.lastUsedTime);
            }

            public override int GetHashCode()
            {
                return query.displayName.GetHashCode() ^ query.lastUsedTime.GetHashCode() * 53;
            }

            public override bool Equals(object other)
            {
                return other is QueryData l && Equals(l);
            }

            public bool Equals(QueryData other)
            {
                return query.lastUsedTime == other.query.lastUsedTime && string.Equals(query.displayName, other.query.displayName, StringComparison.Ordinal);
            }
        }

        public bool blockMode;
        public GUIContent title;
        public string displayName;
        public List<QueryData> queries;

        public QueryHelperSearchGroup(bool blockMode, string title)
        {
            this.blockMode = blockMode;
            displayName = title;
            this.title = new GUIContent(displayName);
            queries = new List<QueryData>();
        }

        public bool Add(ISearchQuery query, QueryType type, Texture2D icon)
        {
            if (string.IsNullOrEmpty(query.searchText))
                return false;

            QueryBuilder builder = null;
            if (blockMode)
            {
                builder = new QueryBuilder(query.searchText) { @readonly = true };
                foreach (var b in builder.blocks)
                    b.disableHovering = true;
            }

            if (builder == null || (builder.errors.Count == 0 && builder.blocks.Count > 0))
            {
                var desc = "";
                if (!string.IsNullOrEmpty(query.details))
                    desc = query.details;
                else if (!string.IsNullOrEmpty(query.displayName))
                    desc = query.displayName;

                queries.Add(new QueryData() { query = query, builder = builder,
                    icon = new GUIContent("", icon),
                    description = new GUIContent(desc, string.IsNullOrEmpty(query.filePath) ? null : templateIcon),
                    searchText = query.searchText,
                    type = type,
                    tooltip = new GUIContent("", query.searchText)
                });
                return true;
            }

            return false;
        }

        public void Add(string queryStr, QueryType type, Texture2D icon)
        {
            Add(CreateQuery(queryStr), type, icon);
        }

        internal static ISearchQuery CreateQuery(string queryStr)
        {
            var q = new SearchQuery() { searchText = queryStr };
            q.viewState.itemSize = SearchSettings.itemIconSize;
            return q;
        }

        public void UpdateTitle()
        {
            title.text = ($"{displayName} ({queries.Count})");
        }

        public bool HasQuery(ISearchQuery query, out int index)
        {
            index = queries.FindIndex(d => d.query == query);
            return index != -1;
        }

        public bool HasBuilder(QueryBuilder builder, out int index)
        {
            index = queries.FindIndex(d => d.builder == builder);
            return index != -1;
        }
    }

    class BlockSeparator : VisualElement
    {
        public BlockSeparator()
        {
            name = "QueryBlockSeparator";
            AddToClassList(QueryBlock.separatorClassName);
        }
    }
}
