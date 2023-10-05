// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

namespace UnityEngine.LightTransport
{
    [StructLayout(LayoutKind.Sequential)]
    internal class WintermuteContext : IDeviceContext
    {
        internal IntPtr m_Ptr;
        internal bool m_OwnsPtr;
        private Dictionary<BufferID, NativeArray<byte>> buffers = new();
        private uint nextFreeBufferId;
        private uint nextFreeEventId;
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(WintermuteContext obj) => obj.m_Ptr;
        }

        [NativeMethod(IsThreadSafe = true)]
        static extern IntPtr Internal_Create();

        [NativeMethod(IsThreadSafe = true)]
        static extern void Internal_Destroy(IntPtr ptr);

        public WintermuteContext()
        {
            m_Ptr = Internal_Create();
            m_OwnsPtr = true;
        }
        public WintermuteContext(IntPtr ptr)
        {
            m_Ptr = ptr;
            m_OwnsPtr = false;
        }
        ~WintermuteContext()
        {
            Destroy();
        }
        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }
        void Destroy()
        {
            if (m_OwnsPtr && m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern bool Initialize();

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
        
        public EventID WriteBuffer(BufferID id, NativeArray<byte> data)
        {
            Debug.Assert(buffers.ContainsKey(id), "Invalid buffer ID given.");
            var buffer = buffers[id];
            buffer.CopyFrom(data);
            return new EventID();
        }

        public EventID ReadBuffer(BufferID buffer, NativeArray<byte> result)
        {
            Debug.Assert(buffers.ContainsKey(buffer), "Invalid buffer ID given.");
            buffers[buffer].CopyTo(result);
            return new EventID { Value = nextFreeEventId++ };
        }

        public bool IsAsyncOperationComplete(EventID id)
        {
            return true;
        }

        public bool WaitForAsyncOperation(EventID id)
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

        [NativeMethod(IsThreadSafe = true)]
        internal static unsafe extern bool DeringSphericalHarmonicsL2Internal(SphericalHarmonicsL2* shIn, SphericalHarmonicsL2* shOut, int probeCount);
    }
}
