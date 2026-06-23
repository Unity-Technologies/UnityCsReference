// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.UIElements.UIR;

namespace UnityEngine.UIElements
{
    /// <summary>
    /// What kind of geometry a <see cref="DrawData"/> represents.
    /// </summary>
    /// <remarks>
    /// Corresponds to the type returned by the Render Type node in Shader Graph.
    /// </remarks>
    public enum RenderType : byte
    {
        /// <summary>Untextured geometry (solid color)</summary>
        Solid,
        /// <summary>Textured geometry</summary>
        Texture,
        /// <summary>Signed-distance-field text</summary>
        SdfText,
        /// <summary>Bitmap (raster) text</summary>
        BitmapText,
        /// <summary>Vector-image gradient fills</summary>
        Gradient
    }

    /// <summary>
    /// Callback signature for modifying generated meshes on a <see cref="VisualElement"/>.
    /// </summary>
    /// <param name="ctx">Per-invocation context.</param>
    public delegate void MeshModificationCallback(MeshModificationContext ctx);

    /// <summary>
    /// Per-invocation context passed to a <see cref="MeshModificationCallback"/>.
    /// </summary>
    public ref struct MeshModificationContext
    {
        internal List<Entry> drawsBuffer;
        internal TempMeshAllocatorImpl allocator;
        internal ExtraVertexChannels panelExtras;
        internal JobMerger pendingHandles;

        /// <summary>The element whose draws this invocation is processing.</summary>
        public VisualElement element { get; internal set; }

        /// <summary>
        /// The generated draws for <see cref="element"/>, in render order.
        /// </summary>
        public DrawDataEnumerable draws => new(drawsBuffer, allocator, panelExtras);

        /// <summary>
        /// Allocates a fresh temporary <see cref="UIMesh"/> sized for <paramref name="vertexCount"/> vertices and
        /// <paramref name="indexCount"/> indices, with slices for the requested extras <paramref name="channels"/>.
        /// </summary>
        /// <remarks>
        /// <para>Channels not requested return as empty slices on the resulting <see cref="UIMesh"/>. Use the
        /// result as input to <see cref="DrawData.SetMesh"/> for tessellation refinement or wholesale geometry
        /// replacement. Memory is valid for the duration of this callback and any jobs added via
        /// <see cref="AddMeshModificationJob"/>.</para>
        /// <para>Requested channels that are NOT enabled on the panel's <see cref="ExtraVertexChannels"/> mask
        /// are dropped and the resulting <c>UIMesh</c> returns empty slices for those channels. Check
        /// slice lengths before filling.</para>
        /// </remarks>
        public void AllocateUIMesh(ExtraVertexChannels channels, int vertexCount, int indexCount, out UIMesh mesh)
        {
            var dropped = channels & ~panelExtras;
            if (dropped != ExtraVertexChannels.None)
                Debug.LogError($"AllocateUIMesh: channel(s) {dropped} are not enabled on the panel's ExtraVertexChannels mask and were dropped; their slices on the returned UIMesh will be empty.");
            allocator.AllocateTempMesh(channels & panelExtras, vertexCount, indexCount, out mesh);
        }

        /// <summary>
        /// Add a user-scheduled job to this element's post-processing dependency chain.
        /// </summary>
        /// <remarks>
        /// <para>Subsequent callbacks on this element are deferred until all handles added by prior
        /// callbacks complete. The main thread does not block while waiting — other dirty elements continue
        /// to advance and this element resumes once its pending jobs complete.</para>
        /// <para>All added handles are awaited before the GPU upload, so jobs are free to write into the
        /// slices exposed by <see cref="DrawData"/>.</para>
        /// </remarks>
        public void AddMeshModificationJob(JobHandle dependency)
        {
            pendingHandles?.Add(dependency);
        }
    }

