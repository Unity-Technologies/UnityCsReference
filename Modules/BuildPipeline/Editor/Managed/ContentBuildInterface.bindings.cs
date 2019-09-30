// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Build.Player;
using UnityEngine.Bindings;

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("Unity.ScriptableBuildPipeline.Editor")]

namespace UnityEditor.Build.Content
{
    [NativeHeader("Modules/BuildPipeline/Editor/Public/ContentBuildTypes.h")]
    [NativeHeader("Modules/BuildPipeline/Editor/Shared/ContentBuildInterface.bindings.h")]
    [StaticAccessor("BuildPipeline", StaticAccessorType.DoubleColon)]
    public static partial class ContentBuildInterface
    {
        public static extern AssetBundleBuild[] GenerateAssetBundleBuilds();

        public static extern BuildUsageTagGlobal GetGlobalUsageFromGraphicsSettings();

        public static SceneDependencyInfo CalculatePlayerDependenciesForScene(string scenePath, BuildSettings settings, BuildUsageTagSet usageSet)
        {
            if (IsBuildInProgress())
                throw new InvalidOperationException("Cannot call CalculatePlayerDependenciesForScene while a build is in progress");
            return CalculatePlayerDependenciesForSceneInternal(scenePath, settings, usageSet, null);
        }

        public static SceneDependencyInfo CalculatePlayerDependenciesForScene(string scenePath, BuildSettings settings, BuildUsageTagSet usageSet, BuildUsageCache usageCache)
        {
            if (IsBuildInProgress())
                throw new InvalidOperationException("Cannot call CalculatePlayerDependenciesForScene while a build is in progress");
            return CalculatePlayerDependenciesForSceneInternal(scenePath, settings, usageSet, usageCache);
        }

        [FreeFunction("CalculatePlayerDependenciesForScene")]
        static extern SceneDependencyInfo CalculatePlayerDependenciesForSceneInternal(string scenePath, BuildSettings settings, BuildUsageTagSet usageSet, BuildUsageCache usageCache);

        public static extern ObjectIdentifier[] GetPlayerObjectIdentifiersInAsset(GUID asset, BuildTarget target);

        internal static extern ObjectIdentifier[] GetPlayerObjectIdentifiersInSerializedFile(string filePath, BuildTarget target);

        public static extern ObjectIdentifier[] GetPlayerDependenciesForObject(ObjectIdentifier objectID, BuildTarget target, TypeDB typeDB);

        public static extern ObjectIdentifier[] GetPlayerDependenciesForObjects(ObjectIdentifier[] objectIDs, BuildTarget target, TypeDB typeDB);

        public static void CalculateBuildUsageTags(ObjectIdentifier[] objectIDs, ObjectIdentifier[] dependentObjectIDs, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet)
        {
            CalculateBuildUsageTags(objectIDs, dependentObjectIDs, globalUsage, usageSet, null);
        }

        public static extern void CalculateBuildUsageTags(ObjectIdentifier[] objectIDs, ObjectIdentifier[] dependentObjectIDs, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildUsageCache usageCache);

        public static extern System.Type GetTypeForObject(ObjectIdentifier objectID);

        public static extern System.Type[] GetTypeForObjects(ObjectIdentifier[] objectIDs);

        internal static extern bool IsBuildInProgress();

        public static WriteResult WriteSerializedFile(string outputFolder, WriteParameters parameters)
        {
            if (IsBuildInProgress())
                throw new InvalidOperationException("Cannot call WriteSerializedFile while a build is in progress");
            if (string.IsNullOrEmpty(outputFolder))
                throw new ArgumentException("String is null or empty.", "outputFolder");
            if (parameters.writeCommand == null)
                throw new ArgumentNullException("parameters.writeCommand");
            if (parameters.referenceMap == null)
                throw new ArgumentNullException("parameters.referenceMap");

            if (parameters.bundleInfo == null)
                return WriteSerializedFileRaw(outputFolder, parameters.writeCommand, parameters.settings, parameters.globalUsage, parameters.usageSet, parameters.referenceMap);
            return WriteSerializedFileAssetBundle(outputFolder, parameters.writeCommand, parameters.settings, parameters.globalUsage, parameters.usageSet, parameters.referenceMap, parameters.bundleInfo);
        }

        static extern WriteResult WriteSerializedFileRaw(string outputFolder, WriteCommand writeCommand, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildReferenceMap referenceMap);

        static extern WriteResult WriteSerializedFileAssetBundle(string outputFolder, WriteCommand writeCommand, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildReferenceMap referenceMap, AssetBundleInfo bundleInfo);

        public static WriteResult WriteSceneSerializedFile(string outputFolder, WriteSceneParameters parameters)
        {
            if (IsBuildInProgress())
                throw new InvalidOperationException("Cannot call WriteSceneSerializedFile while a build is in progress");
            if (string.IsNullOrEmpty(outputFolder))
                throw new ArgumentException("String is null or empty.", "outputFolder");
            if (parameters.writeCommand == null)
                throw new ArgumentNullException("parameters.writeCommand");
            if (parameters.referenceMap == null)
                throw new ArgumentNullException("parameters.referenceMap");

            if (parameters.preloadInfo == null && parameters.sceneBundleInfo == null)
                return WriteSceneSerializedFileRaw(outputFolder, parameters.scenePath, parameters.writeCommand, parameters.settings, parameters.globalUsage, parameters.usageSet, parameters.referenceMap);
            if (parameters.sceneBundleInfo == null)
                return WriteSceneSerializedFilePlayerData(outputFolder, parameters.scenePath, parameters.writeCommand, parameters.settings, parameters.globalUsage, parameters.usageSet, parameters.referenceMap, parameters.preloadInfo);
            return WriteSceneSerializedFileAssetBundle(outputFolder, parameters.scenePath, parameters.writeCommand, parameters.settings, parameters.globalUsage, parameters.usageSet, parameters.referenceMap, parameters.preloadInfo, parameters.sceneBundleInfo);
        }

        static extern WriteResult WriteSceneSerializedFileRaw(string outputFolder, string scenePath, WriteCommand writeCommand, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildReferenceMap referenceMap);

        static extern WriteResult WriteSceneSerializedFilePlayerData(string outputFolder, string scenePath, WriteCommand writeCommand, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildReferenceMap referenceMap, PreloadInfo preloadInfo);

        static extern WriteResult WriteSceneSerializedFileAssetBundle(string outputFolder, string scenePath, WriteCommand writeCommand, BuildSettings settings, BuildUsageTagGlobal globalUsage, BuildUsageTagSet usageSet, BuildReferenceMap referenceMap, PreloadInfo preloadInfo, SceneBundleInfo sceneBundleInfo);

        public static uint ArchiveAndCompress(ResourceFile[] resourceFiles, string outputBundlePath,
            UnityEngine.BuildCompression compression)
        {
            return ArchiveAndCompress(resourceFiles, outputBundlePath, compression, false);
        }

        //modified to be thread safe - if called from a non-main thread, there are no dialogs presented in the case of an error.
        [ThreadSafe]
        public static extern uint ArchiveAndCompress(ResourceFile[] resourceFiles, string outputBundlePath, UnityEngine.BuildCompression compression, bool stripUnityVersion);
    }
}
