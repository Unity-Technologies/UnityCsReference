// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngineInternal;
using UnityEngine.Bindings;

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

        public void Dispose()
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
        internal virtual string GetContentType() { return "text/plain"; }
        internal virtual void   SetContentType(string newContentType) {}
        internal virtual float  GetProgress() { return 0.5f; }
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequest/Public/UploadHandler/UploadHandlerRaw.h")]
    public sealed class UploadHandlerRaw : UploadHandler
    {
        private static extern IntPtr Create(UploadHandlerRaw self, byte[] data);

        public UploadHandlerRaw(byte[] data)
        {
            if (data != null && data.Length == 0)
                throw new ArgumentException("Cannot create a data handler without payload data");
            m_Ptr = Create(this, data);
        }

        [NativeMethod("GetContentType")]
        private extern string InternalGetContentType();

        [NativeMethod("SetContentType")]
        private extern void InternalSetContentType(string newContentType);

        private extern byte[] InternalGetData();

        [NativeMethod("GetProgress")]
        private extern float InternalGetProgress();

        internal override string GetContentType() { return InternalGetContentType(); }

        internal override void SetContentType(string newContentType)
        {
            InternalSetContentType(newContentType);
        }

        internal override byte[] GetData()
        {
            return InternalGetData();
        }

        internal override float GetProgress()
        {
            return InternalGetProgress();
        }

    }
}
