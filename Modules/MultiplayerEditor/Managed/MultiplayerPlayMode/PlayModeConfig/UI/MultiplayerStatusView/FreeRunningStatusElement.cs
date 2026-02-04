// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.PlayMode.Editor;
using UnityEngine.UIElements;
using UnityEngine;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class FreeRunningStatusElement
    {
        // Names representing views
        internal const string k_MultiplayerRunningModeName = "multiplayer-mode-state-field";
        internal const string k_InstanceButtonName = "multiplayer-mode-freerun-button";

        // Text values used in controls or labels
        private const string k_MultiplayerRunningModeLabelText = "Running Mode";
        private const string k_MultiplayerRunningModeTooltipText =
            k_DropDownScenarioControlText + " means the instance will be activated/deactivated when entering/exiting play mode. " +
            k_DropDownManualControlText + " means the instance can be activated only once and reused, which can improve iteration time.";
        private const string k_InstanceButtonCloneActivateText = "Activate";
        private const string k_InstanceButtonCloneDeactivateText = "Deactivate";
        private const string k_InstanceButtonStartText = "Run";
        private const string k_InstanceButtonStopText = "Stop";
        private const string k_InstanceCancelText = "Cancel";
        private const string k_DropDownScenarioControlText = "Scenario Control";
        private const string k_DropDownManualControlText = "Manual Control";
        private const string k_InstanceButtonToolTipText = "Please correct invalid scenario configurational warnings.";
        private const string k_DisabledFreeRunButtonHelpBoxText = "This instance can only be manually started during Edit Mode.";

        // Class names representing view styling
        private const string k_HighlightedBackgroundClassName = "instance-view-highlighted";
        private const string k_RunModeDropDownMenuClassName = "runmode-dropdown";
        private const string k_RunModeButtonClassName = "runmode-button";

        private string m_ButtonCallToActionStartText;
        private string m_ButtonCallToActionStopText;

        // List of Mode states to be shown in Run Mode dropdown controls.
        private readonly List<RunModeState> k_DropdownStates = new List<RunModeState>()
        {
            RunModeState.ScenarioControl,
            RunModeState.ManualControl
        };

        private Instance m_Instance;
        private PopupField<RunModeState> m_DropDown;
        private Image m_DropdownRunModeImage = null;
        private Button m_FreeRunButton;
        private HelpBox m_DisabledFreeRunButtonHelpbox;

        public FreeRunningStatusElement(Instance instance)
        {
            m_Instance = instance;

            var isVirtualInstance = instance.Controller is CloneEditorController;
            m_ButtonCallToActionStartText =
                isVirtualInstance ? k_InstanceButtonCloneActivateText : k_InstanceButtonStartText;
            m_ButtonCallToActionStopText =
                isVirtualInstance ? k_InstanceButtonCloneDeactivateText : k_InstanceButtonStopText;
        }

        internal void BindRunModeDropDownElement(
            VisualElement instanceContainer,
            VisualElement statusContainer,
            VisualElement freeRunButtonContainer)
        {
            // Create and bind Running Mode Label
            var runLabel = new Label();
            runLabel.text = k_MultiplayerRunningModeLabelText;
            runLabel.tooltip = k_MultiplayerRunningModeTooltipText;
            instanceContainer.Add(runLabel);

            // Create and bind Running Mode Dropdown menu
            m_DropDown = new PopupField<RunModeState>() { name = k_MultiplayerRunningModeName };
            m_DropDown.choices = k_DropdownStates;
            m_DropDown.formatListItemCallback = FormatRunningModeDropDownText;
            m_DropDown.formatSelectedValueCallback = FormatRunningModeDropDownText;
            m_DropDown.tooltip = k_MultiplayerRunningModeTooltipText;
            m_DropDown.SetValueWithoutNotify(m_Instance.RunModeState);
            m_DropDown.AddToClassList(k_RunModeDropDownMenuClassName);
            ExtendDropDownUI();
            statusContainer.Add(m_DropDown);

            // Create and bind the Free Running Activate / Deactivate Button
            var instanceActionButton = new Button(){ name = k_InstanceButtonName };
            instanceActionButton.RegisterCallback<ClickEvent>(OnCallToActionButtonClicked);
            freeRunButtonContainer.AddToClassList(k_RunModeButtonClassName);
            freeRunButtonContainer.Clear();
            freeRunButtonContainer.Add(instanceActionButton);

            // Add help boxes for instances that can't be run during PlayMode.
            m_DisabledFreeRunButtonHelpbox =
                new HelpBox(k_DisabledFreeRunButtonHelpBoxText, HelpBoxMessageType.Info) { name = "help-box" };
            m_DisabledFreeRunButtonHelpbox.style.display = DisplayStyle.None;
            freeRunButtonContainer.Add(m_DisabledFreeRunButtonHelpbox);

            // Also keep a reference views for hide / show purposes
            m_FreeRunButton = instanceActionButton;

            // If the scenario config is not valid, disable the call to action button
            var currentConfig = PlayModeScenarioManager.ActiveScenario as OrchestratedScenario;
            var isValid = currentConfig != null && currentConfig.IsValid(out string _);
            instanceActionButton.SetEnabled(isValid);
            instanceActionButton.tooltip = isValid ? "" : k_InstanceButtonToolTipText;

            // Register callbacks to update Instance and UI
            m_DropDown.RegisterValueChangedCallback(OnSetFreeRunningModeSelected);

            // Finally refresh the UI after binding all elements.
            UpdateUI();

            // Ensure that we update Instance Status when shown.
            instanceContainer.schedule.Execute(() =>
            {
                if (IsInstanceRunning())
                    UpdateUI();
            }).Every(1000);

            // Listen to play mode state changes, needed for Instance Call-to-Action buttons.
            ScenarioManagerProvider.instance.StateChanged += (state) =>
            {
                UpdateUI();
            };

            m_Instance.StatusRefreshed += (instance, status) =>
            {
                UpdateUI();
            };
        }

        private void OnSetFreeRunningModeSelected(ChangeEvent<RunModeState> evt)
        {
            // If a runtime instance is already available, update its mode.
            if (m_Instance != null)
                m_Instance.RunModeState = evt.newValue;
            UpdateUI();
        }

        // Extend the DropDownUI to also include and inject the running mode icon in its Header.
        // This is because Unity Foundational UI does not support DropDown Menus with Icons.
        private void ExtendDropDownUI()
        {
            // Grab the Dropdown container within which to inject the running mode icon
            var popupContainer = m_DropDown.contentContainer.Q(className:"unity-base-field__input");
            if (popupContainer == null)
                return;

            // Then from the container, save references to child elements before clearing it.
            var dropDownViews = new List<VisualElement>();
            dropDownViews.AddRange(popupContainer.Children());
            popupContainer.Clear();

            // Next, create and inject the Running Mode icon into the Dropdown
            m_DropdownRunModeImage = new Image { style = { paddingLeft = 2, paddingRight = 2 } };
            popupContainer.Add(m_DropdownRunModeImage);

            // Finally, restore the child references that we previously saved.
            foreach(var child in dropDownViews)
                popupContainer.Add(child);
        }

        private string FormatRunningModeDropDownText(RunModeState state)
        {
            switch (state)
            {
                case RunModeState.ScenarioControl:
                    return k_DropDownScenarioControlText;
                case RunModeState.ManualControl:
                    return k_DropDownManualControlText;
            }

            throw new Exception($"Unsupported Run mode state for Instance: {state}");
        }

        private void UpdateUI()
        {
            // Only show the call-to-action button if we are in Manual Mode state.
            bool isManualModeControlled = m_Instance.RunModeState == RunModeState.ManualControl;
            m_FreeRunButton.style.display = isManualModeControlled ? DisplayStyle.Flex : DisplayStyle.None;

            // Update call-to-action button text as needed
            var isInstanceRunning = IsInstanceRunning();
            UpdateFreeRunButtonText(isInstanceRunning);
            UpdateButtonActiveState();

            // Disable Dropdown if scenario is running
            if (ScenarioRunner.instance.ActiveScenario != null)
            {
                var scenarioState = ScenarioRunner.instance.ActiveScenario.StatusData.OverallStatus.State;
                var enabledDropdown = scenarioState is not (ExecutionState.Running or ExecutionState.Active);
                m_DropDown.SetEnabled(enabledDropdown && !isInstanceRunning);
            }

            // Update Running Mode Icon if it has not changed.
            if (m_DropdownRunModeImage != null)
                m_DropdownRunModeImage.SetRunModeIcon(m_Instance.RunModeState);
        }

        private void UpdateFreeRunButtonText(bool isActive)
        {
            if (!isActive)
            {
                // If not running - user can click activate button to start it.
                m_FreeRunButton.text = m_ButtonCallToActionStartText;
            }
            else if (!HasInstanceDeployed())
            {
                // Else it is running but not yet deployed - show cancel
                m_FreeRunButton.text = k_InstanceCancelText;
            }
            else
            {
                // Else it is running and active - show deactivate.
                m_FreeRunButton.text = m_ButtonCallToActionStopText;
            }
        }

        private void UpdateButtonActiveState()
        {
            var isScenarioRunning = ScenarioRunner.instance.IsRunning;
            var isInstanceRunning = IsInstanceRunning();
            var isEditorInstance = m_Instance != null && m_Instance.Controller is CloneEditorController;
            var isButtonDisplayed = m_FreeRunButton.style.display == DisplayStyle.Flex;

            var shouldEnable = isEditorInstance || isInstanceRunning || !isScenarioRunning;
            m_FreeRunButton.SetEnabled(shouldEnable);
            m_DisabledFreeRunButtonHelpbox.style.display = isButtonDisplayed
                                                           && !shouldEnable ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private bool IsInstanceRunning()
        {
            return m_Instance != null &&
                   m_Instance.IsFreeRunMode() &&
                   m_Instance.HasStartedAsFreeRunning() &&
                   m_Instance.StatusData.OverallStatus.State is ExecutionState.Running or ExecutionState.Active;
        }

        private bool HasInstanceDeployed()
        {
            return m_Instance != null &&
                   m_Instance.IsFreeRunMode() &&
                   m_Instance.HasDeployedAndRun();
        }

        private void OnCallToActionButtonClicked(ClickEvent ev)
        {
            if (m_Instance == null)
            {
                Debug.LogWarning("Unable to toggle instance - Please ensure Scenario configurations are valid.");
                return;
            }

            ToggleActivateCloneInstance(!IsInstanceRunning());
            UpdateUI();

            // Because the Config window update is expansive, perform the update here as a single call.
            RefreshPlayModeConfigsWindowIfShown();
        }

        private void ToggleActivateCloneInstance(bool shouldActivate)
        {
            // Sanity check, don't toggle when in invalid modes
            if (m_Instance.RunModeState == RunModeState.ScenarioControl)
            {
                Debug.LogWarning("Cannot Activate an instance while it is in Scenario Control mode.");
                return;
            }

            // Grab the Instance for toggling
            if (m_Instance == null)
            {
                Debug.LogError("Free Running Status Element Error: Unable to locate runtime instance.");
                return;
            }

            // Finally start or terminate it
            if (shouldActivate)
                m_Instance!.StartOrResumeAsFreeRunning(false).Forget();
            else
                m_Instance!.StopAsFreeRunning();
        }

        private void RefreshPlayModeConfigsWindowIfShown()
        {
            // Grab the Play Mode Scenarios Config window if it's showing
            var windows = Resources.FindObjectsOfTypeAll<PlayModeScenariosWindow>();
            if (windows == null || windows.Length != 1)
                return;

            // Ensure Scenario Configs are disabled if there are active Free Running
            // instances to prevent modifications while they are running.
            PlayModeScenariosWindow PlayModeConfigurationsWindow = windows[0];
            DetailView element = PlayModeConfigurationsWindow.rootVisualElement
                .Query<DetailView>(DetailView.k_DetailedViewName);

            if (element != null)
                element.UpdateView();

        }
    }
}
