// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using ShaderPropertyFlags = UnityEngine.Rendering.ShaderPropertyFlags;
using TextureDimension = UnityEngine.Rendering.TextureDimension;

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
        }

        [Obsolete("Use UnityEngine.Rendering.ShaderPropertyType instead. (UnityUpgradable) -> UnityEngine.Rendering.ShaderPropertyType", false)]
        public enum ShaderPropertyType
        {
            Color,
            Vector,
            Float,
            Range,
            [Obsolete("Use UnityEngine.Rendering.ShaderPropertyType.Texture instead. (UnityUpgradable) -> UnityEngine.Rendering.ShaderPropertyType.Texture", false)]
            TexEnv,
            Int,
        }

        [Obsolete("Use Shader.GetPropertyCount instead.", false)]
        public static int GetPropertyCount(Shader s)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            return s.GetPropertyCount();
        }

        [Obsolete("Use Shader.GetPropertyName instead.", false)]
        public static string GetPropertyName(Shader s, int propertyIdx)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            return s.GetPropertyName(propertyIdx);
        }

        [Obsolete("Use Shader.GetPropertyType instead.", false)]
        public static ShaderPropertyType GetPropertyType(Shader s, int propertyIdx)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            return (ShaderPropertyType)s.GetPropertyType(propertyIdx);
        }

        [Obsolete("Use Shader.GetPropertyDescription instead.", false)]
        public static string GetPropertyDescription(Shader s, int propertyIdx)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            return s.GetPropertyDescription(propertyIdx);
        }

        [Obsolete("Use Shader.GetPropertyRangeLimits and Shader.GetDefaultValue instead.", false)]
        public static float GetRangeLimits(Shader s, int propertyIdx, int defminmax)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            else if (defminmax < 0 || defminmax > 2)
                throw new ArgumentException("defminmax should be one of 0,1,2.");
            return defminmax > 0
                ? s.GetPropertyRangeLimits(propertyIdx)[defminmax - 1]
                : s.GetPropertyDefaultFloatValue(propertyIdx);
        }

        [Obsolete("Use Shader.GetPropertyTextureDimension instead.", false)]
        public static TextureDimension GetTexDim(Shader s, int propertyIdx)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            return s.GetPropertyTextureDimension(propertyIdx);
        }

        [Obsolete("Use Shader.GetPropertyFlags and test against ShaderPropertyFlags.HideInInspector instead.", false)]
        public static bool IsShaderPropertyHidden(Shader s, int propertyIdx)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            return (s.GetPropertyFlags(propertyIdx) & ShaderPropertyFlags.HideInInspector) != 0;
        }

        [Obsolete("Use Shader.GetPropertyFlags and test against ShaderPropertyFlags.NonModifiableTextureData instead.", false)]
        public static bool IsShaderPropertyNonModifiableTexureProperty(Shader s, int propertyIdx)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            return (s.GetPropertyFlags(propertyIdx) & ShaderPropertyFlags.NonModifiableTextureData) != 0;
        }
    }
}
