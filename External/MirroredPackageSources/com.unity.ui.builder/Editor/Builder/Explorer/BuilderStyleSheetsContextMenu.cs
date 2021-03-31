using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderStyleSheetsContextMenu : BuilderElementContextMenu
    {
        public BuilderStyleSheetsContextMenu(BuilderPaneWindow paneWindow, BuilderSelection selection)
            : base(paneWindow, selection)
        {}

        public override void BuildElementContextualMenu(ContextualMenuPopulateEvent evt, VisualElement target)
        {
            base.BuildElementContextualMenu(evt, target);

            var documentElement = target.GetProperty(BuilderConstants.ElementLinkedDocumentVisualElementVEPropertyName) as VisualElement;

            var selectedStyleSheet = documentElement?.GetStyleSheet();
            int selectedStyleSheetIndex = selectedStyleSheet == null ? -1 : (int)documentElement.GetProperty(BuilderConstants.ElementLinkedStyleSheetIndexVEPropertyName);
            var isStyleSheet = documentElement != null && BuilderSharedStyles.IsStyleSheetElement(documentElement);
            var styleSheetBelongsToParent = !string.IsNullOrEmpty(documentElement?.GetProperty(BuilderConstants.ExplorerItemLinkedUXMLFileName) as string);
            if (isStyleSheet)
                evt.StopImmediatePropagation();

            evt.menu.AppendSeparator();

            evt.menu.AppendAction(
                BuilderConstants.ExplorerStyleSheetsPaneCreateNewUSSMenu,
                a =>
                {
                    BuilderStyleSheetsUtilities.CreateNewUSSAsset(paneWindow);
                },
                DropdownMenuAction.Status.Normal);

            evt.menu.AppendAction(
                BuilderConstants.ExplorerStyleSheetsPaneAddExistingUSSMenu,
                a =>
                {
                    BuilderStyleSheetsUtilities.AddExistingUSSToAsset(paneWindow);
                },
                DropdownMenuAction.Status.Normal);

            evt.menu.AppendAction(
                BuilderConstants.ExplorerStyleSheetsPaneRemoveUSSMenu,
                a =>
                {
                    BuilderStyleSheetsUtilities.RemoveUSSFromAsset(paneWindow, selection, documentElement);
                },
                isStyleSheet && !styleSheetBelongsToParent
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendSeparator();

            evt.menu.AppendAction(
                BuilderConstants.ExplorerStyleSheetsPaneSetActiveUSS,
                a =>
                {
                    selection.Select(null, documentElement);
                    BuilderStyleSheetsUtilities.SetActiveUSS(selection, paneWindow, selectedStyleSheet);
                },
                isStyleSheet && !styleSheetBelongsToParent
                ? DropdownMenuAction.Status.Normal
                : DropdownMenuAction.Status.Disabled);
        }
    }
}
