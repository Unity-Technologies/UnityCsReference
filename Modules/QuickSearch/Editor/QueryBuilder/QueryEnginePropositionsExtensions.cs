// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Search
{
    static class CombiningOperatorPropositionsExtensions
    {
        public static bool HasAny(this QueryEnginePropositionsExtension.CombiningOperatorPropositions flags, QueryEnginePropositionsExtension.CombiningOperatorPropositions f) => (flags & f) != 0;
        public static bool HasAll(this QueryEnginePropositionsExtension.CombiningOperatorPropositions flags, QueryEnginePropositionsExtension.CombiningOperatorPropositions all) => (flags & all) == all;
    }
    static class QueryEnginePropositionsExtension
    {
        const string k_BaseSearchPropositionDataKey = "searchproposition_";

        [Flags]
        public enum CombiningOperatorPropositions
        {
            None = 0,
            And = 1 << 0,
            Or = 1 << 1,
            Default = And | Or
        }

        public static IEnumerable<SearchProposition> GetCombiningOperatorPropositions(CombiningOperatorPropositions props, string category = "Operators")
        {
            var icon = Utils.LoadIcon("LayoutElement Icon");

            if (props.HasAny(CombiningOperatorPropositions.Or))
                yield return new SearchProposition(category: category, label: "OR", replacement: "or", icon: icon, color: QueryColors.combine);

            if (props.HasAny(CombiningOperatorPropositions.And))
                yield return new SearchProposition(category: category, label: "AND", replacement: "and", icon: icon, color: QueryColors.combine);
        }

        [Serializable]
        class SearchPropositionDescription
        {
            public string label;
            public string category;
            public string replacement;
            public string help;
            public string data;
            public int priority;
            public Texture2D icon;
            public string type;
            public TextCursorPlacement moveCursor;
            public Color color;

            public SearchProposition ToSearchProposition()
            {
                Type trueType = string.IsNullOrEmpty(type) ? null : Type.GetType(type);
                var trueReplacement = string.IsNullOrEmpty(replacement) ? null : replacement;
                return new SearchProposition(category: category, label: label, replacement: trueReplacement, data: data, help: help, priority: priority, icon: icon, type: trueType, moveCursor: moveCursor, color: color);
            }

            public static SearchPropositionDescription FromSearchProposition(SearchProposition proposition)
            {
                return new SearchPropositionDescription()
                {
                    label = proposition.label,
                    category = proposition.category,
                    replacement = proposition.replacement,
                    help = proposition.help,
                    data = proposition.data?.ToString() ?? Utils.FastToLower(proposition.label),
                    priority = proposition.priority,
                    icon = proposition.icon,
                    type = proposition.type?.FullName,
                    moveCursor = proposition.moveCursor,
                    color = proposition.color
                };
            }
        }

        public static IQueryEngineFilter AddOrUpdatePropositionData(this IQueryEngineFilter filter, string label, string category = null, string replacement = null, string help = null, string data = null,
                                       int priority = 0, Texture2D icon = null, System.Type type = null, Color color = default, TextCursorPlacement moveCursor = TextCursorPlacement.MoveAutoComplete)
        {
            var propositionDescription = new SearchPropositionDescription()
            {
                label = label,
                category = category,
                replacement = replacement,
                help = help,
                data = data ?? Utils.FastToLower(label),
                priority = priority,
                icon = icon,
                type = type?.FullName,
                moveCursor = moveCursor,
                color = color
            };

            var propositionKey = GetPropositionKey(label);
            filter.AddOrUpdateMetaInfo(propositionKey, EditorJsonUtility.ToJson(propositionDescription));

            return filter;
        }

        static IQueryEngineFilter AddOrUpdatePropositionData(this IQueryEngineFilter filter, SearchProposition proposition)
        {
            var propositionDescription = SearchPropositionDescription.FromSearchProposition(proposition);

            var propositionKey = GetPropositionKey(proposition.label);
            filter.AddOrUpdateMetaInfo(propositionKey, EditorJsonUtility.ToJson(propositionDescription));

            return filter;
        }

        public static IQueryEngineFilter SetGlobalPropositionData(this IQueryEngineFilter filter, string category = null, string help = null, string data = null,
            int priority = 0, Texture2D icon = null, System.Type type = null, Color color = default, TextCursorPlacement moveCursor = TextCursorPlacement.MoveAutoComplete)
        {
            var propositionDescription = new SearchPropositionDescription()
            {
                category = category,
                help = help,
                data = data,
                priority = priority,
                icon = icon,
                type = type?.FullName,
                moveCursor = moveCursor,
                color = color
            };

            var propositionKey = GetGlobalPropositionKey();
            filter.AddOrUpdateMetaInfo(propositionKey, EditorJsonUtility.ToJson(propositionDescription));

            return filter;
        }

        public static IQueryEngineFilter AddPropositionsFromFilterType(this IQueryEngineFilter filter, string category = null, string help = null, string data = null,
            int priority = 0, Texture2D icon = null, System.Type type = null, Color color = default, TextCursorPlacement moveCursor = TextCursorPlacement.MoveAutoComplete)
        {
            var typePropositions = GetPropositionsFromType(filter, filter.type, category, type, priority, icon, color);
            var propositionOverride = new SearchProposition(category: category, label: string.Empty, help: help, data: data, priority: priority, icon: icon, type: type, color: color, moveCursor: moveCursor);
            var propositions = MergePropositions(typePropositions, propositionOverride);

            foreach (var proposition in propositions)
            {
                var propositionDescription = SearchPropositionDescription.FromSearchProposition(proposition);
                var propositionKey = GetPropositionKey(propositionDescription.label);
                filter.AddOrUpdateMetaInfo(propositionKey, EditorJsonUtility.ToJson(propositionDescription));
            }

            return filter;
        }

        public static SearchProposition GetProposition(this IQueryEngineFilter filter, string label)
        {
            if (string.IsNullOrEmpty(label))
                return SearchProposition.invalid;

            var key = GetPropositionKey(label);
            return GetPropositionFromKey(filter, key);
        }

        public static SearchProposition GetGlobalProposition(this IQueryEngineFilter filter)
        {
            var key = GetGlobalPropositionKey();
            return GetPropositionFromKey(filter, key);
        }

        public static SearchProposition GetPropositionFromKey(this IQueryEngineFilter filter, string propositionKey)
        {
            if (!filter.metaInfo.TryGetValue(propositionKey, out var propositionDescriptionStr))
                return SearchProposition.invalid;

            var propositionDescription = new SearchPropositionDescription();
            EditorJsonUtility.FromJsonOverwrite(propositionDescriptionStr, propositionDescription);
            return propositionDescription.ToSearchProposition();
        }

        public static IEnumerable<SearchProposition> GetPropositions(this QueryEngine queryEngine, CombiningOperatorPropositions props = CombiningOperatorPropositions.Default)
        {
            return queryEngine.GetPropositions<object>(props);
        }

        public static IEnumerable<SearchProposition> GetPropositions<TData>(this QueryEngine<TData> queryEngine, CombiningOperatorPropositions props = CombiningOperatorPropositions.Default)
        {
            var filters = queryEngine.GetAllFilters();
            return GetCombiningOperatorPropositions(props).Concat(filters.SelectMany(f => f.GetPropositions()));
        }

        public static IEnumerable<SearchProposition> GetPropositions(this IQueryEngineFilter filter)
        {
            // Here is the order in which the propositions are applied
            // 1. Global proposition
            // 2. Custom propositions
            if (filter.metaInfo == null || filter.metaInfo.Count == 0)
                return Enumerable.Empty<SearchProposition>();

            var propositionKeys = filter.metaInfo.Keys.Where(key => key.StartsWith(k_BaseSearchPropositionDataKey, StringComparison.Ordinal)).ToList();
            if (propositionKeys.Count == 0)
                return Enumerable.Empty<SearchProposition>();

            var globalProposition = GetGlobalProposition(filter);
            if (!globalProposition.valid)
                globalProposition = GetDefaultValidProposition();
            var validPropositions = propositionKeys.Select(key => filter.GetPropositionFromKey(key)).Where(p => p.valid);
            var customPropositions = MergePropositions(globalProposition, validPropositions);
            return MergePropositions(globalProposition, customPropositions);
        }

        static IEnumerable<SearchProposition> MergePropositions(IEnumerable<SearchProposition> basePropositions, IEnumerable<SearchProposition> overridePropositions)
        {
            var overrideMap = overridePropositions.ToDictionary(p => p.label);
            foreach (var baseProposition in basePropositions)
            {
                if (!overrideMap.TryGetValue(baseProposition.label, out var overrideProposition))
                    yield return baseProposition;
                else
                {
                    yield return MergeProposition(baseProposition, overrideProposition);
                    overrideMap.Remove(baseProposition.label);
                }
            }

            foreach (var overrideProposition in overrideMap.Values)
            {
                yield return overrideProposition;
            }
        }

        static IEnumerable<SearchProposition> MergePropositions(IEnumerable<SearchProposition> basePropositions, SearchProposition overrideProposition)
        {
            foreach (var baseProposition in basePropositions)
            {
                yield return MergeProposition(baseProposition, overrideProposition);
            }
        }

        static IEnumerable<SearchProposition> MergePropositions(SearchProposition baseProposition, IEnumerable<SearchProposition> overridePropositions)
        {
            foreach (var overrideProposition in overridePropositions)
            {
                yield return MergeProposition(baseProposition, overrideProposition);
            }
        }

        static SearchProposition MergeProposition(SearchProposition baseProposition, SearchProposition overrideProposition)
        {
            var merged = new SearchProposition(
                category: string.IsNullOrEmpty(overrideProposition.category) ? baseProposition.category : overrideProposition.category,
                label: string.IsNullOrEmpty(overrideProposition.label) ? baseProposition.label : overrideProposition.label,
                replacement: string.IsNullOrEmpty(overrideProposition.replacement) ? baseProposition.replacement : overrideProposition.replacement,
                help: string.IsNullOrEmpty(overrideProposition.help) ? baseProposition.help : overrideProposition.help,
                priority: overrideProposition.priority == 0 ? baseProposition.priority : overrideProposition.priority,
                moveCursor: overrideProposition.moveCursor == TextCursorPlacement.MoveAutoComplete ? baseProposition.moveCursor : overrideProposition.moveCursor,
                icon: overrideProposition.icon ? overrideProposition.icon : baseProposition.icon,
                type: overrideProposition.type ?? baseProposition.type,
                data: (overrideProposition.data == null || (overrideProposition.data is string s && string.IsNullOrEmpty(s))) ? baseProposition.data : overrideProposition.data,
                color: overrideProposition.color == default(Color) ? baseProposition.color : overrideProposition.color);
            return merged;
        }

        static IEnumerable<SearchProposition> GetPropositionsFromType(IQueryEngineFilter filter, Type type, string category = null, Type blockType = null, int priority = 0, Texture2D icon = null, Color color = default)
        {
            var filterId = filter.token;
            var defaultOperator = filter.supportedOperators?.FirstOrDefault() ?? ":";
            if (type.IsEnum)
                return SearchUtils.FetchEnumPropositions(type, category, filterId, defaultOperator, blockType, priority, icon, color);

            return Enumerable.Empty<SearchProposition>();
        }

        static string GetPropositionKey(string label)
        {
            return $"{k_BaseSearchPropositionDataKey}{label}";
        }

        static string GetGlobalPropositionKey()
        {
            return "searchpropositionglobal";
        }

        static SearchProposition GetDefaultValidProposition()
        {
            return new SearchProposition(category: null, label: string.Empty);
        }

        public static void AddPropositionsFromFilterAttributes<TAttribute>(this QueryEngine engine, string category = null, int priority = 0,
            string data = null, Type type = null, Texture2D icon = null, Color color = default,
            TextCursorPlacement moveCursor = TextCursorPlacement.MoveAutoComplete, Func<SearchProposition, SearchProposition> propositionTransformation = null)
            where TAttribute : QueryEngineFilterAttribute
        {
            engine.AddPropositionsFromFilterAttributes<object, TAttribute>(category, priority, data, type, icon, color, moveCursor, propositionTransformation);
        }

        public static void AddPropositionsFromFilterAttributes<TData, TAttribute>(this QueryEngine<TData> engine, string category = null,
            int priority = 0, string data = null, Type type = null, Texture2D icon = null, Color color = default,
            TextCursorPlacement moveCursor = TextCursorPlacement.MoveAutoComplete, Func<SearchProposition, SearchProposition> propositionTransformation = null)
            where TAttribute : QueryEngineFilterAttribute
        {
            var queryEngineFunctions = TypeCache.GetMethodsWithAttribute<TAttribute>();
            foreach (var mi in queryEngineFunctions)
            {
                var attr = mi.GetAttribute<TAttribute>();
                if (!engine.TryGetFilter(attr.token, out var filter))
                    continue;
                var op = attr.supportedOperators == null ? ">" : attr.supportedOperators[0];
                var value = op == ":" ? "" : "1";
                var label = attr.token;
                string help = null;

                if (mi.ReturnType == typeof(Vector2))
                    value = "(,)";
                if (mi.ReturnType == typeof(Vector3))
                    value = "(,,)";
                if (mi.ReturnType == typeof(Vector4))
                    value = "(,,,)";

                var replacement = attr.propositionReplacement ?? $"{attr.token}{op}{value}";

                var descriptionAttr = mi.GetAttribute<System.ComponentModel.DescriptionAttribute>();
                if (descriptionAttr != null)
                {
                    help = descriptionAttr.Description;
                }

                var proposition = new SearchProposition(label: label,
                    category: category,
                    replacement: replacement,
                    help: help,
                    data: data,
                    priority: priority,
                    icon: icon,
                    type: type,
                    color: color,
                    moveCursor: moveCursor);

                if (propositionTransformation != null)
                    proposition = propositionTransformation(proposition);

                filter.AddOrUpdatePropositionData(proposition);
            }
        }
    }
}
