// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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
        public string platformId { get; set; }

        public BuildProfileCard()
        {
            displayName = string.Empty;
            platformId = new GUID(string.Empty).ToString();
        }
    }
}
