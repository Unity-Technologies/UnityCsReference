// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Profiling;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.UIElements.UIR
{
    [StructLayout(LayoutKind.Sequential)]
    struct Transform3x4
    {
        public Vector4 v0, v1, v2;
    }

    // The values stored here could be updated behind the back of the holder of this
    // object. Hence, never turn this into a struct or else we can't do automatic
    // defragmentation and address ordering optimizations
    internal class MeshHandle : LinkedPoolItem<MeshHandle>
    {
        internal Alloc allocVerts, allocIndices;
        internal Page allocPage;
        internal uint allocTime; // Frame this mesh was allocated/updated
        internal uint updateAllocID; // If not 0, the alloc here points to a temporary location managed by an update record with the said ID
    }

    class UIRenderDevice : IDisposable
    {
        public static class Testing
        {
            public static CommandListManager GetCommandListManager(UIRenderDevice device) => device.m_CommandListManager;
            public static MeshManager GetMeshManager(UIRenderDevice device) => device.m_MeshManager;
        }

        struct DeviceToFree
        {
            public UInt32 handle;
            public CommandListManager commandListManager;
            public MeshManager meshManager;

            public void Dispose()
            {
                meshManager.Dispose();
                commandListManager.Dispose();
            }
        }

        internal const uint k_MaxQueuedFrameCount = 4; // Support drivers queuing up to 4 frames
        internal const int k_PruneEmptyPageFrameCount = 60; // Empty pages will be pruned if they are empty for x consecutive frames.
        internal static uint maxVerticesPerPage => 0xFFFF; // On DX11, 0xFFFF is an invalid index (associated to primitive restart). With size = 0xFFFF last index is 0xFFFE    cases:1259449

        IntPtr m_DefaultStencilState;
        IntPtr m_VertexDecl;

        List<MeshHandle> m_MeshesPendingFree;
        CommandListManager m_CommandListManager;
        UInt32[] m_Fences;
        MaterialPropertyBlock m_ConstantProps; // Properties that are constant throughout the evaluation of the commands
        MaterialPropertyBlock m_BatchProps;
        uint m_FrameIndex;
        MeshManager m_MeshManager;
        DrawStatistics m_DrawStats;
        bool m_RenderingInProgress;

        readonly DrawParams m_DrawParams = new DrawParams();
        readonly TextureSlotManager m_TextureSlotManager = new TextureSlotManager();
        HashSet<Material> m_ScreenSpaceAlteredMaterials = new();
        static LinkedList<DeviceToFree> m_DeviceFreeQueue = new LinkedList<DeviceToFree>();   // Not thread safe for now
        static int m_ActiveDeviceCount = 0; // Not thread safe for now
        static bool m_SubscribedToNotifications; // Not thread safe for now
        static bool m_SynchronousFree; // This is set on domain unload or app quit, so it is irreversible

        static readonly int s_GradientSettingsTexID = Shader.PropertyToID("_GradientSettingsTex");
        static readonly int s_ShaderInfoTexID = Shader.PropertyToID("_ShaderInfoTex");

        static ProfilerMarker s_MarkerFree = new ProfilerMarker(ProfilerCategory.UIToolkit, "UIR.Free");
        static ProfilerMarker s_MarkerAdvanceFrame = new ProfilerMarker(ProfilerCategory.UIToolkit, "UIR.AdvanceFrame");
        static ProfilerMarker s_MarkerFence = new ProfilerMarker(ProfilerCategory.UIToolkit, "UIR.WaitOnFence");
        static ProfilerMarker s_MarkerBeforeDraw = new ProfilerMarker(ProfilerCategory.UIToolkit, "UIR.BeforeDraw");

        internal bool breakBatches { get; set; }
        internal bool isFlat { get; }
        internal bool forceGammaRendering { get; }

        public GpuUpdateMode gpuUpdateMode { get; }

        // TODO: It is now an insufficient condition to determine if we use command lists or not
        // (nested render trees do not use command lists)
        internal uint frameIndex => m_FrameIndex;

        static UIRenderDevice()
        {
            UIR.Utility.EngineUpdate += OnEngineUpdateGlobal;
            UIR.Utility.FlushPendingResources += OnFlushPendingResources;
        }

        public UIRenderDevice(uint initialVertexCapacity = 0, uint initialIndexCapacity = 0, bool isFlat = true, bool forceGammaRendering = false, GpuUpdateMode gpuUpdateMode = GpuUpdateMode.Default)
        {
            Debug.Assert(!m_SynchronousFree); // Shouldn't create render devices when the app is quitting or domain-unloading
            Debug.Assert(k_PruneEmptyPageFrameCount > k_MaxQueuedFrameCount); // To prevent pending updates from attempting to access a pruned page.
            if (m_ActiveDeviceCount++ == 0)
            {
                if (!m_SubscribedToNotifications)
                {
                    Utility.NotifyOfUIREvents(true);
                    m_SubscribedToNotifications = true;
                }
            }

            this.isFlat = isFlat;
            this.forceGammaRendering = forceGammaRendering;

            if (!Utility.HasMappedBufferRange() || gpuUpdateMode == GpuUpdateMode.StagingBuffer)
            {
                this.gpuUpdateMode = GpuUpdateMode.StagingBuffer;
                m_MeshManager = new MeshManagerStaged(initialVertexCapacity, initialIndexCapacity);
            }
            else
            {
                this.gpuUpdateMode = GpuUpdateMode.MappedSubUpdates;
                m_MeshManager = new MeshManagerMapped(initialVertexCapacity, initialIndexCapacity);
            }

            m_MeshesPendingFree = new();

            InitVertexDeclaration();

            m_Fences = new uint[(int)k_MaxQueuedFrameCount];
            m_ConstantProps = new MaterialPropertyBlock();
            m_BatchProps = new MaterialPropertyBlock();
            m_DefaultStencilState = Utility.CreateStencilState(new StencilState
            {
                enabled = isFlat,
                readMask = 255,
                writeMask = 255,

                compareFunctionFront = CompareFunction.Equal,
                passOperationFront = StencilOp.Keep,
                failOperationFront = StencilOp.Keep,
                zFailOperationFront = StencilOp.IncrementSaturate, // Push

                compareFunctionBack = CompareFunction.Less,
                passOperationBack = StencilOp.Keep,
                failOperationBack = StencilOp.Keep,
                zFailOperationBack = StencilOp.DecrementSaturate, // Pop
            });

            m_CommandListManager = new(m_VertexDecl, m_DefaultStencilState);
        }

        void InitVertexDeclaration()
        {
            var vertexDecl = new VertexAttributeDescriptor[]
            {
                // Vertex position first
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),

                // The UINT32 color
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4),

                // The UV and LayoutUV
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 4),

                // TransformID page coordinate (XY), ClipRectID page coordinate (ZW), packed into a Color32
                new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.UNorm8, 4),

                // In-page index for (TransformID, ClipRectID, OpacityID, TextCoreID)
                new VertexAttributeDescriptor(VertexAttribute.TexCoord2, VertexAttributeFormat.UNorm8, 4),

                // Flags (vertex type), all packed into a Color32
                new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.UNorm8, 4),

                // OpacityID page coordinate (XY), Color-page/TextCore-setting (16-bit encoded in ZW), packed into a Color32
                new VertexAttributeDescriptor(VertexAttribute.TexCoord4, VertexAttributeFormat.UNorm8, 4),

                // SVG (16-bit encoded in XY), packed into a Color32
                new VertexAttributeDescriptor(VertexAttribute.TexCoord5, VertexAttributeFormat.UNorm8, 4),

                // Circle arcs
                new VertexAttributeDescriptor(VertexAttribute.TexCoord6, VertexAttributeFormat.Float32, 4),

                // TextureID, to represent integers from 0 to 2048
                // Float32 is overkill for the time being but it avoids conversion issues on GLES2 and metal. We should
                // use a Float16 instead but this isn't a trivial because C# doesn't have a native "half" datatype.
                new VertexAttributeDescriptor(VertexAttribute.TexCoord7, VertexAttributeFormat.Float32, 1)
            };
            m_VertexDecl = Utility.GetVertexDeclaration(vertexDecl);
        }

        #region Dispose Pattern

        protected bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // For tests tear down
        internal void DisposeImmediate()
        {
            Debug.Assert(!m_SynchronousFree);
            m_SynchronousFree = true;
            Dispose();
            m_SynchronousFree = false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            m_ActiveDeviceCount--;

            if (disposing)
            {
                m_CommandListManager.ResetUIRendererDrawCallData();

                DeviceToFree free = new DeviceToFree
                {
                    handle = Utility.InsertCPUFence(),
                    meshManager = m_MeshManager,
                    commandListManager = m_CommandListManager
                };

                m_MeshManager = null;
                m_CommandListManager = null;

                if (free.handle == 0)
                    free.Dispose();
                else
                {
                    m_DeviceFreeQueue.AddLast(free);
                    if (m_SynchronousFree)
                        ProcessDeviceFreeQueue();
                }
            }
            else
                UnityEngine.UIElements.DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern

        public MeshHandle Allocate(uint vertexCount, uint indexCount, out NativeSlice<Vertex> vertexData, out NativeSlice<UInt16> indexData, out UInt16 indexOffset)
        {
            Debug.Assert(!m_RenderingInProgress);
            return m_MeshManager.Allocate(vertexCount, indexCount, out vertexData, out indexData, out indexOffset);
        }

        public void Update(MeshHandle mesh, uint vertexCount, out NativeSlice<Vertex> vertexData)
        {
            Debug.Assert(!m_RenderingInProgress);
            m_MeshManager.Update(mesh, vertexCount, out vertexData);
        }

        public void Update(MeshHandle mesh, uint vertexCount, uint indexCount, out NativeSlice<Vertex> vertexData, out NativeSlice<UInt16> indexData, out UInt16 indexOffset)
        {
            Debug.Assert(!m_RenderingInProgress);
            m_MeshManager.Update(mesh, vertexCount, indexCount, out vertexData, out indexData, out indexOffset);
        }

        public void Free(MeshHandle mesh)
        {
            if (m_RenderingInProgress)
            {
                // Defer mesh free until frame rendering completes
                m_MeshesPendingFree.Add(mesh);
                return;
            }

            m_MeshManager.Free(mesh);
        }

        public void OnFrameRenderingBegin()
        {
            m_RenderingInProgress = true;

            m_DrawStats = new DrawStatistics();
            m_DrawStats.currentFrameIndex = (int)m_FrameIndex;

            s_MarkerBeforeDraw.Begin();

            m_MeshManager.OnFrameRenderingBegin();

            s_MarkerBeforeDraw.End();

            // UUM-101410: We must update the fence now in case that multiple calls to Update() are performed without
            // Render() being called. Otherwise, the previous fence (which has already passed) won't be updated and we
            // might be modifying the update ranges buffer, or the vertex/index buffers while they're already being
            // copied by the render thread.
            UpdateFenceValue();
        }

        internal unsafe static NativeSlice<T> PtrToSlice<T>(void* p, int count) where T : struct
        {
            var slice = NativeSliceUnsafeUtility.ConvertExistingDataToNativeSlice<T>(p, UnsafeUtility.SizeOf<T>(), count);
            NativeSliceUnsafeUtility.SetAtomicSafetyHandle(ref slice, AtomicSafetyHandle.GetTempUnsafePtrSliceHandle());
            return slice;
        }

        [Flags]
        enum EvaluationFlags
        {
            None = 0,
            MustApplyMaterial = 1 << 0,
            MustApplyBatchProps = 1 << 1, // Indicates that the "stateMatProps" must be applied.
            MustApplyStencil = 1 << 2,

            // 3 bits
            ForceRenderTypeBitOffset = 3,
            ForceRenderTypeSolid = 1 << ForceRenderTypeBitOffset,
            ForceRenderTypeTextured = 2 << ForceRenderTypeBitOffset,
            ForceRenderTypeText = 3 << ForceRenderTypeBitOffset,
            ForceRenderTypeSvgGradient = 4 << ForceRenderTypeBitOffset,
            ForceRenderTypeBits = 7 << ForceRenderTypeBitOffset,

            // 3 bits
            TextureSlotCountBitOffset = 6,
            TextureSlotCount1 = 1 << TextureSlotCountBitOffset,
            TextureSlotCount2 = 2 << TextureSlotCountBitOffset,
            TextureSlotCount4 = 3 << TextureSlotCountBitOffset,
            TextureSlotCount8 = 4 << TextureSlotCountBitOffset,
            TextureSlotCountBits = 7 << TextureSlotCountBitOffset,

            IsSerializing = 1 << 9,
            IsRenderingNestedTreeRT = 1 << 10
        }

        // Lookup table for texture slot counts after shift by TextureSlotCountBitOffset (index 0-7)
        static readonly int[] s_EvaluationFlagsToTextureSlotCount = { -1, 1, 2, 4, 8, -1, -1, -1 };

        // Lookup table mapping TextureSlotCount values (1,2,4,8) to their flag values (1,2,3,4)
        static readonly int[] s_TextureSlotCountToEvaluationFlags = { -1, 1, 2, -1, 3, -1, -1, -1, 4 };

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        static int FlagsToTextureSlotCount(EvaluationFlags flags)
        {
            int index = (int)(flags & EvaluationFlags.TextureSlotCountBits) >> (int)EvaluationFlags.TextureSlotCountBitOffset;
            return s_EvaluationFlagsToTextureSlotCount[index];
        }

        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        static EvaluationFlags TextureSlotCountToFlags(TextureSlotCount count)
        {
            return (EvaluationFlags)(s_TextureSlotCountToEvaluationFlags[(int)count] << (int)EvaluationFlags.TextureSlotCountBitOffset);
        }

        struct EvaluationState
        {
            public CommandList activeCommandList;
            public MaterialPropertyBlock constantProps; // Must be applied on material change only
            public MaterialPropertyBlock batchProps;
            public MaterialPropertyBlock userProps;
            public Material material;
            public int stencilRef;
            public Page curPage;
            public EvaluationFlags flags;
            public VisualElement commandListOwner;
        }

        // Before leaving an iteration over a command, the state that is altered by a draw command must be applied.
        // This must ONLY be called after stash/kick of the previous ranges has been performed.
        [MethodImpl(MethodImplOptionsEx.AggressiveInlining)]
        void ApplyDrawCommandState(RenderChainCommand cmd, int textureSlot, Material newMat, bool newMatDiffers, MaterialPropertyBlock userProps, EvaluationFlags defaultTextureSlotCountFlags, bool kickRanges, Texture gradientSettings, Texture shaderInfo, ref EvaluationState st)
        {
            if (newMatDiffers)
            {
                st.material = newMat;
                st.userProps = userProps;
                st.flags |= EvaluationFlags.MustApplyMaterial;

                st.flags &= ~EvaluationFlags.TextureSlotCountBits;
                if ((cmd.flags & CommandFlags.ForceSingleTextureSlot) != 0)
                    st.flags |= EvaluationFlags.TextureSlotCount1;
                else
                    st.flags |= defaultTextureSlotCountFlags;

                st.flags &= ~EvaluationFlags.ForceRenderTypeBits;
                if ((cmd.flags & CommandFlags.ForceRenderTypeBits) != 0)
                {
                    uint noShiftFlags = (uint)(cmd.flags & CommandFlags.ForceRenderTypeBits) >> (int)CommandFlags.ForceRenderTypeBitOffset;
                    st.flags |= (EvaluationFlags)(noShiftFlags << (int)EvaluationFlags.ForceRenderTypeBitOffset);
                }

                // Add another material to the current owner
                if ((st.flags & EvaluationFlags.IsSerializing) != 0)
                    SetupCommandList(ref st, gradientSettings, shaderInfo, cmd.flags);
            }

            if (kickRanges)
                // For some reason, we're starting a new batch. It could be because of a material change or other reason.
                m_TextureSlotManager.StartNewBatch(FlagsToTextureSlotCount(st.flags));

            st.curPage = cmd.mesh.allocPage;

            if (cmd.texture != TextureId.invalid)
            {
                if (textureSlot < 0)
                {
                    textureSlot = m_TextureSlotManager.FindOldestSlot();
                    m_TextureSlotManager.Bind(cmd.texture, cmd.sdfScale, cmd.sharpness, (cmd.flags & CommandFlags.IsPremultiplied) != 0, textureSlot, st.batchProps, st.activeCommandList);
                    st.flags |= EvaluationFlags.MustApplyBatchProps;
                }
                else
                    m_TextureSlotManager.MarkUsed(textureSlot);
            }

            if (cmd.stencilRef != st.stencilRef)
            {
                st.stencilRef = cmd.stencilRef;
                st.flags |= EvaluationFlags.MustApplyStencil;
            }
        }

        // Before calling KickRanges, this method MUST be called to ensure that any information previously stored
        // in the property blocks is applied.
        void ApplyBatchState(ref EvaluationState st)
        {
            if ((st.flags & EvaluationFlags.MustApplyMaterial) != 0)
            {
                m_DrawStats.materialSetCount++;

                if (st.activeCommandList == null)
                {
                    // World-space rendering should not go through this code path, unless it is rendering
                    // a nested render tree in a RenderTexture.
                    Debug.Assert(isFlat || (st.flags & EvaluationFlags.IsRenderingNestedTreeRT) == EvaluationFlags.IsRenderingNestedTreeRT);

                    bool setsKeyword = false;

                    // TODO: Avoid the use of strings
                    // TODO: Use the native material manager instead of setting keywords like this. Otherwise it invalidates the cached SMDs.
                    if (forceGammaRendering)
                    {
                        st.material.EnableKeyword(Shaders.k_ForceGammaKeyword);
                        setsKeyword = true;
                    }
                    else
                        st.material.DisableKeyword(Shaders.k_ForceGammaKeyword);

                    switch (st.flags & EvaluationFlags.TextureSlotCountBits)
                    {
                        case EvaluationFlags.TextureSlotCount8:
                            st.material.DisableKeyword(Shaders.k_TextureSlotCount1);
                            st.material.DisableKeyword(Shaders.k_TextureSlotCount2);
                            st.material.DisableKeyword(Shaders.k_TextureSlotCount4);
                            break;
                        case EvaluationFlags.TextureSlotCount4:
                            st.material.DisableKeyword(Shaders.k_TextureSlotCount1);
                            st.material.DisableKeyword(Shaders.k_TextureSlotCount2);
                            st.material.EnableKeyword(Shaders.k_TextureSlotCount4);
                            setsKeyword = true;
                            break;
                        case EvaluationFlags.TextureSlotCount2:
                            st.material.DisableKeyword(Shaders.k_TextureSlotCount1);
                            st.material.EnableKeyword(Shaders.k_TextureSlotCount2);
                            st.material.DisableKeyword(Shaders.k_TextureSlotCount4);
                            setsKeyword = true;
                            break;
                        case EvaluationFlags.TextureSlotCount1:
                            st.material.EnableKeyword(Shaders.k_TextureSlotCount1);
                            st.material.DisableKeyword(Shaders.k_TextureSlotCount2);
                            st.material.DisableKeyword(Shaders.k_TextureSlotCount4);
                            setsKeyword = true;
                            break;
                        default:
                            throw new ArgumentException($"Unsupported texture slot count.");
                    }

                    switch (st.flags & EvaluationFlags.ForceRenderTypeBits)
                    {
                        case EvaluationFlags.ForceRenderTypeSolid:
                            st.material.EnableKeyword(Shaders.k_ForceRenderTypeSolid);
                            st.material.DisableKeyword(Shaders.k_ForceRenderTypeTextured);
                            st.material.DisableKeyword(Shaders.k_ForceRenderTypeText);
                            st.material.DisableKeyword(Shaders.k_ForceRenderTypeSvgGradient);
                            setsKeyword = true;
                            break;
                        case EvaluationFlags.ForceRenderTypeTextured:
                            st.material.DisableKeyword(Shaders.k_ForceRenderTypeSolid);
                            st.material.EnableKeyword(Shaders.k_ForceRenderTypeTextured);
                            st.material.DisableKeyword(Shaders.k_ForceRenderTypeText);
                            st.material.DisableKeyword(Shaders.k_ForceRenderTypeSvgGradient);
                            setsKeyword = true;
                            break;
                        case EvaluationFlags.ForceRenderTypeText:
                            st.material.DisableKeyword(Shaders.k_ForceRenderTypeSolid);
                            st.material.DisableKeyword(Shaders.k_ForceRenderTypeTextured);
                            st.material.EnableKeyword(Shaders.k_ForceRenderTypeText);
                            st.material.DisableKeyword(Shaders.k_ForceRenderTypeSvgGradient);
                            setsKeyword = true;
                            break;
                        case EvaluationFlags.ForceRenderTypeSvgGradient:
                            st.material.DisableKeyword(Shaders.k_ForceRenderTypeSolid);
                            st.material.DisableKeyword(Shaders.k_ForceRenderTypeTextured);
                            st.material.DisableKeyword(Shaders.k_ForceRenderTypeText);
                            st.material.EnableKeyword(Shaders.k_ForceRenderTypeSvgGradient);
                            setsKeyword = true;
                            break;
                        default:
                            st.material.DisableKeyword(Shaders.k_ForceRenderTypeSolid);
                            st.material.DisableKeyword(Shaders.k_ForceRenderTypeTextured);
                            st.material.DisableKeyword(Shaders.k_ForceRenderTypeText);
                            st.material.DisableKeyword(Shaders.k_ForceRenderTypeSvgGradient);
                            break;
                    }

                    if (setsKeyword)
                        m_ScreenSpaceAlteredMaterials.Add(st.material);

                    st.material.SetPass(0); // No multipass support, should it be even considered?
                    Utility.SetPropertyBlock(st.constantProps);

                    if (st.userProps != null)
                        Utility.SetPropertyBlock(st.userProps);

                    st.flags |= EvaluationFlags.MustApplyBatchProps | EvaluationFlags.MustApplyStencil;
                }
                else
                {
                    if (st.userProps != null)
                        st.activeCommandList.ApplyUserProps(st.userProps);
                }
            }

            if ((st.flags & EvaluationFlags.MustApplyBatchProps) != 0)
            {
                if (st.activeCommandList == null)
                    Utility.SetPropertyBlock(st.batchProps);
                else
                    st.activeCommandList.ApplyBatchProps();
            }

            if ((st.flags & EvaluationFlags.MustApplyStencil) != 0)
            {
                ++m_DrawStats.stencilRefChanges;
                if (st.activeCommandList == null)
                    Utility.SetStencilState(m_DefaultStencilState, st.stencilRef);
                // else Not supported yet in world-space
            }

            st.flags &= ~(EvaluationFlags.MustApplyMaterial | EvaluationFlags.MustApplyBatchProps | EvaluationFlags.MustApplyStencil);
        }

        public unsafe void EvaluateChain(
            RenderChainCommand head,
            Material defaultMat,
            Texture gradientSettings,
            Texture shaderInfo,
            Rect? scissor,
            float pixelsPerPoint,
            bool isSerializing,
            TextureSlotCount defaultTextureSlotCount,
            bool isRenderingNestedTreeRT,
            ref Exception immediateException)
        {
            Utility.ProfileDrawChainBegin();

            bool doBreakBatches = this.breakBatches; // Keeping this on the stack for better performance

            int rangesCount = 1024; // Must be powers of two. TODO: Can be estimated better from the render chain command count
            DrawBufferRange* ranges = stackalloc DrawBufferRange[rangesCount];
            int rangesCountMinus1 = rangesCount - 1;
            int rangesStart = 0;
            int rangesReady = 0;
            DrawBufferRange curDrawRange = new DrawBufferRange();
            int curDrawIndex = -1;
            int disableCounter = 0;

            EvaluationFlags defaultTextureSlotCountFlags = TextureSlotCountToFlags(defaultTextureSlotCount);
            EvaluationFlags isRenderingNestedRTFlags = isRenderingNestedTreeRT ? EvaluationFlags.IsRenderingNestedTreeRT : EvaluationFlags.None;

            var st = new EvaluationState
            {
                flags = EvaluationFlags.MustApplyBatchProps | EvaluationFlags.MustApplyStencil | defaultTextureSlotCountFlags | isRenderingNestedRTFlags
            };

            if (isSerializing)
            {
                m_CommandListManager.BeginSerialize(defaultTextureSlotCount);
                st.activeCommandList = m_CommandListManager.defaultCommandList;
                st.flags |= EvaluationFlags.IsSerializing;
            }
            else
            {
                st.constantProps = m_ConstantProps;
                InitializeConstantProperties(st.constantProps, gradientSettings, shaderInfo);
                st.batchProps = m_BatchProps;
                st.batchProps.Clear();
            }

            var drawParams = m_DrawParams;
            drawParams.Reset();

            RenderChainCommand.PushScissor(drawParams, scissor ?? DrawParams.k_UnlimitedRect, pixelsPerPoint);

            m_TextureSlotManager.Reset();
            m_TextureSlotManager.StartNewBatch((int)defaultTextureSlotCount);

            MaterialPropertyBlock userProps = null;

            while (head != null)
            {
                if (head.type == CommandType.BeginDisable)
                {
                    m_DrawStats.commandCount++;
                    disableCounter++;
                    head = head.next;
                    continue;
                }

                if (head.type == CommandType.EndDisable)
                {
                    m_DrawStats.commandCount++;
                    disableCounter--;
                    head = head.next;
                    continue;
                }

                if (disableCounter > 0)
                {
                    m_DrawStats.skippedCommandCount++;
                    head = head.next;
                    continue;
                }

                m_DrawStats.drawCommandCount += (head.type == CommandType.Draw ? 1u : 0u);

                bool isLastRange = curDrawRange.indexCount > 0 && rangesReady == rangesCount - 1;
                bool stashRange = false; // Should we close the contiguous draw range that we had before this command (if any)?
                bool kickRanges = false; // Should we draw all the ranges that we had accumulated so far?

                // The following data is fetched during the preprocessing phase below.
                // They are cached so we can skip many checks afterwards.
                bool mustApplyCmdState = false; // Whenever a state change is detected, this must be true.
                int textureSlot = -1; // This avoids looping to find the index again in ApplyDrawCommandState
                Material newMat = null; // This avoids repeating the null check in ApplyDrawCommandState
                bool newMatDiffers = false; // This avoids repeating the material comparison in ApplyDrawCommandState

                // Preprocessing here. We determine whether the command requires to break the batch, and how it should
                // be processed afterwards to avoid redundant computations. ** The state must NOT be altered in any way **
                if (head.type == CommandType.Draw)
                {
                    // Do we change the state by forcing a different render type?
                    uint forcedRenderTypeFromState = ((uint)(st.flags & EvaluationFlags.ForceRenderTypeBits)) >> (int)EvaluationFlags.ForceRenderTypeBitOffset;
                    uint forcedRenderTypeFromCommand = ((uint)(head.flags & CommandFlags.ForceRenderTypeBits)) >> (int)CommandFlags.ForceRenderTypeBitOffset;

                    // Do we change the state by modifying the texture slot count?
                    bool commandForcesSingleTextureSlot = (head.flags & CommandFlags.ForceSingleTextureSlot) != 0;
                    EvaluationFlags prevTextureSlotCount = st.flags & EvaluationFlags.TextureSlotCountBits;
                    EvaluationFlags nextTextureSlotCount = commandForcesSingleTextureSlot ? EvaluationFlags.TextureSlotCount1 : defaultTextureSlotCountFlags;

                    // Do we change the material?
                    newMat = head.material != null ? head.material : defaultMat;

                    if (forcedRenderTypeFromState != forcedRenderTypeFromCommand || prevTextureSlotCount != nextTextureSlotCount || newMat != st.material)
                    {
                        mustApplyCmdState = true;
                        newMatDiffers = true;
                        stashRange = true;
                        kickRanges = true;
                    }
                    else
                    {
                        if (head.mesh.allocPage != st.curPage)
                        {
                            mustApplyCmdState = true;
                            stashRange = true;
                            kickRanges = true;
                        }
                        else if (curDrawIndex != head.mesh.allocIndices.start + head.indexOffset)
                            stashRange = true; // Same page but discontinuous range.

                        if (head.texture != TextureId.invalid)
                        {
                            mustApplyCmdState = true;
                            textureSlot = m_TextureSlotManager.IndexOf(head.texture); // Assuming this command doesn't change the texture slot count
                            if (textureSlot < 0 && m_TextureSlotManager.FreeSlots < 1)
                            { // No more slots available.
                                stashRange = true;
                                kickRanges = true;
                            }
                        }

                        if (head.stencilRef != st.stencilRef)
                        {
                            mustApplyCmdState = true;
                            stashRange = true;
                            kickRanges = true;
                        }

                        if (stashRange && isLastRange)
                        {
                            // The range we'll close is the last that we can store.
                            // TODO: This only works since ranges are serialized and will break once the ranges are
                            //       truly processed in a multi-threaded fashion without copies. When this happens, a new
                            //       mechanism will need to be implemented to handle the "ranges-buffer-full" condition. For
                            //       the time being, calling KickRanges will make the whole buffer available.
                            kickRanges = true;
                        }
                    }
                }
                else
                {
                    // Skip matching Pop/Push default material commands
                    if (head.type == CommandType.PopDefaultMaterial
                        && head.next?.type == CommandType.PushDefaultMaterial
                        && defaultMat == head.next?.material
                        && userProps == head.next?.userProps)
                    {
                        head = head.next.next;
                        continue;
                    }

                    stashRange = true;
                    kickRanges = true;
                }

                if (doBreakBatches)
                {
                    stashRange = true;
                    kickRanges = true;
                }

                if (stashRange)
                {
                    // Stash the current draw range (from previous commands)
                    if (curDrawRange.indexCount > 0)
                    {
                        int wrapAroundIndex = (rangesStart + rangesReady++) & rangesCountMinus1;
                        ranges[wrapAroundIndex] = curDrawRange; // Close the active range (BEFORE the current command).
                        Debug.Assert(rangesReady < rangesCount || kickRanges);
                        curDrawRange = new DrawBufferRange();
                        m_DrawStats.drawRangeCount++;
                    }

                    if (head.type == CommandType.Draw)
                    {
                        curDrawRange.firstIndex = (int)head.mesh.allocIndices.start + head.indexOffset;
                        curDrawRange.indexCount = head.indexCount;
                        curDrawRange.vertsReferenced = (int)(head.mesh.allocVerts.size);
                        curDrawRange.minIndexVal = (int)head.mesh.allocVerts.start;
                        curDrawIndex = curDrawRange.firstIndex + head.indexCount;
                        m_DrawStats.totalIndices += (uint)head.indexCount;
                    }
                }
                else // Only continuous draw commands can get here because other commands force stash+kick
                {
                    // We can chain
                    if (curDrawRange.indexCount == 0)
                        curDrawIndex = curDrawRange.firstIndex = (int)head.mesh.allocIndices.start + head.indexOffset; // A first draw after a stash
                    curDrawRange.indexCount += head.indexCount;
                    int prevFirstVertex = curDrawRange.minIndexVal;
                    int curFirstVertex = (int)head.mesh.allocVerts.start;
                    int prevLastVertex = curDrawRange.minIndexVal + curDrawRange.vertsReferenced;
                    int curLastVertex = (int)(head.mesh.allocVerts.start + head.mesh.allocVerts.size);
                    curDrawRange.minIndexVal = Mathf.Min(prevFirstVertex, curFirstVertex);
                    curDrawRange.vertsReferenced = Mathf.Max(prevLastVertex, curLastVertex) - curDrawRange.minIndexVal;
                    curDrawIndex += head.indexCount;
                    m_DrawStats.totalIndices += (uint)head.indexCount;

                    if (mustApplyCmdState)
                        ApplyDrawCommandState(head, textureSlot, newMat, newMatDiffers, userProps, defaultTextureSlotCountFlags, kickRanges, gradientSettings, shaderInfo, ref st);
                    head = head.next;
                    continue;
                }

                // Only the stashed ranges are kicked: curDrawRange will NOT be kicked.
                if (kickRanges)
                {
                    if (rangesReady > 0)
                    {
                        ApplyBatchState(ref st);
                        KickRanges(ranges, ref rangesReady, ref rangesStart, rangesCount, st.curPage, st.activeCommandList);
                    }

                    if (head.type != CommandType.Draw)
                    {
                        if (head.type == CommandType.CutRenderChain)
                        {
                            st.material = null; // Force command list to be created on next draw command
                            st.commandListOwner = head.owner.owner;
                        }

                        if (head.type == CommandType.Immediate || head.type == CommandType.ImmediateCull)
                            ResetScreenSpaceMaterials();

                        head.ExecuteNonDrawMesh(drawParams, pixelsPerPoint, ref immediateException);
                        if (head.type == CommandType.Immediate || head.type == CommandType.ImmediateCull || head.type == CommandType.PopDefaultMaterial || head.type == CommandType.PushDefaultMaterial)
                        {
                            st.material = null; // A value that is unique to force material reset on next draw command
                            st.flags &= ~EvaluationFlags.MustApplyMaterial;
                            m_DrawStats.immediateDraws++;

                            if (head.type == CommandType.PopDefaultMaterial)
                            {
                                int index = drawParams.defaultMaterial.Count - 1;
                                defaultMat = drawParams.defaultMaterial[index];
                                userProps = drawParams.props[index];
                                drawParams.defaultMaterial.RemoveAt(index);
                            }
                            if (head.type == CommandType.PushDefaultMaterial)
                            {
                                drawParams.defaultMaterial.Add(defaultMat);
                                drawParams.props.Add(head.userProps);
                                defaultMat = head.material;
                                userProps = head.userProps;
                            }
                        }
                    }
                } // If kick ranges

                if (head.type == CommandType.Draw && mustApplyCmdState)
                    ApplyDrawCommandState(head, textureSlot, newMat, newMatDiffers, userProps, defaultTextureSlotCountFlags, kickRanges, gradientSettings, shaderInfo, ref st);

                head = head.next;
            } // While there are commands to execute

            // Kick any pending ranges, this usually occurs when the draw chain ends with a draw command.
            if (curDrawRange.indexCount > 0)
            {
                int wrapAroundIndex = (rangesStart + rangesReady++) & rangesCountMinus1;
                ranges[wrapAroundIndex] = curDrawRange;
            }

            if (rangesReady > 0)
            {
                ApplyBatchState(ref st);
                KickRanges(ranges, ref rangesReady, ref rangesStart, rangesCount, st.curPage, st.activeCommandList);
            }

            Debug.Assert(disableCounter == 0, "Rendering disabled counter is not 0, indicating a mismatch of commands");

            RenderChainCommand.PopScissor(drawParams, pixelsPerPoint);

            UpdateFenceValue(); // TODO: Replace by GPU fence.

            Utility.ProfileDrawChainEnd();

            if ((st.flags & EvaluationFlags.IsSerializing) != 0)
                m_CommandListManager.EndSerialize();

            ResetScreenSpaceMaterials();
        }

        void ResetScreenSpaceMaterials()
        {
            foreach (Material material in m_ScreenSpaceAlteredMaterials)
            {
                if (material == null)
                    continue;

                material.DisableKeyword(Shaders.k_ForceGammaKeyword);
                material.DisableKeyword(Shaders.k_TextureSlotCount1);
                material.DisableKeyword(Shaders.k_TextureSlotCount2);
                material.DisableKeyword(Shaders.k_TextureSlotCount4);
                material.DisableKeyword(Shaders.k_ForceRenderTypeSolid);
                material.DisableKeyword(Shaders.k_ForceRenderTypeTextured);
                material.DisableKeyword(Shaders.k_ForceRenderTypeText);
                material.DisableKeyword(Shaders.k_ForceRenderTypeSvgGradient);
            }

            m_ScreenSpaceAlteredMaterials.Clear();
        }

        private void InitializeConstantProperties(MaterialPropertyBlock constantProps, Texture gradientSettings, Texture shaderInfo)
        {
            if (gradientSettings != null)
                constantProps.SetTexture(s_GradientSettingsTexID, gradientSettings);
            if (shaderInfo != null)
                constantProps.SetTexture(s_ShaderInfoTexID, shaderInfo);
        }

        private void SetupCommandList(ref EvaluationState st, Texture gradientSettings, Texture shaderInfo, CommandFlags commandFlags)
        {
            if (st.commandListOwner == null)
                // This is the default command list. Ignore material changes.
                return;

            CommandList cmdList = m_CommandListManager.GetOrCreateCommandList(st.commandListOwner, st.material, commandFlags);
            InitializeConstantProperties(cmdList.constantProps, gradientSettings, shaderInfo);

            st.activeCommandList = cmdList;

            // When a CommandList is used, we should not be modifying the batchprops,
            // only those owned by the CommandList will be filled at execution time.
            st.constantProps = null;
            st.batchProps = null;

            st.flags |= EvaluationFlags.MustApplyBatchProps | EvaluationFlags.MustApplyStencil;

            m_TextureSlotManager.Reset();
        }

        unsafe void UpdateFenceValue()
        {
            uint newFenceVal = Utility.InsertCPUFence();
            fixed(uint* fence = &m_Fences[(int)(m_FrameIndex % m_Fences.Length)])
            {
                for (;;)
                {
                    uint curFenceVal = *fence;
                    if (((int)(newFenceVal - curFenceVal)) <= 0) // This is the same test as in GfxDeviceWorker::WaitOnCPUFence(). Handles wrap around.
                        break; // Our newFenceVal is already older than the current one, so keep the current
                    int cmpOldVal = System.Threading.Interlocked.CompareExchange(ref *((int*)fence), (int)newFenceVal, (int)curFenceVal);
                    if (cmpOldVal == curFenceVal)
                        break; // The exchange succeeded, now newFenceVal is stored atomically in (*fence)
                }
            }
        }

        unsafe void KickRanges(DrawBufferRange* ranges, ref int rangesReady, ref int rangesStart, int rangesCount, Page curPage, CommandList commandList)
        {
            Debug.Assert(rangesReady > 0);

            if (rangesStart + rangesReady <= rangesCount)
            {
                DrawRanges(curPage.indices.gpuData, curPage.vertices.gpuData, PtrToSlice<DrawBufferRange>(ranges + rangesStart, rangesReady), commandList);
                m_DrawStats.drawRangeCallCount++;
            }
            else
            {
                // Less common situation, the numbers straddles the end of the ranges buffer
                int firstRangeCount = rangesCount - rangesStart;
                int secondRangeCount = rangesReady - firstRangeCount;
                DrawRanges(curPage.indices.gpuData, curPage.vertices.gpuData, PtrToSlice<DrawBufferRange>(ranges + rangesStart, firstRangeCount), commandList);
                DrawRanges(curPage.indices.gpuData, curPage.vertices.gpuData, PtrToSlice<DrawBufferRange>(ranges, secondRangeCount), commandList);

                m_DrawStats.drawRangeCallCount += 2;
            }

            rangesStart = (rangesStart + rangesReady) & (rangesCount - 1);
            rangesReady = 0;
        }

        unsafe void DrawRanges(Utility.GPUBuffer<ushort> ib, Utility.GPUBuffer<Vertex> vb, NativeSlice<DrawBufferRange> ranges, CommandList commandList)
        {
            if (commandList != null)
            {
                commandList.DrawRanges(ib, vb, ranges);
            }
            else
            {
                IntPtr* vStream = stackalloc IntPtr[1];
                vStream[0] = vb.BufferPointer;
                Utility.DrawRanges(ib.BufferPointer, vStream, 1, new IntPtr(ranges.GetUnsafePtr()), ranges.Length, m_VertexDecl);
            }
        }

        // Used for testing purposes only (e.g. performance test warmup)
        internal void WaitOnAllCpuFences()
        {
            for (int i = 0; i < m_Fences.Length; ++i)
                WaitOnCpuFence(m_Fences[i]);
        }

        void WaitOnCpuFence(uint fence)
        {
            if (fence != 0 && !Utility.CPUFencePassed(fence))
            {
                s_MarkerFence.Begin();
                Utility.WaitForCPUFencePassed(fence);
                s_MarkerFence.End();
            }
        }

        public void AdvanceFrame()
        {
            m_RenderingInProgress = false;

            s_MarkerAdvanceFrame.Begin();

            m_FrameIndex++;

            m_DrawStats.currentFrameIndex = (int)m_FrameIndex;

            {
                int fenceIndex = (int)(m_FrameIndex % m_Fences.Length);
                uint fence = m_Fences[fenceIndex];
                WaitOnCpuFence(fence);
                m_Fences[fenceIndex] = 0;
            }

            m_CommandListManager.AdvanceFrame();
            m_MeshManager.AdvanceFrame();

            foreach (var mesh in m_MeshesPendingFree)
                Free(mesh);

            m_MeshesPendingFree.Clear();

            s_MarkerAdvanceFrame.End();
        }

        internal static void PrepareForGfxDeviceRecreate()
        {
            m_ActiveDeviceCount += 1; // Don't let the count reach 0 and unsubscribe from GfxDeviceRecreate
        }

        internal static void WrapUpGfxDeviceRecreate() { m_ActiveDeviceCount -= 1; }
        internal static void FlushAllPendingDeviceDisposes()
        {
            Utility.SyncRenderThread();
            ProcessDeviceFreeQueue();
        }

        internal struct AllocationStatistics
        {
            public struct PageStatistics { internal HeapStatistics vertices, indices; }
            public PageStatistics[] pages;
            public int[] freesDeferred;
        }

        internal AllocationStatistics GatherAllocationStatistics()
        {
            return m_MeshManager.GatherAllocationStatistics();
        }

        internal struct DrawStatistics
        {
            public int currentFrameIndex;
            public uint totalIndices;
            public uint commandCount;
            public uint skippedCommandCount;
            public uint drawCommandCount;
            public uint disableCommandCount;
            public uint materialSetCount;
            public uint drawRangeCount;
            public uint drawRangeCallCount;
            public uint immediateDraws;
            public uint stencilRefChanges;
        }
        internal DrawStatistics GatherDrawStatistics() { return m_DrawStats; }


        #region Internals

        public static void ProcessDeviceFreeQueue()
        {
            s_MarkerFree.Begin();

            if (m_SynchronousFree)
                Utility.SyncRenderThread();

            var freeNode = m_DeviceFreeQueue.First;
            while (freeNode != null)
            {
                if (!Utility.CPUFencePassed(freeNode.Value.handle))
                    break;
                freeNode.Value.Dispose();
                m_DeviceFreeQueue.RemoveFirst();
                freeNode = m_DeviceFreeQueue.First;
            }

            // After synchronizing with the render thread, all cpu fences should pass.
            Debug.Assert(!m_SynchronousFree || m_DeviceFreeQueue.Count == 0);

            if (m_ActiveDeviceCount == 0 && m_SubscribedToNotifications)
            {
                Utility.NotifyOfUIREvents(false);
                m_SubscribedToNotifications = false;
            }

            s_MarkerFree.End();
        }

        private static void OnEngineUpdateGlobal()
        {
            ProcessDeviceFreeQueue();
        }

        private static void OnFlushPendingResources()
        {
            m_SynchronousFree = true;
            ProcessDeviceFreeQueue();
        }

        #endregion // Internals
    }
}
