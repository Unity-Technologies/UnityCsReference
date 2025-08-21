// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine.Analytics;
using UnityEngine.Serialization;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    internal struct LayoutWindowData
    {
        public string Name;
        public bool Active;
    }
    [Serializable]
    internal struct LayoutChangedData : IAnalytic.IData
    {
        public const string k_LayoutChangedEventName = "multiplayer_playmode_layoutChanged";

        public LayoutWindowData[] LayoutWindows;
        public bool IsPlayMode;
    }

    [AnalyticInfo(eventName: LayoutChangedData.k_LayoutChangedEventName, vendorKey: Constants.k_VendorKey)]
    internal class AnalyticsLayoutChangedEvent : AnalyticsEvent<AnalyticsLayoutChangedEvent, LayoutChangedData>
    {
    }
}
