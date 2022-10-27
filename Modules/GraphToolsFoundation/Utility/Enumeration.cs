// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Unity.GraphToolsFoundation
{
    /// <summary>
    /// An extensible enumeration. Enumeration values are defined as static readonly members of the
    /// concrete class.
    /// </summary>
    abstract class Enumeration : IComparable, IComparable<Enumeration>, IEquatable<Enumeration>
    {
        /// <summary>
        /// The name of the enumeration value.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The unique id of the enumeration value.
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// A list of obsolete names for this enumeration value.
        /// </summary>
        public string[] ObsoleteNames;

        /// <summary>
        /// Initializes a new instance of the <see cref="Enumeration" /> class.
        /// </summary>
        /// <param name="id">A unique id for the instance.</param>
        /// <param name="name">The name of the instance.</param>
        /// <param name="obsoleteNames">A list of obsolete names for the instance.</param>
        protected Enumeration(int id, string name, string[] obsoleteNames = null)
        {
            Id = id;
            Name = name;
            ObsoleteNames = obsoleteNames;
        }

        /// <summary>
        /// Gets a string representation for this enumeration.
        /// </summary>
        /// <returns>A string representation for this enumeration.</returns>
        public override string ToString() => Name;

        /// <summary>
        /// Gets an enumerable of all the instances of the enumeration type, excluding instances of the parent types.
        /// </summary>
        /// <typeparam name="T">The enumeration type to get the instances of.</typeparam>
        /// <returns>An enumerable of the instances.</returns>
        public static IEnumerable<T> GetDeclared<T>()
            where T : Enumeration
        {
            var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            return fields.Select(f => f.GetValue(null)).Cast<T>();
        }

        /// <summary>
        /// Gets an enumerable of all the instances of the enumeration type, including instances of the parent types.
        /// </summary>
        /// <typeparam name="T">The enumeration type to get the instances of.</typeparam>
        /// <typeparam name="TBase">The type into which the instances should be cast. If this is not the less derived parent of <typeparamref name="T"/>, some values in the enumerable could be null.</typeparam>
        /// <returns>An enumerable of the instances.</returns>
        public static IEnumerable<TBase> GetAll<T, TBase>()
            where T : TBase
            where TBase : Enumeration
        {
            var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            return fields.Select(f => f.GetValue(null)).Cast<TBase>();
        }

        /// <summary>
        /// Determines if another enumeration instance represents the same value as this.
        /// </summary>
        /// <param name="other">The other value to compare.</param>
        /// <returns>True if <paramref name="other"/> represents the same value as this.</returns>
        public bool Equals(Enumeration other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!Id.Equals(other.Id)) return false;

            return other.GetType().IsInstanceOfType(this) || GetType().IsInstanceOfType(other);
        }

        /// <summary>
        /// Determines if another object instance represents the same value as this.
        /// </summary>
        /// <param name="obj">The other value to compare.</param>
        /// <returns>True if <paramref name="obj"/> represents the same value as this.</returns>
        public override bool Equals(object obj)
        {
            return Equals((Enumeration)obj);
        }

        /// <summary>
        /// Gets the hashcode of the instance.
        /// </summary>
        /// <returns>The hashcode of the instance.</returns>
        public override int GetHashCode()
        {
            return Id;
        }

        /// <summary>
        /// Determines if two enumeration instances represents the same value.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns>True if <paramref name="left"/> represents the same value as <paramref name="right"/>.</returns>
        public static bool operator==(Enumeration left, Enumeration right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines if two enumeration instances represents different values.
        /// </summary>
        /// <param name="left">The first value to compare.</param>
        /// <param name="right">The second value to compare.</param>
        /// <returns>True if <paramref name="left"/> represents a different value than <paramref name="right"/>.</returns>
        public static bool operator!=(Enumeration left, Enumeration right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Determines if an enumeration instance is greater, equal or smaller than this instance.
        /// </summary>
        /// <param name="other">The value to compare.</param>
        /// <returns>-1, 0, or 1 if this instance is smaller, equal or greater than <paramref name="other"/>, respectively.</returns>
        public int CompareTo(Enumeration other)
        {
            return ReferenceEquals(other, null) ? 1 : Id.CompareTo(other.Id);
        }

        /// <summary>
        /// Determines if an object is greater, equal or smaller than this instance.
        /// </summary>
        /// <param name="obj">The value to compare.</param>
        /// <returns>-1, 0, or 1 if this instance is smaller, equal or greater than <paramref name="obj"/>, respectively.</returns>
        /// <exception cref="ArgumentException">If <paramref name="obj"/> is not an instance of <see cref="Enumeration"/>.</exception>
        public int CompareTo(object obj)
        {
            if (obj != null && !(obj is Enumeration))
                throw new ArgumentException("Object must be of type Enumeration.");

            return CompareTo((Enumeration)obj);
        }
    }
}
