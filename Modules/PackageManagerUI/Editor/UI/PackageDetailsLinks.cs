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
        public static readonly string k_ViewDocumentationText = L10n.Tr("Documentation");
        public static readonly string k_ViewChangelogText = L10n.Tr("Changelog");
        public static readonly string k_ViewLicensesText = L10n.Tr("Licenses");
        public static readonly string k_ViewUseCasesText = L10n.Tr("Use Cases");
        public static readonly string k_ViewDashboardText = L10n.Tr("Go to Dashboard");

        public static readonly string k_ViewDisabledDocumentationToolTip = L10n.Tr("Install to view documentation");
        public static readonly string k_ViewDisabledChangelogToolTip = L10n.Tr("Install to view changelog");
        public static readonly string k_ViewDisabledLicensesToolTip = L10n.Tr("Install to view licenses");

        public const string k_LinkClass = "link";

        internal new class UxmlFactory : UxmlFactory<PackageDetailsLinks> {}

        private IPackage m_Package;
        private IPackageVersion m_Version;

        private ApplicationProxy m_Application;
        private IOProxy m_IOProxy;
        private UpmCache m_UpmCache;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_Application = container.Resolve<ApplicationProxy>();
            m_IOProxy = container.Resolve<IOProxy>();
            m_UpmCache = container.Resolve<UpmCache>();
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

            var assetStoreLinks = new VisualElement { classList = { "left" }, name = "packageDetailHeaderAssetStoreLinks" };
            Add(assetStoreLinks);

            // add links from the package
            foreach (var link in package.links)
            {
                if (string.IsNullOrEmpty(link.name) || string.IsNullOrEmpty(link.url))
                    continue;
                AddToLinks(assetStoreLinks, new Button(() =>
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

            var upmLinks = new VisualElement { classList = { "left" }, name = "packageDetailHeaderUPMLinks" };
            var documentationButton = new Button(ViewDocClick) { text = k_ViewDocumentationText, classList = { k_LinkClass } };
            var changelogButton = new Button(ViewChangelogClick) { text = k_ViewChangelogText, classList = { k_LinkClass } };
            var licensesButton = new Button(ViewLicensesClick) { text = k_ViewLicensesText, classList = { k_LinkClass } };

            var packageInfo = m_UpmCache.GetBestMatchPackageInfo(m_Version.name, m_Version.isInstalled, m_Version.versionString);
            // add links related to the upm version
            if (UpmPackageDocs.HasDocs(packageInfo))
                AddToLinks(upmLinks, documentationButton, false);

            if (UpmPackageDocs.HasChangelog(packageInfo))
                AddToLinks(upmLinks, changelogButton);

            if (UpmPackageDocs.HasLicenses(packageInfo))
                AddToLinks(upmLinks, licensesButton);


            if (UpmPackageDocs.HasUseCases(version))
                AddToLinks(upmLinks, new Button(ViewUseCasesClick) { text = k_ViewUseCasesText, classList = { k_LinkClass } });

            if (UpmPackageDocs.HasDashboard(version))
                AddToLinks(upmLinks, new Button(ViewDashboardClick) { text = k_ViewDashboardText, classList = { k_LinkClass } });

            if (upmLinks.Children().Any())
            {
                Add(upmLinks);
                if (package.Is(PackageType.AssetStore) && !version.isInstalled)
                {
                    if (string.IsNullOrEmpty(packageInfo?.documentationUrl))
                    {
                        documentationButton.SetEnabled(false);
                        documentationButton.tooltip = k_ViewDisabledDocumentationToolTip;
                    }
                    if (string.IsNullOrEmpty(packageInfo?.changelogUrl))
                    {
                        changelogButton.SetEnabled(false);
                        changelogButton.tooltip = k_ViewDisabledChangelogToolTip;
                    }
                    if (string.IsNullOrEmpty(packageInfo?.licensesUrl))
                    {
                        licensesButton.SetEnabled(false);
                        licensesButton.tooltip = k_ViewDisabledLicensesToolTip;
                    }
                }
            }

            UIUtils.SetElementDisplay(this, childCount != 0);
        }

        private void AddToLinks(VisualElement parent, VisualElement item, bool showSeparator = true)
        {
            // Add a seperator between links to make them less crowded together
            if (childCount > 0 && showSeparator)
                parent.Add(new Label("|") { classList = { "separator" } });
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
            var packageInfo = m_Version != null ? m_UpmCache.GetBestMatchPackageInfo(m_Version.name, m_Version.isInstalled, m_Version.versionString) : null;
            ViewUrl(UpmPackageDocs.GetDocumentationUrl(packageInfo), UpmPackageDocs.GetOfflineDocumentation(m_IOProxy, packageInfo), L10n.Tr("documentation"), "viewDocs");
        }

        private void ViewChangelogClick()
        {
            var packageInfo = m_Version != null ? m_UpmCache.GetBestMatchPackageInfo(m_Version.name, m_Version.isInstalled, m_Version.versionString) : null;
            UpmPackageDocs.ViewUrl(UpmPackageDocs.GetChangelogUrl(packageInfo), UpmPackageDocs.GetOfflineChangelog(m_IOProxy, packageInfo), L10n.Tr("changelog"), "viewChangelog", m_Version, m_Package, m_Application);
        }

        private void ViewLicensesClick()
        {
            var packageInfo = m_Version != null ? m_UpmCache.GetBestMatchPackageInfo(m_Version.name, m_Version.isInstalled, m_Version.versionString) : null;
            UpmPackageDocs.ViewUrl(UpmPackageDocs.GetLicensesUrl(packageInfo), UpmPackageDocs.GetOfflineLicenses(m_IOProxy, packageInfo), L10n.Tr("license documentation"), "viewLicense", m_Version, m_Package, m_Application);
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
