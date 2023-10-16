// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace Unity.Hierarchy
{
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    unsafe struct HierarchyNodeChildrenAlloc
    {
        [FieldOffset(0)] public HierarchyNode* Ptr;
        [FieldOffset(8)] public int Size;
        [FieldOffset(12)] public int Capacity;
        [FieldOffset(16)] public fixed int Reserved[4];
    };
}
