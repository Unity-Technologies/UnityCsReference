// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderClassDragger : BuilderDragger
    {
        static readonly string s_DraggableStyleClassPillClassName = "unity-builder-class-pill--draggable";

        string m_ClassNameBeingDragged;

        public BuilderClassDragger(
            BuilderPaneWindow paneWindow,
            VisualElement root, BuilderSelection selection,
            BuilderViewport viewport, BuilderParentTracker parentTracker)
            : base(paneWindow, root, selection, viewport, parentTracker)
        {
            exclusive = false;
        }

        protected override VisualElement CreateDraggedElement()
        {
            var classPillTemplate = BuilderPackageUtilities.LoadAssetAtPath<VisualTreeAsset>(
                BuilderConstants.UIBuilderPackagePath + "/BuilderClassPill.uxml");
            var pill = classPillTemplate.CloneTree();
            pill.AddToClassList(s_DraggableStyleClassPillClassName);
            return pill;
        }

        protected override void FillDragElement(VisualElement pill)
        {
            pill.Q<Label>().text = m_ClassNameBeingDragged;
        }

        protected override bool StartDrag(VisualElement target, Vector2 mousePosition, VisualElement pill)
        {
            m_ClassNameBeingDragged = target.GetProperty(BuilderConstants.ExplorerStyleClassPillClassNameVEPropertyName) as string;

            // if a ChildSubDocument is open, make sure that style class is part of active stylesheet, otherwise refuse drag
            if (!paneWindow.document.activeOpenUXMLFile.isChildSubDocument)
                return true;

            if (target.IsParentSelector())
                return false;

            foreach (var openUSSFile in paneWindow.document.openUSSFiles)
            {
                var currentStyleSheet = openUSSFile.styleSheet;
                if (currentStyleSheet.FindSelector(m_ClassNameBeingDragged) != null)
                {
                    return true;
                }
            }

            return false;
        }

        protected override void PerformAction(VisualElement destination, DestinationPane pane, Vector2 localMousePosition, int index = -1)
        {
            if (BuilderSharedStyles.IsDocumentElement(destination))
                return;

            var className = m_ClassNameBeingDragged.TrimStart('.');

            destination.AddToClassList(className);

            // Update VisualTreeAsset.
            BuilderAssetUtilities.AddStyleClassToElementInAsset(
                paneWindow.document, destination, className);

            selection.NotifyOfHierarchyChange(null);
            selection.NotifyOfStylingChange(null);
        }

        protected override bool IsPickedElementValid(VisualElement element)
        {
            if (element == null)
                return false;

            if (element.GetVisualElementAsset() == null)
                return false;

            if (!element.IsPartOfActiveVisualTreeAsset(paneWindow.document))
                return false;

            return true;
        }

        protected override bool SupportsDragInEmptySpace(VisualElement element)
        {
            return false;
        }

        protected override bool SupportsPlacementIndicator()
        {
            return false;
        }
    }
}
