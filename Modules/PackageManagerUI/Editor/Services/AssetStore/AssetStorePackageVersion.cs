// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class AssetStorePackageVersion : BasePackageVersion, ISerializationCallbackReceiver
    {
        [SerializeField]
        private string m_Author;
        [SerializeField]
        private string m_Category;
        [SerializeField]
        private List<Error> m_Errors;
        [SerializeField]
        private string m_PublisherId;
        [SerializeField]
        private bool m_IsAvailableOnDisk;
        [SerializeField]
        private string m_LocalPath;
        [SerializeField]
        private string m_VersionId;
        [SerializeField]
        private List<SemVersion> m_SupportedUnityVersions;

        [SerializeField]
        private string m_SupportedUnityVersionString;
        private SemVersion? m_SupportedUnityVersion;

        [SerializeField]
        private List<PackageSizeInfo> m_SizeInfos;

        public override string author => m_Author;

        public override string authorLink => $"{AssetStoreUtils.instance.assetStoreUrl}/publishers/{m_PublisherId}";

        public override string category => m_Category;

        private Dictionary<string, string> m_CategoryLinks;
        public override IDictionary<string, string> categoryLinks
        {
            get
            {
                if (m_CategoryLinks == null)
                {
                    m_CategoryLinks = new Dictionary<string, string>();
                    var categories = m_Category.Split('/');
                    var parentCategory = "/";
                    foreach (var category in categories)
                    {
                        var lower = category.ToLower(CultureInfo.InvariantCulture);
                        var url = $"{AssetStoreUtils.instance.assetStoreUrl}{parentCategory}{lower}";
                        parentCategory += lower + "/";
                        m_CategoryLinks[category] = url;
                    }
                }
                return m_CategoryLinks;
            }
        }

        public override string uniqueId => $"{m_PackageUniqueId}@{m_VersionId}";

        public override bool isInstalled => false;

        public override bool isFullyFetched => true;

        public override IEnumerable<Error> errors => m_Errors;

        public override bool isAvailableOnDisk => m_IsAvailableOnDisk;

        public override bool isDirectDependency => true;

        public override string localPath => m_LocalPath;

        public override string versionString => m_VersionString;

        public override string versionId => m_VersionId;

        public override SemVersion? supportedVersion => m_SupportedUnityVersion;

        public override IEnumerable<SemVersion> supportedVersions => m_SupportedUnityVersions;

        public override IEnumerable<PackageSizeInfo> sizes => m_SizeInfos;

        public void SetLocalPath(string path)
        {
            m_LocalPath = path ?? string.Empty;
            m_IsAvailableOnDisk = !string.IsNullOrEmpty(m_LocalPath) && File.Exists(m_LocalPath);
        }

        public AssetStorePackageVersion(AssetStoreFetchedInfo fetchedInfo, AssetStoreLocalInfo localInfo = null)
        {
            if (fetchedInfo == null)
                throw new ArgumentNullException(nameof(fetchedInfo));

            m_Errors = new List<Error>();
            m_Tag = PackageTag.Downloadable | PackageTag.Importable;
            m_PackageUniqueId = fetchedInfo.id;

            m_Description = fetchedInfo.description;
            m_Author = fetchedInfo.author;
            m_PublisherId = fetchedInfo.publisherId;

            m_Category = fetchedInfo.category;

            m_VersionString = localInfo?.versionString ?? fetchedInfo.versionString ?? string.Empty;
            m_VersionId = localInfo?.versionId ?? fetchedInfo.versionId ?? string.Empty;
            SemVersionParser.TryParse(m_VersionString.Trim(), out m_Version);

            var publishDateString = localInfo?.publishedDate ?? fetchedInfo.publishedDate ?? string.Empty;
            m_PublishedDateTicks = !string.IsNullOrEmpty(publishDateString) ? DateTime.Parse(publishDateString).Ticks : 0;
            m_DisplayName = !string.IsNullOrEmpty(fetchedInfo.displayName) ? fetchedInfo.displayName : $"Package {m_PackageUniqueId}@{m_VersionId}";

            m_SupportedUnityVersions = new List<SemVersion>();
            if (localInfo != null)
            {
                var simpleVersion = Regex.Replace(localInfo.supportedVersion, @"(?<major>\d+)\.(?<minor>\d+).(?<patch>\d+)[abfp].+", "${major}.${minor}.${patch}");
                SemVersionParser.TryParse(simpleVersion.Trim(), out m_SupportedUnityVersion);
                m_SupportedUnityVersionString = m_SupportedUnityVersion?.ToString();
            }
            else if (fetchedInfo.supportedVersions?.Any() ?? false)
            {
                foreach (var supportedVersion in fetchedInfo.supportedVersions)
                {
                    SemVersion? version;
                    bool isVersionParsed = SemVersionParser.TryParse(supportedVersion as string, out version);

                    if (isVersionParsed)
                        m_SupportedUnityVersions.Add((SemVersion)version);
                }

                m_SupportedUnityVersions.Sort((left, right) => (left).CompareTo(right));
                m_SupportedUnityVersion = m_SupportedUnityVersions.LastOrDefault();
                m_SupportedUnityVersionString = m_SupportedUnityVersion?.ToString();
            }

            m_SizeInfos = new List<PackageSizeInfo>(fetchedInfo.sizeInfos);
            m_SizeInfos.Sort((left, right) => left.supportedUnityVersion.CompareTo(right.supportedUnityVersion));

            var state = fetchedInfo.state ?? string.Empty;
            if (state.Equals("published", StringComparison.InvariantCultureIgnoreCase))
                m_Tag |= PackageTag.Published;
            else if (state.Equals("deprecated", StringComparison.InvariantCultureIgnoreCase))
                m_Tag |= PackageTag.Deprecated;

            SetLocalPath(localInfo?.packagePath);
        }

        public void SetUpmPackageFetchError(Error error)
        {
            m_Errors.Add(error);
            m_Tag &= ~(PackageTag.Downloadable | PackageTag.Importable);
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            SemVersionParser.TryParse(m_SupportedUnityVersionString, out m_SupportedUnityVersion);
        }
    }
}
