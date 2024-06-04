// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Describes the type and size of a Hierarchy Property.
    /// </summary>
    [NativeHeader("Modules/HierarchyCore/Public/HierarchyPropertyDescriptor.h")]
    [StructLayout(LayoutKind.Sequential)]
    public struct HierarchyPropertyDescriptor
    {
        int m_Size;
        HierarchyPropertyStorageType m_Type;

        /// <summary>
        /// The size of the property in bytes.
        /// </summary>
        public int Size
        {
            get => m_Size;
            set => m_Size = value;
        }

        /// <summary>
        /// The storage type of the property.
        /// </summary>
        public HierarchyPropertyStorageType Type
        {
            get => m_Type;
            set => m_Type = value;
        }
    }
}
