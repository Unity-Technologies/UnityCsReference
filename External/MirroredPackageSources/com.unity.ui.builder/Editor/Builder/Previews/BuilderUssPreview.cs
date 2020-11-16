using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

namespace Unity.UI.Builder
{
    internal class BuilderUssPreview : BuilderCodePreview, IBuilderSelectionNotifier
    {
        BuilderSelection m_Selection;

        public BuilderUssPreview(BuilderPaneWindow paneWindow, BuilderSelection selection) : base(paneWindow)
        {
            m_Selection = selection;
        }

        protected override void OnAttachToPanelDefaultAction()
        {
            base.OnAttachToPanelDefaultAction();
            RefreshUSS();
        }

        void RefreshUSS()
        {
            if (hasDocument && document.firstStyleSheet != null)
            {
                var styleSheet = document.activeStyleSheet;

                var selectedElement = m_Selection.isEmpty ? null : m_Selection.selection.First();
                if (selectedElement != null)
                {
                    if (BuilderSharedStyles.IsStyleSheetElement(selectedElement))
                        styleSheet = selectedElement.GetStyleSheet();
                    else if (BuilderSharedStyles.IsSelectorElement(selectedElement))
                        styleSheet = selectedElement.GetClosestStyleSheet();
                }

                SetText(styleSheet.GenerateUSS());
                SetTargetAsset(styleSheet, document.hasUnsavedChanges);
            }
            else
            {
                SetText(string.Empty);
                SetTargetAsset(null, false);
            }
        }

        public void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
            RefreshUSS();
        }

        public void SelectionChanged()
        {
            RefreshUSS();
        }

        public void StylingChanged(List<string> styles, BuilderStylingChangeType changeType)
        {
            RefreshUSS();
        }

        protected override string previewAssetExtension => BuilderConstants.UssExtension;
    }
}
