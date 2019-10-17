// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    [Serializable]
    class MainStageHierarchyState
    {
        [SerializeField]
        float m_ScrollY;
        [SerializeField]
        List<int> m_ExpandedSceneGameObjectInstanceIDs = new List<int>();
        [SerializeField]
        int m_LastClickedInstanceID;
        [SerializeField]
        string[] m_OpenSceneGUIDs = null;

        public void SaveStateFromHierarchy(SceneHierarchyWindow hierarchy, Stage stage)
        {
            var lastClickedGameObject = EditorUtility.InstanceIDToObject(hierarchy.sceneHierarchy.treeViewState.lastClickedID) as GameObject;
            m_LastClickedInstanceID = lastClickedGameObject != null ? lastClickedGameObject.GetInstanceID() : 0;
            m_ExpandedSceneGameObjectInstanceIDs = hierarchy.sceneHierarchy.treeViewState.expandedIDs;

            m_ScrollY = hierarchy.sceneHierarchy.treeViewState.scrollPos.y;

            m_OpenSceneGUIDs = GetCurrentSceneGUIDs();

            if (SceneHierarchy.s_DebugPersistingExpandedState)
                DebugLog("Saving", stage);
        }

        public void LoadStateIntoHierarchy(SceneHierarchyWindow hierarchy, Stage stage)
        {
            // Restore expanded state always
            hierarchy.sceneHierarchy.treeViewState.expandedIDs = m_ExpandedSceneGameObjectInstanceIDs;

            // Restore selection and scroll value when requested
            if (stage.setSelectionAndScrollWhenBecomingCurrentStage)
            {
                Selection.activeInstanceID = m_LastClickedInstanceID;

                // We only want to set scroll position if none of the scene that were open
                // when the scroll was recorded have been closed in the mean time.
                // (Why do we apply selection and expanded state regardless then?
                // Because for those it still make sense to apply it even if only
                // some of the scenes that were open originally are still open, and
                // for the rest it will have no effect anyway.)
                bool anyOfScenesWereClosed = false;
                if (m_OpenSceneGUIDs != null)
                {
                    var currentSceneGUIDs = GetCurrentSceneGUIDs();
                    for (int i = 0; i < m_OpenSceneGUIDs.Length; i++)
                    {
                        if (!currentSceneGUIDs.Contains(m_OpenSceneGUIDs[i]))
                        {
                            anyOfScenesWereClosed = true;
                            break;
                        }
                    }
                }
                if (!anyOfScenesWereClosed)
                    hierarchy.sceneHierarchy.treeViewState.scrollPos.y = m_ScrollY;
            }

            if (SceneHierarchy.s_DebugPersistingExpandedState)
                DebugLog("Restoring", stage);
        }

        void DebugLog(string prefix, Stage stage)
        {
            Debug.Log(prefix + (stage.GetType().ToString()) + string.Format("-main stage: {0}, -scrollY: {1}, -selection {2}, -setSelection {3}",
                DebugUtils.ListToString(m_ExpandedSceneGameObjectInstanceIDs),
                m_ScrollY,
                m_LastClickedInstanceID,
                stage.setSelectionAndScrollWhenBecomingCurrentStage));
        }

        string[] GetCurrentSceneGUIDs()
        {
            int count = SceneManager.sceneCount;
            string[] sceneGUIDs = new string[count];
            for (int i = 0; i < count; i++)
                sceneGUIDs[i] = SceneManager.GetSceneAt(i).guid;
            return sceneGUIDs;
        }
    }
}
