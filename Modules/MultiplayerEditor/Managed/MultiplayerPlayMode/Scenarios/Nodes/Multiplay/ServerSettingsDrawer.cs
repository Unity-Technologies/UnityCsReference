// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
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
            private const string k_DeployModeLabel = "Server Options";
            private const string k_DeployModeLocalLabel = "Local";
            private const string k_DeployModeSimulatedLabel = "Simulated Cloud";

            private SerializedProperty m_Property;
            private SerializedProperty m_ModeProperty;
            private ToggleButtonGroup m_ModeToggleGroup;
            private VisualElement m_ModeSettings;

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

                Add(m_ModeToggleGroup);
                Add(m_ModeSettings = new VisualElement());

                // TODO: put this in a style sheet
                m_ModeSettings.style.marginLeft = 18;

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
                        break;
                }

                m_ModeSettings.Bind(m_Property.serializedObject);
            }
        }
    }
}
