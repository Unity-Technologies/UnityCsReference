// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.PlayMode.Editor;
using UnityEditor.Multiplayer.Internal;

namespace Unity.Multiplayer.PlayMode.Editor
{
    /// <summary>
    /// The content of the status window that is shown, when a user clicks
    /// <see cref="MultiplayerPlayModeStatusButton"/>
    /// </summary>
    class PlaymodeStatusPopupContent : PopupWindowContent
    {
        const string k_Stylesheet = "Multiplayer/UI/PlaymodeStatusPopupContent.uss";
        const string k_StylesheetDark = "Multiplayer/UI/PlaymodeStatusPopupContentDark.uss";
        const string k_StylesheetLight = "Multiplayer/UI/PlaymodeStatusPopupContentLight.uss";

        const string k_HeadlineName = "headline";
        private const string k_StatusButton = "status-button";
        const string k_InstanceListContainer = "status-list";

        const string k_Title = "Instances Status";

        public static readonly Vector2 windowSize = new Vector2(300, 175);
        static Dictionary<InstanceView, Instance> m_ViewToInstance = new();

        public override Vector2 GetWindowSize()
        {
            return windowSize;
        }

        public override VisualElement CreateGUI()
        {
            var container = new ScrollView() { name = k_InstanceListContainer };
            container.style.minHeight = container.style.maxHeight = windowSize.y;
            container.style.minWidth = container.style.maxWidth = windowSize.x;

            var headline = new Label(k_Title) { name = k_HeadlineName };

            var statusButton = new Button() { name = k_StatusButton, text = "Open Status Window" };

            var headlineContainer = new VisualElement();

            headlineContainer.AddToClassList("headline-container");

            statusButton.tooltip = "Open Play Mode Status Window";

            statusButton.RegisterCallback<ClickEvent>(evt => ActiveScenarioWindow.OpenWindow());

            headlineContainer.Add(headline);
            headlineContainer.Add(statusButton);
            container.Add(headlineContainer);
            container.schedule.Execute(() => UpdateInstanceStatus()).Every(1000);

            container.styleSheets.Add(EditorGUIUtility.LoadRequired(k_Stylesheet) as StyleSheet);
            var stylesheet = EditorGUIUtility.isProSkin ? k_StylesheetDark : k_StylesheetLight;
            container.styleSheets.Add(EditorGUIUtility.LoadRequired(stylesheet) as StyleSheet);

            m_ViewToInstance.Clear();
            var currentConfig = PlayModeScenarioManager.ActiveScenario as OrchestratedScenario;

            if (currentConfig == null)
                return new VisualElement() { name = "no content" };

            var instanceItems = currentConfig.GetAllInstances();

            foreach (var instanceItem in instanceItems)
            {
                var instance = GetInstanceByItem(instanceItem);
                if (instance == null)
                    continue;

                var instanceView = new InstanceView(instance);
                m_ViewToInstance.Add(instanceView, instance);
                container.Add(instanceView);
            }

            return container;
        }

        static void UpdateInstanceStatus()
        {
            foreach (var (view, instance) in m_ViewToInstance)
            {
                view.SetStatus(instance.StatusData);
                view.RefreshFreeRunUI();
            }
        }

        class InstanceView : VisualElement
        {
            internal readonly Instance m_Instance;
            readonly VisualElement m_StatusIndicator;
            readonly VisualElement m_DriftIndicator;
            readonly Image m_RunModeIndicator;
            const string k_InstanceViewClass = "instance-view";
            const string k_InstanceIconName = "instance-icon";
            const string k_InstanceNameName = "instance-name";
            const string k_InstanceContainerName = "instance-name-container";
            const string k_StatusContainerName = "status-container";
            const string k_StatusIndicatorName = "status-indicator";
            const string k_RunModeIndicatorName = "runmode-indicator";
            const string k_DriftIconName = "drift-icon";
            const string k_DriftToolTip = "This instance might be drifting. This is caused by running an instance for a long time while possible changes were detected in the Main Editor. Consider exiting and restarting the instance.";

            const string k_ActiveClass = "active";
            const string k_ErrorClass = "error";
            const string k_LoadingClass = "loading";
            const string k_IdleClass = "idle";

