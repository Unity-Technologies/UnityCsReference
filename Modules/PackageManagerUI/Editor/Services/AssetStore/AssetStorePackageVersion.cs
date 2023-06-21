// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class AssetStorePackageVersion : BasePackageVersion
    {
        [SerializeField]
        private string m_Category;
        [SerializeField]
        private List<UIError> m_Errors;
        [SerializeField]
        private string m_LocalPath;
        [SerializeField]
        private long m_UploadId;
        [SerializeField]
        private List<SemVersion> m_SupportedUnityVersions;
        [SerializeField]
        private AssetStoreImportedPackage m_ImportedPackage;

        [SerializeField]
        private string m_SupportedUnityVersionString;
        private SemVersion? m_SupportedUnityVersion;

        [SerializeField]
        private List<PackageSizeInfo> m_SizeInfos;

        // We want to distinguish version.author to the package.publisherName since publisher name is related to a product
        // but author here refers to the author data in the PackageInfo, which is empty for an asset store package version
        public override string author => string.Empty;

        public override string category => m_Category;

        public override string packageId => string.Empty;

        public override string uniqueId => $"{package.uniqueId}@{uploadId}";

        public override bool isInstalled => false;

        public override bool isFullyFetched => true;

        public override IEnumerable<UIError> errors => m_Errors;

        public override bool isDirectDependency => true;

        public override string localPath => m_LocalPath;

        public override string versionString => m_VersionString;

        public override long uploadId => m_UploadId;

        public override SemVersion? supportedVersion => m_SupportedUnityVersion;

        public override IEnumerable<SemVersion> supportedVersions => m_SupportedUnityVersions;

        public override IEnumerable<PackageSizeInfo> sizes => m_SizeInfos;

        public override IEnumerable<Asset> importedAssets => m_ImportedPackage;

        public AssetStorePackageVersion(AssetStoreProductInfo productInfo, long uploadId = 0, AssetStoreLocalInfo localInfo = null, AssetStoreImportedPackage importedPackage = null)
        {
            m_Errors = new List<UIError>();
            m_Tag = PackageTag.LegacyFormat;

            // m_Description is the version level description from PackageInfo, so we set this field to empty here deliberately
            // For asset store packages, we have the `productDescription` at the package level.
            m_Description = string.Empty;

            m_Category = productInfo?.category ?? string.Empty;

            m_PublishNotes = localInfo?.publishNotes ?? string.Empty;

            m_VersionString = importedPackage?.versionString ?? localInfo?.versionString ?? productInfo?.versionString ?? string.Empty;
            m_UploadId = uploadId;
            SemVersionParser.TryParse(m_VersionString.Trim(), out m_Version);

            m_ImportedPackage = importedPackage;

            var publishDateString = localInfo?.publishedDate ?? productInfo?.publishedDate ?? string.Empty;
            m_PublishedDateTicks = !string.IsNullOrEmpty(publishDateString) ? DateTime.Parse(publishDateString).Ticks : 0;
            m_DisplayName = !string.IsNullOrEmpty(productInfo?.displayName) ? productInfo.displayName : importedPackage?.displayName ?? string.Empty;

            m_SupportedUnityVersions = new List<SemVersion>();
            if (!string.IsNullOrEmpty(localInfo?.supportedVersion))
            {
                var simpleVersion = Regex.Replace(localInfo.supportedVersion, @"(?<major>\d+)\.(?<minor>\d+).(?<patch>\d+)[abfp].+", "${major}.${minor}.${patch}");
                SemVersionParser.TryParse(simpleVersion.Trim(), out m_SupportedUnityVersion);
                m_SupportedUnityVersionString = m_SupportedUnityVersion?.ToString();
            }
            else if (productInfo?.supportedVersions?.Any() ?? false)
            {
                foreach (var v in productInfo.supportedVersions)
                    if (SemVersionParser.TryParse(v, out var parsedSemVer))
                        m_SupportedUnityVersions.Add(parsedSemVer.Value);

                m_SupportedUnityVersions.Sort((left, right) => left.CompareTo(right));
                m_SupportedUnityVersion = m_SupportedUnityVersions.LastOrDefault();
                m_SupportedUnityVersionString = m_SupportedUnityVersion?.ToString();
            }

            m_SizeInfos = new List<PackageSizeInfo>(productInfo?.sizeInfos ?? Enumerable.Empty<PackageSizeInfo>());
            m_SizeInfos.Sort((left, right) => left.supportedUnityVersion.CompareTo(right.supportedUnityVersion));

            var state = productInfo?.state ?? string.Empty;
            if (state.Equals("published", StringComparison.InvariantCultureIgnoreCase))
                m_Tag |= PackageTag.Published;
            else if (state.Equals("disabled", StringComparison.InvariantCultureIgnoreCase))
                m_Tag |= PackageTag.Disabled;

            m_LocalPath = localInfo?.packagePath ?? string.Empty;
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            SemVersionParser.TryParse(m_SupportedUnityVersionString, out m_SupportedUnityVersion);
        }
    }
}
