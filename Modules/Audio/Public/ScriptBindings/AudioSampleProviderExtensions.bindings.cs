// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using UnityEngine.Bindings;

[assembly: InternalsVisibleTo("Unity.Audio.Tests")]

namespace UnityEngine.Experimental.Audio
{
    [NativeHeader("Modules/Audio/Public/ScriptBindings/AudioSampleProviderExtensions.bindings.h")]
    [StaticAccessor("AudioSampleProviderExtensionsBindings", StaticAccessorType.DoubleColon)]
    internal static class AudioSampleProviderExtensionsInternal
    {
        public static float GetSpeed(this AudioSampleProvider provider)
        {
            return InternalGetAudioSampleProviderSpeed(provider.id);
        }

        [NativeMethod(IsThreadSafe = true, ThrowsException = true)]
        private extern static float InternalGetAudioSampleProviderSpeed(uint providerId);
    }
}
