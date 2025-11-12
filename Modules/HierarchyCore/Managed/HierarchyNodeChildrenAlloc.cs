// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace Unity.Hierarchy
{
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    unsafe struct HierarchyNodeChildrenAlloc
    {
        // WARNING: Must match layout in native, see HierarchyNodeChildrenAlloc.h
        [FieldOffset(0)] public HierarchyNode* Ptr;
        [FieldOffset(8)] public int Size;
        [FieldOffset(12)] public int Capacity;
        [FieldOffset(16)] public int ControlBit;
        [FieldOffset(20)] public int NullCount;
        [FieldOffset(24)] public int Reserved0;
        [FieldOffset(28)] public int Reserved1;
    };
}
