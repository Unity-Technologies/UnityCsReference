// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

namespace UnityEngine
{
    // skinning/blend-shapes are implemented with compute shaders so we must be able to load them from builtins
    [UsedByNativeCode]
    [NativeHeader("Runtime/Graphics/ShaderScriptBindings.h")]
    [NativeHeader("Runtime/Shaders/ComputeShader.h")]
    public sealed partial class ComputeShader : Object
    {
        // skinning/blend-shapes are implemented with compute shaders so we must be able to load them from builtins
        // alas marking ONLY class as used might not work if we actually use it only in cpp land, so mark "random" method too
        [RequiredByNativeCode]
        [NativeMethod(Name = "ComputeShaderScripting::FindKernel", HasExplicitThis = true, IsFreeFunction = true, ThrowsException = true)]
        extern public int FindKernel(string name);
        [FreeFunction(Name = "ComputeShaderScripting::HasKernel", HasExplicitThis = true)]
        extern public bool HasKernel(string name);

        [FreeFunction(Name = "ComputeShaderScripting::SetValue<float>", HasExplicitThis = true)]
        extern public void SetFloat(int nameID, float val);
        [FreeFunction(Name = "ComputeShaderScripting::SetValue<int>", HasExplicitThis = true)]
        extern public void SetInt(int nameID, int val);
        [FreeFunction(Name = "ComputeShaderScripting::SetValue<Vector4f>", HasExplicitThis = true)]
        extern public void SetVector(int nameID, Vector4 val);
        [FreeFunction(Name = "ComputeShaderScripting::SetValue<Matrix4x4f>", HasExplicitThis = true)]
        extern public void SetMatrix(int nameID, Matrix4x4 val);

        [FreeFunction(Name = "ComputeShaderScripting::SetArray<float>", HasExplicitThis = true)]
        extern private void SetFloatArray(int nameID, float[] values);
        [FreeFunction(Name = "ComputeShaderScripting::SetArray<int>", HasExplicitThis = true)]
        extern private void SetIntArray(int nameID, int[] values);
        [FreeFunction(Name = "ComputeShaderScripting::SetArray<Vector4f>", HasExplicitThis = true)]
        extern public void SetVectorArray(int nameID, Vector4[] values);
        [FreeFunction(Name = "ComputeShaderScripting::SetArray<Matrix4x4f>", HasExplicitThis = true)]
        extern public void SetMatrixArray(int nameID, Matrix4x4[] values);

        [NativeMethod(Name = "ComputeShaderScripting::SetTexture", HasExplicitThis = true, IsFreeFunction = true, ThrowsException = true)]
        extern public void SetTexture(int kernelIndex, int nameID, [NotNull] Texture texture, int mipLevel);

        [NativeMethod(Name = "ComputeShaderScripting::SetRenderTexture", HasExplicitThis = true, IsFreeFunction = true, ThrowsException = true)]
        extern private void SetRenderTexture(int kernelIndex, int nameID, [NotNull] RenderTexture texture, int mipLevel, RenderTextureSubElement element);

        [NativeMethod(Name = "ComputeShaderScripting::SetTextureFromGlobal", HasExplicitThis = true, IsFreeFunction = true, ThrowsException = true)]
        extern public void SetTextureFromGlobal(int kernelIndex, int nameID, int globalTextureNameID);

        [FreeFunction(Name = "ComputeShaderScripting::SetBuffer", HasExplicitThis = true)]
        extern private void Internal_SetBuffer(int kernelIndex, int nameID, [NotNull] ComputeBuffer buffer);
        [FreeFunction(Name = "ComputeShaderScripting::SetBuffer", HasExplicitThis = true)]
        extern private void Internal_SetGraphicsBuffer(int kernelIndex, int nameID, [NotNull] GraphicsBuffer buffer);

        public void SetBuffer(int kernelIndex, int nameID, ComputeBuffer buffer)
        {
            Internal_SetBuffer(kernelIndex, nameID, buffer);
        }

        public void SetBuffer(int kernelIndex, int nameID, GraphicsBuffer buffer)
        {
            Internal_SetGraphicsBuffer(kernelIndex, nameID, buffer);
        }

