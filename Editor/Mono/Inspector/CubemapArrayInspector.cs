// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;

namespace UnityEngine
{
    [ExcludeFromPreset]
    [CustomEditor(typeof(CubemapArray))]
    internal class CubemapArrayInspector : TextureInspector
    {
        static readonly int s_ShaderSliceIndex = Shader.PropertyToID("_SliceIndex");
        static readonly int s_ShaderExposure = Shader.PropertyToID("_Exposure");
        static readonly int s_ShaderMip = Shader.PropertyToID("_Mip");

        private PreviewRenderUtility m_PreviewUtility;
        private Material m_Material;
        private int m_Slice;
        private int m_Mip;
        private int m_MipCount;

        private Mesh m_Mesh;
        public Vector2 m_PreviewDir = new Vector2(0, 0);

        static class Styles
        {
            public static readonly GUIContent slice = EditorGUIUtility.TrTextContent("Slice", "Displayed array slice");
            public static readonly GUIStyle toolbarLabel = "toolbarLabel";
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            InitPreview();
        }

        protected override void OnDisable()
        {
            CleanupPreviewUtility();
            base.OnDisable();
        }

        void CleanupPreviewUtility()
        {
            if (m_PreviewUtility != null)
            {
                m_PreviewUtility.Cleanup();
                m_PreviewUtility = null;
            }
        }

        public override void OnPreviewSettings()
        {
            CubemapArray t = (CubemapArray)target;

            EditorGUI.BeginDisabledGroup(t.cubemapCount <= 1);
            EditorGUILayout.LabelField(Styles.slice, GUILayout.Width(40));
            m_Slice = EditorGUILayout.IntSlider(m_Slice, 0, t.cubemapCount - 1, GUILayout.Width(120));
            EditorGUI.EndDisabledGroup();
            m_Material.SetFloat(s_ShaderSliceIndex, (float)m_Slice);

            EditorGUI.BeginDisabledGroup(!TextureUtil.NeedsExposureControl(t));
            m_ExposureSliderValue = EditorGUIInternal.ExposureSlider(m_ExposureSliderValue, ref m_ExposureSliderMax, EditorStyles.toolbarSlider);
            EditorGUI.EndDisabledGroup();
            m_Material.SetFloat(s_ShaderExposure, GetExposureValueForTexture(t));

            EditorGUI.BeginDisabledGroup(m_MipCount == 0);
            GUILayout.Box(EditorGUIUtility.IconContent("PreTextureMipMapLow"), Styles.toolbarLabel);
            m_Mip = Mathf.RoundToInt(GUILayout.HorizontalSlider(m_Mip, m_MipCount - 1, 0, GUILayout.Width(64)));
            GUILayout.Box(EditorGUIUtility.IconContent("PreTextureMipMapHigh"), Styles.toolbarLabel);
            EditorGUI.EndDisabledGroup();
            m_Material.SetFloat(s_ShaderMip, m_Mip);
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (!SystemInfo.supportsCubemapArrayTextures || (m_Material != null && !m_Material.shader.isSupported))
            {
                EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 40), "Cubemap array preview is not supported");
                return;
            }

            m_PreviewDir = PreviewGUI.Drag2D(m_PreviewDir, r);

            if (Event.current.type == EventType.Repaint)
            {
                CubemapArray t = (CubemapArray)target;
                m_Material.mainTexture = t;
                const float previewDistance = 6.0f;

                m_PreviewUtility.BeginPreview(r, background);
                RenderCubemapSlice(m_Slice, previewDistance, m_PreviewDir);
                Texture renderedTexture = m_PreviewUtility.EndPreview();
                GUI.DrawTexture(r, renderedTexture, ScaleMode.StretchToFill, false);
            }

            EditorGUI.DropShadowLabel(new Rect(r.x, r.y + 10, r.width, 35),
                "Slice " + m_Slice + "\nMip " + m_Mip);
        }

        void InitPreview()
        {
            OnDisable();
            m_PreviewUtility = new PreviewRenderUtility();
            m_PreviewUtility.camera.fieldOfView = 15f;
            m_Mesh = PreviewRenderUtility.GetPreviewSphere();

            var t = target as CubemapArray;
            if (t == null)
                return;

            m_Material = (Material)EditorGUIUtility.LoadRequired("Previews/CubeArrayPreview.mat");
            m_Material.mainTexture = t;

            m_Slice = 0;
            m_Mip = Mathf.RoundToInt(GetMipLevelForRendering());
            m_MipCount = TextureUtil.GetMipmapCount(t);

            m_Material.SetFloat(s_ShaderSliceIndex, (float)m_Slice);
            m_Material.SetFloat(s_ShaderMip, m_Mip);
            m_Material.SetFloat(s_ShaderExposure, GetExposureValueForTexture(t));
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture || !SystemInfo.supportsCubemapArrayTextures)
                return null;

            InitPreview();
            if (m_Material == null || !m_Material.shader.isSupported)
                return null;

            CubemapArray texture = target as CubemapArray;
            if (texture == null)
                return null;

            m_PreviewUtility.BeginStaticPreview(new Rect(0, 0, width, height));

            // No lighting needed.
            m_PreviewUtility.ambientColor = Color.black;

            // Render all slices, from last to first.
            // Cap at 6 slices, similarly to Texture2DArray.
            const float previewDistance = 5.3f;
            const float sliceGap = 1.0f / 12.0f;
            int slicesToDraw = Mathf.Min(texture.cubemapCount, 6);
            float size = (1.0f - (slicesToDraw - 1) * sliceGap);
            for (int i = slicesToDraw - 1; i >= 0; --i)
            {
                float pos = i * sliceGap;
                m_PreviewUtility.camera.rect = new Rect(pos, pos, size, size);
                RenderCubemapSlice(i, previewDistance, Vector2.zero);
            }

            Texture2D preview = m_PreviewUtility.EndStaticPreview();
            CleanupPreviewUtility();
            return preview;
        }

        private void RenderCubemapSlice(int slice, float previewDistance, Vector2 previewDir)
        {
            if (m_Material == null || m_PreviewUtility == null || m_Mesh == null)
                return;

            m_Material.SetFloat(s_ShaderSliceIndex, (float)slice);

            m_PreviewUtility.camera.transform.position = -Vector3.forward * previewDistance;
            m_PreviewUtility.camera.transform.rotation = Quaternion.identity;
            Quaternion rot = Quaternion.Euler(previewDir.y, 0, 0) * Quaternion.Euler(0, previewDir.x, 0);

            m_PreviewUtility.DrawMesh(m_Mesh, Vector3.zero, rot, m_Material, 0);
            m_PreviewUtility.Render();
        }
    }
}
