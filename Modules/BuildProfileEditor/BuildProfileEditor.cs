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
        const string k_CompilingWarningHelpBox = "compiling-warning-help-box";
        const string k_VirtualTextureWarningHelpBox = "virtual-texture-warning-help-box";

        BuildProfileSceneList m_SceneList;
        HelpBox m_CompilingWarningHelpBox;
        HelpBox m_VirtualTexturingHelpBox;
        BuildProfile m_Profile;

        public BuildProfileWindow parent { get; set; }

        public BuildProfileWorkflowState parentState { get; set; }

        public BuildProfileWorkflowState editorState { get; set; }

        public BuildProfileWorkflowState platformSettingsState { get; set; }

        internal BuildProfile buildProfile => m_Profile;

        IBuildProfileExtension m_PlatformExtension = null;

        public BuildProfileEditor()
        {
            editorState = new BuildProfileWorkflowState((next) =>
            {
                if (editorState.buildAction == ActionState.Disabled)
                    editorState.buildAndRunAction = ActionState.Disabled;

                parentState?.Apply(next);
            });

            platformSettingsState = new BuildProfileWorkflowState((next) =>
            {
                if (editorState.buildAction == ActionState.Disabled)
                    next.buildAction = ActionState.Disabled;

                if (editorState.buildAndRunAction == ActionState.Disabled || next.buildAction == ActionState.Disabled)
                    next.buildAndRunAction = ActionState.Disabled;

                editorState?.Apply(next);
            });
        }

        public override VisualElement CreateInspectorGUI()
        {
            if (serializedObject.targetObject is not BuildProfile profile)
            {
                throw new InvalidOperationException("Editor object is not of type BuildProfile.");
            }

            m_Profile = profile;

            CleanupEventHandlers();

            var root = new VisualElement();
            var visualTree = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var windowUss = EditorGUIUtility.LoadRequired(Util.k_StyleSheet) as StyleSheet;
            visualTree.CloneTree(root);
            root.styleSheets.Add(windowUss);

            var noModuleFoundHelpBox = root.Q<HelpBox>(k_PlatformWarningHelpBox);
            var platformSettingsLabel = root.Q<Label>(k_BuildSettingsLabel);
            var platformSettingsBaseRoot = root.Q<VisualElement>(k_PlatformSettingsBaseRoot);
            var buildDataLabel = root.Q<Label>(k_BuildDataLabel);
            var sharedSettingsInfoHelpBox = root.Q<HelpBox>(k_SharedSettingsInfoHelpbox);
            m_VirtualTexturingHelpBox = root.Q<HelpBox>(k_VirtualTextureWarningHelpBox);
            m_CompilingWarningHelpBox = root.Q<HelpBox>(k_CompilingWarningHelpBox);

            m_VirtualTexturingHelpBox.text = TrText.invalidVirtualTexturingSettingMessage;
            m_CompilingWarningHelpBox.text = TrText.compilingMessage;

            platformSettingsLabel.text = TrText.platformSettings;
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

            EditorApplication.update += UpdateWarningsAndButtonStatesForActiveProfile;

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
            root.Q<HelpBox>(k_VirtualTextureWarningHelpBox).Hide();
            root.Q<HelpBox>(k_CompilingWarningHelpBox).Hide();
            root.Q<Button>(k_SharedSettingsInfoHelpboxButton).Hide();

            var sharedSettingsHelpbox = root.Q<HelpBox>(k_SharedSettingsInfoHelpbox);
            sharedSettingsHelpbox.text = TrText.sharedSettingsSectionInfo;

            var sectionLabel = root.Q<Label>(k_BuildDataLabel);
            sectionLabel.AddToClassList("text-large");
            sectionLabel.text = TrText.sceneList;

            AddSceneList(root);
            return root;
        }

        void OnDisable()
        {
            CleanupEventHandlers();

            if (m_PlatformExtension != null)
                m_PlatformExtension.OnDisable();
        }

        internal void OnActivateClicked()
        {
            editorState.UpdateBuildActionStates(ActionState.Disabled, ActionState.Disabled);
        }

        void UpdateWarningsAndButtonStatesForActiveProfile()
        {
            if (!m_Profile.IsActiveBuildProfileOrPlatform())
                return;

            bool isVirtualTexturingValid = BuildProfileModuleUtil.IsVirtualTexturingSettingsValid(m_Profile.buildTarget);
            bool isCompiling = EditorApplication.isCompiling || EditorApplication.isUpdating;
            UpdateHelpBoxVisibility(m_VirtualTexturingHelpBox, !isVirtualTexturingValid);
            UpdateHelpBoxVisibility(m_CompilingWarningHelpBox, isCompiling);

            if (!isVirtualTexturingValid || isCompiling)
            {
                editorState.UpdateBuildActionStates(ActionState.Disabled, ActionState.Disabled);
            }
            else
            {
                editorState.UpdateBuildActionStates(ActionState.Enabled, ActionState.Enabled);
            }
        }

        void UpdateHelpBoxVisibility(HelpBox helpBox, bool showHelpBox)
        {
            if (showHelpBox && helpBox.resolvedStyle.display == DisplayStyle.None)
            {
                helpBox.Show();
            }
            else if (!showHelpBox && helpBox.resolvedStyle.display != DisplayStyle.None)
            {
                helpBox.Hide();
            }
        }

        void ShowPlatformSettings(BuildProfile profile, VisualElement platformSettingsBaseRoot)
        {
            var platformProperties = serializedObject.FindProperty(k_PlatformSettingPropertyName);
            m_PlatformExtension = BuildProfileModuleUtil.GetBuildProfileExtension(profile.buildTarget);
            if (m_PlatformExtension != null)
            {
                var settings = m_PlatformExtension.CreateSettingsGUI(
                    serializedObject, platformProperties, platformSettingsState);
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

            EditorApplication.update -= UpdateWarningsAndButtonStatesForActiveProfile;
        }
    }
}
