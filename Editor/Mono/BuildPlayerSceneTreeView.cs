// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEditor.IMGUI.Controls;

namespace UnityEditor
{
    internal class BuildPlayerSceneTreeViewItem : TreeViewItem
    {
        private const string kAssetsFolder = "Assets/";
        private const string kSceneExtension = ".unity";

        public static int kInvalidCounter = -1;

        public bool active;
        public int counter;
        public string fullName;
        public GUID guid;
        public void UpdateName()
        {
            var name = AssetDatabase.GUIDToAssetPath(guid.ToString());
            if (name != fullName)
            {
                fullName = name;

                displayName = fullName;
                if (displayName.StartsWith(kAssetsFolder))
                    displayName = displayName.Remove(0, kAssetsFolder.Length);
                var ext = displayName.LastIndexOf(kSceneExtension);
                if (ext > 0)
                    displayName = displayName.Substring(0, ext);
            }
        }

        public BuildPlayerSceneTreeViewItem(int id, int depth, string path, bool state) : base(id, depth)
        {
            active = state;
            counter = kInvalidCounter;
            guid = new GUID(AssetDatabase.AssetPathToGUID(path));
            fullName = "";
            displayName = path;
            UpdateName();
        }
    }
    internal class BuildPlayerSceneTreeView : TreeView
    {
        public BuildPlayerSceneTreeView(TreeViewState state) : base(state)
        {
            showBorder = true;
            EditorBuildSettings.sceneListChanged += HandleExternalSceneListChange;
        }

        internal void UnsubscribeListChange()
        {
            EditorBuildSettings.sceneListChanged -= HandleExternalSceneListChange;
        }

        private void HandleExternalSceneListChange()
        {
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem(-1, -1);
            root.children = new List<TreeViewItem>();

            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var sc in scenes)
            {
                var item = new BuildPlayerSceneTreeViewItem(sc.guid.GetHashCode(), 0, sc.path, sc.enabled);
                root.AddChild(item);
            }
            return root;
        }

        protected override bool CanBeParent(TreeViewItem item)
        {
            return false;
        }

