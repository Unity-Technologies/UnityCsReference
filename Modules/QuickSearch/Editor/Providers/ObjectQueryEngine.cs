// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.Search.Providers
{
    enum MissingReferenceFilter
    {
        Script,
        Asset,
        Prefab,
        Any
    }

    class ObjectQueryEngine : ObjectQueryEngine<UnityEngine.Object>
    {
    }

    class ObjectQueryEngine<T> where T : UnityEngine.Object
    {
        protected readonly List<T> m_Objects;
        protected readonly Dictionary<int, GOD> m_GODS = new Dictionary<int, GOD>();
        protected static readonly QueryValidationOptions k_QueryEngineOptions = new QueryValidationOptions { validateFilters = true, skipNestedQueries = true };
        protected readonly QueryEngine<T> m_QueryEngine = new QueryEngine<T>(k_QueryEngineOptions);
        protected HashSet<SearchProposition> m_TypePropositions;

        [ThreadStatic] protected bool m_DoFuzzyMatch;
        [ThreadStatic] List<int> m_FuzzyMatches = new List<int>();

        public QueryEngine<T> engine => m_QueryEngine;

        private static readonly char[] s_EntrySeparators = { '/', ' ', '_', '-', '.' };
        private static readonly SearchProposition[] s_FixedPropositions = new SearchProposition[]
        {
            new SearchProposition(label: "id:", null, "Search object by ID"),
            new SearchProposition(label: "path:", null, "Search object by transform path"),
            new SearchProposition(label: "tag:", null, "Search object with tag"),
            new SearchProposition(label: "layer:", "layer>0", "Search object by layer (number)"),
            new SearchProposition(label: "size:", null, "Search object by volume size"),
            new SearchProposition(label: "components:", "components>=2", "Search object with more than # components"),
            new SearchProposition(label: "is:", null, "Search object by state"),
            new SearchProposition(label: "is:child", null, "Search object with a parent"),
            new SearchProposition(label: "is:leaf", null, "Search object without children"),
            new SearchProposition(label: "is:root", null, "Search root objects"),
            new SearchProposition(label: "is:visible", null, "Search view visible objects"),
            new SearchProposition(label: "is:hidden", null, "Search hierarchically hidden objects"),
            new SearchProposition(label: "is:static", null, "Search static objects"),
            new SearchProposition(label: "is:prefab", null, "Search prefab objects"),
            new SearchProposition(label: "prefab:root", null, "Search prefab roots"),
            new SearchProposition(label: "prefab:top", null, "Search top-level prefab root instances"),
            new SearchProposition(label: "prefab:instance", null, "Search objects that are part of a prefab instance"),
            new SearchProposition(label: "prefab:nonasset", null, "Search prefab objects that are not part of an asset"),
            new SearchProposition(label: "prefab:asset", null, "Search prefab objects that are part of an asset"),
            new SearchProposition(label: "prefab:model", null, "Search prefab objects that are part of a model"),
            new SearchProposition(label: "prefab:regular", null, "Search regular prefab objects"),
            new SearchProposition(label: "prefab:variant", null, "Search variant prefab objects"),
            new SearchProposition(label: "prefab:modified", null, "Search modified prefab assets"),
            new SearchProposition(label: "prefab:altered", null, "Search modified prefab instances"),
            new SearchProposition(label: "t:", null, "Search object by type", priority: -1),
            new SearchProposition(label: "ref:", null, "Search object references"),
        };

        protected class GOD
        {
            public string id;
            public string path;
            public string tag;
            public string[] types;
            public string[] words;
            public HashSet<int> refs;
            public string[] attrs;

            public int? layer;
            public float size = float.MaxValue;

            public bool? isChild;
            public bool? isLeaf;
            public bool missingScript;
            public bool missingPrefab;
            public bool missingAssetReference;
        }

        public bool InvalidateObject(int instanceId)
        {
            return m_GODS.Remove(instanceId);
        }

        public ObjectQueryEngine()
            : this(new T[0])
        {
        }

        public ObjectQueryEngine(IEnumerable<T> objects)
        {
            m_Objects = objects.ToList();
            m_QueryEngine.AddFilter<int>("id", GetId);
            m_QueryEngine.AddFilter("path", GetPath);
            m_QueryEngine.AddFilter<string>("is", OnIsFilter, new[] {":"});
            m_QueryEngine.AddFilter<MissingReferenceFilter>("missing", OnMissing, new[] { ":" });
            m_QueryEngine.AddFilter<string>("t", OnTypeFilter, new[] {"=", ":"});
            m_QueryEngine.SetFilter<string>("ref", GetReferences, new[] {"=", ":"}).AddTypeParser(s =>
            {
                if (!s.StartsWith("GlobalObjectId", StringComparison.Ordinal) || !GlobalObjectId.TryParse(s, out var gid))
                    return ParseResult<string>.none;

                if (gid.targetPrefabId == 0 && gid.identifierType != 2 && gid.identifierType != 4)
                    return new ParseResult<string>(true, AssetDatabase.GUIDToAssetPath(gid.assetGUID));

                return new ParseResult<string>(true, GlobalObjectId.GlobalObjectIdentifierToInstanceIDSlow(gid).ToString());
            });

            SearchValue.SetupEngine(m_QueryEngine);

            m_QueryEngine.AddOperatorHandler("=", (int? ev, int fv) => ev.HasValue && ev == fv);
            m_QueryEngine.AddOperatorHandler("!=", (int? ev, int fv) => ev.HasValue && ev != fv);
            m_QueryEngine.AddOperatorHandler("<=", (int? ev, int fv) => ev.HasValue && ev <= fv);
            m_QueryEngine.AddOperatorHandler("<", (int? ev, int fv) => ev.HasValue && ev < fv);
            m_QueryEngine.AddOperatorHandler(">=", (int? ev, int fv) => ev.HasValue && ev >= fv);
            m_QueryEngine.AddOperatorHandler(">", (int? ev, int fv) => ev.HasValue && ev > fv);

            m_QueryEngine.AddOperatorHandler("=", (float? ev, float fv) => ev.HasValue && ev == fv);
            m_QueryEngine.AddOperatorHandler("!=", (float? ev, float fv) => ev.HasValue && ev != fv);
            m_QueryEngine.AddOperatorHandler("<=", (float? ev, float fv) => ev.HasValue && ev <= fv);
            m_QueryEngine.AddOperatorHandler("<", (float? ev, float fv) => ev.HasValue && ev < fv);
            m_QueryEngine.AddOperatorHandler(">=", (float? ev, float fv) => ev.HasValue && ev >= fv);
            m_QueryEngine.AddOperatorHandler(">", (float? ev, float fv) => ev.HasValue && ev > fv);

            m_QueryEngine.SetSearchWordMatcher(OnSearchData);
            m_QueryEngine.SetSearchDataCallback(OnSearchData, s => s.ToLowerInvariant(), StringComparison.Ordinal);
        }

        public virtual void SetupQueryEnginePropositions()
        {}

        public virtual IEnumerable<SearchProposition> FindPropositions(SearchContext context, SearchPropositionOptions options)
        {
            if (options.StartsWith("t"))
                return FetchTypePropositions(options.HasAny(SearchPropositionFlags.FilterOnly) ? null : "t:");

            return s_FixedPropositions;
        }

        private HashSet<Type> FetchPropositionTypes()
        {
            var types = new HashSet<Type>();
            foreach (var o in m_Objects)
            {
                if (!o)
                    continue;
                types.Add(o.GetType());
                if (o is GameObject go)
                    types.UnionWith(go.GetComponents<Component>().Where(c => c).Select(c => c.GetType()));
            }

            return types;
        }

        private IEnumerable<SearchProposition> FetchTypePropositions(string prefixFilterId = "t:")
        {
            if (m_TypePropositions == null && m_Objects != null)
            {
                var types = FetchPropositionTypes();
                m_TypePropositions = new HashSet<SearchProposition>(types.Select(t => CreateTypeProposition(t, prefixFilterId)));
            }

            return m_TypePropositions ?? Enumerable.Empty<SearchProposition>();
        }

        static SearchProposition CreateTypeProposition(in Type t, string prefixFilterId)
        {
            var typeName = t.Name;
            var label = typeName;
            if (prefixFilterId != null)
                label = prefixFilterId + label;
            return new SearchProposition(label: label, null, $"Search {typeName} components", icon: Utils.FindTextureForType(t));
        }

        #region search_query_error_example
        public IEnumerable<T> Search(SearchContext context, SearchProvider provider, IEnumerable<T> subset = null)
        {
            var query = m_QueryEngine.ParseQuery(context.searchQuery, true);
            if (!query.valid)
            {
                context.AddSearchQueryErrors(query.errors.Select(e => new SearchQueryError(e, context, provider)));
                return Enumerable.Empty<T>();
            }

            m_DoFuzzyMatch = query.HasToggle("fuzzy");
            IEnumerable<T> gameObjects = subset ?? m_Objects;
            return query.Apply(gameObjects, false);
        }

        #endregion

        public virtual bool GetId(T obj, QueryFilterOperator op, int instanceId)
        {
            return instanceId == obj.GetInstanceID();
        }

        protected virtual string GetPath(T obj)
        {
            var god = GetGOD(obj);

            if (god.path == null)
                god.path = AssetDatabase.GetAssetPath(obj);

            return god.path;
        }

        protected GOD GetGOD(UnityEngine.Object obj)
        {
            var instanceId = obj.GetInstanceID();
            if (!m_GODS.TryGetValue(instanceId, out var god))
            {
                god = new GOD();
                m_GODS[instanceId] = god;
            }
            return god;
        }

        protected bool OnMissing(T obj, QueryFilterOperator op, MissingReferenceFilter value)
        {
            var god = GetGOD(obj);

            if (god.refs == null)
            {
                BuildReferences(ref god, obj);
            }

            switch(value)
            {
                case MissingReferenceFilter.Any:
                    return god.missingAssetReference || god.missingScript || god.missingPrefab;
                case MissingReferenceFilter.Script:
                    return god.missingScript;
                case MissingReferenceFilter.Asset:
                    return god.missingAssetReference;
                case MissingReferenceFilter.Prefab:
                    return god.missingPrefab;
                default:
                    return false;
            }
        }

        protected virtual bool OnIsFilter(T obj, QueryFilterOperator op, string value)
        {
            if (string.Equals(value, "object", StringComparison.Ordinal))
                return true;

            return false;
        }

        protected SearchValue FindPropertyValue(UnityEngine.Object obj, string propertyName)
        {
            var property = PropertySelectors.FindProperty(obj, propertyName, out var so);
            if (property == null)
                return SearchValue.invalid;

            var v = SearchValue.ConvertPropertyValue(property);
            so?.Dispose();
            return v;
        }

        protected string ToReplacementValue(SerializedProperty sp, string replacement)
        {
            switch (sp.propertyType)
            {
                case SerializedPropertyType.Integer: return replacement + ">0";
                case SerializedPropertyType.Boolean: return replacement + "=true";
                case SerializedPropertyType.Float: return replacement + ">=0.0";
                case SerializedPropertyType.String: return replacement + ":\"\"";
                case SerializedPropertyType.Enum: return replacement + ":";
                case SerializedPropertyType.ObjectReference: return replacement + ":";
                case SerializedPropertyType.Color: return replacement + "=#FFFFBB";
                case SerializedPropertyType.Bounds:
                case SerializedPropertyType.BoundsInt:
                case SerializedPropertyType.Rect:
                    return replacement + ">0";

                case SerializedPropertyType.Generic:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Vector2:
                case SerializedPropertyType.Vector3:
                case SerializedPropertyType.Vector4:
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.AnimationCurve:
                case SerializedPropertyType.Gradient:
                case SerializedPropertyType.Quaternion:
                case SerializedPropertyType.ExposedReference:
                case SerializedPropertyType.FixedBufferSize:
                case SerializedPropertyType.Vector2Int:
                case SerializedPropertyType.Vector3Int:
                case SerializedPropertyType.RectInt:
                case SerializedPropertyType.ManagedReference:
                default:
                    break;
            }

            return null;
        }

        bool OnTypeFilter(T obj, QueryFilterOperator op, string value)
        {
            if (!obj)
                return false;
            var god = GetGOD(obj);

            if (god.types == null)
            {
                var types = new HashSet<string>(new[] { obj.GetType().Name.ToLowerInvariant() });
                if (obj is GameObject go)
                {
                    if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
                        types.Add("prefab");

                    var gocs = go.GetComponents<Component>();
                    for (int componentIndex = 0; componentIndex < gocs.Length; ++componentIndex)
                    {
                        var c = gocs[componentIndex];
                        if (!c || (c.hideFlags & HideFlags.HideInInspector) == HideFlags.HideInInspector)
                            continue;

                        var componentType = c.GetType();
                        var shortName = componentType.Name;
                        types.Add(shortName.ToLowerInvariant());
                        if (componentType.FullName != shortName)
                            types.Add(componentType.FullName.ToLowerInvariant());
                    }
                }

                god.types = types.ToArray();
            }

            return CompareWords(op, value.ToLowerInvariant(), god.types);
        }

        private void BuildReferences(ref GOD god, T obj)
        {
            var refs = new HashSet<int>();

            BuildReferences(obj, ref god, refs);

            if (obj is GameObject go)
            {
                // Index any prefab reference
                AddReference(go, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go), refs);

                if (PrefabUtility.IsPrefabAssetMissing(go))
                {
                    god.missingPrefab = true;
                }

                var gocs = go.GetComponents<Component>();
                for (int componentIndex = 1; componentIndex < gocs.Length; ++componentIndex)
                {
                    var c = gocs[componentIndex];
                    if (c == null || !c)
                    {
                        god.missingScript = true;
                        continue;
                    }

                    if ((c.hideFlags & HideFlags.HideInInspector) == HideFlags.HideInInspector)
                        continue;
                    BuildReferences(c, ref god, refs);
                }
            }

            refs.Remove(obj.GetHashCode());
            god.refs = refs;
        }

        private void BuildReferences(UnityEngine.Object obj, ref GOD god, ICollection<int> refs)
        {
            if (!obj)
                return;
            try
            {
                using (var so = new SerializedObject(obj))
                {
                    var p = so.GetIterator();
                    var next = p.NextVisible(true);
                    while (next)
                    {
                        AddPropertyReferences(obj, p, ref god, refs);
                        next = p.NextVisible(p.hasVisibleChildren);
                    }
                }
            }
            catch
            {
                // Do not add any references if an exception occurs because of user code.
            }
        }

        private void AddPropertyReferences(UnityEngine.Object obj, SerializedProperty p, ref GOD god, ICollection<int> refs)
        {
            if (p.propertyType != SerializedPropertyType.ObjectReference)
                return;

            if (p.objectReferenceValue == null)
            {
                god.missingAssetReference = god.missingAssetReference || p.objectReferenceInstanceIDValue != 0;
                return;
            }

            var refValue = AssetDatabase.GetAssetPath(p.objectReferenceValue);
            if (string.IsNullOrEmpty(refValue) && p.objectReferenceValue is GameObject go)
                refValue = SearchUtils.GetTransformPath(go.transform);

            if (!string.IsNullOrEmpty(refValue))
                AddReference(p.objectReferenceValue, refValue, refs);
            refs.Add(p.objectReferenceValue.GetInstanceID());
            if (p.objectReferenceValue is Component c)
            {
                refs.Add(c.gameObject.GetInstanceID());
                var compRefValue = SearchUtils.GetTransformPath(c.gameObject.transform);
                AddReference(c.gameObject, compRefValue, refs);
            }

            // Add custom object cases
            if (p.objectReferenceValue is Material material && material.shader)
            {
                AddReference(material.shader, material.shader.name, refs);
            }
        }

        private bool AddReference(UnityEngine.Object refObj, string refValue, ICollection<int> refs)
        {
            if (string.IsNullOrEmpty(refValue))
                return false;

            refValue = refValue.ToLowerInvariant();
            if (refValue[0] == '/')
            {
                refs.Add(refValue.Substring(1).GetHashCode());
            }
            refs.Add(refValue.GetHashCode());

            var refType = refObj?.GetType().Name;
            if (refType != null)
                refs.Add(refType.ToLowerInvariant().GetHashCode());

            return true;
        }

        private bool GetReferences(T obj, QueryFilterOperator op, string value)
        {
            var god = GetGOD(obj);

            if (god.refs == null)
            {
                BuildReferences(ref god, obj);
            }

            if (god.refs.Count == 0)
                return false;

            if (Utils.TryParse(value, out int instanceId))
                return god.refs.Contains(instanceId);

            return god.refs.Contains(value.ToLowerInvariant().GetHashCode());
        }

        protected bool CompareWords(in QueryFilterOperator op, string value, in IEnumerable<string> words, StringComparison stringComparison = StringComparison.Ordinal)
        {
            if (op.type == FilterOperatorType.Equal)
                return words.Any(t => t.Equals(value, stringComparison));
            return words.Any(t => t.IndexOf(value, stringComparison) != -1);
        }

        IEnumerable<string> OnSearchData(T go)
        {
            var god = GetGOD(go);

            if (god.words == null)
            {
                god.words = SplitName(go.name, s_EntrySeparators)
                    .Select(w => w.ToLowerInvariant())
                    .Where(w => w.Length > 1)
                    .ToArray();
            }

            return god.words;
        }

        bool OnSearchData(string term, bool exactMatch, StringComparison sc, string word)
        {
            if (!exactMatch && m_DoFuzzyMatch)
            {
                m_FuzzyMatches.Clear();
                return FuzzySearch.FuzzyMatch(term, word, m_FuzzyMatches);
            }

            if (exactMatch)
                return string.Equals(term, word, sc);
            return word.IndexOf(term, sc) != -1;
        }

        private static IEnumerable<string> SplitName(string entry, char[] entrySeparators)
        {
            yield return entry;
            var cleanName = CleanName(entry);
            var nameTokens = cleanName.Split(entrySeparators);
            var scc = nameTokens.SelectMany(s => SearchUtils.SplitCamelCase(s)).Where(s => s.Length > 0);
            var fcc = scc.Aggregate("", (current, s) => current + s[0]);
            yield return fcc;
        }

        private static string CleanName(string s)
        {
            return s.Replace("(", "").Replace(")", "");
        }
    }
}
