// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering
{
    [UsedByNativeCode]
    [NativeHeader("Runtime/Shaders/ShaderKeywordSet.h")]
    public enum ShaderKeywordType
    {
        None = 0,
        BuiltinDefault = (1 << 1),
        BuiltinExtra = (1 << 2) | BuiltinDefault,
        BuiltinAutoStripped = (1 << 3) | BuiltinDefault,
        UserDefined = (1 << 4),
    }

    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [NativeHeader("Runtime/Shaders/ShaderKeywords.h")]
    public partial class ShaderKeyword
    {
        [NativeMethod("keywords::Find", true)]
        extern internal static int GetShaderKeywordIndex(string keywordName);

        [NativeMethod("keywords::GetKeywordName", true)]
        extern internal static string GetShaderKeywordName(int keywordIndex);

        [NativeMethod("keywords::GetKeywordType", true)]
        extern internal static ShaderKeywordType GetShaderKeywordType(int keywordIndex);

        internal const int k_MaxShaderKeywords = 256; // Keep in sync with kMaxShaderKeywords in ShaderKeywordSet.h
        private const int k_InvalidKeyword = -1; // Keep in sync with keywords::kInvalidKeyword in ShaderKeywords.h

        internal ShaderKeyword(int keywordIndex)
        {
            m_KeywordIndex = keywordIndex;
        }

        public ShaderKeyword(string keywordName)
        {
            m_KeywordIndex = GetShaderKeywordIndex(keywordName);
        }

        public bool IsValid()
        {
            return m_KeywordIndex >= 0 && m_KeywordIndex < k_MaxShaderKeywords && m_KeywordIndex != k_InvalidKeyword;
        }

        public ShaderKeywordType GetKeywordType()
        {
            return GetShaderKeywordType(m_KeywordIndex);
        }

        public string GetKeywordName()
        {
            return GetShaderKeywordName(m_KeywordIndex);
        }

        internal int GetKeywordIndex()
        {
            return m_KeywordIndex;
        }

        internal int m_KeywordIndex;
    }
}
