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

//
// Shader
//

namespace UnityEngine
{
    internal enum DisableBatchingType
    {
        False,
        True,
        WhenLODFading
    }

    [NativeHeader("Runtime/Graphics/ShaderScriptBindings.h")]
    [NativeHeader("Runtime/Shaders/Shader.h")]
    [NativeHeader("Runtime/Shaders/ComputeShader.h")]
    [NativeHeader("Runtime/Shaders/ShaderNameRegistry.h")]
    [NativeHeader("Runtime/Shaders/GpuPrograms/ShaderVariantCollection.h")]
    [NativeHeader("Runtime/Misc/ResourceManager.h")]
    public sealed partial class Shader : Object
    {
        [FreeFunction("GetScriptMapper().FindShader")] extern public static Shader Find(string name);
        [FreeFunction("GetBuiltinResource<Shader>")] extern internal static Shader FindBuiltin(string name);

        [NativeProperty("MaximumShaderLOD")] extern public int maximumLOD { get; set; }
        [NativeProperty("GlobalMaximumShaderLOD")] extern public static int globalMaximumLOD { get; set; }
        extern public bool isSupported {[NativeMethod("IsSupported")] get; }
        extern public static string globalRenderPipeline { get; set; }

        [FreeFunction("ShaderScripting::EnableKeyword")]    extern public static void EnableKeyword(string keyword);
        [FreeFunction("ShaderScripting::DisableKeyword")]   extern public static void DisableKeyword(string keyword);
        [FreeFunction("ShaderScripting::IsKeywordEnabled")] extern public static bool IsKeywordEnabled(string keyword);

        extern public int renderQueue {[FreeFunction("ShaderScripting::GetRenderQueue", HasExplicitThis = true)] get; }
        extern internal DisableBatchingType disableBatching {[FreeFunction("ShaderScripting::GetDisableBatchingType", HasExplicitThis = true)] get; }

        [FreeFunction] extern public static void WarmupAllShaders();

        [FreeFunction("ShaderScripting::TagToID")] extern internal static int TagToID(string name);
        [FreeFunction("ShaderScripting::IDToTag")] extern internal static string IDToTag(int name);

        [FreeFunction(Name = "ShaderScripting::PropertyToID", IsThreadSafe = true)] extern public static int PropertyToID(string name);

        extern public Shader GetDependency(string name);

        extern public int passCount { [FreeFunction(Name = "ShaderScripting::GetPassCount", HasExplicitThis = true)] get; }

        public Rendering.ShaderTagId FindPassTagValue(int passIndex, Rendering.ShaderTagId tagName)
        {
            if (passIndex < 0 || passIndex >= passCount)
                throw new ArgumentOutOfRangeException("passIndex");
            var id = Internal_FindPassTagValue(passIndex, tagName.id);
            return new Rendering.ShaderTagId { id = id };
        }

        [FreeFunction(Name = "ShaderScripting::FindPassTagValue", HasExplicitThis = true)] extern private int Internal_FindPassTagValue(int passIndex, int tagName);

        [NativeProperty("CustomEditorName")] extern internal string customEditor { get; }
    }

    public sealed partial class Shader : Object
    {
        // TODO: get buffer is missing

        [FreeFunction("ShaderScripting::SetGlobalFloat")]   extern private static void SetGlobalFloatImpl(int name, float value);
        [FreeFunction("ShaderScripting::SetGlobalVector")]  extern private static void SetGlobalVectorImpl(int name, Vector4 value);
        [FreeFunction("ShaderScripting::SetGlobalMatrix")]  extern private static void SetGlobalMatrixImpl(int name, Matrix4x4 value);
        [FreeFunction("ShaderScripting::SetGlobalTexture")] extern private static void SetGlobalTextureImpl(int name, Texture value);
        [FreeFunction("ShaderScripting::SetGlobalRenderTexture")] extern private static void SetGlobalRenderTextureImpl(int name, RenderTexture value, Rendering.RenderTextureSubElement element);
        [FreeFunction("ShaderScripting::SetGlobalBuffer")]  extern private static void SetGlobalBufferImpl(int name, ComputeBuffer value);
        [FreeFunction("ShaderScripting::SetGlobalBuffer")]  extern private static void SetGlobalGraphicsBufferImpl(int name, GraphicsBuffer value);
        [FreeFunction("ShaderScripting::SetGlobalConstantBuffer")] extern private static void SetGlobalConstantBufferImpl(int name, ComputeBuffer value, int offset, int size);
        [FreeFunction("ShaderScripting::SetGlobalConstantBuffer")] extern private static void SetGlobalConstantGraphicsBufferImpl(int name, GraphicsBuffer value, int offset, int size);

