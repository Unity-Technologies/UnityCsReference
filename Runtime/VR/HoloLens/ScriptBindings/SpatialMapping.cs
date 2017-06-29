// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.XR.WSA;


namespace UnityEngine.XR.WSA
{
    // This is mirrored in native code.
    [MovedFrom("UnityEngine.VR.WSA")]
    public enum SurfaceChange
    {
        Added = 0,
        Updated = 1,
        Removed = 2
    }

    //
    // A container for the surface ID handle.
    //
    [MovedFrom("UnityEngine.VR.WSA")]
    public struct SurfaceId
    {
        public int handle;
    }

    //
    // A container for submitting surface mesh bake requests and for receiving
    // baked data back from the system.
    //
    [MovedFrom("UnityEngine.VR.WSA")]
    public struct SurfaceData
    {
        public SurfaceData(SurfaceId _id, MeshFilter _outputMesh, WorldAnchor _outputAnchor, MeshCollider _outputCollider, float _trianglesPerCubicMeter, bool _bakeCollider)
        {
            id = _id;
            outputMesh = _outputMesh;
            outputAnchor = _outputAnchor;
            outputCollider = _outputCollider;
            trianglesPerCubicMeter = _trianglesPerCubicMeter;
            bakeCollider = _bakeCollider;
        }

        public SurfaceId id;
        public MeshFilter outputMesh;
        public WorldAnchor outputAnchor;
        public MeshCollider outputCollider;
        public float trianglesPerCubicMeter;
        public bool bakeCollider;
    };

    //
    // A single observer updating surfaces within a single user specified
    // volume.  This volume can be changed at runtime.
    //
    [MovedFrom("UnityEngine.VR.WSA")]
    sealed public partial class SurfaceObserver : IDisposable
    {
        public delegate void SurfaceChangedDelegate(SurfaceId surfaceId, SurfaceChange changeType, Bounds bounds, DateTime updateTime);
        public delegate void SurfaceDataReadyDelegate(SurfaceData bakedData, bool outputWritten, float elapsedBakeTimeSeconds);

        [RequiredByNativeCode]
        private static void InvokeSurfaceChangedEvent(SurfaceChangedDelegate onSurfaceChanged, int surfaceId, SurfaceChange changeType, Bounds bounds, long updateTime)
        {
            if (onSurfaceChanged != null)
            {
                SurfaceId id;
                id.handle = surfaceId;
                onSurfaceChanged(id, changeType, bounds, DateTime.FromFileTime(updateTime));
            }
        }

        [RequiredByNativeCode]
        private static void InvokeSurfaceDataReadyEvent(SurfaceDataReadyDelegate onDataReady, int surfaceId, MeshFilter outputMesh, WorldAnchor outputAnchor, MeshCollider outputCollider, float trisPerCubicMeter, bool bakeCollider, bool outputWritten, float elapsedBakeTimeSeconds)
        {
            if (onDataReady != null)
            {
                SurfaceData data;
                data.id.handle = surfaceId;
                data.outputMesh = outputMesh;
                data.outputAnchor = outputAnchor;
                data.outputCollider = outputCollider;
                data.trianglesPerCubicMeter = trisPerCubicMeter;
                data.bakeCollider = bakeCollider;
                onDataReady(data, outputWritten, elapsedBakeTimeSeconds);
            }
        }

        public SurfaceObserver()
        {
            m_Observer = Internal_Create();
        }

        ~SurfaceObserver()
        {
            if (m_Observer != IntPtr.Zero)
            {
                DestroyThreaded(m_Observer);
                m_Observer = IntPtr.Zero;
                GC.SuppressFinalize(this);
            }
        }

        public void SetVolumeAsAxisAlignedBox(Vector3 origin, Vector3 extents)
        {
            Internal_SetVolumeAsAxisAlignedBox(m_Observer, origin, extents);
        }

        public void SetVolumeAsSphere(Vector3 origin, float radiusMeters)
        {
            Internal_SetVolumeAsSphere(m_Observer, origin, radiusMeters);
        }

        public void SetVolumeAsOrientedBox(Vector3 origin, Vector3 extents, Quaternion orientation)
        {
            Internal_SetVolumeAsOrientedBox(m_Observer, origin, extents, orientation);
        }

        public void SetVolumeAsFrustum(Plane[] planes)
        {
            if (planes == null)
                throw new ArgumentNullException("planes");
            if (planes.Length != 6)
                throw new ArgumentException("Planes array must be 6 items long", "planes");

            Internal_SetVolumeAsFrustum(m_Observer, planes);
        }

        public void Update(SurfaceChangedDelegate onSurfaceChanged)
        {
            if (onSurfaceChanged == null)
            {
                throw new ArgumentNullException("onSurfaceChanged");
            }
            Internal_Update(m_Observer, onSurfaceChanged);
        }

        public bool RequestMeshAsync(SurfaceData dataRequest, SurfaceDataReadyDelegate onDataReady)
        {
            if (onDataReady == null)
            {
                throw new ArgumentNullException("onDataReady");
            }
            if (dataRequest.outputMesh == null)
            {
                throw new ArgumentNullException("dataRequest.outputMesh");
            }
            if (dataRequest.outputAnchor == null)
            {
                throw new ArgumentNullException("dataRequest.outputAnchor");
            }
            if (dataRequest.outputCollider == null && dataRequest.bakeCollider)
            {
                throw new ArgumentException("dataRequest.outputCollider must be non-NULL if dataRequest.bakeCollider is true", "dataRequest.outputCollider");
            }
            if (dataRequest.trianglesPerCubicMeter < 0.0)
            {
                throw new ArgumentException("dataRequest.trianglesPerCubicMeter must be greater than zero", "dataRequest.trianglesPerCubicMeter");
            }
            bool ret = Internal_AddToWorkQueue(
                    m_Observer,
                    onDataReady,
                    dataRequest.id.handle,
                    dataRequest.outputMesh,
                    dataRequest.outputAnchor,
                    dataRequest.outputCollider,
                    dataRequest.trianglesPerCubicMeter,
                    dataRequest.bakeCollider);
            if (!ret)
            {
                // The only real failure is if the ID is unknown.
                Debug.LogError("RequestMeshAsync has failed.  Is your surface ID valid?");
            }
            return ret;
        }
    }
}