        [FreeFunction(Name = "ComputeShaderScripting::SetConstantBuffer", HasExplicitThis = true)]
        extern private void SetConstantComputeBuffer(int nameID, [NotNull] ComputeBuffer buffer, int offset, int size);

        [FreeFunction(Name = "ComputeShaderScripting::SetConstantBuffer", HasExplicitThis = true)]
        extern private void SetConstantGraphicsBuffer(int nameID, [NotNull] GraphicsBuffer buffer, int offset, int size);

        [NativeMethod(Name = "ComputeShaderScripting::GetKernelThreadGroupSizes", HasExplicitThis = true, IsFreeFunction = true, ThrowsException = true)]
        extern public void GetKernelThreadGroupSizes(int kernelIndex, out uint x, out uint y, out uint z);

        [NativeName("DispatchComputeShader")] extern public void Dispatch(int kernelIndex, int threadGroupsX, int threadGroupsY, int threadGroupsZ);
        [FreeFunction(Name = "ComputeShaderScripting::DispatchIndirect", HasExplicitThis = true)]
        extern private void Internal_DispatchIndirect(int kernelIndex, [NotNull] ComputeBuffer argsBuffer, uint argsOffset);
        [FreeFunction(Name = "ComputeShaderScripting::DispatchIndirect", HasExplicitThis = true)]
        extern private void Internal_DispatchIndirectGraphicsBuffer(int kernelIndex, [NotNull] GraphicsBuffer argsBuffer, uint argsOffset);

        extern public LocalKeywordSpace keywordSpace { get; }

        [FreeFunction("ComputeShaderScripting::EnableKeyword", HasExplicitThis = true)]
        extern public void EnableKeyword(string keyword);
        [FreeFunction("ComputeShaderScripting::DisableKeyword", HasExplicitThis = true)]
        extern public void DisableKeyword(string keyword);
        [FreeFunction("ComputeShaderScripting::IsKeywordEnabled", HasExplicitThis = true)]
        extern public bool IsKeywordEnabled(string keyword);

        [FreeFunction("ComputeShaderScripting::EnableKeyword", HasExplicitThis = true)]
        extern private void EnableLocalKeyword(LocalKeyword keyword);
        [FreeFunction("ComputeShaderScripting::DisableKeyword", HasExplicitThis = true)]
        extern private void DisableLocalKeyword(LocalKeyword keyword);
        [FreeFunction("ComputeShaderScripting::SetKeyword", HasExplicitThis = true)]
        extern private void SetLocalKeyword(LocalKeyword keyword, bool value);
        [FreeFunction("ComputeShaderScripting::IsKeywordEnabled", HasExplicitThis = true)]
        extern private bool IsLocalKeywordEnabled(LocalKeyword keyword);

        public void EnableKeyword(in LocalKeyword keyword) { EnableLocalKeyword(keyword); }
        public void DisableKeyword(in LocalKeyword keyword) { DisableLocalKeyword(keyword); }
        public void SetKeyword(in LocalKeyword keyword, bool value) { SetLocalKeyword(keyword, value); }
        public bool IsKeywordEnabled(in LocalKeyword keyword) { return IsLocalKeywordEnabled(keyword); }

        [FreeFunction("ComputeShaderScripting::IsSupported", HasExplicitThis = true)]
        extern public bool IsSupported(int kernelIndex);

        [FreeFunction("ComputeShaderScripting::GetShaderKeywords", HasExplicitThis = true)] extern private string[] GetShaderKeywords();
        [FreeFunction("ComputeShaderScripting::SetShaderKeywords", HasExplicitThis = true)] extern private void SetShaderKeywords(string[] names);
        public string[] shaderKeywords { get { return GetShaderKeywords(); } set { SetShaderKeywords(value); } }

        [FreeFunction("ComputeShaderScripting::GetEnabledKeywords", HasExplicitThis = true)] extern private LocalKeyword[] GetEnabledKeywords();
        [FreeFunction("ComputeShaderScripting::SetEnabledKeywords", HasExplicitThis = true)] extern private void SetEnabledKeywords(LocalKeyword[] keywords);
        public LocalKeyword[] enabledKeywords { get { return GetEnabledKeywords(); } set { SetEnabledKeywords(value); } }
    }
}
