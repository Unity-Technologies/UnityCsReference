// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Unity.Collections
{
    /// <summary>
    /// For scheduling release of unmanaged resources.
    /// </summary>
    public interface INativeDisposable : IDisposable
    {
        /// <summary>
        /// Creates and schedules a job that will release all resources (memory and safety handles) of this collection.
        /// </summary>
        /// <param name="inputDeps">A job handle which the newly scheduled job will depend upon.</param>
        /// <returns>The handle of a new job that will release all resources (memory and safety handles) of this collection.</returns>
        JobHandle Dispose(JobHandle inputDeps);
    }

    /// <summary>
    /// Provides helper methods for collections.
    /// </summary>
    [GenerateTestsForBurstCompatibility]
    public static class CollectionHelper
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        internal static void CheckAllocator(AllocatorManager.AllocatorHandle allocator)
        {
            if (!ShouldDeallocate(allocator))
                throw new ArgumentException($"Allocator {allocator} must not be None or Invalid");
        }

        /// <summary>
        /// The size in bytes of the current platform's L1 cache lines.
        /// </summary>
        /// <value>The size in bytes of the current platform's L1 cache lines.</value>
        public const int CacheLineSize = JobsUtility.CacheLineSize;

        [StructLayout(LayoutKind.Explicit)]
        internal struct LongDoubleUnion
        {
            [FieldOffset(0)]
            internal long longValue;

            [FieldOffset(0)]
            internal double doubleValue;
        }

        /// <summary>
        /// Returns the binary logarithm of the `value`, but the result is rounded down to the nearest integer.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The binary logarithm of the `value`, but the result is rounded down to the nearest integer.</returns>
        public static int Log2Floor(int value)
        {
            return 31 - math.lzcnt((uint)value);
        }

        /// <summary>
        /// Returns the binary logarithm of the `value`, but the result is rounded up to the nearest integer.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The binary logarithm of the `value`, but the result is rounded up to the nearest integer.</returns>
        public static int Log2Ceil(int value)
        {
            return 32 - math.lzcnt((uint)value - 1);
        }

        /// <summary>
        /// Returns an allocation size in bytes that factors in alignment.
        /// </summary>
        /// <example><code>
        /// // 55 aligned to 16 is 64.
        /// int size = CollectionHelper.Align(55, 16);
        /// </code></example>
        /// <param name="size">The size to align.</param>
        /// <param name="alignmentPowerOfTwo">A non-zero, positive power of two.</param>
        /// <returns>The smallest integer that is greater than or equal to<paramref name="size"/> and is a multiple of <paramref name="alignmentPowerOfTwo"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="alignmentPowerOfTwo"/> is not a non-zero, positive power of two.</exception>
        public static int Align(int size, int alignmentPowerOfTwo)
        {
            if (alignmentPowerOfTwo == 0)
                return size;

            CheckIntPositivePowerOfTwo(alignmentPowerOfTwo);

            return (size + alignmentPowerOfTwo - 1) & ~(alignmentPowerOfTwo - 1);
        }

        /// <summary>
        /// Returns an allocation size in bytes that factors in alignment.
        /// </summary>
        /// <example><code>
        /// // 55 aligned to 16 is 64.
        /// long size = CollectionHelper.Align(55, 16);
        /// </code></example>
        /// <param name="size">The size to align.</param>
        /// <param name="alignmentPowerOfTwo">A non-zero, positive power of two.</param>
        /// <returns>The smallest integer that is greater than or equal to<paramref name="size"/> and is a multiple of <paramref name="alignmentPowerOfTwo"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="alignmentPowerOfTwo"/> is not a non-zero, positive power of two.</exception>
        public static long Align(long size, int alignmentPowerOfTwo)
        {
            if (alignmentPowerOfTwo == 0)
                return size;

            CheckIntPositivePowerOfTwo(alignmentPowerOfTwo);

            return (size + alignmentPowerOfTwo - 1) & ~(alignmentPowerOfTwo - 1);
        }

        /// <summary>
        /// Returns an allocation size in bytes that factors in alignment.
        /// </summary>
        /// <example><code>
        /// // 55 aligned to 16 is 64.
        /// ulong size = CollectionHelper.Align(55, 16);
        /// </code></example>
        /// <param name="size">The size to align.</param>
        /// <param name="alignmentPowerOfTwo">A non-zero, positive power of two.</param>
        /// <returns>The smallest integer that is greater than or equal to <paramref name="size"/> and is a multiple of <paramref name="alignmentPowerOfTwo"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="alignmentPowerOfTwo"/> is not a non-zero, positive power of two.</exception>
        public static ulong Align(ulong size, ulong alignmentPowerOfTwo)
        {
            if (alignmentPowerOfTwo == 0)
                return size;

            CheckUlongPositivePowerOfTwo(alignmentPowerOfTwo);

            return (size + alignmentPowerOfTwo - 1) & ~(alignmentPowerOfTwo - 1);
        }

        /// <summary>
        /// Returns a pointer whose value is rounded up to an address aligned to the provided alignment.
        /// </summary>
        /// <example><code>
        /// // 0x80BD5162 aligned to 8 is 0x80BD5168
        /// void* alignedPtr = CollectionHelper.Align(55, 16);
        /// </code></example>
        /// <param name="ptr">The pointer to align.</param>
        /// <param name="alignmentPowerOfTwo">A non-zero, positive power of two.</param>
        /// <returns>The smallest address that is greater than or equal to <paramref name="ptr"/> and is a multiple of <paramref name="alignmentPowerOfTwo"/>.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="alignmentPowerOfTwo"/> is not a non-zero, positive power of two.</exception>
        internal static unsafe void* AlignPointer(void* ptr, int alignmentPowerOfTwo)
        {
            if (alignmentPowerOfTwo == 0)
                return ptr;

            CheckIntPositivePowerOfTwo(alignmentPowerOfTwo);

            var ptrAsNuint = (nuint)ptr;
            var alignmentAsNuint = (nuint)alignmentPowerOfTwo;
            return (void*)( (ptrAsNuint + alignmentAsNuint - 1) & ~(alignmentAsNuint - 1) );
        }

        /// <summary>
        /// Returns true if the address represented by the pointer has a given alignment.
        /// </summary>
        /// <param name="p">The pointer.</param>
        /// <param name="alignmentPowerOfTwo">A non-zero, positive power of two.</param>
        /// <returns>True if the address is a multiple of `alignmentPowerOfTwo`.</returns>
        /// <exception cref="ArgumentException">Thrown if `alignmentPowerOfTwo` is not a non-zero, positive power of two.</exception>
        public static unsafe bool IsAligned(void* p, int alignmentPowerOfTwo)
        {
            CheckIntPositivePowerOfTwo(alignmentPowerOfTwo);
            return ((ulong)p & ((ulong)alignmentPowerOfTwo - 1)) == 0;
        }

        /// <summary>
        /// Returns true if an offset has a given alignment.
        /// </summary>
        /// <param name="offset">An offset</param>
        /// <param name="alignmentPowerOfTwo">A non-zero, positive power of two.</param>
        /// <returns>True if the offset is a multiple of `alignmentPowerOfTwo`.</returns>
        /// <exception cref="ArgumentException">Thrown if `alignmentPowerOfTwo` is not a non-zero, positive power of two.</exception>
        public static bool IsAligned(ulong offset, int alignmentPowerOfTwo)
        {
            CheckIntPositivePowerOfTwo(alignmentPowerOfTwo);
            return (offset & ((ulong)alignmentPowerOfTwo - 1)) == 0;
        }

        /// <summary>
        /// Returns true if a positive value is a non-zero power of two.
        /// </summary>
        /// <remarks>Result is invalid if `value &lt; 0`.</remarks>
        /// <param name="value">A positive value.</param>
        /// <returns>True if the value is a non-zero, positive power of two.</returns>
        public static bool IsPowerOfTwo(int value)
        {
            return (value & (value - 1)) == 0;
        }

        /// <summary>
        /// Returns a (non-cryptographic) hash of a memory block.
        /// </summary>
        /// <remarks>The hash function used is [djb2](http://web.archive.org/web/20190508211657/http://www.cse.yorku.ca/~oz/hash.html).</remarks>
        /// <param name="ptr">A buffer.</param>
        /// <param name="bytes">The number of bytes to hash.</param>
        /// <returns>A hash of the bytes.</returns>
        public static unsafe uint Hash(void* ptr, int bytes)
        {
            // djb2 - Dan Bernstein hash function
            // http://web.archive.org/web/20190508211657/http://www.cse.yorku.ca/~oz/hash.html
            byte* str = (byte*)ptr;
            ulong hash = 5381;
            while (bytes > 0)
            {
                ulong c = str[--bytes];
                hash = ((hash << 5) + hash) + c;
            }
            return (uint)hash;
        }

        [ExcludeFromBurstCompatTesting("Used only for debugging, and uses managed strings")]
        internal static void WriteLayout(Type type)
        {
            Console.WriteLine($"   Offset | Bytes  | Name     Layout: {0}", type.Name);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                Console.WriteLine("   {0, 6} | {1, 6} | {2}"
                    , Marshal.OffsetOf(type, field.Name)
                    , Marshal.SizeOf(field.FieldType)
                    , field.Name
                );
            }
        }

        internal static bool ShouldDeallocate(AllocatorManager.AllocatorHandle allocator)
        {
            // Allocator.Invalid == container is not initialized.
            // Allocator.None    == container is initialized, but container doesn't own data.
            return allocator.ToAllocator > Allocator.None;
        }

        /// <summary>
        /// Tell Burst that an integer can be assumed to map to an always positive value.
        /// </summary>
        /// <param name="value">The integer that is always positive.</param>
        /// <returns>Returns `x`, but allows the compiler to assume it is always positive.</returns>
        [return: AssumeRange(0, int.MaxValue)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int AssumePositive(int value)
        {
            return value;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", GenericTypeArguments = new[] { typeof(NativeArray<int>) })]
        internal static void CheckIsUnmanaged<T>()
        {
            if (!UnsafeUtility.IsUnmanaged<T>())
            {
                throw new ArgumentException($"{typeof(T)} used in native collection is not blittable or not primitive");
            }
        }

        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", GenericTypeArguments = new[] { typeof(NativeArray<int>) })]
        internal static void InitNativeContainer<T>(AtomicSafetyHandle handle)
        {
            if (UnsafeUtility.IsNativeContainerType<T>())
                AtomicSafetyHandle.SetNestedContainer(handle, true);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        internal static void CheckIntPositivePowerOfTwo(int value)
        {
            var valid = (value > 0) && ((value & (value - 1)) == 0);
            if (!valid)
            {
                throw new ArgumentException($"Alignment requested: {value} is not a non-zero, positive power of two.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        internal static void CheckUlongPositivePowerOfTwo(ulong value)
        {
            var valid = (value > 0) && ((value & (value - 1)) == 0);
            if (!valid)
            {
                throw new ArgumentException($"Alignment requested: {value} is not a non-zero, positive power of two.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void CheckIndexInRange(int index, int length)
        {
            // This checks both < 0 and >= Length with one comparison
            if ((uint)index >= (uint)length)
                throw new IndexOutOfRangeException($"Index {index} is out of range in container of '{length}' Length.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        internal static void CheckCapacityInRange(int capacity, int maxCapacity, int length)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException($"Capacity {capacity} must be positive.");

            if (capacity > maxCapacity)
                throw new ArgumentException($"Capacity {capacity} value too large. Maximum capacity is {maxCapacity}.");

            if (capacity < length)
                throw new ArgumentOutOfRangeException($"Capacity {capacity} is out of range in container of '{length}' Length.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        internal static void CheckCapacityInRange(int capacity, int length)
        {
            CheckCapacityInRange(capacity, int.MaxValue, length);
        }

        /// <summary>
        /// Create a NativeArray, using a provided allocator that implements IAllocator.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="U">The type of allocator.</typeparam>
        /// <param name="length">The number of elements to allocate.</param>
        /// <param name="allocator">The allocator to use.</param>
        /// <param name="options">Options for allocation, such as whether to clear the memory.</param>
        /// <returns>Returns the NativeArray that was created.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(AllocatorManager.AllocatorHandle) })]
        public static NativeArray<T> CreateNativeArray<T, U>(int length, ref U allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
            where T : unmanaged
            where U : unmanaged, AllocatorManager.IAllocator
        {
            NativeArray<T> nativeArray;
            if (!allocator.IsCustomAllocator)
            {
                nativeArray = new NativeArray<T>(length, allocator.ToAllocator, options);
            }
            else
            {
                nativeArray = new NativeArray<T>();
                nativeArray.Initialize(length, ref allocator, options);
            }
            return nativeArray;
        }

        /// <summary>
        /// Create a NativeArray, using a provided AllocatorHandle.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="length">The number of elements to allocate.</param>
        /// <param name="allocator">The AllocatorHandle to use.</param>
        /// <param name="options">Options for allocation, such as whether to clear the memory.</param>
        /// <returns>Returns the NativeArray that was created.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public static NativeArray<T> CreateNativeArray<T>(int length, AllocatorManager.AllocatorHandle allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
            where T : unmanaged
        {
            NativeArray<T> nativeArray;
            if(!AllocatorManager.IsCustomAllocator(allocator))
            {
                nativeArray = new NativeArray<T>(length, allocator.ToAllocator, options);
            }
            else
            {
                nativeArray = new NativeArray<T>();
                nativeArray.Initialize(length, allocator, options);
            }
            return nativeArray;
        }

        /// <summary>
        /// Create a NativeArray from another NativeArray, using a provided AllocatorHandle.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="array">The NativeArray to make a copy of.</param>
        /// <param name="allocator">The AllocatorHandle to use.</param>
        /// <returns>Returns the NativeArray that was created.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public static NativeArray<T> CreateNativeArray<T>(NativeArray<T> array, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged
        {
            NativeArray<T> nativeArray;
            if (!AllocatorManager.IsCustomAllocator(allocator))
            {
                nativeArray = new NativeArray<T>(array, allocator.ToAllocator);
            }
            else
            {
                nativeArray = new NativeArray<T>();
                nativeArray.Initialize(array.Length, allocator);
                nativeArray.CopyFrom(array);
            }
            return nativeArray;
        }

        /// <summary>
        /// Create a NativeArray from a managed array, using a provided AllocatorHandle.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="array">The managed array to make a copy of.</param>
        /// <param name="allocator">The AllocatorHandle to use.</param>
        /// <returns>Returns the NativeArray that was created.</returns>
        [ExcludeFromBurstCompatTesting("Managed array")]
        public static NativeArray<T> CreateNativeArray<T>(T[] array, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged
        {
            NativeArray<T> nativeArray;
            if (!AllocatorManager.IsCustomAllocator(allocator))
            {
                nativeArray = new NativeArray<T>(array, allocator.ToAllocator);
            }
            else
            {
                nativeArray = new NativeArray<T>();
                nativeArray.Initialize(array.Length, allocator);
                nativeArray.CopyFrom(array);
            }
            return nativeArray;
        }

        /// <summary>
        /// Create a NativeArray from a managed array, using a provided Allocator.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <typeparam name="U">The type of allocator.</typeparam>
        /// <param name="array">The managed array to make a copy of.</param>
        /// <param name="allocator">The Allocator to use.</param>
        /// <returns>Returns the NativeArray that was created.</returns>
        [ExcludeFromBurstCompatTesting("Managed array")]
        public static NativeArray<T> CreateNativeArray<T, U>(T[] array, ref U allocator)
            where T : unmanaged
            where U : unmanaged, AllocatorManager.IAllocator
        {
            NativeArray<T> nativeArray;
            if (!allocator.IsCustomAllocator)
            {
                nativeArray = new NativeArray<T>(array, allocator.ToAllocator);
            }
            else
            {
                nativeArray = new NativeArray<T>();
                nativeArray.Initialize(array.Length, ref allocator);
                nativeArray.CopyFrom(array);
            }
            return nativeArray;
        }

        /// <summary>
        /// Dispose a NativeArray from an AllocatorHandle where it is allocated.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="nativeArray">The NativeArray to make a copy of.</param>
        /// <param name="allocator">The AllocatorHandle used to allocate the NativeArray.</param>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public static void DisposeNativeArray<T>(NativeArray<T> nativeArray, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged
        {
            nativeArray.DisposeCheckAllocator();
        }

        /// <summary>
        /// Dispose a NativeArray from an AllocatorHandle where it is allocated.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="nativeArray">The NativeArray to be disposed.</param>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public static void Dispose<T>(NativeArray<T> nativeArray)
            where T : unmanaged
        {
            nativeArray.DisposeCheckAllocator();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        static void CheckConvertArguments<T>(int length) where T : unmanaged
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be >= 0");

            if (!UnsafeUtility.IsUnmanaged<T>())
            {
                throw new InvalidOperationException(
                    $"{typeof(T)} used in NativeArray<{typeof(T)}> must be unmanaged (contain no managed types).");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        static void InitNestedNativeContainer<T>(AtomicSafetyHandle handle)
            where T : unmanaged
        {
            if (UnsafeUtility.IsNativeContainerType<T>())
            {
                AtomicSafetyHandle.SetNestedContainer(handle, true);
            }
        }

        /// <summary>
        /// Convert existing data into a NativeArray.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="dataPointer">Pointer to the data to be converted.</param>
        /// <param name="length">The count of elements.</param>
        /// <param name="allocator">The Allocator to use.</param>
        /// <param name="setTempMemoryHandle">Use temporary memory atomic safety handle.</param>
        /// <returns>Returns the NativeArray that was created.</returns>
        /// <remarks>The caller is still the owner of the data.</remarks>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public static unsafe NativeArray<T> ConvertExistingDataToNativeArray<T>(void* dataPointer, int length, AllocatorManager.AllocatorHandle allocator, bool setTempMemoryHandle = false)
            where T : unmanaged
        {
            CheckConvertArguments<T>(length);
            NativeArray<T> nativeArray = default;

            nativeArray.m_Buffer = dataPointer;
            nativeArray.m_Length = length;
            if (!allocator.IsCustomAllocator)
            {
                nativeArray.m_AllocatorLabel = allocator.ToAllocator;
            }
            else
            {
                nativeArray.m_AllocatorLabel = Allocator.None;
            }

            nativeArray.m_MinIndex = 0;
            nativeArray.m_MaxIndex = length - 1;
            if (setTempMemoryHandle)
            {
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref nativeArray, AtomicSafetyHandle.GetTempMemoryHandle());
            }
            return nativeArray;
        }

        /// <summary>
        /// Convert NativeList into a NativeArray.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="nativeList">NativeList to be converted.</param>
        /// <param name="length">The count of elements.</param>
        /// <param name="allocator">The Allocator to use.</param>
        /// <returns>Returns the NativeArray that was created.</returns>
        /// <remarks>There is a caveat if users would like to transfer memory ownership from the NativeList to the converted NativeArray.
        /// NativeList implementation includes two memory allocations, one holds its header, another holds the list data.
        /// After convertion, the converted NativeArray holds the list data and dispose the array only free the list data.
        /// Users need to manually free the list header to avoid memory leaks, for example after convertion call,
        /// AllocatorManager.Free(allocator, nativeList.m_ListData); </remarks>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
        public static unsafe NativeArray<T> ConvertExistingNativeListToNativeArray<T>(ref NativeList<T> nativeList, int length, AllocatorManager.AllocatorHandle allocator)
            where T : unmanaged
        {
            NativeArray<T> nativeArray = ConvertExistingDataToNativeArray<T>(nativeList.GetUnsafePtr(), length, allocator);

            var safetyHandle = NativeListUnsafeUtility.GetAtomicSafetyHandle(ref nativeList);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle<T>(ref nativeArray, safetyHandle);
            InitNestedNativeContainer<T>(nativeArray.m_Safety);
            return nativeArray;
        }

        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) }, RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", CompileTarget = GenerateTestsForBurstCompatibilityAttribute.BurstCompatibleCompileTarget.Editor)]
        internal static AtomicSafetyHandle GetNativeArraySafetyHandle<T>(ref NativeArray<T> nativeArray)
            where T : unmanaged
        {
            return nativeArray.m_Safety;
        }

        /// <summary>
        /// Create a NativeParallelMultiHashMap from a managed array, using a provided Allocator.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys.</typeparam>
        /// <typeparam name="TValue">The type of the values.</typeparam>
        /// <typeparam name="U">The type of allocator.</typeparam>
        /// <param name="length">The desired capacity of the NativeParallelMultiHashMap.</param>
        /// <param name="allocator">The Allocator to use.</param>
        /// <returns>Returns the NativeParallelMultiHashMap that was created.</returns>
        [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int), typeof(AllocatorManager.AllocatorHandle) })]
        public static NativeParallelMultiHashMap<TKey, TValue> CreateNativeParallelMultiHashMap<TKey, TValue, U>(int length, ref U allocator)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
            where U : unmanaged, AllocatorManager.IAllocator
        {
            var container = new NativeParallelMultiHashMap<TKey, TValue>();
            container.Initialize(length, ref allocator);
            return container;
        }

        /// <summary>
        /// Empty job type used for Burst compilation testing
        /// </summary>
        [BurstCompile]
        public struct DummyJob : IJob
        {
            /// <summary>
            /// Empty job execute function used for Burst compilation testing
            /// </summary>
            public void Execute()
            {
            }
        }

        /// <summary>
        /// Checks that reflection data was properly registered for a job.
        /// </summary>
        /// <remarks>This should be called before instantiating JobsUtility.JobScheduleParameters in order to report to the user if they need to take action.</remarks>
        /// <param name="reflectionData">The reflection data pointer.</param>
        /// <typeparam name="T">Job type</typeparam>
        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS",
            GenericTypeArguments = new[] { typeof(DummyJob) })]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        public static void CheckReflectionDataCorrect<T>(IntPtr reflectionData)
        {
            bool burstCompiled = true;
            CheckReflectionDataCorrectInternal<T>(reflectionData, ref burstCompiled);
            if (burstCompiled && reflectionData == IntPtr.Zero)
                throw new InvalidOperationException("Reflection data was not set up by an Initialize() call. For generic job types, please include [assembly: RegisterGenericJobType(typeof(MyJob<MyJobSpecialization>))] in your source file.");
        }

        /// <summary>
        /// Creates a new AtomicSafetyHandle that is valid until [[CollectionHelper.DisposeSafetyHandle]] is called.
        /// </summary>
        /// <param name="allocator">The AllocatorHandle to use.</param>
        /// <returns>Safety handle.</returns>
        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static AtomicSafetyHandle CreateSafetyHandle(AllocatorManager.AllocatorHandle allocator)
        {
            if (allocator.IsCustomAllocator)
            {
                return AtomicSafetyHandle.Create();
            }

            return (allocator.ToAllocator == Allocator.Temp) ? AtomicSafetyHandle.GetTempMemoryHandle() : AtomicSafetyHandle.Create();
        }

        /// <summary>
        /// Disposes a previously created AtomicSafetyHandle.
        /// </summary>
        /// <param name="handle">Safety handle.</param>
        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void DisposeSafetyHandle(ref AtomicSafetyHandle handle)
        {
            AtomicSafetyHandle.CheckDeallocateAndThrow(handle);
            // If the safety handle is for a temp allocation, create a new safety handle for this instance which can be marked as invalid
            // Setting it to new AtomicSafetyHandle is not enough since the handle needs a valid node pointer in order to give the correct errors
            if (AtomicSafetyHandle.IsTempMemoryHandle(handle))
            {
                int staticSafetyId = handle.staticSafetyId;
                handle = AtomicSafetyHandle.Create();
                handle.staticSafetyId = staticSafetyId;
            }
            AtomicSafetyHandle.Release(handle);
        }

        static unsafe void CreateStaticSafetyIdInternal(ref int id, in FixedString512Bytes name)
        {
            id = AtomicSafetyHandle.NewStaticSafetyId(name.GetUnsafePtr(), name.Length);
        }

        [BurstDiscard]
        static void CreateStaticSafetyIdInternal<T>(ref int id)
        {
            CreateStaticSafetyIdInternal(ref id, typeof(T).ToString());
        }

        /// <summary>
        /// Assigns the provided static safety ID to an [[AtomicSafetyHandle]]. The ID's owner type name and any custom error messages are used by the job debugger when reporting errors involving the target handle.
        /// </summary>
        /// <remarks>This is preferable to AtomicSafetyHandle.NewStaticSafetyId as it is compatible with burst.</remarks>
        /// <typeparam name="T">Type of container safety handle refers to.</typeparam>
        /// <param name="handle">Safety handle.</param>
        /// <param name="sharedStaticId">The static safety ID to associate with the provided handle. This ID must have been allocated with ::ref::NewStaticSafetyId.</param>
        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS", GenericTypeArguments = new[] { typeof(NativeArray<int>) })]
        public static void SetStaticSafetyId<T>(ref AtomicSafetyHandle handle, ref int sharedStaticId)
        {
            if (sharedStaticId == 0)
            {
                // This will eventually either work with burst supporting a subset of typeof()
                // or something similar to Burst.BurstRuntime.GetTypeName() will be implemented
                // JIRA issue DOTS-5685

                CreateStaticSafetyIdInternal<T>(ref sharedStaticId);
            }

            AtomicSafetyHandle.SetStaticSafetyId(ref handle, sharedStaticId);
        }

        /// <summary>
        /// Assigns the provided static safety ID to an [[AtomicSafetyHandle]]. The ID's owner type name and any custom error messages are used by the job debugger when reporting errors involving the target handle.
        /// </summary>
        /// <remarks>This is preferable to AtomicSafetyHandle.NewStaticSafetyId as it is compatible with burst.</remarks>
        /// <param name="handle">Safety handle.</param>
        /// <param name="sharedStaticId">The static safety ID to associate with the provided handle. This ID must have been allocated with ::ref::NewStaticSafetyId.</param>
        /// <param name="name">The name of the resource type.</param>
        [GenerateTestsForBurstCompatibility(RequiredUnityDefine = "ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static unsafe void SetStaticSafetyId(ref AtomicSafetyHandle handle, ref int sharedStaticId, FixedString512Bytes name)
        {
            if (sharedStaticId == 0)
            {
                CreateStaticSafetyIdInternal(ref sharedStaticId, name);
            }

            AtomicSafetyHandle.SetStaticSafetyId(ref handle, sharedStaticId);
        }
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        [BurstDiscard]
        static void CheckReflectionDataCorrectInternal<T>(IntPtr reflectionData, ref bool burstCompiled)
        {
            if (reflectionData == IntPtr.Zero)
                throw new InvalidOperationException($"Reflection data was not set up by an Initialize() call. For generic job types, please include [assembly: RegisterGenericJobType(typeof({typeof(T)}))] in your source file.");
            burstCompiled = false;
        }
    }
}
