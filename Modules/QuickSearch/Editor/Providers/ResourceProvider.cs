// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Search.Providers
{
    static class ResourcesProvider
    {
        internal interface IMatchOperation
        {
            Type filterType { get; }
            string name { get; }
            string matchToken { get; }
            Func<Object, string> matchWord { get; }
        }

        private struct MatchOperation<T> : IMatchOperation
        {
            public Type filterType => typeof(T);
            public string name { get; set; }
            public string matchToken { get; set; }
            public Func<Object, T> getFilterData { get; set; }

            public Func<Object, string> matchWord
            {
                get
                {
                    var tmpThis = this;
                    return o => tmpThis.getFilterData(o).ToString();
                }
            }
        }

        internal static string type = "res";
        internal static string displayName = "Resources";

        // Match operations for specific sub-filters
        private static readonly List<IMatchOperation> k_SubMatches = new List<IMatchOperation>
        {
            new MatchOperation<string> { name = "type", matchToken = "t", getFilterData = o => o.GetType().FullName},
            new MatchOperation<string> { name = "name", matchToken = "n", getFilterData = o => o.name},
            new MatchOperation<int> { name = "id", matchToken = "id", getFilterData = o => o.GetInstanceID()},
            new MatchOperation<string> { name = "tag", matchToken = "tag", getFilterData = o => { var go = o as GameObject; return go?.tag ?? ""; }}
        };

        // QueryEngine
        static QueryEngine<Object> s_QueryEngine;

        // Descriptors for specific types of resources
        static readonly List<ResourceDescriptor> k_Descriptors = Assembly
            .GetAssembly(typeof(ResourceDescriptor))
            .GetTypes().Where(t => typeof(ResourceDescriptor).IsAssignableFrom(t))
            .Select(t => t.GetConstructor(new Type[] {})?.Invoke(new object[] {}) as ResourceDescriptor)
            .OrderBy(descriptor => descriptor.Priority).Reverse().ToList();

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            return new SearchProvider(type, displayName)
            {
                filterId = "res:",
                showDetails = true,
                showDetailsOptions = ShowDetailsOptions.Inspector,
                fetchItems = (context, items, provider) => SearchItems(context, provider),
                toObject = (item, type) => GetItemObject(item),
                isExplicitProvider = true,
                fetchDescription = FetchDescription,
                fetchThumbnail = FetchThumbnail,
                fetchPreview = FetchPreview,
                trackSelection = (item, context) => TrackSelection(item),
                startDrag = (item, context) => DragItem(item, context),
                onEnable = OnEnable
            };
        }

        static void OnEnable()
        {
            if (s_QueryEngine == null)
            {
                s_QueryEngine = new QueryEngine<Object>();

                foreach (var matchOperation in k_SubMatches)
                {
                    AddFilter(matchOperation);
                }

                s_QueryEngine.SetSearchDataCallback(DefaultSearchDataCallback);
            }
        }

        static void AddFilter(IMatchOperation matchOperation)
        {
            var thisClassType = typeof(ResourcesProvider);
            var method = thisClassType.GetMethod("AddTypedFilter", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
                throw new NullReferenceException("Cannot find method 'AddTypedFilter'");
            var typedMethod = method.MakeGenericMethod(matchOperation.filterType);
            typedMethod.Invoke(null, new object[] { matchOperation });
        }

        internal static void AddTypedFilter<T>(IMatchOperation matchOperation)
        {
            var typedMatchOperation = (MatchOperation<T>)matchOperation;
            s_QueryEngine.AddFilter(typedMatchOperation.matchToken, typedMatchOperation.getFilterData);
        }

        static string[] DefaultSearchDataCallback(Object data)
        {
            var go = data as GameObject;
            var tag = go?.tag ?? "";
            return new[] { data.GetType().FullName, data.name, data.GetInstanceID().ToString(), tag };
        }

        [SearchActionsProvider]
        internal static IEnumerable<SearchAction> ActionHandlers()
        {
            return new[]
            {
                new SearchAction(type, "select", null, "Select resource") { handler = (item) => TrackSelection(item) }
            };
        }

        private static IEnumerable<SearchItem> SearchItems(SearchContext context, SearchProvider provider)
        {
            if (string.IsNullOrEmpty(context.searchQuery))
                yield break;
            var focusedTokens = context.textFilters.Where(filter => filter.EndsWith(":", System.StringComparison.OrdinalIgnoreCase)).ToList();
            var sanitizedSearchQuery = context.searchQuery;
            foreach (var focusedFilter in focusedTokens)
            {
                sanitizedSearchQuery = sanitizedSearchQuery.Replace(focusedFilter, "");
            }

            if (focusedTokens.Count > 0)
            {
                var focusedFilters = focusedTokens.Select(token => token.Substring(0, token.Length - 1)).ToList();
                var focusedMatchOperations = k_SubMatches.Where(subMatch => focusedFilters.Contains(subMatch.matchToken));
                s_QueryEngine.SetSearchDataCallback(o =>
                {
                    return focusedMatchOperations.Select(matchOp => matchOp.matchWord(o));
                });
            }

            var query = s_QueryEngine.Parse(sanitizedSearchQuery);
            if (!query.valid)
            {
                context.AddSearchQueryErrors(query.errors.Select(e => new SearchQueryError(e.index, e.length, e.reason, context, provider)));
                yield break;
            }

            var allObjects = Resources.FindObjectsOfTypeAll(typeof(Object));
            var filteredObjects = query.Apply(allObjects);

            foreach (var obj in filteredObjects)
            {
                var id = obj.GetInstanceID().ToString();
                var label = $"{obj.name} [{obj.GetType()}] ({obj.GetInstanceID()})";
                yield return provider.CreateItem(context, id, label, null, null, obj.GetInstanceID());
            }

            // Put back the default search callback
            if (focusedTokens.Count > 0)
                s_QueryEngine.SetSearchDataCallback(DefaultSearchDataCallback);
        }

        private static void DragItem(SearchItem item, SearchContext context)
        {
            if (context.selection.Count > 1)
                Utils.StartDrag(context.selection.Select(i => GetItemObject(i)).ToArray(), item.label);
            else
                Utils.StartDrag(new[] { GetItemObject(item) }, item.label);
        }

        private static string FetchDescription(SearchItem item, SearchContext context)
        {
            var obj = GetItemObject(item);
            var sb = new StringBuilder();
            var matchingDescriptor = k_Descriptors.Where(descriptor => descriptor.Match(obj)).ToList();
            foreach (var descriptor in matchingDescriptor)
            {
                if (!descriptor.GetDescription(obj, sb))
                    break;
            }
            item.description = sb.ToString();
            return item.description;
        }

        private static Texture2D FetchThumbnail(SearchItem item, SearchContext context)
        {
            if (item.thumbnail)
                return item.thumbnail;

            var obj = GetItemObject(item);
            var descriptor = k_Descriptors.FirstOrDefault(desc => desc.Match(obj));
            return descriptor == null ? Icons.quicksearch : descriptor.GetThumbnail(obj);
        }

        static Texture2D FetchPreview(SearchItem item, SearchContext context, Vector2 size, FetchPreviewOptions options)
        {
            if (item.preview)
                return item.preview;

            var obj = GetItemObject(item);
            var descriptor = k_Descriptors.FirstOrDefault(desc => desc.Match(obj));
            return descriptor == null ? Icons.quicksearch : descriptor.GetPreview(obj, (int)size.x, (int)size.y);
        }

        private static void TrackSelection(SearchItem item)
        {
            var obj = GetItemObject(item);
            var descriptor = k_Descriptors.FirstOrDefault(desc => desc.Match(obj));
            descriptor?.TrackSelection(obj);
        }

        private static Object GetItemObject(SearchItem item)
        {
            var instanceID = Convert.ToInt32(item.id);
            return EditorUtility.InstanceIDToObject(instanceID);
        }
    }
}
