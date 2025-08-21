// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.PlayMode.Editor;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor
{
    /// <summary>
    /// Button that opens the status of current selected playmode config
    /// aswell as visualizes the progress of the deployment
    /// </summary>
    internal class MultiplayerPlayModeStatusButton : EditorToolbarButton
    {
        const string k_StatusButtonName = "multiplayer-playmodestatus-button";
        const string k_DeployProgressbarName = "deploy-progress";
        const string k_MainIconName = "icon";

        // used in tests therefore it is internal
        internal const string ussClassIdle = "idle";
        internal const string ussClassProcessing = "processing";
        internal const string ussClassRunning = "running";
        internal const string ussClassError = "error";

        VisualElement m_Progressbar;
        const string k_StylePath = "Multiplayer/UI/MultiplayerPlayModeStatusButton.uss";

        public MultiplayerPlayModeStatusButton(ScenarioConfig config)
        {
            name = k_StatusButtonName;
            m_Progressbar = new VisualElement();
            m_Progressbar.name = k_DeployProgressbarName;

            icon = Icons.GetImage(Icons.ImageName.Loading);
            var buttonIcon = this.Q<Image>();
            buttonIcon.name = k_MainIconName;

            var arrow = new VisualElement();
            arrow.AddToClassList("unity-icon-arrow");
            Add(arrow);

            Add(m_Progressbar);
            SetProgress(ScenarioRunner.GetScenarioStatus());

            styleSheets.Add(EditorGUIUtility.LoadRequired(k_StylePath) as StyleSheet);

            PlayModeManager.instance.StateChanged += _ => SetProgress(ScenarioRunner.GetScenarioStatus());
            ScenarioRunner.StatusChanged += _ => SetProgress(ScenarioRunner.GetScenarioStatus());
            clicked += () => UnityEditor.PopupWindow.Show(new Rect(worldBound.x + worldBound.width - PlaymodeStatusPopupContent.windowSize.x, worldBound.y, worldBound.width, worldBound.height), new PlaymodeStatusPopupContent()); ;
        }

        void ClearUssClasses()
        {
            RemoveFromClassList(ussClassError);
            RemoveFromClassList(ussClassRunning);
            RemoveFromClassList(ussClassProcessing);
            RemoveFromClassList(ussClassIdle);
            m_Progressbar.RemoveFromClassList("animate");
        }

        void SetProgressBarWidth(float progressPercent)
        {
            m_Progressbar.style.width = new StyleLength(new Length(progressPercent, LengthUnit.Percent));
            if (progressPercent > 0f && progressPercent < 100f)
            {
                m_Progressbar.AddToClassList("animate");
            }
        }

        void SetProgress(ScenarioStatus state)
        {
            var stage = state.CurrentStage;

            var labelText = state.State.ToString();
            var tooltipText = "";
            var iconImage = default(Texture2D);
            var progressPercent = 0f;

            ClearUssClasses();

            switch (state.State)
            {
                case ScenarioState.Running:
                    if (stage == ExecutionStage.Run && state.StageState == ExecutionState.Active)
                    {
                        AddToClassList(ussClassRunning);
                        labelText = "Running";
                        iconImage = Icons.GetImage(Icons.ImageName.CompletedTask);
                        progressPercent = 100f;
                    }
                    else
                    {
                        AddToClassList(ussClassProcessing);
                        iconImage = Icons.GetImage(Icons.ImageName.Loading);

                        // If in the run stage but state is not active, means we're starting.
                        // Otherwise set the label to the stage we're running in.
                        labelText = stage == ExecutionStage.Run ? "Start" : stage.ToString();

                        var stageProgress = Mathf.Ceil(state.TotalProgress * 100);
                        progressPercent = Mathf.Ceil((((int)stage - 1) + state.TotalProgress) * 100 / 3);
                        tooltipText = $"Scenario Progress\n{labelText}:\t{stageProgress}%\nOverall:\t{progressPercent}%";
                    }
                    break;
                case ScenarioState.Failed:
                    AddToClassList(ussClassError);
                    tooltipText = "Scenario has failed. See console for details.";
                    iconImage = Icons.GetImage(Icons.ImageName.Error);
                    break;
                default: // Includes Idle, Aborted, and Completed states.
                    AddToClassList(ussClassIdle);
                    iconImage = Icons.GetImage(Icons.ImageName.Idle);
                    break;
            }

            text = labelText;
            tooltip = tooltipText;
            icon = iconImage;
            SetProgressBarWidth(progressPercent);
        }
    }
}
