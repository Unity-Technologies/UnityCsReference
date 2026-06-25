// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class TrustWindowSection : VisualElement
    {
        public TrustWindowSection(IResourceLoader resourceLoader, ActiveTrustWindow.SectionData sectionData)
        {
            var root = resourceLoader.GetTemplate("TrustWindowSection.uxml");
            cache = new VisualElementCache(root);

            stateIcon.AddToClassList(sectionData.icon.ClassName());
            stateLabel.text = sectionData.headerText;

            var rows = sectionData.rows;
            packageMultiColumnListView.reorderable = false;
            packageMultiColumnListView.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
            packageMultiColumnListView.selectionType = SelectionType.None;
            packageMultiColumnListView.itemsSource = (IList)rows;
            packageMultiColumnListView.fixedItemHeight = 20;
            packageMultiColumnListView.columns.reorderable = false;
            packageMultiColumnListView.columns.resizable = false;

            var displayNameColumn = packageMultiColumnListView.columns["displayName"];
            displayNameColumn.bindCell = (label, i) => ((Label)label).text = rows[i].displayName;
            displayNameColumn.stretchable = true; // We can't use flex-grow for this element so we must set it here

            var technicalNameColumn = packageMultiColumnListView.columns["technicalName"];
            if (sectionData.hasTechnicalName)
                technicalNameColumn.bindCell = (label, i) => ((Label)label).text = rows[i].technicalName;
            else
                packageMultiColumnListView.columns.Remove(technicalNameColumn);

            var signerNameColumn = packageMultiColumnListView.columns["orgName"];
            signerNameColumn.bindCell = (label, i) => ((Label)label).text = rows[i].signerName;
            if (!sectionData.isOrgKnown)
                signerNameColumn.title = L10n.Tr("Author");

            packageMultiColumnListView.columns["source"].bindCell = (label, i) => ((Label)label).text = rows[i].source;
            packageMultiColumnListView.columns["version"].bindCell = (e, i) => BindVersionCell(rows[i], e as Label);

            Add(root);
        }

        private void BindVersionCell(ActiveTrustWindow.RowData row, Label label)
        {
            PackageSimpleTagLabel packageTag = null;
            switch (row.versionTag)
            {
                case PackageTag.Experimental:
                    packageTag = new PackageSimpleTagLabel(PackageTag.Experimental, L10n.Tr("Exp"));
                    packageTag.AddToClassList("Experimental");
                    break;
                case PackageTag.PreRelease:
                    packageTag = new PackageSimpleTagLabel(PackageTag.PreRelease, L10n.Tr("Pre"));
                    packageTag.AddToClassList("PreRelease");
                    break;
            }

            var showTagInsteadOfLabel = packageTag != null;
            if (showTagInsteadOfLabel)
                label.parent.Add(packageTag);
            else
                label.text = row.version;
            UIUtils.SetElementDisplay(label, !showTagInsteadOfLabel);
        }

        private VisualElementCache cache { get; set; }
        private MultiColumnListView packageMultiColumnListView => cache.Get<MultiColumnListView>("packageMultiColumnListView");
        private VisualElement stateIcon => cache.Get<VisualElement>("stateIcon");
        private Label stateLabel => cache.Get<Label>("stateLabel");
    }
}
