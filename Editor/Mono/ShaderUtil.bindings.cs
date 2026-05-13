// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor.Build.Content;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
using ShaderPlatform = UnityEngine.Rendering.GraphicsDeviceType;
using UnityEditor.AssetImporters;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UnityEditor.ShaderUtil.Tests")]
[assembly: InternalsVisibleTo("UniversalEditorTests")]
[assembly: InternalsVisibleTo("UnityGraphicsKernel")]
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor-testable")]

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
        extern public static string GetClosestHitShaderName([NotNull] RayTracingShader s);
        extern public static int GetClosestHitShaderRayPayloadSize([NotNull] RayTracingShader s);
        extern public static string GetAnyHitShaderName([NotNull] RayTracingShader s);
        extern public static int GetAnyHitShaderRayPayloadSize([NotNull] RayTracingShader s);
        extern public static string GetIntersectionShaderName([NotNull] RayTracingShader s);

        extern static public void ClearCachedData([NotNull] Shader s);

        extern internal static int GetTextureBindingIndex(Shader s, int texturePropertyID);
        extern internal static int GetTextureSamplerBindingIndex(Shader s, int texturePropertyID);
        extern internal static int GetLOD(Shader s);
        extern internal static int GetSRPBatcherCompatibilityCode(Shader s, int subShaderIdx);
        extern internal static string GetSRPBatcherCompatibilityIssueReason(Shader s, int subShaderIdx, int err);

        extern internal static bool             GetVariantCount(Shader s, bool usedBySceneOnly, out ulong outCount);
        extern internal static int              GetComputeShaderPlatformCount(ComputeShader s);
        extern internal static ShaderPlatform   GetComputeShaderPlatformType(ComputeShader s, int platformIndex);
        extern internal static int              GetComputeShaderPlatformKernelCount(ComputeShader s, int platformIndex);
        extern internal static string           GetComputeShaderPlatformKernelName(ComputeShader s, int platformIndex, int kernelIndex);

        extern internal static int              GetRayTracingShaderPlatformCount(RayTracingShader s);
        extern internal static ShaderPlatform   GetRayTracingShaderPlatformType(RayTracingShader s, int platformIndex);
        extern internal static bool             IsRayTracingShaderValidForPlatform(RayTracingShader s, ShaderPlatform renderer);

        extern internal static void CalculateLightmapStrippingFromCurrentScene();
        extern internal static void CalculateLightmapStrippingFromCurrentSceneForBuildProfile(
            out bool lightmapKeepPlain, out bool lightmapKeepDirCombined, out bool lightmapKeepDynamicPlain,
            out bool lightmapKeepDynamicDirCombined, out bool lightmapKeepShadowMask, out bool lightmapKeepSubtractive);
        extern internal static void CalculateFogStrippingFromCurrentScene();
        extern internal static void CalculateFogStrippingFromCurrentSceneForBuildProfile(out bool fogKeepLinear, out bool fogKeepExp, out bool fogKeepExp2);

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

        extern internal static ComputeShader CreateComputeShaderAssetInternal(AssetImportContext context, string source);

        public static ComputeShader CreateComputeShaderAsset(AssetImportContext context, string source)
        {
            if (context == null)
                throw new ArgumentNullException("context is null.");

            return CreateComputeShaderAssetInternal(context, source);
        }

        internal static ComputeShader CreateComputeShaderAsset(string source)
        {
            return CreateComputeShaderAssetInternal(null, source);
        }

        extern public static RayTracingShader CreateRayTracingShaderAsset(AssetImportContext context, string source);

        [FreeFunction("GetShaderNameRegistry().AddShader")] extern public static void RegisterShader([NotNull] Shader shader);


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

        extern private static bool IsGraphicsAPISupportedShader([NotNull] Shader shader, in PassIdentifier passIdentifier, GraphicsDeviceType graphicsAPI);
        extern private static bool IsGraphicsAPISupportedCompute([NotNull] ComputeShader shader, GraphicsDeviceType graphicsAPI);

        public static bool IsGraphicsAPISupported(Shader shader, in PassIdentifier passIdentifier, GraphicsDeviceType graphicsAPI)
        {
            return IsGraphicsAPISupportedShader(shader, passIdentifier, graphicsAPI);
        }
        public static bool IsGraphicsAPISupported(ComputeShader shader, GraphicsDeviceType graphicsAPI)
        {
            return IsGraphicsAPISupportedCompute(shader, graphicsAPI);
        }

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

        [VisibleToOtherModules("UnityEditor.UIBuilderModule", "UnityEditor.UIToolkitAuthoringModule")]
        internal static MaterialProperty[] GetMaterialProperties(UnityEngine.Object[] mats)
        {
            if ((mats == null) || (mats.Length == 0))
                return null;

            int nullIndex = Array.IndexOf(mats, null);
            if (nullIndex >= 0)
                throw new ArgumentException($"List of materials contains null at index {nullIndex}");

            Material firstMaterial = (Material)mats[0];
            Shader shader = firstMaterial.shader;
            if (shader == null)
                throw new ArgumentException("Shader on first material is null");
            int propertyCount = shader.GetPropertyCount();

            MaterialProperty[] materialProperties = new MaterialProperty[propertyCount];
            for (int propertyNum = 0; propertyNum < propertyCount; propertyNum++)
            {
                materialProperties[propertyNum] = ShaderUtil.ExtractMaterialProperty(shader, propertyNum, mats, firstMaterial);
            }
            return materialProperties;
        }

        internal static string[] GetMaterialPropertyNames(UnityEngine.Object[] mats)
        {
            return GetMaterialPropertyNamesImpl(mats);
        }

        extern private static string[] GetMaterialPropertyNamesImpl(System.Object mats);

        internal static MaterialProperty GetMaterialProperty(UnityEngine.Object[] mats, string name)
        {
            if ((mats == null) || (mats.Length == 0))
                return null;

            Material firstMaterial = (Material)mats[0];
            Shader shader = firstMaterial.shader;

            MaterialProperty materialProperty = null;

            if (shader != null)
            {
                int propertyIndex = shader.FindPropertyIndex(name);
                if (propertyIndex != -1)
                {
                    materialProperty = ExtractMaterialProperty(shader, propertyIndex, mats, firstMaterial);
                }
            }

            return (materialProperty != null) ? materialProperty : new MaterialProperty();
        }

        internal static MaterialProperty GetMaterialProperty(UnityEngine.Object[] mats, int propertyIndex)
        {
            if ((mats == null) || (mats.Length == 0))
                return null;

            Material firstMaterial = (Material)mats[0];
            Shader shader = firstMaterial.shader;

            return (shader != null && propertyIndex >= 0 && propertyIndex < shader.GetPropertyCount())
                ? ExtractMaterialProperty(shader, propertyIndex, mats, firstMaterial)
                : new MaterialProperty();
        }

        internal static void ApplyProperty(MaterialProperty prop, int propertyMask, string undoName)
        {
            if (prop.targets == null || prop.targets.Length == 0)
                return;

            ApplyPropertyImpl(prop.targets, prop.name, prop.propertyType, prop.m_Value, prop.textureScaleAndOffset, propertyMask, undoName);
        }

        extern private static void ApplyPropertyImpl(UnityEngine.Object[] propTargets, string propName, UnityEngine.Rendering.ShaderPropertyType propType, object propValue, Vector4 propTextureScaleAndOffset, int propertyMask, string undoName);

        internal static void ApplyMaterialPropertyBlockToMaterialProperty(MaterialPropertyBlock propertyBlock, MaterialProperty materialProperty)
        {
            materialProperty.m_Value = ApplyMaterialPropertyBlockToMaterialPropertyImpl(propertyBlock, materialProperty.name, materialProperty.propertyType,
                materialProperty.m_Value, ref materialProperty.m_TextureScaleAndOffset);
        }

        extern private static object ApplyMaterialPropertyBlockToMaterialPropertyImpl(MaterialPropertyBlock propertyBlock,
            string propName, UnityEngine.Rendering.ShaderPropertyType propType, object propValue, ref Vector4 propTextureScaleAndOffset);

        internal static void ApplyMaterialPropertyToMaterialPropertyBlock(MaterialProperty materialProperty, int propertyMask, MaterialPropertyBlock propertyBlock)
        {
            ApplyMaterialPropertyToMaterialPropertyBlockImpl(propertyBlock, materialProperty.name, materialProperty.propertyType,
                materialProperty.propertyFlags, materialProperty.m_Value, materialProperty.m_TextureScaleAndOffset, propertyMask);
        }

        extern private static void ApplyMaterialPropertyToMaterialPropertyBlockImpl(MaterialPropertyBlock propertyBlock,
            string propName, UnityEngine.Rendering.ShaderPropertyType propType, UnityEngine.Rendering.ShaderPropertyFlags propFlags,
            object propValue, Vector4 propTextureScaleAndOffset, int propertyMask);

        internal static void ApplyMaterialPropertyToMaterialPropertyBlockInEditor(MaterialProperty materialProperty, int propertyMask, MaterialPropertyBlock propertyBlock)
        {
            ApplyMaterialPropertyToMaterialPropertyBlockInEditorImpl(propertyBlock, materialProperty.name, materialProperty.propertyType,
                materialProperty.propertyFlags, materialProperty.m_Value, materialProperty.m_TextureScaleAndOffset, propertyMask);
        }

        extern private static void ApplyMaterialPropertyToMaterialPropertyBlockInEditorImpl(MaterialPropertyBlock propertyBlock,
            string propName, UnityEngine.Rendering.ShaderPropertyType propType, UnityEngine.Rendering.ShaderPropertyFlags propFlags,
            object propValue, Vector4 propTextureScaleAndOffset, int propertyMask);

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

        extern internal static ShaderData.VariantCompileInfo CompileComputeShaderVariant([NotNull] ComputeShader shader, int kernelIndex,
            string[] keywords, GraphicsDeviceType graphicsDeviceType, BuildTarget buildTarget);

        extern private static bool CanBuildShadersWithShaderCompiler(ShaderCompilerPlatform shaderCompilerPlatform, BuildTarget buildTarget);
        extern private static bool CanBuildShadersForGraphicsDevice(GraphicsDeviceType graphicsDeviceType, BuildTarget buildTarget);

        internal static bool CanBuildShadersFor(ShaderCompilerPlatform shaderCompilerPlatform, BuildTarget buildTarget) =>
            CanBuildShadersWithShaderCompiler(shaderCompilerPlatform, buildTarget);
        internal static bool CanBuildShadersFor(GraphicsDeviceType graphicsDeviceType, BuildTarget buildTarget) =>
            CanBuildShadersForGraphicsDevice(graphicsDeviceType, buildTarget);

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

        extern internal static Shader[] GetShaderDependencies([NotNull] Shader shader);

        [FreeFunction("ShaderUtil::GetCompiledData")] extern internal static byte[] GetCompiledData(
            Shader s, BuildUsageTagSet buildUsageTags, BuildUsageTagGlobal globalUsageTag,
            BuildTargetSelection buildTarget, bool shouldIncludeAllVariants);

        internal static MaterialProperty ExtractMaterialProperty(Shader shader, int propertyIndex, UnityEngine.Object[] materials, Material firstMaterial)
        {
            if (materials == null)
                throw new ArgumentNullException(nameof(materials));

            MaterialProperty res = new MaterialProperty();

            ShaderPropertyFlags propertyFlags;
            UnityEngine.Rendering.ShaderPropertyType propertyType;
            Vector4 defaultValue;
            Shader.GetValuesForExtractMaterialProperty(shader, propertyIndex, out res.m_Name, out res.m_DisplayName, out propertyFlags, out propertyType, out defaultValue, out res.m_TextureDimension);

            res.m_Targets = materials;
            res.m_Value = null;
            res.m_MixedValueMask = 0;

            int materialsCount = materials.Length;

            int propId = Shader.PropertyToID(res.m_Name);

            switch (propertyType)
            {
                case UnityEngine.Rendering.ShaderPropertyType.Color:
                    {
                        Color firstMaterialValue = firstMaterial.GetColor(propId);
                        res.m_Value = firstMaterialValue;

                        for (int materialNum = 1; materialNum < materialsCount; materialNum++)
                        {
                            if (((Material)materials[materialNum]).GetColor(propId) != firstMaterialValue)
                            {
                                res.m_MixedValueMask = 1;
                                break;
                            }
                        }
                    }
                    break;

                case UnityEngine.Rendering.ShaderPropertyType.Vector:
                    {
                        Vector4 firstMaterialValue = firstMaterial.GetVector(propId);
                        res.m_Value = firstMaterialValue;

                        for (int materialNum = 1; materialNum < materialsCount; materialNum++)
                        {
                            if (((Material)materials[materialNum]).GetVector(propId) != firstMaterialValue)
                            {
                                res.m_MixedValueMask = 1;
                                break;
                            }
                        }
                    }
                    break;

                case UnityEngine.Rendering.ShaderPropertyType.Float:
                case UnityEngine.Rendering.ShaderPropertyType.Range:
                    {
                        float firstMaterialValue = firstMaterial.GetFloat(propId);
                        res.m_Value = firstMaterialValue;

                        for (int materialNum = 1; materialNum < materialsCount; materialNum++)
                        {
                            if (((Material)materials[materialNum]).GetFloat(propId) != firstMaterialValue)
                            {
                                res.m_MixedValueMask = 1;
                                break;
                            }
                        }
                    }
                    break;

                case UnityEngine.Rendering.ShaderPropertyType.Int:
                    {
                        int firstMaterialValue = firstMaterial.GetInteger(propId);
                        res.m_Value = firstMaterialValue;

                        for (int materialNum = 1; materialNum < materialsCount; materialNum++)
                        {
                            if (((Material)materials[materialNum]).GetInteger(propId) != firstMaterialValue)
                            {
                                res.m_MixedValueMask = 1;
                                break;
                            }
                        }
                    }
                    break;

                case UnityEngine.Rendering.ShaderPropertyType.Texture:
                    {
                        Texture v = null;
                        Vector4 scaleAndOffset = new Vector4(1, 1, 0, 0);

                        if ((propertyFlags & ShaderPropertyFlags.PerRendererData) != 0)
                        {
                            v = EditorGUIUtility.GetEditorAssetBundle().LoadAsset<Texture>("Previews/Textures/textureExternal.png");
                        }

                        if (v == null)
                        {
                            scaleAndOffset = firstMaterial.GetTextureScaleAndOffsetImpl(propId);
                            v = firstMaterial.GetTexture(propId);
                            for (int i = 1; i < materialsCount; i++)
                            {
                                Material curMat = (Material)materials[i];

                                // For textures mixed mask 0 bit represents the texture
                                if (curMat.GetTexture(propId) != v)
                                    res.m_MixedValueMask |= 1;

                                // 1-4 bit represents the scale and offset
                                Vector4 curScaleAndOffset = curMat.GetTextureScaleAndOffsetImpl(propId);
                                for (int c = 0; c < 4; c++)
                                {
                                    bool isComponentMixed = curScaleAndOffset[c] != scaleAndOffset[c];
                                    if (isComponentMixed)
                                        res.m_MixedValueMask |= 1 << (c + 1);
                                }
                            }
                        }

                        res.m_Value = v;
                        res.m_TextureScaleAndOffset = scaleAndOffset;
                    }
                    break;

                default:
                    throw new InvalidDataException($"unknown shader property type {propertyType} on property '{res.m_Name}' of shader '{shader.name}'");
            }

            // defaultValue layout: [0] = default, [1] = range min, [2] = range max
            res.m_RangeLimits.x = defaultValue[1];
            res.m_RangeLimits.y = defaultValue[2];
            res.m_Type = propertyType;
            res.m_Flags = propertyFlags;

            return res;
        }
    }
}
