// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using InstanceSettings = Unity.Multiplayer.PlayMode.Editor.LocalPlayerController.InstanceSettings;

namespace Unity.Multiplayer.PlayMode.Editor;

[CustomPropertyDrawer(typeof(InstanceSettings))]
class LocalPlayerSettingsDrawer : PropertyDrawer
{
    const string k_AdvancedSettingsLabel = "Advanced Configuration";

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var container = new VisualElement();
        var deviceContainer = new VisualElement();

        var buildProfileProperty = property.FindPropertyRelative(nameof(InstanceSettings.BuildProfile));
        var deviceNameProperty = property.FindPropertyRelative(nameof(InstanceSettings.DeviceName));
        var deviceIdProperty = property.FindPropertyRelative(nameof(InstanceSettings.DeviceID));

        container.Add(new BuildProfileField(buildProfileProperty));
        container.Add(deviceContainer);
        container.Add(CreateAdvanceSettings(property));

        deviceContainer.TrackPropertyValue(
            buildProfileProperty,
            _ => RefreshDeviceField(buildProfileProperty, deviceNameProperty, deviceIdProperty, deviceContainer));

        RefreshDeviceField(buildProfileProperty, deviceNameProperty, deviceIdProperty, deviceContainer);
        return container;
    }

    static VisualElement CreateAdvanceSettings(SerializedProperty property)
    {
        var container = new Foldout
        {
            text = k_AdvancedSettingsLabel,
            viewDataKey = $"{property.propertyPath}.AdvancedSettings"
        };

        container.Add(new PropertyField(property.FindPropertyRelative(nameof(InstanceSettings.StreamLogsToMainEditor))));
        container.Add(new PropertyField(property.FindPropertyRelative(nameof(InstanceSettings.LogsColor))));
        container.Add(new PropertyField(property.FindPropertyRelative(nameof(InstanceSettings.Arguments))));

        return container;
    }

    static void RefreshDeviceField(
        SerializedProperty buildProfileProperty,
        SerializedProperty deviceNameProperty,
        SerializedProperty deviceIdProperty,
        VisualElement deviceContainer)
    {
        deviceContainer.Clear();
        deviceContainer.Add(CreateDeviceField(buildProfileProperty, deviceNameProperty, deviceIdProperty));
    }

    static VisualElement CreateDeviceField(SerializedProperty buildProfileProperty, SerializedProperty deviceNameProperty, SerializedProperty deviceIdProperty)
    {
        var profile = buildProfileProperty.objectReferenceValue as BuildProfile;
        if (profile == null || !InternalUtilities.IsAndroidBuildTarget(profile) || !InternalUtilities.IsBuildProfileSupported(profile))
            return null;

        return new AdbDeviceField(deviceNameProperty, deviceIdProperty);
    }
}
