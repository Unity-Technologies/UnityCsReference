// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements
{
    class BuildProfileGraphicsSettingsOverridesView
    {
        /// <summary>
        /// Show the Graphics Settings section in the build profile editor.
        /// </summary>
        internal static void CreateGUI(BuildProfile profile, VisualElement root)
        {
            var graphicsSettingsSection = root.Q<VisualElement>("editor-graphics-settings");
            graphicsSettingsSection.Show();

            var graphicsSettingsTitle = root.Q<Label>("graphics-settings-label");
            var overrideToggle = root.Q<Toggle>("graphics-settings-override-toggle");
            var overrideRoot = root.Q<VisualElement>("graphics-settings-override-root");
            var overrideFoldout = root.Q<Foldout>("graphics-settings-override-foldout");
            var editorVisualElement = root.Q<VisualElement>("graphics-settings-override-editor");
            var optionsButton = root.Q<Button>("graphics-settings-override-options");

            graphicsSettingsTitle.text = TrText.graphicsSettings;
            overrideToggle.label = TrText.overrideGraphicsSettingsToggleLabel;
            overrideFoldout.text = TrText.overrideFoldoutLabel;

            var graphicsSettings = profile.graphicsSettings;
            BuildProfileGraphicsSettingsEditor editor = null;

            if (graphicsSettings == null)
            {
                overrideToggle.value = false;
                overrideRoot.Hide();
            }
            else
            {
                overrideToggle.value = true;
                editor = Editor.CreateEditor(graphicsSettings) as BuildProfileGraphicsSettingsEditor;
                var inspectorGUI = editor.CreateInspectorGUI();
                editorVisualElement.Add(inspectorGUI);
            }

            overrideToggle.RegisterValueChangedCallback((evt) =>
            {
                if (evt.newValue == true)
                    AddGraphicsSettings();
                else
                    RemoveGraphicsSettings();

                if (profile == BuildProfileContext.activeProfile)
                    BuildProfileModuleUtil.OnActiveProfileGraphicsSettingsChanged(evt.newValue);
            });

            optionsButton.clicked += () =>
            {
                var menu = new GenericMenu();
                menu.AddItem(TrText.resetToGlobals, false, editor.IsDataEqualToGlobalGraphicsSettings() ? null : ResetGraphicsSettings);
                menu.ShowAsContext();
            };

            profile.OnGraphicsSettingsSubAssetRemoved -= UpdateViewOnGraphicsSettingsSubAssetRemoved;
            profile.OnGraphicsSettingsSubAssetRemoved += UpdateViewOnGraphicsSettingsSubAssetRemoved;

            void AddGraphicsSettings()
            {
                if (graphicsSettings != null)
                    return;

                BuildProfileModuleUtil.CreateGraphicsSettings(profile);
                graphicsSettings = profile.graphicsSettings;

                editor = Editor.CreateEditor(graphicsSettings) as BuildProfileGraphicsSettingsEditor;
                editor.ResetToGlobalGraphicsSettingsValues();

                var inspectorGUI = editor.CreateInspectorGUI();
                editorVisualElement.Add(inspectorGUI);
                overrideRoot.Show();
            }

            void RemoveGraphicsSettings()
            {
                if (graphicsSettings == null)
                    return;

                bool removeGraphicsSettings = EditorUtility.DisplayDialog(TrText.removeGraphicsSettingsDialogTitle,
                    TrText.removeGraphicsSettingsDialogMessage,
                    TrText.continueButtonText, TrText.cancelButtonText);

                if (!removeGraphicsSettings)
                {
                    overrideToggle.value = true;
                    return;
                }

                BuildProfileModuleUtil.RemoveGraphicsSettings(profile);
            }

            void UpdateViewOnGraphicsSettingsSubAssetRemoved()
            {
                graphicsSettings = null;
                editor = null;

                overrideToggle.value = false;
                editorVisualElement.Clear();
                overrideRoot.Hide();
            }

            void ResetGraphicsSettings()
            {
                bool resetGraphicsSettings = EditorUtility.DisplayDialog(TrText.resetGraphicsSettingsDialogTitle,
                    TrText.resetGraphicsSettingsDialogMessage,
                    TrText.continueButtonText, TrText.cancelButtonText);

                if (!resetGraphicsSettings)
                    return;

                editor.ResetToGlobalGraphicsSettingsValues();

                editorVisualElement.Clear();
                var inspectorGUI = editor.CreateInspectorGUI();
                editorVisualElement.Add(inspectorGUI);
            }
        }
    }
}
