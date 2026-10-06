// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Analytics;
using UnityEngine.Serialization;

namespace Unity.Multiplayer.Center.Editor.Analytics
{
    /// <summary>
    /// Analytics data sent when the user picks a game genre and clicks the Select button.
    /// </summary>
    [Serializable]
    internal struct GenreSelectedData : IAnalytic.IData
    {
        /// <summary>
        /// The name of the game genre the user selected.
        /// </summary>
        public string gameGenre;
    }

    /// <summary>
    /// Event sent when the user confirms their game genre selection by clicking the Select button.
    /// </summary>
    [AnalyticInfo(eventName: MultiplayerCenterAnalyticsConstants.k_GenreSelected, vendorKey: MultiplayerCenterAnalyticsConstants.k_VendorKey)]
    internal class GenreSelectedEvent : AnalyticsEvent<GenreSelectedEvent, GenreSelectedData>
    {
    }
}
