// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using ShaderPlatform = UnityEngine.Rendering.GraphicsDeviceType;
using TextureDimension = UnityEngine.Rendering.TextureDimension;


namespace UnityEditor
{
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


    [NativeHeader("Editor/Mono/ShaderUtil.bindings.h")]
    [NativeHeader("Editor/Src/ShaderData.h")]
    [NativeHeader("Editor/Src/ShaderMenu.h")]
    public partial class ShaderUtil
    {
        public enum ShaderPropertyType
        {
            Color,
            Vector,
            Float,
            Range,
            TexEnv,
        };

        internal enum ShaderCompilerPlatformType
        {
            OpenGL = 0, // removed
            D3D9, // removed
            Xbox360, // removed
            PS3, // removed
            D3D11,
            OpenGLES20,
            OpenGLES20Desktop, // removed
            Flash, // removed
            D3D11_9x, // removed
            OpenGLES30,
            PSVita, // removed
            PS4,
            XboxOne,
            PSM, // removed
            Metal,
            OpenGLCore,
            N3DS, // removed
            WiiU, // removed
            Vulkan,
            Switch,
            Count
        };

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

        extern public static int GetPropertyCount([NotNull] Shader s);


        private static void CheckPropertyIndex(Shader s, int idx)
        {
            if (idx < 0 || idx >= GetPropertyCount(s))
                throw new ArgumentException("Passed property index is out of range.");
        }

        [FreeFunction] extern private static int FindShaderPropertyIndex([NotNull] Shader s, string name);


        [NativeName("GetPropertyName")] extern private static string GetPropertyNameImpl([NotNull] Shader s, int propertyIdx);
        public static string GetPropertyName(Shader s, int propertyIdx)
        {
            CheckPropertyIndex(s, propertyIdx);
            return GetPropertyNameImpl(s, propertyIdx);
        }

        [NativeName("GetPropertyType")] extern private static ShaderPropertyType GetPropertyTypeImpl([NotNull] Shader s, int propertyIdx);
        public static ShaderPropertyType GetPropertyType(Shader s, int propertyIdx)
        {
            CheckPropertyIndex(s, propertyIdx);
            return GetPropertyTypeImpl(s, propertyIdx);
        }

        [NativeName("GetPropertyDescription")] extern private static string GetPropertyDescriptionImpl([NotNull] Shader s, int propertyIdx);
        public static string GetPropertyDescription(Shader s, int propertyIdx)
        {
            CheckPropertyIndex(s, propertyIdx);
            return GetPropertyDescriptionImpl(s, propertyIdx);
        }

        [NativeName("GetShaderPropertyAttributes")] extern private static string[] GetShaderPropertyAttributesImpl([NotNull] Shader s, int propertyIdx);
        internal static string[] GetShaderPropertyAttributes(Shader s, string name)
        {
            int idx = FindShaderPropertyIndex(s, name);
            if (idx < 0) return null;

            string[] ret = GetShaderPropertyAttributesImpl(s, idx);
            return ret.Length > 0 ? ret : null;
        }

        [NativeName("GetRangeLimits")] extern private static float GetRangeLimitsImpl([NotNull] Shader s, int propertyIdx, int defminmax);
        public static float GetRangeLimits(Shader s, int propertyIdx, int defminmax)
        {
            CheckPropertyIndex(s, propertyIdx);
            if (defminmax < 0 || defminmax > 2)
                throw new ArgumentException("defminmax should be one of 0,1,2.");
            return GetRangeLimitsImpl(s, propertyIdx, defminmax);
        }

        [NativeName("GetTexDim")] extern private static TextureDimension GetTexDimImpl([NotNull] Shader s, int propertyIdx);
        public static TextureDimension GetTexDim(Shader s, int propertyIdx)
        {
            CheckPropertyIndex(s, propertyIdx);
            return GetTexDimImpl(s, propertyIdx);
        }

        [NativeName("IsShaderPropertyHidden")] extern private static bool IsShaderPropertyHiddenImpl([NotNull] Shader s, int propertyIdx);
        public static bool IsShaderPropertyHidden(Shader s, int propertyIdx)
        {
            CheckPropertyIndex(s, propertyIdx);
            return IsShaderPropertyHiddenImpl(s, propertyIdx);
        }

        [NativeName("IsShaderPropertyNonModifiableTexureProperty")] extern private static bool IsShaderPropertyNonModifiableTexurePropertyImpl([NotNull] Shader s, int propertyIdx);
        public static bool IsShaderPropertyNonModifiableTexureProperty(Shader s, int propertyIdx)
        {
            CheckPropertyIndex(s, propertyIdx);
            return IsShaderPropertyNonModifiableTexurePropertyImpl(s, propertyIdx);
        }

        extern internal static string GetDependency(Shader s, string name);

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


        extern internal static void CalculateLightmapStrippingFromCurrentScene();
        extern internal static void CalculateFogStrippingFromCurrentScene();

        extern internal static Rect rawViewportRect { get; set; }
        extern internal static Rect rawScissorRect  { get; set; }
        extern public   static bool hardwareSupportsRectRenderTexture { get; }
        extern internal static bool hardwareSupportsFullNPOT { get; }


        extern internal static void RecreateGfxDevice();
        extern internal static void RecreateSkinnedMeshResources();
        extern internal static void ReloadAllShaders();

        extern public static Shader CreateShaderAsset(string source);
        extern public static void   UpdateShaderAsset([NotNull] Shader shader, [NotNull] string source);
        [FreeFunction("GetScriptMapper().AddShader")] extern public static void RegisterShader(Shader shader);


        extern internal static void OpenCompiledShader(Shader shader, int mode, int externPlatformsMask, bool includeAllVariants);
        extern internal static void OpenCompiledComputeShader(ComputeShader shader, bool allVariantsAndPlatforms);
        extern internal static void OpenParsedSurfaceShader(Shader shader);
        extern internal static void OpenGeneratedFixedFunctionShader(Shader shader);
        extern internal static void OpenShaderCombinations(Shader shader, bool usedBySceneOnly);
        extern internal static void OpenSystemShaderIncludeError(string includeName, int line);


        extern internal static void SaveCurrentShaderVariantCollection(string path);
        extern internal static void ClearCurrentShaderVariantCollection();
        extern internal static int  GetCurrentShaderVariantCollectionShaderCount();
        extern internal static int  GetCurrentShaderVariantCollectionVariantCount();
        extern internal static bool AddNewShaderToCollection(Shader shader, ShaderVariantCollection collection);


        [FreeFunction] public static extern ShaderInfo[] GetAllShaderInfo();


        [FreeFunction] extern internal static string GetShaderPassSourceCode([NotNull] Shader shader, int subShaderIndex, int passId);
        [FreeFunction] extern internal static string GetShaderPassName([NotNull] Shader shader, int subShaderIndex, int passId);
        [FreeFunction] extern internal static int GetShaderActiveSubshaderIndex([NotNull] Shader shader);
        [FreeFunction] extern internal static int GetShaderSubshaderCount([NotNull] Shader shader);
        [FreeFunction] extern internal static int GetShaderTotalPassCount([NotNull] Shader shader, int subShaderIndex);
    }
}


namespace UnityEditor
{
    public partial class ShaderUtil
    {
        [System.Obsolete("Use UnityEngine.Rendering.TextureDimension instead.")]
        public enum ShaderPropertyTexDim
        {
            TexDimNone = 0, // no texture
            TexDim2D = 2,
            TexDim3D = 3,
            TexDimCUBE = 4,
            TexDimAny = 6,
        };
    }
}
