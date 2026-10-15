// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
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

        // Render Pipeline Converter container names, matching the [BatchModeConverterClassInfo] attributes in
        // com.unity.render-pipelines.universal.
        internal const string Urp3DContainer = "BuiltInToURP";
        internal const string Urp2DContainer = "BuiltInToURP2D";

        internal static string DefaultTargetPipelineContainer => Urp3DContainer;

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

        // Runs the same converters as the Render Pipeline Converter window, via the API its own command line entry
        // point uses, so a project can be converted without a window ever being opened. onCompleted always fires
        // exactly once, even if the batch mode API could not be found or invoking it threw.
        internal static bool RunRenderPipelineConverterInBatchMode(string containerName, Action<string> onCompleted)
        {
            var convertersType = Type.GetType(MigrationToURPModule.k_ConvertersTypeName);
            if (convertersType == null)
            {
                onCompleted?.Invoke(string.Empty);
                return false;
            }

            var runInBatchModeMethod = convertersType.GetMethod("RunInBatchModeWithCallback",
                BindingFlags.Static | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(Action<string>) }, null);
            if (runInBatchModeMethod == null)
            {
                Debug.LogWarning($"[{ProjectAuditor.DisplayName}] Could not find the Render Pipeline Converter batch mode API on {convertersType.FullName}.");
                onCompleted?.Invoke(string.Empty);
                return false;
            }

            try
            {
                runInBatchModeMethod.Invoke(null, [containerName, onCompleted]);
                return true;
            }
            catch (Exception e)
            {
                var reason = e is TargetInvocationException invocationException ? invocationException.InnerException : e;
                Debug.LogError($"[{ProjectAuditor.DisplayName}] The Render Pipeline Converter failed to run in batch mode: {reason.Message}\n{reason}");
                onCompleted?.Invoke(string.Empty);
                return false;
            }
        }

        // One converted item, parsed from a line of RunRenderPipelineConverterInBatchMode's output (see Converters.BatchConverters).
        internal readonly struct ConversionResultItem
        {
            public readonly bool Succeeded;
            public readonly string ConverterType;
            public readonly string Name;
            public readonly string Info;
            public readonly string Message;

            public ConversionResultItem(bool succeeded, string converterType, string name, string info, string message)
            {
                Succeeded = succeeded;
                ConverterType = converterType;
                Name = name;
                Info = info;
                Message = message;
            }
        }

        internal static IEnumerable<ConversionResultItem> ParseConversionResults(string output)
        {
            if (string.IsNullOrEmpty(output))
                yield break;

            foreach (var rawLine in output.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r');
                if (!line.StartsWith("- "))
                    continue;

                var fields = line.Substring(2).Split('\t');
                if (fields.Length < 4)
                    continue;

                yield return new ConversionResultItem(
                    fields[0] == "Success",
                    fields[1], fields[2], fields[3],
                    fields.Length > 4 ? fields[4] : string.Empty);
            }
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

        // Neither the wizard nor the Render Pipeline Converter support migrating from HDRP or custom SRPs to URP.
        // Only a null asset reads as in scope, so null is unambiguously "no unsupported pipeline in use".
        static RenderPipelineAsset GetUnsupportedRenderPipelineAsset()
        {
            // The wizard writes the project-wide default, so it counts even when every quality level overrides it.
            // This covers the no-Quality-Levels case too, where the default is all there is.
            var defaultAsset = GraphicsSettings.defaultRenderPipeline;
            if (!ShouldMigrateRenderPipelineAsset(defaultAsset))
                return defaultAsset;

            for (int i = 0, c = QualitySettings.names.Length; i < c; ++i)
            {
                var asset = GetEffectiveRenderPipelineAssetAt(i);
                if (!ShouldMigrateRenderPipelineAsset(asset))
                    return asset;
            }

            return null;
        }

        internal static bool IsProjectUsingOtherSRP()
        {
            return GetUnsupportedRenderPipelineAsset() != null;
        }

        internal static bool IsProjectUsingHDRP()
        {
            var asset = GetUnsupportedRenderPipelineAsset();
            return asset != null && IsAssetOfType(asset, k_HdrpAssetTypeName);
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

        // Whether the project's default render pipeline asset is already set up for the given target pipeline
        // (eg has a 2D Renderer assigned, for "BuiltInToURP2D").
        internal static bool DefaultRenderPipelineMatchesTargetPipeline(string targetPipeline)
        {
            return AssetMatchesTargetPipeline(FindAssetCreator(), GraphicsSettings.defaultRenderPipeline, targetPipeline);
        }

        // Prefers an asset the Converter already assigned; creation is delegated via TypeCache to avoid referencing URP.
        internal static RenderPipelineAsset EnsureDefaultRenderPipelineAsset(string targetPipeline)
        {
            var creator = FindAssetCreator();

            // Already done: re-promoting a quality asset over an existing URP default would only change its settings.
            if (AssetMatchesTargetPipeline(creator, GraphicsSettings.defaultRenderPipeline, targetPipeline))
                return GraphicsSettings.defaultRenderPipeline;

            // Checked (rather than calling AssignCurrentQualityUrpAssetAsDefault outright) so a quality-level asset
            // for the wrong renderer isn't promoted over a default that's merely missing, when creating a fresh
            // one for the right renderer is possible instead.
            var qualityAsset = GetCurrentQualityUrpAsset();
            if (AssetMatchesTargetPipeline(creator, qualityAsset, targetPipeline))
            {
                GraphicsSettings.defaultRenderPipeline = qualityAsset;
                return qualityAsset;
            }

            return creator?.CreateAndAssignDefault(targetPipeline); // null if no render-pipeline package with a creator is installed.
        }

        static IRenderPipelineAssetCreator FindAssetCreator()
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<IRenderPipelineAssetCreator>())
            {
                if (type.IsAbstract)
                    continue;

                var creator = TryCreateAssetCreator(type);
                if (creator != null)
                    return creator;
            }

            return null;
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

        // Whether every Quality Level (or the project-wide default, if there are no Quality Levels) already has a
        // Render Pipeline Asset matching the given target pipeline (eg every level uses the 2D Renderer, for
        // "BuiltInToURP2D"). Catches a level with its own override asset for the wrong renderer, which the
        // project default alone (DefaultRenderPipelineMatchesTargetPipeline) wouldn't see.
        internal static void AreQualitySettingsReadyForTargetPipelines(string[] targetPipelines, out bool[] results)
        {
            var creator = FindAssetCreator();
            var qualityLevelCount = QualitySettings.names.Length;

            results = new bool[targetPipelines.Length];
            for (int j = 0; j < targetPipelines.Length; j++)
            {
                var targetPipeline = targetPipelines[j];

                if (qualityLevelCount == 0)
                {
                    results[j] = AssetMatchesTargetPipeline(creator, GraphicsSettings.defaultRenderPipeline, targetPipeline);
                }
                else
                {
                    results[j] = true;
                    for (var i = 0; i < qualityLevelCount; ++i)
                    {
                        if (!AssetMatchesTargetPipeline(creator, GetEffectiveRenderPipelineAssetAt(i), targetPipeline))
                        {
                            results[j] = false;
                            break;
                        }
                    }
                }
            }
        }

        // Null or non-URP hasn't migrated at all. A URP asset with no creator available to confirm the specific
        // renderer is accepted, matching how the rest of the wizard treats a missing creator.
        static bool AssetMatchesTargetPipeline(IRenderPipelineAssetCreator creator, RenderPipelineAsset asset, string targetPipeline)
        {
            if (asset == null || !IsAssetOfType(asset, k_UrpAssetTypeName))
                return false;

            return creator == null || creator.MatchesTargetPipeline(asset, targetPipeline);
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
