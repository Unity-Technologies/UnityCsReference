// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Bindings;

// Typically UnifiedBuildPipeline (UCBP) is accessed via existing Content Build Apis, e.g. BuildPipeline.BuildAssetBundles
// This API exposes additional functionality, e.g. for testing purposes

[assembly: InternalsVisibleTo("BuildPipelineTestUtilities")]

namespace UnityEditor.Build.Content
{
    [NativeHeader("Modules/BuildPipeline/Editor/Public/UnifiedBuildPipelineTestUtilities.h")]
    [NativeHeader("Modules/BuildPipeline/Editor/Ucbp/BuildPipelineCustomDependencies.h")]
    [StaticAccessor("BuildPipelineTestUtilities", StaticAccessorType.DoubleColon)]
    internal static partial class UnifiedBuildPipelineInternalApi
    {
        // Get string with the JSON representation of the build metadata for an asset.
        // When useImporter is false this call directly calculates the data e.g. it bypasses importer caching layer
        // allowCaching controls the importer cache, when false it forces the importer to run even if there is already a cached output
        public static extern string CalculateAssetBuildMetaDataAsJson(GUID asset, bool allowCaching);

        public static extern string GetArtifactIDForMetaDataImport(GUID asset);

        // Used for testing purposes. This will crash the editor so we can test handling importer crashes
        public static extern void CrashEditor();

        [StaticAccessor("BuildPipeline", StaticAccessorType.DoubleColon)]
        public static extern bool TryGetVersionByTypeID(int typeId, BuildTarget target, out Hash128 outVersion);

        [StaticAccessor("BuildPipeline", StaticAccessorType.DoubleColon)]
        public static extern bool TryGetVersionByName(string name, BuildTarget target, out Hash128 outVersion);
    }
}
