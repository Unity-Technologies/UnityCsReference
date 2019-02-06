// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEngine.Bindings;

namespace Unity.Experimental.Audio
{
    [NativeType(Header = "Modules/Audio/Public/csas/DSPCommandBlock.bindings.h")]
    internal partial struct DSPCommandBlock
    {
        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void Internal_CreateDSPNode(ref DSPCommandBlock block, ref DSPNode node, void* jobReflectionData, void* jobMemory, void* parameterDescriptionArray, int parameterCount, void* sampleProviderDescriptionArray, int sampleProviderCount);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void Internal_SetFloat(ref DSPCommandBlock block, ref DSPNode node, void* jobReflectionData, uint pIndex, float value, uint interpolationLength);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void Internal_AddFloatKey(ref DSPCommandBlock block, ref DSPNode node, void* jobReflectionData, uint pIndex, ulong dspClock, float value);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void Internal_SustainFloat(ref DSPCommandBlock block, ref DSPNode node, void* jobReflectionData, uint pIndex, ulong dspClock);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void Internal_UpdateAudioJob(ref DSPCommandBlock block, ref DSPNode node, void* updateJobMem, void* updateJobReflectionData, void* nodeReflectionData);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void Internal_CreateUpdateRequest(
            ref DSPCommandBlock block, ref DSPNode node, ref DSPNodeUpdateRequestHandle request, object callback,
            void* updateJobMem, void* updateJobReflectionData, void* nodeReflectionData);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_ReleaseDSPNode(ref DSPCommandBlock block, ref DSPNode node);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_Connect(ref DSPCommandBlock block, ref DSPNode output, int outputPort, ref DSPNode input, int inputPort, ref DSPConnection connection);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_Disconnect(ref DSPCommandBlock block, ref DSPNode output, int outputPort, ref DSPNode input, int inputPort);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_DisconnectByHandle(ref DSPCommandBlock block, ref DSPConnection connection);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void Internal_SetAttenuation(ref DSPCommandBlock block, ref DSPConnection connection, void* value, byte dimension, uint interpolationLength);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void Internal_AddAttenuationKey(ref DSPCommandBlock block, ref DSPConnection connection, ulong dspClock, void* value, byte dimension);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_SustainAttenuation(ref DSPCommandBlock block, ref DSPConnection connection, ulong dspClock);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_AddInletPort(ref DSPCommandBlock block, ref DSPNode node, int channelCount, SoundFormat format);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_AddOutletPort(ref DSPCommandBlock block, ref DSPNode node, int channelCount, SoundFormat format);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void Internal_SetSampleProvider(ref DSPCommandBlock block, ref DSPNode node, int item, int index, uint audioSampleProviderId);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void Internal_InsertSampleProvider(ref DSPCommandBlock block, ref DSPNode node, int item, int index, uint audioSampleProviderId);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern unsafe void Internal_RemoveSampleProvider(ref DSPCommandBlock block, ref DSPNode node, int item, int index);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        static extern void Internal_Complete(ref DSPCommandBlock block);
    }
}

