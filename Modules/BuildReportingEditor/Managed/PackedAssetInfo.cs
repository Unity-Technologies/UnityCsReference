// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;

namespace UnityEditor.Build.Reporting
{
    ///<summary>Contains information about a range of bytes in a file in the build output.</summary>
    ///<remarks>A Packed Asset contains either the serialized binary representation of a Unity Object,
    ///or the binary data of a texture, mesh, audio or video that belongs to a Unity object.
    ///
    ///Note: the "Packed Asset" name is somewhat misleading because the data is associated with a specific object within an Asset, not an entire Asset.
    ///Some Assets contain just a single object, but in many cases it may contain an entire hierarchy of objects, each with its own PackedAssetInfo entry.</remarks>
    ///<example>
    ///  <code><![CDATA[
    ///using System.Collections.Generic;
    ///using System.IO;
    ///using System.Linq;
    ///using System.Text;
    ///using UnityEditor;
    ///using UnityEditor.Build.Reporting;
    ///using UnityEngine;
    ///
    ///public struct ContentEntry
    ///{
    ///    public ulong size;        // Bytes
    ///    public int objectCount;   // Number of objects from the same source
    ///}
    ///
    ///public class BuildReportPackedAssetInfoExample
    ///{
    ///    [MenuItem("Example/Build and Analyze AssetBundle")]
    ///    static public void Build()
    ///    {
    ///        string buildOutputDirectory = "BuildOutput";
    ///        if (!Directory.Exists(buildOutputDirectory))
    ///            Directory.CreateDirectory(buildOutputDirectory);
    ///
    ///        var bundleDefinitions = new AssetBundleBuild[]
    ///        {
    ///            new AssetBundleBuild()
    ///            {
    ///                assetBundleName = "MyBundle",
    ///
    ///                // Tip: Adjust this list to builds scenes or assets from your project
    ///                assetNames = new string[] { "Assets/Scenes/TestScene.unity" }
    ///            }
    ///        };
    ///
    ///        BuildPipeline.BuildAssetBundles(
    ///            buildOutputDirectory,
    ///            bundleDefinitions,
    ///            BuildAssetBundleOptions.ForceRebuildAssetBundle,
    ///            EditorUserBuildSettings.activeBuildTarget);
    ///
    ///        BuildReport report = BuildReport.GetLatestReport();
    ///        if (report != null)
    ///        {
    ///            var sb = new StringBuilder();
    ///            sb.AppendLine("Build result   : " + report.summary.result);
    ///            sb.AppendLine("Build size     : " + report.summary.totalSize + " bytes");
    ///            sb.Append(ClassifyBuildOutputBySourceAsset(report));
    ///            Debug.Log(sb.ToString());
    ///        }
    ///        else
    ///        {
    ///            Debug.Log("AssetBundle build failed");
    ///        }
    ///    }
    ///
    ///    public static string ClassifyBuildOutputBySourceAsset(BuildReport buildReport)
    ///    {
    ///        var sb = new StringBuilder();
    ///
    ///        var sourceAssetSize = new Dictionary<string, ContentEntry>();
    ///
    ///        var packedAssets = buildReport.packedAssets;
    ///        foreach(var packedAsset in packedAssets)
    ///        {
    ///            sb.AppendLine("Analyzing " + packedAsset.shortPath + "....");
    ///
    ///            var contents = packedAsset.contents;
    ///            foreach(var packedAssetInfo in contents)
    ///            {
    ///                // Path of the asset that contains this object
    ///                var path = packedAssetInfo.sourceAssetPath;
    ///
    ///                if (string.IsNullOrEmpty(path))
    ///                    path = "Internal";
    ///
    ///                if (sourceAssetSize.ContainsKey(path))
    ///                {
    ///                    var existingEntry = sourceAssetSize[path];
    ///                    existingEntry.size += packedAssetInfo.packedSize;
    ///                    existingEntry.objectCount++;
    ///                    sourceAssetSize[path] = existingEntry;
    ///                }
    ///                else
    ///                {
    ///                    sourceAssetSize[path] = new ContentEntry
    ///                    {
    ///                        size = packedAssetInfo.packedSize,
    ///                        objectCount = 1
    ///                    };
    ///                }
    ///            }
    ///        }
    ///
    ///        sb.AppendLine("The Build contains the content from the following source assets:\n");
    ///
    ///        // Sort biggest to smallest
    ///        var sortedSourceAssetSize = sourceAssetSize.OrderByDescending(x => x.Value.size);
    ///
    ///        // Note: for large builds there could be thousands or more different source assets,
    ///        // in which case it could be prudent to only show the top 10 or top 100 results.
    ///        for (int i = 0; i < sortedSourceAssetSize.Count(); i++)
    ///        {
    ///            var entry = sortedSourceAssetSize.ElementAt(i);
    ///            sb.AppendLine(" Asset: \"" + entry.Key + "\" Object Count: " + entry.Value.objectCount + " Size of Objects: " + entry.Value.size);
    ///        }
    ///
    ///        return sb.ToString();
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<seealso cref="BuildReport" />
    ///<seealso cref="PackedAssets.contents" />
    [NativeType(Header = "Modules/BuildReportingEditor/Public/PackedAssets.h")]
    public struct PackedAssetInfo
    {
        ///<summary>Local file id of the object</summary>
        ///<remarks>
        ///For Serialized Files this is the local file id of the object in the file in the build output.  It will be unique within that file.
        ///During the builds ids are reassigned, so this id will be different from the id of the object in its source scene or asset.
        ///
        ///This ID is 0 for the content of .resS and .resource files.
        ///</remarks>
        [NativeName("fileID")]
        public long id { get; }
        ///<summary>The type of the object whose serialized data is represented by the Packed Asset, such as <see cref="GameObject" />, <see cref="Mesh" /> or <see cref="AudioClip" />.</summary>
        public Type type { get; }
        ///<summary>The size in bytes of the Packed Asset.</summary>
        ///<remarks>This is the size prior to any compression.  The actual size on disk can be smaller when the file is stored inside a Unity Archive (e.g. in the case of a AssetBundle, or a Player built with <see cref="BuildOptions.CompressWithLz4HC" />).
        ///
        ///Note: there can be extra padding bytes inserted between Packed Assets, added for alignment purpose.
        ///Because of this, the offset + packedSize may be slightly smaller than the offset of the next element in the PackedAsset array.</remarks>
        ///<seealso cref="PackedAssets._contents" />
        public ulong packedSize { get; }
        ///<summary>The offset from the start of the file to the first byte of the range belonging to the Packed Asset.</summary>
        public ulong offset { get; }
        ///<summary>The Global Unique Identifier (GUID) of the source Asset that the build process used to generate the packed Asset.</summary>
        ///<seealso cref="AssetDatabase.GUIDFromAssetPath" />
        public GUID sourceAssetGUID { get; }
        ///<summary>The file path to the source Asset that the build process used to generate the Packed Asset, relative to the Project directory.</summary>
        ///<remarks>Note: the same path may be repeated many times in the PackedAssets array, because PackedAssets track objects, and a single Asset can contain many objects.
        ///
        ///Note: Some packed Assets might not have a source Asset.  For example a Sprite Atlas that is generated at build time.  Also, AssetBundles contain generated objects that do not come from any source asset, e.g. the <see cref="AssetBundleManifest" />.</remarks>
        [NativeName("buildTimeAssetPath")]
        public string sourceAssetPath { get; }
    }
}
