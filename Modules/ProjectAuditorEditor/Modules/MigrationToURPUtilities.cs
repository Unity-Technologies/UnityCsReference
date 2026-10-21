// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
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

        internal static class Contents
        {
            public static readonly string URP = L10n.Tr("Universal Render Pipeline", null);
            public static readonly string HDRP = L10n.Tr("High Definition Render Pipeline", null);
            public static readonly string BuiltIn = L10n.Tr("Built-in Render Pipeline", null);
            public static readonly string Mixed = L10n.Tr("Mixed", null);
        }

        /// <summary>
        /// Link to the documentation.
        /// </summary>
        public static string DocumentationUrl => "https://docs.unity3d.com/Manual/urp/upgrade-guides.html";

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

        internal static bool IsProjectFullyOnUrp(string targetPipeline)
        {
            var qualityLevelCount = QualitySettings.names.Length;
            var assetCreator = FindAssetCreator();

            // With no Quality Levels, Unity uses the project-wide asset.
            if (qualityLevelCount == 0)
                return AssetMatchesUrpTargetPipeline(assetCreator, GraphicsSettings.defaultRenderPipeline, targetPipeline);

            // Check quality levels.
            for (var i = 0; i < qualityLevelCount; ++i)
            {
                var asset = GetEffectiveRenderPipelineAssetAt(i);
                if (!AssetMatchesUrpTargetPipeline(assetCreator, asset, targetPipeline))
                    return false;
            }

            return true;
        }

        internal static bool IsProjectUsingSRP()
        {
            var qualityLevelCount = QualitySettings.names.Length;

            // With no Quality Levels, Unity uses the project-wide asset.
            if (qualityLevelCount == 0)
            {
                var rp = GraphicsSettings.defaultRenderPipeline;
                return rp != null;
            }

            // Check quality levels.
            for (var i = 0; i < qualityLevelCount; ++i)
            {
                var asset = GetEffectiveRenderPipelineAssetAt(i);
                if (asset != null)
                    return true;
            }

            return false;
        }

        internal static bool IsProjectUsingOtherSRP()
        {
            var qualityLevelCount = QualitySettings.names.Length;

            // With no Quality Levels, Unity uses the project-wide asset.
            if (qualityLevelCount == 0)
            {
                var rp = GraphicsSettings.defaultRenderPipeline;
                return rp != null && !IsAssetOfType(rp, k_UrpAssetTypeName);
            }

            // Check quality levels.
            for (var i = 0; i < qualityLevelCount; ++i)
            {
                var asset = GetEffectiveRenderPipelineAssetAt(i);
                if (asset != null && !IsAssetOfType(asset, k_UrpAssetTypeName))
                    return true;
            }

            return false;
        }

        internal static bool IsProjectUsingHDRP()
        {
            var qualityLevelCount = QualitySettings.names.Length;

            // With no Quality Levels, Unity uses the project-wide asset.
            if (qualityLevelCount == 0)
                return IsAssetOfType(GraphicsSettings.defaultRenderPipeline, k_HdrpAssetTypeName);

            // Check quality levels.
            for (var i = 0; i < qualityLevelCount; ++i)
            {
                var asset = GetEffectiveRenderPipelineAssetAt(i);
                if (IsAssetOfType(asset, k_HdrpAssetTypeName))
                    return true;
            }

            return false;
        }

        // Prefers an asset already in the project; creation is delegated via TypeCache to avoid referencing URP.
        internal static void ApplyTargetPipelineToQualitySettings(string targetPipeline)
        {
            var creator = FindAssetCreator();

            var matchingAsset = FindAssetMatchingTargetPipeline(creator, targetPipeline);
            if (matchingAsset != null)
            {
                ReplaceMismatchedQualityLevelAssets(creator, matchingAsset, targetPipeline);

                // Levels without an asset of their own render with the default, so it has to match too.
                if (!AssetMatchesUrpTargetPipeline(creator, GraphicsSettings.defaultRenderPipeline, targetPipeline))
                    GraphicsSettings.defaultRenderPipeline = matchingAsset;

                return;
            }

            // Null if no render-pipeline package with a creator is installed.
            var createdAsset = creator?.CreateAndAssignDefault(targetPipeline);
            if (createdAsset != null)
                ReplaceMismatchedQualityLevelAssets(creator, createdAsset, targetPipeline);
        }

        // The default if it matches, otherwise the first quality level's own asset that does.
        static RenderPipelineAsset FindAssetMatchingTargetPipeline(IRenderPipelineAssetCreator creator, string targetPipeline)
        {
            var defaultAsset = GraphicsSettings.defaultRenderPipeline;
            if (AssetMatchesUrpTargetPipeline(creator, defaultAsset, targetPipeline))
                return defaultAsset;

            for (int i = 0, c = QualitySettings.names.Length; i < c; ++i)
            {
                var asset = QualitySettings.GetRenderPipelineAssetAt(i);
                if (AssetMatchesUrpTargetPipeline(creator, asset, targetPipeline))
                    return asset;
            }

            return null;
        }

        // Assigns the replacement to every level whose own asset doesn't match the target pipeline. Levels with no
        // asset use the default, so are not replaced.
        static void ReplaceMismatchedQualityLevelAssets(IRenderPipelineAssetCreator creator, RenderPipelineAsset replacement, string targetPipeline)
        {
            var originalLevel = QualitySettings.GetQualityLevel();
            try
            {
                for (int i = 0, c = QualitySettings.names.Length; i < c; ++i)
                {
                    var asset = QualitySettings.GetRenderPipelineAssetAt(i);
                    if (asset == null || AssetMatchesUrpTargetPipeline(creator, asset, targetPipeline))
                        continue;

                    // There is no per-level setter, only one for the current level.
                    QualitySettings.SetQualityLevel(i, false);
                    QualitySettings.renderPipeline = replacement;
                }
            }
            finally
            {
                QualitySettings.SetQualityLevel(originalLevel, false);
            }
        }

        static IRenderPipelineAssetCreator FindAssetCreator()
        {
            foreach (var type in TypeCache.GetTypesDerivedFrom<IRenderPipelineAssetCreator>())
            {
                if (type.IsAbstract)
                    continue;

                try
                {
                    var creator = Activator.CreateInstance(type, nonPublic: true) as IRenderPipelineAssetCreator;       // Creators ship inside render-pipeline packages, so neither the type nor its constructor is guaranteed to be public.
                    if (creator != null)
                        return creator;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Could not create an instance of {type.FullName}: {e.Message}");
                }
            }

            return null;
        }

        internal static string GetCurrentRenderPipelineDisplayName()
        {
            var qualityLevelCount = QualitySettings.names.Length;

            // With no Quality Levels, Unity uses the project-wide asset.
            if (qualityLevelCount == 0)
                return GetRenderPipelineDisplayName(GraphicsSettings.defaultRenderPipeline);

            // Levels using different assets of the same pipeline are not mixed.
            var displayName = GetRenderPipelineDisplayName(GetEffectiveRenderPipelineAssetAt(0));
            for (var i = 1; i < qualityLevelCount; ++i)
            {
                if (GetRenderPipelineDisplayName(GetEffectiveRenderPipelineAssetAt(i)) != displayName)
                    return Contents.Mixed;
            }

            return displayName;
        }

        static string GetRenderPipelineDisplayName(RenderPipelineAsset asset)
        {
            if (asset == null)
                return Contents.BuiltIn;
            if (IsAssetOfType(asset, k_UrpAssetTypeName))
                return Contents.URP;
            if (IsAssetOfType(asset, k_HdrpAssetTypeName))
                return Contents.HDRP;
            return asset.GetType().Name;
        }

        // Whether every Quality Level (or the project-wide default, if there are no Quality Levels) already has a
        // Render Pipeline Asset matching the given target pipeline (eg every level uses the 2D Renderer, for
        // "BuiltInToURP2D"). Catches a level with its own override asset for the wrong renderer.
        internal static void AreQualitySettingsReadyForUrpTargetPipelines(string[] targetPipelines, out bool[] results)
        {
            var creator = FindAssetCreator();
            var qualityLevelCount = QualitySettings.names.Length;

            results = new bool[targetPipelines.Length];
            for (int j = 0; j < targetPipelines.Length; j++)
            {
                var targetPipeline = targetPipelines[j];

                if (qualityLevelCount == 0)
                {
                    results[j] = AssetMatchesUrpTargetPipeline(creator, GraphicsSettings.defaultRenderPipeline, targetPipeline);
                }
                else
                {
                    results[j] = true;
                    for (var i = 0; i < qualityLevelCount; ++i)
                    {
                        if (!AssetMatchesUrpTargetPipeline(creator, GetEffectiveRenderPipelineAssetAt(i), targetPipeline))
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
        static bool AssetMatchesUrpTargetPipeline(IRenderPipelineAssetCreator creator, RenderPipelineAsset asset, string targetPipeline)
        {
            if (!IsAssetOfType(asset, k_UrpAssetTypeName))
                return false;
            return creator == null || creator.MatchesTargetPipeline(asset, targetPipeline);
        }

        // Matched by type name because no render pipeline package is referenced from this assembly.
        static bool IsAssetOfType(RenderPipelineAsset asset, string assetTypeName)
        {
            if (asset == null)
                return false;

            for (var type = asset.GetType(); type != null; type = type.BaseType)
            {
                if (type.FullName == assetTypeName)
                    return true;
            }

            return false;
        }
    }
}
