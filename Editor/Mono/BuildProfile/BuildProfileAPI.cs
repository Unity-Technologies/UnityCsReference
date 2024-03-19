// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Build.Profile
{
    public sealed partial class BuildProfile
    {
        /// <summary>
        /// Gets the active build profile.
        /// </summary>
        /// <returns>
        /// The active build profile. Returns null when a classic platform is active.
        /// </returns>
        public static BuildProfile GetActiveBuildProfile()
        {
            return BuildProfileContext.instance.activeProfile;
        }

        /// <summary>
        /// Sets the active build profile.
        /// </summary>
        /// <param name="buildProfile">
        /// The build profile to be set as the active build profile.
        /// When the value is null, Unity sets the classic platform as active.
        /// </param>
        public static void SetActiveBuildProfile(BuildProfile buildProfile)
        {
            BuildProfileContext.instance.activeProfile = buildProfile;
        }
    }
}
