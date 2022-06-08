// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Text;
using UnityEditor.Profiling;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor
{
    class VisualProgressItem : VisualElement
    {
        public const string visualElementName = "VisualProgressItem";

        public const string k_SuccessIcon = "completed_task";
        public const string k_FailedIcon = "console.erroricon";
        public const string k_CanceledIcon = "console.warnicon";
        public const int k_IconSize = 16;

        private const double k_CheckUnresponsiveDelayInSecond = 1.0;
        private double m_LastUpdate;

        private float m_LastElapsedTime;

        private VisualElement m_BackgroundTaskStatusIcon = null;
        private VisualElement m_Progress = null;

        private Label m_BackgroundTaskDescriptionLabel = null;
        private Label m_BackgroundTaskElapsedTimeLabel = null;
        private Label m_BackgroundTaskNameLabel = null;
        private Label m_ProgressionLabel = null;

        private Button m_PauseButton = null;
        private Button m_CancelButton = null;

        private ProgressBar m_ProgressBar = null;

        private Progress.Item m_ProgressItem = null;

        public VisualProgressItem(VisualTreeAsset visualProgressItemTask)
        {
            CreateVisualProgressItem(visualProgressItemTask);
        }

        private void CreateVisualProgressItem(VisualTreeAsset visualProgressItemTask)
        {
            name = visualElementName;
            if (visualProgressItemTask == null)
                return;

            var ui = visualProgressItemTask.Instantiate();

            m_BackgroundTaskNameLabel = ui.Q<Label>("BackgroundTaskNameLabel");
            m_ProgressionLabel = ui.Q<Label>("ProgressionLabel");
            m_BackgroundTaskStatusIcon = ui.Q<VisualElement>("BackgroundTaskStatusIcon");
            m_BackgroundTaskDescriptionLabel = ui.Q<Label>("BackgroundTaskDescriptionLabel");
            m_BackgroundTaskElapsedTimeLabel = ui.Q<Label>("BackgroundTaskElapsedTimeLabel");
            m_ProgressBar = ui.Q<ProgressBar>("ProgressBar");
            m_Progress = m_ProgressBar.Q(null, "unity-progress-bar__progress");
            m_PauseButton = ui.Q<Button>("PauseButton");
            m_CancelButton = ui.Q<Button>("CancelButton");

            m_PauseButton.RemoveFromClassList("unity-text-element");
            m_PauseButton.RemoveFromClassList("unity-button");
            m_PauseButton.AddToClassList("pause-button");

            m_CancelButton.RemoveFromClassList("unity-text-element");
            m_CancelButton.RemoveFromClassList("unity-button");

            m_PauseButton.clickable.clickedWithEventInfo -= PauseButtonClicked;
            m_CancelButton.clickable.clickedWithEventInfo -= CancelButtonClicked;
            m_PauseButton.clickable.clickedWithEventInfo += PauseButtonClicked;
            m_CancelButton.clickable.clickedWithEventInfo += CancelButtonClicked;

            this.Add(ui);
        }

        private void ResetStyleClasses()
        {
            m_Progress.EnableInClassList("unity-progress-bar__progress__unresponding", false);
            m_Progress.EnableInClassList("unity-progress-bar__progress__full", false);
            m_Progress.EnableInClassList("unity-progress-bar__progress__inactive", false);
            m_Progress.EnableInClassList("unity-progress-bar__progress__idle", false);

            m_PauseButton.EnableInClassList("resume-button", false);
            m_PauseButton.EnableInClassList("pause-button", true);
        }

        public void BindItem(Progress.Item item)
        {
            if (m_ProgressItem == null || m_ProgressItem.id != item.id)
            {
                m_ProgressItem = item;
                m_PauseButton.userData = item;
                m_CancelButton.userData = item;
                OnEverythingChanged();
            }

            UpdateDisplay();

            EditorApplication.update -= OnUpdateVisualProgress;
            EditorApplication.update += OnUpdateVisualProgress;
        }

        public void UnbindItem()
        {
            m_ProgressItem = null;

            EditorApplication.update -= OnUpdateVisualProgress;

            ResetStyleClasses();

            m_PauseButton.userData = null;
            m_CancelButton.userData = null;

            m_LastUpdate = 0.0;
            m_LastElapsedTime = 0f;
        }

        public void DestroyItem()
        {
            UnbindItem();

            m_PauseButton.clickable.clickedWithEventInfo -= PauseButtonClicked;
            m_CancelButton.clickable.clickedWithEventInfo -= CancelButtonClicked;

            this.Clear();
        }

        public void OnUpdateVisualProgress()
        {
            //using (new EditorPerformanceTracker("VisualProgressItem.OnUpdateVisualProgress"))
            //{
            if (m_ProgressItem == null || !m_ProgressItem.running || !m_ProgressItem.exists)
                return;

            var now = EditorApplication.timeSinceStartup;
            var checkUnresponsive = (now - m_LastUpdate) > k_CheckUnresponsiveDelayInSecond;
            if (checkUnresponsive)
                m_LastUpdate = now;

            if (checkUnresponsive)
                CheckUnresponsive();
            UpdateAnimatedState();
            //}
        }

        internal void CheckUnresponsive()
        {
            if (m_ProgressItem.finished)
                return;

            var taskElapsedTime = Mathf.Round(m_ProgressItem.elapsedTime);
            if (taskElapsedTime >= 2f)
            {
                if (m_LastElapsedTime != taskElapsedTime || m_ProgressItem.paused) // a paused task will have the same elapsedTime and lastElapsedTime
                {
                    m_LastElapsedTime = taskElapsedTime;
                    UpdateRunningTime();
                }
            }

            UpdateResponsiveness();
        }

        internal void UpdateRunningTime()
        {
            StringBuilder sb = new StringBuilder();

            if (m_ProgressItem.totalSteps != -1)
            {
                if (string.IsNullOrEmpty(m_ProgressItem.stepLabel))
                    sb.AppendFormat("({0}/{1})", m_ProgressItem.currentStep, m_ProgressItem.totalSteps);
                else
                    sb.AppendFormat("({0}/{1} {2})", m_ProgressItem.currentStep, m_ProgressItem.totalSteps, m_ProgressItem.stepLabel);
            }

            if (m_ProgressItem.paused)
            {
                if (sb.Length > 0)
                    sb.Insert(0, " ");

                sb.Insert(0, "Paused");
                m_BackgroundTaskElapsedTimeLabel.text = sb.ToString();
                return;
            }

            if (m_ProgressItem.timeDisplayMode == Progress.TimeDisplayMode.NoTimeShown)
            {
                m_BackgroundTaskElapsedTimeLabel.text = sb.ToString();
                return;
            }

            if (sb.Length > 0)
                sb.Insert(0, " ");

            if (m_ProgressItem.timeDisplayMode == Progress.TimeDisplayMode.ShowRemainingTime && !m_ProgressItem.finished)
                sb.Insert(0, FormatRemainingTime(m_ProgressItem.remainingTime));
            else
                sb.Insert(0, $"{m_ProgressItem.elapsedTime:0} seconds");
            m_BackgroundTaskElapsedTimeLabel.text = sb.ToString();
        }

        private void UpdateResponsiveness()
        {
            m_Progress.EnableInClassList("unity-progress-bar__progress__unresponding", !m_ProgressItem.responding);

            if (m_ProgressItem.responding)
                m_BackgroundTaskDescriptionLabel.text = m_ProgressItem.description;
            else
                m_BackgroundTaskDescriptionLabel.text = string.IsNullOrEmpty(m_ProgressItem.description) ? "(Not Responding)" : $"{m_ProgressItem.description} (Not Responding)";
        }

        internal void UpdateAnimatedState()
        {
            // Update all ui state that needs to be animated
            if (m_ProgressItem.indefinite)
                SetIndefinite();
            UpdateRunningTime();
        }

        public void SetIndefinite()
        {
            if (m_ProgressItem.indefinite)
            {
                var progressTotalWidth = float.IsNaN(m_ProgressBar.worldBound.width) ? m_ProgressBar.resolvedStyle.width : m_ProgressBar.worldBound.width;
                var barWidth = progressTotalWidth * 0.2f;

                m_Progress.style.width = barWidth;
                var halfBarWidth = barWidth / 2.0f;
                var cos = Mathf.Cos((float)EditorApplication.timeSinceStartup * 2f);
                var rb = halfBarWidth;
                var re = progressTotalWidth - halfBarWidth;
                var scale = (re - rb) / 2f;
                var cursor = scale * cos;
                m_Progress.style.left = cursor + scale;
            }
            else
            {
                m_Progress.style.width = StyleKeyword.Auto;
                m_Progress.style.left = 0;
            }

            m_Progress.EnableInClassList("unity-progress-bar__progress__full", m_ProgressItem.indefinite);
        }

        private void UpdateDisplay()
        {
            //using (new EditorPerformanceTracker("VisualProgressItem.UpdateDisplay"))
            //{
            if (m_ProgressItem.lastUpdates.HasAny(Progress.Updates.NothingChanged))
                return;

            if (m_ProgressItem.lastUpdates.HasAny(Progress.Updates.StatusChanged))
                OnStatusChanged();

            if (m_ProgressItem.lastUpdates.HasAny(Progress.Updates.ProgressChanged))
                OnProgressChanged();

            if (m_ProgressItem.lastUpdates.HasAny(Progress.Updates.DescriptionChanged))
                m_BackgroundTaskDescriptionLabel.text = m_ProgressItem.description;

            if (m_ProgressItem.lastUpdates.HasAny(Progress.Updates.PriorityChanged))
                OnPriorityChanged();

            if (m_ProgressItem.lastUpdates.HasAny(Progress.Updates.TimeDisplayModeChanged |
                Progress.Updates.RemainingTimeChanged | Progress.Updates.StepLabelChanged |
                Progress.Updates.CurrentStepChanged | Progress.Updates.TotalStepsChanged |
                Progress.Updates.EndTimeChanged))
                UpdateRunningTime();

            if (m_ProgressItem.lastUpdates.HasAny(Progress.Updates.CancellableChanged))
                m_CancelButton.visible = m_ProgressItem.cancellable && !m_ProgressItem.finished;

            if (m_ProgressItem.lastUpdates.HasAny(Progress.Updates.PausableChanged))
                m_PauseButton.visible = m_ProgressItem.pausable && !m_ProgressItem.finished;

            if (m_ProgressItem.lastUpdates.HasAll(Progress.Updates.EverythingChanged))
                OnEverythingChanged();
            //}
        }

        private void OnEverythingChanged()
        {
            ResetStyleClasses();

            m_BackgroundTaskNameLabel.text = m_ProgressItem.name;
            m_BackgroundTaskDescriptionLabel.text = m_ProgressItem.description;
            m_BackgroundTaskElapsedTimeLabel.text = "";
            m_ProgressionLabel.text = "";

            m_PauseButton.visible = m_ProgressItem.pausable && !m_ProgressItem.finished;
            m_CancelButton.visible = m_ProgressItem.cancellable && !m_ProgressItem.finished;

            OnProgressChanged();
            OnStatusChanged();
            OnPriorityChanged();
        }

        private void OnStatusChanged()
        {
            switch (m_ProgressItem.status)
            {
                case Progress.Status.Canceled:
                    m_BackgroundTaskDescriptionLabel.text = "Cancelled";
                    UpdateStatusIcon(k_CanceledIcon);
                    m_Progress.EnableInClassList("unity-progress-bar__progress__inactive", true);
                    m_CancelButton.visible = false;
                    m_PauseButton.visible = false;
                    UpdateRunningTime();
                    break;
                case Progress.Status.Paused:
                    m_Progress.EnableInClassList("unity-progress-bar__progress__inactive", true);
                    m_PauseButton.EnableInClassList("pause-button", false);
                    m_PauseButton.EnableInClassList("resume-button", true);
                    UpdateRunningTime();
                    break;
                case Progress.Status.Running: // that case is needed when resuming a paused task
                    UpdateStatusIcon(null);
                    m_Progress.EnableInClassList("unity-progress-bar__progress__inactive", false);
                    m_PauseButton.EnableInClassList("resume-button", false);
                    m_PauseButton.EnableInClassList("pause-button", true);
                    break;
                case Progress.Status.Failed:
                    if (string.IsNullOrEmpty(m_BackgroundTaskDescriptionLabel.text))
                        m_BackgroundTaskDescriptionLabel.text = "Failed";
                    UpdateStatusIcon(k_FailedIcon);
                    m_Progress.EnableInClassList("unity-progress-bar__progress__inactive", true);
                    m_CancelButton.visible = false;
                    m_PauseButton.visible = false;
                    UpdateRunningTime();
                    break;
                case Progress.Status.Succeeded:
                    if (string.IsNullOrEmpty(m_BackgroundTaskDescriptionLabel.text))
                        m_BackgroundTaskDescriptionLabel.text = "Done";
                    m_ProgressBar.value = 100;
                    SetProgressStyleFull(true);
                    UpdateStatusIcon(k_SuccessIcon);
                    m_ProgressionLabel.style.unityBackgroundImageTintColor = new StyleColor(Color.green);
                    m_CancelButton.visible = false;
                    m_PauseButton.visible = false;

                    // Update running time to force elapsed time to show when the task is set to show ETA
                    UpdateRunningTime();
                    break;
                default:
                    break;
            }
        }

        private void UpdateStatusIcon(string iconName)
        {
            if (string.IsNullOrEmpty(iconName))
            {
                m_BackgroundTaskStatusIcon.style.backgroundImage = null;
                m_BackgroundTaskStatusIcon.style.height = 0;
                m_BackgroundTaskStatusIcon.style.width = 0;
            }
            else
            {
                m_BackgroundTaskStatusIcon.style.backgroundImage = EditorGUIUtility.LoadIcon(iconName);
                m_BackgroundTaskStatusIcon.style.height = k_IconSize;
                m_BackgroundTaskStatusIcon.style.width = k_IconSize;
            }

            m_BackgroundTaskStatusIcon.style.backgroundPositionX = BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleToFit);
            m_BackgroundTaskStatusIcon.style.backgroundPositionY = BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleToFit);
            m_BackgroundTaskStatusIcon.style.backgroundRepeat = BackgroundPropertyHelper.ConvertScaleModeToBackgroundRepeat(ScaleMode.ScaleToFit);
            m_BackgroundTaskStatusIcon.style.backgroundSize = BackgroundPropertyHelper.ConvertScaleModeToBackgroundSize(ScaleMode.ScaleToFit);
        }

        public void SetProgressStyleFull(bool styleFull)
        {
            m_Progress.EnableInClassList("unity-progress-bar__progress__full", styleFull);
        }

        private void OnPriorityChanged()
        {
            bool isIdle = m_ProgressItem.priority == (int)Progress.Priority.Idle && m_ProgressItem.status != Progress.Status.Succeeded;
            m_Progress.EnableInClassList("unity-progress-bar__progress__idle", isIdle);
        }

        private void OnProgressChanged()
        {
            SetIndefinite();

            if (!m_ProgressItem.indefinite)
            {
                var p01 = Mathf.Clamp01(m_ProgressItem.progress);
                m_ProgressBar.value = p01 * 100.0f;
                if (m_ProgressionLabel != null)
                    m_ProgressionLabel.text = $"{Mathf.FloorToInt(p01 * 100.0f)}%";

                SetProgressStyleFull(m_ProgressItem.progress > 0.96f);
            }
        }

        private static string FormatRemainingTime(TimeSpan eta)
        {
            if (eta.Days > 0)
                return $"{eta.Days} day{(eta.Days > 1 ? "s" : "")} left";
            if (eta.Hours > 0)
                return $"{eta.Hours} hour{(eta.Hours > 1 ? "s" : "")} left";
            if (eta.Minutes > 0)
                return $"{eta.Minutes} minute{(eta.Minutes > 1 ? "s" : "")} left";
            if (eta.Seconds > 0)
                return $"{eta.Seconds} second{(eta.Seconds > 1 ? "s" : "")} left";

            return "";
        }

        private static void CancelButtonClicked(EventBase obj)
        {
            var sender = obj.target as Button;
            if (sender?.userData is Progress.Item ds)
                ds.Cancel();
        }

        private static void PauseButtonClicked(EventBase obj)
        {
            var sender = obj.target as Button;
            if (sender?.userData is Progress.Item ds)
            {
                if (!ds.paused)
                    ds.Pause();
                else
                    ds.Resume();
            }
        }
    }
}
