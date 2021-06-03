// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Search
{
    class SearchExpressionProvider : SearchProvider
    {
        static SearchProvider s_ExpressionProvider;

        public SearchExpressionProvider()
            : base("expression", "Expression")
        {
            priority = 2;
            fetchItems = (context, items, provider) => EvaluateExpression(context, provider);
            fetchLabel = (item, context) => item.label ?? item.id;
            fetchDescription = (item, context) => FetchEvaluatedDescription(item, context);
            fetchThumbnail = (item, context) => Icons.logInfo;
            showDetails = true;
            showDetailsOptions = ShowDetailsOptions.Inspector;
            toObject = ToObject;
            fetchPropositions = FetchPropositions;
        }

        [SearchItemProvider]
        internal static SearchProvider CreateProvider()
        {
            if (s_ExpressionProvider == null)
                s_ExpressionProvider = new SearchExpressionProvider();
            return s_ExpressionProvider;
        }

        private IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
        {
            foreach (var e in EvaluatorManager.evaluators)
            {
                var help = e.description ?? "Expression evaluator";
                yield return new SearchProposition($"{e.name}{{}}", $"{e.name.ToLowerInvariant()}{{\t}}", help, 1);
            }

            if (options.tokens.Length > 0 && options.tokens[0].Length > 0)
            {
                var token = options.tokens[0][0];
                if (token == '#')
                {
                    foreach (var c in PropertySelectors.Enumerate(context.searchView.results.Take(10)))
                        yield return new SearchProposition($"{token}{c.content.text ?? c.path}", $"{c.selector}\t", $"Property ({c.selector})");
                }

                if (token == '@')
                {
                    foreach (var s in SelectorManager.selectors.Where(s => s.printable))
                        yield return new SearchProposition($"{token}{s.label}", $"@{s.label}\t", $"Selector");
                }
            }
        }

        public static new SearchItem CreateItem(SearchContext context, string id, int score, string label, string description, Texture2D thumbnail, object @ref)
        {
            return s_ExpressionProvider.CreateItem(context, id, score, label, description, thumbnail, @ref);
        }

        internal static SearchItem CreateItem(string id, string label, string description, object value)
        {
            var newItem = s_ExpressionProvider.CreateItem(id, 0, label, description, null, null);
            newItem.value = value;
            return newItem;
        }

        private IEnumerable<SearchItem> EvaluateExpression(SearchContext context, SearchProvider expressionProvider)
        {
            if (string.IsNullOrEmpty(context.searchText) || context.options.HasAny(SearchFlags.QueryString))
                yield break;

            var rootExpression = ParseExpression(context, expressionProvider);
            if (rootExpression == null || (rootExpression.types.HasAny(SearchExpressionType.QueryString) && rootExpression.parameters.Length == 0))
                yield break;

            using (SearchMonitor.GetView())
            {
                var evaluationFlags = SearchExpressionExecutionFlags.ThreadedEvaluation;
                var it = rootExpression.Execute(context, evaluationFlags).GetEnumerator();
                while (EvaluateExpression(context, expressionProvider, it))
                    yield return it.Current;

                TableView.SetupColumns(context, rootExpression);
            }
        }

        private SearchExpression ParseExpression(SearchContext context, SearchProvider expressionProvider)
        {
            try
            {
                return SearchExpression.Parse(context);
            }
            catch (SearchExpressionParseException ex)
            {
                var queryError = new SearchQueryError(ex.index, ex.length, ex.Message,
                    context, expressionProvider, fromSearchQuery: true, SearchQueryErrorType.Error);
                context.AddSearchQueryError(queryError);
                return null;
            }
        }

        private bool EvaluateExpression(SearchContext context, SearchProvider expressionProvider, IEnumerator<SearchItem> it)
        {
            try
            {
                return it.MoveNext();
            }
            catch (SearchExpressionEvaluatorException ex)
            {
                var queryError = new SearchQueryError(ex.errorView.startIndex, ex.errorView.Length, ex.Message,
                    context, expressionProvider, fromSearchQuery: true, SearchQueryErrorType.Error);
                context.AddSearchQueryError(queryError);
                return false;
            }
        }

        internal static UnityEngine.Object ToObject(SearchItem item, Type type)
        {
            var selectItemObject = (item.data as ExpressionItem) ?? ScriptableObject.CreateInstance<ExpressionItem>();
            selectItemObject.name = item.label ?? item.value.ToString();
            selectItemObject.item = item;
            if (item.data == null)
                item.data = selectItemObject;
            return selectItemObject;
        }

        private static string FetchEvaluatedDescription(SearchItem item, SearchContext context)
        {
            if (!item.options.HasFlag(SearchItemOptions.Compacted))
                return item.description;
            return $"{item.GetLabel(context, true)} > {item.value}";
        }

        public static ISearchView ShowWindow(string searchQuery, IEnumerable<SearchProvider> providers)
        {
            var context = SearchService.CreateContext(providers, searchQuery);
            return SearchService.ShowWindow(context, topic: "Expression", saveFilters: false);
        }

        public static ISearchView ShowWindow(string searchQuery)
        {
            return ShowWindow(searchQuery, SearchService.GetActiveProviders());
        }

        public static ISearchView ShowWindow(IEnumerable<SearchProvider> providers)
        {
            return ShowWindow(string.Empty, providers);
        }
    }

    [ExcludeFromPreset]
    class ExpressionItem : ScriptableObject, IDisposable
    {
        public SearchItem item;
        private volatile bool m_Disposed;

        protected virtual void Dispose(bool disposing)
        {
            if (m_Disposed || !this)
                return;

            if (disposing)
            {
                item.data = null;
                item = null;
            }

            DestroyImmediate(this);
            m_Disposed = true;
        }

        ~ExpressionItem()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public override string ToString()
        {
            return item.value.ToString();
        }
    }

    [CustomEditor(typeof(ExpressionItem))]
    class ExpressionItemEditor : Editor
    {
        public SearchItem item;

        internal void OnEnable()
        {
            item = ((ExpressionItem)serializedObject.targetObject).item;
        }

        public override void OnInspectorGUI()
        {
            EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth * 0.3f;
            EditorGUILayout.BeginVertical();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("provider", item.provider.name);
            EditorGUILayout.IntField("score", item.score);
            EditorGUILayout.TextField("id", item.id);
            EditorGUILayout.TextField("label", item.label);
            EditorGUILayout.TextField("description", item.description);
            if (item.data != null)
                EditorGUILayout.TextField("data", item.data.ToString());
            if (item.value != null)
                EditorGUILayout.TextField("value", item.value.ToString());
            EditorGUI.EndDisabledGroup();
            foreach (var f in item.GetFields())
                EditorGUILayout.TextField(Utils.GUIContentTemp(f.alias ?? f.name, f.name), f.value?.ToString());
            EditorGUILayout.EndVertical();
        }
    }
}
