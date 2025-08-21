// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;
using UnityEngine.Serialization;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    internal struct OnThreeDotsClickedData : IAnalytic.IData
    {
        public const string k_OnThreeDotsClickedEventName = "multiplayer_playmode_onThreeDotsClicked";

        public bool IsPlayMode;
    }

    [AnalyticInfo(eventName: OnThreeDotsClickedData.k_OnThreeDotsClickedEventName, vendorKey: Constants.k_VendorKey)]
    internal class AnalyticsOnTreeDotsClickedEvent : AnalyticsEvent<AnalyticsOnTreeDotsClickedEvent, OnThreeDotsClickedData>
    {
    }
}
