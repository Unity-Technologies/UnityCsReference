// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor.ShortcutManagement;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal class BuilderHierarchy : BuilderExplorer, IBuilderSelectionNotifier
    {
        public BuilderHierarchy(
            BuilderPaneWindow paneWindow,
            BuilderViewport viewport,
            BuilderSelection selection,
            BuilderClassDragger classDragger,
            BuilderHierarchyDragger hierarchyDragger,
            BuilderElementContextMenu contextMenuManipulator,
            HighlightOverlayPainter highlightOverlayPainter)
            : base(
                  paneWindow,
                  viewport,
                  selection,
                  classDragger,
                  hierarchyDragger,
                  contextMenuManipulator,
                  viewport.documentRootElement,
                  true,
                  highlightOverlayPainter,
                  null,
                  "Hierarchy")
        {
            viewDataKey = "builder-hierarchy";

            m_ElementHierarchyView.RegisterCallback<FocusEvent>(e => ShortcutIntegration.instance.contextManager.RegisterToolContext(m_Viewport), useTrickleDown: TrickleDown.TrickleDown);
            m_ElementHierarchyView.RegisterCallback<BlurEvent>(e => ShortcutIntegration.instance.contextManager.DeregisterToolContext(m_Viewport), useTrickleDown: TrickleDown.TrickleDown);
            m_ElementHierarchyView.RegisterCallback<DetachFromPanelEvent>(e => ShortcutIntegration.instance.contextManager.DeregisterToolContext(m_Viewport));
        }

        public override void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
            base.HierarchyChanged(element, changeType);

            if ((changeType & (BuilderHierarchyChangeType.ElementName | BuilderHierarchyChangeType.ClassList)) != 0)
            {
                // Look for the item associated with the element renamed. Note that the edited element may not be selected
                // yet.
                var item = m_ElementHierarchyView.FindElement(m_ElementHierarchyView.treeRootItems, element);
                var index = m_ElementHierarchyView.treeView.viewController.GetIndexForId(item.id);
                if (index == -1)
                {
                    return;
                }

                m_ElementHierarchyView.treeView.RefreshItem(index);
            }
        }

        protected override bool IsSelectedItemValid(VisualElement element)
        {
            var isVEA = element.GetVisualElementAsset() != null;
            var isVTA = element.GetVisualTreeAsset() != null;

            return isVEA || isVTA;
        }

        protected override void InitEllipsisMenu()
        {
            base.InitEllipsisMenu();

            if (pane == null)
                return;

            pane.AppendActionToEllipsisMenu("Type",
                a => ChangeVisibilityState(BuilderElementInfoVisibilityState.TypeName),
                a => m_ElementInfoVisibilityState
                .HasFlag(BuilderElementInfoVisibilityState.TypeName)
                ? DropdownMenuAction.Status.Checked
                : DropdownMenuAction.Status.Normal);

            pane.AppendActionToEllipsisMenu("Class List",
                a => ChangeVisibilityState(BuilderElementInfoVisibilityState.ClassList),
                a => m_ElementInfoVisibilityState
                .HasFlag(BuilderElementInfoVisibilityState.ClassList)
                ? DropdownMenuAction.Status.Checked
                : DropdownMenuAction.Status.Normal);

            pane.AppendActionToEllipsisMenu("Attached StyleSheets",
                a => ChangeVisibilityState(BuilderElementInfoVisibilityState.StyleSheets),
                a => m_ElementInfoVisibilityState
                .HasFlag(BuilderElementInfoVisibilityState.StyleSheets)
                ? DropdownMenuAction.Status.Checked
                : DropdownMenuAction.Status.Normal);
        }
    }
}
