// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

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

        protected override void RefreshPreview()
        {
            var openUSS = GetTargetUss();
            SetText(openUSS != null ? openUSS.ussPreview : string.Empty);
        }

        protected override void RefreshHeader()
        {
            var styleSheet = GetTargetStylesheet();
            if (styleSheet != null)
            {
                SetTargetAsset(styleSheet, document.hasUnsavedChanges);
            }
            else
            {
                SetTargetAsset(null, false);
            }
        }

        StyleSheet GetTargetStylesheet()
        {
            if (hasDocument)
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

                return styleSheet;
            }

            return null;
        }

        BuilderDocumentOpenUSS GetTargetUss()
        {
            var targetStylesheet = GetTargetStylesheet();
            if (targetStylesheet == null || document.activeOpenUXMLFile == null)
            {
                return null;
            }

            return document.activeOpenUXMLFile.GetUssFileFromSheet(targetStylesheet);
        }

        public void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
            RefreshHeader();
        }

        public void SelectionChanged()
        {
            RefreshHeader();
            RefreshPreviewIfVisible();
        }

        public void StylingChanged(List<string> styles, BuilderStylingChangeType changeType)
        {
            var selectionIsStyles = m_Selection.selectionType is BuilderSelectionType.StyleSheet
                or BuilderSelectionType.StyleSelector or BuilderSelectionType.ParentStyleSelector or BuilderSelectionType.Nothing; // Nothing has to be included for delete operations
            var inlineStyleChange = styles != null && !selectionIsStyles;

            if (changeType == BuilderStylingChangeType.Default)
            {
                if (!inlineStyleChange)
                    hasUnsavedChanges = document.hasUnsavedChanges;

                foreach (var openUSSFile in document.openUSSFiles)
                    openUSSFile.GeneratePreview();

                var parentDoc = document.activeOpenUXMLFile.openSubDocumentParent;
                while (parentDoc != null)
                {
                    foreach (var openUSSFile in parentDoc.openUSSFiles)
                        openUSSFile.GeneratePreview();
                    parentDoc = parentDoc.openSubDocumentParent;
                }

                RefreshPreviewIfVisible();
            }
        }

        protected override string previewAssetExtension => BuilderConstants.UssExtension;
    }
}
