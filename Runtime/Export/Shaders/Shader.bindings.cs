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
    [NativeHeader("Runtime/Shaders/Keywords/KeywordSpaceScriptBindings.h")]
    [NativeHeader("Runtime/Misc/ResourceManager.h")]
    public sealed partial class Shader : Object
    {
        public static Shader Find(string name) => ResourcesAPI.ActiveAPI.FindShaderByName(name);
        [FreeFunction("GetBuiltinResource<Shader>")] extern internal static Shader FindBuiltin(string name);

        [NativeProperty("MaxChunksRuntimeOverride")] extern public static int maximumChunksOverride { get; set; }

        [NativeProperty("MaximumShaderLOD")] extern public int maximumLOD { get; set; }
        [NativeProperty("GlobalMaximumShaderLOD")] extern public static int globalMaximumLOD { get; set; }
        extern public bool isSupported {[NativeMethod("IsSupported")] get; }
        extern public static string globalRenderPipeline { get; set; }

        public static GlobalKeyword[] enabledGlobalKeywords { get { return GetEnabledGlobalKeywords(); } }
        public static GlobalKeyword[] globalKeywords { get { return GetAllGlobalKeywords(); } }
        extern public LocalKeywordSpace keywordSpace { get; }

        [FreeFunction("keywords::GetEnabledGlobalKeywords")] extern internal static GlobalKeyword[] GetEnabledGlobalKeywords();
        [FreeFunction("keywords::GetAllGlobalKeywords")] extern internal static GlobalKeyword[] GetAllGlobalKeywords();

        [FreeFunction("ShaderScripting::EnableKeyword")]    extern public static void EnableKeyword(string keyword);
        [FreeFunction("ShaderScripting::DisableKeyword")]   extern public static void DisableKeyword(string keyword);
        [FreeFunction("ShaderScripting::IsKeywordEnabled")] extern public static bool IsKeywordEnabled(string keyword);

        [FreeFunction("ShaderScripting::EnableKeyword")]    extern internal static void EnableKeywordFast(GlobalKeyword keyword);
        [FreeFunction("ShaderScripting::DisableKeyword")]   extern internal static void DisableKeywordFast(GlobalKeyword keyword);
        [FreeFunction("ShaderScripting::SetKeyword")]       extern internal static void SetKeywordFast(GlobalKeyword keyword, bool value);
        [FreeFunction("ShaderScripting::IsKeywordEnabled")] extern internal static bool IsKeywordEnabledFast(GlobalKeyword keyword);

        public static void EnableKeyword(in GlobalKeyword keyword)          { EnableKeywordFast(keyword); }
        public static void DisableKeyword(in GlobalKeyword keyword)         { DisableKeywordFast(keyword); }
        public static void SetKeyword(in GlobalKeyword keyword, bool value) { SetKeywordFast(keyword, value); }
        public static bool IsKeywordEnabled(in GlobalKeyword keyword)       { return IsKeywordEnabledFast(keyword); }

        extern public int renderQueue {[FreeFunction("ShaderScripting::GetRenderQueue", HasExplicitThis = true)] get; }
        extern internal DisableBatchingType disableBatching {[FreeFunction("ShaderScripting::GetDisableBatchingType", HasExplicitThis = true)] get; }

        [FreeFunction] extern public static void WarmupAllShaders();

        [FreeFunction("ShaderScripting::TagToID")] extern internal static int TagToID(string name);
        [FreeFunction("ShaderScripting::IDToTag")] extern internal static string IDToTag(int name);

        [FreeFunction(Name = "ShaderScripting::PropertyToID", IsThreadSafe = true)] extern public static int PropertyToID(string name);

        extern public Shader GetDependency(string name);

        extern public int passCount { [FreeFunction(Name = "ShaderScripting::GetPassCount", HasExplicitThis = true)] get; }
        extern public int subshaderCount { [FreeFunction(Name = "ShaderScripting::GetSubshaderCount", HasExplicitThis = true)] get; }

        [FreeFunction(Name = "ShaderScripting::GetPassCountInSubshader", HasExplicitThis = true)] extern public int GetPassCountInSubshader(int subshaderIndex);

        public Rendering.ShaderTagId FindPassTagValue(int passIndex, Rendering.ShaderTagId tagName)
        {
            if (passIndex < 0 || passIndex >= passCount)
                throw new ArgumentOutOfRangeException("passIndex");
            var id = Internal_FindPassTagValue(passIndex, tagName.id);
            return new Rendering.ShaderTagId { id = id };
        }

        public Rendering.ShaderTagId FindPassTagValue(int subshaderIndex, int passIndex, Rendering.ShaderTagId tagName)
        {
            if (subshaderIndex < 0 || subshaderIndex >= subshaderCount)
                throw new ArgumentOutOfRangeException("subshaderIndex");
            if (passIndex < 0 || passIndex >= GetPassCountInSubshader(subshaderIndex))
                throw new ArgumentOutOfRangeException("passIndex");
            var id = Internal_FindPassTagValueInSubShader(subshaderIndex, passIndex, tagName.id);
            return new Rendering.ShaderTagId { id = id };
        }

        public Rendering.ShaderTagId FindSubshaderTagValue(int subshaderIndex, Rendering.ShaderTagId tagName)
        {
            if (subshaderIndex < 0 || subshaderIndex >= subshaderCount)
                throw new ArgumentOutOfRangeException("subshaderIndex");
            var id = Internal_FindSubshaderTagValue(subshaderIndex, tagName.id);
            return new Rendering.ShaderTagId { id = id };
        }

        [FreeFunction(Name = "ShaderScripting::FindPassTagValue", HasExplicitThis = true)] extern private int Internal_FindPassTagValue(int passIndex, int tagName);
        [FreeFunction(Name = "ShaderScripting::FindPassTagValue", HasExplicitThis = true)] extern private int Internal_FindPassTagValueInSubShader(int subShaderIndex, int passIndex, int tagName);
        [FreeFunction(Name = "ShaderScripting::FindSubshaderTagValue", HasExplicitThis = true)] extern private int Internal_FindSubshaderTagValue(int subShaderIndex, int tagName);

        [NativeProperty("CustomEditorName")] extern internal string customEditor { get; }
        [FreeFunction(Name = "ShaderScripting::GetCustomEditorForRenderPipeline", HasExplicitThis = true)] extern internal void Internal_GetCustomEditorForRenderPipeline(string renderPipelineType, out string customEditor);
        // TODO: get buffer is missing

        [FreeFunction("ShaderScripting::SetGlobalInt")]     extern private static void SetGlobalIntImpl(int name, int value);
        [FreeFunction("ShaderScripting::SetGlobalFloat")]   extern private static void SetGlobalFloatImpl(int name, float value);
        [FreeFunction("ShaderScripting::SetGlobalVector")]  extern private static void SetGlobalVectorImpl(int name, Vector4 value);
        [FreeFunction("ShaderScripting::SetGlobalMatrix")]  extern private static void SetGlobalMatrixImpl(int name, Matrix4x4 value);
        [FreeFunction("ShaderScripting::SetGlobalTexture")] extern private static void SetGlobalTextureImpl(int name, Texture value);
        [FreeFunction("ShaderScripting::SetGlobalRenderTexture")] extern private static void SetGlobalRenderTextureImpl(int name, RenderTexture value, Rendering.RenderTextureSubElement element);
        [FreeFunction("ShaderScripting::SetGlobalBuffer")]  extern private static void SetGlobalBufferImpl(int name, ComputeBuffer value);
        [FreeFunction("ShaderScripting::SetGlobalBuffer")]  extern private static void SetGlobalGraphicsBufferImpl(int name, GraphicsBuffer value);
        [FreeFunction("ShaderScripting::SetGlobalConstantBuffer")] extern private static void SetGlobalConstantBufferImpl(int name, ComputeBuffer value, int offset, int size);
        [FreeFunction("ShaderScripting::SetGlobalConstantBuffer")] extern private static void SetGlobalConstantGraphicsBufferImpl(int name, GraphicsBuffer value, int offset, int size);

        [FreeFunction("ShaderScripting::GetGlobalInt")]     extern private static int       GetGlobalIntImpl(int name);
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
