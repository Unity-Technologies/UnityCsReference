// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Build.Profile
{
    public sealed partial class BuildProfile
    {
        /// <summary>
        /// Callback invoked when the active build profile has been changed to a new value.
        /// </summary>
        /// <see cref="activeProfile"/>
        /// <remarks>
        /// Callback expecting the previous and current active build profile.
        /// <code>
        /// BuildProfile.activeProfileChanged += (BuildProfile previous, BuildProfile current) => {}
        /// </code>
        /// </remarks>
        public static event Action<BuildProfile, BuildProfile> activeProfileChanged
        {
            add => BuildProfileContext.activeProfileChanged += value;
            remove => BuildProfileContext.activeProfileChanged -= value;
        }

        /// <summary>
        /// Retrieves the build profile asset located at the specified path within the Unity project.
        /// </summary>
        /// <param name="path">The path to the build profile asset within the "Assets" folder. Cannot be null or empty.</param>
        /// <returns>The <see cref="BuildProfile"/> asset found at the given path, or <see langword="null"/> if the asset does
        /// not exist or the path is invalid.</returns>
        public static BuildProfile GetBuildProfileAtPath(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                return null;

            BuildProfile profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(path);
            if (profile == null)
            {
                Debug.LogWarning($"[BuildProfile] Failed to load asset at path: {path}");
                return null;
            }
            return profile;
        }

        /// <summary>
        /// Retrieves all build profiles available in the project.
        /// </summary>
        /// <remarks>This method returns build profiles that are stored as assets as well as those created
        /// in memory during the editor session. In memory profiles have not been stored on disk and can be lost
        /// during a domain reload; <see cref="AssetDatabase.GetAssetPath(UnityEngine.Object)"/> would return null.</remarks>
        /// <returns>A list of <see cref="BuildProfile"/> objects representing all build profiles found. The list may be empty if
        /// no profiles are available.</returns>
        public static IReadOnlyList<BuildProfile> GetAllBuildProfiles()
        {
            var alreadyLoadedBuildProfiles = Resources.FindObjectsOfTypeAll<BuildProfile>();

            const string buildProfileAssetSearchString = $"t:{nameof(BuildProfile)}";
            var assetsGuids = AssetDatabase.FindAssets(buildProfileAssetSearchString, new[] { "Assets" });
            var result = new List<BuildProfile>(assetsGuids.Length);

            // Suppress missing type warning thrown by serialization. This could happen
            // when the build profile window is opened, then entering play mode and the
            // module for that profile is not installed.
            BuildProfileModuleUtil.SuppressMissingTypeWarning();

            foreach (var guid in assetsGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                BuildProfile profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(path);
                if (profile == null)
                {
                    Debug.LogWarning($"[BuildProfile] Failed to load asset at path: {path}");
                    continue;
                }

                result.Add(profile);
            }

            foreach (var buildProfile in alreadyLoadedBuildProfiles)
            {
                // Asset database will not return any build profiles that get created in memory
                // and those build profiles need to be included in this list as it's used to detect that build profiles
                // have been destroyed and destroy their resources like PlayerSettings afterwards.
                // Skipping the in-memory build profiles will result in deletion of their associated
                // player settings object while it's being used and will lead to a crash (UUM-77423)
                if (buildProfile &&
                    !BuildProfileContext.IsClassicPlatformProfile(buildProfile) &&
                    !BuildProfileContext.IsSharedProfile(buildProfile.platformGuid) &&
                    !EditorUtility.IsPersistent(buildProfile))
                {
                    result.Add(buildProfile);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets a list of all installed platforms.
        /// </summary>
        /// <returns>
        ///     A list of <see cref="InstalledPlatformInfo"/> objects representing each installed editor platform.
        /// </returns>
        public static IReadOnlyList<InstalledPlatformInfo> GetInstalledPlatformModules()
        {
            var installedPlatforms = new List<InstalledPlatformInfo>();
            var keys = BuildProfileModuleUtil.FindAllViewablePlatforms();
            foreach(var key in keys)
            {
                if (BuildProfileModuleUtil.IsModuleInstalled(key))
                    installedPlatforms.Add(new InstalledPlatformInfo
                    {
                        platformGuid = key,
                        displayName = BuildTargetDiscovery.BuildPlatformDisplayName(key),
                    });
            }

            return installedPlatforms;
        }
    }
}
