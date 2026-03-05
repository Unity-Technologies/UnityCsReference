// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsSampleItem
    {
        private readonly IPackageVersion m_Version;
        private Sample m_Sample;

        public PackageDetailsSampleItem(IPackageVersion version, Sample sample, IApplicationProxy application, IIOProxy iOProxy)
        {
            m_Version = version;
            m_Sample = sample;
            nameLabel.text = sample.displayName;
            nameLabel.tooltip = sample.displayName; // add tooltip for when the label text is cut off
            sizeLabel.text = UIUtils.ConvertToHumanReadableSize(sample.sizeInBytes);
            descriptionLabel.text = sample.description;
            RefreshActionButtons();
            var importSampleAction = new ImportSampleAction(application, iOProxy);
            var locateSampleAction = new LocateSampleAction(application, iOProxy);
            importSampleAction.onActionTriggered += RefreshActionButtons;
            importButton.clickable.clicked += () => importSampleAction.TriggerAction(m_Sample);
            locateButton.clickable.clicked += () => locateSampleAction.TriggerAction(m_Sample);
        }

        private void RefreshActionButtons()
        {
            UIUtils.SetElementDisplay(locateButton, true);
            if (m_Sample.isImported)
            {
                importStatus.AddToClassList("imported");
                importButton.text = L10n.Tr("Reimport");
            }
            else if (m_Sample.previousImportPaths?.Count > 0)
            {
                importStatus.AddToClassList("imported");
                importButton.text = L10n.Tr("Update");
            }
            else
            {
                UIUtils.SetElementDisplay(locateButton, false);
                importStatus.RemoveFromClassList("imported");
                importButton.text = L10n.Tr("Import");
            }
        }

        private Label m_ImportStatus;
        public Label importStatus => m_ImportStatus ??= new Label { classList = { "importStatus" } };
        private Label m_NameLabel;
        public Label nameLabel => m_NameLabel ??= new Label { classList = { "nameLabel" } };
        private Label m_SizeLabel;
        public Label sizeLabel => m_SizeLabel ??= new Label { classList = { "sizeLabel" } };
        private SelectableLabel m_DescriptionLabel;
        public SelectableLabel descriptionLabel => m_DescriptionLabel ??= new SelectableLabel { classList = { "descriptionLabel" } };
        private Button m_ImportButton;
        public Button importButton => m_ImportButton ??= new Button { classList = { "actionButton" } };
        private Button m_LocateButton;
        public Button locateButton => m_LocateButton ??= new Button { text = L10n.Tr("Locate"), classList = { "actionButton" } };
    }
}
