// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsLinks : VisualElement
    {
        public static readonly string k_ViewDocumentationText = L10n.Tr("View documentation");
        public static readonly string k_ViewChangelogText = L10n.Tr("View changelog");
        public static readonly string k_ViewLicensesText = L10n.Tr("View licenses");

        public const string k_LinkClass = "link";

        internal new class UxmlFactory : UxmlFactory<PackageDetailsLinks> {}

        private IPackageVersion m_Version;

        private ApplicationProxy m_Application;
        private IOProxy m_IOProxy;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_Application = container.Resolve<ApplicationProxy>();
            m_IOProxy = container.Resolve<IOProxy>();
        }

        public PackageDetailsLinks()
        {
            ResolveDependencies();
        }

        public void Refresh(IPackage package, IPackageVersion version)
        {
            m_Version = version;
            Clear();

            if (package == null || version == null)
                return;

            // add links from the package
            foreach (var link in package.links)
            {
                if (string.IsNullOrEmpty(link.name) || string.IsNullOrEmpty(link.url))
                    continue;
                AddToLinks(new Button(() => { m_Application.OpenURL(link.url); })
                {
                    text = link.name,
                    tooltip = link.url,
                    classList = { k_LinkClass }
                });
            }

            // add links related to the upm version
            if (UpmPackageDocs.HasDocs(version))
                AddToLinks(new Button(ViewDocClick) { text = k_ViewDocumentationText, classList = { k_LinkClass } });

            if (UpmPackageDocs.HasChangelog(version))
                AddToLinks(new Button(ViewChangelogClick) { text = k_ViewChangelogText, classList = { k_LinkClass } });

            if (UpmPackageDocs.HasLicenses(version))
                AddToLinks(new Button(ViewLicensesClick) { text = k_ViewLicensesText, classList = { k_LinkClass } });

            UIUtils.SetElementDisplay(this, childCount != 0);
        }

        private void AddToLinks(VisualElement item)
        {
            // Add a seperator between links to make them less crowded together
            if (childCount > 0)
                Add(new Label("·") { classList = { "interpunct" } });
            Add(item);
        }

        private void ViewOfflineDocs(IPackageVersion version, Func<IOProxy, IPackageVersion, bool, string> getUrl, string messageOnNotFound)
        {
            if (!version.isAvailableOnDisk)
            {
                EditorUtility.DisplayDialog(L10n.Tr("Unity Package Manager"), L10n.Tr("This package is not available offline."), L10n.Tr("Ok"));
                return;
            }
            var offlineUrl = getUrl(m_IOProxy, version, true);
            if (!string.IsNullOrEmpty(offlineUrl))
                m_Application.RevealInFinder(offlineUrl);
            else
                EditorUtility.DisplayDialog(L10n.Tr("Unity Package Manager"), messageOnNotFound, L10n.Tr("Ok"));
        }

        private void ViewUrl(IPackageVersion version, Func<IOProxy, IPackageVersion, bool, string> getUrl, string messageOnNotFound)
        {
            if (m_Application.isInternetReachable)
            {
                var onlineUrl = getUrl(m_IOProxy, version, false);
                var request = UnityWebRequest.Head(onlineUrl);
                var operation = request.SendWebRequest();
                operation.completed += (op) =>
                {
                    if (request.responseCode != 404)
                    {
                        m_Application.OpenURL(onlineUrl);
                    }
                    else
                    {
                        ViewOfflineDocs(version, getUrl, messageOnNotFound);
                    }
                };
            }
            else
            {
                ViewOfflineDocs(version, getUrl, messageOnNotFound);
            }
        }

        private void ViewDocClick()
        {
            ViewUrl(m_Version, UpmPackageDocs.GetDocumentationUrl, L10n.Tr("This package does not contain offline documentation."));
        }

        private void ViewChangelogClick()
        {
            ViewUrl(m_Version, UpmPackageDocs.GetChangelogUrl, L10n.Tr("This package does not contain offline changelog."));
        }

        private void ViewLicensesClick()
        {
            ViewUrl(m_Version, UpmPackageDocs.GetLicensesUrl, L10n.Tr("This package does not contain offline licenses."));
        }
    }
}
