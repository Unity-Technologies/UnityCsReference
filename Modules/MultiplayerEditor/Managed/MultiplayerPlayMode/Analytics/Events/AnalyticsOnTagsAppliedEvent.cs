// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;
using UnityEngine.Serialization;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    internal struct OnTagsAppliedData : IAnalytic.IData
    {
        public const string k_OnTagsAppliedEventName = "multiplayer_playmode_onTagsApplied";

        public string PlayerName;
        public int TagsCount;
        public string[] TagNames;
        public bool IsFromScenario;

    }
    [AnalyticInfo(eventName: OnTagsAppliedData.k_OnTagsAppliedEventName, vendorKey: Constants.k_VendorKey)]
    internal class AnalyticsOnTagsAppliedEvent : AnalyticsEvent<AnalyticsOnTagsAppliedEvent, OnTagsAppliedData>
    {
    }
}
