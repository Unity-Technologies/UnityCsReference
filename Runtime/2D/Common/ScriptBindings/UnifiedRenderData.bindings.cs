// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

namespace UnityEngine.U2D
{
    // Blittable mirror of the native ScriptingSpriteGeometry. Field order and types must
    // stay in lockstep with Runtime/2D/Common/ScriptBindings/UnifiedRenderData.bindings.h.
    [StructLayout(LayoutKind.Sequential)]
    internal struct SpriteGeometryData
    {
        public IntPtr positions;
        public IntPtr uvs;
        public IntPtr normals;
        public IntPtr tangents;
        public IntPtr colors;
        public IntPtr indices;
        public int vertexCount;
        public int indexCount;
    }

    // One entry's slice of an AddSpriteBatch() call's shared arrays; offsets are batch-local,
    // or absolute under a write session. Reinterpreted as UnifiedRenderDataWriterEntry; layouts must match.
    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules("UnityEngine.SpriteShapeModule", "Unity.2D.SpriteShape.Runtime", "Unity.2D.Graphics.Runtime")]
    internal struct SpriteBatchEntry
    {
        // The sprite this entry's texture/material come from. Never dereferenced by the add
        // itself, so may be EntityId.None for geometry that needs no texture.
        public EntityId spriteId;

        // 0 and 1 both mean a single draw. Use AddSprite instead for an entry needing a
        // transform array.
        public int instances;

        public int vertexOffset;
        public int vertexCount;
        public int indexOffset;
        public int indexCount;

        // Taken verbatim, unlike AddSprite's, which native derives from the transformed
        // positions.
        public Bounds bounds;

        // Per-entry shader state, not vertex data. Leave at Color.white / defaultSpriteProps
        // to let adjacent entries coalesce into one draw range.
        public Vector4 spriteProps;
        public Vector4 spriteColor;
    }

    // Blittable mirror of the native ScriptingSpriteBatch. Field order and types must stay in
    // lockstep with UnifiedRenderData.bindings.h.
    [StructLayout(LayoutKind.Sequential)]
    internal struct SpriteBatchGeometryData
    {
        public IntPtr positions;
        public IntPtr uvs;
        public IntPtr normals;
        public IntPtr tangents;
        public IntPtr colors;
        public IntPtr indices;
        public IntPtr entries;
        public int vertexCount;
        public int indexCount;
        public int entryCount;
    }

    // Where a single entry's geometry originates. Mirrors native SpriteGeometrySource and is
    // serialized, so never renumber.
    [VisibleToOtherModules("UnityEngine.SpriteShapeModule", "Unity.2D.SpriteShape.Runtime", "Unity.2D.Graphics.Runtime")]
    internal enum SpriteGeometrySource
    {
        // The entry owns a copy of its geometry, occupying a contiguous slice of the
        // shared buffers.
        Baked = 0,

        // The entry owns no geometry and draws the geometry of its sprite, occupying no
        // space in the shared buffers.
        Referenced = 1,
    }

    // Describes a slice of native memory that a NativeArray/NativeSlice wraps zero-copy.
    // Blittable mirror of native UnifiedRenderDataBufferInfo; keep field order in lockstep.
    [StructLayout(LayoutKind.Sequential)]
    internal struct UnifiedRenderDataBufferInfo
    {
        public IntPtr buffer;
        public int count;
        public int offset;
        public int stride;
    }

    // A snapshot of one entry's render data; modifying it does not affect the source.
    // Reinterpreted as ScriptingPerEntityRenderData, so the two layouts must match.
    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules("UnityEngine.SpriteShapeModule", "Unity.2D.SpriteShape.Runtime", "Unity.2D.Graphics.Runtime")]
    internal struct PerEntityRenderData
    {
        EntityId m_Sprite;
        int m_Instances;
        int m_TransformCount;
        int m_ColorCount;
        Bounds m_Bounds;
        Vector4 m_SpriteProps;
        Vector4 m_SpriteColor;
        int m_IndexCount;
        int m_VertexCount;
        SpriteGeometrySource m_GeometrySource;

        // Supplies the geometry when geometrySource is Referenced. Safe to read from a job;
        // turning it into a Sprite is not -- see ResolveSprite().
        public EntityId spriteId { get { return m_Sprite; } set { m_Sprite = value; } }

        // Main thread only: Resources.EntityIdToObject is not thread-safe. Returns null both
        // for no sprite and for a destroyed one.
        public Sprite ResolveSprite()
        {
            return Resources.EntityIdIsValid(m_Sprite) ? Resources.EntityIdToObject(m_Sprite) as Sprite : null;
        }

        // 0 and 1 both mean a single draw; only a value greater than 1 is instanced.
        public int instances { get { return m_Instances; } set { m_Instances = value; } }

        // How many transforms this entry carries: exactly max(instances, 1), or 0 when the
        // producer supplied none. Read them with UnifiedRenderData.GetEntityTransforms().
        public int transformCount { get { return m_TransformCount; } set { m_TransformCount = value; } }

        // The same for its per-instance colours, which are independently optional. Read them
        // with UnifiedRenderData.GetEntityColors().
        public int colorCount { get { return m_ColorCount; } set { m_ColorCount = value; } }

        public Bounds bounds { get { return m_Bounds; } set { m_Bounds = value; } }

        // Passed through verbatim from the producer; the shader defines each component.
        public Vector4 spriteProps { get { return m_SpriteProps; } set { m_SpriteProps = value; } }

        // The tint, as a linear RGBA quadruplet.
        public Vector4 spriteColor { get { return m_SpriteColor; } set { m_SpriteColor = value; } }

        // For a Baked entry, the length of its slice of the shared buffers. For a
        // Referenced entry, the count read off the sprite, or 0 if it could not be resolved.
        public int indexCount { get { return m_IndexCount; } set { m_IndexCount = value; } }

        public int vertexCount { get { return m_VertexCount; } set { m_VertexCount = value; } }

        public SpriteGeometrySource geometrySource { get { return m_GeometrySource; } set { m_GeometrySource = value; } }
    }

    // One contiguous slice of shared geometry, paired with the entity whose Sprite supplies
    // its texture. Blittable mirror of native UnifiedRenderDataDrawRange; field order must match.
    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules("UnityEngine.SpriteShapeModule", "Unity.2D.SpriteShape.Runtime", "Unity.2D.Graphics.Runtime")]
    internal struct DrawRange
    {
        int m_FirstIndex;
        int m_IndexCount;
        int m_FirstVertex;
        int m_VertexCount;
        int m_EntityIndex;

        public int firstIndex { get { return m_FirstIndex; } set { m_FirstIndex = value; } }

        public int indexCount { get { return m_IndexCount; } set { m_IndexCount = value; } }

        public int firstVertex { get { return m_FirstVertex; } set { m_FirstVertex = value; } }

        public int vertexCount { get { return m_VertexCount; } set { m_VertexCount = value; } }

        // Texture resolution keys off this entity's sprite; skipped by consumers that need a
        // texture if it has none.
        public int entityIndex { get { return m_EntityIndex; } set { m_EntityIndex = value; } }
    }

    // Blittable mirror of native UnifiedRenderDataWriteHandles: raw pointers into a
    // UnifiedRenderData's own buffers for one write session. Never handed to a caller.
    [StructLayout(LayoutKind.Sequential)]
    [VisibleToOtherModules("UnityEngine.SpriteShapeModule", "Unity.2D.SpriteShape.Runtime", "Unity.2D.Graphics.Runtime")]
    internal struct UnifiedRenderDataWriteHandles
    {
        public IntPtr vertexData;
        public IntPtr indices;
        public IntPtr entries;
        public IntPtr drawRanges;
        public IntPtr counts;

        // Byte offset/stride of each channel in the interleaved vertex block; -1 / 0 means
        // the channel is absent.
        public int positionOffset;
        public int positionStride;
        public int texCoordOffset;
        public int texCoordStride;
        public int normalOffset;
        public int normalStride;
        public int tangentOffset;
        public int tangentStride;
        public int colorOffset;
        public int colorStride;

        public int vertexCapacity;
        public int indexCapacity;
        public int entryCapacity;
        public int drawRangeCapacity;
    }

    // A job's zero-copy write window into a UnifiedRenderData's own memory. Buffers stay pinned
    // between BeginWrite() and EndWrite(); the counts live in engine memory to survive the job copy.
    [VisibleToOtherModules("UnityEngine.SpriteShapeModule", "Unity.2D.SpriteShape.Runtime", "Unity.2D.Graphics.Runtime")]
    internal unsafe struct UnifiedRenderDataWriter
    {
        // The only native container field; other views are minted on access (MakeSlice()/MakeArray()).
        // Four SInt32s matching UnifiedRenderDataWriteCounts; tracking it marks this job as the writer.
        NativeArray<int> m_Counts;

        // The session's pointers, byte offset/stride pairs and capacities, exactly as native
        // handed them over.
        [NativeDisableUnsafePtrRestriction] UnifiedRenderDataWriteHandles m_Handles;
        // The session this writer was opened for, compared against the live counts block
        // to tell an open session from one EndWrite() has already closed.
        int m_SessionId;

        // UnifiedRenderDataWriteCounts' field count, fixed by the native struct.
        const int k_CountFieldCount = 5;

        // Index of UnifiedRenderDataWriteCounts::sessionId within that struct.
        const int k_SessionIdField = 4;

        internal UnifiedRenderDataWriter(UnifiedRenderDataWriteHandles handles)
        {
            m_Handles = handles;
            m_Counts = default;
            m_SessionId = 0;

            if (handles.counts == IntPtr.Zero)
                return;

            // Read through the raw pointer, not m_Counts: the safety handle is stamped
            // by the checked constructor below, after this has run.
            m_SessionId = ((int*)handles.counts)[k_SessionIdField];

            // Allocator.None: the session does not own this memory, so Dispose() on this
            // or on any view minted below is a no-op rather than a free of engine memory.
            m_Counts = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>((void*)handles.counts, k_CountFieldCount, Allocator.None);
        }

        // Same session, plus the write-permitted handle stamped onto m_Counts. The
        // unchecked ctor above does all the real work; this only adds safety plumbing.
        internal UnifiedRenderDataWriter(UnifiedRenderDataWriteHandles handles, AtomicSafetyHandle safety)
            : this(handles)
        {
            if (m_Counts.IsCreated)
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref m_Counts, safety);
        }

        // The session's write-permitted handle, retired by EndWrite(). Read off m_Counts
        // rather than a field of its own, so it tracks job-local, main-thread or dead state.
        AtomicSafetyHandle safetyHandle { get { return NativeArrayUnsafeUtility.GetAtomicSafetyHandle(m_Counts); } }

        // A negative offset is the engine's marker for a channel absent from the declared
        // mask; it becomes an empty slice rather than a null dereference.
        NativeSlice<T> MakeSlice<T>(int offset, int stride) where T : struct
        {
            int count = m_Handles.vertexCapacity;
            if (m_Handles.vertexData == IntPtr.Zero || offset < 0 || stride <= 0 || count <= 0)
                return default;

            var slice = NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<T>((byte*)m_Handles.vertexData + offset, stride, count);
            NativeSliceUnsafeUtility.SetAtomicSafetyHandle(ref slice, safetyHandle);
            return slice;
        }

        NativeArray<T> MakeArray<T>(IntPtr buffer, int count) where T : struct
        {
            if (buffer == IntPtr.Zero || count <= 0)
                return default;

            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>((void*)buffer, count, Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, safetyHandle);
            return array;
        }

        // True while the session is open and the pointers are live; false on a
        // default-constructed writer or after EndWrite() has closed the session.
        public bool isCreated
        {
            get
            {
                if (!m_Counts.IsCreated || m_Handles.counts == IntPtr.Zero)
                    return false;
                return ((int*)m_Handles.counts)[k_SessionIdField] == m_SessionId;
            }
        }

        // ---- Reserved capacity ------------------------------------------------

        public int vertexCapacity => m_Handles.vertexCapacity;
        public int indexCapacity => m_Handles.indexCapacity;
        public int entryCapacity => m_Handles.entryCapacity;
        public int drawRangeCapacity => m_Handles.drawRangeCapacity;

        // ---- Geometry, written in place ---------------------------------------

        // Views over the reserved capacity, not over what has been written so far. Slices
        // rather than arrays for the vertex channels because they are interleaved.

        // Always present; a session without positions has no geometry.
        public NativeSlice<Vector3> positions { get { return MakeSlice<Vector3>(m_Handles.positionOffset, m_Handles.positionStride); } }

        // Empty when the channel was not declared. Writing to an empty slice throws.
        public NativeSlice<Vector2> texCoords { get { return MakeSlice<Vector2>(m_Handles.texCoordOffset, m_Handles.texCoordStride); } }
        public NativeSlice<Vector3> normals { get { return MakeSlice<Vector3>(m_Handles.normalOffset, m_Handles.normalStride); } }
        public NativeSlice<Vector4> tangents { get { return MakeSlice<Vector4>(m_Handles.tangentOffset, m_Handles.tangentStride); } }
        public NativeSlice<Color32> colors { get { return MakeSlice<Color32>(m_Handles.colorOffset, m_Handles.colorStride); } }

        // Absolute index values into the vertex channels above, unlike AddSpriteBatch()'s
        // entry-local ones -- a session owns the whole buffer, so nothing to rebase against.
        public NativeArray<uint> indices { get { return MakeArray<uint>(m_Handles.indices, m_Handles.indexCapacity); } }

        // One record per entry, in draw order. Offsets are absolute, for the same reason.
        // spriteId is an EntityId precisely so this stays writable from a job.
        public NativeArray<SpriteBatchEntry> entries { get { return MakeArray<SpriteBatchEntry>(m_Handles.entries, m_Handles.entryCapacity); } }

        // The draw list, written by the job: entries carry EntityIds, so merging two into one
        // draw is an integer compare, never a Sprite dereference.
        public NativeArray<DrawRange> drawRanges { get { return MakeArray<DrawRange>(m_Handles.drawRanges, m_Handles.drawRangeCapacity); } }

        // Hoist a channel into a local before writing in a loop: each access above builds its view.

        // ---- Counts: OUTPUTS of the producing job ------------------------------

        // How much of each buffer was actually filled; assign from the job, EndWrite() reads
        // them back. Write through engine memory, so they survive the job's by-value copy.
        public int vertexCount { get { CheckOpened(); return m_Counts[0]; } set { CheckOpened(); m_Counts[0] = value; } }
        public int indexCount { get { CheckOpened(); return m_Counts[1]; } set { CheckOpened(); m_Counts[1] = value; } }
        public int entryCount { get { CheckOpened(); return m_Counts[2]; } set { CheckOpened(); m_Counts[2] = value; } }
        public int drawRangeCount { get { CheckOpened(); return m_Counts[3]; } set { CheckOpened(); m_Counts[3] = value; } }

        // Only checks the never-opened case; permission/bounds checks are the NativeArray
        // indexer's own.
        [System.Diagnostics.Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        void CheckOpened()
        {
            if (!m_Counts.IsCreated)
                throw new InvalidOperationException("This UnifiedRenderDataWriter was never opened by BeginWrite().");
            if (!isCreated)
                throw new InvalidOperationException("This UnifiedRenderDataWriter's session was closed by EndWrite(); its pointers are no longer valid.");
        }
    }

    // Managed handle over a native UnifiedRenderData: sprites packed into one shared vertex/index
    // buffer so they draw together. Main thread only; Dispose() when done. Positions stored verbatim.
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/2D/Common/ScriptBindings/UnifiedRenderData.bindings.h")]
    [VisibleToOtherModules("UnityEngine.SpriteShapeModule", "Unity.2D.SpriteShape.Runtime", "Unity.2D.Graphics.Runtime")]
    internal sealed class UnifiedRenderData : IDisposable
    {
        internal IntPtr m_Ptr;

        public UnifiedRenderData()
        {
            m_Ptr = Create();
        }

        // Wraps a native object this handle does not own outright -- the pointer arrives
        // already registered and retained on the handle's behalf, so Dispose() releases
        // that one reference and leaves the original owner's intact. Used by
        // SpriteRenderer/SpriteShapeRenderer.GetUnifiedRenderData() and
        // UnifiedRenderDataAsset.GetRenderData().
        [VisibleToOtherModules("UnityEngine.SpriteShapeModule", "Unity.2D.SpriteShape.Runtime", "Unity.2D.Graphics.Runtime")]
        internal UnifiedRenderData(IntPtr nativeRenderData)
        {
            m_Ptr = nativeRenderData;
        }

        // False once disposed, and also after the engine has released leftovers at shutdown.
        public bool isValid => m_Ptr != IntPtr.Zero && IsLive(m_Ptr);

        public void Dispose()
        {
            if (m_Ptr == IntPtr.Zero)
                return;

            Destroy(m_Ptr);
            m_Ptr = IntPtr.Zero;
        }

        // Caught here to report an accurate exception, since the generated marshalling would
        // otherwise raise NullReferenceException for a disposed object.
        private void CheckNotDisposed()
        {
            if (m_Ptr == IntPtr.Zero)
                throw new ObjectDisposedException(nameof(UnifiedRenderData));
        }

        public int spriteCount
        {
            get
            {
                CheckNotDisposed();
                return GetSpriteCount();
            }
        }

        public int vertexCount
        {
            get
            {
                CheckNotDisposed();
                return GetVertexCount();
            }
        }

        public int indexCount
        {
            get
            {
                CheckNotDisposed();
                return GetIndexCount();
            }
        }

        // Aggregate AABB of this batch, never updated by AddSprite/UpdateSprite/RemoveSprite --
        // set it yourself, or derive it with RecalculateBoundsFromSprites().
        public Bounds bounds
        {
            get
            {
                CheckNotDisposed();
                return GetBounds();
            }
            set
            {
                CheckNotDisposed();
                SetBounds(value);
            }
        }

        // True when geometry changed since the last Prepare(), which means the pointers the
        // graphics device acquired then no longer describe the current buffers.
        public bool isPrepareStale
        {
            get
            {
                CheckNotDisposed();
                return IsPrepareStale();
            }
        }

        // The vertex channels carried, as a bit mask of VertexAttribute values; prefer
        // HasVertexAttribute() over testing bits. Frozen by the first sprite added.
        public int channelMask
        {
            get
            {
                CheckNotDisposed();
                return GetChannelMask();
            }
        }

        public bool HasVertexAttribute(VertexAttribute attribute)
        {
            return 0 != (channelMask & (1 << (int)attribute));
        }

        // Bounds of one sprite, derived from its vertex positions when it was added.
        public Bounds GetSpriteBounds(int index)
        {
            CheckNotDisposed();
            return GetSpriteBoundsInternal(index);
        }

        // Per-sprite shader properties, as stored on the entry. Both are serialized with
        // the entry, which is why AddSprite defaults them rather than leaving them zero.
        public Color GetSpriteColor(int index)
        {
            CheckNotDisposed();
            Vector4 color = GetSpriteColorInternal(index);
            return new Color(color.x, color.y, color.z, color.w);
        }

        public Vector4 GetSpriteProps(int index)
        {
            CheckNotDisposed();
            return GetSpritePropsInternal(index);
        }

        // The engine's own default (currently (1, 1, -1, -1)), read from native so it
        // cannot drift from GetDefaultUnitySpriteProps().
        public static Vector4 defaultSpriteProps => GetDefaultSpriteProps();

        // Every entry of this batch, in the order it draws. Empty when there are no sprites.
        // Each call reads afresh, so hold the result rather than re-reading it in a loop.
        //
        // An OWNED copy: the caller disposes it. Unlike the view accessors below, it stays
        // valid across mutations -- it is a snapshot, not a window.
        //
        // internal, not public: PerEntityRenderData is internal, so a public signature here
        // would be inconsistent accessibility.
        [VisibleToOtherModules("UnityEngine.SpriteShapeModule", "Unity.2D.SpriteShape.Runtime", "Unity.2D.Graphics.Runtime")]
        internal unsafe NativeArray<PerEntityRenderData> GetEntities(Allocator allocator)
        {
            CheckNotDisposed();

            int count = GetSpriteCount();
            var entities = new NativeArray<PerEntityRenderData>(count, allocator, NativeArrayOptions.UninitializedMemory);
            if (count > 0)
                FillEntities((IntPtr)entities.GetUnsafePtr(), count);

            return entities;
        }

        // One EntityId per entry, in draw order; EntityId.None where the entry has no sprite.
        // Owned copy, caller disposes.
        public unsafe NativeArray<EntityId> GetEntitySprites(Allocator allocator)
        {
            CheckNotDisposed();

            int count = GetSpriteCount();
            var sprites = new NativeArray<EntityId>(count, allocator, NativeArrayOptions.UninitializedMemory);
            if (count > 0)
                FillEntitySprites((IntPtr)sprites.GetUnsafePtr(), count);

            return sprites;
        }

        // Entry `index`'s per-instance transforms: exactly max(instances, 1) of them, or an
        // empty array when the producer supplied none. Owned copy, caller disposes.
        public unsafe NativeArray<Matrix4x4> GetEntityTransforms(int index, Allocator allocator)
        {
            CheckNotDisposed();

            int count = GetEntityTransformCount(index);
            var transforms = new NativeArray<Matrix4x4>(count, allocator, NativeArrayOptions.UninitializedMemory);
            if (count > 0)
                FillEntityTransforms(index, (IntPtr)transforms.GetUnsafePtr(), count);

            return transforms;
        }

        // Entry `index`'s per-instance colours, independently optional from the transforms.
        // Owned copy, caller disposes.
        public unsafe NativeArray<Color> GetEntityColors(int index, Allocator allocator)
        {
            CheckNotDisposed();

            int count = GetEntityColorCount(index);
            var colors = new NativeArray<Color>(count, allocator, NativeArrayOptions.UninitializedMemory);
            if (count > 0)
                FillEntityColors(index, (IntPtr)colors.GetUnsafePtr(), count);

            return colors;
        }

        // Replaces entry `index`'s per-instance transforms and sets its instance count. An
        // uncreated/empty array clears them; a length mismatch with existing colours clears those too.
        public unsafe void SetEntityTransforms(int index, NativeArray<Matrix4x4> transforms)
        {
            CheckNotDisposed();
            SetEntityTransforms(index, ChannelPointer(transforms), transforms.IsCreated ? transforms.Length : 0);
        }

        // Replaces entry `index`'s per-instance colours and sets its instance count. If the
        // entry carries transforms, `colors` must match their length exactly or this throws.
        public unsafe void SetEntityColors(int index, NativeArray<Color> colors)
        {
            CheckNotDisposed();
            SetEntityColors(index, ChannelPointer(colors), colors.IsCreated ? colors.Length : 0);
        }

        // The draw ranges the producing renderer recorded, in draw order; empty means nothing
        // to draw. A view, valid only until the next mutation -- do not Dispose() it.
        [VisibleToOtherModules("UnityEngine.SpriteShapeModule", "Unity.2D.SpriteShape.Runtime", "Unity.2D.Graphics.Runtime")]
        internal NativeArray<DrawRange> GetDrawRanges()
        {
            CheckNotDisposed();
            return ViewAsArray<DrawRange>(GetDrawRangeInfo());
        }

        public int drawRangeCount
        {
            get
            {
                CheckNotDisposed();
                return GetDrawRangeCount();
            }
        }

        // The shared index buffer; values are absolute into the vertex channels below, not
        // entry-relative. A view, see GetPositions(); uint because native indices are UInt32.
        public NativeArray<uint> GetIndices()
        {
            CheckNotDisposed();
            return ViewAsArray<uint>(GetIndexBufferInfo());
        }

        // The shared vertex positions, in this object's shared space. A zero-copy view, not
        // owned -- do not Dispose(); any mutation invalidates it, so take it, use it, drop it.
        public NativeSlice<Vector3> GetPositions()
        {
            CheckNotDisposed();
            return ViewAsSlice<Vector3>(GetPositionsInfo());
        }

        // The optional vertex channels: empty when the bit is absent from channelMask,
        // otherwise as long as GetPositions(). HasVertexAttribute() is the authoritative test.
        public NativeSlice<Vector2> GetTexCoords()
        {
            CheckNotDisposed();
            return ViewAsSlice<Vector2>(GetTexCoordsInfo());
        }

        public NativeSlice<Vector3> GetNormals()
        {
            CheckNotDisposed();
            return ViewAsSlice<Vector3>(GetNormalsInfo());
        }

        public NativeSlice<Vector4> GetTangents()
        {
            CheckNotDisposed();
            return ViewAsSlice<Vector4>(GetTangentsInfo());
        }

        public NativeSlice<Color32> GetColors()
        {
            CheckNotDisposed();
            return ViewAsSlice<Color32>(GetColorsInfo());
        }

        // Wraps a described slice of native memory without copying, via Allocator.None.
        // Native retires the safety handle on every mutation, turning a stale view into a throw.
        private unsafe NativeArray<T> ViewAsArray<T>(UnifiedRenderDataBufferInfo info) where T : struct
        {
            if (info.buffer == IntPtr.Zero || info.count == 0)
                return default;

            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(
                (byte*)info.buffer + info.offset, info.count, Allocator.None);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, GetSafetyHandle());
            return array;
        }

        private unsafe NativeSlice<T> ViewAsSlice<T>(UnifiedRenderDataBufferInfo info) where T : struct
        {
            if (info.buffer == IntPtr.Zero || info.count == 0)
                return default;

            var slice = NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<T>(
                (byte*)info.buffer + info.offset, info.stride, info.count);
            NativeSliceUnsafeUtility.SetAtomicSafetyHandle(ref slice, GetSafetyHandle());
            return slice;
        }

        // Adds `sprite` as drawn: its own mesh is copied in (baked, not referenced, so the
        // entry can be drawn instanced), placed by `transform`. For geometry of your own, use
        // the overload that takes positions.
        public int AddSprite(Sprite sprite)
        {
            return AddSprite(sprite, Matrix4x4.identity);
        }

        public int AddSprite(Sprite sprite, Matrix4x4 transform)
        {
            CheckNotDisposed();
            if (sprite == null)
                throw new ArgumentNullException(nameof(sprite));
            return AddSpriteMesh(sprite, transform);
        }

        // Adds caller-supplied geometry, transformed by `transform` into this object's shared
        // space; pass Matrix4x4.identity for geometry already there. `sprite` supplies only the
        // textures and may be null. The optional channels are left out by passing default; the
        // first sprite added freezes which channels this object carries.
        public int AddSprite(Sprite sprite, Matrix4x4 transform,
            NativeArray<Vector3> positions, NativeArray<Vector2> uvs, NativeArray<uint> indices,
            NativeArray<Vector3> normals = default, NativeArray<Vector4> tangents = default, NativeArray<Color32> colors = default)
        {
            return AddSprite(sprite, transform, positions, uvs, indices, normals, tangents, colors,
                Color.white, defaultSpriteProps);
        }

        // `color` and `spriteProps` are per-sprite shader properties stored on the entry, not
        // vertex data -- a tilemap's per-tile colour belongs here, not in `colors`.
        public int AddSprite(Sprite sprite, Matrix4x4 transform,
            NativeArray<Vector3> positions, NativeArray<Vector2> uvs, NativeArray<uint> indices,
            NativeArray<Vector3> normals, NativeArray<Vector4> tangents, NativeArray<Color32> colors,
            Color color, Vector4 spriteProps)
        {
            CheckNotDisposed();
            var geometry = BuildGeometry(positions, uvs, normals, tangents, colors, indices);
            return Add(sprite, geometry, transform, new Vector4(color.r, color.g, color.b, color.a), spriteProps);
        }

        // Appends a batch in one call. `positions` are in this object's shared space, `indices` are
        // entry-local, and `entries` must tile gap-free from 0. Returns the first index, or -1 if empty.
        [VisibleToOtherModules("UnityEngine.SpriteShapeModule", "Unity.2D.SpriteShape.Runtime", "Unity.2D.Graphics.Runtime")]
        internal int AddSpriteBatch(NativeArray<SpriteBatchEntry> entries,
            NativeArray<Vector3> positions, NativeArray<Vector2> uvs, NativeArray<Vector3> normals,
            NativeArray<Vector4> tangents, NativeArray<Color32> colors, NativeArray<uint> indices)
        {
            CheckNotDisposed();

            if (!entries.IsCreated || entries.Length == 0 || !positions.IsCreated || positions.Length == 0)
                return -1;

            int vertexCount = positions.Length;
            CheckChannelLength(nameof(uvs), uvs, vertexCount);
            CheckChannelLength(nameof(normals), normals, vertexCount);
            CheckChannelLength(nameof(tangents), tangents, vertexCount);
            CheckChannelLength(nameof(colors), colors, vertexCount);

            var batch = new SpriteBatchGeometryData
            {
                positions = ChannelPointer(positions),
                uvs = ChannelPointer(uvs),
                normals = ChannelPointer(normals),
                tangents = ChannelPointer(tangents),
                colors = ChannelPointer(colors),
                indices = ChannelPointer(indices),
                entries = ChannelPointer(entries),
                vertexCount = vertexCount,
                indexCount = indices.IsCreated ? indices.Length : 0,
                entryCount = entries.Length,
            };

            return AddBatch(batch);
        }

        // Replaces entry `index` with caller-supplied geometry; the parameters mean what they
        // do on AddSprite.
        public void UpdateSprite(int index, Sprite sprite, Matrix4x4 transform,
            NativeArray<Vector3> positions, NativeArray<Vector2> uvs, NativeArray<uint> indices,
            NativeArray<Vector3> normals = default, NativeArray<Vector4> tangents = default, NativeArray<Color32> colors = default)
        {
            UpdateSprite(index, sprite, transform, positions, uvs, indices, normals, tangents, colors,
                Color.white, defaultSpriteProps);
        }

        public void UpdateSprite(int index, Sprite sprite, Matrix4x4 transform,
            NativeArray<Vector3> positions, NativeArray<Vector2> uvs, NativeArray<uint> indices,
            NativeArray<Vector3> normals, NativeArray<Vector4> tangents, NativeArray<Color32> colors,
            Color color, Vector4 spriteProps)
        {
            CheckNotDisposed();
            var geometry = BuildGeometry(positions, uvs, normals, tangents, colors, indices);
            Update(index, sprite, geometry, transform, new Vector4(color.r, color.g, color.b, color.a), spriteProps);
        }

        public void RemoveSprite(int index)
        {
            CheckNotDisposed();
            Remove(index);
        }

        // Empties this batch while keeping the object, so it can be refilled without
        // the cost of destroying and recreating it. The channel layout unfreezes --
        // the next AddSprite picks it again -- and bounds go back to zero.
        //
        // releaseMemory also gives the backing allocations back, at the cost of
        // reallocating on the next AddSprite. Leave it false when this batch is
        // about to be refilled.
        public void Clear(bool releaseMemory = false)
        {
            CheckNotDisposed();
            ClearInternal(releaseMemory);
        }

        // Forces texture bindings to re-resolve next render, for a Sprite's texture swapped
        // underneath an unchanged batch (sprite atlas late binding).
        public void InvalidateTextureData()
        {
            CheckNotDisposed();
            InvalidateTextureDataInternal();
        }

        // Sets bounds to the union of every sprite's bounds. Opt-in.
        public void RecalculateBoundsFromSprites()
        {
            CheckNotDisposed();
            RecalculateBoundsFromSpritesInternal();
        }

        // Derives the draw list: one range per run of adjacent entries sharing a draw (same
        // texture set, colour and props). Opt-in; see InvalidateTextureData() for atlas rebinds.
        public void RecalculateDrawRangesFromSprites()
        {
            CheckNotDisposed();
            RecalculateDrawRangesFromSpritesInternal();
        }

        // As RecalculateDrawRangesFromSprites(), but with no coalescing: one range per entry.
        // Use for instanced drawing, where a merged range would replay a neighbour's geometry too.
        public void RecalculateDrawRangesPerEntity()
        {
            CheckNotDisposed();
            RecalculateDrawRangesPerEntityInternal();
        }

        // Uploads the current geometry to the graphics device. Optional: the render entry
        // points prepare a stale batch themselves (see isPrepareStale), so call this only to
        // pay the upload ahead of the first draw.
        public void Prepare()
        {
            CheckNotDisposed();
            PrepareInternal();
        }

        private static unsafe SpriteGeometryData BuildGeometry(
            NativeArray<Vector3> positions, NativeArray<Vector2> uvs, NativeArray<Vector3> normals,
            NativeArray<Vector4> tangents, NativeArray<Color32> colors, NativeArray<uint> indices)
        {
            if (!positions.IsCreated || positions.Length == 0)
                throw new ArgumentException("A sprite must supply at least one vertex position.", nameof(positions));

            int vertexCount = positions.Length;
            CheckChannelLength(nameof(uvs), uvs, vertexCount);
            CheckChannelLength(nameof(normals), normals, vertexCount);
            CheckChannelLength(nameof(tangents), tangents, vertexCount);
            CheckChannelLength(nameof(colors), colors, vertexCount);

            return new SpriteGeometryData
            {
                positions = ChannelPointer(positions),
                uvs = ChannelPointer(uvs),
                normals = ChannelPointer(normals),
                tangents = ChannelPointer(tangents),
                colors = ChannelPointer(colors),
                indices = ChannelPointer(indices),
                vertexCount = vertexCount,
                indexCount = indices.IsCreated ? indices.Length : 0,
            };
        }

        private static void CheckChannelLength<T>(string name, NativeArray<T> channel, int vertexCount) where T : struct
        {
            int length = channel.IsCreated ? channel.Length : 0;
            if (length != 0 && length != vertexCount)
                throw new ArgumentException($"The {name} array must be empty or hold {vertexCount} entries to match the position array, but it holds {length}.", name);
        }

        // A null pointer tells the native side the channel was not supplied.
        private static unsafe IntPtr ChannelPointer<T>(NativeArray<T> channel) where T : struct
        {
            if (!channel.IsCreated || channel.Length == 0)
                return IntPtr.Zero;

            return (IntPtr)channel.GetUnsafeReadOnlyPtr();
        }

        [FreeFunction("UnifiedRenderData_Bindings::Create")]
        extern private static IntPtr Create();

        [FreeFunction("UnifiedRenderData_Bindings::Destroy")]
        extern private static void Destroy(IntPtr renderData);

        [FreeFunction("UnifiedRenderData_Bindings::IsLive")]
        extern private static bool IsLive(IntPtr renderData);

        [FreeFunction("UnifiedRenderData_Bindings::Add", HasExplicitThis = true, ThrowsException = true)]
        extern private int Add(Sprite sprite, SpriteGeometryData geometry, Matrix4x4 transform, Vector4 spriteColor, Vector4 spriteProps);

        [FreeFunction("UnifiedRenderData_Bindings::AddSpriteMesh", HasExplicitThis = true, ThrowsException = true)]
        extern private int AddSpriteMesh(Sprite sprite, Matrix4x4 transform);

        [FreeFunction("UnifiedRenderData_Bindings::AddBatch", HasExplicitThis = true, ThrowsException = true)]
        extern private int AddBatch(SpriteBatchGeometryData batch);

        [FreeFunction("UnifiedRenderData_Bindings::Update", HasExplicitThis = true, ThrowsException = true)]
        extern private void Update(int index, Sprite sprite, SpriteGeometryData geometry, Matrix4x4 transform, Vector4 spriteColor, Vector4 spriteProps);

        [FreeFunction("UnifiedRenderData_Bindings::Remove", HasExplicitThis = true, ThrowsException = true)]
        extern private void Remove(int index);

        [FreeFunction("UnifiedRenderData_Bindings::Clear", HasExplicitThis = true, ThrowsException = true)]
        extern private void ClearInternal(bool releaseMemory);

        [FreeFunction("UnifiedRenderData_Bindings::InvalidateTextureData", HasExplicitThis = true, ThrowsException = true)]
        extern private void InvalidateTextureDataInternal();

        [FreeFunction("UnifiedRenderData_Bindings::GetSpriteCount", HasExplicitThis = true, ThrowsException = true)]
        extern private int GetSpriteCount();

        [FreeFunction("UnifiedRenderData_Bindings::GetVertexCount", HasExplicitThis = true, ThrowsException = true)]
        extern private int GetVertexCount();

        [FreeFunction("UnifiedRenderData_Bindings::GetIndexCount", HasExplicitThis = true, ThrowsException = true)]
        extern private int GetIndexCount();

        [FreeFunction("UnifiedRenderData_Bindings::GetBounds", HasExplicitThis = true, ThrowsException = true)]
        extern private Bounds GetBounds();

        [FreeFunction("UnifiedRenderData_Bindings::SetBounds", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetBounds(Bounds value);

        [FreeFunction("UnifiedRenderData_Bindings::GetSpriteBounds", HasExplicitThis = true, ThrowsException = true)]
        extern private Bounds GetSpriteBoundsInternal(int index);

        [FreeFunction("UnifiedRenderData_Bindings::GetSpriteColor", HasExplicitThis = true, ThrowsException = true)]
        extern private Vector4 GetSpriteColorInternal(int index);

        [FreeFunction("UnifiedRenderData_Bindings::GetSpriteProps", HasExplicitThis = true, ThrowsException = true)]
        extern private Vector4 GetSpritePropsInternal(int index);

        [FreeFunction("UnifiedRenderData_Bindings::GetDefaultSpriteProps")]
        extern private static Vector4 GetDefaultSpriteProps();

        [FreeFunction("UnifiedRenderData_Bindings::GetChannelMask", HasExplicitThis = true, ThrowsException = true)]
        extern private int GetChannelMask();

        [FreeFunction("UnifiedRenderData_Bindings::GetEntityTransformCount", HasExplicitThis = true, ThrowsException = true)]
        extern private int GetEntityTransformCount(int index);

        [FreeFunction("UnifiedRenderData_Bindings::GetEntityColorCount", HasExplicitThis = true, ThrowsException = true)]
        extern private int GetEntityColorCount(int index);

        [FreeFunction("UnifiedRenderData_Bindings::FillEntitySprites", HasExplicitThis = true, ThrowsException = true)]
        extern private void FillEntitySprites(IntPtr dest, int count);

        [FreeFunction("UnifiedRenderData_Bindings::FillEntityTransforms", HasExplicitThis = true, ThrowsException = true)]
        extern private void FillEntityTransforms(int index, IntPtr dest, int count);

        [FreeFunction("UnifiedRenderData_Bindings::FillEntityColors", HasExplicitThis = true, ThrowsException = true)]
        extern private void FillEntityColors(int index, IntPtr dest, int count);

        [FreeFunction("UnifiedRenderData_Bindings::SetEntityTransforms", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetEntityTransforms(int index, IntPtr src, int count);

        [FreeFunction("UnifiedRenderData_Bindings::SetEntityColors", HasExplicitThis = true, ThrowsException = true)]
        extern private void SetEntityColors(int index, IntPtr src, int count);

        [FreeFunction("UnifiedRenderData_Bindings::FillEntities", HasExplicitThis = true, ThrowsException = true)]
        extern private void FillEntities(IntPtr dest, int count);

        [FreeFunction("UnifiedRenderData_Bindings::GetDrawRangeCount", HasExplicitThis = true, ThrowsException = true)]
        extern private int GetDrawRangeCount();

        [FreeFunction("UnifiedRenderData_Bindings::GetDrawRangeInfo", HasExplicitThis = true, ThrowsException = true)]
        extern private UnifiedRenderDataBufferInfo GetDrawRangeInfo();

        [FreeFunction("UnifiedRenderData_Bindings::GetIndexBufferInfo", HasExplicitThis = true, ThrowsException = true)]
        extern private UnifiedRenderDataBufferInfo GetIndexBufferInfo();

        [FreeFunction("UnifiedRenderData_Bindings::GetPositionsInfo", HasExplicitThis = true, ThrowsException = true)]
        extern private UnifiedRenderDataBufferInfo GetPositionsInfo();

        [FreeFunction("UnifiedRenderData_Bindings::GetTexCoordsInfo", HasExplicitThis = true, ThrowsException = true)]
        extern private UnifiedRenderDataBufferInfo GetTexCoordsInfo();

        [FreeFunction("UnifiedRenderData_Bindings::GetNormalsInfo", HasExplicitThis = true, ThrowsException = true)]
        extern private UnifiedRenderDataBufferInfo GetNormalsInfo();

        [FreeFunction("UnifiedRenderData_Bindings::GetTangentsInfo", HasExplicitThis = true, ThrowsException = true)]
        extern private UnifiedRenderDataBufferInfo GetTangentsInfo();

        [FreeFunction("UnifiedRenderData_Bindings::GetColorsInfo", HasExplicitThis = true, ThrowsException = true)]
        extern private UnifiedRenderDataBufferInfo GetColorsInfo();

        [FreeFunction("UnifiedRenderData_Bindings::GetSafetyHandle", HasExplicitThis = true, ThrowsException = true)]
        extern private AtomicSafetyHandle GetSafetyHandle();

        [FreeFunction("UnifiedRenderData_Bindings::RecalculateBoundsFromSprites", HasExplicitThis = true, ThrowsException = true)]
        extern private void RecalculateBoundsFromSpritesInternal();

        [FreeFunction("UnifiedRenderData_Bindings::RecalculateDrawRangesFromSprites", HasExplicitThis = true, ThrowsException = true)]
        extern private void RecalculateDrawRangesFromSpritesInternal();

        [FreeFunction("UnifiedRenderData_Bindings::RecalculateDrawRangesPerEntity", HasExplicitThis = true, ThrowsException = true)]
        extern private void RecalculateDrawRangesPerEntityInternal();

        // Opens a zero-copy write session and returns a writer a Burst job fills in place.
        // This clears the instance; `channels` is a VertexAttribute mask. Main thread only.
        [VisibleToOtherModules("UnityEngine.SpriteShapeModule", "Unity.2D.SpriteShape.Runtime", "Unity.2D.Graphics.Runtime")]
        internal UnifiedRenderDataWriter BeginWrite(int maxVertices, int maxIndices, int maxEntries, int channels)
        {
            CheckNotDisposed();

            UnifiedRenderDataWriteHandles handles = BeginWriteInternal(maxVertices, maxIndices, maxEntries, channels);
            if (handles.counts == IntPtr.Zero)
                return default;

            AtomicSafetyHandle safety = GetWriteSafetyHandle();
            return new UnifiedRenderDataWriter(handles, safety);
        }

        // Closes the session opened by BeginWrite(), truncating buffers to the job's reported
        // counts. The job must have completed first. Idempotent, main thread only.
        [VisibleToOtherModules("UnityEngine.SpriteShapeModule", "Unity.2D.SpriteShape.Runtime", "Unity.2D.Graphics.Runtime")]
        internal void EndWrite()
        {
            CheckNotDisposed();
            EndWriteInternal();
        }

        // True between BeginWrite() and EndWrite(). While true this instance is pinned and
        // every mutator on it refuses.
        [VisibleToOtherModules("UnityEngine.SpriteShapeModule", "Unity.2D.SpriteShape.Runtime", "Unity.2D.Graphics.Runtime")]
        internal bool isWriteSessionOpen
        {
            get
            {
                CheckNotDisposed();
                return IsWriteSessionOpenInternal();
            }
        }

        [FreeFunction("UnifiedRenderData_Bindings::BeginWrite", HasExplicitThis = true, ThrowsException = true)]
        extern private UnifiedRenderDataWriteHandles BeginWriteInternal(int maxVertices, int maxIndices, int maxEntries, int channelMask);

        [FreeFunction("UnifiedRenderData_Bindings::EndWrite", HasExplicitThis = true, ThrowsException = true)]
        extern private void EndWriteInternal();

        [FreeFunction("UnifiedRenderData_Bindings::IsWriteSessionOpen", HasExplicitThis = true, ThrowsException = true)]
        extern private bool IsWriteSessionOpenInternal();

        [FreeFunction("UnifiedRenderData_Bindings::GetWriteSafetyHandle", HasExplicitThis = true, ThrowsException = true)]
        extern private AtomicSafetyHandle GetWriteSafetyHandle();

        [FreeFunction("UnifiedRenderData_Bindings::IsPrepareStale", HasExplicitThis = true, ThrowsException = true)]
        extern private bool IsPrepareStale();

        [FreeFunction("UnifiedRenderData_Bindings::Prepare", HasExplicitThis = true, ThrowsException = true)]
        extern private void PrepareInternal();

        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(UnifiedRenderData renderData) => renderData.m_Ptr;
        }
    }
}
