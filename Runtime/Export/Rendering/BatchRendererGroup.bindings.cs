// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Scripting;
using UnityEngine.Bindings;
using UnityEngine.Rendering;

using Unity.Jobs;

namespace UnityEngine.Rendering
{
    [StructLayout(LayoutKind.Sequential)]
    public struct BatchVisibility
    {
        public int offset;
        public int instancesCount;
        public int visibleCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Camera/BatchRendererGroup.h")]
    [UsedByNativeCode]
    unsafe public struct BatchCullingContext
    {
        public NativeArray<Plane> cullingPlanes;
        public NativeArray<BatchVisibility> batchVisibility;
        public NativeArray<int> visibleIndices;
        public LODParameters lodParameters;
    };

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Camera/BatchRendererGroup.h")]
    [UsedByNativeCode]
    unsafe struct BatchRendererCullingOutput
    {
        public JobHandle           cullingJobsFence;
        public Plane*              cullingPlanes;
        public BatchVisibility*    batchVisibility;
        public int*                visibleIndices;
        public int                 cullingPlanesCount;
        public int                 batchVisibilityCount;
        public int                 visibleIndicesCount;
    };

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Math/Matrix4x4.h")]
    [NativeHeader("Runtime/Camera/BatchRendererGroup.h")]
    [RequiredByNativeCode]
    public class BatchRendererGroup : IDisposable
    {
        IntPtr m_GroupHandle = IntPtr.Zero;
        OnPerformCulling m_PerformCulling;

        unsafe public delegate JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext);

        public BatchRendererGroup(OnPerformCulling cullingCallback)
        {
            m_PerformCulling = cullingCallback;
            m_GroupHandle = Create(this);
        }

        public void Dispose()
        {
            Destroy(m_GroupHandle);
            m_GroupHandle = IntPtr.Zero;
        }

        public extern int AddBatch(Mesh mesh, int subMeshIndex, Material material, int layer, ShadowCastingMode castShadows, bool receiveShadows, bool invertCulling, Bounds bounds, int instanceCount, MaterialPropertyBlock customProps, GameObject associatedSceneObject);

        public extern void SetInstancingData(int batchIndex, int instanceCount, MaterialPropertyBlock customProps);

        unsafe public NativeArray<Matrix4x4> GetBatchMatrices(int batchIndex)
        {
            int matricesCount = 0;
            var matrices = GetBatchMatrices(batchIndex, out matricesCount);
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Matrix4x4>((void*)matrices, matricesCount, Allocator.Invalid);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, GetMatricesSafetyHandle(batchIndex));
            return arr;
        }

        extern public void SetBatchBounds(int batchIndex, Bounds bounds);

        public extern int GetNumBatches();
        public extern void RemoveBatch(int index);

        unsafe extern void* GetBatchMatrices(int batchIndex, out int matrixCount);


        extern private AtomicSafetyHandle GetMatricesSafetyHandle(int batchIndex);
        static extern IntPtr Create(BatchRendererGroup group);
        static extern void Destroy(IntPtr groupHandle);

        [RequiredByNativeCode]
        unsafe static void InvokeOnPerformCulling(BatchRendererGroup group, ref BatchRendererCullingOutput context, ref LODParameters lodParameters)
        {
            BatchCullingContext ctx;

            ctx.cullingPlanes = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Plane>(context.cullingPlanes, context.cullingPlanesCount, Allocator.Invalid);
            ctx.batchVisibility = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<BatchVisibility>(context.batchVisibility, context.batchVisibilityCount, Allocator.Invalid);
            ctx.visibleIndices = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(context.visibleIndices, context.visibleIndicesCount, Allocator.Invalid);
            ctx.lodParameters = lodParameters;

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref ctx.cullingPlanes, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref ctx.batchVisibility, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref ctx.visibleIndices, AtomicSafetyHandle.Create());
            try
            {
                context.cullingJobsFence = group.m_PerformCulling(group, ctx);
            }
            finally
            {
                JobHandle.ScheduleBatchedJobs();

                //@TODO: Check that the no jobs using the buffers have been scheduled that are not returned here...
                AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(ctx.cullingPlanes));
                AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(ctx.batchVisibility));
                AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(ctx.visibleIndices));
            }
        }
    }
}
