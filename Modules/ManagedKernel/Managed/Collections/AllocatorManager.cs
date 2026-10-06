// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable 0649

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AOT;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using ManagedKernel.Assertions;
using Unity.Jobs.LowLevel.Unsafe;
using System.Runtime.CompilerServices;
using System.Threading;
using Unity.Scripting.LifecycleManagement;

namespace Unity.Collections
{
    [GenerateTestsForBurstCompatibility]
    struct Spinner
    {
        int m_Lock;

        /// <summary>
        /// Continually spin until the lock can be acquired.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Acquire()
        {
            for (; ; )
            {
                // Optimistically assume the lock is free on the first try.
                if (Interlocked.CompareExchange(ref m_Lock, 1, 0) == 0)
                {
                    return;
                }

                // Wait for lock to be released without generate cache misses.
                while (Volatile.Read(ref m_Lock) == 1)
                {
                    continue;
                }

                // Future improvement: the 'continue` instruction above could be swapped for a 'pause' intrinsic
                // instruction when the CPU supports it, to further reduce contention by reducing load-store unit
                // utilization. However, this would need to be optional because if you don't use hyper-threading
                // and you don't care about power efficiency, using the 'pause' instruction will slow down lock
                // acquisition in the contended scenario.
            }
        }

        /// <summary>
        /// Try to acquire the lock and immediately return without spinning.
        /// </summary>
        /// <returns><see langword="true"/> if the lock was acquired, <see langword="false"/> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryAcquire()
        {
            // First do a memory load (read) to check if lock is free in order to prevent uncessary cache missed.
            return Volatile.Read(ref m_Lock) == 0 &&
                Interlocked.CompareExchange(ref m_Lock, 1, 0) == 0;
        }

        /// <summary>
        /// Try to acquire the lock, and spin only if <paramref name="spin"/> is <see langword="true"/>.
        /// </summary>
        /// <param name="spin">Set to true to spin the lock.</param>
        /// <returns>true if the lock was acquired, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryAcquire(bool spin)
        {
            if (spin)
            {
                Acquire();
                return true;
            }

            return TryAcquire();
        }

        /// <summary>
        /// Release the lock
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Release()
        {
            Volatile.Write(ref m_Lock, 0);
        }
    }

    /// <summary>
    /// Manages custom memory allocators.
    /// </summary>
    public unsafe static partial class AllocatorManager
    {
        internal static Block AllocateBlock<T>(ref this T t, int sizeOf, int alignOf, int items) where T : unmanaged, IAllocator
        {
            CheckValid(t.Handle);
            Block block = default;
            block.Range.Pointer = IntPtr.Zero;
            block.Range.Items = items;
            block.Range.Allocator = t.Handle;
            block.BytesPerItem = sizeOf;
            // Make the alignment multiple of cacheline size
            block.Alignment = math.max(JobsUtility.CacheLineSize, alignOf);

            var error = t.Try(ref block);
            CheckFailedToAllocate(error);
            return block;
        }

        internal static Block AllocateBlock<T,U>(ref this T t, U u, int items) where T : unmanaged, IAllocator where U : unmanaged
        {
            return AllocateBlock(ref t, UnsafeUtility.SizeOf<U>(), UnsafeUtility.AlignOf<U>(), items);
        }

        /// <summary>
        /// Allocates memory directly from an allocator.
        /// </summary>
        /// <typeparam name="T">The type of allocator.</typeparam>
        /// /// <param name="t">The allocator of type T used to allocator memory.</param>
        /// <param name="sizeOf">The number of bytes to allocate to item.</param>
        /// <param name="alignOf">The alignment in bytes.</param>
        /// <param name="items">The number of items. Defaults to 1.</param>
        /// <returns>A pointer to the allocated memory.</returns>
        public static unsafe void* Allocate<T>(ref this T t, int sizeOf, int alignOf, int items = 1) where T : unmanaged, IAllocator
        {
            return (void*)AllocateBlock(ref t, sizeOf, alignOf, items).Range.Pointer;
        }

        internal static unsafe U* Allocate<T,U>(ref this T t, U u, int items) where T : unmanaged, IAllocator where U : unmanaged
        {
            return (U*)Allocate(ref t, UnsafeUtility.SizeOf<U>(), UnsafeUtility.AlignOf<U>(), items);
        }

        internal static unsafe void* AllocateStruct<T, U>(ref this T t, U u, int items) where T : unmanaged, IAllocator where U : unmanaged
        {
            return (void*)Allocate(ref t, UnsafeUtility.SizeOf<U>(), UnsafeUtility.AlignOf<U>(), items);
        }

        internal static unsafe void FreeBlock<T>(ref this T t, ref Block block) where T : unmanaged, IAllocator
        {
            CheckValid(t.Handle);
            block.Range.Items = 0;
            var error = t.Try(ref block);
            CheckFailedToFree(error);
        }

        internal static unsafe void Free<T>(ref this T t, void* pointer, int sizeOf, int alignOf, int items) where T : unmanaged, IAllocator
        {
            if (pointer == null)
                return;
            Block block = default;
            block.AllocatedItems = items;
            block.Range.Pointer = (IntPtr)pointer;
            block.BytesPerItem = sizeOf;
            block.Alignment = alignOf;
            t.FreeBlock(ref block);
        }

        internal static unsafe void Free<T,U>(ref this T t, U* pointer, int items) where T : unmanaged, IAllocator where U : unmanaged
        {
            Free(ref t, pointer, UnsafeUtility.SizeOf<U>(), UnsafeUtility.AlignOf<U>(), items);
        }

        /// <summary>
        /// Allocates memory from an allocator.
        /// </summary>
        /// <param name="handle">A handle to the allocator.</param>
        /// <param name="itemSizeInBytes">The number of bytes to allocate.</param>
        /// <param name="alignmentInBytes">The alignment in bytes (must be a power of two).</param>
        /// <param name="items">The number of values to allocate space for. Defaults to 1.</param>
        /// <returns>A pointer to the allocated memory.</returns>
        public unsafe static void* Allocate(AllocatorHandle handle, int itemSizeInBytes, int alignmentInBytes, int items = 1)
        {
            return handle.Allocate(itemSizeInBytes, alignmentInBytes, items);
        }

        /// <summary>
        /// Allocates enough memory for an unmanaged value of a given type.
        /// </summary>
        /// <typeparam name="T">The type of value to allocate for.</typeparam>
        /// <param name="handle">A handle to the allocator.</param>
        /// <param name="items">The number of values to allocate for space for. Defaults to 1.</param>
        /// <returns>A pointer to the allocated memory.</returns>
        public unsafe static T* Allocate<T>(AllocatorHandle handle, int items = 1) where T : unmanaged
        {
            return handle.Allocate(default(T), items);
        }

        /// <summary>
        /// Frees an allocation.
        /// </summary>
        /// <remarks>For some allocators, the size of the allocation must be known to properly deallocate.
        /// Other allocators only need the pointer when deallocating and so will ignore `itemSizeInBytes`, `alignmentInBytes` and `items`.</remarks>
        /// <param name="handle">A handle to the allocator.</param>
        /// <param name="pointer">A pointer to the allocated memory.</param>
        /// <param name="itemSizeInBytes">The size in bytes of the allocation.</param>
        /// <param name="alignmentInBytes">The alignment in bytes (must be a power of two).</param>
        /// <param name="items">The number of values that the memory was allocated for.</param>
        public unsafe static void Free(AllocatorHandle handle, void* pointer, int itemSizeInBytes, int alignmentInBytes, int items = 1)
        {
            handle.Free(pointer, itemSizeInBytes, alignmentInBytes, items);
        }

        /// <summary>
        /// Frees an allocation.
        /// </summary>
        /// <param name="handle">A handle to the allocator.</param>
        /// <param name="pointer">A pointer to the allocated memory.</param>
        public unsafe static void Free(AllocatorHandle handle, void* pointer)
        {
            handle.Free((byte*)pointer, 1);
        }

        /// <summary>
        /// Frees an allocation.
        /// </summary>
        /// <remarks>For some allocators, the size of the allocation must be known to properly deallocate.
        /// Other allocators only need the pointer when deallocating and so  will ignore `T` and `items`.</remarks>
        /// <typeparam name="T">The type of value that the memory was allocated for.</typeparam>
        /// <param name="handle">A handle to the allocator.</param>
        /// <param name="pointer">A pointer to the allocated memory.</param>
        /// <param name="items">The number of values that the memory was allocated for.</param>
        public unsafe static void Free<T>(AllocatorHandle handle, T* pointer, int items = 1) where T : unmanaged
        {
            handle.Free(pointer, items);
        }

