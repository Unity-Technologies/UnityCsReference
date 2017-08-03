// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace UnityEngine.Experimental.Rendering
{
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    unsafe public struct CoreCameraValues
    {
        int filterMode;
        uint cullingMask;
        int guid;
        int renderImmediateObjects;
    };

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    unsafe public struct CameraProperties
    {
        //This needs to be kept in sync with TagManager.h
        private const int kNumLayers = 32;

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

        private fixed float _shadowCullPlanes[6 * 4];
        private fixed float _cameraCullPlanes[6 * 4];

        float baseFarDistance;

        Vector3 shadowCullCenter;
        fixed float layerCullDistances[kNumLayers];
        int layerCullSpherical;

        CoreCameraValues coreCameraValues;
        uint cameraType;

        public Plane GetShadowCullingPlane(int index)
        {
            if (index < 0 || index >= 6)
                throw new IndexOutOfRangeException("Invalid plane index");
            fixed(float* p = _shadowCullPlanes)
            {
                return new Plane(new Vector3(p[index * 4 + 0], p[index * 4 + 1], p[index * 4 + 2]), p[index * 4 + 3]);
            }
        }

        public void SetShadowCullingPlane(int index, Plane plane)
        {
            if (index < 0 || index >= 6)
                throw new IndexOutOfRangeException("Invalid plane index");
            fixed(float* p = _shadowCullPlanes)
            {
                p[index * 4 + 0] = plane.normal.x;
                p[index * 4 + 1] = plane.normal.y;
                p[index * 4 + 2] = plane.normal.z;
                p[index * 4 + 3] = plane.distance;
            }
        }

        public Plane GetCameraCullingPlane(int index)
        {
            if (index < 0 || index >= 6)
                throw new IndexOutOfRangeException("Invalid plane index");
            fixed(float* p = _cameraCullPlanes)
            {
                return new Plane(new Vector3(p[index * 4 + 0], p[index * 4 + 1], p[index * 4 + 2]), p[index * 4 + 3]);
            }
        }

        public void SetCameraCullingPlane(int index, Plane plane)
        {
            if (index < 0 || index >= 6)
                throw new IndexOutOfRangeException("Invalid plane index");
            fixed(float* p = _cameraCullPlanes)
            {
                p[index * 4 + 0] = plane.normal.x;
                p[index * 4 + 1] = plane.normal.y;
                p[index * 4 + 2] = plane.normal.z;
                p[index * 4 + 3] = plane.distance;
            }
        }
    };

    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct ScriptableCullingParameters
    {
        private int m_IsOrthographic;
        private LODParameters m_LodParameters;

        private fixed float m_CullingPlanes[10 * 4];
        private int m_CullingPlaneCount;

        private int m_CullingMask;
        private Int64 m_SceneMask;

        private fixed float m_LayerFarCullDistances[32];
        private int m_LayerCull;

        private Matrix4x4 m_CullingMatrix;
        private Vector3 m_Position;

        private float m_shadowDistance;

        private int m_CullingFlags;

        private ReflectionProbeSortOptions m_ReflectionProbeSortOptions;

        private CameraProperties m_CameraProperties;

        public Matrix4x4 cullStereoView;
        public Matrix4x4 cullStereoProj;
        public float cullStereoSeparation;
        private int padding2;

        public int cullingPlaneCount
        {
            get { return m_CullingPlaneCount; }
            set
            {
                if (value < 0 || value > 10)
                    throw new IndexOutOfRangeException("Invalid plane count (0 <= count <= 10)");
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
            get { return m_LodParameters; }
            set { m_LodParameters = value; }
        }

        public int cullingMask
        {
            get { return m_CullingMask; }
            set { m_CullingMask = value; }
        }

        public long sceneMask
        {
            get { return m_SceneMask; }
            set { m_SceneMask = value; }
        }

        public int layerCull
        {
            get { return m_LayerCull; }
            set { m_LayerCull = value; }
        }

        public Matrix4x4 cullingMatrix
        {
            get { return m_CullingMatrix; }
            set { m_CullingMatrix = value; }
        }

        public Vector3 position
        {
            get { return m_Position; }
            set { m_Position = value; }
        }

        public float shadowDistance
        {
            get { return m_shadowDistance; }
            set { m_shadowDistance = value; }
        }

        public int cullingFlags
        {
            get { return m_CullingFlags; }
            set { m_CullingFlags = value; }
        }

        public ReflectionProbeSortOptions reflectionProbeSortOptions
        {
            get { return m_ReflectionProbeSortOptions; }
            set { m_ReflectionProbeSortOptions = value; }
        }

        public CameraProperties cameraProperties
        {
            get { return m_CameraProperties; }
            set { m_CameraProperties = value; }
        }

        public float GetLayerCullDistance(int layerIndex)
        {
            if (layerIndex < 0 || layerIndex >= 32)
                throw new IndexOutOfRangeException("Invalid layer index");
            fixed(float* p = m_LayerFarCullDistances)
            {
                return p[layerIndex];
            }
        }

        public void SetLayerCullDistance(int layerIndex, float distance)
        {
            if (layerIndex < 0 || layerIndex >= 32)
                throw new IndexOutOfRangeException("Invalid layer index");
            fixed(float* p = m_LayerFarCullDistances)
            {
                p[layerIndex] = distance;
            }
        }

        public Plane GetCullingPlane(int index)
        {
            if (index < 0 || index >= cullingPlaneCount || index >= 10)
                throw new IndexOutOfRangeException("Invalid plane index");
            fixed(float* p = m_CullingPlanes)
            {
                return new Plane(new Vector3(p[index * 4 + 0], p[index * 4 + 1], p[index * 4 + 2]), p[index * 4 + 3]);
            }
        }

        public void SetCullingPlane(int index, Plane plane)
        {
            if (index < 0 || index >= cullingPlaneCount || index >= 10)
                throw new IndexOutOfRangeException("Invalid plane index");
            fixed(float* p = m_CullingPlanes)
            {
                p[index * 4 + 0] = plane.normal.x;
                p[index * 4 + 1] = plane.normal.y;
                p[index * 4 + 2] = plane.normal.z;
                p[index * 4 + 3] = plane.distance;
            }
        }
    }
}
