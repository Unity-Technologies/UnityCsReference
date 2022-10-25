// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using System.Runtime.CompilerServices;
using Unity.Burst;

namespace Unity.Collections.LowLevel.Unsafe
{
    [NativeHeader("Runtime/Export/BurstLike/BurstLike.bindings.h")]
    [StaticAccessor("BurstLike", StaticAccessorType.DoubleColon)]
    internal static partial class BurstLike
    {
        [ThreadSafe(ThrowsException = false)]
        [BurstAuthorizedExternalMethod]
        internal static extern int NativeFunctionCall_Int_IntPtr_IntPtr(IntPtr function, IntPtr p0, IntPtr p1, out int error);

        [ThreadSafe(ThrowsException = false)]
        [BurstAuthorizedExternalMethod]
        internal static extern IntPtr StaticDataGetOrCreate(int key, int sizeInBytes, out int error);
    }
}
