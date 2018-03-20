// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    public struct BoundingSphere
    {
        public Vector3    position;
        public float      radius;

        public BoundingSphere(Vector3 pos, float rad) { position = pos; radius = rad; }
        public BoundingSphere(Vector4 packedSphere) { position = new Vector3(packedSphere.x, packedSphere.y, packedSphere.z); radius = packedSphere.w; }
    }

    internal enum CullingQueryOptions
    {
        Normal = 0,
        IgnoreVisibility = 1,
        IgnoreDistance = 2
    }

    public struct CullingGroupEvent
    {
        #pragma warning disable 649
        private int m_Index;
        private byte m_PrevState;
        private byte m_ThisState;

        public int index { get { return m_Index; } }

        private const byte kIsVisibleMask = 1 << 7;
        private const byte kDistanceMask = (1 << 7) - 1;

        public bool isVisible             { get { return (m_ThisState & kIsVisibleMask) != 0; } }
        public bool wasVisible            { get { return (m_PrevState & kIsVisibleMask) != 0; } }

        public bool hasBecomeVisible      { get { return isVisible && !wasVisible; } }
        public bool hasBecomeInvisible    { get { return !isVisible && wasVisible; } }

        public int currentDistance        { get { return m_ThisState & kDistanceMask; } }
        public int previousDistance       { get { return m_PrevState & kDistanceMask; } }
    }

    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Runtime/Export/CullingGroup.bindings.h")]
    public class CullingGroup : IDisposable
    {
        internal IntPtr m_Ptr;

        public delegate void StateChanged(CullingGroupEvent sphere);

        public CullingGroup()
        {
            m_Ptr = Init(this);
        }

        ~CullingGroup()
        {
            if (m_Ptr != IntPtr.Zero)
            {
                FinalizerFailure();
            }
        }

        [FreeFunction("CullingGroup_Bindings::Dispose", HasExplicitThis = true)]
        extern private void DisposeInternal();

        public void Dispose()
        {
            DisposeInternal();
            m_Ptr = IntPtr.Zero;
        }

        public StateChanged onStateChanged
        {
            get { return m_OnStateChanged; }
            set { m_OnStateChanged = value; }
        }

        extern public bool enabled { get; set; }
        extern public Camera targetCamera { get; set; }

        extern public void SetBoundingSpheres(BoundingSphere[] array);
        extern public void SetBoundingSphereCount(int count);
        extern public void EraseSwapBack(int index);


        public static void EraseSwapBack<T>(int index, T[] myArray, ref int size)
        {
            size--;
            myArray[index] = myArray[size];
        }

        public int QueryIndices(bool visible, int[] result, int firstIndex)
        {
            return QueryIndices(visible, -1, CullingQueryOptions.IgnoreDistance, result, firstIndex);
        }

        public int QueryIndices(int distanceIndex, int[] result, int firstIndex)
        {
            return QueryIndices(false, distanceIndex, CullingQueryOptions.IgnoreVisibility, result, firstIndex);
        }

        public int QueryIndices(bool visible, int distanceIndex, int[] result, int firstIndex)
        {
            return QueryIndices(visible, distanceIndex, CullingQueryOptions.Normal, result, firstIndex);
        }

        [NativeThrows]
        [FreeFunction("CullingGroup_Bindings::QueryIndices", HasExplicitThis = true)]
        extern private int QueryIndices(bool visible, int distanceIndex, CullingQueryOptions options, int[] result, int firstIndex);

        [NativeThrows]
        [FreeFunction("CullingGroup_Bindings::IsVisible", HasExplicitThis = true)]
        extern public bool IsVisible(int index);

        [NativeThrows]
        [FreeFunction("CullingGroup_Bindings::GetDistance", HasExplicitThis = true)]
        extern public int GetDistance(int index);

        [FreeFunction("CullingGroup_Bindings::SetBoundingDistances", HasExplicitThis = true)]
        extern public void SetBoundingDistances(float[] distances);

        [FreeFunction("CullingGroup_Bindings::SetDistanceReferencePoint", HasExplicitThis = true)]
        extern private void SetDistanceReferencePoint_InternalVector3(Vector3 point);

        [NativeMethod("SetDistanceReferenceTransform")]
        extern private void SetDistanceReferencePoint_InternalTransform(Transform transform);

        public void SetDistanceReferencePoint(Vector3 point)
        {
            SetDistanceReferencePoint_InternalVector3(point);
        }

        public void SetDistanceReferencePoint(Transform transform)
        {
            SetDistanceReferencePoint_InternalTransform(transform);
        }

        // private

        private StateChanged m_OnStateChanged = null;

        [System.Security.SecuritySafeCritical]
        [RequiredByNativeCode]
        unsafe private static void SendEvents(CullingGroup cullingGroup, IntPtr eventsPtr, int count)
        {
            CullingGroupEvent* events = (CullingGroupEvent*)eventsPtr.ToPointer();
            if (cullingGroup.m_OnStateChanged == null)
                return;

            for (int i = 0; i < count; ++i)
                cullingGroup.m_OnStateChanged(events[i]);
        }

        [FreeFunction("CullingGroup_Bindings::Init")]
        extern private static IntPtr Init(object scripting);

        [FreeFunction("CullingGroup_Bindings::FinalizerFailure", HasExplicitThis = true, IsThreadSafe = true)]
        extern private void FinalizerFailure();
    }
}
