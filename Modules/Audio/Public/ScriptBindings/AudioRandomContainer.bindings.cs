// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using Unity.IntegerTime;

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
[NativeClass("AudioContainerElement", PersistentTypeId = 0x49805FF5)]
sealed class AudioContainerElement : Object
{
    internal AudioContainerElement(global::UnityEngine.EntityId id) : base(id) {}
    internal AudioContainerElement()
    {
        SetEntityIdFromConstructor(Internal_Create());
    }

    internal extern AudioClip audioClip { get; set; }
    internal extern float volume { get; set; }
    internal extern bool enabled { get; set; }

    static extern EntityId Internal_Create();
}

[NativeHeader("Modules/Audio/Public/AudioRandomContainer.h")]
[NativeClass("AudioRandomContainer", PersistentTypeId = 0x4DF5745F)]
[HelpURL("AudioRandomContainer-UI")]
[ExcludeFromPreset]
sealed class AudioRandomContainer : AudioResource, IAudioGenerator
{
    internal AudioRandomContainer(global::UnityEngine.EntityId id) : base(id) {}
    internal enum ChangeEventType
    {
        Volume,
        Pitch,
        List
    };

    internal AudioRandomContainer()
    {
        SetEntityIdFromConstructor(Internal_Create());
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

    static extern EntityId Internal_Create();

    #region IAudioGenerator

    bool GeneratorInstance.ICapabilities.isFinite => throw new NotImplementedException();
    bool GeneratorInstance.ICapabilities.isRealtime => throw new NotImplementedException();
    DiscreteTime? GeneratorInstance.ICapabilities.length => throw new NotImplementedException();

    GeneratorInstance IAudioGenerator.CreateInstance(ControlContext context, AudioFormat? nestedFormat, GeneratorInstance.CreationParameters creationParameters)
    {
        throw new NotImplementedException();
    }

    #endregion
}
