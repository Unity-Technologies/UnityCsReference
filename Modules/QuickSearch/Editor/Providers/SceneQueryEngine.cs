// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
using UnityEditorInternal;

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

        internal SceneQueryEngineFilterAttribute(string token, string[] supportedOperators, string propositionReplacement)
            : base(token, supportedOperators)
        {
            this.propositionReplacement = propositionReplacement;
        }

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

        static Regex SerializedPropertyRx = new Regex(@"#([\w\d\.\[\]]+)");

        public SceneQueryEngine(IEnumerable<GameObject> gameObjects)
            : base(gameObjects)
        {
            m_QueryEngine.AddFilter("active", IsActive);
            m_QueryEngine.AddFilter("size", GetSize);
            m_QueryEngine.AddFilter("components", GetComponentCount);
            m_QueryEngine.AddFilter("layer", GetLayer);
            m_QueryEngine.AddFilter("tag", GetTag);
            m_QueryEngine.AddFilter<PrefabFilter>("prefab", OnPrefabFilter, new[] { ":" });
            m_QueryEngine.AddFilter<string>("i", OnAttributeFilter, new[] { "=", ":" });
            m_QueryEngine.AddFilter("p", OnPropertyFilter, s => s, StringComparison.OrdinalIgnoreCase);
            m_QueryEngine.AddFilter(SerializedPropertyRx, OnPropertyFilter);
            m_QueryEngine.AddFilter("overlap", GetOverlapCount);

            m_QueryEngine.AddFiltersFromAttribute<SceneQueryEngineFilterAttribute, SceneQueryEngineParameterTransformerAttribute>();
        }

        public override void SetupQueryEnginePropositions()
        {
            var goIcon = Utils.LoadIcon("GameObject Icon");
            m_QueryEngine.GetFilter("active")
                .AddOrUpdatePropositionData(category: "GameObject", label: "Active", replacement: "active=true", help: "Search active objects", icon: goIcon, color: QueryColors.filter);
            m_QueryEngine.GetFilter("size")
                .AddOrUpdatePropositionData(category: "GameObject", label: "Volume Size", replacement: "size>1", help: "Search object by volume size", icon: goIcon, color: QueryColors.filter);;
            m_QueryEngine.GetFilter("components")
                .AddOrUpdatePropositionData(category: "GameObject", label: "Components count", replacement: "components>1", help: "Search object with more than # components", icon: goIcon, color: QueryColors.filter);;;
            m_QueryEngine.GetFilter("id")
                .AddOrUpdatePropositionData(category: "GameObject", label: "InstanceID", replacement: "id=0", help: "Search object with InstanceID", icon: goIcon, color: QueryColors.filter);
            m_QueryEngine.GetFilter("path")
                .AddOrUpdatePropositionData(category: "GameObject", label: "Path", replacement: "path=/root/children1", help: "Search object with Transform path", icon: goIcon, color: QueryColors.filter);

            var layerFilter = m_QueryEngine.GetFilter("layer")
                .SetGlobalPropositionData(category: "Layers", icon: Utils.LoadIcon("GUILayer Icon"), color: QueryColors.typeIcon, type: typeof(QueryLayerBlock));
            for (var i = 0; i < 32; ++i)
            {
                var layerName = InternalEditorUtility.GetLayerName(i);
                if (!string.IsNullOrEmpty(layerName))
                    layerFilter.AddOrUpdatePropositionData(label: ObjectNames.NicifyVariableName(layerName), data: layerName, replacement: $"<$layer:{i}, {layerName}$>");
            }

            var tagFilter = m_QueryEngine.GetFilter("tag")
                .SetGlobalPropositionData(category: "Tags", icon: QueryLabelBlock.GetLabelIcon(), color: QueryColors.typeIcon);
            foreach (var t in InternalEditorUtility.tags)
            {
                tagFilter.AddOrUpdatePropositionData(category: "Tags", label: ObjectNames.NicifyVariableName(t), replacement: "tag=" + SearchUtils.GetListMarkerReplacementText(t, InternalEditorUtility.tags, "AssetLabelIcon", QueryColors.typeIcon));
            }

            m_QueryEngine.GetFilter("prefab")
                .AddPropositionsFromFilterType(icon: Utils.LoadIcon("Prefab Icon"), category: "Prefabs", priority: 0, type: typeof(QueryListMarkerBlock), color: QueryColors.typeIcon);

            var sceneIcon = Utils.LoadIcon("SceneAsset Icon");
            m_QueryEngine.GetFilter("ref")
                .AddOrUpdatePropositionData(category: "Reference", label:"Referencing Asset", replacement:"ref=<$object:none,UnityEngine.Object$>", help: "Find all objects referencing a specific asset.", icon:sceneIcon, color: QueryColors.filter)
                .AddOrUpdatePropositionData(category: "Reference", label: "Referencing GameObject", replacement: "ref=<$object:none,UnityEngine.GameObject$>", help: "Find all objects referencing a specific GameObject.", icon: sceneIcon, color: QueryColors.filter)
                .AddOrUpdatePropositionData(category: "Reference", label:"Reference By Instance ID (Number)", replacement:"ref=1000", help: "Find all objects referencing a specific instance ID (Number).", icon: sceneIcon, color: QueryColors.filter)
                .AddOrUpdatePropositionData(category: "Reference", label:"Reference By Asset Expression", replacement:"ref={p: }", help: "Find all objects referencing for a given asset search.", icon: sceneIcon, color: QueryColors.filter);

            m_QueryEngine.AddPropositionsFromFilterAttributes<GameObject, SceneQueryEngineFilterAttribute>(category: "Custom Scene Filters", icon: sceneIcon, color: QueryColors.filter, propositionTransformation: proposition =>
            {
                return new SearchProposition(category: proposition.category,
                    label: proposition.label,
                    replacement: proposition.replacement,
                    help: proposition.help,
                    data: proposition.data,
                    priority: proposition.priority,
                    icon: proposition.icon,
                    type: proposition.type,
                    color: proposition.color,
                    moveCursor: proposition.moveCursor);
            });
        }

        private bool IsActive(GameObject go)
        {
            return go != null && go.activeInHierarchy;
        }

        static bool OnPrefabFilter(GameObject go, QueryFilterOperator op, PrefabFilter value)
        {
            if (!PrefabUtility.IsPartOfAnyPrefab(go))
                return false;

            switch (value)
            {
                case PrefabFilter.Root:
                    return PrefabUtility.IsAnyPrefabInstanceRoot(go);
                case PrefabFilter.Instance:
                    return PrefabUtility.IsPartOfPrefabInstance(go);
                case PrefabFilter.Top:
                    return PrefabUtility.IsOutermostPrefabInstanceRoot(go);
                case PrefabFilter.NonAsset:
                    return PrefabUtility.IsPartOfNonAssetPrefabInstance(go);
                case PrefabFilter.Asset:
                    return PrefabUtility.IsPartOfPrefabAsset(go);
                case PrefabFilter.Any:
                    return PrefabUtility.IsPartOfAnyPrefab(go);
                case PrefabFilter.Model:
                    return PrefabUtility.IsPartOfModelPrefab(go);
                case PrefabFilter.Regular:
                    return PrefabUtility.IsPartOfRegularPrefab(go);
                case PrefabFilter.Variant:
                    return PrefabUtility.IsPartOfVariantPrefab(go);
                case PrefabFilter.Modified:
                    return PrefabUtility.HasPrefabInstanceAnyOverrides(go, false);
                case PrefabFilter.Altered:
                    return PrefabUtility.HasPrefabInstanceAnyOverrides(go, true);
                default:
                    return false;
            }
        }

        string GetTag(GameObject go)
        {
            var god = GetGOD(go);

            if (god.tag == null)
                god.tag = go.tag.ToLowerInvariant();

            return god.tag;
        }

        public override bool GetId(GameObject go, QueryFilterOperator op, int instanceId)
        {
            int goId = go.GetInstanceID();
            switch (op.type)
            {
                case FilterOperatorType.Contains:
                case FilterOperatorType.Equal:
                    if (instanceId == goId)
                        return true;
                    return EditorUtility.InstanceIDToObject(instanceId) is Component c && c.gameObject == go;

                case FilterOperatorType.NotEqual: return instanceId != goId;
                case FilterOperatorType.Greater: return instanceId > goId;
                case FilterOperatorType.GreaterOrEqual: return instanceId >= goId;
                case FilterOperatorType.Lesser: return instanceId < goId;
                case FilterOperatorType.LesserOrEqual: return instanceId <= goId;
            }

            return false;
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
            if (string.IsNullOrEmpty(propertyName))
                return SearchValue.invalid;

            using (var view = SearchMonitor.GetView())
            {
                var documentKey = SearchUtils.GetDocumentKey(go);
                var recordKey = PropertyDatabase.CreateRecordKey(documentKey, PropertyDatabase.CreatePropertyHash(propertyName));
                if (view.TryLoadProperty(recordKey, out object data) && data is SearchValue sv)
                    return sv;

                foreach (var c in EnumerateSubObjects(go))
                {
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

        IEnumerable<UnityEngine.Object> EnumerateSubObjects(GameObject go)
        {
            yield return go;

            var gocs = go.GetComponents<Component>();
            for (int componentIndex = 0; componentIndex < gocs.Length; ++componentIndex)
            {
                var c = gocs[componentIndex];
                if (!c || (c.hideFlags & HideFlags.HideInInspector) == HideFlags.HideInInspector)
                    continue;

                yield return c;
            }
        }

        bool OnAttributeFilter(GameObject go, QueryFilterOperator op, string value)
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

        bool IsInView(in GameObject go, in Camera cam)
        {
            if (!cam || !go)
                return false;

            var planes = GeometryUtility.CalculateFrustumPlanes(cam);
            var point = go.transform.position;
            foreach (var plane in planes)
            {
                if (plane.GetDistanceToPoint(point) < 0)
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

        protected override bool OnIsFilter(GameObject go, QueryFilterOperator op, string value)
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
                return IsInView(go, SceneView.lastActiveSceneView?.camera);
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
                if (options.StartsWith("#"))
                    return FetchPropertyPropositions(options.tokens.First().Substring(1));
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
                            var label = $"#{p.name.Replace("m_", "")}";
                            var replacement = ToReplacementValue(p, label);
                            if (replacement != null)
                            {
                                var proposition = new SearchProposition(label: label, replacement, $"{cTypeName} ({p.propertyType})");
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
