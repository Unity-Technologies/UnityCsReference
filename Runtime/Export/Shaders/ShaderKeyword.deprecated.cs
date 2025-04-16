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

        [Obsolete(@"GetGlobalKeywordName is deprecated. Use the ShaderKeyword.name property instead.", true)]
        public static string GetGlobalKeywordName(ShaderKeyword index) { return ""; }

        [Obsolete(@"GetKeywordName is deprecated. Use the ShaderKeyword.name property instead.", true)]
        public static string GetKeywordName(Shader shader, ShaderKeyword index) { return ""; }

        [Obsolete(@"GetKeywordName is deprecated. Use the ShaderKeyword.name property instead.", true)]
        public static string GetKeywordName(ComputeShader shader, ShaderKeyword index) { return ""; }

        [Obsolete(@"GetKeywordType is deprecated. Use ShaderKeyword.GetGlobalKeywordType instead.", true)]
        public ShaderKeywordType GetKeywordType() { return ShaderKeywordType.None; }

        [Obsolete(@"GetKeywordName is deprecated. Use ShaderKeyword.name instead.", true)]
        public string GetKeywordName() { return ""; }

        [Obsolete(@"GetName() has been deprecated. Use ShaderKeyword.name instead.", true)]
        public string GetName() { return ""; }
    }
}
