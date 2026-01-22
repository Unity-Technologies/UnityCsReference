// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
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
    ///<code source="../Tests/BuildReporting/Assets/Editor/ReferenceExamples/PackedAssetInfo.cs"/>
    ///</example>
    ///<seealso cref="BuildReport" />
    ///<seealso cref="PackedAssets.contents" />
    [NativeHeader("Modules/BuildReportingEditor/Public/PackedAssets.h")]
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
