// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.GraphToolkit.Editor
{
    /// <summary>
    /// A class that manage polymorphic port state. It holds the list available types and currently selected type.
    /// In case of automatic type, it also handles underlying type resolution.
    /// </summary>
    [Serializable]
    [UnityRestricted]
    internal class PolymorphicPortHandler : IPolymorphicPortHandler
    {
        [SerializeField]
        TypeHandle[] m_TypeHandles;

        [SerializeField]
        uint m_SelectedTypeIndex;

        [SerializeField]
        TypeHandle m_ResolvedType;

        /// <summary>
        /// Creates a new PolymorphicPortHandler
        /// </summary>
        /// <param name="typeIndex">Index of the selected type by default</param>
        /// <param name="typeHandles">Collection of <see cref="TypeHandle"/> supported by this polymorphic port</param>
        /// <exception cref="ArgumentException">If the default <paramref name="typeIndex"/> is out of bounds of the provided <paramref name="typeHandles"/> collection</exception>
        public PolymorphicPortHandler(uint typeIndex, IEnumerable<TypeHandle> typeHandles)
        {
            UpdateTypes(typeHandles);
            if (typeIndex >= m_TypeHandles.Length)
            {
                throw new ArgumentException("Type index exceed types collection range", nameof(typeIndex));
            }
            m_SelectedTypeIndex = typeIndex;
            m_ResolvedType = TypeHandle.Automatic;
        }

        /// <summary>
        /// Event raised when the current <see cref="SelectedType"/> changes
        /// </summary>
        public event Action<uint> SelectedTypeChanged;

        /// <summary>
        /// Currently selected port type
        /// </summary>
        public TypeHandle SelectedType => m_TypeHandles != null && m_SelectedTypeIndex < m_TypeHandles.Length ? m_TypeHandles[m_SelectedTypeIndex] : TypeHandle.Unknown;

        /// <summary>
        /// When selected type is automatic, and the port is connected, this is the underlying type that was resolved when wired
        /// </summary>
        public TypeHandle ResolvedType
        {
            get => SelectedType == TypeHandle.Automatic ? m_ResolvedType : TypeHandle.Unknown;
            private set
            {
                if (SelectedType != TypeHandle.Automatic)
                {
                    throw new InvalidOperationException("Cannot set automatic type if the type handle is not set to automatic");
                }
                m_ResolvedType = value;
            }
        }

        /// <summary>
        /// Current selected type index. The setter should be used in a command and followed by a call to the <see>
        ///     <cref>Unity.GraphToolkit.Editor.Port.UpdateDatatypeHandler</cref>
        /// </see>
        /// </summary>
        public uint SelectedTypeIndex => m_SelectedTypeIndex;

        /// <summary>
        /// List of supported type for the polymorphic port
        /// </summary>
        public IReadOnlyList<TypeHandle> Types => m_TypeHandles;

        /// <summary>
        /// Tells if a type is supported by the polymorphic port
        /// </summary>
        /// <param name="typeHandle">The <see cref="TypeHandle"/> for the type.</param>
        /// <returns>True if the TypeHandle is supported, False otherwise</returns>
        public bool CanConnect(TypeHandle typeHandle) => Array.Exists(m_TypeHandles, x => x == typeHandle);

        /// <summary>
        /// Removes the resolved type used when the selected type is automatic
        /// </summary>
        public void Unresolve() => m_ResolvedType = TypeHandle.Automatic;

        /// <summary>
        /// Define which type to use as the automatic type
        /// </summary>
        /// <param name="typeHandle">Type to use as automatic type</param>
        public void Resolve(TypeHandle typeHandle) => ResolvedType = typeHandle;

        /// <summary>
        /// Change the currently <see cref="SelectedTypeIndex"/>, based on the supported <see cref="Types"/> collection
        /// </summary>
        /// <param name="index">Index of the <see cref="TypeHandle"/> to use</param>
        /// <exception cref="IndexOutOfRangeException">The index is out of bounds of the <see cref="Types"/> collection</exception>
        public void SetSelectedTypeIndex(uint index)
        {
            if (index >= m_TypeHandles.Length)
            {
                throw new IndexOutOfRangeException("Index out of range");
            }

            m_SelectedTypeIndex = index;
            SelectedTypeChanged?.Invoke(m_SelectedTypeIndex);
        }

        void UpdateTypes(IEnumerable<TypeHandle> types)
        {
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            m_TypeHandles = new List<TypeHandle>(types).ToArray();
            if (m_TypeHandles.Length == 0)
            {
                throw new ArgumentException("You must provide at least one support type");
            }
        }
    }
}
