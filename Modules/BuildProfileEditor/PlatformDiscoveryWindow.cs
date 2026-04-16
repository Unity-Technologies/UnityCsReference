// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Build.Profile.Elements;
using UnityEditor.Build.Profile.Handlers;
using UnityEditor.Modules;
using UnityEditor.PackageManager.UI;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Application = UnityEngine.Device.Application;
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

        Action m_OnCloseContinuation = null;

        BuildProfileCard[] m_Cards;
        BuildProfileCard m_SelectedCard;
        Image m_SelectedCardImage;
        Label m_SelectedDisplayNameLabel;
        VisualElement m_SelectedDescription;
        VisualElement m_PlatformBrowserHeaderBG;
        Button m_AddBuildProfileButton;
        BuildProfilePlatformBrowserClosed m_CloseEvent;

        VisualElement m_RhsDetailsPanel;

        VisualElement m_PackageContainer;
        ListView m_InternalPackageListView;
        ListView m_PartnerPackageListView;
        Button m_PackageSelectAll;
        Button m_PackageDeselectAll;
        Toggle m_PackageBrowseSampleCheckbox;

        VisualElement m_KeyFeaturesContainer;
        Label m_KeyFeaturesContentLabel;
        VisualElement m_ResourcesContainer;
        Label m_ResourcesContentLabel;
        VisualElement m_PlatformConfigs;
        VisualElement m_NameLinks;

        VisualElement m_SupportedPlatformStatusContainer;

        Label m_SelectedDescriptionLabel;
        Label m_ConfigLabel;
        VisualElement m_ConfigPanel;
        VisualElement m_ConfigHelpBox;
        Label m_AddtionalInfoLabel;
        Label m_BuildProfileNameLabel;

        TextField m_BuildProfileNameTextField;
        List<BuildProfilePlatformGroup> m_PlatformGroups;

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

        public static void ShowWindow(Action onCloseContinuation)
        {
            ShowWindowAndSelectPlatform(null, onCloseContinuation);
        }

        public static void ShowWindowAndSelectPlatform(GUID? platformGuid = null, Action onCloseContinuation = null)
        {
            var window = GetWindow<PlatformDiscoveryWindow>(true, TrText.platformDiscoveryTitle, true);
            window.minSize = new Vector2(900, 500);
            window.m_OnCloseContinuation = onCloseContinuation;

            if (platformGuid != null)
                window.SelectPlatform(platformGuid.Value);
        }

        static void AddSelectedBuildProfiles(
            BuildProfileCard card, string customProfileName,
            string[] packagesToAdd, UnityAction<BuildProfile> onCreate)
        {
            bool wasCallbackRegistered = false;
            bool noneSelected = true;
            for (var ii = 0; ii < card.preconfiguredSettingsVariants.Length; ii++)
            {
                var variant = card.preconfiguredSettingsVariants[ii];
                if (!variant.Selected)
                    continue;

                noneSelected = false;

                if (wasCallbackRegistered)
                {
                    AddSingleBuildProfile(card, customProfileName, variant.Name, ii, packagesToAdd, null);
                }
                else
                {
                    AddSingleBuildProfile(card, customProfileName, variant.Name, ii, packagesToAdd, onCreate);
                    wasCallbackRegistered = true;
                }
            }
            if (noneSelected)
            {
                AddSingleBuildProfile(card, customProfileName, null,
                    -1, packagesToAdd, onCreate);
            }
        }

        static void AddSingleBuildProfile(
            BuildProfileCard card,
            string customProfileName,
            string preconfiguredSettingsVariantName,
            int preconfiguredSettingsVariant,
            string[] packagesToAdd,
            UnityAction<BuildProfile> onCreate)
        {
            BuildProfileModuleUtil.CreateNewAssetWithName(
                card.platformId,
                customProfileName.Trim(),
                preconfiguredSettingsVariantName,
                preconfiguredSettingsVariant,
                packagesToAdd,
                onCreate);
            EditorAnalytics.SendAnalytic(new BuildProfileCreatedEvent(new BuildProfileCreatedEvent.Payload
            {
                creationType = BuildProfileCreatedEvent.CreationType.PlatformBrowser,
                platformId = card.platformId.ToString(),
                platformDisplayName = card.displayName,
            }));
        }

        /// <summary>
        /// Open package manager samples window filtering for any platform packages.
        /// </summary>
        static void AfterBuildProfileCreatedShowSamples(BuildProfile profile)
        {
            var platformId = profile.platformGuid;
            var internalPackages = BuildProfileModuleUtil.BuildPlatformInternalPackages(platformId);
            var partnerPackages = BuildProfileModuleUtil.BuildPlatformPartnerPackages(platformId);

            var packageNames = new List<string>();
            foreach (var pkg in internalPackages.requiredPackages)
            {
                if (PackageManager.PackageInfo.IsPackageRegistered(pkg.qualifiedName))
                    packageNames.Add(pkg.qualifiedName);
            }
            foreach (var pkg in internalPackages.recommendedPackages)
            {
                if (PackageManager.PackageInfo.IsPackageRegistered(pkg.qualifiedName))
                    packageNames.Add(pkg.qualifiedName);
            }
            foreach (var pkg in partnerPackages.requiredPackages)
            {
                if (PackageManager.PackageInfo.IsPackageRegistered(pkg.qualifiedName))
                    packageNames.Add(pkg.qualifiedName);
            }
            foreach (var pkg in partnerPackages.recommendedPackages)
            {
                if (PackageManager.PackageInfo.IsPackageRegistered(pkg.qualifiedName))
                    packageNames.Add(pkg.qualifiedName);
            }
            PackageManagerWindow.OpenSamplesPage(packageNames);
        }

        static BuildProfileCard[] FindAllVisiblePlatforms(GUID[] platforms)
        {
            var cards = new List<BuildProfileCard>();
            foreach (var platformId in platforms)
            {
                if (!BuildProfileModuleUtil.IsPlatformAvailableOnHostPlatform(platformId, SystemInfo.operatingSystemFamily))
                    continue;

                var internalPackages = BuildProfileModuleUtil.BuildPlatformInternalPackages(platformId);
                var partnerPackages = BuildProfileModuleUtil.BuildPlatformPartnerPackages(platformId);

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
                    keyFeatures = BuildProfileModuleUtil.BuildPlatformKeyFeatures(platformId),
                    resources = BuildProfileModuleUtil.BuildPlatformResources(platformId),
                    platformBannerBgColorHex = BuildProfileModuleUtil.GetPlatformColorString(platformId),
                    internalPackages = internalPackages,
                    partnerPackages = partnerPackages,
                    preconfiguredSettingsVariants = preconfiguredSettingsVariants
                });
            }
            return cards.ToArray();
        }

        public void OnDisable()
        {
            m_OnCloseContinuation?.Invoke();
            BuildProfileContext.packageServiceInfoProvider.OnPackageInfoUpdated -= OnPackageInfoUpdated;
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
            m_RhsDetailsPanel = rootVisualElement.Q<VisualElement>("platform-details-panel");
            m_AddBuildProfileButton = rootVisualElement.Q<Button>("add-build-profile-button");
            m_SelectedDisplayNameLabel = rootVisualElement.Q<Label>("selected-card-name");
            m_SelectedCardImage = rootVisualElement.Q<Image>("selected-card-icon");
            m_PlatformBrowserHeaderBG = rootVisualElement.Q<VisualElement>("platform-header-bg");
            m_CardWarningHelpBox = rootVisualElement.Q<HelpBox>(BuildProfileModuleUtil.platformRequirementWarningHelpboxName);
            m_SelectedDescription = rootVisualElement.Q<VisualElement>("platform-description");
            m_SelectedDescriptionLabel = rootVisualElement.Q<Label>("platform-description-label");
            m_HelpBoxWrapper = rootVisualElement.Q<VisualElement>("helpbox-wrapper");
            m_PackageContainer = rootVisualElement.Q<VisualElement>("package-container");
            m_InternalPackageListView = rootVisualElement.Q<ListView>("internal-package-list");
            m_PartnerPackageListView = rootVisualElement.Q<ListView>("partner-package-list");
            m_ConfigHelpBox = rootVisualElement.Q<VisualElement>("platform-config-help-box");
            m_AddtionalInfoLabel = rootVisualElement.Q<Label>("additional-info");
            m_AddtionalInfoLabel.text = "";

            m_NameLinks = rootVisualElement.Q<VisualElement>("name-link-btn-container");

            m_ConfigPanel = rootVisualElement.Q<VisualElement>("platform-config-panel");
            m_ConfigLabel = rootVisualElement.Q<Label>("config-container-title");
            m_ConfigLabel.text = TrText.buildProfileConfigurationLabel;
            m_PlatformConfigs = rootVisualElement.Q<VisualElement>("platform-configs");
            m_BuildProfileNameTextField = rootVisualElement.Q<TextField>("build-profile-name");

            m_BuildProfileNameLabel = rootVisualElement.Q<Label>("build-profile-name-label");
            m_BuildProfileNameLabel.text = TrText.buildProfileNameLabel;

            m_SupportedPlatformStatusContainer = rootVisualElement.Q<VisualElement>("supported-platform-status-container");

            m_KeyFeaturesContainer = rootVisualElement.Q<VisualElement>("platform-key-features-container");
            m_KeyFeaturesContentLabel = rootVisualElement.Q<Label>("platform-key-features-content");
            var keyFeaturesTitle =  m_KeyFeaturesContainer.Q<Label>("platform-key-features-title");
            keyFeaturesTitle.text = TrText.keyFeaturesTitle;

            m_ResourcesContainer = rootVisualElement.Q<VisualElement>("platform-resources-container");
            m_ResourcesContentLabel = rootVisualElement.Q<Label>("platform-resources-content");
            var resourcesTitle =  m_ResourcesContainer.Q<Label>("platform-resources-title");
            resourcesTitle.text = TrText.resourcesTitle;

            m_RenameOverlay = new BuildProfileRenameOverlay(m_BuildProfileNameTextField);

            m_BuildProfileNameTextField.RegisterValueChangedCallback(changeEvent =>
            {
                if (string.IsNullOrEmpty(changeEvent.newValue))
                    return;
                m_RenameOverlay.OnNameChanged(changeEvent.previousValue, changeEvent.newValue);
            });
            m_BuildProfileNameTextField.RegisterCallback<FocusOutEvent>(evt =>
            {
                // truncate the name, just in case UI restrictions don't apply.
                var newProfileName = m_BuildProfileNameTextField.value;
                if (!string.IsNullOrEmpty(newProfileName) &&
                    System.Text.Encoding.UTF8.GetByteCount(newProfileName) > k_MaxBuildProfileNameLength)
                    m_BuildProfileNameTextField.value = BuildProfileModuleUtil.TruncateUtf8StringByBytes(newProfileName, k_MaxBuildProfileNameLength);
                m_RenameOverlay.OnRenameEnd();
            });
            m_BuildProfileNameTextField.maxLength = k_MaxBuildProfileNameLength;

            var packageContainerTitle = m_PackageContainer.Q<Label>("package-container-title");
            packageContainerTitle.text = TrText.packageContainerTitle;

            SetUpPackageListView(m_InternalPackageListView, null);
            SetUpPackageListView(m_PartnerPackageListView, () =>
            {
                var header = new Label();
                header.text = TrText.partnerPackageListTitle;
                header.AddToClassList("partner-package-list-header");
                return header;
            });

            m_PackageSelectAll = m_PackageContainer.Q<Button>("package-select-all");
            m_PackageSelectAll.text = TrText.selectAll;
            m_PackageSelectAll.clicked += () => SetAllPackagesShouldInstallToggle(true);
            m_PackageDeselectAll = m_PackageContainer.Q<Button>("package-deselect-all");
            m_PackageDeselectAll.text = TrText.deselectAll;
            m_PackageDeselectAll.clicked += () => SetAllPackagesShouldInstallToggle(false);
            m_PackageBrowseSampleCheckbox = m_PackageContainer.Q<Toggle>("package-browse-samples-checkbox");
            m_PackageBrowseSampleCheckbox.text = TrText.browseSamplesCheckboxLabel;

            // Apply localized text to static elements.
            rootVisualElement.Q<ToolbarButton>("toolbar-filter-all").text = TrText.all;
            m_AddBuildProfileButton.text = TrText.addBuildProfile;

            // Build dynamic visual elements.
            m_Cards = FindAllVisiblePlatforms(BuildProfileModuleUtil.FindAllViewablePlatforms().ToArray());
            CreatePlatformView();

            if (BuildProfileContext.packageServiceInfoProvider.FetchInfo())
                BuildProfileContext.packageServiceInfoProvider.OnPackageInfoUpdated += OnPackageInfoUpdated;

            // Register event handlers.
            m_AddBuildProfileButton.SetEnabled(true);
            m_AddBuildProfileButton.clicked += () =>
            {
                OnAddBuildProfileClicked(m_SelectedCard, m_BuildProfileNameTextField.value);
                m_CloseEvent = new BuildProfilePlatformBrowserClosed(new BuildProfilePlatformBrowserClosed.Payload()
                {
                    wasProfileCreated = true,
                    platformId = m_SelectedCard.platformId.ToString(),
                    platformDisplayName = m_SelectedCard.displayName,
                });
                Close();
            };

            // First element should match standalone platform.
            OnCardSelected(m_Cards[0]);

            m_RhsDetailsPanel.focusable = true;
            m_RhsDetailsPanel.delegatesFocus = true;
            rootVisualElement.RegisterCallback<NavigationSubmitEvent>(OnFocusToDetailsPanel);
        }

        void OnPackageInfoUpdated()
        {
            OnCardSelected(m_SelectedCard);
        }

        void SetUpPackageListView(ListView listView, Func<VisualElement> customHeader)
        {
            listView.itemsSource = Array.Empty<PlatformPackageEntry>();
            listView.makeItem = PlatformPackageItem.Make;
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            listView.selectionType = SelectionType.None;
            listView.bindItem = (element, index) =>
            {
                if (element is not PlatformPackageItem packageItem)
                {
                    Debug.LogWarning("Unexpected element type in Platform Browser. element=" + element);
                    return;
                }

                if (index == 0)
                    element.Q<VisualElement>("package-list-item").ToggleInClassList("border-top-1-color-10");

                var packageEntry = listView.itemsSource[index] as PlatformPackageEntry;
                packageItem.Set(packageEntry);
            };

            if (customHeader != null)
                listView.makeHeader = customHeader.Invoke;
        }

        /// <summary>
        /// Set the state of the install toggle for all packages in the internal and partner package lists.
        /// </summary>
        void SetAllPackagesShouldInstallToggle(bool selection)
        {
            SetShouldInstallToggle(m_InternalPackageListView);
            SetShouldInstallToggle(m_PartnerPackageListView);

            void SetShouldInstallToggle(ListView packageListView)
            {
                for (int i = 0; i < packageListView.itemsSource.Count; ++i)
                {
                    var item = packageListView.GetRootElementForIndex(i);
                    if (item is not PlatformPackageItem packageItem)
                        continue;

                    packageItem.SetShouldInstallToggle(selection);
                }
            }
        }

        /// <summary>
        /// Sets the enabled state of the Select All and Deselect All buttons
        /// based on the current state of the package lists.
        /// </summary>
        void SetEnabledPackageSelectionButtons()
        {
            var internalPackageList = m_InternalPackageListView.itemsSource as PlatformPackageEntry[];
            var partnerPackageList = m_PartnerPackageListView.itemsSource as PlatformPackageEntry[];
            if (internalPackageList == null || partnerPackageList == null)
            {
                Debug.LogWarning("Could not parse lists of packages in Platform Browser");
                return;
            }

            var allPackagesAreInstalledOrRequired = AreAllPackagesInstalledOrRequired(internalPackageList) &&
                AreAllPackagesInstalledOrRequired(partnerPackageList);

            m_PackageDeselectAll.SetEnabled(!allPackagesAreInstalledOrRequired);
            m_PackageSelectAll.SetEnabled(!allPackagesAreInstalledOrRequired);

            bool AreAllPackagesInstalledOrRequired(PlatformPackageEntry[] platformPackageEntries)
            {
                foreach (var package in platformPackageEntries)
                {
                    if (!package.isInstalled && !package.required)
                        return false;
                }
                return true;
            }
        }

        void SelectPlatform(GUID platformGuid)
        {
            for (var index = 0; index < m_Cards.Length; index++)
            {
                var card = m_Cards[index];
                if (card.platformId == platformGuid)
                {
                    OnCardSelected(card);
                    break;
                }
            }
        }

        void ClearWindowData()
        {
            m_PlatformConfigs.Clear();
            m_ConfigHelpBox.Clear();
            m_NameLinks.Clear();
            m_AddtionalInfoLabel.Clear();
            m_InternalPackageListView.itemsSource = Array.Empty<PlatformPackageEntry>();
            m_PartnerPackageListView.itemsSource = Array.Empty<PlatformPackageEntry>();
        }

        void OnFocusToDetailsPanel(NavigationSubmitEvent e)
        {
            if (e.target is BuildProfilePlatformElement)
            {
                m_RhsDetailsPanel.Focus();
                e.StopPropagation();
            }
        }

        /// <summary>
        /// Verify editor can create build profiles for the currently selected card. Otherwise
        /// display relevant documentation to the user.
        /// </summary>
        internal void OnCardSelected(BuildProfileCard card)
        {
            ClearWindowData();
            m_BuildProfileNameTextField.value = BuildProfileDataSource.SanitizeFileName(card.displayName);
            m_SelectedCard = card;
            SetCardSelected(card);
            m_SelectedDisplayNameLabel.text = card.displayName;
            m_SelectedCardImage.image = BuildProfileModuleUtil.GetPlatformIconHero(card.platformId);
            m_AddtionalInfoLabel.text = BuildProfileModuleUtil.GetSubtitle(card.platformId);

            // Only color if not a default color and color can be parsed.
            if (!card.platformBannerBgColorHex.Equals("#00000000") && ColorUtility.TryParseHtmlString(card.platformBannerBgColorHex, out Color bgColor))
                m_PlatformBrowserHeaderBG.style.backgroundColor = bgColor;
            else
                m_PlatformBrowserHeaderBG.style.backgroundColor = StyleKeyword.Null;

            m_SupportedPlatformStatusContainer.Hide();
            if (Util.UpdatePlatformRequirementsWarningHelpBox(m_CardWarningHelpBox, card.platformId)
                && !string.IsNullOrEmpty(m_CardWarningHelpBox.text))
                m_HelpBoxWrapper.Show();
            else
            {
                m_HelpBoxWrapper.Hide();

                var supportedPlatformHelpBox = m_SupportedPlatformStatusContainer.Q<HelpBox>("supported-platform-status-helpbox");
                if (Util.UpdateSupportedPlatformStatusHelpBox(supportedPlatformHelpBox, card.platformId))
                    m_SupportedPlatformStatusContainer.Show();
            }

            if (card.internalPackages.packageCount > 0 || card.partnerPackages.packageCount > 0)
            {
                m_PackageContainer.Show();
                RebuildPackageListView(card.internalPackages, m_InternalPackageListView);
                RebuildPackageListView(card.partnerPackages, m_PartnerPackageListView);
                SetEnabledPackageSelectionButtons();

                void RebuildPackageListView(BuildTargetDiscovery.PlatformPackageList packageList, ListView listView)
                {
                    if (packageList.packageCount > 0)
                    {
                        listView.itemsSource = PlatformPackageItem.CreateItemSource(packageList);
                        listView.Show();
                        listView.Rebuild();
                    }
                    else
                        listView.Hide();
                }
            }
            else
            {
                m_PackageContainer.Hide();
            }

            if (!BuildProfileModuleUtil.HasSamplesInPackageManager(card.platformId))
            {
                m_PackageBrowseSampleCheckbox.value = false;
                m_PackageBrowseSampleCheckbox.Hide();
            }
            else
            {
                m_PackageBrowseSampleCheckbox.value = true;
                m_PackageBrowseSampleCheckbox.Show();
            }

            if (card.description.Length > 0)
            {
                m_SelectedDescriptionLabel.text = card.description;
                m_SelectedDescription.Show();
            }
            else
                m_SelectedDescription.Hide();

            if (card.keyFeatures.Length > 0)
            {
                m_KeyFeaturesContentLabel.text = card.keyFeatures;
                m_KeyFeaturesContainer.Show();
            }
            else
                m_KeyFeaturesContainer.Hide();

            if (card.resources.Length > 0)
            {
                m_ResourcesContentLabel.text = card.resources;
                m_ResourcesContainer.Show();
            }
            else
                m_ResourcesContainer.Hide();

            if (card.preconfiguredSettingsVariants.Length > 0)
            {
                // Display preconfig items with tooltip.
                for (int i = 0; i < card.preconfiguredSettingsVariants.Length; i++)
                {
                    var preconfigItem = new PreconfiguredSettingsItem();
                    preconfigItem.Set(card.preconfiguredSettingsVariants[i], UpdateAddBuildProfileButton, card.preconfiguredSettingsVariants[i].Tooltip);
                    m_PlatformConfigs.Insert(i, preconfigItem);
                }
                m_ConfigLabel.Show();
                m_ConfigPanel.Show();

                var preconfigDocLink = BuildProfileModuleUtil.GetPlatformSettingsDocsLink(card.platformId);
                if (preconfigDocLink != null)
                {
                    Button btn = new Button();
                    btn.iconImage = BuildProfileModuleUtil.GetHelpIcon();
                    string link = preconfigDocLink;
                    btn.clicked += () => { Application.OpenURL(link); };
                    btn.AddToClassList("link-button");
                    m_ConfigHelpBox.Add(btn);
                }
            }
            else
            {
                m_ConfigLabel.Hide();
                m_ConfigPanel.Hide();
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

        void CreatePlatformView()
        {
            var allGroups = BuildProfileModuleUtil.GetAllPlatformGroups();
            var platformContainer = rootVisualElement.Q<VisualElement>("platforms-list-container");
            m_PlatformGroups = new List<BuildProfilePlatformGroup>();

            int i = 0;
            foreach (var platformGroup in allGroups)
            {
                BuildProfileCard[] cards = FindAllVisiblePlatforms(platformGroup.platforms);

                if (cards.Length > 0)
                {
                    var item = new BuildProfilePlatformGroup(this);
                    item.Set(platformGroup.groupName, cards);
                    item.AddToClassList("platform-group");
                    platformContainer.Insert(i, item);
                    m_PlatformGroups.Add(item);
                    i++;
                }
            }
        }

        void SetCardSelected(BuildProfileCard card)
        {
            m_PlatformGroups.ForEach(group => group.SetCardSelected(card));
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
        void OnAddBuildProfileClicked(BuildProfileCard card, string customProfileName)
        {
            var packagesToAdd = DeterminePackagesToAdd();
            bool shouldOpenSample = m_PackageBrowseSampleCheckbox.value;
            if (shouldOpenSample)
                AddSelectedBuildProfiles(card, customProfileName, packagesToAdd, AfterBuildProfileCreatedShowSamples);
            else
                AddSelectedBuildProfiles(card, customProfileName, packagesToAdd, null);
        }

        string[] DeterminePackagesToAdd()
        {
            List<string> packagesToAdd = new();
            SetPackagesToAdd(m_InternalPackageListView);
            SetPackagesToAdd(m_PartnerPackageListView);

            return packagesToAdd.ToArray();

            void SetPackagesToAdd(ListView listView)
            {
                foreach (PlatformPackageEntry item in listView.itemsSource)
                {
                    if (!item.isInstalled && item.shouldInstalled)
                        packagesToAdd.Add(item.qualifiedName);
                }
            }
        }
    }
}
