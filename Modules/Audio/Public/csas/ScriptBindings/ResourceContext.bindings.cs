// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using UnityEngine.Bindings;

namespace Unity.Audio
{
    [NativeType(Header = "Modules/Audio/Public/csas/ResourceContext.bindings.h")]
    internal struct ResourceContextInternal
    {
        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true)]
        public static extern unsafe void* Internal_AllocateArray(void* dspNodePtr, int size);

        [NativeMethod(IsThreadSafe = true, IsFreeFunction = true)]
        public static extern unsafe void Internal_FreeArray(void* dspNodePtr, void* memory);
    }
}

