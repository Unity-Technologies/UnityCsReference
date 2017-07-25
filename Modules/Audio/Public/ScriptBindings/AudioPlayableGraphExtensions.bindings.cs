// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Audio;

namespace UnityEngine.Playables
{

    [NativeHeader("Modules/Audio/Public/ScriptBindings/AudioPlayableGraphExtensions.bindings.h")]
    [NativeHeader("Runtime/Director/Core/HPlayableOutput.h")]
    [StaticAccessor("AudioPlayableGraphExtensionsBindings", StaticAccessorType.DoubleColon)]
    internal static class AudioPlayableGraphExtensions
    {
        extern internal static bool InternalCreateAudioOutput(ref PlayableGraph graph, string name, out PlayableOutputHandle handle);
    }

}
