// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor.Build.Reporting
{
    [NativeHeader("Modules/BuildReportingEditor/Public/ContentSummary.h")]
    internal struct TypeStats
    {
        public Type type { get; }

        /// <summary>
        /// Size, in bytes, of the serialized fields and resource data for all objects this type.
        /// </summary>
        public ulong size { get; }

        /// <summary>
        /// Number of objects of this type in the build output.
        /// </summary>
        public int objectCount { get; }

        /// <summary>
        /// Number of binary data blobs referenced from objects of this type.
        /// </summary>
        public int resourceCount { get; }
    }

    [NativeHeader("Modules/BuildReportingEditor/Public/ContentSummary.h")]
    internal struct AssetStats
    {
        /// <summary>
        /// AssetDatabase GUID of the Asset.
        /// </summary>
        public GUID sourceAssetGUID { get; }

        /// <summary>
        /// Path at the time of build.
        /// </summary>
        public string sourceAssetPath { get; }

        /// <summary>
        /// Size, in bytes, of the serialized fields and resource data for all objects originating from this source asset.
        /// </summary>
        public ulong size { get; }

        /// <summary>
        /// Number of objects from this Asset in the build output.
        /// </summary>
        public int objectCount { get; }

        /// <summary>
        /// Number of binary data blobs referenced from objects from this Asset.
        /// </summary>
        public int resourceCount { get; }
    }

    [NativeHeader("Modules/BuildReportingEditor/Public/ContentSummary.h")]
    [NativeClass("BuildReporting::ContentSummary")]
    internal sealed class ContentSummary : Object
    {
        /// <summary>
        /// Size of the content portion of the build.  This is the size prior to any compression that is applied if the content is built into Unity Archives.
        /// </summary>
        /// <remarks>
        /// Padding is used for better data alignment and the actual content files on disk may be slightly larger than the reported size.
        /// This size is calculated before any compression is applied.
        /// </remarks>
        public ulong serializedFileSize
        {
            get => GetSerializedFileSize();
        }

        /// <summary>
        /// Size of the content portion of the build that is newly created in this build.
        /// </summary>
        /// <remarks>
        /// This size is calculated similarly to <see cref="serializedFileSize"/>, 
        /// but it only includes the serialized files and resource files that are newly generated and not retrieved from previous build results.
        /// These files may not be copied to the output destination if identical files already exist there.
        /// This size is calculated before any compression is applied.
        /// </remarks>
        public ulong generatedFileSize
        {
            get => GetGeneratedFileSize();
        }

        /// <summary>
        /// Size of all files at the output destination that remain unaltered and are reused in the current build.
        /// </summary>
        /// <remarks>
        /// This size reflects the total after files have been compressed.
        /// </remarks>
        public ulong sizeReusedContentInOutputDirectory
        {
            get => GetSizeReusedContentInOutputDirectory();
        }

        /// <summary>
        /// Total size of the header section of each Serialized File in the build output.
        /// </summary>
        /// <remarks>
        /// This size is calculated before any compression is applied.
        /// </remarks>
        public ulong headerSize
        {
            get => GetHeaderSize();
        }

        /// <summary>
        /// Total size of the referenced data inside .resS and .Resource files.
        /// </summary>
        /// <remarks>
        /// Padding is used to better data alignment, so the actual files may be slightly larger than the reported size.
        /// This size is calculated before any compression is applied.
        /// </remarks>
        public ulong resourceDataSize
        {
            get => GetResourceDataSize();
        }

        /// <summary>
        /// Number of serialized files (.cf file extension) in the build output.
        /// </summary>
        public int serializedFileCount
        {
            get => GetSerializedFileCount();
        }

        /// <summary>
        /// Number of newly built serialized files and resource files in the build output.
        /// </summary>
        public int generatedFileCount
        {
            get => GetGeneratedFileCount();
        }

        /// <summary>
        /// Number of Resource files (.resS and .resource file extensions) in the build output.
        /// </summary>
        public int resourceFileCount
        {
            get => GetResourceFileCount();
        }

        /// <summary>
        /// Total number of objects that have been serialized in the build output.
        /// </summary>
        public int objectCount
        {
            get => GetObjectCount();
        }

        /// <summary>
        /// Retrieve an array containing statistics for each Unity Object type that is included in the build.
        /// </summary>
        /// <remarks>
        /// In this form all MonoBehaviour and ScriptableObject derived objects are counted together with the MonoBehaviour type.
        /// </remarks>
        public TypeStats[] typeStats
        {
            get => GetTypeStats();
        }

        /// <summary>
        /// Retrieve an array with statistics for each Scene and Asset that contributed content into the build output.
        /// </summary>
        /// <remarks>
        /// For large builds this array can grow large, based on the number of Assets that were build.
        /// It is best to retrieve it once into a local variable that is enumerated, rather than calling assetStats more than once.
        /// </remarks>
        public AssetStats[] assetStats
        {
            get => GetAssetStats();
        }

        private extern ulong GetSerializedFileSize();
        private extern ulong GetGeneratedFileSize();
        private extern ulong GetSizeReusedContentInOutputDirectory();
        private extern ulong GetHeaderSize();
        private extern ulong GetResourceDataSize();
        private extern int GetSerializedFileCount();
        private extern int GetGeneratedFileCount();
        private extern int GetResourceFileCount();
        private extern int GetObjectCount();
        private extern TypeStats[] GetTypeStats();
        private extern AssetStats[] GetAssetStats();
    }
}
