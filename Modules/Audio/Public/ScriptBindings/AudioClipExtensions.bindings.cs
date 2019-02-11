// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEngine.Bindings;
using UnityEngine;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.UNode.Audio")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.UNode.Audio.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.Audio")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.Audio.Tests")]

namespace UnityEngine.Experimental.Audio
{
    [NativeHeader("Modules/Audio/Public/ScriptBindings/AudioClipExtensions.bindings.h")]
    [NativeHeader("Modules/Audio/Public/AudioClip.h")]
    [NativeHeader("AudioScriptingClasses.h")]
    internal static class AudioClipExtensions
    {
        public static AudioSampleProvider CreateAudioSampleProvider(
            this AudioClip audioClip, ulong startSampleFrameIndex = 0,
            long endSampleFrameIndex = 0, bool loop = false, bool allowDrop = false)
        {
            return AudioSampleProvider.Lookup(
                Internal_CreateAudioClipSampleProvider(
                    audioClip, startSampleFrameIndex, endSampleFrameIndex, loop, allowDrop),
                null, 0);
        }

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        extern private static uint Internal_CreateAudioClipSampleProvider(
            AudioClip audioClip, ulong start, long end, bool loop, bool allowDrop);
    }
}

