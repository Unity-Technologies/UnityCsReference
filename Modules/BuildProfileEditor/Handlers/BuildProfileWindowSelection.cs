// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Handlers
{
    internal class BuildProfileWindowSelection
    {
        internal enum ListViewSelectionType
        {
            Classic,
            Custom,
            MissingClassic,
            ClassicAndCustom,
            All
        }

        readonly ListView m_BuildProfileClassicPlatformListView;
        readonly ListView m_MissingClassicPlatformListView;
        readonly ListView m_BuildProfilesListView;

        readonly Image m_SelectedProfileImage;
        readonly Label m_SelectedProfileNameLabel;
        readonly Label m_SelectedProfilePlatformLabel;

        readonly List<BuildProfile> m_SelectedBuildProfiles;

        internal bool IsMultipleSelection() => m_SelectedBuildProfiles.Count > 1;
        internal bool HasSelection() => m_SelectedBuildProfiles.Count > 0;

        internal BuildProfileWindowSelection(VisualElement rootVisualElement, ListView classicPlatformListView, ListView buildProfilesListView, ListView missingPlatformListView)
        {
            m_BuildProfileClassicPlatformListView = classicPlatformListView;
            m_MissingClassicPlatformListView = missingPlatformListView;
            m_BuildProfilesListView = buildProfilesListView;
            m_SelectedBuildProfiles = new List<BuildProfile>();
            m_SelectedProfileImage = rootVisualElement.Q<Image>("selected-profile-image");
            m_SelectedProfileNameLabel = rootVisualElement.Q<Label>("selected-profile-name");
            m_SelectedProfilePlatformLabel = rootVisualElement.Q<Label>("selected-profile-platform");
        }

        internal void UpdateSelectionGUI(BuildProfile profile)
        {
            if (IsMultipleSelection())
            {
                m_SelectedProfileImage.image = BuildProfileModuleUtil.GetPlatformIcon(string.Empty, StandaloneBuildSubtarget.Default);
                m_SelectedProfileNameLabel.text = $"{m_SelectedBuildProfiles.Count} Build Profiles";
            }
            else
            {
                // Selected profile could be a custom or classic platform.
                // Classic platforms only display the platform name, while custom show file name and platform name.
                // When a selection is made, we clear the currently selection in the opposite list view.
                m_SelectedProfileImage.image = BuildProfileModuleUtil.GetPlatformIcon(profile.moduleName, profile.subtarget);
                if (BuildProfileContext.IsClassicPlatformProfile(profile))
                {
                    m_SelectedProfileNameLabel.text = BuildProfileModuleUtil.GetClassicPlatformDisplayName(
                        profile.moduleName, profile.subtarget);
                    m_SelectedProfilePlatformLabel.Hide();
                    ClearListViewSelection(ListViewSelectionType.Custom);
                }
                else
                {
                    m_SelectedProfileNameLabel.text = profile.name;
                    m_SelectedProfilePlatformLabel.text = BuildProfileModuleUtil.GetClassicPlatformDisplayName(
                        profile.moduleName, profile.subtarget);
                    m_SelectedProfilePlatformLabel.Show();
                    ClearListViewSelection(ListViewSelectionType.Classic);
                }
            }
        }

        /// <summary>
        /// Update selected profile for missing platform
        /// </summary>
        internal void MissingPlatformSelected(string moduleName, StandaloneBuildSubtarget subtarget)
        {
            ClearSelectedProfiles();

            m_SelectedProfileImage.image = BuildProfileModuleUtil.GetPlatformIcon(moduleName, subtarget);
            m_SelectedProfileNameLabel.text = BuildProfileModuleUtil.GetClassicPlatformDisplayName(moduleName, subtarget);
            m_SelectedProfilePlatformLabel.Hide();
        }

        /// <summary>
        /// Check custom profiles and classic platforms to select the active one
        /// </summary>
        internal void SelectActiveProfile(IList<BuildProfile> customProfiles, IList<BuildProfile> classicPlatforms)
        {
            bool setActive = TrySelectActiveProfile(customProfiles);
            if (!setActive)
            {
                TrySelectActiveProfile(classicPlatforms);
            }
        }

        /// <summary>
        /// Get selected profile at index
        /// </summary>
        internal BuildProfile Get(int index)
        {
            if (index < 0 || index >= m_SelectedBuildProfiles.Count)
                return null;

            return m_SelectedBuildProfiles[index];
        }

        /// <summary>
        /// Get all selected profiles
        /// </summary>
        internal List<BuildProfile> GetAll()
        {
            return m_SelectedBuildProfiles;
        }

        /// <summary>
        /// Clear custom and classic list view selections
        /// </summary>
        internal void ClearListViewSelection(ListViewSelectionType listViewSelectionType)
        {
            switch (listViewSelectionType)
            {
                case ListViewSelectionType.ClassicAndCustom:
                    m_BuildProfilesListView.ClearSelection();
                    m_BuildProfileClassicPlatformListView.ClearSelection();
                    break;

                case ListViewSelectionType.Classic:
                    m_BuildProfileClassicPlatformListView.ClearSelection();
                    break;

                case ListViewSelectionType.Custom:
                    m_BuildProfilesListView.ClearSelection();
                    break;

                case ListViewSelectionType.MissingClassic:
                    m_MissingClassicPlatformListView.ClearSelection();
                    break;

                case ListViewSelectionType.All:
                    m_MissingClassicPlatformListView.ClearSelection();
                    m_BuildProfileClassicPlatformListView.ClearSelection();
                    m_BuildProfilesListView.ClearSelection();
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Add selected build profiles
        /// </summary>
        /// <param name="selectedItems"></param>
        internal void SelectItems(IEnumerable<object> selectedItems)
        {
            ClearSelectedProfiles();

            foreach (var selectedItem in selectedItems)
            {
                if (selectedItem is BuildProfile buildProfile)
                    m_SelectedBuildProfiles.Add(buildProfile);
            }
        }

        /// <summary>
        /// Selects the active build profile. Must be called after the list views have been updated,
        /// as <see cref="m_ActiveProfileListIndex"/> is dependent on latest list view state.
        /// </summary>
        internal void SelectActiveProfile(int activeProfileListIndex)
        {
            if (activeProfileListIndex < 0)
            {
                m_BuildProfileClassicPlatformListView.SetSelection(0);
                Debug.LogWarning("[BuildProfile] Failed to find an active profile.");
                return;
            }

            if (BuildProfileContext.instance.activeProfile is not null
                && activeProfileListIndex < m_BuildProfilesListView.itemsSource.Count)
            {
                m_BuildProfilesListView.SetSelection(activeProfileListIndex);
            }
            else if (activeProfileListIndex < m_BuildProfileClassicPlatformListView.itemsSource.Count)
            {
                m_BuildProfileClassicPlatformListView.SetSelection(activeProfileListIndex);
            }
            else
            {
                Debug.LogWarning("[BuildProfile] Active profile not found in build profile window data source.");
            }
        }

        /// <summary>
        /// Set or add build profile to list view selection by index
        /// </summary>
        internal void SelectBuildProfileInViewByIndex(int index, bool isClassic, bool shouldAppend)
        {
            var targetView = isClassic ? m_BuildProfileClassicPlatformListView : m_BuildProfilesListView;
            if (shouldAppend)
            {
                targetView.AddToSelection(index);
            }
            else
            {
                targetView.SetSelection(index);
            }
        }

        void ClearSelectedProfiles()
        {
            m_SelectedBuildProfiles.Clear();
        }

        bool TrySelectActiveProfile(IList<BuildProfile> buildProfiles)
        {
            for (int i = 0; i < buildProfiles.Count; ++i)
            {
                if (buildProfiles[i].IsActiveBuildProfileOrPlatform())
                {
                    SelectActiveProfile(i);
                    return true;
                }
            }

            return false;
        }
    }
}
