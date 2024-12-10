// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;

namespace UnityEngine.UIElements.UIR
{
    class EntryProcessor
    {
        struct MaskMesh
        {
            public NativeSlice<Vertex> vertices;
            public NativeSlice<ushort> indices;
            public int indexOffset; // From the beginning of the "real" index buffer to the first index actually being read
        }

        EntryPreProcessor m_PreProcessor = new EntryPreProcessor();

        RenderChain m_RenderChain;
        VisualElement m_CurrentElement;

        int m_MaskDepth; // The currently used depth
        int m_MaskDepthPopped;
        int m_MaskDepthPushed;

        int m_StencilRef; // The currently used stencil ref
        int m_StencilRefPopped;
        int m_StencilRefPushed;

        BMPAlloc m_ClipRectId; // The currently used clip rect
        BMPAlloc m_ClipRectIdPopped;
        BMPAlloc m_ClipRectIdPushed;

        bool m_IsDrawingMask;
        Stack<MaskMesh> m_MaskMeshes = new Stack<MaskMesh>(1);

        // Vertex data, lazily computed
        bool m_VertexDataComputed;
        Matrix4x4 m_Transform;
        Color32 m_TransformData;
        Color32 m_OpacityData;
        Color32 m_TextCoreSettingsPage;

        // Invariant within an alloc
        MeshHandle m_Mesh;              // The current destination mesh
        NativeSlice<Vertex> m_Verts;    // The current destination vertex slice
        NativeSlice<UInt16> m_Indices;  // The current destination index slice
        ushort m_IndexOffset;           // The index offset provided by the render device for the current sub-alloc
        int m_AllocVertexCount;         // Number of vertices in the current alloc
        int m_AllocIndex;               // Index of the current alloc in the list of allocs

        // Increases as we fill the alloc
        int m_VertsFilled;
        int m_IndicesFilled;

        // Per entry
        VertexFlags m_RenderType;
        bool m_RemapUVs;
        Rect m_AtlasRect;
        int m_GradientSettingIndexOffset;
        bool m_IsTail;

        // First command is always a dummy
        RenderChainCommand m_FirstCommand;
        RenderChainCommand m_LastCommand;

        public RenderChainCommand firstHeadCommand { get; private set; }
        public RenderChainCommand lastHeadCommand { get; private set; }
        public RenderChainCommand firstTailCommand { get; private set; }
        public RenderChainCommand lastTailCommand { get; private set; }

        public void Init(Entry root, RenderChain renderChain, VisualElement ve)
        {
            UIRenderDevice device = renderChain.device;
            m_RenderChain = renderChain;
            m_CurrentElement = ve;

            m_PreProcessor.PreProcess(root);

            // Free the allocated meshes, if necessary
            if (m_PreProcessor.headAllocs.Count == 0 && ve.renderChainData.headMesh != null)
            {
                device.Free(ve.renderChainData.headMesh);
                ve.renderChainData.headMesh = null;
            }

            if (m_PreProcessor.tailAllocs.Count == 0 && ve.renderChainData.tailMesh != null)
            {
                device.Free(ve.renderChainData.tailMesh);
                ve.renderChainData.tailMesh = null;
            }

            if (ve.renderChainData.hasExtraMeshes)
                renderChain.FreeExtraMeshes(ve);

            renderChain.ResetTextures(ve);

            var parent = m_CurrentElement.hierarchy.parent;
            bool isGroupTransform = m_CurrentElement.renderChainData.isGroupTransform;

            if (parent != null)
            {
                m_MaskDepthPopped = parent.renderChainData.childrenMaskDepth;
                m_StencilRefPopped = parent.renderChainData.childrenStencilRef;
                m_ClipRectIdPopped = isGroupTransform ? UIRVEShaderInfoAllocator.infiniteClipRect : parent.renderChainData.clipRectID;
            }
            else
            {
                m_MaskDepthPopped = 0;
                m_StencilRefPopped = 0;
                m_ClipRectIdPopped = UIRVEShaderInfoAllocator.infiniteClipRect;
            }

            m_MaskDepthPushed = m_MaskDepthPopped + 1;
            m_StencilRefPushed = m_MaskDepthPopped;
            m_ClipRectIdPushed = m_CurrentElement.renderChainData.clipRectID;

            m_MaskDepth = m_MaskDepthPopped;
            m_StencilRef = m_StencilRefPopped;
            m_ClipRectId = m_ClipRectIdPopped;

            // Vertex data, lazily computed
            m_VertexDataComputed = false;
            m_Transform = Matrix4x4.identity;
            m_TextCoreSettingsPage = new Color32(0, 0, 0, 0);

            m_MaskMeshes.Clear();
            m_IsDrawingMask = false;
        }

