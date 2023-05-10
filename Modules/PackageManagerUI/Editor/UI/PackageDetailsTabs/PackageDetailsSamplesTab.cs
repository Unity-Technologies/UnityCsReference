// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsSamplesTab : PackageDetailsTabElement
    {
        public const string k_Id = "samples";

        private const int k_SampleListSwitchWidthBreakpoint = 420;

        private IEnumerable<Sample> m_Samples;
        private IPackageVersion m_Version;

        private readonly ResourceLoader m_ResourceLoader;
        private readonly PackageDatabase m_PackageDatabase;
        private readonly SelectionProxy m_Selection;
        private readonly AssetDatabaseProxy m_AssetDatabase;
        private readonly ApplicationProxy m_Application;
        private readonly IOProxy m_IOProxy;
        public PackageDetailsSamplesTab(UnityConnectProxy unityConnect,
            ResourceLoader resourceLoader,
            PackageDatabase packageDatabase,
            SelectionProxy selection,
            AssetDatabaseProxy assetDatabase,
            ApplicationProxy application,
            IOProxy iOProxy) : base(unityConnect)
        {
            m_Id = k_Id;
            m_DisplayName = L10n.Tr("Samples");
            m_ResourceLoader = resourceLoader;
            m_PackageDatabase = packageDatabase;
            m_Selection = selection;
            m_AssetDatabase = assetDatabase;
            m_Application = application;
            m_IOProxy = iOProxy;

            var root = m_ResourceLoader.GetTemplate("PackageDetailsSamplesTab.uxml");
            m_ContentContainer.Add(root);
            m_Cache = new VisualElementCache(root);

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        public override bool IsValid(IPackageVersion version)
        {
            if (version == null || version?.HasTag(PackageTag.BuiltIn) == true)
                return false;

            var samples = version.isInstalled || version.HasTag(PackageTag.Feature) ? m_PackageDatabase.GetSamples(version) : Enumerable.Empty<Sample>();
            return samples?.Any() == true;
        }

        protected override void RefreshContent(IPackageVersion version)
        {
            var packageChanged = version.package.uniqueId != m_Version?.package.uniqueId;

            m_Version = version;
            m_Samples = m_Version.isInstalled || m_Version.HasTag(PackageTag.Feature) ? m_PackageDatabase.GetSamples(m_Version) : Enumerable.Empty<Sample>();

            UIUtils.SetElementDisplay(samplesErrorInfoBox, m_Version.HasTag(PackageTag.InDevelopment) && m_Samples.Any(sample => string.IsNullOrEmpty(sample.displayName)));
            ToggleLowWidthSampleView(layout.width, packageChanged);
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (m_Version != null && m_Samples?.Count() > 0 && Math.Abs(evt.newRect.width - evt.oldRect.width) > 1.0f)
                ToggleLowWidthSampleView(evt.newRect.width, false);
        }

        private void ToggleLowWidthSampleView(float width, bool packageChanged)
        {
            var showLowWidth = width <= k_SampleListSwitchWidthBreakpoint;

            if (showLowWidth && (!samplesListLowWidth.visible || samplesContainer.visible || packageChanged))
                SwitchToSampleListLowWidth();
            else if (!showLowWidth && (!samplesContainer.visible || samplesListLowWidth.visible || packageChanged))
                SwitchToRegularSampleList();
        }

        private void SwitchToSampleListLowWidth()
        {
            UIUtils.SetElementDisplay(samplesListLowWidth, true);
            UIUtils.SetElementDisplay(samplesContainer, false);

            samplesListLowWidth.Clear();

            foreach (var sample in m_Samples)
            {
                var sampleVisualItemLowWidth = new PackageSampleItemLowWidth(m_ResourceLoader, m_Version, sample, m_Selection, m_AssetDatabase, m_Application, m_IOProxy);
                samplesListLowWidth.Add(sampleVisualItemLowWidth);
            }
        }

        private void SwitchToRegularSampleList()
        {
            UIUtils.SetElementDisplay(samplesListLowWidth, false);
            UIUtils.SetElementDisplay(samplesContainer, true);

            samplesContainer.Clear();

            foreach (var sample in m_Samples.Where(s => !string.IsNullOrEmpty(s.displayName)))
            {
                var sampleItem = new PackageDetailsSampleItem(m_Version, sample, m_Selection, m_AssetDatabase, m_Application, m_IOProxy);
                var sampleContainer = new VisualElement();
                sampleContainer.AddClasses("sampleContainer");

                var importStatus = new VisualElement();
                importStatus.name = "importStatusContainer";
                importStatus.Add(sampleItem.importStatus);

                var nameAndSizeLabel = new VisualElement();
                nameAndSizeLabel.name = "nameAndSizeLabelContainer";
                nameAndSizeLabel.Add(sampleItem.nameLabel);
                nameAndSizeLabel.Add(sampleItem.sizeLabel);
                nameAndSizeLabel.Add(importStatus);
                sampleContainer.Add(nameAndSizeLabel);

                var importButton = new VisualElement();
                importButton.name = "importButtonContainer";
                importButton.Add(sampleItem.importButton);
                sampleContainer.Add(importButton);

                if (!string.IsNullOrEmpty(sample.description))
                {
                    var description = new VisualElement();
                    description.name = "descriptionContainer";
                    description.Add(sampleItem.descriptionLabel);
                    sampleContainer.Add(description);
                }

                samplesContainer.Add(sampleContainer);
                sampleItem.importButton.SetEnabled(m_Version.isInstalled);
            }
        }

        private readonly VisualElementCache m_Cache;
        internal VisualElement samplesListLowWidth => m_Cache.Get<VisualElement>("samplesListLowWidth");
        internal VisualElement samplesContainer => m_Cache.Get<VisualElement>("samplesContainer");
        internal HelpBox samplesErrorInfoBox => m_Cache.Get<HelpBox>("samplesErrorInfoBox");
    }
}
