// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    [NativeHeader("Editor/Mono/ShaderUtil.bindings.h")]
    public partial class ShaderUtil
    {
        [Obsolete("ClearShaderErrors has been deprecated. Use ClearShaderMessages instead (UnityUpgradable) -> ClearShaderMessages(*)")]
        [NativeName("ClearShaderMessages")]
        extern public static void ClearShaderErrors([NotNull] Shader s);

        [Obsolete("Use UnityEngine.Rendering.TextureDimension instead.")]
        public enum ShaderPropertyTexDim
        {
            TexDimNone = 0, // no texture
            TexDim2D = 2,
            TexDim3D = 3,
            TexDimCUBE = 4,
            TexDimAny = 6,
        };
    }
}
