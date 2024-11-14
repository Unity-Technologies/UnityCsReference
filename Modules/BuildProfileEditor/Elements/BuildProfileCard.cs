// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Modules;

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

        public string description { get; set; }

        public string[] recommendedPackages { get; set; }

        public string[] requiredPackages { get; set; }

        public PreconfiguredSettingsVariant[] preconfiguredSettingsVariants { get; set; }

        public BuildProfileCard()
        {
            displayName = string.Empty;
            platformId = new GUID(string.Empty);
            recommendedPackages = Array.Empty<string>();
            requiredPackages = Array.Empty<string>();
            preconfiguredSettingsVariants = Array.Empty<PreconfiguredSettingsVariant>();
            description = string.Empty;
        }
    }
}
