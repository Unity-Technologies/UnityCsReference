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

        [NativeMethod("GetIntFromScript", IsThreadSafe = true)]     extern private int       GetIntImpl(int name);
        [NativeMethod("GetFloatFromScript", IsThreadSafe = true)]   extern private float     GetFloatImpl(int name);
        [NativeMethod("GetVectorFromScript", IsThreadSafe = true)]  extern private Vector4   GetVectorImpl(int name);
        [NativeMethod("GetColorFromScript", IsThreadSafe = true)]   extern private Color     GetColorImpl(int name);
        [NativeMethod("GetMatrixFromScript", IsThreadSafe = true)]  extern private Matrix4x4 GetMatrixImpl(int name);
        [NativeMethod("GetTextureFromScript", IsThreadSafe = true)] extern private Texture   GetTextureImpl(int name);

        [NativeMethod("HasPropertyFromScript")] extern private bool HasPropertyImpl(int name);
        [NativeMethod("HasFloatFromScript")] extern private bool HasFloatImpl(int name);
        [NativeMethod("HasIntegerFromScript")] extern private bool HasIntImpl(int name);
        [NativeMethod("HasTextureFromScript")] extern private bool HasTextureImpl(int name);
        [NativeMethod("HasMatrixFromScript")] extern private bool HasMatrixImpl(int name);
        [NativeMethod("HasVectorFromScript")] extern private bool HasVectorImpl(int name);
        [NativeMethod("HasBufferFromScript")] extern private bool HasBufferImpl(int name);
        [NativeMethod("HasConstantBufferFromScript")] extern private bool HasConstantBufferImpl(int name);

        [NativeMethod("SetIntFromScript", IsThreadSafe = true)]     extern private void SetIntImpl(int name, int value);
        [NativeMethod("SetFloatFromScript", IsThreadSafe = true)]   extern private void SetFloatImpl(int name, float value);
        [NativeMethod("SetVectorFromScript", IsThreadSafe = true)]  extern private void SetVectorImpl(int name, Vector4 value);
        [NativeMethod("SetColorFromScript", IsThreadSafe = true)]   extern private void SetColorImpl(int name, Color value);
        [NativeMethod("SetMatrixFromScript", IsThreadSafe = true)]  extern private void SetMatrixImpl(int name, Matrix4x4 value);
        [NativeMethod("SetTextureFromScript", IsThreadSafe = true)] extern private void SetTextureImpl(int name, [NotNull] Texture value);
        [NativeMethod("SetRenderTextureFromScript", IsThreadSafe = true)] extern private void SetRenderTextureImpl(int name, [NotNull] RenderTexture value, RenderTextureSubElement element);
        [NativeMethod("SetBufferFromScript", IsThreadSafe = true)]  extern private void SetBufferImpl(int name, ComputeBuffer value);
        [NativeMethod("SetBufferFromScript", IsThreadSafe = true)]  extern private void SetGraphicsBufferImpl(int name, GraphicsBuffer value);
        [NativeMethod("SetConstantBufferFromScript", IsThreadSafe = true)] extern private void SetConstantBufferImpl(int name, ComputeBuffer value, int offset, int size);
        [NativeMethod("SetConstantBufferFromScript", IsThreadSafe = true)] extern private void SetConstantGraphicsBufferImpl(int name, GraphicsBuffer value, int offset, int size);

        [NativeMethod("SetFloatArrayFromScript", IsThreadSafe = true)]  extern private void SetFloatArrayImpl(int name, float[] values, int count);
        [NativeMethod("SetVectorArrayFromScript", IsThreadSafe = true)] extern private void SetVectorArrayImpl(int name, Vector4[] values, int count);
        [NativeMethod("SetMatrixArrayFromScript", IsThreadSafe = true)] extern private void SetMatrixArrayImpl(int name, Matrix4x4[] values, int count);

        [NativeMethod("GetFloatArrayFromScript", IsThreadSafe = true)]  extern private float[]     GetFloatArrayImpl(int name);
        [NativeMethod("GetVectorArrayFromScript", IsThreadSafe = true)] extern private Vector4[]   GetVectorArrayImpl(int name);
        [NativeMethod("GetMatrixArrayFromScript", IsThreadSafe = true)] extern private Matrix4x4[] GetMatrixArrayImpl(int name);

        [NativeMethod("GetFloatArrayCountFromScript", IsThreadSafe = true)]  extern private int GetFloatArrayCountImpl(int name);
        [NativeMethod("GetVectorArrayCountFromScript", IsThreadSafe = true)] extern private int GetVectorArrayCountImpl(int name);
        [NativeMethod("GetMatrixArrayCountFromScript", IsThreadSafe = true)] extern private int GetMatrixArrayCountImpl(int name);

        [NativeMethod("ExtractFloatArrayFromScript", IsThreadSafe = true)]  extern private void ExtractFloatArrayImpl(int name, [Out] float[] val);
        [NativeMethod("ExtractVectorArrayFromScript", IsThreadSafe = true)] extern private void ExtractVectorArrayImpl(int name, [Out] Vector4[] val);
        [NativeMethod("ExtractMatrixArrayFromScript", IsThreadSafe = true)] extern private void ExtractMatrixArrayImpl(int name, [Out] Matrix4x4[] val);

        [FreeFunction("ConvertAndCopySHCoefficientArraysToPropertySheetFromScript" ,IsThreadSafe = true)]
        extern internal static void Internal_CopySHCoefficientArraysFrom(MaterialPropertyBlock properties, SphericalHarmonicsL2[] lightProbes, int sourceStart, int destStart, int count);

        [FreeFunction("CopyProbeOcclusionArrayToPropertySheetFromScript", IsThreadSafe = true)]
        extern internal static void Internal_CopyProbeOcclusionArrayFrom(MaterialPropertyBlock properties, Vector4[] occlusionProbes, int sourceStart, int destStart, int count);

        [NativeMethod(Name = "MaterialPropertyBlockScripting::Create", IsFreeFunction = true)]
        extern private static System.IntPtr CreateImpl();
        [NativeMethod(Name = "MaterialPropertyBlockScripting::Destroy", IsFreeFunction = true, IsThreadSafe = true)]
        extern private static void DestroyImpl(System.IntPtr mpb);

        extern public bool isEmpty {[NativeMethod("IsEmpty", IsThreadSafe = true)] get; }

        [NativeMethod(IsThreadSafe = true)]
        extern private void Clear(bool keepMemory);
        public void Clear() { Clear(true); }

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(MaterialPropertyBlock materialPropertyBlock) => materialPropertyBlock.m_Ptr;
        }
    }
}
