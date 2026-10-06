// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [global::UnityEngine.NativeClass("ComputeShaderImporter", PersistentTypeId = 1008)]
    [NativeHeader("Editor/Src/AssetPipeline/ComputeShaderImporter.h")]
    public sealed partial class ComputeShaderImporter : AssetImporter
    {
        internal ComputeShaderImporter(global::UnityEngine.EntityId id) : base(id) {}
        public ComputeShaderImporter() {}
        public PreprocessorOverride preprocessorOverride { get { return PreprocessorOverride.UseProjectSettings; } set {} }
    }
}
