// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
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

    class SceneQueryEngine : ObjectQueryEngine<GameObject>
    {
        private List<SearchProposition> m_PropertyPrositions;

        public SceneQueryEngine(IEnumerable<GameObject> gameObjects)
            : base(gameObjects)
        {
            m_QueryEngine.AddFilter("layer", GetLayer);
            m_QueryEngine.AddFilter("tag", GetTag);
            m_QueryEngine.AddFilter<string>("prefab", OnPrefabFilter, new[] { ":" });
            m_QueryEngine.AddFilter<string>("i", OnAttributeFilter, new[] { "=", ":" });
            m_QueryEngine.AddFilter("p", OnPropertyFilter, s => s, StringComparison.OrdinalIgnoreCase);
            m_QueryEngine.AddFilter("size", GetSize);
            m_QueryEngine.AddFilter("components", GetComponentCount);
            m_QueryEngine.AddFilter("overlap", GetOverlapCount);

            m_QueryEngine.AddFiltersFromAttribute<SceneQueryEngineFilterAttribute, SceneQueryEngineParameterTransformerAttribute>();
        }

        bool OnPrefabFilter(GameObject go, string op, string value)
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

        string GetTag(GameObject go)
        {
            var god = GetGOD(go);

            if (god.tag == null)
                god.tag = go.tag.ToLowerInvariant();

            return god.tag;
        }

        public override bool GetId(GameObject go, string op, int instanceId)
        {
            if (instanceId == go.GetInstanceID())
                return true;

            return EditorUtility.InstanceIDToObject(instanceId) is Component c && c.gameObject == go;
        }

        int GetLayer(GameObject go)
        {
            var god = GetGOD(go);

            if (!god.layer.HasValue)
                god.layer = go.layer;

            return god.layer.Value;
        }

        float GetSize(GameObject go)
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

        int GetComponentCount(GameObject go)
        {
            return go.GetComponents<Component>().Length;
        }

        int GetOverlapCount(GameObject go)
        {
            int overlapCount = -1;

            if (go.TryGetComponent<Renderer>(out var renderer))
            {
                overlapCount = 0;

                var renderers = UnityEngine.Object.FindObjectsOfType<Renderer>();

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

        SearchValue OnPropertyFilter(GameObject go, string propertyName)
        {
            if (!go)
                return SearchValue.invalid;

            using (var view = SearchMonitor.GetView())
            {
                var documentKey = SearchUtils.GetDocumentKey(go);
                var recordKey = PropertyDatabase.CreateRecordKey(documentKey, PropertyDatabase.CreatePropertyHash(propertyName));
                if (view.TryLoadProperty(recordKey, out object data))
                    return (SearchValue)data;

                var gocs = go.GetComponents<Component>();
                for (int componentIndex = 0; componentIndex < gocs.Length; ++componentIndex)
                {
                    var c = gocs[componentIndex];
                    if (!c || (c.hideFlags & HideFlags.HideInInspector) == HideFlags.HideInInspector)
                        continue;

                    var property = FindPropertyValue(c, propertyName);
                    if (property.valid)
                    {
                        view.StoreProperty(recordKey, property);
                        return property;
                    }
                }

                view.StoreProperty(recordKey, SearchValue.invalid);
            }

            return SearchValue.invalid;
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

        bool IsInView(GameObject toCheck, Camera cam)
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

        protected override string GetPath(GameObject go)
        {
            var god = GetGOD(go);

            if (god.path == null)
                god.path = SearchUtils.GetTransformPath(go.transform).ToLowerInvariant();

            return god.path;
        }

        protected override bool OnIsFilter(GameObject go, string op, string value)
        {
            var god = GetGOD(go);

            if (string.Equals(value, "child", StringComparison.Ordinal))
            {
                if (!god.isChild.HasValue)
                    god.isChild = go != null && go.transform.root != go.transform;
                return god.isChild.Value;
            }
            else if (string.Equals(value, "leaf", StringComparison.Ordinal))
            {
                if (!god.isLeaf.HasValue)
                    god.isLeaf = go != null && go.transform.childCount == 0;
                return god.isLeaf.Value;
            }
            else if (string.Equals(value, "root", StringComparison.Ordinal))
            {
                return go != null && go.transform.root == go.transform;
            }
            else if (go && string.Equals(value, "visible", StringComparison.Ordinal))
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

            return base.OnIsFilter(go, op, value);
        }

        public override IEnumerable<SearchProposition> FindPropositions(SearchContext context, SearchPropositionOptions options)
        {
            if (!options.HasAny(SearchPropositionFlags.FilterOnly))
            {
                if (options.StartsWith("p("))
                    return FetchPropertyPropositions(options.tokens.First().Substring(2));
            }

            return base.FindPropositions(context, options);
        }

        private IEnumerable<SearchProposition> FetchPropertyPropositions(string input)
        {
            if (m_PropertyPrositions != null)
                return m_PropertyPrositions;
            m_PropertyPrositions = new List<SearchProposition>(m_Objects.SelectMany(go =>
            {
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
            return m_PropertyPrositions;
        }
    }
}
