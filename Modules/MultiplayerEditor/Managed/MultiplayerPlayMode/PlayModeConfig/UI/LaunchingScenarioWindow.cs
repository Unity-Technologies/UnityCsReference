// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using Unity.PlayMode.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class LaunchingScenarioWindow : EditorWindow
    {
        const string k_Stylesheet = "Multiplayer/UI/LaunchingScenarioWindow.uss";

        private const string k_WindowTitle = "Starting Scenario";
        private const string k_ValidateLabel = "Validating";
        private const string k_PrepareLabel = "Preparing";
        private const string k_DeployLabel = "Deploying";
        private const string k_StartLabel = "Starting";
        private const string k_RunLabel = "Running";
        private const string k_CleanupLabel = "Stopping";
        private const string k_LoadingIconName = "LoadingIcon";

        private const int k_WindowWidth = 400;
        private const int k_WindowHeight = 130;
        private const float k_VerticalPosition = 0.333f;

        // These parameters control the smoothness of the progress bar.
        private const float k_LerpFactor = 0.1f;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (HasOpenInstances<LaunchingScenarioWindow>())
            {
                CloseAll();

                if (MigrationUtility.ShouldDisableMultiplayerPlayMode())
                    return;

                var scenarioConfig = PlayModeScenarioManager.ActiveScenario as OrchestratedScenario;
                if (scenarioConfig == null)
                {
                    return;
                }

                var scenario = scenarioConfig.Scenario;
                if (scenario != null && scenario.StatusData.IsExecutingLaunchingStages())
                    OnScenarioStarted(scenarioConfig);
            }
        }

        private static void CloseAll()
        {
            var windows = Resources.FindObjectsOfTypeAll<LaunchingScenarioWindow>();

            EditorApplication.delayCall += () =>
            {
                foreach (var window in windows)
                    window.Close();
            };
        }

        internal static void OnScenarioStarted(OrchestratedScenario scenarioConfig)
        {
            var window = CreateInstance<LaunchingScenarioWindow>();
            window.SetupAndShow(scenarioConfig);
        }

        private OrchestratedScenario m_ScenarioConfig;
        private ProgressBar m_ProgressBar;
        private Label m_Message;
        private Label m_MainEditorMessage;
        private Label m_EditorInstanceMessage;
        private Label m_LocalInstanceMessage;
        private Button m_CancelButton;

        private void SetupAndShow(OrchestratedScenario scenarioConfig)
        {
            titleContent = new GUIContent(k_WindowTitle);

            maxSize = new Vector2(k_WindowWidth, k_WindowHeight);
            minSize = new Vector2(k_WindowWidth, k_WindowHeight);

            m_ScenarioConfig = scenarioConfig;

            m_ScenarioConfig.Scenario.StatusRefreshed -= OnScenarioStatusRefreshed;
            m_ScenarioConfig.Scenario.StatusRefreshed += OnScenarioStatusRefreshed;

            ShowUtility();

            CenterWindow();
        }

        private void OnDestroy()
        {
            if (m_ScenarioConfig == null || m_ScenarioConfig.Scenario == null)
                return;

            m_ScenarioConfig.Scenario.StatusRefreshed -= OnScenarioStatusRefreshed;
        }

        private void OnScenarioStatusRefreshed(ScenarioStatusData status)
        {
            if (!status.IsExecutingLaunchingStages())
            {
                Close();
                return;
            }

            if (m_Message == null)
                return;

            m_Message.text = $"Scenario is {GetLabelForStage(status.CurrentStage)}...";

            var mainEditorCount = GetInstanceCount<MainEditorController>(m_ScenarioConfig);
            m_MainEditorMessage.text = mainEditorCount > 0 ? $"Activating main editor..." : string.Empty;

            var editorInstanceCount = GetInstanceCount<CloneEditorController>(m_ScenarioConfig);
            m_EditorInstanceMessage.text = editorInstanceCount > 0 ? $"Activating {editorInstanceCount} editor instance(s)..." : string.Empty;

            var localInstanceCount = GetInstanceCount<LocalPlayerController>(m_ScenarioConfig);
            m_LocalInstanceMessage.text = localInstanceCount > 0 ? $"Building {localInstanceCount} local instance(s)..." : string.Empty;

            UpdateProgressBar(m_ProgressBar, status.OverallStatus.Progress);
        }

        internal static string GetLabelForStage(ExecutionStage stage)
        {
            var label = stage switch
            {
                ExecutionStage.Validate => k_ValidateLabel,
                ExecutionStage.Prepare => k_PrepareLabel,
                ExecutionStage.Deploy => k_DeployLabel,
                ExecutionStage.Start => k_StartLabel,
                ExecutionStage.Run => k_RunLabel,
                ExecutionStage.Cleanup => k_CleanupLabel,
                _ => string.Empty,
            };

            Assert.IsFalse(string.IsNullOrEmpty(label), $"No label defined for stage '{stage}'");
            return label;
        }

        private void UpdateProgressBar(ProgressBar progressBar, float progress)
        {
            if (m_ScenarioConfig == null || m_ScenarioConfig.Scenario == null)
            {
                return;
            }

            if (progress == 0)
                progressBar.AddToClassList("progress-bar-zero");
            else
                progressBar.RemoveFromClassList("progress-bar-zero");

            if (progress == 1)
            {
                progressBar.value = 1;
                return;
            }

            progressBar.value = Mathf.Lerp(progressBar.value, progress, k_LerpFactor);
        }

        void CreateGUI()
        {
            m_ProgressBar = new ProgressBar() { lowValue = 0, highValue = 1, };
            m_ScenarioConfig.Scenario.StatusRefreshed += OnScenarioStatusRefreshed;

            var LoadingIcon = new Image() { name = k_LoadingIconName, image = Icons.GetImage(Icons.ImageName.Loading) };
            UIUtils.Spin(LoadingIcon);

            m_Message = new Label();

            var instanceMessageContainer = new VisualElement();

            m_MainEditorMessage = new Label();
            m_EditorInstanceMessage = new Label();
            m_LocalInstanceMessage = new Label();

            instanceMessageContainer.Add(m_MainEditorMessage);
            instanceMessageContainer.Add(m_EditorInstanceMessage);
            instanceMessageContainer.Add(m_LocalInstanceMessage);

            m_CancelButton = new Button()
            {
                text = "Cancel"
            };
            m_CancelButton.RegisterCallback<ClickEvent>(evt =>
            {
                ScenarioManagerProvider.instance.Stop();
            });

            m_CancelButton.AddToClassList("cancel-button");

            var messageBar = new VisualElement();
            messageBar.style.flexDirection = FlexDirection.Row;

            messageBar.Add(LoadingIcon);
            messageBar.Add(m_Message);

            var progressBarContainer = new VisualElement();
            progressBarContainer.AddToClassList("progress-bar-container");
            m_ProgressBar.AddToClassList("progress-bar");

            progressBarContainer.Add(m_ProgressBar);

            rootVisualElement.Add(progressBarContainer);
            rootVisualElement.Add(messageBar);
            rootVisualElement.Add(instanceMessageContainer);
            rootVisualElement.Add(m_CancelButton);

            rootVisualElement.styleSheets.Add(EditorGUIUtility.LoadRequired(k_Stylesheet) as StyleSheet);
        }

        private static int GetInstanceCount<T>(OrchestratedScenario scenarioConfig) where T : InstanceController
        {
            var instanceCount = 0;
            var allInstances = scenarioConfig.GetAllInstances();
            foreach (var instanceItem in allInstances)
            {
                if (instanceItem.GetRunMode() == RunModeState.ScenarioControl &&
                    instanceItem.IsInstanceType(typeof(T)))
                {
                    instanceCount++;
                }
            }
            return instanceCount;
        }

        private void CenterWindow()
        {
            var mainWindowRect = EditorGUIUtility.GetMainWindowPosition();
            var windowSize = position.size;

            position = new Rect(
                (mainWindowRect.width - windowSize.x) / 2 + mainWindowRect.x,
                mainWindowRect.height * k_VerticalPosition - windowSize.y * 0.5f + mainWindowRect.y,
                windowSize.x,
                windowSize.y);
        }
    }
}
