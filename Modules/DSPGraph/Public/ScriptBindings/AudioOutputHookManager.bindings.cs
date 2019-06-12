// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine.Bindings;

namespace Unity.Audio
{
    [NativeType(Header = "Modules/DSPGraph/Public/AudioOutputHookManager.bindings.h")]
    internal struct AudioOutputHookManager
    {
        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe void Internal_CreateAudioOutputHook(out Handle outputHook, void* jobReflectionData, void* jobData);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe void Internal_DisposeAudioOutputHook(ref Handle outputHook);
    }
}

