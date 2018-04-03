// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.XR
{
    [RequiredByNativeCode]
    public static partial class InputTracking
    {
        private enum TrackingStateEventType
        {
            NodeAdded,
            NodeRemoved,
            TrackingAcquired,
            TrackingLost
        }

        public static event Action<XRNodeState> trackingAcquired = null;
        public static event Action<XRNodeState> trackingLost = null;
        public static event Action<XRNodeState> nodeAdded = null;
        public static event Action<XRNodeState> nodeRemoved = null;

        [RequiredByNativeCode]
        private static void InvokeTrackingEvent(TrackingStateEventType eventType, XRNode nodeType, long uniqueID, bool tracked)
        {
            Action<XRNodeState> callback = null;
            XRNodeState callbackParam = new XRNodeState();

            callbackParam.uniqueID = (ulong)uniqueID;
            callbackParam.nodeType = nodeType;
            callbackParam.tracked = tracked;

            switch (eventType)
            {
                case TrackingStateEventType.TrackingAcquired:
                    callback = trackingAcquired;
                    break;
                case TrackingStateEventType.TrackingLost:
                    callback = trackingLost;
                    break;
                case TrackingStateEventType.NodeAdded:
                    callback = nodeAdded;
                    break;
                case TrackingStateEventType.NodeRemoved:
                    callback = nodeRemoved;
                    break;
                default:
                    throw new ArgumentException("TrackingEventHandler - Invalid EventType: " + eventType);
            }

            if (null != callback)
            {
                callback(callbackParam);
            }
        }
    }
}
