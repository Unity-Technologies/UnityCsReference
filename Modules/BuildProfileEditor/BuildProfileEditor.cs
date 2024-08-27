// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Modules;
using UnityEditor.Build.Profile.Elements;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;

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
        const string k_PlatformBuildWarningsRoot = "platform-build-warning-root";
        const string k_PlayerScriptingDefinesFoldout = "scripting-defines-foldout";
        const string k_BuildSettingsFoldout = "build-settings-foldout";
        BuildProfileSceneList m_SceneList;
        HelpBox m_CompilingWarningHelpBox;
        HelpBox m_VirtualTexturingHelpBox;
        Button recompileDefinesButton;
        Button revertDefinesButton;
        BuildProfile m_Profile;

        public BuildProfileWindow parent { get; set; }

        public BuildProfileWorkflowState parentState { get; set; }

        public BuildProfileWorkflowState editorState { get; set; }

        public BuildProfileWorkflowState platformSettingsState { get; set; }

        internal BuildProfile buildProfile => m_Profile;

        IBuildProfileExtension m_PlatformExtension = null;
        Foldout m_PlayerScriptingDefinesFoldout;

        BuildProfilePlayerSettingsEditor m_ProfilePlayerSettingsEditor = null;

        public BuildProfileEditor()
        {
            editorState = new BuildProfileWorkflowState((next) =>
            {
                if (editorState.buildAction == ActionState.Disabled)
                    editorState.buildAndRunAction = ActionState.Disabled;

                if (editorState.additionalActions != next.additionalActions)
                    editorState.additionalActions = next.additionalActions;

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
            var platformBuildWarningsRoot = root.Q<VisualElement>(k_PlatformBuildWarningsRoot);
            var buildDataLabel = root.Q<Label>(k_BuildDataLabel);
            var sharedSettingsInfoHelpBox = root.Q<HelpBox>(k_SharedSettingsInfoHelpbox);
            var buildSettingsFoldout = root.Q<Foldout>(k_BuildSettingsFoldout);
            m_VirtualTexturingHelpBox = root.Q<HelpBox>(k_VirtualTextureWarningHelpBox);
            m_CompilingWarningHelpBox = root.Q<HelpBox>(k_CompilingWarningHelpBox);

            m_VirtualTexturingHelpBox.text = TrText.invalidVirtualTexturingSettingMessage;
            m_CompilingWarningHelpBox.text = TrText.compilingMessage;

            platformSettingsLabel.text = TrText.platformSettings;
            buildDataLabel.text = TrText.buildData;
            sharedSettingsInfoHelpBox.text = TrText.sharedSettingsInfo;
            buildSettingsFoldout.text = TrText.GetSettingsSectionName(BuildProfileModuleUtil.GetClassicPlatformDisplayName(profile.platformId));

            AddSceneList(root, profile);
            AddScriptingDefineListView(root);

            bool hasErrors = Util.UpdatePlatformRequirementsWarningHelpBox(noModuleFoundHelpBox, profile.platformId);
            bool isClassic = BuildProfileContext.IsClassicPlatformProfile(profile);

            if (!isClassic)
            {
                sharedSettingsInfoHelpBox.Hide();
                m_ProfilePlayerSettingsEditor = BuildProfilePlayerSettingsEditor
                    .CreatePlayerSettingsUI(root, hasErrors ? null : serializedObject);
            }
            else
            {
                var button = root.Q<Button>(k_SharedSettingsInfoHelpboxButton);
                button.text = TrText.addBuildProfile;
                button.clicked += DuplicateSelectedClassicProfile;
            }

            if (hasErrors)
            {
                buildSettingsFoldout.Hide();
                return root;
            }

            EditorApplication.update += EditorUpdate;

            ShowPlatformSettings(profile, platformSettingsBaseRoot, platformBuildWarningsRoot);
            root.Bind(serializedObject);
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
            root.Q<Foldout>(k_PlayerScriptingDefinesFoldout).Hide();
            root.Q<Foldout>(k_BuildSettingsFoldout).Hide();

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
            HandleRecompileRequiredDialog();

            if (m_PlatformExtension != null)
                m_PlatformExtension.OnDisable();

            // To prevent errors when entering play mode with the the build profile
            // window open, we need to remove the player settings inspector.
            m_ProfilePlayerSettingsEditor?.RemovePlayerSettingsInspector();
        }

        internal void OnActivateClicked()
        {
            editorState.buildAndRunButtonDisplayName = platformSettingsState.buildAndRunButtonDisplayName;
            editorState.buildButtonDisplayName = platformSettingsState.buildButtonDisplayName;
            editorState.UpdateBuildActionStates(ActionState.Disabled, ActionState.Disabled);
            recompileDefinesButton.Show();
            revertDefinesButton.Show();
        }

        internal void DuplicateSelectedClassicProfile()
        {
            parent.DuplicateSelectedClassicProfile();
        }

        void EditorUpdate()
        {
            UpdateWarningsAndButtonStatesForActiveProfile();

            m_ProfilePlayerSettingsEditor?.EditorUpdate();
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
                recompileDefinesButton?.SetEnabled(false);
                revertDefinesButton?.SetEnabled(false);
                m_PlayerScriptingDefinesFoldout?.SetEnabled(false);
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

        void ShowPlatformSettings(BuildProfile profile, VisualElement platformSettingsBaseRoot, VisualElement platformBuildWarningsRoot)
        {
            var platformProperties = serializedObject.FindProperty(k_PlatformSettingPropertyName);
            m_PlatformExtension = BuildProfileModuleUtil.GetBuildProfileExtension(profile.moduleName);
            if (m_PlatformExtension == null)
                return;

            var warningContainer = m_PlatformExtension.CreatePlatformBuildWarningsGUI(serializedObject, platformProperties);

            // Build Profile Window reserves space for custom
            // platform GUI outside of the editor scroll view.
            if (parent != null && warningContainer != null)
            {
                parent.AppendInspectorHeaderElement(warningContainer);
            }

            var settings = m_PlatformExtension.CreateSettingsGUI(
                serializedObject, platformProperties, platformSettingsState);
            platformSettingsBaseRoot.Add(settings);
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
            var container = m_SceneList.GetSceneListGUI(isEnable);
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

            EditorApplication.update -= EditorUpdate;
        }

        void AddScriptingDefineListView(VisualElement root)
        {
            m_PlayerScriptingDefinesFoldout = root.Q<Foldout>(k_PlayerScriptingDefinesFoldout);
            recompileDefinesButton = m_PlayerScriptingDefinesFoldout.Q<Button>("scripting-defines-apply-button");
            revertDefinesButton = m_PlayerScriptingDefinesFoldout.Q<Button>("scripting-defines-revert-button");
            if (BuildProfileContext.IsClassicPlatformProfile(m_Profile))
            {
                m_PlayerScriptingDefinesFoldout.Hide();
                return;
            }

            var property = serializedObject.FindProperty("m_ScriptingDefines");
            m_PlayerScriptingDefinesFoldout.text = TrText.scriptingDefines;
            m_PlayerScriptingDefinesFoldout.tooltip = TrText.scriptingDefinesTooltip;
            var listView = m_PlayerScriptingDefinesFoldout.Q<ListView>("scripting-defines-listview");
            var warningHelpbox = m_PlayerScriptingDefinesFoldout.Q<HelpBox>("scripting-defines-warning-help-box");
            warningHelpbox.text = TrText.scriptingDefinesWarningHelpbox;
            revertDefinesButton.text = TrText.revert;
            recompileDefinesButton.text = TrText.apply;

            recompileDefinesButton.clicked += () => BuildProfileModuleUtil.RequestScriptCompilation(m_Profile);
            revertDefinesButton.clicked += RevertScriptingDefines;
            listView.TrackPropertyValue(property, this.OnScriptingDefinePropertyChange);
            listView.BindProperty(property);

            if (!m_Profile.IsActiveBuildProfileOrPlatform())
            {
                recompileDefinesButton.Hide();
                revertDefinesButton.Hide();
                warningHelpbox.Hide();
            }
            else
            {
                BuildProfileContext.instance.cachedEditorScriptingDefines = (string[]) m_Profile.scriptingDefines.Clone();
                var targetName = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(m_Profile.buildTarget));
                if (string.IsNullOrEmpty(PlayerSettings.GetScriptingDefineSymbols(targetName)))
                {
                    warningHelpbox.Hide();
                }
            }

            this.OnScriptingDefinePropertyChange(property);
        }

        void HandleRecompileRequiredDialog()
        {
            if (m_Profile == null)
                return;

            if (m_Profile != BuildProfileContext.instance.activeProfile)
                return;

            // Avoid dialog when waiting for compilation.
            if (m_PlayerScriptingDefinesFoldout != null && !m_PlayerScriptingDefinesFoldout.enabledSelf)
                return;

            // Playmode manually handles script compilation.
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            serializedObject.ApplyModifiedProperties();
            var lastCompiledDefines = BuildProfileContext.instance.cachedEditorScriptingDefines;
            if (ArrayUtility.ArrayEquals(m_Profile.scriptingDefines, lastCompiledDefines))
            {
                return;
            }

            if (EditorUtility.DisplayDialog(TrText.scriptingDefinesModified, TrText.scriptingDefinesModifiedBody, TrText.apply, TrText.revert))
            {
                BuildProfileModuleUtil.RequestScriptCompilation(m_Profile);
            }
            else
            {
                RevertScriptingDefines();
            }
        }

        void OnScriptingDefinePropertyChange(SerializedProperty property)
        {
            if (!m_Profile.IsActiveBuildProfileOrPlatform())
                return;

            var lastCompiledDefines = BuildProfileContext.instance.cachedEditorScriptingDefines;
            if (property.arraySize != lastCompiledDefines.Length)
            {
                recompileDefinesButton.SetEnabled(true);
                revertDefinesButton.SetEnabled(true);
                return;
            }

            for (int i = 0; i < property.arraySize; i++)
            {
                if (property.GetArrayElementAtIndex(i).stringValue != lastCompiledDefines[i])
                {
                    recompileDefinesButton.SetEnabled(true);
                    revertDefinesButton.SetEnabled(true);
                    return;
                }
            }

            recompileDefinesButton.SetEnabled(false);
            revertDefinesButton.SetEnabled(false);
        }

        void RevertScriptingDefines()
        {
            m_Profile.scriptingDefines = BuildProfileContext.instance.cachedEditorScriptingDefines;
            serializedObject.Update();
            recompileDefinesButton.SetEnabled(false);
            revertDefinesButton.SetEnabled(false);
        }
    }
}
