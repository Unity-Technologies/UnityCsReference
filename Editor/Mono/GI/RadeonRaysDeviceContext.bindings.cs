// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.RenderPipelines.Core.Editor")]
namespace UnityEngine.LightTransport
{
    [StructLayout(LayoutKind.Sequential)]
    internal class RadeonRaysContext : IDeviceContext, IDisposable
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
        public static class BindingsMarshaller
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
        public unsafe extern EventID ReadBufferASync(BufferID id, void* result, int length);

        public unsafe EventID EnqueueBufferRead(BufferID id, NativeArray<byte> result)
        {
            void* ptr = NativeArrayUnsafeUtility.GetUnsafePtr(result);
            return ReadBufferASync(id, ptr, result.Length);
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern unsafe void ReadBufferBlocking(BufferID id, void* result, int length);

        public unsafe void ReadBuffer(BufferID id, NativeArray<byte> result)
        {
            void* ptr = NativeArrayUnsafeUtility.GetUnsafePtr(result);
            ReadBufferBlocking(id, ptr, result.Length);
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern unsafe EventID WriteBufferASync(BufferID id, void* result, int length);

        public unsafe EventID EnqueueBufferWrite(BufferID id, NativeArray<byte> data)
        {
            void* ptr = NativeArrayUnsafeUtility.GetUnsafePtr(data);
            return WriteBufferASync(id, ptr, data.Length);
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern unsafe void WriteBufferBlocking(BufferID id, void* result, int length);

        public unsafe void WriteBuffer(BufferID id, NativeArray<byte> data)
        {
            void* ptr = NativeArrayUnsafeUtility.GetUnsafePtr(data);
            WriteBufferBlocking(id, ptr, data.Length);
        }

        [NativeMethod(IsThreadSafe = true)]
        public extern bool IsAsyncOperationComplete(EventID id);

        [NativeMethod(IsThreadSafe = true)]
        internal static extern bool InitializePostProcessingInternal(RadeonRaysContext context);
        
        [NativeMethod(IsThreadSafe = true)]
        internal static extern bool ConvolveRadianceToIrradianceInternal(RadeonRaysContext context, BufferID radianceIn, BufferID irradianceOut, int probeCount);
        
        [NativeMethod(IsThreadSafe = true)]
        internal static extern bool ConvertToUnityFormatInternal(RadeonRaysContext context, BufferID irradianceIn, BufferID irradianceOut, int probeCount);
        
        [NativeMethod(IsThreadSafe = true)]
        internal static extern bool AddSphericalHarmonicsL2Internal(RadeonRaysContext context, BufferID a, BufferID b, BufferID sum, int probeCount);
    }
}