    /// <summary>
    /// Mutable view over one generated mesh belonging to the current element.
    /// </summary>
    /// <remarks>
    /// Slices alias the underlying CPU storage — writes propagate to the GPU at the next upload. Conceptually
    /// a mesh, not a draw call: the renderer is free to batch multiple meshes (even from different elements)
    /// into a single GPU draw later in the pipeline.
    /// </remarks>
    public readonly ref struct DrawData
    {
        readonly Entry m_Entry;
        readonly TempMeshAllocatorImpl m_Allocator;
        readonly ExtraVertexChannels m_PanelExtras;

        internal DrawData(Entry entry, TempMeshAllocatorImpl allocator, ExtraVertexChannels panelExtras)
        {
            m_Entry = entry;
            m_Allocator = allocator;
            m_PanelExtras = panelExtras;
        }

        /// <summary>What kind of geometry this draw holds.</summary>
        public RenderType renderType => ResolveRenderType(m_Entry);

        /// <summary>The visual phase this draw belongs to.</summary>
        public DrawPhase phase => m_Entry.phase;

        /// <summary>
        /// The texture sampled by this draw, or <c>null</c> for <see cref="RenderType.Solid"/>.
        /// </summary>
        public Texture texture => m_Entry.texture;

        /// <summary>
        /// User-provided identifier set at draw time via the <c>userData</c> parameter of APIs such as <c>DrawMesh</c>.
        /// </summary>
        /// <remarks>
        /// Defaults to <c>0</c> for draws made without an explicit userData.
        /// </remarks>
        public int userData => m_Entry.userData;

        /// <summary>The mesh's vertices.</summary>
        public NativeSlice<Vertex> vertices => m_Entry.vertices;

        /// <summary>The mesh's indices (triangles, 3 per tri).</summary>
        public NativeSlice<ushort> indices => m_Entry.indices;

        /// <summary>
        /// Returns the UV1 (TEXCOORD1) extras slice.
        /// </summary>
        /// <param name="allocate">When true, attaches a fresh slice sized to <c>vertices.Length</c> if the
        /// channel isn't already present. Returns empty if the channel is disabled on the panel's
        /// <see cref="ExtraVertexChannels"/> mask. Idempotent on re-call.</param>
        public NativeSlice<Vector4> GetUv1(bool allocate = false)
        {
            if (m_Entry.texCoord1.Length > 0 || !allocate)
                return m_Entry.texCoord1;
            if ((m_PanelExtras & ExtraVertexChannels.TexCoord1) == 0)
            {
                Debug.LogError("GetUv1(allocate: true) returned empty: channel TexCoord1 is not enabled on the panel's ExtraVertexChannels mask.");
                return m_Entry.texCoord1;
            }
            m_Entry.texCoord1 = m_Allocator.AllocChannel<Vector4>(m_Entry.vertices.Length);
            m_Entry.flags |= EntryFlags.HasExtras;
            return m_Entry.texCoord1;
        }

        /// <summary>
        /// Returns the UV2 (TEXCOORD2) extras slice.
        /// </summary>
        /// <param name="allocate">When true, attaches a fresh slice sized to <c>vertices.Length</c> if the
        /// channel isn't already present. Returns empty if the channel is disabled on the panel's
        /// <see cref="ExtraVertexChannels"/> mask. Idempotent on re-call.</param>
        public NativeSlice<Vector4> GetUv2(bool allocate = false)
        {
            if (m_Entry.texCoord2.Length > 0 || !allocate)
                return m_Entry.texCoord2;
            if ((m_PanelExtras & ExtraVertexChannels.TexCoord2) == 0)
            {
                Debug.LogError("GetUv2(allocate: true) returned empty: channel TexCoord2 is not enabled on the panel's ExtraVertexChannels mask.");
                return m_Entry.texCoord2;
            }
            m_Entry.texCoord2 = m_Allocator.AllocChannel<Vector4>(m_Entry.vertices.Length);
            m_Entry.flags |= EntryFlags.HasExtras;
            return m_Entry.texCoord2;
        }

        /// <summary>
        /// Returns the UV3 (TEXCOORD3) extras slice.
        /// </summary>
        /// <param name="allocate">When true, attaches a fresh slice sized to <c>vertices.Length</c> if the
        /// channel isn't already present. Returns empty if the channel is disabled on the panel's
        /// <see cref="ExtraVertexChannels"/> mask. Idempotent on re-call.</param>
        public NativeSlice<Vector4> GetUv3(bool allocate = false)
        {
            if (m_Entry.texCoord3.Length > 0 || !allocate)
                return m_Entry.texCoord3;
            if ((m_PanelExtras & ExtraVertexChannels.TexCoord3) == 0)
            {
                Debug.LogError("GetUv3(allocate: true) returned empty: channel TexCoord3 is not enabled on the panel's ExtraVertexChannels mask.");
                return m_Entry.texCoord3;
            }
            m_Entry.texCoord3 = m_Allocator.AllocChannel<Vector4>(m_Entry.vertices.Length);
            m_Entry.flags |= EntryFlags.HasExtras;
            return m_Entry.texCoord3;
        }

        /// <summary>
        /// Returns the Normal extras slice. <c>Vector3</c> per vertex.
        /// </summary>
        /// <param name="allocate">When true, attaches a fresh slice sized to <c>vertices.Length</c> if the
        /// channel isn't already present. Returns empty if the channel is disabled on the panel's
        /// <see cref="ExtraVertexChannels"/> mask. Idempotent on re-call.</param>
        public NativeSlice<Vector3> GetNormal(bool allocate = false)
        {
            if (m_Entry.normal.Length > 0 || !allocate)
                return m_Entry.normal;
            if ((m_PanelExtras & ExtraVertexChannels.Normal) == 0)
            {
                Debug.LogError("GetNormal(allocate: true) returned empty: channel Normal is not enabled on the panel's ExtraVertexChannels mask.");
                return m_Entry.normal;
            }
            m_Entry.normal = m_Allocator.AllocChannel<Vector3>(m_Entry.vertices.Length);
            m_Entry.flags |= EntryFlags.HasExtras;
            return m_Entry.normal;
        }

        /// <summary>
        /// Returns the Tangent extras slice.
        /// </summary>
        /// <param name="allocate">When true, attaches a fresh slice sized to <c>vertices.Length</c> if the
        /// channel isn't already present. Returns empty if the channel is disabled on the panel's
        /// <see cref="ExtraVertexChannels"/> mask. Idempotent on re-call.</param>
        public NativeSlice<Vector4> GetTangent(bool allocate = false)
        {
            if (m_Entry.tangent.Length > 0 || !allocate)
                return m_Entry.tangent;
            if ((m_PanelExtras & ExtraVertexChannels.Tangent) == 0)
            {
                Debug.LogError("GetTangent(allocate: true) returned empty: channel Tangent is not enabled on the panel's ExtraVertexChannels mask.");
                return m_Entry.tangent;
            }
            m_Entry.tangent = m_Allocator.AllocChannel<Vector4>(m_Entry.vertices.Length);
            m_Entry.flags |= EntryFlags.HasExtras;
            return m_Entry.tangent;
        }

        /// <summary>Replace this draw's vertices.</summary>
        /// <remarks>
        /// The new slice can be any length greater than 0, but all currently-attached non-empty extras must already match the new
        /// vertex count, or this call is logged and rejected. To resize vertices and extras together, use
        /// <see cref="SetMesh"/>.
        /// </remarks>
        public void SetVertices(NativeSlice<Vertex> vertices)
        {
            int newLength = vertices.Length;
            if (newLength == 0)
            {
                Debug.LogError("SetVertices rejected: vertices slice must be non-empty.");
                return;
            }
            if (!CheckExistingExtrasMatch(newLength, "SetVertices"))
                return;
            m_Entry.vertices = vertices;
        }

        /// <summary>Replace this draw's indices.</summary>
        /// <remarks>Length must be a multiple of 3 (triangle list).</remarks>
        public void SetIndices(NativeSlice<ushort> indices)
        {
            if (indices.Length == 0)
            {
                Debug.LogError("SetIndices rejected: indices slice must be non-empty.");
                return;
            }
            if (indices.Length % 3 != 0)
            {
                Debug.LogError($"SetIndices rejected: indices.Length {indices.Length} is not a multiple of 3.");
                return;
            }
            m_Entry.indices = indices;
        }

        /// <summary>Replace the UV1 (TEXCOORD1) extras slice.</summary>
        /// <remarks>
        /// <paramref name="uv1"/> must have length equal to <c>vertices.Length</c> OR be empty. An empty slice
        /// detaches the channel. Non-empty slices require the channel to be enabled on the panel's
        /// <see cref="ExtraVertexChannels"/> mask.
        /// </remarks>
        public void SetUv1(NativeSlice<Vector4> uv1)
        {
            if (!ValidateExtrasSet(uv1.Length, ExtraVertexChannels.TexCoord1, "SetUv1"))
                return;
            m_Entry.texCoord1 = uv1;
            UpdateHasExtrasFlag();
        }

        /// <inheritdoc cref="SetUv1"/>
        public void SetUv2(NativeSlice<Vector4> uv2)
        {
            if (!ValidateExtrasSet(uv2.Length, ExtraVertexChannels.TexCoord2, "SetUv2"))
                return;
            m_Entry.texCoord2 = uv2;
            UpdateHasExtrasFlag();
        }

        /// <inheritdoc cref="SetUv1"/>
        public void SetUv3(NativeSlice<Vector4> uv3)
        {
            if (!ValidateExtrasSet(uv3.Length, ExtraVertexChannels.TexCoord3, "SetUv3"))
                return;
            m_Entry.texCoord3 = uv3;
            UpdateHasExtrasFlag();
        }

        /// <summary>Replace the Normal extras slice. <c>Vector3</c> per vertex.</summary>
        /// <remarks>See <see cref="SetUv1"/> for validation rules.</remarks>
        public void SetNormal(NativeSlice<Vector3> normal)
        {
            if (!ValidateExtrasSet(normal.Length, ExtraVertexChannels.Normal, "SetNormal"))
                return;
            m_Entry.normal = normal;
            UpdateHasExtrasFlag();
        }

        /// <summary>Replace the Tangent extras slice.</summary>
        /// <remarks>See <see cref="SetUv1"/> for validation rules.</remarks>
        public void SetTangent(NativeSlice<Vector4> tangent)
        {
            if (!ValidateExtrasSet(tangent.Length, ExtraVertexChannels.Tangent, "SetTangent"))
                return;
            m_Entry.tangent = tangent;
            UpdateHasExtrasFlag();
        }

        /// <summary>
        /// Replace this draw's entire geometry in a single call: vertices, indices, and all extras channels.
        /// </summary>
        /// <remarks>
        /// <para>Atomic: all fields update together, so cross-field length invariants are guaranteed. Prefer
        /// this over per-slice setters when resizing the entire mesh.</para>
        /// <para>Validation: every non-empty extras slice in <paramref name="mesh"/> must have length equal to
        /// <c>mesh.vertices.Length</c>, AND its channel must be enabled on the panel's
        /// <see cref="ExtraVertexChannels"/> mask. <c>mesh.indices.Length</c> must be a multiple of 3. If any
        /// check fails the call is rejected (this draw stays unchanged) and an error is logged.</para>
        /// <para>Metadata (<c>renderType</c>, <c>phase</c>, <c>texture</c>, <c>userData</c>) is preserved.</para>
        /// </remarks>
        public void SetMesh(in UIMesh mesh)
        {
            int vc = mesh.vertices.Length;
            if (vc == 0)
            {
                Debug.LogError("SetMesh rejected: mesh.vertices must be non-empty.");
                return;
            }
            if (mesh.indices.Length == 0)
            {
                Debug.LogError("SetMesh rejected: mesh.indices must be non-empty.");
                return;
            }
            if (mesh.indices.Length % 3 != 0)
            {
                Debug.LogError($"SetMesh rejected: mesh.indices.Length {mesh.indices.Length} is not a multiple of 3.");
                return;
            }
            if (!CheckMeshExtras(mesh.uv1.Length,     vc, ExtraVertexChannels.TexCoord1, "uv1")) return;
            if (!CheckMeshExtras(mesh.uv2.Length,     vc, ExtraVertexChannels.TexCoord2, "uv2")) return;
            if (!CheckMeshExtras(mesh.uv3.Length,     vc, ExtraVertexChannels.TexCoord3, "uv3")) return;
            if (!CheckMeshExtras(mesh.normal.Length,  vc, ExtraVertexChannels.Normal,    "normal")) return;
            if (!CheckMeshExtras(mesh.tangent.Length, vc, ExtraVertexChannels.Tangent,   "tangent")) return;

            m_Entry.vertices = mesh.vertices;
            m_Entry.indices = mesh.indices;
            m_Entry.texCoord1 = mesh.uv1;
            m_Entry.texCoord2 = mesh.uv2;
            m_Entry.texCoord3 = mesh.uv3;
            m_Entry.normal = mesh.normal;
            m_Entry.tangent = mesh.tangent;
            UpdateHasExtrasFlag();
        }

        bool ValidateExtrasSet(int sliceLength, ExtraVertexChannels channel, string name)
        {
            if (sliceLength == 0)
                return true;
            if (sliceLength != m_Entry.vertices.Length)
            {
                Debug.LogError($"{name} rejected: slice length {sliceLength} doesn't match vertices.Length {m_Entry.vertices.Length}.");
                return false;
            }
            if ((m_PanelExtras & channel) == 0)
            {
                Debug.LogError($"{name} rejected: channel {channel} is not enabled on the panel's ExtraVertexChannels mask.");
                return false;
            }
            return true;
        }

        bool CheckExistingExtrasMatch(int newLength, string name)
        {
            if (m_Entry.texCoord1.Length > 0 && m_Entry.texCoord1.Length != newLength) { LogExtrasMismatch(name, "uv1",     m_Entry.texCoord1.Length, newLength); return false; }
            if (m_Entry.texCoord2.Length > 0 && m_Entry.texCoord2.Length != newLength) { LogExtrasMismatch(name, "uv2",     m_Entry.texCoord2.Length, newLength); return false; }
            if (m_Entry.texCoord3.Length > 0 && m_Entry.texCoord3.Length != newLength) { LogExtrasMismatch(name, "uv3",     m_Entry.texCoord3.Length, newLength); return false; }
            if (m_Entry.normal.Length    > 0 && m_Entry.normal.Length    != newLength) { LogExtrasMismatch(name, "normal",  m_Entry.normal.Length,    newLength); return false; }
            if (m_Entry.tangent.Length   > 0 && m_Entry.tangent.Length   != newLength) { LogExtrasMismatch(name, "tangent", m_Entry.tangent.Length,   newLength); return false; }
            return true;
        }

        static void LogExtrasMismatch(string op, string channel, int existing, int newLength)
        {
            Debug.LogError($"{op} rejected: existing {channel} slice has length {existing} which doesn't match the new vertices.Length {newLength}. Clear the channel first or use SetMesh for atomic resize.");
        }

        bool CheckMeshExtras(int sliceLength, int vertexCount, ExtraVertexChannels channel, string name)
        {
            if (sliceLength == 0)
                return true;
            if (sliceLength != vertexCount)
            {
                Debug.LogError($"SetMesh rejected: {name} slice length {sliceLength} doesn't match vertices.Length {vertexCount}.");
                return false;
            }
            if ((m_PanelExtras & channel) == 0)
            {
                Debug.LogError($"SetMesh rejected: {name} provided but channel {channel} is not enabled on the panel's ExtraVertexChannels mask.");
                return false;
            }
            return true;
        }

        void UpdateHasExtrasFlag()
        {
            int total = m_Entry.texCoord1.Length | m_Entry.texCoord2.Length | m_Entry.texCoord3.Length | m_Entry.normal.Length | m_Entry.tangent.Length;
            if (total != 0)
                m_Entry.flags |= EntryFlags.HasExtras;
            else
                m_Entry.flags &= ~EntryFlags.HasExtras;
        }

        static RenderType ResolveRenderType(Entry e)
        {
            switch (e.type)
            {
                case EntryType.DrawSolidMesh: return RenderType.Solid;
                case EntryType.DrawTexturedMesh: return RenderType.Texture;
                case EntryType.DrawTextMesh: return e.textScale != 0f ? RenderType.SdfText : RenderType.BitmapText;
                case EntryType.DrawGradients: return RenderType.Gradient;
                default: return RenderType.Solid;
            }
        }
    }

