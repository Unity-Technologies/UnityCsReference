// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Rendering
{
    public partial struct ShaderKeyword
    {
        [Obsolete(@"GetKeywordType is deprecated. Only global keywords can have a type. This method always returns ShaderKeywordType.UserDefined.")]
        public static ShaderKeywordType GetKeywordType(Shader shader, ShaderKeyword index)
        {
            return ShaderKeywordType.UserDefined;
        }

        [Obsolete(@"GetKeywordType is deprecated. Only global keywords can have a type. This method always returns ShaderKeywordType.UserDefined.")]
        public static ShaderKeywordType GetKeywordType(ComputeShader shader, ShaderKeyword index)
        {
            return ShaderKeywordType.UserDefined;
        }

        [Obsolete(@"GetGlobalKeywordName is deprecated. Use the ShaderKeyword.name property instead.")]
        public static string GetGlobalKeywordName(ShaderKeyword index)
        {
            return index.m_Name;
        }

        [Obsolete(@"GetKeywordName is deprecated. Use the ShaderKeyword.name property instead.")]
        public static string GetKeywordName(Shader shader, ShaderKeyword index)
        {
            return index.m_Name;
        }

        [Obsolete(@"GetKeywordName is deprecated. Use the ShaderKeyword.name property instead.")]
        public static string GetKeywordName(ComputeShader shader, ShaderKeyword index)
        {
            return index.m_Name;
        }

        [Obsolete(@"GetKeywordType is deprecated. Use ShaderKeyword.name instead.")]
        public ShaderKeywordType GetKeywordType() { return GetGlobalKeywordType(this); }

        [Obsolete(@"GetKeywordName is deprecated. Use ShaderKeyword.name instead.")]
        public string GetKeywordName() { return GetGlobalKeywordName(this); }

        [Obsolete(@"GetName() has been deprecated. Use ShaderKeyword.name instead.")]
        public string GetName() { return GetKeywordName(); }
    }
}
