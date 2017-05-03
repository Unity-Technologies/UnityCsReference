// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [CustomEditor(typeof(BillboardAsset))]
    [CanEditMultipleObjects]
    internal class BillboardAssetInspector : Editor
    {
        private SerializedProperty m_Width;
        private SerializedProperty m_Height;
        private SerializedProperty m_Bottom;
        private SerializedProperty m_Images;
        private SerializedProperty m_Vertices;
        private SerializedProperty m_Indices;
        private SerializedProperty m_Material;

        private bool m_PreviewShaded = true;
        private Vector2 m_PreviewDir = new Vector2(-120, 20);
        private PreviewRenderUtility m_PreviewUtility;
        private Mesh m_ShadedMesh;
        private Mesh m_GeometryMesh;
        private MaterialPropertyBlock m_ShadedMaterialProperties;
        private Material m_GeometryMaterial;
        private Material m_WireframeMaterial;

        private class GUIStyles
        {
            public readonly GUIContent m_Shaded = new GUIContent("Shaded");
            public readonly GUIContent m_Geometry = new GUIContent("Geometry");
            public readonly GUIStyle m_DropdownButton = new GUIStyle("MiniPopup");
        };

        private static GUIStyles s_Styles = null;
        private static GUIStyles Styles
        {
            get
            {
                if (s_Styles == null)
                    s_Styles = new GUIStyles();
                return s_Styles;
            }
        }

        private void OnEnable()
        {
            m_Width = serializedObject.FindProperty("width");
            m_Height = serializedObject.FindProperty("height");
            m_Bottom = serializedObject.FindProperty("bottom");
            m_Images = serializedObject.FindProperty("imageTexCoords");
            m_Vertices = serializedObject.FindProperty("vertices");
            m_Indices = serializedObject.FindProperty("indices");
            m_Material = serializedObject.FindProperty("material");
        }

        private void OnDisable()
        {
            if (m_PreviewUtility != null)
            {
                m_PreviewUtility.Cleanup();
                m_PreviewUtility = null;
                DestroyImmediate(m_ShadedMesh, true);
                DestroyImmediate(m_GeometryMesh, true);
                m_GeometryMaterial = null;
                if (m_WireframeMaterial != null)
                {
                    DestroyImmediate(m_WireframeMaterial, true);
                }
            }
        }

        private void InitPreview()
        {
            if (m_PreviewUtility == null)
            {
                m_PreviewUtility = new PreviewRenderUtility();
                m_ShadedMesh = new Mesh();
                m_ShadedMesh.hideFlags = HideFlags.HideAndDontSave;
                m_ShadedMesh.MarkDynamic();
                m_GeometryMesh = new Mesh();
                m_GeometryMesh.hideFlags = HideFlags.HideAndDontSave;
                m_GeometryMesh.MarkDynamic();
                m_ShadedMaterialProperties = new MaterialPropertyBlock();
                m_GeometryMaterial = EditorGUIUtility.GetBuiltinExtraResource(typeof(Material), "Default-Material.mat") as Material;
                m_WireframeMaterial = ModelInspector.CreateWireframeMaterial();
                EditorUtility.SetCameraAnimateMaterials(m_PreviewUtility.camera, true);
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Width);
            EditorGUILayout.PropertyField(m_Height);
            EditorGUILayout.PropertyField(m_Bottom);
            EditorGUILayout.PropertyField(m_Material);

            serializedObject.ApplyModifiedProperties();
        }

        public override bool HasPreviewGUI()
        {
            return (target != null);
        }

        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
                return null;

            InitPreview();

            m_PreviewUtility.BeginStaticPreview(new Rect(0, 0, width, height));

            DoRenderPreview(true);

            return m_PreviewUtility.EndStaticPreview();
        }

        public override void OnPreviewSettings()
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
                return;

            bool switchable = m_Material.objectReferenceValue != null;
            GUI.enabled = switchable;
            if (!switchable)
                m_PreviewShaded = false;

            var content = m_PreviewShaded ? Styles.m_Shaded : Styles.m_Geometry;
            var rect = GUILayoutUtility.GetRect(content, Styles.m_DropdownButton, GUILayout.Width(75));

            if (EditorGUI.DropdownButton(rect, content, FocusType.Passive, Styles.m_DropdownButton))
            {
                GUIUtility.hotControl = 0;
                var dropDownMenu = new GenericMenu();
                dropDownMenu.AddItem(Styles.m_Shaded, m_PreviewShaded, () => m_PreviewShaded = true);
                dropDownMenu.AddItem(Styles.m_Geometry, !m_PreviewShaded, () => m_PreviewShaded = false);
                dropDownMenu.DropDown(rect);
            }
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
            {
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 40), "Preview requires\nrender texture support");
                return;
            }

            InitPreview();

            m_PreviewDir = PreviewGUI.Drag2D(m_PreviewDir, r);

            if (Event.current.type != EventType.Repaint)
                return;

            m_PreviewUtility.BeginPreview(r,  background);

            DoRenderPreview(m_PreviewShaded);

            m_PreviewUtility.EndAndDrawPreview(r);
        }

        public override string GetInfoString()
        {
            string info = String.Format("{0} verts, {1} tris, {2} images",
                    m_Vertices.arraySize,
                    m_Indices.arraySize / 3,
                    m_Images.arraySize);
            return info;
        }

        // Make a mesh out of the BillboardAsset that is used for rendering by SpeedTree billboard shader.
        // Vertices are expanded by the shader therefore the mesh vertices are not used (filled by zeroes).
        private void MakeRenderMesh(Mesh mesh, BillboardAsset billboard)
        {
            mesh.SetVertices(Enumerable.Repeat(Vector3.zero, billboard.vertexCount).ToList());
            mesh.SetColors(Enumerable.Repeat(Color.black, billboard.vertexCount).ToList());
            mesh.SetUVs(0, billboard.GetVertices().ToList());
            mesh.SetUVs(1, Enumerable.Repeat(new Vector4(1.0f, 1.0f, 0.0f, 0.0f), billboard.vertexCount).ToList());
            mesh.SetTriangles(billboard.GetIndices().Select(v => (int)v).ToList(), 0);
        }

        // Make a mesh out of the BillboardAsset that is suitable for previewing the geometry.
        // The vertices are expanded and made double-face.
        private void MakePreviewMesh(Mesh mesh, BillboardAsset billboard)
        {
            float width = billboard.width;
            float height = billboard.height;
            float bottom = billboard.bottom;
            mesh.SetVertices(Enumerable.Repeat(
                    billboard.GetVertices().Select(v => new Vector3(
                            (v.x - 0.5f) * width,
                            v.y * height + bottom,
                            0)),
                    // Repeat the sequence twice
                    2).SelectMany(s => s).ToList());

            // (0,0,1) for the front-facing vertices and (0,0,-1) for the back-facing vertices
            mesh.SetNormals(
                Enumerable.Repeat(Vector3.forward, billboard.vertexCount).Concat(
                    Enumerable.Repeat(-Vector3.forward, billboard.vertexCount)).ToList());

            // make a new triangle list with second half triangles flipped
            var indices = new int[billboard.indexCount * 2];
            var billboardIndices = billboard.GetIndices();
            for (int i = 0; i < billboard.indexCount / 3; ++i)
            {
                indices[i * 3 + 0] = billboardIndices[i * 3 + 0];
                indices[i * 3 + 1] = billboardIndices[i * 3 + 1];
                indices[i * 3 + 2] = billboardIndices[i * 3 + 2];
                indices[i * 3 + 0 + billboard.indexCount] = billboardIndices[i * 3 + 2];
                indices[i * 3 + 1 + billboard.indexCount] = billboardIndices[i * 3 + 1];
                indices[i * 3 + 2 + billboard.indexCount] = billboardIndices[i * 3 + 0];
            }
            mesh.SetTriangles(indices, 0);
        }

        private void DoRenderPreview(bool shaded)
        {
            var billboard = target as BillboardAsset;

            Bounds bounds = new Bounds(
                    new Vector3(0, (m_Height.floatValue + m_Bottom.floatValue) * 0.5f, 0),
                    new Vector3(m_Width.floatValue, m_Height.floatValue, m_Width.floatValue));

            float halfSize = bounds.extents.magnitude;
            float distance = 8.0f * halfSize;

            var rotation = Quaternion.Euler(-m_PreviewDir.y, -m_PreviewDir.x, 0);
            m_PreviewUtility.camera.transform.rotation = rotation;
            m_PreviewUtility.camera.transform.position = rotation * (-Vector3.forward * distance);
            m_PreviewUtility.camera.nearClipPlane = distance - halfSize * 1.1f;
            m_PreviewUtility.camera.farClipPlane = distance + halfSize * 1.1f;

            m_PreviewUtility.lights[0].intensity = 1.4f;
            m_PreviewUtility.lights[0].transform.rotation = rotation * Quaternion.Euler(40f, 40f, 0);
            m_PreviewUtility.lights[1].intensity = 1.4f;
            m_PreviewUtility.ambientColor = new Color(.1f, .1f, .1f, 0);
            if (shaded)
            {
                MakeRenderMesh(m_ShadedMesh, billboard);
                billboard.MakeMaterialProperties(m_ShadedMaterialProperties, m_PreviewUtility.camera);
                ModelInspector.RenderMeshPreviewSkipCameraAndLighting(m_ShadedMesh, bounds, m_PreviewUtility, billboard.material, null, m_ShadedMaterialProperties, new Vector2(0, 0), -1);
            }
            else
            {
                MakePreviewMesh(m_GeometryMesh, billboard);
                ModelInspector.RenderMeshPreviewSkipCameraAndLighting(m_GeometryMesh, bounds, m_PreviewUtility, m_GeometryMaterial, m_WireframeMaterial, null, new Vector2(0, 0), -1);
            }
        }
    }
}
