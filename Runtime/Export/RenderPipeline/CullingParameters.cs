// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting;

namespace UnityEngine.Rendering
{
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    struct CoreCameraValues : IEquatable<CoreCameraValues>
    {
        int filterMode;
        uint cullingMask;
        int instanceID;
        int renderImmediateObjects;

        public bool Equals(CoreCameraValues other)
        {
            return filterMode == other.filterMode && cullingMask == other.cullingMask && instanceID == other.instanceID && renderImmediateObjects == other.renderImmediateObjects;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is CoreCameraValues && Equals((CoreCameraValues)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = filterMode;
                hashCode = (hashCode * 397) ^ (int)cullingMask;
                hashCode = (hashCode * 397) ^ instanceID;
                hashCode = (hashCode * 397) ^ renderImmediateObjects;
                return hashCode;
            }
        }

        public static bool operator==(CoreCameraValues left, CoreCameraValues right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(CoreCameraValues left, CoreCameraValues right)
        {
            return !left.Equals(right);
        }
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    unsafe public struct CameraProperties : IEquatable<CameraProperties>
    {
        //This needs to be kept in sync with TagManager.h
        const int k_NumLayers = 32;

        Rect screenRect;
        Vector3 viewDir;
        float projectionNear;
        float projectionFar;
        float cameraNear;
        float cameraFar;
        float cameraAspect;

        Matrix4x4 cameraToWorld;
        Matrix4x4 actualWorldToClip;
        Matrix4x4 cameraClipToWorld;
        Matrix4x4 cameraWorldToClip;
        Matrix4x4 implicitProjection;
        Matrix4x4 stereoWorldToClipLeft;
        Matrix4x4 stereoWorldToClipRight;
        Matrix4x4 worldToCamera;

        Vector3 up;
        Vector3 right;
        Vector3 transformDirection;
        Vector3 cameraEuler;
        Vector3 velocity;

        float farPlaneWorldSpaceLength;

        uint rendererCount;

        // can't make fixed types private, because then the compiler generates different code which BindingsGenerator does not handle yet.
        // We can expose these when we have ref returns
        const int k_PlaneCount = 6;
        internal unsafe fixed byte m_ShadowCullPlanes[k_PlaneCount * Plane.size];
        internal unsafe fixed byte m_CameraCullPlanes[k_PlaneCount * Plane.size];

        float baseFarDistance;

        Vector3 shadowCullCenter;
        // can't make fixed types private, because then the compiler generates different code which BindingsGenerator does not handle yet.
        internal fixed float layerCullDistances[k_NumLayers];
        int layerCullSpherical;

        CoreCameraValues coreCameraValues;
        uint cameraType;
        private int projectionIsOblique;
        private int isImplicitProjectionMatrix;

        public Plane GetShadowCullingPlane(int index)
        {
            if (index < 0 || index >= k_PlaneCount)
                throw new ArgumentOutOfRangeException($"{nameof(index)} was {index}, but must be at least 0 and less than {k_PlaneCount}");
            unsafe
            {
                fixed(byte* ptr = m_ShadowCullPlanes)
                {
                    var planes = (Plane*)ptr;
                    return planes[index];
                }
            }
        }

        public void SetShadowCullingPlane(int index, Plane plane)
        {
            if (index < 0 || index >= k_PlaneCount)
                throw new ArgumentOutOfRangeException($"{nameof(index)} was {index}, but must be at least 0 and less than {k_PlaneCount}");
            unsafe
            {
                fixed(byte* ptr = m_ShadowCullPlanes)
                {
                    var planes = (Plane*)ptr;
                    planes[index] = plane;
                }
            }
        }

        public Plane GetCameraCullingPlane(int index)
        {
            if (index < 0 || index >= k_PlaneCount)
                throw new ArgumentOutOfRangeException($"{nameof(index)} was {index}, but must be at least 0 and less than {k_PlaneCount}");
            unsafe
            {
                fixed(byte* ptr = m_CameraCullPlanes)
                {
                    var planes = (Plane*)ptr;
                    return planes[index];
                }
            }
        }

        public void SetCameraCullingPlane(int index, Plane plane)
        {
            if (index < 0 || index >= k_PlaneCount)
                throw new ArgumentOutOfRangeException($"{nameof(index)} was {index}, but must be at least 0 and less than {k_PlaneCount}");
            unsafe
            {
                fixed(byte* ptr = m_CameraCullPlanes)
                {
                    var planes = (Plane*)ptr;
                    planes[index] = plane;
                }
            }
        }

        public bool Equals(CameraProperties other)
        {
            for (var i = 0; i < k_PlaneCount; i++)
            {
                if (!GetShadowCullingPlane(i).Equals(other.GetShadowCullingPlane(i)))
                    return false;
            }

            for (var i = 0; i < k_PlaneCount; i++)
            {
                if (!GetCameraCullingPlane(i).Equals(other.GetCameraCullingPlane(i)))
                    return false;
            }

            fixed(float* distancesPtr = layerCullDistances)
            {
                for (var i = 0; i < k_NumLayers; i++)
                {
                    if (distancesPtr[i] != other.layerCullDistances[i])
                        return false;
                }
            }

            // m_ShadowCullPlanes == other.m_ShadowCullPlanes
            // m_CameraCullPlanes == other.m_CameraCullPlanes
            // layerCullDistances == other.layerCullDistances
            return screenRect.Equals(other.screenRect) && viewDir.Equals(other.viewDir) && projectionNear.Equals(other.projectionNear) && projectionFar.Equals(other.projectionFar) && cameraNear.Equals(other.cameraNear) && cameraFar.Equals(other.cameraFar) && cameraAspect.Equals(other.cameraAspect) && cameraToWorld.Equals(other.cameraToWorld) && actualWorldToClip.Equals(other.actualWorldToClip) && cameraClipToWorld.Equals(other.cameraClipToWorld) && cameraWorldToClip.Equals(other.cameraWorldToClip) && implicitProjection.Equals(other.implicitProjection) && stereoWorldToClipLeft.Equals(other.stereoWorldToClipLeft) && stereoWorldToClipRight.Equals(other.stereoWorldToClipRight) && worldToCamera.Equals(other.worldToCamera) && up.Equals(other.up) && right.Equals(other.right) && transformDirection.Equals(other.transformDirection) && cameraEuler.Equals(other.cameraEuler) && velocity.Equals(other.velocity) && farPlaneWorldSpaceLength.Equals(other.farPlaneWorldSpaceLength) && rendererCount == other.rendererCount && baseFarDistance.Equals(other.baseFarDistance) && shadowCullCenter.Equals(other.shadowCullCenter) && layerCullSpherical == other.layerCullSpherical && coreCameraValues.Equals(other.coreCameraValues) && cameraType == other.cameraType && projectionIsOblique == other.projectionIsOblique && isImplicitProjectionMatrix == other.isImplicitProjectionMatrix;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is CameraProperties && Equals((CameraProperties)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = screenRect.GetHashCode();
                hashCode = (hashCode * 397) ^ viewDir.GetHashCode();
                hashCode = (hashCode * 397) ^ projectionNear.GetHashCode();
                hashCode = (hashCode * 397) ^ projectionFar.GetHashCode();
                hashCode = (hashCode * 397) ^ cameraNear.GetHashCode();
                hashCode = (hashCode * 397) ^ cameraFar.GetHashCode();
                hashCode = (hashCode * 397) ^ cameraAspect.GetHashCode();
                hashCode = (hashCode * 397) ^ cameraToWorld.GetHashCode();
                hashCode = (hashCode * 397) ^ actualWorldToClip.GetHashCode();
                hashCode = (hashCode * 397) ^ cameraClipToWorld.GetHashCode();
                hashCode = (hashCode * 397) ^ cameraWorldToClip.GetHashCode();
                hashCode = (hashCode * 397) ^ implicitProjection.GetHashCode();
                hashCode = (hashCode * 397) ^ stereoWorldToClipLeft.GetHashCode();
                hashCode = (hashCode * 397) ^ stereoWorldToClipRight.GetHashCode();
                hashCode = (hashCode * 397) ^ worldToCamera.GetHashCode();
                hashCode = (hashCode * 397) ^ up.GetHashCode();
                hashCode = (hashCode * 397) ^ right.GetHashCode();
                hashCode = (hashCode * 397) ^ transformDirection.GetHashCode();
                hashCode = (hashCode * 397) ^ cameraEuler.GetHashCode();
                hashCode = (hashCode * 397) ^ velocity.GetHashCode();
                hashCode = (hashCode * 397) ^ farPlaneWorldSpaceLength.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)rendererCount;
                for (var i = 0; i < k_PlaneCount; i++)
                {
                    hashCode = (hashCode * 397) ^ GetShadowCullingPlane(i).GetHashCode();
                }
                for (var i = 0; i < k_PlaneCount; i++)
                {
                    hashCode = (hashCode * 397) ^ GetCameraCullingPlane(i).GetHashCode();
                }
                hashCode = (hashCode * 397) ^ baseFarDistance.GetHashCode();
                hashCode = (hashCode * 397) ^ shadowCullCenter.GetHashCode();
                fixed(float* distancesPtr = layerCullDistances)
                {
                    for (var i = 0; i < k_NumLayers; i++)
                    {
                        hashCode = (hashCode * 397) ^ distancesPtr[i].GetHashCode();
                    }
                }
                hashCode = (hashCode * 397) ^ layerCullSpherical;
                hashCode = (hashCode * 397) ^ coreCameraValues.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)cameraType;
                hashCode = (hashCode * 397) ^ projectionIsOblique;
                hashCode = (hashCode * 397) ^ isImplicitProjectionMatrix;
                return hashCode;
            }
        }

        public static bool operator==(CameraProperties left, CameraProperties right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(CameraProperties left, CameraProperties right)
        {
            return !left.Equals(right);
        }
    }

    // Keep in sync with CameraCullingParameters.h CullingOptions
    [Flags]
    public enum CullingOptions
    {
        None = 0,
        ForceEvenIfCameraIsNotActive = 1 << 0,
        OcclusionCull = 1 << 1,
        NeedsLighting = 1 << 2,
        NeedsReflectionProbes = 1 << 3,
        Stereo = 1 << 4,
        DisablePerObjectCulling = 1 << 5,
        ShadowCasters = 1 << 6
    }

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ScriptableCullingParameters : IEquatable<ScriptableCullingParameters>
    {
        int m_IsOrthographic;
        LODParameters m_LODParameters;

        // can't make fixed types private, because then the compiler generates different code which BindinsgGenerator does not handle yet.
        const int k_MaximumCullingPlaneCount = 10;
        public static readonly int maximumCullingPlaneCount = k_MaximumCullingPlaneCount;
        internal fixed byte m_CullingPlanes[k_MaximumCullingPlaneCount * Plane.size];
        int m_CullingPlaneCount;

        uint m_CullingMask;
        ulong m_SceneMask;

        // can't make fixed types private, because then the compiler generates different code which BindinsgGenerator does not handle yet.
        const int k_LayerCount = 32;
        public static readonly int layerCount = k_LayerCount;
        internal fixed float m_LayerFarCullDistances[k_LayerCount];
        int m_LayerCull;

        Matrix4x4 m_CullingMatrix;
        Vector3 m_Origin;

        float m_ShadowDistance;

        CullingOptions m_CullingOptions;

        ReflectionProbeSortingCriteria m_ReflectionProbeSortingCriteria;

        CameraProperties m_CameraProperties;
        private float m_AccurateOcclusionThreshold;
        private int m_MaximumPortalCullingJobs;
        const int k_CullingJobCountLowerLimit = 1;

        // Keep in synch with C++ version `kUmbraCullingJobsUpperLimit`.
        const int k_CullingJobCountUpperLimit = 16;

        Matrix4x4 m_StereoViewMatrix;
        Matrix4x4 m_StereoProjectionMatrix;
        float m_StereoSeparationDistance;

        private int m_maximumVisibleLights;
        public int maximumVisibleLights
        {
            get { return m_maximumVisibleLights; }
            set { m_maximumVisibleLights = value; }
        }

        public int cullingPlaneCount
        {
            get { return m_CullingPlaneCount; }
            set
            {
                if (value < 0 || value > k_MaximumCullingPlaneCount)
                    throw new ArgumentOutOfRangeException($"{nameof(value)} was {value}, but must be at least 0 and less than {k_MaximumCullingPlaneCount}");
                m_CullingPlaneCount = value;
            }
        }

        public bool isOrthographic
        {
            get { return Convert.ToBoolean(m_IsOrthographic); }
            set { m_IsOrthographic = Convert.ToInt32(value); }
        }

        public LODParameters lodParameters
        {
            get { return m_LODParameters; }
            set { m_LODParameters = value; }
        }

        public uint cullingMask
        {
            get { return m_CullingMask; }
            set { m_CullingMask = value; }
        }

        public Matrix4x4 cullingMatrix
        {
            get { return m_CullingMatrix; }
            set { m_CullingMatrix = value; }
        }

        public Vector3 origin
        {
            get { return m_Origin; }
            set { m_Origin = value; }
        }

        public float shadowDistance
        {
            get { return m_ShadowDistance; }
            set { m_ShadowDistance = value; }
        }

        public CullingOptions cullingOptions
        {
            get { return m_CullingOptions; }
            set { m_CullingOptions = value; }
        }

        public ReflectionProbeSortingCriteria reflectionProbeSortingCriteria
        {
            get { return m_ReflectionProbeSortingCriteria; }
            set { m_ReflectionProbeSortingCriteria = value; }
        }

        public CameraProperties cameraProperties
        {
            get { return m_CameraProperties; }
            set { m_CameraProperties = value; }
        }

        public Matrix4x4 stereoViewMatrix
        {
            get { return m_StereoViewMatrix; }
            set { m_StereoViewMatrix = value; }
        }

        public Matrix4x4 stereoProjectionMatrix
        {
            get { return m_StereoProjectionMatrix; }
            set { m_StereoProjectionMatrix = value; }
        }

        public float stereoSeparationDistance
        {
            get { return m_StereoSeparationDistance; }
            set { m_StereoSeparationDistance = value; }
        }

        public float accurateOcclusionThreshold
        {
            get { return m_AccurateOcclusionThreshold; }
            set { m_AccurateOcclusionThreshold = Mathf.Max(-1f, value); }
        }

        public int maximumPortalCullingJobs
        {
            get { return m_MaximumPortalCullingJobs; }
            set
            {
                if (value < k_CullingJobCountLowerLimit || value > k_CullingJobCountUpperLimit)
                    throw new ArgumentOutOfRangeException($"{nameof(maximumPortalCullingJobs)} was {maximumPortalCullingJobs}, but must be in range {k_CullingJobCountLowerLimit} to {k_CullingJobCountUpperLimit}");
                m_MaximumPortalCullingJobs = value;
            }
        }

        public static int cullingJobsLowerLimit
        {
            get { return k_CullingJobCountLowerLimit; }
        }

        public static int cullingJobsUpperLimit
        {
            get { return k_CullingJobCountUpperLimit; }
        }

        public float GetLayerCullingDistance(int layerIndex)
        {
            if (layerIndex < 0 || layerIndex >= k_LayerCount)
                throw new ArgumentOutOfRangeException($"{nameof(layerIndex)} was {layerIndex}, but must be at least 0 and less than {k_LayerCount}");
            fixed(float* ptr = m_LayerFarCullDistances)
            {
                return ptr[layerIndex];
            }
        }

        public void SetLayerCullingDistance(int layerIndex, float distance)
        {
            if (layerIndex < 0 || layerIndex >= k_LayerCount)
                throw new ArgumentOutOfRangeException($"{nameof(layerIndex)} was {layerIndex}, but must be at least 0 and less than {k_LayerCount}");
            fixed(float* p = m_LayerFarCullDistances)
            {
                p[layerIndex] = distance;
            }
        }

        public Plane GetCullingPlane(int index)
        {
            if (index < 0 || index >= cullingPlaneCount)
                throw new ArgumentOutOfRangeException($"{nameof(index)} was {index}, but must be at least 0 and less than {cullingPlaneCount}");
            fixed(byte* ptr = m_CullingPlanes)
            {
                var planes = (Plane*)ptr;
                return planes[index];
            }
        }

        public void SetCullingPlane(int index, Plane plane)
        {
            if (index < 0 || index >= cullingPlaneCount)
                throw new ArgumentOutOfRangeException($"{nameof(index)} was {index}, but must be at least 0 and less than {cullingPlaneCount}");
            fixed(byte* ptr = m_CullingPlanes)
            {
                var planes = (Plane*)ptr;
                planes[index] = plane;
            }
        }

        public bool Equals(ScriptableCullingParameters other)
        {
            for (var i = 0; i < k_LayerCount; i++)
            {
                if (!GetLayerCullingDistance(i).Equals(other.GetLayerCullingDistance(i)))
                    return false;
            }

            for (var i = 0; i < cullingPlaneCount; i++)
            {
                if (!GetCullingPlane(i).Equals(other.GetCullingPlane(i)))
                    return false;
            }

            return m_IsOrthographic == other.m_IsOrthographic
                && m_LODParameters.Equals(other.m_LODParameters)
                && m_CullingPlaneCount == other.m_CullingPlaneCount
                && m_CullingMask == other.m_CullingMask
                && m_SceneMask == other.m_SceneMask
                && m_LayerCull == other.m_LayerCull
                && m_CullingMatrix.Equals(other.m_CullingMatrix)
                && m_Origin.Equals(other.m_Origin)
                && m_ShadowDistance.Equals(other.m_ShadowDistance)
                && m_CullingOptions == other.m_CullingOptions
                && m_ReflectionProbeSortingCriteria == other.m_ReflectionProbeSortingCriteria
                && m_CameraProperties.Equals(other.m_CameraProperties)
                && m_AccurateOcclusionThreshold.Equals(other.m_AccurateOcclusionThreshold)
                && m_StereoViewMatrix.Equals(other.m_StereoViewMatrix)
                && m_StereoProjectionMatrix.Equals(other.m_StereoProjectionMatrix)
                && m_StereoSeparationDistance.Equals(other.m_StereoSeparationDistance)
                && m_maximumVisibleLights == other.m_maximumVisibleLights;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ScriptableCullingParameters && Equals((ScriptableCullingParameters)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = m_IsOrthographic;
                hashCode = (hashCode * 397) ^ m_LODParameters.GetHashCode();
                hashCode = (hashCode * 397) ^ m_CullingPlaneCount;
                hashCode = (hashCode * 397) ^ (int)m_CullingMask;
                hashCode = (hashCode * 397) ^ m_SceneMask.GetHashCode();
                hashCode = (hashCode * 397) ^ m_LayerCull;
                hashCode = (hashCode * 397) ^ m_CullingMatrix.GetHashCode();
                hashCode = (hashCode * 397) ^ m_Origin.GetHashCode();
                hashCode = (hashCode * 397) ^ m_ShadowDistance.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)m_CullingOptions;
                hashCode = (hashCode * 397) ^ (int)m_ReflectionProbeSortingCriteria;
                hashCode = (hashCode * 397) ^ m_CameraProperties.GetHashCode();
                hashCode = (hashCode * 397) ^ m_AccurateOcclusionThreshold.GetHashCode();
                hashCode = (hashCode * 397) ^ m_MaximumPortalCullingJobs.GetHashCode();
                hashCode = (hashCode * 397) ^ m_StereoViewMatrix.GetHashCode();
                hashCode = (hashCode * 397) ^ m_StereoProjectionMatrix.GetHashCode();
                hashCode = (hashCode * 397) ^ m_StereoSeparationDistance.GetHashCode();
                hashCode = (hashCode * 397) ^ m_maximumVisibleLights;
                return hashCode;
            }
        }

        public static bool operator==(ScriptableCullingParameters left, ScriptableCullingParameters right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(ScriptableCullingParameters left, ScriptableCullingParameters right)
        {
            return !left.Equals(right);
        }
    }
}
