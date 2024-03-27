// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Build.Profile.Handlers;
using UnityEngine;
using UnityEngine.UIElements;

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
            public string moduleName;
            public StandaloneBuildSubtarget subtarget;
        }

        internal enum ListItemType
        {
            SceneList,
            InstalledPlatform,
            MissingPlatform,
        }

        internal const string buildProfilesVisualElement = "custom-build-profiles";
        internal const string buildProfileClassicPlatformVisualElement = "build-profile-classic-platforms";

        readonly BuildProfileWindow m_Parent;
        readonly BuildProfileDataSource m_DataSource;
        readonly ListView m_PlatformListView;
        readonly ListView m_BuildProfilesListView;

        public PlatformListView(BuildProfileWindow parent, BuildProfileDataSource dataSource)
        {
            m_Parent = parent;
            m_DataSource = dataSource;
            m_PlatformListView = parent.rootVisualElement.Q<ListView>(buildProfileClassicPlatformVisualElement);
            m_BuildProfilesListView = parent.rootVisualElement.Q<ListView>(buildProfilesVisualElement);
        }

        internal void Create()
        {
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
                    case ListItemType.SceneList:
                        m_Parent.OnClassicSceneListSelected();
                        break;
                    case ListItemType.InstalledPlatform:
                        m_Parent.OnClassicPlatformSelected(data.data);
                        break;
                    case ListItemType.MissingPlatform:
                        m_Parent.OnMissingClassicPlatformSelected(data.moduleName, data.subtarget);
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

                var icon = BuildProfileModuleUtil.GetPlatformIconSmall(profile.moduleName, profile.subtarget);
                editableBuildProfileLabel.Set(profile.name, icon);

                if (profile.IsActiveBuildProfileOrPlatform())
                {
                    editableBuildProfileLabel.SetActiveIndicator(true);
                }
                else
                    editableBuildProfileLabel.SetActiveIndicator(false);

                if (!BuildProfileContext.IsClassicPlatformProfile(profile))
                {
                    editableBuildProfileLabel.tooltip = AssetDatabase.GetAssetPath(profile);
                }
            };
            m_BuildProfilesListView.selectionChanged += m_Parent.OnCustomProfileSelected;
            m_BuildProfilesListView.unbindItem = UnbindItem;
        }

        internal void ClearPlatformSelection() => m_PlatformListView.ClearSelection();

        internal void ClearProfileSelection() => m_BuildProfilesListView.ClearSelection();

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

            // Offset by 1 to account for the scene list item.
            m_PlatformListView.SetSelection(index + 1);
        }

        internal void SelectBuildProfile(int index)
        {
            if (m_PlatformListView.selectedIndex >= 0)
                m_PlatformListView.ClearSelection();

            m_BuildProfilesListView.SetSelection(index);
        }

        internal void AppendBuildProfileSelection(int index) => m_BuildProfilesListView.AddToSelection(index);

        internal void ShowCustomBuildProfiles() => m_BuildProfilesListView.Show();

        internal void HideCustomBuildProfiles() => m_BuildProfilesListView.Hide();

        /// <summary>
        /// Selects active profile from available custom build profiles or installed platforms.
        /// </summary>
        internal void SelectActiveProfile()
        {
            var search = m_DataSource.customBuildProfiles;
            for (int i = 0; i < search.Count; ++i)
            {
                if (search[i].IsActiveBuildProfileOrPlatform())
                {
                    SelectBuildProfile(i);
                    return;
                }
            }

            search = m_DataSource.classicPlatforms;
            for (int i = 0; i < search.Count; ++i)
            {
                if (search[i].IsActiveBuildProfileOrPlatform())
                {
                    // Consider scene list item occupies the first index.
                    SelectInstalledPlatform(i);
                    return;
                }
            }

            Debug.LogWarning("[BuildProfile] Active profile not found in build profile window data source.");
        }

        static List<ClassicItemData> GetPlatformListData(BuildProfileDataSource dataSource)
        {
            var result = new List<ClassicItemData>
            {
                new()
                {
                    type = ListItemType.SceneList,
                    text = TrText.sceneList,
                    icon = BuildProfileModuleUtil.GetSceneListIcon()
                }
            };

            foreach (var profile in dataSource.classicPlatforms)
            {
                result.Add(new ClassicItemData
                {
                    type = ListItemType.InstalledPlatform,
                    data = profile,
                    text = BuildProfileModuleUtil.GetClassicPlatformDisplayName(profile.moduleName, profile.subtarget),
                    icon = BuildProfileModuleUtil.GetPlatformIconSmall(profile.moduleName, profile.subtarget)
                });
            }

            foreach (var (moduleName, subtarget) in BuildProfileContext.instance.GetMissingKnownPlatformModules())
            {
                result.Add(new ClassicItemData()
                {
                    type = ListItemType.MissingPlatform,
                    text = BuildProfileModuleUtil.GetClassicPlatformDisplayName(moduleName, subtarget),
                    icon = BuildProfileModuleUtil.GetPlatformIconSmall(moduleName, subtarget),
                    moduleName = moduleName,
                    subtarget = subtarget
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
