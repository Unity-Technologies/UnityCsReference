// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;


namespace UnityEngine.Experimental.Rendering
{
    public struct ShaderWarmupSetup
    {
        public VertexAttributeDescriptor[] vdecl;
    }

    [NativeHeader("Runtime/Graphics/ShaderScriptBindings.h")]
    public static class ShaderWarmup
    {
        [FreeFunction(Name = "ShaderWarmupScripting::WarmupShader")]
        static public extern void WarmupShader(Shader shader, ShaderWarmupSetup setup);
        [FreeFunction(Name = "ShaderWarmupScripting::WarmupShaderFromCollection")]
        static public extern void WarmupShaderFromCollection(ShaderVariantCollection collection, Shader shader, ShaderWarmupSetup setup);
    }
}