        /// <summary>
        /// Corresponds to Allocator.Invalid.
        /// </summary>
        /// <value>Corresponds to Allocator.Invalid.</value>
        public static readonly AllocatorHandle Invalid = new AllocatorHandle { Index = 0 };

        /// <summary>
        /// Corresponds to Allocator.None.
        /// </summary>
        /// <value>Corresponds to Allocator.None.</value>
        public static readonly AllocatorHandle None = new AllocatorHandle { Index = 1 };

        /// <summary>
        /// Corresponds to Allocator.Temp.
        /// </summary>
        /// <value>Corresponds to Allocator.Temp.</value>
        public static readonly AllocatorHandle Temp = new AllocatorHandle { Index = 2 };

        /// <summary>
        /// Corresponds to Allocator.TempJob.
        /// </summary>
        /// <value>Corresponds to Allocator.TempJob.</value>
        public static readonly AllocatorHandle TempJob = new AllocatorHandle { Index = 3 };

        /// <summary>
        /// Corresponds to Allocator.Persistent.
        /// </summary>
        /// <value>Corresponds to Allocator.Persistent.</value>
        public static readonly AllocatorHandle Persistent = new AllocatorHandle { Index = 4 };

        /// <summary>
        /// Corresponds to Allocator.AudioKernel.
        /// </summary>
        /// <value>Corresponds to Allocator.AudioKernel.</value>
        /// <remarks>Do not use. Reserved for internal use.</remarks>
        public static readonly AllocatorHandle AudioKernel = new AllocatorHandle { Index = 5 };

        /// <summary>
        /// Used for calling an allocator function.
        /// </summary>
        /// <param name="allocatorState">IntPtr to allocator's custom state.</param>
        /// <param name="block">The block to allocate.</param>
        /// <returns></returns>
        public delegate int TryFunction(IntPtr allocatorState, ref Block block);

        /// <summary>
        /// Convert an Allocator to an AllocatorHandle, keeping the Version.
        /// </summary>
        /// <param name="a">The Allocator to convert.</param>
        /// <returns>The AllocatorHandle of an allocator.</returns>
        public static AllocatorHandle ConvertToAllocatorHandle(Allocator a)
        {
            ushort index = (ushort)((uint)a & 0xFFFF);
            ushort version = (ushort)((uint)a >> 16);
            return new AllocatorHandle { Index = index, Version = version };
        }

