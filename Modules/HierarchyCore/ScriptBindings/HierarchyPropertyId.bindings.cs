// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Internal;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Hierarchy Property Id.
    /// </summary>
    [NativeType(Header = "Modules/HierarchyCore/Public/HierarchyPropertyId.h")]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct HierarchyPropertyId : IEquatable<HierarchyPropertyId>
    {
        const int k_HierarchyPropertyIdNull = 0;

        static readonly HierarchyPropertyId s_Null;
        readonly int m_Id;

        /// <summary>
        /// Represents an hierarchy property that is null/invalid.
        /// </summary>
        public static ref readonly HierarchyPropertyId Null => ref s_Null;

        /// <summary>
        /// Unique identification number of the hierarchy property.
        /// </summary>
        public int Id => m_Id;

        /// <summary>
        /// Create null property Id.
        /// </summary>
        public HierarchyPropertyId()
        {
            m_Id = k_HierarchyPropertyIdNull;
        }

        internal HierarchyPropertyId(int id)
        {
            m_Id = id;
        }

        [ExcludeFromDocs]
        public static bool operator ==(in HierarchyPropertyId lhs, in HierarchyPropertyId rhs) => lhs.Id == rhs.Id;
        [ExcludeFromDocs]
        public static bool operator !=(in HierarchyPropertyId lhs, in HierarchyPropertyId rhs) => !(lhs == rhs);
        [ExcludeFromDocs]
        public bool Equals(HierarchyPropertyId other) => other.Id == Id;
        [ExcludeFromDocs]
        public override string ToString() => $"{nameof(HierarchyPropertyId)}({(this == Null ? nameof(Null) : Id)})";
        [ExcludeFromDocs]
        public override bool Equals(object obj) => obj is HierarchyPropertyId node && Equals(node);
        [ExcludeFromDocs]
        public override int GetHashCode() => Id.GetHashCode();
    }
}
