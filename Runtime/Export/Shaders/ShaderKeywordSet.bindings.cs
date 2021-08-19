// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Assertions;
using System.Runtime.InteropServices;
using System;

namespace UnityEngine.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [NativeHeader("Editor/Src/Graphics/ShaderCompilerData.h")]
    unsafe public struct ShaderKeywordSet
    {
        private IntPtr m_KeywordState;
        private IntPtr m_Shader;
        private IntPtr m_ComputeShader;
        private UInt64 m_StateIndex;

        [FreeFunction("keywords::IsKeywordEnabled")] extern private static bool IsGlobalKeywordEnabled(ShaderKeywordSet state, uint index);
        [FreeFunction("keywords::IsKeywordEnabled")] extern private static bool IsKeywordEnabled(ShaderKeywordSet state, LocalKeyword keyword);
        [FreeFunction("keywords::IsKeywordEnabled")] extern private static bool IsKeywordNameEnabled(ShaderKeywordSet state, string name);
        [FreeFunction("keywords::EnableKeyword")] extern private static void EnableGlobalKeyword(ShaderKeywordSet state, uint index);
        [FreeFunction("keywords::EnableKeyword")] extern private static void EnableKeywordName(ShaderKeywordSet state, string name);
        [FreeFunction("keywords::DisableKeyword")] extern private static void DisableGlobalKeyword(ShaderKeywordSet state, uint index);
        [FreeFunction("keywords::DisableKeyword")] extern private static void DisableKeywordName(ShaderKeywordSet state, string name);

        [FreeFunction("keywords::GetEnabledKeywords")] extern private static ShaderKeyword[] GetEnabledKeywords(ShaderKeywordSet state);

        private void CheckKeywordCompatible(ShaderKeyword keyword)
        {
            if (keyword.m_IsLocal)
            {
                if (m_Shader != IntPtr.Zero)
                    Assert.IsTrue(!keyword.m_IsCompute, "Trying to use a keyword that comes from a different shader.");
                else
                    Assert.IsTrue(keyword.m_IsCompute, "Trying to use a keyword that comes from a different shader.");
            }
        }

        public bool IsEnabled(ShaderKeyword keyword)
        {
            CheckKeywordCompatible(keyword);
            return IsKeywordNameEnabled(this, keyword.m_Name);
        }

        public bool IsEnabled(GlobalKeyword keyword)
        {
            return IsGlobalKeywordEnabled(this, keyword.m_Index);
        }

        public bool IsEnabled(LocalKeyword keyword)
        {
            return IsKeywordEnabled(this, keyword);
        }

        public void Enable(ShaderKeyword keyword)
        {
            CheckKeywordCompatible(keyword);
            if (keyword.m_IsLocal || !keyword.IsValid())
                EnableKeywordName(this, keyword.m_Name);
            else
                EnableGlobalKeyword(this, keyword.m_Index);
        }

        public void Disable(ShaderKeyword keyword)
        {
            if (keyword.m_IsLocal || !keyword.IsValid())
                DisableKeywordName(this, keyword.m_Name);
            else
                DisableGlobalKeyword(this, keyword.m_Index);
        }

        public ShaderKeyword[] GetShaderKeywords()
        {
            return GetEnabledKeywords(this);
        }
    }
}
