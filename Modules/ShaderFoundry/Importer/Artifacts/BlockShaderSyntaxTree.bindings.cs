// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    namespace ShaderFoundry
    {
        [NativeHeader("Modules/ShaderFoundry/Importer/Artifacts/BlockShaderSyntaxTree.h")]
        [NativeClass("ShaderFoundry::BlockShaderSyntaxTree")]
        internal sealed partial class BlockShaderSyntaxTree : Object
        {
            internal extern SyntaxTree SyntaxTree { get; }
            // TODO @ SHADERS: SHADERS-539 Expose AST functionality
        }
    }
}
