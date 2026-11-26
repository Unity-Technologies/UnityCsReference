// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.PlayMode.Editor
{
    /// <summary>
    /// Content of the PlaymodePopup that gets shown when the user opens the scenario dropdown
    /// </summary>
    class PlaymodePopupContent : PopupWindowContent
    {
        const float k_DefaultWidth = 220f;
        const float k_Height = 114f;
        // Footer label padding (4px each side in Framework.uss) plus a margin for IMGUI vs UI Toolkit text measurement.
        const float k_FooterLabelHorizontalPadding = 16f;
        internal const float maxWidth = 400f;
        const string k_Stylesheet = "PlayMode/UI/Framework.uss";
        static readonly string k_StatusButtonText = L10n.Tr("Active Play Mode Window", null);
        static readonly string k_ManageButtonText = L10n.Tr("Configure Play Mode Scenarios", null);

        // Name is used in tests to identify the element.
        public const string listElementName = "playmode-config-list";
        ListView m_ListView;
        Label m_ManageButton;

        readonly float m_Width = CalculateWidth(new[] { k_StatusButtonText, k_ManageButtonText });

        internal static float CalculateWidth(IEnumerable<string> footerLabels)
        {
            // EditorStyles is only initialized inside an IMGUI pass; the built-in skin is available anywhere.
            var labelStyle = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).label;
            var widest = 0f;
            foreach (var label in footerLabels)
                widest = Mathf.Max(widest, labelStyle.CalcSize(new GUIContent(label)).x + k_FooterLabelHorizontalPadding);
            return Mathf.Clamp(widest, k_DefaultWidth, maxWidth);
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(m_Width, k_Height);
        }

        public override VisualElement CreateGUI()
        {
            return SetupUI();
        }

        public override void OnOpen()
        {
            ScenarioManagerProvider.instance.StateChanged += HandleStateChange;
        }

        public override void OnClose()
        {
            ScenarioManagerProvider.instance.StateChanged -= HandleStateChange;
        }

        void HandleStateChange(PlayModeScenarioState state)
        {
            m_ListView.enabledSelf = state == PlayModeScenarioState.Idle;
            UpdateManageButton(state);
        }

        void UpdateManageButton(PlayModeScenarioState state)
        {
            // The Play Mode Scenarios window disables its content unless the scenario is idle.
            m_ManageButton.enabledSelf = state == PlayModeScenarioState.Idle;
        }

        VisualElement SetupUI()
        {
            var root = new VisualElement();
            root.style.maxHeight = k_Height;
            root.style.minWidth = m_Width;
            root.style.maxWidth = m_Width;
            var enableListView = PlayModeScenarioManager.State == PlayModeScenarioState.Idle && Application.isPlaying == false;
            m_ListView = new ListView { fixedItemHeight = 20, selectionType = SelectionType.Single, enabledSelf = enableListView };
            m_ListView.selectionChanged += OnItemSelected;
            m_ListView.name = listElementName;
            m_ListView.AddToClassList("unity-scenarios-playmode-popup__config-list");
            root.Add(m_ListView);

            var statusButton = new Label() { name = "open-playmode-status-button", text = k_StatusButtonText };
            statusButton.AddToClassList("unity-scenarios-playmode-popup__status-button");
            statusButton.RegisterCallback<ClickEvent>(evt => ActiveScenarioWindow.OpenWindow());

            var manageButton = new Label() { name = "manage-playmode-scenarios-configs-button", text = k_ManageButtonText };
            m_ManageButton = manageButton;
            PlayModeScenariosWindow.ExplainEditingNotAllowedWhenDisabled(manageButton);
            UpdateManageButton(PlayModeScenarioManager.State);
            manageButton.AddToClassList("unity-scenarios-playmode-popup__manage-button");
            manageButton.RegisterCallback<ClickEvent>(evt => PlayModeScenariosWindow.ShowWindow());

            root.Add(statusButton);
            root.Add(manageButton);

            root.styleSheets.Add(EditorGUIUtility.LoadRequired(k_Stylesheet) as StyleSheet);
            RefreshList();
            return root;
        }

        void OnItemSelected(IEnumerable<object> selection)
        {
            PlayModeScenario config = null;
            foreach (var item in selection)
            {
                config = item as PlayModeScenario;
                if (config != null)
                    break;
            }

            if (config == null)
                return;

            var currConfig = PlayModeScenarioManager.ActiveScenario;
            if (currConfig == null || currConfig.WantsToDeselect())
                PlayModeScenarioManager.ActiveScenario = config;

            editorWindow.Close();
        }

        void RefreshList()
        {
            var configs = new List<PlayModeScenario>();
            configs.AddRange(PlayModeScenarioUtils.GetAllConfigs());
            m_ListView.itemsSource = configs;
            m_ListView.Rebuild();
            var selectedConfigIndex = configs.FindIndex(c => c == PlayModeScenarioManager.ActiveScenario);
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
                container.RegisterCallback<ClickEvent>(_ => editorWindow.Close());
                return container;
            };

            m_ListView.bindItem = (element, i) =>
            {
                var label = element.Q<Label>();
                var icon = element.Q<Image>("type-icon");
                var warningIcon = element.Q<Image>("warn-icon");
                var config = configs[i];

                var configIsValid = config.IsValid(out var reason);
                element.RemoveFromClassList("unity-scenarios-playmode-popup__item--has-warning");
                if (!configIsValid)
                {
                    element.AddToClassList("unity-scenarios-playmode-popup__item--has-warning");
                }

                warningIcon.tooltip = reason;

                label.text = config.name;
                icon.image = config.Icon;
                element.userData = config;

                // add the name to the tooltip if the label cannot be rendered because the line is not long enough.
                element.tooltip = label.text + "\n \n" + config.DescriptionTooltip;
            };
        }
    }
}
