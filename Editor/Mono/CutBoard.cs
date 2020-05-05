// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal static class CutBoard
    {
        internal static bool hasCutboardData { get { return m_GOCutboard != null && m_GOCutboard.Length > 0; } }
        private static Transform[] m_GOCutboard;
        private static Object[] m_SelectedObjects;
        private static HashSet<Transform> m_CutAffectedGOs = new HashSet<Transform>();
        private const string kCutAndPaste = "Cut And Paste";
        private static Stage m_StageCutWasPerformedIn;

        internal static void CutGO()
        {
            m_GOCutboard = Selection.transforms;
            m_SelectedObjects = Selection.objects;
            // Selection.transform does not provide correct list order, so we have to do it manually
            m_GOCutboard = m_GOCutboard.ToList().OrderBy(g => Array.IndexOf(m_SelectedObjects, g.gameObject)).ToArray();

            // Return if nothing selected
            if (!hasCutboardData)
                return;

            m_StageCutWasPerformedIn = StageUtility.GetStage(m_GOCutboard[0].gameObject);

            // If cut gameObject is prefab, get its root transform
            for (int i = 0; i < m_GOCutboard.Length; i++)
            {
                if (PrefabUtility.GetPrefabAssetType(m_GOCutboard[i].gameObject) != PrefabAssetType.NotAPrefab)
                {
                    m_GOCutboard[i] = PrefabUtility.GetOutermostPrefabInstanceRoot(m_GOCutboard[i].gameObject)?.transform;
                }
            }

            // Cut gameObjects should be visually marked as cut, so adding them to the hashset
            m_CutAffectedGOs.Clear();
            foreach (var item in m_GOCutboard)
            {
                m_CutAffectedGOs.Add(item);
                AddChildrenToHashset(item);
            }

            // Clean pasteboard when cutting gameObjects
            Unsupported.ClearPasteboard();
        }

        internal static bool CanGameObjectsBePasted()
        {
            return hasCutboardData && AreCutAndPasteStagesSame();
        }

        internal static void PasteGameObjects(Transform fallbackParent)
        {
            if (!AreCutAndPasteStagesSame())
                return;

            // Paste as a sibling of a active transform
            if (Selection.activeTransform != null)
            {
                PasteAsSiblings(Selection.activeTransform);
            }
            // If nothing selected, paste as child of the fallback parent if present
            else if (fallbackParent != null)
            {
                PasteAsChildren(fallbackParent);
            }
            // Otherwise, move to the scene of the active object
            else
            {
                Scene targetScene = EditorSceneManager.GetSceneByHandle(Selection.activeInstanceID);
                PasteToScene(targetScene);
            }
        }

        internal static void PasteAsChildren(Transform parent)
        {
            if (m_GOCutboard == null || !AreCutAndPasteStagesSame())
                return;

            foreach (var go in m_GOCutboard)
            {
                if (go != null)
                {
                    SetParent(go, parent);
                }
            }

            Selection.objects = m_SelectedObjects;
            // Reset cutBoard and greyed out gameObject list after paste
            Reset();
        }

        private static void PasteAsSiblings(Transform target)
        {
            foreach (var go in m_GOCutboard)
            {
                if (go != null)
                {
                    if (target.parent != null)
                        SetParent(go, target.parent);
                    else
                        MoveToScene(go, target.gameObject.scene);
                }
            }

            Selection.objects = m_SelectedObjects;
            // Reset cutBoard and greyed out gameObject list after paste
            Reset();
        }

        private static void PasteToScene(Scene targetScene)
        {
            foreach (var go in m_GOCutboard)
            {
                if (go != null)
                {
                    MoveToScene(go, targetScene);
                }
            }

            Selection.objects = m_SelectedObjects;
            // Reset cutBoard and greyed out gameObject list after paste
            Reset();
        }

        private static void SetParent(Transform go, Transform parent)
        {
            Undo.SetTransformParent(go, parent, kCutAndPaste);
            go.SetAsLastSibling();
        }

        private static void MoveToScene(Transform current, Scene target)
        {
            if (current == null)
                return;

            Undo.SetTransformParent(current, null, kCutAndPaste);

            if (target.isLoaded)
            {
                Undo.MoveGameObjectToScene(current.gameObject, target, kCutAndPaste);
            }

            current.SetAsLastSibling();
        }

        private static void AddChildrenToHashset(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform childTransform = parent.transform.GetChild(i);
                m_CutAffectedGOs.Add(childTransform);
                if (childTransform.childCount > 0)
                {
                    AddChildrenToHashset(childTransform);
                }
            }
        }

        internal static bool IsGameObjectPartOfCutAndPaste(GameObject gameObject)
        {
            if (gameObject == null || !hasCutboardData)
                return false;

            var transform = gameObject.transform;
            if (transform == null)
                return false;

            if (m_CutAffectedGOs.Contains(transform))
                return true;

            return false;
        }

        internal static void Reset()
        {
            m_SelectedObjects = null;
            m_GOCutboard = null;
            m_CutAffectedGOs.Clear();
        }

        internal static bool AreCutAndPasteStagesSame()
        {
            return m_StageCutWasPerformedIn == StageUtility.GetCurrentStage();
        }
    }
}
