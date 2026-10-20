// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Unity.Entities
{
    /// <summary> Obsolete. Use <see cref="ComponentTypeSet"/> instead.</summary>
    [Obsolete("ComponentTypes has been renamed to ComponentTypeSet. (UnityUpgradable) -> ComponentTypeSet", true)]
    public struct ComponentTypes
    {
    }

    /// <summary>
    /// An immutable set of <see cref="ComponentType"/> values.
    /// </summary>
    /// <remarks>
    /// Max numbers of types is 15 (the capacity of <see cref="FixedList64Bytes{T}"/> of 32-bit integers).
    ///
    /// Values in the list are sorted by their internal type index.
    ///
    /// Only the types themselves are stored, not any access modes.
    ///
    /// Cannot contain multiple ComponentType values with the same type index (safety checks in the constructors will throw an exception).
    /// </remarks>
    [GenerateTestsForBurstCompatibility]
    [DebuggerTypeProxy(typeof(ComponentTypeSetDebugView))]
    public readonly unsafe struct ComponentTypeSet
    {
        readonly FixedList64Bytes<TypeIndex> _sorted;
        internal readonly struct Masks
        {
            public readonly UInt16 BufferMask;
            public readonly UInt16 CleanupComponentMask;
            public readonly UInt16 SharedComponentMask;
            public readonly UInt16 ZeroSizedMask;

            public Masks(in FixedList64Bytes<TypeIndex> sortedTypeIndices)
            {
                BufferMask = 0;
                CleanupComponentMask = 0;
                SharedComponentMask = 0;
                ZeroSizedMask = 0;
                for (var i = 0; i < sortedTypeIndices.Length; ++i)
                {
                    var typeIndex = sortedTypeIndices[i];
                    var mask = (UInt16)(1 << i);
                    if (typeIndex.IsBuffer)
                        BufferMask |= mask;
                    if (typeIndex.IsCleanupComponent)
                        CleanupComponentMask |= mask;
                    if (typeIndex.IsSharedComponentType)
                        SharedComponentMask |= mask;
                    if (typeIndex.IsZeroSized)
                        ZeroSizedMask |= mask;
                }
            }

            public bool IsSharedComponent(int index)
            {
                return (SharedComponentMask & (1 << index)) != 0;
            }

            public bool IsZeroSized(int index)
            {
                return (ZeroSizedMask & (1 << index)) != 0;
            }

            public int Buffers => math.countbits((UInt32)BufferMask);
            public int CleanupComponents => math.countbits((UInt32)CleanupComponentMask);
            public int SharedComponents => math.countbits((UInt32)SharedComponentMask);
            public int ZeroSizeds => math.countbits((UInt32)ZeroSizedMask);
        }

        internal readonly Masks m_masks;

        /// <summary>
        /// The component type count
        /// </summary>
        public int Length
        {
            get => _sorted.Length;
        }

        /// <summary>
        /// Get the type index of a component type within this list
        /// </summary>
        /// <param name="index">The index of the component within the types stored in this list</param>
        /// <returns>The type index of the component type at the specified index</returns>
        public TypeIndex GetTypeIndex(int index)
        {
            return _sorted[index];
        }

        /// <summary>
        /// Get a pointer to the internal array of type indices.
        /// </summary>
        /// <remarks>
        /// This pointer should only be used for read-only access
        /// </remarks>
        internal TypeIndex* UnsafeTypesPtrRO => (TypeIndex*)_sorted.Buffer;

        internal int ChunkComponentCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _sorted.Length; i++)
                {
                    if (GetComponentType(i).IsChunkComponent)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// Gets a ComponentType for the type stored at the index in the list.
        /// </summary>
        /// <param name="index">The index in the list.</param>
        /// <returns>Returns the ComponentType for the type stored at the index in the list.
        /// The returned ComponentType always has access mode ReadWrite.</returns>
        public ComponentType GetComponentType(int index)
        {
            return ComponentType.FromTypeIndex(_sorted[index]);
        }

        /// <summary>
        /// Create an instance with one component type
        /// </summary>
        /// <param name="a">A component type</param>
        public ComponentTypeSet(ComponentType a)
        {
            _sorted = new FixedList64Bytes<TypeIndex>();
            _sorted.Add(a.TypeIndex);
            m_masks = new Masks(_sorted);
        }

        /// <summary>
        /// Create an instance with two component types
        /// </summary>
        /// <param name="a">A component type</param>
        /// <param name="b">A component type</param>
        public ComponentTypeSet(ComponentType a, ComponentType b)
        {
            _sorted = new FixedList64Bytes<TypeIndex>();
            _sorted.Add(a.TypeIndex);
            _sorted.Add(b.TypeIndex);
            _sorted.Sort();
            m_masks = new Masks(_sorted);
            CheckForDuplicates();
        }

        /// <summary>
        /// Create an instance with three component types
        /// </summary>
        /// <param name="a">A component type</param>
        /// <param name="b">A component type</param>
        /// <param name="c">A component type</param>
        public ComponentTypeSet(ComponentType a, ComponentType b, ComponentType c)
        {
            _sorted = new FixedList64Bytes<TypeIndex>();
            _sorted.Add(a.TypeIndex);
            _sorted.Add(b.TypeIndex);
            _sorted.Add(c.TypeIndex);
            _sorted.Sort();
            m_masks = new Masks(_sorted);
            CheckForDuplicates();
        }

        /// <summary>
        /// Create an instance with four component types
        /// </summary>
        /// <param name="a">A component type</param>
        /// <param name="b">A component type</param>
        /// <param name="c">A component type</param>
        /// <param name="d">A component type</param>
        public ComponentTypeSet(ComponentType a, ComponentType b, ComponentType c, ComponentType d)
        {
            _sorted = new FixedList64Bytes<TypeIndex>();
            _sorted.Add(a.TypeIndex);
            _sorted.Add(b.TypeIndex);
            _sorted.Add(c.TypeIndex);
            _sorted.Add(d.TypeIndex);
            _sorted.Sort();
            m_masks = new Masks(_sorted);
            CheckForDuplicates();
        }

        /// <summary>
        /// Create an instance with five component types
        /// </summary>
        /// <param name="a">A component type</param>
        /// <param name="b">A component type</param>
        /// <param name="c">A component type</param>
        /// <param name="d">A component type</param>
        /// <param name="e">A component type</param>
        public ComponentTypeSet(ComponentType a, ComponentType b, ComponentType c, ComponentType d, ComponentType e)
        {
            _sorted = new FixedList64Bytes<TypeIndex>();
            _sorted.Add(a.TypeIndex);
            _sorted.Add(b.TypeIndex);
            _sorted.Add(c.TypeIndex);
            _sorted.Add(d.TypeIndex);
            _sorted.Add(e.TypeIndex);
            _sorted.Sort();
            m_masks = new Masks(_sorted);
            CheckForDuplicates();
        }

        /// <summary>
        /// Create an instance from a list of component types
        /// </summary>
        /// <param name="types">The list of component types</param>
        /// <exception cref="ArgumentException">Thrown if the length of <paramref name="types"/> exceeds the maximum ComponentTypes capacity (15).</exception>
        [ExcludeFromBurstCompatTesting("Takes managed array")]
        public ComponentTypeSet(ComponentType[] types)
        {
            _sorted = new FixedList64Bytes<TypeIndex>();
            if (types.Length > _sorted.Capacity)
                throw new ArgumentException($"A ComponentTypes value cannot have more than {_sorted.Capacity} types.");
            for (var i = 0; i < types.Length; ++i)
                _sorted.Add(types[i].TypeIndex);
            _sorted.Sort();
            m_masks = new Masks(_sorted);
            CheckForDuplicates();
        }

        /// <summary>
        /// Create an instance from a list of component types
        /// </summary>
        /// <param name="types">The list of component types</param>
        /// <exception cref="ArgumentException">Thrown if the length of <paramref name="types"/> exceeds the maximum ComponentTypes capacity (15).</exception>
        public ComponentTypeSet(in FixedList128Bytes<ComponentType> types)
        {
            _sorted = new FixedList64Bytes<TypeIndex>();
            if (types.Length > _sorted.Capacity)
                throw new ArgumentException($"A ComponentTypes value cannot have more than {_sorted.Capacity} types.");
            for (var i = 0; i < types.Length; ++i)
                _sorted.Add(types[i].TypeIndex);
            _sorted.Sort();
            m_masks = new Masks(_sorted);
            CheckForDuplicates();
        }

        // Assumes _sorted has already been sorted.
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private readonly void CheckForDuplicates()
        {
            if (Hint.Unlikely(_sorted.IsEmpty))
                return;
            var prev = _sorted[0];
            for (int i = 1; i < _sorted.Length; i++)
            {
                var current = _sorted[i];
                if (Hint.Unlikely(prev == current))
                {
                    throw new ArgumentException(
                        $"ComponentTypes cannot contain duplicate types. Remove all but one occurrence of \"{GetComponentType(i).ToString()}\"");
                }
                prev = current;
            }
        }
    }
}
