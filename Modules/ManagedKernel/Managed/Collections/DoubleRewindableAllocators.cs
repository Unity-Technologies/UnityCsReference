// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using ManagedKernel.Assertions;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections
{
    /// <summary>
    /// A double rewindable allocators <see cref="RewindableAllocator"/>.
    /// </summary>
    unsafe public struct DoubleRewindableAllocators : IDisposable
    {
        RewindableAllocator* Pointer;
        AllocatorHelper<RewindableAllocator> UpdateAllocatorHelper0;
        AllocatorHelper<RewindableAllocator> UpdateAllocatorHelper1;

        /// <summary>
        /// Update the double rewindable allocators, switch Pointer to another allocator and rewind the newly switched allocator.
        /// </summary>
        public void Update()
        {
            var UpdateAllocator0 = (RewindableAllocator*)UnsafeUtility.AddressOf(ref UpdateAllocatorHelper0.Allocator);
            var UpdateAllocator1 = (RewindableAllocator*)UnsafeUtility.AddressOf(ref UpdateAllocatorHelper1.Allocator);
            Pointer = (Pointer == UpdateAllocator0) ? UpdateAllocator1 : UpdateAllocator0;
            CheckIsCreated();
            Allocator.Rewind();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_ENABLE_CHECKS")]
        void CheckIsCreated()
        {
            if (!IsCreated)
            {
                throw new InvalidOperationException($"DoubleRewindableAllocators is not created.");
            }
        }

        /// <summary>
        /// Retrieve the current rewindable allocator.
        /// </summary>
        /// <value>The Allocator retrieved.</value>
        public ref RewindableAllocator Allocator
        {
            get
            {
                CheckIsCreated();
                return ref UnsafeUtility.AsRef<RewindableAllocator>(Pointer);
            }
        }

        /// <summary>
        /// Check whether the double rewindable allocators is created.
        /// </summary>
        /// <value>True if current allocator is not null, otherwise false.</value>
        public bool IsCreated => Pointer != null;

        /// <summary>
        /// Construct a double rewindable allocators by allocating the allocators from backingAllocator and registering them.
        /// </summary>
        /// <param name="backingAllocator">Allocator used to allocate the double rewindable allocators.</param>
        /// <param name="initialSizeInBytes">The initial capacity of the allocators, in bytes</param>
        public DoubleRewindableAllocators(AllocatorManager.AllocatorHandle backingAllocator, int initialSizeInBytes)
        {
            this = default;
// With code reloading, whenever we enter/exit the playmode, 
// we enter OnCodeLoaded scope, so we need to ensure that we don’t store old references from previous playmode sessions.
// Here, in an instance constructor, we have ensured the constructed object can't outlive the current CodeLoaded scope.
// So it's safe to suppress the UAL0015 warning. 
#pragma warning disable UAL0015
            Initialize(backingAllocator, initialSizeInBytes);
#pragma warning restore UAL0015
        }

        /// <summary>
        /// Initialize a double rewindable allocators by allocating the allocators from backingAllocator and registering them.
        /// </summary>
        /// <param name="backingAllocator">Allocator used to allocate the double rewindable allocators.</param>
        /// <param name="initialSizeInBytes">The initial capacity of the allocators, in bytes</param>
        public void Initialize(AllocatorManager.AllocatorHandle backingAllocator, int initialSizeInBytes)
        {
            UpdateAllocatorHelper0 = new AllocatorHelper<RewindableAllocator>(backingAllocator);
            UpdateAllocatorHelper1 = new AllocatorHelper<RewindableAllocator>(backingAllocator);
            UpdateAllocatorHelper0.Allocator.Initialize(initialSizeInBytes);
            UpdateAllocatorHelper1.Allocator.Initialize(initialSizeInBytes);
            Pointer = null;
            Update();
        }

        /// <summary>
        /// the double rewindable allocators and unregister it.
        /// </summary>
        public void Dispose()
        {
            if (!IsCreated)
            {
                return;
            }

            UpdateAllocatorHelper0.Allocator.Dispose();
            UpdateAllocatorHelper1.Allocator.Dispose();

            UpdateAllocatorHelper0.Dispose();
            UpdateAllocatorHelper1.Dispose();
        }

        internal bool EnableBlockFree
        {
            get
            {
                Assert.IsTrue(UpdateAllocatorHelper0.Allocator.EnableBlockFree == UpdateAllocatorHelper1.Allocator.EnableBlockFree);
                return UpdateAllocatorHelper0.Allocator.EnableBlockFree;
            }
            set
            {
                UpdateAllocatorHelper0.Allocator.EnableBlockFree = value;
                UpdateAllocatorHelper1.Allocator.EnableBlockFree = value;
            }
        }
    }
}
