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
    ///<summary>A BoundProperty is a safe handle to read and write value in a generic way from any Unity components.</summary>
    [NativeHeader("Modules/Animation/BoundProperty.h")]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct BoundProperty : IEquatable<BoundProperty>, IComparable<BoundProperty>
    {
        ///<summary>The index of this BoundProperty to the internal list of bound properties.</summary>
        ///<remarks>The bound property index is recycled when a code reload occurs or when either the GameObject or Component associated with a BoundProperty is destroyed. When the index is recycled, the system increments the version identifier.
        ///                    
        ///To represent the same BoundProperty, the Index and the Version must match. If the Index and Version differ, then the Index has been recycled.</remarks>
        ///<value>The index into the internal list of bound properties.</value>
        public int index => m_Index;

        ///<summary>The version of the BoundProperty.</summary>
        ///<remarks>Use this property to determine whether the BoundProperty object identifies an existing bound property.
        ///
        ///During the life of an application, its Version can overflow and wrap around. For this reason, you cannot assume that a BoundProperty with a higher Version is more recent than a BoundProperty with a lower Version. The lower Version could be more recent.</remarks>
        ///<value>Used to determine whether this BoundProperty object still identifies an existing BoundProperty.</value>
        public int version => m_Version;

        readonly int m_Index;
        readonly int m_Version;

        ///<summary>An empty BoundProperty object that does not refer to a property.</summary>
        public static BoundProperty Null => new BoundProperty();

        ///<summary>BoundProperty objects are equal if they refer to the same property.</summary>
        ///<param name="lhs">A BoundProperty object.</param>
        ///<param name="rhs">Another BoundProperty object.</param>
        ///<returns>Returns true if both Index and Version are identical.</returns>
        public static bool operator==(BoundProperty lhs, BoundProperty rhs)
        {
            return lhs.m_Index == rhs.m_Index && lhs.m_Version == rhs.m_Version;
        }

        ///<summary>BoundProperty objects are not equal if they refer to different properties.</summary>
        ///<param name="lhs">A BoundProperty object.</param>
        ///<param name="rhs">Another BoundProperty object.</param>
        ///<returns>Returns true if either Index or Version are different.</returns>
        public static bool operator!=(BoundProperty lhs, BoundProperty rhs)
        {
            return !(lhs == rhs);
        }

        ///<summary>Checks if this BoundProperty equals a specified BoundProperty object.</summary>
        ///<param name="compare">The BoundProperty object to compare.</param>
        ///<returns>Returns True if the Index and Version of this BoundProperty matches the specified BoundProperty object. Returns False otherwise.</returns>
        public override bool Equals(object compare)
        {
            return compare is BoundProperty compareBoundProperty && Equals(compareBoundProperty);
        }

        ///<summary>Checks if this BoundProperty instance equals a specified BoundProperty instance.</summary>
        ///<param name="boundProperty">The BoundProperty instance to compare.</param>
        ///<returns>Returns True if the Index and Version of this BoundProperty instance matches the specified BoundProperty instance. Returns False otherwise.</returns>
        public bool Equals(BoundProperty boundProperty)
        {
            return boundProperty.m_Index == m_Index && boundProperty.m_Version == m_Version;
        }

        ///<summary>Compares this BoundProperty to a specific BoundProperty.</summary>
        ///<param name="other">The BoundProperty to compare.</param>
        ///<returns>Returns zero if the Index values for the two BoundProperty objects match. Otherwise, this method returns the difference between the BoundProperty Index values.</returns>
        public int CompareTo(BoundProperty other)
        {
            return m_Index - other.m_Index;
        }

        ///<summary>Retrieves a unique hash based on this BoundProperty.</summary>
        ///<returns>Returns a unique hash code.</returns>
        public override int GetHashCode()
        {
            return m_Version * 397 ^ m_Index;
        }
    }
}
