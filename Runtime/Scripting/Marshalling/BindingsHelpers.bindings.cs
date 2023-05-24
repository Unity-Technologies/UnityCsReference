// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEngine.Bindings
{
    [VisibleToOtherModules]
    [NativeHeader("Runtime/Scripting/Marshalling/BindingsAllocator.h")]
    [StaticAccessor("Marshalling::BindingsAllocator", StaticAccessorType.DoubleColon)]
    internal static unsafe class BindingsAllocator
    {
        [ThreadSafe]
        public static extern void* Malloc(int size);
        [ThreadSafe]
        public static extern void Free(void* ptr);

        [ThreadSafe]
        public static extern void FreeNativeOwnedMemory(void* ptr);

        private struct NativeOwnedMemory
        {
#pragma warning disable CS0649
            public void* data;
#pragma warning restore CS0649
            // MemLabelId
        }

        public static void* GetNativeOwnedDataPointer(void* ptr)
        {
            return ((NativeOwnedMemory*)ptr)->data;
        }
    }
}
