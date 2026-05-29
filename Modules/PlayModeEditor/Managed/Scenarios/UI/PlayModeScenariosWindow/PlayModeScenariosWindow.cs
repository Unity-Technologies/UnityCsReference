// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.PlayMode.Editor
{
    class PlayModeScenariosWindow : EditorWindow
    {
        internal const float k_MinWidth = 700;
        internal const float k_MinHeight = 400;
        internal const string k_NewConfigurationButtonName = "NewConfigurationButton";
        internal const string k_NewConfigurationButtonTooltip = "Create a new Play Mode Scenario";
        internal const string k_WindowTitle = "Play Mode Scenarios";

        DetailView m_DetailView;
        HelpBox m_DisableEditingHelpbox;
        PlayModeListView m_PlayModeListView;
        [SerializeField] PlayModeScenario m_LastSelectedScenario;

        public static void ShowWindow()
        {
            var window = GetWindow<PlayModeScenariosWindow>(k_WindowTitle);
            window.m_PlayModeListView.TrySelect(PlayModeScenarioManager.ActiveScenario);
        }

        public void OnEnable()
        {
            minSize = new Vector2(k_MinWidth, k_MinHeight);

            m_DisableEditingHelpbox = new HelpBox("Editing is not allowed in Playmode.", HelpBoxMessageType.Info);
            rootVisualElement.Add(m_DisableEditingHelpbox);

            rootVisualElement.Add(CreateToolbar());

            var splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(splitView);

            m_DetailView = new DetailView();
            m_DetailView.viewDataKey = $"{nameof(PlayModeScenariosWindow)}.{nameof(DetailView)}";

            m_PlayModeListView = new PlayModeListView();
            m_PlayModeListView.OnConfigSelected += SelectConfig;

            if (m_LastSelectedScenario != null)
                m_PlayModeListView.TrySelect(m_LastSelectedScenario);

            m_DetailView.SetConfig(m_PlayModeListView.ConfigurationToEdit);

            splitView.Add(m_PlayModeListView);
            splitView.Add(m_DetailView);

            SetHelpboxStatus();
            ScenarioManagerProvider.instance.StateChanged += (state) =>
            {
                SetHelpboxStatus();
            };

            rootVisualElement.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            rootVisualElement.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (PlayModeScenarioManager.State == PlayModeScenarioState.Running)
            {
                rootVisualElement.focusController.IgnoreEvent(evt);
                evt.StopPropagation();
            }
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (PlayModeScenarioManager.State == PlayModeScenarioState.Running)
            {
                rootVisualElement.focusController.IgnoreEvent(evt);
                evt.StopPropagation();
            }
        }

        VisualElement CreateToolbar()
        {
            if (PlayModeScenarioManager.ScenarioTypesCount == 0)
                return null;

            var creatableScenarioTypes = new List<PlayModeScenarioManager.ScenarioTypeData>();
            foreach (var scenarioType in PlayModeScenarioManager.GetScenarioTypes())
            {
                if (string.IsNullOrEmpty(scenarioType.Label))
                    continue;

                creatableScenarioTypes.Add(scenarioType);
            }

            if (creatableScenarioTypes.Count == 0)
                return null;

            var toolbar = new UnityEditor.UIElements.Toolbar();
            toolbar.Add(CreateNewScenarioMenu(creatableScenarioTypes));
            return toolbar;
        }

        private VisualElement CreateNewScenarioMenu(List<PlayModeScenarioManager.ScenarioTypeData> scenarioTypes)
        {
            Assert.IsTrue(scenarioTypes.Count > 0, "There should be at least one creatable scenario type.");

            if (scenarioTypes.Count == 1)
            {
                var enumerator = scenarioTypes.GetEnumerator();
                enumerator.MoveNext();
                var configTypeData = enumerator.Current;
                var button = new ToolbarButton(() => NewScenarioAction(configTypeData.ScenarioType, configTypeData.NewItemName))
                {
                    name = k_NewConfigurationButtonName,
                    tooltip = k_NewConfigurationButtonTooltip,
                    iconImage = Background.FromTexture2D((Texture2D)EditorGUIUtility.IconContent("Toolbar Plus").image)
                };
                return button;
            }

            var toolbarMenu = new ToolbarMenu() { text = "+" };
            toolbarMenu.style.fontSize = 16;

            foreach (var configType in scenarioTypes)
            {
                toolbarMenu.menu.AppendAction(configType.Label, _ => { NewScenarioAction(configType.ScenarioType, configType.NewItemName); });
            }

            return toolbarMenu;
        }

        private void NewScenarioAction(Type configType, string newItemName)
            => m_PlayModeListView.ShowAddTextField(configType, newItemName);

        void SetHelpboxStatus()
        {
            m_DisableEditingHelpbox.style.display = PlayModeScenarioManager.State == PlayModeScenarioState.Running ? new StyleEnum<DisplayStyle>(DisplayStyle.Flex) : new StyleEnum<DisplayStyle>(DisplayStyle.None);

            foreach (var visualElement in rootVisualElement.Children())
            {
                visualElement.enabledSelf = PlayModeScenarioManager.State == PlayModeScenarioState.Idle;
            }

            m_DisableEditingHelpbox.enabledSelf = true;
        }

        private void SelectConfig(PlayModeScenario config)
        {
            m_LastSelectedScenario = config;
            m_DetailView.SetConfig(config);
        }
    }
}