        [FreeFunction("ShaderScripting::GetGlobalFloat")]   extern private static float     GetGlobalFloatImpl(int name);
        [FreeFunction("ShaderScripting::GetGlobalVector")]  extern private static Vector4   GetGlobalVectorImpl(int name);
        [FreeFunction("ShaderScripting::GetGlobalMatrix")]  extern private static Matrix4x4 GetGlobalMatrixImpl(int name);
        [FreeFunction("ShaderScripting::GetGlobalTexture")] extern private static Texture   GetGlobalTextureImpl(int name);

        [FreeFunction("ShaderScripting::SetGlobalFloatArray")]  extern private static void SetGlobalFloatArrayImpl(int name, float[] values, int count);
        [FreeFunction("ShaderScripting::SetGlobalVectorArray")] extern private static void SetGlobalVectorArrayImpl(int name, Vector4[] values, int count);
        [FreeFunction("ShaderScripting::SetGlobalMatrixArray")] extern private static void SetGlobalMatrixArrayImpl(int name, Matrix4x4[] values, int count);

        [FreeFunction("ShaderScripting::GetGlobalFloatArray")]  extern private static float[]     GetGlobalFloatArrayImpl(int name);
        [FreeFunction("ShaderScripting::GetGlobalVectorArray")] extern private static Vector4[]   GetGlobalVectorArrayImpl(int name);
        [FreeFunction("ShaderScripting::GetGlobalMatrixArray")] extern private static Matrix4x4[] GetGlobalMatrixArrayImpl(int name);

        [FreeFunction("ShaderScripting::GetGlobalFloatArrayCount")]  extern private static int GetGlobalFloatArrayCountImpl(int name);
        [FreeFunction("ShaderScripting::GetGlobalVectorArrayCount")] extern private static int GetGlobalVectorArrayCountImpl(int name);
        [FreeFunction("ShaderScripting::GetGlobalMatrixArrayCount")] extern private static int GetGlobalMatrixArrayCountImpl(int name);

        [FreeFunction("ShaderScripting::ExtractGlobalFloatArray")]  extern private static void ExtractGlobalFloatArrayImpl(int name, [Out] float[] val);
        [FreeFunction("ShaderScripting::ExtractGlobalVectorArray")] extern private static void ExtractGlobalVectorArrayImpl(int name, [Out] Vector4[] val);
        [FreeFunction("ShaderScripting::ExtractGlobalMatrixArray")] extern private static void ExtractGlobalMatrixArrayImpl(int name, [Out] Matrix4x4[] val);
    }
}

//
// Material
//

namespace UnityEngine
{
    [NativeHeader("Runtime/Graphics/ShaderScriptBindings.h")]
    [NativeHeader("Runtime/Shaders/Material.h")]
    public partial class Material : Object
    {
        [FreeFunction("MaterialScripting::CreateWithShader")]   extern private static void CreateWithShader([Writable] Material self, [NotNull] Shader shader);
        [FreeFunction("MaterialScripting::CreateWithMaterial")] extern private static void CreateWithMaterial([Writable] Material self, [NotNull] Material source);
        [FreeFunction("MaterialScripting::CreateWithString")]   extern private static void CreateWithString([Writable] Material self);

