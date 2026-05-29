// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace Unity.Hierarchy
{
    /// <summary>
    /// Information about the UID format used by a <see cref="HierarchyNodeTypeHandlerBase"/>.
    /// The UID is a binary blob that uniquely identifies a node across sessions.
    /// A <see cref="Size"/> of 0 means the handler does not support UID serialization.
    /// </summary>
    [NativeHeader("Modules/HierarchyCore/Public/HierarchyUIDInfo.h")]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct HierarchyUIDInfo
    {
        readonly int m_Version;
        readonly int m_Size;

        /// <summary>
        /// The format version. Increment when the UID encoding changes.
        /// </summary>
        public int Version => m_Version;

        /// <summary>
        /// The per-node UID size in bytes. Zero means no UID support.
        /// </summary>
        public int Size => m_Size;

        /// <summary>
        /// Creates a <see cref="HierarchyUIDInfo"/>.
        /// </summary>
        public HierarchyUIDInfo(int version, int size)
        {
            m_Version = version;
            m_Size = size;
        }
    }
}
