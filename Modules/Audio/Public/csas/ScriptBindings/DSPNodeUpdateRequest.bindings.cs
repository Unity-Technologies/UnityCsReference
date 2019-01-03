// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using Unity.Jobs;
using UnityEngine.Bindings;

namespace Unity.Experimental.Audio
{
    [NativeType(Header = "Modules/Audio/Public/csas/DSPNodeUpdateRequest.bindings.h")]
    internal partial struct DSPNodeUpdateRequestHandle
    {
        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe void* Internal_GetUpdateJobData(ref DSPNodeUpdateRequestHandle requestHandle);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern bool Internal_HasError(ref DSPNodeUpdateRequestHandle requestHandle);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_GetDSPNode(ref DSPNodeUpdateRequestHandle requestHandle, ref DSPNode node);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_GetFence(ref DSPNodeUpdateRequestHandle requestHandle, ref JobHandle fence);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_Dispose(ref DSPNodeUpdateRequestHandle requestHandle);
    }
}

