// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace UnityEditor.Search
{
    static class IndexerExtensions
    {
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

        [CustomObjectIndexer(typeof(Shader), version = 1)]
        internal static void ShaderIndexing(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            if (!(context.target is Shader shader) ||
                !indexer.settings.options.properties ||
                !indexer.settings.type.Equals("asset", System.StringComparison.Ordinal))
                return;

            for (int i = 0, end = shader.GetPropertyCount(); i != end; ++i)
            {
                var name = shader.GetPropertyName(i).ToLowerInvariant();
                if (name.Length > 0 && name[0] == '_')
                    name = name.Substring(1);
                switch (shader.GetPropertyType(i))
                {
                    case UnityEngine.Rendering.ShaderPropertyType.Color:
                        var v = shader.GetPropertyDefaultVectorValue(i);
                        IndexColor(name, new Color(v.x, v.y, v.z, v.w), indexer, context.documentIndex);
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Vector:
                        v = shader.GetPropertyDefaultVectorValue(i);
                        IndexVector(name, v, indexer, context.documentIndex);
                        break;
                    case UnityEngine.Rendering.ShaderPropertyType.Float:
                        indexer.IndexNumber(context.documentIndex, name, shader.GetPropertyDefaultFloatValue(i));
                        break;
                }
            }
        }

        [CustomObjectIndexer(typeof(Material), version = 1)]
        internal static void MaterialShaderReferences(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            var material = context.target as Material;
            if (material == null)
                return;

            if (material.shader)
            {
                var fullShaderName = material.shader.name.ToLowerInvariant();
                indexer.AddReference(context.documentIndex, fullShaderName);
            }

            if (!indexer.settings.type.Equals("asset", System.StringComparison.Ordinal))
                return;

            if (!indexer.settings.options.properties)
                return;

            var properties = MaterialEditor.GetMaterialProperties(new Material[] { material });
            foreach (var property in properties)
            {
                var propertyName = property.name.ToLowerInvariant();
                if (propertyName.Length > 0 && propertyName[0] == '_')
                    propertyName = propertyName.Substring(1);
                switch (property.type)
                {
                    case MaterialProperty.PropType.Color:
                        IndexColor(propertyName, property.colorValue, indexer, context.documentIndex);
                        break;

                    case MaterialProperty.PropType.Vector:
                        IndexVector(propertyName, property.vectorValue, indexer, context.documentIndex);
                        break;

                    case MaterialProperty.PropType.Float:
                        indexer.AddNumber(propertyName, property.floatValue, indexer.settings.baseScore, context.documentIndex);
                        break;

                    case MaterialProperty.PropType.Range:
                        IndexVector(propertyName, property.rangeLimits, indexer, context.documentIndex);
                        break;

                    case MaterialProperty.PropType.Texture:
                        if (property.textureValue)
                            indexer.AddReference(context.documentIndex, AssetDatabase.GetAssetPath(property.textureValue));
                        break;
                }
            }
        }

        [CustomObjectIndexer(typeof(MeshRenderer), version = 1)]
        internal static void IndexMeshRendererMaterials(CustomObjectIndexerTarget context, ObjectIndexer indexer)
        {
            var c = context.target as MeshRenderer;
            if (c == null || !indexer.settings.type.Equals("asset", System.StringComparison.Ordinal))
                return;

            indexer.AddNumber("materialcount", c.sharedMaterials.Length, indexer.settings.baseScore + 2, context.documentIndex);
            foreach (var m in c.sharedMaterials)
            {
                indexer.AddProperty("material", m.name.Replace(" (Instance)", "").ToLowerInvariant(), context.documentIndex, saveKeyword: true);

                var mp = AssetDatabase.GetAssetPath(m);
                if (!string.IsNullOrEmpty(mp))
                    indexer.AddProperty("material", mp.ToLowerInvariant(), context.documentIndex, saveKeyword: false, exact: true);
            }
        }

        internal static void IndexColor(string propertyName, Color c, ObjectIndexer indexer, int documentIndex)
        {
            var colorHex = c.a < 1f ? ColorUtility.ToHtmlStringRGBA(c) : ColorUtility.ToHtmlStringRGB(c);
            indexer.AddProperty(propertyName, colorHex.ToLowerInvariant(), documentIndex, exact: true, saveKeyword: false);
        }

        internal static void IndexVector(string propertyName, Vector2 v, ObjectIndexer indexer, int documentIndex)
        {
            indexer.AddNumber(propertyName + ".x", v.x, indexer.settings.baseScore, documentIndex);
            indexer.AddNumber(propertyName + ".y", v.y, indexer.settings.baseScore, documentIndex);
        }

        internal static void IndexVector(string propertyName, Vector3 v, ObjectIndexer indexer, int documentIndex)
        {
            indexer.AddNumber(propertyName + ".x", v.x, indexer.settings.baseScore, documentIndex);
            indexer.AddNumber(propertyName + ".y", v.y, indexer.settings.baseScore, documentIndex);
            indexer.AddNumber(propertyName + ".z", v.z, indexer.settings.baseScore, documentIndex);
        }

        internal static void IndexVector(string propertyName, Vector4 v, ObjectIndexer indexer, int documentIndex)
        {
            indexer.AddNumber(propertyName + ".x", v.x, indexer.settings.baseScore, documentIndex);
            indexer.AddNumber(propertyName + ".y", v.y, indexer.settings.baseScore, documentIndex);
            indexer.AddNumber(propertyName + ".z", v.z, indexer.settings.baseScore, documentIndex);
            indexer.AddNumber(propertyName + ".w", v.w, indexer.settings.baseScore, documentIndex);
        }
    }
}
