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
        private readonly IDelayedSelectionHandler m_DelayedSelectionHandler;
        private readonly ISampleImporter m_SampleImporter;
        public PackageDetailsSamplesTab(IUnityConnectProxy unityConnect,
            IResourceLoader resourceLoader,
            IPackageDatabase packageDatabase,
            IApplicationProxy application,
            IIOProxy iOProxy,
            IDelayedSelectionHandler delayedSelectionHandler,
            ISampleImporter sampleImporter) : base(unityConnect)
        {
            m_Id = k_Id;
            m_DisplayName = L10n.Tr("Samples");
            m_PackageDatabase = packageDatabase;
            m_Application = application;
            m_IOProxy = iOProxy;
            m_DelayedSelectionHandler = delayedSelectionHandler;
            m_SampleImporter = sampleImporter;

            var root = resourceLoader.GetTemplate("DetailsTabs/PackageDetailsSamplesTab.uxml");
            m_ContentContainer.Add(root);
            m_Cache = new VisualElementCache(root);

            viewMoreSamplesButton.clickable.clicked += OnViewMoreSamplesClicked;
        }

        public override bool IsValid(IPackageVersion version)
        {
            if (version == null || version.HasTag(PackageTag.BuiltIn))
                return false;

            IReadOnlyCollection<Sample> samples = version.isInstalled || version.HasTag(PackageTag.Feature)
                ? m_PackageDatabase.GetSamples(version.package.uniqueId)
                : Array.Empty<Sample>();

            return samples?.Count > 0 || CheckDependenciesForSamples(version);
        }

        private bool CheckDependenciesForSamples(IPackageVersion version)
        {
            var matchingPackages = GetMatchingAuthorDependencies(version);

            foreach (var package in matchingPackages)
            {
                var sampleId = package.product != null ? package.product.id.ToString() : package.uniqueId;
                var samples = m_PackageDatabase.GetSamples(sampleId);

                if (samples?.Count > 0)
                    return true;
            }

            return false;
        }

        private List<IPackage> GetMatchingAuthorDependencies(IPackageVersion version)
        {
            var matches = new List<IPackage>();
            var dependencies = version.resolvedDependencies;

            if (dependencies == null || dependencies.Length == 0)
                return matches;

            var baseAuthor = version.isFromUnity ? L10n.Tr("Unity Technologies") : version.author?.name;
            if (string.IsNullOrEmpty(baseAuthor))
                return matches;

            foreach (var dependency in dependencies)
            {
                m_PackageDatabase.GetPackageAndVersion(dependency, out var package, out var v);
                var dependencyAuthor = v?.isFromUnity == true ? L10n.Tr("Unity Technologies") : v?.author?.name;
                if (package != null && dependencyAuthor == baseAuthor)
                    matches.Add(package);
            }
            return matches;
        }

        protected override void RefreshContent(IPackageVersion version)
        {
            m_Version = version;
            m_Samples = m_Version.isInstalled || m_Version.HasTag(PackageTag.Feature)
                ? m_PackageDatabase.GetSamples(version.package.uniqueId)
                : Array.Empty<Sample>();

            UIUtils.SetElementDisplay(samplesErrorInfoBox, m_Version.HasTag(PackageTag.InDevelopment) && m_Samples?.Exists(sample => string.IsNullOrEmpty(sample.displayName)) == true);
            RefreshSampleList();
        }

        private void RefreshSampleList()
        {
            samplesContainer.Clear();

            viewMoreSamplesTitleLabel.text = L10n.Tr("Discover Samples Faster");
            viewMoreSamplesDescriptionLabel.text = L10n.Tr("Samples are now available in one convenient location. Browse, discover, and import the assets you need to kickstart your project from a single, unified view.");
            viewMoreSamplesButton.text = L10n.Tr("View More Samples");
            UIUtils.SetElementDisplay(viewMoreSamplesContainer, m_Samples?.Count > 0 || CheckDependenciesForSamples(m_Version));

            if (m_Samples == null)
                return;

            foreach (var sample in m_Samples.Filter(s => !string.IsNullOrEmpty(s.displayName)))
            {
                var sampleItem = new PackageDetailsSampleItem(sample, m_Application, m_IOProxy, m_SampleImporter);
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
            }
        }

        private void OnViewMoreSamplesClicked()
        {
            var packageIdsToSelect = new HashSet<string>();
            if (m_Samples != null && m_Samples.Count > 0)
                packageIdsToSelect.Add(m_Version.package.uniqueId);

            var matchingPackages = GetMatchingAuthorDependencies(m_Version);

            foreach (var package in matchingPackages)
            {
                var samples = m_PackageDatabase.GetSamples(package.uniqueId);

                if (samples != null && samples.Count > 0)
                    packageIdsToSelect.Add(package.uniqueId);
            }

            var idsAsList = new List<string>(packageIdsToSelect);
            PackageManagerWindowAnalytics.SendEvent("viewMoreSamples", m_Version);
            m_DelayedSelectionHandler.SelectSamplePageWithPackageFilters(idsAsList);
        }

        private readonly VisualElementCache m_Cache;
        private VisualElement samplesContainer => m_Cache.Get<VisualElement>("samplesContainer");
        private HelpBox samplesErrorInfoBox => m_Cache.Get<HelpBox>("samplesErrorInfoBox");
        private VisualElement viewMoreSamplesContainer => m_Cache.Get<VisualElement>("viewMoreSamplesContainer");
        private Label viewMoreSamplesTitleLabel => m_Cache.Get<Label>("viewMoreSamplesTitleLabel");
        private Label viewMoreSamplesDescriptionLabel => m_Cache.Get<Label>("viewMoreSamplesDescriptionLabel");
        private Button viewMoreSamplesButton => m_Cache.Get<Button>("viewMoreSamplesButton");
    }
}
