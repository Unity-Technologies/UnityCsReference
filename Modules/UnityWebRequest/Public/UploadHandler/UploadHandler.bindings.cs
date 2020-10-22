// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngineInternal;
using UnityEngine.Bindings;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Networking
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/UploadHandler/UploadHandler.h")]
    public class UploadHandler : IDisposable
    {
        [System.NonSerialized]
        internal IntPtr m_Ptr;

        [NativeMethod(IsThreadSafe = true)]
        private extern void Release();

        internal UploadHandler() {}

        ~UploadHandler()
        {
            Dispose();
        }

        public virtual void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Release();
                m_Ptr = IntPtr.Zero;
            }
        }

        public byte[] data
        {
            get
            {
                return GetData();
            }
        }

        public string contentType
        {
            get
            {
                return GetContentType();
            }
            set
            {
                SetContentType(value);
            }
        }

        public float progress
        {
            get
            {
                return GetProgress();
            }
        }

        internal virtual byte[] GetData() { return null; }
        internal virtual string GetContentType() { return InternalGetContentType(); }
        internal virtual void   SetContentType(string newContentType) { InternalSetContentType(newContentType); }
        internal virtual float  GetProgress() { return InternalGetProgress(); }

        [NativeMethod("GetContentType")]
        private extern string InternalGetContentType();

        [NativeMethod("SetContentType")]
        private extern void InternalSetContentType(string newContentType);

        [NativeMethod("GetProgress")]
        private extern float InternalGetProgress();
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/UploadHandler/UploadHandlerRaw.h")]
    public sealed class UploadHandlerRaw : UploadHandler
    {
        NativeArray<byte> m_Payload;

        private static extern unsafe IntPtr Create(UploadHandlerRaw self, byte* data, int dataLength);

        public UploadHandlerRaw(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Cannot create a data handler without payload data");
            m_Payload = new NativeArray<byte>(data, Allocator.Persistent);
            unsafe
            {
                m_Ptr = Create(this, (byte*)m_Payload.GetUnsafeReadOnlyPtr(), m_Payload.Length);
            }
        }

        public UploadHandlerRaw(NativeArray<byte> data, bool transferOwnership)
        {
            if (!data.IsCreated || data.Length == 0)
                throw new ArgumentException("Cannot create a data handler without payload data");
            if (transferOwnership)
                m_Payload = data;
            unsafe
            {
                m_Ptr = Create(this, (byte*)data.GetUnsafeReadOnlyPtr(), data.Length);
            }
        }

        public UploadHandlerRaw(NativeArray<byte>.ReadOnly data)
        {
            if (data.Length == 0)
                throw new ArgumentException("Cannot create a data handler without payload data");
            unsafe
            {
                m_Ptr = Create(this, (byte*)data.GetUnsafeReadOnlyPtr(), data.Length);
            }
        }

        internal override byte[] GetData()
        {
            if (m_Payload.IsCreated)
                return m_Payload.ToArray();
            return null;
        }

        public override void Dispose()
        {
            if (m_Payload.IsCreated)
                m_Payload.Dispose();
            base.Dispose();
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/UploadHandler/UploadHandlerFile.h")]
    public sealed class UploadHandlerFile : UploadHandler
    {
        [NativeThrows]
        private static extern IntPtr Create(UploadHandlerFile self, string filePath);

        public UploadHandlerFile(string filePath)
        {
            m_Ptr = Create(this, filePath);
        }
    }
}
