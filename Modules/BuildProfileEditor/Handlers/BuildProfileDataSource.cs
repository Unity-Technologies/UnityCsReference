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

        const string k_AssetFolderPath = "Assets/Settings/Build Profiles";

        static string GetNewProfileName(string displayName) => $"{k_AssetFolderPath}/{displayName}.asset";

        internal BuildProfileDataSource(BuildProfileWindow window)
        {
            this.m_Window = window;
            classicPlatforms = BuildProfileContext.instance.classicPlatformProfiles;
            customBuildProfiles = FindAllBuildProfiles();
            m_DuplicatedProfiles = new List<BuildProfile>();

            BuildProfile.AddOnBuildProfileEnable(OnBuildProfileCreated);
        }

        public void Dispose()
        {
            BuildProfile.RemoveOnBuildProfileEnable(OnBuildProfileCreated);
        }

        /// <summary>
        /// Removes all null unity objects from the custom profiles list.
        /// </summary>
        internal bool ClearDeletedProfiles()
        {
            bool changed = false;
            for (int i = customBuildProfiles.Count - 1; i >= 0; --i)
            {
                var obj = customBuildProfiles[i];
                if (obj != null)
                    continue;

                if (BuildProfileContext.instance.activeProfile == obj)
                    BuildProfileContext.instance.activeProfile = null;

                customBuildProfiles.RemoveAt(i);
                changed = true;
            }

            return changed;
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
        /// Create a custom build profile asset, making sure to create the folders
        /// if needed
        /// </summary>
        internal static void CreateAsset(string moduleName, StandaloneBuildSubtarget subtarget, string displayName)
        {
            CheckCreateCustomBuildProfileFolders();

            BuildProfile.CreateInstance(moduleName, subtarget, GetNewProfileName(displayName));
        }

        /// <summary>
        /// Clone build profile and create new build profile asset based on it. The
        /// build profile will be added to the custom build profile list on enable
        /// </summary>
        internal static BuildProfile DuplicateAsset(BuildProfile buildProfile, bool isClassic)
        {
            if (buildProfile == null)
                return null;

            string path = isClassic ? GetDuplicatedBuilProfilePathForClassic(buildProfile) : AssetDatabase.GetAssetPath(buildProfile);
            if (string.IsNullOrEmpty(path))
                return null;

            BuildProfile duplicatedProfile = UnityEngine.Object.Instantiate(buildProfile);

            // If it's a classic profile we need to copy the scenes from the editor build settings
            // since classic profiles share scenes
            if (isClassic)
                duplicatedProfile.scenes = EditorBuildSettings.GetEditorBuildSettingsSceneIgnoreProfile();

            CheckCreateCustomBuildProfileFolders();

            string uniqueFilePath = AssetDatabase.GenerateUniqueAssetPath(path);
            AssetDatabase.CreateAsset(duplicatedProfile, uniqueFilePath);
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
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        internal void DeleteNullProfiles()
        {
            for (int i = customBuildProfiles.Count - 1; i >= 0; i--)
            {
                if (customBuildProfiles[i] == null)
                    customBuildProfiles.RemoveAt(i);
            }
        }

        /// <summary>
        /// Rename build profile asset and remove build profile from custom
        /// build profile list. The build profile will be re-added when it
        /// gets enabled after renaming.
        /// </summary>
        internal void RenameAsset(BuildProfile buildProfile, string newName)
        {
            if (buildProfile?.name == newName || string.IsNullOrEmpty(newName))
                return;

            var originalPath = AssetDatabase.GetAssetPath(buildProfile);
            var newPath = ReplaceFileNameInPath(originalPath, newName);
            var uniqueAssetPath = AssetDatabase.GenerateUniqueAssetPath(newPath);
            var finalName = Path.GetFileNameWithoutExtension(uniqueAssetPath);

            if (!string.IsNullOrEmpty(originalPath))
            {
                // Remove and rebuild list views before renaming to avoid
                // list view 'SerializedObject of SerializedProperty has been
                // Disposed' error
                customBuildProfiles.Remove(buildProfile);
                m_Window.RebuildProfileListViews();
                AssetDatabase.RenameAsset(originalPath, finalName);
            }
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
            if (profile.buildTarget == BuildTarget.NoTarget || string.IsNullOrEmpty(profile.name))
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

        static List<BuildProfile> FindAllBuildProfiles()
        {
            const string buildProfileAssetSearchString = $"t:{nameof(BuildProfile)}";
            var assetsGuids = AssetDatabase.FindAssets(buildProfileAssetSearchString);
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

            result.Sort((lhs, rhs) => EditorUtility.NaturalCompare(lhs.name, rhs.name));
            return result;
        }

        static string ReplaceFileNameInPath(string originalPath, string newName)
        {
            string directory = Path.GetDirectoryName(originalPath);
            string extension = Path.GetExtension(originalPath);
            return Path.Combine(directory, $"{newName}{extension}");
        }

        static string GetDuplicatedBuilProfilePathForClassic(BuildProfile buildProfile)
        {
            string name = BuildProfileModuleUtil.GetClassicPlatformDisplayName(buildProfile.moduleName, buildProfile.subtarget);
            return Path.Combine(k_AssetFolderPath, $"{name}.asset");
        }

        static void CheckCreateCustomBuildProfileFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");

            if (!AssetDatabase.IsValidFolder(k_AssetFolderPath))
                AssetDatabase.CreateFolder("Assets/Settings", "Build Profiles");
        }
    }
}
