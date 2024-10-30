// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor.Build.Profile.Elements;
using UnityEditor.Build.Profile.Handlers;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Platform Discovery Window used for creating new Build Profile assets from installed platform modules.
    /// </summary>
    internal class PlatformDiscoveryWindow : EditorWindow
    {
        const string k_Uxml = "BuildProfile/UXML/PlatformDiscoveryWindow.uxml";

        BuildProfileCard[] m_Cards;
        BuildProfileCard m_SelectedCard;
        Image m_SelectedCardImage;
        Label m_SelectedDisplayNameLabel;
        Foldout m_SelectedDescriptionFoldout;
        Label m_SelectedDescriptionLabel;
        Button m_AddBuildProfileButton;
        BuildProfilePlatformBrowserClosed m_CloseEvent;

        ListView m_PackagesListView;
        Foldout m_PackagesFoldout;

        /// <summary>
        /// Warning message displayed when the selected card's platform
        /// is not supported.
        /// </summary>
        HelpBox m_CardWarningHelpBox;
        VisualElement m_HelpBoxWrapper;

        public static void ShowWindow()
        {
            var window = GetWindow<PlatformDiscoveryWindow>(true, TrText.platformDiscoveryTitle, true);
            window.minSize = new Vector2(900, 500);
        }

        public void OnDisable()
        {
            EditorAnalytics.SendAnalytic(m_CloseEvent);
        }

        public void CreateGUI()
        {
            var windowUxml = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var windowUss = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
            rootVisualElement.styleSheets.Add(windowUss);
            windowUxml.CloneTree(rootVisualElement);
            m_CloseEvent = new BuildProfilePlatformBrowserClosed();

            // Capture static visual element reference.
            m_AddBuildProfileButton = rootVisualElement.Q<Button>("add-build-profile-button");
            m_SelectedDisplayNameLabel = rootVisualElement.Q<Label>("selected-card-name");
            m_SelectedCardImage = rootVisualElement.Q<Image>("selected-card-icon");
            m_CardWarningHelpBox = rootVisualElement.Q<HelpBox>("helpbox-card-warning");
            m_SelectedDescriptionFoldout = rootVisualElement.Q<Foldout>("platform-description-foldout");
            m_SelectedDescriptionLabel = rootVisualElement.Q<Label>("platform-description-label");
            m_HelpBoxWrapper = rootVisualElement.Q<VisualElement>("helpbox-wrapper");
            m_PackagesListView = rootVisualElement.Q<ListView>("packages-root-listview");
            m_PackagesFoldout = rootVisualElement.Q<Foldout>("packages-foldout");
            m_PackagesFoldout.text = TrText.packagesHeader;

            m_PackagesListView.itemsSource = Array.Empty<object>();
            m_PackagesListView.makeItem = PlatformPackageItem.Make;
            m_PackagesListView.bindItem = (VisualElement element, int index) =>
            {
                if (element is not PlatformPackageItem packageItem)
                {
                    Debug.LogWarning("Unexpected element type in PlatformDiscoveryWindow. element=" + element);
                    return;
                }

                var packageEntry = m_PackagesListView.itemsSource[index] as PlatformPackageEntry;
                packageItem.Set(packageEntry);
            };
            var packageSelectAll = m_PackagesFoldout.Q<Button>("package-select-all");
            packageSelectAll.text = TrText.selectAll;
            packageSelectAll.clicked += () =>
            {
                for (int i = 0; i <  m_PackagesListView.itemsSource.Count; ++i)
                {
                    var item = m_PackagesListView.GetRootElementForIndex(i);
                    if (item is not PlatformPackageItem packageItemVisualElement)
                        continue;

                    packageItemVisualElement.SetShouldInstallToggle(true);

                }
            };
            var packageDeselectAll = m_PackagesFoldout.Q<Button>("package-deselect-all");
            packageDeselectAll.text = TrText.deselectAll;
            packageDeselectAll.clicked += () =>
            {
                for (int i = 0; i < m_PackagesListView.itemsSource.Count; ++i)
                {
                    var item = m_PackagesListView.GetRootElementForIndex(i);
                    if (item is not PlatformPackageItem packageItemVisualElement)
                        continue;

                    packageItemVisualElement.SetShouldInstallToggle(false);
                }
            };

            // Apply localized text to static elements.
            rootVisualElement.Q<ToolbarButton>("toolbar-filter-all").text = TrText.all;
            m_SelectedDescriptionFoldout.text = TrText.description;
            m_AddBuildProfileButton.text = TrText.addBuildProfile;

            // Build dynamic visual elements.
            m_Cards = FindAllVisiblePlatforms();
            var cards = CreateCardListView();

            // Register event handlers.
            m_AddBuildProfileButton.SetEnabled(true);
            m_AddBuildProfileButton.clicked += () =>
            {
                OnAddBuildProfileClicked(m_SelectedCard);
                m_CloseEvent = new BuildProfilePlatformBrowserClosed(new BuildProfilePlatformBrowserClosed.Payload()
                {
                    wasProfileCreated = true,
                    platformId = m_SelectedCard.platformId,
                    platformDisplayName = m_SelectedCard.displayName,
                });
                Close();
            };

            // First element should match standalone platform.
            cards.SetSelection(0);
        }

        /// <summary>
        /// Verify editor can create build profiles for the currently selected card. Otherwise
        /// display relevant documentation to the user.
        /// </summary>
        void OnCardSelected(BuildProfileCard card)
        {
            m_SelectedCard = card;
            m_SelectedDisplayNameLabel.text = card.displayName;
            m_SelectedCardImage.image = BuildProfileModuleUtil.GetPlatformIcon(card.platformId);
            if (Util.UpdatePlatformRequirementsWarningHelpBox(m_CardWarningHelpBox, card.platformId))
                m_HelpBoxWrapper.Show();
            else
                m_HelpBoxWrapper.Hide();

            if (card.requiredPackages.Length > 0 || card.recommendedPackages.Length > 0)
            {
                m_PackagesListView.itemsSource = PlatformPackageItem
                    .CreateItemSource(card.requiredPackages, card.recommendedPackages);
                m_PackagesListView.Rebuild();
                m_PackagesFoldout.Show();
            }
            else
                m_PackagesFoldout.Hide();

            if (card.description.Length > 0)
            {
                m_SelectedDescriptionLabel.text = card.description;
                m_SelectedDescriptionFoldout.Show();
            }
            else
                m_SelectedDescriptionFoldout.Hide();
        }

        ListView CreateCardListView()
        {
            var cards = rootVisualElement.Q<ListView>("cards-root-listview");
            cards.Q<ScrollView>().verticalScrollerVisibility = ScrollerVisibility.Hidden;
            cards.itemsSource = m_Cards;
            cards.makeItem = () =>
            {
                var label = new BuildProfileListLabel();
                label.AddToClassList("pl-small");
                return label;
            };
            cards.bindItem = (VisualElement element, int index) =>
            {
                if (element is not BuildProfileListLabel card)
                {
                    Debug.LogWarning("Unexpected element type in PlatformDiscoveryWindow. element=" + element);
                    return;
                }

                var cardDescription = m_Cards[index];
                card.Set(
                    cardDescription.displayName,
                    BuildProfileModuleUtil.GetPlatformIconSmall(cardDescription.platformId));
            };
            cards.selectedIndicesChanged += (indices) =>
            {
                using var index = indices.GetEnumerator();
                if (!index.MoveNext())
                    return;

                OnCardSelected(m_Cards[index.Current]);
            };
            return cards;
        }

        /// <summary>
        /// Creates a build profile assets based on the selected card.
        /// </summary>
        void OnAddBuildProfileClicked(BuildProfileCard card)
        {
            BuildProfileDataSource.CreateNewAsset(card.platformId, card.displayName);
            EditorAnalytics.SendAnalytic(new BuildProfileCreatedEvent(new BuildProfileCreatedEvent.Payload
            {
                creationType = BuildProfileCreatedEvent.CreationType.PlatformBrowser,
                platformId = card.platformId,
                platformDisplayName = card.displayName,
            }));

            // Scan list view packages.
            List<string> packagesToAdd = new List<string>();
            foreach(PlatformPackageEntry item in m_PackagesListView.itemsSource)
            {
                if (!item.isInstalled && item.shouldInstalled)
                    packagesToAdd.Add(item.packageName);
            }
            if (packagesToAdd.Count > 0)
               PackageManager.Client.AddAndRemove(packagesToAdd.ToArray());
        }

        static BuildProfileCard[] FindAllVisiblePlatforms()
        {
            var cards = new List<BuildProfileCard>();
            foreach (var platformId in BuildProfileModuleUtil.FindAllViewablePlatforms())
            {
                if (!BuildProfileModuleUtil.IsPlatformAvailableOnHostPlatform(platformId, SystemInfo.operatingSystemFamily))
                    continue;

                var requiredPackageNames = BuildProfileModuleUtil.BuildPlatformRequiredPackages(platformId);
                var recommendedPackageNames = BuildProfileModuleUtil.BuildPlatformRecommendedPackages(platformId);

                cards.Add(new BuildProfileCard()
                {
                    displayName = BuildProfileModuleUtil.GetClassicPlatformDisplayName(platformId),
                    platformId = platformId,
                    description = BuildProfileModuleUtil.BuildPlatformDescription(platformId),
                    recommendedPackages = recommendedPackageNames,
                    requiredPackages = requiredPackageNames
                });
            }
            return cards.ToArray();
        }
    }
}
