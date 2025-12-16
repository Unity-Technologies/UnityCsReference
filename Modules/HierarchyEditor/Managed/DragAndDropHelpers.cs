// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEngine;

namespace Unity.Hierarchy.Editor
{
    static class DragAndDropHelpers
    {
        public static DragVisualMode ConvertDragAndDropVisualModeToDragVisualMode(DragAndDropVisualMode visualMode)
        {
            switch (visualMode)
            {
                case DragAndDropVisualMode.None:
                    return DragVisualMode.None;
                case DragAndDropVisualMode.Copy:
                    return DragVisualMode.Copy;
                case DragAndDropVisualMode.Link:
                    return DragVisualMode.Copy;
                case DragAndDropVisualMode.Move:
                    return DragVisualMode.Move;
                case DragAndDropVisualMode.Generic:
                    return DragVisualMode.Copy;
                case DragAndDropVisualMode.Rejected:
                    return DragVisualMode.Rejected;
                default:
                    throw new ArgumentOutOfRangeException(nameof(visualMode), visualMode, null);
            }
        }

        public static bool IsDraggingScene(in HierarchyViewDragAndDropHandlingData data)
        {
            if (data.EntityIds != null)
            {
                var refCount = 0;
                var sceneAssetCount = 0;
                foreach (var id in data.EntityIds)
                {
                    ++refCount;
                    var obj = EditorUtility.EntityIdToObject(id);
                    if (obj is SceneAsset)
                        sceneAssetCount++;
                }
                if (refCount != 0)
                    return sceneAssetCount == refCount;
            }

            if (data.Paths?.Length > 0)
            {
                var scenePathCount = 0;
                foreach (var draggedPath in data.Paths)
                {
                    if (AssetDatabase.GetMainAssetTypeAtPath(draggedPath) == typeof(SceneAsset))
                        scenePathCount++;
                }
                return scenePathCount == data.Paths.Length;
            }

            return false;
        }

        public static bool IsDraggingEntity(HierarchyViewDragAndDropHandlingData data)
        {
            if (data.EntityIds != null)
            {
                foreach (var dragged in data.EntityIds)
                {
                    if (dragged != EntityId.None)
                    {
                        var obj = EditorUtility.EntityIdToObject(dragged);
                        if (obj == null)
                            return true;
                    }
                }
            }
            return false;
        }

        public static HierarchyDropFlags GetDefaultDropFlags(HierarchyView hierarchyView, in HierarchyNode droppedInParent, DragAndDropPosition dropPosition, bool searchActive, int insertAtIndex, int parentIndex, int targetIndex)
        {
            var option = searchActive ? HierarchyDropFlags.SearchActive : HierarchyDropFlags.None;
            if (dropPosition == DragAndDropPosition.OverItem)
            {
                option |= HierarchyDropFlags.DropUpon;
            }
            else
            {
                if (insertAtIndex == targetIndex)
                    option |= HierarchyDropFlags.DropAbove;
                else
                    option |= HierarchyDropFlags.DropBetween;
            }

            var insertAfterParent = insertAtIndex == parentIndex + 1 || (parentIndex == targetIndex && dropPosition != DragAndDropPosition.OverItem);
            if (droppedInParent != hierarchyView.Source.Root && insertAfterParent && hierarchyView.Source.GetChildrenCount(in droppedInParent) > 0 && hierarchyView.ViewModel.HasFlags(in droppedInParent, HierarchyNodeFlags.Expanded))
            {
                option |= HierarchyDropFlags.DropAfterParent;
            }

            return option;
        }
    }
}
