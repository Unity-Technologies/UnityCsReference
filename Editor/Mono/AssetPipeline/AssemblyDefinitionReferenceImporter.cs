// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
    [ExcludeFromPreset]
    [NativeClass("AssemblyDefinitionReferenceImporter", PersistentTypeId = 0x118A83A3)]
    public sealed partial class AssemblyDefinitionReferenceImporter : AssetImporter
    {
        internal AssemblyDefinitionReferenceImporter(global::UnityEngine.EntityId id) : base(id) {}
        public AssemblyDefinitionReferenceImporter() {}
    }

    [NativeClass("AssemblyDefinitionReferenceAsset", PersistentTypeId = 0x277E3BD6)]
    public sealed partial class AssemblyDefinitionReferenceAsset : TextAsset
    {
        internal AssemblyDefinitionReferenceAsset(global::UnityEngine.EntityId id) : base(id) {}
        private AssemblyDefinitionReferenceAsset() {}

        private AssemblyDefinitionReferenceAsset(string text) {}
    }
}
