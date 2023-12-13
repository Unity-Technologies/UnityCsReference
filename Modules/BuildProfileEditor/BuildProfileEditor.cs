// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Modules;
using UnityEditor.Build.Profile.Elements;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile
{
    [CustomEditor(typeof(BuildProfile))]
    internal class BuildProfileEditor : Editor
    {
        const string k_Uxml = "BuildProfile/UXML/BuildProfileEditor.uxml";
        const string k_PlatformSettingPropertyName = "m_PlatformBuildProfile";
        const string k_PlatformWarningHelpBox = "platform-warning-help-box";
        const string k_BuildSettingsLabel = "build-settings-label";
        const string k_PlatformSettingsBaseRoot = "platform-settings-base-root";
        const string k_BuildDataLabel = "build-data-label";
        const string k_SharedSettingsInfoHelpbox = "shared-settings-info-helpbox";
        const string k_SharedSettingsInfoHelpboxButton = "shared-settings-info-helpbox-button";
        const string k_SceneListFoldout = "scene-list-foldout";
        const string k_SceneListFoldoutRoot = "scene-list-foldout-root";
        const string k_SceneListFoldoutClassicSection = "scene-list-foldout-classic-section";
        const string k_SceneListFoldoutClassicButton = "scene-list-foldout-classic-button";

        private BuildProfileSceneList m_SceneList;

        public BuildProfileWindow parent { get; set; }

        public BuildProfileWorkflowState parentState { get; set; }

        public BuildProfileWorkflowState editorState { get; set; }

        IBuildProfileExtension m_PlatformExtension = null;

        public BuildProfileEditor()
        {
            editorState = new BuildProfileWorkflowState((next) => parentState?.Apply(next));
        }

        public override VisualElement CreateInspectorGUI()
        {
            if (serializedObject.targetObject is not BuildProfile profile)
            {
                throw new InvalidOperationException("Editor object is not of type BuildProfile.");
            }

            CleanupEventHandlers();

            var root = new VisualElement();
            var visualTree = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var windowUss = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
            visualTree.CloneTree(root);
            root.styleSheets.Add(windowUss);

            var noModuleFoundHelpBox = root.Q<HelpBox>(k_PlatformWarningHelpBox);
            var buildSettingsLabel = root.Q<Label>(k_BuildSettingsLabel);
            var platformSettingsBaseRoot = root.Q<VisualElement>(k_PlatformSettingsBaseRoot);
            var buildDataLabel = root.Q<Label>(k_BuildDataLabel);
            var sharedSettingsInfoHelpBox = root.Q<HelpBox>(k_SharedSettingsInfoHelpbox);

            buildSettingsLabel.text = TrText.buildSettings;
            buildDataLabel.text = TrText.buildData;
            sharedSettingsInfoHelpBox.text = TrText.sharedSettingsInfo;

            AddSceneList(root, profile);

            if (!BuildProfileContext.IsClassicPlatformProfile(profile))
                sharedSettingsInfoHelpBox.Hide();
            else
            {
                var button = root.Q<Button>(k_SharedSettingsInfoHelpboxButton);
                button.text = TrText.addBuildProfile;
                button.clicked += parent.DuplicateSelectedClassicProfile;
            }

            bool hasErrors = Util.UpdatePlatformRequirementsWarningHelpBox(noModuleFoundHelpBox, profile.moduleName, profile.subtarget);
            if (hasErrors)
                return root;

            ShowPlatformSettings(profile, platformSettingsBaseRoot);
            return root;
        }

        /// <summary>
        /// Create modified GUI for shared setting amongst classic platforms.
        /// </summary>
        public VisualElement CreateLegacyGUI()
        {
            CleanupEventHandlers();

            var root = new VisualElement();
            var visualTree = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var windowUss = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
            visualTree.CloneTree(root);
            root.styleSheets.Add(windowUss);

            root.Q<HelpBox>(k_PlatformWarningHelpBox).Hide();
            root.Q<Label>(k_BuildSettingsLabel).Hide();
            root.Q<VisualElement>(k_PlatformSettingsBaseRoot).Hide();
            root.Q<Button>(k_SharedSettingsInfoHelpboxButton).Hide();

            var sharedSettingsHelpbox = root.Q<HelpBox>(k_SharedSettingsInfoHelpbox);
            sharedSettingsHelpbox.text = TrText.sharedSettingsSectionInfo;

            var sectionLabel = root.Q<Label>(k_BuildDataLabel);
            sectionLabel.AddToClassList("text-large");
            sectionLabel.text = TrText.sceneList;

            AddSceneList(root);
            return root;
        }

        public void OnDisable()
        {
            CleanupEventHandlers();

            if (m_PlatformExtension != null)
                m_PlatformExtension.OnDisable();
        }

        void ShowPlatformSettings(BuildProfile profile, VisualElement platformSettingsBaseRoot)
        {
            var platformProperties = serializedObject.FindProperty(k_PlatformSettingPropertyName);
            m_PlatformExtension = BuildProfileModuleUtil.GetBuildProfileExtension(profile.buildTarget);
            if (m_PlatformExtension != null)
            {
                var settings = m_PlatformExtension.CreateSettingsGUI(
                    serializedObject, platformProperties, editorState);
                platformSettingsBaseRoot.Add(settings);
            }
        }

        void AddSceneList(VisualElement root, BuildProfile profile = null)
        {
            // On no build profile show EditorBuildSetting scene list,
            // classic platforms show read-only version.
            bool isGlobalSceneList = profile == null;
            bool isClassicPlatform = !isGlobalSceneList && BuildProfileContext.IsClassicPlatformProfile(profile);
            bool isEnable = isGlobalSceneList || !isClassicPlatform;

            var sceneListFoldout = root.Q<Foldout>(k_SceneListFoldout);
            sceneListFoldout.text = TrText.sceneList;
            m_SceneList = (isGlobalSceneList || isClassicPlatform)
                ? new BuildProfileSceneList()
                : new BuildProfileSceneList(profile);
            Undo.undoRedoEvent += m_SceneList.OnUndoRedo;
            var container = m_SceneList.GetSceneListGUI(sceneListFoldout, isEnable);
            container.SetEnabled(isEnable);
            root.Q<VisualElement>(k_SceneListFoldoutRoot).Add(container);

            if (isClassicPlatform)
            {
                // Bind Global Scene List button
                root.Q<VisualElement>(k_SceneListFoldoutClassicSection).Show();
                var globalSceneListButton = root.Q<Button>(k_SceneListFoldoutClassicButton);
                globalSceneListButton.text = TrText.openSceneList;
                globalSceneListButton.clicked += () =>
                {
                    parent.OnClassicSceneListSelected();
                };
            }
        }

        void CleanupEventHandlers()
        {
            if (m_SceneList is not null)
                Undo.undoRedoEvent -= m_SceneList.OnUndoRedo;
        }
    }
}
