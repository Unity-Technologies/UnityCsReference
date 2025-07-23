// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Pool;

namespace UnityEngine.UIElements.UIR
{
    static class RenderTreeAtlas
    {
        // Add an artifical margins to force users to use the UV-rect in their own filter implementation.
        const int k_LeftMargin = 10;
        const int k_TopMargin = 10;
        const int k_RightMargin = 5;
        const int k_BottomMargin = 5;

        public struct AtlasBlock
        {
            public int width;
            public int height;
            public RectInt rect;
            public Rect uvRect;

            public RenderTexture texture;

            public AtlasBlock(int w, int h, RectInt r, Rect uv)
            {
                width = w;
                height = h;
                rect = r;
                uvRect = uv;
                texture = null;
            }
        }

        static public bool ReserveSize(int width, int height, out AtlasBlock block)
        {
            // This is a placeholder implementation that uses temporary RenderTextures.
            // In the future, a real atlas management system should be implemented.

            int texWidth = width + k_LeftMargin + k_RightMargin;
            int texHeight = height + k_TopMargin + k_BottomMargin;

            var rect = new RectInt(k_LeftMargin, k_TopMargin, width, height);
            var uvRect = new Rect(k_LeftMargin/(float)texWidth, k_TopMargin/(float)texHeight, width/(float)texWidth, height/(float)texHeight);

            // Flip the rect and UV rect to match UI Toolkit's coordinate system
            rect.y = texHeight - (rect.y + rect.height);
            uvRect.y = 1.0f - uvRect.yMax;

            block = new AtlasBlock(width, height, rect, uvRect);

            return true;
        }

        static public bool CreateTextureForAtlasBlock(ref AtlasBlock block, bool forceGammaRendering)
        {
            // Fills the entry.texture with a newly allocated RenderTexture
            Debug.Assert(block.texture == null, "Entry already has a texture assigned.");
            Debug.Assert(block.width > 0 && block.height > 0, "Invalid texture size requested.");

            int texWidth = block.width + k_LeftMargin + k_RightMargin;
            int texHeight = block.height + k_TopMargin + k_BottomMargin;

            var colorspace = QualitySettings.activeColorSpace;
            if (forceGammaRendering)
                colorspace = ColorSpace.Gamma;

            var graphicsFormat = (colorspace == ColorSpace.Linear) ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
            var descriptor = new RenderTextureDescriptor(texWidth, texHeight, graphicsFormat, GraphicsFormat.D24_UNorm_S8_UInt);
            descriptor.useMipMap = false;

            block.texture = RenderTexture.GetTemporary(descriptor);

            if (block.texture == null)
            {
                Debug.LogError($"Failed to allocate RenderTexture of size {block.width}x{block.height}.");
                return false;
            }

            var old = RenderTexture.active;
            RenderTexture.active = block.texture;
            GL.Clear(true, true, Color.clear, 1.0f);

            // To remind custom filter creators to use the atlas UVs, we draw a colored margin around the texture.
            // Leave a 2 pixels gab to avoid filtering issues.
            DrawMargins(block.texture, k_LeftMargin-2, k_TopMargin-2, k_RightMargin-2, k_BottomMargin-2, Color.pink);

            RenderTexture.active = old;

            return true;
        }

        static Material s_MarginMat = null;

        static void DrawMargins(RenderTexture rt, int leftMargin, int topMargin, int rightMargin, int bottomMargin, Color color)
        {
            if (s_MarginMat == null)
            {
                s_MarginMat = new Material(Shader.Find("Unlit/Color"));
                s_MarginMat.hideFlags = HideFlags.HideAndDontSave;
            }

            s_MarginMat.SetPass(0);
            s_MarginMat.SetColor("_Color", color);

            GL.Viewport(new Rect(0, 0, rt.width, rt.height));
            GL.modelview = Matrix4x4.identity;
            GL.LoadProjectionMatrix(Matrix4x4.Ortho(0, rt.width, rt.height, 0, -1, 1));

            GL.Begin(GL.TRIANGLES);

            // Top
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(rt.width, 0, 0);
            GL.Vertex3(leftMargin, topMargin, 0);
            GL.Vertex3(leftMargin, topMargin, 0);
            GL.Vertex3(rt.width, 0, 0);
            GL.Vertex3(rt.width - rightMargin, topMargin, 0);

            // Left
            GL.Vertex3(0, 0, 0);
            GL.Vertex3(leftMargin, topMargin, 0);
            GL.Vertex3(0, rt.height, 0);
            GL.Vertex3(leftMargin, topMargin, 0);
            GL.Vertex3(leftMargin, rt.height - bottomMargin, 0);
            GL.Vertex3(0, rt.height, 0);

            // Right
            GL.Vertex3(rt.width, 0, 0);
            GL.Vertex3(rt.width, rt.height, 0);
            GL.Vertex3(rt.width - rightMargin, topMargin, 0);
            GL.Vertex3(rt.width - rightMargin, topMargin, 0);
            GL.Vertex3(rt.width, rt.height, 0);
            GL.Vertex3(rt.width - rightMargin, rt.height - bottomMargin, 0);

            // Bottom
            GL.Vertex3(0, rt.height, 0);
            GL.Vertex3(leftMargin, rt.height - bottomMargin, 0);
            GL.Vertex3(rt.width, rt.height, 0);
            GL.Vertex3(leftMargin, rt.height - bottomMargin, 0);
            GL.Vertex3(rt.width - rightMargin, rt.height - bottomMargin, 0);
            GL.Vertex3(rt.width, rt.height, 0);

            GL.End();
        }

        static public void ReleaseTextureForAtlasBlock(AtlasBlock block)
        {
            RenderTexture texture = block.texture;
            if (texture != null)
                RenderTexture.ReleaseTemporary(texture);
        }
    }
}