        /// <summary>
        /// Represents the allocator function used within an allocator.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct AllocatorHandle : IAllocator, IEquatable<AllocatorHandle>, IComparable<AllocatorHandle>
        {
            internal ref TableEntry TableEntry => ref SharedStatics.TableEntry.Ref.Data.ElementAt(Index);
            internal bool IsInstalled => ((SharedStatics.IsInstalled.Ref.Data.ElementAt(Index>>6) >> (Index&63)) & 1) != 0;

            internal void IncrementVersion()
            {
                if (IsInstalled && IsCurrent)
                {
                    // When allocator version is larger than 0x7FFF, allocator.ToAllocator
                    // returns a negative value which causes problem when comparing to Allocator.None.
                    // So only lower 15 bits of version is valid.
                    Version = OfficialVersion = (ushort)(++OfficialVersion & 0x7FFF);
                }
            }

            internal void Rewind()
            {
                InvalidateDependents();
                IncrementVersion();
            }

            internal void Install(TableEntry tableEntry)
            {
                // if this allocator has never been visited before, then the unsafelists for its child allocators
                // and child safety handles are uninitialized, which means their allocator is Allocator.Invalid.
                // rectify that here.
                if (ChildAllocators.Allocator.Value != (int)Allocator.Persistent)
                {
                    ChildAllocators = new UnsafeList<AllocatorHandle>(0, Allocator.Persistent);
                    ChildSafetyHandles = new UnsafeList<AtomicSafetyHandle>(0, Allocator.Persistent);
                }
                Rewind();
                TableEntry = tableEntry;
            }

            internal ref ushort OfficialVersion => ref SharedStatics.Version.Ref.Data.ElementAt(Index);
            internal ref UnsafeList<AllocatorHandle> ChildAllocators => ref SharedStatics.ChildAllocators.Ref.Data.ElementAt(Index);
            internal ref AllocatorHandle Parent => ref SharedStatics.Parent.Ref.Data.ElementAt(Index);
            internal ref int IndexInParent => ref SharedStatics.IndexInParent.Ref.Data.ElementAt(Index);

            internal bool IsCurrent => (Version == 0) || (Version == OfficialVersion);
            internal bool IsValid => (Index < FirstUserIndex) || (IsInstalled && IsCurrent);

            internal ref Spinner ChildSpinLock => ref SharedStatics.ChildSpinLock.Ref.Data.ElementAt(Index);
            internal ref UnsafeList<AtomicSafetyHandle> ChildSafetyHandles => ref SharedStatics.ChildSafetyHandles.Ref.Data.ElementAt(Index);

            /// <summary>
            ///   <para>Determines if the handle is still valid, because we intend to release it if it is.</para>
            /// </summary>
            /// <param name="handle">Safety handle.</param>
            internal static unsafe bool CheckExists(AtomicSafetyHandle handle)
            {
                bool res = false;
                int* versionNode = (int*) (void*) handle.versionNode;
                res = (handle.version == (*versionNode & AtomicSafetyHandle.ReadWriteDisposeCheck));
                return res;
            }

            internal static unsafe bool AreTheSame(AtomicSafetyHandle a, AtomicSafetyHandle b)
            {
                if(a.version != b.version)
                    return false;
                if(a.versionNode != b.versionNode)
                    return false;
                return true;
            }

            /// <summary>
            /// For internal use only.
            /// </summary>
            /// <value>For internal use only.</value>
            public const int InvalidChildSafetyHandleIndex = -1;

            internal int AddSafetyHandle(AtomicSafetyHandle handle)
            {
                if(!NeedsUseAfterFreeTracking())
                    return InvalidChildSafetyHandleIndex;
                ChildSpinLock.Acquire();
                var result = ChildSafetyHandles.Length;
                ChildSafetyHandles.Add(handle);
                ChildSpinLock.Release();
                return result;
            }

            internal bool TryRemoveSafetyHandle(AtomicSafetyHandle handle, int safetyHandleIndex)
            {
                if(!NeedsUseAfterFreeTracking())
                    return false;
                if(safetyHandleIndex == InvalidChildSafetyHandleIndex)
                    return false;

                ChildSpinLock.Acquire();
                safetyHandleIndex = math.min(safetyHandleIndex, ChildSafetyHandles.Length - 1);
                while(safetyHandleIndex >= 0)
                {
                    unsafe
                    {
                        var safetyHandle = ChildSafetyHandles.Ptr + safetyHandleIndex;
                        if(AreTheSame(*safetyHandle, handle))
                        {
                            ChildSafetyHandles.RemoveAtSwapBack(safetyHandleIndex);
                            ChildSpinLock.Release();
                            return true;
                        }
                    }
                    --safetyHandleIndex;
                }
                ChildSpinLock.Release();
                return false;
            }

            internal bool NeedsUseAfterFreeTracking()
            {
                if (IsValid == false)
                    return false;

                if (ChildAllocators.Allocator.Value != (int)Allocator.Persistent)
                    return false;

                return true;
            }

            internal static bool AreTheSame(AllocatorHandle a, AllocatorHandle b)
            {
                if (a.Index != b.Index)
                    return false;
                if (a.Version != b.Version)
                    return false;
                return true;
            }

            /// <summary>
            /// For internal use only.
            /// </summary>
            /// <value>For internal use only.</value>
            public const int InvalidChildAllocatorIndex = -1;

            internal int AddChildAllocator(AllocatorHandle handle)
            {
                if(!NeedsUseAfterFreeTracking())
                    return InvalidChildAllocatorIndex;
                var result = ChildAllocators.Length;
                ChildAllocators.Add(handle);
                handle.Parent = this;
                handle.IndexInParent = result;
                return result;
            }

            internal bool TryRemoveChildAllocator(AllocatorHandle handle, int childAllocatorIndex)
            {
                if(!NeedsUseAfterFreeTracking())
                    return false;
                if(childAllocatorIndex == InvalidChildAllocatorIndex)
                    return false;
                childAllocatorIndex = math.min(childAllocatorIndex, ChildAllocators.Length - 1);
                while(childAllocatorIndex >= 0)
                {
                    unsafe
                    {
                        var allocatorHandle = ChildAllocators.Ptr + childAllocatorIndex;
                        if(AreTheSame(*allocatorHandle, handle))
                        {
                            ChildAllocators.RemoveAtSwapBack(childAllocatorIndex);
                            return true;
                        }
                    }
                    --childAllocatorIndex;
                }
                return false;
            }

            // when you rewind an allocator, it invalidates and unregisters all of its child allocators - allocators that use as
            // backing memory, memory that was allocated from this (parent) allocator. the rewind operation was itself unmanaged,
            // until we added a managed global table of delegates alongside the unmanaged global table of function pointers. once
            // this table was added, the "unregister" extension function became managed, because it manipulates a managed array of
            // delegates.

            // a workaround (UnmanagedUnregister) was found that makes it possible for rewind to become unmanaged again: only in
            // the case that we rewind an allocator and invalidate all of its child allocators, we then unregister the child
            // allocators without touching the managed array of delegates as well.

            // this can "leak" delegates - it's possible for this to cause us to hold onto a GC reference to a delegate until
            // the end of the program, long after the delegate is no longer needed. but, there are only 65,536 such slots to
            // burn, and delegates are small data structures, and the leak ends when a delegate slot is reused, and most importantly,
            // when we've rewound an allocator while child allocators remain registered, we are likely before long to encounter
            // a use-before-free crash or a safety handle violation, both of which are likely to terminate the session before
            // anything can leak.

            internal void InvalidateDependents()
            {
                if (!NeedsUseAfterFreeTracking())
                    return;
                ChildSpinLock.Acquire();
                for (var i = 0; i < ChildSafetyHandles.Length; ++i)
                {
                    unsafe
                    {
                        AtomicSafetyHandle* handle = ChildSafetyHandles.Ptr + i;
                        if(CheckExists(*handle))
                            AtomicSafetyHandle.Release(*handle);
                    }
                }
                ChildSafetyHandles.Clear();
                ChildSafetyHandles.TrimExcess();
                ChildSpinLock.Release();
                if (Parent.IsValid)
                    Parent.TryRemoveChildAllocator(this, IndexInParent);
                Parent = default;
                IndexInParent = InvalidChildAllocatorIndex;
                for (var i = 0; i < ChildAllocators.Length; ++i)
                {
                    unsafe
                    {
                        AllocatorHandle* handle = (AllocatorHandle*)ChildAllocators.Ptr + i;
                        if (handle->IsValid)
                            handle->UnmanagedUnregister(); // see above comment
                    }
                }
                ChildAllocators.Clear();
                ChildAllocators.TrimExcess();
            }


            /// <summary>
            /// Implicitly convert an Allocator to an AllocatorHandle with its Version being reset to 0.
            /// </summary>
            /// <param name="a">The Allocator to convert.</param>
            /// <returns>The AllocatorHandle of an allocator.</returns>
            public static implicit operator AllocatorHandle(Allocator a) => new AllocatorHandle
            {
                Index = (ushort)((uint)a & 0xFFFF),
                Version = 0
            };

            /// <summary>
            /// This allocator's index into the global table of allocator functions.
            /// </summary>
            /// <value>This allocator's index into the global table of allocator functions.</value>
            public ushort Index;

            /// <summary>
            /// This allocator's version number.
            /// </summary>
            /// <remarks>An allocator function is uniquely identified by its *combination* of <see cref="Index"/> and <see cref="Version"/> together: each
            /// index has a version number that starts at 0; the version number is incremented each time the allocator is invalidated.  Only the
            /// lower 15 bits of Version is in use because when allocator version is larger than 0x7FFF, allocator.ToAllocator returns a negative value
            /// which causes problem when comparing to Allocator.None.
            /// </remarks>
            /// <value>This allocator's version number.</value>
            public ushort Version;

            /// <summary>
            /// The <see cref="Index"/> cast to int.
            /// </summary>
            /// <value>The <see cref="Index"/> cast to int.</value>
            public int Value => Index;

            /// <summary>
            /// Allocates a block from this allocator.
            /// </summary>
            /// <typeparam name="T">The type of value to allocate for.</typeparam>
            /// <param name="block">Outputs the allocated block.</param>
            /// <param name="items">The number of values to allocate for.</param>
            /// <returns>0 if successful. Otherwise, returns the error code from the allocator function.</returns>
            public int TryAllocateBlock<T>(out Block block, int items) where T : unmanaged
            {
                block = new Block
                {
                    Range = new Range { Items = items, Allocator = this },
                    BytesPerItem = sizeof(T),
                    Alignment = 1 << math.min(3, math.tzcnt(sizeof(T)))
                };
                var returnCode = Try(ref block);
                return returnCode;
            }

            /// <summary>
            /// Allocates a block with this allocator function.
            /// </summary>
            /// <typeparam name="T">The type of value to allocate for.</typeparam>
            /// <param name="items">The number of values to allocate for.</param>
            /// <returns>The allocated block.</returns>
            /// <exception cref="ArgumentException">Thrown if the allocator is not valid or if the allocation failed.</exception>
            public Block AllocateBlock<T>(int items) where T : unmanaged
            {
                CheckValid(this);
                var error = TryAllocateBlock<T>(out Block block, items);
                CheckAllocatedSuccessfully(error);
                return block;
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
            static void CheckAllocatedSuccessfully(int error)
            {
                if (error != 0)
                    throw new ArgumentException($"Error {error}: Failed to Allocate");
            }

            /// <summary>
            /// For internal use only.
            /// </summary>
            /// <value>For internal use only.</value>
            public TryFunction Function => default;

            /// <summary>
            /// Tries to allocate the block with this allocator.
            /// </summary>
            /// <param name="block">The block to allocate.</param>
            /// <returns>0 if successful. Otherwise, returns an error code.</returns>
            public int Try(ref Block block)
            {
                block.Range.Allocator = this;
                var error = AllocatorManager.Try(ref block);
                return error;
            }

            /// <summary>
            /// This handle.
            /// </summary>
            /// <value>This handle.</value>
            public AllocatorHandle Handle { get { return this; } set { this = value; } }

            /// <summary>
            /// Retrieve the Allocator associated with this allocator handle.
            /// </summary>
            /// <value>The Allocator retrieved.</value>
            public Allocator ToAllocator
            {
                get
                {
                    uint lo = Index;
                    uint hi = Version;
                    uint value = (hi << 16) | lo;
                    return (Allocator)value;
                }
            }

            /// <summary>
            /// Check whether this allocator is a custom allocator.
            /// </summary>
            /// <remarks>The AllocatorHandle is a custom allocator if its Index is larger or equal to `FirstUserIndex`.</remarks>
            /// <value>True if this AllocatorHandle is a custom allocator.</value>
            public bool IsCustomAllocator { get { return this.Index >= FirstUserIndex; } }

            /// <summary>
            /// Check whether this allocator will automatically dispose allocations.
            /// </summary>
            /// <value>True if allocations made by this AllocatorHandle are not automatically disposed.</value>
            public bool IsAutoDispose { get { return ((SharedStatics.IsAutoDispose.Ref.Data.ElementAt(Index >> 6) >> (Index & 63)) & 1) != 0; } }

            /// <summary>
            /// Dispose the allocator.
            /// </summary>
            public void Dispose()
            {
                Rewind();
                ChildAllocators.Dispose();
                ChildSafetyHandles.Dispose();
                TableEntry = default;
            }

            /// <summary>
            /// <seealso cref="Unity.Collections.AllocatorManager.AllocatorHandle"/> instances are equal if they refer to the same instance at the same version.
            /// </summary>
            /// <param name="obj">Object containing an <seealso cref="Unity.Collections.AllocatorManager.AllocatorHandle"/>.</param>
            /// <returns>Returns true if both handles are for the same allocator instance at the same version, otherwise false.</returns>
            public override bool Equals(object obj)
            {
                if (obj is AllocatorHandle)
                    return Value == ((AllocatorHandle) obj).Value;

                if (obj is Allocator)
                    return ToAllocator == ((Allocator)obj);

                return false;
            }

            /// <summary>
            /// <seealso cref="Unity.Collections.AllocatorManager.AllocatorHandle"/> instances are equal if they refer to the same instance at the same version.
            /// </summary>
            /// <param name="other"><seealso cref="Unity.Collections.AllocatorManager.AllocatorHandle"/> to compare against.</param>
            /// <returns>Returns true if both handles are for the same allocator instance at the same version, otherwise false.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(AllocatorHandle other)
            {
                return Value == other.Value;
            }

            /// <summary>
            /// <seealso cref="Unity.Collections.AllocatorManager.AllocatorHandle"/> instances are equal if they refer to the same instance at the same version.
            /// </summary>
            /// <param name="other"><seealso cref="Unity.Collections.Allocator"/> to compare against.</param>
            /// <returns>Returns true if both handles are for the same allocator instance at the same version, otherwise false.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Equals(Allocator other)
            {
                return ToAllocator == other;
            }

            /// <summary>
            /// A hash used for comparisons.
            /// </summary>
            /// <returns>A unique hash code.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode()
            {
                return Value;
            }

            /// <summary>
            /// Evaluates if one <seealso cref="Unity.Collections.AllocatorManager.AllocatorHandle"/> is equal to the other.
            /// </summary>
            /// <param name="lhs">The left-hand side</param>
            /// <param name="rhs">The right-hand side</param>
            /// <returns>True if the left-hand side's <seealso cref="Unity.Collections.AllocatorManager.AllocatorHandle"/> is equal to the right-hand side's.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator ==(AllocatorHandle lhs, AllocatorHandle rhs)
            {
                return lhs.Value == rhs.Value;
            }

            /// <summary>
            /// Evaluates if one <seealso cref="Unity.Collections.AllocatorManager.AllocatorHandle"/> is not equal to the other.
            /// </summary>
            /// <param name="lhs">The left-hand side</param>
            /// <param name="rhs">The right-hand side</param>
            /// <returns>True if the left-hand side's <seealso cref="Unity.Collections.AllocatorManager.AllocatorHandle"/> is not equal to the right-hand side's.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator !=(AllocatorHandle lhs, AllocatorHandle rhs)
            {
                return lhs.Value != rhs.Value;
            }

            /// <summary>
            /// Evaluates if one <seealso cref="Unity.Collections.AllocatorManager.AllocatorHandle"/> is less than the other.
            /// </summary>
            /// <param name="lhs">The left-hand side</param>
            /// <param name="rhs">The right-hand side</param>
            /// <returns>True if the left-hand side's <seealso cref="Unity.Collections.AllocatorManager.AllocatorHandle"/> is less than the right-hand side's.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator <(AllocatorHandle lhs, AllocatorHandle rhs)
            {
                return lhs.Value < rhs.Value;
            }

            /// <summary>
            /// Evaluates if one <seealso cref="Unity.Collections.AllocatorManager.AllocatorHandle"/> is greater than the other.
            /// </summary>
            /// <param name="lhs">The left-hand side</param>
            /// <param name="rhs">The right-hand side</param>
            /// <returns>True if the left-hand side's <seealso cref="Unity.Collections.AllocatorManager.AllocatorHandle"/> is greater than the right-hand side's.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator >(AllocatorHandle lhs, AllocatorHandle rhs)
            {
                return lhs.Value > rhs.Value;
            }

            /// <summary>
            /// Evaluates if one <seealso cref="Unity.Collections.AllocatorManager.AllocatorHandle"/> is less than or equal to the other.
            /// </summary>
            /// <param name="lhs">The left-hand side</param>
            /// <param name="rhs">The right-hand side</param>
            /// <returns>True if the left-hand side's <seealso cref="Unity.Collections.AllocatorManager.AllocatorHandle"/> is less than or equal to the right-hand side's.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator <=(AllocatorHandle lhs, AllocatorHandle rhs)
            {
                return lhs.Value <= rhs.Value;
            }

            /// <summary>
            /// Evaluates if one <seealso cref="Unity.Collections.AllocatorManager.AllocatorHandle"/> is greater than or equal to the other.
            /// </summary>
            /// <param name="lhs">The left-hand side</param>
            /// <param name="rhs">The right-hand side</param>
            /// <returns>True if the left-hand side's <seealso cref="Unity.Collections.AllocatorManager.AllocatorHandle"/> is greater than or equal to the right-hand side's.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool operator >=(AllocatorHandle lhs, AllocatorHandle rhs)
            {
                return lhs.Value >= rhs.Value;
            }

            /// <summary>
            /// Compare this <seealso cref="Unity.Collections.AllocatorManager.AllocatorHandle"/> against a given one
            /// </summary>
            /// <param name="other">The other <seealso cref="Unity.Collections.AllocatorManager.AllocatorHandle"/> to compare to</param>
            /// <returns>Difference between <seealso cref="Unity.Collections.AllocatorManager.AllocatorHandle"/> values</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int CompareTo(AllocatorHandle other)
            {
                return Value - other.Value;
            }
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct BlockHandle
        {
            /// <summary>
            /// Represents the handle.
            /// </summary>
            /// <value>Represents the handle.</value>
            public ushort Value;
        }

        /// <summary>
        /// A range of allocated memory.
        /// </summary>
        /// <remarks>The name is perhaps misleading: only in combination with a <see cref="Block"/> does
        /// a `Range` have sufficient information to represent the number of bytes in an allocation. The reason `Range` is its own type that's separate from `Block`
        /// stems from some efficiency concerns in the implementation details. In most cases, a `Range` is only used in conjunction with an associated `Block`.
        /// </remarks>
        [StructLayout(LayoutKind.Sequential)]
        public struct Range : IDisposable
        {
            /// <summary>
            /// Pointer to the start of this range.
            /// </summary>
            /// <value>Pointer to the start of this range.</value>
            public IntPtr Pointer; //  0

            /// <summary>
            /// Number of items allocated in this range.
            /// </summary>
            /// <remarks>The actual allocation may be larger. See <see cref="Block.AllocatedItems"/>.</remarks>
            /// <value>Number of items allocated in this range. </value>
            public int Items; //  8

            /// <summary>
            /// The allocator function used for this range.
            /// </summary>
            /// <value>The allocator function used for this range.</value>
            public AllocatorHandle Allocator; // 12

            /// <summary>
            /// Deallocates the memory represented by this range.
            /// </summary>
            /// <remarks>
            /// Same as disposing the <see cref="Block"/> which contains this range.
            ///
            /// Cannot be used with allocators which need the allocation size to deallocate.
            /// </remarks>
            public void Dispose()
            {
                Block block = new Block { Range = this };
                block.Dispose();
                this = block.Range;
            }
        }

        /// <summary>
        /// Represents an individual allocation within an allocator.
        /// </summary>
        /// <remarks>A block consists of a <see cref="Range"/> plus metadata about the type of elements for which the block was allocated.</remarks>
        [StructLayout(LayoutKind.Sequential)]
        public struct Block : IDisposable
        {
            /// <summary>
            /// The range of memory encompassed by this block.
            /// </summary>
            /// <value>The range of memory encompassed by this block.</value>
            public Range Range;

            /// <summary>
            /// Number of bytes per item.
            /// </summary>
            /// <value>Number of bytes per item.</value>
            public int BytesPerItem;

            /// <summary>
            /// Number of items allocated for.
            /// </summary>
            /// <value>Number of items allocated for.</value>
            public int AllocatedItems;

            /// <summary>
            /// Log2 of the byte alignment.
            /// </summary>
            /// <remarks>The alignment must always be power of 2. Storing the alignment as its log2 helps enforces this.</remarks>
            /// <value>Log2 of the byte alignment.</value>
            public byte Log2Alignment;

            /// <summary>
            /// This field only exists to pad the `Block` struct. Ignore it.
            /// </summary>
            /// <value>This field only exists to pad the `Block` struct. Ignore it.</value>
            public byte Padding0;

            /// <summary>
            /// This field only exists to pad the `Block` struct. Ignore it.
            /// </summary>
            /// <value>This field only exists to pad the `Block` struct. Ignore it.</value>
            public ushort Padding1;

            /// <summary>
            /// This field only exists to pad the `Block` struct. Ignore it.
            /// </summary>
            /// <value>This field only exists to pad the `Block` struct. Ignore it.</value>
            public uint Padding2;

            /// <summary>
            /// Number of bytes requested for this block.
            /// </summary>
            /// <remarks>The actual allocation size may be larger due to alignment.</remarks>
            /// <value>Number of bytes requested for this block.</value>
            public long Bytes => (long) BytesPerItem * Range.Items;

            /// <summary>
            /// Number of bytes allocated for this block.
            /// </summary>
            /// <remarks>The requested allocation size may be smaller. Any excess is due to alignment</remarks>
            /// <value>Number of bytes allocated for this block.</value>
            public long AllocatedBytes => (long) BytesPerItem * AllocatedItems;

            /// <summary>
            /// The alignment.
            /// </summary>
            /// <remarks>Must be power of 2 that's greater than or equal to 0.
            ///
            /// Set alignment *before* the allocation is made. Setting it after has no effect on the allocation.</remarks>
            /// <param name="value">A new alignment. If not a power of 2, it will be rounded up to the next largest power of 2.</param>
            /// <value>The alignment.</value>
            public int Alignment
            {
                get => 1 << Log2Alignment;
                set => Log2Alignment = (byte)(32 - math.lzcnt(math.max(1, value) - 1));
            }

            /// <summary>
            /// Deallocates this block.
            /// </summary>
            /// <remarks>Same as <see cref="TryAllocate"/>.</remarks>
            public void Dispose()
            {
                TryFree();
            }

            /// <summary>
            /// Attempts to allocate this block.
            /// </summary>
            /// <returns>0 if successful. Otherwise, returns the error code from the allocator function.</returns>
            public int TryAllocate()
            {
                Range.Pointer = IntPtr.Zero;
                return Try(ref this);
            }

            /// <summary>
            /// Attempts to free this block.
            /// </summary>
            /// <returns>0 if successful. Otherwise, returns the error code from the allocator function.</returns>
            public int TryFree()
            {
                Range.Items = 0;
                return Try(ref this);
            }

            /// <summary>
            /// Allocates this block.
            /// </summary>
            /// <exception cref="ArgumentException">Thrown if safety checks are enabled and the allocation fails.</exception>
            public void Allocate()
            {
                var error = TryAllocate();
                CheckFailedToAllocate(error);
            }

            /// <summary>
            /// Frees the block.
            /// </summary>
            /// <exception cref="ArgumentException">Thrown if safety checks are enabled and the deallocation fails.</exception>
            public void Free()
            {
                var error = TryFree();
                CheckFailedToFree(error);
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
            void CheckFailedToAllocate(int error)
            {
                if (error != 0)
                    throw new ArgumentException($"Error {error}: Failed to Allocate {this}");
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
            void CheckFailedToFree(int error)
            {
                if (error != 0)
                    throw new ArgumentException($"Error {error}: Failed to Free {this}");
            }
        }

        /// <summary>
        /// An allocator function pointer.
        /// </summary>
        public interface IAllocator : IDisposable
        {
            /// <summary>
            /// The allocator function. It can allocate, deallocate, or reallocate.
            /// </summary>
            TryFunction Function { get; }

            /// <summary>
            /// Invoke the allocator function.
            /// </summary>
            /// <param name="block">The block to allocate, deallocate, or reallocate. See <see cref="AllocatorManager.Try"/></param>
            /// <returns>0 if successful. Otherwise, returns the error code from the allocator function.</returns>
            int Try(ref Block block);

            /// <summary>
            /// This allocator.
            /// </summary>
            /// <value>This allocator.</value>
            AllocatorHandle Handle { get; set; }

            /// <summary>
            /// Cast the Allocator index into Allocator
            /// </summary>
            Allocator ToAllocator { get; }

            /// <summary>
            /// Check whether an allocator is a custom allocator
            /// </summary>
            bool IsCustomAllocator { get; }

            /// <summary>
            /// Check whether an allocator will automatically dispose allocations.
            /// </summary>
            /// <remarks>Allocations made by allocator are not automatically disposed by default.</remarks>
            bool IsAutoDispose { get { return false; } }
        }

        /// <summary>
        /// Memory allocation Success status
        /// </summary>
        public const int kErrorNone = 0;

        /// <summary>
        /// Memory allocation Buffer Overflow status
        /// </summary>
        public const int kErrorBufferOverflow = -1;

        [BurstDiscard]
        private static void CheckDelegate(ref bool useDelegate)
        {
            //@TODO: This should use BurstCompiler.IsEnabled once that is available as an efficient API.
            useDelegate = true;
        }

        private static bool UseDelegate()
        {
            bool result = false;
            CheckDelegate(ref result);
            return result;
        }

        private static int allocate_block(ref Block block)
        {
            TableEntry tableEntry = default;
            tableEntry = block.Range.Allocator.TableEntry;
            var function = new FunctionPointer<TryFunction>(tableEntry.function);
            // this is a path for bursted caller, for non-Burst C#, it generates garbage each time we call Invoke
            return function.Invoke(tableEntry.state, ref block);
        }

        [BurstDiscard]
        private static void forward_mono_allocate_block(ref Block block, ref int error)
        {
            TableEntry tableEntry = default;
            tableEntry = block.Range.Allocator.TableEntry;

            var index = block.Range.Allocator.Handle.Index;
            if (index >= MaxNumCustomAllocators)
            {
                throw new ArgumentException("Allocator index into TryFunction delegate table exceeds maximum.");
            }
            ref TryFunction function = ref Managed.TryFunctionDelegates[block.Range.Allocator.Handle.Index];
            error = function(tableEntry.state, ref block);
        }

        internal static Allocator LegacyOf(AllocatorHandle handle)
        {
            if (handle.Value >= FirstUserIndex)
                return Allocator.Persistent;
            return (Allocator) handle.Value;
        }

        static unsafe int TryLegacy(ref Block block)
        {
            if (block.Range.Pointer == IntPtr.Zero) // Allocate
            {
                block.Range.Pointer = (IntPtr)Memory.Unmanaged.Allocate(block.Bytes, block.Alignment, LegacyOf(block.Range.Allocator));
                block.AllocatedItems = block.Range.Items;
                return (block.Range.Pointer == IntPtr.Zero) ? -1 : 0;
            }
            if (block.Bytes == 0) // Free
            {
                if (LegacyOf(block.Range.Allocator) != Allocator.None)
                {
                    Memory.Unmanaged.Free((void*)block.Range.Pointer, LegacyOf(block.Range.Allocator));
                }
                block.Range.Pointer = IntPtr.Zero;
                block.AllocatedItems = 0;
                return 0;
            }
            // Reallocate (keep existing pointer and change size if possible. otherwise, allocate new thing and copy)
            return -1;
        }

        /// <summary>
        /// Invokes the allocator function of a block.
        /// </summary>
        /// <remarks>The allocator function is looked up from a global table.
        ///
        /// - If the block range's Pointer is null, it will allocate.
        /// - If the block range's Pointer is not null, it will reallocate.
        /// - If the block range's Items is 0, it will deallocate.
        /// </remarks>
        /// <param name="block">The block to allocate, deallocate, or reallocate.</param>
        /// <returns>0 if successful. Otherwise, returns the error code from the block's allocator function.</returns>
        public static unsafe int Try(ref Block block)
        {
            if (block.Range.Allocator.Value < FirstUserIndex)
                return TryLegacy(ref block);
            TableEntry tableEntry = default;
            tableEntry = block.Range.Allocator.TableEntry;
            var function = new FunctionPointer<TryFunction>(tableEntry.function);
            // if the allocator being passed in has a version of 0, that means "whatever the current version is."
            // so we patch it here, with whatever the current version is...
            if (block.Range.Allocator.Version == 0)
                block.Range.Allocator.Version = block.Range.Allocator.OfficialVersion;

            if (UseDelegate())
            {
                int error = kErrorNone;
                forward_mono_allocate_block(ref block, ref error);
                return error;
            }
            return allocate_block(ref block);
        }

        /// <summary>
        /// A stack allocator with no storage of its own. Uses the storage of its parent.
        /// </summary>
        [BurstCompile]
        internal struct StackAllocator : IAllocator, IDisposable
        {
            public AllocatorHandle Handle { get { return m_handle; } set { m_handle = value; } }
            public Allocator ToAllocator { get { return m_handle.ToAllocator; } }
            public bool IsCustomAllocator { get { return m_handle.IsCustomAllocator; } }

            internal AllocatorHandle m_handle;

            internal Block m_storage;
            internal long m_top;

            public void Initialize(Block storage)
            {
                m_storage = storage;
                m_top = 0;
                m_storage.Range.Allocator.AddChildAllocator(Handle);
            }

            public unsafe int Try(ref Block block)
            {
                if (block.Range.Pointer == IntPtr.Zero) // Allocate
                {
                    if (m_top + block.Bytes > m_storage.Bytes)
                    {
                        return -1;
                    }

                    block.Range.Pointer = (IntPtr)((byte*)m_storage.Range.Pointer + m_top);
                    block.AllocatedItems = block.Range.Items;
                    m_top += block.Bytes;
                    return 0;
                }

                if (block.Bytes == 0) // Free
                {
                    if ((byte*)block.Range.Pointer - (byte*)m_storage.Range.Pointer == (long)(m_top - block.AllocatedBytes))
                    {
                        m_top -= block.AllocatedBytes;
                        var blockSizeInBytes = block.AllocatedItems * block.BytesPerItem;
                        block.Range.Pointer = IntPtr.Zero;
                        block.AllocatedItems = 0;
                        return 0;
                    }

                    return -1;
                }

                // Reallocate (keep existing pointer and change size if possible. otherwise, allocate new thing and copy)
                return -1;
            }

            [BurstCompile]
			[MonoPInvokeCallback(typeof(TryFunction))]
            public static unsafe int Try(IntPtr allocatorState, ref Block block)
            {
                return ((StackAllocator*)allocatorState)->Try(ref block);
            }

            public TryFunction Function => Try;

            public void Dispose()
            {
                m_handle.Rewind();
            }
        }

        /// <summary>
        /// Slab allocator with no backing storage.
        /// </summary>
        [BurstCompile]
        internal struct SlabAllocator : IAllocator, IDisposable
        {
            public AllocatorHandle Handle { get { return m_handle; } set { m_handle = value; } }

            public Allocator ToAllocator { get { return m_handle.ToAllocator; } }

            public bool IsCustomAllocator { get { return m_handle.IsCustomAllocator; } }

            internal AllocatorHandle m_handle;

            internal Block Storage;
            internal int Log2SlabSizeInBytes;
            internal FixedList4096Bytes<int> Occupied;
            internal long budgetInBytes;
            internal long allocatedBytes;

            public long BudgetInBytes => budgetInBytes;

            public long AllocatedBytes => allocatedBytes;

            internal int SlabSizeInBytes
            {
                get => 1 << Log2SlabSizeInBytes;
                set => Log2SlabSizeInBytes = (byte)(32 - math.lzcnt(math.max(1, value) - 1));
            }

            internal int Slabs => (int)(Storage.Bytes >> Log2SlabSizeInBytes);

            internal void Initialize(Block storage, int slabSizeInBytes, long budget)
            {
                storage.Range.Allocator.AddChildAllocator(Handle);
                Assert.IsTrue((slabSizeInBytes & (slabSizeInBytes - 1)) == 0);
                Storage = storage;
                Log2SlabSizeInBytes = 0;
                Occupied = default;
                budgetInBytes = budget;
                allocatedBytes = 0;
                SlabSizeInBytes = slabSizeInBytes;
                Occupied.Length = (Slabs + 31) / 32;
            }

            public int Try(ref Block block)
            {
                if (block.Range.Pointer == IntPtr.Zero) // Allocate
                {
                    if (block.Bytes + allocatedBytes > budgetInBytes)
                        return -2; //over allocator budget
                    if (block.Bytes > SlabSizeInBytes)
                        return -1;
                    for (var wordIndex = 0; wordIndex < Occupied.Length; ++wordIndex)
                    {
                        var word = Occupied[wordIndex];
                        if (word == -1)
                            continue;
                        for (var bitIndex = 0; bitIndex < 32; ++bitIndex)
                            if ((word & (1 << bitIndex)) == 0)
                            {
                                Occupied[wordIndex] |= 1 << bitIndex;
                                block.Range.Pointer = Storage.Range.Pointer +
                                    (int)(SlabSizeInBytes * (wordIndex * 32U + bitIndex));
                                block.AllocatedItems = SlabSizeInBytes / block.BytesPerItem;
                                allocatedBytes += block.Bytes;
                                return 0;
                            }
                    }

                    return -1;
                }

                if (block.Bytes == 0) // Free
                {
                    var slabIndex = ((ulong)block.Range.Pointer - (ulong)Storage.Range.Pointer) >>
                        Log2SlabSizeInBytes;
                    int wordIndex = (int)(slabIndex >> 5);
                    int bitIndex = (int)(slabIndex & 31);
                    Occupied[wordIndex] &= ~(1 << bitIndex);
                    block.Range.Pointer = IntPtr.Zero;
                    var blockSizeInBytes = block.AllocatedItems * block.BytesPerItem;
                    allocatedBytes -= blockSizeInBytes;
                    block.AllocatedItems = 0;
                    return 0;
                }

                // Reallocate (keep existing pointer and change size if possible. otherwise, allocate new thing and copy)
                return -1;
            }

            [BurstCompile]
			[MonoPInvokeCallback(typeof(TryFunction))]
            public static unsafe int Try(IntPtr allocatorState, ref Block block)
            {
                return ((SlabAllocator*)allocatorState)->Try(ref block);
            }

            public TryFunction Function => Try;

            public void Dispose()
            {
                m_handle.Rewind();
            }
        }

        internal struct TableEntry
        {
            internal IntPtr function;
            internal IntPtr state;
        }

        internal struct Array16<T> where T : unmanaged
        {
            internal T f0, f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11, f12, f13, f14, f15;
        }
        internal struct Array256<T> where T : unmanaged
        {
            internal Array16<T> f0, f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11, f12, f13, f14, f15;
        }
        internal struct Array4096<T> where T : unmanaged
        {
            internal Array256<T> f0, f1, f2, f3, f4, f5, f6, f7, f8, f9, f10, f11, f12, f13, f14, f15;
        }
        internal struct Array32768<T> : IIndexable<T> where T : unmanaged
        {
            internal Array4096<T> f0, f1, f2, f3, f4, f5, f6, f7;
            public int Length { get { return 32768; } set {} }
            public ref T ElementAt(int index)
            {
                unsafe { fixed(Array4096<T>* p = &f0) { return ref UnsafeUtility.AsRef<T>((T*)p + index); } }
            }
        }

        /// <summary>
        /// Contains arrays of the allocator function pointers.
        /// </summary>
        internal sealed class SharedStatics
        {
            internal sealed class IsInstalled { internal static readonly SharedStatic<Long1024> Ref = SharedStatic<Long1024>.GetOrCreate<IsInstalled>(); }
            internal sealed class TableEntry { internal static readonly SharedStatic<Array32768<AllocatorManager.TableEntry>> Ref = SharedStatic<Array32768<AllocatorManager.TableEntry>>.GetOrCreate<TableEntry>(); }
            internal sealed class IsAutoDispose { internal static readonly SharedStatic<Long1024> Ref = SharedStatic<Long1024>.GetOrCreate<IsAutoDispose>(); }
            internal sealed class ChildSpinLock { internal static readonly SharedStatic<Array32768<Spinner>> Ref = SharedStatic<Array32768<Spinner>>.GetOrCreate<ChildSpinLock>(); }
            internal sealed class ChildSafetyHandles { internal static readonly SharedStatic<Array32768<UnsafeList<AtomicSafetyHandle>>> Ref = SharedStatic<Array32768<UnsafeList<AtomicSafetyHandle>>>.GetOrCreate<ChildSafetyHandles>(); }
            internal sealed class Version { internal static readonly SharedStatic<Array32768<ushort>> Ref = SharedStatic<Array32768<ushort>>.GetOrCreate<Version>(); }
            internal sealed class ChildAllocators { internal static readonly SharedStatic<Array32768<UnsafeList<AllocatorHandle>>> Ref = SharedStatic<Array32768<UnsafeList<AllocatorHandle>>>.GetOrCreate<ChildAllocators>(); }
            internal sealed class Parent { internal static readonly SharedStatic<Array32768<AllocatorHandle>> Ref = SharedStatic<Array32768<AllocatorHandle>>.GetOrCreate<Parent>(); }
            internal sealed class IndexInParent { internal static readonly SharedStatic<Array32768<int>> Ref = SharedStatic<Array32768<int>>.GetOrCreate<IndexInParent>(); }
        }

        internal static partial class Managed
        {
            /// <summary>
            /// Global delegate table to hold TryFunction delegates for managed callers
            /// </summary>
            [NoAutoStaticsCleanup] // must survive play mode transitions to avoid NRE; cleared on code reload via OnCodeLoaded
            internal static TryFunction[] TryFunctionDelegates = new TryFunction[MaxNumCustomAllocators];

            [OnCodeLoaded]
            static void Initialize()
            {
                TryFunctionDelegates = new TryFunction[MaxNumCustomAllocators];
            }

            /// <summary>
            /// Register TryFunction delegates for managed caller to avoid garbage collections
            /// </summary>
            /// <param name="index">Index into the TryFunction delegates table.</param>
            /// <param name="function">TryFunction delegate to be registered.</param>
            [ExcludeFromBurstCompatTesting("Uses managed delegate")]
            public static void RegisterDelegate(int index, TryFunction function)
            {
                if(index >= MaxNumCustomAllocators)
                {
                    throw new ArgumentException("index to be registered in TryFunction delegate table exceeds maximum.");
                }
                // Register TryFunction delegates for managed caller to avoid garbage collections
                Managed.TryFunctionDelegates[index] = function;
            }

            /// <summary>
            /// Unregister TryFunction delegate
            /// </summary>
            /// <param name="index">Index into the TryFunction delegates table.</param>
            [ExcludeFromBurstCompatTesting("Uses managed delegate")]
            public static void UnregisterDelegate(int index)
            {
                if (index >= MaxNumCustomAllocators)
                {
                    throw new ArgumentException("index to be unregistered in TryFunction delegate table exceeds maximum.");
                }
                Managed.TryFunctionDelegates[index] = default;
            }
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        public static void Initialize()
        {
        }

        /// <summary>
        /// Saves an allocator's function pointers at a particular index in the global function table.
        /// </summary>
        /// <param name="handle">The global function table index at which to install the allocator function.</param>
        /// <param name="allocatorState">IntPtr to allocator's custom state.</param>
        /// <param name="functionPointer">The allocator function to install in the global function table.</param>
        /// <param name="function">The allocator function to install in the global function table.</param>
        /// <param name="IsAutoDispose">Flag indicating if the allocator will automatically dispose allocations.</param>
        internal static void Install(AllocatorHandle handle,
                                        IntPtr allocatorState,
                                        FunctionPointer<TryFunction> functionPointer,
                                        TryFunction function,
                                        bool IsAutoDispose = false)
        {
            if(functionPointer.Value == IntPtr.Zero)
                handle.Unregister();
            else
            {
                int error = ConcurrentMask.TryAllocate(ref SharedStatics.IsInstalled.Ref.Data, handle.Value, 1);
                if (ConcurrentMask.Succeeded(error))
                {
                    handle.Install(new TableEntry { state = allocatorState, function = functionPointer.Value });
                    Managed.RegisterDelegate(handle.Index, function);

                    // If the allocator will automatically dispose allocations.
                    if (IsAutoDispose)
                    {
                        ConcurrentMask.TryAllocate(ref SharedStatics.IsAutoDispose.Ref.Data, handle.Value, 1);
                    }
                }
            }
        }

        /// <summary>
        /// Saves an allocator's function pointers at a particular index in the global function table.
        /// </summary>
        /// <param name="handle">The global function table index at which to install the allocator function.</param>
        /// <param name="allocatorState">IntPtr to allocator's custom state.</param>
        /// <param name="function">The allocator function to install in the global function table.</param>
        internal static void Install(AllocatorHandle handle, IntPtr allocatorState, TryFunction function)
        {
            var functionPointer = (function == null)
                ? new FunctionPointer<TryFunction>(IntPtr.Zero)
                : BurstCompiler.CompileFunctionPointer(function);
            Install(handle, allocatorState, functionPointer, function);
        }

        /// <summary>
        /// Saves an allocator's function pointers in a free slot of the global function table. Thread safe.
        /// </summary>
        /// <param name="allocatorState">IntPtr to allocator's custom state.</param>
        /// <param name="functionPointer">Function pointer to create or save in the function table.</param>
        /// <param name="IsAutoDispose">Flag indicating if the allocator will automatically dispose allocations.</param>
        /// <param name="isGlobal">Flag indicating if the allocator is a global allocator.</param>
        /// <param name="globalIndex">Index into the global function table of the allocator to be created.</param>
        /// <returns>Returns a handle to the newly registered allocator function.</returns>
        internal static AllocatorHandle Register(IntPtr allocatorState,
                                                    FunctionPointer<TryFunction> functionPointer,
                                                    bool IsAutoDispose = false,
                                                    bool isGlobal = false,
                                                    int globalIndex = 0)
        {
            int error;
            int offset;
            if (isGlobal)
            {
                if (globalIndex < GlobalAllocatorBaseIndex)
                {
                    throw new ArgumentException($"Error: {globalIndex} is less than GlobalAllocatorBaseIndex");
                }
                error = ConcurrentMask.TryAllocate(ref SharedStatics.IsInstalled.Ref.Data, globalIndex, 1);
                offset = globalIndex;
            }
            else
            {
                error = ConcurrentMask.TryAllocate(ref SharedStatics.IsInstalled.Ref.Data, out offset, (FirstUserIndex + 63) >> 6, (int)(GlobalAllocatorBaseIndex - 1), 1);
            }
            var tableEntry = new TableEntry { state = allocatorState, function = functionPointer.Value };
            AllocatorHandle handle = default;
            if(ConcurrentMask.Succeeded(error))
            {
                handle.Index = (ushort)offset;
                handle.Install(tableEntry);

                // If the allocator will automatically dispose allocations.
                if (IsAutoDispose)
                {
                    ConcurrentMask.TryAllocate(ref SharedStatics.IsAutoDispose.Ref.Data, offset, 1);
                }

                handle.Version = handle.OfficialVersion;
            }
            return handle;
        }

        static partial class AllocatorCache<T> where T : unmanaged, IAllocator
        {
            [AutoStaticsCleanup]
            public static FunctionPointer<TryFunction> TryFunction;
            [AutoStaticsCleanup]
            public static TryFunction CachedFunction;
        }

        /// <summary>
        /// Saves an allocator's function pointers in a free slot of the global function table. Thread safe.
        /// </summary>
        /// <typeparam name="T">The type of allocator to register.</typeparam>
        /// <param name="t">Reference to the allocator.</param>
        /// <param name="IsAutoDispose">Flag indicating if the allocator will automatically dispose allocations.</param>
        /// <param name="isGlobal">Flag indicating if the allocator is a global allocator.</param>
        /// <param name="globalIndex">Index into the global function table of the allocator to be created.</param>
        [ExcludeFromBurstCompatTesting("Uses managed delegate")]
        public static unsafe void Register<T>(ref this T t, bool IsAutoDispose = false, bool isGlobal = false, int globalIndex = 0) where T : unmanaged, IAllocator
        {
            FunctionPointer<TryFunction> functionPointer;
            var func = t.Function;
            if (func == null)
                functionPointer = new FunctionPointer<TryFunction>(IntPtr.Zero);
            else
            {
                if (func != AllocatorCache<T>.CachedFunction)
                {
                    AllocatorCache<T>.TryFunction = BurstCompiler.CompileFunctionPointer(func);
                    AllocatorCache<T>.CachedFunction = func;
                }
                functionPointer = AllocatorCache<T>.TryFunction;
            }
            t.Handle = Register((IntPtr)UnsafeUtility.AddressOf(ref t), functionPointer, IsAutoDispose, isGlobal, globalIndex);

            Managed.RegisterDelegate(t.Handle.Index, t.Function);

            if (!t.Handle.IsValid)
                throw new InvalidOperationException("Allocator registration succeeded, but failed to produce valid handle.");
        }

        /// <summary>
        /// Removes an allocator's function pointers from the global function table, without managed code
        /// </summary>
        /// <typeparam name="T">The type of allocator to unregister.</typeparam>
        /// <param name="t">Reference to the allocator.</param>
        public static void UnmanagedUnregister<T>(ref this T t) where T : unmanaged, IAllocator
        {
            if(t.Handle.IsInstalled)
            {
                t.Handle.Install(default);
                ConcurrentMask.TryFree(ref SharedStatics.IsInstalled.Ref.Data, t.Handle.Value, 1);
                ConcurrentMask.TryFree(ref SharedStatics.IsAutoDispose.Ref.Data, t.Handle.Value, 1);
            }
        }

        /// <summary>
        /// Removes an allocator's function pointers from the global function table.
        /// </summary>
        /// <typeparam name="T">The type of allocator to unregister.</typeparam>
        /// <param name="t">Reference to the allocator.</param>
        [ExcludeFromBurstCompatTesting("Uses managed delegate")]
        public static void Unregister<T>(ref this T t) where T : unmanaged, IAllocator
        {
            if(t.Handle.IsInstalled)
            {
                t.Handle.Dispose();
                ConcurrentMask.TryFree(ref SharedStatics.IsInstalled.Ref.Data, t.Handle.Value, 1);
                ConcurrentMask.TryFree(ref SharedStatics.IsAutoDispose.Ref.Data, t.Handle.Value, 1);
                Managed.UnregisterDelegate(t.Handle.Index);
            }
        }

        /// <summary>
        /// Create a custom allocator by allocating a backing storage to store the allocator and then register it
        /// </summary>
        /// <typeparam name="T">The type of allocator to create.</typeparam>
        /// <param name="backingAllocator">Allocator used to allocate backing storage.</param>
        /// <param name="isGlobal">Flag indicating if the allocator is a global allocator.</param>
        /// <param name="globalIndex">Index into the global function table of the allocator to be created.</param>
        /// <returns>Returns reference to the newly created allocator.</returns>
        [ExcludeFromBurstCompatTesting("Register uses managed delegate")]
        internal static ref T CreateAllocator<T>(AllocatorHandle backingAllocator, bool isGlobal = false, int globalIndex = 0)
            where T : unmanaged, IAllocator
        {
            unsafe
            {
                var allocatorPtr = (T*)Memory.Unmanaged.Allocate(sizeof(T), 16, backingAllocator);
                *allocatorPtr = default;
                ref T allocator = ref UnsafeUtility.AsRef<T>(allocatorPtr);
                Register(ref allocator, allocatorPtr->IsAutoDispose, isGlobal, globalIndex);
                return ref allocator;
            }
        }

        /// <summary>
        /// Destroy a custom allocator by unregistering the allocator and freeing its backing storage
        /// </summary>
        /// <typeparam name="T">The type of allocator to destroy.</typeparam>
        /// <param name="t">Reference to the allocator.</param>
        /// <param name="backingAllocator">Allocator used in allocating the backing storage.</param>
        [ExcludeFromBurstCompatTesting("Registration uses managed delegates")]
        internal static void DestroyAllocator<T>(ref this T t, AllocatorHandle backingAllocator)
            where T : unmanaged, IAllocator
        {
            Unregister(ref t);

            unsafe
            {
                var allocatorPtr = UnsafeUtility.AddressOf<T>(ref t);
                Memory.Unmanaged.Free(allocatorPtr, backingAllocator);
            }
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        public static void Shutdown()
        {
        }

        /// <summary>
        /// Index in the global function table of the first user-defined allocator.
        /// </summary>
        /// <remarks>The indexes from 0 up to `FirstUserIndex` are reserved and so should not be used for your own allocators.</remarks>
        /// <value>Index in the global function table of the first user-defined allocator.</value>
        public const ushort FirstUserIndex = 64;

        /// <summary>
        /// Maximum number of user-defined allocators.
        /// </summary>
        public const ushort MaxNumCustomAllocators = 32768;

        /// <summary>
        /// Number of global scratchpad allocators reserved in the global function table.
        /// </summary>
        /// <remarks>Number of global scratchpad allocators reserved in the global function table. Make sure it is larger than or equals to the max number of jobs that can run at the same time.</remarks>
        internal static readonly ushort NumGlobalScratchAllocators = (ushort) (JobsUtility.ThreadIndexCount);

        /// <summary>
        /// Max number of global allocators reserved in the global function table.
        /// </summary>
        /// <remarks>Max number of global allocators reserved in the global function table. Make sure it is larger than or equals to NumGlobalScratchAllocators.</remarks>
        internal static readonly ushort MaxNumGlobalAllocators = (ushort)(JobsUtility.ThreadIndexCount);

        /// <summary>
        /// Base index in the global function table for global allocators.
        /// </summary>
        /// <remarks>The indexes from `GlobalAllocatorBaseIndex` up to `MaxNumCustomAllocators` are reserved which
        /// should not be used for your own allocators.</remarks>
        /// <value>Base index in the global function table for global allocators.</value>
        internal static readonly uint GlobalAllocatorBaseIndex = (uint)(MaxNumCustomAllocators - MaxNumGlobalAllocators);

        /// <summary>
        /// Index in the global function table of the first global scratchpad allocator.
        /// </summary>
        /// <remarks>The indexes from `GlobalAllocatorBaseIndex` up to `NumGlobalScratchAllocators` are reserved for global scratchpad allocators.</remarks>
        /// <value>Index in the global function table of the first global scratchpad allocator.</value>
        internal static readonly uint FirstGlobalScratchpadAllocatorIndex = GlobalAllocatorBaseIndex;

        internal static bool IsCustomAllocator(AllocatorHandle allocator)
        {
            return allocator.Index >= FirstUserIndex;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        internal static void CheckFailedToAllocate(int error)
        {
            if (error != 0)
                throw new ArgumentException("failed to allocate");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        internal static void CheckFailedToFree(int error)
        {
            if (error != 0)
                throw new ArgumentException("failed to free");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        internal static void CheckValid(AllocatorHandle handle)
        {
            if (handle.IsValid == false)
                throw new ArgumentException("allocator handle is not valid.");
        }
    }

    /// <summary>
    /// Provides a wrapper for custom allocator.
    /// </summary>
    /// <typeparam name="T">The type of the allocator.</typeparam>
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(AllocatorManager.AllocatorHandle) })]
    public unsafe struct AllocatorHelper<T> : IDisposable
        where T : unmanaged, AllocatorManager.IAllocator
    {
        /// <summary>
        /// Pointer to a custom allocator.
        /// </summary>
        readonly T* m_allocator;

        /// <summary>
        /// Allocator used to allocate backing storage of T.
        /// </summary>
        AllocatorManager.AllocatorHandle m_backingAllocator;

        /// <summary>
        /// Get the custom allocator.
        /// </summary>
        public ref T Allocator => ref UnsafeUtility.AsRef<T>(m_allocator);

        /// <summary>
        /// Allocate the custom allocator from backingAllocator and register it.
        /// </summary>
        /// <param name="backingAllocator">Allocator used to allocate backing storage.</param>
        /// <param name="isGlobal">Flag indicating if the allocator is a global allocator.</param>
        /// <param name="globalIndex">Index into the global function table of the allocator to be created.</param>
        [ExcludeFromBurstCompatTesting("CreateAllocator is unburstable")]
        public AllocatorHelper(AllocatorManager.AllocatorHandle backingAllocator, bool isGlobal = false, int globalIndex = 0)
        {
// With code reloading, whenever we enter/exit the playmode, 
// we enter OnCodeLoaded scope, so we need to ensure that we don’t store old references from previous playmode sessions.
// Here, in an instance constructor, we have ensured the constructed object can't outlive the current CodeLoaded scope.
// So it's safe to suppress the UAL0015 warning. 
#pragma warning disable UAL0015
            ref var allocator = ref AllocatorManager.CreateAllocator<T>(backingAllocator, isGlobal, globalIndex);
#pragma warning restore UAL0015
            m_allocator = (T*)UnsafeUtility.AddressOf<T>(ref allocator);
            m_backingAllocator = backingAllocator;
        }

        /// <summary>
        /// Dispose the custom allocator backing memory and unregister it.
        /// </summary>
        [ExcludeFromBurstCompatTesting("DestroyAllocator is unburstable")]
        public void Dispose()
        {
            ref var allocator = ref UnsafeUtility.AsRef<T>(m_allocator);
            AllocatorManager.DestroyAllocator(ref allocator, m_backingAllocator);
        }
    }
}

#pragma warning restore 0649
