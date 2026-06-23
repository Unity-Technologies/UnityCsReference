// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace UnityEngine.UIElements.UIR
{
    enum EntryType : ushort
    {
        DrawSolidMesh,
        DrawTexturedMesh,
        DrawDynamicTexturedMesh,
        DrawTextMesh,
        DrawGradients,
        DrawImmediate,
        DrawImmediateCull,
        DrawChildren,
        BeginStencilMask,
        EndStencilMask,
        PopStencilMask,
        PushClippingRect,
        PopClippingRect,
        PushScissors,
        PopScissors,
        PushGroupMatrix,
        PopGroupMatrix,
        PushDefaultMaterial,
        PopDefaultMaterial,
        CutRenderChain,
        // Profiler-only Begin/End markers.
        BeginPanelComponent,
        EndPanelComponent,
        DedicatedPlaceholder,
        GenerateBackdropFilterTexture
    }

    [Flags]
    enum EntryFlags : ushort
    {
        UsesTextCoreSettings         = 1 << 0,
        IsPremultiplied              = 1 << 1,
        SkipDynamicAtlas             = 1 << 2,
        HasExtras                    = 1 << 3,

        // DrawPhase packed in 2 bits; Background==0 so default flags read as Background.
        DrawPhaseBitOffset           = 4,
        DrawPhaseBackground          = 0 << DrawPhaseBitOffset,
        DrawPhaseBorder              = 1 << DrawPhaseBitOffset,
        DrawPhaseContent             = 2 << DrawPhaseBitOffset,
        DrawPhaseMask                = 3 << DrawPhaseBitOffset,
        DrawPhaseBits                = 3 << DrawPhaseBitOffset,

        UsesPerGlyphTextCoreSettings = 1 << 6,
    }

    class Entry
    {
        public EntryType type;
        public EntryFlags flags;

        // In an entry, the winding order is ALWAYS clockwise (front-facing)
        public NativeSlice<Vertex> vertices;
        public NativeSlice<ushort> indices;

        // Empty slice means "channel not provided" — the convert job zero-fills.
        public NativeSlice<Vector4> texCoord1;
        public NativeSlice<Vector4> texCoord2;
        public NativeSlice<Vector4> texCoord3;
        public NativeSlice<Vector3> normal;
        public NativeSlice<Vector4> tangent;

        public Texture texture;
        public float textScale;
        public float fontSharpness;
        public VectorImage gradientsOwner;
        public Material material;
        public MaterialPropertyBlock userProps;
        public Action immediateCallback;
        public TextureId textureId;
        // Set on BeginPanelComponent entries only.
        public EntityId panelComponentId;
        public int userData;

        public Entry nextSibling;

        public Entry firstChild;
        public Entry lastChild;

        public DrawPhase phase
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (DrawPhase)((int)(flags & EntryFlags.DrawPhaseBits) >> (int)EntryFlags.DrawPhaseBitOffset);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => flags = (flags & ~EntryFlags.DrawPhaseBits) | (EntryFlags)((int)value << (int)EntryFlags.DrawPhaseBitOffset);
        }

        public void Reset()
        {
            nextSibling = null;
            firstChild = null;
            lastChild = null;
            texture = null;
            material = null;
            userProps = null;
            gradientsOwner = null;
            flags = 0;
            userData = 0;
            immediateCallback = null;
            panelComponentId = EntityId.None;
            texCoord1 = default;
            texCoord2 = default;
            texCoord3 = default;
            normal = default;
            tangent = default;
        }
    }

    // This class converts the most basic operations into entries. It performs no transformation of any kind,
    // no tessellation. Any higher-level operation that uses these operations must NOT be added to this class. This
    // must be the ONLY place where we create entries.
    class EntryRecorder
    {
        EntryPool m_EntryPool;
        readonly ExtraVertexChannels m_PanelExtras;

        public EntryRecorder(EntryPool entryPool, ExtraVertexChannels panelExtras = ExtraVertexChannels.None)
        {
            Debug.Assert(entryPool != null);
            m_EntryPool = entryPool;
            m_PanelExtras = panelExtras;
        }

        public void DrawMesh(Entry parentEntry, NativeSlice<Vertex> vertices, NativeSlice<ushort> indices, DrawPhase phase = DrawPhase.Content, int userData = 0)
        {
            DrawMesh(parentEntry, vertices, indices, null, TextureOptions.None, phase, userData);
        }

        public void DrawMesh(Entry parentEntry, NativeSlice<Vertex> vertices, NativeSlice<ushort> indices, Texture texture, TextureOptions textureOptions = TextureOptions.None, DrawPhase phase = DrawPhase.Content, int userData = 0)
        {
            var mesh = new UIMesh { vertices = vertices, indices = indices };
            DrawMesh(parentEntry, ref mesh, texture, textureOptions, true, phase, userData);
        }

        public void DrawMesh(Entry parentEntry, ref UIMesh mesh, Texture texture, TextureOptions textureOptions = TextureOptions.None, bool ignoreExtras = false, DrawPhase phase = DrawPhase.Content, int userData = 0)
        {
            int vertexCount = mesh.vertices.Length;

            if (!ignoreExtras)
            {
                // Strict: every non-empty extras slice must match vertices.Length. Indices reference vertex slots,
                // so a shorter extras slice would cause an out-of-range read in the convert job.
                if (!CheckExtras(mesh.uv1,     nameof(UIMesh.uv1),     vertexCount)) return;
                if (!CheckExtras(mesh.uv2,     nameof(UIMesh.uv2),     vertexCount)) return;
                if (!CheckExtras(mesh.uv3,     nameof(UIMesh.uv3),     vertexCount)) return;
                if (!CheckExtras(mesh.normal,  nameof(UIMesh.normal),  vertexCount)) return;
                if (!CheckExtras(mesh.tangent, nameof(UIMesh.tangent), vertexCount)) return;

                // Drop slices for channels the panel didn't opt into. Stream-1 has no GPU slot for them and the
                // user wouldn't see the data anyway — log so the mistake is visible during development.
                mesh.uv1     = DropDisabledChannel(mesh.uv1,     nameof(UIMesh.uv1),     ExtraVertexChannels.TexCoord1);
                mesh.uv2     = DropDisabledChannel(mesh.uv2,     nameof(UIMesh.uv2),     ExtraVertexChannels.TexCoord2);
                mesh.uv3     = DropDisabledChannel(mesh.uv3,     nameof(UIMesh.uv3),     ExtraVertexChannels.TexCoord3);
                mesh.normal  = DropDisabledChannel(mesh.normal,  nameof(UIMesh.normal),  ExtraVertexChannels.Normal);
                mesh.tangent = DropDisabledChannel(mesh.tangent, nameof(UIMesh.tangent), ExtraVertexChannels.Tangent);
            }

            var entry = m_EntryPool.Get();
            entry.vertices = mesh.vertices;
            entry.indices = mesh.indices;
            entry.texture = texture;
            entry.userData = userData;

            EntryFlags entryFlags = 0;

            if (!ignoreExtras)
            {
                entry.texCoord1 = mesh.uv1;
                entry.texCoord2 = mesh.uv2;
                entry.texCoord3 = mesh.uv3;
                entry.normal = mesh.normal;
                entry.tangent = mesh.tangent;
                if ((mesh.uv1.Length | mesh.uv2.Length | mesh.uv3.Length | mesh.normal.Length | mesh.tangent.Length) != 0) // Bitwise OR is faster
                    entryFlags |= EntryFlags.HasExtras;
            }

            if (object.ReferenceEquals(null, texture))
                entry.type = EntryType.DrawSolidMesh;
            else
            {
                entry.type = EntryType.DrawTexturedMesh;
                if ((textureOptions & TextureOptions.PremultipliedAlpha) != 0)
                    entryFlags |= EntryFlags.IsPremultiplied;
                if ((textureOptions & TextureOptions.SkipDynamicAtlas) != 0)
                    entryFlags |= EntryFlags.SkipDynamicAtlas;
            }

            entry.flags = entryFlags;
            entry.phase = phase;

            AppendMeshEntry(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool CheckExtras<T>(NativeSlice<T> slice, string name, int expectedLength) where T : struct
        {
            if (slice.Length == 0 || slice.Length == expectedLength)
                return true;
            Debug.LogError($"UIMesh.{name} has length {slice.Length}, expected {expectedLength} (vertices.Length). Dropping the entire draw call.\n{Environment.StackTrace}");
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        NativeSlice<T> DropDisabledChannel<T>(NativeSlice<T> slice, string name, ExtraVertexChannels channel) where T : struct
        {
            if (slice.Length == 0 || (m_PanelExtras & channel) != 0)
                return slice;
            Debug.LogError($"UIMesh.{name} provided but the panel's ExtraVertexChannels does not enable {channel}. Dropping this channel from the draw.\n{Environment.StackTrace}");
            return default;
        }

        public void DrawMesh(Entry parentEntry, NativeSlice<Vertex> vertices, NativeSlice<ushort> indices, TextureId textureId, bool isPremultiplied = false, DrawPhase phase = DrawPhase.Content, int userData = 0)
        {
            Debug.Assert(textureId.IsValid());
            var entry = m_EntryPool.Get();
            entry.vertices = vertices;
            entry.indices = indices;
            entry.textureId = textureId;
            entry.flags = isPremultiplied ? EntryFlags.IsPremultiplied : 0;
            entry.type = EntryType.DrawDynamicTexturedMesh;
            entry.phase = phase;
            entry.userData = userData;
            AppendMeshEntry(parentEntry, entry);
        }

        public void DrawRasterText(Entry parentEntry, NativeSlice<Vertex> vertices, NativeSlice<ushort> indices, Texture texture, bool multiChannel, bool usesPerGlyphTextCoreSettings = false, DrawPhase phase = DrawPhase.Content)
        {
            var entry = m_EntryPool.Get();
            entry.flags = EntryFlags.UsesTextCoreSettings; // For dynamic color
            if (usesPerGlyphTextCoreSettings)
                entry.flags |= EntryFlags.UsesPerGlyphTextCoreSettings;
            if (multiChannel)
            {
                entry.type = EntryType.DrawTexturedMesh;
                entry.flags |= EntryFlags.SkipDynamicAtlas;
            }
            else
                entry.type = EntryType.DrawTextMesh;
            entry.vertices = vertices;
            entry.indices = indices;
            entry.texture = texture;
            entry.textScale = 0; // Used in the shader to indicate raster text
            entry.fontSharpness = 0; // N/A
            entry.phase = phase;
            AppendMeshEntry(parentEntry, entry);
        }

        public void DrawSdfText(Entry parentEntry, NativeSlice<Vertex> vertices, NativeSlice<ushort> indices, Texture texture, float scale, float sharpness, bool usesPerGlyphTextCoreSettings = false, DrawPhase phase = DrawPhase.Content)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.DrawTextMesh;
            entry.flags = EntryFlags.UsesTextCoreSettings;
            if (usesPerGlyphTextCoreSettings)
                entry.flags |= EntryFlags.UsesPerGlyphTextCoreSettings;
            entry.vertices = vertices;
            entry.indices = indices;
            entry.texture = texture;
            entry.textScale = scale;
            entry.fontSharpness = sharpness;
            entry.phase = phase;
            AppendMeshEntry(parentEntry, entry);
        }

        // Note: A vector image that doesn't use gradients must NOT be submitted with this call.
        public void DrawGradients(Entry parentEntry, NativeSlice<Vertex> vertices, NativeSlice<ushort> indices, VectorImage gradientsOwner, DrawPhase phase = DrawPhase.Content, int userData = 0)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.DrawGradients;
            entry.vertices = vertices;
            entry.indices = indices;
            entry.gradientsOwner = gradientsOwner;
            entry.phase = phase;
            entry.userData = userData;
            AppendMeshEntry(parentEntry, entry);
        }

        public void DrawImmediate(Entry parentEntry, Action callback, bool cullingEnabled)
        {
            var entry = m_EntryPool.Get();
            entry.type = cullingEnabled ? EntryType.DrawImmediateCull : EntryType.DrawImmediate;
            entry.immediateCallback = callback;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DrawChildren(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.DrawChildren;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginStencilMask(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.BeginStencilMask;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndStencilMask(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.EndStencilMask;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PopStencilMask(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PopStencilMask;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushClippingRect(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PushClippingRect;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PopClippingRect(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PopClippingRect;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushScissors(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PushScissors;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PopScissors(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PopScissors;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushGroupMatrix(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PushGroupMatrix;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PopGroupMatrix(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PopGroupMatrix;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushDefaultMaterial(Entry parentEntry, UnmanagedMaterialDefinition matDef)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PushDefaultMaterial;
            entry.material = (Material)Resources.EntityIdToObject(matDef.material);
            entry.userProps = matDef.BuildPropertyBlock();
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PopDefaultMaterial(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.PopDefaultMaterial;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CutRenderChain(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.CutRenderChain;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginPanelComponent(Entry parentEntry, EntityId panelComponentId)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.BeginPanelComponent;
            entry.panelComponentId = panelComponentId;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndPanelComponent(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.EndPanelComponent;
            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GenerateBackdropFilterTexture(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.GenerateBackdropFilterTexture;
            Append(parentEntry, entry);
        }

        // Returns an entry to which children can be added
        public Entry InsertPlaceholder(Entry parentEntry)
        {
            var entry = m_EntryPool.Get();
            entry.type = EntryType.DedicatedPlaceholder;
            Append(parentEntry, entry);
            return entry;
        }

        static void AppendMeshEntry(Entry parentEntry, Entry entry)
        {
            int vertexCount = entry.vertices.Length;
            int indexCount = entry.indices.Length;

            if (vertexCount == 0)
            {
                Debug.LogError("Attempting to add an entry without vertices.");
                return;
            }

            if (vertexCount > UIRenderDevice.maxVerticesPerPage)
            {
                Debug.LogError($"Attempting to add an entry with {vertexCount} vertices. The maximum number of vertices per entry is {UIRenderDevice.maxVerticesPerPage}.");
                return;
            }

            if (indexCount == 0)
            {
                Debug.LogError("Attempting to add an entry without indices.");
                return;
            }

            Append(parentEntry, entry);
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        static void Append(Entry parentEntry, Entry entry)
        {
            if (parentEntry.lastChild == null)
            {
                Debug.Assert(parentEntry.firstChild == null);
                parentEntry.firstChild = entry;
                parentEntry.lastChild = entry;
            }
            else
            {
                parentEntry.lastChild.nextSibling = entry;
                parentEntry.lastChild = entry;
            }
        }
    }
}
