// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [NativeHeader("Runtime/Shaders/ShaderKeywords.h")]
    public class ShaderKeyword
    {
        [NativeMethod("keywords::Find", true)]
        extern internal static int GetShaderKeywordIndex(string keywordName);

        private const int k_MaxShaderKeywords = 256; // Keep in sync with kMaxShaderKeywords in ShaderKeywordSet.h
        private const int k_InvalidKeyword = -1; // Keep in sync with keywords::kInvalidKeyword in ShaderKeywords.h

        public ShaderKeyword(string keywordName)
        {
            m_Keyword = keywordName;
        }

        public bool IsValid()
        {
            var index = GetShaderKeywordIndex(m_Keyword);
            return index >= 0 && index < k_MaxShaderKeywords && index != k_InvalidKeyword;
        }

        public string GetName()
        {
            return m_Keyword;
        }

        internal int GetIndex()
        {
            return GetShaderKeywordIndex(m_Keyword);
        }

        internal string m_Keyword;
    }
}
