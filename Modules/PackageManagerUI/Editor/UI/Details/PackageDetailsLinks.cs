// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsLinks : VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance()
            {
                var container = ServicesContainer.instance;
                return new PackageDetailsLinks(
                    container.Resolve<IApplicationProxy>(),
                    container.Resolve<IPackageLinkFactory>());
            }
        }

        private readonly IApplicationProxy m_Application;
        private readonly IPackageLinkFactory m_PackageLinkFactory;
        public PackageDetailsLinks(
            IApplicationProxy application,
            IPackageLinkFactory packageLinkFactory)
        {
            m_Application = application;
            m_PackageLinkFactory = packageLinkFactory;
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
            var assetStoreLinks = new VisualElement { name = "assetStoreLinksContainer" }.WithClassList("left");

            AddToParentWithSeparatorIfVisible(assetStoreLinks, m_PackageLinkFactory.CreateProductLink(version));
            AddToParentWithSeparatorIfVisible(assetStoreLinks, m_PackageLinkFactory.CreatePublisherSupportLink(version));
            AddToParentWithSeparatorIfVisible(assetStoreLinks, m_PackageLinkFactory.CreatePublisherWebsiteLink(version));
            AddToParentWithSeparatorIfVisible(assetStoreLinks, m_PackageLinkFactory.CreateReviewLink(version));

            if (assetStoreLinks.childCount > 0)
                Add(assetStoreLinks);
        }

        private void AddUpmLinks(IPackageVersion version)
        {
            var upmLinks = new VisualElement { name = "upmLinksContainer" }.WithClassList("left");

            AddToParentWithSeparatorIfVisible(upmLinks, m_PackageLinkFactory.CreateUpmDocumentationLink(version));
            AddToParentWithSeparatorIfVisible(upmLinks, m_PackageLinkFactory.CreateUpmChangelogLink(version));
            AddToParentWithSeparatorIfVisible(upmLinks, m_PackageLinkFactory.CreateUpmLicenseLink(version));

            AddToParentWithSeparatorIfVisible(upmLinks, m_PackageLinkFactory.CreateUseCasesLink(version));
            AddToParentWithSeparatorIfVisible(upmLinks, m_PackageLinkFactory.CreateDashboardLink(version));

            if (upmLinks.childCount > 0)
                Add(upmLinks);
        }

        private void AddToParentWithSeparatorIfVisible(VisualElement parent, PackageLink link)
        {
            if (link?.isVisible != true)
                return;

            var item = new PackageLinkButton(m_Application, link);

            if (parent.childCount > 0)
                parent.Add(new Label("|").WithClassList("separator"));

            parent.Add(item);
        }
    }
}
