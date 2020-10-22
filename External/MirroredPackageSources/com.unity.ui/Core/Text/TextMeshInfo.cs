using System;

namespace UnityEngine.UIElements
{
    enum VertexSortingOrder { Normal, Reverse }

    /// <summary>
    /// Structure which contains the vertex attributes (geometry) of the text object.
    /// </summary>
    internal struct TextMeshInfo
    {
        static readonly Color32 k_DefaultColor = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);

        public int vertexCount;

        public Vector3[] vertices;

        public Vector2[] uvs0;
        public Vector2[] uvs2;
        public Color32[] colors32;
        public int[] triangles;

        public Material material;
    }
}
