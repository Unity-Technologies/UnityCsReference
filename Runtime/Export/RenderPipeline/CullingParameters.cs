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
    unsafe public struct CullingParameters
    {
        public int isOrthographic;
        public LODParameters lodParameters;

        private fixed float _cullingPlanes[10 * 4];
        public int cullingPlaneCount;

        public int cullingMask;

        private fixed float _layerFarCullDistances[32];
        private int layerCull;

        public Matrix4x4 cullingMatrix;
        public Vector3 position;

        public float shadowDistance;

#pragma warning disable 414
        private int _cullingFlags;
        private int _cameraInstanceID;
#pragma warning restore 414

        public ReflectionProbeSortOptions reflectionProbeSortOptions;

        public float GetLayerCullDistance(int layerIndex)
        {
            if (layerIndex < 0 || layerIndex >= 32)
                throw new IndexOutOfRangeException("Invalid layer index");
            fixed(float* p = _layerFarCullDistances)
            {
                return p[layerIndex];
            }
        }

        public void SetLayerCullDistance(int layerIndex, float distance)
        {
            if (layerIndex < 0 || layerIndex >= 32)
                throw new IndexOutOfRangeException("Invalid layer index");
            fixed(float* p = _layerFarCullDistances)
            {
                p[layerIndex] = distance;
            }
        }

        public Plane GetCullingPlane(int index)
        {
            if (index < 0 || index >= cullingPlaneCount || index >= 10)
                throw new IndexOutOfRangeException("Invalid plane index");
            fixed(float* p = _cullingPlanes)
            {
                return new Plane(new Vector3(p[index * 4 + 0], p[index * 4 + 1], p[index * 4 + 2]), p[index * 4 + 3]);
            }
        }

        public void SetCullingPlane(int index, Plane plane)
        {
            if (index < 0 || index >= cullingPlaneCount || index >= 10)
                throw new IndexOutOfRangeException("Invalid plane index");
            fixed(float* p = _cullingPlanes)
            {
                p[index * 4 + 0] = plane.normal.x;
                p[index * 4 + 1] = plane.normal.y;
                p[index * 4 + 2] = plane.normal.z;
                p[index * 4 + 3] = plane.distance;
            }
        }
    }
}
