// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Assertions;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    [NativeHeader("Runtime/Graphics/ShaderScriptBindings.h")]
    [NativeHeader("Runtime/Shaders/Keywords/KeywordSpaceScriptBindings.h")]
    public readonly struct LocalKeyword : IEquatable<LocalKeyword>
    {
        [FreeFunction("keywords::IsKeywordOverridable")] extern private static bool IsOverridable(LocalKeyword kw);
        [FreeFunction("ShaderScripting::GetKeywordCount")] extern private static uint GetShaderKeywordCount(Shader shader);
        [FreeFunction("ShaderScripting::GetKeywordIndex")] extern private static uint GetShaderKeywordIndex(Shader shader, string keyword);
        [FreeFunction("ShaderScripting::GetKeywordCount")] extern private static uint GetComputeShaderKeywordCount(ComputeShader shader);
        [FreeFunction("ShaderScripting::GetKeywordIndex")] extern private static uint GetComputeShaderKeywordIndex(ComputeShader shader, string keyword);
        [FreeFunction("keywords::GetKeywordType")] extern private static ShaderKeywordType GetKeywordType(LocalKeywordSpace spaceInfo, uint keyword);
        [FreeFunction("keywords::IsKeywordValid")] extern private static bool IsValid(LocalKeywordSpace spaceInfo, uint keyword);

        public string name { get { return m_Name; } }
        public bool isOverridable { get { return IsOverridable(this); } }
        public bool isValid { get { return IsValid(m_SpaceInfo, m_Index); } }
        public ShaderKeywordType type { get { return GetKeywordType(m_SpaceInfo, m_Index); } }

        public LocalKeyword(Shader shader, string name)
        {
            if (shader == null)
                Debug.LogError("Cannot initialize a LocalKeyword with a null Shader.");
            m_SpaceInfo = shader.keywordSpace;
            m_Name = name;
            m_Index = GetShaderKeywordIndex(shader, name);
            if (m_Index >= GetShaderKeywordCount(shader))
                Debug.LogErrorFormat("Local keyword {0} doesn't exist in the shader.", name);
        }

        public LocalKeyword(ComputeShader shader, string name)
        {
            if (shader == null)
                Debug.LogError("Cannot initialize a LocalKeyword with a null ComputeShader.");
            m_SpaceInfo = shader.keywordSpace;
            m_Name = name;
            m_Index = GetComputeShaderKeywordIndex(shader, name);
            if (m_Index >= GetComputeShaderKeywordCount(shader))
                Debug.LogErrorFormat("Local keyword {0} doesn't exist in the compute shader.", name);
        }

        public override string ToString() { return m_Name; }

        public override bool Equals(object o)
        {
            return o is LocalKeyword other && this.Equals(other);
        }

        public bool Equals(LocalKeyword rhs)
        {
            return m_SpaceInfo == rhs.m_SpaceInfo && m_Index == rhs.m_Index;
        }

        public static bool operator==(LocalKeyword lhs, LocalKeyword rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator!=(LocalKeyword lhs, LocalKeyword rhs)
        {
            return !(lhs == rhs);
        }

        public override int GetHashCode()
        {
            return m_Index.GetHashCode() ^ m_SpaceInfo.GetHashCode();
        }

        internal readonly LocalKeywordSpace m_SpaceInfo;
        internal readonly string m_Name;
        internal readonly uint m_Index;
    }
}
