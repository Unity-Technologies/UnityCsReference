// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Represents a hierarchy property ID.
    /// </summary>
    [NativeHeader("Modules/HierarchyCore/Public/HierarchyPropertyId.h")]
    [StructLayout(LayoutKind.Sequential)]
    readonly struct HierarchyPropertyId : IEquatable<HierarchyPropertyId>
    {
        const int k_HierarchyPropertyIdNull = 0;

        static readonly HierarchyPropertyId s_Null;
        readonly int m_Id;

        /// <summary>
        /// Represents a hierarchy property that is null or invalid.
        /// </summary>
        public static ref readonly HierarchyPropertyId Null => ref s_Null;

        /// <summary>
        /// The unique identification number of the hierarchy property.
        /// </summary>
        public int Id => m_Id;

        /// <summary>
        /// Creates a null property ID.
        /// </summary>
        public HierarchyPropertyId()
        {
            m_Id = k_HierarchyPropertyIdNull;
        }

        internal HierarchyPropertyId(int id)
        {
            m_Id = id;
        }

        public static bool operator ==(in HierarchyPropertyId lhs, in HierarchyPropertyId rhs) => lhs.Id == rhs.Id;

        public static bool operator !=(in HierarchyPropertyId lhs, in HierarchyPropertyId rhs) => !(lhs == rhs);

        public bool Equals(HierarchyPropertyId other) => other.Id == Id;

        public override string ToString() => $"{nameof(HierarchyPropertyId)}({(this == Null ? nameof(Null) : Id)})";

        public override bool Equals(object obj) => obj is HierarchyPropertyId node && Equals(node);

        public override int GetHashCode() => Id.GetHashCode();
    }
}
