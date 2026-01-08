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
    enum SerializedCommandType
    {
        DrawRanges,
        SetTexture,
        ApplyBatchProps,
        ApplyUserProps
    }

    struct SerializedCommand
    {
        public SerializedCommandType type;
        public IntPtr vertexBuffer;
        public IntPtr indexBuffer;
        public int firstRange;
        public int rangeCount;

        public int textureName;
        public IntPtr textureRefPtr;
        public int gpuDataOffset;
        public Vector4 gpuData0;
        public Vector4 gpuData1;

        public MaterialPropertyBlock userProps;
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

        List<SerializedCommand> m_Commands = new();
        Vector4[] m_GpuTextureData = new Vector4[TextureSlotManager.k_SlotSize * TextureSlotManager.k_MaxSlotCount];
        NativeList<DrawBufferRange> m_DrawRanges;

        public CommandList(IntPtr vertexDecl, IntPtr stencilState)
        {
            m_VertexDecl = vertexDecl;
            m_StencilState = stencilState;
            m_DrawRanges = new(1024, k_MemoryLabel);
            handle = GCHandle.Alloc(this);
        }

        public int Count => m_Commands.Count;
        public NativeList<DrawBufferRange> ActiveDrawRanges => m_DrawRanges;

        public List<SerializedCommand> Commands => m_Commands;

        public void Reset()
        {
            m_Owner = null;
            m_UIRenderer = null;
            m_Material = null;
            for (int i = 0; i < m_Commands.Count; i++)
            {
                SerializedCommand cmd = m_Commands[i];
                if (cmd.type == SerializedCommandType.SetTexture)
                {
                    Utility.ReleaseTextureRef(cmd.textureRefPtr);
                }
            }
            m_Commands.Clear();
            m_DrawRanges.Clear();
            constantProps.Clear();
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

        public unsafe void Execute()
        {
            IntPtr* vStream = stackalloc IntPtr[1];

            // Initialize state
            Utility.SetPropertyBlock(constantProps);
            Utility.SetStencilState(m_StencilState, 0);

            int textureCount = 0;
            int* textureNames = stackalloc int[8];
            IntPtr* textureRefPtrs = stackalloc IntPtr[8];

            IntPtr shaderPropertySheetPtr = Utility.AllocateShaderPropertySheet();
            try
            {
                for (int i = 0; i < m_Commands.Count; ++i)
                {
                    // TODO: Use reference instead of copy (not currently possible with List<T>)
                    SerializedCommand cmd = m_Commands[i];
                    switch (cmd.type)
                    {
                        case SerializedCommandType.SetTexture:
                            textureNames[textureCount] = cmd.textureName;
                            textureRefPtrs[textureCount] = cmd.textureRefPtr;
                            textureCount++;
                            m_GpuTextureData[cmd.gpuDataOffset + 0] = cmd.gpuData0;
                            m_GpuTextureData[cmd.gpuDataOffset + 1] = cmd.gpuData1;
                            break;
                        case SerializedCommandType.ApplyBatchProps:
                            Utility.SetAllTextures(shaderPropertySheetPtr, new IntPtr(textureNames), new IntPtr(textureRefPtrs), textureCount);
                            textureCount = 0;
                            Utility.SetVectorArray(shaderPropertySheetPtr, TextureSlotManager.textureTableId, m_GpuTextureData);
                            Utility.ApplyShaderPropertySheet(shaderPropertySheetPtr);
                            break;
                        case SerializedCommandType.ApplyUserProps:
                            Utility.SetPropertyBlock(cmd.userProps);
                            break;
                        case SerializedCommandType.DrawRanges:
                            vStream[0] = cmd.vertexBuffer;
                            Utility.DrawRanges(cmd.indexBuffer, vStream, 1, new IntPtr(m_DrawRanges.GetSlice(cmd.firstRange, cmd.rangeCount).GetUnsafePtr()), cmd.rangeCount, m_VertexDecl);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
            finally
            {
                Utility.ReleasePropertySheet(shaderPropertySheetPtr);
            }
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

            m_Commands.Add(cmd);
        }

        public void ApplyUserProps(MaterialPropertyBlock userProps)
        {
            var cmd = new SerializedCommand
            {
                type = SerializedCommandType.ApplyUserProps,
                userProps = userProps
            };
            m_Commands.Add(cmd);
        }

        public void ApplyBatchProps()
        {
            var cmd = new SerializedCommand
            {
                type = SerializedCommandType.ApplyBatchProps
            };

            m_Commands.Add(cmd);
        }

        public void DrawRanges(Utility.GPUBuffer<ushort> ib, Utility.GPUBuffer<Vertex> vb, NativeSlice<DrawBufferRange> ranges)
        {
            var cmd = new SerializedCommand
            {
                type = SerializedCommandType.DrawRanges,
                vertexBuffer = vb.BufferPointer,
                indexBuffer = ib.BufferPointer,
                firstRange = m_DrawRanges.Count,
                rangeCount = ranges.Length,
            };
            m_Commands.Add(cmd);
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
                m_DrawRanges.Dispose();
                m_DrawRanges = null;
                if (handle.IsAllocated)
                    handle.Free();

                for (int i = 0; i < m_Commands.Count; i++)
                {
                    SerializedCommand cmd = m_Commands[i];
                    if (cmd.type == SerializedCommandType.SetTexture)
                    {
                        Utility.ReleaseTextureRef(cmd.textureRefPtr);
                    }
                }
            }
            else DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern
    }
}
