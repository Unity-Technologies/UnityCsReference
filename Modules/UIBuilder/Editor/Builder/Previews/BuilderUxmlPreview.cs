// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Unity.UI.Builder
{
    internal class BuilderUxmlPreview : BuilderCodePreview, IBuilderSelectionNotifier
    {
        public BuilderUxmlPreview(BuilderPaneWindow paneWindow) : base(paneWindow)
        {
        }

        string GenerateUXMLText()
        {
            bool writingToFile = true; // Set this to false to see the special selection elements and attributes.
            var uxmlText = document.visualTreeAsset.GenerateUXML(document.uxmlPath, writingToFile);
            return uxmlText;
        }

        protected override void RefreshPreview()
        {
            SetText(hasDocument ? GenerateUXMLText() : string.Empty);
        }

        protected override void RefreshHeader()
        {
            if (hasDocument)
            {
                SetTargetAsset(document.visualTreeAsset, document.hasUnsavedChanges);
            }
            else
            {
                SetTargetAsset(null, false);
            }
        }

        public void HierarchyChanged(VisualElement element, BuilderHierarchyChangeType changeType)
        {
            if ((changeType & (BuilderHierarchyChangeType.FullRefresh |
                    BuilderHierarchyChangeType.Attributes |
                    BuilderHierarchyChangeType.ElementName |
                    BuilderHierarchyChangeType.ClassList)) != 0)
            {
                RefreshPreviewIfVisible();
            }
        }

        public void SelectionChanged()
        {
            RefreshPreviewIfVisible();
        }

        public void StylingChanged(List<string> styles, BuilderStylingChangeType changeType)
        {
            RefreshHeader();
        }

        protected override string previewAssetExtension => BuilderConstants.UxmlExtension;
    }
}
