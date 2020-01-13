// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("VideoTesting")]
[assembly: InternalsVisibleTo("Unity.Audio.DSPGraph.Tests")]

namespace UnityEngineInternal.Video
{
    [UsedByNativeCode]
    [NativeHeader("Modules/Video/Public/Base/VideoMediaPlayback.h")]
    internal class VideoPlaybackMgr : IDisposable
    {
        internal IntPtr m_Ptr;
        public VideoPlaybackMgr()
        {
            m_Ptr = Internal_Create();
        }

        public void Dispose()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                Internal_Destroy(m_Ptr);
                m_Ptr = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        private static extern IntPtr Internal_Create();
        private static extern void Internal_Destroy(IntPtr ptr);

        public delegate void Callback();
        public delegate void MessageCallback(string message);
        extern public VideoPlayback CreateVideoPlayback(string fileName, MessageCallback errorCallback, Callback readyCallback, Callback reachedEndCallback, bool splitAlpha = false);
        extern public void ReleaseVideoPlayback(VideoPlayback playback);
        extern public ulong videoPlaybackCount { get; }
        extern public void Update();

        extern static internal void ProcessOSMainLoopMessagesForTesting();
    }
}

