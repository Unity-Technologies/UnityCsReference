// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal class PackageSampleList : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageSampleList> {}

        private ResourceLoader m_ResourceLoader;
        private PackageManagerPrefs m_PackageManagerPrefs;
        private PackageDatabase m_PackageDatabase;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_PackageManagerPrefs = container.Resolve<PackageManagerPrefs>();
            m_PackageDatabase = container.Resolve<PackageDatabase>();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            float newWidth = evt.newRect.width;
            ToggleLowWidthDependencyView(newWidth);
        }

        private void ToggleLowWidthDependencyView(float width)
        {
            if (width <= 420)
            {
                UIUtils.SetElementDisplay(samplesListLowWidth, true);
                UIUtils.SetElementDisplay(samplesContainer, false);
            }
            else
            {
                UIUtils.SetElementDisplay(samplesListLowWidth, false);
                UIUtils.SetElementDisplay(samplesContainer, true);
            }
        }

        public PackageSampleList()
        {
            ResolveDependencies();

            var root = m_ResourceLoader.GetTemplate("PackageSampleList.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            SetExpanded(m_PackageManagerPrefs.samplesExpanded);
            samplesExpander.RegisterValueChangedCallback(evt => SetExpanded(evt.newValue));
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        private void SetExpanded(bool expanded)
        {
            if (samplesExpander.value != expanded)
                samplesExpander.value = expanded;
            if (m_PackageManagerPrefs.samplesExpanded != expanded)
                m_PackageManagerPrefs.samplesExpanded = expanded;
            UIUtils.SetElementDisplay(samplesOuterContainer, expanded);
        }

        public void SetPackageVersion(IPackageVersion version)
        {
            importStatusContainer.Clear();
            nameAndSizeLabelContainer.Clear();
            importButtonContainer.Clear();
            samplesListLowWidth.Clear();

            var samples = m_PackageDatabase.GetSamples(version);
            if (samples?.Any() != true)
            {
                UIUtils.SetElementDisplay(this, false);
                return;
            }
            UIUtils.SetElementDisplay(this, true);
            foreach (var sample in samples)
            {
                var sampleItem = new PackageSampleItem(version, sample);
                importStatusContainer.Add(sampleItem.importStatus);
                nameAndSizeLabelContainer.Add(sampleItem.nameLabel);
                nameAndSizeLabelContainer.Add(sampleItem.sizeLabel);
                importButtonContainer.Add(sampleItem.importButton);
                sampleItem.importButton.SetEnabled(version.isInstalled);

                var sampleVisualItemLowWidth = new PackageDependencySampleItemLowWidth(version, sample);
                samplesListLowWidth.Add(sampleVisualItemLowWidth);
            }
        }

        private VisualElementCache cache { get; set; }
        private Toggle samplesExpander { get { return cache.Get<Toggle>("samplesExpander"); } }
        internal VisualElement samplesListLowWidth { get { return cache.Get<VisualElement>("samplesListLowWidth"); } }
        private VisualElement samplesOuterContainer { get { return cache.Get<VisualElement>("samplesOuterContainer"); } }
        internal VisualElement samplesContainer { get { return cache.Get<VisualElement>("samplesContainer"); } }
        internal VisualElement importStatusContainer { get { return cache.Get<VisualElement>("importStatusContainer"); } }
        internal VisualElement nameAndSizeLabelContainer { get { return cache.Get<VisualElement>("nameAndSizeLabelContainer"); } }
        internal VisualElement importButtonContainer { get { return cache.Get<VisualElement>("importButtonContainer"); } }
    }
}
