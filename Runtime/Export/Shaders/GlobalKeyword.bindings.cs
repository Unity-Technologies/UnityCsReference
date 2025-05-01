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
    [NativeHeader("Runtime/Graphics/ShaderScriptBindings.h")]
    [NativeHeader("Runtime/Shaders/Keywords/KeywordSpaceScriptBindings.h")]
    public readonly struct GlobalKeyword
    {
        [FreeFunction("ShaderScripting::GetGlobalKeywordCount")] extern private static uint GetGlobalKeywordCount();
        [FreeFunction("ShaderScripting::GetGlobalKeywordIndex")] extern private static uint GetGlobalKeywordIndex(string keyword);
        [FreeFunction("ShaderScripting::CreateGlobalKeyword")] extern private static void CreateGlobalKeyword(string keyword);
        [FreeFunction("ShaderScripting::GetGlobalKeywordName")] extern private static string GetGlobalKeywordName(uint keywordIndex);

        public static GlobalKeyword Create(string name)
        {
            CreateGlobalKeyword(name);
            return new GlobalKeyword(name);
        }

        public GlobalKeyword(string name)
        {
            m_Index = GetGlobalKeywordIndex(name);
            if (m_Index >= GetGlobalKeywordCount())
                Debug.LogErrorFormat("Global keyword {0} doesn't exist.", name);
        }

        public string name => GetGlobalKeywordName(m_Index);

        public override string ToString() => name;

        internal readonly uint m_Index;
    }
}
