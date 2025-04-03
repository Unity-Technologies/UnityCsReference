// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.UIElements.UIR
{
    enum SerializedCommandType
    {
        DrawRanges,
        SetTexture,
        ApplyBatchProps,
    }

    struct SerializedCommand
    {
        public SerializedCommandType type;
        public IntPtr vertexBuffer;
        public IntPtr indexBuffer;
        public int firstRange;
        public int rangeCount;

        public int textureName;
        public Texture texture;
        public int gpuDataOffset;
        public Vector4 gpuData0;
        public Vector4 gpuData1;
    }

    class CommandList : IDisposable
    {
        public VisualElement m_Owner;
        readonly IntPtr m_VertexDecl;
        readonly IntPtr m_StencilState;
        public MaterialPropertyBlock constantProps = new();
        public MaterialPropertyBlock batchProps = new();
        public GCHandle handle; // GCHandle for native-side interactions
        public Material m_Material;

        List<SerializedCommand> m_Commands = new();
        Vector4[] m_GpuTextureData = new Vector4[TextureSlotManager.k_SlotSize * TextureSlotManager.k_SlotCount];
        NativeList<DrawBufferRange> m_DrawRanges;

        public CommandList(VisualElement owner, IntPtr vertexDecl, IntPtr stencilState, Material material)
        {
            m_Owner = owner;
            m_VertexDecl = vertexDecl;
            m_StencilState = stencilState;
            m_DrawRanges = new(1024);
            handle = GCHandle.Alloc(this);
            m_Material = material;
        }

        public int Count => m_Commands.Count;

        public void Reset(VisualElement newOwner, Material material)
        {
            m_Owner = newOwner;
            m_Commands.Clear();
            m_DrawRanges.Clear();
            m_Material = material;

            for (int i = 0; i < m_GpuTextureData.Length; ++i)
                m_GpuTextureData[i] = Vector4.zero;
        }

        public unsafe void Execute()
        {
            IntPtr* vStream = stackalloc IntPtr[1];

            // Initialize state
            Utility.SetPropertyBlock(constantProps);
            Utility.SetStencilState(m_StencilState, 0);

            for(int i = 0 ; i < m_Commands.Count ; ++i)
            {
                SerializedCommand cmd = m_Commands[i];
                switch (cmd.type)
                {
                    case SerializedCommandType.SetTexture:
                        batchProps.SetTexture(cmd.textureName, cmd.texture);
                        m_GpuTextureData[cmd.gpuDataOffset + 0] = cmd.gpuData0;
                        m_GpuTextureData[cmd.gpuDataOffset + 1] = cmd.gpuData1;
                        batchProps.SetVectorArray(TextureSlotManager.textureTableId, m_GpuTextureData);
                        break;
                    case SerializedCommandType.ApplyBatchProps:
                        Utility.SetPropertyBlock(batchProps);
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

        public void SetTexture(int name, Texture texture, int gpuDataOffset, Vector4 gpuData0, Vector4 gpuData1)
        {
            var cmd = new SerializedCommand
            {
                type = SerializedCommandType.SetTexture,
                textureName = name,
                texture = texture,
                gpuDataOffset = gpuDataOffset,
                gpuData0 = gpuData0,
                gpuData1 = gpuData1,
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
            GC.SuppressFinalize(this);
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
            }
            else DisposeHelper.NotifyMissingDispose(this);

            disposed = true;
        }

        #endregion // Dispose Pattern
    }
}
