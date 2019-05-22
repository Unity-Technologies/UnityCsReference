// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class PackageInfo : IEquatable<PackageInfo>, ISerializationCallbackReceiver
    {
        // Module package.json files contain a documentation url embedded in the description.
        // We parse that to have the "View Documentation" button direct to it, instead of showing
        // the link in the description text.
        private const string builtinPackageDocsUrlKey = "Scripting API: ";

        public string Name;
        public string DisplayName;
        private string _PackageId;
        [SerializeField]
        private string _Version; // Required cause SemVersion is not Serializable
        public SemVersion Version;
        public string Description;
        public string Category;
        public PackageState State;
        public bool IsInstalledByDependency;
        public bool IsInstalledByUpm;
        public bool IsLatest;
        public string Group;
        public string Type;
        public PackageSource Origin;
        public List<Error> Errors;
        public bool IsVerified;
        public string Author;
        public bool IsDiscoverable;        // Packages found from search are discoverable

        public List<Sample> Samples;

        // Full fetch is when the information this package has isn't derived from another version
        // (and therefore may be slightly wrong)
        public bool HasFullFetch;

        public PackageManager.PackageInfo Info;

        public bool IsInstalled
        {
            get { return IsInstalledByUpm || IsInstalledByDependency; }
            set { IsInstalledByUpm = value; }
        }

        public static string ModulePrefix { get { return string.Format("{0}modules.", UnityPrefix); } }
        public static string UnityPrefix { get { return "com.unity."; } }

        public static string FormatPackageId(string name, string version)
        {
            return string.Format("{0}@{1}", name.ToLower(), version);
        }

        public static string FormatPackageId(DependencyInfo dependencyInfo)
        {
            return FormatPackageId(dependencyInfo.name, dependencyInfo.version);
        }

        public string PackageId
        {
            get
            {
                if (!string.IsNullOrEmpty(_PackageId))
                    return _PackageId;
                return FormatPackageId(Name, Version.ToString());
            }
            set
            {
                _PackageId = value;
            }
        }

        // This will always be <name>@<version>, even for an embedded package.
        public string VersionId { get { return FormatPackageId(Name, Version.ToString()); } }
        public string ShortVersionId { get { return FormatPackageId(Name, Version.ShortVersion()); } }

        public string BuiltInDescription
        {
            get
            {
                if (string.IsNullOrEmpty(Description))
                    return string.Format("This built in package controls the presence of the {0} module.", DisplayName);
                else
                    return Description.Split(new[] {builtinPackageDocsUrlKey}, StringSplitOptions.None)[0];
            }
        }

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

        // Method content must be matched in package manager UI
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

        public bool RedirectsToManual
        {
            get { return !string.IsNullOrEmpty(GetPackageUrlRedirect(Name, ShortVersionId)); }
        }

        public virtual bool IsDirectDependency
        {
            get { return Info?.isDirectDependency ?? false; }
            internal set { throw new NotImplementedException(); }
        }

        public bool HasChangelog
        {
            // Packages with no docs have no third party notice
            get { return !RedirectsToManual && !IsBuiltIn; }
        }

        public string GetDocumentationUrl(bool offline = false)
        {
            if (offline)
                return GetOfflineDocumentationUrl();

            if (IsBuiltIn)
            {
                if (!string.IsNullOrEmpty(Description))
                {
                    var split = Description.Split(new[] {builtinPackageDocsUrlKey}, StringSplitOptions.None);
                    if (split.Length > 1)
                        return split[1];
                }
            }
            return string.Format("http://docs.unity3d.com/Packages/{0}/index.html", ShortVersionId);
        }

        private string GetOfflineDocumentationUrl()
        {
            if (!IsAvailableOffline)
                return string.Empty;
            var docsFolder = Path.Combine(Info.resolvedPath, "Documentation~");
            if (!Directory.Exists(docsFolder))
                docsFolder = Path.Combine(Info.resolvedPath, "Documentation");
            if (Directory.Exists(docsFolder))
            {
                var docsMd = Directory.GetFiles(docsFolder, "*.md", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (!string.IsNullOrEmpty(docsMd))
                    return new Uri(docsMd).AbsoluteUri;
            }
            return string.Empty;
        }

        public string AssetPath { get { return Info != null ? Info.assetPath : ""; } }

        public UnityEngine.Object PackageManifestAsset
        {
            get
            {
                if (string.IsNullOrEmpty(AssetPath))
                    return null;

                var path = Path.Combine(AssetPath, "package.json");
                return AssetDatabase.LoadMainAssetAtPath(path);
            }
        }

        public void SelectPackageManifestAsset()
        {
            var asset = PackageManifestAsset;
            if (asset == null)
            {
                Debug.LogWarning("Could not find package.json asset for package: " + DisplayName);
                return;
            }

            UnityEditor.Selection.activeObject = asset;
        }

        public string GetChangelogUrl(bool offline = false)
        {
            if (offline)
                return GetOfflineChangelogUrl();

            return string.Format("http://docs.unity3d.com/Packages/{0}/changelog/CHANGELOG.html", ShortVersionId);
        }

        private string GetOfflineChangelogUrl()
        {
            if (!IsAvailableOffline)
                return string.Empty;
            var changelogFile = Path.Combine(Info.resolvedPath, "CHANGELOG.md");
            return File.Exists(changelogFile) ? new Uri(changelogFile).AbsoluteUri : string.Empty;
        }

        public string GetLicensesUrl(bool offline = false)
        {
            if (offline)
                return GetOfflineLicensesUrl();

            var url = string.Format("http://docs.unity3d.com/Packages/{0}/license/index.html", ShortVersionId);
            if (RedirectsToManual)
                url = "https://unity3d.com/legal/licenses/Unity_Companion_License";
            return url;
        }

        private string GetOfflineLicensesUrl()
        {
            if (!IsAvailableOffline)
                return string.Empty;
            var licenseFile = Path.Combine(Info.resolvedPath, "LICENSE.md");
            return File.Exists(licenseFile) ? new Uri(licenseFile).AbsoluteUri : string.Empty;
        }

        public bool Equals(PackageInfo other)
        {
            if (other == null)
                return false;
            if (other == this)
                return true;

            return Name == other.Name && Version == other.Version;
        }

        public override int GetHashCode()
        {
            return PackageId.GetHashCode();
        }

        public bool HasVersionTag(string tag)
        {
            if (string.IsNullOrEmpty(Version.Prerelease))
                return false;

            return String.Equals(Version.Prerelease.Split('.').First(), tag, StringComparison.CurrentCultureIgnoreCase);
        }

        public bool HasVersionTag(PackageTag tag)
        {
            return HasVersionTag(tag.ToString());
        }

        // Is it a pre-release (alpha/beta/experimental/preview)?
        //        Current logic is any tag is considered pre-release, except recommended and core
        public bool IsPreRelease
        {
            get { return !string.IsNullOrEmpty(Version.Prerelease) || Version.Major == 0; }
        }

        public bool IsPreview
        {
            get { return HasVersionTag(PackageTag.preview) || Version.Major == 0; }
        }

        // A version is user visible if it has a supported tag (or no tag at all)
        public bool IsUserVisible
        {
            get { return IsInstalled || string.IsNullOrEmpty(Version.Prerelease) || HasVersionTag(PackageTag.preview) || IsVerified || IsCore; }
        }

        public static bool IsPackageBuiltIn(PackageSource source, string type)
        {
            return source == PackageSource.BuiltIn && type == PackageType.module.ToString();
        }

        public bool IsInDevelopment { get { return Origin == PackageSource.Embedded; } }
        public bool IsLocal { get { return Origin == PackageSource.Local; } }
        public bool IsGit { get { return Origin == PackageSource.Git; } }
        // A builtin package is a module
        public bool IsBuiltIn { get { return IsPackageBuiltIn(Origin, Type); } }
        // A core package is built from the trunk/Packages folder
        public bool IsCore
        {
            get
            {
                return Origin == PackageSource.BuiltIn && Type != PackageType.module.ToString();
            }
        }

        public bool IsAvailableOffline
        {
            get { return !string.IsNullOrEmpty(Info.resolvedPath); }
        }

        public string VersionWithoutTag { get { return Version.VersionOnly(); } }

        public bool IsVersionLocked
        {
            get { return Origin == PackageSource.Embedded || Origin == PackageSource.Git || Origin == PackageSource.BuiltIn; }
        }

        public bool CanBeRemoved
        {
            get { return Origin == PackageSource.Registry || Origin == PackageSource.BuiltIn || Origin == PackageSource.Local || Origin == PackageSource.Git; }
        }

        public void OnBeforeSerialize()
        {
            _Version = Version.ToString();
        }

        public void OnAfterDeserialize()
        {
            Version = _Version;
        }

        public void Consolidate(PackageInfo other)
        {
            Name = other.Name;
            DisplayName = other.DisplayName;
            PackageId = other.PackageId;
            Version = other.Version;
            Description = other.Description;
            Category = other.Category;
            IsInstalled = other.IsInstalled;
            IsLatest = other.IsLatest;
            IsVerified = other.IsVerified;
            Errors = other.Errors;
            Group = other.Group;
            Type = other.Type;
            State = other.State;
            Origin = other.Origin;
            Author = other.Author;
            IsDiscoverable = other.IsDiscoverable;
            Info = other.Info;
            HasFullFetch = other.HasFullFetch;
        }

        public string StandardizedLabel(bool showSimplified = true)
        {
            if (Version == null)
                return string.Empty;

            var label = VersionWithoutTag;

            if (IsLocal && showSimplified)
                label = "local - " + label;
            if (IsInstalled && showSimplified)
                label = "current - " + label;
            if (IsVerified && showSimplified)
                label = "verified - " + label;
            else if (!string.IsNullOrEmpty(Version.Prerelease))
                label = string.Format("{0} - {1}", Version.Prerelease, label);
            return label;
        }

        public IEnumerable<DependencyInfo> DependentModules
        {
            get
            {
                if (Info == null)
                    return new List<DependencyInfo>();

                return Info.resolvedDependencies.Where(d => d.name.StartsWith(ModulePrefix));
            }
        }
    }
}
