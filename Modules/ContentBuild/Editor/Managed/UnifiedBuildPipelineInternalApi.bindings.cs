// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Bindings;


// Additional Build Functionality, exposed for testing and other internal usage

[assembly: InternalsVisibleTo("BuildPipelineTestUtilities")]
[assembly: InternalsVisibleTo("ContentDirectoryUtilities")]

namespace UnityEditor.Build.Content
{
    [NativeHeader("Modules/ContentBuild/Editor/Public/UnifiedBuildPipelineTestUtilities.h")]
    [NativeHeader("Modules/ContentBuild/Editor/Ucbp/BuildPipelineCustomDependencies.h")]
    [StaticAccessor("BuildPipelineTestUtilities", StaticAccessorType.DoubleColon)]
    internal static partial class UnifiedBuildPipelineInternalApi
    {
        // Get string with the JSON representation of the build metadata for an asset.
        // When useImporter is false this call directly calculates the data e.g. it bypasses importer caching layer
        // allowCaching controls the importer cache, when false it forces the importer to run even if there is already a cached output


        // Returns the import result as SinglePassImportResult JSON.
        public static extern string RunSinglePassImport(GUID asset);

        public static extern string GetImportResultIDForSinglePassImport(GUID asset);

        // Loads the FileWriteMetaData stored under a content-file metadata UDS hash (as returned by
        // BuildArtifactMetadata.TypeSpecificMetadata) and returns it as JSON, or "" if none exists.
        public static extern string FileWriteMetaDataToJson(Hash128 contentFileMetadataHash);

        public static extern bool MetaDataImportArtifactExists(GUID asset);

        // Used for testing purposes. This will crash the editor so we can test handling importer crashes
        public static extern void CrashEditor();

        [StaticAccessor("BuildPipeline", StaticAccessorType.DoubleColon)]
        public static extern bool TryGetVersionByTypeID(int typeId, BuildTarget target, out Hash128 outVersion);

        [StaticAccessor("BuildPipeline", StaticAccessorType.DoubleColon)]
        public static extern bool TryGetVersionByName(string name, BuildTarget target, out Hash128 outVersion);

        public unsafe extern static Hash128 X3HashData(byte* input, UInt64 len);
    }
}
