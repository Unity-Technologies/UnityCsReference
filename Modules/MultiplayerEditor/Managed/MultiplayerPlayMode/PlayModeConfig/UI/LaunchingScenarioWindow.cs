// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.PlayMode.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    class LaunchingScenarioWindow : EditorWindow
    {
        const string k_Stylesheet = "Multiplayer/UI/LaunchingScenarioWindow.uss";

        private const string k_WindowTitle = "Starting Scenario";
        private const string k_PreparingMessage = "Preparing";
        private const string k_DeployingMessage = "Deploying";
        private const string k_LaunchingMessage = "Launching";
        private const string k_LoadingIconName = "LoadingIcon";

        private const int k_WindowWidth = 400;
        private const int k_WindowHeight = 130;
        private const float k_VerticalPosition = 0.333f;

        // These parameters control the smoothness of the progress bar.
        private const int k_UpdateFrequency = 32;
        private const float k_LerpFactor = 0.1f;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (HasOpenInstances<LaunchingScenarioWindow>())
            {
                CloseAll();

                if (MigrationUtility.ShouldDisableMultiplayerPlayMode())
                    return;

                var scenarioConfig = PlayModeManager.instance.ActivePlayModeConfig as ScenarioConfig;
                if (scenarioConfig == null)
                {
                    return;
                }

                var scenario = scenarioConfig.Scenario;
                if (scenario != null && StatusIsLaunching(scenario.Status))
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

        internal static void OnScenarioStarted(ScenarioConfig scenarioConfig)
        {
            var window = CreateInstance<LaunchingScenarioWindow>();
            window.SetupAndShow(scenarioConfig);
        }

        private ScenarioConfig m_ScenarioConfig;
        private ProgressBar m_PrepareProgressBar;
        private ProgressBar m_DeployProgressBar;
        private ProgressBar m_RunProgressBar;
        private Label m_Message;
        private Label m_MainEditorMessage;
        private Label m_EditorInstanceMessage;
        private Label m_LocalInstanceMessage;
        private Label m_RemoteInstanceMessage;
        private Button m_CancelButton;

        private void SetupAndShow(ScenarioConfig scenarioConfig)
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

        private void OnScenarioStatusRefreshed(ScenarioStatus status)
        {
            if (!StatusIsLaunching(status))
            {
                Close();
                return;
            }

            var stage = status.CurrentStage switch
            {
                ExecutionStage.Prepare => k_PreparingMessage,
                ExecutionStage.Deploy => k_DeployingMessage,
                ExecutionStage.Run => k_LaunchingMessage,
                _ => "Error"
            };

            if (m_Message == null)
                return;

            m_Message.text = $"Scenario is {stage}...";

            var mainEditorCount = GetInstanceCount<MainEditorInstanceDescription>(m_ScenarioConfig);
            m_MainEditorMessage.text = mainEditorCount > 0 ? $"Activating main editor..." : string.Empty;

            var editorInstanceCount = GetInstanceCount<VirtualEditorInstanceDescription>(m_ScenarioConfig);
            m_EditorInstanceMessage.text = editorInstanceCount > 0 ? $"Activating {editorInstanceCount} editor instance(s)..." : string.Empty;

            var localInstanceCount = GetInstanceCount<LocalInstanceDescription>(m_ScenarioConfig);
            m_LocalInstanceMessage.text = localInstanceCount > 0 ? $"Building {localInstanceCount} local instance(s)..." : string.Empty;

            var remoteInstanceCount = GetInstanceCount<RemoteInstanceDescription>(m_ScenarioConfig);
            m_RemoteInstanceMessage.text = remoteInstanceCount > 0 ? $"Deploying {remoteInstanceCount} remote instance(s)..." : string.Empty;
        }

        private static bool StatusIsLaunching(ScenarioStatus status)
        {
            if (status.State is not ScenarioState.Running)
                return false;

            return status.CurrentStage switch
            {
                ExecutionStage.Prepare or ExecutionStage.Deploy => true,
                ExecutionStage.Run => status.StageState is not ExecutionState.Active and not ExecutionState.Completed,
                _ => false,
            };
        }

        private void UpdateProgressBar(ProgressBar progressBar, ExecutionStage executionStage)
        {
            if (m_ScenarioConfig == null || m_ScenarioConfig.Scenario == null ||
                m_ScenarioConfig.Scenario.Status.StageProgress == null ||
                m_ScenarioConfig.Scenario.Status.StageProgress.Length == 0)
            {
                return;
            }

            var progress = m_ScenarioConfig.Scenario.Status.StageProgress[(int)executionStage];

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
            m_PrepareProgressBar = new ProgressBar() { lowValue = 0, highValue = 1, };
            m_PrepareProgressBar.schedule.Execute(() =>
            {
                UpdateProgressBar(m_PrepareProgressBar, ExecutionStage.Prepare);
            }).Every(k_UpdateFrequency);

            m_DeployProgressBar = new ProgressBar() { lowValue = 0, highValue = 1 };
            m_DeployProgressBar.schedule.Execute(() =>
            {
                UpdateProgressBar(m_DeployProgressBar, ExecutionStage.Deploy);
            }).Every(k_UpdateFrequency);
            ;

            m_RunProgressBar = new ProgressBar() { lowValue = 0, highValue = 1 };
            m_RunProgressBar.schedule.Execute(() =>
            {
                UpdateProgressBar(m_RunProgressBar, ExecutionStage.Run);
            }).Every(k_UpdateFrequency);
            ;

            var LoadingIcon = new Image() { name = k_LoadingIconName, image = Icons.GetImage(Icons.ImageName.Loading) };
            UIUtils.Spin(LoadingIcon);

            m_Message = new Label();

            var instanceMessageContainer = new VisualElement();

            m_MainEditorMessage = new Label();
            m_EditorInstanceMessage = new Label();
            m_LocalInstanceMessage = new Label();
            m_RemoteInstanceMessage = new Label();

            instanceMessageContainer.Add(m_MainEditorMessage);
            instanceMessageContainer.Add(m_EditorInstanceMessage);
            instanceMessageContainer.Add(m_LocalInstanceMessage);
            instanceMessageContainer.Add(m_RemoteInstanceMessage);

            m_CancelButton = new Button()
            {
                text = "Cancel"
            };
            m_CancelButton.RegisterCallback<ClickEvent>(evt =>
            {
                PlayModeManager.instance.Stop();
            });

            m_CancelButton.AddToClassList("cancel-button");

            var messageBar = new VisualElement();
            messageBar.style.flexDirection = FlexDirection.Row;

            messageBar.Add(LoadingIcon);
            messageBar.Add(m_Message);

            var progressBarContainer = new VisualElement();
            progressBarContainer.AddToClassList("progress-bar-container");
            m_PrepareProgressBar.AddToClassList("progress-bar");
            m_DeployProgressBar.AddToClassList("progress-bar");
            m_RunProgressBar.AddToClassList("progress-bar");

            progressBarContainer.Add(m_PrepareProgressBar);
            progressBarContainer.Add(m_DeployProgressBar);
            progressBarContainer.Add(m_RunProgressBar);

            rootVisualElement.Add(progressBarContainer);
            rootVisualElement.Add(messageBar);
            rootVisualElement.Add(instanceMessageContainer);
            rootVisualElement.Add(m_CancelButton);

            rootVisualElement.styleSheets.Add(EditorGUIUtility.LoadRequired(k_Stylesheet) as StyleSheet);
        }

        private static int GetInstanceCount<T>(ScenarioConfig scenarioConfig) where T : InstanceDescription
        {
            var instanceCount = 0;
            var allInstances = scenarioConfig.GetAllInstances();
            foreach (var instance in allInstances)
            {
                if (instance is T description &&
                    description.RunModeState == RunModeState.ScenarioControl)
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
