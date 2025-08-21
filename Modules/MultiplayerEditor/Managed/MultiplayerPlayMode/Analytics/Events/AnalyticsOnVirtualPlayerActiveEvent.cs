// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;
using UnityEngine.Serialization;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    internal struct OnVirtualPlayerActiveData : IAnalytic.IData
    {
        public const string k_OnVirtualPlayerActiveEventName = "multiplayer_playmode_onVirtualPlayerActive";
        public long LaunchingDurationMs;
    }

    [AnalyticInfo(eventName: OnVirtualPlayerActiveData.k_OnVirtualPlayerActiveEventName, vendorKey: Constants.k_VendorKey)]
    internal class AnalyticsOnVirtualPlayerActiveEvent : AnalyticsEvent<AnalyticsOnVirtualPlayerActiveEvent, OnVirtualPlayerActiveData> { }
}
