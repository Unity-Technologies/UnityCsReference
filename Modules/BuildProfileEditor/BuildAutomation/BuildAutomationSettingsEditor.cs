// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile
{
    /// <summary>
    /// Editor class responsible for visualizing build automation settings.
    /// </summary>
    internal class BuildAutomationSettingsEditor : VisualElement
    {
        const string k_Uxml = "BuildProfile/UXML/BuildAutomationSettings.uxml";
        const string k_StyleSheet = "BuildProfile/StyleSheets/BuildAutomation.uss";

        const string k_BuildAutomationRoot = "editor-build-automation-settings";
        const string k_BuildAutomationErrorBox = "custom-build-automation-info-errorbox";
        const string k_BuildAutomationHelpBox = "custom-build-automation-info-helpbox";
        const string k_BuildAutomationHelpBoxButton = "custom-build-automation-info-helpbox-button";
        const string k_BuildAutomationLabel = "build-automation-label";
        const string k_BuildAutomationOptions = "build-automation-options";
        const string k_BuildAutomationPackageName = "com.unity.services.cloud-build";

        private static readonly string buildAutomationLabelText = L10n.Tr("Build Automation Settings");
        private static readonly GUIContent buildAutomationRemove = EditorGUIUtility.TrTextContent("Remove Build Automation Settings");
        private static readonly string removeBuildAutomationDialogTitle = L10n.Tr("Remove Build Automation Settings");
        private static readonly string removeBuildAutomationDialogMessage = L10n.Tr("This will remove all Build Automation settings");
        private static readonly string BuildAutomationInfo = L10n.Tr("Add Build Automation settings in order to build your project in the cloud");
        private static readonly string addBuildAutomationSettingsButton = L10n.Tr("Add Build Automation Settings");
        private static readonly string BuildAutomationError = L10n.Tr("Build Automation Settings failed to load");
        private static readonly string cancelButtonText = L10n.Tr("Cancel");
        private static readonly string continueButtonText = L10n.Tr("Continue");

        VisualElement m_BuildAutomationRoot;
        Label m_BuildAutomationLabel;

        Button m_BuildAutomationInfoButton;
        Button m_BuildAutomationOptions;

        HelpBox m_BuildAutomationHelpBox;
        HelpBox m_BuildAutomationErrorBox;

        BuildProfile m_Profile;
        BuildAutomationSettings m_BuildAutomationSettings;
        Editor m_BuildAutomationSettingsEditor;
        VisualElement m_BuildAutomationInspector;

        internal static BuildAutomationSettingsEditor CreateBuildAutomationUI(BuildProfile buildProfile)
        {
            var buildProfileBuildAutomationEditor = new BuildAutomationSettingsEditor();
            var visualTree = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var windowUss = EditorGUIUtility.LoadRequired(k_StyleSheet) as StyleSheet;
            visualTree.CloneTree(buildProfileBuildAutomationEditor);
            buildProfileBuildAutomationEditor.styleSheets.Add(windowUss);

            buildProfileBuildAutomationEditor.InitializeVisualElements();

            var buildAutomationPackageInfo = PackageManager.PackageInfo.FindForPackageName(k_BuildAutomationPackageName);
            if (buildProfile == null || buildAutomationPackageInfo == null)
            {
                buildProfileBuildAutomationEditor.HideBuildAutomationUI();
                return buildProfileBuildAutomationEditor;
            }

            buildProfileBuildAutomationEditor.m_Profile = buildProfile;
            buildProfileBuildAutomationEditor.m_BuildAutomationLabel.text = buildAutomationLabelText;
            buildProfileBuildAutomationEditor.m_BuildAutomationSettings =
                GetSubAssetFromBuildProfile(buildProfileBuildAutomationEditor.m_Profile);

            if (buildProfileBuildAutomationEditor.m_BuildAutomationSettings != null)
                buildProfileBuildAutomationEditor.ShowBuildAutomationEditor();
            else
                buildProfileBuildAutomationEditor.ShowBuildAutomationHelpBox();

            return buildProfileBuildAutomationEditor;
        }

        private void InitializeVisualElements()
        {
            m_BuildAutomationRoot = this.Q<VisualElement>(k_BuildAutomationRoot);
            m_BuildAutomationErrorBox = m_BuildAutomationRoot.Q<HelpBox>(k_BuildAutomationErrorBox);
            m_BuildAutomationHelpBox = m_BuildAutomationRoot.Q<HelpBox>(k_BuildAutomationHelpBox);
            m_BuildAutomationInfoButton = m_BuildAutomationRoot.Q<Button>(k_BuildAutomationHelpBoxButton);
            m_BuildAutomationLabel = m_BuildAutomationRoot.Q<Label>(k_BuildAutomationLabel);
            m_BuildAutomationOptions = m_BuildAutomationRoot.Q<Button>(k_BuildAutomationOptions);

            m_BuildAutomationRoot.Show();
        }

        /// <summary>
        /// Return a sub asset of the specified type, or default if one doesn't exist
        /// </summary>
        internal static BuildAutomationSettings GetSubAssetFromBuildProfile(BuildProfile buildProfile)
        {
            var assetPath = AssetDatabase.GetAssetPath(buildProfile);
            var subObjects = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var subObject in subObjects)
            {
                if (subObject == null)
                    continue;

                if (subObject is BuildAutomationSettings settings)
                    return settings;
            }
            return default;
        }

        /// <summary>
        /// Creates a BuildAutomationSettings sub asset for the specified BuildProfile.
        /// </summary>
        internal static BuildAutomationSettings AddBuildAutomationSettings(BuildProfile buildProfile)
        {
            var subAsset = ScriptableObject.CreateInstance<BuildAutomationSettings>();
            subAsset.name = "BuildAutomationSettings";
            var (buildTarget, _) = BuildProfileModuleUtil.GetBuildTargetAndSubtarget(buildProfile.platformGuid);
            subAsset.buildTarget = buildTarget;
            AssetDatabase.AddObjectToAsset(subAsset, buildProfile);
            return subAsset;
        }

        private void HideBuildAutomationUI()
        {
            m_BuildAutomationLabel.Hide();
            m_BuildAutomationHelpBox.Hide();
            m_BuildAutomationInfoButton.Hide();
            m_BuildAutomationErrorBox.Hide();
            HideBuildAutomationEditor();
        }

        private void ShowBuildAutomationEditor()
        {
            HideBuildAutomationHelpBox();

            if (m_BuildAutomationSettings == null)
            {
                m_BuildAutomationSettings = AddBuildAutomationSettings(m_Profile);
            }

            try
            {
                if (m_BuildAutomationSettingsEditor == null)
                {
                    m_BuildAutomationSettingsEditor = Editor.CreateEditor(m_BuildAutomationSettings);
                }

                if (m_BuildAutomationInspector == null)
                {
                    m_BuildAutomationInspector = new InspectorElement(m_BuildAutomationSettingsEditor);
                    m_BuildAutomationRoot.Add(m_BuildAutomationInspector);
                }

                m_BuildAutomationErrorBox.Hide();
            }
            catch (Exception e)
            {
                m_BuildAutomationErrorBox.Show();
                m_BuildAutomationErrorBox.text = $"{BuildAutomationError}: {e.Message}";
                Debug.LogException(e);
            }

            m_BuildAutomationOptions.clicked += BuildAutomationOptionMenu;
            m_BuildAutomationOptions.Show();
        }

        private void HideBuildAutomationEditor()
        {
            if (m_BuildAutomationSettingsEditor != null)
            {
                UnityEngine.Object.DestroyImmediate(m_BuildAutomationSettingsEditor);
            }

            if (m_BuildAutomationInspector != null && m_BuildAutomationRoot.Contains(m_BuildAutomationInspector))
            {
                m_BuildAutomationRoot.Remove(m_BuildAutomationInspector);
            }

            m_BuildAutomationSettingsEditor = null;
            m_BuildAutomationInspector = null;
            m_BuildAutomationOptions.clicked -= BuildAutomationOptionMenu;
            m_BuildAutomationOptions.Hide();
            m_BuildAutomationErrorBox.Hide();
        }

        private void ShowBuildAutomationHelpBox()
        {
            m_BuildAutomationOptions.Hide();
            m_BuildAutomationHelpBox.text = BuildAutomationInfo;
            m_BuildAutomationInfoButton.text = addBuildAutomationSettingsButton;
            m_BuildAutomationInfoButton.clicked += ShowBuildAutomationEditor;
            m_BuildAutomationLabel.Show();
            m_BuildAutomationHelpBox.Show();
            m_BuildAutomationInfoButton.Show();
        }

        private void HideBuildAutomationHelpBox()
        {
            m_BuildAutomationInfoButton.clicked -= ShowBuildAutomationEditor;
            m_BuildAutomationInfoButton.Hide();
            m_BuildAutomationHelpBox.Hide();
        }

        private void BuildAutomationOptionMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(buildAutomationRemove, false, RemoveBuildAutomationSettings);
            menu.ShowAsContext();
        }

        private void RemoveBuildAutomationSettings()
        {
            bool removeBuildAutomation = EditorUtility.DisplayDialog(removeBuildAutomationDialogTitle,
                                                                    removeBuildAutomationDialogMessage,
                                                                    continueButtonText,
                                                                    cancelButtonText);
            if (!removeBuildAutomation)
                return;

            if (m_BuildAutomationSettings != null)
            {
                AssetDatabase.RemoveObjectFromAsset(m_BuildAutomationSettings);
                m_BuildAutomationSettings = null;
            }

            HideBuildAutomationEditor();
            ShowBuildAutomationHelpBox();
        }
    }
}
