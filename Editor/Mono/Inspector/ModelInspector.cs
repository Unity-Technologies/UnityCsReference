// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;


namespace UnityEditor
{
    [CustomEditor(typeof(Mesh))]
    [CanEditMultipleObjects]
    internal class ModelInspector : Editor
    {
        private PreviewRenderUtility m_PreviewUtility;
        private Material m_Material;
        private Material m_WireMaterial;
        public Vector2 previewDir = new Vector2(-120, 20);


        internal static Material CreateWireframeMaterial()
        {
            var shader = Shader.FindBuiltin("Internal-Colored.shader");
            if (!shader)
            {
                Debug.LogWarning("Could not find Colored builtin shader");
                return null;
            }
            var mat = new Material(shader);
            mat.hideFlags = HideFlags.HideAndDontSave;
            mat.SetColor("_Color", new Color(0, 0, 0, 0.3f));
            mat.SetInt("_ZWrite", 0);
            mat.SetFloat("_ZBias", -1.0f);
            return mat;
        }

        void Init()
        {
            if (m_PreviewUtility == null)
            {
                m_PreviewUtility = new PreviewRenderUtility();
                m_PreviewUtility.camera.fieldOfView = 30.0f;
                m_Material = EditorGUIUtility.GetBuiltinExtraResource(typeof(Material), "Default-Material.mat") as Material;
                m_WireMaterial = CreateWireframeMaterial();
            }
        }

        public override void OnPreviewSettings()
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
                return;
            GUI.enabled = true;
            Init();
        }

        internal static void RenderMeshPreview(
            Mesh mesh,
            PreviewRenderUtility previewUtility,
            Material litMaterial,
            Material wireMaterial,
            Vector2 direction,
            int meshSubset)
        {
            if (mesh == null || previewUtility == null)
                return;

            Bounds bounds = mesh.bounds;
            float halfSize = bounds.extents.magnitude;
            float distance = 4.0f * halfSize;

            previewUtility.camera.transform.position = -Vector3.forward * distance;
            previewUtility.camera.transform.rotation = Quaternion.identity;
            previewUtility.camera.nearClipPlane = distance - halfSize * 1.1f;
            previewUtility.camera.farClipPlane = distance + halfSize * 1.1f;

            previewUtility.lights[0].intensity = 1.4f;
            previewUtility.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0);
            previewUtility.lights[1].intensity = 1.4f;

            previewUtility.ambientColor = new Color(.1f, .1f, .1f, 0);
            RenderMeshPreviewSkipCameraAndLighting(mesh, bounds, previewUtility, litMaterial, wireMaterial, null, direction, meshSubset);
        }

        internal static void RenderMeshPreviewSkipCameraAndLighting(
            Mesh mesh,
            Bounds bounds,
            PreviewRenderUtility previewUtility,
            Material litMaterial,
            Material wireMaterial,
            MaterialPropertyBlock customProperties,
            Vector2 direction,
            int meshSubset) // -1 for whole mesh
        {
            if (mesh == null || previewUtility == null)
                return;

            Quaternion rot = Quaternion.Euler(direction.y, 0, 0) * Quaternion.Euler(0, direction.x, 0);
            Vector3 pos = rot * (-bounds.center);

            bool oldFog = RenderSettings.fog;
            Unsupported.SetRenderSettingsUseFogNoDirty(false);

            int submeshes = mesh.subMeshCount;
            if (litMaterial != null)
            {
                previewUtility.camera.clearFlags = CameraClearFlags.Nothing;
                if (meshSubset < 0 || meshSubset >= submeshes)
                {
                    for (int i = 0; i < submeshes; ++i)
                        previewUtility.DrawMesh(mesh, pos, rot, litMaterial, i, customProperties);
                }
                else
                    previewUtility.DrawMesh(mesh, pos, rot, litMaterial, meshSubset, customProperties);
                previewUtility.Render();
            }

            if (wireMaterial != null)
            {
                previewUtility.camera.clearFlags = CameraClearFlags.Nothing;
                GL.wireframe = true;
                if (meshSubset < 0 || meshSubset >= submeshes)
                {
                    for (int i = 0; i < submeshes; ++i)
                        previewUtility.DrawMesh(mesh, pos, rot, wireMaterial, i, customProperties);
                }
                else
                    previewUtility.DrawMesh(mesh, pos, rot, wireMaterial, meshSubset, customProperties);
                previewUtility.Render();
                GL.wireframe = false;
            }

            Unsupported.SetRenderSettingsUseFogNoDirty(oldFog);
        }

        private void DoRenderPreview()
        {
            RenderMeshPreview(target as Mesh, m_PreviewUtility, m_Material, m_WireMaterial, previewDir, -1);
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
            {
                //Debug.Log("Could not generate static preview. Render texture not supported by hardware.");
                return null;
            }

            Init();

            m_PreviewUtility.BeginStaticPreview(new Rect(0, 0, width, height));

            DoRenderPreview();

            return m_PreviewUtility.EndStaticPreview();
        }

        public override bool HasPreviewGUI()
        {
            return (target != null);
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
            {
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 40), "Mesh preview requires\nrender texture support");
                return;
            }

            Init();

            previewDir = PreviewGUI.Drag2D(previewDir, r);

            if (Event.current.type != EventType.Repaint)
                return;

            m_PreviewUtility.BeginPreview(r, background);

            DoRenderPreview();

            m_PreviewUtility.EndAndDrawPreview(r);
        }

        // A minimal list of settings to be shown in the Asset Store preview inspector
        internal override void OnAssetStoreInspectorGUI()
        {
            OnInspectorGUI();
        }

        public void OnDestroy()
        {
            if (m_PreviewUtility != null)
            {
                m_PreviewUtility.Cleanup();
                m_PreviewUtility = null;
            }
            if (m_WireMaterial)
            {
                DestroyImmediate(m_WireMaterial, true);
            }
        }

        public override string GetInfoString()
        {
            Mesh mesh = target as Mesh;
            string info = mesh.vertexCount + " verts, " + InternalMeshUtil.GetPrimitiveCount(mesh) + " tris";
            int submeshes = mesh.subMeshCount;
            if (submeshes > 1)
                info += ", " + submeshes + " submeshes";

            int blendShapeCount = mesh.blendShapeCount;
            if (blendShapeCount > 1)
                info += ", " + blendShapeCount + " blendShapes";

            info += "\n" + InternalMeshUtil.GetVertexFormat(mesh);
            return info;
        }
    }
}
