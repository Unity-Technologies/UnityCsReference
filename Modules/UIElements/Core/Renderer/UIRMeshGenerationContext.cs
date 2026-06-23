// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine.Bindings;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    enum VertexFlags : ushort
    {
        None                     = 0,
        RenderTypeSolid            = 0,
        RenderTypeText             = 1,
        RenderTypeTexture          = 2,
        RenderTypeDynamicTexture   = 3,
        RenderTypeSvgGradient      = 4,
        RenderTypeMask             = 0x7,
        IsArc                    = 1 << 3,
        DynamicColorDisabled     = 0 << 4,
        DynamicColorEnabled      = 1 << 4,
        DynamicColorEnabledText  = 2 << 4,
        DynamicColorMask         = DynamicColorEnabled | DynamicColorEnabledText,
    }

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
        internal Vector2 layoutUV; // Layout UV of Visual Element
        internal ushort clipRectId;
        internal ushort transformId;
        internal ushort dynamicColorOrTextCoreId;
        internal ushort opacityId;
        internal VertexFlags flags;
        internal ushort textureId;
        internal ushort svgGradientIndex;
        internal ushort _reserved;
        internal Vector4 circle; // XY (outer) ZW (inner) | X (Text Extra Dilate)

        // Winding order of vertices matters. CCW is for clipped meshes.
    }

    /// <summary>
    /// An enum used to qualify deferred mesh generation actions.
    /// </summary>
    /// <remarks>
    /// See <see cref="MeshGenerationContext.DeferMeshGeneration"/>.
    /// </remarks>
    enum MeshGenerationCallbackType
    {
        /// <summary>
        /// Qualifies a callback that doesn't perform significant work on the main thread and promptly dispatches jobs.
        /// </summary>
        Fork,
        /// <summary>
        /// Qualifies a callback that performs significant work on the main thread, but eventually dispatches jobs.
        /// </summary>
        WorkThenFork,
        /// <summary>
        /// Qualifies a callback that performs work on the main thread without dispatching jobs.
        /// </summary>
        Work,
    }

    /// <summary>
    /// Flags describing how a texture must be used in the context of a draw command.
    /// </summary>
    /// <seealso cref="MeshGenerationContext.DrawMesh"/>
    /// <seealso cref="MeshGenerationNode.DrawMesh"/>
    [Flags]
    public enum TextureOptions
    {
        /// <summary>
        /// The texture has no special properties.
        /// </summary>
        None = 0,

        /// <summary>
        /// The texture must not be included in the dynamic atlas.
        /// </summary>
        SkipDynamicAtlas = 1 << 0,

        /// <summary>
        /// The texture content is alpha-premultiplied.
        /// </summary>
        /// <remarks>
        /// In premultiplied alpha, the RGB channels have been multiplied by the alpha channel. A typical source is a
        /// RenderTexture that has had transparent geometry rendered to it using standard alpha blending
        /// (e.g. Blend SrcAlpha OneMinusSrcAlpha), which stores premultiplied results. Some operations may require
        /// converting to straight alpha by dividing RGB by alpha.
        /// </remarks>
        PremultipliedAlpha = 1 << 1,
    }

    /// <summary>
    /// Optional per-vertex channels that a UI Toolkit panel can opt into for use by custom shaders.
    /// </summary>
    /// <remarks>
    /// Each flag's value equals <c>1 &lt;&lt; (int)VertexAttribute.X</c> for the matching semantic.
    /// </remarks>
    [Flags]
    public enum ExtraVertexChannels
    {
        /// <summary>No optional channels.</summary>
        None      = 0,
        /// <summary>TEXCOORD1 (float4).</summary>
        TexCoord1 = 1 << 5,
        /// <summary>TEXCOORD2 (float4).</summary>
        TexCoord2 = 1 << 6,
        /// <summary>TEXCOORD3 (float4).</summary>
        TexCoord3 = 1 << 7,
        /// <summary>NORMAL (float3 in the public API; padded to float4 on the GPU).</summary>
        Normal    = 1 << 1,
        /// <summary>TANGENT (float4, .w = handedness).</summary>
        Tangent   = 1 << 2
    }

    /// <summary>
    /// Classifies the visual phase a draw belongs to in element rendering.
    /// </summary>
    /// <remarks>
    /// Mirrors CSS's per-element paint phases (Background → Border → Content) with Mask as the stencil bracket.
    /// Lets mesh modifiers filter by intent — for example, skip <see cref="Mask"/> draws so an effect
    /// doesn't distort stencil geometry.
    /// </remarks>
    public enum DrawPhase : byte
    {
        /// <summary>Element's background fill (color or background image).</summary>
        Background,
        /// <summary>Element's border strokes.</summary>
        Border,
        /// <summary>Element's content — text, image, custom geometry from <c>generateVisualContent</c>, or <see cref="Painter2D"/> output.</summary>
        Content,
        /// <summary>Geometry that defines a stencil-clipping mask region for this element.</summary>
        Mask
    }

    /// <summary>
    /// Bundle of vertex, index, and optional extras slices passed to
    /// <see cref="MeshGenerationContext.DrawMesh(ref UIMesh, Texture)"/>.
    /// </summary>
    /// <remarks>
    /// <para><see cref="vertices"/> and <see cref="indices"/> are required. The optional extras
    /// (<see cref="uv1"/>, <see cref="uv2"/>, <see cref="uv3"/>, <see cref="normal"/>, <see cref="tangent"/>)
    /// are channels the panel must have opted into via <see cref="PanelSettings.extraVertexChannels"/> or
    /// <see cref="IPanel.extraVertexChannels"/>. Providing a slice for a channel the panel did not enable
    /// logs an error and drops that slice.</para>
    /// <para>Each non-empty extras slice must have <c>Length == vertices.Length</c>; mismatches are logged
    /// and the entire draw is dropped.</para>
    /// <para>Channels enabled on the panel but left empty in the draw are zero-filled.</para>
    /// <para>Slices passed to <see cref="MeshGenerationContext.DrawMesh(ref UIMesh, Texture)"/> are read at
    /// flush time, not at the call site. Slices returned by
    /// <see cref="MeshGenerationContext.AllocateTempMesh(ExtraVertexChannels, int, int, out UIMesh)"/> are
    /// guaranteed valid through the flush; user-supplied slices must remain valid until the same flush point.</para>
    /// </remarks>
    public struct UIMesh
    {
        /// <summary>Vertex positions, colors, and UVs.</summary>
        public NativeSlice<Vertex> vertices
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set;
        }
        /// <summary>Triangle list indices.</summary>
        public NativeSlice<ushort> indices
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set;
        }

        /// <summary>UV1 coordinates (TEXCOORD1 in the shader).</summary>
        public NativeSlice<Vector4> uv1
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set;
        }
        /// <summary>UV2 coordinates (TEXCOORD2 in the shader).</summary>
        public NativeSlice<Vector4> uv2
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set;
        }
        /// <summary>UV3 coordinates (TEXCOORD3 in the shader).</summary>
        public NativeSlice<Vector4> uv3
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set;
        }
        /// <summary>Vertex normals. Padded to <c>Vector4</c> on the GPU (<c>.w</c> zero-filled).</summary>
        public NativeSlice<Vector3> normal
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set;
        }
        /// <summary>Vertex tangents (<c>.w</c> = handedness).</summary>
        public NativeSlice<Vector4> tangent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set;
        }
    }

    /// <summary>
    /// Represents the vertex and index data allocated for drawing the content of a <see cref="VisualElement"/>.
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

        internal int currentIndex;
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal int currentVertex;
    }

    internal struct ColorId
    {
        public bool isValid;
        public ushort id;

        public static ColorId Init(RenderTreeManager renderTreeManager, BMPAlloc alloc)
        {
            bool isValid = alloc.IsValid();
            return new ColorId() {
                isValid = isValid,
                id = isValid ? ShaderInfoAllocator.BMPAllocToId(alloc) : (ushort)0
            };
        }

        public MeshBuilderNative.NativeColorId ToNativeColorId()
        {
            return new MeshBuilderNative.NativeColorId() {
                isValid = isValid ? 1 : 0,
                id = id
            };
        }
    }

    /// <summary>
    /// Provides methods for generating a visual element's visual content during the [[VisualElement.generateVisualContent]] callback.
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
    /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/TexturedElement.cs"/>
    /// </example>
    public class MeshGenerationContext
    {
        [Flags]
        internal enum MeshFlags
        {
            None = 0,
            SkipDynamicAtlas = 1 << 1,
            IsUsingVectorImageGradients = 1 << 2,
            SliceTiled = 1 << 3,
        }

        /// <summary>
        /// The element for which <see cref="VisualElement.generateVisualContent"/> was invoked.
        /// </summary>
        public VisualElement visualElement { get; private set; }
        internal RenderData renderData { get; private set; }

        /// <summary>
        /// The painter object used to issue 2D drawing commands. Use this object to draw vector shapes such as lines, arcs and Bezier curves.
        /// </summary>
        /// <example>
        /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/Painter2DDrawing.cs"/>
        /// </example>
        public Painter2D painter2D
        {
            get
            {
                if (disposed)
                {
                    Debug.LogError("Accessing painter2D on disposed MeshGenerationContext");
                    return null;
                }
                if (m_Painter2D == null)
                    m_Painter2D = new Painter2D(this);
                return m_Painter2D;
            }
        }
        internal bool hasPainter2D => (m_Painter2D != null);
        Painter2D m_Painter2D;

        MeshWriteDataPool m_MeshWriteDataPool;
        TempMeshAllocatorImpl m_Allocator;
        MeshGenerationDeferrer m_MeshGenerationDeferrer;
        MeshGenerationNodeManager m_MeshGenerationNodeManager;

        internal IMeshGenerator meshGenerator { get; set; }

        internal EntryRecorder entryRecorder { get; private set; }
        internal Entry parentEntry { get; private set; }

        internal MeshGenerationContext(MeshWriteDataPool meshWriteDataPool, EntryRecorder entryRecorder, TempMeshAllocatorImpl allocator, MeshGenerationDeferrer meshGenerationDeferrer, MeshGenerationNodeManager meshGenerationNodeManager)
        {
            m_MeshWriteDataPool = meshWriteDataPool;
            m_Allocator = allocator;
            m_MeshGenerationDeferrer = meshGenerationDeferrer;
            m_MeshGenerationNodeManager = meshGenerationNodeManager;
            this.entryRecorder = entryRecorder;
            meshGenerator = new MeshGenerator(this);
        }

        static readonly ProfilerMarker k_AllocateMarker = new ProfilerMarker(ProfilerCategory.UIToolkit, "UIR.MeshGenerationContext.Allocate");
        static readonly ProfilerMarker k_DrawVectorImageMarker = new ProfilerMarker(ProfilerCategory.UIToolkit, "UIR.MeshGenerationContext.DrawVectorImage");

        /// <summary>
        /// Allocates the specified number of vertices and indices from a temporary allocator.
        /// </summary>
        /// <remarks>
        /// You can only call this method during the mesh generation phase of the panel and shouldn't use it beyond.
        /// </remarks>
        /// <param name="vertexCount">The number of vertices to allocate. The maximum is 65535 (or UInt16.MaxValue).</param>
        /// <param name="indexCount">The number of triangle list indices to allocate. Each 3 indices represent one triangle, so this value should be multiples of 3.</param>
        /// <param name="vertices">The returned vertices.</param>
        /// <param name="indices">The returned indices.</param>
        public void AllocateTempMesh(int vertexCount, int indexCount, out NativeSlice<Vertex> vertices, out NativeSlice<ushort> indices)
        {
            m_Allocator.AllocateTempMesh(vertexCount, indexCount, out vertices, out indices);
        }

        /// <summary>
        /// Allocates a temporary <see cref="UIMesh"/> with mandatory vertex+index storage and one extras slice per
        /// channel set in <paramref name="extraChannels"/>.
        /// </summary>
        /// <remarks>
        /// <para>Pass <paramref name="vertexCount"/> = 0 to skip the vertex (and extras) allocation, or
        /// <paramref name="indexCount"/> = 0 to skip the index allocation, so callers can mix-and-match
        /// (e.g. allocate indices once and reuse them across draws by stitching them into multiple
        /// <see cref="UIMesh"/> values).</para>
        /// <para>Channels not requested are returned as default (empty) slices; the renderer treats those as
        /// "channel not provided" and zero-fills them at draw time. Use only during the mesh generation phase.</para>
        /// </remarks>
        public void AllocateTempMesh(ExtraVertexChannels extraChannels, int vertexCount, int indexCount, out UIMesh mesh)
        {
            m_Allocator.AllocateTempMesh(extraChannels, vertexCount, indexCount, out mesh);
        }

        /// <summary>
        /// Allocates and draws the specified number of vertices and indices required to express geometry for drawing the content of a <see cref="VisualElement"/>.
        /// </summary>
        /// <param name="vertexCount">The number of vertices to allocate. The maximum is 65535 (or UInt16.MaxValue).</param>
        /// <param name="indexCount">The number of triangle list indices to allocate. Each 3 indices represent one triangle, so this value should be multiples of 3.</param>
        /// <param name="texture">An optional texture to be applied on the triangles allocated. Pass null to rely on vertex colors only.</param>
        /// <returns>An object that gives access to the newely allocated data. If the returned vertex count is 0, the allocation failed (the system ran out of memory).</returns>
        /// <remarks>
        /// See <see cref="Vertex.position"/> for details on geometry generation conventions. When the vertices are indexed, the triangles described must follow clock-wise winding order given that Y+ goes down.
        /// </remarks>
        /// <remarks>
        /// If a valid texture was passed, then the <see cref="Vertex.uv"/> values should be used to map the texture to the geometry.
        /// </remarks>
        /// <remarks>
        /// You can call `MeshGenerationContext.Allocate()` multiple times for the same element or context.
        /// To optimize performance, minimize the number of calls whenever possible.
        /// </remarks>
        /// <remarks>
        /// SA: [[MeshWriteData]]
        /// </remarks>
        /// <example>
        /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/TexturedElement.cs"/>
        /// </example>
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

                m_Allocator.AllocateTempMesh(vertexCount, indexCount, out NativeSlice<Vertex> vertices, out NativeSlice<ushort> indices);

                Debug.Assert(vertices.Length == vertexCount);
                Debug.Assert(indices.Length == indexCount);

                mwd.Reset(vertices, indices);

                entryRecorder.DrawMesh(parentEntry, mwd.m_Vertices, mwd.m_Indices, texture, TextureOptions.None);

                return mwd;
            }
        }

        /// <summary>
        /// Records a draw command with the provided triangle-list indexed mesh.
        /// </summary>
        /// <remarks>
        /// You can generate the mesh content later because the renderer doesn't immediately process the mesh. The mesh
        /// content must be fully generated before you return from GenerateVisualContent, unless you call <see cref="AddMeshGenerationJob"/>.
        ///
        /// The renderer will process the mesh when the following conditions are met:
        /// - GenerateVisualContent has been called on all dirty VisualElements
        /// - All registered generation dependencies have completed
        /// - All deferred generation callbacks have been issued
        /// </remarks>
        /// <param name="vertices">The vertices to be drawn. All referenced vertices must be initialized.</param>
        /// <param name="indices">The triangle list indices. Must be a multiple of 3. All indices must be initialized.</param>
        /// <param name="texture">An optional texture to be applied on the triangles. Pass null to rely on vertex colors only.</param>
        public void DrawMesh(NativeSlice<Vertex> vertices, NativeSlice<ushort> indices, Texture texture = null)
        {
            DrawMesh(vertices, indices, texture, TextureOptions.None);
        }

        /// <summary>
        /// Records a draw command with the provided triangle-list indexed mesh.
        /// </summary>
        /// <remarks>
        /// You can generate the mesh content later because the renderer doesn't immediately process the mesh. The mesh
        /// content must be fully generated before you return from GenerateVisualContent, unless you call <see cref="AddMeshGenerationJob"/>.
        ///
        /// The renderer will process the mesh when the following conditions are met:
        /// - GenerateVisualContent has been called on all dirty VisualElements
        /// - All registered generation dependencies have completed
        /// - All deferred generation callbacks have been issued
        /// </remarks>
        /// <param name="vertices">The vertices to be drawn. All referenced vertices must be initialized.</param>
        /// <param name="indices">The triangle list indices. Must be a multiple of 3. All indices must be initialized.</param>
        /// <param name="texture">An optional texture to be applied on the triangles. Pass null to rely on vertex colors only.</param>
        /// <param name="textureOptions">Flags that apply to the provided texture for this draw call.</param>
        public void DrawMesh(NativeSlice<Vertex> vertices, NativeSlice<ushort> indices, Texture texture, TextureOptions textureOptions)
        {
            // Sugar over DrawMesh(ref UIMesh, ...). Single producer path; extras stay empty.
            var mesh = new UIMesh { vertices = vertices, indices = indices };
            DrawMesh(ref mesh, texture, textureOptions);
        }

        /// <summary>
        /// Records a draw command with a <see cref="UIMesh"/> bundle.
        /// </summary>
        /// <remarks>
        /// <para>The slices inside <paramref name="mesh"/> are read at flush time (end of the current repaint
        /// pass), not at this call. See <see cref="UIMesh"/> for the lifetime contract.</para>
        /// <para><see cref="UIMesh.vertices"/> and <see cref="UIMesh.indices"/> are mandatory. Each non-empty
        /// extras slice must have <c>Length == mesh.vertices.Length</c>; mismatches are logged and the entire
        /// draw is dropped.</para>
        /// </remarks>
        public void DrawMesh(ref UIMesh mesh, Texture texture = null)
        {
            DrawMesh(ref mesh, texture, TextureOptions.None);
        }

        /// <summary>
        /// Records a draw command with a <see cref="UIMesh"/> bundle.
        /// </summary>
        /// <remarks>See <see cref="DrawMesh(ref UIMesh, Texture)"/> for the lifetime contract and validation rules.</remarks>
        public void DrawMesh(ref UIMesh mesh, Texture texture, TextureOptions textureOptions)
        {
            if (mesh.vertices.Length == 0 || mesh.indices.Length == 0)
                return;

            entryRecorder.DrawMesh(parentEntry, ref mesh, texture, textureOptions);
        }

        /// <summary>
        /// Records a draw command and tags it with a user-defined identifier surfaced on
        /// <c>DrawData.userData</c> in mesh modifiers.
        /// </summary>
        /// <param name="vertices">The vertices to be drawn. All referenced vertices must be initialized.</param>
        /// <param name="indices">The triangle list indices. Must be a multiple of 3.</param>
        /// <param name="texture">An optional texture to be applied. Pass null to rely on vertex colors only.</param>
        /// <param name="userData">User-defined identifier surfaced on <c>DrawData.userData</c>.</param>
        public void DrawMesh(NativeSlice<Vertex> vertices, NativeSlice<ushort> indices, Texture texture, int userData)
        {
            DrawMesh(vertices, indices, texture, TextureOptions.None, userData);
        }

        /// <summary>
        /// Records a draw command and tags it with a user-defined identifier surfaced on
        /// <c>DrawData.userData</c> in mesh modifiers.
        /// </summary>
        /// <param name="vertices">The vertices to be drawn. All referenced vertices must be initialized.</param>
        /// <param name="indices">The triangle list indices. Must be a multiple of 3.</param>
        /// <param name="texture">An optional texture to be applied. Pass null to rely on vertex colors only.</param>
        /// <param name="textureOptions">Flags that apply to the provided texture for this draw call.</param>
        /// <param name="userData">User-defined identifier surfaced on <c>DrawData.userData</c>.</param>
        public void DrawMesh(NativeSlice<Vertex> vertices, NativeSlice<ushort> indices, Texture texture, TextureOptions textureOptions, int userData)
        {
            var mesh = new UIMesh { vertices = vertices, indices = indices };
            DrawMesh(ref mesh, texture, textureOptions, userData);
        }

        /// <summary>
        /// Records a draw command with a <see cref="UIMesh"/> bundle and tags it with a user-defined identifier
        /// surfaced on <c>DrawData.userData</c> in mesh modifiers.
        /// </summary>
        /// <remarks>See <see cref="DrawMesh(ref UIMesh, Texture)"/> for the lifetime contract and validation rules.</remarks>
        public void DrawMesh(ref UIMesh mesh, Texture texture, int userData)
        {
            DrawMesh(ref mesh, texture, TextureOptions.None, userData);
        }

        /// <summary>
        /// Records a draw command with a <see cref="UIMesh"/> bundle and tags it with a user-defined identifier
        /// surfaced on <c>DrawData.userData</c> in mesh modifiers.
        /// </summary>
        /// <remarks>See <see cref="DrawMesh(ref UIMesh, Texture)"/> for the lifetime contract and validation rules.</remarks>
        public void DrawMesh(ref UIMesh mesh, Texture texture, TextureOptions textureOptions, int userData)
        {
            if (mesh.vertices.Length == 0 || mesh.indices.Length == 0)
                return;

            entryRecorder.DrawMesh(parentEntry, ref mesh, texture, textureOptions, false, DrawPhase.Content, userData);
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
        /// Draws a <see cref="VectorImage" /> asset and tags the resulting draw with a user-defined identifier
        /// surfaced on <c>DrawData.userData</c> in mesh modifiers.
        /// </summary>
        /// <param name="vectorImage">The vector image to draw.</param>
        /// <param name="offset">The position offset where to draw the vector image.</param>
        /// <param name="rotationAngle">The rotation of the vector image.</param>
        /// <param name="scale">The scale of the vector image</param>
        /// <param name="userData">User-defined identifier surfaced on <c>DrawData.userData</c>.</param>
        public void DrawVectorImage(VectorImage vectorImage, Vector2 offset, Angle rotationAngle, Vector2 scale, int userData)
        {
            using (k_DrawVectorImageMarker.Auto())
                meshGenerator.DrawVectorImage(vectorImage, offset, rotationAngle, scale, userData);
        }

        /// <summary>
        /// Draw a string of text.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="pos">The start position where the text will be displayed.</param>
        /// <param name="fontSize">The font size to use.</param>
        /// <param name="color">The text color.</param>
        /// <param name="font">The font asset to use. If the value is null, the font asset of the VisualElement style is used instead. For more information, refer to <see cref="IStyle.unityFontDefinition"/>.</param>
        public void DrawText(string text, Vector2 pos, float fontSize, Color color, FontAsset font = null)
        {
            if (font == null)
                font = TextUtilities.GetFontAsset(visualElement);
            meshGenerator.DrawText(text, pos, fontSize, color, font);
        }

        /// <summary>
        /// Draw a string of text using the Standard Text Generator.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="pos">The start position where the text will be displayed.</param>
        /// <param name="fontSize">The font size to use.</param>
        /// <param name="color">The text color.</param>
        /// <param name="font">The font asset to use. If the value is null, the font asset of the VisualElement style is used instead. For more information, refer to <see cref="IStyle.unityFontDefinition"/>.</param>
        [Obsolete("DrawTextStandard is deprecated and will be removed in a future release. Use DrawText instead, which uses the Advanced Text Generator (ATG).", false)]
        public void DrawTextStandard(string text, Vector2 pos, float fontSize, Color color, FontAsset font = null)
        {
            if (font == null)
                font = TextUtilities.GetFontAsset(visualElement);
            meshGenerator.DrawText(text, pos, fontSize, color, font, false);
        }

        /// <summary>
        /// Returns an allocator that can be used to safely allocate temporary meshes from the job system. The meshes
        /// have the same scope as those allocated by <see cref="AllocateTempMesh"/>.
        /// </summary>
        /// <param name="allocator">The allocator.</param>
        public void GetTempMeshAllocator(out TempMeshAllocator allocator)
        {
            m_Allocator.CreateNativeHandle(out allocator);
        }

        /// <summary>
        /// Inserts a node into the rendering tree that can be populated from the job system.
        /// </summary>
        /// <param name="node">The inserted mesh generation node.</param>
        public void InsertMeshGenerationNode(out MeshGenerationNode node)
        {
            var entry = entryRecorder.InsertPlaceholder(parentEntry);
            m_MeshGenerationNodeManager.CreateNode(entry, out node);
        }

        internal void InsertUnsafeMeshGenerationNode(out UnsafeMeshGenerationNode node)
        {
            var entry = entryRecorder.InsertPlaceholder(parentEntry);
            m_MeshGenerationNodeManager.CreateUnsafeNode(entry, out node);
        }

        /// <summary>
        /// Instructs the renderer to wait for the completion of the provided JobHandle before beginning processing the meshes.
        /// </summary>
        /// <param name="jobHandle">JobHandle to wait for.</param>
        /// <example>
        /// The following code example shows how to use a job to generate a mesh:
        /// <code source="../../../../Modules/UIElements/Tests/UIElementsExamples/Assets/Examples/MeshGenerationContext_AddMeshGenerationJob.cs"/>
        /// </example>
        public void AddMeshGenerationJob(JobHandle jobHandle)
        {
            m_MeshGenerationDeferrer.AddMeshGenerationJob(jobHandle);
        }

        /// <summary>
        /// Instructs the renderer to execute the provided callback before the end of the current mesh generation iteration.
        /// </summary>
        /// <remarks>
        ///To keep the CPU as busy as possible, the renderer prioritizes callbacks based on the following order:
        /// 1. <see cref="MeshGenerationCallbackType.Fork"/>
        /// 2. <see cref="MeshGenerationCallbackType.WorkThenFork"/>
        /// 3. <see cref="MeshGenerationCallbackType.Work"/>
        /// </remarks>
        /// <param name="callback">The callback.</param>
        /// <param name="userData">The data provided to the callback.</param>
        /// <param name="callbackType">The type of callback.</param>
        /// <param name="isJobDependent">Indicates if the callback requires the execution of a job to be completed.</param>
        internal void AddMeshGenerationCallback(UIR.MeshGenerationCallback callback, object userData, MeshGenerationCallbackType callbackType, bool isJobDependent)
        {
            m_MeshGenerationDeferrer.AddMeshGenerationCallback(callback, userData, callbackType, isJobDependent);
        }

        internal void Begin(Entry parentEntry, VisualElement ve, RenderData renderData)
        {
            if (visualElement != null)
                throw new InvalidOperationException($"{nameof(Begin)} can only be called when there is no target set. Did you forget to call {nameof(End)}?");
            if (parentEntry == null)
                throw new ArgumentException($"The state of the provided {nameof(MeshGenerationNode)} is invalid (entry is null).");
            if (ve == null)
                throw new ArgumentException(nameof(ve));

            this.parentEntry = parentEntry;
            this.visualElement = ve;
            this.renderData = renderData;
            meshGenerator.currentElement = ve;

        }

        internal void End()
        {
            if (visualElement == null)
                throw new InvalidOperationException($"{nameof(End)} can only be called after a successful call to {nameof(Begin)}.");

            meshGenerator.currentElement = null;
            visualElement = null;
            parentEntry = null;
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
                entryRecorder = null;
                (meshGenerator as MeshGenerator)?.Dispose();
                meshGenerator = null;
                m_Allocator = null;
                m_MeshGenerationDeferrer = null;
                m_MeshGenerationNodeManager = null;
            }

            disposed = true;
        }

        #endregion // Dispose Pattern
    }
}
