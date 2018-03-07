// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Video
{
    [RequiredByNativeCode]
    [NativeHeader("Modules/Video/Public/VideoClip.h")]
    public sealed class VideoClip : Object
    {
        private VideoClip() {}

        public extern string originalPath { get; }

        public extern ulong frameCount { get; }

        public extern double frameRate { get; }

        [NativeName("Duration")]
        public extern double length { get; }

        public extern uint width { get; }

        public extern uint height { get; }

        public extern uint pixelAspectRatioNumerator { get; }

        public extern uint pixelAspectRatioDenominator { get; }

        public extern ushort audioTrackCount { get; }

        public extern ushort GetAudioChannelCount(ushort audioTrackIdx);

        public extern uint GetAudioSampleRate(ushort audioTrackIdx);

        public extern string GetAudioLanguage(ushort audioTrackIdx);
    }
}