        public Material(Shader shader)   { CreateWithShader(this, shader); }
        // will otherwise be stripped if scene only uses default materials not explicitly referenced
        // (ie some components will get a default material if a material reference is null)
        [RequiredByNativeCode]
        public Material(Material source) { CreateWithMaterial(this, source); }

        // TODO: is it time to make it deprecated with error?
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Creating materials from shader source string is no longer supported. Use Shader assets instead.", false)]
        public Material(string contents) { CreateWithString(this); }

        static extern internal Material GetDefaultMaterial();
        static extern internal Material GetDefaultParticleMaterial();
        static extern internal Material GetDefaultLineMaterial();

        extern public Shader shader { get; set; }

        public Color color
        {
            get
            {
                // Try to find property with [MainColor] attribute and use that, otherwise fallback to old hardcoded one.
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainColor);
                if (nameId >= 0)
                    return GetColor(nameId);
                else
                    return GetColor("_Color");
            }
            set
            {
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainColor);
                if (nameId >= 0)
                    SetColor(nameId, value);
                else
                    SetColor("_Color", value);
            }
        }
        public Texture mainTexture
        {
            get
            {
                // Try to find property with [MainTexture] attribute and use that, otherwise fallback to old hardcoded one.
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainTexture);
                if (nameId >= 0)
                    return GetTexture(nameId);
                else
                    return GetTexture("_MainTex");
            }
            set
            {
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainTexture);
                if (nameId >= 0)
                    SetTexture(nameId, value);
                else
                    SetTexture("_MainTex", value);
            }
        }
        public Vector2 mainTextureOffset
        {
            get
            {
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainTexture);
                if (nameId >= 0)
                    return GetTextureOffset(nameId);
                else
                    return GetTextureOffset("_MainTex");
            }
            set
            {
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainTexture);
                if (nameId >= 0)
                    SetTextureOffset(nameId, value);
                else
                    SetTextureOffset("_MainTex", value);
            }
        }
        public Vector2 mainTextureScale
        {
            get
            {
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainTexture);
                if (nameId >= 0)
                    return GetTextureScale(nameId);
                else
                    return GetTextureScale("_MainTex");
            }
            set
            {
                int nameId = GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags.MainTexture);
                if (nameId >= 0)
                    SetTextureScale(nameId, value);
                else
                    SetTextureScale("_MainTex", value);
            }
        }
        [NativeName("GetFirstPropertyNameIdByAttributeFromScript")] extern private int GetFirstPropertyNameIdByAttribute(ShaderPropertyFlags attributeFlag);

        [NativeName("HasPropertyFromScript")] extern public bool HasProperty(int nameID);
        public bool HasProperty(string name) { return HasProperty(Shader.PropertyToID(name)); }

        extern public int renderQueue {[NativeName("GetActualRenderQueue")] get; [NativeName("SetCustomRenderQueue")] set; }
        extern internal int rawRenderQueue {[NativeName("GetCustomRenderQueue")] get; }

        extern public void EnableKeyword(string keyword);
        extern public void DisableKeyword(string keyword);
        extern public bool IsKeywordEnabled(string keyword);

        extern public MaterialGlobalIlluminationFlags globalIlluminationFlags { get; set; }
        extern public bool doubleSidedGI { get; set; }
        [NativeProperty("EnableInstancingVariants")] extern public bool enableInstancing { get; set; }

        extern public int passCount { get; }
        [FreeFunction("MaterialScripting::SetShaderPassEnabled", HasExplicitThis = true)] extern public void SetShaderPassEnabled(string passName, bool enabled);
        [FreeFunction("MaterialScripting::GetShaderPassEnabled", HasExplicitThis = true)] extern public bool GetShaderPassEnabled(string passName);
        extern public string GetPassName(int pass);
        extern public int FindPass(string passName);

        extern public void SetOverrideTag(string tag, string val);
        [NativeName("GetTag")] extern private string GetTagImpl(string tag, bool currentSubShaderOnly, string defaultValue);
        public string GetTag(string tag, bool searchFallbacks, string defaultValue) { return GetTagImpl(tag, !searchFallbacks, defaultValue); }
        public string GetTag(string tag, bool searchFallbacks) { return GetTagImpl(tag, !searchFallbacks, ""); }

        [NativeThrows]
        [FreeFunction("MaterialScripting::Lerp", HasExplicitThis = true)] extern public void Lerp(Material start, Material end, float t);
        [FreeFunction("MaterialScripting::SetPass", HasExplicitThis = true)] extern public bool SetPass(int pass);
        [FreeFunction("MaterialScripting::CopyPropertiesFrom", HasExplicitThis = true)] extern public void CopyPropertiesFromMaterial(Material mat);

        [FreeFunction("MaterialScripting::GetShaderKeywords", HasExplicitThis = true)] extern private string[] GetShaderKeywords();
        [FreeFunction("MaterialScripting::SetShaderKeywords", HasExplicitThis = true)] extern private void SetShaderKeywords(string[] names);
        public string[] shaderKeywords { get { return GetShaderKeywords(); } set { SetShaderKeywords(value); } }

        extern public int ComputeCRC();

        [FreeFunction("MaterialScripting::GetTexturePropertyNames", HasExplicitThis = true)]
        extern public String[] GetTexturePropertyNames();

        [FreeFunction("MaterialScripting::GetTexturePropertyNameIDs", HasExplicitThis = true)]
        extern public int[] GetTexturePropertyNameIDs();

        [FreeFunction("MaterialScripting::GetTexturePropertyNamesInternal", HasExplicitThis = true)]
        extern private void GetTexturePropertyNamesInternal(object outNames);

        [FreeFunction("MaterialScripting::GetTexturePropertyNameIDsInternal", HasExplicitThis = true)]
        extern private void GetTexturePropertyNameIDsInternal(object outNames);

        public void GetTexturePropertyNames(List<string> outNames)
        {
            if (outNames == null)
            {
                throw new ArgumentNullException(nameof(outNames));
            }

            GetTexturePropertyNamesInternal(outNames);
        }

        public void GetTexturePropertyNameIDs(List<int> outNames)
        {
            if (outNames == null)
            {
                throw new ArgumentNullException(nameof(outNames));
            }

            GetTexturePropertyNameIDsInternal(outNames);
        }

    }

    public partial class Material : Object
    {
        // TODO: get buffer is missing

        [NativeName("SetFloatFromScript")]   extern private void SetFloatImpl(int name, float value);
        [NativeName("SetColorFromScript")]   extern private void SetColorImpl(int name, Color value);
        [NativeName("SetMatrixFromScript")]  extern private void SetMatrixImpl(int name, Matrix4x4 value);
        [NativeName("SetTextureFromScript")] extern private void SetTextureImpl(int name, Texture value);
        [NativeName("SetRenderTextureFromScript")] extern private void SetRenderTextureImpl(int name, RenderTexture value, Rendering.RenderTextureSubElement element);
        [NativeName("SetBufferFromScript")]  extern private void SetBufferImpl(int name, ComputeBuffer value);
        [NativeName("SetGraphicsBufferFromScript")]  extern private void SetGraphicsBufferImpl(int name, GraphicsBuffer value);
        [NativeName("SetConstantBufferFromScript")] extern private void SetConstantBufferImpl(int name, ComputeBuffer value, int offset, int size);
        [NativeName("SetConstantGraphicsBufferFromScript")] extern private void SetConstantGraphicsBufferImpl(int name, GraphicsBuffer value, int offset, int size);

        [NativeName("GetFloatFromScript")]   extern private float     GetFloatImpl(int name);
        [NativeName("GetColorFromScript")]   extern private Color     GetColorImpl(int name);
        [NativeName("GetMatrixFromScript")]  extern private Matrix4x4 GetMatrixImpl(int name);
        [NativeName("GetTextureFromScript")] extern private Texture   GetTextureImpl(int name);

        [FreeFunction(Name = "MaterialScripting::SetFloatArray", HasExplicitThis = true)]  extern private void SetFloatArrayImpl(int name, float[] values, int count);
        [FreeFunction(Name = "MaterialScripting::SetVectorArray", HasExplicitThis = true)] extern private void SetVectorArrayImpl(int name, Vector4[] values, int count);
        [FreeFunction(Name = "MaterialScripting::SetColorArray", HasExplicitThis = true)]  extern private void SetColorArrayImpl(int name, Color[] values, int count);
        [FreeFunction(Name = "MaterialScripting::SetMatrixArray", HasExplicitThis = true)] extern private void SetMatrixArrayImpl(int name, Matrix4x4[] values, int count);

        [FreeFunction(Name = "MaterialScripting::GetFloatArray", HasExplicitThis = true)]  extern private float[]     GetFloatArrayImpl(int name);
        [FreeFunction(Name = "MaterialScripting::GetVectorArray", HasExplicitThis = true)] extern private Vector4[]   GetVectorArrayImpl(int name);
        [FreeFunction(Name = "MaterialScripting::GetColorArray", HasExplicitThis = true)]  extern private Color[]     GetColorArrayImpl(int name);
        [FreeFunction(Name = "MaterialScripting::GetMatrixArray", HasExplicitThis = true)] extern private Matrix4x4[] GetMatrixArrayImpl(int name);

        [FreeFunction(Name = "MaterialScripting::GetFloatArrayCount", HasExplicitThis = true)]  extern private int GetFloatArrayCountImpl(int name);
        [FreeFunction(Name = "MaterialScripting::GetVectorArrayCount", HasExplicitThis = true)] extern private int GetVectorArrayCountImpl(int name);
        [FreeFunction(Name = "MaterialScripting::GetColorArrayCount", HasExplicitThis = true)]  extern private int GetColorArrayCountImpl(int name);
        [FreeFunction(Name = "MaterialScripting::GetMatrixArrayCount", HasExplicitThis = true)] extern private int GetMatrixArrayCountImpl(int name);

        [FreeFunction(Name = "MaterialScripting::ExtractFloatArray", HasExplicitThis = true)]  extern private void ExtractFloatArrayImpl(int name, [Out] float[] val);
        [FreeFunction(Name = "MaterialScripting::ExtractVectorArray", HasExplicitThis = true)] extern private void ExtractVectorArrayImpl(int name, [Out] Vector4[] val);
        [FreeFunction(Name = "MaterialScripting::ExtractColorArray", HasExplicitThis = true)]  extern private void ExtractColorArrayImpl(int name, [Out] Color[] val);
        [FreeFunction(Name = "MaterialScripting::ExtractMatrixArray", HasExplicitThis = true)] extern private void ExtractMatrixArrayImpl(int name, [Out] Matrix4x4[] val);

        [NativeName("GetTextureScaleAndOffsetFromScript")] extern private Vector4 GetTextureScaleAndOffsetImpl(int name);
        [NativeName("SetTextureOffsetFromScript")] extern private void SetTextureOffsetImpl(int name, Vector2 offset);
        [NativeName("SetTextureScaleFromScript")]  extern private void SetTextureScaleImpl(int name, Vector2 scale);
    }
}

