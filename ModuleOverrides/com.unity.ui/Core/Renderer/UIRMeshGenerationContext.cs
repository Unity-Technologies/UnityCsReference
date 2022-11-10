// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// Represents a vertex of geometry for drawing content of <see cref="VisualElement"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        /// <summary>
        /// A special value representing the near clipping plane. Always use this value as the vertex position's z component when building 2D (flat) UI geometry.
        /// </summary>
        public readonly static float nearZ = UIRUtility.k_MeshPosZ;

        /// <summary>
        /// Describes the vertex's position.
        /// </summary>
        /// <remarks>
        /// Note this value is a <see cref="Vector3"/>. If the vertex represents flat UI geometry, set the z component of this position field to <see cref="Vertex.nearZ"/>. The position value is relative to the <see cref="VisualElement"/>'s local rectangle top-left corner. The coordinate system is X+ to the right, and Y+ goes down. The unit of position is <see cref="VisualElement"/> points. When the vertices are indexed, the triangles described must follow clock-wise winding order given that Y+ goes down.
        /// </remarks>
        public Vector3 position;
        /// <summary>
        /// A color value for the vertex.
        /// </summary>
        /// <remarks>
        /// This value is multiplied by any other color information of the <see cref="VisualElement"/> (e.g. texture). Use <see cref="Color.white"/> to disable tinting on the vertex.
        /// </remarks>
        public Color32 tint;
        /// <summary>
        /// The UV coordinate of the vertex.
        /// </summary>
        /// <remarks>
        /// This is used to sample the required region of the associated texture if any. Values outside the range 0..1 are currently not supported and could lead to undefined results.
        /// </remarks>
        public Vector2 uv;
        internal Color32 xformClipPages; // Top-left of xform and clip pages: XY,XY
        internal Color32 ids; //XYZW (xform,clip,opacity,color/textcore)
        internal Color32 flags; //X (flags) Y (textcore-dilate) Z (is-arc) W (is-dynamic-color)
        internal Color32 opacityColorPages; //XY (opacity) ZW (color/textcore page)
        internal Color32 settingIndex; // XY (SVG setting) ZW (unused)
        internal Vector4 circle; // XY (outer) ZW (inner)
        internal float textureId;

        // Winding order of vertices matters. CCW is for clipped meshes.
    }

    /// <summary>
    /// A class that represents the vertex and index data allocated for drawing the content of a <see cref="VisualElement"/>.
    /// </summary>
    /// <remarks>
    /// You can use this object to fill the values for the vertices and indices only during a callback to the <see cref="VisualElement.generateVisualContent"/> delegate. Do not store the passed <see cref="MeshWriteData"/> outside the scope of <see cref="VisualElement.generateVisualContent"/> as Unity could recycle it for other callbacks.
    /// </remarks>
    public class MeshWriteData
    {
        internal MeshWriteData() {}  // Don't want users to instantiate this class themselves

        /// <summary>
        /// The number of vertices successfully allocated for <see cref="VisualElement"/> content drawing.
        /// </summary>
        public int vertexCount { get { return m_Vertices.Length; } }

        /// <summary>
        /// The number of indices successfully allocated for <see cref="VisualElement"/> content drawing.
        /// </summary>
        public int indexCount { get { return m_Indices.Length; } }

        /// <undoc/>
        [Obsolete("Texture coordinates are now automatically remapped by the renderer. You are no longer required to remap the UV coordinates in the provided rectangle.")]
        public Rect uvRegion => new Rect(0, 0, 1, 1);

        /// <summary>
        /// Assigns the value of the next vertex of the allocated vertices list.
        /// </summary>
        /// <param name="vertex">The value of the next vertex.</param>
        /// <remarks>
        /// Used to iteratively fill the values of the allocated vertices via repeated calls to this function until all values have been provided. This way of filling vertex data is mutually exclusive with the use of <see cref="SetAllVertices"/>.
        /// After each invocation to this function, the internal counter for the next vertex is automatically incremented.
        /// When this method is called, it is not possible to use <see cref="SetAllVertices"/> to fill the vertices.
        ///
        /// Note that calling <see cref="SetNextVertex"/> fewer times than the allocated number of vertices will leave the remaining vertices with random values as <see cref="MeshGenerationContext.Allocate"/> does not initialize the returned data to 0 to avoid redundant work.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetNextVertex(Vertex vertex) { m_Vertices[currentVertex++] = vertex; }

        /// <summary>
        /// Assigns the value of the next index of the allocated indices list.
        /// </summary>
        /// <param name="index">The value of the next index.</param>
        /// <remarks>
        /// Used to iteratively fill the values of the allocated indices via repeated calls to this function until all values have been provided. This way of filling index data is mutually exclusive with the use of <see cref="SetAllIndices"/>.
        /// After each invocation to this function, the internal counter for the next index is automatically incremented.
        /// When this method is called, it is not possible to use <see cref="SetAllIndices"/> to fill the indices.
        /// The index values provided refer directly to the vertices allocated in the same <see cref="MeshWriteData"/> object. Thus, an index of 0 means the first vertex and index 1 means the second vertex and so on.
        /// </remarks>
        /// <remarks>
        /// Note that calling <see cref="SetNextIndex"/> fewer times than the allocated number of indices will leave the remaining indices with random values as <see cref="MeshGenerationContext.Allocate"/> does not initialize the returned data to 0 to avoid redundant work.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetNextIndex(UInt16 index) { m_Indices[currentIndex++] = index; }

        /// <summary>
        /// Fills the values of the allocated vertices with values copied directly from an array.
        /// When this method is called, it is not possible to use <see cref="SetNextVertex"/> to fill the allocated vertices array.
        /// </summary>
        /// <param name="vertices">The array of vertices to copy from. The length of the array must match the allocated vertex count.</param>
        /// <remarks>
        /// When this method is called, it is not possible to use <see cref="SetNextVertex"/> to fill the vertices.
        /// </remarks>
        /// <example>
        /// <code>
        /// public class MyVisualElement : VisualElement
        /// {
        ///     void MyGenerateVisualContent(MeshGenerationContext mgc)
        ///     {
        ///         var meshWriteData = mgc.Allocate(4, 6);
        ///         // meshWriteData has been allocated with 6 indices for 2 triangles
        ///
        ///         // ... set the vertices
        ///
        ///         // Set indices for the first triangle
        ///         meshWriteData.SetNextIndex(0);
        ///         meshWriteData.SetNextIndex(1);
        ///         meshWriteData.SetNextIndex(2);
        ///
        ///         // Set indices for the second triangle
        ///         meshWriteData.SetNextIndex(2);
        ///         meshWriteData.SetNextIndex(1);
        ///         meshWriteData.SetNextIndex(3);
        ///     }
        /// }
        /// </code>
        /// </example>
        public void SetAllVertices(Vertex[] vertices)
        {
            if (currentVertex == 0)
            {
                m_Vertices.CopyFrom(vertices);
                currentVertex = m_Vertices.Length;
            }
            else throw new InvalidOperationException("SetAllVertices may not be called after using SetNextVertex");
        }

        /// <summary>
        /// Fills the values of the allocated vertices with values copied directly from an array.
        /// When this method is called, it is not possible to use <see cref="SetNextVertex"/> to fill the allocated vertices array.
        /// </summary>
        /// <param name="vertices">The array of vertices to copy from. The length of the array must match the allocated vertex count.</param>
        /// <remarks>
        /// When this method is called, it is not possible to use <see cref="SetNextVertex"/> to fill the vertices.
        /// </remarks>
        public void SetAllVertices(NativeSlice<Vertex> vertices)
        {
            if (currentVertex == 0)
            {
                m_Vertices.CopyFrom(vertices);
                currentVertex = m_Vertices.Length;
            }
            else throw new InvalidOperationException("SetAllVertices may not be called after using SetNextVertex");
        }

        /// <summary>
        /// Fills the values of the allocated indices with values copied directly from an array. Each 3 consecutive indices form a single triangle.
        /// </summary>
        /// <param name="indices">The array of indices to copy from. The length of the array must match the allocated index count.</param>
        /// <remarks>
        /// When this method is called, it is not possible to use <see cref="SetNextIndex"/> to fill the indices.
        /// </remarks>
        public void SetAllIndices(UInt16[] indices)
        {
            if (currentIndex == 0)
            {
                m_Indices.CopyFrom(indices);
                currentIndex = m_Indices.Length;
            }
            else throw new InvalidOperationException("SetAllIndices may not be called after using SetNextIndex");
        }

        /// <summary>
        /// Fills the values of the allocated indices with values copied directly from an array. Each 3 consecutive indices form a single triangle.
        /// </summary>
        /// <param name="indices">The array of indices to copy from. The length of the array must match the allocated index count.</param>
        /// <remarks>
        /// When this method is called, it is not possible to use <see cref="SetNextIndex"/> to fill the indices.
        /// </remarks>
        public void SetAllIndices(NativeSlice<UInt16> indices)
        {
            if (currentIndex == 0)
            {
                m_Indices.CopyFrom(indices);
                currentIndex = m_Indices.Length;
            }
            else throw new InvalidOperationException("SetAllIndices may not be called after using SetNextIndex");
        }

        internal void Reset(NativeSlice<Vertex> vertices, NativeSlice<UInt16> indices)
        {
            m_Vertices = vertices;
            m_Indices = indices;
            currentIndex = currentVertex = 0;
        }

        internal NativeSlice<Vertex> m_Vertices;
        internal NativeSlice<UInt16> m_Indices;
        internal int currentIndex, currentVertex;
    }

    internal struct ColorPage
    {
        public bool isValid;
        public Color32 pageAndID;

        public static ColorPage Init(UIR.RenderChain renderChain, UIR.BMPAlloc alloc)
        {
            bool isValid = alloc.IsValid();
            return new ColorPage() {
                isValid = isValid,
                pageAndID = isValid ? renderChain.shaderInfoAllocator.ColorAllocToVertexData(alloc) : new Color32()
            };
        }

        public MeshBuilderNative.NativeColorPage ToNativeColorPage()
        {
            return new MeshBuilderNative.NativeColorPage() {
                isValid = isValid ? 1 : 0,
                pageAndID = pageAndID
            };
        }
    }

    // Keeping it internal for now
    struct MeshGenerationNode
    {
        internal Entry placeholder;
    }

    /// <summary>
    /// Provides methods for generating a <see cref="VisualElement"/>'s visual content during the <see cref="generateVisualContent"/> callback.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Visual content is generated by using the <see cref="Painter2D"/> object, or manually by allocating a mesh using the <see cref="MeshGenerationContext.Allocate"/>
    /// method and then filling the vertices and indices.
    /// </para>
    /// <para>
    /// To use the painter object, access the <see cref="MeshGenerationContext.painter2D"/> property, and then use it to
    /// issue drawing commands. You can find an example in the <see cref="Painter2D"/> documentation.
    /// </para>
    /// <para>
    /// If you manually provide content with <see cref="MeshGenerationContext.Allocate"/> and also provide a texture during the allocation, you can use the <see cref="Vertex.uv"/>
    /// vertex values to map it to the resulting mesh. UI Toolkit might store the texture in an internal atlas.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// <![CDATA[
    /// class TexturedElement : VisualElement
    /// {
    ///     static readonly Vertex[] k_Vertices = new Vertex[4];
    ///     static readonly ushort[] k_Indices = { 0, 1, 2, 2, 3, 0 };
    ///
    ///     static TexturedElement()
    ///     {
    ///         k_Vertices[0].tint = Color.white;
    ///         k_Vertices[1].tint = Color.white;
    ///         k_Vertices[2].tint = Color.white;
    ///         k_Vertices[3].tint = Color.white;
    ///
    ///         k_Vertices[0].uv = new Vector2(0, 0);
    ///         k_Vertices[1].uv = new Vector2(0, 1);
    ///         k_Vertices[2].uv = new Vector2(1, 1);
    ///         k_Vertices[3].uv = new Vector2(1, 0);
    ///     }
    ///
    ///     public TexturedElement()
    ///     {
    ///         generateVisualContent += OnGenerateVisualContent;
    ///         m_Texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/tex.png");
    ///     }
    ///
    ///     Texture2D m_Texture;
    ///
    ///     void OnGenerateVisualContent(MeshGenerationContext mgc)
    ///     {
    ///         Rect r = contentRect;
    ///         if (r.width < 0.01f || r.height < 0.01f)
    ///             return; // Skip rendering when too small.
    ///
    ///         float left = 0;
    ///         float right = r.width;
    ///         float top = 0;
    ///         float bottom = r.height;
    ///
    ///         k_Vertices[0].position = new Vector3(left, bottom, Vertex.nearZ);
    ///         k_Vertices[1].position = new Vector3(left, top, Vertex.nearZ);
    ///         k_Vertices[2].position = new Vector3(right, top, Vertex.nearZ);
    ///         k_Vertices[3].position = new Vector3(right, bottom, Vertex.nearZ);
    ///
    ///         MeshWriteData mwd = mgc.Allocate(k_Vertices.Length, k_Indices.Length, m_Texture);
    ///         mwd.SetAllVertices(k_Vertices);
    ///         mwd.SetAllIndices(k_Indices);
    ///     }
    /// }
    /// ]]>
    /// </code>
    /// </example>
    public class MeshGenerationContext
    {
        [Flags]
        internal enum MeshFlags
        {
            None = 0,
            SkipDynamicAtlas = 1 << 1
        }

        /// <summary>
        /// The element for which <see cref="VisualElement.generateVisualContent"/> was invoked.
        /// </summary>
        public VisualElement visualElement { get; private set; }

        /// <summary>
        /// The vector painter object used to issue drawing commands.
        /// </summary>
        public Painter2D painter2D
        {
            get
            {
                if (m_Painter2D == null)
                    m_Painter2D = new Painter2D(this);
                return m_Painter2D;
            }
        }
        Painter2D m_Painter2D;

        MeshWriteDataPool m_MeshWriteDataPool;
        TempAllocator<Vertex> m_VertexPool;
        TempAllocator<ushort> m_IndexPool;

        internal IMeshGenerator meshGenerator { get; set; }
        internal EntryRecorder entryRecorder { get; private set; }

        internal MeshGenerationContext(MeshWriteDataPool meshWriteDataPool, EntryPool entryPool, TempAllocator<Vertex> vertexPool, TempAllocator<ushort> indexPool)
        {
            m_MeshWriteDataPool = meshWriteDataPool;
            m_VertexPool = vertexPool;
            m_IndexPool = indexPool;

            entryRecorder = new EntryRecorder(entryPool);
            meshGenerator = new MeshGenerator(this);
        }

        static readonly ProfilerMarker k_AllocateMarker = new ProfilerMarker("UIR.MeshGenerationContext.Allocate");
        static readonly ProfilerMarker k_DrawVectorImageMarker = new ProfilerMarker("UIR.MeshGenerationContext.DrawVectorImage");

        /// <summary>
        /// Allocates and draws the specified number of vertices and indices required to express geometry for drawing the content of a <see cref="VisualElement"/>.
        /// </summary>
        /// <param name="vertexCount">The number of vertices to allocate. The maximum is 65535 (or UInt16.MaxValue).</param>
        /// <param name="indexCount">The number of triangle list indices to allocate. Each 3 indices represent one triangle, so this value should be multiples of 3.</param>
        /// <param name="texture">An optional texture to be applied on the triangles allocated. Pass null to rely on vertex colors only.</param>
        /// <remarks>
        /// See <see cref="Vertex.position"/> for details on geometry generation conventions. If a valid texture was passed, then the returned <see cref="MeshWriteData"/> will also describe a rectangle for the UVs to use to sample the passed texture. This is needed because textures passed to this API can be internally copied into a larger atlas.
        /// </remarks>
        /// <returns>An object that gives access to the newely allocated data. If the returned vertex count is 0, then allocation failed (the system ran out of memory).</returns>
        public MeshWriteData Allocate(int vertexCount, int indexCount, Texture texture = null)
        {
            using (k_AllocateMarker.Auto())
            {
                MeshWriteData mwd = m_MeshWriteDataPool.Get();
                if (vertexCount == 0 || indexCount == 0)
                {
                    mwd.Reset(new NativeSlice<Vertex>(), new NativeSlice<ushort>());
                    return mwd;
                }

                if (vertexCount > UIRenderDevice.maxVerticesPerPage)
                    throw new ArgumentOutOfRangeException(nameof(vertexCount), $"Attempting to allocate {vertexCount} vertices which exceeds the limit of {UIRenderDevice.maxVerticesPerPage}.");

                NativeSlice<Vertex> vertices = m_VertexPool.Alloc(vertexCount);
                NativeSlice<ushort> indices = m_IndexPool.Alloc(indexCount);

                Debug.Assert(vertices.Length == vertexCount);
                Debug.Assert(indices.Length == indexCount);

                mwd.Reset(vertices, indices);

                entryRecorder.DrawMesh(mwd.m_Vertices, mwd.m_Indices, texture, false);

                return mwd;
            }
        }

        internal void AllocateTempMesh(int vertexCount, int indexCount, out NativeSlice<Vertex> vertices, out NativeSlice<ushort> indices)
        {
            if (vertexCount == 0 || indexCount == 0)
            {
                vertices = new NativeSlice<Vertex>();
                indices = new NativeSlice<ushort>();
                return;
            }

            if (vertexCount > UIRenderDevice.maxVerticesPerPage)
                throw new ArgumentOutOfRangeException(nameof(vertexCount), $"Attempting to allocate {vertexCount} vertices which exceeds the limit of {UIRenderDevice.maxVerticesPerPage}.");

            vertices = m_VertexPool.Alloc(vertexCount);
            indices = m_IndexPool.Alloc(indexCount);
        }

        /// <summary>
        /// Draws a <see cref="VectorImage" /> asset.
        /// </summary>
        /// <param name="vectorImage">The vector image to draw.</param>
        /// <param name="offset">The position offset where to draw the vector image.</param>
        /// <param name="rotationAngle">The rotation of the vector image.</param>
        /// <param name="scale">The scale of the vector image</param>
        public void DrawVectorImage(VectorImage vectorImage, Vector2 offset, Angle rotationAngle, Vector2 scale)
        {
            using (k_DrawVectorImageMarker.Auto())
                meshGenerator.DrawVectorImage(vectorImage, offset, rotationAngle, scale);
        }

        /// <summary>
        /// Draw a string of text.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="pos">The start position where the text will be displayed.</param>
        /// <param name="fontSize">The font size to use.</param>
        /// <param name="color">The text color.</param>
        /// <param name="font">The font asset to use.</param>
        public void DrawText(string text, Vector2 pos, float fontSize, Color color, FontAsset font = null)
        {
            if (font == null)
                font = TextUtilities.GetFontAsset(visualElement);
            meshGenerator.DrawText(text, pos, fontSize, color, font);
        }

        bool m_HasTarget;

        internal void Begin(MeshGenerationNode node, VisualElement ve)
        {
            if (node.placeholder == null)
                throw new ArgumentException($"The state of the provided {nameof(MeshGenerationNode)} is invalid (entry is null).");
            if (node.placeholder.firstChild != null)
                throw new ArgumentException($"The state of the provided {nameof(MeshGenerationNode)} is invalid (entry isn't empty).");

            entryRecorder.Begin(node.placeholder);
            visualElement = ve;
            m_HasTarget = true;
            meshGenerator.currentElement = ve;
        }

        internal void End()
        {
            if (!m_HasTarget)
                throw new InvalidOperationException($"{nameof(End)} can only be called after a successful call to {nameof(Begin)}.");

            meshGenerator.currentElement = null;
            m_HasTarget = false;
            visualElement = null;
            entryRecorder.End();
            m_Painter2D?.Reset();
        }

        #region Dispose Pattern

        internal bool disposed { get; private set; }


        internal void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                m_Painter2D?.Dispose();
                m_Painter2D = null;
                m_MeshWriteDataPool = null;
                m_VertexPool = null;
                m_IndexPool = null;
                entryRecorder = null;
                meshGenerator = null;

            }
            // else DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern
    }
}
