// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.ProjectAuditor.Editor.Utils
{
    internal static class ShaderUtilProxy
    {
#pragma warning disable 0414
        static Type s_TypeShaderUtil;
        static MethodInfo s_MethodGetAvailableShaderCompilerPlatforms;
        static MethodInfo s_MethodGetShaderVariantCount;
        static MethodInfo s_MethodGetShaderGlobalKeywords;
        static MethodInfo s_MethodGetShaderLocalKeywords;
        static MethodInfo s_MethodGetShaderActiveSubshaderIndex;
        static MethodInfo s_MethodGetSRPBatcherCompatibilityCode;
        static MethodInfo s_MethodHasInstancing;
        static MethodInfo s_MethodHasSurfaceShaders;
#pragma warning restore 0414

        static string[] s_ShaderPlatformNames;
        static Material s_SRPCompatibilityCheckMaterial;

        static void Init()
        {
            s_TypeShaderUtil = typeof(ShaderUtil);
            s_MethodGetAvailableShaderCompilerPlatforms = s_TypeShaderUtil.GetMethod("GetAvailableShaderCompilerPlatforms", BindingFlags.Static | BindingFlags.NonPublic);
            s_MethodGetShaderActiveSubshaderIndex = s_TypeShaderUtil.GetMethod("GetShaderActiveSubshaderIndex", BindingFlags.Static | BindingFlags.NonPublic);
            s_MethodGetShaderGlobalKeywords = s_TypeShaderUtil.GetMethod("GetShaderGlobalKeywords", BindingFlags.Static | BindingFlags.NonPublic);
            s_MethodGetShaderLocalKeywords = s_TypeShaderUtil.GetMethod("GetShaderLocalKeywords", BindingFlags.Static | BindingFlags.NonPublic);
            s_MethodGetShaderVariantCount = s_TypeShaderUtil.GetMethod("GetVariantCount", BindingFlags.Static | BindingFlags.NonPublic);
            s_MethodGetSRPBatcherCompatibilityCode = s_TypeShaderUtil.GetMethod("GetSRPBatcherCompatibilityCode", BindingFlags.Static | BindingFlags.NonPublic);
            s_MethodHasInstancing = s_TypeShaderUtil.GetMethod("HasInstancing", BindingFlags.Static | BindingFlags.NonPublic);
            s_MethodHasSurfaceShaders = s_TypeShaderUtil.GetMethod("HasSurfaceShaders", BindingFlags.Static | BindingFlags.NonPublic);

            var platformMask = (int)s_MethodGetAvailableShaderCompilerPlatforms.Invoke(null, Array.Empty<object>());
            var names = new List<string>();
            for (var i = 0; i < 32; ++i)
            {
                if ((platformMask & (1 << i)) == 0)
                    continue;
                names.Add(((UnityEditor.Rendering.ShaderCompilerPlatform)i).ToString());
            }
            s_ShaderPlatformNames = names.ToArray();
        }

        // note that this method is not present in ShaderUtil
        public static string[] GetCompilerPlatformNames()
        {
            if (s_TypeShaderUtil == null)
                Init();

            return s_ShaderPlatformNames;
        }

        public static int GetShaderActiveSubshaderIndex(Shader shader)
        {
            if (s_TypeShaderUtil == null)
                Init();

            if (s_MethodGetShaderActiveSubshaderIndex == null)
                return 0;

            return (int)s_MethodGetShaderActiveSubshaderIndex.Invoke(null, [shader]);
        }

        public static string[] GetShaderGlobalKeywords(Shader shader)
        {
            if (s_TypeShaderUtil == null)
                Init();

            if (s_MethodGetShaderGlobalKeywords == null)
                return null;

            return (string[])s_MethodGetShaderGlobalKeywords.Invoke(null, [shader]);
        }

        public static string[] GetShaderLocalKeywords(Shader shader)
        {
            if (s_TypeShaderUtil == null)
                Init();

            if (s_MethodGetShaderLocalKeywords == null)
                return null;

            return (string[])s_MethodGetShaderLocalKeywords.Invoke(null, [shader]);
        }

        public static int GetSRPBatcherCompatibilityCode(Shader shader, int subShaderIdx)
        {
            if (s_TypeShaderUtil == null)
                Init();

            if (s_MethodGetSRPBatcherCompatibilityCode == null)
                return -1;

            if (!s_SRPCompatibilityCheckMaterial)
            {
                s_SRPCompatibilityCheckMaterial = new Material(shader);
                s_SRPCompatibilityCheckMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
            else
            {
                s_SRPCompatibilityCheckMaterial.shader = shader;
            }

            s_SRPCompatibilityCheckMaterial.SetPass(0);

            int result = (int)s_MethodGetSRPBatcherCompatibilityCode.Invoke(null, [shader, subShaderIdx]);

            s_SRPCompatibilityCheckMaterial.shader = null;

            return result;
        }

        public static ulong GetVariantCount(Shader shader)
        {
            if (s_TypeShaderUtil == null)
                Init();

            if (s_MethodGetShaderVariantCount == null)
                return 0;

            ulong outCount = 0;
            if (!(bool)s_MethodGetShaderVariantCount.Invoke(null, [shader, false, outCount]))
                return 0;

            return outCount;
        }

        public static bool HasInstancing(Shader shader)
        {
            if (s_TypeShaderUtil == null)
                Init();

            if (s_MethodHasInstancing == null)
                return false;

            return (bool)s_MethodHasInstancing.Invoke(null, [shader]);
        }

        public static bool HasSurfaceShaders(Shader shader)
        {
            if (s_TypeShaderUtil == null)
                Init();

            if (s_MethodHasSurfaceShaders == null)
                return false;

            return (bool)s_MethodHasSurfaceShaders.Invoke(null, [shader]);
        }

        public static int GetPropertyCount(Shader shader)
        {
            return shader.GetPropertyCount();
        }

        public static int GetTexturePropertyCount(Shader shader)
        {
            var texturePropertyCount = 0;
            var propertyCount = GetPropertyCount(shader);
            for (int i = 0; i < propertyCount; ++i)
            {
                if (shader.GetPropertyType(i) == ShaderPropertyType.Texture)
                {
                    ++texturePropertyCount;
                }
            }

            return texturePropertyCount;
        }
    }
}
