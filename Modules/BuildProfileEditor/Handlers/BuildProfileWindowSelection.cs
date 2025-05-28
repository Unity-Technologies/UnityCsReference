// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Build.Profile.Elements;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Handlers
{
    /// <summary>
    /// Track selected profiles in build profile window and updates GUI state based
    /// on current selection. <see cref="BuildProfileContextMenu"/> references this
    /// class when duplicating or deleting profiles.
    /// </summary>
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

        readonly PlatformListView m_PlatformListViews;
        readonly Image m_SelectedProfileImage;
        readonly Label m_SelectedProfileNameLabel;
        readonly Label m_SelectedProfilePlatformLabel;

        readonly List<BuildProfile> m_SelectedBuildProfiles;
        readonly Dictionary<GUID, int> m_MultiSelectLabelCountMap;

        internal bool IsSingleSelection() => m_SelectedBuildProfiles.Count == 1;
        internal bool IsMultipleSelection() => m_SelectedBuildProfiles.Count > 1;
        internal bool HasSelection() => m_SelectedBuildProfiles.Count > 0;

        /// <summary>
        /// Visual element tied to the selection.
        /// </summary>
        internal PlatformListView visualElement {  get => m_PlatformListViews; }

        internal BuildProfileWindowSelection(VisualElement rootVisualElement, PlatformListView platformListView)
        {
            m_PlatformListViews = platformListView;
            m_SelectedBuildProfiles = new List<BuildProfile>();
            m_MultiSelectLabelCountMap = new Dictionary<GUID, int>();
            m_SelectedProfileImage = rootVisualElement.Q<Image>("selected-profile-image");
            m_SelectedProfileNameLabel = rootVisualElement.Q<Label>("selected-profile-name");
            m_SelectedProfilePlatformLabel = rootVisualElement.Q<Label>("selected-profile-platform");
        }

        internal void UpdateSelectionGUI(BuildProfile profile)
        {
            if (IsMultipleSelection())
            {
                m_SelectedProfileImage.image = BuildProfileModuleUtil.GetPlatformIcon(string.Empty);
                m_SelectedProfileNameLabel.text = $"{m_SelectedBuildProfiles.Count} Build Profiles";
                m_SelectedProfilePlatformLabel.text = GetMultiSelectLabelString();
            }
            else
            {
                // Selected profile could be a custom or classic platform.
                // Classic platforms only display the platform name, while custom show file name and platform name.
                // When a selection is made, we clear the currently selection in the opposite list view.
                m_SelectedProfileImage.image = BuildProfileModuleUtil.GetPlatformIcon(profile.platformId);
                var platformDisplayName = BuildProfileModuleUtil.GetClassicPlatformDisplayName(profile.platformId);
                if (BuildProfileContext.IsClassicPlatformProfile(profile))
                {
                    m_SelectedProfileNameLabel.text = platformDisplayName;
                    m_SelectedProfilePlatformLabel.Hide();
                }
                else
                {
                    m_SelectedProfileNameLabel.text = profile.name;
                    m_SelectedProfilePlatformLabel.text = platformDisplayName;
                    m_SelectedProfilePlatformLabel.Show();
                }
            }
        }

        /// <summary>
        /// Update selected profile for missing platform
        /// </summary>
        internal void MissingPlatformSelected(string platformId)
        {
            ClearSelectedProfiles();

            m_SelectedProfileImage.image = BuildProfileModuleUtil.GetPlatformIcon(platformId);
            m_SelectedProfileNameLabel.text = BuildProfileModuleUtil.GetClassicPlatformDisplayName(platformId);
            m_SelectedProfilePlatformLabel.Hide();
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
                    m_PlatformListViews.ClearProfileSelection();
                    m_PlatformListViews.ClearPlatformSelection();
                    break;
                case ListViewSelectionType.Classic:
                    m_PlatformListViews.ClearPlatformSelection();
                    break;

                case ListViewSelectionType.Custom:
                    m_PlatformListViews.ClearProfileSelection();
                    break;

                case ListViewSelectionType.MissingClassic:
                    m_PlatformListViews.ClearPlatformSelection();
                    break;

                case ListViewSelectionType.All:
                    m_PlatformListViews.ClearPlatformSelection();
                    m_PlatformListViews.ClearProfileSelection();
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
        /// Add selected build profiles
        /// </summary>
        /// <param name="selectedItems"></param>
        internal void SelectItem(BuildProfile selectedItem)
        {
            ClearSelectedProfiles();
            m_SelectedBuildProfiles.Add(selectedItem);
        }

        internal void ClearSelectedProfiles()
        {
            m_SelectedBuildProfiles.Clear();
        }

        string GetMultiSelectLabelString()
        {
            string labelText = string.Empty;

            foreach (var buildProfile in m_SelectedBuildProfiles)
            {
                if (!m_MultiSelectLabelCountMap.TryAdd(new GUID(buildProfile.platformId), 1))
                {
                    m_MultiSelectLabelCountMap[new GUID(buildProfile.platformId)]++;
                }
            }

            int index = 0;
            foreach (var platform in m_MultiSelectLabelCountMap)
            {
                var platformName = BuildProfileModuleUtil.GetClassicPlatformDisplayName(platform.Key.ToString());
                labelText += $"{platform.Value} {platformName}";

                if (++index < m_MultiSelectLabelCountMap.Count)
                {
                    labelText += ", ";
                }
            }

            labelText += $" {TrText.buildProfilesName}";
            m_MultiSelectLabelCountMap.Clear();
            return labelText;
        }
    }
}