        protected override void BeforeRowsGUI()
        {
            int counter = 0;
            foreach (var item in rootItem.children)
            {
                var bpst = item as BuildPlayerSceneTreeViewItem;
                if (bpst != null)
                    bpst.UpdateName();

                //Need to set counter here because RowGUI is only called on items that are visible.
                if (bpst.active)
                {
                    bpst.counter = counter;
                    counter++;
                }
                else
                    bpst.counter = BuildPlayerSceneTreeViewItem.kInvalidCounter;
            }

            base.BeforeRowsGUI();
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var sceneItem = args.item as BuildPlayerSceneTreeViewItem;
            if (sceneItem != null)
            {
                var sceneWasDeleted = sceneItem.guid.Empty();
                var sceneExists = !sceneWasDeleted && File.Exists(sceneItem.fullName);

                using (new EditorGUI.DisabledScope(!sceneExists))
                {
                    var newState = sceneItem.active;
                    if (!sceneExists)
                        newState = false;
                    newState = GUI.Toggle(new Rect(args.rowRect.x, args.rowRect.y, 16f, 16f), newState, "");
                    if (newState != sceneItem.active)
                    {
                        if (GetSelection().Contains(sceneItem.id))
                        {
                            var selection = GetSelection();
                            foreach (var id in selection)
                            {
                                var item = FindItem(id, rootItem) as BuildPlayerSceneTreeViewItem;
                                item.active = newState;
                            }
                        }
                        else
                        {
                            sceneItem.active = newState;
                        }

                        EditorBuildSettings.scenes = GetSceneList();
                    }

                    base.RowGUI(args);

                    if (sceneItem.counter != BuildPlayerSceneTreeViewItem.kInvalidCounter)
                    {
                        TreeView.DefaultGUI.LabelRightAligned(args.rowRect, "" + sceneItem.counter, args.selected, args.focused);
                    }
                    else if (sceneItem.displayName == string.Empty || !sceneExists)
                    {
                        TreeView.DefaultGUI.LabelRightAligned(args.rowRect, "Deleted", args.selected, args.focused);
                    }
                }
            }
            else
                base.RowGUI(args);
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            DragAndDropVisualMode visualMode = DragAndDropVisualMode.None;

            var draggedIDs = DragAndDrop.GetGenericData("BuildPlayerSceneTreeViewItem") as List<int>;
            if (draggedIDs != null && draggedIDs.Count > 0)
            {
                visualMode = DragAndDropVisualMode.Move;
                if (args.performDrop)
                {
                    int newIndex = FindDropAtIndex(args);

                    var result = new List<TreeViewItem>();
                    int toInsert = 0;
                    foreach (var item in rootItem.children)
                    {
                        if (toInsert == newIndex)
                        {
                            foreach (var id in draggedIDs)
                            {
                                result.Add(FindItem(id, rootItem));
                            }
                        }
                        toInsert++;
                        if (!draggedIDs.Contains(item.id))
                        {
                            result.Add(item);
                        }
                    }

                    if (result.Count < rootItem.children.Count) //must be appending.
                    {
                        foreach (var id in draggedIDs)
                        {
                            result.Add(FindItem(id, rootItem));
                        }
                    }
                    rootItem.children = result;
                    EditorBuildSettings.scenes = GetSceneList();
                    ReloadAndSelect(draggedIDs);
                    Repaint();
                }
            }
            else if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
            {
                visualMode = DragAndDropVisualMode.Copy;
                if (args.performDrop)
                {
                    var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
                    var scenesToAdd = new List<EditorBuildSettingsScene>();
                    var selection = new List<int>();

                    foreach (var path in DragAndDrop.paths)
                    {
                        if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(SceneAsset))
                        {
                            var guid = new GUID(AssetDatabase.AssetPathToGUID(path));
                            selection.Add(guid.GetHashCode());

                            bool unique = true;
                            foreach (var scene in scenes)
                            {
                                if (scene.path == path)
                                {
                                    unique = false;
                                    break;
                                }
                            }
                            if (unique)
                                scenesToAdd.Add(new EditorBuildSettingsScene(path, true));
                        }
                    }


                    int newIndex = FindDropAtIndex(args);
                    scenes.InsertRange(newIndex, scenesToAdd);
                    EditorBuildSettings.scenes = scenes.ToArray();
                    ReloadAndSelect(selection);
                    Repaint();
                }
            }
            return visualMode;
        }

        private void ReloadAndSelect(IList<int> hashCodes)
        {
            Reload();
            SetSelection(hashCodes, TreeViewSelectionOptions.RevealAndFrame);
            SelectionChanged(hashCodes);
        }

        protected override void DoubleClickedItem(int id)
        {
            BuildPlayerSceneTreeViewItem item = FindItem(id , rootItem) as BuildPlayerSceneTreeViewItem;
            int instanceID = AssetDatabase.GetInstanceIDFromGUID(AssetDatabase.AssetPathToGUID(item.fullName));
            EditorGUIUtility.PingObject(instanceID);
        }

        protected int FindDropAtIndex(DragAndDropArgs args)
        {
            int indexToDrop = args.insertAtIndex;

            // covers if(args.dragAndDropPosition == DragAndDropPosition.OutsideItems) and a safety check.
            if (indexToDrop < 0 || indexToDrop > rootItem.children.Count)
                indexToDrop = rootItem.children.Count;

            return indexToDrop;
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return true;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.paths = null;
            DragAndDrop.objectReferences = new UnityEngine.Object[] {};
            DragAndDrop.SetGenericData("BuildPlayerSceneTreeViewItem", new List<int>(args.draggedItemIDs));
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            DragAndDrop.StartDrag("BuildPlayerSceneTreeView");
        }

        protected override void KeyEvent()
        {
            if ((Event.current.keyCode == KeyCode.Delete || Event.current.keyCode == KeyCode.Backspace) &&
                (GetSelection().Count > 0))
            {
                RemoveSelection();
            }
        }

        protected override void ContextClicked()
        {
            if (GetSelection().Count > 0)
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Remove Selection"), false, RemoveSelection);
                menu.ShowAsContext();
            }
        }

        protected void RemoveSelection()
        {
            foreach (var nodeID in GetSelection())
            {
                rootItem.children.Remove(FindItem(nodeID, rootItem));
            }
            EditorBuildSettings.scenes = GetSceneList();
            Reload();
            Repaint();
        }

        public EditorBuildSettingsScene[] GetSceneList()
        {
            var sceneList = new EditorBuildSettingsScene[rootItem.children.Count];
            for (int index = 0; index < rootItem.children.Count; index++)
            {
                var sceneItem = rootItem.children[index] as BuildPlayerSceneTreeViewItem;
                sceneList[index] = new EditorBuildSettingsScene(sceneItem.fullName, sceneItem.active);
            }
            return sceneList;
        }
    }
}
