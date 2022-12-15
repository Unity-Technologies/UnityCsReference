// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Scripting.ScriptCompilation;

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

        public static string[] SplitBuiltinDescription(PackageInfo packageInfo)
        {
            if (string.IsNullOrEmpty(packageInfo?.description))
                return new string[] { string.Format(L10n.Tr("This built in package controls the presence of the {0} module."), packageInfo.displayName) };
            else
                return packageInfo.description.Split(new[] { k_BuiltinPackageDocsUrlKey }, StringSplitOptions.None);
        }

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
            return string.IsNullOrEmpty(packageInfo?.description) ?
                string.Format(L10n.Tr("This built in package controls the presence of the {0} module."), packageInfo.displayName) :
                packageInfo.description.Split(new[] { k_BuiltinPackageDocsUrlKey }, StringSplitOptions.None)[0];
        }

        private static string GetOfflineDocumentation(IOProxy IOProxy, PackageInfo packageInfo)
        {
            if (!string.IsNullOrEmpty(packageInfo?.resolvedPath))
            {
                try
                {
                    var docsFolder = IOProxy.PathsCombine(packageInfo.resolvedPath, "Documentation~");
                    if (!IOProxy.DirectoryExists(docsFolder))
                        docsFolder = IOProxy.PathsCombine(packageInfo.resolvedPath, "Documentation");
                    if (!IOProxy.DirectoryExists(docsFolder))
                    {
                        var readMeFile = IOProxy.PathsCombine(packageInfo.resolvedPath, "README.md");
                        return IOProxy.FileExists(readMeFile) ? readMeFile : string.Empty;
                    }
                    else
                    {
                        var mdFiles = IOProxy.DirectoryGetFiles(docsFolder, "*.md", System.IO.SearchOption.TopDirectoryOnly);
                        var docsMd = mdFiles.FirstOrDefault(d => IOProxy.GetFileName(d).ToLower() == "index.md")
                            ?? mdFiles.FirstOrDefault(d => IOProxy.GetFileName(d).ToLower() == "tableofcontents.md") ?? mdFiles.FirstOrDefault();
                        if (!string.IsNullOrEmpty(docsMd))
                            return docsMd;
                    }
                }
                catch (System.IO.IOException e)
                {
                    Debug.Log($"[Package Manager] Cannot get offline documentation: {e.Message}");
                    return string.Empty;
                }
            }
            return string.Empty;
        }

        private static string GetShortVersionId(PackageInfo packageInfo)
        {
            if (string.IsNullOrEmpty(packageInfo?.version))
                return string.Empty;
            SemVersionParser.TryParse(packageInfo.version, out var semVer);
            return semVer == null ? string.Empty : UpmPackageVersion.FormatPackageId(packageInfo.name, semVer.Value.ShortVersion());
        }

        public static string[] GetDocumentationUrl(IOProxy IOProxy, PackageInfo packageInfo, bool offline = false, bool isUnityPackage = true)
        {
            if (packageInfo == null)
                return new string[0];

            if (offline)
                return new string[] { GetOfflineDocumentation(IOProxy, packageInfo) };

            if (!string.IsNullOrEmpty(packageInfo?.documentationUrl))
                return new string[] { packageInfo.documentationUrl };

            if (IsBuiltIn(packageInfo) && !string.IsNullOrEmpty(packageInfo.description))
                return FetchUrlsFromDescription(packageInfo);

            if (!isUnityPackage)
                return new string[0];

            return new string[] { $"https://docs.unity3d.com/Packages/{GetShortVersionId(packageInfo)}/index.html" };
        }

        public static string GetChangelogUrl(IOProxy IOProxy, PackageInfo packageInfo, bool offline = false, bool isUnityPackage = true)
        {
            if (packageInfo == null)
                return string.Empty;

            if (offline)
                return GetOfflineChangelog(IOProxy, packageInfo);

            if (!string.IsNullOrEmpty(packageInfo?.changelogUrl))
                return packageInfo.changelogUrl;

            if (!isUnityPackage)
                return string.Empty;

            return $"http://docs.unity3d.com/Packages/{GetShortVersionId(packageInfo)}/changelog/CHANGELOG.html";
        }

        private static string GetOfflineChangelog(IOProxy IOProxy, PackageInfo packageInfo)
        {
            if (!string.IsNullOrEmpty(packageInfo?.resolvedPath))
            {
                try
                {
                    var changelogFile = IOProxy.PathsCombine(packageInfo.resolvedPath, "CHANGELOG.md");
                    return IOProxy.FileExists(changelogFile) ? changelogFile : string.Empty;
                }
                catch (System.IO.IOException e)
                {
                    Debug.Log($"[Package Manager] Cannot get offline change log: {e.Message}");
                }
            }
            return string.Empty;
        }

        public static string GetLicensesUrl(IOProxy IOProxy, PackageInfo packageInfo, bool offline = false, bool isUnityPackage = true)
        {
            if (packageInfo == null)
                return string.Empty;

            if (offline)
                return GetOfflineLicenses(IOProxy, packageInfo);

            if (!string.IsNullOrEmpty(packageInfo?.licensesUrl))
                return packageInfo.licensesUrl;

            if (!isUnityPackage)
                return string.Empty;

            return $"http://docs.unity3d.com/Packages/{GetShortVersionId(packageInfo)}/license/index.html";
        }

        private static string GetOfflineLicenses(IOProxy IOProxy, PackageInfo packageInfo)
        {
            if (!string.IsNullOrEmpty(packageInfo?.resolvedPath))
            {
                try
                {
                    var licenseFile = IOProxy.PathsCombine(packageInfo.resolvedPath, "LICENSE.md");
                    return IOProxy.FileExists(licenseFile) ? licenseFile : string.Empty;
                }
                catch (System.IO.IOException e)
                {
                    Debug.Log($"[Package Manager] Cannot get offline licenses: {e.Message}");
                }
            }
            return string.Empty;
        }

        public static bool HasDocs(PackageInfo packageInfo)
        {
            return packageInfo != null;
        }

        public static bool HasChangelog(PackageInfo packageInfo)
        {
            if (packageInfo == null)
                return false;
            return !string.IsNullOrEmpty(packageInfo.changelogUrl) || !IsBuiltIn(packageInfo);
        }

        public static bool HasLicenses(PackageInfo packageInfo)
        {
            if (packageInfo == null)
                return false;
            return !string.IsNullOrEmpty(packageInfo.licensesUrl) || !IsBuiltIn(packageInfo);
        }

        private static bool IsBuiltIn(PackageInfo packageInfo)
        {
            return packageInfo.source == PackageSource.BuiltIn && packageInfo.type == "module";
        }
    }
}
