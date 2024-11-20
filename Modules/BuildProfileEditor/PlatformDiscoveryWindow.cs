// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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

        /// <summary>
        /// Warning message displayed when the selected card's platform
        /// is not supported.
        /// </summary>
        HelpBox m_CardWarningHelpBox;

        public static void ShowWindow()
        {
            ShowWindowAndSelectPlatform();
        }

        public static void ShowWindowAndSelectPlatform(string platformId = null)
        {
            var window = GetWindow<PlatformDiscoveryWindow>(true, TrText.platformDiscoveryTitle, true);
            window.minSize = new Vector2(900, 500);

            if (platformId != null)
                window.SelectPlatform(platformId);
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

        void SelectPlatform(string platformId)
        {
            for (var index = 0; index < m_Cards.Length; index++)
            {
                var card = m_Cards[index];
                if (card.platformId == platformId)
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
            m_SelectedCard = card;
            m_SelectedDisplayNameLabel.text = card.displayName;
            m_SelectedCardImage.image = BuildProfileModuleUtil.GetPlatformIcon(card.platformId);
            Util.UpdatePlatformRequirementsWarningHelpBox(m_CardWarningHelpBox, card.platformId);

            string description = BuildProfileModuleUtil.GetPlatformDescription(card.platformId);
            if (string.IsNullOrEmpty(description))
                m_SelectedDescriptionFoldout.Hide();
            else
            {
                m_SelectedDescriptionFoldout.Show();
                m_SelectedDescriptionLabel.text = description;
            }
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
        static void OnAddBuildProfileClicked(BuildProfileCard card)
        {
            BuildProfileDataSource.CreateNewAsset(card.platformId, card.displayName);
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
                if (!BuildProfileModuleUtil.IsPlatformAvailableOnHostPlatform(new GUID(platformId), SystemInfo.operatingSystemFamily))
                    continue;

                cards.Add(new BuildProfileCard()
                {
                    displayName = BuildProfileModuleUtil.GetClassicPlatformDisplayName(platformId),
                    platformId = platformId
                });
            }
            return cards.ToArray();
        }
    }
}
