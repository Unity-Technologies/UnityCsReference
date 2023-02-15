// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

[assembly: InternalsVisibleTo("Assembly-CSharp-Editor-testable")]

namespace UnityEditor.ShaderKeywordFilter
{
    // SettingsVariant represents a leaf node in the settings tree.
    // The keyword arrays there tell which keywords need to be selected and which removed.
    // This data is used in the native code to prune the enumeration data, thus reducing
    // the size of enumerated variant space.
    [RequiredByNativeCode (GenerateProxy = true)]
    [StructLayout(LayoutKind.Sequential)]
    internal struct SettingsVariant
    {
        internal SettingsVariant(List<FilterRule> rules)
        {
            var selected = new List<string>();
            var selectedMessages = new List<string>();
            var emptyKwSelectors = new List<int>();
            var removed = new List<string>();
            var removedMessages = new List<string>();
            var emptyKwRemovers = new List<int>();

            foreach (var rule in rules)
            {
                if (rule.action == FilterAction.Select)
                {
                    selected.Add(rule.keywordName);
                    if(rule.withEmptyKeyword)
                        emptyKwSelectors.Add(selected.Count - 1);

                    selectedMessages.Add(rule.resolutionMessage);
                }
                else if (rule.action == FilterAction.Remove)
                {
                    removed.Add(rule.keywordName);
                    if (rule.withEmptyKeyword)
                        emptyKwRemovers.Add(removed.Count - 1);

                    removedMessages.Add(rule.resolutionMessage);
                }
            }

            selectKeywords = selected.ToArray();
            removeKeywords = removed.ToArray();
            emptyKeywordSelectingKeywords = emptyKwSelectors.ToArray();
            emptyKeywordRemovingKeywords = emptyKwRemovers.ToArray();
            selectReasons = selectedMessages.ToArray();
            removeReasons = removedMessages.ToArray();
        }

        readonly internal string[] selectKeywords;  // Keywords that have resolved to be selected in the build by a filter rule
        readonly internal string[] selectReasons;   // Human readable message on why the keyword was selected
        readonly internal int[] emptyKeywordSelectingKeywords; // Indices of keywords that will also retain "empty keyword" case

        readonly internal string[] removeKeywords;  // Keywords that have resolved to be removed from the build by a filter rule
        readonly internal string[] removeReasons;   // Human readable message on why the keyword was removed
        readonly internal int[] emptyKeywordRemovingKeywords; // Indices of keywords that will also remove "empty keyword" case
    }

    // This struct is for passing the constraint state of the currently compiled shader pass from C++ to C#.
    // It is used for comparing against the filter attribute constraints and this way
    // we can select only the filter rules that are meeting the constraint requirements.
    [RequiredByNativeCode (GenerateProxy = true)]
    internal struct ConstraintState
    {
        internal string[] tags;
        internal GraphicsDeviceType[] graphicsAPIs;
    }

    // This is the main class for shader keyword filtering C++/C# interop
    [RequiredByNativeCode]
    internal static class ShaderKeywordFilterUtil
    {
        internal struct CachedFilterData
        {
            public Hash128 dependencyHash;
            public SettingsNode settingsNode;
        };

        // In memory cache for filter data per renderpipeline asset.
        // This is to avoid redundant attribute search for each shader/pass/stage.
        internal static Dictionary<string, CachedFilterData> PerAssetFilterDataCache = new Dictionary<string, CachedFilterData>();

        internal static SettingsNode GetFilterDataCached(string nodeName, UnityEngine.Object containerObject)
        {
            string assetPath = AssetDatabase.GetAssetPath(containerObject);
            Hash128 dependencyHash = AssetDatabase.GetAssetDependencyHash(assetPath);

            CachedFilterData cachedData;
            if (PerAssetFilterDataCache.TryGetValue(assetPath, out cachedData))
            {
                // Cached data is valid only if dependency hash hasn't changed
                if (cachedData.dependencyHash == dependencyHash)
                    return cachedData.settingsNode;
            }

            // No valid data found in the cache so we need to do the full processing
            // and then enter the result into the cache.
            var visited = new HashSet<object>();
            cachedData.dependencyHash = dependencyHash;
            cachedData.settingsNode = SettingsNode.GatherFilterData(nodeName, containerObject, visited);

            PerAssetFilterDataCache[assetPath] = cachedData;

            return cachedData.settingsNode;
        }

        // For the current build target with given constraint state, gets the list of active filter rule sets.
        [RequiredByNativeCode]
        internal static SettingsVariant[] GetKeywordFilterVariants(string buildTargetGroupName, ConstraintState constraintState)
        {
            var rpAssets = new List<RenderPipelineAsset>();
            QualitySettings.GetAllRenderPipelineAssetsForPlatform(buildTargetGroupName, ref rpAssets);

            // Gather the settings/attribute tree from the renderpipe assets
            SettingsNode root = new SettingsNode("root");
            foreach(var rpAsset in rpAssets)
            {
                if (rpAsset == null)
                    continue;

                var node = GetFilterDataCached(rpAsset.name, rpAsset);
                if (node != null)
                    root.Children.Add(node);
            }

            // Extract the filter rules for each leaf node in the settings tree and return that in an array form.
            var variants = new List<SettingsVariant>();
            root.GetVariantArray(constraintState, variants);

            // We need to return at least a blank settings variant, even if there were no rules
            if (variants.Count == 0)
            {
                variants.Add(new SettingsVariant(new List<FilterRule>()));
            }

            return variants.ToArray();
        }
    }
}
