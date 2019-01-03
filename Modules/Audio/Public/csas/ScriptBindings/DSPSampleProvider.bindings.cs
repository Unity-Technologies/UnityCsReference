// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEngine.Bindings;

namespace Unity.Experimental.Audio
{
    [NativeType(Header = "Modules/Audio/Public/csas/DSPSampleProvider.bindings.h")]
    internal partial struct DSPSampleProvider
    {
        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe internal int Internal_ReadUInt8FromSampleProvider(
            DSPSampleProvider provider, SampleProvider.NativeFormatType format, void* buffer, int length);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe internal int Internal_ReadSInt16FromSampleProvider(
            DSPSampleProvider provider, SampleProvider.NativeFormatType format, void* buffer, int length);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe internal int Internal_ReadFloatFromSampleProvider(
            DSPSampleProvider provider, void* buffer, int length);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe internal ushort Internal_GetChannelCount(DSPSampleProvider provider);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe internal uint Internal_GetSampleRate(DSPSampleProvider provider);
    }
}

