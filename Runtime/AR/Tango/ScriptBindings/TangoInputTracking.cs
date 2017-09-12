// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace UnityEngine.XR.Tango
{
    [UsedByNativeCode]
    internal static partial class TangoInputTracking
    {
        // Must match enum in Runtime/AR/Tango/TangoTypes.h
        private enum TrackingStateEventType
        {
            TrackingAcquired,
            TrackingLost
        }

        internal static event Action<CoordinateFrame> trackingAcquired = null;
        internal static event Action<CoordinateFrame> trackingLost = null;

        [UsedByNativeCode]
        private static void InvokeTangoTrackingEvent(TrackingStateEventType eventType, CoordinateFrame frame)
        {
            Action<CoordinateFrame> callback = null;

            switch (eventType)
            {
                case TrackingStateEventType.TrackingAcquired:
                    callback = trackingAcquired;
                    break;
                case TrackingStateEventType.TrackingLost:
                    callback = trackingLost;
                    break;
                default:
                    throw new ArgumentException("TrackingEventHandler - Invalid EventType: " + eventType);
            }

            if (callback != null)
            {
                callback(frame);
            }
        }
    }
}