        // Clear important references to prevent memory retention
        public void ClearReferences()
        {
            m_PreProcessor.ClearReferences();

            m_RenderChain = null;
            m_CurrentElement = null;
            m_Mesh = null;

            m_FirstCommand = null;
            m_LastCommand = null;
            firstHeadCommand = null;
            lastHeadCommand = null;
            firstTailCommand = null;
            lastTailCommand = null;
        }

        public void ProcessHead()
        {
            m_IsTail = false;

            ProcessFirstAlloc(m_PreProcessor.headAllocs, ref m_CurrentElement.renderChainData.headMesh);

            m_FirstCommand = null;
            m_LastCommand = null;

            ProcessRange(0, m_PreProcessor.childrenIndex - 1);

            firstHeadCommand = m_FirstCommand;
            lastHeadCommand = m_LastCommand;
        }

        public void ProcessTail()
        {
            m_IsTail = true;

            ProcessFirstAlloc(m_PreProcessor.tailAllocs, ref m_CurrentElement.renderChainData.tailMesh);

            m_FirstCommand = null;
            m_LastCommand = null;

            ProcessRange(m_PreProcessor.childrenIndex + 1, m_PreProcessor.flattenedEntries.Count - 1);

            firstTailCommand = m_FirstCommand;
            lastTailCommand = m_LastCommand;

            Debug.Assert(m_MaskDepth == m_MaskDepthPopped);
            Debug.Assert(m_MaskMeshes.Count == 0);
            Debug.Assert(!m_IsDrawingMask);
        }

