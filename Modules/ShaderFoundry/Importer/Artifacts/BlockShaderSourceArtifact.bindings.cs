// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    namespace ShaderFoundry
    {
        [NativeHeader("Modules/ShaderFoundry/Importer/Artifacts/BlockShaderSourceArtifact.h")]
        [NativeClass("ShaderFoundry::BlockShaderSourceArtifact")]
        internal sealed partial class BlockShaderSourceArtifact : Object
        {
            [NativeProperty("shaderName", false, TargetType.Field)]
            public extern string shaderName { get; }

            [NativeProperty("shaderSource", false, TargetType.Field)]
            public extern string shaderSource { get; }
        }
    }
}
