// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngineInternal;
using Unity.Collections;

namespace UnityEngine.Networking
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequestTexture/Public/DownloadHandlerTexture.h")]
    public sealed class DownloadHandlerTexture : DownloadHandler
    {
        private NativeArray<byte> m_NativeData;
        private bool mNonReadable;

        private static extern IntPtr Create(DownloadHandlerTexture obj, bool readable);

        private void InternalCreateTexture(bool readable)
        {
            m_Ptr = Create(this, readable);
        }

        public DownloadHandlerTexture()
        {
            InternalCreateTexture(true);
        }

        public DownloadHandlerTexture(bool readable)
        {
            InternalCreateTexture(readable);
            mNonReadable = !readable;
        }

        protected override NativeArray<byte> GetNativeData()
        {
            return InternalGetNativeArray(this, ref m_NativeData);
        }

        public override void Dispose()
        {
            DisposeNativeArray(ref m_NativeData);
            base.Dispose();
        }

        public Texture2D texture
        {
            get { return InternalGetTextureNative(); }
        }

        [NativeThrows]
        private extern Texture2D InternalGetTextureNative();

        public static Texture2D GetContent(UnityWebRequest www)
        {
            return GetCheckedDownloader<DownloadHandlerTexture>(www).texture;
        }

    }
}
