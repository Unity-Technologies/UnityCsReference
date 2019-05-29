// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using Unity.Jobs;
using UnityEngine.Bindings;

namespace Unity.Audio
{
    [NativeType(Header = "Modules/DSPGraph/Public/DSPNodeUpdateRequest.bindings.h")]
    internal struct DSPNodeUpdateRequestHandleInternal
    {
        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern unsafe void* Internal_GetUpdateJobData(ref Handle graph, ref Handle requestHandle);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern bool Internal_HasError(ref Handle graph, ref Handle requestHandle);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_GetDSPNode(ref Handle graph, ref Handle requestHandle, ref Handle node);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_GetFence(ref Handle graph, ref Handle requestHandle, ref JobHandle fence);

        [NativeMethod(IsFreeFunction = true, ThrowsException = true)]
        public static extern void Internal_Dispose(ref Handle graph, ref Handle requestHandle);
    }
}

