// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageSampleList : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageSampleList> {}

        private readonly VisualElement root;

        public PackageSampleList()
        {
            root = Resources.GetTemplate("PackageSampleList.uxml");
            Add(root);
            Cache = new VisualElementCache(root);
        }

        public void SetPackage(PackageInfo package)
        {
            ImportStatusContainer.Clear();
            NameLabelContainer.Clear();
            SizeLabelContainer.Clear();
            ImportButtonContainer.Clear();

            if (package == null || package.Samples == null || package.Samples.Count == 0)
            {
                UIUtils.SetElementDisplay(this, false);
                return;
            }
            UIUtils.SetElementDisplay(this, true);
            foreach (var sample in package.Samples)
            {
                var sampleItem = new PackageSampleItem(sample);
                ImportStatusContainer.Add(sampleItem.ImportStatus);
                NameLabelContainer.Add(sampleItem.NameLabel);
                SizeLabelContainer.Add(sampleItem.SizeLabel);
                ImportButtonContainer.Add(sampleItem.ImportButton);
                sampleItem.ImportButton.SetEnabled(package.IsInstalled);
            }
        }

        private VisualElementCache Cache { get; set; }

        internal VisualElement ImportStatusContainer { get { return Cache.Get<VisualElement>("importStatusContainer"); } }
        internal VisualElement NameLabelContainer { get { return Cache.Get<VisualElement>("nameLabelContainer"); } }
        internal VisualElement SizeLabelContainer { get { return Cache.Get<VisualElement>("sizeLabelContainer"); } }
        internal VisualElement ImportButtonContainer { get { return Cache.Get<VisualElement>("importButtonContainer"); } }
    }
}
