// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Video
{
    [RequiredByNativeCode]
    public enum VideoRenderMode
    {
        CameraFarPlane   = 0,
        CameraNearPlane  = 1,
        RenderTexture    = 2,
        MaterialOverride = 3,
        APIOnly          = 4
    }

    [RequiredByNativeCode]
    public enum Video3DLayout
    {
        No3D         = 0,
        SideBySide3D = 1,
        OverUnder3D  = 2
    }

    [RequiredByNativeCode]
    public enum VideoAspectRatio
    {
        NoScaling       = 0,
        FitVertically   = 1,
        FitHorizontally = 2,
        FitInside       = 3,
        FitOutside      = 4,
        Stretch         = 5
    }

    [RequiredByNativeCode]
    public enum VideoTimeSource
    {
        AudioDSPTimeSource = 0,
        GameTimeSource     = 1
    }

    [RequiredByNativeCode]
    public enum VideoTimeReference
    {
        Freerun         = 0,
        InternalTime    = 1,
        ExternalTime    = 2
    }

    [RequiredByNativeCode]
    public enum VideoSource
    {
        VideoClip = 0,
        Url       = 1
    }

    [RequiredByNativeCode]
    public enum VideoAudioOutputMode
    {
        None        = 0,
        AudioSource = 1,
        Direct      = 2,
        APIOnly     = 3
    }

    [RequiredByNativeCode]
    [RequireComponent(typeof(Transform))]
    [NativeHeader("Modules/Video/Public/VideoPlayer.h")]
    public sealed class VideoPlayer : Behaviour
    {
        public extern VideoSource source { get; set; }

        [NativeName("VideoUrl")]
        public extern string url { get; set; }

        [NativeName("VideoClip")]
        public extern VideoClip clip { get; set; }

        public extern VideoRenderMode renderMode { get; set; }

        [NativeHeader("Runtime/Camera/Camera.h")]
        public extern Camera targetCamera { get; set; }

        [NativeHeader("Runtime/Graphics/RenderTexture.h")]
        public extern RenderTexture targetTexture { get; set; }

        [NativeHeader("Runtime/Graphics/Renderer.h")]
        public extern Renderer targetMaterialRenderer { get; set; }

        public extern string targetMaterialProperty { get; set; }

        public extern VideoAspectRatio aspectRatio { get; set; }

        public extern float targetCameraAlpha { get; set; }

        public extern Video3DLayout targetCamera3DLayout { get; set; }

        [NativeHeader("Runtime/Graphics/Texture.h")]
        public extern Texture texture { get; }

        public extern void Prepare();

        public extern bool isPrepared
        {
            [NativeName("IsPrepared")]
            get;
        }


        public extern bool waitForFirstFrame { get; set; }

        public extern bool playOnAwake { get; set; }

        public extern void Play();

        public extern void Pause();

        public extern void Stop();

        public extern bool isPlaying
        {
            [NativeName("IsPlaying")]
            get;
        }
        public extern bool isPaused
        {
            [NativeName("IsPaused")]
            get;
        }

        public extern bool canSetTime
        {
            [NativeName("CanSetTime")]
            get;
        }

        [NativeName("SecPosition")]
        public extern double time { get; set; }

        [NativeName("FramePosition")]
        public extern long frame { get; set; }

        public extern double clockTime { get; }

        public extern bool canStep
        {
            [NativeName("CanStep")]
            get;
        }

        public extern void StepForward();

        public extern bool canSetPlaybackSpeed
        {
            [NativeName("CanSetPlaybackSpeed")]
            get;
        }

        public extern float playbackSpeed { get; set; }

        [NativeName("Loop")]
        public extern bool isLooping { get; set; }

        public extern bool canSetTimeSource
        {
            [NativeName("CanSetTimeSource")]
            get;
        }

        public extern VideoTimeSource timeSource { get; set; }

        public extern VideoTimeReference timeReference { get; set; }

        public extern double externalReferenceTime { get; set; }

        public extern bool canSetSkipOnDrop
        {
            [NativeName("CanSetSkipOnDrop")]
            get;
        }

        public extern bool skipOnDrop { get; set; }

        public extern ulong frameCount { get; }

        public extern float frameRate { get; }

        [NativeName("Duration")]
        public extern double length
        {
            get;
        }

        public extern uint width { get; }

        public extern uint height { get; }

        public extern uint pixelAspectRatioNumerator { get; }

        public extern uint pixelAspectRatioDenominator { get; }

        public extern ushort audioTrackCount { get; }

        public extern string GetAudioLanguageCode(ushort trackIndex);

        public extern ushort GetAudioChannelCount(ushort trackIndex);

        public extern uint GetAudioSampleRate(ushort trackIndex);

        public static extern ushort controlledAudioTrackMaxCount { get; }

        public ushort controlledAudioTrackCount
        {
            get
            {
                return GetControlledAudioTrackCount();
            }

            set
            {
                int maxNumTracks = controlledAudioTrackMaxCount;
                if (value > maxNumTracks)
                    throw new ArgumentException(string.Format("Cannot control more than {0} tracks.", maxNumTracks), "value");

                SetControlledAudioTrackCount(value);
            }
        }

        private extern ushort GetControlledAudioTrackCount();

        private extern void SetControlledAudioTrackCount(ushort value);

        public extern void EnableAudioTrack(ushort trackIndex, bool enabled);

        public extern bool IsAudioTrackEnabled(ushort trackIndex);

        public extern VideoAudioOutputMode audioOutputMode { get; set; }

        public extern bool canSetDirectAudioVolume
        {
            [NativeName("CanSetDirectAudioVolume")]
            get;
        }

        public extern float GetDirectAudioVolume(ushort trackIndex);

        public extern void SetDirectAudioVolume(ushort trackIndex, float volume);

        public extern bool GetDirectAudioMute(ushort trackIndex);

        public extern void SetDirectAudioMute(ushort trackIndex, bool mute);

        [NativeHeader("Modules/Audio/Public/AudioSource.h")]
        public extern AudioSource GetTargetAudioSource(ushort trackIndex);

        public extern void SetTargetAudioSource(ushort trackIndex, AudioSource source);

        public delegate void EventHandler(VideoPlayer source);
        public delegate void ErrorEventHandler(VideoPlayer source, string message);
        public delegate void FrameReadyEventHandler(VideoPlayer source, long frameIdx);
        public delegate void TimeEventHandler(VideoPlayer source, double seconds);

        public event EventHandler prepareCompleted;
        public event EventHandler loopPointReached;
        public event EventHandler started;
        public event EventHandler frameDropped;
        public event ErrorEventHandler errorReceived;
        public event EventHandler seekCompleted;
        public event TimeEventHandler clockResyncOccurred;

        public extern bool sendFrameReadyEvents
        {
            [NativeName("AreFrameReadyEventsEnabled")]
            get;
            [NativeName("EnableFrameReadyEvents")]
            set;
        }

        public event FrameReadyEventHandler frameReady;

        [RequiredByNativeCode]
        private static void InvokePrepareCompletedCallback_Internal(VideoPlayer source)
        {
            if (source.prepareCompleted != null)
                source.prepareCompleted(source);
        }

        [RequiredByNativeCode]
        private static void InvokeFrameReadyCallback_Internal(VideoPlayer source, long frameIdx)
        {
            if (source.frameReady != null)
                source.frameReady(source, frameIdx);
        }

        [RequiredByNativeCode]
        private static void InvokeLoopPointReachedCallback_Internal(VideoPlayer source)
        {
            if (source.loopPointReached != null)
                source.loopPointReached(source);
        }

        [RequiredByNativeCode]
        private static void InvokeStartedCallback_Internal(VideoPlayer source)
        {
            if (source.started != null)
                source.started(source);
        }

        [RequiredByNativeCode]
        private static void InvokeFrameDroppedCallback_Internal(VideoPlayer source)
        {
            if (source.frameDropped != null)
                source.frameDropped(source);
        }

        [RequiredByNativeCode]
        private static void InvokeErrorReceivedCallback_Internal(VideoPlayer source, string errorStr)
        {
            if (source.errorReceived != null)
                source.errorReceived(source, errorStr);
        }

        [RequiredByNativeCode]
        private static void InvokeSeekCompletedCallback_Internal(VideoPlayer source)
        {
            if (source.seekCompleted != null)
                source.seekCompleted(source);
        }

        [RequiredByNativeCode]
        private static void InvokeClockResyncOccurredCallback_Internal(VideoPlayer source, double seconds)
        {
            if (source.clockResyncOccurred != null)
                source.clockResyncOccurred(source, seconds);
        }
    }
}
