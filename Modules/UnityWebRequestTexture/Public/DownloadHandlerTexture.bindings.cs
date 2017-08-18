// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngineInternal;

namespace UnityEngine.Networking
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequestTexture/Public/DownloadHandlerTexture.h")]
    public sealed class DownloadHandlerTexture : DownloadHandler
    {
        private Texture2D mTexture;
        private bool mHasTexture;
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

        protected override byte[] GetData()
        {
            return InternalGetByteArray(this);
        }

        public Texture2D texture
        {
            get { return InternalGetTexture(); }
        }

        private Texture2D InternalGetTexture()
        {
            if (mHasTexture)
            {
                if (mTexture == null)
                {
                    // this is corner case when this DH survives scene reload, while texture does not
                    mTexture = new Texture2D(2, 2);
                    mTexture.LoadImage(GetData(), mNonReadable);
                }
            }
            else if (mTexture == null)
            {
                mTexture = InternalGetTextureNative();
                mHasTexture = true;
            }

            return mTexture;
        }

        [NativeThrows]
        private extern Texture2D InternalGetTextureNative();

        public static Texture2D GetContent(UnityWebRequest www)
        {
            return GetCheckedDownloader<DownloadHandlerTexture>(www).texture;
        }

    }
}
