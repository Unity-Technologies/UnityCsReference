// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEditor
{
    internal static class CutCopyPasteUtility
    {
        internal static void CutGO()
        {
            CutBoard.CutGO();
            RepaintHierarchyWindowsAfterPaste();
        }

        internal static void CopyGO()
        {
            CutBoard.Reset();
            Unsupported.CopyGameObjectsToPasteboard();
        }

        internal static void PasteGO(Transform fallbackParent)
        {
            if (CutBoard.hasCutboardData)
            {
                CutBoard.PasteGameObjects(fallbackParent);
            }
            // If it is not a Cut operation, execute regular paste
            else
            {
                bool customParentIsSelected = GetIsCustomParentSelected(fallbackParent);
                Unsupported.PasteGameObjectsFromPasteboard();
                if (customParentIsSelected)
                    Selection.activeTransform.SetParent(fallbackParent, true);
            }
        }

        internal static void PasteGOAsChild()
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
                            CutBoard.PasteToScene(subScene, selected[0]);
                    }
                    else if (!isSubScene)
                    {
                        CutBoard.PasteAsChildren(selected[0]);
                    }
                }
                // paste after copy
                else if (pasteToSubScene || !isSubScene)
                {
                    Unsupported.PasteGameObjectsFromPasteboard(selected[0], pasteToSubScene ? subScene.handle : 0);
                }
            }
            RepaintHierarchyWindowsAfterPaste();
        }

        internal static void DuplicateGO(Transform fallbackParent)
        {
            CutBoard.Reset();
            bool customParentIsSelected = GetIsCustomParentSelected(fallbackParent);
            Unsupported.DuplicateGameObjectsUsingPasteboard();
            if (customParentIsSelected)
                Selection.activeTransform.SetParent(fallbackParent, true);
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
