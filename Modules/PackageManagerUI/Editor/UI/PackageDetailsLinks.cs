// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
        public static readonly string k_ViewUseCasesText = L10n.Tr("Use Cases");
        public static readonly string k_ViewDashboardText = L10n.Tr("Go to Dashboard");
        public static readonly string k_ViewQuickStartText = L10n.Tr("QuickStart");

        public const string k_LinkClass = "link";

        internal new class UxmlFactory : UxmlFactory<PackageDetailsLinks> {}

        private IPackage m_Package;
        private IPackageVersion m_Version;

        private ApplicationProxy m_Application;
        private UpmCache m_UpmCache;
        private IOProxy m_IOProxy;
        private void ResolveDependencies()
        {
            var container = ServicesContainer.instance;
            m_Application = container.Resolve<ApplicationProxy>();
            m_UpmCache = container.Resolve<UpmCache>();
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
                AddToLinks(leftItems, new Button(() => { m_Application.OpenURL(link.url); })
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

            var topOffset = false;
            if (package.Is(PackageType.Feature) && !string.IsNullOrEmpty(GetQuickStartUrl(m_Version)))
            {
                var quickStartButton = new Button(ViewQuickStartClick) {  name = "quickStart", classList = { "quickStartButton", "right" } };
                quickStartButton.Add(new VisualElement { classList = { "quickStartIcon" } });
                quickStartButton.Add(new TextElement { text = k_ViewQuickStartText, classList = { "quickStartText" } });

                Add(quickStartButton);

                topOffset = leftItems.childCount == 0;
            }
            // Offset the links container to the top when there are no links and only quick start button visible
            EnableInClassList("topOffset", topOffset);
            UIUtils.SetElementDisplay(this, childCount != 0);
        }

        private void AddToLinks(VisualElement parent, VisualElement item, bool showInterpunct = true)
        {
            // Add a seperator between links to make them less crowded together
            if (childCount > 0 && showInterpunct)
                parent.Add(new Label("·") { classList = { "interpunct" } });
            parent.Add(item);
        }

        private void HandleInvalidOrUnreachableOnlineUrl(string onlineUrl, string offlineDocPath, string docType, string analyticsEvent)
        {
            if (!string.IsNullOrEmpty(offlineDocPath))
            {
                m_Application.RevealInFinder(offlineDocPath);

                PackageManagerWindowAnalytics.SendEvent($"{analyticsEvent}OnDisk", m_Version?.uniqueId);
                return;
            }

            if (!string.IsNullOrEmpty(onlineUrl))
            {
                // With the current `UpmPackageDocs.GetDocumentationUrl` implementation,
                // We'll get invalid url links for non-unity packages on unity3d.com
                // We want to avoiding opening these kinds of links to avoid confusion.
                if (!UpmClient.IsUnityUrl(onlineUrl) || m_Package.Is(PackageType.Unity) || m_Version.packageUniqueId.StartsWith("com.unity."))
                {
                    m_Application.OpenURL(onlineUrl);

                    PackageManagerWindowAnalytics.SendEvent($"{analyticsEvent}UnreachableOrInvalidUrl", m_Version?.uniqueId);
                    return;
                }
            }

            PackageManagerWindowAnalytics.SendEvent($"{analyticsEvent}NotFound", m_Version?.uniqueId);

            Debug.LogError(string.Format(L10n.Tr("[Package Manager Window] Unable to find valid {0} for this {1}."), docType, m_Package.GetDescriptor()));
        }

        private void OpenWebUrl(string onlineUrl, string analyticsEvent, Action errorCallback)
        {
            var request = UnityWebRequest.Head(onlineUrl);
            try
            {
                var operation = request.SendWebRequest();
                operation.completed += (op) =>
                {
                    if (request.responseCode >= 200 && request.responseCode < 300)
                    {
                        m_Application.OpenURL(onlineUrl);
                        PackageManagerWindowAnalytics.SendEvent($"{analyticsEvent}ValidUrl", m_Version?.uniqueId);
                    }
                    else
                        errorCallback?.Invoke();
                };
            }
            catch (InvalidOperationException e)
            {
                if (e.Message != "Insecure connection not allowed")
                    throw e;
            }
        }

        private void ViewUrl(string[] onlineUrls, string offlineDocPath, string docType, string analyticsEvent)
        {
            if (m_Application.isInternetReachable)
            {
                if (onlineUrls.Length == 0)
                {
                    HandleInvalidOrUnreachableOnlineUrl(string.Empty, offlineDocPath, docType, analyticsEvent);
                    return;
                }

                OpenWebUrl(onlineUrls[0], analyticsEvent, () =>
                {
                    var urls = new List<string>(onlineUrls).Skip(1).ToArray();
                    ViewUrl(urls, offlineDocPath, docType, analyticsEvent);
                });
            }
            else
            {
                HandleInvalidOrUnreachableOnlineUrl(string.Empty, offlineDocPath, docType, analyticsEvent);
            }
        }

        private void ViewUrl(string onlineUrl, string offlineDocPath, string docType, string analyticsEvent)
        {
            if (!string.IsNullOrEmpty(onlineUrl) && m_Application.isInternetReachable)
            {
                OpenWebUrl(onlineUrl, analyticsEvent, () =>
                {
                    HandleInvalidOrUnreachableOnlineUrl(onlineUrl, offlineDocPath, docType, analyticsEvent);
                });
                return;
            }
            HandleInvalidOrUnreachableOnlineUrl(onlineUrl, offlineDocPath, docType, analyticsEvent);
        }

        private void ViewDocClick()
        {
            ViewUrl(UpmPackageDocs.GetDocumentationUrl(m_Version), UpmPackageDocs.GetOfflineDocumentation(m_IOProxy, m_Version), L10n.Tr("documentation"), "viewDocs");
        }

        private void ViewChangelogClick()
        {
            ViewUrl(UpmPackageDocs.GetChangelogUrl(m_Version), UpmPackageDocs.GetOfflineChangelog(m_IOProxy, m_Version), L10n.Tr("changelog"), "viewChangelog");
        }

        private void ViewLicensesClick()
        {
            ViewUrl(UpmPackageDocs.GetLicensesUrl(m_Version), UpmPackageDocs.GetOfflineLicenses(m_IOProxy, m_Version), L10n.Tr("license documentation"), "viewLicense");
        }

        private void ViewUseCasesClick()
        {
            ViewUrl(UpmPackageDocs.GetUseCasesUrl(m_Version), UpmPackageDocs.GetOfflineUseCasesUrl(m_IOProxy, m_Version), L10n.Tr("use cases"), "viewUseCases");
        }

        private void ViewDashboardClick()
        {
            ViewUrl(UpmPackageDocs.GetDashboardUrl(m_Version), UpmPackageDocs.GetOfflineDashboardUrl(m_IOProxy, m_Version), L10n.Tr("dashboard"), "viewDashboard");
        }

        private void ViewQuickStartClick()
        {
            ViewUrl(GetQuickStartUrl(m_Version), string.Empty, L10n.Tr("quick start documentation"), "viewQuickstart");
        }

        public string GetQuickStartUrl(IPackageVersion version)
        {
            var upmReserved = m_UpmCache.ParseUpmReserved(version?.packageInfo);
            return upmReserved?.GetString("quickstart") ?? string.Empty;
        }
    }
}
