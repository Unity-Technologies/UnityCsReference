// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [CustomEditor(typeof(Texture2DArray))]
    [CanEditMultipleObjects]
    class Texture2DArrayInspector : TextureInspector
    {
        static readonly int s_ShaderColorMask = Shader.PropertyToID("_ColorMask");
        static readonly int s_ShaderSliceIndex = Shader.PropertyToID("_SliceIndex");
        static readonly int s_ShaderMip = Shader.PropertyToID("_Mip");
        static readonly int s_ShaderToSrgb = Shader.PropertyToID("_ToSRGB");
        static readonly int s_ShaderIsNormalMap = Shader.PropertyToID("_IsNormalMap");

        GUIContent m_AlphaToggleContent;
        Material m_Material;
        int m_Slice;

        public override string GetInfoString()
        {
            var tex = (Texture2DArray)target;
            var info = $"{tex.width}x{tex.height} {tex.depth} slice{(tex.depth != 1 ? "s" : "")} {TextureUtil.GetTextureFormatString(tex.format)} {EditorUtility.FormatBytes(TextureUtil.GetRuntimeMemorySizeLong(tex))}";
            return info;
        }

        void SetShaderColorMask()
        {
            var mode = m_PreviewMode;
            var mask = 15;
            switch (mode)
            {
                case PreviewMode.RGB: mask = 7; break;
                case PreviewMode.R: mask = 1; break;
                case PreviewMode.G: mask = 2; break;
                case PreviewMode.B: mask = 4; break;
                case PreviewMode.A: mask = 8; break;
            }
            m_Material.SetInt(s_ShaderColorMask, mask);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            InitPreview();
            SetShaderColorMask();
        }

        public override void OnPreviewSettings()
        {
            InitPreview();

            var t = (Texture2DArray)target;
            m_Material.mainTexture = t;

            if (t.depth > 1)
            {
                m_Slice = (int)PreviewSettingsSlider(GUIContent.none, m_Slice, 0, t.depth - 1, 60, 30, isInteger: true);
                m_Material.SetInt(s_ShaderSliceIndex, m_Slice);
            }

            var prevColorMode = m_PreviewMode;
            base.OnPreviewSettings();
            if (m_PreviewMode != prevColorMode)
                SetShaderColorMask();
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            if (!SystemInfo.supports2DArrayTextures)
            {
                EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 40), "2D texture array preview not supported");
                return;
            }

            var t = (Texture2DArray)target;

            InitPreview();
            m_Material.mainTexture = t;

            // If multiple objects are selected, we might be using a slice level before the maximum
            int effectiveSlice = Mathf.Clamp(m_Slice, 0, t.depth - 1);

            m_Material.SetInt(s_ShaderSliceIndex, effectiveSlice);
            m_Material.SetInt(s_ShaderToSrgb, QualitySettings.activeColorSpace == ColorSpace.Linear ? 1 : 0);
            m_Material.SetInt(s_ShaderIsNormalMap, IsNormalMap(t) ? 1 : 0);

            int texWidth = Mathf.Max(t.width, 1);
            int texHeight = Mathf.Max(t.height, 1);

            float effectiveMipLevel = GetMipLevelForRendering();
            float zoomLevel = Mathf.Min(Mathf.Min(r.width / texWidth, r.height / texHeight), 1);
            Rect wantedRect = new Rect(r.x, r.y, texWidth * zoomLevel, texHeight * zoomLevel);
            PreviewGUI.BeginScrollView(r, m_Pos, wantedRect, "PreHorizontalScrollbar",
                "PreHorizontalScrollbarThumb");
            FilterMode oldFilter = t.filterMode;
            TextureUtil.SetFilterModeNoDirty(t, FilterMode.Point);

            EditorGUI.DrawPreviewTexture(wantedRect, t, m_Material, ScaleMode.StretchToFill, 0, effectiveMipLevel);

            TextureUtil.SetFilterModeNoDirty(t, oldFilter);

            m_Pos = PreviewGUI.EndScrollView();
            if (effectiveSlice != 0 || (int)effectiveMipLevel != 0)
            {
                EditorGUI.DropShadowLabel(new Rect(r.x, r.y + 10, r.width, 30),
                    "Slice " + effectiveSlice + "\nMip " + effectiveMipLevel);
            }
        }

        void InitPreview()
        {
            if (m_Material == null)
                m_Material = (Material)EditorGUIUtility.LoadRequired("Previews/Preview2DTextureArrayMaterial.mat");
            if (m_AlphaToggleContent == null)
                m_AlphaToggleContent = EditorGUIUtility.TrIconContent("PreTexA");
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture || !SystemInfo.supports2DArrayTextures)
                return null;

            var texture = target as Texture2DArray;
            if (texture == null)
                return null;

            var previewUtility = new PreviewRenderUtility();
            previewUtility.BeginStaticPreview(new Rect(0, 0, width, height));

            InitPreview();
            m_Material.mainTexture = texture;
            m_Material.SetInt(s_ShaderColorMask, 15);
            m_Material.SetFloat(s_ShaderMip, 0);
            m_Material.SetInt(s_ShaderToSrgb, 0);
            m_Material.SetInt(s_ShaderIsNormalMap, IsNormalMap(texture) ? 1 : 0);

            int sliceDistance = previewUtility.renderTexture.width / 12;
            var elementCount = Mathf.Min(texture.depth, 6);
            Rect screenRect = new Rect(), sourceRect = new Rect();
            var subRect = new Rect(0, 0, previewUtility.renderTexture.width - sliceDistance * (elementCount - 1), previewUtility.renderTexture.height - sliceDistance * (elementCount - 1));
            for (var el = elementCount - 1; el >= 0; --el)
            {
                m_Material.SetInt(s_ShaderSliceIndex, el);

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
