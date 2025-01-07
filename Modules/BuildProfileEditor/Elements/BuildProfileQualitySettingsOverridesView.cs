// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Build.Profile.Elements
{
    class BuildProfileQualitySettingsOverridesView
    {
        /// <summary>
        /// Show the Quality Settings section in the build profile editor.
        /// </summary>
        internal static void CreateGUI(BuildProfile profile, VisualElement root)
        {
            var overrideToggle = root.Q<Toggle>("quality-settings-override-toggle");
            var overrideRoot = root.Q<VisualElement>("quality-settings-override-root");
            var overrideFoldout = root.Q<Foldout>("quality-settings-override-foldout");
            var editorVisualElement = root.Q<VisualElement>("quality-settings-override-editor");
            var optionsButton = root.Q<Button>("quality-settings-override-options");

            overrideToggle.label = TrText.overrideQualitySettingsToggleLabel;
            overrideFoldout.text = TrText.overrideQualitySettingsFoldoutLabel;

            var qualitySettings = profile.qualitySettings;
            BuildProfileQualitySettingsEditor editor = null;

            if (qualitySettings == null)
            {
                overrideToggle.value = false;
                overrideRoot.Hide();
            }
            else
            {
                overrideToggle.value = true;
                editor = Editor.CreateEditor(qualitySettings) as BuildProfileQualitySettingsEditor;
                var inspectorGUI = editor.CreateInspectorGUI();
                editorVisualElement.Add(inspectorGUI);
            }

            overrideToggle.RegisterValueChangedCallback((evt) =>
            {
                if (evt.newValue == true)
                    AddQualitySettings();
                else
                    RemoveQualitySettings();
            });

            optionsButton.clicked += () =>
            {
                var menu = new GenericMenu();
                menu.AddItem(TrText.resetToGlobals, false, editor.IsDataEqualToGlobalQualitySettings(profile) ? null : ResetQualitySettings);
                menu.ShowAsContext();
            };

            profile.OnQualitySettingsSubAssetRemoved -= UpdateViewOnQualitySettingsSubAssetRemoved;
            profile.OnQualitySettingsSubAssetRemoved += UpdateViewOnQualitySettingsSubAssetRemoved;

            void AddQualitySettings()
            {
                if (qualitySettings != null)
                    return;

                BuildProfileModuleUtil.CreateQualitySettings(profile);
                qualitySettings = profile.qualitySettings;

                editor = Editor.CreateEditor(qualitySettings) as BuildProfileQualitySettingsEditor;
                profile.ResetToGlobalQualitySettingsValues();

                var inspectorGUI = editor.CreateInspectorGUI();
                editorVisualElement.Add(inspectorGUI);
                overrideRoot.Show();
            }

            void RemoveQualitySettings()
            {
                if (qualitySettings == null)
                    return;

                bool removeQualitySettings = EditorUtility.DisplayDialog(TrText.removeQualitySettingsDialogTitle,
                    TrText.removeQualitySettingsDialogMessage,
                    TrText.continueButtonText, TrText.cancelButtonText);

                if (!removeQualitySettings)
                {
                    overrideToggle.value = true;
                    return;
                }

                BuildProfileModuleUtil.RemoveQualitySettings(profile);
            }

            void UpdateViewOnQualitySettingsSubAssetRemoved()
            {
                qualitySettings = null;
                editor = null;

                overrideToggle.value = false;
                editorVisualElement.Clear();
                overrideRoot.Hide();
            }

            void ResetQualitySettings()
            {
                bool resetQualitySettings = EditorUtility.DisplayDialog(TrText.resetQualitySettingsDialogTitle,
                    TrText.resetQualitySettingsDialogMessage,
                    TrText.continueButtonText, TrText.cancelButtonText);

                if (!resetQualitySettings)
                    return;

                profile.ResetToGlobalQualitySettingsValues();

                editorVisualElement.Clear();
                var inspectorGUI = editor.CreateInspectorGUI();
                editorVisualElement.Add(inspectorGUI);
            }
        }
    }
}
