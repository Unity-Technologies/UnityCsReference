// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;

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

        public void SaveStateFromHierarchy(SceneHierarchyWindow hierarchy, Stage stage)
        {
            var lastClickedGameObject = EditorUtility.InstanceIDToObject(hierarchy.sceneHierarchy.treeViewState.lastClickedID) as GameObject;
            m_LastClickedInstanceID = lastClickedGameObject != null ? lastClickedGameObject.GetInstanceID() : 0;
            m_ExpandedSceneGameObjectInstanceIDs = hierarchy.sceneHierarchy.treeViewState.expandedIDs;

            m_ScrollY = hierarchy.sceneHierarchy.treeViewState.scrollPos.y;

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
    }
}
