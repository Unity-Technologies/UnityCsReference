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
    public class BundleBuildInterface
    {
        [FreeFunction("BuildPipeline::GenerateBuildInput")]
        // DEPRECATED - We want to move AB info out of asset meta file into separate asset for all bundle info
        extern public static BuildInput GenerateBuildInput();

        [FreeFunction("BuildPipeline::PrepareScene")]
        extern private static SceneLoadInfo PrepareSceneInternal(string scenePath, BuildSettings settings, string outputFolder);

        [FreeFunction("BuildPipeline::GetPlayerObjectIdentifiersInAsset")]
        extern public static ObjectIdentifier[] GetPlayerObjectIdentifiersInAsset(GUID asset, BuildTarget target);

        [FreeFunction("BuildPipeline::GetPlayerDependenciesForObject")]
        extern public static ObjectIdentifier[] GetPlayerDependenciesForObject(ObjectIdentifier objectID, BuildTarget target, TypeDB typeDB);

        [FreeFunction("BuildPipeline::GetPlayerDependenciesForObjects")]
        extern public static ObjectIdentifier[] GetPlayerDependenciesForObjects(ObjectIdentifier[] objectIDs, BuildTarget target, TypeDB typeDB);

        [FreeFunction("BuildPipeline::GetTypeForObject")]
        extern public static System.Type GetTypeForObject(ObjectIdentifier objectID);

        [FreeFunction("BuildPipeline::GetTypeForObjects")]
        extern public static System.Type[] GetTypeForObjects(ObjectIdentifier[] objectIDs);

        [FreeFunction("BuildPipeline::IsBuildInProgress")]
        extern internal static bool IsBuildInProgress();

        public static SceneLoadInfo PrepareScene(string scenePath, BuildSettings settings, string outputFolder)
        {
            if (IsBuildInProgress())
                throw new InvalidOperationException("Cannot call PrepareScene while a build is in progress");
            return PrepareSceneInternal(scenePath, settings, outputFolder);
        }

        [FreeFunction("BuildPipeline::WriteResourceFilesForBundle")]
        extern public static BuildOutput WriteResourceFilesForBundle(BuildCommandSet commands, string bundleName, BuildSettings settings, string outputFolder);

        [FreeFunction("BuildPipeline::WriteResourceFilesForBundles")]
        extern public static BuildOutput WriteResourceFilesForBundles(BuildCommandSet commands, string[] bundleNames, BuildSettings settings, string outputFolder);

        [FreeFunction("BuildPipeline::WriteAllResourceFiles")]
        extern public static BuildOutput WriteAllResourceFiles(BuildCommandSet commands, BuildSettings settings, string outputFolder);

        [FreeFunction("BuildPipeline::ArchiveAndCompress")]
        extern public static uint ArchiveAndCompress(ResourceFile[] resourceFiles, string outputBundlePath, BuildCompression compression);
    }
}
