// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;
using UnityEngine.Serialization;

namespace Unity.Multiplayer.PlayMode.Editor
{

    [Serializable]
    internal struct EnterPlayModeFromMppmData : IAnalytic.IData
    {
        public const string k_EnterPlayModeFromMppmEventName = "multiplayer_playmode_enterPlayModeFromMppm";

        public int VirtualPlayerCount;
        public int CloneWindowErrorCount;
    }

    [AnalyticInfo(eventName: EnterPlayModeFromMppmData.k_EnterPlayModeFromMppmEventName, vendorKey: Constants.k_VendorKey)]
    internal class AnalyticsEnterPlayModeFromMppmEvent : AnalyticsEvent<AnalyticsEnterPlayModeFromMppmEvent, EnterPlayModeFromMppmData>
    {
    }
}
