// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.LightTransport
{
    [DebuggerDisplay("BufferID({Value})")]
    public struct BufferID : IEquatable<BufferID>
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

        // Value type semantics
        public override int GetHashCode() => Value.GetHashCode();
        public bool Equals(BufferID other) => other.Value == Value;
        public override bool Equals(object obj) => obj is BufferID other && Equals(other);
        public static bool operator ==(BufferID a, BufferID b) => a.Equals(b);
        public static bool operator !=(BufferID a, BufferID b) => !a.Equals(b);

    }
    [DebuggerDisplay("BufferSlice(Id: {Id.Value}, Offset: {Offset})")]
    public struct BufferSlice<T> : IEquatable<BufferSlice<T>>
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

        // Value type semantics
        public override int GetHashCode() => HashCode.Combine(Id, Offset);
        public bool Equals(BufferSlice<T> other) => other.Id == Id && other.Offset == Offset;
        public override bool Equals(object obj) => obj is BufferSlice<T> other && Equals(other);
        public static bool operator ==(BufferSlice<T> a, BufferSlice<T> b) => a.Equals(b);
        public static bool operator !=(BufferSlice<T> a, BufferSlice<T> b) => !a.Equals(b);
    }
    [DebuggerDisplay("EventID({Value})")]
    public struct EventID : IEquatable<EventID>
    {
        public UInt64 Value;
        public EventID(UInt64 value)
        {
            Value = value;
        }

        // Value type semantics
        public override int GetHashCode() => Value.GetHashCode();
        public bool Equals(EventID other) => other.Value == Value;
        public override bool Equals(object obj) => obj is EventID other && Equals(other);
        public static bool operator ==(EventID a, EventID b) => a.Equals(b);
        public static bool operator !=(EventID a, EventID b) => !a.Equals(b);
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
        void WriteBuffer<T>(BufferSlice<T> dst, NativeArray<T> src) where T : struct;
        void ReadBuffer<T>(BufferSlice<T> src, NativeArray<T> dst) where T : struct;
        void WriteBuffer<T>(BufferSlice<T> dst, NativeArray<T> src, EventID id) where T : struct;
        void ReadBuffer<T>(BufferSlice<T> src, NativeArray<T> dst, EventID id) where T : struct;
        EventID CreateEvent();
        void DestroyEvent(EventID id);
        bool IsCompleted(EventID id);
        bool Wait(EventID id);
        bool Flush();
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
        public void WriteBuffer<T>(BufferSlice<T> dst, NativeArray<T> src)
            where T : struct
        {
            Debug.Assert(buffers.ContainsKey(dst.Id), "Invalid buffer ID given.");
            var dstBuffer = buffers[dst.Id].Reinterpret<T>(1);
            dstBuffer.GetSubArray((int)dst.Offset, dstBuffer.Length - (int)dst.Offset).CopyFrom(src);
        }
        public void ReadBuffer<T>(BufferSlice<T> src, NativeArray<T> dst)
            where T : struct
        {
            Debug.Assert(buffers.ContainsKey(src.Id), "Invalid buffer ID given.");
            var srcBuffer = buffers[src.Id].Reinterpret<T>(1);
            dst.CopyFrom(srcBuffer.GetSubArray((int)src.Offset, srcBuffer.Length - (int)src.Offset));
        }
        public void WriteBuffer<T>(BufferSlice<T> dst, NativeArray<T> src, EventID id)
            where T : struct
        {
            WriteBuffer(dst, src);
        }
        public void ReadBuffer<T>(BufferSlice<T> src, NativeArray<T> dst, EventID id)
            where T : struct
        {
            ReadBuffer(src, dst);
        }
        public EventID CreateEvent()
        {
            return new EventID(nextFreeEventId++);
        }
        public void DestroyEvent(EventID id)
        {
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
