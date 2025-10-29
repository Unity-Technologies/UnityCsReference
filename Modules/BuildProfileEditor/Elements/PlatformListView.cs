// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Build.Profile.Handlers;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Build.Profile.Handlers.BuildProfileWindowSelection;

namespace UnityEditor.Build.Profile.Elements
{
    /// <summary>
    /// Build Profile window list views for classic platforms,
    /// scene list, missing platforms, and custom build profiles.
    /// </summary>
    internal class PlatformListView
    {
        internal struct ClassicItemData
        {
            public ListItemType type;
            public BuildProfile data;
            public string text;
            public Texture2D icon;
            public GUID platformId;
        }

        internal enum ListItemType
        {
            InstalledPlatform,
            MissingPlatform,
        }

        internal const string buildProfilesVisualElement = "custom-build-profiles";
        internal const string buildProfileClassicPlatformVisualElement = "build-profile-classic-platforms";
        internal const string buildProfileSharedSceneListElement = "shared-scene-list-elem";

        readonly BuildProfileWindow m_Parent;
        readonly BuildProfileDataSource m_DataSource;
        readonly ListView m_PlatformListView;
        readonly ListView m_BuildProfilesListView;
        readonly VisualElement m_SharedSceneListElement;
        internal BuildProfileListEditableLabel m_SharedSceneListItem;

        public PlatformListView(BuildProfileWindow parent, BuildProfileDataSource dataSource)
        {
            m_Parent = parent;
            m_DataSource = dataSource;
            m_PlatformListView = parent.rootVisualElement.Q<ListView>(buildProfileClassicPlatformVisualElement);
            m_BuildProfilesListView = parent.rootVisualElement.Q<ListView>(buildProfilesVisualElement);
            m_SharedSceneListElement = parent.rootVisualElement.Q<VisualElement>(buildProfileSharedSceneListElement);
        }

        internal void Create()
        {
            m_SharedSceneListItem = m_Parent.CreateEditableLabelItem();
            m_SharedSceneListItem.Set(TrText.sceneList, BuildProfileModuleUtil.GetSceneListIcon());
            m_SharedSceneListItem.AddToClassList("unity-list-view__item");
            m_SharedSceneListItem.AddToClassList("unity-collection-view__item");
            m_SharedSceneListItem.AddManipulator(new Clickable(evt => {
                m_SharedSceneListItem.AddToClassList("unity-collection-view__item--selected");
                m_Parent.OnClassicSceneListSelected();
            }));
            m_SharedSceneListElement.Add(m_SharedSceneListItem);

            m_PlatformListView.Q<ScrollView>().verticalScrollerVisibility = ScrollerVisibility.Hidden;
            m_PlatformListView.selectionType = SelectionType.Single;
            m_PlatformListView.itemsSource = GetPlatformListData(m_DataSource);
            m_PlatformListView.makeItem = m_Parent.CreateEditableLabelItem;
            m_PlatformListView.bindItem = (element, index) =>
            {
                var item = (ClassicItemData)m_PlatformListView.itemsSource[index];
                var label = element as BuildProfileListEditableLabel;
                label.Set(item.text, item.icon);
                label.dataSource = item.data;

                switch (item.type)
                {
                    case ListItemType.MissingPlatform:
                        label.SetEnabled(false);
                        break;
                    case ListItemType.InstalledPlatform:
                        if (item.data.IsActiveBuildProfileOrPlatform())
                        {
                            label.SetActiveIndicator(true);
                        }
                        break;
                    default:
                        label.SetActiveIndicator(false);
                        break;
                }
            };
            m_PlatformListView.selectionChanged += (items) =>
            {
                if (m_PlatformListView.selectedIndex < 0)
                    return;

                var data = (ClassicItemData)m_PlatformListView.selectedItem;

                switch (data.type)
                {
                    case ListItemType.InstalledPlatform:
                        m_Parent.OnClassicPlatformSelected(data.data);
                        break;
                    case ListItemType.MissingPlatform:
                        m_Parent.OnMissingClassicPlatformSelected(data.platformId);
                        break;
                }
            };
            m_PlatformListView.unbindItem = UnbindItem;

            m_BuildProfilesListView.Q<ScrollView>().verticalScrollerVisibility = ScrollerVisibility.Hidden;
            m_BuildProfilesListView.selectionType = SelectionType.Multiple;
            m_BuildProfilesListView.itemsSource = (System.Collections.IList)m_DataSource.customBuildProfiles;
            m_BuildProfilesListView.makeItem = m_Parent.CreateEditableLabelItem;
            m_BuildProfilesListView.bindItem = (VisualElement element, int index) =>
            {
                var profile = m_DataSource.customBuildProfiles[index];
                UnityEngine.Assertions.Assert.IsNotNull(profile, "Build profile is null");

                var editableBuildProfileLabel = element as BuildProfileListEditableLabel;
                editableBuildProfileLabel.dataSource = profile;
                UnityEngine.Assertions.Assert.IsNotNull(editableBuildProfileLabel, "Build profile label is null");

                var icon = BuildProfileModuleUtil.GetPlatformIconSmall(profile.platformGuid);
                editableBuildProfileLabel.Set(profile.name, icon);

                if (profile.IsActiveBuildProfileOrPlatform())
                {
                    editableBuildProfileLabel.SetActiveIndicator(true);
                }
                else
                    editableBuildProfileLabel.SetActiveIndicator(false);

                element.RegisterCallback<PointerEnterEvent, BuildProfile>(PointerEntersBuildProfileElement, profile);
            };
            m_BuildProfilesListView.selectionChanged += m_Parent.OnCustomProfileSelected;
            m_BuildProfilesListView.unbindItem = UnbindItem;

            // Check if we need to select a recently enabled platform
            string lastEnabledPlatformGUID = EditorPrefs.GetString("LastEnabledPlatformGUID", "");
            if (!string.IsNullOrEmpty(lastEnabledPlatformGUID))
            {
                m_PlatformListView.RegisterCallback<GeometryChangedEvent>(SelectLastEnabledPlatformOnGeometryChange);
            }
        }

