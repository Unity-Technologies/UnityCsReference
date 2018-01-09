// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace UnityEngine.Experimental.XR
{
    [NativeHeader("Modules/XR/Subsystems/Environment/XREnvironment.h")]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct TrackableId
    {
        public override string ToString()
        {
            return string.Format("{0}-{1}",
                m_SubId1.ToString("X16"),
                m_SubId2.ToString("X16"));
        }

        public override int GetHashCode()
        {
            return m_SubId1.GetHashCode() ^ m_SubId2.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is TrackableId)
            {
                var id = (TrackableId)obj;
                return
                    (m_SubId1 == id.m_SubId1) &&
                    (m_SubId2 == id.m_SubId2);
            }

            return false;
        }

        public static bool operator==(TrackableId id1, TrackableId id2)
        {
            return
                (id1.m_SubId1 == id2.m_SubId1) &&
                (id1.m_SubId2 == id2.m_SubId2);
        }

        public static bool operator!=(TrackableId id1, TrackableId id2)
        {
            return
                (id1.m_SubId1 != id2.m_SubId1) ||
                (id1.m_SubId2 != id2.m_SubId2);
        }

        private static TrackableId s_InvalidId = new TrackableId();
        public static TrackableId InvalidId { get { return s_InvalidId; } }

        private ulong m_SubId1;
        private ulong m_SubId2;
    }

    internal static class DotNetHelper
    {
        public static bool TryCopyFixedArrayToList<T>(T[] fixedArrayIn, List<T> listOut)
        {
            if (fixedArrayIn == null)
                return false;

            var count = fixedArrayIn.Length;

            listOut.Clear();

            if (listOut.Capacity < count)
                listOut.Capacity = count;

            listOut.AddRange(fixedArrayIn);
            return true;
        }
    }

    [NativeHeader("Modules/XR/Subsystems/Environment/XREnvironment.h")]
    [UsedByNativeCode("xrEnvironment")]
    public class XREnvironment : XRInstance<XREnvironmentSubsystemDescriptor>
    {
        // internal since XREnvironment may only be created by the XRSubsystemDescriptor
        internal XREnvironment() : base() {}

        public XRDepth Depth
        {
            get
            {
                return m_DepthInterface;
            }
        }

        public XRPlane Planes
        {
            get
            {
                return m_PlanesInterface;
            }
        }

        public XRRaycast Raycast
        {
            get
            {
                return m_RaycastInterface;
            }
        }

        public XRReferencePoint ReferencePoints
        {
            get
            {
                return m_ReferencePointsInterface;
            }
        }

        public XRSession Session
        {
            get
            {
                return m_SessionInterface;
            }
        }

        private XRDepth m_DepthInterface;
        private XRPlane m_PlanesInterface;
        private XRRaycast m_RaycastInterface;
        private XRReferencePoint m_ReferencePointsInterface;
        private XRSession m_SessionInterface;

        override internal void Initialize()
        {
            m_DepthInterface = new XRDepth(m_Ptr, this);
            m_PlanesInterface = new XRPlane(m_Ptr, this);
            m_RaycastInterface = new XRRaycast(m_Ptr, this);
            m_ReferencePointsInterface = new XRReferencePoint(m_Ptr, this);
            m_SessionInterface = new XRSession(m_Ptr, this);
        }
    }
}
