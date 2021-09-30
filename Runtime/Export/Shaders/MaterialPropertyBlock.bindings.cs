// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;
using SphericalHarmonicsL2 = UnityEngine.Rendering.SphericalHarmonicsL2;

namespace UnityEngine
{
    [NativeHeader("Runtime/Graphics/ShaderScriptBindings.h")]
    [NativeHeader("Runtime/Shaders/ShaderPropertySheet.h")]
    [NativeHeader("Runtime/Shaders/ComputeShader.h")]
    [NativeHeader("Runtime/Math/SphericalHarmonicsL2.h")]
    public sealed partial class MaterialPropertyBlock
    {
        // TODO: get buffer is missing

        [NativeName("GetFloatFromScript")]   extern private float     GetFloatImpl(int name);
        [NativeName("GetVectorFromScript")]  extern private Vector4   GetVectorImpl(int name);
        [NativeName("GetColorFromScript")]   extern private Color     GetColorImpl(int name);
        [NativeName("GetMatrixFromScript")]  extern private Matrix4x4 GetMatrixImpl(int name);
        [NativeName("GetTextureFromScript")] extern private Texture   GetTextureImpl(int name);

        [NativeName("SetFloatFromScript")]   extern private void SetFloatImpl(int name, float value);
        [NativeName("SetVectorFromScript")]  extern private void SetVectorImpl(int name, Vector4 value);
        [NativeName("SetColorFromScript")]   extern private void SetColorImpl(int name, Color value);
        [NativeName("SetMatrixFromScript")]  extern private void SetMatrixImpl(int name, Matrix4x4 value);
        [NativeName("SetTextureFromScript")] extern private void SetTextureImpl(int name, [NotNull] Texture value);
        [NativeName("SetRenderTextureFromScript")] extern private void SetRenderTextureImpl(int name, [NotNull] RenderTexture value, RenderTextureSubElement element);
        [NativeName("SetBufferFromScript")]  extern private void SetBufferImpl(int name, ComputeBuffer value);
        [NativeName("SetConstantBufferFromScript")] extern private void SetConstantBufferImpl(int name, ComputeBuffer value, int offset, int size);

        [NativeName("SetFloatArrayFromScript")]  extern private void SetFloatArrayImpl(int name, float[] values, int count);
        [NativeName("SetVectorArrayFromScript")] extern private void SetVectorArrayImpl(int name, Vector4[] values, int count);
        [NativeName("SetMatrixArrayFromScript")] extern private void SetMatrixArrayImpl(int name, Matrix4x4[] values, int count);

        [NativeName("GetFloatArrayFromScript")]  extern private float[]     GetFloatArrayImpl(int name);
        [NativeName("GetVectorArrayFromScript")] extern private Vector4[]   GetVectorArrayImpl(int name);
        [NativeName("GetMatrixArrayFromScript")] extern private Matrix4x4[] GetMatrixArrayImpl(int name);

        [NativeName("GetFloatArrayCountFromScript")]  extern private int GetFloatArrayCountImpl(int name);
        [NativeName("GetVectorArrayCountFromScript")] extern private int GetVectorArrayCountImpl(int name);
        [NativeName("GetMatrixArrayCountFromScript")] extern private int GetMatrixArrayCountImpl(int name);

        [NativeName("ExtractFloatArrayFromScript")]  extern private void ExtractFloatArrayImpl(int name, [Out] float[] val);
        [NativeName("ExtractVectorArrayFromScript")] extern private void ExtractVectorArrayImpl(int name, [Out] Vector4[] val);
        [NativeName("ExtractMatrixArrayFromScript")] extern private void ExtractMatrixArrayImpl(int name, [Out] Matrix4x4[] val);

        [FreeFunction("ConvertAndCopySHCoefficientArraysToPropertySheetFromScript")]
        extern internal static void Internal_CopySHCoefficientArraysFrom(MaterialPropertyBlock properties, SphericalHarmonicsL2[] lightProbes, int sourceStart, int destStart, int count);

        [FreeFunction("CopyProbeOcclusionArrayToPropertySheetFromScript")]
        extern internal static void Internal_CopyProbeOcclusionArrayFrom(MaterialPropertyBlock properties, Vector4[] occlusionProbes, int sourceStart, int destStart, int count);

        [NativeMethod(Name = "MaterialPropertyBlockScripting::Create", IsFreeFunction = true)]
        extern private static System.IntPtr CreateImpl();
        [NativeMethod(Name = "MaterialPropertyBlockScripting::Destroy", IsFreeFunction = true, IsThreadSafe = true)]
        extern private static void DestroyImpl(System.IntPtr mpb);

        extern public bool isEmpty {[NativeName("IsEmpty")] get; }

        extern private void Clear(bool keepMemory);
        public void Clear() { Clear(true); }
    }
}
