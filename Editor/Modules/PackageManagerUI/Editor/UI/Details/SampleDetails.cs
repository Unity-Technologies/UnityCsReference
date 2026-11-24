// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class SampleDetails : BaseDetailsView
    {
        private readonly IPackageDatabase m_PackageDatabase;
        private readonly IPageManager m_PageManager;

        public SampleDetails(IPackageDatabase packageDatabase, IPageManager pageManager, IResourceLoader resourceLoader)
        {
            m_PackageDatabase = packageDatabase;
            m_PageManager = pageManager;

            var root = resourceLoader.GetTemplate("SampleDetails.uxml");
            Add(root);
            cache = new VisualElementCache(root);

            AddToClassList("detail");

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            m_PackageDatabase.onSamplesChanged += OnSamplesChanged;
            m_PackageDatabase.onPackagesChanged += OnPackagesChanged;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            m_PackageDatabase.onSamplesChanged -= OnSamplesChanged;
            m_PackageDatabase.onPackagesChanged -= OnPackagesChanged;
        }

        public override void Refresh(PageSelection selections)
        {
            var itemUniqueId = selections?.first ?? string.Empty;
            var sample = m_PackageDatabase.GetSample(itemUniqueId);

            detailsHeader.Refresh(sample);
            detailsBody.Refresh(sample);
        }

        private void OnSamplesChanged(SamplesChangeArgs args)
        {
            Refresh(m_PageManager.activePage.GetSelection());
        }

        private void OnPackagesChanged(PackagesChangeArgs args)
        {
            Refresh(m_PageManager.activePage.GetSelection());
        }

        private VisualElementCache cache { get; }
        private SampleDetailsHeader detailsHeader => cache.Get<SampleDetailsHeader>("detailsHeader");
        private SampleDetailsBody detailsBody => cache.Get<SampleDetailsBody>("detailsBody");
    }
}
