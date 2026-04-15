// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEngine.Bindings
{
    [VisibleToOtherModules]
    [NativeHeader("Scripting/Marshalling/BindingsAllocator.h")]
    [StaticAccessor("Marshalling::BindingsAllocator", StaticAccessorType.DoubleColon)]
    internal static unsafe class BindingsAllocator
    {
        [NativeMethod(IsThreadSafe = true)]
        [NativeName("BindingsAllocator::Malloc")]
        static extern void* Malloc_Internal(int size);

        public static void* Malloc(int size)
        {
            if (size > 0)
                return Malloc_Internal(size);
            return null;
        }

        [NativeMethod(IsThreadSafe = true)]
        public static extern void MemClear(void* ptr, uint size);

        [NativeMethod(IsThreadSafe = true)]
        public static extern void Free(void* ptr);
        [NativeMethod(IsThreadSafe = true)]
        public static extern void* AllocateCoreString(string ptr);
        [NativeMethod(IsThreadSafe = true)]
        public static extern void FreeCoreString(void* ptr);
        [NativeMethod(IsThreadSafe = true)]
        public static extern int SizeOfCoreString();
        [NativeMethod(IsThreadSafe = true)]
        public static extern void SetCoreStringBuffer(void* buffer, string str);
        [NativeMethod(IsThreadSafe = true)]
        public static extern string GetStringForCoreString(void* str);

        public static ManagedSpanWrapper AllocateCoreStringArray(string[] str)
        {
            if (str == null) return default;
            var size = SizeOfCoreString();
            var buffer = Malloc(SizeOfCoreString() * str.Length);
            for (int i = 0; i < str.Length; i++)
            {
                var ptr = (byte*)buffer + size * i;
                SetCoreStringBuffer(ptr, str[i]);
            }
            return new ManagedSpanWrapper(buffer, str.Length);
        }

        [NativeMethod(IsThreadSafe = true)]
        public static extern void FreeCoreStringArray(void* buffer, int numberOfStrings);

        [NativeMethod(IsThreadSafe = true)]
        public static extern void FreeNativeOwnedMemory(void* ptr);

        public static void* AllocateZeroedBuffer(int size)
        {
            var buffer = Malloc(size);
            MemClear(buffer, (uint)size);
            return buffer;
        }

        public static T* AllocateAndCopyToBuffer<T>(Span<T> span, int allocLength) where T : unmanaged
        {
            if (allocLength < span.Length) throw new ArgumentException($"Not enough space in {allocLength} for span of size {span.Length}!");

            var size = sizeof(T) * allocLength;
            T* buffer = (T*)Malloc(size);
            span.CopyTo(new Span<T>(buffer, span.Length));
            return buffer;
        }

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

    [VisibleToOtherModules]
    internal static class SimpleThrowHelper
    {
        public static void ThrowArgumentNullException(object obj, string parameterName)
        {
            throw new ArgumentNullException(parameterName);
        }

        public static void ThrowNullReferenceException(object obj)
        {
            throw new NullReferenceException();
        }
    }
}
