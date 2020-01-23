// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;


namespace UnityEditor
{
    [CustomEditor(typeof(Texture2DArray))]
    [CanEditMultipleObjects]
    internal class Texture2DArrayInspector : TextureInspector
    {
        private Material m_Material;
        private int m_Slice;
        private bool alphaOnly;

        public override string GetInfoString()
        {
            Texture2DArray tex = (Texture2DArray)target;

            string info = UnityString.Format("{0}x{1} {2} slice{5} {3} {4}",
                tex.width, tex.height, tex.depth,
                TextureUtil.GetTextureFormatString(tex.format),
                EditorUtility.FormatBytes(TextureUtil.GetRuntimeMemorySizeLong(tex)),
                tex.depth != 1 ? "s" : "");

            return info;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            InitPreview();
            alphaOnly = false;
            m_Material.SetInt("_AlphaOnly", alphaOnly ? 1 : 0);
        }

        public override void OnPreviewSettings()
        {
            if (m_Material == null)
                InitPreview();

            Texture2DArray t = (Texture2DArray)target;
            m_Material.mainTexture = t;

            if (t.depth > 1)
            {
                m_Slice = EditorGUILayout.IntSlider(m_Slice, 0, t.depth - 1, GUILayout.Width(120));
                m_Material.SetInt("_SliceIndex", m_Slice);
            }

            if (GUILayout.Toggle(alphaOnly, EditorGUIUtility.TrIconContent("PreTexA"), "toolbarbutton") != alphaOnly)
            {
                alphaOnly = !alphaOnly;
                m_Material.SetInt("_AlphaOnly", alphaOnly ? 1 : 0);
            }
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (!SystemInfo.supports2DArrayTextures)
            {
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 40), "2D texture array preview not supported");
                return;
            }

            Texture2DArray t = (Texture2DArray)target;

            if (Event.current.type == EventType.Repaint)
            {
                InitPreview();
                m_Material.mainTexture = t;

                // If multiple objects are selected, we might be using a slice level before the maximum
                int effectiveSlice = Mathf.Clamp(m_Slice, 0, t.depth - 1);

                m_Material.SetInt("_SliceIndex", effectiveSlice);

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
        }

        void InitPreview()
        {
            if (m_Material == null)
            {
                m_Material = (Material)EditorGUIUtility.LoadRequired("Previews/Preview2DTextureArrayMaterial.mat");
            }
        }

        internal override void OnHeaderIconGUI(Rect iconRect)
        {
            // Even though we have a Preview panel, we don't want to reuse it for the header icon, because it looks very odd when
            // you start scrubbing through different slices etc. Just render the default icon for Texture2DArray objects.
            GUI.Label(iconRect, AssetPreview.GetMiniThumbnail(target));
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            // It's not clear what a meaningful preview for a Texture2DArray would be - the first slice? Multiple slices composited?
            // Until we have a clear idea about the best way to do things, return null for now, to indicate no preview.
            return null;
        }
    }
}
