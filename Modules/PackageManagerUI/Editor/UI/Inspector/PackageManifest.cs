// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;

namespace UnityEditorInternal
{
    [ExcludeFromPreset]
    public sealed class PackageManifestImporter : AssetImporter
    {
    }

    public sealed class PackageManifest : TextAsset
    {
        private PackageManifest() {}

        private PackageManifest(string text) {}
    }
}
