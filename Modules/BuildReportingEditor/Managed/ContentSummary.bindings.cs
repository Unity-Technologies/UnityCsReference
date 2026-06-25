// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor.Build.Reporting
{
    /// <summary>
    /// Statistics for a specific Unity Object type included in a build.
    /// </summary>
    /// <seealso cref="ContentSummary.typeStats"/>
    [NativeHeader("Modules/BuildReportingEditor/Public/ContentSummary.h")]
    public struct TypeStats
    {
        /// <summary>
        /// The Unity Object type, such as <see cref="Texture2D"/> or <see cref="Mesh"/>.
        /// </summary>
        /// <remarks>
        /// All <see cref="MonoBehaviour"/> and <see cref="ScriptableObject"/> derived types
        /// are aggregated under <see cref="MonoBehaviour"/>.
        /// </remarks>
        public Type type { get; }

        /// <summary>
        /// Total size, in bytes, of the serialized fields and referenced resource data
        /// for all objects of this type in the build output.
        /// </summary>
        public ulong size { get; }

        /// <summary>
        /// Number of objects of this type in the build output.
        /// </summary>
        public int objectCount { get; }

        /// <summary>
        /// Number of binary data blobs (resource streams) referenced by objects of this type.
        /// </summary>
        public int resourceCount { get; }
    }

    /// <summary>
    /// Statistics for a specific source Asset included in a build.
    /// </summary>
    /// <seealso cref="ContentSummary.assetStats"/>
    [NativeHeader("Modules/BuildReportingEditor/Public/ContentSummary.h")]
    public struct AssetStats
    {
        /// <summary>
        /// The AssetDatabase GUID of the source Asset.
        /// </summary>
        public GUID sourceAssetGUID { get; }

        /// <summary>
        /// The Asset path at the time of the build.
        /// </summary>
        public string sourceAssetPath { get; }

        /// <summary>
        /// Total size, in bytes, of the serialized fields and referenced resource data
        /// for all objects originating from this source Asset.
        /// </summary>
        public ulong size { get; }

        /// <summary>
        /// Number of objects from this source Asset in the build output.
        /// </summary>
        public int objectCount { get; }

        /// <summary>
        /// Number of binary data blobs (resource streams) referenced by objects from this source Asset.
        /// </summary>
        public int resourceCount { get; }
    }

    /// <summary>
    /// Provides a high-level summary of the content included in a build.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ContentSummary provides a compact overview of build content that is more efficient to work with than
    /// the detailed <see cref="PackedAssets"/> data. It includes aggregate statistics such as total sizes,
    /// object counts, and breakdowns by Unity Object type and source Asset.
    /// </para>
    /// <para>
    /// ContentSummary is populated for Player builds, AssetBundle builds, and
    /// ContentDirectory builds.
    /// It is not populated for scripts-only Player builds (see <see cref="BuildOptions.BuildScriptsOnly"/>).
    /// </para>
    /// <para>
    /// For incremental AssetBundle builds, the statistics only reflect the content of the
    /// AssetBundles that were rebuilt in the current build invocation. AssetBundles that were
    /// unchanged and reused from previous build results are not included.
    /// </para>
    /// </remarks>
    /// <seealso cref="BuildPipeline.BuildContentDirectory"/>
    /// <seealso cref="BuildReport"/>
    /// <example>
    /// <code source="../Tests/BuildReporting/Assets/Editor/ReferenceExamples/ContentSummary.cs"/>
    /// </example>
    [NativeHeader("Modules/BuildReportingEditor/Public/ContentSummary.h")]
    [NativeClass("BuildReporting::ContentSummary")]
    public sealed class ContentSummary : Object
    {
        private ContentSummary()
        {
        }

        /// <summary>
        /// Total size, in bytes, of the serialized files in the build output.
        /// </summary>
        /// <remarks>
        /// This is the uncompressed size prior to any compression that may be applied when content is packed
        /// into AssetBundles or other Unity Archives. Padding used for data alignment may cause actual files on disk to be
        /// slightly larger than the reported size.
        /// </remarks>
        public ulong serializedFileSize
        {
            get => GetSerializedFileSize();
        }

        /// <summary>
        /// Size, in bytes, of the serialized files reused from previous builds rather than rebuilt.
        /// </summary>
        /// <remarks>
        /// This only accounts for serialized files. Resource files are excluded because resource
        /// content can be shared across multiple serialized files, making per-file reuse attribution
        /// inaccurate. This size is calculated before any compression is applied.
        /// </remarks>
        public ulong reusedSerializedFileSize
        {
            get => GetReusedSerializedFileSize();
        }

        /// <summary>
        /// Total size, in bytes, of the header sections across all serialized files in the build output.
        /// </summary>
        /// <remarks>
        /// Each serialized file has a header and metadata section, that contains the object table, TypeTrees
        /// and external file dependency information.  The rest of each file is dedicated to storing the
        /// serialized state of the Unity Objects assigned to that file.
        /// This size is calculated before any compression is applied.
        /// Header size is included in the <see cref="serializedFileSize"/> total.
        /// </remarks>
        public ulong headerSize
        {
            get => GetHeaderSize();
        }

        /// <summary>
        /// Total size, in bytes, of the binary data stored in <c>.resS</c> and <c>.resource</c> files.
        /// </summary>
        /// <remarks>
        /// Resource files contain large binary data such as textures, audio, and meshes that are
        /// referenced from serialized objects. Padding used for data alignment may cause actual files
        /// on disk to be slightly larger than the reported size.
        /// This size is calculated before any compression is applied if the file is inside a Unity Archive.
        /// </remarks>
        public ulong resourceDataSize
        {
            get => GetResourceDataSize();
        }

        /// <summary>
        /// Number of serialized files in the build output.
        /// </summary>
        /// <remarks>
        /// Serialized files are the specific Unity binary format used to store serialized Unity Objects.
        /// For example level files, shared assets and content files are all examples of serialized files.
        /// This count does not include other file formats, such as files with the .resS and .resource extension.
        /// The count includes serialized files located inside AssetBundles or other Unity Archive files.
        /// </remarks>
        public int serializedFileCount
        {
            get => GetSerializedFileCount();
        }

        /// <summary>
        /// Number of serialized files reused from previous builds rather than rebuilt.
        /// </summary>
        /// <remarks>
        /// The ratio of <c>reusedSerializedFileCount</c> to <see cref="serializedFileCount"/>
        /// indicates what fraction of serialized files were reused versus rebuilt.
        /// For Player and AssetBundle builds this value is always zero.
        /// </remarks>
        public int reusedSerializedFileCount
        {
            get => GetReusedSerializedFileCount();
        }

        /// <summary>
        /// Number of resource files (<c>.resS</c> and <c>.resource</c> extensions) in the build output.
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
        /// Returns an array containing statistics for each Unity Object type included in the build.
        /// </summary>
        /// <remarks>
        /// All <see cref="MonoBehaviour"/> and <see cref="ScriptableObject"/> derived types are
        /// aggregated under <c>typeof(MonoBehaviour)</c>. A new array is allocated each time this
        /// property is accessed.
        /// </remarks>
        public TypeStats[] typeStats
        {
            get => GetTypeStats();
        }

        /// <summary>
        /// Returns an array with statistics for each source Asset that contributed content to the build output.
        /// </summary>
        /// <remarks>
        /// For large builds this array can be large, proportional to the number of source Assets
        /// included in the build. A new array is allocated each time this property is accessed;
        /// cache the result in a local variable rather than accessing it repeatedly.
        /// </remarks>
        public AssetStats[] assetStats
        {
            get => GetAssetStats();
        }

        private extern ulong GetSerializedFileSize();
        private extern ulong GetReusedSerializedFileSize();
        private extern ulong GetHeaderSize();
        private extern ulong GetResourceDataSize();
        private extern int GetSerializedFileCount();
        private extern int GetReusedSerializedFileCount();
        private extern int GetResourceFileCount();
        private extern int GetObjectCount();
        private extern TypeStats[] GetTypeStats();
        private extern AssetStats[] GetAssetStats();
    }
}