//
// MaterialPropertyBlock
//

namespace UnityEngine
{
    [NativeHeader("Runtime/Graphics/ShaderScriptBindings.h")]
    [NativeHeader("Runtime/Shaders/ShaderPropertySheet.h")]
    [NativeHeader("Runtime/Shaders/ComputeShader.h")]
    [NativeHeader("Runtime/Math/SphericalHarmonicsL2.h")]
    public sealed partial class MaterialPropertyBlock
    {
        // TODO: get buffer is missing

        [NativeName("GetFloatFromScript"), ThreadSafe]   extern private float     GetFloatImpl(int name);
        [NativeName("GetVectorFromScript"), ThreadSafe]  extern private Vector4   GetVectorImpl(int name);
        [NativeName("GetColorFromScript"), ThreadSafe]   extern private Color     GetColorImpl(int name);
        [NativeName("GetMatrixFromScript"), ThreadSafe]  extern private Matrix4x4 GetMatrixImpl(int name);
        [NativeName("GetTextureFromScript"), ThreadSafe] extern private Texture   GetTextureImpl(int name);

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

//
// ShaderVariantCollection
//

namespace UnityEngine
{
    public sealed partial class ShaderVariantCollection : Object
    {
        public partial struct ShaderVariant
        {
            [FreeFunction][NativeConditional("UNITY_EDITOR")]
            extern private static string CheckShaderVariant(Shader shader, UnityEngine.Rendering.PassType passType, string[] keywords);
        }
    }

