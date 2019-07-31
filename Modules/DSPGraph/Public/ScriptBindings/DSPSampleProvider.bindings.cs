// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using UnityEngine.Bindings;

namespace Unity.Audio
{
    [NativeType(Header = "Modules/DSPGraph/Public/DSPSampleProvider.bindings.h")]
    internal partial struct DSPSampleProviderInternal
    {
        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe int Internal_ReadUInt8FromSampleProvider(void* provider, int format, void* buffer, int length);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe int Internal_ReadSInt16FromSampleProvider(void* provider, int format, void* buffer, int length);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe int Internal_ReadFloatFromSampleProvider(void* provider, void* buffer, int length);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe ushort Internal_GetChannelCount(void* provider);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe uint Internal_GetSampleRate(void* provider);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe int Internal_ReadUInt8FromSampleProviderById(uint providerId, int format, void* buffer, int length);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe int Internal_ReadSInt16FromSampleProviderById(uint providerId, int format, void* buffer, int length);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe int Internal_ReadFloatFromSampleProviderById(uint providerId, void* buffer, int length);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe ushort Internal_GetChannelCountById(uint providerId);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe uint Internal_GetSampleRateById(uint providerId);
    }
}

