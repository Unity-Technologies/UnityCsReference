// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.Multiplayer.Internal;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor;

class MultiplayerRoleMaskField : PopupField<MultiplayerRoleFlags>
{
    SerializedProperty m_SerializedProperty;

    public MultiplayerRoleMaskField(SerializedProperty serializedProperty, string labelText)
        : base(labelText)
    {
        m_SerializedProperty = serializedProperty;
        choices = new((MultiplayerRoleFlags[])Enum.GetValues(typeof(MultiplayerRoleFlags)));
        formatSelectedValueCallback = MultiplayerPlayerRoleFlagsText;
        formatListItemCallback = MultiplayerPlayerRoleFlagsText;
        AddToClassList("unity-base-field__aligned");

        this.TrackPropertyValue(serializedProperty, OnPropertyValueChanged);
        this.RegisterValueChangedCallback(OnFieldValueChanged);
        OnPropertyValueChanged(serializedProperty);
    }

    void OnPropertyValueChanged(SerializedProperty property)
    {
        SetValueWithoutNotify((MultiplayerRoleFlags)property.enumValueFlag);
    }

    void OnFieldValueChanged(ChangeEvent<MultiplayerRoleFlags> changeEvent)
    {
        m_SerializedProperty.enumValueFlag = (int)changeEvent.newValue;
        m_SerializedProperty.serializedObject.ApplyModifiedProperties();
    }

    static string MultiplayerPlayerRoleFlagsText(MultiplayerRoleFlags flag)
    {
        return flag switch
        {
            MultiplayerRoleFlags.ClientAndServer => "Client And Server",
            MultiplayerRoleFlags.Client => "Client",
            MultiplayerRoleFlags.Server => "Server",
            _ => "Invalid Role"
        };
    }
}
