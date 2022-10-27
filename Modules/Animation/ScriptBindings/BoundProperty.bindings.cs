// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using UnityEngine.Bindings;
using UnityEngine.Scripting;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;

namespace UnityEngine.Animations
{
    [NativeHeader("Modules/Animation/BoundProperty.h")]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BoundProperty : IEquatable<BoundProperty>, IComparable<BoundProperty>
    {
        /// <summary>
        /// The ID of an boundproperty.
        /// </summary>
        /// <value>The index into the internal list of bound properties.</value>
        /// <remarks>
        /// Bound property indexes are recycled when a domain reloard occurs or when the GameObject/Component bound to this property is destroyed. When thoses operation occur, the
        /// system increments the version identifier. To represent the same BoundProperty, both the Index and the
        /// Version fields of the BoundProperty object must match. If the Index is the same, but the Version is different,
        /// then the BoundProperty has been recycled.
        /// </remarks>
        public int index => m_Index;

        /// <summary>
        /// The generational version of the bound properties.
        /// </summary>
        /// <remarks>The Version number can, theoretically, overflow and wrap around within the lifetime of an
        /// application. For this reason, you cannot assume that an BoundProperty instance with a larger Version is a more
        /// recent incarnation of the BoundProperty than one with a smaller Version (and the same Index).</remarks>
        /// <value>Used to determine whether this BoundProperty object still identifies an existing BoundProperty.</value>
        public int version => m_Version;

        readonly int m_Index;
        readonly int m_Version;

        /// <summary>
        /// A "blank" BoundProperty object that does not refer to an actual property.
        /// </summary>
        public static BoundProperty Null => new BoundProperty();

        /// <summary>
        /// BoundProperty instances are equal if they refer to the same property.
        /// </summary>
        /// <param name="lhs">A BoundProperty object.</param>
        /// <param name="rhs">Another BoundProperty object.</param>
        /// <returns>True, if both Index and Version are identical.</returns>
        public static bool operator==(BoundProperty lhs, BoundProperty rhs)
        {
            return lhs.m_Index == rhs.m_Index && lhs.m_Version == rhs.m_Version;
        }

        /// <summary>
        /// BoundProperty instances are equal if they refer to the same property.
        /// </summary>
        /// <param name="lhs">A BoundProperty object.</param>
        /// <param name="rhs">Another BoundProperty object.</param>
        /// <returns>True, if either Index or Version are different.</returns>
        public static bool operator!=(BoundProperty lhs, BoundProperty rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// BoundProperty instances are equal if they refer to the same property.
        /// </summary>
        /// <param name="compare">The object to compare to this BoundProperty.</param>
        /// <returns>True, if the compare parameter contains a BoundProperty object having the same Index and Version
        /// as this BoundProperty.</returns>
        public override bool Equals(object compare)
        {
            return compare is BoundProperty compareBoundProperty && Equals(compareBoundProperty);
        }

        /// <summary>
        /// BoundProperty instances are equal if they represent the same property.
        /// </summary>
        /// <param name="boundProperty">The other BoundProperty.</param>
        /// <returns>True, if the BoundProperty instances have the same Index and Version.</returns>
        public bool Equals(BoundProperty boundProperty)
        {
            return boundProperty.m_Index == m_Index && boundProperty.m_Version == m_Version;
        }

        /// <summary>
        /// Compare this BoundProperty against a given one
        /// </summary>
        /// <param name="other">The other BoundProperty to compare to</param>
        /// <returns>Difference based on the BoundProperty Index value</returns>
        public int CompareTo(BoundProperty other)
        {
            return m_Index - other.m_Index;
        }

        /// <summary>
        /// A hash used for comparisons.
        /// </summary>
        /// <returns>A unique hash code.</returns>
        public override int GetHashCode()
        {
            return m_Version * 397 ^ m_Index;
        }
    }
}
