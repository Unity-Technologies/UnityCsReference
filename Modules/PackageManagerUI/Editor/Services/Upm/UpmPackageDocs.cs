// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Scripting.ScriptCompilation;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class UpmPackageDocs
    {
        // Module package.json files contain a documentation url embedded in the description.
        // We parse that to have the "View Documentation" button direct to it, instead of showing
        // the link in the description text.
        internal const string k_BuiltinPackageDocsUrlKey = "Scripting API: ";

        public static string[] FetchUrlsFromDescription(PackageInfo packageInfo)
        {
            var applicationProxy = ServicesContainer.instance.Resolve<ApplicationProxy>();
            List<string> urls = new List<string>();

            var descriptionSlitWithUrl = packageInfo.description.Split(new[] { $"{k_BuiltinPackageDocsUrlKey}https://docs.unity3d.com/" }, StringSplitOptions.None);
            if (descriptionSlitWithUrl.Length > 1)
                urls.Add($"https://docs.unity3d.com/{applicationProxy.shortUnityVersion}/Documentation/" + descriptionSlitWithUrl[1]);

            var descriptionSlitWithoutUrl = packageInfo.description.Split(new[] { k_BuiltinPackageDocsUrlKey }, StringSplitOptions.None);
            if (descriptionSlitWithoutUrl.Length > 1)
                urls.Add(descriptionSlitWithoutUrl[1]);

            return urls.ToArray();
        }

        public static string FetchBuiltinDescription(PackageInfo packageInfo)
        {
            return string.IsNullOrEmpty(packageInfo.description) ?
                string.Format(L10n.Tr("This built in package controls the presence of the {0} module."), packageInfo.displayName) :
                packageInfo.description.Split(new[] { k_BuiltinPackageDocsUrlKey }, StringSplitOptions.None)[0];
        }

        public static string GetOfflineDocumentation(IOProxy IOProxy, PackageInfo packageInfo)
        {
            if (!string.IsNullOrEmpty(packageInfo?.resolvedPath))
            {
                try
                {
                    var docsFolder = IOProxy.PathsCombine(packageInfo.resolvedPath, "Documentation~");
                    if (!IOProxy.DirectoryExists(docsFolder))
                        docsFolder = IOProxy.PathsCombine(packageInfo.resolvedPath, "Documentation");
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

        public static string GetShortVersionId(PackageInfo packageInfo)
        {
            if (string.IsNullOrEmpty(packageInfo?.version))
                return string.Empty;
            SemVersionParser.TryParse(packageInfo.version, out var semVer);
            return semVer == null ? string.Empty : UpmPackageVersion.FormatPackageId(packageInfo.name, semVer.Value.ShortVersion());
        }

        public static string[] GetDocumentationUrl(PackageInfo packageInfo)
        {
            if (!string.IsNullOrEmpty(packageInfo?.documentationUrl))
                return new string[] { packageInfo.documentationUrl };

            if (IsBuiltIn(packageInfo) && !string.IsNullOrEmpty(packageInfo.description))
                return FetchUrlsFromDescription(packageInfo);

            var shortVersionId = GetShortVersionId(packageInfo);
            if (string.IsNullOrEmpty(shortVersionId))
                return new string[0];
            return new string[] { $"https://docs.unity3d.com/Packages/{shortVersionId}/index.html" };
        }

        public static string GetChangelogUrl(PackageInfo packageInfo)
        {
            if (!string.IsNullOrEmpty(packageInfo?.changelogUrl))
                return packageInfo.changelogUrl;

            var shortVersionId = GetShortVersionId(packageInfo);
            if (string.IsNullOrEmpty(shortVersionId))
                return string.Empty;
            return $"https://docs.unity3d.com/Packages/{shortVersionId}/changelog/CHANGELOG.html";
        }

        public static string GetOfflineChangelog(IOProxy IOProxy, PackageInfo packageInfo)
        {
            if (!string.IsNullOrEmpty(packageInfo?.resolvedPath))
            {
                try
                {
                    var changelogFile = IOProxy.PathsCombine(packageInfo.resolvedPath, "CHANGELOG.md");
                    return IOProxy.FileExists(changelogFile) ? changelogFile : string.Empty;
                }
                catch (System.IO.IOException) {}
            }
            return string.Empty;
        }

        public static string GetLicensesUrl(PackageInfo packageInfo)
        {
            if (!string.IsNullOrEmpty(packageInfo?.licensesUrl))
                return packageInfo.licensesUrl;

            var shortVersionId = GetShortVersionId(packageInfo);
            if (string.IsNullOrEmpty(shortVersionId))
                return string.Empty;
            return $"https://docs.unity3d.com/Packages/{shortVersionId}/license/index.html";
        }

        public static string GetOfflineLicenses(IOProxy IOProxy, PackageInfo packageInfo)
        {
            if (!string.IsNullOrEmpty(packageInfo?.resolvedPath))
            {
                try
                {
                    var licenseFile = IOProxy.PathsCombine(packageInfo.resolvedPath, "LICENSE.md");
                    return IOProxy.FileExists(licenseFile) ? licenseFile : string.Empty;
                }
                catch (System.IO.IOException) {}
            }
            return string.Empty;
        }

        public static bool HasDocs(PackageInfo packageInfo)
        {
            if (packageInfo == null)
                return false;
            return !string.IsNullOrEmpty(packageInfo.documentationUrl) || !IsFeature(packageInfo);
        }

        public static bool HasChangelog(PackageInfo packageInfo)
        {
            if (packageInfo == null)
                return false;
            return !string.IsNullOrEmpty(packageInfo.changelogUrl) || !IsBuiltInOrFeature(packageInfo);
        }

        public static bool HasLicenses(PackageInfo packageInfo)
        {
            if (packageInfo == null)
                return false;
            return !string.IsNullOrEmpty(packageInfo.licensesUrl) || !IsBuiltInOrFeature(packageInfo);
        }

        private static bool IsBuiltIn(PackageInfo packageInfo)
        {
            return packageInfo.source == PackageSource.BuiltIn && packageInfo.type == "module";
        }

        private static bool IsFeature(PackageInfo packageInfo)
        {
            return packageInfo.source == PackageSource.BuiltIn && packageInfo.type == "feature";
        }

        private static bool IsBuiltInOrFeature(PackageInfo packageInfo)
        {
            return packageInfo.source == PackageSource.BuiltIn && (packageInfo.type == "module" || packageInfo.type == "feature");
        }
    }
}
