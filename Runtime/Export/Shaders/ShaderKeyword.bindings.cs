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
    [NativeHeader("Runtime/Graphics/ShaderScriptBindings.h")]
    [NativeHeader("Runtime/Shaders/ShaderKeywords.h")]
    public partial struct ShaderKeyword
    {
        internal const int k_MaxShaderKeywords = 448; // Keep in sync with kMaxShaderKeywords in ShaderKeywordSet.h
        private const int k_InvalidKeyword = -1; // Keep in sync with keywords::kInvalidKeyword in ShaderKeywords.h

        [FreeFunction("ShaderScripting::GetGlobalKeywordIndex")] extern internal static int GetGlobalKeywordIndex(string keyword);
        [FreeFunction("ShaderScripting::GetKeywordIndex")]       extern internal static int GetKeywordIndex(Shader shader, string keyword);

        [FreeFunction("ShaderScripting::GetGlobalKeywordName")] extern public static string GetGlobalKeywordName(ShaderKeyword index);
        [FreeFunction("ShaderScripting::GetGlobalKeywordType")] extern public static ShaderKeywordType GetGlobalKeywordType(ShaderKeyword index);
        [FreeFunction("ShaderScripting::IsKeywordLocal")]       extern public static bool IsKeywordLocal(ShaderKeyword index);

        [FreeFunction("ShaderScripting::GetKeywordName")] extern public static string GetKeywordName(Shader shader, ShaderKeyword index);
        [FreeFunction("ShaderScripting::GetKeywordType")] extern public static ShaderKeywordType GetKeywordType(Shader shader, ShaderKeyword index);

        internal ShaderKeyword(int keywordIndex)
        {
            m_KeywordIndex = keywordIndex;
        }

        public ShaderKeyword(string keywordName)
        {
            m_KeywordIndex = GetGlobalKeywordIndex(keywordName);
        }

        public ShaderKeyword(Shader shader, string keywordName)
        {
            m_KeywordIndex = GetKeywordIndex(shader, keywordName);
        }

        public bool IsValid()
        {
            return m_KeywordIndex >= 0 && m_KeywordIndex < k_MaxShaderKeywords && m_KeywordIndex != k_InvalidKeyword;
        }

        public int index { get { return m_KeywordIndex; } }

        internal int m_KeywordIndex;
    }
}
