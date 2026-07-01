// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;

namespace Unity.ProjectAuditor.Editor.UI.Framework
{
    internal class ProgressBar : IProgress
    {
        bool m_IsCancelled = false;
        int m_RootProgressId = -1;

        public bool IsCancelled => m_IsCancelled;

        public void Cancel()
        {
            m_IsCancelled = true;
        }

        public AsyncProgressState Start(string title, int total)
        {
            var id = Progress.Start(title, parentId: m_RootProgressId);
            if (total > 0)
                Progress.Report(id, 0, total);

            return new AsyncProgressState()
            {
                Id = id,
                Current = 0,
                Total = total
            };
        }

        public void Advance(AsyncProgressState state, string description)
        {
            state.Current++;
            Progress.Report(state.Id, state.Current, state.Total, description);
        }

        public void Clear(AsyncProgressState state)
        {
            Progress.Finish(state.Id);
        }

        public AsyncProgressState StartRoot(string title, string description, int total)
        {
            m_RootProgressId = Progress.Start(title, description);
            if (total > 0)
                Progress.Report(m_RootProgressId, 0, total);

            return new AsyncProgressState()
            {
                Id = m_RootProgressId,
                Current = 0,
                Total = total
            };
        }

        public void AdvanceRoot(AsyncProgressState state)
        {
            state.Current++;
            Progress.Report(m_RootProgressId, state.Current, state.Total);
        }

        public void ClearRoot(AsyncProgressState state)
        {
            Progress.Finish(m_RootProgressId);
            m_RootProgressId = -1;
        }
    }
}
