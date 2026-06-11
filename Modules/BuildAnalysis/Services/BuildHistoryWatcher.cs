// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;

namespace UnityEditor.Build.Analysis
{
    internal sealed class BuildHistoryWatcher
    {
        private const double k_PollIntervalSeconds = 1.0;

        private readonly IBuildHistoryProvider m_BuildHistory;
        private int m_LastRevision;
        private int m_LastBuildHistoryLimit;
        private bool m_IsRefreshing;
        private DateTime m_LastDirectoryWriteTime;
        private DateTime m_LastPollTime;

        public event Action BuildHistoryChanged;

        public BuildHistoryWatcher(IBuildHistoryProvider buildHistory)
        {
            m_BuildHistory = buildHistory;
            m_LastRevision = 0;
            m_LastBuildHistoryLimit = buildHistory.GetBuildHistoryLimit();
        }

        public void Enable()
        {
#pragma warning disable UDR0004 // Properly unsubscribed in Disable - false positive
            EditorApplication.update += CheckForChanges;
#pragma warning restore UDR0004
            m_LastDirectoryWriteTime = GetBuildHistoryWriteTime();
            m_LastBuildHistoryLimit = m_BuildHistory.GetBuildHistoryLimit();
            m_LastPollTime = DateTime.UtcNow;
        }

        public void Disable()
        {
            EditorApplication.update -= CheckForChanges;
        }

        /// <summary>
        /// Syncs the recorded revision to the current revision so the next
        /// poll does not trigger a spurious refresh (call after ClearAllBuilds).
        /// </summary>
        public void SyncRevision()
        {
            m_LastRevision = m_BuildHistory.GetRevision();
        }

        private void CheckForChanges()
        {
            if (m_IsRefreshing)
                return;

            var now = DateTime.UtcNow;
            if ((now - m_LastPollTime).TotalSeconds < k_PollIntervalSeconds)
                return;
            m_LastPollTime = now;

            try
            {
                var writeTime = GetBuildHistoryWriteTime();
                if (writeTime != m_LastDirectoryWriteTime)
                {
                    m_LastDirectoryWriteTime = writeTime;
                    m_BuildHistory.Refresh();
                }

                var revision = m_BuildHistory.GetRevision();
                var limit = m_BuildHistory.GetBuildHistoryLimit();
                if (revision == m_LastRevision && limit == m_LastBuildHistoryLimit)
                    return;

                m_LastRevision = revision;
                m_LastBuildHistoryLimit = limit;
                m_IsRefreshing = true;
                try
                {
                    BuildHistoryChanged?.Invoke();
                }
                finally
                {
                    m_IsRefreshing = false;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{BuildAnalysisConstants.k_ConsoleLogPrefix} Error checking for build changes: {e.Message}");
            }
        }

        private static DateTime GetBuildHistoryWriteTime()
        {
            var directory = BuildHistory.BuildHistoryDirectory;
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                return DateTime.MinValue;

            return Directory.GetLastWriteTimeUtc(directory);
        }
    }
}
