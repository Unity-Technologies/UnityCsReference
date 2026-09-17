// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.Modules
{
    /// <summary>
    /// Utilities to help with migration advice to the Universal Render Pipeline.
    /// </summary>
    public static class MigrationToURPUtilities
    {
        internal const string k_UrpPackageName = "com.unity.render-pipelines.universal";
        const string k_UrpAssetTypeName = "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset";
        const string k_HdrpAssetTypeName = "UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset";

        /// <summary>
        /// Link to the documentation.
        /// </summary>
        public static string DocumentationUrl => "https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html?subfolder=/manual/upgrade-guides.html";

        /// <summary>
        /// Shows where to install the URP package, and opens the Render Pipeline Converter tool.
        /// </summary>
        /// <param name="issue">ReportItem triggering this Quick Fix helper.</param>
        /// <param name="analysisParams">AnalysisParams for the current analysis.</param>
        /// <returns>Whether the method fixed the issue. Always false for this fixer.</returns>
        public static bool OpenRenderPipelineConverter(ReportItem issue, AnalysisParams analysisParams)
        {
            // When an SRP package is installed, the Render Pipeline Converter is the migration entry point.
            if (EditorApplication.ExecuteMenuItem("Window/Rendering/Render Pipeline Converter"))
                return false;

            // No SRP package is installed yet: open Package Manager so the user can add URP first.
            UnityEditor.PackageManager.UI.Window.Open(k_UrpPackageName);

            // Opening a window doesn't migrate the project, so the issue remains until BiRP is no longer in use.
            return false;
        }

        internal static bool IsUrpPackageInstalled()
        {
            return UnityEditor.PackageManager.PackageInfo.IsPackageRegistered(k_UrpPackageName);
        }

        // A quality level with no asset of its own renders with the project-wide default.
        static RenderPipelineAsset GetEffectiveRenderPipelineAssetAt(int qualityLevel)
        {
            var asset = QualitySettings.GetRenderPipelineAssetAt(qualityLevel);
            return (asset == null) ? GraphicsSettings.defaultRenderPipeline : asset;
        }

        // Unlike the migration checks in DescriptorExtensions, Built-in is not a match: is every asset in use URP?
        internal static bool IsProjectFullyOnUrp()
        {
            var qualityLevelCount = QualitySettings.names.Length;

            // With no Quality Levels, Unity uses the project-wide asset and there is nothing else in play.
            if (qualityLevelCount == 0)
                return HasDefaultUrpRenderPipeline();

            for (var i = 0; i < qualityLevelCount; ++i)
            {
                // A level left on Built-in reads as a match for migration, but it is not URP.
                var asset = GetEffectiveRenderPipelineAssetAt(i);
                if (asset == null || !ShouldMigrateRenderPipelineAsset(asset))
                    return false;
            }

            return true;
        }

        // A default from another SRP leaves the wizard's work undone, so only a URP default counts as assigned.
        // ShouldMigrateRenderPipelineAsset reads a null asset as Built-in, which is not a default at all.
        internal static bool HasDefaultUrpRenderPipeline()
        {
            var asset = GraphicsSettings.defaultRenderPipeline;
            return asset != null && ShouldMigrateRenderPipelineAsset(asset);
        }

        // A mixed project can hold assets for other SRPs, and any of those promoted project-wide would switch
        // every Built-in quality level to the wrong pipeline, so only URP assets are candidates.
        // Reads each level's own asset rather than its effective one: the default is what this feeds.
        internal static RenderPipelineAsset GetCurrentQualityUrpAsset()
        {
            var currentQualityAsset = QualitySettings.renderPipeline;
            if (currentQualityAsset != null && ShouldMigrateRenderPipelineAsset(currentQualityAsset))
                return currentQualityAsset;

            for (int i = 0, c = QualitySettings.names.Length; i < c; ++i)
            {
                var asset = QualitySettings.GetRenderPipelineAssetAt(i);
                if (asset != null && ShouldMigrateRenderPipelineAsset(asset))
                    return asset;
            }

            return null;
        }

        // The Render Pipeline Converter assigns a URP asset to each quality level but never sets a default.
        internal static RenderPipelineAsset AssignCurrentQualityUrpAssetAsDefault()
        {
            var asset = GetCurrentQualityUrpAsset();
            if (asset != null)
                GraphicsSettings.defaultRenderPipeline = asset;
            return asset;
        }

        // Prefers an asset the Converter already assigned; creation is delegated via TypeCache to avoid referencing URP.
        internal static RenderPipelineAsset EnsureDefaultRenderPipelineAsset()
        {
            // Already done: re-promoting a quality asset over an existing URP default would only change its settings.
            if (HasDefaultUrpRenderPipeline())
                return GraphicsSettings.defaultRenderPipeline;

            var existing = AssignCurrentQualityUrpAssetAsDefault();
            if (existing != null)
                return existing;

            foreach (var type in UnityEditor.TypeCache.GetTypesDerivedFrom<IRenderPipelineAssetCreator>())
            {
                if (type.IsAbstract)
                    continue;

                var creator = TryCreateAssetCreator(type);
                if (creator != null)
                    return creator.CreateAndAssignDefault();
            }

            return null; // No render-pipeline package with a creator is installed.
        }

        // Creators ship inside render-pipeline packages, so neither the type nor its constructor is guaranteed to be public.
        static IRenderPipelineAssetCreator TryCreateAssetCreator(Type type)
        {
            try
            {
                return Activator.CreateInstance(type, nonPublic: true) as IRenderPipelineAssetCreator;
            }
            catch (Exception e)
            {
                // One creator we cannot construct must not stop the wizard from trying the others.
                Debug.LogWarning($"Could not create an instance of {type.FullName}: {e.Message}");
                return null;
            }
        }

        internal static string GetDefaultRenderPipelineDisplayName()
        {
            var asset = GraphicsSettings.defaultRenderPipeline;
            if (asset == null)
                return string.Empty;

            if (IsAssetOfType(asset, k_UrpAssetTypeName))
                return MigrationWorkflowView.Contents.URP;

            if (IsAssetOfType(asset, k_HdrpAssetTypeName))
                return MigrationWorkflowView.Contents.HDRP;

            // A custom SRP has no product name to show, so its asset type is the closest thing to one.
            return asset.GetType().Name;
        }

        internal static List<int> FindQualitySettingsToMigrate()
        {
            var qualityLevelsToMigrate = new List<int>();
            for (int i = 0, c = QualitySettings.names.Length; i < c; ++i)
            {
                if (ShouldMigrateRenderPipelineAsset(GetEffectiveRenderPipelineAssetAt(i)))
                    qualityLevelsToMigrate.Add(i);
            }

            return qualityLevelsToMigrate;
        }

        // Built-in (a null asset) and URP both match; any other SRP does not.
        internal static bool ShouldMigrateRenderPipelineAsset(RenderPipelineAsset asset)
        {
            // BiRP
            if (asset == null)
                return true;

            // URP; any other SRP is not a migration target.
            return IsAssetOfType(asset, k_UrpAssetTypeName);
        }

        // Matched by type name because no render pipeline package is referenced from this assembly.
        static bool IsAssetOfType(RenderPipelineAsset asset, string assetTypeName)
        {
            for (var type = asset.GetType(); type != null; type = type.BaseType)
            {
                if (type.FullName == assetTypeName)
                    return true;
            }

            return false;
        }
    }
}
