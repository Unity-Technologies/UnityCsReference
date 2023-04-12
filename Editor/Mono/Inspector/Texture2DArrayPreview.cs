// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    internal class Texture2DArrayPreview
    {
        static readonly int s_ShaderColorMask = Shader.PropertyToID("_ColorMaskBits");
        static readonly int s_ShaderSliceIndex = Shader.PropertyToID("_SliceIndex");
        static readonly int s_ShaderMip = Shader.PropertyToID("_Mip");
        static readonly int s_ShaderToSrgb = Shader.PropertyToID("_ToSRGB");
        static readonly int s_ShaderIsNormalMap = Shader.PropertyToID("_IsNormalMap");

        // Preview settings
        Material m_Material;
        int m_Slice = 0;

        private Vector2 m_Pos;

        static class Styles
        {
            public static GUIContent smallZoom = EditorGUIUtility.IconContent("PreTextureMipMapLow");
            public static GUIContent largeZoom = EditorGUIUtility.IconContent("PreTextureMipMapHigh");
            public static GUIContent alphaIcon = EditorGUIUtility.IconContent("PreTextureAlpha");
            public static GUIContent RGBIcon = EditorGUIUtility.IconContent("PreTextureRGB");
            public static GUIContent arrayIcon = EditorGUIUtility.IconContent("Texture2DArray");
        }

        public float GetMipLevelForRendering(Texture texture, float mipLevel)
        {
            return Mathf.Min(mipLevel, TextureUtil.GetMipmapCount(texture));
        }

        public void SetShaderColorMask(TextureInspector.PreviewMode mode)
        {
            var mask = 15;
            switch (mode)
            {
                case TextureInspector.PreviewMode.RGB: mask = 7; break;
                case TextureInspector.PreviewMode.R: mask = 1; break;
                case TextureInspector.PreviewMode.G: mask = 2; break;
                case TextureInspector.PreviewMode.B: mask = 4; break;
                case TextureInspector.PreviewMode.A: mask = 8; break;
            }
            m_Material.SetFloat(s_ShaderColorMask, (float)mask);
        }

        public void OnPreviewSettings(Texture target)
        {
            Texture2DArray texture2DArray = target as Texture2DArray;
            RenderTexture renderTexture = target as RenderTexture;

            if (texture2DArray == null && renderTexture == null)
                return;

            if (texture2DArray != null)
            {
                m_Slice = (int)TextureInspector.PreviewSettingsSlider(Styles.arrayIcon, m_Slice, 0, texture2DArray.depth - 1, 60, 30, isInteger: true);
            }
            else
            {
                m_Slice = (int)TextureInspector.PreviewSettingsSlider(Styles.arrayIcon, m_Slice, 0, renderTexture.volumeDepth - 1, 60, 30, isInteger: true);
            }
        }

        public void OnPreviewGUI(Texture t, Rect r, GUIStyle background, float exposure, TextureInspector.PreviewMode previewMode, float mipLevel)
        {
            if (t == null)
                return;

            if (!SystemInfo.supports2DArrayTextures)
            {
                EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 40), "2D texture array preview not supported");
                return;
            }

            InitPreviewMaterialIfNeeded();
            m_Material.mainTexture = t;

            int effectiveSlice = GetEffectiveSlice(t);

            m_Material.SetFloat(s_ShaderSliceIndex, (float)effectiveSlice);
            m_Material.SetFloat(s_ShaderToSrgb, QualitySettings.activeColorSpace == ColorSpace.Linear ? 1.0f : 0.0f);
            m_Material.SetFloat(s_ShaderIsNormalMap, TextureInspector.IsNormalMap(t) ? 1.0f : 0.0f);

            SetShaderColorMask(previewMode);

            int texWidth = Mathf.Max(t.width, 1);
            int texHeight = Mathf.Max(t.height, 1);

            float effectiveMipLevel = GetMipLevelForRendering(t, mipLevel);
            float zoomLevel = Mathf.Min(Mathf.Min(r.width / texWidth, r.height / texHeight), 1);
            Rect wantedRect = new Rect(r.x, r.y, texWidth * zoomLevel, texHeight * zoomLevel);
            PreviewGUI.BeginScrollView(r, m_Pos, wantedRect, "PreHorizontalScrollbar",
                "PreHorizontalScrollbarThumb");
            FilterMode oldFilter = t.filterMode;
            TextureUtil.SetFilterModeNoDirty(t, FilterMode.Point);

            EditorGUI.DrawPreviewTexture(wantedRect, t, m_Material, ScaleMode.StretchToFill, 0, effectiveMipLevel, UnityEngine.Rendering.ColorWriteMask.All, exposure);

            TextureUtil.SetFilterModeNoDirty(t, oldFilter);

            int mipmapLimit = (t as Texture2DArray).activeMipmapLimit;
            int cpuMipLevel = Mathf.Min(TextureUtil.GetMipmapCount(t) - 1, (int)mipLevel + mipmapLimit);
            m_Pos = PreviewGUI.EndScrollView();

            GUIContent sliceAndMipLevelTextContent = new GUIContent($"Slice {effectiveSlice}");
            if (cpuMipLevel != 0)
                sliceAndMipLevelTextContent.text += cpuMipLevel != (int)effectiveMipLevel
                    ? $" | Mip {cpuMipLevel}\nMip {mipLevel} on GPU (Texture Limit)"
                    : $" | Mip {mipLevel}";

            EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 40), sliceAndMipLevelTextContent);
        }

        int GetEffectiveSlice(Texture t)
        {
            var texture2DArray = t as Texture2DArray;

            if (texture2DArray != null)
            {
                // If multiple objects are selected, we might be using a slice level before the maximum
                return Mathf.Clamp(m_Slice, 0, texture2DArray.depth - 1);
            }
            else
            {
                var renderTexture = t as RenderTexture;
                // If multiple objects are selected, we might be using a slice level before the maximum
                return Mathf.Clamp(m_Slice, 0, renderTexture.volumeDepth - 1);
            }
        }

        void InitPreviewMaterialIfNeeded()
        {
            if (m_Material == null)
                m_Material = (Material)EditorGUIUtility.LoadRequired("Previews/Preview2DTextureArrayMaterial.mat");
        }

        public Texture2D RenderStaticPreview(Texture target, string assetPath, Object[] subAssets, int width, int height)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture || !SystemInfo.supports2DArrayTextures)
                return null;

            var texture = target as Texture2DArray;
            if (texture == null)
                return null;

            var previewUtility = new PreviewRenderUtility();
            previewUtility.BeginStaticPreview(new Rect(0, 0, width, height));

            InitPreviewMaterialIfNeeded();
            m_Material.mainTexture = texture;
            m_Material.SetFloat(s_ShaderColorMask, 15.0f);
            m_Material.SetFloat(s_ShaderMip, 0);
            m_Material.SetFloat(s_ShaderToSrgb, 0.0f);
            m_Material.SetFloat(s_ShaderIsNormalMap, TextureInspector.IsNormalMap(texture) ? 1.0f : 0.0f);

            int sliceDistance = previewUtility.renderTexture.width / 12;
            var elementCount = Mathf.Min(texture.depth, 6);
            Rect screenRect = new Rect(), sourceRect = new Rect();
            var subRect = new Rect(0, 0, previewUtility.renderTexture.width - sliceDistance * (elementCount - 1), previewUtility.renderTexture.height - sliceDistance * (elementCount - 1));
            for (var el = elementCount - 1; el >= 0; --el)
            {
                m_Material.SetFloat(s_ShaderSliceIndex, (float)el);

                subRect.x = sliceDistance * el;
                subRect.y = previewUtility.renderTexture.height - subRect.height - sliceDistance * el;

                var aspect = texture.width / (float)texture.height;
                GUI.CalculateScaledTextureRects(subRect, ScaleMode.ScaleToFit, aspect, ref screenRect, ref sourceRect);
                Graphics.DrawTexture(screenRect, texture, sourceRect, 0, 0, 0, 0, Color.white, m_Material);
            }

            var res = previewUtility.EndStaticPreview();
            previewUtility.Cleanup();
            return res;
        }
    }
}
