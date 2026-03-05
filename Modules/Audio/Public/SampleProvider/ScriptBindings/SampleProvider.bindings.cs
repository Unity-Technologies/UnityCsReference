// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Audio
{
    [NativeHeader("Modules/Audio/Public/SampleProvider/ScriptBindings/SampleProvider.bindings.h")]
    static class SampleProviderBindings
    {
        [NativeMethod(Name = "audio::CreateSampleProviderGeneratorHeader", IsFreeFunction = true, ThrowsException = true)]
        internal static extern unsafe void* CreateGeneratorHeader(AudioClip audioClip, void* resourceHeader, AudioConfiguration* nestedConfiguration);
    }
}
