// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.Build.Profile.Elements;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Build Settings window in 'File > Build Settings'.
    /// Handles creating and editing of <see cref="BuildProfile"/> assets.
    ///
    /// TODO EPIC: https://jira.unity3d.com/browse/PLAT-5878
    /// </summary>
    [EditorWindowTitle(title = "Build Settings")]
    internal class BuildProfileWindow : EditorWindow
    {
        const string k_Uxml = "BuildProfile/UXML/BuildProfileWindow.uxml";

        internal const string buildProfileClassicPlatformVisualElement = "build-profile-classic-platforms";
        internal const string buildProfileInspectorVisualElement = "build-profile-editor-inspector";

        internal Editor buildProfileEditor;

        /// <summary>
        /// Left column classic profile list view.
        /// </summary>
        ListView m_BuildProfileClassicPlatformListView;

        /// <summary>
        /// Build Profile inspector for the selected classic platform or profile,
        /// repainted on <see cref="m_BuildProfileClassicPlatformListView"/> selection change.
        /// </summary>
        /// <see cref="OnClassicPlatformSelected"/>
        ScrollView m_BuildProfileInspectorElement;
        Image m_SelectedProfileImage;
        Label m_SelectedProfileNameLabel;
        Label m_SelectedProfilePlatformLabel;

        Button m_BuildButton;

        [UsedImplicitly, RequiredByNativeCode]
        public static void ShowBuildProfileWindow()
        {
            var window = GetWindow<BuildProfileWindow>(TrText.buildProfilesName);
            window.minSize = new Vector2(640, 400);
        }

        public void CreateGUI()
        {
            var windowUxml = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var windowUss = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
            rootVisualElement.styleSheets.Add(windowUss);
            windowUxml.CloneTree(rootVisualElement);

            // Capture static visual element reference. TODO
            m_SelectedProfileImage = rootVisualElement.Q<Image>("selected-profile-image");
            m_SelectedProfileNameLabel = rootVisualElement.Q<Label>("selected-profile-name");
            m_SelectedProfilePlatformLabel = rootVisualElement.Q<Label>("selected-profile-platform");
            m_BuildButton = rootVisualElement.Q<Button>("build-button");

            // Apply localized text to static elements.
            rootVisualElement.Q<Label>("platforms-label").text = TrText.classicPlatforms;
            m_BuildButton.text = TrText.build;

            // Build dynamic visual elements.
            m_BuildProfileInspectorElement = rootVisualElement.Q<ScrollView>(buildProfileInspectorVisualElement);
            CreateClassicPlatformList(BuildProfileContext.instance.classicPlatformProfiles);

            // TODO: Temporarily allow showing legacy window.
            // jira: https://jira.unity3d.com/browse/PLAT-7025
            m_BuildButton.clicked += BuildPlayerWindow.ShowBuildPlayerWindow;
        }

        void CreateClassicPlatformList(IList<BuildProfile> itemSource)
        {
            m_BuildProfileClassicPlatformListView = rootVisualElement.Q<ListView>(buildProfileClassicPlatformVisualElement);
            m_BuildProfileClassicPlatformListView.selectionType = SelectionType.Single;
            m_BuildProfileClassicPlatformListView.Q<ScrollView>().verticalScrollerVisibility = ScrollerVisibility.Hidden;
            m_BuildProfileClassicPlatformListView.itemsSource = new List<BuildProfile>(itemSource);
            m_BuildProfileClassicPlatformListView.makeItem = () => new BuildProfileListLabel();
            m_BuildProfileClassicPlatformListView.bindItem = (VisualElement element, int index) =>
            {
                var profile = itemSource[index];
                var buildDefinitionLabel = element as BuildProfileListLabel;
                UnityEngine.Assertions.Assert.IsNotNull(profile, "Build definition is null");
                UnityEngine.Assertions.Assert.IsNotNull(buildDefinitionLabel, "Build definition label is null");

                string platformDisplayName = BuildProfileModuleUtil.GetClassicPlatformDisplayName(profile);
                Texture2D icon = BuildProfileModuleUtil.GetPlatformIcon(profile.buildTarget, profile.subtarget);
                buildDefinitionLabel.Set(platformDisplayName, icon);
            };
            m_BuildProfileClassicPlatformListView.selectionChanged += OnClassicPlatformSelected;

            // On load, automatically select the first available platform.
            if (itemSource.Count > 0)
            {
                m_BuildProfileClassicPlatformListView.selectedIndex = 0;
            }
        }

        void OnClassicPlatformSelected(IEnumerable<object> selectedItems)
        {
            using var item = selectedItems.GetEnumerator();
            if (!item.MoveNext())
                return;

            if (item.Current is not BuildProfile profile)
            {
                Debug.LogWarning("BuildProfileWindow: selected item is not a BuildProfile? item=" + item.Current);
                return;
            }

            m_SelectedProfileImage.image = BuildProfileModuleUtil.GetPlatformIcon(profile.buildTarget, profile.subtarget);
            m_SelectedProfileNameLabel.text = BuildProfileModuleUtil.GetClassicPlatformDisplayName(profile);
            m_SelectedProfilePlatformLabel.style.display = DisplayStyle.None;

            // Rebuild the BuildProfile inspector, targeting the newly selected BuildProfile.
            DestroyImmediate(buildProfileEditor);
            buildProfileEditor = Editor.CreateEditor(profile, typeof(BuildProfileEditor));
            m_BuildProfileInspectorElement.Clear();
            m_BuildProfileInspectorElement.Add(buildProfileEditor.CreateInspectorGUI());
        }

    }
}
