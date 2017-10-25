// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PassType = UnityEngine.Rendering.PassType;

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

        private static void SetGlobalIntImpl(int name, int value)       { SetGlobalFloatImpl(name, (float)value); }
        private static void SetGlobalColorImpl(int name, Color value)   { SetGlobalVectorImpl(name, (Vector4)value); }

        private static void SetGlobalValueImpl(int name, object value, Type t)
        {
            if (t == typeof(float))              SetGlobalFloatImpl(name, (float)value);
            else if (t == typeof(int))           SetGlobalIntImpl(name, (int)value);
            else if (t == typeof(Color))         SetGlobalColorImpl(name, (Color)value);
            else if (t == typeof(Vector4))       SetGlobalVectorImpl(name, (Vector4)value);
            else if (t == typeof(Matrix4x4))     SetGlobalMatrixImpl(name, (Matrix4x4)value);
            else if (t == typeof(Texture))       SetGlobalTextureImpl(name, (Texture)value);
            else if (t == typeof(ComputeBuffer)) SetGlobalBufferImpl(name, (ComputeBuffer)value);
            else throw new ArgumentException("Unsupported type for value");
        }

        [FreeFunction("ShaderScripting::GetGlobalFloat")]   extern private static float     GetGlobalFloatImpl(int name);
        [FreeFunction("ShaderScripting::GetGlobalVector")]  extern private static Vector4   GetGlobalVectorImpl(int name);
        [FreeFunction("ShaderScripting::GetGlobalMatrix")]  extern private static Matrix4x4 GetGlobalMatrixImpl(int name);
        [FreeFunction("ShaderScripting::GetGlobalTexture")] extern private static Texture   GetGlobalTextureImpl(int name);

        private static int   GetGlobalIntImpl(int name)     { return (int)GetGlobalFloatImpl(name); }
        private static Color GetGlobalColorImpl(int name)   { return (Color)GetGlobalVectorImpl(name); }

        private static object GetGlobalValueImpl(int name, Type t)
        {
            if (t == typeof(float))          return GetGlobalFloatImpl(name);
            else if (t == typeof(int))       return GetGlobalIntImpl(name);
            else if (t == typeof(Color))     return GetGlobalColorImpl(name);
            else if (t == typeof(Vector4))   return GetGlobalVectorImpl(name);
            else if (t == typeof(Matrix4x4)) return GetGlobalMatrixImpl(name);
            else if (t == typeof(Texture))   return GetGlobalTextureImpl(name);
            else throw new ArgumentException("Unsupported type for value");
        }

        [FreeFunction("ShaderScripting::SetGlobalFloatArray")]  extern private static void SetGlobalFloatArrayImpl(int name, float[] values, int count);
        [FreeFunction("ShaderScripting::SetGlobalVectorArray")] extern private static void SetGlobalVectorArrayImpl(int name, Vector4[] values, int count);
        [FreeFunction("ShaderScripting::SetGlobalMatrixArray")] extern private static void SetGlobalMatrixArrayImpl(int name, Matrix4x4[] values, int count);

        internal static void SetGlobalValueArrayImpl(int name, System.Array values, int count, Type t)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Length == 0) throw new ArgumentException("Zero-sized array is not allowed.");
            if (values.Length < count) throw new ArgumentException("array has less elements than passed count.");

            if (t == typeof(float))          SetGlobalFloatArrayImpl(name, (float[])values, count);
            else if (t == typeof(Vector4))   SetGlobalVectorArrayImpl(name, (Vector4[])values, count);
            else if (t == typeof(Matrix4x4)) SetGlobalMatrixArrayImpl(name, (Matrix4x4[])values, count);
            else throw new ArgumentException("Unsupported type for value");
        }

        [FreeFunction("ShaderScripting::GetGlobalFloatArray")]  extern private static float[]     GetGlobalFloatArrayImpl(int name);
        [FreeFunction("ShaderScripting::GetGlobalVectorArray")] extern private static Vector4[]   GetGlobalVectorArrayImpl(int name);
        [FreeFunction("ShaderScripting::GetGlobalMatrixArray")] extern private static Matrix4x4[] GetGlobalMatrixArrayImpl(int name);

        private static System.Array GetGlobalValueArrayImpl(int name, Type t)
        {
            if (t == typeof(float))          return GetGlobalFloatArrayImpl(name);
            else if (t == typeof(Vector4))   return GetGlobalVectorArrayImpl(name);
            else if (t == typeof(Matrix4x4)) return GetGlobalMatrixArrayImpl(name);
            else throw new ArgumentException("Unsupported type for value");
        }

        [FreeFunction("ShaderScripting::GetGlobalFloatArrayCount")]  extern private static int GetGlobalFloatArrayCountImpl(int name);
        [FreeFunction("ShaderScripting::GetGlobalVectorArrayCount")] extern private static int GetGlobalVectorArrayCountImpl(int name);
        [FreeFunction("ShaderScripting::GetGlobalMatrixArrayCount")] extern private static int GetGlobalMatrixArrayCountImpl(int name);

        private static int GetGlobalValueArrayCountImpl(int name, Type t)
        {
            if (t == typeof(float))          return GetGlobalFloatArrayCountImpl(name);
            else if (t == typeof(Vector4))   return GetGlobalVectorArrayCountImpl(name);
            else if (t == typeof(Matrix4x4)) return GetGlobalMatrixArrayCountImpl(name);
            else throw new ArgumentException("Unsupported type for value");
        }

        [FreeFunction("ShaderScripting::ExtractGlobalFloatArray")]  extern private static void ExtractGlobalFloatArrayImpl(int name, [Out] float[] val);
        [FreeFunction("ShaderScripting::ExtractGlobalVectorArray")] extern private static void ExtractGlobalVectorArrayImpl(int name, [Out] Vector4[] val);
        [FreeFunction("ShaderScripting::ExtractGlobalMatrixArray")] extern private static void ExtractGlobalMatrixArrayImpl(int name, [Out] Matrix4x4[] val);

        private static void ExtractGlobalValueArrayImpl(int name, System.Array values, Type t)
        {
            if (t == typeof(float))          ExtractGlobalFloatArrayImpl(name,  (float[])values);
            else if (t == typeof(Vector4))   ExtractGlobalVectorArrayImpl(name, (Vector4[])values);
            else if (t == typeof(Matrix4x4)) ExtractGlobalMatrixArrayImpl(name, (Matrix4x4[])values);
            else throw new ArgumentException("Unsupported type for value");
        }

    }


    [NativeHeader("Runtime/Graphics/GraphicsScriptBindings.h")]
    [NativeHeader("Runtime/Shaders/Material.h")]
    public partial class Material : Object
    {
        [FreeFunction("MaterialScripting::CreateWithShader")]   extern private static void CreateWithShader([Writable] Material self, Shader src);
        [FreeFunction("MaterialScripting::CreateWithMaterial")] extern private static void CreateWithMaterial([Writable] Material self, Material src);
        [FreeFunction("MaterialScripting::CreateWithString")]   extern private static void CreateWithString([Writable] Material self);

        public Material(Shader shader)   { CreateWithShader(this, shader); }
        public Material(Material source) { CreateWithMaterial(this, source); }

        // TODO: is it time to make it deprecated with error?
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Creating materials from shader source string is no longer supported. Use Shader assets instead.", false)]
        public Material(string contents) { CreateWithString(this); }


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
        private void SetIntImpl(int name, int value)        { SetFloatImpl(name, (float)value); }
        private void SetVectorImpl(int name, Vector4 value) { SetColorImpl(name, (Color)value); }

        private void SetValueImpl(int name, object value, Type t)
        {
            if (t == typeof(float))              SetFloatImpl(name, (float)value);
            else if (t == typeof(int))           SetIntImpl(name, (int)value);
            else if (t == typeof(Color))         SetColorImpl(name, (Color)value);
            else if (t == typeof(Vector4))       SetVectorImpl(name, (Vector4)value);
            else if (t == typeof(Matrix4x4))     SetMatrixImpl(name, (Matrix4x4)value);
            else if (t == typeof(Texture))       SetTextureImpl(name, (Texture)value);
            else if (t == typeof(ComputeBuffer)) SetBufferImpl(name, (ComputeBuffer)value);
            else throw new ArgumentException("Unsupported type for value");
        }

        [NativeName("GetFloatFromScript")]   extern private float     GetFloatImpl(int name);
        [NativeName("GetColorFromScript")]   extern private Color     GetColorImpl(int name);
        [NativeName("GetMatrixFromScript")]  extern private Matrix4x4 GetMatrixImpl(int name);
        [NativeName("GetTextureFromScript")] extern private Texture   GetTextureImpl(int name);
        private int     GetIntImpl(int name)    { return (int)GetFloatImpl(name); }
        private Vector4 GetVectorImpl(int name) { return (Vector4)GetColorImpl(name); }

        private object GetValueImpl(int name, Type t)
        {
            if (t == typeof(float))          return GetFloatImpl(name);
            else if (t == typeof(int))       return GetIntImpl(name);
            else if (t == typeof(Color))     return GetColorImpl(name);
            else if (t == typeof(Vector4))   return GetVectorImpl(name);
            else if (t == typeof(Matrix4x4)) return GetMatrixImpl(name);
            else if (t == typeof(Texture))   return GetTextureImpl(name);
            else throw new ArgumentException("Unsupported type for value");
        }

        [FreeFunction(Name = "MaterialScripting::SetFloatArray", HasExplicitThis = true)]  extern private void SetFloatArrayImpl(int name, float[] values, int count);
        [FreeFunction(Name = "MaterialScripting::SetVectorArray", HasExplicitThis = true)] extern private void SetVectorArrayImpl(int name, Vector4[] values, int count);
        [FreeFunction(Name = "MaterialScripting::SetColorArray", HasExplicitThis = true)]  extern private void SetColorArrayImpl(int name, Color[] values, int count);
        [FreeFunction(Name = "MaterialScripting::SetMatrixArray", HasExplicitThis = true)] extern private void SetMatrixArrayImpl(int name, Matrix4x4[] values, int count);

        private void SetValueArrayImpl(int name, System.Array values, int count, Type t)
        {
            if (values == null) throw new ArgumentNullException("values");
            if (values.Length == 0) throw new ArgumentException("Zero-sized array is not allowed.");
            if (values.Length < count) throw new ArgumentException("array has less elements than passed count.");

            if (t == typeof(float))          SetFloatArrayImpl(name, (float[])values, count);
            else if (t == typeof(Color))     SetColorArrayImpl(name, (Color[])values, count);
            else if (t == typeof(Vector4))   SetVectorArrayImpl(name, (Vector4[])values, count);
            else if (t == typeof(Matrix4x4)) SetMatrixArrayImpl(name, (Matrix4x4[])values, count);
            else throw new ArgumentException("Unsupported type for value");
        }

        [FreeFunction(Name = "MaterialScripting::GetFloatArray", HasExplicitThis = true)]  extern private float[]     GetFloatArrayImpl(int name);
        [FreeFunction(Name = "MaterialScripting::GetVectorArray", HasExplicitThis = true)] extern private Vector4[]   GetVectorArrayImpl(int name);
        [FreeFunction(Name = "MaterialScripting::GetColorArray", HasExplicitThis = true)]  extern private Color[]     GetColorArrayImpl(int name);
        [FreeFunction(Name = "MaterialScripting::GetMatrixArray", HasExplicitThis = true)] extern private Matrix4x4[] GetMatrixArrayImpl(int name);

        private System.Array GetValueArrayImpl(int name, Type t)
        {
            if (t == typeof(float))          return GetFloatArrayImpl(name);
            else if (t == typeof(Color))     return GetColorArrayImpl(name);
            else if (t == typeof(Vector4))   return GetVectorArrayImpl(name);
            else if (t == typeof(Matrix4x4)) return GetMatrixArrayImpl(name);
            else throw new ArgumentException("Unsupported type for value");
        }

        [FreeFunction(Name = "MaterialScripting::GetFloatArrayCount", HasExplicitThis = true)]  extern private int GetFloatArrayCountImpl(int name);
        [FreeFunction(Name = "MaterialScripting::GetVectorArrayCount", HasExplicitThis = true)] extern private int GetVectorArrayCountImpl(int name);
        [FreeFunction(Name = "MaterialScripting::GetMatrixArrayCount", HasExplicitThis = true)] extern private int GetMatrixArrayCountImpl(int name);

        private int GetValueArrayCountImpl(int name, Type t)
        {
            if (t == typeof(float))          return GetFloatArrayCountImpl(name);
            else if (t == typeof(Color))     return GetVectorArrayCountImpl(name);
            else if (t == typeof(Vector4))   return GetVectorArrayCountImpl(name);
            else if (t == typeof(Matrix4x4)) return GetMatrixArrayCountImpl(name);
            else throw new ArgumentException("Unsupported type for value");
        }

        [FreeFunction(Name = "MaterialScripting::ExtractFloatArray", HasExplicitThis = true)]  extern private void ExtractFloatArrayImpl(int name, [Out] float[] val);
        [FreeFunction(Name = "MaterialScripting::ExtractVectorArray", HasExplicitThis = true)] extern private void ExtractVectorArrayImpl(int name, [Out] Vector4[] val);
        [FreeFunction(Name = "MaterialScripting::ExtractColorArray", HasExplicitThis = true)]  extern private void ExtractColorArrayImpl(int name, [Out] Color[] val);
        [FreeFunction(Name = "MaterialScripting::ExtractMatrixArray", HasExplicitThis = true)] extern private void ExtractMatrixArrayImpl(int name, [Out] Matrix4x4[] val);

        private void ExtractValueArrayImpl(int name, System.Array values, Type t)
        {
            if (t == typeof(float))          ExtractFloatArrayImpl(name,  (float[])values);
            else if (t == typeof(Color))     ExtractColorArrayImpl(name, (Color[])values);
            else if (t == typeof(Vector4))   ExtractVectorArrayImpl(name, (Vector4[])values);
            else if (t == typeof(Matrix4x4)) ExtractMatrixArrayImpl(name, (Matrix4x4[])values);
            else throw new ArgumentException("Unsupported type for value");
        }


        [NativeName("GetTextureScaleAndOffsetFromScript")] extern private Vector4 GetTextureScaleAndOffsetImpl(int name);
        [NativeName("SetTextureOffsetFromScript")] extern private void SetTextureOffsetImpl(int name, Vector2 offset);
        [NativeName("SetTextureScaleFromScript")]  extern private void SetTextureScaleImpl(int name, Vector2 scale);
    }

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
