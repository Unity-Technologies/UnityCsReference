// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEngine.Audio;

enum AudioRandomContainerTriggerMode
{
    Manual = 0,
    Automatic = 1
}

enum AudioRandomContainerPlaybackMode
{
    Sequential = 0,
    Shuffle = 1,
    Random = 2
}

enum AudioRandomContainerAutomaticTriggerMode
{
    Pulse = 0,
    Offset = 1
}

enum AudioRandomContainerLoopMode
{
    Infinite = 0,
    Clips = 1,
    Cycles = 2
}

[NativeHeader("Modules/Audio/Public/AudioContainerElement.h")]
sealed class AudioContainerElement : Object
{
    internal AudioContainerElement()
    {
        Internal_Create(this);
    }

    internal extern AudioClip audioClip { get; set; }
    internal extern float volume { get; set; }
    internal extern bool enabled { get; set; }

    static extern void Internal_Create([Writable] AudioContainerElement self);
}

[NativeHeader("Modules/Audio/Public/AudioRandomContainer.h")]
sealed class AudioRandomContainer : AudioResource
{
    internal enum ChangeEventType
    {
        Volume,
        Pitch,
        List
    };

    internal AudioRandomContainer()
    {
        Internal_Create(this);
    }

    internal extern float volume { get; set; }
    internal extern Vector2 volumeRandomizationRange { get; set; }
    internal extern bool volumeRandomizationEnabled { get; set; }

    internal extern float pitch { get; set; }
    internal extern Vector2 pitchRandomizationRange { get; set; }
    internal extern bool pitchRandomizationEnabled { get; set; }

    // Note: list changes will implicitly stop and reset playback
    internal extern AudioContainerElement[] elements { get; set; }

    internal extern AudioRandomContainerTriggerMode triggerMode { get; set; }
    internal extern AudioRandomContainerPlaybackMode playbackMode { get; set; }
    internal extern int avoidRepeatingLast { get; set; }

    internal extern AudioRandomContainerAutomaticTriggerMode automaticTriggerMode { get; set; }
    internal extern float automaticTriggerTime { get; set; }
    internal extern Vector2 automaticTriggerTimeRandomizationRange { get; set; }
    internal extern bool automaticTriggerTimeRandomizationEnabled { get; set; }

    internal extern AudioRandomContainerLoopMode loopMode { get; set; }
    internal extern int loopCount { get; set; }
    internal extern Vector2 loopCountRandomizationRange { get; set; }
    internal extern bool loopCountRandomizationEnabled { get; set; }

    // Note: list changes will implicitly stop and reset playback
    internal extern void NotifyObservers(ChangeEventType eventType);

    static extern void Internal_Create([Writable] AudioRandomContainer self);
}
