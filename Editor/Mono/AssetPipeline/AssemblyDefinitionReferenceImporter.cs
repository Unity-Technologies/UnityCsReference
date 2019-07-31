// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEngine;

namespace UnityEditorInternal
{
    [ExcludeFromPreset]
    public sealed partial class AssemblyDefinitionReferenceImporter : AssetImporter
    {
    }

    public sealed partial class AssemblyDefinitionReferenceAsset : TextAsset
    {
        private AssemblyDefinitionReferenceAsset() {}

        private AssemblyDefinitionReferenceAsset(string text) {}
    }
}
