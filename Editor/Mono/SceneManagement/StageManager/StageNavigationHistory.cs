// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace UnityEditor.SceneManagement
{
    [Serializable]
    internal class StageNavigationHistory
    {
        [SerializeField]
        List<Stage> m_History = new List<Stage>();

        ReadOnlyCollection<Stage> m_ReadOnlyHistory;

        [SerializeField]
        int m_CurrentIndex = -1;

        internal StageNavigationHistory()
        {
            m_ReadOnlyHistory = new ReadOnlyCollection<Stage>(m_History);
        }

        internal void Init()
        {
            SetMainStage(MainStage.CreateMainStage());
        }

        public Stage currentStage
        {
            get
            {
                if (m_CurrentIndex < 0 || m_CurrentIndex >= m_History.Count)
                    return null;

                return m_History[m_CurrentIndex];
            }
            // No setter since invoking code should explicitly specify desired effect on history.
        }

        public ReadOnlyCollection<Stage> GetHistory()
        {
            return m_ReadOnlyHistory;
        }

        // Always keeps main stage
        public List<Stage> ClearHistory()
        {
            var removedStages = new List<Stage>();
            if (m_History.Count > 1)
            {
                removedStages = m_History.GetRange(1, m_History.Count - 1);
                m_History.RemoveRange(1, m_History.Count - 1);
            }
            return removedStages;
        }

        public void AddStage(Stage stage)
        {
            m_History.Add(stage);
            m_CurrentIndex = m_History.Count - 1;
        }

        public List<Stage> ClearForwardHistoryAfterCurrentStage()
        {
            if (m_CurrentIndex >= 0)
            {
                int start = m_CurrentIndex + 1;
                int count = m_History.Count - start;
                var removed = m_History.GetRange(start, count);
                m_History.RemoveRange(start, count);
                return removed;
            }
            return new List<Stage>();
        }

        public List<Stage> ClearForwardHistoryAndAddItem(Stage stage)
        {
            if (stage == currentStage)
                return new List<Stage>();

            var removed = ClearForwardHistoryAfterCurrentStage();
            AddStage(stage);
            return removed;
        }

        public bool TrySetToIndexOfItem(Stage stage)
        {
            for (int i = 0; i < m_History.Count; ++i)
            {
                if (m_History[i] == stage)
                {
                    m_CurrentIndex = i;
                    return true;
                }
            }

            return false;
        }

        public int GetStageCount()
        {
            return m_History.Count;
        }

        public bool CanGoForward()
        {
            if (m_History.Count <= 1)
                return false;

            if (m_CurrentIndex >= m_History.Count - 1)
                return false;

            return true;
        }

        public bool CanGoBackward()
        {
            if (m_History.Count <= 1)
                return false;

            if (m_CurrentIndex == 0)
                return false;

            return true;
        }

        public Stage GetPrevious()
        {
            if (!CanGoBackward())
                return null;

            return m_History[m_CurrentIndex - 1];
        }

        public Stage GetNext()
        {
            if (!CanGoForward())
                return null;

            return m_History[m_CurrentIndex + 1];
        }

        internal MainStage GetMainStage()
        {
            if (m_History.Count == 0 || m_History[0] == null)
                SetMainStage(MainStage.CreateMainStage());

            return (MainStage)m_History[0];
        }

        void SetMainStage(MainStage mainStage)
        {
            if (mainStage == null)
                throw new ArgumentNullException("mainStage");

            if (m_History.Count > 0 && m_History[0] != null)
                throw new InvalidOperationException("The MainStage is already set");

            if (m_History.Count == 0)
                m_History.Add(mainStage);
            else
                m_History[0] = mainStage;

            if (m_CurrentIndex < 0)
            {
                m_CurrentIndex = 0;
            }

            mainStage.opened = true;
        }
    }
}
