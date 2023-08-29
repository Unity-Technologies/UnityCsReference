// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageSampleItemLowWidth : VisualElement
    {
        private VisualElementCache cache { get; set; }

        public PackageSampleItemLowWidth(IResourceLoader resourceLoader, IPackageVersion version, Sample sample, ISelectionProxy selection, IAssetDatabaseProxy assetDatabase, IApplicationProxy application, IIOProxy iOProxy)
        {
            var root = resourceLoader.GetTemplate("PackageSampleItemLowWidth.uxml");
            Add(root);

            cache = new VisualElementCache(root);

            var sampleItem  = new PackageDetailsSampleItem(version, sample, selection, assetDatabase, application, iOProxy);
            sampleItem.importButton.SetEnabled(version.isInstalled);

            var name = sampleItem.nameLabel.text;
            var size = sampleItem.sizeLabel.text;
            var description = sampleItem.descriptionLabel.text;

            itemName.text = name;
            itemName.tooltip = name;

            sampleStatus.Add(sampleItem.importStatus);

            itemSizeOrVersion.value = size;
            itemSizeOrVersion.tooltip = size;

            importButtonContainer.Add(sampleItem.importButton);

            if (!string.IsNullOrEmpty(description))
            {
                UIUtils.SetElementDisplay(sampleDescription, true);
                sampleDescription.SetValueWithoutNotify(description);
            }
            else
            {
                UIUtils.SetElementDisplay(sampleDescription, false);
            }
        }

        private VisualElement itemStatusNameContainer { get { return cache.Get<VisualElement>("itemStatusNameContainer"); } }
        private VisualElement sampleStatus { get { return cache.Get<VisualElement>("sampleStatus"); } }
        private Label itemName { get { return cache.Get<Label>("itemName"); } }
        private SelectableLabel itemSizeOrVersion { get { return cache.Get<SelectableLabel>("itemSizeOrVersion"); } }
        private VisualElement item { get { return cache.Get<VisualElement>("dependencySampleItemLowWidth"); } }
        private VisualElement importButtonContainer { get { return cache.Get<VisualElement>("importButtonContainer"); } }
        private SelectableLabel sampleDescription { get { return cache.Get<SelectableLabel>("sampleDescription"); } }
    }
}
