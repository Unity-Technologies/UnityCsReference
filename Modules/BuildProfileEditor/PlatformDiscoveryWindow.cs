// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.Build.Profile.Elements;
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
        const string k_AssetFolderPath = "Assets/Settings/BuildProfiles";

        BuildProfileCard[] m_Cards;
        BuildProfileCard m_SelectedCard;
        Image m_SelectedCardImage;
        Label m_SelectedDisplayNameLabel;
        Button m_AddBuildProfileButton;

        /// <summary>
        /// Warning message displayed when the selected card's platform
        /// is not supported.
        /// </summary>
        HelpBox m_CardWarningHelpBox;

        public static void ShowWindow()
        {
            var window = GetWindow<PlatformDiscoveryWindow>(true, TrText.platformDiscoveryTitle, true);
            window.minSize = new Vector2(900, 500);
        }

        public void CreateGUI()
        {
            var windowUxml = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var windowUss = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
            rootVisualElement.styleSheets.Add(windowUss);
            windowUxml.CloneTree(rootVisualElement);

            // Capture static visual element reference.
            m_AddBuildProfileButton = rootVisualElement.Q<Button>("add-build-profile-button");
            m_SelectedDisplayNameLabel = rootVisualElement.Q<Label>("selected-card-name");
            m_SelectedCardImage = rootVisualElement.Q<Image>("selected-card-icon");
            m_CardWarningHelpBox = rootVisualElement.Q<HelpBox>("helpbox-card-warning");

            // Apply localized text to static elements.
            rootVisualElement.Q<ToolbarButton>("toolbar-filter-all").text = TrText.all;
            m_AddBuildProfileButton.text = TrText.addBuildProfile;

            // Build dynamic visual elements.
            m_Cards = FindAllVisiblePlatforms();
            var cards = CreateCardListView();

            // Register event handlers.
            m_AddBuildProfileButton.SetEnabled(true);
            m_AddBuildProfileButton.clicked += () =>
            {
                OnAddBuildProfileClicked(m_SelectedCard);
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
            m_SelectedCardImage.image = BuildProfileModuleUtil.GetPlatformIcon(card.moduleName, card.subtarget);
            Util.UpdatePlatformRequirementsWarningHelpBox(m_CardWarningHelpBox, card.moduleName, card.subtarget);
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
                    BuildProfileModuleUtil.GetPlatformIcon(cardDescription.moduleName, cardDescription.subtarget));
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
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");

            if (!AssetDatabase.IsValidFolder(k_AssetFolderPath))
                AssetDatabase.CreateFolder("Assets/Settings", "BuildProfiles");

            BuildProfile.CreateInstance(card.moduleName, card.subtarget, GetNewProfileName(card.displayName));
        }

        static BuildProfileCard[] FindAllVisiblePlatforms()
        {
            var cards = new List<BuildProfileCard>();
            foreach (var (moduleName, subtarget) in BuildProfileModuleUtil.FindAllViewablePlatforms())
            {
                cards.Add(new BuildProfileCard()
                {
                    displayName = BuildProfileModuleUtil.GetClassicPlatformDisplayName(moduleName, subtarget),
                    moduleName = moduleName,
                    subtarget = subtarget
                });
            }
            return cards.ToArray();
        }

        static string GetNewProfileName(string displayName) => $"{k_AssetFolderPath}/{displayName}.asset";
    }
}
