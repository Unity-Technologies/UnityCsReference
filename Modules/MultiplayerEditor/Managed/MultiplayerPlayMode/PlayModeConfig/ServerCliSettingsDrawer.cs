// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor;

[CustomPropertyDrawer(typeof(ServerCliSettings))]
class ServerCliSettingsDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        return new ServerCliSettingsField(property);
    }

    class ServerCliSettingsField : VisualElement
    {
        const string k_PortLabel = "Port";
        const string k_QueryProtocolLabel = "Query Protocol";
        const string k_QueryPortLabel = "Query Port";

        SerializedProperty m_UseDefaultArgumentsProperty;
        VisualElement m_ArgumentsContainer;
        VisualElement m_DefaultArgumentsContainer;

        public ServerCliSettingsField(SerializedProperty property)
        {
            m_UseDefaultArgumentsProperty = property.FindPropertyRelative(nameof(ServerCliSettings.UseDefaultArguments));

            var useDefaultArgumentsField = new PropertyField(m_UseDefaultArgumentsProperty);

            m_ArgumentsContainer = new VisualElement();
            m_ArgumentsContainer.Add(new PropertyField(property.FindPropertyRelative(nameof(ServerCliSettings.Port)), k_PortLabel));
            m_ArgumentsContainer.Add(new PropertyField(property.FindPropertyRelative(nameof(ServerCliSettings.QueryProtocol)), k_QueryProtocolLabel));
            m_ArgumentsContainer.Add(new PropertyField(property.FindPropertyRelative(nameof(ServerCliSettings.QueryPort)), k_QueryPortLabel));

            m_DefaultArgumentsContainer = CreateDefaultArgumentsContainer();

            Add(useDefaultArgumentsField);
            Add(m_ArgumentsContainer);
            Add(m_DefaultArgumentsContainer);

            useDefaultArgumentsField.RegisterValueChangeCallback(evt => Refresh());
            Refresh();
        }

        private VisualElement CreateDefaultArgumentsContainer()
        {
            var container = new VisualElement();
            container.enabledSelf = false;

            var portField = new IntegerField(k_PortLabel) { value = GetDefaultArgumentInt("port") };
            var queryProtocolField = new EnumField(k_QueryProtocolLabel, GetDefaultArgumentEnum<SimulatorSettings.ProtocolType>("querytype"));
            var queryPortField = new IntegerField(k_QueryPortLabel) { value = GetDefaultArgumentInt("queryport") };

            portField.AddToClassList(TextField.alignedFieldUssClassName);
            queryProtocolField.AddToClassList(TextField.alignedFieldUssClassName);
            queryPortField.AddToClassList(TextField.alignedFieldUssClassName);

            container.Add(portField);
            container.Add(queryProtocolField);
            container.Add(queryPortField);

            return container;
        }

        static int GetDefaultArgumentInt(string argumentName)
        {
            var value = EditorUserBuildSettings.GetPlatformSettings(NamedBuildTarget.Server.TargetName, $"arg-default-{argumentName}");
            return int.TryParse(value, out var intValue) ? intValue : 0;
        }

        static T GetDefaultArgumentEnum<T>(string argumentName) where T : struct, Enum
        {
            var value = EditorUserBuildSettings.GetPlatformSettings(NamedBuildTarget.Server.TargetName, $"arg-default-{argumentName}");
            Enum.TryParse<T>(value, out var enumValue);
            return enumValue;
        }

        void Refresh()
        {
            var useDefaultArguments = m_UseDefaultArgumentsProperty.boolValue;
            m_ArgumentsContainer.style.display = useDefaultArguments ? DisplayStyle.None : DisplayStyle.Flex;
            m_DefaultArgumentsContainer.style.display = useDefaultArguments ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
