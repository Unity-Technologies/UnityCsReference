// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering
{
    [UsedByNativeCode]
    [NativeHeader("Runtime/Shaders/Keywords/KeywordSpaceScriptBindings.h")]
    public enum ShaderKeywordType
    {
        None = 0,
        BuiltinDefault = (1 << 1),
        [System.Obsolete("Shader keyword type BuiltinExtra is no longer used. Use BuiltinDefault instead. (UnityUpgradable) -> BuiltinDefault")]
        BuiltinExtra = (1 << 2) | BuiltinDefault,
        [System.Obsolete("Shader keyword type BuiltinAutoStripped is no longer used. Use BuiltinDefault instead. (UnityUpgradable) -> BuiltinDefault")]
        BuiltinAutoStripped = (1 << 3) | BuiltinDefault,
        UserDefined = (1 << 4),
        Plugin = (1 << 5),
    }

    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [NativeHeader("Runtime/Graphics/ShaderScriptBindings.h")]
    [NativeHeader("Runtime/Shaders/Keywords/KeywordSpaceScriptBindings.h")]
    public partial struct ShaderKeyword
    {
        [FreeFunction("ShaderScripting::GetGlobalKeywordCount")] extern internal static uint GetGlobalKeywordCount();
        [FreeFunction("ShaderScripting::GetGlobalKeywordIndex")] extern internal static uint GetGlobalKeywordIndex(string keyword);
        [FreeFunction("ShaderScripting::GetKeywordCount")]       extern internal static uint GetKeywordCount(Shader shader);
        [FreeFunction("ShaderScripting::GetKeywordIndex")]       extern internal static uint GetKeywordIndex(Shader shader, string keyword);
        [FreeFunction("ShaderScripting::GetKeywordCount")]       extern internal static uint GetComputeShaderKeywordCount(ComputeShader shader);
        [FreeFunction("ShaderScripting::GetKeywordIndex")]       extern internal static uint GetComputeShaderKeywordIndex(ComputeShader shader, string keyword);
        [FreeFunction("ShaderScripting::CreateGlobalKeyword")]   extern internal static void CreateGlobalKeyword(string keyword);

        [FreeFunction("ShaderScripting::GetKeywordType")]        extern internal static ShaderKeywordType GetGlobalShaderKeywordType(uint keyword);

        public string name { get { return m_Name; } }

        public static ShaderKeywordType GetGlobalKeywordType(ShaderKeyword index)
        {
            if (index.IsValid())
                return GetGlobalShaderKeywordType(index.m_Index);
            return ShaderKeywordType.UserDefined;
        }

        public ShaderKeyword(string keywordName)
        {
            m_Name = keywordName;
            m_Index = GetGlobalKeywordIndex(keywordName);
            if (m_Index >= GetGlobalKeywordCount())
            {
                CreateGlobalKeyword(keywordName);
                m_Index = GetGlobalKeywordIndex(keywordName);
            }
            m_IsValid = true;
            m_IsLocal = false;
            m_IsCompute = false;
        }

        public ShaderKeyword(Shader shader, string keywordName)
        {
            m_Name = keywordName;
            m_Index = GetKeywordIndex(shader, keywordName);
            m_IsValid = m_Index < GetKeywordCount(shader);
            m_IsLocal = true;
            m_IsCompute = false;
        }

        public ShaderKeyword(ComputeShader shader, string keywordName)
        {
            m_Name = keywordName;
            m_Index = GetComputeShaderKeywordIndex(shader, keywordName);
            m_IsValid = m_Index < GetComputeShaderKeywordCount(shader);
            m_IsLocal = true;
            m_IsCompute = true;
        }

        public static bool IsKeywordLocal(ShaderKeyword keyword)
        {
            return keyword.m_IsLocal;
        }

        public bool IsValid()
        {
            return m_IsValid;
        }

        public bool IsValid(ComputeShader shader)
        {
            return m_IsValid;
        }

        public bool IsValid(Shader shader)
        {
            return m_IsValid;
        }

        public int index { get { return (int)m_Index; } }

        public override string ToString() { return m_Name; }

        internal string m_Name;
        internal uint m_Index;
        internal bool m_IsLocal;
        internal bool m_IsCompute;
        internal bool m_IsValid;
    }
}
