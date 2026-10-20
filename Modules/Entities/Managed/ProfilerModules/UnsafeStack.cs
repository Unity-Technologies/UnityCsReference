// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Unity.Entities.LowLevel
{
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int) })]
    internal struct UnsafeStack<T> : INativeDisposable where T : unmanaged
    {
        UnsafeList<T> m_List;

        /// <summary>
        /// Determine if the stack is empty.
        /// </summary>
        public bool IsEmpty => m_List.IsEmpty;

        /// <summary>
        /// Retrieve the number of items on the stack.
        /// </summary>
        public int Length => m_List.Length;

        /// <summary>
        /// Determine if the stack has been allocated.
        /// </summary>
        public bool IsCreated => m_List.IsCreated;

        /// <summary>
        /// Create a new stack.
        /// </summary>
        /// <param name="initialCapacity">The initial capacity of the stack.</param>
        /// <param name="allocator">The allocator for the stack container.</param>
        /// <param name="options">Initialization options for the stack allocation.</param>
        public UnsafeStack(int initialCapacity, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.UninitializedMemory)
        {
            m_List = new UnsafeList<T>(initialCapacity, allocator, options);
        }

        /// <summary>
        /// Push an item onto the stack.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Push(in T item)
        {
            m_List.Add(in item);
        }

        /// <summary>
        /// Retrieve the item on the top of the stack.
        /// </summary>
        /// <returns>The item returned by reference.</returns>
        public ref T Top()
        {
            return ref m_List.ElementAt(m_List.m_length - 1);
        }

        /// <summary>
        /// Pop the item on top of the stack.
        /// </summary>
        public void Pop()
        {
            m_List.Resize(m_List.m_length - 1);
        }

        /// <summary>
        /// Clear the stack from all its items.
        /// </summary>
        public void Clear()
        {
            m_List.Clear();
        }

        /// <summary>
        /// Dispose the memory of the stack.
        /// </summary>
        public void Dispose()
        {
            m_List.Dispose();
        }

        /// <summary>
        /// Dispose the memory of the stack.
        /// </summary>
        public JobHandle Dispose(JobHandle inputDeps)
        {
            return m_List.Dispose(inputDeps);
        }
    }
}
