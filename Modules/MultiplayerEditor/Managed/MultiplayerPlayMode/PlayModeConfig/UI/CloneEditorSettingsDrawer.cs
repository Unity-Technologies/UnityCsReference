// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.Multiplayer.Internal;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using InstanceSettings = Unity.Multiplayer.PlayMode.Editor.CloneEditorController.InstanceSettings;

namespace Unity.Multiplayer.PlayMode.Editor;

[CustomPropertyDrawer(typeof(InstanceItem<CloneEditorController, InstanceSettings>))]
class CloneEditorItemDrawer : InstanceItemDrawer
{
    public CloneEditorItemDrawer()
    {
        DisableNameEditing = true;
    }
}

[CustomPropertyDrawer(typeof(InstanceSettings))]
class CloneEditorSettingsDrawer : PropertyDrawer
{
    const string k_RoleLabel = "Multiplayer Role";
    const string k_TagLabel = "Tag";
    const string k_AdvancedSettingsLabel = "Advanced Configuration";

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var container = new VisualElement();
        if (EditorMultiplayerManager.enableMultiplayerRoles)
        {
            container.Add(new MultiplayerRoleMaskField(property.FindPropertyRelative(nameof(InstanceSettings.RoleMask)), k_RoleLabel));
        }
        container.Add(new MultiplayerTagField(property.FindPropertyRelative(nameof(InstanceSettings.PlayerTag)), k_TagLabel));
        container.Add(CreateAdvanceSettings(property));

        return container;
    }

    VisualElement CreateAdvanceSettings(SerializedProperty property)
    {
        var container = new Foldout
        {
            text = k_AdvancedSettingsLabel
        };

        container.viewDataKey = $"{property.propertyPath}.AdvancedSettings";

        container.Add(new PropertyField(property.FindPropertyRelative(nameof(InstanceSettings.StreamLogsToMainEditor))));
        container.Add(new PropertyField(property.FindPropertyRelative(nameof(InstanceSettings.LogsColor))));

        return container;
    }
}
