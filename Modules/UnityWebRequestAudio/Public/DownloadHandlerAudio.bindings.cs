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
    [NativeHeader("Modules/UnityWebRequestAudio/Public/DownloadHandlerAudioClip.h")]
    public sealed class DownloadHandlerAudioClip : DownloadHandler
    {
        private extern static IntPtr Create(DownloadHandlerAudioClip obj, string url, AudioType audioType);

        private void InternalCreateAudioClip(string url, AudioType audioType)
        {
            m_Ptr = Create(this, url, audioType);
        }

        public DownloadHandlerAudioClip(string url, AudioType audioType)
        {
            InternalCreateAudioClip(url, audioType);
        }

        protected override byte[] GetData()
        {
            return InternalGetByteArray(this);
        }

        protected override string GetText()
        {
            throw new System.NotSupportedException("String access is not supported for audio clips");
        }

        public extern AudioClip audioClip { get; }

        public static AudioClip GetContent(UnityWebRequest www)
        {
            return GetCheckedDownloader<DownloadHandlerAudioClip>(www).audioClip;
        }

    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/UnityWebRequestAudio/Public/DownloadHandlerMovieTexture.h")]
    [NativeHeader("Runtime/Video/MovieTexture.h")]  // for BIND_MANAGED_TYPE_NAME
    public sealed class DownloadHandlerMovieTexture : DownloadHandler
    {
        public DownloadHandlerMovieTexture()
        {
            InternalCreateDHMovieTexture();
        }

        private extern static IntPtr Create(DownloadHandlerMovieTexture obj);

        private void InternalCreateDHMovieTexture()
        {
            m_Ptr = Create(this);
        }

        protected override byte[] GetData()
        {
            return InternalGetByteArray(this);
        }

        protected override string GetText()
        {
            throw new System.NotSupportedException("String access is not supported for movies");
        }

        public extern MovieTexture movieTexture { get; }

        public static MovieTexture GetContent(UnityWebRequest uwr)
        {
            return GetCheckedDownloader<DownloadHandlerMovieTexture>(uwr).movieTexture;
        }

    }
}
