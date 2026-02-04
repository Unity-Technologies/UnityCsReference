// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.Multiplayer.Internal;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using InstanceSettings = Unity.Multiplayer.PlayMode.Editor.MainEditorController.InstanceSettings;

namespace Unity.Multiplayer.PlayMode.Editor;

[CustomPropertyDrawer(typeof(InstanceItem<MainEditorController, InstanceSettings>))]
class MainEditorItemDrawer : InstanceItemDrawer
{
    protected override VisualElement CreateNameField(SerializedProperty property) => null;
}

[CustomPropertyDrawer(typeof(InstanceSettings))]
class MainEditorSettingsDrawer : PropertyDrawer
{
    const string k_RoleLabel = "Multiplayer Role";
    const string k_TagLabel = "Tag";

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var container = new VisualElement();
        if (EditorMultiplayerManager.enableMultiplayerRoles)
        {
            container.Add(new MultiplayerRoleMaskField(property.FindPropertyRelative(nameof(InstanceSettings.RoleMask)), k_RoleLabel));
        }
        container.Add(new MultiplayerTagField(property.FindPropertyRelative(nameof(InstanceSettings.PlayerTag)), k_TagLabel));
        container.Add(new PropertyField(property.FindPropertyRelative(nameof(InstanceSettings.InitialScene))));
        return container;
    }
}