    public sealed partial class ShaderVariantCollection : Object
    {
        extern public int  shaderCount  { get; }
        extern public int  variantCount { get; }
        extern public bool isWarmedUp   {[NativeName("IsWarmedUp")] get; }

        extern private bool AddVariant(Shader shader, UnityEngine.Rendering.PassType passType, string[] keywords);
        extern private bool RemoveVariant(Shader shader, UnityEngine.Rendering.PassType passType, string[] keywords);
        extern private bool ContainsVariant(Shader shader, UnityEngine.Rendering.PassType passType, string[] keywords);

        [NativeName("ClearVariants")] extern public void Clear();
        [NativeName("WarmupShaders")] extern public void WarmUp();

        [NativeName("CreateFromScript")] extern private static void Internal_Create([Writable] ShaderVariantCollection svc);
    }
}

//
// ComputeShader
//

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

        [FreeFunction("ComputeShaderScripting::EnableKeyword", HasExplicitThis = true)]
        extern public void EnableKeyword(string keyword);
        [FreeFunction("ComputeShaderScripting::DisableKeyword", HasExplicitThis = true)]
        extern public void DisableKeyword(string keyword);
        [FreeFunction("ComputeShaderScripting::IsKeywordEnabled", HasExplicitThis = true)]
        extern public bool IsKeywordEnabled(string keyword);

