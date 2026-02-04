// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using System.Runtime.CompilerServices;
using Unity.Burst;
using UnityEngine.Profiling;

namespace Unity.Collections.LowLevel.Unsafe
{
    [NativeHeader("Runtime/Export/Unsafe/UnsafeUtility.bindings.h")]
    [StaticAccessor("UnsafeUtility", StaticAccessorType.DoubleColon)]
    public static partial class UnsafeUtility
    {
        [ThreadSafe]
        extern static int GetFieldOffsetInStruct(FieldInfo field);

        [ThreadSafe]
        extern static int GetFieldOffsetInClass(FieldInfo field);

        public static int GetFieldOffset(FieldInfo field)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));
            if (field.DeclaringType.IsValueType)
                return GetFieldOffsetInStruct(field);
            else if (field.DeclaringType.IsClass)
                return GetFieldOffsetInClass(field);
            else
            {
                throw new ArgumentException("field.DeclaringType must be a struct or class");
            }
        }

        [Obsolete("Use GCHandle.Alloc with GCHandle.AddrOfPinnedObject instead.")]
        unsafe public static void* PinGCObjectAndGetAddress(System.Object target, out ulong gcHandle)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            return PinSystemObjectAndGetAddress(target, out gcHandle);
        }

        [Obsolete("Use GCHandle.Alloc with GCHandle.AddrOfPinnedObject instead.")]
        unsafe public static void* PinGCArrayAndGetDataAddress(System.Array target, out ulong gcHandle)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            return PinSystemArrayAndGetAddress(target, out gcHandle);
        }

        [ThreadSafe]
        unsafe private static extern void* PinSystemArrayAndGetAddress(System.Object target, out ulong gcHandle);

        [ThreadSafe]
        unsafe private static extern void* PinSystemObjectAndGetAddress(System.Object target, out ulong gcHandle);

        [Obsolete("Use GCHandle.Free instead.")]
        [ThreadSafe]
        unsafe public static extern void ReleaseGCObject(ulong gcHandle);

        [Obsolete("The garbage collector cannot track object references stored in unmanaged memory, leading to undefined behavior.")]
        unsafe public static void CopyObjectAddressToPtr(object target, void* dstPtr)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            ClassAsRef<object>(dstPtr) = target;
        }


        public static unsafe bool IsBlittable<T>() where T : struct
        {
            return IsBlittable(typeof(T));
        }

        [ThreadSafe]
        public static extern int CheckForLeaks();

        [ThreadSafe]
        public static extern int ForgiveLeaks();

        [ThreadSafe]
        [BurstAuthorizedExternalMethod]
        public static extern NativeLeakDetectionMode GetLeakDetectionMode();

        [ThreadSafe]
        [BurstAuthorizedExternalMethod]
        public static extern void SetLeakDetectionMode(NativeLeakDetectionMode value);

        [ThreadSafe]
        [BurstAuthorizedExternalMethod]
        [VisibleToOtherModules("UnityEngine.AIModule")]
        unsafe internal static extern int LeakRecord(IntPtr handle, LeakCategory category, int callstacksToSkip);

        [ThreadSafe]
        [BurstAuthorizedExternalMethod]
        [VisibleToOtherModules("UnityEngine.AIModule")]
        unsafe internal static extern int LeakErase(IntPtr handle, LeakCategory category);

        unsafe public static void* MallocTracked(long size, int alignment, Allocator allocator, int callstacksToSkip)
        {
            // Preserve existing public API signature for MallocTracked
            // However we do not need to have two binding functions doing exactly the same job
            return MallocTracked(size, alignment, allocator, callstacksToSkip + 1, IntPtr.Zero);
        }

        unsafe public static void* MallocTracked(long size, int alignment, MemoryLabel label, int callstacksToSkip)
        {
            return MallocTracked(size, alignment, label.allocator, callstacksToSkip + 1, label.pointer);
        }

        [ThreadSafe, NativeThrows]
        unsafe internal static extern void* MallocTracked(long size, int alignment, Allocator allocator, int callstacksToSkip, IntPtr label);

        [ThreadSafe, NativeThrows]
        unsafe public static extern void FreeTracked(void* memory, Allocator allocator);

        unsafe public static void FreeTracked(void* memory, MemoryLabel label)
        {
            FreeTracked(memory, label.allocator);
        }

        unsafe public static void* Malloc(long size, int alignment, Allocator allocator)
        {
            return Malloc(size, alignment, allocator, IntPtr.Zero);
        }

        unsafe public static void* Malloc(long size, int alignment, MemoryLabel label)
        {
            label.CheckArgument();
            return Malloc(size, alignment, label.allocator, label.pointer);
        }

        [ThreadSafe, NativeThrows]
        unsafe static extern void* Malloc(long size, int alignment, Allocator allocator, IntPtr label);

        [ThreadSafe, NativeThrows]
        unsafe public static extern void Free(void* memory, Allocator allocator);

        unsafe public static void Free(void* memory, MemoryLabel label)
        {
            label.CheckArgument();
            Free(memory, label.allocator);
        }

        public static bool IsValidAllocator(Allocator allocator) { return allocator > Allocator.None; }


        [ThreadSafe, NativeThrows]
        unsafe public static extern void MemCpy(void* destination, void* source, long size);

        [ThreadSafe, NativeThrows]
        unsafe public static extern void MemCpyReplicate(void* destination, void* source, int size, int count);

        [ThreadSafe, NativeThrows]
        unsafe public static extern void MemCpyStride(void* destination, int destinationStride, void* source, int sourceStride, int elementSize, int count);

        [ThreadSafe, NativeThrows]
        unsafe public static extern void MemMove(void* destination, void* source, long size);

        [ThreadSafe, NativeThrows]
        unsafe public static extern void MemSwap(void* ptr1, void* ptr2, long size);

        [ThreadSafe, NativeThrows]
        unsafe public static extern void MemSet(void* destination, byte value, long size);

        unsafe public static void MemClear(void* destination, long size)
        {
            MemSet(destination, 0, size);
        }

        [ThreadSafe, NativeThrows]
        unsafe public static extern int MemCmp(void* ptr1, void* ptr2, long size);

        [ThreadSafe]
        public static extern int SizeOf(Type type);

        [ThreadSafe]
        public static extern bool IsBlittable(Type type);

        [ThreadSafe]
        public static extern bool IsUnmanaged(Type type);

        [ThreadSafe]
        public static extern bool IsValidNativeContainerElementType(Type type);

        [ThreadSafe]
        internal static extern int GetScriptingTypeFlags(Type type);

        // @TODO : This is probably not the ideal place to have this?
        [ThreadSafe]
        internal static extern void LogError(string msg, string filename, int linenumber);
    }
}
