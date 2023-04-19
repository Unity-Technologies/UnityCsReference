// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor
{
    // This derived class looks barren because TextureInspector already handles Texture2DArray (Texture or RenderTexture) for most functions.
    [CustomEditor(typeof(Texture2DArray))]
    [CanEditMultipleObjects]
    class Texture2DArrayInspector : TextureInspector
    {
        public override string GetInfoString()
        {
            var tex = (Texture2DArray)target;
            var info = $"{tex.width}x{tex.height} {tex.depth} slice{(tex.depth != 1 ? "s" : "")} {GraphicsFormatUtility.GetFormatString(tex.format)} {EditorUtility.FormatBytes(TextureUtil.GetStorageMemorySizeLong(tex))}";
            return info;
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            return m_Texture2DArrayPreview.RenderStaticPreview(target as Texture, assetPath, subAssets, width, height);
        }
    }
}
