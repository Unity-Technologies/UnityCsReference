// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using ShaderPlatform = UnityEngine.Rendering.GraphicsDeviceType;
using UnityEditor.AssetImporters;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UniversalEditorTests")]

namespace UnityEditor
{
    public enum PreprocessorOverride
    {
        UseProjectSettings = 0,
        ForcePlatformPreprocessor = 1,
        ForceCachingPreprocessor = 2
    }

    [Serializable]
    [RequiredByNativeCode]
    [NativeHeader("Editor/Src/ShaderMenu.h")]
    public struct ShaderInfo
    {
        [SerializeField][NativeName("name")]       internal string m_Name;
        [SerializeField][NativeName("supported")]  internal bool m_Supported;
        [SerializeField][NativeName("hasErrors")]  internal bool m_HasErrors;
        [SerializeField][NativeName("hasWarnings")] internal bool m_HasWarnings;

        public string   name      { get { return m_Name; } }
        public bool     supported { get { return m_Supported; } }
        public bool     hasErrors { get { return m_HasErrors; } }
        public bool hasWarnings { get { return m_HasWarnings; } }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ShaderMessage : IEquatable<ShaderMessage>
    {
        public ShaderMessage(string msg, ShaderCompilerMessageSeverity sev = ShaderCompilerMessageSeverity.Error)
        {
            message = msg;
            messageDetails = string.Empty;
            file = string.Empty;
            line = 0;
            platform = ShaderCompilerPlatform.None;
            severity = sev;
        }

        public string message { get; }
        public string messageDetails { get; }
        public string file { get; }
        public int line { get; }
        public ShaderCompilerPlatform platform { get; }
        public ShaderCompilerMessageSeverity severity { get; }

        public bool Equals(ShaderMessage other)
        {
            return string.Equals(message, other.message)
                && string.Equals(messageDetails, other.messageDetails)
                && string.Equals(file, other.file)
                && line == other.line
                && platform == other.platform
                && severity == other.severity;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ShaderMessage && Equals((ShaderMessage)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (message != null ? message.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (messageDetails != null ? messageDetails.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (file != null ? file.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ line;
                hashCode = (hashCode * 397) ^ (int)platform;
                hashCode = (hashCode * 397) ^ (int)severity;
                return hashCode;
            }
        }

        public static bool operator==(ShaderMessage left, ShaderMessage right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(ShaderMessage left, ShaderMessage right)
        {
            return !left.Equals(right);
        }
    }

    internal struct ShaderVariantEntriesData
    {
        public int[] passTypes;
        public string[] keywordLists;
        public string[] remainingKeywords;
    }

    [NativeHeader("Editor/Mono/ShaderUtil.bindings.h")]
    [NativeHeader("Editor/Src/ShaderData.h")]
    [NativeHeader("Editor/Src/ShaderMenu.h")]
    [NativeHeader("Runtime/Shaders/GpuPrograms/GpuProgramManager.h")]
    public sealed partial class ShaderUtil
    {
        extern internal static int GetAvailableShaderCompilerPlatforms();

        extern internal static bool HasSurfaceShaders([NotNull] Shader s);
        extern internal static bool HasFixedFunctionShaders([NotNull] Shader s);
        extern internal static bool HasShaderSnippets([NotNull] Shader s);
        extern internal static bool HasInstancing([NotNull] Shader s);
        extern public static bool HasProceduralInstancing([NotNull] Shader s);
        extern internal static bool HasShadowCasterPass([NotNull] Shader s);
        extern internal static bool DoesIgnoreProjector([NotNull] Shader s);
        extern internal static int  GetRenderQueue([NotNull] Shader s);
        extern internal static bool HasTangentChannel([NotNull] Shader s);

        extern internal static void FetchCachedMessages([NotNull] Shader s);
        extern public static int GetShaderMessageCount([NotNull] Shader s);
        public static ShaderMessage[] GetShaderMessages(Shader s)
        {
            return GetShaderMessages(s, (ShaderCompilerPlatform)0);
        }

        extern public static ShaderMessage[] GetShaderMessages([NotNull] Shader s, ShaderCompilerPlatform platform);
        extern public static void ClearShaderMessages([NotNull] Shader s);
        extern public static int GetComputeShaderMessageCount([NotNull] ComputeShader s);
        extern public static ShaderMessage[] GetComputeShaderMessages([NotNull] ComputeShader s);

        extern public static int GetRayTracingShaderMessageCount([NotNull] RayTracingShader s);
        extern public static ShaderMessage[] GetRayTracingShaderMessages([NotNull] RayTracingShader s);
        extern public static int GetRayGenerationShaderCount([NotNull] RayTracingShader s);
        extern public static string GetRayGenerationShaderName([NotNull] RayTracingShader s, int shaderIndex);
        extern public static int GetMissShaderCount([NotNull] RayTracingShader s);
        extern public static string GetMissShaderName([NotNull] RayTracingShader s, int shaderIndex);
        extern public static int GetMissShaderRayPayloadSize([NotNull] RayTracingShader s, int shaderIndex);
        extern public static int GetCallableShaderCount([NotNull] RayTracingShader s);
        extern public static string GetCallableShaderName([NotNull] RayTracingShader s, int shaderIndex);
        extern public static int GetCallableShaderParamSize([NotNull] RayTracingShader s, int shaderIndex);

        extern static public void ClearCachedData([NotNull] Shader s);

        extern internal static int GetTextureBindingIndex(Shader s, int texturePropertyID);
        extern internal static int GetTextureSamplerBindingIndex(Shader s, int texturePropertyID);
        extern internal static int GetLOD(Shader s);
        extern internal static int GetSRPBatcherCompatibilityCode(Shader s, int subShaderIdx);
        extern internal static string GetSRPBatcherCompatibilityIssueReason(Shader s, int subShaderIdx, int err);

        extern internal static ulong            GetVariantCount(Shader s, bool usedBySceneOnly);
        extern internal static int              GetComputeShaderPlatformCount(ComputeShader s);
        extern internal static ShaderPlatform   GetComputeShaderPlatformType(ComputeShader s, int platformIndex);
        extern internal static int              GetComputeShaderPlatformKernelCount(ComputeShader s, int platformIndex);
        extern internal static string           GetComputeShaderPlatformKernelName(ComputeShader s, int platformIndex, int kernelIndex);

        extern internal static int              GetRayTracingShaderPlatformCount(RayTracingShader s);
        extern internal static ShaderPlatform   GetRayTracingShaderPlatformType(RayTracingShader s, int platformIndex);
        extern internal static bool             IsRayTracingShaderValidForPlatform(RayTracingShader s, ShaderPlatform renderer);

        extern internal static void CalculateLightmapStrippingFromCurrentScene();
        extern internal static void CalculateFogStrippingFromCurrentScene();

        extern internal static Rect rawViewportRect { get; set; }
        extern internal static Rect rawScissorRect  { get; set; }
        extern public   static bool hardwareSupportsRectRenderTexture { get; }
        extern internal static bool hardwareSupportsFullNPOT { get; }
        public static extern bool disableShaderOptimization { get; set; }

        extern internal static void RequestLoadRenderDoc();
        extern internal static void RecreateGfxDevice();
        extern internal static void RecreateSkinnedMeshResources();
        extern internal static void ReloadAllShaders();

        extern public static Shader CreateShaderAsset(AssetImportContext context, string source, bool compileInitialShaderVariants);
        public static Shader CreateShaderAsset(string source)
        {
            return CreateShaderAsset(null, source, true);
        }

        public static Shader CreateShaderAsset(string source, bool compileInitialShaderVariants)
        {
            return CreateShaderAsset(null, source, compileInitialShaderVariants);
        }

        extern public static void   UpdateShaderAsset(AssetImportContext context, [NotNull] Shader shader, [NotNull] string source, bool compileInitialShaderVariants);
        public static void          UpdateShaderAsset(Shader shader, string source)
        {
            UpdateShaderAsset(null, shader, source, true);
        }

        public static void UpdateShaderAsset(Shader shader, string source, bool compileInitialShaderVariants)
        {
            UpdateShaderAsset(null, shader, source, compileInitialShaderVariants);
        }

        extern public static ComputeShader CreateComputeShaderAsset(AssetImportContext context, string source);

        [FreeFunction("GetShaderNameRegistry().AddShader")] extern public static void RegisterShader([NotNull("NullExceptionObject")] Shader shader);


        extern internal static void OpenCompiledShader(Shader shader, int mode, int externPlatformsMask, bool includeAllVariants, bool preprocessOnly, bool stripLineDirectives);

        extern internal static void OpenCompiledComputeShader(ComputeShader shader, bool allVariantsAndPlatforms, bool showPreprocessed, bool stripLineDirectives);
        extern internal static void OpenParsedSurfaceShader(Shader shader);
        extern internal static void OpenGeneratedFixedFunctionShader(Shader shader);
        extern internal static void OpenShaderCombinations(Shader shader, bool usedBySceneOnly);
        extern internal static void OpenSystemShaderIncludeError(string includeName, int line);


        extern internal static void SaveCurrentShaderVariantCollection(string path);
        extern internal static void ClearCurrentShaderVariantCollection();
        extern internal static int  GetCurrentShaderVariantCollectionShaderCount();
        extern internal static int  GetCurrentShaderVariantCollectionVariantCount();
        extern internal static bool AddNewShaderToCollection(Shader shader, ShaderVariantCollection collection);

        private static extern ShaderVariantEntriesData GetShaderVariantEntriesFilteredInternal([NotNull] Shader shader, int maxEntries, string[] filterKeywords, ShaderVariantCollection excludeCollection);

        internal static void GetShaderVariantEntriesFiltered(Shader shader, int maxEntries, string[] filterKeywords, ShaderVariantCollection excludeCollection, out int[] passTypes, out string[] keywordLists, out string[] remainingKeywords)
        {
            ShaderVariantEntriesData data = GetShaderVariantEntriesFilteredInternal(shader, maxEntries, filterKeywords, excludeCollection);
            passTypes = data.passTypes;
            keywordLists = data.keywordLists;
            remainingKeywords = data.remainingKeywords;
        }

        extern internal static string[] GetAllGlobalKeywords();
        extern internal static string[] GetShaderGlobalKeywords([NotNull] Shader shader);
        extern internal static string[] GetShaderLocalKeywords([NotNull] Shader shader);

        [FreeFunction] public static extern ShaderInfo[] GetAllShaderInfo();
        [FreeFunction] public static extern ShaderInfo GetShaderInfo([NotNull] Shader shader);

        [FreeFunction] extern internal static string GetShaderPassSourceCode([NotNull] Shader shader, int subShaderIndex, int passId);
        [FreeFunction] extern internal static string GetShaderPassName([NotNull] Shader shader, int subShaderIndex, int passId);
        [FreeFunction] extern internal static int GetShaderActiveSubshaderIndex([NotNull] Shader shader);
        [FreeFunction] extern internal static int GetShaderSubshaderCount([NotNull] Shader shader);
        [FreeFunction] extern internal static int GetShaderTotalPassCount([NotNull] Shader shader, int subShaderIndex);
        [FreeFunction] extern internal static int GetSubshaderLOD([NotNull] Shader shader, int subShaderIndex);
        [FreeFunction] extern internal static bool IsGrabPass([NotNull] Shader shader, int subShaderIndex, int passId);
        [FreeFunction("ShaderUtil::GetShaderSerializedSubshaderCount")] extern internal static int GetShaderSerializedSubshaderCount([NotNull] Shader shader);
        [FreeFunction("ShaderUtil::FindSerializedSubShaderTagValue")] extern internal static int FindSerializedSubShaderTagValue([NotNull] Shader shader, int subShaderIndex, int tagName);
        [FreeFunction("ShaderUtil::FindPassTagValue")] extern internal static int FindPassTagValue([NotNull] Shader shader, int subShaderIndex, int passIndex, int tagName);

        extern public static bool anythingCompiling { get; }
        extern public static bool allowAsyncCompilation { get; set; }
        extern public static void SetAsyncCompilation([NotNull] CommandBuffer cmd, bool allow);
        extern public static void RestoreAsyncCompilation([NotNull] CommandBuffer cmd);
        extern public static bool IsPassCompiled([NotNull] Material material, int pass);
        extern public static void CompilePass([NotNull] Material material, int pass, bool forceSync = false);

        internal static MaterialProperty[] GetMaterialProperties(UnityEngine.Object[] mats)
        {
            return (MaterialProperty[])GetMaterialPropertiesImpl(mats);
        }

        extern private static System.Object GetMaterialPropertiesImpl(System.Object mats);

        internal static string[] GetMaterialPropertyNames(UnityEngine.Object[] mats)
        {
            return GetMaterialPropertyNamesImpl(mats);
        }

        extern private static string[] GetMaterialPropertyNamesImpl(System.Object mats);

        internal static MaterialProperty GetMaterialProperty(UnityEngine.Object[] mats, string name)
        {
            return (MaterialProperty)GetMaterialPropertyImpl(mats, name);
        }

        extern private static System.Object GetMaterialPropertyImpl(System.Object mats, string name);

        internal static MaterialProperty GetMaterialProperty(UnityEngine.Object[] mats, int propertyIndex)
        {
            return (MaterialProperty)GetMaterialPropertyByIndex(mats, propertyIndex);
        }

        extern private static System.Object GetMaterialPropertyByIndex(System.Object mats, int propertyIndex);

        internal static void ApplyProperty(MaterialProperty prop, int propertyMask, string undoName)
        {
            ApplyPropertyImpl(prop, propertyMask, undoName);
        }

        [NativeThrows]
        extern private static void ApplyPropertyImpl(System.Object prop, int propertyMask, string undoName);

        internal static void ApplyMaterialPropertyBlockToMaterialProperty(MaterialPropertyBlock propertyBlock, MaterialProperty materialProperty)
        {
            ApplyMaterialPropertyBlockToMaterialPropertyImpl(propertyBlock, materialProperty);
        }

        extern private static void ApplyMaterialPropertyBlockToMaterialPropertyImpl(System.Object propertyBlock, System.Object materialProperty);

        internal static void ApplyMaterialPropertyToMaterialPropertyBlock(MaterialProperty materialProperty, int propertyMask, MaterialPropertyBlock propertyBlock)
        {
            ApplyMaterialPropertyToMaterialPropertyBlockImpl(materialProperty, propertyMask, propertyBlock);
        }

        extern private static void ApplyMaterialPropertyToMaterialPropertyBlockImpl(System.Object materialProperty, int propertyMask, System.Object propertyBlock);

        public static string GetCustomEditorForRenderPipeline(Shader shader, string renderPipelineType)
        {
            if (shader == null)
                return null;

            shader.Internal_GetCustomEditorForRenderPipeline(renderPipelineType, out var rpEditor);
            return String.IsNullOrEmpty(rpEditor) ? null : rpEditor;
        }

        public static string GetCustomEditorForRenderPipeline(Shader shader, Type renderPipelineType) => GetCustomEditorForRenderPipeline(shader, renderPipelineType?.FullName);
        public static string GetCurrentCustomEditor(Shader shader)
        {
            if (shader == null)
                return null;

            var rpEditor = GetCustomEditorForRenderPipeline(shader, GraphicsSettings.currentRenderPipeline?.GetType());
            return String.IsNullOrEmpty(rpEditor) ? shader.customEditor : rpEditor;
        }

        extern public static BuiltinShaderDefine[] GetShaderPlatformKeywordsForBuildTarget(ShaderCompilerPlatform shaderCompilerPlatform, BuildTarget buildTarget, GraphicsTier tier);

        public static BuiltinShaderDefine[] GetShaderPlatformKeywordsForBuildTarget(ShaderCompilerPlatform shaderCompilerPlatform, BuildTarget buildTarget)
        {
            return GetShaderPlatformKeywordsForBuildTarget(shaderCompilerPlatform, buildTarget, GraphicsTier.Tier1);
        }

        extern internal static ShaderData.VariantCompileInfo CompileShaderVariant([NotNull] Shader shader, int subShaderIndex, int passId,
            ShaderType shaderType, BuiltinShaderDefine[] platformKeywords, string[] keywords, ShaderCompilerPlatform shaderCompilerPlatform, BuildTarget buildTarget, GraphicsTier tier, bool outputForExternalTool);
        extern internal static ShaderData.PreprocessedVariant PreprocessShaderVariant([NotNull] Shader shader, int subShaderIndex, int passId,
            ShaderType shaderType, BuiltinShaderDefine[] platformKeywords, string[] keywords, ShaderCompilerPlatform shaderCompilerPlatform, BuildTarget buildTarget, GraphicsTier tier, bool stripLineDirectives);

        [FreeFunction("ShaderUtil::GetPassKeywords")] extern private static LocalKeyword[] GetPassAllStageKeywords(Shader s, in PassIdentifier passIdentifier);
        [FreeFunction("ShaderUtil::GetPassKeywords")] extern private static LocalKeyword[] GetPassStageKeywords(Shader s, in PassIdentifier passIdentifier, ShaderType shaderType);
        [FreeFunction("ShaderUtil::GetPassKeywords")] extern private static LocalKeyword[] GetPassStageKeywordsForAPI(Shader s, in PassIdentifier passIdentifier, ShaderType shaderType, ShaderCompilerPlatform shaderCompilerPlatform);
        [FreeFunction("ShaderUtil::PassHasKeyword")] extern private static bool PassAnyStageHasKeyword(Shader s, in PassIdentifier passIdentifier, uint keywordIndex);
        [FreeFunction("ShaderUtil::PassHasKeyword")] extern private static bool PassStageHasKeyword(Shader s, in PassIdentifier passIdentifier, uint keywordIndex, ShaderType shaderType);
        [FreeFunction("ShaderUtil::PassHasKeyword")] extern private static bool PassStageHasKeywordForAPI(Shader s, in PassIdentifier passIdentifier, uint keywordIndex, ShaderType shaderType, ShaderCompilerPlatform shaderCompilerPlatform);

        public static LocalKeyword[] GetPassKeywords(Shader s, in PassIdentifier passIdentifier)
        {
            return GetPassAllStageKeywords(s, passIdentifier);
        }

        public static LocalKeyword[] GetPassKeywords(Shader s, in PassIdentifier passIdentifier, ShaderType shaderType)
        {
            return GetPassStageKeywords(s, passIdentifier, shaderType);
        }

        public static LocalKeyword[] GetPassKeywords(Shader s, in PassIdentifier passIdentifier, ShaderType shaderType, ShaderCompilerPlatform shaderCompilerPlatform)
        {
            return GetPassStageKeywordsForAPI(s, passIdentifier, shaderType, shaderCompilerPlatform);
        }

        public static bool PassHasKeyword(Shader s, in PassIdentifier passIdentifier, in LocalKeyword keyword)
        {
            return PassAnyStageHasKeyword(s, passIdentifier, keyword.m_Index);
        }

        public static bool PassHasKeyword(Shader s, in PassIdentifier passIdentifier, in LocalKeyword keyword, ShaderType shaderType)
        {
            return PassStageHasKeyword(s, passIdentifier, keyword.m_Index, shaderType);
        }

        public static bool PassHasKeyword(Shader s, in PassIdentifier passIdentifier, in LocalKeyword keyword, ShaderType shaderType, ShaderCompilerPlatform shaderCompilerPlatform)
        {
            return PassStageHasKeywordForAPI(s, passIdentifier, keyword.m_Index, shaderType, shaderCompilerPlatform);
        }
    }
}