        [FreeFunction("ComputeShaderScripting::GetShaderKeywords", HasExplicitThis = true)] extern private string[] GetShaderKeywords();
        [FreeFunction("ComputeShaderScripting::SetShaderKeywords", HasExplicitThis = true)] extern private void SetShaderKeywords(string[] names);
        public string[] shaderKeywords { get { return GetShaderKeywords(); } set { SetShaderKeywords(value); } }
    }
}

//
// RayTracingShader
//
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

        [NativeMethod(Name = "RayTracingShaderScripting::SetBuffer", HasExplicitThis = true, IsFreeFunction = true, ThrowsException = true)]
        extern private void SetGraphicsBuffer(int nameID, [NotNull] GraphicsBuffer buffer);

        [FreeFunction(Name = "RayTracingShaderScripting::SetConstantBuffer", HasExplicitThis = true)]
        extern private void SetConstantComputeBuffer(int nameID, [NotNull] ComputeBuffer buffer, int offset, int size);

        [FreeFunction(Name = "RayTracingShaderScripting::SetConstantBuffer", HasExplicitThis = true)]
        extern private void SetConstantGraphicsBuffer(int nameID, [NotNull] GraphicsBuffer buffer, int offset, int size);

        [NativeMethod(Name = "RayTracingShaderScripting::SetAccelerationStructure", HasExplicitThis = true, IsFreeFunction = true, ThrowsException = true)]
        extern public void SetAccelerationStructure(int nameID, [NotNull] RayTracingAccelerationStructure accelerationStructure);
        extern public void SetShaderPass(string passName);

        [NativeMethod(Name = "RayTracingShaderScripting::SetTextureFromGlobal", HasExplicitThis = true, IsFreeFunction = true, ThrowsException = true)]
        extern public void SetTextureFromGlobal(int nameID, int globalTextureNameID);

        [NativeName("DispatchRays")]
        extern public void Dispatch(string rayGenFunctionName, int width, int height, int depth, Camera camera = null);

        public void SetBuffer(int nameID, GraphicsBuffer buffer)
        {
            SetGraphicsBuffer(nameID, buffer);
        }
    }
}
