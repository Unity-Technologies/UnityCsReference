// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Modules;
using UnityEngine;
using PlatformPackageList = UnityEditor.BuildTargetDiscovery.PlatformPackageList;

namespace UnityEditor.Build.Profile.Elements
{
    internal struct BuildProfileCard
    {
        /// <summary>
        /// Display name of the card.
        /// </summary>
        internal string displayName { get; set; }

        /// <summary>
        /// Platform ID of the target build profile.
        /// </summary>
        public GUID platformId { get; set; }

        /// <summary>
        /// Description of a platform.
        /// </summary>
        public string description { get; set; }

        /// <summary>
        /// Key features of a platform.
        /// </summary>
        public string keyFeatures { get; set; }

        /// <summary>
        /// Resources and links related to a platform.
        /// </summary>
        public string resources { get; set; }

        /// <summary>
        /// Hex string for the background color of the platform banner.
        /// </summary>
        public string platformBannerBgColorHex { get; set; }

        /// <summary>
        /// List of Unity-maintained required and recommended packages for a platform.
        /// </summary>
        public PlatformPackageList internalPackages { get; set; }

        /// <summary>
        /// List of Partner-maintained required and recommended packages for a platform.
        /// </summary>
        public PlatformPackageList partnerPackages { get; set; }

        /// <summary>
        /// Preconfigured settings variants for a platform.
        /// </summary>
        public PreconfiguredSettingsVariant[] preconfiguredSettingsVariants { get; set; }

        public BuildProfileCard()
        {
            displayName = string.Empty;
            platformId = new GUID(string.Empty);
            internalPackages = new PlatformPackageList();
            partnerPackages = new PlatformPackageList();
            preconfiguredSettingsVariants = Array.Empty<PreconfiguredSettingsVariant>();
            description = string.Empty;
            keyFeatures = string.Empty;
            resources = string.Empty;
            platformBannerBgColorHex = "#00000000";
        }
    }
}
