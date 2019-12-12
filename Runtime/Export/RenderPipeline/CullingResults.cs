// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Rendering;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine.Rendering
{
    // Must match ScriptableCullingAllocationInfo in C++
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct CullingAllocationInfo
    {
        public VisibleLight *visibleLightsPtr;
        public VisibleLight *visibleOffscreenVertexLightsPtr;
        public VisibleReflectionProbe *visibleReflectionProbesPtr;
        public int visibleLightCount;
        public int visibleOffscreenVertexLightCount;
        public int visibleReflectionProbeCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public partial struct CullingResults : IEquatable<CullingResults>
    {
        internal IntPtr ptr;
        unsafe CullingAllocationInfo* m_AllocationInfo;
        AtomicSafetyHandle m_Safety;

        public unsafe NativeArray<VisibleLight> visibleLights => GetNativeArray<VisibleLight>(m_AllocationInfo->visibleLightsPtr, m_AllocationInfo->visibleLightCount);

        public unsafe NativeArray<VisibleLight> visibleOffscreenVertexLights => GetNativeArray<VisibleLight>(m_AllocationInfo->visibleOffscreenVertexLightsPtr, m_AllocationInfo->visibleOffscreenVertexLightCount);

        public unsafe NativeArray<VisibleReflectionProbe> visibleReflectionProbes => GetNativeArray<VisibleReflectionProbe>(m_AllocationInfo->visibleReflectionProbesPtr, m_AllocationInfo->visibleReflectionProbeCount);

        unsafe NativeArray<T> GetNativeArray<T>(void* dataPointer, int length) where T : struct
        {
            // No need to validate here as we pass the safety handle on to the native array, which will also validate it.
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(dataPointer, length, Allocator.Invalid);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_Safety);
            return array;
        }

        public int lightIndexCount
        {
            get
            {
                Validate();
                return GetLightIndexCount(ptr);
            }
        }

        public int reflectionProbeIndexCount
        {
            get
            {
                Validate();
                return GetReflectionProbeIndexCount(ptr);
            }
        }

        public int lightAndReflectionProbeIndexCount
        {
            get
            {
                Validate();
                return GetLightIndexCount(ptr) + GetReflectionProbeIndexCount(ptr);
            }
        }

        public void FillLightAndReflectionProbeIndices(ComputeBuffer computeBuffer)
        {
            Validate();
            FillLightAndReflectionProbeIndices(ptr, computeBuffer);
        }

        public void FillLightAndReflectionProbeIndices(GraphicsBuffer buffer)
        {
            Validate();
            FillLightAndReflectionProbeIndicesGraphicsBuffer(ptr, buffer);
        }

        public unsafe NativeArray<int> GetLightIndexMap(Allocator allocator)
        {
            Validate();
            var lightIndexMapSize = GetLightIndexMapSize(ptr);
            var lightIndexMap = new NativeArray<int>(lightIndexMapSize, allocator, NativeArrayOptions.UninitializedMemory);
            FillLightIndexMap(ptr, (IntPtr)lightIndexMap.GetUnsafePtr(), lightIndexMapSize);
            return lightIndexMap;
        }

        public unsafe void SetLightIndexMap(NativeArray<int> lightIndexMap)
        {
            Validate();
            SetLightIndexMap(ptr, (IntPtr)lightIndexMap.GetUnsafeReadOnlyPtr(), lightIndexMap.Length);
        }

        public unsafe NativeArray<int> GetReflectionProbeIndexMap(Allocator allocator)
        {
            Validate();
            var lightIndexMapSize = GetReflectionProbeIndexMapSize(ptr);
            var lightIndexMap = new NativeArray<int>(lightIndexMapSize, allocator, NativeArrayOptions.UninitializedMemory);
            FillReflectionProbeIndexMap(ptr, (IntPtr)lightIndexMap.GetUnsafePtr(), lightIndexMapSize);
            return lightIndexMap;
        }

        public unsafe void SetReflectionProbeIndexMap(NativeArray<int> lightIndexMap)
        {
            Validate();
            SetReflectionProbeIndexMap(ptr, (IntPtr)lightIndexMap.GetUnsafeReadOnlyPtr(), lightIndexMap.Length);
        }

        public bool GetShadowCasterBounds(int lightIndex, out Bounds outBounds)
        {
            Validate();
            return GetShadowCasterBounds(ptr, lightIndex, out outBounds);
        }

        public bool ComputeSpotShadowMatricesAndCullingPrimitives(int activeLightIndex,
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData)
        {
            Validate();
            return ComputeSpotShadowMatricesAndCullingPrimitives(ptr, activeLightIndex,
                out viewMatrix, out projMatrix, out shadowSplitData);
        }

        public bool ComputePointShadowMatricesAndCullingPrimitives(int activeLightIndex,
            CubemapFace cubemapFace, float fovBias,
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData)
        {
            Validate();
            return ComputePointShadowMatricesAndCullingPrimitives(ptr, activeLightIndex,
                cubemapFace, fovBias,
                out viewMatrix, out projMatrix, out shadowSplitData);
        }

        public bool ComputeDirectionalShadowMatricesAndCullingPrimitives(int activeLightIndex,
            int splitIndex, int splitCount, Vector3 splitRatio, int shadowResolution, float shadowNearPlaneOffset,
            out Matrix4x4 viewMatrix, out Matrix4x4 projMatrix, out ShadowSplitData shadowSplitData)
        {
            Validate();
            return ComputeDirectionalShadowMatricesAndCullingPrimitives(ptr, activeLightIndex,
                splitIndex, splitCount, splitRatio, shadowResolution, shadowNearPlaneOffset,
                out viewMatrix, out projMatrix, out shadowSplitData);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void Validate()
        {
            if (ptr == IntPtr.Zero)
                throw new InvalidOperationException($"The {nameof(CullingResults)} instance is invalid. This can happen if you construct an instance using the default constructor.");

            try
            {
                AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"The {nameof(CullingResults)} instance is no longer valid. This can happen if you re-use it across multiple frames.", e);
            }
        }

        public unsafe bool Equals(CullingResults other)
        {
            return ptr.Equals(other.ptr) && m_AllocationInfo == other.m_AllocationInfo;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is CullingResults && Equals((CullingResults)obj);
        }

        public override unsafe int GetHashCode()
        {
            unchecked
            {
                var hashCode = ptr.GetHashCode();
                hashCode = (hashCode * 397) ^ unchecked((int)(long)m_AllocationInfo);
                return hashCode;
            }
        }

        public static bool operator==(CullingResults left, CullingResults right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(CullingResults left, CullingResults right)
        {
            return !left.Equals(right);
        }
    }
}
