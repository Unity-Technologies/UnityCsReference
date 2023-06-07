// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Internal;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Provides access to string property data.
    /// </summary>
    public readonly struct HierarchyPropertyString :
        IEquatable<HierarchyPropertyString>,
        IHierarchyProperty<string>
    {
        /// <summary>
        /// The hierarchy this property belongs to.
        /// </summary>
        readonly Hierarchy m_Hierarchy;

        /// <summary>
        /// The internal property binding.
        /// </summary>
        internal readonly HierarchyPropertyId m_Property;

        /// <summary>
        /// Returns <see langword="true"/> if the native property is valid.
        /// </summary>
        public bool IsCreated => m_Property != HierarchyPropertyId.Null && m_Hierarchy is { IsCreated: true };

        /// <summary>
        /// Creates a new instance of <see cref="HierarchyPropertyString"/>.
        /// </summary>
        internal HierarchyPropertyString(Hierarchy hierarchy, in HierarchyPropertyId property)
        {
            if (hierarchy == null)
                throw new ArgumentNullException(nameof(hierarchy));
            if (property == HierarchyPropertyId.Null)
                throw new ArgumentException(nameof(property));

            m_Hierarchy = hierarchy;
            m_Property = property;
        }

        /// <summary>
        /// Gets the property value for the given <see cref="HierarchyNode"/>.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <returns>The property value of the hierarchy node.</returns>
        public string GetValue(in HierarchyNode node)
        {
            if (m_Hierarchy == null)
                throw new NullReferenceException("Hierarchy reference has not been set.");
            if (!m_Hierarchy.IsCreated)
                throw new InvalidOperationException("Hierarchy has been disposed.");

            return m_Hierarchy.GetPropertyString(in m_Property, in node);
        }

        /// <summary>
        /// Sets the property value for a <see cref="HierarchyNode"/>.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        /// <param name="value">The value to set.</param>
        public void SetValue(in HierarchyNode node, string value)
        {
            if (m_Hierarchy == null)
                throw new NullReferenceException("Hierarchy reference has not been set.");
            if (!m_Hierarchy.IsCreated)
                throw new InvalidOperationException("Hierarchy has been disposed.");

            m_Hierarchy.SetPropertyString(in m_Property, in node, value);
        }

        /// <summary>
        /// Removes the property value for a <see cref="HierarchyNode"/>.
        /// </summary>
        /// <param name="node">The hierarchy node.</param>
        public void ClearValue(in HierarchyNode node)
        {
            if (m_Hierarchy == null)
                throw new NullReferenceException("Hierarchy reference has not been set.");
            if (!m_Hierarchy.IsCreated)
                throw new InvalidOperationException("Hierarchy has been disposed.");

            m_Hierarchy.ClearProperty(in m_Property, in node);
        }

        [ExcludeFromDocs]
        public static bool operator ==(in HierarchyPropertyString lhs, in HierarchyPropertyString rhs) => lhs.m_Hierarchy == rhs.m_Hierarchy && lhs.m_Property == rhs.m_Property;
        [ExcludeFromDocs]
        public static bool operator !=(in HierarchyPropertyString lhs, in HierarchyPropertyString rhs) => !(lhs == rhs);
        [ExcludeFromDocs]
        public bool Equals(HierarchyPropertyString other) => m_Hierarchy == other.m_Hierarchy && m_Property == other.m_Property;
        [ExcludeFromDocs]
        public override string ToString() => m_Property.ToString();
        [ExcludeFromDocs]
        public override bool Equals(object obj) => obj is HierarchyPropertyString property && Equals(property);
        [ExcludeFromDocs]
        public override int GetHashCode() => m_Property.GetHashCode();
    }
}
