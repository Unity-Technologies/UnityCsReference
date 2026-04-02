// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using JetBrains.Annotations;
using Unity.GraphToolkit.Editor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.GraphToolkit
{
    /// <summary>
    /// The type representing an unknown type.
    /// </summary>
    [UnityRestricted]
    internal class Unknown
    {
        Unknown() { }
    }

    /// <summary>
    /// The type for missing ports.
    /// </summary>
    [UnityRestricted]
    internal class MissingPort
    {
        MissingPort() { }
    }

    /// <summary>
    /// The type for untyped ports.
    /// </summary>
    [UnityRestricted]
    internal class Untyped
    {
        Untyped() { }
    }

    /// <summary>
    /// The type for subgraphs.
    /// </summary>
    [UnityRestricted]
    internal class Subgraph
    {
        Subgraph() { }
    }

    /// <summary>
    /// Represents a data type in the graph.
    /// </summary>
    [Serializable]
    [PublicAPI]
    [UnityRestricted]
    internal partial struct TypeHandle : IEquatable<TypeHandle>, IComparable<TypeHandle>, ISerializationCallbackReceiver
    {
        // This empty static constructor is required so the static properties will be initialized on CoreCLR as initializing these properties has side effects other code depends on.
        // The static constructor was not necessary on Mono as the static properties were initialized there regardless.
        static TypeHandle()
        {
        }

        /// <summary>
        /// Whether the type handle is valid.
        /// </summary>
        public bool IsValid => !string.IsNullOrEmpty(m_Identification);

        /// <summary>
        /// The unique id for the type handle.
        /// </summary>
        [SerializeField, FormerlySerializedAs("Identification")]
        string m_Identification;

        /// <summary>
        /// The unique identification for the type handle.
        /// </summary>
        public string Identification => m_Identification;

        internal static TypeHandle Create(string identification)
        {
            return new TypeHandle(identification);
        }

        TypeHandle(string identification)
        {
            m_Identification = InternalTypeHelpers.ConvertTypeNameFromMonoToCoreClr(identification);
            m_Name = null;
            m_FriendlyName = null;
        }

        string m_Name;
        string m_FriendlyName;

        /// <summary>
        /// The name of the type.
        /// </summary>
        public string Name => m_Name ??= this.IsCustomTypeHandle() ? Identification : Resolve().Name;

        /// <summary>
        /// The friendly name of the type, ie the name people are used to see ( ex: "float" instead of "single" )
        /// </summary>
        public string FriendlyName => m_FriendlyName ??= this.GetFriendlyName_Internal() ?? (this.IsCustomTypeHandle() ? Identification : TypeHelpers.GetFriendlyName(Resolve()));

        /// <summary>
        /// Determines whether this TypeHandle is equal to another TypeHandle.
        /// </summary>
        /// <param name="other">The other type handle.</param>
        /// <returns>True if this TypeHandle is equal to the other TypeHandle.</returns>
        public bool Equals(TypeHandle other)
        {
            return string.Equals(Identification, other.Identification);
        }

        /// <summary>
        /// Determines whether this TypeHandle is equal to another object.
        /// </summary>
        /// <param name="obj">The other object.</param>
        /// <returns>True if this TypeHandle is equal to <paramref name="obj"/>.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is TypeHandle th && Equals(th);
        }

        /// <summary>
        /// Gets the hash code for this object.
        /// </summary>
        /// <returns>The hash code for this object.</returns>
        public override int GetHashCode()
        {
            return Identification?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// Gets a string representation of this object.
        /// </summary>
        /// <returns>The string representation of this object.</returns>
        public override string ToString()
        {
            return $"TypeName:{Identification}";
        }

        /// <summary>
        /// Determines whether a TypeHandle is equal to another TypeHandle.
        /// </summary>
        /// <param name="left">The first TypeHandle to compare.</param>
        /// <param name="right">The second TypeHandle to compare.</param>
        /// <returns>True if the first TypeHandle is equal to the second one.</returns>
        public static bool operator ==(TypeHandle left, TypeHandle right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether a TypeHandle is different from another TypeHandle.
        /// </summary>
        /// <param name="left">The first TypeHandle to compare.</param>
        /// <param name="right">The second TypeHandle to compare.</param>
        /// <returns>True if the first TypeHandle is different from the second one.</returns>
        public static bool operator !=(TypeHandle left, TypeHandle right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Compares this type handle to another type handle.
        /// </summary>
        /// <param name="other">The other type handle to compare.</param>
        /// <returns>-1, 0, or 1 if this instance is smaller, equal or greater than <paramref name="other"/>, respectively.</returns>
        public int CompareTo(TypeHandle other)
        {
            return string.Compare(Identification, other.Identification, StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets the <see cref="Type"/> represented by this object.
        /// </summary>
        /// <returns>The Type represented by this object.</returns>
        public Type Resolve()
        {
            return TypeHandleHelpers.ResolveType(this);
        }

        /// <inheritdoc />
        public void OnBeforeSerialize()
        {
        }

        /// <inheritdoc />
        public void OnAfterDeserialize()
        {
            m_Name = null;
            m_FriendlyName = null;
            var newValue = TypeHandleHelpers.ResolveMovedFromType(m_Identification);
            if (newValue != null)
                m_Identification = newValue;
            m_Identification = InternalTypeHelpers.ConvertTypeNameFromMonoToCoreClr(m_Identification);
        }
    }
}
