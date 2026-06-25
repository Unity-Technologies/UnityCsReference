// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Scripting.LifecycleManagement;
using UnityEditor.Experimental;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Lighting.LightingSearch
{
    class LightingSearchQueryTreeNodeHandler(string baseFolderPath, string queryTreeName)
        : ProjectSearchQueryTreeNodeHandler(isEditorResources: true,
            $"{SearchQueryTreeConfig.BuiltInQueriesFolder}/{baseFolderPath}",
            queryTreeName)
    {
        static string GetQueryDisplayName(ISearchQuery query)
        {
            return FormatEditorResourcesFileName(query.displayName);
        }

        static string GetFolderDisplayName(SearchQueryTreeViewItem item)
        {
            return FormatEditorResourcesFileName(item.Data.Name);
        }

        const string k_OnlyHdrpLabel = "OnlyHDRP";
        const string k_OnlyUrpBirpLabel = "OnlyURP&BIRP";
        const string k_OnlySrpLabel = "OnlySRP";
        const string k_OnlyApvLabel = "OnlyAPV";
        const string k_OnlyLightProbeGroupsLabel = "OnlyLightProbeGroups";

        // We assume that queries have only one label for our filtering purposes.
        // Note: Labels are stored in the description field because AssetDatabase.GetLabels() doesn't work on
        // assets loaded from the EditorResources bundle (bundle assets have no .meta files or AssetDatabase path).
        public override bool IsQueryVisible(string queryFilter, ISearchQuery query)
        {
            bool includeQuery = true;

            // The label is stored in the query's description field
            var label = query.details;
            if (!string.IsNullOrEmpty(label))
            {
                includeQuery = ShouldIncludeItemWithLabel(label);
            }

            return includeQuery && SearchQueryPanelTreeUtils.IsQueryNameMatchingFilter(queryFilter, query.displayName);
        }

        static readonly Texture2D s_FolderOpenedIcon = EditorGUIUtility.LoadIcon(EditorResources.openedFolderIconName);
        static readonly Texture2D s_QueryIcon = EditorGUIUtility.LoadIcon("UnityEditor/Search/SearchQueryAsset Icon");

        static readonly Texture2D s_AdaptiveProbeVolumeIcon = EditorGUIUtility.LoadIcon("ProbeVolume Icon");
        static readonly Texture2D s_ProbeVolumeBakingSetIcon = EditorGUIUtility.LoadIcon("ProbeVolumeBakingSet Icon");
        static readonly Texture2D s_LightProbesIcon = EditorGUIUtility.LoadIcon("LightProbes Icon");
        static readonly Texture2D s_LightingSettingsIcon = EditorGUIUtility.LoadIcon("LightingSettings Icon");
        static readonly Texture2D s_LightmapsIcon = EditorGUIUtility.LoadIcon("Lightmap Icon");
        static readonly Texture2D s_LightIcon = EditorGUIUtility.LoadIcon("Light Icon");
        static readonly Texture2D s_MaterialIcon = EditorGUIUtility.LoadIcon("Material Icon");
        static readonly Texture2D s_MeshRendererIcon = EditorGUIUtility.LoadIcon("MeshRenderer Icon");
        static readonly Texture2D s_PlanarReflectionProbeIcon = EditorGUIUtility.LoadIcon("PlanarReflections Icon");
        static readonly Texture2D s_ProbeAdjustmentVolumeIcon = EditorGUIUtility.LoadIcon("ProbeAdjustmentVolume Icon");
        static readonly Texture2D s_ReflectionProbeIcon = EditorGUIUtility.LoadIcon("ReflectionProbe Icon");
        static readonly Texture2D s_VolumeIcon = EditorGUIUtility.LoadIcon("Volume Icon");

        // Files read from Editor Resources will always be made lowercase, so we match using lowercase keys
        [NoAutoStaticsCleanup] // static lookup table; icon references are stable across code reload
        static readonly Dictionary<string, Texture2D> s_IconMapping = new()
        {
            { "adaptive probe volumes", s_AdaptiveProbeVolumeIcon },
            { "baking sets", s_ProbeVolumeBakingSetIcon },
            { "light probes", s_LightProbesIcon },
            { "lighting settings", s_LightingSettingsIcon },
            { "lightmaps", s_LightmapsIcon },
            { "lights", s_LightIcon },
            { "hdrp lights", s_LightIcon },
            { "emissive materials", s_MaterialIcon },
            { "hdrp emissive materials", s_MaterialIcon },
            { "mesh renderers", s_MeshRendererIcon },
            { "hdrp mesh renderers", s_MeshRendererIcon },
            { "planar reflection probes", s_PlanarReflectionProbeIcon },
            { "probe adjustment volumes", s_ProbeAdjustmentVolumeIcon },
            { "reflection probes", s_ReflectionProbeIcon },
            { "hdrp reflection probes", s_ReflectionProbeIcon },
            { "volumes", s_VolumeIcon }
        };

        public override void BindItemToQuery(TreeView tree, SearchQueryTreeViewItem item, ISearchQuery query)
        {
            if (query != null)
            {
                item.Bind(s_QueryIcon, GetQueryDisplayName(query), query.itemCount);
            }
            else
            {
                var displayName = GetFolderDisplayName(item);
                var icon = s_IconMapping.GetValueOrDefault(item.Data.Name, s_FolderOpenedIcon);
                item.Bind(icon, displayName);
            }
        }

        internal static bool IsAdaptiveProbeVolumesEnabled()
        {
            var currentRenderPipeline = GraphicsSettings.currentRenderPipeline;
            if (currentRenderPipeline is null)
                return false;

            var rpType = currentRenderPipeline.GetType();
            var lightProbeSystemValue = rpType.Name switch
            {
                LightingSearchWindow.k_URPAssetTypeName => GetUrpLightProbeSystem(rpType, currentRenderPipeline),
                LightingSearchWindow.k_HDRPAssetTypeName => GetHdrpLightProbeSystem(rpType, currentRenderPipeline),
                _ => null
            };

            // LightProbeGroups = 0, ProbeVolumes = 1
            if (lightProbeSystemValue is int intValue)
                return intValue == 1;

            if (lightProbeSystemValue is Enum enumValue)
                return Convert.ToInt32(enumValue) == 1;

            return false;
        }

        static object GetUrpLightProbeSystem(Type rpType, RenderPipelineAsset asset)
        {
            var property = rpType.GetProperty("lightProbeSystem",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            return property?.GetValue(asset);
        }

        static object GetHdrpLightProbeSystem(Type rpType, RenderPipelineAsset asset)
        {
            var settingsProp = rpType.GetProperty("currentPlatformRenderPipelineSettings",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            var settings = settingsProp?.GetValue(asset);
            if (settings == null)
                return null;

            var field = settings.GetType().GetField("lightProbeSystem",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(settings);
        }

        internal static bool ShouldIncludeItemWithLabel(string itemLabel)
        {
            RenderPipelineAsset currentRenderPipeline = GraphicsSettings.currentRenderPipeline;
            string srpName = currentRenderPipeline?.GetType().Name;

            switch (itemLabel)
            {
                case k_OnlyHdrpLabel when srpName != LightingSearchWindow.k_HDRPAssetTypeName:
                case k_OnlyUrpBirpLabel when srpName != null && srpName != LightingSearchWindow.k_URPAssetTypeName:
                case k_OnlySrpLabel when srpName == null: // Built-in RP
                    return false;
                case k_OnlyApvLabel:
                    return IsAdaptiveProbeVolumesEnabled();
                case k_OnlyLightProbeGroupsLabel:
                    return !IsAdaptiveProbeVolumesEnabled();
            }
            return true;
        }

        private static readonly Regex WordBoundaryRegex = new(@"\b\w+\b", RegexOptions.Compiled);
        public static string FormatEditorResourcesFileName(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;

            // Remove underscores used to escape decimal points in filenames
            var processed = s.Replace("._", ".");

            const string hdrpPrefixLower = "hdrp ";
            if (processed.StartsWith(hdrpPrefixLower, StringComparison.Ordinal))
                processed = processed[hdrpPrefixLower.Length..];

            // Input filenames from Editor Resources is all lowercase, so we capitalize the first letter of each word
            var formatted = WordBoundaryRegex.Replace(processed, match =>
            {
                var word = match.Value;

                return string.Create(word.Length, word, (span, w) =>
                {
                    span[0] = char.ToUpperInvariant(w[0]);
                    w.AsSpan(1).CopyTo(span[1..]);
                });
            });

            return formatted;
        }

    }
}

