// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Globalization;
using UnityEngine;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class CubemapPreview
    {
        private enum PreviewType
        {
            RGB = 0,
            Alpha = 1
        }
        // Preview settings
        [SerializeField]
        private PreviewType             m_PreviewType = PreviewType.RGB;
        [SerializeField]
        float                           m_MipLevel = 0.0F;
        private float                   m_Intensity = 1.0f;

        // Cached preview data
        private PreviewRenderUtility    m_PreviewUtility;
        private Mesh                    m_Mesh;
        public Vector2                  m_PreviewDir = new Vector2(0, 0);

        static class Styles
        {
            public static GUIStyle preButton = "preButton";
            public static GUIStyle preSlider = "preSlider";
            public static GUIStyle preSliderThumb = "preSliderThumb";
            public static GUIStyle preLabel = "preLabel";
            public static GUIContent smallZoom = EditorGUIUtility.IconContent("PreTextureMipMapLow");
            public static GUIContent largeZoom = EditorGUIUtility.IconContent("PreTextureMipMapHigh");
            public static GUIContent alphaIcon = EditorGUIUtility.IconContent("PreTextureAlpha");
            public static GUIContent RGBIcon = EditorGUIUtility.IconContent("PreTextureRGB");
        }

        public void OnDisable()
        {
            if (m_PreviewUtility != null)
            {
                m_PreviewUtility.Cleanup();
                m_PreviewUtility = null;
            }
        }

        public float mipLevel { get { return m_MipLevel; } set { m_MipLevel = value; } }

        // For mip maps we render by default with mipLevel 0 but allow for
        public float GetMipLevelForRendering(Texture texture)
        {
            return Mathf.Min(m_MipLevel, TextureUtil.GetMipmapCount(texture));
        }

        public void SetIntensity(float intensity)
        {
            m_Intensity = intensity;
        }

        void InitPreview()
        {
            // Initialized?
            if (m_PreviewUtility != null)
                return;

            m_PreviewUtility = new PreviewRenderUtility();
            m_PreviewUtility.camera.fieldOfView = 15f;
            m_Mesh = PreviewRenderUtility.GetPreviewSphere();
        }

        public void OnPreviewSettings(Object[] targets)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
                return;
            GUI.enabled = true;
            InitPreview();

            bool showMode = true;
            bool alphaOnly = true;
            //@TODO: Share some code with texture inspector???
            bool hasAlpha = false;
            int mipCount = 8;
            foreach (Texture t2 in targets)
            {
                mipCount = Mathf.Max(mipCount, TextureUtil.GetMipmapCount(t2));

                Cubemap cubemap = t2 as Cubemap;
                if (cubemap)
                {
                    TextureFormat format = cubemap.format;
                    if (!TextureUtil.IsAlphaOnlyTextureFormat(format))
                        alphaOnly = false;
                    if (TextureUtil.HasAlphaTextureFormat(format))
                    {
                        TextureUsageMode mode = TextureUtil.GetUsageMode(t2);
                        if (mode == TextureUsageMode.Default) // all other texture usage modes don't displayable alpha
                            hasAlpha = true;
                    }
                }
                else
                {
                    hasAlpha = true;
                    alphaOnly = false;
                }
            }

            if (alphaOnly)
            {
                m_PreviewType = PreviewType.Alpha;
                showMode = false;
            }
            else if (!hasAlpha)
            {
                m_PreviewType = PreviewType.RGB;
                showMode = false;
            }

            if (showMode)
            {
                GUIContent[] kPreviewIcons = { Styles.RGBIcon, Styles.alphaIcon };
                int index = (int)m_PreviewType;
                if (GUILayout.Button(kPreviewIcons[index], Styles.preButton))
                    m_PreviewType = (PreviewType)(++index % kPreviewIcons.Length);
            }

            GUI.enabled = (mipCount != 1);
            GUILayout.Box(Styles.smallZoom, Styles.preLabel);
            GUI.changed = false;
            m_MipLevel = Mathf.Round(GUILayout.HorizontalSlider(m_MipLevel, mipCount - 1, 0, Styles.preSlider, Styles.preSliderThumb, GUILayout.MaxWidth(64)));
            GUILayout.Box(Styles.largeZoom, Styles.preLabel);
            GUI.enabled = true;
        }

        public void OnPreviewGUI(Texture t, Rect r, GUIStyle background)
        {
            if (t == null)
                return;

            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
            {
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 40), "Cubemap preview requires\nrender texture support");
                return;
            }

            m_PreviewDir = PreviewGUI.Drag2D(m_PreviewDir, r);

            if (Event.current.type != EventType.Repaint)
                return;

            InitPreview();
            m_PreviewUtility.BeginPreview(r, background);
            const float previewDistance = 6.0f;

            RenderCubemap(t, m_PreviewDir, previewDistance);

            Texture renderedTexture = m_PreviewUtility.EndPreview();
            GUI.DrawTexture(r, renderedTexture, ScaleMode.StretchToFill, false);

            if (mipLevel != 0)
                EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 20), "Mip " + mipLevel);
        }

        public Texture2D RenderStaticPreview(Texture t, int width, int height)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
                return null;

            InitPreview();
            m_PreviewUtility.BeginStaticPreview(new Rect(0, 0, width, height));
            const float previewDistance = 5.3f;
            Vector2 previewDirection = new Vector2(0, 0);

            // When rendering the cubemap preview we don't need lighting so we provide a custom list with no lights.
            // If we don't do this and we are generating the preview for a point light cookie, if a light uses this cookie it will try to bind it which result in internal assert in AssetDatabase due to using the texture while building it.
            m_PreviewUtility.ambientColor = Color.black;

            RenderCubemap(t, previewDirection, previewDistance);

            return m_PreviewUtility.EndStaticPreview();
        }

        private void RenderCubemap(Texture t, Vector2 previewDir, float previewDistance)
        {
            m_PreviewUtility.camera.transform.position = -Vector3.forward * previewDistance;
            m_PreviewUtility.camera.transform.rotation = Quaternion.identity;
            Quaternion rot = Quaternion.Euler(previewDir.y, 0, 0) * Quaternion.Euler(0, previewDir.x, 0);

            var mat = EditorGUIUtility.LoadRequired("Previews/PreviewCubemapMaterial.mat") as Material;
            mat.mainTexture = t;

            mat.SetMatrix("_CubemapRotation", Matrix4x4.TRS(Vector3.zero, rot, Vector3.one));

            // -1 indicates "use regular sampling"; mips 0 and larger sample only that mip level for preview
            float mipLevel = GetMipLevelForRendering(t);
            mat.SetFloat("_Mip", mipLevel);
            mat.SetFloat("_Alpha", (m_PreviewType == PreviewType.Alpha) ? 1.0f : 0.0f);
            mat.SetFloat("_Intensity", m_Intensity);

            m_PreviewUtility.DrawMesh(m_Mesh, Vector3.zero, rot, mat, 0);
            m_PreviewUtility.Render();
        }
    }
}