    /// <summary>Iterable view over the draws of a <see cref="MeshModificationContext"/>.</summary>
    public ref struct DrawDataEnumerable
    {
        readonly List<Entry> m_Buffer;
        readonly TempMeshAllocatorImpl m_Allocator;
        readonly ExtraVertexChannels m_PanelExtras;

        internal DrawDataEnumerable(List<Entry> buffer, TempMeshAllocatorImpl allocator, ExtraVertexChannels panelExtras)
        {
            m_Buffer = buffer;
            m_Allocator = allocator;
            m_PanelExtras = panelExtras;
        }

        /// <summary>Returns an enumerator that iterates through the draws.</summary>
        public DrawDataEnumerator GetEnumerator() => new(m_Buffer, m_Allocator, m_PanelExtras);
    }

    /// <summary>Enumerator over the draws of a <see cref="MeshModificationContext"/>.</summary>
    public ref struct DrawDataEnumerator
    {
        readonly List<Entry> m_Buffer;
        readonly TempMeshAllocatorImpl m_Allocator;
        readonly ExtraVertexChannels m_PanelExtras;
        int m_Index;

        internal DrawDataEnumerator(List<Entry> buffer, TempMeshAllocatorImpl allocator, ExtraVertexChannels panelExtras)
        {
            m_Buffer = buffer;
            m_Allocator = allocator;
            m_PanelExtras = panelExtras;
            m_Index = -1;
        }

        /// <summary>The current draw.</summary>
        public DrawData Current => new(m_Buffer[m_Index], m_Allocator, m_PanelExtras);

        /// <summary>Advances to the next draw.</summary>
        public bool MoveNext()
        {
            if (m_Buffer == null)
                return false;
            return ++m_Index < m_Buffer.Count;
        }
    }

    readonly struct MeshModifierRegistration
    {
        public readonly MeshModificationCallback callback;
        public readonly bool recursive;
        public readonly int priority;
        public readonly long id;

        public MeshModifierRegistration(MeshModificationCallback callback, bool recursive, int priority, long id)
        {
            this.callback = callback;
            this.recursive = recursive;
            this.priority = priority;
            this.id = id;
        }

        public static readonly Comparison<MeshModifierRegistration> s_Comparer = (a, b) =>
        {
            if (a.priority != b.priority)
                return a.priority < b.priority ? -1 : 1;
            if (a.recursive != b.recursive)
                return a.recursive ? 1 : -1;
            return a.id.CompareTo(b.id);
        };
    }
}
