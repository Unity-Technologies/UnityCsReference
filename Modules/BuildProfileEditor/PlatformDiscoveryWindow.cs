// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Build.Profile.Elements;
using UnityEditor.Build.Profile.Handlers;
using UnityEditor.Modules;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using Application = UnityEngine.Device.Application;
using UnityEditorInternal;
using Button = UnityEngine.UIElements.Button;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Platform Discovery Window used for creating new Build Profile assets from installed platform modules.
    /// </summary>
    internal class PlatformDiscoveryWindow : EditorWindow
    {
        const string k_Uxml = "BuildProfile/UXML/PlatformDiscoveryWindow.uxml";

        // Asset database says max file name length = 250.
        // Then, the longest variation we have now is " - Desktop - Development" = 24 bytes long.
        // We also need to account for the `.asset` extension (6 bytes)
        // and potentially ` 1`(2,3..) unique name suffix (3 bytes ought to be enough for everyone TM)
        // if suddenly a build profile with such a long name already exists.
        // This makes for 33 bytes worth of overhead, but to be even more safe let's limit the name to 200 during creation.
        // There are checks at a lower level too.
        const int k_MaxBuildProfileNameLength = 200;

        BuildProfileCard[] m_Cards;
        BuildProfileCard m_SelectedCard;
        Image m_SelectedCardImage;
        Label m_SelectedDisplayNameLabel;
        Foldout m_SelectedDescriptionFoldout;

        Button m_AddBuildProfileButton;
        BuildProfilePlatformBrowserClosed m_CloseEvent;

        ListView m_PackagesListView;
        Foldout m_PackagesFoldout;

        VisualElement m_PackageContainer;
        VisualElement m_PlatformConfigs;
        VisualElement m_NameLinks;

        Label m_SelectedDescriptionLabel;
        Label m_ConfigLabel;
        Label m_AddtionalInfoLabel;

        TextField m_BuildProfileNameTextField;

        /// <summary>
        /// Warning message displayed when the selected card's platform
        /// is not supported.
        /// </summary>
        HelpBox m_CardWarningHelpBox;
        VisualElement m_HelpBoxWrapper;
        private BuildProfileRenameOverlay m_RenameOverlay;

        public static void ShowWindow()
        {
            ShowWindowAndSelectPlatform();
        }

        public static void ShowWindowAndSelectPlatform(GUID? platformGuid = null)
        {
            var window = GetWindow<PlatformDiscoveryWindow>(true, TrText.platformDiscoveryTitle, true);
            window.minSize = new Vector2(900, 500);

            if (platformGuid != null)
                window.SelectPlatform(platformGuid.Value);
        }

        public static void AddBuildProfile(GUID platformId)
        {
            var platforms = FindAllVisiblePlatforms();
            foreach (var card in platforms)
            {
                if (card.platformId != platformId)
                {
                    continue;
                }

                AddSelectedBuildProfiles(card, Array.Empty<string>());
            }
        }

        static void AddSelectedBuildProfiles(BuildProfileCard card, string[] packagesToAdd)
        {
            bool noneSelected = true;
            for (var ii = 0; ii < card.preconfiguredSettingsVariants.Length; ii++)
            {
                var variant = card.preconfiguredSettingsVariants[ii];
                if (variant.Selected)
                {
                    AddSingleBuildProfile(card, variant.Name, ii, packagesToAdd);
                    noneSelected = false;
                }
            }
            if (noneSelected)
            {
                AddSingleBuildProfile(card, null, -1, packagesToAdd);
            }
        }

        static void AddSingleBuildProfile(BuildProfileCard card, string preconfiguredSettingsVariantName, int preconfiguredSettingsVariant, string[] packagesToAdd)
        {
            BuildProfileDataSource.CreateNewAssetWithName(card.platformId, card.displayName.Trim(), preconfiguredSettingsVariantName, preconfiguredSettingsVariant, packagesToAdd);
            EditorAnalytics.SendAnalytic(new BuildProfileCreatedEvent(new BuildProfileCreatedEvent.Payload
            {
                creationType = BuildProfileCreatedEvent.CreationType.PlatformBrowser,
                platformId = card.platformId,
                platformDisplayName = card.displayName,
            }));
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

                var buildProfileExtension = BuildProfileModuleUtil.GetBuildProfileExtension(platformId);
                var preconfiguredSettingsVariants = Array.Empty<PreconfiguredSettingsVariant>();
                if (buildProfileExtension != null)
                {
                    var variants = buildProfileExtension.GetPreconfiguredSettingsVariants();
                    if (variants != null)
                    {
                        preconfiguredSettingsVariants = variants;
                    }
                }

                cards.Add(new BuildProfileCard()
                {
                    displayName = BuildProfileModuleUtil.GetClassicPlatformDisplayName(platformId),
                    platformId = platformId,
                    description = BuildProfileModuleUtil.BuildPlatformDescription(platformId),
                    recommendedPackages = recommendedPackageNames,
                    requiredPackages = requiredPackageNames,
                    preconfiguredSettingsVariants = preconfiguredSettingsVariants
                });
            }
            return cards.ToArray();
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
            m_ConfigLabel = rootVisualElement.Q<Label>("config-text");
            m_AddtionalInfoLabel = rootVisualElement.Q<Label>("additional-info");
            m_AddtionalInfoLabel.text = "";

            m_PackageContainer = rootVisualElement.Q<VisualElement>("package-container");
            m_PackagesFoldout = rootVisualElement.Q<Foldout>("packages-foldout");
            m_PackagesFoldout.text = TrText.packagesHeader;
            m_NameLinks = rootVisualElement.Q<VisualElement>("name-link-btn-container");

            m_PlatformConfigs = rootVisualElement.Q<VisualElement>("platform-configs");
            m_BuildProfileNameTextField = rootVisualElement.Q<TextField>("build-profile-name");
            m_RenameOverlay = new BuildProfileRenameOverlay(m_BuildProfileNameTextField);

            m_BuildProfileNameTextField.RegisterValueChangedCallback(changeEvent =>
            {
                if (string.IsNullOrEmpty(changeEvent.newValue))
                    return;
                m_RenameOverlay.OnNameChanged(changeEvent.previousValue, changeEvent.newValue);
            });
            m_BuildProfileNameTextField.RegisterCallback<FocusOutEvent>(evt =>
            {
                m_SelectedCard.displayName = m_BuildProfileNameTextField.value;
                // truncate the name, just in case UI restrictions don't apply.
                if (!string.IsNullOrEmpty(m_SelectedCard.displayName) &&
                    m_SelectedCard.displayName.Length > k_MaxBuildProfileNameLength)
                    m_SelectedCard.displayName = m_SelectedCard.displayName.Substring(0, k_MaxBuildProfileNameLength);
                m_RenameOverlay.OnRenameEnd();
            });
            m_BuildProfileNameTextField.maxLength = k_MaxBuildProfileNameLength;
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
            var packageSelectAll = m_PackageContainer.Q<Button>("package-select-all");
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
            var packageDeselectAll = m_PackageContainer.Q<Button>("package-deselect-all");
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

        void SelectPlatform(GUID platformGuid)
        {
            for (var index = 0; index < m_Cards.Length; index++)
            {
                var card = m_Cards[index];
                if (card.platformId == platformGuid)
                {
                    var cardListView = rootVisualElement.Q<ListView>("cards-root-listview");
                    cardListView.SetSelection(index);
                    break;
                }
            }
        }

        /// <summary>
        /// Verify editor can create build profiles for the currently selected card. Otherwise
        /// display relevant documentation to the user.
        /// </summary>
        void OnCardSelected(BuildProfileCard card)
        {
            m_PlatformConfigs.Clear();
            m_NameLinks.Clear();
            m_AddtionalInfoLabel.Clear();
            m_BuildProfileNameTextField.value = BuildProfileModuleUtil.GetClassicPlatformDisplayName(card.platformId);
            m_SelectedCard = card;
            m_SelectedDisplayNameLabel.text = card.displayName;
            m_SelectedCardImage.image = BuildProfileModuleUtil.GetPlatformIcon(card.platformId);
            m_AddtionalInfoLabel.text = BuildProfileModuleUtil.GetSubtitle(card.platformId);

            if (Util.UpdatePlatformRequirementsWarningHelpBox(m_CardWarningHelpBox, card.platformId))
                m_HelpBoxWrapper.Show();
            else
                m_HelpBoxWrapper.Hide();

            if (card.requiredPackages.Length > 0 || card.recommendedPackages.Length > 0)
            {
                m_PackagesListView.itemsSource = PlatformPackageItem
                    .CreateItemSource(card.requiredPackages, card.recommendedPackages);
                m_PackagesListView.Rebuild();
                m_PackageContainer.Show();
            }
            else
            {
                m_PackageContainer.Hide();
            }

            if (card.description.Length > 0)
            {
                m_SelectedDescriptionLabel.text = card.description;
                m_SelectedDescriptionFoldout.Show();
            }
            else
                m_SelectedDescriptionFoldout.Hide();

            if (card.preconfiguredSettingsVariants.Length > 0)
            {
                // Display preconfig items with tooltip.
                for(int i = 0 ; i < card.preconfiguredSettingsVariants.Length; i++)
                {
                    var preconfigItem = new PreconfiguredSettingsItem();
                    preconfigItem.Set(card.preconfiguredSettingsVariants[i], UpdateAddBuildProfileButton, card.preconfiguredSettingsVariants[i].Tooltip);
                    preconfigItem.style.marginRight = 32;
                    m_PlatformConfigs.Insert(i, preconfigItem);
                }
                m_ConfigLabel.Show();
            }
            else
            {
                m_ConfigLabel.Hide();
            }

            // Displays the buttons with url.
            var nameAndLinkList = BuildProfileModuleUtil.GetPlatformNameLinkList(card.platformId);
            if (nameAndLinkList != null)
            {
                for (int i = 0; i < nameAndLinkList.Count; i++)
                {
                    Button btn = new Button();
                    btn.text = nameAndLinkList[i].name;
                    string linkUrl = nameAndLinkList[i].linkUrl;
                    btn.clicked += () => { Application.OpenURL(linkUrl); };
                    btn.AddToClassList("link-button");
                    btn.AddToClassList("ml-none");
                    if (i > 0)
                    {
                        Label seperator = new Label("|");
                        seperator.style.marginLeft = 2;
                        seperator.style.marginRight = 2;
                        m_NameLinks.Add(seperator);
                    }
                    m_NameLinks.Add(btn);
                }
            }

            UpdateAddBuildProfileButton();
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

        void UpdateAddBuildProfileButton()
        {
            var card = m_SelectedCard;
            var count = 0;
            for (var ii = 0; ii < card.preconfiguredSettingsVariants.Length; ii++)
            {
                var variant = card.preconfiguredSettingsVariants[ii];
                if (variant.Selected)
                {
                    count++;
                }
            }
            m_AddBuildProfileButton.text = (count < 2) ? TrText.addBuildProfile : string.Format(TrText.addBuildProfiles, count);
        }

        /// <summary>
        /// Creates a build profile assets based on the selected card.
        /// </summary>
        void OnAddBuildProfileClicked(BuildProfileCard card)
        {
            var packagesToAdd = DeterminePackagesToAdd();
            AddSelectedBuildProfiles(card, packagesToAdd);
        }

        string[] DeterminePackagesToAdd()
        {
            List<string> packagesToAdd = new();
            foreach (PlatformPackageEntry item in m_PackagesListView.itemsSource)
            {
                if (!item.isInstalled && item.shouldInstalled)
                {
                    packagesToAdd.Add(item.packageName);
                }
            }
            return packagesToAdd.ToArray();
        }
    }
}
