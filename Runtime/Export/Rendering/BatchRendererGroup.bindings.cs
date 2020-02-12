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
        readonly public int offset;
        readonly public int instancesCount;
        public int visibleCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Camera/BatchRendererGroup.h")]
    [UsedByNativeCode]
    unsafe public struct BatchCullingContext
    {
        public BatchCullingContext(NativeArray<Plane> inCullingPlanes, NativeArray<BatchVisibility> inOutBatchVisibility, NativeArray<int> outVisibleIndices, LODParameters inLodParameters)
        {
            cullingPlanes = inCullingPlanes;
            batchVisibility = inOutBatchVisibility;
            visibleIndices = outVisibleIndices;
            lodParameters = inLodParameters;
        }

        readonly public NativeArray<Plane> cullingPlanes;
        public NativeArray<BatchVisibility> batchVisibility;
        public NativeArray<int> visibleIndices;
        readonly public LODParameters lodParameters;
    }

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
    }

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

        // sceneCullingMask default is native kDefaultSceneCullingMask
        public extern int AddBatch(Mesh mesh, int subMeshIndex, Material material, int layer, ShadowCastingMode castShadows, bool receiveShadows, bool invertCulling, Bounds bounds, int instanceCount, MaterialPropertyBlock customProps, GameObject associatedSceneObject, UInt64 sceneCullingMask = 1UL << 63);

        public extern void  SetBatchFlags(int batchIndex, UInt64 flags);
        unsafe public void SetBatchPropertyMetadata(int batchIndex, NativeArray<int> cbufferLengths, NativeArray<int> cbufferMetadata)
        {
            InternalSetBatchPropertyMetadata(batchIndex, (IntPtr)cbufferLengths.GetUnsafeReadOnlyPtr(), cbufferLengths.Length, (IntPtr)cbufferMetadata.GetUnsafeReadOnlyPtr(), cbufferMetadata.Length);
        }

        extern private void InternalSetBatchPropertyMetadata(int batchIndex, IntPtr cbufferLengths, int cbufferLengthsCount, IntPtr cbufferMetadata, int cbufferMetadataCount);

        public extern void SetInstancingData(int batchIndex, int instanceCount, MaterialPropertyBlock customProps);

        unsafe public NativeArray<Matrix4x4> GetBatchMatrices(int batchIndex)
        {
            int matricesCount = 0;
            var matrices = GetBatchMatrices(batchIndex, out matricesCount);
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Matrix4x4>((void*)matrices, matricesCount, Allocator.Invalid);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, GetMatricesSafetyHandle(batchIndex));
            return arr;
        }

        unsafe public NativeArray<int> GetBatchScalarArrayInt(int batchIndex, string propertyName)
        {
            int elementCount = 0;

            var elements = GetBatchScalarArray(batchIndex, propertyName, out elementCount);
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>((void*)elements, elementCount, Allocator.Invalid);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, GetBatchArraySafetyHandle(batchIndex, propertyName));
            return arr;
        }

        unsafe public NativeArray<float> GetBatchScalarArray(int batchIndex, string propertyName)
        {
            int elementCount = 0;

            var elements = GetBatchScalarArray(batchIndex, propertyName, out elementCount);
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<float>((void*)elements, elementCount, Allocator.Invalid);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, GetBatchArraySafetyHandle(batchIndex, propertyName));
            return arr;
        }

        unsafe public NativeArray<int> GetBatchVectorArrayInt(int batchIndex, string propertyName)
        {
            int elementCount = 0;

            var elements = GetBatchVectorArray(batchIndex, propertyName, out elementCount);
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>((void*)elements, elementCount, Allocator.Invalid);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, GetBatchArraySafetyHandle(batchIndex, propertyName));
            return arr;
        }

        unsafe public NativeArray<Vector4> GetBatchVectorArray(int batchIndex, string propertyName)
        {
            int elementCount = 0;

            var elements = GetBatchVectorArray(batchIndex, propertyName, out elementCount);
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector4>((void*)elements, elementCount, Allocator.Invalid);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, GetBatchArraySafetyHandle(batchIndex, propertyName));
            return arr;
        }

        unsafe public NativeArray<Matrix4x4> GetBatchMatrixArray(int batchIndex, string propertyName)
        {
            int elementCount = 0;

            var elements = GetBatchMatrixArray(batchIndex, propertyName, out elementCount);
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Matrix4x4>((void*)elements, elementCount, Allocator.Invalid);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, GetBatchArraySafetyHandle(batchIndex, propertyName));
            return arr;
        }

        unsafe public NativeArray<int> GetBatchScalarArrayInt(int batchIndex, int propertyName)
        {
            int elementCount = 0;

            var elements = GetBatchScalarArray_Internal(batchIndex, propertyName, out elementCount);
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>((void*)elements, elementCount, Allocator.Invalid);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, GetBatchArraySafetyHandle_Int(batchIndex, propertyName));
            return arr;
        }

        unsafe public NativeArray<float> GetBatchScalarArray(int batchIndex, int propertyName)
        {
            int elementCount = 0;

            var elements = GetBatchScalarArray_Internal(batchIndex, propertyName, out elementCount);
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<float>((void*)elements, elementCount, Allocator.Invalid);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, GetBatchArraySafetyHandle_Int(batchIndex, propertyName));
            return arr;
        }

        unsafe public NativeArray<int> GetBatchVectorArrayInt(int batchIndex, int propertyName)
        {
            int elementCount = 0;

            var elements = GetBatchVectorArray_Internal(batchIndex, propertyName, out elementCount);
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>((void*)elements, elementCount, Allocator.Invalid);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, GetBatchArraySafetyHandle_Int(batchIndex, propertyName));
            return arr;
        }

        unsafe public NativeArray<Vector4> GetBatchVectorArray(int batchIndex, int propertyName)
        {
            int elementCount = 0;

            var elements = GetBatchVectorArray_Internal(batchIndex, propertyName, out elementCount);
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Vector4>((void*)elements, elementCount, Allocator.Invalid);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, GetBatchArraySafetyHandle_Int(batchIndex, propertyName));
            return arr;
        }

        unsafe public NativeArray<Matrix4x4> GetBatchMatrixArray(int batchIndex, int propertyName)
        {
            int elementCount = 0;

            var elements = GetBatchMatrixArray_Internal(batchIndex, propertyName, out elementCount);
            var arr = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Matrix4x4>((void*)elements, elementCount, Allocator.Invalid);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref arr, GetBatchArraySafetyHandle_Int(batchIndex, propertyName));
            return arr;
        }

        extern public void SetBatchBounds(int batchIndex, Bounds bounds);

        public extern int GetNumBatches();
        public extern void RemoveBatch(int index);

        unsafe extern void* GetBatchMatrices(int batchIndex, out int matrixCount);
        unsafe extern void* GetBatchScalarArray(int batchIndex, string propertyName, out int elementCount);
        unsafe extern void* GetBatchVectorArray(int batchIndex, string propertyName, out int elementCount);
        unsafe extern void* GetBatchMatrixArray(int batchIndex, string propertyName, out int elementCount);
        [NativeName("GetBatchScalarArray")]
        unsafe extern void* GetBatchScalarArray_Internal(int batchIndex, int propertyName, out int elementCount);
        [NativeName("GetBatchVectorArray")]
        unsafe extern void* GetBatchVectorArray_Internal(int batchIndex, int propertyName, out int elementCount);
        [NativeName("GetBatchMatrixArray")]
        unsafe extern void* GetBatchMatrixArray_Internal(int batchIndex, int propertyName, out int elementCount);
        extern private AtomicSafetyHandle GetMatricesSafetyHandle(int batchIndex);
        extern private AtomicSafetyHandle GetBatchArraySafetyHandle(int batchIndex, string propertyName);
        [NativeName("GetBatchArraySafetyHandle")]
        extern private AtomicSafetyHandle GetBatchArraySafetyHandle_Int(int batchIndex, int propertyName);
        static extern IntPtr Create(BatchRendererGroup group);
        static extern void Destroy(IntPtr groupHandle);

        [RequiredByNativeCode]
        unsafe static void InvokeOnPerformCulling(BatchRendererGroup group, ref BatchRendererCullingOutput context, ref LODParameters lodParameters)
        {
            NativeArray<Plane> cullingPlanes = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<Plane>(context.cullingPlanes, context.cullingPlanesCount, Allocator.Invalid);
            NativeArray<BatchVisibility> batchVisibility = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<BatchVisibility>(context.batchVisibility, context.batchVisibilityCount, Allocator.Invalid);
            NativeArray<int> visibleIndices = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(context.visibleIndices, context.visibleIndicesCount, Allocator.Invalid);

            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref cullingPlanes, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref batchVisibility, AtomicSafetyHandle.Create());
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref visibleIndices, AtomicSafetyHandle.Create());

            try
            {
                context.cullingJobsFence = group.m_PerformCulling(group, new BatchCullingContext(cullingPlanes, batchVisibility, visibleIndices, lodParameters));
            }
            finally
            {
                JobHandle.ScheduleBatchedJobs();

                //@TODO: Check that the no jobs using the buffers have been scheduled that are not returned here...
                AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(cullingPlanes));
                AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(batchVisibility));
                AtomicSafetyHandle.Release(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(visibleIndices));
            }
        }
    }
}
