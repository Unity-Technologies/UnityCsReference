// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageSampleList : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageSampleList> {}

        private const int k_SampleListSwitchWidthBreakpoint = 420;
        private ResourceLoader m_ResourceLoader;
        private PackageManagerPrefs m_PackageManagerPrefs;
        private PackageDatabase m_PackageDatabase;
        private IEnumerable<Sample> m_Samples;
        private IPackageVersion m_Version;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_ResourceLoader = container.Resolve<ResourceLoader>();
            m_PackageManagerPrefs = container.Resolve<PackageManagerPrefs>();
            m_PackageDatabase = container.Resolve<PackageDatabase>();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (m_Version != null && m_Samples?.Count() > 0 == true)
            {
                float newWidth = evt.newRect.width;
                ToggleLowWidthSampleView(newWidth, false);
            }
        }

        private void ToggleLowWidthSampleView(float width, bool packageChanged)
        {
            var showLowWidth = width <= k_SampleListSwitchWidthBreakpoint;

            if (showLowWidth && (!samplesListLowWidth.visible || samplesContainer.visible || packageChanged))
                SwitchToSampleListLowWidth();
            else if (!showLowWidth && (!samplesContainer.visible || samplesListLowWidth.visible || packageChanged))
                SwitchToRegularSampleList();
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
            var samples = version.isInstalled ? m_PackageDatabase.GetSamples(version) : Enumerable.Empty<Sample>();
            if (samples?.Any() != true)
            {
                UIUtils.SetElementDisplay(this, false);
                return;
            }
            UIUtils.SetElementDisplay(this, true);

            var packageChanged = version.packageUniqueId != m_Version?.packageUniqueId;
            var width = rect.width;

            m_Version = version;
            m_Samples = samples;

            ToggleLowWidthSampleView(width, packageChanged);
        }

        private void SwitchToSampleListLowWidth()
        {
            UIUtils.SetElementDisplay(samplesListLowWidth, true);
            UIUtils.SetElementDisplay(samplesContainer, false);

            samplesListLowWidth.Clear();

            foreach (var sample in m_Samples)
            {
                var sampleVisualItemLowWidth = new PackageDependencySampleItemLowWidth(m_Version, sample);
                samplesListLowWidth.Add(sampleVisualItemLowWidth);
            }
        }

        private void SwitchToRegularSampleList()
        {
            UIUtils.SetElementDisplay(samplesListLowWidth, false);
            UIUtils.SetElementDisplay(samplesContainer, true);

            importStatusContainer.Clear();
            nameAndSizeLabelContainer.Clear();
            importButtonContainer.Clear();

            UIUtils.SetElementDisplay(samplesErrorInfoBox, m_Version.HasTag(PackageTag.InDevelopment) && m_Samples.Any(sample => string.IsNullOrEmpty(sample.displayName)));

            foreach (var sample in m_Samples.Where(s => !string.IsNullOrEmpty(s.displayName)))
            {
                var sampleItem = new PackageSampleItem(m_Version, sample);
                importStatusContainer.Add(sampleItem.importStatus);
                nameAndSizeLabelContainer.Add(sampleItem.nameLabel);
                nameAndSizeLabelContainer.Add(sampleItem.sizeLabel);
                importButtonContainer.Add(sampleItem.importButton);
                sampleItem.importButton.SetEnabled(m_Version.isInstalled);
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
        internal HelpBox samplesErrorInfoBox => cache.Get<HelpBox>("samplesErrorInfoBox");
    }
}
