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

            rootVisualElement.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            rootVisualElement.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (PlayModeManager.instance.CurrentState == PlayModeState.Running)
            {
                rootVisualElement.focusController.IgnoreEvent(evt);
                evt.StopPropagation();
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (PlayModeManager.instance.CurrentState == PlayModeState.Running)
            {
                rootVisualElement.focusController.IgnoreEvent(evt);
                evt.StopPropagation();
            }
        }

        private VisualElement CreateNewScenarioMenu()
        {
            var typesCount = PlayModeConfigurationUtils.ConfigurationTypesCount;

            if (typesCount == 0)
                return null;

            var configTypes = PlayModeConfigurationUtils.GetPlayModeConfigurationTypes();

            if (typesCount == 1)
            {
                var enumerator = configTypes.GetEnumerator();
                enumerator.MoveNext();
                var configTypeData = enumerator.Current;
                var button = new ToolbarButton(() => NewScenarioAction(configTypeData.ConfigurationType, configTypeData.NewItemName))
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
                toolbarMenu.menu.AppendAction(configType.Label, _ => { NewScenarioAction(configType.ConfigurationType, configType.NewItemName); });
            }

            return toolbarMenu;
        }

        private void NewScenarioAction(Type configType, string newItemName)
            => m_PlayModeListView.ShowAddTextField(configType, newItemName);

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
