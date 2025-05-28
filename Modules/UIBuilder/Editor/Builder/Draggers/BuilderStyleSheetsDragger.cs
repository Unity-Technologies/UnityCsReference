// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderStyleSheetsDragger : BuilderExplorerDragger
    {
        public BuilderStyleSheetsDragger(
            BuilderPaneWindow paneWindow,
            VisualElement root, BuilderSelection selection)
            : base(paneWindow, root, selection)
        {
        }

        protected override bool ExplorerCanStartDrag(VisualElement targetElement)
        {
            bool readyForDrag = targetElement.IsSelector() && !targetElement.IsParentSelector();
            if (readyForDrag)
                return true;

            if (!targetElement.IsStyleSheet() || targetElement.IsParentSelector())
                return false;
            if (!paneWindow.document.activeOpenUXMLFile.isChildSubDocument)
                return true;
            if (paneWindow.document.openUSSFiles.Count == 0)
                return false;

            var styleSheet = targetElement.GetStyleSheet();
            foreach (var openUSSFile in paneWindow.document.openUSSFiles)
            {
                if (openUSSFile.styleSheet == styleSheet)
                {
                    return true;
                }
            }
            return false;
        }

        protected override string ExplorerGetDraggedPillText(VisualElement targetElement)
        {
            return targetElement.IsSelector()
                ? StyleSheetToUss.ToUssSelector(targetElement.GetStyleComplexSelector())
                : targetElement.GetStyleSheet().name + BuilderConstants.UssExtension;
        }

        protected override void PerformAction(VisualElement destination, DestinationPane pane, Vector2 localMousePosition, int index = -1)
        {
            base.PerformAction(destination, pane, localMousePosition, index);

            if (m_TargetElementToReparent.IsSelector())
                PerformActionForSelector(destination, pane, index);
            else if (m_TargetElementToReparent.IsStyleSheet())
                PerformActionForStyleSheet(destination, pane, index);
        }

        void PerformActionForSelector(VisualElement destination, DestinationPane pane, int index = -1)
        {
            var newStyleSheetElement = destination;

            bool undo = true;

            foreach (var elementToReparent in m_ElementsToReparent)
            {
                var selectorElementToReparent = elementToReparent.element;
                var oldStyleSheetElement = elementToReparent.oldParent;

                if (newStyleSheetElement == oldStyleSheetElement)
                    continue;

                BuilderSharedStyles.MoveSelectorBetweenStyleSheets(
                    oldStyleSheetElement, newStyleSheetElement, selectorElementToReparent, undo);

                paneWindow.commandHandler.UpdateStyleSheetUssPreview(oldStyleSheetElement.GetStyleSheet());
                paneWindow.commandHandler.UpdateStyleSheetUssPreview(newStyleSheetElement.GetStyleSheet());

                undo = false;
            }

            BuilderSharedStyles.MatchSelectorElementOrderInAsset(newStyleSheetElement, undo);

            selection.NotifyOfHierarchyChange();
            selection.NotifyOfStylingChange(null);
            selection.ForceReselection();
        }

        void PerformActionForStyleSheet(VisualElement destination, DestinationPane pane, int index = -1)
        {
            if (destination == null)
                destination = BuilderSharedStyles.GetSelectorContainerElement(selection.documentRootElement);

            BuilderAssetUtilities.ReorderStyleSheetsInAsset(paneWindow.document, destination);

            selection.NotifyOfHierarchyChange();
            selection.NotifyOfStylingChange(null);
            selection.ForceReselection();
        }

        protected override bool IsPickedElementValid(VisualElement element)
        {
            if (element == null)
                return true;

            var newParent = element;
            foreach (var elementToReparent in m_ElementsToReparent)
                if (newParent == elementToReparent.element)
                    return false;

            if (!element.IsStyleSheet()) // Can only parent selectors under a StyleSheet.
                return false;

            // Check if USS is part of active document.
            if (!string.IsNullOrEmpty(element.GetProperty(BuilderConstants.ExplorerItemLinkedUXMLFileName) as string))
                return false;

            return true;
        }

        protected override bool SupportsDragBetweenElements(VisualElement element)
        {
            if (element == null)
                return false;

            var newParent = element;
            foreach (var elementToReparent in m_ElementsToReparent)
            {
                var toReparent = elementToReparent.element;

                if (newParent == toReparent)
                    return false;

                if (element.IsParentSelector() || toReparent.IsParentSelector())
                    return false;

                if (element.IsSelector() && !toReparent.IsSelector())
                    return false;

                if (element.IsStyleSheet() && toReparent.IsSelector())
                    return false;

                // Check if USS is part of active document.
                if (element.IsStyleSheet() && toReparent.IsStyleSheet() && !string.IsNullOrEmpty(element.GetProperty(BuilderConstants.ExplorerItemLinkedUXMLFileName) as string))
                    return false;

                if (element.IsPartOfCurrentDocument())
                    return false;

                if (element.HasAncestor(builderHierarchyRoot))
                    return false;
            }

            return true;
        }

        protected override bool SupportsDragInEmptySpace(VisualElement element)
        {
            if (paneWindow.document.activeOpenUXMLFile.openUSSFiles.Count != BuilderSharedStyles.GetSelectorContainerElement(selection.documentRootElement).childCount)
                return false;

            return element != null && element.HasAncestor(builderStylesheetRoot);
        }

        protected override bool SupportsPlacementIndicator()
        {
            return false;
        }

        protected override VisualElement GetDefaultTargetElement()
        {
            if (m_TargetElementToReparent.IsSelector())
                return BuilderSharedStyles.GetSelectorContainerElement(paneWindow.rootVisualElement).Children().Last();

            if (m_TargetElementToReparent.IsStyleSheet())
                return BuilderSharedStyles.GetSelectorContainerElement(paneWindow.rootVisualElement);

            return null;
        }
    }
}
