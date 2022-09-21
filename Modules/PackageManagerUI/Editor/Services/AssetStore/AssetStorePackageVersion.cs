// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class AssetStorePackageVersion : BasePackageVersion, ISerializationCallbackReceiver
    {
        public static readonly string k_IncompatibleWarningMessage = L10n.Tr("The downloaded version of this package is intended for Unity {0} and higher." +
            " This version might not work with your current version of Unity." +
            " Click Update to download a compatible version of the package.");

        [SerializeField]
        private string m_Category;
        [SerializeField]
        private List<UIError> m_Errors;
        [SerializeField]
        private bool m_IsAvailableOnDisk;
        [SerializeField]
        private string m_LocalPath;
        [SerializeField]
        private long m_VersionId;
        [SerializeField]
        private List<SemVersion> m_SupportedUnityVersions;

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

        public override string uniqueId => $"{package.uniqueId}@{versionId}";

        public override bool isInstalled => false;

        public override bool isFullyFetched => true;

        public override IEnumerable<UIError> errors => m_Errors;

        public override bool isAvailableOnDisk => m_IsAvailableOnDisk;

        public override bool isDirectDependency => true;

        public override string localPath => m_LocalPath;

        public override string versionString => m_VersionString;

        public override long versionId => m_VersionId;

        public override SemVersion? supportedVersion => m_SupportedUnityVersion;

        public override IEnumerable<SemVersion> supportedVersions => m_SupportedUnityVersions;

        public override IEnumerable<PackageSizeInfo> sizes => m_SizeInfos;

        public void SetLocalPath(IOProxy ioProxy, string path)
        {
            m_LocalPath = path ?? string.Empty;
            try
            {
                m_IsAvailableOnDisk = !string.IsNullOrEmpty(m_LocalPath) && ioProxy.FileExists(m_LocalPath);
            }
            catch (System.IO.IOException e)
            {
                Debug.Log($"[Package Manager Window] Cannot determine local path for {package.uniqueId}: {e.Message}");
                m_IsAvailableOnDisk = false;
            }
        }

        public AssetStorePackageVersion(IOProxy ioProxy, AssetStoreProductInfo productInfo, AssetStoreLocalInfo localInfo = null)
        {
            if (productInfo == null)
                throw new ArgumentNullException(nameof(productInfo));

            m_Errors = new List<UIError>();
            m_Tag = PackageTag.LegacyFormat;

            // m_Description is the version level description from PackageInfo, so we set this field to empty here deliberately
            // For asset store packages, we have the `productDescription` at the package level.
            m_Description = string.Empty;

            m_Category = productInfo.category;

            m_PublishNotes = localInfo?.publishNotes ?? string.Empty;

            m_VersionString = localInfo?.versionString ?? productInfo.versionString ?? string.Empty;
            m_VersionId = localInfo?.versionId ?? productInfo.versionId;
            SemVersionParser.TryParse(m_VersionString.Trim(), out m_Version);

            var publishDateString = localInfo?.publishedDate ?? productInfo.publishedDate ?? string.Empty;
            m_PublishedDateTicks = !string.IsNullOrEmpty(publishDateString) ? DateTime.Parse(publishDateString).Ticks : 0;
            m_DisplayName = !string.IsNullOrEmpty(productInfo.displayName) ? productInfo.displayName : $"Package {productInfo.productId}@{m_VersionId}";

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
            else if (state.Equals("disabled", StringComparison.InvariantCultureIgnoreCase))
                m_Tag |= PackageTag.Disabled;

            SetLocalPath(ioProxy, localInfo?.packagePath);
        }

        public void AddDowngradeWarningIfApplicable(AssetStoreLocalInfo localInfo, AssetStoreUpdateInfo updateInfo)
        {
            if (updateInfo?.canUpdate == true)
            {
                var warningMessage = string.Format(k_IncompatibleWarningMessage, localInfo.supportedVersion);
                m_Errors.Add(new UIError(UIErrorCode.AssetStorePackageError, warningMessage, UIError.Attribute.IsWarning));
            }
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            SemVersionParser.TryParse(m_SupportedUnityVersionString, out m_SupportedUnityVersion);
        }
    }
}
