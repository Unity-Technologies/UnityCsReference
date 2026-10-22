// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Scripting;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.XR
{
    ///<summary>A collection of methods and properties for accessing XR input devices by their XR Node representation.</summary>
    ///<remarks>XR devices can be accessed in different ways, with the XR Node representing a physical input source such as a head position, hand, or camera.
    ///
    ///See [XR Input](xref:xr_input) for an overview of accessing XR devices.</remarks>
    [RequiredByNativeCode]
    public static partial class InputTracking
    {
        internal enum TrackingStateEventType
        {
            NodeAdded,
            NodeRemoved,
            TrackingAcquired,
            TrackingLost
        }

        ///<summary>Called when a tracked node begins reporting tracking information.</summary>
        ///<remarks>The event argument describes the node that has begun being tracked.</remarks>
        [AutoStaticsCleanupOnCodeReload]
        public static event Action<XRNodeState> trackingAcquired = null;
        ///<summary>Called when a tracked node stops reporting tracking information.</summary>
        ///<remarks>The event argument describes the node that has lost tracking.</remarks>
        [AutoStaticsCleanupOnCodeReload]
        public static event Action<XRNodeState> trackingLost = null;
        ///<summary>Called when a tracked node is added to the underlying XR system.</summary>
        ///<remarks>The event argument describes the node that has been added.</remarks>
        [AutoStaticsCleanupOnCodeReload]
        public static event Action<XRNodeState> nodeAdded = null;
        ///<summary>Called when a tracked node is removed from the underlying XR system.</summary>
        ///<remarks>The event argument describes the node that has been removed.</remarks>
        [AutoStaticsCleanupOnCodeReload]
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
