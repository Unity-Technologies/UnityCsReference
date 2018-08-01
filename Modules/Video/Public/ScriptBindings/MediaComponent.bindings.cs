// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo("VideoTesting")]

namespace UnityEngineInternal.Video
{
    [UsedByNativeCode]
    internal enum VideoError
    {
        NoErr                = 0,
        OutOfMemoryErr       = 1,
        CantReadFile         = 2,
        CantWriteFile        = 3,
        BadParams            = 4,
        NoData               = 5,
        BadPermissions       = 6,
        DeviceNotAvailable   = 7,
        ResourceNotAvailable = 8,
        NetworkErr           = 9
    }

    [UsedByNativeCode]
    internal enum VideoPixelFormat
    {
        RGB  = 0,
        RGBA = 1,
        YUV  = 2,
        YUVA = 3
    }

    [UsedByNativeCode]
    [NativeHeader("Modules/Video/Public/Base/MediaComponent.h")]
    internal class VideoPlayback
    {
#pragma warning disable 0649  // Field m_Ptr is never assigned to, and will always have its default value
        internal IntPtr m_Ptr;
#pragma warning restore 0649

        extern public void StartPlayback();
        extern public void PausePlayback();
        extern public void StopPlayback();

        extern public VideoError GetStatus();
        extern public bool IsReady();
        extern public bool IsPlaying();

        extern public void Step();
        extern public bool CanStep();

        extern public uint GetWidth();
        extern public uint GetHeight();
        extern public float GetFrameRate();
        extern public float GetDuration();
        extern public ulong GetFrameCount();
        extern public uint GetPixelAspectRatioNumerator();
        extern public uint GetPixelAspectRatioDenominator();
        extern public VideoPixelFormat GetPixelFormat();

        extern public bool CanNotSkipOnDrop();
        extern public void SetSkipOnDrop(bool skipOnDrop);
        extern public bool GetTexture(Texture texture, out long outputFrameNum);

        public delegate void Callback();
        extern public void SeekToFrame(long frameIndex, Callback seekCompletedCallback);
        extern public void SeekToTime(double secs, Callback seekCompletedCallback);

        extern public float GetPlaybackSpeed();
        extern public void SetPlaybackSpeed(float value);
        extern public bool GetLoop();
        extern public void SetLoop(bool value);
    }
}

