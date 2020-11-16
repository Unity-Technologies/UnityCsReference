using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEngine;

namespace Unity.UI.Builder
{
    internal class BuilderHierarchy : BuilderExplorer, IBuilderSelectionNotifier
    {
        [SerializeField] BuilderElementInfoVisibilityState m_ElementInfoVisibilityState;

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
                  null)
        {
            viewDataKey = "builder-hierarchy";
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

        void ChangeVisibilityState(BuilderElementInfoVisibilityState state)
        {
            m_ElementInfoVisibilityState ^= state;
            m_ElementHierarchyView.elementInfoVisibilityState = m_ElementInfoVisibilityState;
            SaveViewData();
            UpdateHierarchyAndSelection(m_ElementHierarchyView.hasUnsavedChanges);
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();
            OverwriteFromViewData(this, viewDataKey);
            m_ElementHierarchyView.elementInfoVisibilityState = m_ElementInfoVisibilityState;
        }
    }
}
