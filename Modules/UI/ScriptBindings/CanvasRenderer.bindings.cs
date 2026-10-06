// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine
{
    ///<summary>A component that will render to the screen after all normal rendering has completed when attached to a <see cref="Canvas" />. Designed for GUI application.</summary>
    ///<seealso cref="Canvas" />
    [NativeClass("UI::CanvasRenderer", PersistentTypeId = 222),
     NativeHeader("Modules/UI/CanvasRenderer.h")]
    [UIModuleHelpURL("class-CanvasRenderer")]
    public sealed partial class CanvasRenderer : Component
    {
        internal CanvasRenderer(global::UnityEngine.EntityId id) : base(id) {}
        public CanvasRenderer() {}
        ///<summary>Enable 'render stack' pop draw call.</summary>
        ///<remarks>When rendering using the hierarchy the renderer can insert a 'pop'. The pop instruction is executed after all children have been rendered. The canvas renderer is rerendered using the configured pop materials.
        ///
        ///See: <see cref="SetPopMaterial" /><see cref="popMaterialCount" />.</remarks>
        public extern bool hasPopInstruction { get; set; }
        ///<summary>The number of materials usable by this renderer.</summary>
        public extern int materialCount { get; set; }
        ///<summary>The number of materials usable by this renderer. Used internally for masking.</summary>
        public extern int popMaterialCount { get; set; }
        ///<summary>Depth of the renderer relative to the root canvas.</summary>
        public extern int absoluteDepth { get; }
        ///<summary>True if any change has occured that would invalidate the positions of generated geometry.</summary>
        public extern bool hasMoved { get; }
        ///<summary>Indicates whether geometry emitted by this renderer can be ignored when the vertex color alpha is close to zero for every vertex of the mesh.</summary>
        public extern bool cullTransparentMesh { get; set; }
        ///<summary>True if rect clipping has been enabled on this renderer.
        ///</summary>
        ///<seealso cref="CanvasRenderer.EnableRectClipping" />
        ///<seealso cref="CanvasRenderer.DisableRectClipping" />
        [NativeProperty("RectClipping", false, TargetType.Function)] public extern bool hasRectClipping { get; }
        ///<summary>Depth of the renderer realative to the parent canvas.</summary>
        [NativeProperty("Depth", false, TargetType.Function)] public extern int relativeDepth { get; }
        ///<summary>Indicates whether geometry emitted by this renderer is ignored.</summary>
        [NativeProperty("ShouldCull", false, TargetType.Function)] public extern bool cull { get; set; }

        ///<summary>Is the UIRenderer a mask component.</summary>
        ///<remarks>If the UI renderer is configured to be a masking component then children components will only render if they intersect the mask area created by this pass.</remarks>
        [Obsolete("isMask is no longer supported.See EnableClipping for vertex clipping configuration", false)]
        public bool isMask { get; set; }

        ///<summary>Set the color of the renderer. Will be multiplied with the <see cref="UIVertex" /> color and the <see cref="Canvas" /> color.</summary>
        ///<param name="color">Renderer multiply color.</param>
        public extern void SetColor(Color color);
        ///<summary>Get the current color of the renderer.</summary>
        public extern Color GetColor();
        ///<summary>Enables rect clipping on the CanvasRendered. Geometry outside of the specified rect will be clipped (not rendered).</summary>
        ///<seealso cref="CanvasRenderer.DisableRectClipping" />
        public extern void EnableRectClipping(Rect rect);
        ///<summary>The clipping softness to apply to the renderer.</summary>
        ///<remarks>Clipping softness is a linear alpha fade of clippingSoftness pixels. It gets passed to the shader through the _MaskSoftnessX and _MaskSoftnessY properties.</remarks>
        public extern Vector2 clippingSoftness { get; set; }
        ///<summary>Disables rectangle clipping for this CanvasRenderer.</summary>
        ///<seealso cref="CanvasRenderer.EnableRectClipping" />
        public extern void DisableRectClipping();
        ///<summary>Set the material for the canvas renderer. If a texture is specified then it will be used as the 'MainTex' instead of the material's 'MainTex'.
        ///</summary>
        ///<param name="material">Material for rendering.</param>
        ///<param name="index">Material index.</param>
        ///<seealso cref="CanvasRenderer.materialCount" />
        ///<seealso cref="CanvasRenderer.SetTexture" />
        public extern void SetMaterial(Material material, int index);
        ///<summary>Gets the current <see cref="Material" /> assigned to the CanvasRenderer.</summary>
        ///<param name="index">The material index to retrieve (0 if this parameter is omitted).</param>
        ///<returns>Result.</returns>
        public extern Material GetMaterial(int index);
        ///<summary>Set the material for the canvas renderer. Used internally for masking.</summary>
        public extern void SetPopMaterial(Material material, int index);
        ///<summary>Gets the current <see cref="Material" /> assigned to the CanvasRenderer. Used internally for masking.</summary>
        public extern Material GetPopMaterial(int index);
        ///<summary>Sets the texture used by this renderer's material.</summary>
        public extern void SetTexture(Texture texture);
        ///<summary>Get the number of secondary textures usable by this renderer.</summary>
        ///<returns>The number of secondary textures.</returns>
        public extern int GetSecondaryTextureCount();
        ///<summary>Set the number of secondary textures usable by this renderer. If the size is increased then the new data is initialized with default empty values.</summary>
        ///<param name="size">The new size of the secondary texture array usable by this renderer.</param>
        public extern void SetSecondaryTextureCount(int size);
        ///<summary>Get the shader property name of the secondary texture at the specifed index.</summary>
        ///<param name="index">The index of the secondary texture.</param>
        ///<returns>The shader property name of the secondary texture at the index.</returns>
        public extern string GetSecondaryTextureName(int index);
        ///<summary>Get the secondary texture at the specifed index.</summary>
        ///<param name="index">The index of the secondary texture.</param>
        ///<returns>The secondary texture at the index.</returns>
        public extern Texture2D GetSecondaryTexture(int index);
        ///<summary>Sets the secondary texture usable by this renderer at the specified index with the specified shader property name.</summary>
        ///<param name="index">The index in the secondary texture array of this renderer where to set data.</param>
        ///<param name="name">The shader property name associated with the secondary texture.</param>
        ///<param name="texture">The secondary texture usable by this renderer in a shader.</param>
        public extern void SetSecondaryTexture(int index, string name, Texture2D texture);
        ///<summary>The Alpha Texture that will be passed to the Shader under the _AlphaTex property.</summary>
        ///<param name="texture">The Texture to be passed.</param>
        public extern void SetAlphaTexture(Texture texture);
        ///<summary>Sets the Mesh used by this renderer. Note the Mesh must be read/write enabled.</summary>
        public extern void SetMesh(Mesh mesh);
        ///<summary>Returns the current mesh used to render the canvas content into.</summary>
        ///<returns>The current mesh for the canvas.</returns>
        public extern Mesh GetMesh();

        ///<summary>Remove all cached vertices.</summary>
        public extern void Clear();

        ///<summary>Get the current alpha of the renderer.</summary>
        public float GetAlpha()
        {
            return GetColor().a;
        }

        ///<summary>Set the alpha of the renderer. Will be multiplied with the <see cref="UIVertex" /> alpha and the <see cref="Canvas" /> alpha.</summary>
        ///<param name="alpha">Alpha.</param>
        public void SetAlpha(float alpha)
        {
            var color = GetColor();
            color.a = alpha;
            SetColor(color);
        }

        ///<summary>Get the final inherited alpha calculated by including all the parent alphas from included parent CanvasGroups.</summary>
        ///<remarks>Alpha is calculated by getting the alpha from all parent CanvasGroups (if GetIgnoreParentGroups is false) and multiplying the original alpha.</remarks>
        ///<returns>The calculated inherited alpha.</returns>
        public extern float GetInheritedAlpha();

        ///<summary>Set the material for the canvas renderer. If a texture is specified then it will be used as the 'MainTex' instead of the material's 'MainTex'.
        ///</summary>
        ///<param name="material">Material for rendering.</param>
        ///<param name="texture">Material texture overide.</param>
        ///<seealso cref="CanvasRenderer.materialCount" />
        ///<seealso cref="CanvasRenderer.SetTexture" />
        public void SetMaterial(Material material, Texture texture)
        {
            materialCount = Math.Max(1, materialCount);
            SetMaterial(material, 0);
            SetTexture(texture);
        }

        ///<summary>Gets the current <see cref="Material" /> assigned to the CanvasRenderer.</summary>
        ///<returns>Result.</returns>
        public Material GetMaterial()
        {
            return GetMaterial(0);
        }

        ///<summary>Given a list of UIVertex, split the stream into its component types.</summary>
        public static void SplitUIVertexStreams(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector4> uv0S, List<Vector4> uv1S,
            List<Vector3> normals, List<Vector4> tangents, List<int> indices)
        {
            SplitUIVertexStreams(verts, positions, colors, uv0S, uv1S, new List<Vector4>(), new List<Vector4>(), normals, tangents, new List<Vector4>(), indices);
        }

        ///<summary>Given a list of UIVertex, split the stream into its component types.</summary>
        public static void SplitUIVertexStreams(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector4> uv0S, List<Vector4> uv1S,
            List<Vector4> uv2S, List<Vector4> uv3S, List<Vector3> normals, List<Vector4> tangents, List<int> indices)
        {
            SplitUIVertexStreams(verts, positions, colors, uv0S, uv1S, uv2S, uv3S, normals, tangents, new List<Vector4>(), indices);
        }
        ///<summary>Given a list of UIVertex, split the stream into its component types.</summary>
        public static void SplitUIVertexStreams(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector4> uv0S, List<Vector4> uv1S,
            List<Vector4> uv2S, List<Vector4> uv3S, List<Vector3> normals, List<Vector4> tangents, List<Vector4> prevPositions, List<int> indices)
        {
            SplitUIVertexStreamsInternal(NoAllocHelpers.CreateReadOnlySpan(verts), positions, colors, uv0S, uv1S, uv2S, uv3S, normals, tangents, prevPositions);
            SplitIndicesStreamsInternal(verts, indices);
        }

        ///<summary>Convert a set of vertex components into a stream of UIVertex.</summary>
        public static void CreateUIVertexStream(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector4> uv0S, List<Vector4> uv1S, List<Vector3> normals, List<Vector4> tangents, List<int> indices)
        {
            var defaultValues = new List<Vector4>();
            NoAllocHelpers.EnsureListElemCount(defaultValues, positions.Count);
            CreateUIVertexStream(verts, positions, colors, uv0S, uv1S, defaultValues, defaultValues, normals, tangents, defaultValues, indices);
        }

        ///<summary>Convert a set of vertex components into a stream of UIVertex.</summary>
        public static void CreateUIVertexStream(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector4> uv0S, List<Vector4> uv1S, List<Vector4> uv2S, List<Vector4> uv3S, List<Vector3> normals, List<Vector4> tangents, List<int> indices)
        {
            var defaultValues = new List<Vector4>();
            NoAllocHelpers.EnsureListElemCount(defaultValues, positions.Count);
            CreateUIVertexStream(verts, positions, colors, uv0S, uv1S, uv2S, uv3S, normals, tangents, defaultValues, indices);
        }
        public static void CreateUIVertexStream(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector4> uv0S, List<Vector4> uv1S, List<Vector4> uv2S, List<Vector4> uv3S, List<Vector3> normals, List<Vector4> tangents, List<Vector4> prevPositions, List<int> indices)
        {
            CreateUIVertexStreamInternal(verts, NoAllocHelpers.CreateReadOnlySpan(positions), NoAllocHelpers.CreateReadOnlySpan(colors),
                NoAllocHelpers.CreateReadOnlySpan(uv0S), NoAllocHelpers.CreateReadOnlySpan(uv1S), NoAllocHelpers.CreateReadOnlySpan(uv2S),
                NoAllocHelpers.CreateReadOnlySpan(uv3S), NoAllocHelpers.CreateReadOnlySpan(normals), NoAllocHelpers.CreateReadOnlySpan(tangents),
                NoAllocHelpers.CreateReadOnlySpan(prevPositions), NoAllocHelpers.CreateReadOnlySpan(indices));
        }

        ///<summary>Take the UIVertex stream and split it into the corresponding arrays (positions, colors, uv0s, uv1s, normals and tangents).</summary>
        ///<param name="verts">The UIVertex list to split.</param>
        ///<param name="positions">The destination list for the verts positions.</param>
        ///<param name="colors">The destination list for the verts colors.</param>
        ///<param name="uv0S">The destination list for the verts uv0s.</param>
        ///<param name="uv1S">The destination list for the verts uv1s.</param>
        ///<param name="normals">The destination list for the verts normals.</param>
        ///<param name="tangents">The destination list for the verts tangents.</param>
        public static void AddUIVertexStream(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector4> uv0S, List<Vector4> uv1S, List<Vector3> normals, List<Vector4> tangents)
        {
            AddUIVertexStream(verts, positions, colors, uv0S, uv1S, new List<Vector4>(), new List<Vector4>(), normals, tangents, new List<Vector4>());
        }

        ///<summary>Take the UIVertex stream and split it into the corresponding arrays (positions, colors, uv0s, uv1s, uv2s, uv3s, normals and tangents).</summary>
        public static void AddUIVertexStream(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector4> uv0S, List<Vector4> uv1S, List<Vector4> uv2S, List<Vector4> uv3S, List<Vector3> normals, List<Vector4> tangents)
        {
            AddUIVertexStream(verts, positions, colors, uv0S, uv1S, uv2S, uv3S, normals, tangents, new List<Vector4>());
        }
        ///<summary>Take the UIVertex stream and split it into the corresponding arrays (positions, colors, uv0s, uv1s, uv2s, uv3s, normals, tangents and previous positions).</summary>
        public static void AddUIVertexStream(List<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector4> uv0S, List<Vector4> uv1S, List<Vector4> uv2S, List<Vector4> uv3S, List<Vector3> normals, List<Vector4> tangents, List<Vector4> prevPositions)
        {
            SplitUIVertexStreamsInternal(NoAllocHelpers.CreateReadOnlySpan(verts), positions, colors, uv0S, uv1S, uv2S, uv3S, normals, tangents, prevPositions);
        }

        ///<summary>Set the vertices for the <see cref="CanvasRenderer" />.</summary>
        ///<param name="vertices">Array of vertices to set.</param>
        [Obsolete("UI System now uses meshes.Generate a mesh and use 'SetMesh' instead", false)]
        public void SetVertices(List<UIVertex> vertices)
        {
            SetVertices(vertices.ToArray(), vertices.Count);
        }

        ///<summary>Set the vertices for the <see cref="CanvasRenderer" />.</summary>
        ///<param name="vertices">Array of vertices to set.</param>
        ///<param name="size">Number of vertices to set.</param>
        [Obsolete("UI System now uses meshes.Generate a mesh and use 'SetMesh' instead", false)]
        public void SetVertices(UIVertex[] vertices, int size)
        {
            var mesh = new Mesh();

            var positions = new List<Vector3>();
            var colors = new List<Color32>();
            var uv0S = new List<Vector4>();
            var uv1S = new List<Vector4>();
            var uv2S = new List<Vector4>();
            var uv3S = new List<Vector4>();
            var normals = new List<Vector3>();
            var tangents = new List<Vector4>();
            var prevPositions = new List<Vector4>();
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
                    prevPositions.Add(vertices[i + k].prevPosition);
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
            mesh.SetUVs(4, prevPositions);
            mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);
            SetMesh(mesh);
            DestroyImmediate(mesh);
        }

        private static void SplitIndicesStreamsInternal(List<UIVertex> verts, List<int> indices)
        {
            indices.Clear();
            for (var i = 0; i < verts.Count; ++i)
                indices.Add(i);
        }

        [StaticAccessor("UI", StaticAccessorType.DoubleColon)]
        private static extern void SplitUIVertexStreamsInternal(ReadOnlySpan<UIVertex> verts, List<Vector3> positions, List<Color32> colors, List<Vector4> uv0S, List<Vector4> uv1S, List<Vector4> uv2S,
            List<Vector4> uv3S, List<Vector3> normals, List<Vector4> tangents, List<Vector4> prevPositions);

        [StaticAccessor("UI", StaticAccessorType.DoubleColon)]
        private static extern void CreateUIVertexStreamInternal(List<UIVertex> verts, ReadOnlySpan<Vector3> positions, ReadOnlySpan<Color32> colors, ReadOnlySpan<Vector4> uv0S, ReadOnlySpan<Vector4> uv1S, ReadOnlySpan<Vector4> uv2S,
            ReadOnlySpan<Vector4> uv3S, ReadOnlySpan<Vector3> normals, ReadOnlySpan<Vector4> tangents, ReadOnlySpan<Vector4> prevPositions, ReadOnlySpan<int> indices);

        ///<exclude />
        public delegate void OnRequestRebuild();
        ///<summary>(Editor Only) Event that gets fired whenever the data in the <see cref="CanvasRenderer" /> gets invalidated and needs to be rebuilt.</summary>
        ///<remarks>For instance, whenever a Texture gets re-imported this event gets fired.</remarks>
        [AutoStaticsCleanupOnCodeReload]
        public static event OnRequestRebuild onRequestRebuild;

        [RequiredByNativeCode]
        internal static void RequestRefresh()
        {
            onRequestRebuild?.Invoke();
        }

    }
}
