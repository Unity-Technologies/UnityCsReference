// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsSamplesTab : PackageDetailsTabElement
    {
        public const string k_Id = "samples";

        private IReadOnlyList<Sample> m_Samples;
        private IPackageVersion m_Version;

        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IApplicationProxy m_Application;
        private readonly IIOProxy m_IOProxy;
        public PackageDetailsSamplesTab(IUnityConnectProxy unityConnect,
            IResourceLoader resourceLoader,
            IPackageDatabase packageDatabase,
            IApplicationProxy application,
            IIOProxy iOProxy) : base(unityConnect)
        {
            m_Id = k_Id;
            m_DisplayName = L10n.Tr("Samples");
            m_PackageDatabase = packageDatabase;
            m_Application = application;
            m_IOProxy = iOProxy;

            var root = resourceLoader.GetTemplate("DetailsTabs/PackageDetailsSamplesTab.uxml");
            m_ContentContainer.Add(root);
            m_Cache = new VisualElementCache(root);
        }

        public override bool IsValid(IPackageVersion version)
        {
            if (version == null || version.HasTag(PackageTag.BuiltIn))
                return false;

            IReadOnlyCollection<Sample> samples = version.isInstalled || version.HasTag(PackageTag.Feature) ? m_PackageDatabase.GetSamples(version.package.uniqueId) : Array.Empty<Sample>();
            return samples?.Count > 0;
        }

        protected override void RefreshContent(IPackageVersion version)
        {
            m_Version = version;
            m_Samples = m_Version.isInstalled || m_Version.HasTag(PackageTag.Feature) ? m_PackageDatabase.GetSamples(version.package.uniqueId) : Array.Empty<Sample>();

            UIUtils.SetElementDisplay(samplesErrorInfoBox, m_Version.HasTag(PackageTag.InDevelopment) && m_Samples.Exists(sample => string.IsNullOrEmpty(sample.displayName)));
            RefreshSampleList();
        }

        private void RefreshSampleList()
        {
            samplesContainer.Clear();

            foreach (var sample in m_Samples.Filter(s => !string.IsNullOrEmpty(s.displayName)))
            {
                var sampleItem = new PackageDetailsSampleItem(m_Version, sample, m_Application, m_IOProxy);
                var sampleContainer = new VisualElement();
                sampleContainer.AddToClassList("sampleContainer");

                var sampleInformationContainer = new VisualElement { name = "sampleInformationContainer"};

                var nameAndSizeLabel = new VisualElement { name = "nameSizeLabelAndImportStatus"};
                nameAndSizeLabel.Add(sampleItem.nameLabel);
                nameAndSizeLabel.Add(sampleItem.sizeLabel);
                nameAndSizeLabel.Add(sampleItem.importStatus);
                sampleInformationContainer.Add(nameAndSizeLabel);

                if (!string.IsNullOrEmpty(sample.description))
                    sampleInformationContainer.Add(sampleItem.descriptionLabel);

                sampleContainer.Add(sampleInformationContainer);

                var actionButtonsContainer = new VisualElement { name = "actionButtonsContainer"};
                actionButtonsContainer.Add(sampleItem.importButton);
                actionButtonsContainer.Add(sampleItem.locateButton);
                sampleContainer.Add(actionButtonsContainer);

                samplesContainer.Add(sampleContainer);
                sampleItem.importButton.SetEnabled(m_Version.isInstalled);
            }
        }

        private readonly VisualElementCache m_Cache;
        private VisualElement samplesContainer => m_Cache.Get<VisualElement>("samplesContainer");
        private HelpBox samplesErrorInfoBox => m_Cache.Get<HelpBox>("samplesErrorInfoBox");
    }
}
