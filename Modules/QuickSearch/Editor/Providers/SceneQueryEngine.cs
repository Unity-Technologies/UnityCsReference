// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.Search.Providers
{
    /// <summary>
    /// This is a <see cref="QueryEngineFilterAttribute"/> use for query in a scene provider.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SceneQueryEngineFilterAttribute : QueryEngineFilterAttribute
    {
        /// <summary>
        /// Create a filter with the corresponding token and supported operators.
        /// </summary>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (i.e. "id" in "id>=2").</param>
        /// <param name="supportedOperators">List of supported operator tokens. Null for all operators.</param>
        public SceneQueryEngineFilterAttribute(string token, string[] supportedOperators = null)
            : base(token, supportedOperators) {}

        /// <summary>
        /// Create a filter with the corresponding token, string comparison options and supported operators.
        /// </summary>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (i.e. "id" in "id>=2").</param>
        /// <param name="options">String comparison options.</param>
        /// <param name="supportedOperators">List of supported operator tokens. Null for all operators.</param>
        /// <remarks>This sets the flag overridesStringComparison to true.</remarks>
        public SceneQueryEngineFilterAttribute(string token, StringComparison options, string[] supportedOperators = null)
            : base(token, options, supportedOperators) {}

        /// <summary>
        /// Create a filter with the corresponding token, parameter transformer function and supported operators.
        /// </summary>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (i.e. "id" in "id>=2").</param>
        /// <param name="paramTransformerFunction">Name of the parameter transformer function to use with this filter. Tag the parameter transformer function with the appropriate ParameterTransformer attribute.</param>
        /// <param name="supportedOperators">List of supported operator tokens. Null for all operators.</param>
        /// <remarks>Sets the flag useParamTransformer to true.</remarks>
        public SceneQueryEngineFilterAttribute(string token, string paramTransformerFunction, string[] supportedOperators = null)
            : base(token, paramTransformerFunction, supportedOperators) {}

        /// <summary>
        /// Create a filter with the corresponding token, parameter transformer function, string comparison options and supported operators.
        /// </summary>
        /// <param name="token">The identifier of the filter. Typically what precedes the operator in a filter (i.e. "id" in "id>=2").</param>
        /// <param name="paramTransformerFunction">Name of the parameter transformer function to use with this filter. Tag the parameter transformer function with the appropriate ParameterTransformer attribute.</param>
        /// <param name="options">String comparison options.</param>
        /// <param name="supportedOperators">List of supported operator tokens. Null for all operators.</param>
        /// <remarks>Sets both overridesStringComparison and useParamTransformer flags to true.</remarks>
        public SceneQueryEngineFilterAttribute(string token, string paramTransformerFunction, StringComparison options, string[] supportedOperators = null)
            : base(token, paramTransformerFunction, options, supportedOperators) {}
    }

    /// <summary>
    /// Attribute class that defines a custom parameter transformer function applied for query running in a scene provider.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class SceneQueryEngineParameterTransformerAttribute : QueryEngineParameterTransformerAttribute {}

    class SceneQueryEngine
    {
        private readonly List<GameObject> m_GameObjects;
        private readonly Dictionary<int, GOD> m_GODS = new Dictionary<int, GOD>();
        private static readonly QueryValidationOptions k_QueryEngineOptions = new QueryValidationOptions { validateFilters = true, skipNestedQueries = true };
        private readonly QueryEngine<GameObject> m_QueryEngine = new QueryEngine<GameObject>(k_QueryEngineOptions);
        private List<SearchProposition> m_PropertyPrositions;
        private HashSet<SearchProposition> m_TypePropositions;

        private static readonly char[] s_EntrySeparators = { '/', ' ', '_', '-', '.' };
        private static readonly SearchProposition[] s_FixedPropositions = new SearchProposition[]
        {
            new SearchProposition("id:", null, "Search object by id"),
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
            new SearchProposition("prefab:any", null, "Search any prefab objects"),
            new SearchProposition("prefab:root", null, "Search prefab roots"),
            new SearchProposition("prefab:top", null, "Search prefab root instances"),
            new SearchProposition("prefab:instance", null, "Search objects part of a prefab instance"),
            new SearchProposition("prefab:nonasset", null, "Search prefab objects not part of an asset"),
            new SearchProposition("prefab:asset", null, "Search prefab objects part of an asset"),
            new SearchProposition("prefab:model", null, "Search prefab objects part of a model"),
            new SearchProposition("prefab:regular", null, "Search regular prefab objects"),
            new SearchProposition("prefab:variant", null, "Search variant prefab objects"),
            new SearchProposition("prefab:modified", null, "Search modified prefab assets"),
            new SearchProposition("prefab:altered", null, "Search modified prefab instances"),
            new SearchProposition("t:", null, "Search object by type", priority: -1),
            new SearchProposition("ref:", null, "Search object references"),
            new SearchProposition("p", "p(", "Search object's properties"),
        };

        class GOD
        {
            public string id;
            public string path;
            public string tag;
            public string[] types;
            public string[] words;
            public string[] refs;
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

        public SceneQueryEngine(List<GameObject> gameObjects)
        {
            m_GameObjects = gameObjects;
            m_QueryEngine.AddFilter("id", GetId);
            m_QueryEngine.AddFilter("path", GetPath);
            m_QueryEngine.AddFilter("tag", GetTag);
            m_QueryEngine.AddFilter("layer", GetLayer);
            m_QueryEngine.AddFilter("size", GetSize);
            m_QueryEngine.AddFilter("components", GetComponentCount);
            m_QueryEngine.AddFilter("overlap", GetOverlapCount);
            m_QueryEngine.AddFilter<string>("is", OnIsFilter, new[] {":"});
            m_QueryEngine.AddFilter<string>("prefab", OnPrefabFilter, new[] { ":" });
            m_QueryEngine.AddFilter<string>("t", OnTypeFilter, new[] {"=", ":"});
            m_QueryEngine.AddFilter<string>("i", OnAttributeFilter, new[] {"=", ":"});
            m_QueryEngine.AddFilter<string>("ref", GetReferences, new[] {"=", ":"});

            m_QueryEngine.AddFilter("p", OnPropertyFilter, s => s, StringComparison.OrdinalIgnoreCase);

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
            m_QueryEngine.AddFiltersFromAttribute<SceneQueryEngineFilterAttribute, SceneQueryEngineParameterTransformerAttribute>();
        }

        private bool OnPrefabFilter(GameObject go, string op, string value)
        {
            if (!PrefabUtility.IsPartOfAnyPrefab(go))
                return false;

            if (value == "root")
                return PrefabUtility.IsAnyPrefabInstanceRoot(go);

            if (value == "instance")
                return PrefabUtility.IsPartOfPrefabInstance(go);

            if (value == "top")
                return PrefabUtility.IsOutermostPrefabInstanceRoot(go);

            if (value == "nonasset")
                return PrefabUtility.IsPartOfNonAssetPrefabInstance(go);

            if (value == "asset")
                return PrefabUtility.IsPartOfPrefabAsset(go);

            if (value == "any")
                return PrefabUtility.IsPartOfAnyPrefab(go);

            if (value == "model")
                return PrefabUtility.IsPartOfModelPrefab(go);

            if (value == "regular")
                return PrefabUtility.IsPartOfRegularPrefab(go);

            if (value == "variant")
                return PrefabUtility.IsPartOfVariantPrefab(go);

            if (value == "modified")
                return PrefabUtility.HasPrefabInstanceAnyOverrides(go, false);

            if (value == "altered")
                return PrefabUtility.HasPrefabInstanceAnyOverrides(go, true);

            return false;
        }

        internal IEnumerable<SearchProposition> FindPropositions(SearchContext context, SearchPropositionOptions options)
        {
            if (options.tokens.Any(t => t.StartsWith("p(")))
                return FetchPropertyPropositions(options.tokens.First().Substring(2));

            if (options.tokens.Any(t => t.StartsWith("t:", StringComparison.OrdinalIgnoreCase)))
                return FetchTypePropositions();

            return s_FixedPropositions;
        }

        private IEnumerable<SearchProposition> FetchTypePropositions()
        {
            if (m_TypePropositions == null && m_GameObjects != null)
            {
                var types = m_GameObjects
                    .Where(go => go)
                    .SelectMany(go => go.GetComponents<Component>()
                        .Where(c => c)
                        .Select(c => c.GetType())).Distinct();

                m_TypePropositions = new HashSet<SearchProposition>(
                    types.Select(t => new SearchProposition($"t:{t.Name.ToLowerInvariant()}", null, $"Search {t.Name} components")));
            }

            return m_TypePropositions ?? Enumerable.Empty<SearchProposition>();
        }

        private IEnumerable<SearchProposition> FetchPropertyPropositions(string input)
        {
            if (m_PropertyPrositions == null)
            {
                m_PropertyPrositions = new List<SearchProposition>(m_GameObjects.SelectMany(go =>
                {
                    if (!go)
                        return null;

                    var propositions = new List<SearchProposition>();
                    var gocs = go.GetComponents<Component>();
                    for (int componentIndex = 1; componentIndex < gocs.Length; ++componentIndex)
                    {
                        var c = gocs[componentIndex];
                        if (!c || (c.hideFlags & HideFlags.HideInInspector) == HideFlags.HideInInspector)
                            continue;

                        var cTypeName = c.GetType().Name;
                        using (var so = new SerializedObject(c))
                        {
                            var p = so.GetIterator();
                            var next = p.NextVisible(true);
                            while (next)
                            {
                                var label = $"p({p.name.Replace("m_", "")})";
                                var replacement = ToReplacementValue(p, label);
                                if (replacement != null)
                                {
                                    var proposition = new SearchProposition(label, replacement, $"{cTypeName} ({p.propertyType})");
                                    propositions.Add(proposition);
                                }
                                next = p.NextVisible(false);
                            }
                        }
                    }

                    return propositions;
                }));
            }

            return m_PropertyPrositions;
        }

        #region search_query_error_example
        public IEnumerable<GameObject> Search(SearchContext context, SearchProvider provider, IEnumerable<GameObject> subset)
        {
            var query = m_QueryEngine.Parse(ConvertSelectors(context.searchQuery), true);
            if (!query.valid)
            {
                context.AddSearchQueryErrors(query.errors.Select(e => new SearchQueryError(e, context, provider)));
                return new GameObject[] {};
            }

            IEnumerable<GameObject> gameObjects = subset ?? m_GameObjects;
            return query.Apply(gameObjects, false);
        }

        #endregion

        static readonly Regex k_HashPropertyFilterFunctionRegex = new Regex(@"([#][^><=!:\s]+)[><=!:]");
        static string ConvertSelectors(string queryStr)
        {
            return ParserUtils.ReplaceSelectorInExpr(queryStr, (selector, cleanedSelector) => $"p({cleanedSelector})", k_HashPropertyFilterFunctionRegex);
        }

        public string GetId(GameObject go)
        {
            var god = GetGOD(go);

            if (god.id == null)
                god.id = go.GetInstanceID().ToString();

            return god.id;
        }

        public string GetPath(GameObject go)
        {
            var god = GetGOD(go);

            if (god.path == null)
                god.path = SearchUtils.GetTransformPath(go.transform).ToLowerInvariant();

            return god.path;
        }

        public string GetTag(GameObject go)
        {
            var god = GetGOD(go);

            if (god.tag == null)
                god.tag = go.tag.ToLowerInvariant();

            return god.tag;
        }

        public int GetLayer(GameObject go)
        {
            var god = GetGOD(go);

            if (!god.layer.HasValue)
                god.layer = go.layer;

            return god.layer.Value;
        }

        public float GetSize(GameObject go)
        {
            var god = GetGOD(go);

            if (god.size == float.MaxValue)
            {
                if (go.TryGetComponent<Collider>(out var collider))
                    god.size = collider.bounds.size.magnitude;
                else if (go.TryGetComponent<Renderer>(out var renderer))
                    god.size = renderer.bounds.size.magnitude;
                else
                    god.size = 0;
            }

            return god.size;
        }

        private int GetComponentCount(GameObject go)
        {
            return go.GetComponents<Component>().Length;
        }

        public int GetOverlapCount(GameObject go)
        {
            int overlapCount = -1;

            if (go.TryGetComponent<Renderer>(out var renderer))
            {
                overlapCount = 0;

                var renderers = GameObject.FindObjectsOfType<Renderer>();

                foreach (var r in renderers)
                {
                    if (renderer == r)
                        continue;

                    if (renderer.bounds.Intersects(r.bounds))
                        overlapCount++;
                }
            }

            return overlapCount;
        }

        GOD GetGOD(GameObject go)
        {
            var instanceId = go.GetHashCode();
            if (!m_GODS.TryGetValue(instanceId, out var god))
            {
                god = new GOD();
                m_GODS[instanceId] = god;
            }
            return god;
        }

        bool OnIsFilter(GameObject go, string op, string value)
        {
            var god = GetGOD(go);

            if (string.Equals(value, "object", StringComparison.Ordinal))
            {
                return true;
            }
            else if (string.Equals(value, "child", StringComparison.Ordinal))
            {
                if (!god.isChild.HasValue)
                    god.isChild = go.transform.root != go.transform;
                return god.isChild.Value;
            }
            else if (string.Equals(value, "leaf", StringComparison.Ordinal))
            {
                if (!god.isLeaf.HasValue)
                    god.isLeaf = go.transform.childCount == 0;
                return god.isLeaf.Value;
            }
            else if (string.Equals(value, "root", StringComparison.Ordinal))
            {
                return go.transform.root == go.transform;
            }
            else if (string.Equals(value, "visible", StringComparison.Ordinal))
            {
                return IsInView(go, SceneView.GetAllSceneCameras().FirstOrDefault());
            }
            else if (string.Equals(value, "hidden", StringComparison.Ordinal))
            {
                return SceneVisibilityManager.instance.IsHidden(go);
            }
            else if (string.Equals(value, "static", StringComparison.Ordinal))
            {
                return go.isStatic;
            }
            else if (string.Equals(value, "prefab", StringComparison.Ordinal))
            {
                return PrefabUtility.IsPartOfAnyPrefab(go);
            }

            return false;
        }

        private SearchValue FindPropertyValue(UnityEngine.Object obj, string propertyName)
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

        private string ToReplacementValue(SerializedProperty sp, string replacement)
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

        private SearchValue OnPropertyFilter(GameObject go, string propertyName)
        {
            if (!go)
                return SearchValue.invalid;

            {

                var gocs = go.GetComponents<Component>();
                for (int componentIndex = 0; componentIndex < gocs.Length; ++componentIndex)
                {
                    var c = gocs[componentIndex];
                    if (!c || (c.hideFlags & HideFlags.HideInInspector) == HideFlags.HideInInspector)
                        continue;

                    var property = FindPropertyValue(c, propertyName);
                    if (property.valid)
                    {
                        return property;
                    }
                }

            }

            return SearchValue.invalid;
        }

        bool OnTypeFilter(GameObject go, string op, string value)
        {
            var god = GetGOD(go);

            if (god.types == null)
            {
                var types = new List<string>();
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

                god.types = types.ToArray();
            }

            return CompareWords(op, value.ToLowerInvariant(), god.types);
        }

        bool OnAttributeFilter(GameObject go, string op, string value)
        {
            var god = GetGOD(go);

            if (god.attrs == null)
            {
                var attrs = new List<string>();

                var gocs = go.GetComponents<MonoBehaviour>();
                for (int componentIndex = 0; componentIndex < gocs.Length; ++componentIndex)
                {
                    var c = gocs[componentIndex];
                    if (!c || (c.hideFlags & HideFlags.HideInInspector) == HideFlags.HideInInspector)
                        continue;

                    attrs.AddRange(c.GetType().GetInterfaces().Select(t => t.Name.ToLowerInvariant()));
                }

                god.attrs = attrs.ToArray();
            }

            return CompareWords(op, value.ToLowerInvariant(), god.attrs);
        }

        private void BuildReferences(UnityEngine.Object obj, ICollection<string> refs)
        {
            using (var so = new SerializedObject(obj))
            {
                var p = so.GetIterator();
                var next = p.NextVisible(true);
                while (next)
                {
                    AddPropertyReferences(p, refs);
                    next = p.NextVisible(p.hasVisibleChildren);
                }
            }
        }

        private void AddPropertyReferences(SerializedProperty p, ICollection<string> refs)
        {
            if (p.propertyType != SerializedPropertyType.ObjectReference || !p.objectReferenceValue)
                return;

            var refValue = AssetDatabase.GetAssetPath(p.objectReferenceValue);
            if (string.IsNullOrEmpty(refValue) && p.objectReferenceValue is GameObject go)
                refValue = SearchUtils.GetTransformPath(go.transform);

            if (!string.IsNullOrEmpty(refValue) && !refs.Contains(refValue))
                AddReference(p.objectReferenceValue, refValue, refs);

            // Add custom object cases
            if (p.objectReferenceValue is Material material)
            {
                if (material.shader)
                    AddReference(material.shader, material.shader.name, refs);
            }
        }

        private bool AddReference(UnityEngine.Object refObj, string refValue, ICollection<string> refs)
        {
            if (String.IsNullOrEmpty(refValue))
                return false;

            if (refValue[0] == '/')
                refValue = refValue.Substring(1);

            var refType = refObj?.GetType().Name;
            if (refType != null)
                refs.Add(refType.ToLowerInvariant());
            refs.Add(refValue.ToLowerInvariant());

            return true;
        }

        private bool GetReferences(GameObject go, string op, string value)
        {
            var god = GetGOD(go);

            if (god.refs == null)
            {
                var refs = new HashSet<string>();

                BuildReferences(go, refs);

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

                god.refs = refs.ToArray();
            }

            return CompareWords(op, value.ToLowerInvariant(), god.refs);
        }

        private bool CompareWords(string op, string value, IEnumerable<string> words, StringComparison stringComparison = StringComparison.Ordinal)
        {
            if (op == "=")
                return words.Any(t => t.Equals(value, stringComparison));
            return words.Any(t => t.IndexOf(value, stringComparison) != -1);
        }

        IEnumerable<string> OnSearchData(GameObject go)
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

        private bool IsInView(GameObject toCheck, Camera cam)
        {
            if (!cam || !toCheck)
                return false;

            var renderer = toCheck.GetComponentInChildren<Renderer>();
            if (!renderer)
                return false;

            Vector3 pointOnScreen = cam.WorldToScreenPoint(renderer.bounds.center);

            // Is in front
            if (pointOnScreen.z < 0)
                return false;

            // Is in FOV
            if ((pointOnScreen.x < 0) || (pointOnScreen.x > Screen.width) ||
                (pointOnScreen.y < 0) || (pointOnScreen.y > Screen.height))
                return false;

            if (Physics.Linecast(cam.transform.position, renderer.bounds.center, out var hit))
            {
                if (hit.transform.GetInstanceID() != toCheck.GetInstanceID())
                    return false;
            }
            return true;
        }
    }
}
