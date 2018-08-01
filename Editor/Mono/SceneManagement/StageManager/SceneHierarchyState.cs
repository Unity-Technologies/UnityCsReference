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
    class SceneHierarchyState
    {
        [SerializeField]
        List<UInt64> m_ExpandedPrefabGameObjectFileIDs = new List<UInt64>();
        [SerializeField]
        List<int> m_ExpandedSceneGameObjectInstanceIDs = new List<int>();
        [SerializeField]
        float m_ScrollY;
        [SerializeField]
        UInt64 m_LastClickedFileID;
        [SerializeField]
        int m_LastClickedInstanceID;

        public void SaveStateFromHierarchy(SceneHierarchyWindow hierarchy, StageNavigationItem stage)
        {
            var lastClickedGameObject = EditorUtility.InstanceIDToObject(hierarchy.sceneHierarchy.treeViewState.lastClickedID) as GameObject;
            if (stage.isMainStage)
            {
                m_LastClickedInstanceID = lastClickedGameObject != null ? lastClickedGameObject.GetInstanceID() : 0;
                m_ExpandedSceneGameObjectInstanceIDs = hierarchy.sceneHierarchy.treeViewState.expandedIDs;
            }
            else
            {
                m_LastClickedFileID = lastClickedGameObject != null ? GetOrGenerateFileID(lastClickedGameObject) : 0;
                m_ExpandedPrefabGameObjectFileIDs = GetExpandedGameObjectFileIDs(hierarchy);
            }

            m_ScrollY = hierarchy.sceneHierarchy.treeViewState.scrollPos.y;

            if (SceneHierarchy.s_DebugPersistingExpandedState)
                DebugLog("Saving", stage);
        }

        public void LoadStateIntoHierarchy(SceneHierarchyWindow hierarchy, StageNavigationItem stage)
        {
            // Restore expanded state always
            if (stage.isMainStage)
                hierarchy.sceneHierarchy.treeViewState.expandedIDs = m_ExpandedSceneGameObjectInstanceIDs;
            else
                RestoreExandedStateFromFileIDs(hierarchy, stage, m_ExpandedPrefabGameObjectFileIDs, m_LastClickedFileID);


            // Restore selection and scroll value when requested
            if (stage.setSelectionAndScrollWhenBecomingCurrentStage)
            {
                Selection.activeInstanceID = stage.isMainStage ? m_LastClickedInstanceID : hierarchy.sceneHierarchy.treeViewState.lastClickedID;
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
                UInt64 fileID = GetOrGenerateFileID(go);
                if (fileID != 0)
                    fileIDs.Add(fileID);
            }
            return fileIDs;
        }

        internal static UInt64 GetOrGenerateFileID(GameObject gameObject)
        {
            UInt64 fileID = Unsupported.GetFileIDHint(gameObject);

            if (fileID == 0)
            {
                // GenerateFileIDHint only work on saved nested prefabs instances.
                var instanceHandle = PrefabUtility.GetPrefabInstanceHandle(gameObject);
                if (instanceHandle != null)
                {
                    bool isPrefabInstanceSaved = Unsupported.GetFileIDHint(instanceHandle) != 0;
                    if (isPrefabInstanceSaved && PrefabUtility.IsPartOfNonAssetPrefabInstance(gameObject) && PrefabUtility.GetPrefabAssetType(gameObject) != PrefabAssetType.MissingAsset)
                        fileID = Unsupported.GenerateFileIDHint(gameObject);
                }
            }

            return fileID;
        }

        static void RestoreExandedStateFromFileIDs(SceneHierarchyWindow hierarchy, StageNavigationItem item, List<UInt64> expandedGameObjectFileIDs, UInt64 lastClickedFileID)
        {
            var searchRoot = item.prefabStage.prefabContentsRoot;
            var fileIDToInstanceIDMapper = new FileIDToInstanceIDMapper(searchRoot.transform, expandedGameObjectFileIDs, lastClickedFileID);
            hierarchy.sceneHierarchy.treeViewState.lastClickedID = fileIDToInstanceIDMapper.instanceID;
            hierarchy.sceneHierarchy.treeViewState.expandedIDs = fileIDToInstanceIDMapper.instanceIDs;
            hierarchy.sceneHierarchy.treeViewState.expandedIDs.Sort(); // required to be sorted (see TreeViewState)
        }

        public static GameObject FindFirstGameObjectThatMatchesFileID(Transform searchRoot, UInt64 fileID)
        {
            GameObject result = null;
            var transformVisitor = new TransformVisitor();
            transformVisitor.VisitAndAllowEarlyOut(searchRoot,
                (transform, userdata) =>
                {
                    UInt64 id = GetOrGenerateFileID(transform.gameObject);
                    if (id == fileID)
                    {
                        result = transform.gameObject;
                        return false; // stop searching
                    }
                    return true; // continue searching
                }
                , null);

            return result;
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
                UInt64 fileID = GetOrGenerateFileID(transform.gameObject);
                if (m_FileIDs.Contains(fileID))
                    instanceIDs.Add(transform.gameObject.GetInstanceID());
                if (fileID == m_FileID)
                    instanceID = transform.gameObject.GetInstanceID();
            }
        }

        void DebugLog(string prefix, StageNavigationItem stage)
        {
            Debug.Log(prefix + (stage.isMainStage ? " Main stage " : " Prefab stage ") + string.Format("-main stage: {0}, -prefab stage: {1}, -scrollY: {2}, -selection {3}, -setSelection {4}",
                DebugUtils.ListToString(m_ExpandedSceneGameObjectInstanceIDs),
                DebugUtils.ListToString(m_ExpandedPrefabGameObjectFileIDs),
                m_ScrollY,
                m_LastClickedInstanceID,
                stage.setSelectionAndScrollWhenBecomingCurrentStage));
        }
    }
}
