// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class UpmPackageDocs
    {
        // Module package.json files contain a documentation url embedded in the description.
        // We parse that to have the "View Documentation" button direct to it, instead of showing
        // the link in the description text.
        internal const string k_BuiltinPackageDocsUrlKey = "Scripting API: ";

        public static string[] FetchUrlsFromDescription(UpmPackageVersion version)
        {
            var applicationProxy = ServicesContainer.instance.Resolve<ApplicationProxy>();
            List<string> urls = new List<string>();

            var descriptionSlitWithUrl = version.packageInfo.description.Split(new[] { $"{k_BuiltinPackageDocsUrlKey}https://docs.unity3d.com/" }, StringSplitOptions.None);
            if (descriptionSlitWithUrl.Length > 1)
                urls.Add($"https://docs.unity3d.com/{applicationProxy.shortUnityVersion}/Documentation/" + descriptionSlitWithUrl[1]);

            var descriptionSlitWithoutUrl = version.packageInfo.description.Split(new[] { k_BuiltinPackageDocsUrlKey }, StringSplitOptions.None);
            if (descriptionSlitWithoutUrl.Length > 1)
                urls.Add(descriptionSlitWithoutUrl[1]);

            return urls.ToArray();
        }

        public static string FetchBuiltinDescription(UpmPackageVersion version)
        {
            return string.IsNullOrEmpty(version?.packageInfo?.description) ?
                string.Format(L10n.Tr("This built in package controls the presence of the {0} module."), version.displayName) :
                version.packageInfo.description.Split(new[] { k_BuiltinPackageDocsUrlKey }, StringSplitOptions.None)[0];
        }

        public static void HandleInvalidOrUnreachableOnlineUrl(string onlineUrl,  string offlineDocPath, string docType, string analyticsEvent, IPackageVersion version, IPackage package, ApplicationProxy applicationProxy)
        {
            if (!string.IsNullOrEmpty(offlineDocPath))
            {
                applicationProxy.RevealInFinder(offlineDocPath);

                PackageManagerWindowAnalytics.SendEvent($"{analyticsEvent}OnDisk", version?.uniqueId);
                return;
            }

            if (!string.IsNullOrEmpty(onlineUrl))
            {
                // With the current `UpmPackageDocs.GetDocumentationUrl` implementation,
                // We'll get invalid url links for non-unity packages on unity3d.com
                // We want to avoiding opening these kinds of links to avoid confusion.
                if (!UpmClient.IsUnityUrl(onlineUrl) || package.Is(PackageType.Unity) || version.packageUniqueId.StartsWith("com.unity."))
                {
                    applicationProxy.OpenURL(onlineUrl);

                    PackageManagerWindowAnalytics.SendEvent($"{analyticsEvent}UnreachableOrInvalidUrl", version?.uniqueId);
                    return;
                }
            }

            PackageManagerWindowAnalytics.SendEvent($"{analyticsEvent}NotFound", version?.uniqueId);

            Debug.LogError(string.Format(L10n.Tr("[Package Manager Window] Unable to find valid {0} for this {1}."), docType, package.GetDescriptor()));
        }

        public static void OpenWebUrl(string onlineUrl, IPackageVersion version, ApplicationProxy applicationProxy, string analyticsEvent, Action errorCallback)
        {
            var request = UnityWebRequest.Head(onlineUrl);
            try
            {
                var operation = request.SendWebRequest();
                operation.completed += (op) =>
                {
                    if (request.responseCode >= 200 && request.responseCode < 300)
                    {
                        applicationProxy.OpenURL(onlineUrl);
                        PackageManagerWindowAnalytics.SendEvent($"{analyticsEvent}ValidUrl", version?.uniqueId);
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

        public static void ViewUrl(string onlineUrl, string offlineDocPath, string docType, string analyticsEvent, IPackageVersion version, IPackage package, ApplicationProxy applicationProxy)
        {
            if (!string.IsNullOrEmpty(onlineUrl) && applicationProxy.isInternetReachable)
            {
                OpenWebUrl(onlineUrl, version, applicationProxy, analyticsEvent, () =>
                {
                    HandleInvalidOrUnreachableOnlineUrl(onlineUrl, offlineDocPath, docType, analyticsEvent, version, package, applicationProxy);
                });
                return;
            }
            HandleInvalidOrUnreachableOnlineUrl(onlineUrl, offlineDocPath, docType, analyticsEvent, version, package, applicationProxy);
        }

        public static string GetOfflineDocumentation(IOProxy IOProxy, IPackageVersion version)
        {
            if (version?.isAvailableOnDisk == true && version.packageInfo != null)
            {
                try
                {
                    var docsFolder = IOProxy.PathsCombine(version.packageInfo.resolvedPath, "Documentation~");
                    if (!IOProxy.DirectoryExists(docsFolder))
                        docsFolder = IOProxy.PathsCombine(version.packageInfo.resolvedPath, "Documentation");
                    if (IOProxy.DirectoryExists(docsFolder))
                    {
                        var mdFiles = IOProxy.DirectoryGetFiles(docsFolder, "*.md", System.IO.SearchOption.TopDirectoryOnly);
                        var docsMd = mdFiles.FirstOrDefault(d => IOProxy.GetFileName(d).ToLower() == "index.md")
                            ?? mdFiles.FirstOrDefault(d => IOProxy.GetFileName(d).ToLower() == "tableofcontents.md") ?? mdFiles.FirstOrDefault();
                        if (!string.IsNullOrEmpty(docsMd))
                            return docsMd;
                    }
                }
                catch (System.IO.IOException) {}
            }
            return string.Empty;
        }

        public static string[] GetDocumentationUrl(IPackageVersion version)
        {
            var upmVersion = version as UpmPackageVersion;

            if (upmVersion == null)
                return new string[] { };

            if (!string.IsNullOrEmpty(upmVersion.documentationUrl))
                return new [] { upmVersion.documentationUrl };

            if (upmVersion.HasTag(PackageTag.BuiltIn) && !string.IsNullOrEmpty(upmVersion.description))
                return FetchUrlsFromDescription(upmVersion);

            return upmVersion.isUnityPackage
                ? new [] { $"https://docs.unity3d.com/Packages/{upmVersion.shortVersionId}/index.html" }
                : new string[] { };
        }

        public static string GetQuickStartUrl(IPackageVersion version, UpmCache upmCache)
        {
            var upmReserved = upmCache.ParseUpmReserved(version?.packageInfo);
            return upmReserved?.GetString("quickstart") ?? string.Empty;
        }

        public static string GetChangelogUrl(IPackageVersion version)
        {
            var upmVersion = version as UpmPackageVersion;
            if (upmVersion == null)
                return string.Empty;

            if (!string.IsNullOrEmpty(upmVersion.changelogUrl))
                return upmVersion.changelogUrl;

            return upmVersion.isUnityPackage
                ? $"https://docs.unity3d.com/Packages/{upmVersion.shortVersionId}/changelog/CHANGELOG.html"
                : string.Empty;
        }

        public static string GetOfflineChangelog(IOProxy IOProxy, IPackageVersion version)
        {
            if (version?.isAvailableOnDisk == true && version.packageInfo != null)
            {
                try
                {
                    var changelogFile = IOProxy.PathsCombine(version.packageInfo.resolvedPath, "CHANGELOG.md");
                    return IOProxy.FileExists(changelogFile) ? changelogFile : string.Empty;
                }
                catch (System.IO.IOException) {}
            }
            return string.Empty;
        }

        public static string GetLicensesUrl(IPackageVersion version)
        {
            var upmVersion = version as UpmPackageVersion;
            if (upmVersion == null)
                return string.Empty;

            if (!string.IsNullOrEmpty(upmVersion.licensesUrl))
                return upmVersion.licensesUrl;

            return upmVersion.isUnityPackage
                ? $"https://docs.unity3d.com/Packages/{upmVersion.shortVersionId}/license/index.html"
                : string.Empty;
        }

        public static string GetOfflineLicenses(IOProxy IOProxy, IPackageVersion version)
        {
            if (version?.isAvailableOnDisk == true && version.packageInfo != null)
            {
                try
                {
                    var licenseFile = IOProxy.PathsCombine(version.packageInfo.resolvedPath, "LICENSE.md");
                    return IOProxy.FileExists(licenseFile) ? licenseFile : string.Empty;
                }
                catch (System.IO.IOException) {}
            }
            return string.Empty;
        }

        public static string GetUseCasesUrl(IPackageVersion version)
        {
            return EditorGameServiceExtension.GetUseCasesUrl(version);
        }

        public static string GetOfflineUseCasesUrl(IOProxy IOProxy, IPackageVersion version)
        {
            return string.Empty;
        }

        public static string GetDashboardUrl(IPackageVersion version)
        {
            return EditorGameServiceExtension.GetDashboardUrl(version);
        }

        public static string GetOfflineDashboardUrl(IOProxy IOProxy, IPackageVersion version)
        {
            return string.Empty;
        }

        public static bool HasChangelog(IPackageVersion version)
        {
            var upmVersion = version as UpmPackageVersion;
            if (!string.IsNullOrEmpty(upmVersion?.changelogUrl))
                return true;
            return upmVersion != null && !version.HasTag(PackageTag.BuiltIn | PackageTag.Feature);
        }

        public static bool HasUseCases(IPackageVersion version)
        {
            return !string.IsNullOrEmpty(EditorGameServiceExtension.GetUseCasesUrl(version));
        }

        public static bool HasDashboard(IPackageVersion version)
        {
            return !string.IsNullOrEmpty(EditorGameServiceExtension.GetDashboardUrl(version));
        }
    }
}
