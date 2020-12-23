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
        private readonly QueryEngine<GameObject> m_QueryEngine = new QueryEngine<GameObject>(true);
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

        private static readonly Regex s_RangeRx = new Regex(@"\[(-?[\d\.]+)[,](-?[\d\.]+)\s*\]");

        class PropertyRange
        {
            public float min { get; private set; }
            public float max { get; private set; }

            public PropertyRange(float min, float max)
            {
                this.min = min;
                this.max = max;
            }

            public bool Contains(float f)
            {
                if (f >= min && f <= max)
                    return true;
                return false;
            }
        }

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

            public Dictionary<string, GOP> properties;
        }

        readonly struct IntegerColor : IEquatable<IntegerColor>, IComparable<IntegerColor>
        {
            public readonly int r;
            public readonly int g;
            public readonly int b;
            public readonly int a;

            public IntegerColor(Color c)
            {
                r = Mathf.RoundToInt(c.r * 255f);
                g = Mathf.RoundToInt(c.g * 255f);
                b = Mathf.RoundToInt(c.b * 255f);
                a = Mathf.RoundToInt(c.a * 255f);
            }

            public int this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0: return r;
                        case 1: return g;
                        case 2: return b;
                        case 3: return a;
                        default:
                            throw new IndexOutOfRangeException("Invalid Color index(" + index + ")!");
                    }
                }
            }

            public bool Equals(IntegerColor other)
            {
                for (var i = 0; i < 4; ++i)
                {
                    if (this[i] != other[i])
                        return false;
                }

                return true;
            }

            public override bool Equals(object obj)
            {
                if (obj is IntegerColor ic)
                    return base.Equals(ic);
                return false;
            }

            public override int GetHashCode()
            {
                return r.GetHashCode() ^ (g.GetHashCode() << 2) ^ (b.GetHashCode() >> 2) ^ (a.GetHashCode() >> 1);
            }

            public int CompareTo(IntegerColor other)
            {
                for (var i = 0; i < 4; ++i)
                {
                    if (this[i] > other[i])
                        return 1;
                    if (this[i] < other[i])
                        return -1;
                }

                return 0;
            }

            public static bool operator==(IntegerColor lhs, IntegerColor rhs)
            {
                return lhs.Equals(rhs);
            }

            public static bool operator!=(IntegerColor lhs, IntegerColor rhs)
            {
                return !lhs.Equals(rhs);
            }

            public static bool operator>(IntegerColor lhs, IntegerColor rhs)
            {
                return lhs.CompareTo(rhs) > 0;
            }

            public static bool operator<(IntegerColor lhs, IntegerColor rhs)
            {
                return lhs.CompareTo(rhs) < 0;
            }

            public static bool operator>=(IntegerColor lhs, IntegerColor rhs)
            {
                return lhs.CompareTo(rhs) >= 0;
            }

            public static bool operator<=(IntegerColor lhs, IntegerColor rhs)
            {
                return lhs.CompareTo(rhs) <= 0;
            }
        }

        readonly struct GOP
        {
            public enum ValueType
            {
                Nil = 0,
                Bool,
                Number,
                Text,
                Color
            }

            public readonly ValueType type;
            public readonly bool b;
            public readonly float number;
            public readonly string text;
            public readonly IntegerColor? color;

            public bool valid => type != ValueType.Nil;

            public static GOP invalid = new GOP();

            public GOP(bool v)
            {
                this.type = ValueType.Bool;
                this.number = float.NaN;
                this.text = null;
                this.b = v;
                this.color = null;
            }

            public GOP(float number)
            {
                this.type = ValueType.Number;
                this.number = number;
                this.text = null;
                this.b = false;
                this.color = null;
            }

            public GOP(string text)
            {
                this.type = ValueType.Text;
                this.number = float.NaN;
                this.text = text;
                this.b = false;
                this.color = null;
            }

            public GOP(Color color)
            {
                this.type = ValueType.Color;
                this.number = float.NaN;
                this.text = null;
                this.b = false;
                this.color = new IntegerColor(color);
            }
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
            m_QueryEngine.AddFilter("overlap", GetOverlapCount);
            m_QueryEngine.AddFilter<string>("is", OnIsFilter, new[] {":"});
            m_QueryEngine.AddFilter<string>("prefab", OnPrefabFilter, new[] { ":" });
            m_QueryEngine.AddFilter<string>("t", OnTypeFilter, new[] {"=", ":"});
            m_QueryEngine.AddFilter<string>("i", OnAttributeFilter, new[] {"=", ":"});
            m_QueryEngine.AddFilter<string>("ref", GetReferences, new[] {"=", ":"});

            m_QueryEngine.AddFilter("p", OnPropertyFilter, s => s, StringComparison.OrdinalIgnoreCase);

            m_QueryEngine.AddOperatorHandler(":", (GOP v, PropertyRange range) => PropertyRangeCompare(v, range, (f, r) => r.Contains(f)));
            m_QueryEngine.AddOperatorHandler("=", (GOP v, PropertyRange range) => PropertyRangeCompare(v, range, (f, r) => r.Contains(f)));
            m_QueryEngine.AddOperatorHandler("!=", (GOP v, PropertyRange range) => PropertyRangeCompare(v, range, (f, r) => !r.Contains(f)));
            m_QueryEngine.AddOperatorHandler("<=", (GOP v, PropertyRange range) => PropertyRangeCompare(v, range, (f, r) => f <= r.max));
            m_QueryEngine.AddOperatorHandler("<", (GOP v, PropertyRange range) => PropertyRangeCompare(v, range, (f, r) => f < r.min));
            m_QueryEngine.AddOperatorHandler(">", (GOP v, PropertyRange range) => PropertyRangeCompare(v, range, (f, r) => f > r.max));
            m_QueryEngine.AddOperatorHandler(">=", (GOP v, PropertyRange range) => PropertyRangeCompare(v, range, (f, r) => f >= r.min));

            m_QueryEngine.AddOperatorHandler(":", (GOP v, float number, StringComparison sc) => PropertyFloatCompare(v, number, (f, r) => StringContains(f, r, sc)));
            m_QueryEngine.AddOperatorHandler("=", (GOP v, float number) => PropertyFloatCompare(v, number, (f, r) => Math.Abs(f - r) < Mathf.Epsilon));
            m_QueryEngine.AddOperatorHandler("!=", (GOP v, float number) => PropertyFloatCompare(v, number, (f, r) => Math.Abs(f - r) >= Mathf.Epsilon));
            m_QueryEngine.AddOperatorHandler("<=", (GOP v, float number) => PropertyFloatCompare(v, number, (f, r) => f <= r));
            m_QueryEngine.AddOperatorHandler("<", (GOP v, float number) => PropertyFloatCompare(v, number, (f, r) => f < r));
            m_QueryEngine.AddOperatorHandler(">", (GOP v, float number) => PropertyFloatCompare(v, number, (f, r) => f > r));
            m_QueryEngine.AddOperatorHandler(">=", (GOP v, float number) => PropertyFloatCompare(v, number, (f, r) => f >= r));

            m_QueryEngine.AddOperatorHandler("=", (GOP v, bool b) => PropertyBoolCompare(v, b, (f, r) => f == r));
            m_QueryEngine.AddOperatorHandler("!=", (GOP v, bool b) => PropertyBoolCompare(v, b, (f, r) => f != r));

            m_QueryEngine.AddOperatorHandler(":", (GOP v, string s, StringComparison sc) => PropertyStringCompare(v, s, (f, r) => StringContains(f, r, sc)));
            m_QueryEngine.AddOperatorHandler("=", (GOP v, string s, StringComparison sc) => PropertyStringCompare(v, s, (f, r) => string.Equals(f, r, sc)));
            m_QueryEngine.AddOperatorHandler("!=", (GOP v, string s, StringComparison sc) => PropertyStringCompare(v, s, (f, r) => !string.Equals(f, r, sc)));
            m_QueryEngine.AddOperatorHandler("<=", (GOP v, string s, StringComparison sc) => PropertyStringCompare(v, s, (f, r) => string.Compare(f, r, sc) <= 0));
            m_QueryEngine.AddOperatorHandler("<", (GOP v, string s, StringComparison sc) => PropertyStringCompare(v, s, (f, r) => string.Compare(f, r, sc) < 0));
            m_QueryEngine.AddOperatorHandler(">", (GOP v, string s, StringComparison sc) => PropertyStringCompare(v, s, (f, r) => string.Compare(f, r, sc) > 0));
            m_QueryEngine.AddOperatorHandler(">=", (GOP v, string s, StringComparison sc) => PropertyStringCompare(v, s, (f, r) => string.Compare(f, r, sc) >= 0));

            m_QueryEngine.AddOperatorHandler(":", (GOP v, IntegerColor c) => PropertyColorCompare(v, c, (f, r) => f == r));
            m_QueryEngine.AddOperatorHandler("=", (GOP v, IntegerColor c) => PropertyColorCompare(v, c, (f, r) => f == r));
            m_QueryEngine.AddOperatorHandler("!=", (GOP v, IntegerColor c) => PropertyColorCompare(v, c, (f, r) => f != r));
            m_QueryEngine.AddOperatorHandler("<=", (GOP v, IntegerColor c) => PropertyColorCompare(v, c, (f, r) => f <= r));
            m_QueryEngine.AddOperatorHandler("<", (GOP v, IntegerColor c) => PropertyColorCompare(v, c, (f, r) => f < r));
            m_QueryEngine.AddOperatorHandler(">", (GOP v, IntegerColor c) => PropertyColorCompare(v, c, (f, r) => f > r));
            m_QueryEngine.AddOperatorHandler(">=", (GOP v, IntegerColor c) => PropertyColorCompare(v, c, (f, r) => f >= r));

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

            m_QueryEngine.AddTypeParser(arg =>
            {
                if (arg.Length > 0 && arg.Last() == ']')
                {
                    var rangeMatches = s_RangeRx.Matches(arg);
                    if (rangeMatches.Count == 1 && rangeMatches[0].Groups.Count == 3)
                    {
                        var rg = rangeMatches[0].Groups;
                        if (float.TryParse(rg[1].Value, out var min) && float.TryParse(rg[2].Value, out var max))
                            return new ParseResult<PropertyRange>(true, new PropertyRange(min, max));
                    }
                }

                return ParseResult<PropertyRange>.none;
            });

            m_QueryEngine.AddTypeParser(s =>
            {
                if (s == "on")
                    return new ParseResult<bool>(true, true);
                if (s == "off")
                    return new ParseResult<bool>(true, false);
                return new ParseResult<bool>(false, false);
            });

            m_QueryEngine.AddTypeParser(s =>
            {
                if (!s.StartsWith("#"))
                    return new ParseResult<IntegerColor?>(false, null);
                if (ColorUtility.TryParseHtmlString(s, out var color))
                    return new ParseResult<IntegerColor?>(true, new IntegerColor(color));
                return new ParseResult<IntegerColor?>(false, null);
            });

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

        private static bool StringContains<T>(T ev, T fv, StringComparison sc)
        {
            return ev.ToString().IndexOf(fv.ToString(), sc) != -1;
        }

        private static bool PropertyRangeCompare(GOP v, PropertyRange range, Func<float, PropertyRange, bool> comparer)
        {
            if (v.type != GOP.ValueType.Number)
                return false;
            return comparer(v.number, range);
        }

        private static bool PropertyFloatCompare(GOP v, float value, Func<float, float, bool> comparer)
        {
            if (v.type != GOP.ValueType.Number)
                return false;
            return comparer(v.number, value);
        }

        private static bool PropertyBoolCompare(GOP v, bool b, Func<bool, bool, bool> comparer)
        {
            if (v.type != GOP.ValueType.Bool)
                return false;
            return comparer(v.b, b);
        }

        private static bool PropertyStringCompare(GOP v, string s, Func<string, string, bool> comparer)
        {
            if (v.type != GOP.ValueType.Text || String.IsNullOrEmpty(v.text))
                return false;
            return comparer(v.text, s);
        }

        private static bool PropertyColorCompare(GOP v, IntegerColor value, Func<IntegerColor, IntegerColor, bool> comparer)
        {
            if (v.type != GOP.ValueType.Color || !v.color.HasValue)
                return false;
            return comparer(v.color.Value, value);
        }

        internal IEnumerable<SearchProposition> FindPropositions(SearchContext context, SearchPropositionOptions options)
        {
            if (options.token.StartsWith("p("))
                return FetchPropertyPropositions(options.token.Substring(2));

            if (options.token.StartsWith("t:", StringComparison.OrdinalIgnoreCase))
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
                        if (!c || c.hideFlags.HasFlag(HideFlags.HideInInspector))
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
            var query = m_QueryEngine.Parse(context.searchQuery, true);
            if (!query.valid)
            {
                context.AddSearchQueryErrors(query.errors.Select(e => new SearchQueryError(e.index, e.length, e.reason, context, provider)));
                return new GameObject[] {};
            }

            IEnumerable<GameObject> gameObjects = subset ?? m_GameObjects;
            return query.Apply(gameObjects);
        }

        #endregion

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
            var instanceId = go.GetInstanceID();
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

            if (value == "child")
            {
                if (!god.isChild.HasValue)
                    god.isChild = go.transform.root != go.transform;
                return god.isChild.Value;
            }
            else if (value == "leaf")
            {
                if (!god.isLeaf.HasValue)
                    god.isLeaf = go.transform.childCount == 0;
                return god.isLeaf.Value;
            }
            else if (value == "root")
            {
                return go.transform.root == go.transform;
            }
            else if (value == "visible")
            {
                return IsInView(go, SceneView.GetAllSceneCameras().FirstOrDefault());
            }
            else if (value == "hidden")
            {
                return SceneVisibilityManager.instance.IsHidden(go);
            }
            else if (value == "static")
            {
                return go.isStatic;
            }
            else if (value == "prefab")
            {
                return PrefabUtility.IsPartOfAnyPrefab(go);
            }

            return false;
        }

        private GOP FindPropertyValue(UnityEngine.Object obj, string propertyName)
        {
            using (var so = new SerializedObject(obj))
            {
                var property = so.FindProperty(propertyName) ?? so.FindProperty($"m_{propertyName}");
                if (property != null)
                    return ConvertPropertyValue(property);

                property = so.GetIterator();
                var next = property.NextVisible(true);
                while (next)
                {
                    if (property.name.LastIndexOf(propertyName, StringComparison.OrdinalIgnoreCase) != -1)
                        return ConvertPropertyValue(property);
                    next = property.NextVisible(false);
                }
            }

            return GOP.invalid;
        }

        private GOP ConvertPropertyValue(SerializedProperty sp)
        {
            switch (sp.propertyType)
            {
                case SerializedPropertyType.Integer: return new GOP((float)sp.intValue);
                case SerializedPropertyType.Boolean: return new GOP(sp.boolValue);
                case SerializedPropertyType.Float: return new GOP(sp.floatValue);
                case SerializedPropertyType.String: return new GOP(sp.stringValue);
                case SerializedPropertyType.Enum: return new GOP(sp.enumNames[sp.enumValueIndex]);
                case SerializedPropertyType.ObjectReference: return new GOP(sp.objectReferenceValue?.name);
                case SerializedPropertyType.Bounds: return new GOP(sp.boundsValue.size.magnitude);
                case SerializedPropertyType.BoundsInt: return new GOP(sp.boundsIntValue.size.magnitude);
                case SerializedPropertyType.Rect: return new GOP(sp.rectValue.size.magnitude);
                case SerializedPropertyType.Color: return new GOP(sp.colorValue);
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

            return GOP.invalid;
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

        private GOP OnPropertyFilter(GameObject go, string propertyName)
        {
            var god = GetGOD(go);

            if (god.properties == null)
                god.properties = new Dictionary<string, GOP>();
            else if (god.properties.TryGetValue(propertyName, out var existingProperty))
                return existingProperty;

            var gocs = go.GetComponents<Component>();
            for (int componentIndex = 1; componentIndex < gocs.Length; ++componentIndex)
            {
                var c = gocs[componentIndex];
                if (!c || c.hideFlags.HasFlag(HideFlags.HideInInspector))
                    continue;

                var property = FindPropertyValue(c, propertyName);
                if (property.valid)
                {
                    god.properties[propertyName] = property;
                    return property;
                }
            }
            return GOP.invalid;
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
                    if (!c || c.hideFlags.HasFlag(HideFlags.HideInInspector))
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
                    if (!c || c.hideFlags.HasFlag(HideFlags.HideInInspector))
                        continue;

                    attrs.AddRange(c.GetType().GetInterfaces().Select(t => t.Name.ToLowerInvariant()));
                }

                god.attrs = attrs.ToArray();
            }

            return CompareWords(op, value.ToLowerInvariant(), god.attrs);
        }

        private void BuildReferences(UnityEngine.Object obj, ICollection<string> refs, int depth, int maxDepth)
        {
            if (depth > maxDepth)
                return;

            using (var so = new SerializedObject(obj))
            {
                var p = so.GetIterator();
                var next = p.NextVisible(true);
                while (next)
                {
                    AddPropertyReferences(p, refs, depth, maxDepth);
                    next = p.NextVisible(p.hasVisibleChildren);
                }
            }
        }

        private void AddPropertyReferences(SerializedProperty p, ICollection<string> refs, int depth, int maxDepth)
        {
            if (p.propertyType != SerializedPropertyType.ObjectReference || !p.objectReferenceValue)
                return;

            var refValue = AssetDatabase.GetAssetPath(p.objectReferenceValue);
            if (String.IsNullOrEmpty(refValue))
            {
                if (p.objectReferenceValue is GameObject go)
                {
                    refValue = SearchUtils.GetTransformPath(go.transform);
                }
            }

            if (!String.IsNullOrEmpty(refValue))
            {
                if (!refs.Contains(refValue))
                {
                    AddReference(p.objectReferenceValue, refValue, refs);
                    BuildReferences(p.objectReferenceValue, refs, depth + 1, maxDepth);
                }
            }

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
                const int maxReferenceDepth = 3;
                var refs = new HashSet<string>();

                BuildReferences(go, refs, 0, maxReferenceDepth);

                var gocs = go.GetComponents<Component>();
                for (int componentIndex = 1; componentIndex < gocs.Length; ++componentIndex)
                {
                    var c = gocs[componentIndex];
                    if (!c || c.hideFlags.HasFlag(HideFlags.HideInInspector))
                        continue;
                    BuildReferences(c, refs, 1, maxReferenceDepth);
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
