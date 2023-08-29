// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsLinks : VisualElement
    {
        internal new class UxmlFactory : UxmlFactory<PackageDetailsLinks> {}

        private IApplicationProxy m_Application;
        private IPackageLinkFactory m_PackageLinkFactory;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_Application = container.Resolve<IApplicationProxy>();
            m_PackageLinkFactory = container.Resolve<IPackageLinkFactory>();
        }

        public PackageDetailsLinks()
        {
            ResolveDependencies();
        }

        public void Refresh(IPackageVersion version)
        {
            Clear();

            if (version == null)
                return;

            AddAssetStoreLinks(version);
            AddUpmLinks(version);

            UIUtils.SetElementDisplay(this, childCount != 0);
        }

        private void AddAssetStoreLinks(IPackageVersion version)
        {
            var assetStoreLinks = new VisualElement { classList = { "left" }, name = "assetStoreLinksContainer" };

            AddToParentWithSeparatorIfVisible(assetStoreLinks, m_PackageLinkFactory.CreateProductLink(version));
            AddToParentWithSeparatorIfVisible(assetStoreLinks, m_PackageLinkFactory.CreatePublisherSupportLink(version));
            AddToParentWithSeparatorIfVisible(assetStoreLinks, m_PackageLinkFactory.CreatePublisherWebsiteLink(version));

            if (assetStoreLinks.Children().Any())
                Add(assetStoreLinks);
        }

        private void AddUpmLinks(IPackageVersion version)
        {
            var upmLinks = new VisualElement { classList = { "left" }, name = "upmLinksContainer" };

            AddToParentWithSeparatorIfVisible(upmLinks, m_PackageLinkFactory.CreateUpmDocumentationLink(version));
            AddToParentWithSeparatorIfVisible(upmLinks, m_PackageLinkFactory.CreateUpmChangelogLink(version));
            AddToParentWithSeparatorIfVisible(upmLinks, m_PackageLinkFactory.CreateUpmLicenseLink(version));

            AddToParentWithSeparatorIfVisible(upmLinks, m_PackageLinkFactory.CreateUseCasesLink(version));
            AddToParentWithSeparatorIfVisible(upmLinks, m_PackageLinkFactory.CreateDashboardLink(version));

            if (upmLinks.Children().Any())
                Add(upmLinks);
        }

        private void AddToParentWithSeparatorIfVisible(VisualElement parent, PackageLink link)
        {
            if (link?.isVisible != true)
                return;

            var item = new PackageLinkButton(m_Application, link);

            if (parent.childCount > 0)
                parent.Add(new Label("|") { classList = { "separator" } });

            parent.Add(item);
        }
    }
}
