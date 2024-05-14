// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsSamplesTab : PackageDetailsTabElement
    {
        public const string k_Id = "samples";

        private IEnumerable<Sample> m_Samples;
        private IPackageVersion m_Version;

        private readonly IResourceLoader m_ResourceLoader;
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly ISelectionProxy m_Selection;
        private readonly IAssetDatabaseProxy m_AssetDatabase;
        private readonly IApplicationProxy m_Application;
        private readonly IIOProxy m_IOProxy;
        public PackageDetailsSamplesTab(IUnityConnectProxy unityConnect,
            IResourceLoader resourceLoader,
            IPackageDatabase packageDatabase,
            ISelectionProxy selection,
            IAssetDatabaseProxy assetDatabase,
            IApplicationProxy application,
            IIOProxy iOProxy) : base(unityConnect)
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
            if (packageChanged)
                RefreshSampleList();
        }

        private void RefreshSampleList()
        {
            samplesContainer.Clear();

            foreach (var sample in m_Samples.Where(s => !string.IsNullOrEmpty(s.displayName)))
            {
                var sampleItem = new PackageDetailsSampleItem(m_Version, sample, m_Selection, m_AssetDatabase, m_Application, m_IOProxy);
                var sampleContainer = new VisualElement();
                sampleContainer.AddClasses("sampleContainer");

                var sampleInformationContainer = new VisualElement { name = "sampleInformationContainer"};

                var nameAndSizeLabel = new VisualElement { name = "nameSizeLabelAndImportStatus"};
                nameAndSizeLabel.Add(sampleItem.nameLabel);
                nameAndSizeLabel.Add(sampleItem.sizeLabel);
                nameAndSizeLabel.Add(sampleItem.importStatus);
                sampleInformationContainer.Add(nameAndSizeLabel);

                if (!string.IsNullOrEmpty(sample.description))
                    sampleInformationContainer.Add(sampleItem.descriptionLabel);

                sampleContainer.Add(sampleInformationContainer);
                sampleContainer.Add(sampleItem.importButton);

                samplesContainer.Add(sampleContainer);
                sampleItem.importButton.SetEnabled(m_Version.isInstalled);
            }
        }

        private readonly VisualElementCache m_Cache;
        internal VisualElement samplesContainer => m_Cache.Get<VisualElement>("samplesContainer");
        private HelpBox samplesErrorInfoBox => m_Cache.Get<HelpBox>("samplesErrorInfoBox");
    }
}
