// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.LightTransport
{
    public struct BufferID
    {
        public UInt64 Value;
        public BufferID(UInt64 value)
        {
            Value = value;
        }

        public BufferSlice<T> Slice<T>(UInt64 offset = 0)
            where T : struct
        {
            return new BufferSlice<T>(this, offset);
        }
    }
    public struct BufferSlice<T>
        where T : struct
    {
        public BufferSlice(BufferID id, UInt64 offset)
        {
            Id = id;
            Offset = offset;
        }
        public BufferID Id;
        public UInt64 Offset;

        public BufferSlice<U> SafeReinterpret<U>()
            where U : struct
        {
            if (!UnsafeUtility.IsBlittable<U>())
                throw new ArgumentException($"Type {typeof(U)} must be blittable. {UnsafeUtility.GetReasonForTypeNonBlittable(typeof(U))}.");

            int oldSize = UnsafeUtility.SizeOf<T>();
            int newSize = UnsafeUtility.SizeOf<U>();
            if (oldSize % newSize != 0)
                throw new ArgumentException($"Type {typeof(T)} size must be a multiple of {typeof(U)} size.");

            return UnsafeReinterpret<U>();
        }

        public unsafe BufferSlice<U> UnsafeReinterpret<U>()
            where U : struct
        {
            int oldSize = UnsafeUtility.SizeOf<T>();
            int newSize = UnsafeUtility.SizeOf<U>();

            UInt64 newOffset = (Offset * (UInt64)oldSize) / (UInt64)newSize;

            return new BufferSlice<U>(Id, newOffset);
        }
    }
    public struct EventID
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
    public interface IDeviceContext : IDisposable
    {
        bool Initialize();
        BufferID CreateBuffer(UInt64 size);
        void DestroyBuffer(BufferID id);
        EventID WriteBuffer<T>(BufferSlice<T> dst, NativeArray<T> src) where T : struct;
        EventID ReadBuffer<T>(BufferSlice<T> src, NativeArray<T> dst) where T : struct;
        bool IsCompleted(EventID id);
        bool Wait(EventID id);
        bool Flush();
    }
    public static class DeviceContextExtensions
    {
        public static EventID WriteBuffer<T>(this IDeviceContext context, BufferID dst, NativeArray<T> src)
            where T : struct => context.WriteBuffer(new BufferSlice<T>(dst, 0), src);

        public static EventID ReadBuffer<T>(this IDeviceContext context, BufferID src, NativeArray<T> dst)
            where T : struct => context.ReadBuffer(new BufferSlice<T>(src, 0), dst);
    }
    [StructLayout(LayoutKind.Sequential)]
    public class ReferenceContext : IDeviceContext
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
        public EventID WriteBuffer<T>(BufferSlice<T> dst, NativeArray<T> src)
            where T : struct
        {
            Debug.Assert(buffers.ContainsKey(dst.Id), "Invalid buffer ID given.");
            var dstBuffer = buffers[dst.Id].Reinterpret<T>(1);
            dstBuffer.GetSubArray((int)dst.Offset, dstBuffer.Length - (int)dst.Offset).CopyFrom(src);
            return new EventID();
        }
        public EventID ReadBuffer<T>(BufferSlice<T> src, NativeArray<T> dst)
            where T : struct
        {
            Debug.Assert(buffers.ContainsKey(src.Id), "Invalid buffer ID given.");
            var srcBuffer = buffers[src.Id].Reinterpret<T>(1);
            dst.CopyFrom(srcBuffer.GetSubArray((int)src.Offset, srcBuffer.Length - (int)src.Offset));
            return new EventID { Value = nextFreeEventId++ };
        }
        public bool IsCompleted(EventID id)
        {
            return true;
        }
        public bool Wait(EventID id)
        {
            return true;
        }
        public NativeArray<byte> GetNativeArray(BufferID id)
        {
            return buffers[id];
        }

        public bool Flush()
        {
            return true;
        }
    }
}
