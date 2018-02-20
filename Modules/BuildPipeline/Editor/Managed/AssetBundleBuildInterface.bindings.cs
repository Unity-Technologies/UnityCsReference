// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor.Experimental.Build.Player;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Experimental.Build.AssetBundle
{
    public enum CompressionType
    {
        None,
        Lzma,
        Lz4,
        Lz4HC,
    }

    public enum CompressionLevel
    {
        None,
        Fastest,
        Fast,
        Normal,
        High,
        Maximum,
    }

    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct BuildCompression
    {
        public static readonly BuildCompression DefaultUncompressed = new BuildCompression
        {
            compression = CompressionType.None,
            level = CompressionLevel.Maximum,
            blockSize = 128 * 1024
        };

        public static readonly BuildCompression DefaultLZ4 = new BuildCompression
        {
            compression = CompressionType.Lz4HC,
            level = CompressionLevel.Maximum,
            blockSize = 128 * 1024
        };

        public static readonly BuildCompression DefaultLZMA = new BuildCompression
        {
            compression = CompressionType.Lzma,
            level = CompressionLevel.Maximum,
            blockSize = 128 * 1024
        };

        public CompressionType compression;
        public CompressionLevel level;
        public uint blockSize;
    }


    [NativeHeader("Modules/BuildPipeline/Editor/Public/AssetBundleBuildInterface.h")]

    [NativeHeader("Modules/BuildPipeline/Editor/Shared/AssetBundleBuildInterface.bindings.h")]
    public class BundleBuildInterface
    {
        [FreeFunction("BuildPipeline::GenerateBuildInput")]
        // DEPRECATED - We want to move AB info out of asset meta file into separate asset for all bundle info
        extern public static BuildInput GenerateBuildInput();

        [FreeFunction("BuildPipeline::PrepareScene")]
        extern public static SceneDependencyInfo PrepareScene(string scenePath, BuildSettings settings, BuildUsageTagSet usageSet, string outputFolder);

        [FreeFunction("BuildPipeline::GetPlayerObjectIdentifiersInAsset")]
        extern public static ObjectIdentifier[] GetPlayerObjectIdentifiersInAsset(GUID asset, BuildTarget target);

        [FreeFunction("BuildPipeline::GetPlayerDependenciesForObject")]
        extern public static ObjectIdentifier[] GetPlayerDependenciesForObject(ObjectIdentifier objectID, BuildTarget target, TypeDB typeDB);

        [FreeFunction("BuildPipeline::GetPlayerDependenciesForObjects")]
        extern public static ObjectIdentifier[] GetPlayerDependenciesForObjects(ObjectIdentifier[] objectIDs, BuildTarget target, TypeDB typeDB);

        [FreeFunction("BuildPipeline::CalculateBuildUsageTags")]
        extern public static void CalculateBuildUsageTags(ObjectIdentifier[] objectIDs, ObjectIdentifier[] dependentObjectIDs, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet);

        [FreeFunction("BuildPipeline::GetTypeForObject")]
        extern public static System.Type GetTypeForObject(ObjectIdentifier objectID);

        [FreeFunction("BuildPipeline::GetTypeForObjects")]
        extern public static System.Type[] GetTypeForObjects(ObjectIdentifier[] objectIDs);

        public static WriteResult WriteSerializedFile(string outputFolder, WriteCommand writeCommand, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildReferenceMap referenceMap)
        {
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
        extern public static uint ArchiveAndCompress(ResourceFile[] resourceFiles, string outputBundlePath, BuildCompression compression);
    }
}
