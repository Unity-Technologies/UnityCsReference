// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlayMode.Editor
{
    class PlayModeConfigurationsWindow : EditorWindow
    {
        internal const float k_MinWidth = 700;
        internal const float k_MinHeight = 400;
        internal const string k_NewConfigurationButtonName = "NewConfigurationButton";
        internal const string k_NewConfigurationButtonTooltip = "Create a new Play Mode Scenario";

        DetailView m_DetailView;
        HelpBox m_DisableEditingHelpbox;
        PlayModeListView m_PlayModeListView;

        public static void ShowWindow()
        {
            GetWindow<PlayModeConfigurationsWindow>("Play Mode Scenarios");
        }

        public void OnEnable()
        {
            minSize = new Vector2(k_MinWidth, k_MinHeight);

            m_DisableEditingHelpbox = new HelpBox("Editing is not allowed in Playmode.", HelpBoxMessageType.Info);
            rootVisualElement.Add(m_DisableEditingHelpbox);

            var toolbar = new UnityEditor.UIElements.Toolbar();
            m_PlayModeListView = new PlayModeListView();

            toolbar.Add(CreateNewScenarioMenu());
            rootVisualElement.Add(toolbar);

            var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(splitView);

            m_DetailView = new DetailView();

            m_PlayModeListView.OnConfigSelected += SelectConfig;

            m_DetailView.SetConfig(m_PlayModeListView.ConfigurationToEdit);

            splitView.Add(m_PlayModeListView);
            splitView.Add(m_DetailView);

            SetHelpboxStatus();
            PlayModeManager.instance.StateChanged += (state) =>
            {
                SetHelpboxStatus();
            };

            rootVisualElement.RegisterCallback<MouseDownEvent>(OnMouseDown, TrickleDown.TrickleDown);
            rootVisualElement.RegisterCallback<MouseUpEvent>(OnMouseUp, TrickleDown.TrickleDown);
        }

        private void OnMouseDown(MouseDownEvent evt)
        {
            if (PlayModeManager.instance.CurrentState == PlayModeState.Running)
            {
                evt.StopPropagation();
                // evt.PreventDefault();
            }
        }

        private void OnMouseUp(MouseUpEvent evt)
        {
            if (PlayModeManager.instance.CurrentState == PlayModeState.Running)
            {
                evt.StopPropagation();
                // evt.PreventDefault();
            }
        }

        private VisualElement CreateNewScenarioMenu()
        {
            var configTypes = TypeCache.GetTypesWithAttribute<CreatePlayModeConfigurationMenuAttribute>();

            if (configTypes.Count == 0)
                return null;

            if (configTypes.Count == 1)
            {
                var configType = configTypes[0];
                if (!ValidatePlayModeConfigType(configType))
                    throw new InvalidOperationException("Invalid PlayModeConfig type.");

                var attribute = CreatePlayModeConfigurationMenuAttribute.GetAttribute(configType);
                var button = new ToolbarButton(() => NewScenarioAction(configType, attribute.NewItemName))
                {
                    name = k_NewConfigurationButtonName,
                    tooltip = k_NewConfigurationButtonTooltip,
                    iconImage = Background.FromTexture2D((Texture2D)EditorGUIUtility.IconContent("Toolbar Plus").image)
                };
                return button;
            }

            var toolbarMenu = new ToolbarMenu() { text = "+" };
            toolbarMenu.style.fontSize = 16;

            foreach (var configType in configTypes)
            {
                if (!ValidatePlayModeConfigType(configType))
                    continue;

                var attribute = CreatePlayModeConfigurationMenuAttribute.GetAttribute(configType);
                toolbarMenu.menu.AppendAction(attribute.Label, _ => { NewScenarioAction(configType, attribute.NewItemName); });
            }

            return toolbarMenu;
        }

        private void NewScenarioAction(Type configType, string newItemName)
            => m_PlayModeListView.ShowAddTextField(configType, newItemName);

        private static bool ValidatePlayModeConfigType(Type type)
        {
            if (!typeof(PlayModeConfiguration).IsAssignableFrom(type))
            {
                Debug.LogWarning($"Type {type} is not a PlayModeConfig. Only types that inherit from PlayModeConfig are allowed to have the CreatePlayModeConfigurationMenuAttribute.");
                return false;
            }

            if (type.IsAbstract)
            {
                Debug.LogWarning($"Type {type} is abstract. Only concrete types are allowed to have the CreatePlayModeConfigurationMenuAttribute.");
                return false;
            }

            return true;
        }

        void SetHelpboxStatus()
        {
            m_DisableEditingHelpbox.style.display = PlayModeManager.instance.CurrentState == PlayModeState.Running ? new StyleEnum<DisplayStyle>(DisplayStyle.Flex) : new StyleEnum<DisplayStyle>(DisplayStyle.None);

            foreach (var visualElement in rootVisualElement.Children())
            {
                visualElement.enabledSelf = PlayModeManager.instance.CurrentState == PlayModeState.NotRunning;
            }

            m_DisableEditingHelpbox.enabledSelf = true;
        }

        private void SelectConfig(PlayModeConfiguration config)
        {
            m_DetailView.SetConfig(config);
        }
    }
}
