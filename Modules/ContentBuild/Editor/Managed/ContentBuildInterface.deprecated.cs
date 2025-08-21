// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.Build.Content
{
    public static partial class ContentBuildInterface
    {
        [Obsolete("ContentBuildInterface.PrepareScene has been deprecated. Use ContentBuildInterface.CalculatePlayerDependenciesForScene instead", true)]
        public static SceneDependencyInfo PrepareScene(string scenePath, BuildSettings settings, BuildUsageTagSet usageSet, string outputFolder)
        {
            return CalculatePlayerDependenciesForScene(scenePath, settings, usageSet);
        }

        [Obsolete("ContentBuildInterface.PrepareScene has been deprecated. Use ContentBuildInterface.CalculatePlayerDependenciesForScene instead")]
        public static SceneDependencyInfo PrepareScene(string scenePath, BuildSettings settings, BuildUsageTagSet usageSet, BuildUsageCache usageCache, string outputFolder)
        {
            return CalculatePlayerDependenciesForScene(scenePath, settings, usageSet, usageCache);
        }

        [Obsolete("ContentBuildInterface.WriteSerializedFile has been deprecated. Use ContentBuildInterface.WriteSerializedFile instead", true)]
        public static WriteResult WriteSerializedFile(string outputFolder, WriteCommand writeCommand, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildReferenceMap referenceMap)
        {
            return WriteSerializedFile(outputFolder, new WriteParameters
            {
                writeCommand = writeCommand,
                settings = settings,
                globalUsage = globalUsage,
                usageSet = usageSet,
                referenceMap = referenceMap
            });
        }

        [Obsolete("ContentBuildInterface.WriteSerializedFile has been deprecated. Use ContentBuildInterface.WriteSerializedFile instead", true)]
        public static WriteResult WriteSerializedFile(string outputFolder, WriteCommand writeCommand, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildReferenceMap referenceMap, AssetBundleInfo bundleInfo)
        {
            return WriteSerializedFile(outputFolder, new WriteParameters
            {
                writeCommand = writeCommand,
                settings = settings,
                globalUsage = globalUsage,
                usageSet = usageSet,
                referenceMap = referenceMap,
                bundleInfo = bundleInfo
            });
        }

        [Obsolete("ContentBuildInterface.WriteSceneSerializedFile has been deprecated. Use ContentBuildInterface.WriteSceneSerializedFile instead", true)]
        public static WriteResult WriteSceneSerializedFile(string outputFolder, string scenePath, string processedScene, WriteCommand writeCommand, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildReferenceMap referenceMap)
        {
            return WriteSceneSerializedFile(outputFolder, new WriteSceneParameters
            {
                scenePath = scenePath,
                writeCommand = writeCommand,
                settings = settings,
                globalUsage = globalUsage,
                usageSet = usageSet,
                referenceMap = referenceMap
            });
        }

        [Obsolete("ContentBuildInterface.WriteSceneSerializedFile has been deprecated. Use ContentBuildInterface.WriteSceneSerializedFile with WriteParameters instead", true)]
        public static WriteResult WriteSceneSerializedFile(string outputFolder, string scenePath, string processedScene, WriteCommand writeCommand, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildReferenceMap referenceMap, PreloadInfo preloadInfo)
        {
            return WriteSceneSerializedFile(outputFolder, new WriteSceneParameters
            {
                scenePath = scenePath,
                writeCommand = writeCommand,
                settings = settings,
                globalUsage = globalUsage,
                usageSet = usageSet,
                referenceMap = referenceMap,
                preloadInfo = preloadInfo
            });
        }

        [Obsolete("ContentBuildInterface.WriteSceneSerializedFile has been deprecated. Use ContentBuildInterface.WriteSceneSerializedFile with WriteSceneParameters instead", true)]
        public static WriteResult WriteSceneSerializedFile(string outputFolder, string scenePath, string processedScene, WriteCommand writeCommand, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildReferenceMap referenceMap, PreloadInfo preloadInfo, SceneBundleInfo sceneBundleInfo)
        {
            return WriteSceneSerializedFile(outputFolder, new WriteSceneParameters
            {
                scenePath = scenePath,
                writeCommand = writeCommand,
                settings = settings,
                globalUsage = globalUsage,
                usageSet = usageSet,
                referenceMap = referenceMap,
                preloadInfo = preloadInfo,
                sceneBundleInfo = sceneBundleInfo
            });
        }
    }
}
