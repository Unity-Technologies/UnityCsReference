// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Build.Profile.Handlers
{
    internal class BuildProfileDataSource : IDisposable
    {
        internal IList<BuildProfile> classicPlatforms { get; }
        internal IList<BuildProfile> customBuildProfiles { get; }

        BuildProfileWindow m_Window;

        internal BuildProfileDataSource(BuildProfileWindow window)
        {
            this.m_Window = window;
            classicPlatforms = BuildProfileContext.instance.classicPlatformProfiles;
            customBuildProfiles = FindAllBuildProfiles();

            BuildProfile.AddOnBuildProfileEnable(OnBuildProfileCreated);
        }

        public void Dispose()
        {
            BuildProfile.RemoveOnBuildProfileEnable(OnBuildProfileCreated);
        }

        /// <summary>
        /// Called by the Build Project Window when a null custom profile is detected.
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
    }
}
