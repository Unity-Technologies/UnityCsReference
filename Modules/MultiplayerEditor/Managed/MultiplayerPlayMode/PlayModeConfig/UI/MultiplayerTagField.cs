// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor;

class MultiplayerTagField : PopupField<string>
{
    SerializedProperty m_SerializedProperty;

    public MultiplayerTagField(SerializedProperty serializedProperty, string labelText)
        : base(labelText)
    {
        m_SerializedProperty = serializedProperty;
        choices = [string.Empty, .. ProjectDataStore.GetMain().GetAllPlayerTags()];
        formatSelectedValueCallback = TagToLabel;
        formatListItemCallback = TagToLabel;
        AddToClassList("unity-base-field__aligned");

        this.TrackPropertyValue(serializedProperty, OnPropertyValueChanged);
        this.RegisterValueChangedCallback(OnFieldValueChanged);
        OnPropertyValueChanged(serializedProperty);
    }

    void OnPropertyValueChanged(SerializedProperty property)
    {
        SetValueWithoutNotify(property.stringValue);
    }

    void OnFieldValueChanged(ChangeEvent<string> changeEvent)
    {
        m_SerializedProperty.stringValue = changeEvent.newValue;
        m_SerializedProperty.serializedObject.ApplyModifiedProperties();
    }

    static string TagToLabel(string tag)
    {
        return string.IsNullOrEmpty(tag) ? "<None>" : tag;
    }
}
