// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Bindings
{
    [VisibleToOtherModules]
    [NativeHeader("Runtime/Scripting/Marshalling/BindingsAllocator.h")]
    [StaticAccessor("BindingsAllocator", StaticAccessorType.DoubleColon)]
    internal static unsafe class BindingsAllocator
    {
        [ThreadSafe]
        public static extern void* Malloc(int size);
        [ThreadSafe]
        public static extern void Free(void* ptr);
    }
}
