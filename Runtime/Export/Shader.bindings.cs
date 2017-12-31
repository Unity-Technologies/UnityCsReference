// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
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

    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
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
    }

    public sealed partial class Shader : Object
    {
        // TODO: get buffer is missing

        [FreeFunction("ShaderScripting::SetGlobalFloat")]   extern private static void SetGlobalFloatImpl(int name, float value);
        [FreeFunction("ShaderScripting::SetGlobalVector")]  extern private static void SetGlobalVectorImpl(int name, Vector4 value);
        [FreeFunction("ShaderScripting::SetGlobalMatrix")]  extern private static void SetGlobalMatrixImpl(int name, Matrix4x4 value);
        [FreeFunction("ShaderScripting::SetGlobalTexture")] extern private static void SetGlobalTextureImpl(int name, Texture value);
        [FreeFunction("ShaderScripting::SetGlobalBuffer")]  extern private static void SetGlobalBufferImpl(int name, ComputeBuffer value);

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
    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    [NativeHeader("Runtime/Shaders/Material.h")]
    public partial class Material : Object
    {
        [FreeFunction("MaterialScripting::CreateWithShader")]   extern private static void CreateWithShader([Writable] Material self, [NotNull] Shader shader);
        [FreeFunction("MaterialScripting::CreateWithMaterial")] extern private static void CreateWithMaterial([Writable] Material self, [NotNull] Material source);
        [FreeFunction("MaterialScripting::CreateWithString")]   extern private static void CreateWithString([Writable] Material self);

        public Material(Shader shader)   { CreateWithShader(this, shader); }
        public Material(Material source) { CreateWithMaterial(this, source); }

        // TODO: is it time to make it deprecated with error?
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Creating materials from shader source string is no longer supported. Use Shader assets instead.", false)]
        public Material(string contents) { CreateWithString(this); }

        static extern internal Material GetDefaultMaterial();
        static extern internal Material GetDefaultParticleMaterial();
        static extern internal Material GetDefaultLineMaterial();

        extern public Shader shader { get; set; }

        public Color color { get { return GetColor("_Color"); } set { SetColor("_Color", value); } }
        public Texture mainTexture       { get { return GetTexture("_MainTex"); }       set { SetTexture("_MainTex", value); } }
        public Vector2 mainTextureOffset { get { return GetTextureOffset("_MainTex"); } set { SetTextureOffset("_MainTex", value); } }
        public Vector2 mainTextureScale  { get { return GetTextureScale("_MainTex"); }  set { SetTextureScale("_MainTex", value); } }

        [NativeName("HasPropertyFromScript")] extern public bool HasProperty(int name);
        public bool HasProperty(string name) { return HasProperty(Shader.PropertyToID(name)); }

        extern public int renderQueue {[NativeName("GetActualRenderQueue")] get; [NativeName("SetCustomRenderQueue")] set; }

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
    }

    public partial class Material : Object
    {
        // TODO: get buffer is missing

        [NativeName("SetFloatFromScript")]   extern private void SetFloatImpl(int name, float value);
        [NativeName("SetColorFromScript")]   extern private void SetColorImpl(int name, Color value);
        [NativeName("SetMatrixFromScript")]  extern private void SetMatrixImpl(int name, Matrix4x4 value);
        [NativeName("SetTextureFromScript")] extern private void SetTextureImpl(int name, Texture value);
        [NativeName("SetBufferFromScript")]  extern private void SetBufferImpl(int name, ComputeBuffer value);

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
    [NativeHeader("Runtime/Shaders/ShaderPropertySheet.h")]
    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    [NativeHeader("Runtime/Shaders/ComputeShader.h")]
    [NativeHeader("Runtime/Math/SphericalHarmonicsL2.h")]
    public sealed partial class MaterialPropertyBlock
    {
        // TODO: set int is missing
        // TODO: get int/color/buffer is missing

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
        [NativeName("SetBufferFromScript")]  extern private void SetBufferImpl(int name, ComputeBuffer value);

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
    }

    public sealed partial class MaterialPropertyBlock
    {
        [NativeMethod(Name = "MaterialPropertyBlockScripting::Create", IsFreeFunction = true)]
        extern private static System.IntPtr CreateImpl();
        [NativeMethod(Name = "MaterialPropertyBlockScripting::Destroy", IsFreeFunction = true, IsThreadSafe = true)]
        extern private static void DestroyImpl(System.IntPtr mpb);

        extern public bool isEmpty {[NativeName("IsEmpty")] get; }

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
