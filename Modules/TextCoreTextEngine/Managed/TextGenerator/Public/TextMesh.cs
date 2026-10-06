// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine.TextCore.Generation
{
    ///<summary>The renderable geometry produced by the Advanced Text Generator for a piece of text.</summary>
    public struct TextMesh
    {
        ///<summary>The material of each sub-mesh, in sub-mesh order, ready to assign to the renderer that draws the filled mesh.</summary>
        public Material[] materials;

        ///<summary>The rendered size of the text, in pixels.</summary>
        public Vector2 size;

        ///<summary>Whether the text was truncated to fit the layout area.</summary>
        public bool isElided;

        internal TextMeshInfo[] meshInfos;

        ///<summary>Fills the mesh with the generated geometry, one sub-mesh per material.</summary>
        ///<remarks>Render sub-mesh i with <see cref="materials"/>[i].</remarks>
        ///<param name="mesh">The mesh to fill. Its previous content is cleared.</param>
        public void FillMesh(Mesh mesh)
        {
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));

            mesh.Clear();
            if (meshInfos == null || meshInfos.Length == 0)
                return;

            int vertexCount = 0;
            foreach (var meshInfo in meshInfos)
                vertexCount += meshInfo.vertices?.Length ?? 0;

            var vertices = new List<Vector3>(vertexCount);
            var uvs0 = new List<Vector2>(vertexCount);
            var uvs2 = new List<Vector2>(vertexCount);
            var colors = new List<Color32>(vertexCount);

            foreach (var meshInfo in meshInfos)
            {
                if (meshInfo.vertices == null)
                    continue;

                vertices.AddRange(meshInfo.vertices);
                uvs0.AddRange(meshInfo.uvs0);
                uvs2.AddRange(meshInfo.uvs2);
                colors.AddRange(meshInfo.colors);
            }

            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs0);
            mesh.SetUVs(1, uvs2);
            mesh.SetColors(colors);

            mesh.subMeshCount = meshInfos.Length;
            int baseVertex = 0;
            for (int m = 0; m < meshInfos.Length; m++)
            {
                mesh.SetTriangles(meshInfos[m].triangles, m, calculateBounds: false, baseVertex);
                baseVertex += meshInfos[m].vertices?.Length ?? 0;
            }

            mesh.RecalculateBounds();
        }

        ///<summary>Flattens every sub-mesh into a single list of vertices, for consumers of UIVertex geometry such as custom UI Graphics.</summary>
        ///<remarks>The per-material split required to correctly render SDF text is not preserved; use <see cref="FillMesh"/> and <see cref="materials"/> when it is needed.</remarks>
        ///<returns>The vertices of every sub-mesh, in generation order.</returns>
        public IList<UIVertex> ToUIVertices()
        {
            var result = new List<UIVertex>();
            if (meshInfos == null)
                return result;

            for (int m = 0; m < meshInfos.Length; m++)
            {
                var meshInfo = meshInfos[m];
                var positions = meshInfo.vertices;
                if (positions == null)
                    continue;

                for (int i = 0; i < positions.Length; i++)
                {
                    var vert = UIVertex.simpleVert;
                    vert.position = positions[i];
                    vert.color = meshInfo.colors[i];

                    var uv0 = meshInfo.uvs0[i];
                    vert.uv0 = new Vector4(uv0.x, uv0.y, 0f, 0f);

                    var uv2 = meshInfo.uvs2[i];
                    vert.uv1 = new Vector4(uv2.x, uv2.y, 0f, 0f);

                    result.Add(vert);
                }
            }

            return result;
        }
    }

    internal struct TextMeshInfo
    {
        internal Vector3[] vertices;
        internal Vector2[] uvs0;
        internal Vector2[] uvs2;
        internal Color32[] colors;
        internal int[] triangles;
    }
}
