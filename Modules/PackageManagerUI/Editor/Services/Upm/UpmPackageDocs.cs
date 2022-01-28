// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
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

        public static string[] FetchUrlsFromDescription(UpmPackageVersion version)
        {
            List<string> urls = new List<string>();

            var descriptionSlitWithUrl = version.packageInfo.description.Split(new[] { $"{k_BuiltinPackageDocsUrlKey}https://docs.unity3d.com/" }, StringSplitOptions.None);
            if (descriptionSlitWithUrl.Length > 1)
                urls.Add($"https://docs.unity3d.com/{ApplicationUtil.instance.shortUnityVersion}/Documentation/" + descriptionSlitWithUrl[1]);

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

        private static string GetOfflineDocumentationUrl(UpmPackageVersion version)
        {
            if (version?.isAvailableOnDisk ?? false)
            {
                var docsFolder = Path.Combine(version.packageInfo.resolvedPath, "Documentation~");
                if (!Directory.Exists(docsFolder))
                    docsFolder = Path.Combine(version.packageInfo.resolvedPath, "Documentation");
                if (Directory.Exists(docsFolder))
                {
                    var mdFiles = Directory.GetFiles(docsFolder, "*.md", SearchOption.TopDirectoryOnly);
                    var docsMd = mdFiles.FirstOrDefault(d => Path.GetFileName(d).ToLower() == "index.md")
                        ?? mdFiles.FirstOrDefault(d => Path.GetFileName(d).ToLower() == "tableofcontents.md") ?? mdFiles.FirstOrDefault();
                    if (!string.IsNullOrEmpty(docsMd))
                        return new Uri(docsMd).AbsoluteUri;
                }
            }
            return string.Empty;
        }

        public static string[] GetDocumentationUrl(IPackageVersion version, bool offline = false)
        {
            var upmVersion = version as UpmPackageVersion;
            if (upmVersion == null)
                return new string[0];

            if (offline)
                return new[] { GetOfflineDocumentationUrl(upmVersion) };

            if (upmVersion.HasTag(PackageTag.BuiltIn) && !string.IsNullOrEmpty(upmVersion.description))
                return FetchUrlsFromDescription(upmVersion);

            return new[] { $"https://docs.unity3d.com/Packages/{upmVersion.shortVersionId}/index.html" };
        }

        public static string[] GetChangelogUrl(IPackageVersion version, bool offline = false)
        {
            var upmVersion = version as UpmPackageVersion;
            if (upmVersion == null)
                return new string[0];

            if (offline)
                return new[] { GetOfflineChangelogUrl(upmVersion) };

            return new[] { $"http://docs.unity3d.com/Packages/{upmVersion.shortVersionId}/changelog/CHANGELOG.html" };
        }

        private static string GetOfflineChangelogUrl(UpmPackageVersion version)
        {
            if (version?.isAvailableOnDisk ?? false)
            {
                var changelogFile = Path.Combine(version.packageInfo.resolvedPath, "CHANGELOG.md");
                return File.Exists(changelogFile) ? new Uri(changelogFile).AbsoluteUri : string.Empty;
            }
            return string.Empty;
        }

        public static string[] GetLicensesUrl(IPackageVersion version, bool offline = false)
        {
            var upmVersion = version as UpmPackageVersion;
            if (upmVersion == null)
                return new string[0];

            if (offline)
                return new[] { GetOfflineLicensesUrl(upmVersion) };

            string url;
            if (!string.IsNullOrEmpty(GetPackageUrlRedirect(upmVersion)))
                url = "https://unity3d.com/legal/licenses/Unity_Companion_License";
            else
                url = $"http://docs.unity3d.com/Packages/{upmVersion.shortVersionId}/license/index.html";
            return new[] { url };
        }

        private static string GetOfflineLicensesUrl(UpmPackageVersion version)
        {
            if (version?.isAvailableOnDisk ?? false)
            {
                var licenseFile = Path.Combine(version.packageInfo.resolvedPath, "LICENSE.md");
                return File.Exists(licenseFile) ? new Uri(licenseFile).AbsoluteUri : string.Empty;
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
