// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor.Build.Player;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Build.Content
{
    [NativeHeader("Modules/BuildPipeline/Editor/Public/ContentBuildTypes.h")]

    [NativeHeader("Modules/BuildPipeline/Editor/Shared/ContentBuildInterface.bindings.h")]

    [NativeHeader("Modules/BuildPipeline/Editor/Shared/ContentBuildInterface.bindings.h")]
    public static class ContentBuildInterface
    {
        [FreeFunction("BuildPipeline::GenerateAssetBundleBuilds")]
        extern public static AssetBundleBuild[] GenerateAssetBundleBuilds();

        public static SceneDependencyInfo PrepareScene(string scenePath, BuildSettings settings, BuildUsageTagSet usageSet, string outputFolder)
        {
            if (IsBuildInProgress())
                throw new InvalidOperationException("Cannot call PrepareScene while a build is in progress");
            return PrepareSceneInternal(scenePath, settings, usageSet, null, outputFolder);
        }

        public static SceneDependencyInfo PrepareScene(string scenePath, BuildSettings settings, BuildUsageTagSet usageSet, BuildUsageCache usageCache, string outputFolder)
        {
            if (IsBuildInProgress())
                throw new InvalidOperationException("Cannot call PrepareScene while a build is in progress");
            return PrepareSceneInternal(scenePath, settings, usageSet, usageCache, outputFolder);
        }

        [FreeFunction("BuildPipeline::PrepareScene")]
        extern private static SceneDependencyInfo PrepareSceneInternal(string scenePath, BuildSettings settings, BuildUsageTagSet usageSet, BuildUsageCache usageCache, string outputFolder);

        [FreeFunction("BuildPipeline::GetPlayerObjectIdentifiersInAsset")]
        extern public static ObjectIdentifier[] GetPlayerObjectIdentifiersInAsset(GUID asset, BuildTarget target);

        [FreeFunction("BuildPipeline::GetPlayerDependenciesForObject")]
        extern public static ObjectIdentifier[] GetPlayerDependenciesForObject(ObjectIdentifier objectID, BuildTarget target, TypeDB typeDB);

        [FreeFunction("BuildPipeline::GetPlayerDependenciesForObjects")]
        extern public static ObjectIdentifier[] GetPlayerDependenciesForObjects(ObjectIdentifier[] objectIDs, BuildTarget target, TypeDB typeDB);

        public static void CalculateBuildUsageTags(ObjectIdentifier[] objectIDs, ObjectIdentifier[] dependentObjectIDs, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet)
        {
            CalculateBuildUsageTags(objectIDs, dependentObjectIDs, globalUsage, usageSet, null);
        }

        [FreeFunction("BuildPipeline::CalculateBuildUsageTags")]
        extern public static void CalculateBuildUsageTags(ObjectIdentifier[] objectIDs, ObjectIdentifier[] dependentObjectIDs, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildUsageCache usageCache);

        [FreeFunction("BuildPipeline::GetTypeForObject")]
        extern public static System.Type GetTypeForObject(ObjectIdentifier objectID);

        [FreeFunction("BuildPipeline::GetTypeForObjects")]
        extern public static System.Type[] GetTypeForObjects(ObjectIdentifier[] objectIDs);

        [FreeFunction("BuildPipeline::IsBuildInProgress")]
        extern internal static bool IsBuildInProgress();

        public static WriteResult WriteSerializedFile(string outputFolder, WriteCommand writeCommand, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildReferenceMap referenceMap)
        {
            if (IsBuildInProgress())
                throw new InvalidOperationException("Cannot call WriteSerializedFile while a build is in progress");
            if (string.IsNullOrEmpty(outputFolder))
                throw new ArgumentException("String is null or empty.", "outputFolder");
            if (writeCommand == null)
                throw new ArgumentNullException("writeCommand");
            if (referenceMap == null)
                throw new ArgumentNullException("referenceMap");
            return WriteSerializedFileRaw(outputFolder, writeCommand, settings, globalUsage, usageSet, referenceMap);
        }

        public static WriteResult WriteSerializedFile(string outputFolder, WriteCommand writeCommand, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildReferenceMap referenceMap, AssetBundleInfo bundleInfo)
        {
            if (IsBuildInProgress())
                throw new InvalidOperationException("Cannot call WriteSerializedFile while a build is in progress");
            if (string.IsNullOrEmpty(outputFolder))
                throw new ArgumentException("String is null or empty.", "outputFolder");
            if (writeCommand == null)
                throw new ArgumentNullException("writeCommand");
            if (referenceMap == null)
                throw new ArgumentNullException("referenceMap");
            if (bundleInfo == null)
                throw new ArgumentNullException("bundleInfo");
            return WriteSerializedFileAssetBundle(outputFolder, writeCommand, settings, globalUsage, usageSet, referenceMap, bundleInfo);
        }

        [FreeFunction("BuildPipeline::WriteSerializedFileRaw")]
        extern private static WriteResult WriteSerializedFileRaw(string outputFolder, WriteCommand writeCommand, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildReferenceMap referenceMap);

        [FreeFunction("BuildPipeline::WriteSerializedFileAssetBundle")]
        extern private static WriteResult WriteSerializedFileAssetBundle(string outputFolder, WriteCommand writeCommand, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildReferenceMap referenceMap, AssetBundleInfo bundleInfo);

        public static WriteResult WriteSceneSerializedFile(string outputFolder, string scenePath, string processedScene, WriteCommand writeCommand, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildReferenceMap referenceMap)
        {
            if (IsBuildInProgress())
                throw new InvalidOperationException("Cannot call WriteSceneSerializedFile while a build is in progress");
            if (string.IsNullOrEmpty(outputFolder))
                throw new ArgumentException("String is null or empty.", "outputFolder");
            if (string.IsNullOrEmpty(scenePath))
                throw new ArgumentException("String is null or empty.", "scenePath");
            if (string.IsNullOrEmpty(processedScene))
                throw new ArgumentException("String is null or empty.", "processedScene");
            if (!File.Exists(processedScene))
                throw new ArgumentException(string.Format("File '{0}' does not exist.", processedScene), "processedScene");
            if (writeCommand == null)
                throw new ArgumentNullException("writeCommand");
            if (referenceMap == null)
                throw new ArgumentNullException("referenceMap");
            return WriteSceneSerializedFileRaw(outputFolder, scenePath, processedScene, writeCommand, settings, globalUsage, usageSet, referenceMap);
        }

        public static WriteResult WriteSceneSerializedFile(string outputFolder, string scenePath, string processedScene, WriteCommand writeCommand, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildReferenceMap referenceMap, PreloadInfo preloadInfo)
        {
            if (IsBuildInProgress())
                throw new InvalidOperationException("Cannot call WriteSceneSerializedFile while a build is in progress");
            if (string.IsNullOrEmpty(outputFolder))
                throw new ArgumentException("String is null or empty.", "outputFolder");
            if (string.IsNullOrEmpty(scenePath))
                throw new ArgumentException("String is null or empty.", "scenePath");
            if (string.IsNullOrEmpty(processedScene))
                throw new ArgumentException("String is null or empty.", "processedScene");
            if (!File.Exists(processedScene))
                throw new ArgumentException(string.Format("File '{0}' does not exist.", processedScene), "processedScene");
            if (writeCommand == null)
                throw new ArgumentNullException("writeCommand");
            if (referenceMap == null)
                throw new ArgumentNullException("referenceMap");
            if (preloadInfo == null)
                throw new ArgumentNullException("preloadInfo");
            return WriteSceneSerializedFilePlayerData(outputFolder, scenePath, processedScene, writeCommand, settings, globalUsage, usageSet, referenceMap, preloadInfo);
        }

        public static WriteResult WriteSceneSerializedFile(string outputFolder, string scenePath, string processedScene, WriteCommand writeCommand, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildReferenceMap referenceMap, PreloadInfo preloadInfo, SceneBundleInfo sceneBundleInfo)
        {
            if (IsBuildInProgress())
                throw new InvalidOperationException("Cannot call WriteSceneSerializedFile while a build is in progress");
            if (string.IsNullOrEmpty(outputFolder))
                throw new ArgumentException("String is null or empty.", "outputFolder");
            if (string.IsNullOrEmpty(scenePath))
                throw new ArgumentException("String is null or empty.", "scenePath");
            if (string.IsNullOrEmpty(processedScene))
                throw new ArgumentException("String is null or empty.", "processedScene");
            if (!File.Exists(processedScene))
                throw new ArgumentException(string.Format("File '{0}' does not exist.", processedScene), "processedScene");
            if (writeCommand == null)
                throw new ArgumentNullException("writeCommand");
            if (referenceMap == null)
                throw new ArgumentNullException("referenceMap");
            if (preloadInfo == null)
                throw new ArgumentNullException("preloadInfo");
            if (sceneBundleInfo == null)
                throw new ArgumentNullException("sceneBundleInfo");
            return WriteSceneSerializedFileAssetBundle(outputFolder, scenePath, processedScene, writeCommand, settings, globalUsage, usageSet, referenceMap, preloadInfo, sceneBundleInfo);
        }

        [FreeFunction("BuildPipeline::WriteSceneSerializedFileRaw")]
        extern private static WriteResult WriteSceneSerializedFileRaw(string outputFolder, string scenePath, string processedScene, WriteCommand writeCommand, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildReferenceMap referenceMap);

        [FreeFunction("BuildPipeline::WriteSceneSerializedFilePlayerData")]
        extern private static WriteResult WriteSceneSerializedFilePlayerData(string outputFolder, string scenePath, string processedScene, WriteCommand writeCommand, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildReferenceMap referenceMap, PreloadInfo preloadInfo);

        [FreeFunction("BuildPipeline::WriteSceneSerializedFileAssetBundle")]
        extern private static WriteResult WriteSceneSerializedFileAssetBundle(string outputFolder, string scenePath, string processedScene, WriteCommand writeCommand, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildReferenceMap referenceMap, PreloadInfo preloadInfo, SceneBundleInfo sceneBundleInfo);

        [FreeFunction("BuildPipeline::ArchiveAndCompress")]
        extern public static uint ArchiveAndCompress(ResourceFile[] resourceFiles, string outputBundlePath, UnityEngine.BuildCompression compression);
    }
}
