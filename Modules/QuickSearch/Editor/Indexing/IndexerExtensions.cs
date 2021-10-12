// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEditor;
using UnityEditor.Search.Providers;
using UnityEngine;

namespace UnityEditor.Search
{
    static class IndexerExtensions
    {
        [CustomObjectIndexer(typeof(GameObject), version = 2)]
        internal static void IndexPrefabTypes(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            if (!(context.target is GameObject prefab))
                return;
            IndexPrefabProperties(context.documentIndex, prefab, indexer);
        }

        internal static void IndexPrefabProperties(int documentIndex, GameObject prefab, ObjectIndexer indexer)
        {
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

            if (PrefabUtility.HasPrefabInstanceAnyOverrides(prefab, false))
                indexer.IndexProperty(documentIndex, "prefab", "modified", saveKeyword: true, exact: true);

            if (PrefabUtility.HasPrefabInstanceAnyOverrides(prefab, true))
                indexer.IndexProperty(documentIndex, "prefab", "altered", saveKeyword: true, exact: true);
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

            indexer.AddNumber("events", clip.events.Length, indexer.settings.baseScore, context.documentIndex);
            foreach (var e in clip.events)
            {
                indexer.AddNumber("time", e.time, indexer.settings.baseScore, context.documentIndex);
                indexer.AddProperty("function", e.functionName.ToLowerInvariant(), context.documentIndex, saveKeyword: true, exact: false);
            }
        }

        [CustomObjectIndexer(typeof(Texture2D), version = 1)]
        internal static void Texture2DIndexing(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            if (!(context.target is Texture2D texture) || !indexer.settings.options.properties)
                return;

            indexer.AddProperty("format", texture.format.ToString().ToLowerInvariant(), context.documentIndex, saveKeyword: true, exact: true);
            indexer.AddProperty("filtermode", texture.filterMode.ToString().ToLowerInvariant(), context.documentIndex, saveKeyword: true, exact: true);
            indexer.AddProperty("dimension", texture.dimension.ToString().ToLowerInvariant(), context.documentIndex, saveKeyword: true, exact: true);

            var ti = AssetImporter.GetAtPath(context.id) as TextureImporter;
            if (ti)
            {
                indexer.AddProperty("type", ti.textureType.ToString().ToLowerInvariant(), context.documentIndex, saveKeyword: true, exact: true);
                indexer.AddProperty("shape", ti.textureShape.ToString().ToLowerInvariant(), context.documentIndex, saveKeyword: true, exact: true);
                indexer.AddProperty("readable", ti.isReadable.ToString().ToLowerInvariant(), context.documentIndex, saveKeyword: false, exact: true);
                indexer.AddProperty("srgb", ti.sRGBTexture.ToString().ToLowerInvariant(), context.documentIndex, saveKeyword: false, exact: true);
                indexer.AddProperty("compression", ti.textureCompression.ToString().ToLowerInvariant(), context.documentIndex, saveKeyword: true, exact: true);
                indexer.AddNumber("compressionquality", ti.compressionQuality, indexer.settings.baseScore, context.documentIndex);
            }
        }

        #region ShaderIndexing
        [CustomObjectIndexer(typeof(Shader), version = 1)]
        internal static void ShaderIndexing(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            if (!(context.target is Shader shader) || !indexer.settings.options.properties)
                return;

            var ownerPropertyType = typeof(Shader);
            for (int i = 0, end = shader.GetPropertyCount(); i != end; ++i)
            {
                var label = shader.GetPropertyName(i);
                var name = label.ToLowerInvariant();
                if (name.Length > 0 && name[0] == '_')
                    name = name.Substring(1);
                switch (shader.GetPropertyType(i))
                {
                    case UnityEngine.Rendering.ShaderPropertyType.Color:
                        var v = shader.GetPropertyDefaultVectorValue(i);
                        IndexColor(name, new Color(v.x, v.y, v.z, v.w), indexer, context.documentIndex, label, ownerPropertyType);
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Vector:
                        v = shader.GetPropertyDefaultVectorValue(i);
                        IndexVector(name, v, indexer, context.documentIndex, label, ownerPropertyType);
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Float:
                        indexer.IndexNumber(context.documentIndex, name, shader.GetPropertyDefaultFloatValue(i));
                        break;
                }
            }
        }

        #endregion

        [CustomObjectIndexer(typeof(Material), version = 4)]
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
                if (property.flags == MaterialProperty.PropFlags.HideInInspector)
                    continue;

                var upn = property.name;
                if (upn == null)
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

        [SceneQueryEngineFilter("material", supportedOperators = new[] { ":" })]
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
            }

            return false;
        }
    }
}
