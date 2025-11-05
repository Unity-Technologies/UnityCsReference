// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [CustomPropertyDrawer(typeof(ServerSettings))]
    class ServerSettingsDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new ServerSettingsField(property);
        }

        private class ServerSettingsField : VisualElement
        {
            const string k_DeployModeLabel = "Server Options";
            const string k_DeployModeLocalLabel = "Local";
            const string k_DeployModeSimulatedLabel = "Simulated Cloud";
            const string k_MissingPackageHelpboxTextValue = "To simulate Multiplay locally, " +
                                                            "Multiplayer Services Package should be installed.";
            const string k_MissingPackageHelpboxTextInstall = "Install Package";
            const string k_MissingPackageHelpboxTextInstalling = "Installing Package";
            const string k_MissingPackageHelpboxClass = "unity-scenarios-local-missing-packages__helpbox";
            const string k_MissingPackageHelpboxButtonClass = "unity-scenarios-local-missing-packages__button";
            const string k_MissingPackageHelpboxButtonContainerClass = "unity-scenarios-local-missing-packages__container";

            SerializedProperty m_Property;
            SerializedProperty m_ModeProperty;
            ToggleButtonGroup m_ModeToggleGroup;
            VisualElement m_ModeSettings;
            VisualElement m_InstallPackageView;

            public ServerSettingsField(SerializedProperty property)
            {
                m_Property = property;
                m_ModeProperty = property.FindPropertyRelative(nameof(ServerSettings.DeployMode));
                Refresh();
            }

            private void Refresh()
            {
                Clear();
                m_ModeToggleGroup = new ToggleButtonGroup(k_DeployModeLabel);
                m_ModeToggleGroup.AddToClassList("unity-base-field__aligned");
                m_ModeToggleGroup.Add(CreateToggleGroupButton(k_DeployModeLocalLabel));
                m_ModeToggleGroup.Add(CreateToggleGroupButton(k_DeployModeSimulatedLabel));
                m_InstallPackageView = CreateMissingPackageHelpbox();

                Add(m_ModeToggleGroup);
                Add(m_ModeSettings = new VisualElement());
                Add(m_InstallPackageView);

                // TODO: put this in a style sheet
                m_ModeSettings.style.marginLeft = 18;
                m_ModeSettings.style.marginBottom = 8;

                m_ModeToggleGroup.TrackPropertyValue(m_ModeProperty, UpdateModeFieldValue);
                m_ModeToggleGroup.RegisterValueChangedCallback(OnModeValueChanged);
                UpdateModeFieldValue(m_ModeProperty);
            }

            private static Button CreateToggleGroupButton(string text)
            {
                var button = new Button() { text = text };
                button.AddToClassList("unity-base-field__input");

                // TODO: put this in a style sheet
                button.style.flexBasis = 0;
                button.style.marginRight = 0;
                return button;
            }

            VisualElement CreateMissingPackageHelpbox()
            {
                var helpBox = new HelpBox(k_MissingPackageHelpboxTextValue, HelpBoxMessageType.Warning);
                var buttonContainer = new VisualElement();
                var installPackagesButton = new Button { text = k_MissingPackageHelpboxTextInstall };
                var buttonOnClick = new Clickable(async void () =>
                {
                    installPackagesButton.enabledSelf = false;
                    installPackagesButton.text = k_MissingPackageHelpboxTextInstalling;

                    await OrchestratedScenario.LoadPackagesAsync();

                    installPackagesButton.enabledSelf = true;
                    installPackagesButton.text = k_MissingPackageHelpboxTextInstall;
                });

                helpBox.AddToClassList(k_MissingPackageHelpboxClass);
                buttonContainer.AddToClassList(k_MissingPackageHelpboxButtonContainerClass);
                installPackagesButton.AddToClassList(k_MissingPackageHelpboxButtonClass);

                installPackagesButton.clickable = buttonOnClick;
                buttonContainer.Add(installPackagesButton);
                helpBox.Add(buttonContainer);
                return helpBox;
            }

            private void OnModeValueChanged(ChangeEvent<ToggleButtonGroupState> evt)
            {
                var value = evt.newValue[1]
                    ? ServerSettings.ServerDeployMode.Simulated
                    : ServerSettings.ServerDeployMode.Local;

                m_ModeProperty.enumValueIndex = (int)value;
                m_ModeProperty.serializedObject.ApplyModifiedProperties();
            }

            private void UpdateModeFieldValue(SerializedProperty deployModeProperty)
            {
                var value = (ServerSettings.ServerDeployMode)deployModeProperty.enumValueIndex;
                var index = value switch
                {
                    ServerSettings.ServerDeployMode.Local => 0,
                    ServerSettings.ServerDeployMode.Simulated => 1,
                    _ => 0
                };
                var state = new ToggleButtonGroupState(0, 3);
                state[index] = true;
                m_ModeToggleGroup.SetValueWithoutNotify(state);

                m_ModeSettings.Clear();
                switch (value)
                {
                    case ServerSettings.ServerDeployMode.Local:
                        break;
                    case ServerSettings.ServerDeployMode.Simulated:
                        m_ModeSettings.Add(new PlainPropertyField(m_Property.FindPropertyRelative(nameof(ServerSettings.SimulatorSettings))));

                        var cliSettingsFoldout = new Foldout()
                        {
                            text = "Server CLI Arguments",
                        };
                        m_ModeSettings.Add(cliSettingsFoldout);
                        cliSettingsFoldout.Add(new PropertyField(m_Property.FindPropertyRelative(nameof(ServerSettings.CliSettings))));
                        break;
                }

                m_ModeSettings.Bind(m_Property.serializedObject);

                RefreshInstallPackageUI();
            }

            void RefreshInstallPackageUI()
            {
                var isInstalled = OrchestratedScenario.PackagesForRemoteDeployInstalled(out _);
                var isLocalSelected = m_ModeProperty.enumValueIndex == (int)ServerSettings.ServerDeployMode.Local;
                var shouldShowModeSettings = isInstalled && !isLocalSelected;
                var shouldShowInstallPackage = !isInstalled && !isLocalSelected;

                m_InstallPackageView.style.display = shouldShowInstallPackage ? DisplayStyle.Flex : DisplayStyle.None;
                m_ModeSettings.style.display = shouldShowModeSettings ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
