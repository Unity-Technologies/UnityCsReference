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

        [NativeName("GetIntFromScript"), ThreadSafe]     extern private int       GetIntImpl(int name);
        [NativeName("GetFloatFromScript"), ThreadSafe]   extern private float     GetFloatImpl(int name);
        [NativeName("GetVectorFromScript"), ThreadSafe]  extern private Vector4   GetVectorImpl(int name);
        [NativeName("GetColorFromScript"), ThreadSafe]   extern private Color     GetColorImpl(int name);
        [NativeName("GetMatrixFromScript"), ThreadSafe]  extern private Matrix4x4 GetMatrixImpl(int name);
        [NativeName("GetTextureFromScript"), ThreadSafe] extern private Texture   GetTextureImpl(int name);

        [NativeName("HasPropertyFromScript")] extern private bool HasPropertyImpl(int name);
        [NativeName("HasFloatFromScript")] extern private bool HasFloatImpl(int name);
        [NativeName("HasIntegerFromScript")] extern private bool HasIntImpl(int name);
        [NativeName("HasTextureFromScript")] extern private bool HasTextureImpl(int name);
        [NativeName("HasMatrixFromScript")] extern private bool HasMatrixImpl(int name);
        [NativeName("HasVectorFromScript")] extern private bool HasVectorImpl(int name);
        [NativeName("HasBufferFromScript")] extern private bool HasBufferImpl(int name);
        [NativeName("HasConstantBufferFromScript")] extern private bool HasConstantBufferImpl(int name);

        [NativeName("SetIntFromScript"), ThreadSafe]     extern private void SetIntImpl(int name, int value);
        [NativeName("SetFloatFromScript"), ThreadSafe]   extern private void SetFloatImpl(int name, float value);
        [NativeName("SetVectorFromScript"), ThreadSafe]  extern private void SetVectorImpl(int name, Vector4 value);
        [NativeName("SetColorFromScript"), ThreadSafe]   extern private void SetColorImpl(int name, Color value);
        [NativeName("SetMatrixFromScript"), ThreadSafe]  extern private void SetMatrixImpl(int name, Matrix4x4 value);
        [NativeName("SetTextureFromScript"), ThreadSafe] extern private void SetTextureImpl(int name, [NotNull] Texture value);
        [NativeName("SetRenderTextureFromScript"), ThreadSafe] extern private void SetRenderTextureImpl(int name, [NotNull] RenderTexture value, RenderTextureSubElement element);
        [NativeName("SetBufferFromScript"), ThreadSafe]  extern private void SetBufferImpl(int name, ComputeBuffer value);
        [NativeName("SetGraphicsBufferFromScript"), ThreadSafe]  extern private void SetGraphicsBufferImpl(int name, GraphicsBuffer value);
        [NativeName("SetConstantBufferFromScript"), ThreadSafe] extern private void SetConstantBufferImpl(int name, ComputeBuffer value, int offset, int size);
        [NativeName("SetConstantGraphicsBufferFromScript"), ThreadSafe] extern private void SetConstantGraphicsBufferImpl(int name, GraphicsBuffer value, int offset, int size);

        [NativeName("SetFloatArrayFromScript"), ThreadSafe]  extern private void SetFloatArrayImpl(int name, float[] values, int count);
        [NativeName("SetVectorArrayFromScript"), ThreadSafe] extern private void SetVectorArrayImpl(int name, Vector4[] values, int count);
        [NativeName("SetMatrixArrayFromScript"), ThreadSafe] extern private void SetMatrixArrayImpl(int name, Matrix4x4[] values, int count);

        [NativeName("GetFloatArrayFromScript"), ThreadSafe]  extern private float[]     GetFloatArrayImpl(int name);
        [NativeName("GetVectorArrayFromScript"), ThreadSafe] extern private Vector4[]   GetVectorArrayImpl(int name);
        [NativeName("GetMatrixArrayFromScript"), ThreadSafe] extern private Matrix4x4[] GetMatrixArrayImpl(int name);

        [NativeName("GetFloatArrayCountFromScript"), ThreadSafe]  extern private int GetFloatArrayCountImpl(int name);
        [NativeName("GetVectorArrayCountFromScript"), ThreadSafe] extern private int GetVectorArrayCountImpl(int name);
        [NativeName("GetMatrixArrayCountFromScript"), ThreadSafe] extern private int GetMatrixArrayCountImpl(int name);

        [NativeName("ExtractFloatArrayFromScript"), ThreadSafe]  extern private void ExtractFloatArrayImpl(int name, [Out] float[] val);
        [NativeName("ExtractVectorArrayFromScript"), ThreadSafe] extern private void ExtractVectorArrayImpl(int name, [Out] Vector4[] val);
        [NativeName("ExtractMatrixArrayFromScript"), ThreadSafe] extern private void ExtractMatrixArrayImpl(int name, [Out] Matrix4x4[] val);

        [FreeFunction("ConvertAndCopySHCoefficientArraysToPropertySheetFromScript"), ThreadSafe]
        extern internal static void Internal_CopySHCoefficientArraysFrom(MaterialPropertyBlock properties, SphericalHarmonicsL2[] lightProbes, int sourceStart, int destStart, int count);

        [FreeFunction("CopyProbeOcclusionArrayToPropertySheetFromScript"), ThreadSafe]
        extern internal static void Internal_CopyProbeOcclusionArrayFrom(MaterialPropertyBlock properties, Vector4[] occlusionProbes, int sourceStart, int destStart, int count);
    }

    public sealed partial class MaterialPropertyBlock
    {
        [NativeMethod(Name = "MaterialPropertyBlockScripting::Create", IsFreeFunction = true)]
        extern private static System.IntPtr CreateImpl();
        [NativeMethod(Name = "MaterialPropertyBlockScripting::Destroy", IsFreeFunction = true, IsThreadSafe = true)]
        extern private static void DestroyImpl(System.IntPtr mpb);

        extern public bool isEmpty {[NativeName("IsEmpty"), ThreadSafe] get; }

        [ThreadSafe]
        extern private void Clear(bool keepMemory);
        public void Clear() { Clear(true); }
    }
}
