// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor
{
    partial class MaterialProperty
    {
        [Obsolete("Use UnityEngine.Rendering.ShaderPropertyType instead. (UnityUpgradable) -> UnityEngine.Rendering.ShaderPropertyType", false)]
        public enum PropType
        {
            Color,
            Vector,
            Float,
            Range,
            Texture,
            Int,
        }

        [Obsolete("Use UnityEngine.Rendering.ShaderPropertyFlags instead. (UnityUpgradable) -> UnityEngine.Rendering.ShaderPropertyFlags", false)]
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

        [Obsolete("Use UnityEngine.Rendering.TextureDimension instead. (UnityUpgradable) -> UnityEngine.Rendering.TextureDimension", true)]
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
