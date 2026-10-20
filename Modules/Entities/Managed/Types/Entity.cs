// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Unity.Entities
{
    /// <summary>
    /// Identifies an entity.
    /// </summary>
    /// <remarks>
    /// The entity is a fundamental part of the Entity Component System. Everything in your application that has data or an
    /// identity of its own is an entity. However, an entity doesn't contain either data or behavior itself. Instead,
    /// the data is stored in the components and the behavior is provided by the systems that process those
    /// components. The entity acts as an identifier or key to the data stored in components.
    ///
    /// The <see cref="EntityManager"/> class manages entities and they exist within a <see cref="World"/>. An
    /// Entity struct refers to an entity, but isn't a reference. Rather, the Entity struct contains an
    /// <see cref="Index"/> that you can use to access entity data, and a <see cref="Version"/> that you can 
    /// use to check whether the Index is still valid. Note that you must pass the Index or Version values to  
    /// relevant API methods, rather than accessing them directly.
    ///
    /// To add or remove components, access components, or to destroy the entity, pass an Entity struct to methods of 
    /// the <see cref="EntityManager"/>, the <see cref="EntityCommandBuffer"/>, or the <see cref="ComponentSystemBase"/>. 
    /// </remarks>
    [DebuggerTypeProxy(typeof(EntityDebugProxy))]
    [DebuggerDisplay("{EntityDebugProxy.GetDebugName(Index, Version)}")]
    [StructLayout(LayoutKind.Sequential, Size = 8)]
    public struct Entity : IEquatable<Entity>, IComparable<Entity>
    {
        // Bit layout matches C++ EntityId in Modules/NativeKernel/Include/NativeKernel/BaseClasses/EntityId.h:
        //   [Version:24 | TypeId:12 | Index:28]
        // A reinterpret cast to UnityEngine.EntityId is therefore byte-identical.
        internal ulong m_RawData;

        internal const int kIndexBits   = 28;
        internal const int kTypeIdBits  = 12;
        internal const int kVersionBits = 24;

        internal const ulong kIndexMask   = (1UL << kIndexBits) - 1;
        internal const ulong kTypeIdMask  = ((1UL << kTypeIdBits) - 1) << kIndexBits;
        internal const ulong kVersionMask = ((1UL << kVersionBits) - 1) << (kIndexBits + kTypeIdBits);

        internal const int kTypeIdShift  = kIndexBits;                  // 28
        internal const int kVersionShift = kIndexBits + kTypeIdBits;    // 40

        /// <summary>
        /// The ID of an entity.
        /// </summary>
        /// <value>The index into the internal list of entities.</value>
        /// <remarks>
        /// Entity indexes are recycled when an entity is destroyed. When an entity is destroyed, the
        /// EntityManager increments the version identifier. To represent the same entity, both the Index and the
        /// Version fields of the Entity object must match. If the Index is the same, but the Version is different,
        /// then the entity has been recycled.
        /// </remarks>
        public int Index
        {
            get => (int)(m_RawData & kIndexMask);
            set => m_RawData = (m_RawData & ~kIndexMask) | ((ulong)value & kIndexMask);
        }

        /// <summary>
        /// The generational version of the entity.
        /// </summary>
        /// <remarks>The Version number can, theoretically, overflow and wrap around within the lifetime of an
        /// application. For this reason, you cannot assume that an Entity instance with a larger Version is a more
        /// recent incarnation of the entity than one with a smaller Version (and the same Index).</remarks>
        /// <value>Used to determine whether this Entity object still identifies an existing entity.</value>
        public int Version
        {
            get => (int)((m_RawData & kVersionMask) >> kVersionShift);
            set => m_RawData = (m_RawData & ~kVersionMask)
                | (((ulong)(uint)value & ((1UL << kVersionBits) - 1)) << kVersionShift);
        }

        /// <summary>
        /// Entity instances are equal if they refer to the same entity.
        /// </summary>
        /// <param name="lhs">An Entity object.</param>
        /// <param name="rhs">Another Entity object.</param>
        /// <returns>True, if both Index and Version are identical.</returns>
        public static bool operator==(Entity lhs, Entity rhs)
        {
            return lhs.m_RawData == rhs.m_RawData;
        }

        /// <summary>
        /// Entity instances are equal if they refer to the same entity.
        /// </summary>
        /// <param name="lhs">An Entity object.</param>
        /// <param name="rhs">Another Entity object.</param>
        /// <returns>True, if either Index or Version are different.</returns>
        public static bool operator!=(Entity lhs, Entity rhs)
        {
            return lhs.m_RawData != rhs.m_RawData;
        }

        /// <summary>
        /// Compare this entity against a given one
        /// </summary>
        /// <param name="other">The other entity to compare to</param>
        /// <returns>Difference based on the Entity Index value</returns>
        public int CompareTo(Entity other)
        {
            return Index - other.Index;
        }

        /// <summary>
        /// Entity instances are equal if they refer to the same entity.
        /// </summary>
        /// <param name="compare">The object to compare to this Entity.</param>
        /// <returns>True, if the compare parameter contains an Entity object having the same Index and Version
        /// as this Entity.</returns>
        public override bool Equals(object compare)
        {
            return compare is Entity compareEntity && Equals(compareEntity);
        }

        /// <summary>
        /// A hash used for comparisons.
        /// </summary>
        /// <returns>A unique hash code.</returns>
        public override int GetHashCode()
        {
            // Same Fibonacci hash as UnityEngine.EntityId.GetHashCode; identical layout means it
            // matches ((EntityId)this).GetHashCode().
            unchecked
            {
                const ulong kKnuth64 = 0x9E3779B97F4A7C15UL;
                uint hash = (uint)((m_RawData * kKnuth64) >> 32);
                return (int)(hash ^ (hash >> 16));
            }
        }

        /// <summary>
        /// A "blank" Entity object that does not refer to an actual entity.
        /// </summary>
        public static Entity Null => default;

        /// <summary>
        /// Entity instances are equal if they represent the same entity.
        /// </summary>
        /// <param name="entity">The other Entity.</param>
        /// <returns>True, if the Entity instances have the same Index and Version.</returns>
        public bool Equals(Entity entity)
        {
            return entity.m_RawData == m_RawData;
        }


        /// <summary>
        /// Converts an entity to the <see cref="EntityId"/> with the same index and version.
        /// </summary>
        /// <param name="entity">The entity to convert.</param>
        /// <returns>The corresponding EntityId.</returns>
        public static implicit operator EntityId(Entity entity)
        {
            return UnsafeUtility.As<Entity, EntityId>(ref entity);
        }


        /// <summary>
        /// Provides a debugging string.
        /// </summary>
        /// <returns>A string containing the entity index and generational version.</returns>
        public override string ToString()
        {
            return Equals(Null) ? "Entity.Null" : $"Entity({Index}:{Version})";
        }

        /// <summary>
        /// Provides a Burst compatible debugging string.
        /// </summary>
        /// <returns>A string containing the entity index and generational version.</returns>
        [GenerateTestsForBurstCompatibility]
        public FixedString64Bytes ToFixedString()
        {
            if (Equals(Null))
                return (FixedString64Bytes)"Entity.Null";

            var fs = new FixedString64Bytes();
            fs.Append((FixedString32Bytes)"Entity(");
            fs.Append(Index);
            fs.Append(':');
            fs.Append(Version);
            fs.Append(')');
            return fs;
        }
    }
}
