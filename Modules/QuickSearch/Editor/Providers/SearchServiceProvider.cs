// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Search
{
    /// <summary>
    /// Basic Search Provider for items that are not part of any providers.
    /// This provider is not registered.
    /// </summary>
    class SearchServiceProvider : SearchProvider
    {
        static SearchProvider s_Provider;

        public SearchServiceProvider()
            : base("default", "Default")
        {
            priority = 2;
            fetchLabel = (item, context) => item.label ?? item.id;
            fetchDescription = (item, context) => FetchEvaluatedDescription(item, context);
            fetchThumbnail = (item, context) => Icons.logInfo;
            showDetails = true;
            showDetailsOptions = ShowDetailsOptions.Inspector;
            toObject = ToObject;
            fetchPropositions = FetchPropositions;
        }

        internal static SearchProvider CreateProvider()
        {
            if (s_Provider == null)
                s_Provider = new SearchServiceProvider();
            return s_Provider;
        }

        private IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
        {
            var category = options.HasAny(SearchPropositionFlags.QueryBuilder) ? "Expressions" : null;
            foreach (var e in EvaluatorManager.evaluators)
            {
                var help = e.description ?? "Expression evaluator";
                yield return new SearchProposition(category: category, $"{e.name}{{}}", $"{e.name.ToLowerInvariant()}{{\t}}", help, 1);
            }

            if (options.tokens.Length > 0 && options.tokens[0].Length > 0)
            {
                var token = options.tokens[0][0];
                if (token == '#')
                {
                    foreach (var c in PropertySelectors.Enumerate(context.searchView.results.Take(10)))
                        yield return new SearchProposition(category: category, label: $"{token}{c.content.text ?? c.path}", $"{c.selector}\t", $"Property ({c.selector})");
                }

                if (token == '@')
                {
                    foreach (var s in SelectorManager.selectors.Where(s => s.printable))
                        yield return new SearchProposition(category: category, label: $"{token}{s.label}", help: s.description ?? "Selector", replacement: $"@{s.label}\t");
                }
            }
        }

        public static new SearchItem CreateItem(SearchContext context, string id, int score, string label, string description, Texture2D thumbnail, object @ref)
        {
            return s_Provider.CreateItem(context, id, score, label, description, thumbnail, @ref);
        }

        internal static SearchItem CreateItem(string id, string label, string description, object value)
        {
            var newItem = s_Provider.CreateItem(s_Provider.defaultContext, id, 0, label, description, null, null);
            newItem.value = value;
            return newItem;
        }

        internal static UnityEngine.Object ToObject(SearchItem item, Type type)
        {
            var selectItemObject = (item.data as SearchServiceItem) ?? ScriptableObject.CreateInstance<SearchServiceItem>();
            selectItemObject.hideFlags |= HideFlags.DontSaveInEditor;
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
    }

    [ExcludeFromPreset]
    class SearchServiceItem : ScriptableObject, IDisposable
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

        ~SearchServiceItem()
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

    [CustomEditor(typeof(SearchServiceItem))]
    class SearchServiceItemEditor : Editor
    {
        public SearchItem item;

        internal void OnEnable()
        {
            item = ((SearchServiceItem)serializedObject.targetObject).item;
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
