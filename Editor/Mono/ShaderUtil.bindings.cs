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
using UnityEngine.Experimental.Rendering;

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

        public string   name      { get { return m_Name; } }
        public bool     supported { get { return m_Supported; } }
        public bool     hasErrors { get { return m_HasErrors; } }
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
        extern internal static bool HasProceduralInstancing([NotNull] Shader s);
        extern internal static bool HasShadowCasterPass([NotNull] Shader s);
        extern internal static bool DoesIgnoreProjector([NotNull] Shader s);
        extern internal static int  GetRenderQueue([NotNull] Shader s);
        extern internal static bool HasTangentChannel([NotNull] Shader s);

        extern internal static void FetchCachedMessages([NotNull] Shader s);
        extern public static int GetShaderMessageCount([NotNull] Shader s);
        extern public static ShaderMessage[] GetShaderMessages([NotNull] Shader s);
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

        extern internal static void RequestLoadRenderDoc();
        extern internal static void RecreateGfxDevice();
        extern internal static void RecreateSkinnedMeshResources();
        extern internal static void ReloadAllShaders();

        extern public static Shader CreateShaderAsset(string source, bool compileInitialShaderVariants);
        public static Shader CreateShaderAsset(string source)
        {
            return CreateShaderAsset(source, true);
        }

        extern public static void   UpdateShaderAsset([NotNull] Shader shader, [NotNull] string source, bool compileInitialShaderVariants);
        public static void          UpdateShaderAsset(Shader shader, string source)
        {
            UpdateShaderAsset(shader, source, true);
        }

        [FreeFunction("GetScriptMapper().AddShader")] extern public static void RegisterShader(Shader shader);


        extern internal static void OpenCompiledShader(Shader shader, int mode, int externPlatformsMask, bool includeAllVariants, bool preprocessOnly);
        extern internal static void OpenCompiledComputeShader(ComputeShader shader, bool allVariantsAndPlatforms, bool showPreprocessed);
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


        [FreeFunction] extern internal static string GetShaderPassSourceCode([NotNull] Shader shader, int subShaderIndex, int passId);
        [FreeFunction] extern internal static string GetShaderPassName([NotNull] Shader shader, int subShaderIndex, int passId);
        [FreeFunction] extern internal static int GetShaderActiveSubshaderIndex([NotNull] Shader shader);
        [FreeFunction] extern internal static int GetShaderSubshaderCount([NotNull] Shader shader);
        [FreeFunction] extern internal static int GetShaderTotalPassCount([NotNull] Shader shader, int subShaderIndex);

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
    }
}
