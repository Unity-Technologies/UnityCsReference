// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using Unity.Collections;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Represents a collection of <see cref="HierarchyNode"/> and values of type <typeparamref name="T"/> with O(1) access time.
    /// </summary>
    public struct HierarchyNodeMapUnmanaged<T> : IDisposable where T : unmanaged
    {
        NativeSparseArray<HierarchyNode, T> m_Values;

        /// <summary>
        /// Whether or not this object is valid and uses memory.
        /// </summary>
        public bool IsCreated
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Values.IsCreated;
        }

        /// <summary>
        /// The number of elements that can be contained in the <see cref="HierarchyNodeMapUnmanaged{T}"/> without resizing.
        /// </summary>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Values.Capacity;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => m_Values.Capacity = value;
        }

        /// <summary>
        /// The number of elements contained in the <see cref="HierarchyNodeMapUnmanaged{T}"/>.
        /// </summary>
        public int Count => m_Values.Count;

        /// <summary>
        /// Gets or sets the value associated with the specified <see cref="HierarchyNode"/>.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The value associated with the specified <see cref="HierarchyNode"/>.</returns>
        public T this[in HierarchyNode node]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Values[node];

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => m_Values[node] = value;
        }

        /// <summary>
        /// Constructs a new <see cref="HierarchyNodeMapUnmanaged{T}"/>.
        /// </summary>
        /// <param name="allocator">The memory allocator.</param>
        public HierarchyNodeMapUnmanaged(Allocator allocator)
        {
            m_Values = new NativeSparseArray<HierarchyNode, T>(KeyIndex, KeyEqual, allocator);
        }

        /// <summary>
        /// Constructs a new <see cref="HierarchyNodeMapUnmanaged{T}"/>.
        /// </summary>
        /// <param name="initValue">The value to use to initialize memory.</param>
        /// <param name="allocator">The memory allocator.</param>
        public HierarchyNodeMapUnmanaged(in T initValue, Allocator allocator)
        {
            m_Values = new NativeSparseArray<HierarchyNode, T>(in initValue, KeyIndex, KeyEqual, allocator);
        }

        /// <summary>
        /// Dispose this object and release its memory.
        /// </summary>
        public void Dispose()
        {
            m_Values.Dispose();
        }

        /// <summary>
        /// Reserve enough memory to contain the specified number of elements.
        /// </summary>
        /// <param name="capacity">The requested capacity.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reserve(int capacity)
        {
            m_Values.Reserve(capacity);
        }

        /// <summary>
        /// Determine whether or not the <see cref="HierarchyNodeMapUnmanaged{T}"/> contains the specified <see cref="HierarchyNode"/>.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the <see cref="HierarchyNodeMapUnmanaged{T}"/> contains the specified <see cref="HierarchyNode"/>, <see langword="false"/> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsKey(in HierarchyNode node)
        {
            return m_Values.ContainsKey(in node);
        }

        /// <summary>
        /// Adds the specified <see cref="HierarchyNode"/> and value to the <see cref="HierarchyNodeMapUnmanaged{T}"/>.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in HierarchyNode node, in T value)
        {
            m_Values.Add(in node, in value);
        }

        /// <summary>
        /// Adds the specified <see cref="HierarchyNode"/> and value to the <see cref="HierarchyNodeMapUnmanaged{T}"/> without increasing capacity.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="value">The value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddNoResize(in HierarchyNode node, in T value)
        {
            m_Values.AddNoResize(in node, in value);
        }

        /// <summary>
        /// Attempts to add the specified <see cref="HierarchyNode"/> and value to the <see cref="HierarchyNodeMapUnmanaged{T}"/>.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="value">The value.</param>
        /// <returns><see langword="true"/> if the <see cref="HierarchyNode"/>/value pair was added to the <see cref="HierarchyNodeMapUnmanaged{T}"/>, <see langword="false"/> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(in HierarchyNode node, in T value)
        {
            return m_Values.TryAdd(in node, in value);
        }

        /// <summary>
        /// Attempts to add the specified <see cref="HierarchyNode"/> and value to the <see cref="HierarchyNodeMapUnmanaged{T}"/> without increasing capacity.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="value">The value.</param>
        /// <returns><see langword="true"/> if the <see cref="HierarchyNode"/>/value pair was added to the <see cref="HierarchyNodeMapUnmanaged{T}"/>, <see langword="false"/> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAddNoResize(in HierarchyNode node, in T value)
        {
            return m_Values.TryAddNoResize(in node, in value);
        }

        /// <summary>
        /// Gets the value associated with the specified <see cref="HierarchyNode"/>.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="value">The value.</param>
        /// <returns><see langword="true"/> if the <see cref="HierarchyNodeMapUnmanaged{T}"/> contains the specified <see cref="HierarchyNode"/>, <see langword="false"/> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(in HierarchyNode node, out T value)
        {
            return m_Values.TryGetValue(in node, out value);
        }

        /// <summary>
        /// Removes the value with the specified <see cref="HierarchyNode"/> from the <see cref="HierarchyNodeMapUnmanaged{T}"/>.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns><see langword="true"/> if the <see cref="HierarchyNode"/> is found and removed, <see langword="false"/> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(in HierarchyNode node)
        {
            return m_Values.Remove(in node);
        }

        /// <summary>
        /// Removes all <see cref="HierarchyNode"/> and values from the <see cref="HierarchyNodeMapUnmanaged{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            m_Values.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int KeyIndex(in HierarchyNode node)
        {
            return node.Id - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool KeyEqual(in HierarchyNode lhs, in HierarchyNode rhs)
        {
            return lhs.Version == rhs.Version;
        }
    }
}
