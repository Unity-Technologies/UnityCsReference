// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine.Bindings;
using TransformChangeSystemMask = System.UInt64;

namespace Unity.Transforms
{
    // Mirrors TransformInternal::ChangeMaskCache in Runtime/Transform/TransformHierarchy.h.
    [StructLayout(LayoutKind.Sequential)]
    internal struct ChangeMaskCache
    {
        internal TransformChangeSystemMask localT;
        internal TransformChangeSystemMask localR;
        internal TransformChangeSystemMask localS;
        internal TransformChangeSystemMask globalT;
        internal TransformChangeSystemMask globalR;
        internal TransformChangeSystemMask globalS;
    }

    // Mirrors TransformInternal::g_ChangeMaskCache in Runtime/Transform/TransformHierarchy.h.
    // Every TransformHierarchy shares one global native change mask cache,
    // and we need to fetch its address from the native engine.
    [VisibleToOtherModules("UnityEngine.CoreModule")]
    internal static unsafe class GlobalChangeMaskCache
    {
        struct Key {}
        static readonly SharedStatic<IntPtr> s_Ptr = SharedStatic<IntPtr>.GetOrCreate<Key>();

        internal static ChangeMaskCache* Ptr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ChangeMaskCache*)s_Ptr.Data;
        }

        [VisibleToOtherModules("UnityEngine.CoreModule")]
        internal static void Initialize(IntPtr changeMaskCache)
        {
            s_Ptr.Data = changeMaskCache;
        }
    }

    // Mirrors the functions of Runtime/Transform/TransformHierarchy.h, both those in namespace TransformInternal
    // and those at global scope.
    internal static unsafe class TransformInternal
    {
        // Mirrors TransformInternal::OnTransformChangedMask(TransformHierarchy&, UInt32, ...) in
        // Runtime/Transform/TransformHierarchy.h.
        //
        // Note that this is a race condition: combinedSystemChanged is shared by the whole hierarchy and updated with a
        // non-atomic |=, and the loop writes systemChanged for every transform in the subtree, so jobs writing
        // different transforms in the same hierarchy can lose each other's change flags.
        // It is also O(n) for setting a local transform, where n is the size of the subtree.
        // TODO KERNEL-1219: We should elide this for pure entity hierarchies. An early out on
        //  combinedSystemInterest == 0 should be sufficient, as nothing registers interest in pure entities.
        internal static void OnTransformChangedMask(TransformHierarchy* hierarchy, int index,
            TransformChangeSystemMask localOnlyMask, TransformChangeSystemMask commonMask,
            TransformChangeSystemMask childrenOnlyMask)
        {
            TransformChangeSystemMask localSystemInterested = hierarchy->systemInterested[index] & (localOnlyMask | commonMask);
            hierarchy->systemChanged[index] |= localSystemInterested;
            hierarchy->combinedSystemChanged |= localSystemInterested;
            UInt32 count = hierarchy->deepChildCount[index];
            Int32 cur = hierarchy->nextIndices[index];
            for (UInt32 i = 1; i < count; i++)
            {
                TransformChangeSystemMask sysInterested = hierarchy->systemInterested[cur] & (commonMask | childrenOnlyMask);
                hierarchy->systemChanged[cur] |= sysInterested;
                hierarchy->combinedSystemChanged |= sysInterested;
                cur = hierarchy->nextIndices[cur];
            }
        }

        // Mirrors TransformInternal::OnScaleChangedCalculateTransformType in
        // Runtime/Transform/TransformHierarchy.cpp, except that native re-reads localTransforms[index].s
        // while we take the scale that was just written, to avoid the readback.
        //
        // localTransformTypes is only read on the GameObject side. An odd negative count determines front/back face,
        // and is used by renderers and collision. NonUniform and NoScale are used for optimizations in renderers,
        // collision, and light culling. It could be derived from the transform matrix when used, but caching it on
        // the TransformHierarchy saves time.
        //
        // Entities never reads it. A hierarchy with no GameObjects could skip this, but a single pure entity can't,
        // because a GameObject below it in a mixed hierarchy needs it to be correct all the way up the hierarchy.
        internal static void OnScaleChangedCalculateTransformType(TransformHierarchy* hierarchy, int index, float3 scale)
        {
            hierarchy->localTransformTypes[index] = math.CalculateTransformType(scale);
        }

        // Mirrors CalculateGlobalMatrix(TransformAccessReadOnly), at global scope in
        // Runtime/Transform/TransformHierarchy.h.
        internal static affineX CalculateGlobalMatrix(TransformHierarchy* hierarchy, int index)
        {
            affineX parentX;
            affineX globalX;

            trsX* localTransforms = hierarchy->localTransforms;
            globalX = math.convert(localTransforms[index]);

            int* parentIndices = hierarchy->parentIndices;
            int parentIndex = parentIndices[index];

            while (parentIndex >= 0)
            {
                parentX = math.convert(localTransforms[parentIndex]);
                globalX = math.mul(parentX, globalX);
                parentIndex = parentIndices[parentIndex];
            }

            return globalX;
        }

        // Mirrors CalculateGlobalMatrixInverse(TransformAccessReadOnly), at global scope in
        // Runtime/Transform/TransformHierarchy.h.
        // Same walk as CalculateGlobalMatrix, but each level is inverted and the composition order is
        // reversed: mul(globalXInv, parentXInv) rather than mul(parentX, globalX), because the inverse
        // of a composition applies in the opposite order.
        internal static affineX CalculateGlobalMatrixInverse(TransformHierarchy* hierarchy, int index)
        {
            affineX parentX;
            affineX globalX;

            trsX* localTransforms = hierarchy->localTransforms;
            globalX = math.inverse(localTransforms[index]);

            int* parentIndices = hierarchy->parentIndices;
            int parentIndex = parentIndices[index];

            while (parentIndex >= 0)
            {
                parentX = math.inverse(localTransforms[parentIndex]);
                globalX = math.mul(globalX, parentX);
                parentIndex = parentIndices[parentIndex];
            }

            return globalX;
        }

        // Mirrors CalculateGlobalPositionAndRotation(TransformAccessReadOnly, ...), at global
        // scope in Runtime/Transform/TransformHierarchy.h.
        // Note this accumulates a position and a quaternion up the parent chain rather than
        // composing matrices the way CalculateGlobalMatrix does. It's cheaper per level, and
        // doesn't have to deal with non-uniform scale in a matrix.
        internal static void CalculateGlobalPositionAndRotation(TransformHierarchy* hierarchy, int index,
            out float3 position, out quaternion rotation)
        {
            trsX* localTransforms = hierarchy->localTransforms;
            int* parentIndices = hierarchy->parentIndices;
            ref trsX trs = ref localTransforms[index];

            position = trs.t;
            float4 r = trs.q;

            int parentIndex = parentIndices[index];
            while (parentIndex >= 0)
            {
                ref trsX ptrs = ref localTransforms[parentIndex];

                position = math.mul(ptrs, position);
                r = math.scaleMulQuat(ptrs.s, r);
                r = math.quatMul(ptrs.q, r);

                parentIndex = parentIndices[parentIndex];
            }

            rotation = r;
        }

        // Mirrors InverseTransformPositionAndRotation(const TransformAccessReadOnly&, ...), at global scope in
        // Runtime/Transform/TransformHierarchy.h, and its NonRecursive half in TransformInternal. Converts a
        // world-space position and rotation into the
        // local space of transform at index by applying each ancestor's inverse transform, root first.
        //
        // TODO KERNEL-1216: make this iterative. Hierarchy depth is unbounded and job worker stacks are especially
        // small, so a deep hierarchy can overflow the stack here, and having some TransformRef APIs
        // that stackoverflow while others don't is a bad experience.
        // An iterative version has to gather the ancestor path and walk it backwards, because the
        // inverse of a composition applies in reverse while parentIndices only walks up.
        internal static void InverseTransformPositionAndRotation(TransformHierarchy* hierarchy, int index,
            ref float3 p, ref float4 r)
        {
            if (index > 0)
            {
                InverseTransformPositionAndRotation(hierarchy,
                    hierarchy->parentIndices[index], ref p, ref r);
            }

            ref trsX trs = ref hierarchy->localTransforms[index];

            p = math.invMul(trs, p);
            r = math.quatMul(math.quatConj(trs.q), r);
            r = math.scaleMulQuat(trs.s, r);
        }

        // Mirrors SetLocalT(TransformAccess, const math::float3&, ...), at global scope in
        // Runtime/Transform/TransformHierarchy.h, without its comparison: this always dispatches.
        internal static void SetLocalTNoCompare(TransformHierarchy* hierarchy, int index, float3 t)
        {
            hierarchy->localTransforms[index].t = t;
            var changeMaskCache = GlobalChangeMaskCache.Ptr;
            TransformChangeSystemMask mask = changeMaskCache->globalT;
            OnTransformChangedMask(hierarchy, index,
                changeMaskCache->localT, mask, 0);
        }

        // Mirrors SetLocalR(TransformAccess, const math::float4&, ...), at global scope in
        // Runtime/Transform/TransformHierarchy.h, without its comparison: this always dispatches.
        internal static void SetLocalRNoCompare(TransformHierarchy* hierarchy, int index, quaternion r)
        {
            hierarchy->localTransforms[index].q = r.value;

            // A rotation change can also change the global (lossy) scale when there is non-uniform scale in the
            // hierarchy, because rotating against a non-uniform scale skews the result. We don't dispatch a scale
            // change for that skew, and native doesn't either. It is an acceptable known issue.
            var changeMaskCache = GlobalChangeMaskCache.Ptr;
            TransformChangeSystemMask mask = changeMaskCache->globalR;
            OnTransformChangedMask(hierarchy, index,
                changeMaskCache->localR, mask, changeMaskCache->globalT);
        }

        // Mirrors SetLocalS(TransformAccess, const math::float3&, ...), at global scope in
        // Runtime/Transform/TransformHierarchy.h, without its comparison: this always dispatches.
        internal static void SetLocalSNoCompare(TransformHierarchy* hierarchy, int index, float3 s)
        {
            hierarchy->localTransforms[index].s = s;
            OnScaleChangedCalculateTransformType(hierarchy,
                index, s);

            var changeMaskCache = GlobalChangeMaskCache.Ptr;
            TransformChangeSystemMask mask = changeMaskCache->globalS;
            OnTransformChangedMask(hierarchy, index,
                changeMaskCache->localS, mask, changeMaskCache->globalR | changeMaskCache->globalT);
        }

        // Mirrors SetLocalTR(TransformAccess, ...), at global scope in Runtime/Transform/TransformHierarchy.h,
        // without its comparison: one combined dispatch, always.
        internal static void SetLocalTRNoCompare(TransformHierarchy* hierarchy, int index, float3 t, quaternion r)
        {
            ref trsX trs = ref hierarchy->localTransforms[index];
            trs.t = t;
            // Stored verbatim, not normalized; see TransformUnion.Rotation.
            trs.q = r.value;

            var changeMaskCache = GlobalChangeMaskCache.Ptr;
            OnTransformChangedMask(hierarchy, index,
                changeMaskCache->localT | changeMaskCache->localR,
                changeMaskCache->globalT | changeMaskCache->globalR,
                changeMaskCache->globalT);
        }

        // Mirrors SetLocalTRS(TransformAccess, ...), at global scope in Runtime/Transform/TransformHierarchy.h,
        // without its comparison: one combined dispatch, always.
        internal static void SetLocalTRSNoCompare(TransformHierarchy* hierarchy, int index, float3 t, quaternion r,
            float3 s)
        {
            ref trsX trs = ref hierarchy->localTransforms[index];
            trs.t = t;
            // Stored verbatim, not normalized; see TransformUnion.Rotation.
            trs.q = r.value;
            trs.s = s;

            OnScaleChangedCalculateTransformType(hierarchy,
                index, s);

            var changeMaskCache = GlobalChangeMaskCache.Ptr;
            OnTransformChangedMask(hierarchy, index,
                changeMaskCache->localT | changeMaskCache->localR | changeMaskCache->localS,
                changeMaskCache->globalT | changeMaskCache->globalR | changeMaskCache->globalS,
                changeMaskCache->globalT | changeMaskCache->globalR);
        }

        // Mirrors SetGlobalTR(TransformAccess, ...), at global scope in Runtime/Transform/TransformHierarchy.h, without
        // its comparison: convert the world-space values into parent space, then do
        // an ordinary local TR write. Index 0 is always the hierarchy root, so index > 0 is the
        // "has a parent" test, same as native.
        internal static void SetGlobalTRNoCompare(TransformHierarchy* hierarchy, int index, float3 position,
            quaternion rotation)
        {
            float3 lt = position;
            float4 lr = rotation.value;

            if (index > 0)
            {
                InverseTransformPositionAndRotation(hierarchy,
                    hierarchy->parentIndices[index], ref lt, ref lr);
            }

            SetLocalTRNoCompare(hierarchy, index, lt, new quaternion(lr));
        }
    }
}
