// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Rendering
{
    public partial struct ShaderKeyword
    {
        [Obsolete(@"GetKeywordType is deprecated. Use Shader.GetGlobalKeywordType instead.")]
        public ShaderKeywordType GetKeywordType() { return GetGlobalKeywordType(this); }

        [Obsolete(@"GetKeywordName is deprecated. Use Shader.GetGlobalKeywordName instead.")]
        public string GetKeywordName() { return GetGlobalKeywordName(this); }

        [Obsolete(@"GetName() has been deprecated. Use Shader.GetGlobalKeywordName instead.")]
        public string GetName() { return GetKeywordName(); }
    }
}
