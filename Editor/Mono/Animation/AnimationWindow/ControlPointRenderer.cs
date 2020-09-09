// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor
{
    //  Control point renderer
    internal class ControlPointRenderer
    {
        private class RenderChunk
        {
            public Mesh mesh;

            public List<Vector3> vertices;
            public List<Color32> colors;
            public List<Vector2> uvs;
            public List<int> indices;

            public bool isDirty = true;
        }

        private int m_RenderChunkIndex = -1;
        private List<RenderChunk> m_RenderChunks = new List<RenderChunk>();

        private Texture2D m_Icon;

        //  Can hold a maximum of 16250 control points.
        const int kMaxVertices = 65000;
        const string kControlPointRendererMeshName = "ControlPointRendererMesh";

        private static Material s_Material;
        public static Material material
        {
            get
            {
                if (!s_Material)
                {
                    Shader shader = (Shader)EditorGUIUtility.LoadRequired("Editors/AnimationWindow/ControlPoint.shader");
                    s_Material = new Material(shader);
                    s_Material.hideFlags = HideFlags.HideAndDontSave;
                }

                return s_Material;
            }
        }

        public ControlPointRenderer(Texture2D icon)
        {
            m_Icon = icon;
        }

        public void FlushCache()
        {
            for (int i = 0; i < m_RenderChunks.Count; ++i)
            {
                Object.DestroyImmediate(m_RenderChunks[i].mesh);
            }

            m_RenderChunks.Clear();
        }

        public void Clear()
        {
            for (int i = 0; i < m_RenderChunks.Count; ++i)
            {
                RenderChunk renderChunk = m_RenderChunks[i];

                renderChunk.mesh.Clear();

                renderChunk.vertices.Clear();
                renderChunk.colors.Clear();
                renderChunk.uvs.Clear();

                renderChunk.indices.Clear();

                renderChunk.isDirty = true;
            }

            m_RenderChunkIndex = 0;
        }

        public void Render()
        {
            Material mat = material;
            mat.SetTexture("_MainTex", m_Icon);
            mat.SetPass(0);

            for (int i = 0; i < m_RenderChunks.Count; ++i)
            {
                RenderChunk renderChunk = m_RenderChunks[i];

                if (renderChunk.isDirty)
                {
                    renderChunk.mesh.SetVertices(renderChunk.vertices);
                    renderChunk.mesh.SetColors(renderChunk.colors);
                    renderChunk.mesh.SetUVs(0, renderChunk.uvs);

                    renderChunk.mesh.SetIndices(renderChunk.indices, MeshTopology.Triangles, 0);

                    renderChunk.isDirty = false;
                }

                // Previous camera may still be active when calling DrawMeshNow.
                Camera.SetupCurrent(null);

                Graphics.DrawMeshNow(renderChunk.mesh, Handles.matrix);
            }
        }

        public void AddPoint(Rect rect, Color color)
        {
            RenderChunk renderChunk = GetRenderChunk();

            int baseIndex = renderChunk.vertices.Count;

            renderChunk.vertices.Add(new Vector3(rect.xMin, rect.yMin, 0.0f));
            renderChunk.vertices.Add(new Vector3(rect.xMax, rect.yMin, 0.0f));
            renderChunk.vertices.Add(new Vector3(rect.xMax, rect.yMax, 0.0f));
            renderChunk.vertices.Add(new Vector3(rect.xMin, rect.yMax, 0.0f));

            renderChunk.colors.Add(color);
            renderChunk.colors.Add(color);
            renderChunk.colors.Add(color);
            renderChunk.colors.Add(color);

            renderChunk.uvs.Add(new Vector2(0.0f, 0.0f));
            renderChunk.uvs.Add(new Vector2(1.0f, 0.0f));
            renderChunk.uvs.Add(new Vector2(1.0f, 1.0f));
            renderChunk.uvs.Add(new Vector2(0.0f, 1.0f));

            renderChunk.indices.Add(baseIndex);
            renderChunk.indices.Add(baseIndex + 1);
            renderChunk.indices.Add(baseIndex + 2);

            renderChunk.indices.Add(baseIndex);
            renderChunk.indices.Add(baseIndex + 2);
            renderChunk.indices.Add(baseIndex + 3);

            renderChunk.isDirty = true;
        }

        private RenderChunk GetRenderChunk()
        {
            RenderChunk renderChunk = null;
            if (m_RenderChunks.Count > 0)
            {
                while (m_RenderChunkIndex < m_RenderChunks.Count)
                {
                    renderChunk = m_RenderChunks[m_RenderChunkIndex];
                    // Dynamically create new render chunks when needed.
                    if ((renderChunk.vertices.Count + 4) > kMaxVertices)
                    {
                        m_RenderChunkIndex++;
                        renderChunk = null;
                        continue;
                    }

                    break;
                }

                if (renderChunk == null)
                {
                    renderChunk = CreateRenderChunk();
                }
            }
            else
            {
                renderChunk = CreateRenderChunk();
            }

            return renderChunk;
        }

        private RenderChunk CreateRenderChunk()
        {
            RenderChunk renderChunk = new RenderChunk();

            renderChunk.mesh = new Mesh();
            renderChunk.mesh.name = kControlPointRendererMeshName;
            renderChunk.mesh.hideFlags |= HideFlags.DontSave;

            renderChunk.vertices = new List<Vector3>();
            renderChunk.colors = new List<Color32>();
            renderChunk.uvs = new List<Vector2>();
            renderChunk.indices = new List<int>();

            m_RenderChunks.Add(renderChunk);
            m_RenderChunkIndex = m_RenderChunks.Count - 1;

            return renderChunk;
        }
    }
}
