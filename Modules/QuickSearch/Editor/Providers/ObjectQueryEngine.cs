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

        private static readonly char[] s_EntrySeparators = { '/', ' ', '_', '-', '.' };
        private static readonly SearchProposition[] s_FixedPropositions = new SearchProposition[]
        {
            new SearchProposition("id:", null, "Search object by ID"),
            new SearchProposition("path:", null, "Search object by transform path"),
            new SearchProposition("tag:", null, "Search object with tag"),
            new SearchProposition("layer:", "layer>0", "Search object by layer (number)"),
            new SearchProposition("size:", null, "Search object by volume size"),
            new SearchProposition("components:", "components>=2", "Search object with more than # components"),
            new SearchProposition("is:", null, "Search object by state"),
            new SearchProposition("is:child", null, "Search object with a parent"),
            new SearchProposition("is:leaf", null, "Search object without children"),
            new SearchProposition("is:root", null, "Search root objects"),
            new SearchProposition("is:visible", null, "Search view visible objects"),
            new SearchProposition("is:hidden", null, "Search hierarchically hidden objects"),
            new SearchProposition("is:static", null, "Search static objects"),
            new SearchProposition("is:prefab", null, "Search prefab objects"),
            new SearchProposition("prefab:root", null, "Search prefab roots"),
            new SearchProposition("prefab:top", null, "Search top-level prefab root instances"),
            new SearchProposition("prefab:instance", null, "Search objects that are part of a prefab instance"),
            new SearchProposition("prefab:nonasset", null, "Search prefab objects that are not part of an asset"),
            new SearchProposition("prefab:asset", null, "Search prefab objects that are part of an asset"),
            new SearchProposition("prefab:model", null, "Search prefab objects that are part of a model"),
            new SearchProposition("prefab:regular", null, "Search regular prefab objects"),
            new SearchProposition("prefab:variant", null, "Search variant prefab objects"),
            new SearchProposition("prefab:modified", null, "Search modified prefab assets"),
            new SearchProposition("prefab:altered", null, "Search modified prefab instances"),
            new SearchProposition("t:", null, "Search object by type", priority: -1),
            new SearchProposition("ref:", null, "Search object references"),
            new SearchProposition("p", "p(", "Search object's properties"),
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

            m_QueryEngine.AddFilter<string>("t", OnTypeFilter, new[] {"=", ":"});
            m_QueryEngine.AddFilter<string>("ref", GetReferences, new[] {"=", ":"});

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

            m_QueryEngine.SetSearchDataCallback(OnSearchData, s => s.ToLowerInvariant(), StringComparison.Ordinal);
        }

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
            return new SearchProposition(label, null, $"Search {typeName} components", icon: Utils.FindTextureForType(t));
        }

        #region search_query_error_example
        public IEnumerable<T> Search(SearchContext context, SearchProvider provider, IEnumerable<T> subset = null)
        {
            var query = m_QueryEngine.Parse(context.searchQuery, true);
            if (!query.valid)
            {
                context.AddSearchQueryErrors(query.errors.Select(e => new SearchQueryError(e, context, provider)));
                return new T[] {};
            }

            IEnumerable<T> gameObjects = subset ?? m_Objects;
            return query.Apply(gameObjects, false);
        }

        #endregion

        public virtual bool GetId(T obj, string op, int instanceId)
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

        protected virtual bool OnIsFilter(T obj, string op, string value)
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

            var v = ConvertPropertyValue(property);
            so?.Dispose();
            return v;
        }

        public static SearchValue ConvertPropertyValue(in SerializedProperty sp)
        {
            switch (sp.propertyType)
            {
                case SerializedPropertyType.Integer: return new SearchValue(Convert.ToDouble(sp.intValue));
                case SerializedPropertyType.Boolean: return new SearchValue(sp.boolValue);
                case SerializedPropertyType.Float: return new SearchValue(sp.floatValue);
                case SerializedPropertyType.String: return new SearchValue(sp.stringValue);
                case SerializedPropertyType.Enum: return new SearchValue(sp.enumNames[sp.enumValueIndex]);
                case SerializedPropertyType.ObjectReference: return new SearchValue(sp.objectReferenceValue?.name);
                case SerializedPropertyType.Bounds: return new SearchValue(sp.boundsValue.size.magnitude);
                case SerializedPropertyType.BoundsInt: return new SearchValue(sp.boundsIntValue.size.magnitude);
                case SerializedPropertyType.Rect: return new SearchValue(sp.rectValue.size.magnitude);
                case SerializedPropertyType.Color: return new SearchValue(sp.colorValue);
                case SerializedPropertyType.Generic: break;
                case SerializedPropertyType.LayerMask: break;
                case SerializedPropertyType.Vector2: break;
                case SerializedPropertyType.Vector3: break;
                case SerializedPropertyType.Vector4: break;
                case SerializedPropertyType.ArraySize: break;
                case SerializedPropertyType.Character: break;
                case SerializedPropertyType.AnimationCurve: break;
                case SerializedPropertyType.Gradient: break;
                case SerializedPropertyType.Quaternion: break;
                case SerializedPropertyType.ExposedReference: break;
                case SerializedPropertyType.FixedBufferSize: break;
                case SerializedPropertyType.Vector2Int: break;
                case SerializedPropertyType.Vector3Int: break;
                case SerializedPropertyType.RectInt: break;
                case SerializedPropertyType.ManagedReference: break;
            }

            if (sp.isArray)
                return new SearchValue(sp.arraySize);

            return SearchValue.invalid;
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

        bool OnTypeFilter(T obj, string op, string value)
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
                    for (int componentIndex = 1; componentIndex < gocs.Length; ++componentIndex)
                    {
                        var c = gocs[componentIndex];
                        if (!c || (c.hideFlags & HideFlags.HideInInspector) == HideFlags.HideInInspector)
                            continue;

                        types.Add(c.GetType().Name.ToLowerInvariant());
                    }
                }

                god.types = types.ToArray();
            }

            return CompareWords(op, value.ToLowerInvariant(), god.types);
        }

        private void BuildReferences(UnityEngine.Object obj, ICollection<int> refs)
        {
            if (!obj)
                return;
            using (var so = new SerializedObject(obj))
            {
                var p = so.GetIterator();
                var next = p.NextVisible(true);
                while (next)
                {
                    AddPropertyReferences(obj, p, refs);
                    next = p.NextVisible(p.hasVisibleChildren);
                }
            }
        }

        private void AddPropertyReferences(UnityEngine.Object obj, SerializedProperty p, ICollection<int> refs)
        {
            if (p.propertyType != SerializedPropertyType.ObjectReference || !p.objectReferenceValue)
                return;

            var refValue = AssetDatabase.GetAssetPath(p.objectReferenceValue);
            if (string.IsNullOrEmpty(refValue) && p.objectReferenceValue is GameObject go)
                refValue = SearchUtils.GetTransformPath(go.transform);

            if (!string.IsNullOrEmpty(refValue))
                AddReference(p.objectReferenceValue, refValue, refs);
            refs.Add(p.objectReferenceValue.GetInstanceID());
            if (p.objectReferenceValue is Component c)
                refs.Add(c.gameObject.GetInstanceID());

            // Add custom object cases
            if (p.objectReferenceValue is Material material)
            {
                if (material.shader)
                    AddReference(material.shader, material.shader.name, refs);
            }
        }

        private bool AddReference(UnityEngine.Object refObj, string refValue, ICollection<int> refs)
        {
            if (string.IsNullOrEmpty(refValue))
                return false;

            if (refValue[0] == '/')
                refValue = refValue.Substring(1);
            refs.Add(refValue.ToLowerInvariant().GetHashCode());

            var refType = refObj?.GetType().Name;
            if (refType != null)
                refs.Add(refType.ToLowerInvariant().GetHashCode());

            return true;
        }

        private bool GetReferences(T obj, string op, string value)
        {
            var god = GetGOD(obj);

            if (god.refs == null)
            {
                var refs = new HashSet<int>();

                BuildReferences(obj, refs);

                if (obj is GameObject go)
                {
                    // Index any prefab reference
                    AddReference(go, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go), refs);

                    var gocs = go.GetComponents<Component>();
                    for (int componentIndex = 1; componentIndex < gocs.Length; ++componentIndex)
                    {
                        var c = gocs[componentIndex];
                        if (!c || (c.hideFlags & HideFlags.HideInInspector) == HideFlags.HideInInspector)
                            continue;
                        BuildReferences(c, refs);
                    }
                }

                refs.Remove(obj.GetHashCode());
                god.refs = refs;
            }

            if (Utils.TryParse(value, out int instanceId))
                return god.refs.Contains(instanceId);
            return god.refs.Contains(value.ToLowerInvariant().GetHashCode());
        }

        protected bool CompareWords(string op, string value, IEnumerable<string> words, StringComparison stringComparison = StringComparison.Ordinal)
        {
            if (op == "=")
                return words.Any(t => t.Equals(value, stringComparison));
            return words.Any(t => t.IndexOf(value, stringComparison) != -1);
        }

        IEnumerable<string> OnSearchData(T go)
        {
            var god = GetGOD(go);

            if (god.words == null)
            {
                god.words = SplitWords(go.name, s_EntrySeparators)
                    .Select(w => w.ToLowerInvariant())
                    .ToArray();
            }

            return god.words;
        }

        private static IEnumerable<string> SplitWords(string entry, char[] entrySeparators)
        {
            var nameTokens = CleanName(entry).Split(entrySeparators);
            var scc = nameTokens.SelectMany(s => SearchUtils.SplitCamelCase(s)).Where(s => s.Length > 0);
            var fcc = scc.Aggregate("", (current, s) => current + s[0]);
            return new[] { fcc, entry }.Concat(scc.Where(s => s.Length > 1))
                .Where(s => s.Length > 0)
                .Distinct();
        }

        private static string CleanName(string s)
        {
            return s.Replace("(", "").Replace(")", "");
        }
    }
}
