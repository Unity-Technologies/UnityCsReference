// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    static class BuilderLibraryUtility
    {
        public static bool InsertElementToDocument(BuilderDocument document, BuilderSelection selection, BuilderLibraryTreeItem libraryItem, VisualElement parentElement,
            int index = -1, VisualElement newElement = null, string name = null)
        {
            // We should have an item reference here if the OnDragStart() worked.
            var item = libraryItem;

            if (item != null)
            {
                var itemVTA = item.sourceAsset;

                if (document.WillCauseCircularDependency(itemVTA))
                {
                    BuilderDialogsUtility.DisplayDialog(BuilderConstants.InvalidWouldCauseCircularDependencyMessage,
                        BuilderConstants.InvalidWouldCauseCircularDependencyMessageDescription,
                        BuilderConstants.DialogOkOption);
                    return false;
                }

                newElement = item.makeVisualElementCallback?.Invoke();

                if (item.makeElementAssetCallback != null && newElement is TemplateContainer tempContainer)
                {
                    if (!BuilderAssetUtilities.ValidateAsset(item.sourceAsset, item.sourceAssetPath))
                        return false;
                }
            }

            if (newElement == null)
                return false;

            if (!string.IsNullOrEmpty(name))
                newElement.name = name;

            if (index < 0)
                parentElement.Add(newElement);
            else
                parentElement.Insert(index, newElement);

            // Create equivalent VisualElementAsset.
            if (item?.makeElementAssetCallback == null)
                BuilderAssetUtilities.AddElementToAsset(document.visualTreeAsset, newElement, index);
            else
                BuilderAssetUtilities.AddElementToAsset(document.visualTreeAsset, newElement, item.makeElementAssetCallback, index);

            selection.NotifyOfHierarchyChange(null);
            selection.NotifyOfStylingChange(null, null, BuilderStylingChangeType.RefreshOnly);
            selection.Select(null, newElement);
            return true;
        }
    }
}
