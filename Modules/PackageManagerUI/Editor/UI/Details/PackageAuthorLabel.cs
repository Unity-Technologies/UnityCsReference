// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageAuthorLabel : VisualElement
    {
        [Serializable]
        public new class UxmlSerializedData : VisualElement.UxmlSerializedData
        {
            public override object CreateInstance()
            {
                return new PackageAuthorLabel(
                    ServicesContainer.instance.Resolve<IApplicationProxy>(),
                    ServicesContainer.instance.Resolve<IPackageLinkFactory>());
            }
        }

        private IApplicationProxy m_Application;
        private IPackageLinkFactory m_PackageLinkFactory;

        public PackageAuthorLabel(IApplicationProxy application, IPackageLinkFactory packageLinkFactory)
        {
            m_Application = application;
            m_PackageLinkFactory = packageLinkFactory;

            AddToClassList("package-author-label");
        }

        public void Refresh(IPackageVersion version)
        {
            Clear();

            if (version == null)
                return;

            if (version.isFromAssetStore)
                SetAuthorLabel(null, m_PackageLinkFactory.CreateAssetStoreAuthorLink(version));
            else if (version.isFromUnity)
                SetAuthorLabel(L10n.Tr("Unity Technologies"), null);
            else
                SetAuthorLabel(version.author?.name ?? string.Empty, null);
        }

        private void SetAuthorLabel(string authorName, PackageLink authorLink)
        {
            VisualElement authorElement = null;

            if (authorLink is { isVisible: true })
                authorElement = new PackageLinkButton(m_Application, authorLink);
            else if (!string.IsNullOrEmpty(authorName))
                authorElement = new Label(authorName) { name = "authorLabel" };

            if (authorElement == null)
            {
                Add(new Label(L10n.Tr("Author unknown")) { name = "authorLabel" });
                return;
            }

            Add(new Label(L10n.Tr("By")));
            Add(authorElement);
        }
    }
}
