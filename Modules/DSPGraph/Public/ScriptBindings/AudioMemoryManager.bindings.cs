// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine.Bindings;

namespace Unity.Audio
{
    [NativeType(Header = "Modules/DSPGraph/Public/AudioMemoryManager.bindings.h")]
    internal struct AudioMemoryManager
    {
        [NativeMethod(IsFreeFunction = true, ThrowsException = false)]
        public static extern unsafe void* Internal_AllocateAudioMemory(int size, int alignment);

        [NativeMethod(IsFreeFunction = true, ThrowsException = false)]
        public static extern unsafe void Internal_FreeAudioMemory(void* memory);
    }
}

