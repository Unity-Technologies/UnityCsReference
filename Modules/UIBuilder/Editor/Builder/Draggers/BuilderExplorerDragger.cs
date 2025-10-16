// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderExplorerDragger : BuilderDragger
    {
        protected struct ElementToReparent
        {
            public VisualElement element;
            public VisualElement oldParent;
            public int oldIndex;
        }

        static protected readonly string s_DraggableStyleClassPillClassName = "unity-builder-class-pill--draggable";

        protected VisualElement m_DragPreviewLastParent;
        protected int m_DragPreviewLastIndex;

        protected VisualElement m_TargetElementToReparent;

        protected List<ElementToReparent> m_ElementsToReparent = new List<ElementToReparent>();

        public BuilderExplorerDragger(
            BuilderPaneWindow paneWindow,
            VisualElement root, BuilderSelection selection,
            BuilderViewport viewport = null, BuilderParentTracker parentTracker = null)
            : base(paneWindow, root, selection, viewport, parentTracker)
        {
        }

        protected virtual bool ExplorerCanStartDrag(VisualElement targetElement)
        {
            return true;
        }

        protected virtual string ExplorerGetDraggedPillText(VisualElement targetElement)
        {
            return targetElement.name;
        }

        protected virtual void ExplorerPerformDrag()
        {
        }

        protected virtual VisualElement ExplorerGetDragPreviewFromTarget(VisualElement target, Vector2 mousePosition)
        {
            var element = target.GetProperty(BuilderConstants.ExplorerItemElementLinkVEPropertyName) as VisualElement;
            if (element == null)
            {
                var explorerItem = target.GetFirstAncestorWithClass(BuilderConstants.ExplorerItemLabelContClassName).parent;
                element = explorerItem.GetProperty(BuilderConstants.ExplorerItemElementLinkVEPropertyName) as VisualElement;
            }

            return element;
        }

        protected virtual void ResetDragPreviewElement()
        {
            if (m_DragPreviewLastParent == null)
                return;

            m_DragPreviewLastParent = null;
        }

        protected virtual IEnumerable<VisualElement> GetSelectedElements()
        {
            return selection.selection;
        }

        protected override VisualElement CreateDraggedElement()
        {
            var pill = new BuilderClassPill();
            pill.AddToClassList(s_DraggableStyleClassPillClassName);
            return pill;
        }

        protected override void FillDragElement(VisualElement pill)
        {
            // We use the primary target element for our pill info.
            var pillLabel = pill.Q<Label>();
            pillLabel.text = ExplorerGetDraggedPillText(m_TargetElementToReparent);
            pillLabel.RemoveFromClassList(BuilderConstants.ElementClassNameClassName);
        }

        protected override bool PrepareDrag(VisualElement target, Vector2 mousePosition)
        {
            if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
            {
                return VerifyExternalDrag();
            }

            m_ElementsToReparent.Clear();
            m_TargetElementToReparent = null;

            if (!selection.isEmpty)
            {
                var selectedElements = GetSelectedElements();

                // Create list of elements to reparent.
                foreach (var selectedElement in selectedElements)
                {
                    if (!ExplorerCanStartDrag(selectedElement))
                        continue;

                    var elementToReparent = new ElementToReparent()
                    {
                        element = selectedElement,
                        oldParent = selectedElement.parent,
                        oldIndex = selectedElement.parent.IndexOf(selectedElement)
                    };

                    m_ElementsToReparent.Add(elementToReparent);

                    m_TargetElementToReparent ??= selectedElement;
                }
            }

            // We still need a primary element that is "being dragged" for visualization purposes.
            if (m_TargetElementToReparent == null)
            {
                m_TargetElementToReparent = ExplorerGetDragPreviewFromTarget(target, mousePosition);
            }
            if (m_TargetElementToReparent == null || !ExplorerCanStartDrag(m_TargetElementToReparent))
                return false;

            return true;
        }

        protected override void PerformDrag(VisualElement target, VisualElement pickedElement, int index = -1)
        {
            if (pickedElement == null)
            {
                FailAction(target);
                return;
            }

            if (pickedElement == m_DragPreviewLastParent && index == m_DragPreviewLastIndex)
                return;

            ResetDragPreviewElement();

            m_DragPreviewLastParent = pickedElement;
            m_DragPreviewLastIndex = index;

            FixElementSizeAndPosition(m_DragPreviewLastParent);

            ExplorerPerformDrag();
        }

        T ImportAndLoadAsset<T>(string path, out string relativePath) where T : Object
        {
            relativePath = BuilderAssetUtilities.ImportAssetFromOutsideProject(path);
            return string.IsNullOrEmpty(relativePath)
                ? null
                : AssetDatabase.LoadAssetAtPath<T>(relativePath);
        }

        bool PerformActionFromOutsideBuilder(VisualElement destination = null, int index = -1)
        {
            var paths = DragAndDrop.paths;
            if (paths.Length == 0) return false;

            var objectReferences = DragAndDrop.objectReferences;
            bool assetExistsInProject = paths.Length == objectReferences.Length;

            for (int i = 0; i < paths.Length; i++)
            {
                var path = paths[i];
                var reference = assetExistsInProject ? objectReferences[i] : null;

                if (path.EndsWith(BuilderConstants.UxmlExtension))
                {
                    string relativePath = path;
                    var vta = reference as VisualTreeAsset ?? ImportAndLoadAsset<VisualTreeAsset>(path, out relativePath);
                    if (vta ==null) continue;

                    var dst = BuilderSharedStyles.IsSelectorsContainerElement(destination)
                        ? paneWindow.document.primaryViewportWindow.documentRootElement
                        : destination;

                    if (!VerifyAndAddVisualTreeAsset(vta, relativePath, dst, index))
                        return false;
                }
                else if (path.EndsWith(BuilderConstants.UssExtension))
                {
                    var assetRelativePath = assetExistsInProject ? paths[i] : BuilderAssetUtilities.ImportAssetFromOutsideProject(path);

                    if (!AddStyleSheetToDocument(assetRelativePath, index))
                        return false;
                }
            }

            return true;
        }

        bool VerifyExternalDrag()
        {
            string ext = string.Empty;

            if (this is BuilderStyleSheetsDragger)
                ext = BuilderConstants.UssExtension;
            else if (this is BuilderHierarchyDragger)
                ext = BuilderConstants.UxmlExtension;

            List<string> listOfPaths = new List<string>();
            BuilderAssetUtilities.GetListOfPathsInDragAndDrop(listOfPaths);

            foreach (var path in listOfPaths)
            {
                if (path.EndsWith(ext))
                    return true;
            }

            return false;
        }
        private bool VerifyAndAddVisualTreeAsset(VisualTreeAsset visualTreeAsset, string path, VisualElement destination = null, int index = -1)
        {
            var isCurrentDocumentVisualTreeAsset = visualTreeAsset == paneWindow.document.visualTreeAsset;

            if (isCurrentDocumentVisualTreeAsset || paneWindow.document.WillCauseCircularDependency(visualTreeAsset))
            {
                BuilderDialogsUtility.DisplayDialog(BuilderConstants.InvalidWouldCauseCircularDependencyMessage,
                    BuilderConstants.InvalidWouldCauseCircularDependencyMessageDescription, BuilderConstants.DialogOkOption);
                return false;
            }
            AddVisualTreeAssetToDocument(visualTreeAsset, path, destination, index);
            return true;
        }

        private void AddVisualTreeAssetToDocument(VisualTreeAsset visualTreeAsset, string relativePath, VisualElement destination = null, int index =-1)
        {
            var elementAdded = BuilderAssetUtilities.AddTemplateContainerToAsset(paneWindow, visualTreeAsset, relativePath, destination, index);

            if (elementAdded == null)
                return;

            selection.NotifyOfHierarchyChange(null);
            selection.NotifyOfStylingChange(null);
            selection.Select(null, elementAdded);
        }

        protected override bool PerformAction(VisualElement destination, DestinationPane pane, Vector2 localMousePosition, int index = -1)
        {
            if (DragAndDrop.paths.Length > 0)
            {
                return PerformActionFromOutsideBuilder(destination, index);
            }

            Reparent(destination, index);
            return true;
        }

        void Reparent(VisualElement newParent, int index)
        {
            foreach (var elementToReparent in m_ElementsToReparent)
                index = ReparentIndividualElement(elementToReparent.element, newParent, index);
        }

        int ReparentIndividualElement(VisualElement element, VisualElement newParent, int index)
        {
            var elementToReparent = element;

            if (newParent == elementToReparent)
                return index;

            if (!BuilderAssetUtilities.IsSupportedChildType(newParent, element.GetType()))
                return index;

            var oldParent = elementToReparent.parent;
            if (oldParent != newParent)
            {
                if (index < 0 || index > newParent.childCount - 1)
                {
                    newParent.Insert(newParent.childCount, elementToReparent);
                    return newParent.childCount;
                }
                else
                {
                    newParent.Insert(index, elementToReparent);
                    return index + 1;
                }
            }

            if (index < 0)
                index = newParent.childCount;

            var oldIndex = oldParent.IndexOf(elementToReparent);
            if (oldIndex < index)
                index = index - 1;

            elementToReparent.RemoveFromHierarchy();
            newParent.Insert(index, elementToReparent);

            return index + 1;
        }

        protected override void EndDrag()
        {
            ResetDragPreviewElement();
            m_ElementsToReparent.Clear();
        }

        protected override void FailAction(VisualElement target)
        {
            if (m_DragPreviewLastParent == null)
                return;

            ResetDragPreviewElement();
        }

        private bool AddStyleSheetToDocument(string relativePath, int index = -1)
        {
            if (!relativePath.EndsWith(BuilderConstants.UssExtension))
                return false;

            var result = BuilderStyleSheetsUtilities.AddUSSToAsset(paneWindow, relativePath, index);

            // Only notify of changes if the stylesheet was actually added
            if (result)
            {
                selection.NotifyOfStylingChange(null);
                selection.ForceReselection(null);
            }

            return result;
        }

    }
}
