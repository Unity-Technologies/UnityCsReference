// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor;

[CustomPropertyDrawer(typeof(InstanceItem<,>))]
class InstanceItemDrawer : PropertyDrawer
{
    internal const string k_UssClassName = "unity-instance-item-field__container";
    const string k_UniqueNameErrorTooltip = "Instance names must be unique within a scenario.";
    const string k_WarningIconClass = "unity-instance-name-icon__warning";

    bool m_DisableNameEditing = false;

    protected bool DisableNameEditing
    {
        get => m_DisableNameEditing;
        set => m_DisableNameEditing = value;
    }

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var container = new VisualElement();
        container.AddToClassList(k_UssClassName);
        container.Add(CreateNameField(property));
        container.Add(CreateSettingsField(property));
        return container;
    }

    protected virtual VisualElement CreateNameField(SerializedProperty property)
    {
        var nameProperty = property.FindPropertyRelative(IInstanceItem.k_NamePropertyPath);
        var nameField = new TextField(nameProperty.displayName);
        nameField.BindProperty(nameProperty);
        nameField.Bind(property.serializedObject);
        nameField.AddToClassList("unity-base-field__aligned");
        nameField.SetEnabled(!m_DisableNameEditing);
        var icon = new FieldIcon<string>(nameField, Icons.ImageName.Warning)
        {
            tooltip = k_UniqueNameErrorTooltip
        };
        icon.AddToClassList(k_WarningIconClass);
        return nameField;
    }

    protected virtual VisualElement CreateSettingsField(SerializedProperty property)
    {
        var settingsProperty = property.FindPropertyRelative(IInstanceItem.k_SettingsPropertyPath);
        var settingsField = new PlainPropertyField(settingsProperty);
        return settingsField;
    }
}

