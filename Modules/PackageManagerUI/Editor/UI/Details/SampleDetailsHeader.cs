// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class SampleDetailsHeader: VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance() => new SampleDetailsHeader(
                ServicesContainer.instance.Resolve<IResourceLoader>(),
                ServicesContainer.instance.Resolve<IApplicationProxy>(),
                ServicesContainer.instance.Resolve<IIOProxy>(),
                ServicesContainer.instance.Resolve<IPackageManagerWindowProxy>(),
                ServicesContainer.instance.Resolve<ISampleImporter>());
        }

        private IPackageVersion m_ParentVersion;
        private Sample m_Sample;
        private SampleToolBarSimpleButton m_ImportButton;
        private SampleToolBarSimpleButton m_LocateButton;
        private SampleToolBarSimpleButton m_GoToPackageButton;

        private readonly IApplicationProxy m_Application;
        private readonly IIOProxy m_IOProxy;
        private readonly IPackageManagerWindowProxy m_PackageManagerWindowProxy;
        private readonly ISampleImporter m_SampleImporter;

        public SampleDetailsHeader(IResourceLoader resourceLoader, IApplicationProxy applicationProxy, IIOProxy ioProxy, IPackageManagerWindowProxy packageManagerWindowProxy, ISampleImporter sampleImporter)
        {
            m_Application = applicationProxy;
            m_IOProxy = ioProxy;
            m_PackageManagerWindowProxy = packageManagerWindowProxy;
            m_SampleImporter = sampleImporter;

            var root = resourceLoader.GetTemplate("SampleDetailsHeader.uxml");
            Add(root);
            cache = new VisualElementCache(root);
            InitializeActionButtons();
        }

        private void InitializeActionButtons()
        {
            var importAction = new ImportSampleAction(m_Application, m_IOProxy, m_SampleImporter);
            var locateAction = new LocateSampleAction(m_Application, m_IOProxy);
            var goToPackageAction = new GoToPackageAction(m_PackageManagerWindowProxy);

            m_ImportButton = new SampleToolBarSimpleButton(importAction);
            m_LocateButton = new SampleToolBarSimpleButton(locateAction);
            m_GoToPackageButton = new SampleToolBarSimpleButton(goToPackageAction);

            actionButtonsContainer.Add(m_ImportButton);
            actionButtonsContainer.Add(m_LocateButton);
            actionButtonsContainer.Add(m_GoToPackageButton);
        }

        public void Refresh(Sample sample)
        {
            m_Sample = sample;
            m_ParentVersion = m_Sample.package?.versions.primary;
            if (m_Sample.isDefault || m_ParentVersion == null)
                return;

            packageAuthorLabel.Refresh(m_ParentVersion);

            RefreshDisplayName();
            RefreshReleaseDate();
            RefreshActionButtons();
        }

        private void RefreshDisplayName()
        {
            sampleDisplayName.text = m_Sample.displayName;
        }

        private void RefreshReleaseDate()
        {
            var versionString = m_ParentVersion.versionString;
            var releaseDateString = m_ParentVersion.publishedDate?.ToString("MMMM dd, yyyy", CultureInfo.CreateSpecificCulture("en-US"));
            detailVersion.text = string.IsNullOrEmpty(releaseDateString)
                ? versionString
                : string.Format(L10n.Tr("{0} · {1}"), versionString, releaseDateString);
        }

        private void RefreshActionButtons()
        {
            m_ImportButton.Refresh(m_Sample);
            m_LocateButton.Refresh(m_Sample);
            m_GoToPackageButton.Refresh(m_Sample);
        }

        private VisualElementCache cache { get; }
        private SelectableLabel sampleDisplayName => cache.Get<SelectableLabel>("sampleDisplayName");
        private SelectableLabel detailVersion => cache.Get<SelectableLabel>("detailVersion");
        private PackageAuthorLabel packageAuthorLabel => cache.Get<PackageAuthorLabel>("packageAuthorLabel");
        private VisualElement actionButtonsContainer => cache.Get<VisualElement>("actionButtonsContainer");
    }
}
