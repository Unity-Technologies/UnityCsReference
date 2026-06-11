// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.UIElements.UIR
{
    // Keep in sync with native SerializedCommandType enum
    enum SerializedCommandType : byte
    {
        DrawRanges,
        SetTexture,
        ApplyBatchProps,
        ApplyUserProps
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UIElements/Core/Native/Renderer/PanelRenderer.h")]
    struct SerializedCommand
    {
        public SerializedCommandType type;
        public KickRangesReason kickReason;
        public IntPtr vertexBuffer;
        public IntPtr indexBuffer;
        public int firstRange;
        public int rangeCount;

        public int textureName;
        public int gpuDataOffset;
        public IntPtr textureRefPtr;
        public Vector4 gpuData0;
        public Vector4 gpuData1;

        public IntPtr userProps;
    }

    class CommandList : IDisposable
    {
        static readonly MemoryLabel k_MemoryLabel = new (nameof(UIElements), $"Renderer.{nameof(CommandList)}");

        public VisualElement m_Owner; // Might be null if non-initialized or for the default command list.
        public UIRenderer m_UIRenderer;
        public PanelRenderer m_PanelRenderer;
        readonly IntPtr m_VertexDecl;
        readonly IntPtr m_StencilState;
        public MaterialPropertyBlock constantProps = new();
        public GCHandle handle; // GCHandle for native-side interactions
        public Material m_Material;
        public CommandFlags flags;

        public IntPtr stencilState => m_StencilState;

        NativeList<SerializedCommand> m_Commands;
        Vector4[] m_GpuTextureData = new Vector4[TextureSlotManager.k_SlotSize * TextureSlotManager.k_MaxSlotCount];
        NativeList<DrawBufferRange> m_DrawRanges;
        List<MaterialPropertyBlock> m_UserPropBlocks = new(); // Keep MaterialPropertyBlocks alive for a few frames

        public CommandList(IntPtr vertexDecl, IntPtr stencilState)
        {
            m_VertexDecl = vertexDecl;
            m_StencilState = stencilState;
            m_Commands = new(256, k_MemoryLabel); // No deferred disposal needed has the pointer is sent to native after building
            m_DrawRanges = new(1024, k_MemoryLabel, (int)UIRenderDevice.k_MaxQueuedFrameCount);
            handle = GCHandle.Alloc(this);
        }

        public int Count => m_Commands.Count;
        public NativeList<DrawBufferRange> ActiveDrawRanges => m_DrawRanges;

        public NativeList<SerializedCommand> Commands => m_Commands;

        public void Reset()
        {
            m_Owner = null;
            m_UIRenderer = null;
            m_Material = null;
            var commandsBuffer = m_Commands.GetBuffer();
            for (int i = 0; i < m_Commands.Count; i++)
            {
                SerializedCommand cmd = commandsBuffer[i];
                if (cmd.type == SerializedCommandType.SetTexture)
                {
                    Utility.ReleaseTextureRef(cmd.textureRefPtr);
                }
            }
            m_Commands.Clear();
            m_DrawRanges.Clear();
            constantProps.Clear();
            m_UserPropBlocks.Clear();
        }

        public void Init(VisualElement owner, Material material, CommandFlags commandFlags)
        {
            Debug.Assert(m_Owner == null);
            m_Owner = owner;
            m_UIRenderer = (owner as UIDocumentRootElement)?.uiRenderer;
            m_PanelRenderer = (owner as PanelRendererRootElement)?.panelRenderer;
            m_Material = material;
            flags = commandFlags;

            for (int i = 0; i < m_GpuTextureData.Length; ++i)
                m_GpuTextureData[i] = Vector4.zero;
        }

        public void SetTexture(int name, Texture texture, int gpuDataOffset, Vector4 gpuData0, Vector4 gpuData1)
        {
            var cmd = new SerializedCommand
            {
                type = SerializedCommandType.SetTexture,
                textureName = name,
                textureRefPtr = Utility.AllocateTextureRef(texture),
                gpuDataOffset = gpuDataOffset,
                gpuData0 = gpuData0,
                gpuData1 = gpuData1,
            };

            m_Commands.Add(ref cmd);
        }

        public void ApplyUserProps(MaterialPropertyBlock userProps)
        {
            // Keep the MaterialPropertyBlock alive so the GC doesn't free the underlying native object
            // while it's still in use by the render thread
            m_UserPropBlocks.Add(userProps);

            var cmd = new SerializedCommand
            {
                type = SerializedCommandType.ApplyUserProps,
                userProps = MaterialPropertyBlock.BindingsMarshaller.ConvertToNative(userProps)
            };
            m_Commands.Add(ref cmd);
        }

        public void ApplyBatchProps()
        {
            var cmd = new SerializedCommand
            {
                type = SerializedCommandType.ApplyBatchProps
            };

            m_Commands.Add(ref cmd);
        }

        public void DrawRanges(Utility.GPUBuffer ib, Utility.GPUBuffer vb, NativeSlice<DrawBufferRange> ranges, KickRangesReason kickReason)
        {
            var cmd = new SerializedCommand
            {
                type = SerializedCommandType.DrawRanges,
                kickReason = kickReason,
                vertexBuffer = vb.BufferPointer,
                indexBuffer = ib.BufferPointer,
                firstRange = m_DrawRanges.Count,
                rangeCount = ranges.Length,
            };
            m_Commands.Add(ref cmd);
            m_DrawRanges.Add(ranges);
        }

        #region Dispose Pattern

        protected bool disposed { get; private set; }


        public void Dispose()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                var commandsBuffer = m_Commands.GetBuffer();
                for (int i = 0; i < m_Commands.Count; i++)
                {
                    SerializedCommand cmd = commandsBuffer[i];
                    if (cmd.type == SerializedCommandType.SetTexture)
                    {
                        Utility.ReleaseTextureRef(cmd.textureRefPtr);
                    }
                }

                m_Commands.Dispose();
                m_DrawRanges.Dispose();
                m_DrawRanges = null;
                if (handle.IsAllocated)
                    handle.Free();
            }
            else DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern
    }
}
