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
        private List<UIError> m_Errors;
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
        [NonSerialized]
        private AssetStoreUtils m_AssetStoreUtils;
        [NonSerialized]
        private IOProxy m_IOProxy;
        public void ResolveDependencies(AssetStoreUtils assetStoreUtils, IOProxy ioProxy)
        {
            m_AssetStoreUtils = assetStoreUtils;
            m_IOProxy = ioProxy;
        }

        public override string author => m_Author;

        public override string authorLink => $"{m_AssetStoreUtils.assetStoreUrl}/publishers/{m_PublisherId}";

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
                        var url = $"{m_AssetStoreUtils.assetStoreUrl}{parentCategory}{lower}";
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

        public override IEnumerable<UIError> errors => m_Errors;

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
            m_IsAvailableOnDisk = !string.IsNullOrEmpty(m_LocalPath) && m_IOProxy.FileExists(m_LocalPath);
        }

        public AssetStorePackageVersion(AssetStoreUtils assetStoreUtils, IOProxy ioProxy, AssetStoreProductInfo productInfo, AssetStoreLocalInfo localInfo = null)
        {
            if (productInfo == null)
                throw new ArgumentNullException(nameof(productInfo));

            ResolveDependencies(assetStoreUtils, ioProxy);

            m_Errors = new List<UIError>();
            m_Tag = PackageTag.Downloadable | PackageTag.Importable;
            m_PackageUniqueId = productInfo.id;

            m_Description = productInfo.description;
            m_Author = productInfo.author;
            m_PublisherId = productInfo.publisherId;

            m_Category = productInfo.category;

            m_PublishNotes = localInfo?.publishNotes ?? productInfo.publishNotes ?? string.Empty;

            m_VersionString = localInfo?.versionString ?? productInfo.versionString ?? string.Empty;
            m_VersionId = localInfo?.versionId ?? productInfo.versionId ?? string.Empty;
            SemVersionParser.TryParse(m_VersionString.Trim(), out m_Version);

            var publishDateString = localInfo?.publishedDate ?? productInfo.publishedDate ?? string.Empty;
            m_PublishedDateTicks = !string.IsNullOrEmpty(publishDateString) ? DateTime.Parse(publishDateString).Ticks : 0;
            m_DisplayName = !string.IsNullOrEmpty(productInfo.displayName) ? productInfo.displayName : $"Package {m_PackageUniqueId}@{m_VersionId}";

            m_SupportedUnityVersions = new List<SemVersion>();
            if (localInfo != null)
            {
                var simpleVersion = Regex.Replace(localInfo.supportedVersion, @"(?<major>\d+)\.(?<minor>\d+).(?<patch>\d+)[abfp].+", "${major}.${minor}.${patch}");
                SemVersionParser.TryParse(simpleVersion.Trim(), out m_SupportedUnityVersion);
                m_SupportedUnityVersionString = m_SupportedUnityVersion?.ToString();
            }
            else if (productInfo.supportedVersions?.Any() ?? false)
            {
                foreach (var supportedVersion in productInfo.supportedVersions)
                {
                    SemVersion? version;
                    bool isVersionParsed = SemVersionParser.TryParse(supportedVersion, out version);

                    if (isVersionParsed)
                        m_SupportedUnityVersions.Add((SemVersion)version);
                }

                m_SupportedUnityVersions.Sort((left, right) => (left).CompareTo(right));
                m_SupportedUnityVersion = m_SupportedUnityVersions.LastOrDefault();
                m_SupportedUnityVersionString = m_SupportedUnityVersion?.ToString();
            }

            m_SizeInfos = new List<PackageSizeInfo>(productInfo.sizeInfos);
            m_SizeInfos.Sort((left, right) => left.supportedUnityVersion.CompareTo(right.supportedUnityVersion));

            var state = productInfo.state ?? string.Empty;
            if (state.Equals("published", StringComparison.InvariantCultureIgnoreCase))
                m_Tag |= PackageTag.Published;
            else if (state.Equals("deprecated", StringComparison.InvariantCultureIgnoreCase))
                m_Tag |= PackageTag.Deprecated;

            SetLocalPath(localInfo?.packagePath);
        }

        public void SetUpmPackageFetchError(UIError error)
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