        private static void PointerEntersBuildProfileElement(PointerEnterEvent evt, BuildProfile profile)
        {
            if (evt.currentTarget is not BuildProfileListEditableLabel buildProfileLabel)
                return;

            var tooltipMessage = BuildProfileContext.IsClassicPlatformProfile(profile)
                ? string.Empty
                : AssetDatabase.GetAssetPath(profile);
            buildProfileLabel.tooltip = tooltipMessage;
        }

        internal void ClearPlatformSelection() => m_PlatformListView.ClearSelection();

        internal void ClearProfileSelection() => m_BuildProfilesListView.ClearSelection();

        internal void ClearSharedSceneListSelection() => m_SharedSceneListItem.RemoveFromClassList("unity-collection-view__item--selected");

        internal void CleanupSelection(ListViewSelectionType listViewSelectionType)
        {
            switch (listViewSelectionType)
            {
                case ListViewSelectionType.Classic:
                    ClearProfileSelection();
                    ClearSharedSceneListSelection();
                    break;

                case ListViewSelectionType.Custom:
                    ClearPlatformSelection();
                    ClearSharedSceneListSelection();
                    break;

                case ListViewSelectionType.MissingClassic:
                    ClearProfileSelection();
                    ClearSharedSceneListSelection();
                    break;

                default:
                    break;
            }
        }

        internal void Unbind()
        {
            if (m_PlatformListView != null)
                m_PlatformListView.itemsSource = null;

            if (m_BuildProfilesListView != null)
                m_BuildProfilesListView.itemsSource = null;
        }

        internal void Rebuild()
        {
            m_PlatformListView.Rebuild();
            m_BuildProfilesListView.Rebuild();
        }

        internal void SelectInstalledPlatform(int index)
        {
            if (m_BuildProfilesListView.selectedIndex >= 0)
                m_BuildProfilesListView.ClearSelection();

            ClearSharedSceneListSelection();

            m_PlatformListView.SetSelection(index);
        }

        internal void SelectBuildProfile(int index)
        {
            if (m_PlatformListView.selectedIndex >= 0)
                m_PlatformListView.ClearSelection();

            ClearSharedSceneListSelection();

            m_BuildProfilesListView.SetSelection(index);
        }

        internal void SelectSharedSceneList()
        {
            if (m_PlatformListView.selectedIndex >= 0)
                m_PlatformListView.ClearSelection();

            if (m_BuildProfilesListView.selectedIndex >= 0)
                m_BuildProfilesListView.ClearSelection();

            m_SharedSceneListItem.AddToClassList("unity-collection-view__item--selected");
        }

        internal void AppendBuildProfileSelection(int index) => m_BuildProfilesListView.AddToSelection(index);

        internal void ShowCustomBuildProfiles() => m_BuildProfilesListView.Show();

        internal void HideCustomBuildProfiles() => m_BuildProfilesListView.Hide();

        /// <summary>
        /// Selects active profile from available custom build profiles or installed platforms.
        /// </summary>
        internal void SelectActiveProfile()
        {
            if (TrySelectCustomBuildProfile())
                return;

            if (TrySelectClassicPlatform())
                return;

            if (TrySelectClassicBasePlatform())
                return;

            // Disabled warning message. See PLAT-12653.
            //Debug.LogWarning("[BuildProfile] Active profile not found in build profile window data source.");
        }

