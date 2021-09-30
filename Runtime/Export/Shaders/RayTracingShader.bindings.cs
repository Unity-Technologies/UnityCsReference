// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace UnityEngine.Experimental.Rendering
{
    [NativeHeader("Runtime/Shaders/RayTracingShader.h")]
    [NativeHeader("Runtime/Shaders/RayTracingAccelerationStructure.h")]
    [NativeHeader("Runtime/Graphics/ShaderScriptBindings.h")]
    public sealed partial class RayTracingShader : Object
    {
        public extern float maxRecursionDepth { get; }
        // Set uniforms
        [FreeFunction(Name = "RayTracingShaderScripting::SetValue<float>", HasExplicitThis = true)]
        extern public void SetFloat(int nameID, float val);
        [FreeFunction(Name = "RayTracingShaderScripting::SetValue<int>", HasExplicitThis = true)]
        extern public void SetInt(int nameID, int val);
        [FreeFunction(Name = "RayTracingShaderScripting::SetValue<Vector4f>", HasExplicitThis = true)]
        extern public void SetVector(int nameID, Vector4 val);
        [FreeFunction(Name = "RayTracingShaderScripting::SetValue<Matrix4x4f>", HasExplicitThis = true)]
        extern public void SetMatrix(int nameID, Matrix4x4 val);
        [FreeFunction(Name = "RayTracingShaderScripting::SetArray<float>", HasExplicitThis = true)]
        extern private void SetFloatArray(int nameID, float[] values);
        [FreeFunction(Name = "RayTracingShaderScripting::SetArray<int>", HasExplicitThis = true)]
        extern private void SetIntArray(int nameID, int[] values);
        [FreeFunction(Name = "RayTracingShaderScripting::SetArray<Vector4f>", HasExplicitThis = true)]
        extern public void SetVectorArray(int nameID, Vector4[] values);
        [FreeFunction(Name = "RayTracingShaderScripting::SetArray<Matrix4x4f>", HasExplicitThis = true)]
        extern public void SetMatrixArray(int nameID, Matrix4x4[] values);
        [NativeMethod(Name = "RayTracingShaderScripting::SetTexture", HasExplicitThis = true, IsFreeFunction = true, ThrowsException = true)]
        extern public void SetTexture(int nameID, [NotNull] Texture texture);
        [NativeMethod(Name = "RayTracingShaderScripting::SetBuffer", HasExplicitThis = true, IsFreeFunction = true, ThrowsException = true)]
        extern public void SetBuffer(int nameID, [NotNull] ComputeBuffer buffer);
        [NativeMethod(Name = "RayTracingShaderScripting::SetAccelerationStructure", HasExplicitThis = true, IsFreeFunction = true, ThrowsException = true)]
        extern public void SetAccelerationStructure(int nameID, [NotNull] RayTracingAccelerationStructure accelerationStrucure);
        extern public void SetShaderPass(string passName);
        [NativeMethod(Name = "RayTracingShaderScripting::SetTextureFromGlobal", HasExplicitThis = true, IsFreeFunction = true, ThrowsException = true)]
        extern public void SetTextureFromGlobal(int nameID, int globalTextureNameID);
        [NativeName("DispatchRayTracingShader")]
        extern public void Dispatch(string rayGenFunctionName, int width, int height, int depth, Camera camera = null);
    }
}
