// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using Unity.ProjectAuditor.Editor.AssetAnalysis;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.ProjectAuditor.Editor.Modules
{
    class TextureUtilizationAnalyzer : TextureModuleAnalyzer
    {
        internal const string PAA0005 = nameof(PAA0005);
        internal const string PAA0006 = nameof(PAA0006);
        internal const string PAA0007 = nameof(PAA0007);

        internal static readonly Descriptor k_TextureSolidColorDescriptor = new Descriptor(
            PAA0005,
            "Texture: Solid color is not 1x1 size",
            Areas.Memory,
            "The texture is a single, solid color and is bigger than 1x1 pixels in size. Redundant texture data occupies memory unnecessarily.",
            "Consider shrinking the texture to 1x1 size."
        )
        {
            IsEnabledByDefault = false,
            MessageFormat = "Texture '{0}' is a solid color and not 1x1 size",
            Fixer = (issue, analysisParams) => { return ShrinkSolidTexture(issue.RelativePath); }
        };

        // NOTE:  This is only here to run the same analysis without a quick fix button.  Clean up when we either have appropriate quick fix for other dimensions or improved Fixer support.
        internal static readonly Descriptor k_TextureSolidColorNoFixerDescriptor = new Descriptor(
            PAA0006,
            "Texture: Solid color is not 1x1 size",
            Areas.Memory,
            "The texture is a single, solid color and is bigger than 1x1 pixels in size. Redundant texture data occupies memory unnecessarily.",
            "Consider shrinking the texture to 1x1 size."
        )
        {
            IsEnabledByDefault = false,
            MessageFormat = "Texture '{0}' is a solid color and not 1x1 size"
        };

        internal static readonly Descriptor k_TextureAtlasEmptyDescriptor = new Descriptor(
            PAA0007,
            "Texture Atlas: Too much empty space",
            Areas.Memory,
            "The texture atlas contains a lot of empty space. Empty space contributes to texture memory usage.",
            "Consider reorganizing your texture atlas in order to reduce the amount of empty space."
        )
        {
            IsEnabledByDefault = false,
            MessageFormat = "Texture Atlas '{0}' has too much empty space ({1}, {2})"
        };

        //[DiagnosticParameter("TextureEmptySpaceLimit", "Empty Texture Atlas use threshold (percentage)", "Warn if the percentage of unused pixels in a Texture Atlas is greater than this threshold.", 50)]
        const int m_EmptySpaceLimit = 0;

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_TextureSolidColorDescriptor);
            registerDescriptor(k_TextureSolidColorNoFixerDescriptor);
            registerDescriptor(k_TextureAtlasEmptyDescriptor);
        }

        public override IEnumerable<ReportItem> Analyze(TextureAnalysisContext context)
        {
            var dimensionAppropriateDescriptor = context.Texture.dimension == UnityEngine.Rendering.TextureDimension.Tex2D ? k_TextureSolidColorDescriptor : k_TextureSolidColorNoFixerDescriptor;
            if (context.IsDescriptorEnabled(dimensionAppropriateDescriptor) &&
                TextureUtils.IsTextureSolidColorTooBig(context.Importer, context.Texture))
            {
                var location = new Location(context.Importer.assetPath);
                var dependencyNode = new TextureDependencyNode { Location = location };

                yield return context.CreateIssue(IssueCategory.AssetIssue, dimensionAppropriateDescriptor.Id, context.Name)
                    .WithDependencies(dependencyNode)
                    .WithLocation(location);
            }

            var texture2D = context.Texture as Texture2D;
            if (context.IsDescriptorEnabled(k_TextureAtlasEmptyDescriptor) && texture2D != null)
            {
                TextureUtils.GetEmptyPixelsPercent(texture2D, out var emptyPercent, out var emptyBytes);
                if (emptyPercent > m_EmptySpaceLimit)
                {
                    var location = new Location(context.Importer.assetPath);
                    var dependencyNode = new TextureDependencyNode { Location = location };

                    yield return context.CreateIssue(IssueCategory.AssetIssue, k_TextureAtlasEmptyDescriptor.Id, context.Name, Formatting.FormatPercentage(emptyPercent / 100.0f), Formatting.FormatSize(emptyBytes))
                        .WithDependencies(dependencyNode)
                        .WithLocation(location);
                }
            }
        }

        internal static bool ShrinkSolidTexture(string path)
        {
            var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
            if (textureImporter == null)
                return false;

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
                return false;

            Color color;
            if (textureImporter.isReadable)
            {
                color = texture.GetPixel(0, 0);
            }
            else
            {
                var copy = TextureUtils.CopyTexture(texture);
                color = copy.GetPixel(0, 0);
                UnityEngine.Object.DestroyImmediate(copy);
            }

            var newTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            newTexture.SetPixel(0, 0, color);
            newTexture.Apply();
            File.WriteAllBytes(path, newTexture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(newTexture);

            textureImporter.SaveAndReimport();
            return true;
        }
    }
}
