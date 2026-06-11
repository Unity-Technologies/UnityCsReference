// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityEditor.Build.Profile.Handlers
{
    internal class BuildProfileDataSource : IDisposable
    {
        internal IList<BuildProfile> classicPlatforms { get; }
        internal IList<BuildProfile> customBuildProfiles { get; }

        BuildProfileWindow m_Window;

        List<BuildProfile> m_DuplicatedProfiles;

        internal BuildProfileDataSource(BuildProfileWindow window)
        {
            this.m_Window = window;
            classicPlatforms = BuildProfileContext.instance.classicPlatformProfiles;
            customBuildProfiles = BuildProfileModuleUtil.FindAllBuildProfiles();
            m_DuplicatedProfiles = new List<BuildProfile>();

            BuildProfile.AddOnBuildProfileEnable(OnBuildProfileCreated);
            BuildProfile.AddOnBuildProfileCreated(m_Window.OnBuildProfileCreated);
            BuildProfileModuleUtil.CleanUpPlayerSettingsForDeletedBuildProfiles(currentBuildProfiles: customBuildProfiles);
        }

        public void Dispose()
        {
            BuildProfile.RemoveOnBuildProfileEnable(OnBuildProfileCreated);
            BuildProfile.RemoveOnBuildProfileCreated(m_Window.OnBuildProfileCreated);
        }

        /// <summary>
        /// Helper function that takes a list of profiles and duplicates them
        /// </summary>
        internal List<BuildProfile> DuplicateProfiles(List<BuildProfile> profilesToDuplicate, bool isClassic)
        {
            m_DuplicatedProfiles.Clear();
            var profilesCount = profilesToDuplicate.Count;
            for (int i = 0; i < profilesCount; ++i)
            {
                var profile = profilesToDuplicate[i];
                var duplicatedProfile = BuildProfileDataSource.DuplicateAsset(profile, isClassic);
                if (duplicatedProfile != null)
                    m_DuplicatedProfiles.Add(duplicatedProfile);
            }

            // When duplicating an asset it will be created with the BaseName(Clone) in its name.
            // At the time the proper name is set, OnEnable will be already called and the build
            // profile will not be added in proper order, so we need to sort in here
            SortCustomBuildProfiles();
            return m_DuplicatedProfiles;
        }

        /// <summary>
        /// Clone build profile and create new build profile asset based on it. The
        /// build profile will be added to the custom build profile list on enable
        /// </summary>
        internal static BuildProfile DuplicateAsset(BuildProfile buildProfile, bool isClassic)
        {
            if (buildProfile == null)
                return null;

            string path = string.Empty;
            if (isClassic)
            {
                path = BuildProfileModuleUtil.GetDefaultNewProfilePath(buildProfile.platformGuid);
            }
            else
            {
                path = AssetDatabase.GetAssetPath(buildProfile);
            }
            if (string.IsNullOrEmpty(path))
                return null;

            BuildProfileModuleUtil.EnsureCustomBuildProfileFolderExists();
            string uniqueFilePath = BuildProfileModuleUtil.GetUniqueBuildProfilePath(path);

            BuildProfile duplicatedProfile;
            if (isClassic)
            {
                // If it's a classic profile we need to copy the scenes from the
                // editor build settings since classic profiles share scenes
                duplicatedProfile = ScriptableObject.Instantiate(buildProfile);
                duplicatedProfile.scenes = EditorBuildSettings.GetEditorBuildSettingsSceneIgnoreProfile();
                AssetDatabase.CreateAsset(duplicatedProfile, uniqueFilePath);
            }
            else
            {
                if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(buildProfile), uniqueFilePath))
                {
                    return null;
                }

                duplicatedProfile = AssetDatabase.LoadAssetAtPath<BuildProfile>(uniqueFilePath);
            }

            EditorAnalytics.SendAnalytic(new BuildProfileCreatedEvent(new BuildProfileCreatedEvent.Payload
            {
                creationType = (isClassic)
                    ? BuildProfileCreatedEvent.CreationType.DuplicateClassic
                    : BuildProfileCreatedEvent.CreationType.DuplicateProfile,
                platformId = duplicatedProfile.platformGuid.ToString(),
                platformDisplayName = BuildProfileModuleUtil.GetClassicPlatformDisplayName(duplicatedProfile.platformGuid),
            }));

            return duplicatedProfile;
        }

        /// <summary>
        /// Delete build profile asset and remove from the list of
        /// custom build profiles
        /// </summary>
        internal void DeleteAsset(BuildProfile buildProfile)
        {
            if (buildProfile == null || !customBuildProfiles.Contains(buildProfile))
                return;

            customBuildProfiles.Remove(buildProfile);

            string assetPath = AssetDatabase.GetAssetPath(buildProfile);
            if (!string.IsNullOrEmpty(assetPath))
            {
                BuildProfileModuleUtil.DeleteLastRunnableBuildKeyForProfile(buildProfile);

                // We call DestroyImmediate so the build profile's OnDisable gets called
                UnityEngine.Object.DestroyImmediate(buildProfile, allowDestroyingAssets: true);
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        internal bool DeleteNullProfiles()
        {
            bool removedProfile = false;
            bool activeProfileIsNull = false;
            for (int i = customBuildProfiles.Count - 1; i >= 0; i--)
            {
                if (customBuildProfiles[i] != null)
                {
                    continue;
                }

                if (BuildProfileContext.activeProfile == customBuildProfiles[i])
                        activeProfileIsNull = true;
                customBuildProfiles.RemoveAt(i);
                removedProfile = true;
            }

            if (activeProfileIsNull)
                BuildProfileContext.activeProfile = null;

            if (removedProfile)
            {
                BuildProfileModuleUtil.CleanUpPlayerSettingsForDeletedBuildProfiles(currentBuildProfiles: customBuildProfiles);
                BuildProfileModuleUtil.DeleteLastRunnableBuildKeyForDeletedProfiles();
            }

            return removedProfile;
        }

        /// <summary>
        /// Rename build profile asset and remove build profile from custom
        /// build profile list. The build profile will be re-added when it
        /// gets enabled after renaming.
        /// </summary>
        internal bool RenameAsset(BuildProfile buildProfile, string newName)
        {
            if (buildProfile?.name == newName || string.IsNullOrEmpty(newName))
                return false;

            var originalPath = AssetDatabase.GetAssetPath(buildProfile);
            var newPath = ReplaceFileNameInPath(originalPath, newName);
            var onlyCaseChange = string.Equals(buildProfile?.name, newName, StringComparison.OrdinalIgnoreCase);
            var uniqueAssetPath = onlyCaseChange ? newPath : BuildProfileModuleUtil.GetUniqueBuildProfilePath(newPath);
            var finalName = Path.GetFileNameWithoutExtension(uniqueAssetPath);

            if (!string.IsNullOrEmpty(originalPath))
            {
                // Remove and rebuild list views before renaming to avoid
                // list view 'SerializedObject of SerializedProperty has been
                // Disposed' error
                customBuildProfiles.Remove(buildProfile);
                m_Window.RebuildProfileListViews();
                AssetDatabase.RenameAsset(originalPath, finalName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sort custom build profiles by name. Called by the Build Project Window after
        /// all selected build profiles gets duplicated
        /// </summary>
        void SortCustomBuildProfiles()
        {
            List<BuildProfile> sortedProfiles = new List<BuildProfile>(customBuildProfiles);
            sortedProfiles.Sort((lhs, rhs) => EditorUtility.NaturalCompare(lhs.name, rhs.name));
            for (int i = 0; i < sortedProfiles.Count; i++)
            {
                customBuildProfiles[i] = sortedProfiles[i];
            }
        }

        /// <summary>
        /// This is called by <see cref="BuildProfile.OnEnable"/>
        /// (creation originated by user or by code)
        /// </summary>
        void OnBuildProfileCreated(BuildProfile profile)
        {
            // Only track profiles stored in the assets folder.
            if (profile.platformGuid.Empty() || string.IsNullOrEmpty(profile.name))
                return;

            if (BuildProfileContext.IsClassicPlatformProfile(profile))
                return;

            bool wasChanged = AddNewToCustomProfilesInOrder(profile);
            if (wasChanged)
            {
                m_Window.RebuildProfileListViews();
            }
        }

        /// <summary>
        /// Adds a new build profile to the tracked sorted list of custom profiles.
        /// </summary>
        /// <returns>true, if profile was successfully appended. </returns>
        bool AddNewToCustomProfilesInOrder(BuildProfile profile)
        {
            int index = 0;
            foreach (var customBuildProfile in customBuildProfiles)
            {
                if (customBuildProfile == null)
                {
                    // Consider case where a custom profile was deleted outside the editor.
                    // Cleanup list entry and try again.
                    customBuildProfiles.RemoveAt(index);
                    return AddNewToCustomProfilesInOrder(profile);
                }

                if (customBuildProfile == profile)
                    return false;

                if (EditorUtility.NaturalCompare(customBuildProfile.name, profile.name) > 0)
                {
                    customBuildProfiles.Insert(index, profile);
                    return true;
                }

                index++;
            }

            customBuildProfiles.Add(profile);
            return true;
        }

        static string ReplaceFileNameInPath(string originalPath, string newName)
        {
            string directory = Path.GetDirectoryName(originalPath);
            string extension = Path.GetExtension(originalPath);
            return Path.Combine(directory, $"{newName}{extension}");
        }

        internal static string SanitizeFileName(string name)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            if (name.IndexOfAny(invalidChars) == -1)
                return name;

            foreach (char c in invalidChars)
                name = name.Replace(c.ToString(), string.Empty);
            return name;
        }
    }
}
