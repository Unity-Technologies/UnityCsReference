// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using TransformChangeSystemMask = System.UInt64;

namespace Unity.Transforms
{
    // Mirrors TransformHierarchy in Runtime/Transform/TransformHierarchyTypes.h, so keep the two in sync.
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct TransformHierarchy
    {
        internal JobHandle fence;

        internal uint transformCapacity;
        internal int firstFreeIndex;

        internal trsX* localTransforms;
        internal int* parentIndices;

        internal uint* deepChildCount;

        private IntPtr childIndices;

        private IntPtr mainThreadOnlyTransformPointers;
        internal EntityId* mainThreadOnlyEntityReferences;

        private ulong ecsOwner;
        private uint ecsAccess;

        internal int changeDispatchIndex;
        internal TransformChangeSystemMask combinedSystemChanged;

        internal TransformChangeSystemMask* systemChanged;
        internal TransformChangeSystemMask* systemInterested;

        internal TransformChangeSystemMask combinedSystemInterest;
        // The native element type, TransformHierarchyChangeSystemMask, is UInt64 when the engine is built with
        // ENABLE_UNIT_TESTS and UInt32 otherwise. C# can't see that define, so this is left opaque.
        private IntPtr hierarchySystemInterested;

        internal byte* localTransformTypes;

        internal int* nextIndices;
        internal int* prevIndices;

        internal float4* eulerHints; // These are math::float3 which are 16 bytes wide
        internal uint version;

        // MemLabelId is 8 bytes under ENABLE_MEM_PROFILER and 2 bytes otherwise, so no single C#
        // type is correct in every configuration. It is the last field and we don't need it in C#,
        // so as long as no fields are added after this, the rest of our offsets are correct.
        //MemLabelId memLabel;
    }
}
