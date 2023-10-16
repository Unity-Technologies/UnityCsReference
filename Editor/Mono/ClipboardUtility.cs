// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    public static class ClipboardUtility
    {
        public static Func<GameObject, bool> canCopyGameObject;
        public static Func<GameObject, bool> canCutGameObject;
        public static Func<GameObject, bool> canDuplicateGameObject;
        public static event Action<GameObject[]> copyingGameObjects;
        public static event Action<GameObject[]> cuttingGameObjects;
        public static event Action<GameObject[]> duplicatingGameObjects;
        public static event Action<GameObject[]> duplicatedGameObjects;
        public static event Action<GameObject[]> rejectedGameObjects;
        public static event Action<GameObject[]> pastedGameObjects;

        private static void FilterSelection(Func<GameObject, bool> filter)
        {
            if (filter == null)
                return;

            // If we dont have any filters we really should just leave right now
            Delegate[] invocationList = filter.GetInvocationList();
            int invocationListLength = invocationList.Length;

            if (invocationListLength == 0)
                return;

            // Make our tracking objects
            int selectedGameObjectsLength = Selection.gameObjects.Length;
            Queue<UnityEngine.Object> pass = new Queue<UnityEngine.Object>(selectedGameObjectsLength);
            Queue<GameObject> fail = new Queue<GameObject>(selectedGameObjectsLength);

            // Iterate through our selected game objects, checking each
            for (int i = 0; i < selectedGameObjectsLength; i++)
            {
                GameObject currentGameObject = Selection.gameObjects[i];

                bool shouldInclude = true;
                for (int j = 0; j < invocationListLength; j++)
                {
                    Func<GameObject, bool> func = (Func<GameObject, bool>)invocationList[j];

                    // Passed, next check
                    if (func.Invoke(currentGameObject))
                        continue;

                    // Failed, add to failures
                    if (fail.Contains(currentGameObject))
                        continue;

                    fail.Enqueue(currentGameObject);
                    shouldInclude = false;
                    break;
                }

                // Passed everything add to passed gameobjects
                if (shouldInclude && !pass.Contains(currentGameObject))
                {
                    pass.Enqueue(currentGameObject);
                }
            }

            // Update the selection with the filtered GameObjects
            Selection.objects = pass.ToArray();

            // Notify after filter, for that off chance someone wants to subscribe for logging OR be able to reintroduce
            // into the selection an object at their control.
            if (rejectedGameObjects != null && fail.Count > 0)
            {
                rejectedGameObjects.Invoke(fail.ToArray());
            }
        }

        internal static void CutGO()
        {
            FilterSelection(canCutGameObject);
            cuttingGameObjects?.Invoke(Selection.gameObjects);
            CutBoard.CutGO();
            RepaintHierarchyWindowsAfterPaste();
        }

        internal static void CopyGO()
        {
            CutBoard.Reset();
            FilterSelection(canCopyGameObject);
            copyingGameObjects?.Invoke(Selection.gameObjects);
            Unsupported.CopyGameObjectsToPasteboard();
        }

        internal static void PasteGO(Transform fallbackParent)
        {
            if (CutBoard.hasCutboardData)
            {
                CutBoard.PasteGameObjects(fallbackParent, false);
            }
            // If it is not a Cut operation, execute regular paste
            else
            {
                bool customParentIsSelected = GetIsCustomParentSelected(fallbackParent);
                Unsupported.PasteGameObjectsFromPasteboard();
                if (customParentIsSelected)
                    Selection.activeTransform.SetParent(fallbackParent, true);
            }
            pastedGameObjects?.Invoke(Selection.gameObjects);
        }

        internal static void PasteGOAsChild(bool worldPositionStays = false)
        {
            Transform[] selected = Selection.transforms;

            // paste as a child if a gameObject is selected
            if (selected.Length == 1)
            {
                Scene subScene = new Scene();
                bool pasteToSubScene = false;
                bool isSubScene = false;

                // If target is subScene make sure we just move objects under subScene
                if (SubSceneGUI.IsSubSceneHeader(selected[0].gameObject))
                {
                    subScene = SubSceneGUI.GetSubScene(selected[0].gameObject);
                    isSubScene = subScene.isSubScene;
                    pasteToSubScene = subScene.IsValid();
                }

                // handle paste after cut
                if (CutBoard.hasCutboardData)
                {
                    if (pasteToSubScene)
                    {
                        if (subScene.handle != 0)
                        {
                            CutBoard.PasteToScene(subScene, selected[0]);
                            pastedGameObjects?.Invoke(Selection.gameObjects);
                        }
                    }
                    else if (!isSubScene)
                    {
                        CutBoard.PasteAsChildren(selected[0], worldPositionStays);
                        pastedGameObjects?.Invoke(Selection.gameObjects);
                    }
                }
                // paste after copy
                else if (pasteToSubScene || !isSubScene)
                {
                    Unsupported.PasteGameObjectsFromPasteboard(selected[0],
                        pasteToSubScene ? subScene.handle : 0,
                        worldPositionStays);
                    pastedGameObjects?.Invoke(Selection.gameObjects);
                }
            }
            RepaintHierarchyWindowsAfterPaste();
        }

        internal static void DuplicateGO(Transform fallbackParent)
        {
            CutBoard.Reset();
            FilterSelection(canDuplicateGameObject);
            bool customParentIsSelected = GetIsCustomParentSelected(fallbackParent);
            duplicatingGameObjects?.Invoke(Selection.gameObjects);
            Unsupported.DuplicateGameObjectsUsingPasteboard();
            if (customParentIsSelected)
                Selection.activeTransform.SetParent(fallbackParent, true);
            duplicatedGameObjects?.Invoke(Selection.gameObjects);
        }

        internal static bool CanPasteAsChild()
        {
            bool canPaste = (Unsupported.CanPasteGameObjectsFromPasteboard() || CutBoard.hasCutboardData)
                && ((SceneHierarchyWindow.lastInteractedHierarchyWindow != null && SceneHierarchyWindow.lastInteractedHierarchyWindow.sceneHierarchy != null)
                    || SceneView.lastActiveSceneView != null)
                && Selection.transforms.Length == 1;

            var activeGO = Selection.activeGameObject;
            if (activeGO != null && SubSceneGUI.IsSubSceneHeader(activeGO))
                return canPaste && SubSceneGUI.GetSubScene(activeGO).IsValid();

            return canPaste;
        }

        internal static bool GetIsCustomParentSelected(Transform fallbackParent)
        {
            if (fallbackParent == null)
                return false;

            GameObject[] selected = Selection.gameObjects;
            for (int i = 0; i < selected.Length; i++)
            {
                if (selected[i] == fallbackParent.gameObject)
                    return true;
            }

            return false;
        }

        static void RepaintHierarchyWindowsAfterPaste()
        {
            if (SceneHierarchyWindow.lastInteractedHierarchyWindow == null)
                return;


            foreach (var win in SceneHierarchyWindow.GetAllSceneHierarchyWindows())
            {
                win.Repaint();
            }
        }

        internal static void ResetCutboardAndRepaintHierarchyWindows()
        {
            if (CutBoard.hasCutboardData)
            {
                CutBoard.Reset();
                RepaintHierarchyWindowsAfterPaste();
            }
        }
    }
}
