// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Connect;
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

        public static readonly string k_InstallToViewDocumentationTooltip = L10n.Tr("Install to view documentation");
        public static readonly string k_InstallToViewChangelogTooltip = L10n.Tr("Install to view changelog");
        public static readonly string k_InstallToViewLicenseTooltip = L10n.Tr("Install to view licenses");
        public static readonly string k_UnavailableDocumentationTooltip = L10n.Tr("Documentation unavailable");
        public static readonly string k_UnavailableChangelogTooltip = L10n.Tr("Changelog unavailable");
        public static readonly string k_UnavailableLicenseTooltip = L10n.Tr("Licenses unavailable");

        public const string k_LinkClass = "link";

        internal new class UxmlFactory : UxmlFactory<PackageDetailsLinks> {}

        private IPackage m_Package;
        private IPackageVersion m_Version;

        private ApplicationProxy m_Application;
        private IOProxy m_IOProxy;

        private enum LinkState
        {
            NotVisible,
            Enabled,
            Disabled
        }

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

            AddAssetStoreLinks(package, version);
            AddUpmLinks(package,version);

            UIUtils.SetElementDisplay(this, childCount != 0);
        }

        private void AddAssetStoreLinks(IPackage package, IPackageVersion version)
        {
            if (package.product?.links?.Any() != true)
                return;

            var assetStoreLinks = new VisualElement { classList = { "left" }, name = "packageDetailHeaderAssetStoreLinks" };

            // add links from the package
            foreach (var link in package.product.links)
            {
                if (string.IsNullOrEmpty(link.name) || string.IsNullOrEmpty(link.url))
                    continue;

                AddToParentWithSeparator(assetStoreLinks, new Button(() =>
                {
                    m_Application.OpenURL(link.url);
                    if (!string.IsNullOrEmpty(link.analyticsEventName))
                        PackageManagerWindowAnalytics.SendEvent(link.analyticsEventName, version?.uniqueId);
                })
                {
                    text = link.name,
                    tooltip = link.url,
                    classList = { k_LinkClass }
                });
            }

            if (assetStoreLinks.Children().Any())
                Add(assetStoreLinks);
        }

        private void AddUpmLinks(IPackage package, IPackageVersion version)
        {
            var upmLinks = new VisualElement { classList = { "left" }, name = "packageDetailHeaderUPMLinks" };
            var documentationButton = new Button(ViewDocClick) { text = k_ViewDocumentationText, classList = { k_LinkClass } };
            var changelogButton = new Button(ViewChangelogClick) { text = k_ViewChangelogText, classList = { k_LinkClass } };
            var licensesButton = new Button(ViewLicensesClick) { text = k_ViewLicensesText, classList = { k_LinkClass } };


            (LinkState, string) docsStateAndTooltip = GetDocLinkStateAndTooltip(package, version);
            (LinkState, string) changelogStateAndTooltip = GetChangelogLinkStateAndTooltip(package, version);
            (LinkState, string) licenseStateAndTooltip = GetLicenseLinkStateAndTooltip(package, version);
            if (docsStateAndTooltip.Item1 != LinkState.NotVisible)
                AddToParentWithSeparator(upmLinks, documentationButton);

            if (changelogStateAndTooltip.Item1 != LinkState.NotVisible)
                AddToParentWithSeparator(upmLinks, changelogButton);

            if (licenseStateAndTooltip.Item1 != LinkState.NotVisible)
                AddToParentWithSeparator(upmLinks, licensesButton);


            if (docsStateAndTooltip.Item1 == LinkState.Disabled)
            {
                documentationButton.SetEnabled(false);
                documentationButton.tooltip = docsStateAndTooltip.Item2;
            }
            if (changelogStateAndTooltip.Item1 == LinkState.Disabled)
            {
                changelogButton.SetEnabled(false);
                changelogButton.tooltip = changelogStateAndTooltip.Item2;
            }
            if (licenseStateAndTooltip.Item1 == LinkState.Disabled)
            {
                licensesButton.SetEnabled(false);
                licensesButton.tooltip = licenseStateAndTooltip.Item2;
            }

            if (UpmPackageDocs.HasUseCases(version))
                AddToParentWithSeparator(upmLinks, new Button(ViewUseCasesClick) { text = k_ViewUseCasesText, classList = { k_LinkClass } });

            if (UpmPackageDocs.HasDashboard(version))
                AddToParentWithSeparator(upmLinks, new Button(ViewDashboardClick) { text = k_ViewDashboardText, classList = { k_LinkClass } });

            if (upmLinks.Children().Any())
                Add(upmLinks);
        }

        private void AddToParentWithSeparator(VisualElement parent, VisualElement item)
        {
            if (parent.childCount > 0)
                parent.Add(new Label("|") { classList = { "separator" } });
            parent.Add(item);
        }

        private (LinkState state, string tooltip) GetDocLinkStateAndTooltip(IPackage package, IPackageVersion version)
        {
            var upmVersion = version as UpmPackageVersion;

            if (upmVersion == null || version.HasTag(PackageTag.Feature))
                return (LinkState.NotVisible, "");

            if (UpmPackageDocs.GetDocumentationUrl(upmVersion).Any() ||
                !string.IsNullOrEmpty(UpmPackageDocs.GetOfflineDocumentation(m_IOProxy, version)) ||
                version.HasTag(PackageTag.BuiltIn))
                return (LinkState.Enabled, "");

            if (package.product != null && !version.isInstalled)
                return (LinkState.Disabled, k_InstallToViewDocumentationTooltip);

            return (LinkState.Disabled, k_UnavailableDocumentationTooltip);
        }

        private (LinkState state, string tooltip) GetChangelogLinkStateAndTooltip(IPackage package, IPackageVersion version)
        {
            var upmVersion = version as UpmPackageVersion;

            if (upmVersion == null || version.HasTag(PackageTag.Feature | PackageTag.BuiltIn))
                return (LinkState.NotVisible, "");

            if (!string.IsNullOrEmpty(UpmPackageDocs.GetChangelogUrl(upmVersion)) ||
                !string.IsNullOrEmpty(UpmPackageDocs.GetOfflineChangelog(m_IOProxy, upmVersion)))
                return (LinkState.Enabled, "");

            if (package.product != null && !version.isInstalled)
                return (LinkState.Disabled, k_InstallToViewChangelogTooltip);

            return (LinkState.Disabled, k_UnavailableChangelogTooltip);
        }

        private (LinkState state, string tooltip) GetLicenseLinkStateAndTooltip(IPackage package, IPackageVersion version)
        {
            var upmVersion = version as UpmPackageVersion;

            if (upmVersion == null || version.HasTag(PackageTag.Feature | PackageTag.BuiltIn))
                return (LinkState.NotVisible, "");

            if (!string.IsNullOrEmpty(UpmPackageDocs.GetLicensesUrl(upmVersion)) ||
                !string.IsNullOrEmpty(UpmPackageDocs.GetOfflineLicenses(m_IOProxy, upmVersion)))
                return (LinkState.Enabled, "");

            if (package.product != null && !version.isInstalled)
                return (LinkState.Disabled, k_InstallToViewLicenseTooltip);

            return (LinkState.Disabled, k_UnavailableLicenseTooltip);
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
