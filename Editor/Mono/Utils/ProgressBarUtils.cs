// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor
{
    // Class used as a callback handler for progress bar notifications.
    // Supports clamping to a sub range of a complete process (defined from 0.0f to 1.0f).
    // When using a ProgressHandler with a sub range, processes can refer to their local completion rate
    //  and the ProgressHandler will report back progress within that sub range.
    internal class ProgressHandler
    {
        public delegate void ProgressCallback(string title, string message, float globalProgress);
        private ProgressCallback m_ProgressCallback;
        private string m_Title;
        private float m_ProgressRangeMin;
        private float m_ProgressRangeMax;

        public ProgressHandler(string title, ProgressCallback callback, float progressRangeMin = 0.0f, float progressRangeMax = 1.0f)
        {
            m_Title = title;
            m_ProgressCallback += callback;
            m_ProgressRangeMin = progressRangeMin;
            m_ProgressRangeMax = progressRangeMax;
        }

        private float CalcGlobalProcess(float localProcess)
        {
            return Mathf.Clamp(m_ProgressRangeMin * (1.0f - localProcess) + m_ProgressRangeMax * localProcess, 0.0f, 1.0f);
        }

        public void OnProgress(string message, float progress)
        {
            m_ProgressCallback(m_Title, message, CalcGlobalProcess(progress));
        }

        public ProgressHandler SpawnFromLocalSubRange(float localRangeMin, float localRangeMax)
        {
            return new ProgressHandler(m_Title, m_ProgressCallback, CalcGlobalProcess(localRangeMin), CalcGlobalProcess(localRangeMax));
        }
    }

    // A helper class that attaches to a ProgressHandler
    // Supports queueing tasks which will automatically notify the ProgressHandler when executed
    internal class ProgressTaskManager
    {
        private ProgressHandler m_Handler;
        private List<Action> m_Tasks = new List<Action>();
        private int m_ProgressUpdatesForCurrentTask;
        private int m_StartedTasks;

        public ProgressTaskManager(ProgressHandler handler)
        {
            m_Handler = handler;
        }

        public void AddTask(Action task)
        {
            m_Tasks.Add(task);
        }

        public void Run()
        {
            // Run should not be run within a previous run
            System.Diagnostics.Debug.Assert(m_StartedTasks == 0);

            foreach (var task in m_Tasks)
            {
                m_StartedTasks++;
                m_ProgressUpdatesForCurrentTask = 0;
                task();
            }
        }

        public void UpdateProgress(string message)
        {
            if (m_Handler != null)
            {
                // Get some movement of the bar, even for unknown number of progress updates
                float taskProgress = 1.0f - Mathf.Pow(0.85f, m_ProgressUpdatesForCurrentTask);
                int totalTasks = m_Tasks.Count;
                if (totalTasks <= m_StartedTasks)
                    totalTasks = m_StartedTasks;
                float taskStep = 1.0f / totalTasks;
                float runProgress = (m_StartedTasks - 1) * taskStep + taskProgress * taskStep;
                m_Handler.OnProgress(message, runProgress);
            }

            m_ProgressUpdatesForCurrentTask++;
        }

        public ProgressHandler SpawnProgressHandlerFromCurrentTask()
        {
            if (m_Handler != null)
            {
                int totalTasks = m_Tasks.Count;
                float taskStep = 1.0f / totalTasks;
                float minRange = (m_StartedTasks - 1) * taskStep;
                float maxRange = m_StartedTasks * taskStep;
                return m_Handler.SpawnFromLocalSubRange(minRange, maxRange);
            }
            return null;
        }
    }
}
