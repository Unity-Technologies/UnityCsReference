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
        /// Module name for target profile creation.
        /// </summary>
        public string moduleName { get; set; }

        /// <summary>
        /// StandaloneBuildSubtarget for the target build profile.
        /// </summary>
        public StandaloneBuildSubtarget subtarget { get; set; }

        public BuildProfileCard()
        {
            displayName = string.Empty;
            moduleName = string.Empty;
            subtarget = StandaloneBuildSubtarget.Default;
        }
    }
}
