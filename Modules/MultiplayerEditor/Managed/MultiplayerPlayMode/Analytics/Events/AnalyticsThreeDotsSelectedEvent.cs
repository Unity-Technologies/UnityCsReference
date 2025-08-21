// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine.Analytics;
using UnityEngine.Serialization;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    internal struct ThreeDotsSelectedData : IAnalytic.IData
    {
        public const string k_ThreeDotsSelectedEventName = "multiplayer_playmode_threeDotsSelected";

        public bool IsPlayMode;
        public string OptionSelected;
    }

    [AnalyticInfo(eventName: ThreeDotsSelectedData.k_ThreeDotsSelectedEventName, vendorKey: Constants.k_VendorKey)]
    internal class AnalyticsThreeDotsSelectedEvent : AnalyticsEvent<AnalyticsThreeDotsSelectedEvent, ThreeDotsSelectedData> { }
}
