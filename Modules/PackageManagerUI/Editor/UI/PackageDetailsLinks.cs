// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class PackageDetailsLinks : VisualElement
    {
        public static readonly string k_ViewDocumentationText = L10n.Tr("View documentation");
        public static readonly string k_ViewChangelogText = L10n.Tr("View changelog");
        public static readonly string k_ViewLicensesText = L10n.Tr("View licenses");
        public static readonly string k_ViewUseCasesText = L10n.Tr("Use Cases");
        public static readonly string k_ViewDashboardText = L10n.Tr("Go to Dashboard");

        public const string k_LinkClass = "link";

        internal new class UxmlFactory : UxmlFactory<PackageDetailsLinks> {}

        private IPackage m_Package;
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
            m_Package = package;
            m_Version = version;
            Clear();

            if (package == null || version == null)
                return;

            var leftItems = new VisualElement { classList = { "left" } };
            Add(leftItems);

            // add links from the package
            foreach (var link in package.links)
            {
                if (string.IsNullOrEmpty(link.name) || string.IsNullOrEmpty(link.url))
                    continue;
                AddToLinks(leftItems, new Button(() =>
                {
                    m_Application.OpenURL(link.url);
                    if (!string.IsNullOrEmpty(link.analyticsEventName))
                        PackageManagerWindowAnalytics.SendEvent(link.analyticsEventName, version?.uniqueId);
                })
                {
                    text = link.name,
                    tooltip = link.url,
                    classList = { k_LinkClass }
                }, package.links.First() != link);
            }

            // add links related to the upm version
            if (UpmPackageDocs.HasDocs(version))
                AddToLinks(leftItems, new Button(ViewDocClick) { text = k_ViewDocumentationText, classList = { k_LinkClass } }, false);

            if (UpmPackageDocs.HasChangelog(version))
                AddToLinks(leftItems, new Button(ViewChangelogClick) { text = k_ViewChangelogText, classList = { k_LinkClass } });

            if (UpmPackageDocs.HasLicenses(version))
                AddToLinks(leftItems, new Button(ViewLicensesClick) { text = k_ViewLicensesText, classList = { k_LinkClass } });

            if (UpmPackageDocs.HasUseCases(version))
                AddToLinks(leftItems, new Button(ViewUseCasesClick) { text = k_ViewUseCasesText, classList = { k_LinkClass } });

            if (UpmPackageDocs.HasDashboard(version))
                AddToLinks(leftItems, new Button(ViewDashboardClick) { text = k_ViewDashboardText, classList = { k_LinkClass } });

            UIUtils.SetElementDisplay(this, childCount != 0);
        }

        private void AddToLinks(VisualElement parent, VisualElement item, bool showInterpunct = true)
        {
            // Add a seperator between links to make them less crowded together
            if (childCount > 0 && showInterpunct)
                parent.Add(new Label("·") { classList = { "interpunct" } });
            parent.Add(item);
        }

        private void ViewUrl(string[] onlineUrls, string offlineDocPath, string docType, string analyticsEvent)
        {
            if (m_Application.isInternetReachable)
            {
                if (onlineUrls.Length == 0)
                {
                    UpmPackageDocs.HandleInvalidOrUnreachableOnlineUrl(string.Empty, offlineDocPath, docType, analyticsEvent, m_Version, m_Package, m_Application);
                    return;
                }

                UpmPackageDocs.OpenWebUrl(onlineUrls[0], m_Version, m_Application, analyticsEvent, () =>
                {
                    var urls = new List<string>(onlineUrls).Skip(1).ToArray();
                    ViewUrl(urls, offlineDocPath, docType, analyticsEvent);
                });
            }
            else
            {
                UpmPackageDocs.HandleInvalidOrUnreachableOnlineUrl(string.Empty, offlineDocPath, docType, analyticsEvent, m_Version, m_Package, m_Application);
            }
        }

        private void ViewDocClick()
        {
            ViewUrl(UpmPackageDocs.GetDocumentationUrl(m_Version), UpmPackageDocs.GetOfflineDocumentation(m_IOProxy, m_Version), L10n.Tr("documentation"), "viewDocs");
        }

        private void ViewChangelogClick()
        {
            UpmPackageDocs.ViewUrl(UpmPackageDocs.GetChangelogUrl(m_Version), UpmPackageDocs.GetOfflineChangelog(m_IOProxy, m_Version), L10n.Tr("changelog"), "viewChangelog", m_Version, m_Package, m_Application);
        }

        private void ViewLicensesClick()
        {
            UpmPackageDocs.ViewUrl(UpmPackageDocs.GetLicensesUrl(m_Version), UpmPackageDocs.GetOfflineLicenses(m_IOProxy, m_Version), L10n.Tr("license documentation"), "viewLicense", m_Version, m_Package, m_Application);
        }

        private void ViewUseCasesClick()
        {
            UpmPackageDocs.ViewUrl(UpmPackageDocs.GetUseCasesUrl(m_Version), UpmPackageDocs.GetOfflineUseCasesUrl(m_IOProxy, m_Version), L10n.Tr("use cases"), "viewUseCases", m_Version, m_Package, m_Application);
        }

        private void ViewDashboardClick()
        {
            UpmPackageDocs.ViewUrl(UpmPackageDocs.GetDashboardUrl(m_Version), UpmPackageDocs.GetOfflineDashboardUrl(m_IOProxy, m_Version), L10n.Tr("dashboard"), "viewDashboard", m_Version, m_Package, m_Application);
        }
    }
}
