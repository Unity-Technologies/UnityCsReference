// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.TextCore.LowLevel;

namespace UnityEngine.TextCore.Text
{

    /// <summary>
    /// Structure which contains the vertex attributes (geometry) of the text object, as well as the material to be used.
    /// </summary>
    [VisibleToOtherModules("UnityEngine.IMGUIModule", "UnityEngine.UIElementsModule")]
    internal struct MeshInfo
    {
        public int vertexCount;
        public TextCoreVertex[] vertexData;
        public Material material;


        [Ignore] public int vertexBufferSize;


        [Ignore] public bool applySDF = true;

        [VisibleToOtherModules("UnityEngine.UIElementsModule")]
        internal GlyphRenderMode glyphRenderMode;

        /// <summary>
        /// Function to pre-allocate vertex attributes for a mesh of size X.
        /// </summary>
        /// <param name="size"></param>
        public MeshInfo(int size, bool isIMGUI) : this()
        {
            material = null;

            if (isIMGUI)
            {
                // Limit the mesh to less than 65535 vertices which is the limit for Unity's Mesh.
                size = Mathf.Min(size, 16383);
            }

            int sizeX4 = size * 4;
            int sizeX6 = size * 6;

            vertexCount = 0;
            vertexBufferSize = sizeX4;

            vertexData = new TextCoreVertex[sizeX4];
         
            material = null;
            glyphRenderMode = 0;
        }

        /// <summary>
        /// Function to resized the content of MeshData and re-assign normals, tangents and triangles.
        /// </summary>
        /// <param name="size"></param>
        internal void ResizeMeshInfo(int size, bool isIMGUI)
        {
            if (isIMGUI)
            {
                // Limit the mesh to less than 65535 vertices which is the limit for Unity's Mesh.
                size = Mathf.Min(size, 16383);
            }

            int sizeX4 = size * 4;
            int sizeX6 = size * 6;

            vertexBufferSize = sizeX4;

            Array.Resize(ref vertexData, sizeX4);
        }

        /// <summary>
        /// Function to clear the vertices while preserving the Triangles, Normals and Tangents.
        /// </summary>
        internal void Clear(bool uploadChanges)
        {
            {
                if (vertexData == null) return;
                Array.Clear(vertexData, 0, vertexData.Length);
                vertexBufferSize = vertexData.Length;
            }
            vertexCount = 0;
        }

        /// <summary>
        /// Function to clear the vertices while preserving the Triangles, Normals and Tangents.
        /// </summary>
        internal void ClearUnusedVertices()
        {
            {
                int length = vertexData.Length - vertexCount;

                if (length > 0)
                    Array.Clear(vertexData, vertexCount, length);

                vertexBufferSize = vertexData.Length;
            }
          
        }

    }
}
