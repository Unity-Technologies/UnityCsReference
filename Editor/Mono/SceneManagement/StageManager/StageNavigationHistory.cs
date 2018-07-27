// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.SceneManagement
{
    [Serializable]
    internal class StageNavigationHistory
    {
        [SerializeField]
        List<StageNavigationItem> m_History = new List<StageNavigationItem>();
        [SerializeField]
        int m_CurrentIndex = -1;

        public StageNavigationItem currentItem
        {
            get
            {
                if (m_CurrentIndex < 0 || m_CurrentIndex >= m_History.Count)
                    return null;

                return m_History[m_CurrentIndex];
            }
            // No setter since invoking code should explicitly specify desired effect on history.
        }

        public void ClearHistory()
        {
            // Always keep main scenes stage
            if (m_History.Count > 1)
            {
                m_History.RemoveRange(1, m_History.Count - 1);
            }
        }

        public void AddItem(StageNavigationItem item)
        {
            m_History.Add(item);
            m_CurrentIndex = m_History.Count - 1;
        }

        public void ClearForwardHistoryAfterCurrentStage()
        {
            if (m_CurrentIndex >= 0)
            {
                m_History.RemoveRange(m_CurrentIndex + 1, m_History.Count - (m_CurrentIndex + 1));
            }
        }

        public void ClearForwardHistoryAndAddItem(StageNavigationItem item)
        {
            if (item == currentItem)
                return;

            ClearForwardHistoryAfterCurrentStage();
            AddItem(item);
        }

        public bool TrySetToIndexOfItem(StageNavigationItem item)
        {
            for (int i = 0; i < m_History.Count; ++i)
            {
                if (m_History[i] == item)
                {
                    m_CurrentIndex = i;
                    return true;
                }
            }

            return false;
        }

        public int GetItemCount()
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

        public StageNavigationItem GetPrevious()
        {
            if (!CanGoBackward())
                return null;

            return m_History[m_CurrentIndex - 1];
        }

        public StageNavigationItem GetNext()
        {
            if (!CanGoForward())
                return null;

            return m_History[m_CurrentIndex + 1];
        }

        public StageNavigationItem[] GetHistory()
        {
            return m_History.ToArray();
        }

        public StageNavigationItem GetOrCreateMainStage()
        {
            for (int i = 0; i < m_History.Count; i++)
                if (m_History[i].isMainStage)
                    return m_History[i];

            return StageNavigationItem.CreateMainStage();
        }

        public StageNavigationItem GetOrCreatePrefabStage(string prefabAssetPath)
        {
            for (int i = 0; i < m_History.Count; i++)
                if (!m_History[i].isMainStage && m_History[i].prefabAssetPath == prefabAssetPath)
                    return m_History[i];

            return StageNavigationItem.CreatePrefabStage(prefabAssetPath);
        }

        public void OnAssetsChangedOnHDD(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var stage in m_History)
                if (stage.isPrefabStage)
                    stage.SyncAssetPathFromAssetGUID();
        }
    }
}
