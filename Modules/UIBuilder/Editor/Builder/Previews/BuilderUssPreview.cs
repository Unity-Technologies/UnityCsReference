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
            var targetStylesheet = GetTargetStylesheet();
            if (targetStylesheet == null || document.activeOpenUXMLFile == null)
            {
                SetText(string.Empty);
                return;
            }

            foreach (var openUssFile in document.activeOpenUXMLFile.openUSSFiles)
            {
                if (openUssFile.styleSheet != targetStylesheet)
                    continue;

                SetText(openUssFile.ussPreview);
                break;
            }
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

            foreach (var openUssFile in document.activeOpenUXMLFile.openUSSFiles)
            {
                if (openUssFile.styleSheet != targetStylesheet)
                    continue;
                return openUssFile;
            }

            return null;
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
            if (changeType == BuilderStylingChangeType.Default)
            {
                GetTargetUss()?.GeneratePreview();
                RefreshPreviewIfVisible();
            }
        }

        protected override string previewAssetExtension => BuilderConstants.UssExtension;
    }
}