        void ProcessRange(int first, int last)
        {
            List<Entry> entries = m_PreProcessor.flattenedEntries;
            for (int i = first; i <= last; ++i)
            {
                var entry = entries[i];
                switch (entry.type)
                {
                    case EntryType.DrawSolidMesh:
                    {
                        m_RenderType = VertexFlags.IsSolid;
                        ProcessMeshEntry(entry, TextureId.invalid);
                        break;
                    }
                    case EntryType.DrawTexturedMesh:
                    {
                        Texture texture = entry.texture;
                        TextureId textureId = TextureId.invalid;
                        if (texture != null)
                        {
                            // Attempt to override with an atlas
                            if (m_RenderChain.atlas != null && m_RenderChain.atlas.TryGetAtlas(m_CurrentElement, texture as Texture2D, out textureId, out RectInt atlasRect))
                            {
                                m_RenderType = VertexFlags.IsDynamic;
                                m_AtlasRect = new Rect(atlasRect.x, atlasRect.y, atlasRect.width, atlasRect.height);
                                m_RemapUVs = true;
                                m_RenderChain.InsertTexture(m_CurrentElement, texture, textureId, true);
                            }
                            else
                            {
                                m_RenderType = VertexFlags.IsTextured;
                                textureId = TextureRegistry.instance.Acquire(texture);
                                m_RenderChain.InsertTexture(m_CurrentElement, texture, textureId, false);
                            }
                        }
                        else
                            m_RenderType = VertexFlags.IsSolid; // Fallback to solid rendering

                        ProcessMeshEntry(entry, textureId);
                        m_RemapUVs = false;
                        break;
                    }
                    case EntryType.DrawTexturedMeshSkipAtlas:
                    {
                        m_RenderType = VertexFlags.IsTextured;
                        TextureId textureId = TextureRegistry.instance.Acquire(entry.texture);
                        m_RenderChain.InsertTexture(m_CurrentElement, entry.texture, textureId, false);
                        ProcessMeshEntry(entry, textureId);
                        break;
                    }
                    case EntryType.DrawTextMesh:
                    {
                        m_RenderType = VertexFlags.IsText;
                        TextureId textureId = TextureRegistry.instance.Acquire(entry.texture);
                        m_RenderChain.InsertTexture(m_CurrentElement, entry.texture, textureId, false);
                        ProcessMeshEntry(entry, textureId);
                        break;
                    }
                    case EntryType.DrawGradients:
                    {
                        m_RenderType = VertexFlags.IsSvgGradients;
                        TextureId textureId;

                        // The vector image has embedded textures/gradients and we have a manager that can accept the settings.
                        // Register the settings and assume that it works.
                        var gradientRemap = m_RenderChain.vectorImageManager.AddUser(entry.gradientsOwner, m_CurrentElement);
                        m_GradientSettingIndexOffset = gradientRemap.destIndex;
                        if (gradientRemap.atlas != TextureId.invalid)

                            // The textures/gradients themselves have also been atlased
                            textureId = gradientRemap.atlas;
                        else
                        {
                            // Only the settings were atlased
                            textureId = TextureRegistry.instance.Acquire(entry.gradientsOwner.atlas);
                            m_RenderChain.InsertTexture(m_CurrentElement, entry.gradientsOwner.atlas, textureId, false);
                        }

                        ProcessMeshEntry(entry, textureId);
                        m_GradientSettingIndexOffset = -1; // This effectively disables this conversion operation for the next entries
                        break;
                    }
                    case EntryType.DrawImmediate:
                    {
                        var cmd = m_RenderChain.AllocCommand();
                        cmd.type = CommandType.Immediate;
                        cmd.owner = m_CurrentElement;
                        cmd.isTail = m_IsTail;
                        cmd.callback = entry.immediateCallback;
                        AppendCommand(cmd);
                        break;
                    }
                    case EntryType.DrawImmediateCull:
                    {
                        var cmd = m_RenderChain.AllocCommand();
                        cmd.type = CommandType.ImmediateCull;
                        cmd.owner = m_CurrentElement;
                        cmd.isTail = m_IsTail;
                        cmd.callback = entry.immediateCallback;
                        AppendCommand(cmd);
                        break;
                    }
                    case EntryType.DrawChildren: // We should only be processing entries BEFORE or AFTER this one
                    case EntryType.DedicatedPlaceholder: // These should have been filtered out by pre-processing
                        Debug.Assert(false);
                        break;
                    case EntryType.BeginStencilMask:
                    {
                        Debug.Assert(m_MaskDepth == m_MaskDepthPopped); // For now, we only support 1 masking level per element
                        Debug.Assert(!m_IsDrawingMask); // We can't begin a mask while we're not fully done pushing the previous
                        m_IsDrawingMask = true;

                        // If we're already at a masking depth of ref+1, this should increment ref
                        m_StencilRef = m_StencilRefPushed;

                        // We can only push when mask depth is at ref
                        Debug.Assert(m_MaskDepth == m_StencilRef);
                        break;
                    }
                    case EntryType.EndStencilMask:
                    {
                        Debug.Assert(m_IsDrawingMask);
                        m_IsDrawingMask = false;
                        m_MaskDepth = m_MaskDepthPushed;
                        break;
                    }
                    case EntryType.PopStencilMask:
                    {
                        // We can only pop when mask depth is at ref+1
                        Debug.Assert(m_MaskDepth == m_StencilRef + 1);
                        DrawReverseMask();
                        m_MaskDepth = m_MaskDepthPopped;
                        m_StencilRef = m_StencilRefPopped;
                        break;
                    }
                    case EntryType.PushClippingRect:
                        m_ClipRectId = m_ClipRectIdPushed;
                        break;
                    case EntryType.PopClippingRect:
                        m_ClipRectId = m_ClipRectIdPopped;
                        break;
                    case EntryType.PushScissors:
                    {
                        var cmd = m_RenderChain.AllocCommand();
                        cmd.type = CommandType.PushScissor;
                        cmd.owner = m_CurrentElement;
                        cmd.isTail = m_IsTail;
                        AppendCommand(cmd);
                        break;
                    }
                    case EntryType.PopScissors:
                    {
                        var cmd = m_RenderChain.AllocCommand();
                        cmd.type = CommandType.PopScissor;
                        cmd.owner = m_CurrentElement;
                        cmd.isTail = m_IsTail;
                        AppendCommand(cmd);
                        break;
                    }
                    case EntryType.PushGroupMatrix:
                    {
                        var cmd = m_RenderChain.AllocCommand();
                        cmd.type = CommandType.PushView;
                        cmd.owner = m_CurrentElement;
                        cmd.isTail = m_IsTail;
                        AppendCommand(cmd);
                        break;
                    }
                    case EntryType.PopGroupMatrix:
                    {
                        var cmd = m_RenderChain.AllocCommand();
                        cmd.type = CommandType.PopView;
                        cmd.owner = m_CurrentElement;
                        cmd.isTail = m_IsTail;
                        AppendCommand(cmd);
                        break;
                    }
                    case EntryType.PushRenderTexture:
                    {
                        Debug.Assert(m_MaskDepth == 0, "The RenderTargetMode feature must not be used within a stencil mask.");
                        var cmd = m_RenderChain.AllocCommand();
                        cmd.type = CommandType.PushRenderTexture;
                        cmd.owner = m_CurrentElement;
                        cmd.isTail = m_IsTail;
                        AppendCommand(cmd);
                        break;
                    }
                    case EntryType.BlitAndPopRenderTexture:
                    {
                        {
                            var cmd = m_RenderChain.AllocCommand();
                            cmd.type = CommandType.BlitToPreviousRT;
                            cmd.owner = m_CurrentElement;
                            cmd.isTail = m_IsTail;
                            cmd.state.material = GetBlitMaterial(m_CurrentElement.subRenderTargetMode);
                            Debug.Assert(cmd.state.material != null);
                            AppendCommand(cmd);
                        }
                        {
                            var cmd = m_RenderChain.AllocCommand();
                            cmd.type = CommandType.PopRenderTexture;
                            cmd.owner = m_CurrentElement;
                            cmd.isTail = m_IsTail;
                            AppendCommand(cmd);
                        }
                        break;
                    }
                    case EntryType.PushDefaultMaterial:
                    {
                        var cmd = m_RenderChain.AllocCommand();
                        cmd.type = CommandType.PushDefaultMaterial;
                        cmd.owner = m_CurrentElement;
                        cmd.isTail = m_IsTail;
                        cmd.state.material = entry.material;
                        AppendCommand(cmd);
                        break;
                    }
                    case EntryType.PopDefaultMaterial:
                    {
                        var cmd = m_RenderChain.AllocCommand();
                        cmd.type = CommandType.PopDefaultMaterial;
                        cmd.owner = m_CurrentElement;
                        cmd.isTail = m_IsTail;
                        AppendCommand(cmd);
                        break;
                    }
                    case EntryType.CutRenderChain:
                    {
                        var cmd = m_RenderChain.AllocCommand();
                        cmd.type = CommandType.CutRenderChain;
                        cmd.owner = m_CurrentElement;
                        cmd.isTail= m_IsTail;
                        AppendCommand(cmd);
                        break;
                    }
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        unsafe void ProcessMeshEntry(Entry entry, TextureId textureId)
        {
            int entryVertexCount = entry.vertices.Length;
            int entryIndexCount = entry.indices.Length;

            Debug.Assert(entryVertexCount > 0 == entryIndexCount > 0);
            if (entryVertexCount > 0 && entryIndexCount > 0)
            {
                if (m_VertsFilled + entryVertexCount > m_AllocVertexCount)
                {
                    ProcessNextAlloc();
                    Debug.Assert(m_VertsFilled + entryVertexCount <= m_AllocVertexCount);
                }

                if (!m_VertexDataComputed)
                {
                    UIRUtility.GetVerticesTransformInfo(m_CurrentElement, out m_Transform);
                    m_CurrentElement.renderChainData.verticesSpace = m_Transform; // This is the space for the generated vertices below
                    m_TransformData = m_RenderChain.shaderInfoAllocator.TransformAllocToVertexData(m_CurrentElement.renderChainData.transformID);
                    m_OpacityData = m_RenderChain.shaderInfoAllocator.OpacityAllocToVertexData(m_CurrentElement.renderChainData.opacityID);
                    m_VertexDataComputed = true;
                }

                Color32 opacityPage = new Color32(m_OpacityData.r, m_OpacityData.g, 0, 0);
                Color32 clipRectData = m_RenderChain.shaderInfoAllocator.ClipRectAllocToVertexData(m_ClipRectId);
                Color32 ids = new Color32(m_TransformData.b, clipRectData.b, m_OpacityData.b, 0);
                Color32 xformClipPages = new Color32(m_TransformData.r, m_TransformData.g, clipRectData.r, clipRectData.g);
                Color32 addFlags = new Color32((byte)m_RenderType, 0, 0, 0);

                if ((entry.flags & EntryFlags.UsesTextCoreSettings) != 0)
                {
                    // It's important to avoid writing these values when the vertices aren't for text,
                    // as some of these settings are shared with the vector graphics gradients.
                    // The same applies to the CopyTransformVertsPos* methods below.
                    Color32 textCoreSettingsData = m_RenderChain.shaderInfoAllocator.TextCoreSettingsToVertexData(m_CurrentElement.renderChainData.textCoreSettingsID);
                    m_TextCoreSettingsPage.r = textCoreSettingsData.r;
                    m_TextCoreSettingsPage.g = textCoreSettingsData.g;
                    ids.a = textCoreSettingsData.b;
                }

                // Copy vertices, transforming them as necessary
                var targetVerticesSlice = m_Verts.Slice(m_VertsFilled, entryVertexCount);

                int entryIndexOffset = m_VertsFilled + m_IndexOffset;
                var targetIndicesSlice = m_Indices.Slice(m_IndicesFilled, entryIndexCount);
                bool shapeWindingIsClockwise = UIRUtility.ShapeWindingIsClockwise(m_MaskDepth, m_StencilRef);
                bool transformFlipsWinding = m_CurrentElement.renderChainData.worldFlipsWinding;

                var job = new ConvertMeshJobData
                {
                    vertSrc = (IntPtr)entry.vertices.GetUnsafePtr(),
                    vertDst = (IntPtr)targetVerticesSlice.GetUnsafePtr(),
                    vertCount = entryVertexCount,
                    transform = m_Transform,
                    xformClipPages = xformClipPages,
                    ids = ids,
                    addFlags = addFlags,
                    opacityPage = opacityPage,
                    textCoreSettingsPage = m_TextCoreSettingsPage,
                    usesTextCoreSettings = (entry.flags & EntryFlags.UsesTextCoreSettings) != 0 ? 1 : 0,
                    textureId = textureId.ConvertToGpu(),
                    gradientSettingsIndexOffset = m_GradientSettingIndexOffset,

                    indexSrc = (IntPtr)entry.indices.GetUnsafePtr(),
                    indexDst = (IntPtr)targetIndicesSlice.GetUnsafePtr(),
                    indexCount = targetIndicesSlice.Length,
                    indexOffset = entryIndexOffset,

                    flipIndices = shapeWindingIsClockwise == transformFlipsWinding ? 1 : 0,
                    forceZ = m_RenderChain.isFlat ? 1 : 0,
                    positionZ = m_IsDrawingMask ? UIRUtility.k_MaskPosZ : UIRUtility.k_MeshPosZ,

                    remapUVs = m_RemapUVs ? 1 : 0,
                    atlasRect = m_AtlasRect,
                };
                m_RenderChain.jobManager.Add(ref job);

                if (m_IsDrawingMask)
                {
                    m_MaskMeshes.Push(new MaskMesh
                    {
                        vertices = targetVerticesSlice,
                        indices = targetIndicesSlice,
                        indexOffset = entryIndexOffset
                    });
                }

                var cmd = CreateMeshDrawCommand(m_Mesh, entryIndexCount, m_IndicesFilled, entry.material, textureId);
                AppendCommand(cmd);

                if (entry.type == EntryType.DrawTextMesh)
                {
                    // Set font atlas texture gradient scale
                    cmd.state.sdfScale = entry.textScale;
                    cmd.state.sharpness = entry.fontSharpness;
                }

                m_VertsFilled += entryVertexCount;
                m_IndicesFilled += entryIndexCount;
            }
        }

        void DrawReverseMask()
        {
            while (m_MaskMeshes.TryPop(out MaskMesh mesh))
            {
                Debug.Assert(mesh.indices.Length > 0 == mesh.vertices.Length > 0);
                if (mesh.indices.Length > 0 && mesh.vertices.Length > 0)
                {
                    // At this point, the destination mesh has already been allocated but the data isn't
                    // copied yet. It's not a problem, we can create the command nonetheless.
                    var cmd = CreateMeshDrawCommand(m_Mesh, mesh.indices.Length, m_IndicesFilled, null, TextureId.invalid);
                    AppendCommand(cmd);

                    // Now we need to copy the data
                    unsafe
                    {
                        NativeSlice<Vertex> dstVertices = m_Verts.Slice(m_VertsFilled, mesh.vertices.Length);
                        NativeSlice<ushort> dstIndices = m_Indices.Slice(m_IndicesFilled, mesh.indices.Length);

                        var job = new CopyMeshJobData
                        {
                            vertSrc = (IntPtr)mesh.vertices.GetUnsafePtr(),
                            vertDst = (IntPtr)dstVertices.GetUnsafePtr(),
                            vertCount = mesh.vertices.Length,
                            indexSrc = (IntPtr)mesh.indices.GetUnsafePtr(),
                            indexDst = (IntPtr)dstIndices.GetUnsafePtr(),
                            indexCount = mesh.indices.Length,
                            indexOffset = m_IndexOffset + m_VertsFilled - mesh.indexOffset
                        };
                        m_RenderChain.jobManager.Add(ref job);
                    }

                    m_IndicesFilled += mesh.indices.Length;
                    m_VertsFilled += mesh.vertices.Length;
                }
            }
        }

        RenderChainCommand CreateMeshDrawCommand(MeshHandle mesh, int indexCount, int indexOffset, Material material, TextureId texture)
        {
            var cmd = m_RenderChain.AllocCommand();
            cmd.type = CommandType.Draw;
            cmd.state = new State { material = material, texture = texture, stencilRef = m_StencilRef };
            cmd.mesh = mesh;
            cmd.indexOffset = indexOffset;
            cmd.indexCount = indexCount;
            cmd.owner = m_CurrentElement;
            cmd.isTail = m_IsTail;
            return cmd;
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        void AppendCommand(RenderChainCommand next)
        {
            if (m_FirstCommand == null)
            {
                m_FirstCommand = next;
                m_LastCommand = next;
            }
            else
            {
                next.prev = m_LastCommand;
                m_LastCommand.next = next;
                m_LastCommand = next;
            }
        }

        void ProcessFirstAlloc(List<EntryPreProcessor.AllocSize> allocList, ref MeshHandle mesh)
        {
            if (allocList.Count > 0)
            {
                EntryPreProcessor.AllocSize allocSize = allocList[0];
                UpdateOrAllocate(ref mesh, allocSize.vertexCount, allocSize.indexCount, m_RenderChain.device, out m_Verts, out m_Indices, out m_IndexOffset, ref m_RenderChain.statsByRef);
                m_AllocVertexCount = (int)mesh.allocVerts.size;
            }
            else
            {
                Debug.Assert(mesh == null); // It should have been cleared during the init
                m_Verts = new NativeSlice<Vertex>();
                m_Indices = new NativeSlice<ushort>();
                m_IndexOffset = 0;
                m_AllocVertexCount = 0;
            }

            m_Mesh = mesh;
            m_VertsFilled = 0;
            m_IndicesFilled = 0;
            m_AllocIndex = 0;
        }

        // This is only called for extra allocs, after the first alloc has been filled. Extra allocs are very infrequent,
        // so we don't need to optimize this code path as much.
        void ProcessNextAlloc()
        {
            List<EntryPreProcessor.AllocSize> allocList = m_IsTail ? m_PreProcessor.tailAllocs : m_PreProcessor.headAllocs;
            Debug.Assert(m_AllocIndex < allocList.Count - 1);

            EntryPreProcessor.AllocSize allocSize = allocList[++m_AllocIndex];
            m_Mesh = null; // Extra allocations have been previously freed, so we don't have any mesh to update
            UpdateOrAllocate(ref m_Mesh, allocSize.vertexCount, allocSize.indexCount, m_RenderChain.device, out m_Verts, out m_Indices, out m_IndexOffset, ref m_RenderChain.statsByRef);
            m_AllocVertexCount = (int)m_Mesh.allocVerts.size;

            m_RenderChain.InsertExtraMesh(m_CurrentElement, m_Mesh);

            m_VertsFilled = 0;
            m_IndicesFilled = 0;
        }

        static void UpdateOrAllocate(ref MeshHandle data, int vertexCount, int indexCount, UIRenderDevice device, out NativeSlice<Vertex> verts, out NativeSlice<UInt16> indices, out UInt16 indexOffset, ref ChainBuilderStats stats)
        {
            if (data != null)
            {
                // Try to fit within the existing allocation, optionally we can change the condition
                // to be an exact match of size to guarantee continuity in draw ranges
                if (data.allocVerts.size >= vertexCount && data.allocIndices.size >= indexCount)
                {
                    device.Update(data, (uint)vertexCount, (uint)indexCount, out verts, out indices, out indexOffset);
                    stats.updatedMeshAllocations++;
                }
                else
                {
                    // Won't fit in the existing allocated region, free the current one
                    device.Free(data);
                    data = device.Allocate((uint)vertexCount, (uint)indexCount, out verts, out indices, out indexOffset);
                    stats.newMeshAllocations++;
                }
            }
            else
            {
                data = device.Allocate((uint)vertexCount, (uint)indexCount, out verts, out indices, out indexOffset);
                stats.newMeshAllocations++;
            }
        }

        static Material s_blitMaterial_LinearToGamma;
        static Material s_blitMaterial_GammaToLinear;
        static Material s_blitMaterial_NoChange;
        static Shader s_blitShader;

        static Material CreateBlitShader(float colorConversion)
        {
            if (s_blitShader == null)
                s_blitShader = Shader.Find(Shaders.k_ColorConversionBlit);

            Debug.Assert(s_blitShader != null, "UI Tollkit Render Event: Shader Not found");
            var blitMaterial = new Material(s_blitShader);
            blitMaterial.hideFlags |= HideFlags.DontSaveInEditor;
            blitMaterial.SetFloat("_ColorConversion", colorConversion);
            return blitMaterial;
        }

        static Material GetBlitMaterial(VisualElement.RenderTargetMode mode)
        {
            switch (mode)
            {
                case VisualElement.RenderTargetMode.GammaToLinear:
                    if (s_blitMaterial_GammaToLinear == null)
                        s_blitMaterial_GammaToLinear = CreateBlitShader(-1);
                    return s_blitMaterial_GammaToLinear;

                case VisualElement.RenderTargetMode.LinearToGamma:
                    if (s_blitMaterial_LinearToGamma == null)
                        s_blitMaterial_LinearToGamma = CreateBlitShader(1);
                    return s_blitMaterial_LinearToGamma;

                case VisualElement.RenderTargetMode.NoColorConversion:
                    if (s_blitMaterial_NoChange == null)
                        s_blitMaterial_NoChange = CreateBlitShader(0);
                    return s_blitMaterial_NoChange;

                default:
                    Debug.LogError($"No Shader for Unsupported RenderTargetMode: {mode}");
                    return null;
            }
        }
    }
}
