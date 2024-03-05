// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Profiling;
using UnityEditor.Search.Providers;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Search
{
    readonly struct PrefabPropertyIndexKey
    {
        public readonly int documentIndex;
        public readonly int instanceId;

        public PrefabPropertyIndexKey(int documentIndex, int instanceId)
        {
            this.documentIndex = documentIndex;
            this.instanceId = instanceId;
        }
    }

    struct PrefabPropertyIndexData
    {
        public bool isModified;
        public bool isAltered;
    }

    static class IndexerExtensions
    {
        static readonly ProfilerMarker k_IndexPrefabPropertiesMarker = new($"{nameof(IndexerExtensions)}.{nameof(IndexPrefabProperties)}");

        static ConcurrentDictionary<ObjectIndexer, HashSet<PrefabPropertyIndexKey>> s_PrefabIndexCaches = new();
        static ConcurrentDictionary<ObjectIndexer, Dictionary<int, PrefabPropertyIndexData>> s_PrefabInstanceRootCaches = new();

        [CustomObjectIndexer(typeof(GameObject), version = 2)]
        internal static void IndexPrefabTypes(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            if (!(context.target is GameObject prefab))
                return;
            IndexPrefabProperties(context.documentIndex, prefab, indexer);
        }

        internal static void IndexPrefabProperties(int documentIndex, GameObject prefab, ObjectIndexer indexer)
        {
            using var _ = k_IndexPrefabPropertiesMarker.Auto();

            var prefabIndexCache = s_PrefabIndexCaches.GetOrAdd(indexer, _ => new HashSet<PrefabPropertyIndexKey>());
            var key = new PrefabPropertyIndexKey(documentIndex, prefab.GetInstanceID());
            if (prefabIndexCache.Contains(key))
                return; // Already indexed

            var prefabType = PrefabUtility.GetPrefabAssetType(prefab);
            indexer.IndexProperty(documentIndex, "prefab", prefabType.ToString(), saveKeyword: true, exact: true);

            if (prefabType != PrefabAssetType.NotAPrefab)
                indexer.IndexProperty(documentIndex, "prefab", "any", saveKeyword: true, exact: true);

            var rootPrefab = PrefabUtility.GetOriginalSourceOrVariantRoot(prefab);
            if (rootPrefab != null && rootPrefab != prefab)
                indexer.AddReference(documentIndex, "root", rootPrefab, "Root Prefab", typeof(GameObject));

            var source = PrefabUtility.GetCorrespondingObjectFromSource(prefab);
            if (source != null && source != prefab)
                indexer.AddReference(documentIndex, "base", source, "Base Prefab", typeof(GameObject));

            if (rootPrefab == null || source == null)
            {
                indexer.IndexProperty(documentIndex, "prefab", "base", saveKeyword: true, exact: true);
            }

            var instanceRoot = PrefabUtility.GetPrefabInstanceHandle(prefab);
            var instanceRootId = instanceRoot != null ? instanceRoot.GetInstanceID() : 0;
            var indexerDictionary = s_PrefabInstanceRootCaches.GetOrAdd(indexer, _ => new Dictionary<int, PrefabPropertyIndexData>());

            if (instanceRoot != null && indexerDictionary.TryGetValue(instanceRootId, out var data))
            {
                if (data.isModified)
                    indexer.IndexProperty(documentIndex, "prefab", "modified", saveKeyword: true, exact: true);
                if (data.isAltered)
                    indexer.IndexProperty(documentIndex, "prefab", "altered", saveKeyword: true, exact: true);
            }
            else
            {
                var newData = new PrefabPropertyIndexData
                {
                    isModified = PrefabUtility.HasPrefabInstanceAnyOverrides(prefab, false),
                    isAltered = PrefabUtility.HasPrefabInstanceAnyOverrides(prefab, true)
                };
                if (newData.isModified)
                    indexer.IndexProperty(documentIndex, "prefab", "modified", saveKeyword: true, exact: true);
                if (newData.isAltered)
                    indexer.IndexProperty(documentIndex, "prefab", "altered", saveKeyword: true, exact: true);

                if (instanceRoot != null)
                    indexerDictionary.TryAdd(instanceRootId, newData);
            }

            prefabIndexCache.Add(key);
        }

        internal static void ClearIndexerCaches(ObjectIndexer indexer)
        {
            s_PrefabIndexCaches.TryRemove(indexer, out _);
            s_PrefabInstanceRootCaches.TryRemove(indexer, out _);
        }

        [SearchSelector("prefabtype", provider: "scene")]
        [SearchSelector("prefabtype", provider: "asset")]
        [System.ComponentModel.Description("Prefab Type")]
        internal static object GetPrefabType(SearchItem item)
        {
            var prefab = item.ToObject();
            if (!prefab)
                return null;
            return PrefabUtility.GetPrefabAssetType(prefab);
        }

        [SearchSelector("prefabstatus", provider: "scene")]
        [SearchSelector("prefabstatus", provider: "asset")]
        [System.ComponentModel.Description("Prefab Status")]
        internal static object GetPrefabStatus(SearchItem item)
        {
            var prefab = item.ToObject();
            if (!prefab)
                return null;
            return PrefabUtility.GetPrefabInstanceStatus(prefab);
        }

        [SearchSelector("prefabbase", provider: "scene")]
        [SearchSelector("prefabbase", provider: "asset")]
        [System.ComponentModel.Description("Prefab Base")]
        internal static object GetPrefabBase(SearchItem item)
        {
            var prefab = item.ToObject();
            if (!prefab)
                return null;
            var basePrefab = PrefabUtility.GetCorrespondingObjectFromSource(prefab);
            if (basePrefab == null || prefab == basePrefab)
                return null;
            return basePrefab;
        }

        [SearchSelector("prefabroot", provider: "scene")]
        [SearchSelector("prefabroot", provider: "asset")]
        [System.ComponentModel.Description("Prefab Root")]
        internal static object GetPrefabRoot(SearchItem item)
        {
            var prefab = item.ToObject();
            if (!prefab)
                return null;
            var rootPrefab = PrefabUtility.GetOriginalSourceOrVariantRoot(prefab);
            if (rootPrefab == null || prefab == rootPrefab)
                return null;
            return rootPrefab;
        }

        [CustomObjectIndexer(typeof(AnimationClip), version = 1)]
        internal static void AnimationClipEventsIndexing(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            if (!(context.target is AnimationClip clip) || !indexer.settings.options.properties)
                return;

            indexer.AddProperty("t", "animation", indexer.settings.baseScore, context.documentIndex);

            indexer.AddNumber("events", clip.events.Length, indexer.settings.baseScore, context.documentIndex);
            foreach (var e in clip.events)
            {
                indexer.AddNumber("time", e.time, indexer.settings.baseScore, context.documentIndex);
                indexer.AddProperty("function", e.functionName.ToLowerInvariant(), context.documentIndex, saveKeyword: true, exact: false);
            }
        }

        [CustomObjectIndexer(typeof(TerrainData), version = 1)]
        internal static void TerrainIndexing(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            if (!(context.target is TerrainData terrain) || !indexer.settings.options.types)
                return;
            indexer.AddProperty("t", "terrain", context.documentIndex);
        }

        [CustomObjectIndexer(typeof(AssemblyDefinitionReferenceAsset), version = 1)]
        internal static void AssemblyDefRefIndexing(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            if (!(context.target is AssemblyDefinitionReferenceAsset asmref) || !indexer.settings.options.types)
                return;
            indexer.AddProperty("t", "asmref", context.documentIndex);
        }

        [CustomObjectIndexer(typeof(AssemblyDefinitionAsset), version = 1)]
        internal static void AssemblyDefIndexing(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            if (!(context.target is AssemblyDefinitionAsset asmdef) || !indexer.settings.options.types)
                return;
            indexer.AddProperty("t", "asmdef", context.documentIndex);
        }

        [CustomObjectIndexer(typeof(Texture2D), version = 3)]
        internal static void Texture2DIndexing(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            if (!(context.target is Texture2D texture) || !indexer.settings.options.properties)
                return;

            indexer.IndexProperty<TextureFormat, Texture2D>(context.documentIndex, "format", texture.format.ToString(), saveKeyword: true, exact: true, "Format", string.Empty);
            indexer.IndexProperty<FilterMode, Texture2D>(context.documentIndex, "filtermode", texture.filterMode.ToString(), saveKeyword: true, exact: true, "Filter Mode", string.Empty);
            indexer.IndexProperty<TextureDimension, Texture2D>(context.documentIndex, "dimension", texture.dimension.ToString(), saveKeyword: true, exact: true, "Dimension", string.Empty);

            var ti = AssetImporter.GetAtPath(context.id) as TextureImporter;
            if (ti)
            {
                indexer.IndexProperty<TextureImporterType, TextureImporter>(context.documentIndex, "type", ti.textureType.ToString(), saveKeyword: true, exact: true, "Type", string.Empty);
                indexer.IndexProperty<TextureImporterShape, TextureImporter>(context.documentIndex, "shape", ti.textureShape.ToString(), saveKeyword: true, exact: true, "Shape", string.Empty);
                indexer.IndexProperty<bool, TextureImporter>(context.documentIndex, "readable", ti.isReadable.ToString(), saveKeyword: false, exact: true, "Readable", string.Empty);
                indexer.IndexProperty<bool, TextureImporter>(context.documentIndex, "srgb", ti.sRGBTexture.ToString(), saveKeyword: false, exact: true, "sRGB", string.Empty);
                indexer.IndexProperty<TextureImporterCompression, TextureImporter>(context.documentIndex, "compression", ti.textureCompression.ToString(), saveKeyword: true, exact: true, "Compression", string.Empty);

                var so = new SerializedObject(ti);
                var psArray = so.FindProperty("m_PlatformSettings");
                if (psArray != null)
                {
                    for (var i = 0; i < psArray.arraySize; ++i)
                    {
                        var platformSettings = psArray.GetArrayElementAtIndex(i);
                        var buildTarget = platformSettings.FindPropertyRelative("m_BuildTarget");
                        if (buildTarget != null && buildTarget.stringValue == TextureImporter.defaultPlatformName)
                        {
                            // Loop over all properties in the DefaultPlatformSettings
                            var parentPath = platformSettings.propertyPath;
                            indexer.IndexProperties(context.documentIndex, platformSettings, recursive: false, 2, p => p.propertyPath.StartsWith(parentPath));
                            break;
                        }
                    }
                }
            }
        }

        #region ShaderIndexing
        [CustomObjectIndexer(typeof(Shader), version = 2)]
        internal static void ShaderIndexing(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            if (!(context.target is Shader shader) || !indexer.settings.options.properties)
                return;

            var ownerPropertyType = typeof(Shader);
            for (int i = 0, end = shader.GetPropertyCount(); i != end; ++i)
            {
                var label = shader.GetPropertyName(i);

                // Keep some property name patterns
                if (s_IgnorePropertyNameRx.IsMatch(label))
                    continue;

                var name = label.ToLowerInvariant();
                if (name.Length > 0 && name[0] == '_')
                    name = name.Substring(1);
                switch (shader.GetPropertyType(i))
                {
                    case ShaderPropertyType.Color:
                        var v = shader.GetPropertyDefaultVectorValue(i);
                        IndexColor(name, new Color(v.x, v.y, v.z, v.w), indexer, context.documentIndex, label, ownerPropertyType);
                        break;
                    case ShaderPropertyType.Vector:
                        v = shader.GetPropertyDefaultVectorValue(i);
                        IndexVector(name, v, indexer, context.documentIndex, label, ownerPropertyType);
                        break;
                    case ShaderPropertyType.Float:
                        indexer.IndexNumber(context.documentIndex, name, shader.GetPropertyDefaultFloatValue(i));
                        break;
                }
            }
        }

        #endregion

        static readonly Regex s_IgnorePropertyNameRx = new Regex(@"_([A-F0-9]{8}|[a-f0-9]{32})$");
        [CustomObjectIndexer(typeof(Material), version = 15)]
        internal static void MaterialShaderReferences(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            var material = context.target as Material;
            if (material == null || !material.shader)
                return;

            indexer.AddReference(context.documentIndex, "shader", material.shader);

            if (!indexer.settings.options.properties)
                return;

            var ownerPropertyType = typeof(Shader);
            var shaderName = $"{material.shader.name}/" ?? string.Empty;
            var properties = MaterialEditor.GetMaterialProperties(new Material[] { material });
            foreach (var property in properties)
            {
                var flags = (ShaderPropertyFlags)property.flags;
                if ((flags & (ShaderPropertyFlags.HideInInspector | ShaderPropertyFlags.NonModifiableTextureData)) != 0)
                    continue;

                var upn = property.name;
                if (upn == null)
                    continue;

                // Keep some property name patterns
                if (s_IgnorePropertyNameRx.IsMatch(upn))
                    continue;

                var propertyName = upn.ToLowerInvariant();
                if (propertyName.Length > 0 && propertyName[0] == '_')
                    propertyName = propertyName.Substring(1);
                if (propertyName.Length < 3)
                    continue;

                var shaderPropName = $"{shaderName}{property.displayName}";
                switch (property.type)
                {
                    case MaterialProperty.PropType.Color:
                        IndexColor(propertyName, property.colorValue, indexer, context.documentIndex, shaderPropName, ownerPropertyType);
                        break;

                    case MaterialProperty.PropType.Vector:
                        IndexVector(propertyName, property.vectorValue, indexer, context.documentIndex, shaderPropName, ownerPropertyType);
                        break;

                    case MaterialProperty.PropType.Int:
                        indexer.AddNumber(propertyName, property.intValue, indexer.settings.baseScore, context.documentIndex);
                        indexer.MapProperty(propertyName, shaderPropName, null, "Number", ownerPropertyType.AssemblyQualifiedName, false);
                        break;

                    case MaterialProperty.PropType.Float:
                        indexer.AddNumber(propertyName, property.floatValue, indexer.settings.baseScore, context.documentIndex);
                        indexer.MapProperty(propertyName, shaderPropName, null, "Number", ownerPropertyType.AssemblyQualifiedName, false);
                        break;

                    case MaterialProperty.PropType.Range:
                        indexer.AddNumber(propertyName, property.floatValue, indexer.settings.baseScore, context.documentIndex);
                        indexer.MapProperty(propertyName, shaderPropName, null, "Number", ownerPropertyType.AssemblyQualifiedName, false);
                        break;

                    case MaterialProperty.PropType.Texture:
                        if (property.textureValue)
                            indexer.AddReference(context.documentIndex, propertyName, property.textureValue, shaderPropName, ownerPropertyType);
                        break;
                }
            }
        }

        [CustomObjectIndexer(typeof(MeshRenderer), version = 2)]
        internal static void IndexMeshRendererMaterials(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            var c = context.target as MeshRenderer;
            if (!c || !indexer.settings.options.properties)
                return;

            indexer.AddNumber("materialcount", c.sharedMaterials.Length, indexer.settings.baseScore + 2, context.documentIndex);
            foreach (var m in c.sharedMaterials)
            {
                if (!m)
                    continue;

                if (!string.IsNullOrEmpty(m.name) && indexer.settings.options.types)
                    indexer.AddProperty("material", m.name.Replace(" (Instance)", "").ToLowerInvariant(), context.documentIndex, saveKeyword: false, exact: false);

                if (indexer.settings.options.dependencies)
                {
                    var mp = AssetDatabase.GetAssetPath(m);
                    if (!string.IsNullOrEmpty(mp))
                        indexer.AddReference(context.documentIndex, mp);
                }

                if (m.shader != null)
                    indexer.AddReference(context.documentIndex, "shader", m.shader);
            }
        }

        internal static void IndexColor(string propertyName, in Color c, ObjectIndexer indexer, int documentIndex, in string label = null, in System.Type ownerType = null)
        {
            var colorHex = c.a < 1f ? ColorUtility.ToHtmlStringRGBA(c) : ColorUtility.ToHtmlStringRGB(c);
            indexer.AddProperty(propertyName, "#" + colorHex.ToLowerInvariant(), documentIndex, exact: true, saveKeyword: false);
            indexer.AddNumber(propertyName + ".r", c.r, indexer.settings.baseScore, documentIndex);
            indexer.AddNumber(propertyName + ".g", c.g, indexer.settings.baseScore, documentIndex);
            indexer.AddNumber(propertyName + ".b", c.b, indexer.settings.baseScore, documentIndex);
            indexer.AddNumber(propertyName + ".a", c.a, indexer.settings.baseScore, documentIndex);
            if (label != null && ownerType != null)
                indexer.MapProperty(propertyName, label ?? propertyName, null, "Color", ownerType?.AssemblyQualifiedName, removeNestedKeys: true);
        }

        internal static void IndexVector(string propertyName, in Vector2 v, ObjectIndexer indexer, int documentIndex, in string label = null, in System.Type ownerType = null)
        {
            indexer.AddNumber(propertyName + ".x", v.x, indexer.settings.baseScore, documentIndex);
            indexer.AddNumber(propertyName + ".y", v.y, indexer.settings.baseScore, documentIndex);
            if (label != null && ownerType != null)
                indexer.MapProperty(propertyName, label ?? propertyName, null, "Vector2", ownerType?.AssemblyQualifiedName, removeNestedKeys: true);
        }

        internal static void IndexVector(string propertyName, in Vector3 v, ObjectIndexer indexer, int documentIndex, in string label = null, in System.Type ownerType = null)
        {
            indexer.AddNumber(propertyName + ".x", v.x, indexer.settings.baseScore, documentIndex);
            indexer.AddNumber(propertyName + ".y", v.y, indexer.settings.baseScore, documentIndex);
            indexer.AddNumber(propertyName + ".z", v.z, indexer.settings.baseScore, documentIndex);
            if (label != null && ownerType != null)
                indexer.MapProperty(propertyName, label ?? propertyName, null, "Vector3", ownerType?.AssemblyQualifiedName, removeNestedKeys: true);
        }

        internal static void IndexVector(string propertyName, in Vector4 v, ObjectIndexer indexer, int documentIndex, in string label = null, in System.Type ownerType = null)
        {
            indexer.AddNumber(propertyName + ".x", v.x, indexer.settings.baseScore, documentIndex);
            indexer.AddNumber(propertyName + ".y", v.y, indexer.settings.baseScore, documentIndex);
            indexer.AddNumber(propertyName + ".z", v.z, indexer.settings.baseScore, documentIndex);
            indexer.AddNumber(propertyName + ".w", v.w, indexer.settings.baseScore, documentIndex);
            if (label != null && ownerType != null)
                indexer.MapProperty(propertyName, label ?? propertyName, null, "Vector4", ownerType?.AssemblyQualifiedName, removeNestedKeys: true);
        }

        [SceneQueryEngineFilter("material", new[] { ":" }, "material:<$object:none,Material$>")]
        [System.ComponentModel.Description("Check if a MeshRenderer uses a specific material")]
        internal static bool FilterMeshRendererMaterials(GameObject go, string op, string value)
        {
            if (!go.TryGetComponent<MeshRenderer>(out var c))
                return false;
            foreach (var m in c.sharedMaterials)
            {
                if (m == null || m.name == null)
                    continue;
                var mname = m.name.Replace(" (Instance)", "");
                if (mname.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) != -1)
                    return true;

                // Try with the asset path of the material.
                var path = AssetDatabase.GetAssetPath(m);
                if (!string.IsNullOrEmpty(path) && path.IndexOf(value, StringComparison.OrdinalIgnoreCase) != -1)
                    return true;

                // Try with the GlobalObjectId of the material.
                if (value.StartsWith("GlobalObjectId", StringComparison.Ordinal))
                {
                    var goid = GlobalObjectId.GetGlobalObjectIdSlow(m);
                    if (goid.ToString().Equals(value, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            return false;
        }
    }
}
