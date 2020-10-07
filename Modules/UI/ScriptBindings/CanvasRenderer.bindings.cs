// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [NativeClass("UI::CanvasRenderer"),
     NativeHeader("Modules/UI/CanvasRenderer.h")]
    public sealed class CanvasRenderer : Component
    {
        public extern bool hasPopInstruction { get; set; }
        public extern int materialCount { get; set; }
        public extern int popMaterialCount { get; set; }
        public extern int absoluteDepth { get; }
        public extern bool hasMoved { get; }
        public extern bool cullTransparentMesh { get; set; }
        [NativeProperty("RectClipping", false, TargetType.Function)] public extern bool hasRectClipping { get; }
        [NativeProperty("Depth", false, TargetType.Function)] public extern int relativeDepth { get; }
        [NativeProperty("ShouldCull", false, TargetType.Function)] public extern bool cull { get; set; }

        [Obsolete("isMask is no longer supported.See EnableClipping for vertex clipping configuration", false)]
        public bool isMask { get; set; }

        public extern void SetColor(Color color);
        public extern Color GetColor();
        public extern void EnableRectClipping(Rect rect);
        public extern Vector2 clippingSoftness { get; set; }
        public extern void DisableRectClipping();
        public extern void SetMaterial(Material material, int index);
        public extern Material GetMaterial(int index);
        public extern void SetPopMaterial(Material material, int index);
        public extern Material GetPopMaterial(int index);
        public extern void SetTexture(Texture texture);
        public extern void SetAlphaTexture(Texture texture);
        public extern void SetMesh(Mesh mesh);
        public extern void Clear();

        public float GetAlpha()
        {
            return GetColor().a;
        }

        public void SetAlpha(float alpha)
        {
            var color = GetColor();
            color.a = alpha;
            SetColor(color);
        }

        public extern float GetInheritedAlpha();

        public void SetMaterial(Material material, Texture texture)
        {
            materialCount = Math.Max(1, materialCount);
            SetMaterial(material, 0);
            SetTexture(texture);
        }

        public Material GetMaterial()
        {
            return GetMaterial(0);
        }

        public static void SplitUIVertexStreams(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector2> uv0S, List<Vector2> uv1S,
            List<Vector3> normals, List<Vector4> tangents, List<int> indices)
        {
            SplitUIVertexStreams(verts, positions, colors, uv0S, uv1S, new List<Vector2>(), new List<Vector2>(), normals, tangents, indices);
        }

        public static void SplitUIVertexStreams(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector2> uv0S, List<Vector2> uv1S,
            List<Vector2> uv2S, List<Vector2> uv3S, List<Vector3> normals, List<Vector4> tangents, List<int> indices)
        {
            SplitUIVertexStreamsInternal(verts, positions, colors, uv0S, uv1S, uv2S, uv3S, normals, tangents);
            SplitIndicesStreamsInternal(verts, indices);
        }

        public static void CreateUIVertexStream(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector2> uv0S, List<Vector2> uv1S, List<Vector3> normals, List<Vector4> tangents, List<int> indices)
        {
            CreateUIVertexStream(verts, positions, colors, uv0S, uv1S, new List<Vector2>(), new List<Vector2>(), normals, tangents, indices);
        }

        public static void CreateUIVertexStream(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector2> uv0S, List<Vector2> uv1S, List<Vector2> uv2S, List<Vector2> uv3S, List<Vector3> normals, List<Vector4> tangents, List<int> indices)
        {
            CreateUIVertexStreamInternal(verts, positions, colors, uv0S, uv1S, uv2S, uv3S, normals, tangents, indices);
        }

        public static void AddUIVertexStream(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector2> uv0S, List<Vector2> uv1S, List<Vector3> normals, List<Vector4> tangents)
        {
            AddUIVertexStream(verts, positions, colors, uv0S, uv1S, new List<Vector2>(), new List<Vector2>(), normals, tangents);
        }

        public static void AddUIVertexStream(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector2> uv0S, List<Vector2> uv1S, List<Vector2> uv2S, List<Vector2> uv3S, List<Vector3> normals, List<Vector4> tangents)
        {
            SplitUIVertexStreamsInternal(verts, positions, colors, uv0S, uv1S, uv2S, uv3S, normals, tangents);
        }

        [Obsolete("UI System now uses meshes.Generate a mesh and use 'SetMesh' instead", false)]
        public void SetVertices(List<UIVertex> vertices)
        {
            SetVertices(vertices.ToArray(), vertices.Count);
        }

        [Obsolete("UI System now uses meshes.Generate a mesh and use 'SetMesh' instead", false)]
        public void SetVertices(UIVertex[] vertices, int size)
        {
            var mesh = new Mesh();

            var positions = new List<Vector3>();
            var colors = new List<Color32>();
            var uv0S = new List<Vector2>();
            var uv1S = new List<Vector2>();
            var uv2S = new List<Vector2>();
            var uv3S = new List<Vector2>();
            var normals = new List<Vector3>();
            var tangents = new List<Vector4>();
            var indices = new List<int>();

            for (var i = 0; i < size; i += 4)
            {
                for (var k = 0; k < 4; k++)
                {
                    positions.Add(vertices[i + k].position);
                    colors.Add(vertices[i + k].color);
                    uv0S.Add(vertices[i + k].uv0);
                    uv1S.Add(vertices[i + k].uv1);
                    uv2S.Add(vertices[i + k].uv2);
                    uv3S.Add(vertices[i + k].uv3);
                    normals.Add(vertices[i + k].normal);
                    tangents.Add(vertices[i + k].tangent);
                }
                //Add the two triangles
                indices.Add(i);
                indices.Add(i + 1);
                indices.Add(i + 2);

                indices.Add(i + 2);
                indices.Add(i + 3);
                indices.Add(i);
            }

            mesh.SetVertices(positions);
            mesh.SetColors(colors);
            mesh.SetNormals(normals);
            mesh.SetTangents(tangents);
            mesh.SetUVs(0, uv0S);
            mesh.SetUVs(1, uv1S);
            mesh.SetUVs(2, uv2S);
            mesh.SetUVs(3, uv3S);
            mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
            SetMesh(mesh);
            DestroyImmediate(mesh);
        }

        [StaticAccessor("UI", StaticAccessorType.DoubleColon)]
        private static extern void SplitIndicesStreamsInternal(object verts, object indices);

        [StaticAccessor("UI", StaticAccessorType.DoubleColon)]
        private static extern void SplitUIVertexStreamsInternal(object verts, object positions, object colors, object uv0S, object uv1S, object uv2S,
            object uv3S, object normals, object tangents);

        [StaticAccessor("UI", StaticAccessorType.DoubleColon)]
        private static extern void CreateUIVertexStreamInternal(object verts, object positions, object colors, object uv0S, object uv1S, object uv2S,
            object uv3S, object normals, object tangents, object indices);

        public delegate void OnRequestRebuild();
        public static event OnRequestRebuild onRequestRebuild;

        [RequiredByNativeCode]
        internal static void RequestRefresh()
        {
            onRequestRebuild?.Invoke();
        }

    }
}
