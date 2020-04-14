// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageDependencySampleItemLowWidth : VisualElement
    {
        private VisualElementCache cache { get; set; }

        private ResourceLoader m_ResourceLoader;

        public void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
        }

        public PackageDependencySampleItemLowWidth(string name, string version, Label installStatus)
        {
            ResolveDependencies();
            var root = m_ResourceLoader.GetTemplate("PackageDependencySampleItemLowWidth.uxml");
            Add(root);

            cache = new VisualElementCache(root);

            itemName.text = name;
            itemName.tooltip = name;

            itemSizeOrVersion.value = version;
            itemSizeOrVersion.tooltip = version;
            itemSizeOrVersion.isReadOnly = true;

            if (installStatus != null && !string.IsNullOrEmpty(installStatus.text))
                item.Add(installStatus);
        }

        public PackageDependencySampleItemLowWidth(IPackageVersion version, Sample sample)
        {
            ResolveDependencies();
            var root = m_ResourceLoader.GetTemplate("PackageDependencySampleItemLowWidth.uxml");
            Add(root);

            cache = new VisualElementCache(root);

            var sampleItem  = new PackageSampleItem(version, sample);
            sampleItem.importButton.SetEnabled(version.isInstalled);

            var name = sampleItem.nameLabel.text;
            var size = sampleItem.sizeLabel.text;

            itemName.text = name;
            itemName.tooltip = name;

            itemStatusNameContainer.Add(sampleItem.importStatus);

            itemSizeOrVersion.value = size;
            itemSizeOrVersion.tooltip = size;
            itemSizeOrVersion.isReadOnly = true;

            item.Add(sampleItem.importButton);
        }

        private VisualElement itemStatusNameContainer { get { return cache.Get<VisualElement>("itemStatusNameContainer"); } }
        private Label itemName { get { return cache.Get<Label>("itemName"); } }
        private TextField itemSizeOrVersion { get { return cache.Get<TextField>("itemSizeOrVersion"); } }
        private VisualElement item { get { return cache.Get<VisualElement>("dependencySampleItemLowWidth"); } }
    }
}
