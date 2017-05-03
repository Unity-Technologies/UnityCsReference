// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;


namespace UnityEditor
{
    [CustomEditor(typeof(Texture3D))]
    [CanEditMultipleObjects]
    internal class Texture3DInspector : TextureInspector
    {
        private PreviewRenderUtility    m_PreviewUtility;
        private Material                m_Material;
        private Mesh                    m_Mesh;
        public Vector2                  m_PreviewDir = new Vector2(0, 0);

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }

        public override string GetInfoString()
        {
            Texture3D tex = target as Texture3D;

            string info = string.Format("{0}x{1}x{2} {3} {4}",
                    tex.width, tex.height, tex.depth,
                    TextureUtil.GetTextureFormatString(tex.format),
                    EditorUtility.FormatBytes(TextureUtil.GetRuntimeMemorySizeLong(tex)));

            return info;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (m_PreviewUtility != null)
            {
                m_PreviewUtility.Cleanup();
                m_PreviewUtility = null;
            }
        }

        public override void OnPreviewSettings()
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture || !SystemInfo.supports3DTextures)
                return;
            GUI.enabled = true;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture || !SystemInfo.supports3DTextures)
            {
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 40), "3D texture preview not supported");
                return;
            }

            m_PreviewDir = PreviewGUI.Drag2D(m_PreviewDir, r);

            if (Event.current.type != EventType.Repaint)
                return;

            InitPreview();
            m_Material.mainTexture = target as Texture;

            m_PreviewUtility.BeginPreview(r, background);
            bool oldFog = RenderSettings.fog;
            Unsupported.SetRenderSettingsUseFogNoDirty(false);

            m_PreviewUtility.camera.transform.position = -Vector3.forward * 3.0f;
            m_PreviewUtility.camera.transform.rotation = Quaternion.identity;
            Quaternion rot = Quaternion.Euler(m_PreviewDir.y, 0, 0) * Quaternion.Euler(0, m_PreviewDir.x, 0);
            m_PreviewUtility.DrawMesh(m_Mesh, Vector3.zero, rot, m_Material, 0);
            m_PreviewUtility.Render();

            Unsupported.SetRenderSettingsUseFogNoDirty(oldFog);
            m_PreviewUtility.EndAndDrawPreview(r);
        }

        void InitPreview()
        {
            if (m_PreviewUtility == null)
            {
                m_PreviewUtility = new PreviewRenderUtility();
                m_PreviewUtility.camera.fieldOfView = 30.0f;
            }

            if (m_Mesh == null)
            {
                m_Mesh = PreviewRenderUtility.GetPreviewSphere();
            }
            if (m_Material == null)
            {
                m_Material = EditorGUIUtility.LoadRequired("Previews/Preview3DTextureMaterial.mat") as Material;
            }
        }
    }
}
