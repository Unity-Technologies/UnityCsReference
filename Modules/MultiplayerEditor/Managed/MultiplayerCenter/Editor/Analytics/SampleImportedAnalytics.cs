// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Multiplayer.Center.Common;
using UnityEngine.Analytics;

namespace Unity.Multiplayer.Center.Editor.Analytics
{
    /// <summary>
    /// Analytics data sent when the user imports a basic getting-started sample.
    /// </summary>
    [Serializable]
    internal struct SampleImportedData : IAnalytic.IData
    {
        /// <summary>The currently selected game genre when the sample was imported.</summary>
        public string gameGenre;

        /// <summary>The package sample ID that was imported.</summary>
        public string sampleId;

        /// <summary>True if the sample was already imported and the user clicked Re-Import.</summary>
        public bool isReimport;

        public SampleImportedData(GameGenre genre, string sampleId, bool isReimport)
        {
            this.gameGenre = genre.ToString("G");
            this.sampleId = sampleId;
            this.isReimport = isReimport;
        }
    }

    /// <summary>
    /// Event fired when the user clicks Import (or Re-Import) for a basic getting-started sample.
    /// </summary>
    [AnalyticInfo(eventName: MultiplayerCenterAnalyticsConstants.k_SampleImported,
        vendorKey: MultiplayerCenterAnalyticsConstants.k_VendorKey)]
    internal class SampleImportedEvent : AnalyticsEvent<SampleImportedEvent, SampleImportedData>
    {
    }
}
