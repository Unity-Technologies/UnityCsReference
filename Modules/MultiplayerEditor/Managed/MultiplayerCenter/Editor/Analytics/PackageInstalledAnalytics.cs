// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.Multiplayer.Center.Common;
using UnityEngine.Analytics;

namespace Unity.Multiplayer.Center.Editor.Analytics
{
    /// <summary>
    /// The recommendation tier of packages installed from the Multiplayer Center.
    /// </summary>
    internal enum RecommendationTier : ushort
    {
        /// <summary>The Essentials — core packages for the selected genre.</summary>
        Essential = 0,

        /// <summary>The Recommended — optional packages for the selected genre.</summary>
        Recommended = 1,
    }

    /// <summary>
    /// Analytics data sent when packages are installed from the Multiplayer Center.
    /// </summary>
    [Serializable]
    internal struct PackageInstalledData : IAnalytic.IData
    {
        /// <summary>The currently selected game genre when the action was triggered.</summary>
        public string gameGenre;

        /// <summary>The recommendation tier the packages belong to.</summary>
        public string recommendationTier;

        /// <summary>An array of all package names installed as part of this single user action.</summary>
        public string[] packageNames;

        public PackageInstalledData(GameGenre genre, RecommendationTier tier, string[] packageNames)
        {
            this.gameGenre = genre.ToString("G");
            this.recommendationTier = tier.ToString("G");
            this.packageNames = packageNames;
        }
    }

    /// <summary>
    /// Event fired when the user installs all essential packages or recommended package from the Multiplayer Center Window
    /// </summary>
    [AnalyticInfo(eventName: MultiplayerCenterAnalyticsConstants.k_PackageInstalled,
        vendorKey: MultiplayerCenterAnalyticsConstants.k_VendorKey)]
    internal class PackageInstalledEvent : AnalyticsEvent<PackageInstalledEvent, PackageInstalledData>
    {
    }
}
