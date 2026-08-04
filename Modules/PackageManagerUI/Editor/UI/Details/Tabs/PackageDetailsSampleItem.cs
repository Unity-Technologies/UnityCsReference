// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsSampleItem
    {
        private const string k_ImportedStatusClass = "imported";
        private Sample m_Sample;

        public PackageDetailsSampleItem(Sample sample, IApplicationProxy application, IIOProxy iOProxy, ISampleImporter sampleImporter)
        {
            m_Sample = sample;
            nameLabel.text = sample.displayName;
            nameLabel.tooltip = sample.displayName; // add tooltip for when the label text is cut off
            sizeLabel.text = UIUtils.ConvertToHumanReadableSize(sample.sizeInBytes);
            descriptionLabel.text = sample.description;

            var importSampleAction = new ImportSampleAction(application, iOProxy, sampleImporter);
            var locateSampleAction = new LocateSampleAction(application, iOProxy);
            m_ImportButton = new SampleToolBarSimpleButton(importSampleAction);
            m_LocateButton = new SampleToolBarSimpleButton(locateSampleAction);
            // We should refresh all buttons when the Import button is clicked in case no code was modified
            // and domain reload is not triggered assuring a refresh of visibility and text either way.
            importSampleAction.onActionTriggered += OnActionTriggered;
            RefreshActionButtonsAndStatus();
        }

        private void OnActionTriggered()
        {
            RefreshActionButtonsAndStatus();
        }

        private void RefreshActionButtonsAndStatus()
        {
            m_ImportButton.Refresh(m_Sample);
            m_LocateButton.Refresh(m_Sample);

            if (m_Sample.isImported || m_Sample.previousImportPaths?.Count > 0)
                importStatus.AddToClassList(k_ImportedStatusClass);
            else
                importStatus.RemoveFromClassList(k_ImportedStatusClass);
        }

        private Label m_ImportStatus;
        public Label importStatus => m_ImportStatus ??= new Label().WithClassList("importStatus");
        private Label m_NameLabel;
        public Label nameLabel => m_NameLabel ??= new Label().WithClassList("nameLabel");
        private Label m_SizeLabel;
        public Label sizeLabel => m_SizeLabel ??= new Label().WithClassList("sizeLabel");
        private SelectableLabel m_DescriptionLabel;
        public SelectableLabel descriptionLabel => m_DescriptionLabel ??= new SelectableLabel().WithClassList("descriptionLabel");
        private SampleToolBarSimpleButton m_ImportButton;
        public SampleToolBarSimpleButton importButton => m_ImportButton;
        private SampleToolBarSimpleButton m_LocateButton;
        public SampleToolBarSimpleButton locateButton => m_LocateButton;
    }
}
