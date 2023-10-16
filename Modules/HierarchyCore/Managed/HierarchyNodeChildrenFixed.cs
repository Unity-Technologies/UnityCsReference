// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Hierarchy
{
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    unsafe struct HierarchyNodeChildrenFixed
    {
        public const int Capacity = 4;

        [FieldOffset(0)] HierarchyNode m_Node1;
        [FieldOffset(8)] HierarchyNode m_Node2;
        [FieldOffset(16)] HierarchyNode m_Node3;
        [FieldOffset(24)] HierarchyNode m_Node4;

        public HierarchyNode* Ptr => (HierarchyNode*)UnsafeUtility.AddressOf(ref m_Node1);
    };
}
