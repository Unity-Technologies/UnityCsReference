// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace UnityEditor.UIElements
{
    // This class wraps the use of Progress (for using Background Tasks) for the UI Toolkit Package asset conversion.
    internal class GUIDConversionTask
    {
        private const int kTaskNotStarted = -1;

        private string m_TaskName;
        private string m_TaskDescription;
        private EditorApplication.CallbackFunction m_UpdateAction;
        private Func<bool> m_CancelAction;
        private Progress.Options m_ProgressOptions = Progress.Options.None;
        private int m_TaskId = kTaskNotStarted;

        public GUIDConversionTask(string taskName, string taskDescription,
                                  EditorApplication.CallbackFunction updateAction, Func<bool> cancelAction, bool isIndefinite = false)
        {
            m_TaskName = taskName;
            m_TaskDescription = taskDescription;
            m_UpdateAction = updateAction;
            m_CancelAction = cancelAction;

            if (isIndefinite)
            {
                m_ProgressOptions = Progress.Options.Indefinite;
            }
        }

        public void Start()
        {
            if (m_TaskId != kTaskNotStarted)
            {
                return;
            }

            m_TaskId = Progress.Start(m_TaskName, m_TaskDescription, m_ProgressOptions);
            Progress.SetTimeDisplayMode(m_TaskId, Progress.TimeDisplayMode.ShowRunningTime);
            Progress.RegisterCancelCallback(m_TaskId, m_CancelAction);
            EditorApplication.update += m_UpdateAction;
        }

        public void SetProgress(float progress, string description = null)
        {
            if (m_TaskId == kTaskNotStarted)
            {
                return;
            }

            if (!string.IsNullOrEmpty(description))
            {
                Progress.SetDescription(m_TaskId, description);
            }
            Progress.Report(m_TaskId, progress);
        }

        public void Stop(string description = null, bool hasFailed = false)
        {
            if (m_TaskId == kTaskNotStarted)
            {
                return;
            }

            StopProgress(description);
            Progress.Finish(m_TaskId, hasFailed ? Progress.Status.Failed : Progress.Status.Succeeded);
            m_TaskId = kTaskNotStarted;
        }

        public void Cancel(string description = null)
        {
            if (m_TaskId == kTaskNotStarted)
            {
                return;
            }

            StopProgress(description);
        }

        private void StopProgress(string description = null)
        {
            EditorApplication.update -= m_UpdateAction;
            if (!string.IsNullOrEmpty(description))
            {
                Progress.SetDescription(m_TaskId, description);
            }
        }
    }
}
