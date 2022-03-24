// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Search
{
    class SearchTemplateAttribute : Attribute
    {
        static List<SearchTemplateAttribute> s_QueryProviders;

        public string providerId { get; set; }
        public string description { get; set; }
        public UnityEngine.Search.SearchViewFlags viewFlags { get; set; }

        private Func<IEnumerable<string>> multiEntryHandler;

        public SearchTemplateAttribute(string description = null, string providerId = null, UnityEngine.Search.SearchViewFlags viewFlags = UnityEngine.Search.SearchViewFlags.None)
        {
            this.providerId = providerId;
            this.description = description;
            this.viewFlags = viewFlags;
        }

        internal static IEnumerable<ISearchQuery> GetAllQueries()
        {
            return providers.SelectMany(p => p.CreateQuery());
        }

        IEnumerable<ISearchQuery> CreateQuery()
        {
            var queries = multiEntryHandler();
            foreach (var query in queries)
            {
                var q = new SearchQuery();
                var provider = SearchService.GetProvider(providerId);
                var searchText = query;
                if (provider != null && !query.StartsWith(provider.filterId))
                {
                    searchText = $"{provider.filterId} {query}";
                }
                q.searchText = searchText;
                q.displayName = searchText;
                if (!string.IsNullOrEmpty(providerId))
                    q.viewState.providerIds = new[] { providerId };
                q.description = description;
                q.viewState.SetSearchViewFlags(viewFlags);
                yield return q;
            }
        }

        internal static IEnumerable<SearchTemplateAttribute> providers
        {
            get
            {
                if (s_QueryProviders == null)
                    RefreshQueryProviders();
                return s_QueryProviders;
            }
        }

        internal static void RefreshQueryProviders()
        {
            s_QueryProviders = new List<SearchTemplateAttribute>();
            var methods = TypeCache.GetMethodsWithAttribute<SearchTemplateAttribute>();
            foreach (var mi in methods)
            {
                try
                {
                    var attr = mi.GetCustomAttributes(typeof(SearchTemplateAttribute), false).Cast<SearchTemplateAttribute>().First();
                    if (mi.ReturnType == typeof(string))
                    {
                        var singleEntryHandler = Delegate.CreateDelegate(typeof(Func<string>), mi) as Func<string>;
                        attr.multiEntryHandler = () => new[] { singleEntryHandler() };
                    }
                    else
                    {
                        attr.multiEntryHandler = Delegate.CreateDelegate(typeof(Func<IEnumerable<string>>), mi) as Func<IEnumerable<string>>;
                    }

                    s_QueryProviders.Add(attr);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Cannot register State provider: {mi.Name}\n{e}");
                }
            }
        }
    }
}
