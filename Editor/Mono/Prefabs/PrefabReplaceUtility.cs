// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor
{
    static class PrefabReplaceUtility
    {
        static bool s_CreateOverrides = false;
        static PrefabOverridesOptions s_PrefabOverideOptions = PrefabOverridesOptions.KeepAllPossibleOverrides;

        internal static void ReplaceCurrentSelectionWithPrefabUsingObjectPicker(object userdata)
        {
            var args = ((GameObject, PrefabOverridesOptions, bool))userdata;
            var contextClickedGameObject = args.Item1;
            var prefabOverrideOptions = args.Item2;
            var createOverrides = args.Item3;

            s_CreateOverrides = createOverrides;
            s_PrefabOverideOptions = prefabOverrideOptions;
            ObjectSelector.get.Show(null, typeof(GameObject), contextClickedGameObject, false, null, null, OnObjectSelectorSelectionUpdated, false);
        }

        private static void OnObjectSelectorSelectionUpdated(UnityEngine.Object obj)
        {
            var selectedPrefabAsset = (GameObject)obj;
            if (selectedPrefabAsset == null)
                return;

            if (!EditorUtility.IsPersistent(selectedPrefabAsset))
                return;

            var contextClickedGameObject = ObjectSelector.get.objectBeingEdited as GameObject;
            List<GameObject> listOfInstanceRoots;
            List<GameObject> listOfPlainGameObjects;
            FindGameObjectsToReplace(contextClickedGameObject, out listOfPlainGameObjects, out listOfInstanceRoots);

            if (listOfInstanceRoots.Count == 0 && listOfPlainGameObjects.Count == 0)
                return;

            if (listOfInstanceRoots.Count > 0)
                PrefabUtility.ReplacePrefabAssetOfPrefabInstances(listOfInstanceRoots.ToArray(), selectedPrefabAsset, GetPrefabReplacingSettingsForUI(s_PrefabOverideOptions), InteractionMode.UserAction);
            if (listOfPlainGameObjects.Count > 0)
                PrefabUtility.ConvertToPrefabInstances(listOfPlainGameObjects.ToArray(), selectedPrefabAsset, GetConvertToPrefabInstanceSettingsForUI(s_CreateOverrides), InteractionMode.UserAction);

            if (contextClickedGameObject != null && !Selection.Contains(contextClickedGameObject))
                Selection.activeGameObject = contextClickedGameObject;
        }

        internal static bool HasInstancesAnyOverrides(List<GameObject> listOfInstanceRoots)
        {
            foreach (var root in listOfInstanceRoots)
                if (PrefabUtility.HasPrefabInstanceAnyOverrides(root, false))
                    return true;

            return false;
        }

        internal static bool AddReplaceMenuItemsToMenuBasedOnCurrentSelection(GenericMenu menu, string parentMenuItemPath, GameObject contextGameObject, List<GameObject> listOfInstanceRoots, List<GameObject> listOfPlainGameObjects, GameObject prefabAsset)
        {
            if (listOfInstanceRoots.Count == 0 && listOfPlainGameObjects.Count == 0)
                return false;

            string GetMenuItemText(string text, string parentMenuItemPath, bool needsObjectSelector)
            {
                return string.Format("{0}{1}{2}", parentMenuItemPath, text, (needsObjectSelector ? "..." : ""));
            }

            bool needObjectSelector = prefabAsset == null;
            var replaceText = L10n.Tr("Replace");
            var replaceAndKeepOverridesText = L10n.Tr("Replace and Keep Overrides");
            var reconnectPrefabText = L10n.Tr("Reconnect Prefab");
            var replaceAndReconnectPrefabText = L10n.Tr("Replace and Reconnect Prefab");

            if (listOfInstanceRoots.Count > 0)
            {
                var hasAnyOverrides = HasInstancesAnyOverrides(listOfInstanceRoots);

                if (hasAnyOverrides)
                {
                    var text1 = new GUIContent(GetMenuItemText(replaceText, parentMenuItemPath, needObjectSelector));
                    var text2 = new GUIContent(GetMenuItemText(replaceAndKeepOverridesText, parentMenuItemPath, needObjectSelector));

                    if (needObjectSelector)
                    {
                        menu.AddItem(text1, false, ReplaceCurrentSelectionWithPrefabUsingObjectPicker, (contextGameObject, PrefabOverridesOptions.ClearAllNonDefaultOverrides, true));
                        menu.AddItem(text2, false, ReplaceCurrentSelectionWithPrefabUsingObjectPicker, (contextGameObject, PrefabOverridesOptions.KeepAllPossibleOverrides, false));
                    }
                    else
                    {
                        menu.AddItem(text1, false, MenuActionForConvertAndReplacePrefabInstance, (contextGameObject, listOfInstanceRoots, PrefabOverridesOptions.ClearAllNonDefaultOverrides, listOfPlainGameObjects, true, prefabAsset));
                        menu.AddItem(text2, false, MenuActionForConvertAndReplacePrefabInstance, (contextGameObject, listOfInstanceRoots, PrefabOverridesOptions.KeepAllPossibleOverrides, listOfPlainGameObjects, false, prefabAsset));
                    }
                }
                else // no overrides on instances
                {
                    var text1 = new GUIContent(GetMenuItemText(replaceText, parentMenuItemPath, needObjectSelector));
                    GUIContent text2 = null;
                    if (listOfPlainGameObjects.Count > 0)
                        text2 = new GUIContent(GetMenuItemText(replaceAndReconnectPrefabText, parentMenuItemPath, needObjectSelector));
                    else
                        text2 = new GUIContent(GetMenuItemText(replaceAndKeepOverridesText, parentMenuItemPath, needObjectSelector));

                    if (needObjectSelector)
                    {
                        menu.AddItem(text1, false, ReplaceCurrentSelectionWithPrefabUsingObjectPicker, (contextGameObject, PrefabOverridesOptions.KeepAllPossibleOverrides, false));
                        if (listOfPlainGameObjects.Count > 0)
                            menu.AddItem(text2, false, ReplaceCurrentSelectionWithPrefabUsingObjectPicker, (contextGameObject, PrefabOverridesOptions.ClearAllNonDefaultOverrides, true));
                        else
                            menu.AddDisabledItem(text2, false);
                    }
                    else
                    {
                        menu.AddItem(text1, false, MenuActionForConvertAndReplacePrefabInstance, (contextGameObject, listOfInstanceRoots, PrefabOverridesOptions.KeepAllPossibleOverrides, listOfPlainGameObjects, false, prefabAsset));
                        if (listOfPlainGameObjects.Count > 0)
                            menu.AddItem(text2, false, MenuActionForConvertAndReplacePrefabInstance, (contextGameObject, listOfInstanceRoots, PrefabOverridesOptions.ClearAllNonDefaultOverrides, listOfPlainGameObjects, true, prefabAsset));
                        else
                            menu.AddDisabledItem(text2, false);
                    }
                }
            }
            else if (listOfPlainGameObjects.Count > 0)
            {
                // Only plain gameobjects
                var text1 = new GUIContent(GetMenuItemText(replaceText, parentMenuItemPath, needObjectSelector));
                var text2 = new GUIContent(GetMenuItemText(reconnectPrefabText, parentMenuItemPath, needObjectSelector));

                if (needObjectSelector)
                {
                    menu.AddItem(text1, false, ReplaceCurrentSelectionWithPrefabUsingObjectPicker, (contextGameObject, PrefabOverridesOptions.KeepAllPossibleOverrides, false));
                    menu.AddItem(text2, false, ReplaceCurrentSelectionWithPrefabUsingObjectPicker, (contextGameObject, PrefabOverridesOptions.ClearAllNonDefaultOverrides, true));
                }
                else
                {
                    menu.AddItem(text1, false, MenuActionForConvertAndReplacePrefabInstance, (contextGameObject, listOfInstanceRoots, PrefabOverridesOptions.KeepAllPossibleOverrides, listOfPlainGameObjects, false, prefabAsset));
                    menu.AddItem(text2, false, MenuActionForConvertAndReplacePrefabInstance, (contextGameObject, listOfInstanceRoots, PrefabOverridesOptions.ClearAllNonDefaultOverrides, listOfPlainGameObjects, true, prefabAsset));
                }
            }

            return true;
        }

        // Returns true if this this function uses the event and propagation of the drag should be stopped after this
        internal static bool GetDragVisualModeAndShowMenuWithReplaceMenuItemsWhenNeeded(GameObject droppedUponGameObject, bool draggingUpon, bool perform, bool addAsChildMenuItem, bool dragInHierarchy, out DragAndDropVisualMode cursorVisualMode)
        {
            cursorVisualMode = DragAndDropVisualMode.None;

            GameObject prefabAsset = TryGetSinglePrefabAssetFromDraggedObjects();
            if (prefabAsset == null)
                return false;

            if (Event.current == null)
                return false;

            if (!dragInHierarchy || EditorGUI.actionKey)
            {
                if (!draggingUpon)
                {
                    cursorVisualMode = DragAndDropVisualMode.Rejected;
                    return true;
                }

                List<GameObject> listOfInstanceRoots;
                List<GameObject> listOfPlainGameObjects;
                FindGameObjectsToReplace(droppedUponGameObject, out listOfPlainGameObjects, out listOfInstanceRoots);

                if (listOfInstanceRoots.Count == 0 && listOfPlainGameObjects.Count == 0)
                {
                    cursorVisualMode = DragAndDropVisualMode.Rejected;
                    return true;
                }

                cursorVisualMode = DragAndDropVisualMode.Link;

                if (perform)
                {
                    if (!dragInHierarchy && listOfInstanceRoots.Count > 0 && listOfPlainGameObjects.Count == 0 && !HasInstancesAnyOverrides(listOfInstanceRoots))
                    {
                        return false; // don't show the drag menu when we know we will only have one menuitem over the objectfield in the Inspector
                    }

                    Event.current.Use();

                    var menu = new GenericMenu();

                    AddReplaceMenuItemsToMenuBasedOnCurrentSelection(menu, "", droppedUponGameObject, listOfInstanceRoots, listOfPlainGameObjects, prefabAsset);
                    if (addAsChildMenuItem)
                    {
                        menu.AddSeparator("");
                        menu.AddItem(EditorGUIUtility.TrTextContent("Add as Child"), false, MenuActionForInstantiateDraggedPrefabAsChild, (droppedUponGameObject, prefabAsset));
                    }
                    menu.AddSeparator("");
                    menu.AddItem(EditorGUIUtility.TrTextContent("Cancel"), false, () => {/*nop*/ });
                    menu.ShowAsContext();
                }
                return true;
            }
            return false;
        }

        internal static GameObject TryGetSinglePrefabAssetFromDraggedObjects()
        {
            var draggedObjects = DragAndDrop.objectReferences;
            GameObject prefabAsset = null;
            foreach (var draggedObject in draggedObjects)
            {
                if (draggedObject is GameObject && EditorUtility.IsPersistent(draggedObject))
                {
                    if (prefabAsset == null)
                        prefabAsset = (GameObject)draggedObject;
                    else
                        return null;
                }
            }
            return prefabAsset;
        }

        internal static void FindGameObjectsToReplace(GameObject droppedUponGameObject, out List<GameObject> listOfPlainGameObjects, out List<GameObject> listOfInstanceRoots)
        {
            listOfInstanceRoots = new List<GameObject>();
            listOfPlainGameObjects = new List<GameObject>();

            // Dropping on single
            if (droppedUponGameObject != null && !Selection.gameObjects.Contains(droppedUponGameObject))
            {
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(droppedUponGameObject))
                    listOfInstanceRoots.Add(droppedUponGameObject);
                else if (!PrefabUtility.IsPartOfAnyPrefab(droppedUponGameObject))
                    listOfPlainGameObjects.Add(droppedUponGameObject);

                return;
            }

            // Dropping on selection: need to find the common roots
            var selectedGameObjects = Selection.GetFiltered<GameObject>(SelectionMode.TopLevel | SelectionMode.ExcludePrefab | SelectionMode.Editable);
            foreach (var go in selectedGameObjects)
            {
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(go))
                    listOfInstanceRoots.Add(go);
                else if (!PrefabUtility.IsPartOfNonAssetPrefabInstance(go))
                    listOfPlainGameObjects.Add(go);
            }
        }

        static void MenuActionForConvertAndReplacePrefabInstance(object userData)
        {
            var args = ((GameObject, List<GameObject>, PrefabOverridesOptions, List<GameObject>, bool, GameObject))userData;
            var contextGameObject = args.Item1;
            var droppedOnInstances = args.Item2;
            var overrideOptionsOnReplace = args.Item3;
            var droppedOnGameObjects = args.Item4;
            var createOverridesOnConvert = args.Item5;
            var prefabAssetRoot = args.Item6;

            Undo.IncrementCurrentGroup();

            if (droppedOnInstances.Count > 0)
                PrefabUtility.ReplacePrefabAssetOfPrefabInstances(droppedOnInstances.ToArray(), prefabAssetRoot, GetPrefabReplacingSettingsForUI(overrideOptionsOnReplace), InteractionMode.UserAction);
            if (droppedOnGameObjects.Count > 0)
                PrefabUtility.ConvertToPrefabInstances(droppedOnGameObjects.ToArray(), prefabAssetRoot, GetConvertToPrefabInstanceSettingsForUI(createOverridesOnConvert), InteractionMode.UserAction);

            if (contextGameObject != null && !Selection.Contains(contextGameObject))
                Selection.activeGameObject = contextGameObject;

            Undo.IncrementCurrentGroup();
        }

        static void MenuActionForInstantiateDraggedPrefabAsChild(object userData)
        {
            var args = ((GameObject, GameObject))userData;
            var droppedUponGameObject = args.Item1;
            var prefabAssetRoot = args.Item2;

            Undo.IncrementCurrentGroup();

            var childInstanceIDs = GetChildrenInstanceIDs(droppedUponGameObject);
            PrefabUtility.InstantiateDraggedPrefabUpon(droppedUponGameObject, prefabAssetRoot);
            var newChildren = GetNewChildren(droppedUponGameObject, childInstanceIDs);
            if (newChildren != null && newChildren.Count > 0)
                Selection.instanceIDs = newChildren.ToArray();

            Undo.IncrementCurrentGroup();
        }

        static HashSet<int> GetChildrenInstanceIDs(GameObject parent)
        {
            var childInstanceIDs = new HashSet<int>();
            var transform = parent.transform;
            var childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var childInstanceID = transform.GetChild(i).gameObject.GetInstanceID();
                childInstanceIDs.Add(childInstanceID);
            }
            return childInstanceIDs;
        }

        static List<int> GetNewChildren(GameObject parent, HashSet<int> oldChildren)
        {
            var newChildren = new List<int>();
            var transform = parent.transform;
            var childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var childInstanceID = transform.GetChild(i).gameObject.GetInstanceID();
                if (!oldChildren.Contains(childInstanceID))
                    newChildren.Add(childInstanceID);
            }
            return newChildren;
        }

        public static ConvertToPrefabInstanceSettings GetConvertToPrefabInstanceSettingsForUI(bool createOverrides)
        {
            return new ConvertToPrefabInstanceSettings
            {
                objectMatchMode = ObjectMatchMode.ByHierarchy,
                componentsNotMatchedBecomesOverride = createOverrides,
                gameObjectsNotMatchedBecomesOverride = createOverrides,
                recordPropertyOverridesOfMatches = createOverrides,
                changeRootNameToAssetName = true,
                logInfo = false
            };
        }

        public static PrefabReplacingSettings GetPrefabReplacingSettingsForUI(PrefabOverridesOptions prefabOverridesOptions)
        {
            return new PrefabReplacingSettings
            {
                objectMatchMode = ObjectMatchMode.ByHierarchy,
                prefabOverridesOptions = prefabOverridesOptions,
                changeRootNameToAssetName = true,
                logInfo = false
            };
        }

    }

    internal static class PrefabReplaceSettingsUserPreferences
    {
        static SavedInt m_ObjectMatchMode = new SavedInt("PrefabReplace.ObjectMatchMode", (int)ObjectMatchMode.ByHierarchy);
        static SavedBool m_GameObjectsNotMatchedBecomesOverride = new SavedBool("PrefabReplace.GameObjectsNotMatchedBecomesOverride", true);
        static SavedBool m_ComponentsNotMatchedBecomesOverride = new SavedBool("PrefabReplace.ComponentsNotMatchedBecomesOverride", true);
        static SavedInt m_ClearOverridesOptions = new SavedInt("PrefabReplace.ClearOverridesOptions", (int)PrefabOverridesOptions.KeepAllPossibleOverrides);
        static SavedBool m_UseAssetName = new SavedBool("PrefabReplace.UseAssetName", true);
        static SavedBool m_LogInfo = new SavedBool("PrefabReplace.LogInfo", false);

        public static ObjectMatchMode objectMatchMode { get { return (ObjectMatchMode)m_ObjectMatchMode.value; } set { m_ObjectMatchMode.value = Convert.ToInt32(value); } }
        public static PrefabOverridesOptions prefabOverridesOptions { get { return (PrefabOverridesOptions)m_ClearOverridesOptions.value; } set { m_ClearOverridesOptions.value = Convert.ToInt32(value); } }

        public static bool gameObjectsNotMatchedBecomesOverride { get { return m_GameObjectsNotMatchedBecomesOverride.value; } set { m_GameObjectsNotMatchedBecomesOverride.value = value; } }
        public static bool componentsNotMatchedBecomesOverride { get { return m_ComponentsNotMatchedBecomesOverride.value; } set { m_ComponentsNotMatchedBecomesOverride.value = value; } }
        public static bool useAssetName { get { return m_UseAssetName.value; } set { m_UseAssetName.value = value; } }
        public static bool logInfo { get { return m_LogInfo.value; } set { m_LogInfo.value = value; } }

        public static ConvertToPrefabInstanceSettings GetConvertToPrefabInstancePreferences()
        {
            return new ConvertToPrefabInstanceSettings
            {
                objectMatchMode = objectMatchMode,
                componentsNotMatchedBecomesOverride = componentsNotMatchedBecomesOverride,
                gameObjectsNotMatchedBecomesOverride = gameObjectsNotMatchedBecomesOverride,
                changeRootNameToAssetName = useAssetName,
                logInfo = logInfo
            };
        }

        public static PrefabReplacingSettings GetPrefabReplacingPreferences()
        {
            return new PrefabReplacingSettings
            {
                objectMatchMode = objectMatchMode,
                prefabOverridesOptions = prefabOverridesOptions,
                logInfo = logInfo
            };
        }
    }
}
