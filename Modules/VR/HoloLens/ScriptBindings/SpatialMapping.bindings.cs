// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;


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
    [UsedByNativeCode]
    [StaticAccessor("Unity::SurfaceObserver", StaticAccessorType.DoubleColon)]
    [NativeHeader("Modules/VR/HoloLens/SpatialMapping/SurfaceObserver.h")]
    [NativeHeader("VRScriptingClasses.h")]
    [StructLayout(LayoutKind.Sequential)]   // needed for IntPtr binding classes
    sealed public class SurfaceObserver : IDisposable
    {
        const int k_InvalidObserverId = 0;
        internal int m_Observer;  // Native object id

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
            m_Observer = Internal_Create(this);
        }

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("Create")]
        private static extern int Internal_Create(System.Object surfaceObserver);

        ~SurfaceObserver()
        {
            if (m_Observer != k_InvalidObserverId)
            {
                DestroyThreaded();
                m_Observer = k_InvalidObserverId;
                GC.SuppressFinalize(this);
            }
        }

        public void Dispose()
        {
            if (m_Observer != k_InvalidObserverId)
            {
                Destroy();
                m_Observer = k_InvalidObserverId;
            }
            GC.SuppressFinalize(this);
        }

        private void Destroy()
        {
            Internal_Destroy(m_Observer);
        }

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        private static extern void Internal_Destroy(int id);

        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.1. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public void SetVolumeAsAxisAlignedBox(Vector3 origin, Vector3 extents)
        {
            if (m_Observer != k_InvalidObserverId) Internal_SetVolumeAsAxisAlignedBox(m_Observer, origin, extents);
        }

        private void DestroyThreaded()
        {
            Internal_DestroyThreaded(m_Observer);
        }

        [ThreadAndSerializationSafe]
        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        private static extern void Internal_DestroyThreaded(int id);


        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        private static extern void Internal_SetVolumeAsAxisAlignedBox(int id, Vector3 origin, Vector3 extents);

        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.1. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public void SetVolumeAsSphere(Vector3 origin, float radiusMeters)
        {
            if (m_Observer != k_InvalidObserverId) Internal_SetVolumeAsSphere(m_Observer, origin, radiusMeters);
        }

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        private static extern void Internal_SetVolumeAsSphere(int id, Vector3 origin, float radiusMeters);

        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.1. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public void SetVolumeAsOrientedBox(Vector3 origin, Vector3 extents, Quaternion orientation)
        {
            if (m_Observer != k_InvalidObserverId) Internal_SetVolumeAsOrientedBox(m_Observer, origin, extents, orientation);
        }

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        private static extern void Internal_SetVolumeAsOrientedBox(int id, Vector3 origin, Vector3 extents, Quaternion orientation);

        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.1. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public void SetVolumeAsFrustum(Plane[] planes)
        {
            if (planes == null)
                throw new ArgumentNullException("planes");

            if (planes.Length != 6)
                throw new ArgumentException("Planes array must be 6 items long", "planes");

            if (m_Observer != k_InvalidObserverId) Internal_SetVolumeAsFrustum(m_Observer, planes);
        }

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        private static extern void Internal_SetVolumeAsFrustum(int id, [NotNull] Plane[] planes);

        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.1. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
        public void Update(SurfaceChangedDelegate onSurfaceChanged)
        {
            if (m_Observer != k_InvalidObserverId) Internal_Update(m_Observer, onSurfaceChanged);
        }

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        private static extern void Internal_Update(int id, [NotNull] SurfaceChangedDelegate onSurfaceChanged);

        [System.Obsolete("Support for built-in VR will be removed in Unity 2020.1. Please update to the new Unity XR Plugin System. More information about the new XR Plugin System can be found at https://docs.unity3d.com/2019.3/Documentation/Manual/XR.html.", false)]
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

        [NativeConditional("ENABLE_HOLOLENS_MODULE")]
        [NativeName("AddToWorkQueue")]
        private static extern bool Internal_AddToWorkQueue(int observer, SurfaceDataReadyDelegate onDataReady, int surfaceId, MeshFilter filter, WorldAnchor wa, MeshCollider mc, float trisPerCubicMeter, bool createColliderData);
    }
}

