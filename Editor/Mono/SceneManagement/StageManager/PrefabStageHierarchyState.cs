// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityEditor
{
    [Serializable]
    class PrefabStageHierarchyState
    {
        [SerializeField]
        float m_ScrollY;
        [SerializeField]
        List<UInt64> m_ExpandedPrefabGameObjectFileIDs = new List<UInt64>();
        [SerializeField]
        UInt64 m_LastClickedFileID;

        public void SaveStateFromHierarchy(SceneHierarchyWindow hierarchy, Stage stage)
        {
            var lastClickedGameObject = EditorUtility.InstanceIDToObject(hierarchy.sceneHierarchy.treeViewState.lastClickedID) as GameObject;
            m_LastClickedFileID = lastClickedGameObject != null ? Unsupported.GetOrGenerateFileIDHint(lastClickedGameObject) : 0;
            m_ExpandedPrefabGameObjectFileIDs = GetExpandedGameObjectFileIDs(hierarchy);

            m_ScrollY = hierarchy.sceneHierarchy.treeViewState.scrollPos.y;

            if (SceneHierarchy.s_DebugPersistingExpandedState)
                DebugLog("Saving", stage);
        }

        public void LoadStateIntoHierarchy(SceneHierarchyWindow hierarchy, Stage stage)
        {
            // Restore expanded state always
            RestoreExandedStateFromFileIDs(hierarchy, stage as PrefabStage, m_ExpandedPrefabGameObjectFileIDs, m_LastClickedFileID);

            // Restore selection and scroll value when requested
            if (stage.setSelectionAndScrollWhenBecomingCurrentStage)
            {
                Selection.activeInstanceID = hierarchy.sceneHierarchy.treeViewState.lastClickedID;
                hierarchy.sceneHierarchy.treeViewState.scrollPos.y = m_ScrollY;
            }

            if (SceneHierarchy.s_DebugPersistingExpandedState)
                DebugLog("Restoring", stage);
        }

        static List<UInt64> GetExpandedGameObjectFileIDs(SceneHierarchyWindow hierarchy)
        {
            var fileIDs = new List<UInt64>();
            var expandedGameObjects = hierarchy.sceneHierarchy.GetExpandedGameObjects();
            foreach (var go in expandedGameObjects)
            {
                UInt64 fileID = Unsupported.GetOrGenerateFileIDHint(go);
                if (fileID != 0)
                    fileIDs.Add(fileID);
            }
            return fileIDs;
        }

        static void RestoreExandedStateFromFileIDs(SceneHierarchyWindow hierarchy, PrefabStage prefabStage, List<UInt64> expandedGameObjectFileIDs, UInt64 lastClickedFileID)
        {
            var searchRoot = prefabStage.prefabContentsRoot;
            var fileIDToInstanceIDMapper = new FileIDToInstanceIDMapper(searchRoot.transform, expandedGameObjectFileIDs, lastClickedFileID);
            hierarchy.sceneHierarchy.treeViewState.lastClickedID = fileIDToInstanceIDMapper.instanceID;
            hierarchy.sceneHierarchy.treeViewState.expandedIDs = fileIDToInstanceIDMapper.instanceIDs;
            hierarchy.sceneHierarchy.treeViewState.expandedIDs.Sort(); // required to be sorted (see TreeViewState)
        }

        class FileIDToInstanceIDMapper
        {
            readonly List<UInt64> m_FileIDs;
            readonly UInt64 m_FileID = 0;

            public List<int> instanceIDs = new List<int>();
            public int instanceID = 0;

            public FileIDToInstanceIDMapper(Transform searchRoot, List<UInt64> fileIDs, UInt64 fileID)
            {
                m_FileIDs = fileIDs;
                m_FileID = fileID;

                var transformVisitor = new TransformVisitor();
                transformVisitor.VisitAll(searchRoot, AddGameObjectIfMatching, null);
            }

            public void AddGameObjectIfMatching(Transform transform, object userData)
            {
                UInt64 fileID = Unsupported.GetOrGenerateFileIDHint(transform.gameObject);
                if (m_FileIDs.Contains(fileID))
                    instanceIDs.Add(transform.gameObject.GetInstanceID());
                if (fileID == m_FileID)
                    instanceID = transform.gameObject.GetInstanceID();
            }
        }

        void DebugLog(string prefix, Stage stage)
        {
            Debug.Log(prefix + (stage.GetType().ToString()) + string.Format("-prefab stage: {0}, -scrollY: {1}, -selection {2}, -setSelection {3}",
                DebugUtils.ListToString(m_ExpandedPrefabGameObjectFileIDs),
                m_ScrollY,
                m_LastClickedFileID,
                stage.setSelectionAndScrollWhenBecomingCurrentStage));
        }
    }
}
