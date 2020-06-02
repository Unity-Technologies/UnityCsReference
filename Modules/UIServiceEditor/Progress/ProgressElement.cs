// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine.Assertions;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityEditor
{
    internal class DisplayedTask
    {
        public readonly Label nameLabel;
        public readonly Label progressLabel;
        public readonly VisualElement descriptionIcon;
        public readonly Label descriptionLabel;
        public readonly Label elapsedTimeLabel;
        public readonly ProgressBar progressBar;
        public readonly Button cancelButton;
        public VisualElement progress;
        public bool isIndefinite;
        public bool isResponding;
        public float lastElapsedTime;

        public DisplayedTask(Label name, Label progress, VisualElement descriptionIcon, Label description, Label elapsedTime, ProgressBar progressBar, Button cancelButton)
        {
            nameLabel = name;
            progressLabel = progress;
            this.elapsedTimeLabel = elapsedTime;
            this.descriptionIcon = descriptionIcon;
            this.descriptionLabel = description;
            this.progressBar = progressBar;
            this.cancelButton = cancelButton;
            this.progress = this.progressBar.Q(null, "unity-progress-bar__progress");
            isIndefinite = false;
            isResponding = true;
        }

        public void SetIndefinite(bool indefinite)
        {
            if (indefinite)
            {
                var progressTotalWidth = float.IsNaN(progressBar.worldBound.width) ? 146 : progressBar.worldBound.width;
                var barWidth = progressTotalWidth * 0.2f;
                if (indefinite != isIndefinite)
                {
                    progress.AddToClassList("unity-progress-bar__progress__full");
                    progress.style.left = 0;
                }
                progress.style.width = barWidth;
                var halfBarWidth = barWidth / 2.0f;
                var cos = Mathf.Cos((float)EditorApplication.timeSinceStartup * 2f);
                var rb = halfBarWidth;
                var re = progressTotalWidth - halfBarWidth;
                var scale = (re - rb) / 2f;
                var cursor = scale * cos;
                progress.style.left = cursor + scale;
            }
            else if (indefinite != isIndefinite)
            {
                progress.style.width = StyleKeyword.Auto;
                progress.style.left = 0;
                progress.RemoveFromClassList("unity-progress-bar__progress__full");
            }

            isIndefinite = indefinite;
        }

        public void SetProgressStyleFull(bool styleFull)
        {
            var isAlreadyFull = progress.ClassListContains("unity-progress-bar__progress__full");
            if (isAlreadyFull != styleFull)
            {
                if (styleFull)
                {
                    progress.AddToClassList("unity-progress-bar__progress__full");
                }
                else
                {
                    progress.RemoveFromClassList("unity-progress-bar__progress__full");
                }
            }
        }
    }

    internal class ProgressElement
    {
        private const string k_UxmlProgressPath = "UXML/ProgressWindow/ProgressElement.uxml";
        private const string k_UxmlSubTaskPath = "UXML/ProgressWindow/SubTaskElement.uxml";
        private static VisualTreeAsset s_VisualTreeBackgroundTask = null;
        private static VisualTreeAsset s_VisualTreeSubTask = null;

        private DisplayedTask m_MainTask;
        private List<Progress.Item> m_ProgressItemChildren;
        private List<DisplayedTask> m_SubTasks;
        private VisualElement m_Details;
        private ScrollView m_DetailsScrollView;
        private Toggle m_DetailsFoldoutToggle;

        public VisualElement rootVisualElement { get; }
        public Progress.Item dataSource { get; private set; }

        public ProgressElement(Progress.Item dataSource)
        {
            rootVisualElement = new TemplateContainer();
            if (s_VisualTreeBackgroundTask == null)
                s_VisualTreeBackgroundTask = EditorGUIUtility.Load(k_UxmlProgressPath) as VisualTreeAsset;

            var task = new VisualElement() { name = "Task" };
            rootVisualElement.Add(task);
            var horizontalLayout = new VisualElement() { name = "TaskOnly" };
            horizontalLayout.style.flexDirection = FlexDirection.Row;
            task.Add(horizontalLayout);
            m_DetailsFoldoutToggle = new Toggle() { visible = false };
            m_DetailsFoldoutToggle.AddToClassList("unity-foldout__toggle");
            m_DetailsFoldoutToggle.RegisterValueChangedCallback(ToggleDetailsFoldout);
            horizontalLayout.Add(m_DetailsFoldoutToggle);
            var parentTask = s_VisualTreeBackgroundTask.CloneTree();
            parentTask.name = "ParentTask";
            horizontalLayout.Add(parentTask);

            var details = new VisualElement() { name = "Details" };
            details.style.display = DisplayStyle.None;
            task.Add(details);

            m_Details = rootVisualElement.Q<VisualElement>("Details");
            m_DetailsScrollView = new ScrollView();
            m_Details.Add(m_DetailsScrollView);
            m_DetailsScrollView.AddToClassList("details-content");

            this.dataSource = dataSource;

            if (s_VisualTreeSubTask == null)
                s_VisualTreeSubTask = EditorGUIUtility.Load(k_UxmlSubTaskPath) as VisualTreeAsset;

            m_ProgressItemChildren = new List<Progress.Item>();
            m_SubTasks = new List<DisplayedTask>();

            m_MainTask = InitializeTask(dataSource, rootVisualElement);
        }

        internal Progress.Item GetSubTaskItem(int id)
        {
            foreach (var child in m_ProgressItemChildren)
            {
                if (child.id == id)
                    return child;
            }

            return null;
        }

        internal void CheckUnresponsive()
        {
            if (!dataSource.running)
            {
                return;
            }

            var taskElapsedTime = Mathf.Round(dataSource.elapsedTime);
            if (dataSource.finished || taskElapsedTime >= 2f)
            {
                if (m_MainTask.lastElapsedTime != taskElapsedTime)
                {
                    m_MainTask.lastElapsedTime = taskElapsedTime;
                    UpdateRunningTime();
                }
            }

            if (m_MainTask.isResponding != dataSource.responding)
            {
                UpdateResponsiveness(m_MainTask, dataSource);
            }

            for (int i = 0; i < m_ProgressItemChildren.Count; ++i)
            {
                if (m_SubTasks[i].isResponding != m_ProgressItemChildren[i].responding)
                {
                    UpdateResponsiveness(m_SubTasks[i], m_ProgressItemChildren[i]);
                }
            }
        }

        internal void UpdateRunningTime()
        {
            if (m_MainTask == null)
                return;

            if (dataSource.timeDisplayMode == Progress.TimeDisplayMode.NoTimeShown)
            {
                m_MainTask.elapsedTimeLabel.text = "";
                return;
            }

            if (dataSource.timeDisplayMode == Progress.TimeDisplayMode.ShowRemainingTime && !dataSource.finished)
                m_MainTask.elapsedTimeLabel.text = FormatRemainingTime(dataSource.remainingTime);
            else
                m_MainTask.elapsedTimeLabel.text = $"{m_MainTask.lastElapsedTime:0} seconds";
        }

        internal bool TryUpdate(Progress.Item op, int id)
        {
            if (dataSource.id == id)
            {
                dataSource = op;
                UpdateDisplay(m_MainTask, dataSource);
                return true;
            }
            else
            {
                for (int i = 0; i < m_ProgressItemChildren.Count; ++i)
                {
                    if (m_ProgressItemChildren[i].id == id)
                    {
                        m_ProgressItemChildren[i] = op;
                        UpdateDisplay(m_SubTasks[i], m_ProgressItemChildren[i]);
                        return true;
                    }
                }
            }
            return false;
        }

        internal bool TryRemove(int id)
        {
            for (int i = 0; i < m_ProgressItemChildren.Count; ++i)
            {
                if (m_ProgressItemChildren[i].id == id)
                {
                    m_DetailsScrollView.RemoveAt(i);
                    m_ProgressItemChildren.RemoveAt(i);
                    m_SubTasks.RemoveAt(i);
                    if (!m_ProgressItemChildren.Any())
                        rootVisualElement.Q<Toggle>().visible = false;
                    return true;
                }
            }
            return false;
        }

        internal void AddElement(Progress.Item item)
        {
            m_DetailsFoldoutToggle.visible = true;
            SubTaskInitialization(item);
            if (dataSource.running)
            {
                m_DetailsFoldoutToggle.SetValueWithoutNotify(true);
                ToggleDetailsFoldout(ChangeEvent<bool>.GetPooled(false, true));
            }
        }

        private static string ToPrettyFormat(TimeSpan span)
        {
            if (span == TimeSpan.Zero) return "00:00:00";
            return span.Days > 0 ? $"{span:dd\\.hh\\:mm\\:ss}" : $"{span:hh\\:mm\\:ss}";
        }

        private static void UpdateResponsiveness(DisplayedTask task, Progress.Item dataSource)
        {
            if (dataSource.responding && !task.isResponding)
            {
                task.descriptionLabel.text = dataSource.description;
                if (task.progress.ClassListContains("unity-progress-bar__progress__unresponding"))
                {
                    task.progress.RemoveFromClassList("unity-progress-bar__progress__unresponding");
                }
            }
            else if (!dataSource.responding && task.isResponding)
            {
                task.descriptionLabel.text = string.IsNullOrEmpty(dataSource.description) ? "(Not Responding)" : $"{dataSource.description} (Not Responding)";
                if (!task.progress.ClassListContains("unity-progress-bar__progress__unresponding"))
                {
                    task.progress.AddToClassList("unity-progress-bar__progress__unresponding");
                }
            }
            task.isResponding = dataSource.responding;
        }

        private void UpdateDisplay(DisplayedTask task, Progress.Item dataSource)
        {
            task.nameLabel.text = dataSource.name;

            task.descriptionLabel.text = dataSource.description;
            task.SetIndefinite(dataSource.indefinite);

            if (!dataSource.indefinite)
            {
                var p01 = Mathf.Clamp01(dataSource.progress);
                task.progressBar.value = p01 * 100.0f;
                if (task.progressLabel != null)
                    task.progressLabel.text = $"{Mathf.FloorToInt(p01 * 100.0f)}%";

                task.SetProgressStyleFull(dataSource.progress > 0.96f);
            }

            if (dataSource.status == Progress.Status.Canceled)
            {
                task.descriptionLabel.text = "Cancelled";
                UpdateStatusIcon(task, ProgressWindow.kCanceledIcon);
                task.progress.AddToClassList("unity-progress-bar__progress__inactive");
            }
            else if (dataSource.status == Progress.Status.Failed)
            {
                if (string.IsNullOrEmpty(task.descriptionLabel.text))
                    task.descriptionLabel.text = "Failed";
                UpdateStatusIcon(task, ProgressWindow.kFailedIcon);
                task.progress.AddToClassList("unity-progress-bar__progress__inactive");
            }
            else if (dataSource.status == Progress.Status.Succeeded)
            {
                if (string.IsNullOrEmpty(task.descriptionLabel.text))
                    task.descriptionLabel.text = "Done";
                task.progressBar.value = 100;
                task.SetProgressStyleFull(true);
                UpdateStatusIcon(task, ProgressWindow.kSuccessIcon);
                task.progressLabel.style.unityBackgroundImageTintColor = new StyleColor(Color.green);

                // Update running time to force elapsed time to show when the task is set to show ETA
                UpdateRunningTime();

                if (m_MainTask == task && m_DetailsFoldoutToggle.value)
                {
                    m_DetailsFoldoutToggle.value = false;
                }
            }

            task.cancelButton.visible = dataSource.cancellable && dataSource.running;
        }

        private static void UpdateStatusIcon(DisplayedTask task, string iconName)
        {
            task.descriptionIcon.style.backgroundImage = EditorGUIUtility.LoadIcon(iconName);
            task.descriptionIcon.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
            task.descriptionIcon.style.height = ProgressWindow.kIconSize;
            task.descriptionIcon.style.width = ProgressWindow.kIconSize;
        }

        private void ToggleDetailsFoldout(ChangeEvent<bool> evt)
        {
            m_Details.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SubTaskInitialization(Progress.Item subTaskSource)
        {
            var parentElement = s_VisualTreeBackgroundTask.CloneTree();
            parentElement.name = "SubTask";

            DisplayedTask displayedSubTask = InitializeTask(subTaskSource, parentElement);

            m_ProgressItemChildren.Add(subTaskSource);
            m_SubTasks.Add(displayedSubTask);
            m_DetailsScrollView.Add(parentElement);
        }

        private DisplayedTask InitializeTask(Progress.Item progressItem, VisualElement parentElement)
        {
            var displayedTask = new DisplayedTask(
                parentElement.Q<Label>("BackgroundTaskNameLabel"),
                parentElement.Q<Label>("ProgressionLabel"),
                parentElement.Q<VisualElement>("BackgroundTaskStatusIcon"),
                parentElement.Q<Label>("BackgroundTaskDescriptionLabel"),
                parentElement.Q<Label>("BackgroundTaskElapsedTimeLabel"),
                parentElement.Q<ProgressBar>("ProgressBar"),
                parentElement.Q<Button>("CancelButton")
            );
            Assert.IsNotNull(displayedTask.nameLabel);
            Assert.IsNotNull(displayedTask.descriptionIcon);
            Assert.IsNotNull(displayedTask.descriptionLabel);
            Assert.IsNotNull(displayedTask.elapsedTimeLabel);
            Assert.IsNotNull(displayedTask.progressLabel);
            Assert.IsNotNull(displayedTask.progressBar);
            Assert.IsNotNull(displayedTask.cancelButton);

            displayedTask.cancelButton.RemoveFromClassList("unity-text-element");
            displayedTask.cancelButton.RemoveFromClassList("unity-button");

            displayedTask.cancelButton.userData = progressItem;
            displayedTask.cancelButton.clickable.clickedWithEventInfo += CancelButtonClicked;

            UpdateDisplay(displayedTask, progressItem);
            UpdateResponsiveness(displayedTask, progressItem);
            return displayedTask;
        }

        private void CancelButtonClicked(EventBase obj)
        {
            var sender = obj.target as Button;
            var ds = sender?.userData as Progress.Item;
            if (ds != null)
            {
                var wasCancelled = ds.Cancel();
                if (wasCancelled)
                {
                    OnCancelled();
                }
            }
        }

        private void OnCancelled()
        {
            UpdateDisplay(m_MainTask, dataSource);
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
    }
}
