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
            return BuildProfileContext.activeProfile;
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
            BuildProfileContext.activeProfile = buildProfile;

            if (buildProfile == null)
                return;

            BuildProfileModuleUtil.SwitchLegacyActiveFromBuildProfile(buildProfile);
        }

        /// <summary>
        /// Gets a component of type T associated with the build profile, its global fallback,
        /// or null if the component is not available.
        /// </summary>
        public T GetComponent<T>() where T : class
        {
            if (typeof(T) == typeof(PlayerSettings))
            {
                if (m_PlayerSettings != null)
                    return m_PlayerSettings as T;
                return s_GlobalPlayerSettings as T;
            }

            return null;
        }

        /// <summary>
        /// Gets a component of type T associated with the currently active build profile,
        /// its global fallback, or null if the component is not available.
        /// </summary>
        public static T GetActiveComponent<T>() where T : class
        {
            var buildProfile = GetActiveBuildProfile();
            if (buildProfile == null)
            {
                if (typeof(T) == typeof(PlayerSettings))
                    return s_GlobalPlayerSettings as T;
            }
            else
            {
                return buildProfile.GetComponent<T>();
            }

            return null;
        }
    }
}
