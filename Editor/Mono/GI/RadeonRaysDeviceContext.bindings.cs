// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;

namespace UnityEngine.LightTransport
{
    [StructLayout(LayoutKind.Sequential)]
    public class RadeonRaysContext : IDeviceContext, IDisposable
    {
        internal IntPtr m_Ptr;
        internal bool m_OwnsPtr;

        [NativeMethod(IsThreadSafe = true)]
        static extern IntPtr Internal_Create();

        [NativeMethod(IsThreadSafe = true)]
        static extern void Internal_Destroy(IntPtr ptr);

        public RadeonRaysContext()
        {
            m_Ptr = Internal_Create();
            m_OwnsPtr = true;
        }
        public RadeonRaysContext(IntPtr ptr)
        {
            m_Ptr = ptr;
            m_OwnsPtr = false;
        }
        ~RadeonRaysContext()
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
        internal static class BindingsMarshaller
        {
            public static IntPtr ConvertToNative(RadeonRaysContext obj) => obj.m_Ptr;
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern bool Initialize();

        [NativeMethod(IsThreadSafe = true)]
        public extern BufferID CreateBuffer(UInt64 size);

        [NativeMethod(IsThreadSafe = true)]
        public extern void DestroyBuffer(BufferID id);

        [NativeMethod(IsThreadSafe = true)]
        private unsafe extern EventID EnqueueBufferRead(BufferID id, void* result, UInt64 length, UInt64 offset);

        public unsafe EventID ReadBuffer<T>(BufferSlice<T> src, NativeArray<T> dst)
            where T: struct
        {
            void* ptr = NativeArrayUnsafeUtility.GetUnsafePtr(dst);
            return EnqueueBufferRead(src.Id, ptr, (UInt64)dst.Length * (UInt64)UnsafeUtility.SizeOf<T>(), src.Offset);
        }

        [NativeMethod(IsThreadSafe = true)]
        private extern unsafe EventID EnqueueBufferWrite(BufferID id, void* result, UInt64 length, UInt64 offset);

        public unsafe EventID WriteBuffer<T>(BufferSlice<T> dst, NativeArray<T> src)
            where T: struct
        {
            void* ptr = NativeArrayUnsafeUtility.GetUnsafePtr(src);
            return EnqueueBufferWrite(dst.Id, ptr, (UInt64)src.Length * (UInt64)UnsafeUtility.SizeOf<T>(), dst.Offset);
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern bool IsCompleted(EventID id);

        [NativeMethod(IsThreadSafe = true)]
		public extern bool Wait(EventID id);

		[NativeMethod(IsThreadSafe = true)]
        public extern bool Flush();

        [NativeMethod(IsThreadSafe = true)]
        internal static extern bool InitializePostProcessingInternal(RadeonRaysContext context);
        
        [NativeMethod(IsThreadSafe = true)]
        internal static extern bool ConvolveRadianceToIrradianceInternal(RadeonRaysContext context, BufferID radianceIn, BufferID irradianceOut, int probeCount);
        
        [NativeMethod(IsThreadSafe = true)]
        internal static extern bool ConvertToUnityFormatInternal(RadeonRaysContext context, BufferID irradianceIn, BufferID irradianceOut, int probeCount);
        
        [NativeMethod(IsThreadSafe = true)]
        internal static extern bool AddSphericalHarmonicsL2Internal(RadeonRaysContext context, BufferID a, BufferID b, BufferID sum, int probeCount);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern bool ScaleSphericalHarmonicsL2Internal(RadeonRaysContext context, BufferID shIn, BufferID shOut, int probeCount, float scale);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern bool WindowSphericalHarmonicsL2Internal(RadeonRaysContext context, BufferID shIn, BufferID shOut, int probeCount);
    }
}