        /// <summary>
        /// This is used as a callback for the GeometryChangedEvent to select the last enabled platform
        /// via the "Enable Platform" button available on specific platforms. Requires the LastEnabledPlatformGUID
        /// to be set in the EditorPrefs. This callback is intended for the classic platforms list. 
        /// </summary>
        private void SelectLastEnabledPlatformOnGeometryChange(GeometryChangedEvent evt)
        {
            // Check to make sure it's not a custom profile, as it'd end up selecting the classic platform instead.
            if (m_PlatformListView.selectedIndex < 0 && m_BuildProfilesListView.selectedIndex >= 0)
            {
                EditorPrefs.DeleteKey("LastEnabledPlatformGUID");
                m_PlatformListView.UnregisterCallback<GeometryChangedEvent>(SelectLastEnabledPlatformOnGeometryChange);
                return;
            }

            string lastEnabledPlatformGUID = EditorPrefs.GetString("LastEnabledPlatformGUID", "");
            if (!string.IsNullOrEmpty(lastEnabledPlatformGUID))
            {
                GUID platformId = new GUID(lastEnabledPlatformGUID);

                // To avoid duplicate processing
                EditorPrefs.DeleteKey("LastEnabledPlatformGUID");
                m_PlatformListView.UnregisterCallback<GeometryChangedEvent>(SelectLastEnabledPlatformOnGeometryChange);

                // Select the platform by GUID after the layout is updated
                TrySelectInstalledClassicPlatformByGUID(platformId);
            }
        }

        /// <summary>
        /// Selects a given installed, classic platform by its GUID. This selection is similar to the user
        /// selection of clicking on a platform in the list view. It does not change the active build profile.
        /// </summary>
        private void TrySelectInstalledClassicPlatformByGUID(GUID platformId)
        {
            string guidString = platformId.ToString();

            var items = m_DataSource.classicPlatforms;
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    // Compare the GUID strings
                    if (CompareBuildProfileWithGuid(items[i], guidString))
                    {
                        SelectInstalledPlatform(i);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Compares a Build Profile platformGuid with a GUID string.
        /// </summary>
        /// <returns>true if the ID is the same, false if not or unknown.</returns>
        private bool CompareBuildProfileWithGuid(BuildProfile item, string guidString)
        {
            string itemGuidString;

            if (item != null)
            {
                itemGuidString = item.platformGuid.ToString();
            }
            else
            {
                // Fallback to avoid null references for future cases
                return false;
            }

            // Do an exact string comparison on the GUIDs
            return string.Equals(itemGuidString, guidString, System.StringComparison.Ordinal);
        }

        bool TrySelectClassicPlatform()
        {
            var search = m_DataSource.classicPlatforms;
            for (int i = 0; i < search.Count; ++i)
            {
                if (search[i].IsActiveBuildProfileOrPlatform())
                {
                    SelectInstalledPlatform(i);
                    return true;
                }
            }
            return false;
        }

        bool TrySelectClassicBasePlatform()
        {
            var search = m_DataSource.classicPlatforms;
            for (int i = 0; i < search.Count; ++i)
            {
                if (BuildProfileModuleUtil.IsBasePlatformOfActivePlatform(search[i].platformGuid))
                {
                    SelectInstalledPlatform(i);
                    BuildProfileModuleUtil.SwitchLegacyActiveFromBuildProfile(search[i]);
                    return true;
                }
            }
            return false;
        }

        bool TrySelectCustomBuildProfile()
        {
            var search = m_DataSource.customBuildProfiles;
            for (int i = 0; i < search.Count; ++i)
            {
                if (search[i].IsActiveBuildProfileOrPlatform())
                {
                    SelectBuildProfile(i);
                    return true;
                }
            }
            return false;
        }

        static List<ClassicItemData> GetPlatformListData(BuildProfileDataSource dataSource)
        {
            var result = new List<ClassicItemData>();

            foreach (var profile in dataSource.classicPlatforms)
            {
                result.Add(new ClassicItemData
                {
                    type = ListItemType.InstalledPlatform,
                    data = profile,
                    text = BuildProfileModuleUtil.GetClassicPlatformDisplayName(profile.platformGuid),
                    icon = BuildProfileModuleUtil.GetPlatformIconSmall(profile.platformGuid)
                });
            }

            foreach (var platformId in BuildProfileContext.instance.GetMissingKnownPlatformModules())
            {
                result.Add(new ClassicItemData()
                {
                    type = ListItemType.MissingPlatform,
                    text = BuildProfileModuleUtil.GetClassicPlatformDisplayName(platformId),
                    icon = BuildProfileModuleUtil.GetPlatformIconSmall(platformId),
                    platformId = platformId
                });
            }

            return result;
        }

        static void UnbindItem(VisualElement element, int index)
        {
            var label = element as BuildProfileListEditableLabel;
            label.UnbindItem();
        }
    }
}