            internal InstanceView(Instance instance)
            {
                m_Instance = instance;
                AddToClassList(k_InstanceViewClass);
                AddToClassList(k_ActiveClass);

                var instanceIcon = new VisualElement() { name = k_InstanceIconName };
                instanceIcon.AddToClassList("icon");
                var nameLabel = new Label(instance.Name) { name = k_InstanceNameName };

                var instanceContainer = new VisualElement() { name = k_InstanceContainerName };
                instanceContainer.Add(instanceIcon);
                instanceContainer.Add(nameLabel);

                var statusContainer = new VisualElement() { name = k_StatusContainerName };
                m_StatusIndicator = new VisualElement() { name = k_StatusIndicatorName };
                m_StatusIndicator.AddToClassList("icon");
                m_DriftIndicator = new VisualElement() { name = k_DriftIconName };
                m_DriftIndicator.AddToClassList("icon");
                m_DriftIndicator.tooltip = k_DriftToolTip;
                m_DriftIndicator.style.backgroundImage = Icons.GetImage(Icons.ImageName.Drift);
                m_RunModeIndicator = new Image() { name = k_RunModeIndicatorName };
                m_RunModeIndicator.AddToClassList("icon");
                m_RunModeIndicator.style.paddingRight = 2;

                statusContainer.Add(m_DriftIndicator);
                statusContainer.Add(m_RunModeIndicator);
                statusContainer.Add(m_StatusIndicator);

                var roleLabel = new Label();
                statusContainer.Add(roleLabel);

                // Todo: this info should come from the configuration and not be branched here.
                if (instance.Controller is MainEditorController mainEditorController)
                {
                    instanceIcon.style.backgroundImage = EditorGUIUtility.FindTexture("UnityLogo");
                    roleLabel.style.display = EditorMultiplayerManager.enableMultiplayerRoles ? DisplayStyle.Flex : DisplayStyle.None;
                    if (EditorMultiplayerManager.enableMultiplayerRoles)
                    {
                        roleLabel.text = mainEditorController.Settings.RoleMask.ToString();
                    }
                    m_RunModeIndicator.visible = false;
                }
                else if (instance.Controller is CloneEditorController cloneEditorController)
                {
                    instanceIcon.style.backgroundImage = EditorGUIUtility.FindTexture("UnityLogo");
                    roleLabel.style.display = EditorMultiplayerManager.enableMultiplayerRoles ? DisplayStyle.Flex : DisplayStyle.None;
                    if (EditorMultiplayerManager.enableMultiplayerRoles)
                    {
                        roleLabel.text = cloneEditorController.Settings.RoleMask.ToString();
                    }
                }
                else if (instance.Controller is LocalPlayerController localController)
                {
                    instanceIcon.style.backgroundImage = InternalUtilities.GetBuildProfileTypeIcon(localController.Settings.BuildProfile);
                    roleLabel.text = "no role";
                    if (localController.Settings.BuildProfile != null)
                        roleLabel.text = MultiplayerRolesSettings.instance.GetMultiplayerRoleForBuildProfile(localController.Settings.BuildProfile).ToString();
                }

                Add(instanceContainer);
                Add(statusContainer);
            }

            internal void SetStatus(InstanceStatusData status)
            {
                RemoveFromClassList(k_ActiveClass);
                RemoveFromClassList(k_ErrorClass);
                RemoveFromClassList(k_LoadingClass);
                RemoveFromClassList(k_IdleClass);
                switch (status.OverallStatus.State)
                {
                    case ExecutionState.Running:
                        if (status.IsExecutingRunningStage())
                        {
                            AddToClassList(k_ActiveClass);
                            m_StatusIndicator.tooltip = "running";
                        }
                        else
                        {
                            AddToClassList(k_LoadingClass);
                            m_StatusIndicator.tooltip = "loading";
                        }
                        break;
                    case ExecutionState.Failed:
                        AddToClassList(k_ErrorClass);
                        m_StatusIndicator.tooltip = "error";
                        break;
                    default:
                        AddToClassList(k_IdleClass);
                        m_StatusIndicator.tooltip = "idle";
                        break;
                }
            }

            internal void RefreshFreeRunUI()
            {
                // Grab the running mode and update the visual icon if it's changed.
                var currRunMode = m_Instance.RunModeState;
                m_RunModeIndicator.SetRunModeIcon(currRunMode);

                // Grab the tool tip for the current mode and update if it's changed.
                var toolTipText = GetRunModeToolTip(currRunMode);
                if (!m_RunModeIndicator.tooltip.Equals(toolTipText))
                    m_RunModeIndicator.tooltip = toolTipText;

                // Refresh the coherence drift UI for free run instances
                var instance = ScenarioRunner.instance.ActiveScenario?.GetInstanceByName(m_Instance.Name);
                if (instance != null)
                    m_DriftIndicator.visible = instance.Drifted;
            }

            private string GetRunModeToolTip(RunModeState runMode)
            {
                switch (runMode)
                {
                    case RunModeState.ScenarioControl:
                        if (m_Instance.Controller.GetType().IsSubclassOf(typeof(EditorController<>)))
                            return "This instance is controlled by the main editor process.\n" +
                                   "It will be activated when entering play mode.";
                        if (m_Instance.Controller is LocalPlayerController)
                            return "This instance is controlled by the main editor process.\n" +
                                   "It will build and run when entering play mode.";

                        // Default tool tip for manually controlled instances.
                        return "This instance is controlled by the main editor process.";
                    case RunModeState.ManualControl:
                        return "This instance is controlled manually. It can be controlled separately from play mode.";
                }

                return String.Empty;
            }
        }

        private Instance GetInstanceByItem(IInstanceItem instanceItem)
        {
            var currentConfig = PlayModeScenarioManager.ActiveScenario as OrchestratedScenario;
            if (currentConfig == null || currentConfig.Scenario == null)
                return null;

            return currentConfig.Scenario.GetInstanceById(instanceItem.GetId());
        }
    }
}
