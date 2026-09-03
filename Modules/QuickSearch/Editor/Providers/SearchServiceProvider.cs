// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: Search not yet converted
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace UnityEditor.Search
{
    /// <summary>
    /// Basic Search Provider for items that are not part of any providers.
    /// This provider is not registered.
    /// </summary>
    class SearchServiceProvider : SearchProvider
    {
        static readonly ScopedLazy<SearchServiceProvider, CodeLoadedScope> s_ScopedLazy = new(() => new SearchServiceProvider());

        public static SearchProvider Instance => s_ScopedLazy.Value;

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
                    #pragma warning disable UAC2001 // Avoid Linq
                    foreach (var c in PropertySelectors.Enumerate(context.searchView.results.Take(10)))
#pragma warning restore UAC2001
                        yield return new SearchProposition(category: category, label: $"{token}{c.content.text ?? c.path}", $"{c.selector}\t", $"Property ({c.selector})");
                }

                if (token == '@')
                {
                    #pragma warning disable UAC2001 // Avoid Linq
                    foreach (var s in SelectorManager.selectors.Where(s => s.printable))
#pragma warning restore UAC2001
                        yield return new SearchProposition(category: category, label: $"{token}{s.label}", help: s.description ?? "Selector", replacement: $"@{s.label}\t");
                }
            }
        }

        public static new SearchItem CreateItem(SearchContext context, string id, int score, string label, string description, Texture2D thumbnail, object @ref)
        {
            return Instance.CreateItem(context, id, score, label, description, thumbnail, @ref);
        }

        internal static SearchItem CreateItem(string id, string label, string description, object value)
        {
            var provider = Instance;
            var newItem = provider.CreateItem(provider.defaultContext, id, 0, label, description, null, null);
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
        public SearchItem item { get; set; }
        private volatile bool m_Disposed;

        public void Dispose()
        {
            if (m_Disposed || !this)
                return;

            m_Disposed = true;

            if (item != null)
            {
                // Evict from the owning item's caches so a later ToObject() can't hand
                // back this destroyed object.
                item.RemoveCachedObject(this);
                if (ReferenceEquals(item.data, this))
                    item.data = null;
                item = null;
            }

            DestroyImmediate(this);
        }

        public override string ToString()
        {
            return item.value.ToString();
        }
    }

    [CustomEditor(typeof(SearchServiceItem))]
    class SearchServiceItemEditor : Editor
    {
        public SearchItem item { get; set; }

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
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
