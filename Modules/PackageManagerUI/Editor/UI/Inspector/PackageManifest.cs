// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditorInternal
{
    [ExcludeFromPreset]
    [NativeClass("PackageManifestImporter", PersistentTypeId = 0x710E27E6)]
    public sealed class PackageManifestImporter : AssetImporter
    {
        internal PackageManifestImporter(global::UnityEngine.EntityId id) : base(id) {}
        public PackageManifestImporter() {}
    }

    [NativeClass("PackageManifest", PersistentTypeId = 0x710E27E5)]
    public sealed class PackageManifest : TextAsset
    {
        internal PackageManifest(global::UnityEngine.EntityId id) : base(id) {}
        private PackageManifest() {}

        private PackageManifest(string text) {}
    }
}
