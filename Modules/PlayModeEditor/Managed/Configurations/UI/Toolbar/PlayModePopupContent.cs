// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlayMode.Editor
{
    /// <summary>PlaymodePopupContent
    /// Content of the PlaymodePopup that gets shown when the user presses the PlaymodeDropDownButton
    /// <see cref="PlaymodeDropdownButton"/>
    /// </summary>
    class PlaymodePopupContent : PopupWindowContent
    {
        public static readonly Vector2 windowSize = new Vector2(220, 114);
        const string k_Stylesheet = "PlayMode/UI/Framework.uss";

        public static Action OpenPlayModeConfigurationsWindowDelegate;

        // Name is used in tests to identify the element.
        public const string listElementName = "playmode-config-list";
        ListView m_ListView;

        public override Vector2 GetWindowSize()
        {
            return windowSize;
        }

        public override VisualElement CreateGUI()
        {
            return SetupUI();
        }

        public override void OnOpen()
        {
            PlayModeManager.instance.StateChanged += HandleStateChange;
        }

        public override void OnClose()
        {
            PlayModeManager.instance.StateChanged -= HandleStateChange;
        }

        void HandleStateChange(PlayModeState state)
        {
            m_ListView.enabledSelf = state == PlayModeState.NotRunning;
        }

        VisualElement SetupUI()
        {
            var root = new VisualElement();
            root.style.maxHeight = windowSize.y;
            root.style.minWidth = windowSize.x;
            root.style.maxWidth = windowSize.x;
            var enableListView = PlayModeManager.instance.CurrentState == PlayModeState.NotRunning && Application.isPlaying == false;
            m_ListView = new ListView { fixedItemHeight = 20, selectionType = SelectionType.Single, enabledSelf = enableListView };
            m_ListView.selectionChanged += OnItemSelected;
            m_ListView.name = listElementName;
            m_ListView.AddToClassList("unity-scenarios-playmode-popup__config-list");
            root.Add(m_ListView);

            var statusButton = new Label() { name = "open-playmode-status-button", text = "Play Mode Status Window" };
            statusButton.AddToClassList("unity-scenarios-playmode-popup__status-button");
            statusButton.RegisterCallback<ClickEvent>(evt => OpenPlayModeConfigurationsWindowDelegate?.Invoke());

            var manageButton = new Label() { name = "manage-playmode-scenarios-configs-button", text = "Configure Play Mode Scenarios" };
            manageButton.AddToClassList("unity-scenarios-playmode-popup__manage-button");
            manageButton.RegisterCallback<ClickEvent>(evt => PlayModeConfigurationsWindow.ShowWindow());

            root.Add(statusButton);
            root.Add(manageButton);

            root.styleSheets.Add(EditorGUIUtility.LoadRequired(k_Stylesheet) as StyleSheet);
            RefreshList();
            return root;
        }

        void OnItemSelected(IEnumerable<object> selection)
        {
            PlayModeConfiguration config = null;
            foreach (var item in selection)
            {
                config = item as PlayModeConfiguration;
                if (config != null)
                    break;
            }

            if (config == null)
                return;

            var currConfig = PlayModeManager.instance.ActivePlayModeConfig;
            if (currConfig == null || currConfig.WantsToDeselectConfiguration())
                PlayModeManager.instance.ActivePlayModeConfig = config;

            editorWindow.Close();
        }

        void RefreshList()
        {
            var configs = new List<PlayModeConfiguration>();
            configs.Add(PlayModeManager.instance.DefaultConfig);
            configs.AddRange(PlayModeConfigurationUtils.GetAllConfigs());
            m_ListView.itemsSource = configs;
            m_ListView.Rebuild();
            var selectedConfigIndex = configs.FindIndex(c => c == PlayModeManager.instance.ActivePlayModeConfig);
            m_ListView.SetSelectionWithoutNotify(new List<int> { selectedConfigIndex });
            m_ListView.makeItem = () =>
            {
                var container = new VisualElement();
                container.AddToClassList("unity-scenarios-playmode-list-view__item--scenario-window");
                container.style.flexDirection = FlexDirection.Row;
                container.Add(new Image() { name = "type-icon" });
                container.Add(new Label() { displayTooltipWhenElided = false });
                var warnIcon = new Image() { name = "warn-icon" };
                warnIcon.AddToClassList("unity-scenarios-playmode-popup__warn-icon");
                warnIcon.image = EditorGUIUtility.FindTexture("console.warnicon");
                container.Add(warnIcon);
                return container;
            };

            m_ListView.bindItem = (element, i) =>
            {
                var label = element.Q<Label>();
                var icon = element.Q<Image>("type-icon");
                var warningIcon = element.Q<Image>("warn-icon");
                var config = configs[i];

                var configIsValid = config.IsConfigurationValid(out var reason);
                element.RemoveFromClassList("unity-scenarios-playmode-popup__item--has-warning");
                if (!configIsValid)
                {
                    element.AddToClassList("unity-scenarios-playmode-popup__item--has-warning");
                }

                warningIcon.tooltip = reason;

                label.text = config.name;
                icon.image = config.Icon;
                element.userData = config;

                var tooltip = string.IsNullOrEmpty(config.Description) ? "No description available." : config.Description;
                // add the name to the tooltip if the label cannot be rendered because the line is not long enough.
                element.tooltip = label.text + "\n \n" + tooltip;
            };
        }
    }
}
