// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Linq;

namespace UnityEditor.PackageManager.UI
{
    internal class UpmPackageDocs
    {
        // Module package.json files contain a documentation url embedded in the description.
        // We parse that to have the "View Documentation" button direct to it, instead of showing
        // the link in the description text.
        internal const string k_BuiltinPackageDocsUrlKey = "Scripting API: ";

        private static Version ParseShortVersion(string shortVersionId)
        {
            try
            {
                var versionToken = shortVersionId.Split('@')[1];
                return new Version(versionToken);
            }
            catch (Exception)
            {
                // Keep default version 0.0 on exception
                return new Version();
            }
        }

        // Method content must be matched in package manager doc tools
        public static string GetPackageUrlRedirect(string packageName, string shortVersionId)
        {
            var redirectUrl = "";
            if (packageName == "com.unity.ads")
                redirectUrl = "https://docs.unity3d.com/Manual/UnityAds.html";
            else if (packageName == "com.unity.analytics")
            {
                if (ParseShortVersion(shortVersionId) < new Version(3, 2))
                    redirectUrl = "https://docs.unity3d.com/Manual/UnityAnalytics.html";
            }
            else if (packageName == "com.unity.purchasing")
                redirectUrl = "https://docs.unity3d.com/Manual/UnityIAP.html";
            else if (packageName == "com.unity.standardevents")
                redirectUrl = "https://docs.unity3d.com/Manual/UnityAnalyticsStandardEvents.html";
            else if (packageName == "com.unity.xiaomi")
                redirectUrl = "https://unity3d.com/cn/partners/xiaomi/guide";
            else if (packageName == "com.unity.shadergraph")
            {
                if (ParseShortVersion(shortVersionId) < new Version(4, 1))
                    redirectUrl = "https://github.com/Unity-Technologies/ShaderGraph/wiki";
            }
            return redirectUrl;
        }

        public static string GetPackageUrlRedirect(IPackageVersion version)
        {
            var upmVersion = version as UpmPackageVersion;
            return upmVersion == null ? string.Empty : GetPackageUrlRedirect(upmVersion.name, upmVersion.shortVersionId);
        }

        public static string[] SplitBuiltinDescription(UpmPackageVersion version)
        {
            if (string.IsNullOrEmpty(version?.packageInfo?.description))
                return new string[] { string.Format(L10n.Tr("This built in package controls the presence of the {0} module."), version.displayName) };
            else
                return version.packageInfo.description.Split(new[] { k_BuiltinPackageDocsUrlKey }, StringSplitOptions.None);
        }

        private static string GetOfflineDocumentationUrl(IOProxy IOProxy, UpmPackageVersion version)
        {
            if (version?.isAvailableOnDisk ?? false)
            {
                var docsFolder = Path.Combine(version.packageInfo.resolvedPath, "Documentation~");
                if (!IOProxy.DirectoryExists(docsFolder))
                    docsFolder = Path.Combine(version.packageInfo.resolvedPath, "Documentation");
                if (IOProxy.DirectoryExists(docsFolder))
                {
                    var mdFiles = IOProxy.DirectoryGetFiles(docsFolder, "*.md", SearchOption.TopDirectoryOnly);
                    var docsMd = mdFiles.FirstOrDefault(d => Path.GetFileName(d).ToLower() == "index.md")
                        ?? mdFiles.FirstOrDefault(d => Path.GetFileName(d).ToLower() == "tableofcontents.md") ?? mdFiles.FirstOrDefault();
                    if (!string.IsNullOrEmpty(docsMd))
                        return new Uri(docsMd).AbsoluteUri;
                }
            }
            return string.Empty;
        }

        public static string GetDocumentationUrl(IOProxy IOProxy, IPackageVersion version, bool offline = false)
        {
            var upmVersion = version as UpmPackageVersion;
            if (upmVersion == null)
                return string.Empty;

            if (offline)
                return GetOfflineDocumentationUrl(IOProxy, upmVersion);

            if (!string.IsNullOrEmpty(upmVersion.documentationUrl))
                return upmVersion.documentationUrl;

            if (upmVersion.HasTag(PackageTag.BuiltIn) && !string.IsNullOrEmpty(upmVersion.description))
            {
                var split = SplitBuiltinDescription(upmVersion);
                if (split.Length > 1)
                    return split[1];
            }
            return $"http://docs.unity3d.com/Packages/{upmVersion.shortVersionId}/index.html";
        }

        public static string GetChangelogUrl(IOProxy IOProxy, IPackageVersion version, bool offline = false)
        {
            var upmVersion = version as UpmPackageVersion;
            if (upmVersion == null)
                return string.Empty;

            if (offline)
                return GetOfflineChangelogUrl(IOProxy, upmVersion);

            if (!string.IsNullOrEmpty(upmVersion.changelogUrl))
                return upmVersion.changelogUrl;

            return $"http://docs.unity3d.com/Packages/{upmVersion.shortVersionId}/changelog/CHANGELOG.html";
        }

        private static string GetOfflineChangelogUrl(IOProxy IOProxy, UpmPackageVersion version)
        {
            if (version?.isAvailableOnDisk ?? false)
            {
                var changelogFile = Path.Combine(version.packageInfo.resolvedPath, "CHANGELOG.md");
                return IOProxy.FileExists(changelogFile) ? new Uri(changelogFile).AbsoluteUri : string.Empty;
            }
            return string.Empty;
        }

        public static string GetLicensesUrl(IOProxy IOProxy, IPackageVersion version, bool offline = false)
        {
            var upmVersion = version as UpmPackageVersion;
            if (upmVersion == null)
                return string.Empty;

            if (offline)
                return GetOfflineLicensesUrl(IOProxy, upmVersion);

            if (!string.IsNullOrEmpty(upmVersion.licensesUrl))
                return upmVersion.licensesUrl;

            string url;
            if (!string.IsNullOrEmpty(GetPackageUrlRedirect(upmVersion)))
                url = "https://unity3d.com/legal/licenses/Unity_Companion_License";
            else
                url = $"http://docs.unity3d.com/Packages/{upmVersion.shortVersionId}/license/index.html";
            return url;
        }

        private static string GetOfflineLicensesUrl(IOProxy IOProxy, UpmPackageVersion version)
        {
            if (version?.isAvailableOnDisk ?? false)
            {
                var licenseFile = Path.Combine(version.packageInfo.resolvedPath, "LICENSE.md");
                return IOProxy.FileExists(licenseFile) ? new Uri(licenseFile).AbsoluteUri : string.Empty;
            }
            return string.Empty;
        }

        public static bool HasDocs(IPackageVersion version)
        {
            return (version as UpmPackageVersion) != null;
        }

        public static bool HasChangelog(IPackageVersion version)
        {
            return (version as UpmPackageVersion) != null && !version.HasTag(PackageTag.BuiltIn) && string.IsNullOrEmpty(GetPackageUrlRedirect(version));
        }

        public static bool HasLicenses(IPackageVersion version)
        {
            return (version as UpmPackageVersion) != null && !version.HasTag(PackageTag.BuiltIn);
        }
    }
}
