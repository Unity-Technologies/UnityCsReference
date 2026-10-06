// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor.Experimental
{
    // AssetImporter for importing ReferenceArtifactGenerator
    [NativeHeader("Modules/AssetDatabase/Editor/V2/MultiArtifactTestImporter.h")]
    [NativeClass("MultiArtifactTestImporter", PersistentTypeId = 0x48E92AD4)]
    [ExcludeFromPreset]
    internal partial class MultiArtifactTestImporter : AssetImporter
    {
        internal MultiArtifactTestImporter(global::UnityEngine.EntityId id) : base(id) {}
    }
}
