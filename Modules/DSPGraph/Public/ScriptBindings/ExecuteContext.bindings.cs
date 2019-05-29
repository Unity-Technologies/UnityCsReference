// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEngine.Bindings;

namespace Unity.Audio
{
    [NativeType(Header = "Modules/DSPGraph/Public/ExecuteContext.bindings.h")]
    internal unsafe struct ExecuteContextInternal
    {
        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true)]
        public static extern unsafe void Internal_PostEvent(void* dspNodePtr, long eventTypeHashCode, void* eventPtr, int eventSize);
    }
}

