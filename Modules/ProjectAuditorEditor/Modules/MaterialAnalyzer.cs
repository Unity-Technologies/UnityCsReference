// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class MaterialAnalyzer : MaterialModuleAnalyzer
    {
        internal const string PAA5000 = nameof(PAA5000);

        internal static readonly Descriptor k_MaterialNormalMapTexturesDescriptor = new Descriptor(
            PAA5000,
            "Material: NormalMap Textures are not imported with the NormalMap type",
            Areas.GPU,
            "All textures assigned to NormalMap slots must use the <b>Normal Map</b> Texture Type in the Texture Import Settings. Not doing this will result in NormalMaps not rendering correctly.",
            "Set the affected textures to use the NormalMap type in the Texture Import Settings."
        )
        {
            MessageFormat = "Material '{0}' uses Texture '{1}' that has not been imported as a Normal Map",
            Fixer = (issue, analysisParams) =>
            {
                var textureImporter = AssetImporter.GetAtPath(issue.Location.Path) as TextureImporter;
                if (textureImporter != null)
                {
                    textureImporter.textureType = TextureImporterType.NormalMap;
                    textureImporter.SaveAndReimport();
                    return true;
                }

                return false;
            }
        };

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_MaterialNormalMapTexturesDescriptor);
        }

        public override IEnumerable<ReportItem> Analyze(MaterialAnalysisContext context)
        {
            Material material = context.Material;
            Shader shader = material.shader;

            var foundTextureNames = new HashSet<string>();

            // Firstly, enumerate all textures and check for [Normal] attr
            for (int i = 0, count = shader.GetPropertyCount(); i < count; i++)
            {
                if (shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture &&
                    ((shader.GetPropertyFlags(i) & UnityEngine.Rendering.ShaderPropertyFlags.Normal) != 0))
                {
                    TextureImporter textureImporter = FindTextureImporterForProperty(material, shader.GetPropertyName(i), out var texture);
                    if (textureImporter == null || textureImporter.textureType == TextureImporterType.NormalMap)
                        continue;
                    if (foundTextureNames.Contains(texture.name))
                        continue;

                    foundTextureNames.Add(texture.name);

                    yield return context.CreateIssue(IssueCategory.AssetIssue,
                        k_MaterialNormalMapTexturesDescriptor.Id, context.Name, texture.name)
                        .WithLocation(textureImporter.assetPath);
                }
            }

            // Secondly, support "old style" shaders: also check builtin normal map properties
            string[] kBumpMapProps = { "_BumpMap", "_DetailBumpMap" };
            for (int i = 0; i < kBumpMapProps.Length; i++)
            {
                if (material.HasTexture(kBumpMapProps[i]))
                {
                    TextureImporter textureImporter = FindTextureImporterForProperty(material, kBumpMapProps[i], out var texture);
                    if (textureImporter == null || textureImporter.textureType == TextureImporterType.NormalMap)
                        continue;
                    if (foundTextureNames.Contains(texture.name))
                        continue;

                    foundTextureNames.Add(texture.name);

                    yield return context.CreateIssue(IssueCategory.AssetIssue,
                        k_MaterialNormalMapTexturesDescriptor.Id, context.Name, texture.name)
                        .WithLocation(textureImporter.assetPath);
                }
            }
        }

        private TextureImporter FindTextureImporterForProperty(Material material, string propertyName, out Texture texture)
        {
            texture = material.GetTexture(propertyName);
            if (texture == null)
                return null;

            string path = AssetDatabase.GetAssetPath(texture);
            return AssetImporter.GetAtPath(path) as TextureImporter;
        }
    }
}
