// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.RenderPipelines.Core.Editor")]
namespace UnityEngine.LightTransport
{
    internal struct BufferID
    {
        public UInt64 Value;
        public BufferID(UInt64 value)
        {
            Value = value;
        }
    }
    internal struct BufferSlice
    {
        public BufferSlice(BufferID id, UInt64 offset)
        {
            Id = id;
            Offset = offset;
        }
        public BufferID Id;
        public UInt64 Offset;
    }
    internal struct EventID
    {
        public UInt64 Value;
        public EventID(UInt64 value)
        {
            Value = value;
        }
    }
    /// <summary>
    /// Buffer and command queue abstraction layer hiding the underlying storage
    /// and compute architecture (CPU or GPU with unified or dedicated memory).
    /// </summary>
    internal interface IDeviceContext : IDisposable
    {
        bool Initialize();
        BufferID CreateBuffer(UInt64 size);
        void DestroyBuffer(BufferID id);
        void WriteBuffer(BufferID id, NativeArray<byte> data);
        EventID EnqueueBufferWrite(BufferID id, NativeArray<byte> data);
        void ReadBuffer(BufferID id, NativeArray<byte> result);
        EventID EnqueueBufferRead(BufferID id, NativeArray<byte> result);
        bool IsAsyncOperationComplete(EventID id);
    }
    [StructLayout(LayoutKind.Sequential)]
    internal class ReferenceContext : IDeviceContext
    {
        private Dictionary<BufferID, NativeArray<byte>> buffers = new();
        private uint nextFreeBufferId;
        private uint nextFreeEventId;
        public bool Initialize()
        {
            return true;
        }
        public void Dispose()
        {
            foreach (var entry in buffers)
            {
                Debug.Assert(entry.Value.IsCreated, "A buffer was unexpectedly not created.");
                entry.Value.Dispose();
            }
        }
        public BufferID CreateBuffer(UInt64 size)
        {
            Debug.Assert(size != 0, "Buffer size cannot be zero.");
            var buffer = new NativeArray<byte>((int)size, Allocator.Persistent);
            var idInteger = nextFreeBufferId++;
            var id = new BufferID(idInteger);
            buffers[id] = buffer;
            return id;
        }
        public void DestroyBuffer(BufferID id)
        {
            Debug.Assert(buffers.ContainsKey(id), "Invalid buffer ID given.");
            buffers[id].Dispose();
            buffers.Remove(id);
        }
        public void WriteBuffer(BufferID id, NativeArray<byte> data)
        {
            Debug.Assert(buffers.ContainsKey(id), "Invalid buffer ID given.");
            var buffer = buffers[id];
            buffer.CopyFrom(data);
        }
        public EventID EnqueueBufferWrite(BufferID id, NativeArray<byte> data)
        {
            WriteBuffer(id, data);
            return new EventID();
        }
        public void ReadBuffer(BufferID buffer, NativeArray<byte> result)
        {
            Debug.Assert(buffers.ContainsKey(buffer), "Invalid buffer ID given.");
            buffers[buffer].CopyTo(result);
        }
        public EventID EnqueueBufferRead(BufferID buffer, NativeArray<byte> result)
        {
            Debug.Assert(buffers.ContainsKey(buffer), "Invalid buffer ID given.");
            buffers[buffer].CopyTo(result);
            return new EventID { Value = nextFreeEventId++ };
        }
        public bool IsAsyncOperationComplete(EventID id)
        {
            return true;
        }
        public NativeArray<byte> GetNativeArray(BufferID id)
        {
            return buffers[id];
        }
    }
}
