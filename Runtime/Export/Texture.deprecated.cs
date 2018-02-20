// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using TextureDimension = UnityEngine.Rendering.TextureDimension;

namespace UnityEngine
{
    partial class RenderTexture
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Use RenderTexture.autoGenerateMips instead (UnityUpgradable) -> autoGenerateMips", false)]
        public bool generateMips
        {
            get { return autoGenerateMips; }
            set { autoGenerateMips = value; }
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("UsSetBorderColor is no longer supported.", true)]
        public void SetBorderColor(Color color) {}

        [Obsolete("Use RenderTexture.dimension instead.", false)]
        public bool isCubemap
        {
            get { return dimension == TextureDimension.Cube; }
            set { dimension = value ? TextureDimension.Cube : TextureDimension.Tex2D; }
        }

        [Obsolete("Use RenderTexture.dimension instead.", false)]
        public bool isVolume
        {
            get { return dimension == TextureDimension.Tex3D; }
            set { dimension = value ? TextureDimension.Tex3D : TextureDimension.Tex2D; }
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("RenderTexture.enabled is always now, no need to use it.", false)]
        // for some reason we are providing enabled setter which is empty (i dont know what the intent is/was)
        public static bool enabled { get { return true; } set {} }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("GetTexelOffset always returns zero now, no point in using it.", false)]
        public Vector2 GetTexelOffset() { return Vector2.zero; }
    }
}
