// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
    [ExcludeFromPreset]
    [NativeClass("AssemblyDefinitionImporter", PersistentTypeId = 0x694E83A9)]
    public sealed partial class AssemblyDefinitionImporter : AssetImporter
    {
        internal AssemblyDefinitionImporter(global::UnityEngine.EntityId id) : base(id) {}
        public AssemblyDefinitionImporter() {}
    }

    [NativeClass("AssemblyDefinitionAsset", PersistentTypeId = 0x44AD69A7)]
    public sealed partial class AssemblyDefinitionAsset : TextAsset
    {
        internal AssemblyDefinitionAsset(global::UnityEngine.EntityId id) : base(id) {}
        private AssemblyDefinitionAsset() {}

        private AssemblyDefinitionAsset(string text) {}
    }
}
