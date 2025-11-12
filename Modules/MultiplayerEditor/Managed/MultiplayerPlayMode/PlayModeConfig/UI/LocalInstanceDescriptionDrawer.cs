// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.PlayMode.Editor;
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Multiplayer.Internal;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Image = UnityEngine.UIElements.Image;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [CustomPropertyDrawer(typeof(LocalInstanceDescription), true)]
    class LocalInstanceDescriptionDrawer : PropertyDrawer
    {
        internal const string k_InstanceDrawerClass = "instance-drawer";
        const string k_WarnIconClass = "warn-icon";
        const string k_InstanceDisabledHelpboxClass = "instance-disabled-helpbox";
        internal const string k_MultiplayerRolePopupName = "main-multiplayer-role-field";
        internal const string k_MultiplayerAdditionalRolePopupName = "additional-multiplayer-role-field";
        internal const string k_MultiplayerBuildTargetWarnName = "build-target-warn-icon";
        internal const string k_MultiplayerDefaultDeviceName = "Select Device";
        internal const string k_MultiplayerDevicePopupName = "multiplayer-device-popup";
        internal const string k_InstanceDrawerContainerName = "main-multiplayer-instance-drawer-container";
        internal const string k_DisabledInstanceHelpBoxName = "main-multiplayer-instance-DisabledHelpBox";
        internal const string k_DisabledInstanceHelpBoxText = "An instance is currently running. To modify any settings for editor instances, please terminate this instance in the Play Mode Status Window.";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var enumerator = property.GetEnumerator();
            var instanceContainer = new VisualElement() { name = k_InstanceDrawerContainerName };
            var settingsContainer = new VisualElement();
            instanceContainer.Add(settingsContainer);

            var instanceProp = property.Copy();
            settingsContainer.AddToClassList(k_InstanceDrawerClass);

            // When building UI for Free Run Instances, disable modifications if there are actively running ones.
            var scenario = PlayModeManager.instance.ActivePlayModeConfig as ScenarioConfig;
            var instanceDescript = instanceProp.boxedValue as InstanceDescription;
            if (scenario != null && scenario.Scenario != null && !scenario.Scenario.HasActiveFreeRunInstanceOfType(instanceDescript!.GetType()))
            {
                settingsContainer.AddManipulator(new ContextualMenuManipulator(evt =>
                {
                    evt.menu.AppendAction("Remove", action =>
                    {
                        instanceProp.serializedObject.Update();
                        var so = instanceProp.serializedObject;
                        instanceProp.DeleteCommand();
                        so.ApplyModifiedProperties();
                    });
                    evt.menu.AppendAction("Duplicate", action =>
                    {
                        instanceProp.serializedObject.Update();
                        var so = instanceProp.serializedObject;
                        instanceProp.DuplicateCommand();
                        so.ApplyModifiedProperties();
                    }, _ => ValidateDuplicationOption(_, instanceProp));
                }));
            }

            settingsContainer.Add(CreateNameField(instanceProp));
            settingsContainer.Add(CreateBuildProfileField(instanceProp, out var roleField));
            settingsContainer.Add(CreateServerSettingsField(instanceProp, roleField));
            settingsContainer.Add(CreateAdvancedConfigurationField(instanceProp));

            DisableInstanceContainerIfFreeRunningActive(settingsContainer, instanceProp);
            return instanceContainer;
        }

        private VisualElement CreateNameField(SerializedProperty instanceProperty)
        {
            var nameField = new TextField();
            nameField.AddToClassList("instance-name-field");
            nameField.label = "Name";
            nameField.AddToClassList("unity-base-field__aligned");
            nameField.Bind(instanceProperty.serializedObject);
            nameField.BindProperty(instanceProperty.FindPropertyRelative("Name"));
            nameField.RegisterValueChangedCallback(evt =>
            {
                var warnIcon = ((VisualElement)evt.target).Q<Image>(className: k_WarnIconClass);
                ValidateInstanceName(warnIcon, evt.target as TextField);
            });

            var warnIcon = CreateBuildProfileWarnIcon("Please select a unique name.");
            nameField.Insert(1, warnIcon);

            return nameField;
        }

        private VisualElement CreateAdvancedConfigurationField(SerializedProperty instanceProperty)
        {
            var advancedConfigProp = instanceProperty.FindPropertyRelative("advancedConfiguration");
            var container = new Foldout();
            container.AddToClassList("unity-base-field__aligned");
            container.text = "Advanced Configuration";

            var streamLogsProp = advancedConfigProp.FindPropertyRelative("m_StreamLogsToMainEditor");
            var streamLogsField = new PropertyField(streamLogsProp);
            streamLogsField.Bind(instanceProperty.serializedObject);
            container.Add(streamLogsField);

            var logsColorProp = advancedConfigProp.FindPropertyRelative("m_LogsColor");
            var logsColorField = new PropertyField(logsColorProp);
            logsColorField.Bind(instanceProperty.serializedObject);
            container.Add(logsColorField);

            // Add Arguments based on current deploy mode
            var argumentsContainer = new VisualElement();
            var serverSettingsProp = instanceProperty.FindPropertyRelative("m_ServerSettings");
            var deployModeProp = serverSettingsProp.FindPropertyRelative("DeployMode");

            UpdateArgumentsBasedOnDeployMode(argumentsContainer, advancedConfigProp, deployModeProp, instanceProperty);

            argumentsContainer.TrackPropertyValue(deployModeProp, _ => {
                argumentsContainer.Clear();
                UpdateArgumentsBasedOnDeployMode(argumentsContainer, advancedConfigProp, deployModeProp, instanceProperty);
            });

            container.Add(argumentsContainer);

            return container;
        }

        private void UpdateArgumentsBasedOnDeployMode(VisualElement container, SerializedProperty advancedConfigProp, SerializedProperty deployModeProp, SerializedProperty instanceProperty)
        {
            var deployMode = (ServerSettings.ServerDeployMode)deployModeProp.enumValueIndex;
            var argumentsProp = deployMode switch
            {
                ServerSettings.ServerDeployMode.Local => advancedConfigProp.FindPropertyRelative("m_LocalArguments"),
                ServerSettings.ServerDeployMode.Simulated => advancedConfigProp.FindPropertyRelative("m_SimulatedArguments"),
                _ => advancedConfigProp.FindPropertyRelative("m_LocalArguments")
            };

            if (argumentsProp == null) return;
            var argumentsField = new PropertyField(argumentsProp) { label = "Arguments" };
            argumentsField.Bind(instanceProperty.serializedObject);
            container.Add(argumentsField);
        }

        private VisualElement CreateServerSettingsField(SerializedProperty instanceProperty, TextField roleField)
        {
            if (!LocalDeploymentUtility.IsLocalDeploymentAvailable())
                return null;

            var container = new VisualElement();
            var buildProfileProperty = instanceProperty.FindPropertyRelative("m_BuildProfile");
            container.TrackPropertyValue(buildProfileProperty, _ => RefreshServerSettings(container, instanceProperty, buildProfileProperty));
            roleField.RegisterValueChangedCallback( _ => RefreshServerSettings(container, instanceProperty, buildProfileProperty));
            RefreshServerSettings(container, instanceProperty, buildProfileProperty);

            return container;
        }

        private static void RefreshServerSettings(VisualElement container, SerializedProperty instanceProperty, SerializedProperty buildProfileProperty)
        {
            container.Clear();

            var serverSettingsProp = instanceProperty.FindPropertyRelative("m_ServerSettings");
            var serverSettingsField = new PropertyField(serverSettingsProp);
            serverSettingsField.Bind(instanceProperty.serializedObject);
            container.Add(serverSettingsField);

            var buildProfile = buildProfileProperty.objectReferenceValue as BuildProfile;
            serverSettingsField.style.display = LocalDeploymentUtility.IsServerProfileOrRole(buildProfile)
                ? new StyleEnum<DisplayStyle>(DisplayStyle.Flex)
                : new StyleEnum<DisplayStyle>(DisplayStyle.None);

            if (buildProfile != null)
                serverSettingsField.TrackSerializedObjectValue(new SerializedObject(buildProfile), _ => RefreshServerSettings(container, instanceProperty, buildProfileProperty));
        }

        private VisualElement CreateBuildProfileField(SerializedProperty instanceProperty, out TextField roleFieldOut)
        {
            instanceProperty = instanceProperty.Copy();
            var buildProfileProperty = instanceProperty.FindPropertyRelative("m_BuildProfile");
            var container = new VisualElement();
            var objectField = new ObjectField() { label = "Build Profile", objectType = typeof(BuildProfile) };
            objectField.allowSceneObjects = false;
            objectField.SetValueWithoutNotify(buildProfileProperty.objectReferenceValue);
            objectField.AddToClassList("unity-base-field__aligned");
            var buildProfileProp = buildProfileProperty.Copy();

            roleFieldOut = null;
            var roleField = new TextField("Multiplayer Role") { isReadOnly = true, enabledSelf = false, focusable = false };
            roleField.tooltip = "The multiplayer role can be modified in the build profile window or from the build profile asset.";
            roleField.AddToClassList("unity-base-field__aligned");
            roleFieldOut = roleField;

            var choices = AdbUtilities.GetADBDevicesDetailed();
            var runDeviceField = new PopupField<String>() { label = "Run Device", choices = choices, name = k_MultiplayerDevicePopupName };
            var advanceConfigProperty = instanceProperty.FindPropertyRelative("advancedConfiguration");
            var nextDeviceName = advanceConfigProperty.FindPropertyRelative("m_DeviceName");
            var nextDeviceID = advanceConfigProperty.FindPropertyRelative("m_DeviceID");
            var refreshButton = new Button(() => RefreshDeviceList(runDeviceField, advanceConfigProperty))
            {
                text = "Refresh"
            };

            switch (choices.Count)
            {
                case 0:
                    runDeviceField[1].SetEnabled(false);
                    runDeviceField.SetValueWithoutNotify("No Devices Available");
                    runDeviceField.tooltip =
                        "No device connected. Please go to https://docs.unity3d.com/Manual/android-debugging-on-an-android-device.html for more information on how to connect a device.";
                    break;
                case > 0:
                    runDeviceField.SetValueWithoutNotify(k_MultiplayerDefaultDeviceName);
                    runDeviceField.tooltip = "Select the device to run the instance on.";
                    break;
            }

            if (nextDeviceName != null)
            {
                if (choices.Count == 0 || !choices.Contains(nextDeviceName.stringValue))
                {
                    nextDeviceID.stringValue = "";
                    nextDeviceID.serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    runDeviceField.SetValueWithoutNotify(nextDeviceName.stringValue == "" ? k_MultiplayerDefaultDeviceName : nextDeviceName.stringValue);
                }
            }

            runDeviceField.AddToClassList("unity-base-field__aligned");
            runDeviceField.RegisterValueChangedCallback(evt =>
            {
                var deviceName = buildProfileProperty.FindPropertyRelative("m_DeviceName");
                deviceName.stringValue = evt.newValue;
                deviceName.serializedObject.ApplyModifiedProperties();
                runDeviceField.SetValueWithoutNotify(deviceName.stringValue);
                var warnIcon = ((VisualElement)evt.target).Q<Image>(className: k_WarnIconClass);
                SetRunDeviceWarnIcon(warnIcon, runDeviceField);
                string devices = AdbUtilities.GetADBDevices();
                string[] splitDevices = devices.Split("\n");

                foreach (var device in splitDevices)
                {
                    string[] deviceParts = device.Split('\t', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var devicePart in deviceParts)
                    {
                        if (evt.newValue.Contains(devicePart))
                        {
                            var deviceID = devicePart;
                            var runDeviceID = buildProfileProperty.FindPropertyRelative("m_DeviceID");
                            runDeviceID.stringValue = deviceID;
                            runDeviceID.serializedObject.ApplyModifiedProperties();
                        }
                    }
                }
            });


            objectField.RegisterValueChangedCallback(evt =>
            {
                buildProfileProp.objectReferenceValue = evt.newValue;
                buildProfileProp.serializedObject.ApplyModifiedProperties();
                var warnIcon = ((VisualElement)evt.target).Q<Image>(className: k_WarnIconClass);
                SetBuildProfileWarnIcon(warnIcon, evt.newValue as BuildProfile, instanceProperty);

                SetRoleDisplay(roleField, buildProfileProp.objectReferenceValue as BuildProfile);
                SetDeviceRunDisplay(runDeviceField, buildProfileProp.objectReferenceValue as BuildProfile, instanceProperty);
                SetButtonRefreshDisplay(buildProfileProp.objectReferenceValue as BuildProfile,
                    refreshButton);

            });

            var buildProfileButton = new Button(() => EditorApplication.ExecuteMenuItem("File/Build Profiles")) { name = "build-profile-button", text = "Open Build Profiles" };
            objectField.Add(buildProfileButton);

            var warnIcon = CreateBuildProfileWarnIcon("Build Profile is not supported, please check File - BuildProfiles for more info.");
            objectField.Insert(1, warnIcon);
            SetBuildProfileWarnIcon(warnIcon, buildProfileProp.objectReferenceValue as BuildProfile, instanceProperty);

            container.Add(objectField);
            container.Add(roleField);
            SetRoleDisplay(roleField, buildProfileProp.objectReferenceValue as BuildProfile);

            var deviceWarnIcon = CreateBuildProfileWarnIcon("Please select a device to run the instance on before running the scenario.");
            runDeviceField.Insert(1, deviceWarnIcon);
            SetRunDeviceWarnIcon(deviceWarnIcon, runDeviceField);
            SetDeviceRunDisplay(runDeviceField, buildProfileProp.objectReferenceValue as BuildProfile, instanceProperty);
            SetButtonRefreshDisplay(buildProfileProp.objectReferenceValue as BuildProfile, refreshButton);
            runDeviceField.Add(refreshButton);
            container.Add(runDeviceField);

            return container;
        }

        private void SetButtonRefreshDisplay(BuildProfile profile, Button button)
        {
            var shouldshow = profile != null && InternalUtilities.IsAndroidBuildTarget(profile);

            button.style.display = shouldshow ? new StyleEnum<DisplayStyle>(DisplayStyle.Flex) : new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }

        static Image CreateBuildProfileWarnIcon(string tooltip)
        {
            var warnIcon = new Image() { name = k_MultiplayerBuildTargetWarnName };
            warnIcon.AddToClassList("propertyfield-icon");
            warnIcon.AddToClassList(k_WarnIconClass);
            warnIcon.image = Icons.GetImage(Icons.ImageName.Warning);
            warnIcon.tooltip = tooltip;
            return warnIcon;
        }

        private static void RefreshDeviceList(PopupField<String> devicerunfield, SerializedProperty property)
        {
            var choices = devicerunfield.choices = AdbUtilities.GetADBDevicesDetailed();
            var deviceID = property.FindPropertyRelative("m_DeviceID");
            if (choices.Count == 0)
            {
                devicerunfield.value = "No Devices Available";
                devicerunfield[2].SetEnabled(false);
                devicerunfield.tooltip =
                    "No device connected. Please go to https://docs.unity3d.com/Manual/android-debugging-on-an-android-device.html for more information on how to connect a device.";
                deviceID.stringValue = "";
            }
            else
            {
                devicerunfield[2].SetEnabled(true);
                devicerunfield.value = k_MultiplayerDefaultDeviceName;
                deviceID.stringValue = "";
            }
        }

        DropdownMenuAction.Status ValidateDuplicationOption(DropdownMenuAction arg, SerializedProperty prop)
        {
            var playerInstances = prop.serializedObject.FindProperty("m_EditorInstances");
            if (prop.boxedValue.GetType() == typeof(EditorInstanceDescription) && playerInstances.arraySize >= ScenarioConfigEditor.MaxEditorInstanceCount)
            {
                return DropdownMenuAction.Status.Disabled;
            }

            var localInstances = prop.serializedObject.FindProperty("m_LocalInstances");
            if (prop.boxedValue.GetType() == typeof(LocalInstanceDescription) && localInstances.arraySize >= ScenarioConfigEditor.MaxLocalInstanceCount)
            {
                return DropdownMenuAction.Status.Disabled;
            }

            var remoteInstances = prop.serializedObject.FindProperty("m_RemoteInstances");
            if (prop.boxedValue.GetType() == typeof(RemoteInstanceDescription) && remoteInstances.arraySize >= ScenarioConfigEditor.MaxServerCount)
            {
                return DropdownMenuAction.Status.Disabled;
            }

            return DropdownMenuAction.Status.Normal;
        }

        void SetRoleDisplay(TextField field, BuildProfile profile)
        {
            field.style.display = profile == null ? new StyleEnum<DisplayStyle>(DisplayStyle.None) : new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            if (profile == null)
                return;

            field.value = MultiplayerRolesSettings.instance.GetMultiplayerRoleForBuildProfile(profile).ToString();
        }

        void SetDeviceRunDisplay(PopupField<string> field, BuildProfile profile, SerializedProperty instance)
        {
            field.style.display = profile == null ? new StyleEnum<DisplayStyle>(DisplayStyle.None) : new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            if (profile == null)
                return;

            var instanceDescript = instance.boxedValue as InstanceDescription;
            var hasValidBuildProfile = IsBuildProfileSupported(profile, instanceDescript, out _);
            field.style.display = hasValidBuildProfile && InternalUtilities.IsAndroidBuildTarget(profile)
                ? new StyleEnum<DisplayStyle>(DisplayStyle.Flex)
                : new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }

        private bool IsBuildProfileSupported(BuildProfile newProfile, InstanceDescription instance, out string error)
        {
            // sanity check
            if (newProfile == null)
            {
                error = "Build Profile is not selected.";
                return false;
            }

            // First check if the specified build target is currently available in the Editor
            if (!InternalUtilities.IsBuildProfileSupported(newProfile))
            {
                error = "Build Profile is not supported or installed for this type of instance.";
                return false;
            }

            // Ensure that it is also supported for the current RuntimePlatform
            var canRun = InternalUtilities.BuildProfileCanRunOnCurrentPlatform(newProfile);
            if (instance is LocalInstanceDescription && !canRun)
            {
                var currenPlatform = Application.platform;
                error = $"Build Profile is not supported on the current platform: {currenPlatform}.";
                return false;
            }

            // Also ensure instance-type specific build target checks.
            var isServerInstance = instance.GetType() == typeof(RemoteInstanceDescription);
            if (isServerInstance && InternalUtilities.IsAndroidBuildTarget(newProfile))
            {
                error = "Build Profile is not supported or installed for this type of instance.";
                return false;
            }

            error = null;
            return true;
        }

        void SetBuildProfileWarnIcon(Image warnIcon, BuildProfile newProfile, SerializedProperty so)
        {
            if (newProfile == null)
                return;

            var instanceDescript = so.boxedValue as InstanceDescription;
            var isSupported = IsBuildProfileSupported(newProfile, instanceDescript, out string errorMsg);
            if (!isSupported)
                warnIcon.tooltip = errorMsg;

            warnIcon.style.display = isSupported ? new StyleEnum<DisplayStyle>(DisplayStyle.None) : new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
        }

        private void DisableInstanceContainerIfFreeRunningActive(VisualElement container, SerializedProperty property)
        {
            // Grab the corresponding configs to look for Free Running instances
            var instanceConfig = property.boxedValue as InstanceDescription;
            var scenarioConfig = PlayModeManager.instance.ActivePlayModeConfig as ScenarioConfig;
            if (instanceConfig == null || scenarioConfig == null || scenarioConfig.Scenario == null)
                return;

            // Find the specific instance Type + Name that we are looking for to make the comparison
            if (!scenarioConfig.Scenario.HasActiveFreeRunInstance(name: instanceConfig.Name,
                                                                  type: instanceConfig.GetType()))
                return;

            // Else we've found a player that is active and we should lock configurations
            container.SetEnabled(false);

            // Also show a disabled help box in the container of that instance.
            var disableEditingHelpbox = new HelpBox(k_DisabledInstanceHelpBoxText, HelpBoxMessageType.Info) { name = k_DisabledInstanceHelpBoxName };
            disableEditingHelpbox.AddToClassList(k_InstanceDisabledHelpboxClass);
            container.parent.Add(disableEditingHelpbox);
        }

        void SetRunDeviceWarnIcon(Image warnIcon, PopupField<string> runDeviceField)
        {
            var isSelected = runDeviceField.value != k_MultiplayerDefaultDeviceName && runDeviceField.value != "No Devices Available";


            warnIcon.style.display = isSelected ? new StyleEnum<DisplayStyle>(DisplayStyle.None) : new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
        }

        void ValidateInstanceName(Image warnIcon, TextField nameField)
        {
            var textfields = nameField.panel.visualTree.Query<TextField>(className: "instance-name-field").ToList();

            List<string> takenNames = new List<string>();
            textfields.ForEach(textField =>
            {
                var warningIcon = textField.Q<Image>();
                if (takenNames.Contains(textField.value))
                {
                    warningIcon.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
                }
                else
                {
                    warningIcon.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
                    takenNames.Add(textField.value);
                }
            });
        }
    }
}
