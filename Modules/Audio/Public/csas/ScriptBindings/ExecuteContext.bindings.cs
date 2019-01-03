// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEngine.Bindings;

namespace Unity.Experimental.Audio
{
    [NativeType(Header = "Modules/Audio/Public/csas/ExecuteContext.bindings.h")]
    internal unsafe struct ExecuteContextInternal
    {
        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true)]
        static extern unsafe internal void Internal_PostEvent(void* dspNodePtr, long eventTypeHashCode, void* eventPtr, int eventSize);
    }
}

