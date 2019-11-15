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

        public DownloadHandlerAudioClip(Uri uri, AudioType audioType)
        {
            InternalCreateAudioClip(uri.AbsoluteUri, audioType);
        }

        protected override byte[] GetData()
        {
            return InternalGetByteArray(this);
        }

        protected override string GetText()
        {
            throw new System.NotSupportedException("String access is not supported for audio clips");
        }

        [NativeThrows]
        public extern AudioClip audioClip { get; }

        public extern bool streamAudio { get; set; }

        public extern bool compressed { get; set; }

        public static AudioClip GetContent(UnityWebRequest www)
        {
            return GetCheckedDownloader<DownloadHandlerAudioClip>(www).audioClip;
        }

    }

    [System.Obsolete("MovieTexture is deprecated. Use VideoPlayer instead.", true)]
    [StructLayout(LayoutKind.Sequential)]
    public sealed class DownloadHandlerMovieTexture : DownloadHandler
    {
        public DownloadHandlerMovieTexture()
        {
            FeatureRemoved();
        }

        protected override byte[] GetData()
        {
            FeatureRemoved();
            return null;
        }

        protected override string GetText()
        {
            throw new System.NotSupportedException("String access is not supported for movies");
        }

        public MovieTexture movieTexture { get { FeatureRemoved(); return null; } }

        public static MovieTexture GetContent(UnityWebRequest uwr)
        {
            FeatureRemoved();
            return null;
        }

        static void FeatureRemoved()
        {
            throw new Exception("Movie texture has been removed, use VideoPlayer instead");
        }

    }
}
