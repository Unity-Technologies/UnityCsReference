// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    partial class MaterialProperty
    {
        // We can't deprecate them yet (Sept 2019): SRP uses them, and we have the rule of not running APIUpdater on verified packages.
        // Because of this rule, we can't upgrade MaterialProperty.propType and MaterialProperty.flags to the new enum type in UnityEngine
        // namespace but to keep them separate. (We could have two differently named propType/flags properties, one returning old enum type and the
        // other returning the new ShaderPropertyType and proceed with api deprecation with staged approach, but that will create even
        // more confusion.)
        // We won't even have deprecation warnings for them because some Katana tests expect no warnings from SRP.

        //[Obsolete("Use UnityEngine.Rendering.ShaderPropertyType instead. (UnityUpgradable) -> UnityEngine.Rendering.ShaderPropertyType")]
        public enum PropType
        {
            Color,
            Vector,
            Float,
            Range,
            Texture,
            Int,
        }

        //[Obsolete("Use UnityEngine.Rendering.ShaderPropertyFlags instead. (UnityUpgradable) -> UnityEngine.Rendering.ShaderPropertyFlags")]
        [Flags]
        public enum PropFlags
        {
            None = 0,
            HideInInspector = (1 << 0),
            PerRendererData = (1 << 1),
            NoScaleOffset = (1 << 2),
            Normal = (1 << 3),
            HDR = (1 << 4),
            Gamma = (1 << 5),
            NonModifiableTextureData = (1 << 6),
        }

        [Obsolete("Use UnityEngine.Rendering.TextureDimension instead.", false)]
        public enum TexDim
        {
            Unknown = -1,
            None = 0,
            Tex2D = 2,
            Tex3D = 3,
            Cube = 4,
            Any = 6,
        }
    }
}
