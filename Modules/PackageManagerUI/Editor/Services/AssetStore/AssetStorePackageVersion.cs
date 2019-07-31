// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.PackageManager.UI.AssetStore
{
    [Serializable]
    internal class AssetStorePackageVersion : IPackageVersion
    {
        public class SpecificVersionInfo
        {
            public string versionString;
            public string versionId;
            public string publishedDate;
            public string supportedVersion;
            public string packagePath;
        }

        [SerializeField]
        private string m_PackageUniqueId;
        [SerializeField]
        private string m_DisplayName;
        [SerializeField]
        private string m_Type;
        [SerializeField]
        private string m_Author;
        [SerializeField]
        private string m_Description;
        [SerializeField]
        private string m_Category;
        [SerializeField]
        private List<Error> m_Errors;
        [SerializeField]
        private SemVersion m_Version;
        [SerializeField]
        private DateTime m_PublishedDate;
        [SerializeField]
        private string m_PublisherId;
        [SerializeField]
        private bool m_IsAvailableOnDisk;
        [SerializeField]
        private string m_LocalPath;
        [SerializeField]
        private string m_VersionString;
        [SerializeField]
        private string m_VersionId;
        [SerializeField]
        private List<SemVersion> m_SupportedUnityVersions;
        [SerializeField]
        private SemVersion m_SupportedUnityVersion;
        [SerializeField]
        private List<PackageImage> m_Images;
        [SerializeField]
        private List<PackageSizeInfo> m_SizeInfos;
        [SerializeField]
        private List<PackageLink> m_Links;
        [SerializeField]
        private PackageTag m_Tag;

        public string name => string.Empty;

        public string displayName => m_DisplayName;

        public string type => m_Type;

        public string author => m_Author;

        public string description => m_Description;

        public string category => m_Category;

        public string packageUniqueId => m_PackageUniqueId;

        public string uniqueId => m_VersionId;

        public PackageSource source => PackageSource.Unknown;

        public IEnumerable<Error> errors => m_Errors;

        public IEnumerable<Sample> samples => Enumerable.Empty<Sample>();

        public EntitlementsInfo entitlements => null;

        public SemVersion version
        {
            get { return m_Version; }
            set { m_Version = value; }
        }

        public DateTime? publishedDate => m_PublishedDate;

        public string publisherId => m_PublisherId;

        public DependencyInfo[] dependencies => null;

        public DependencyInfo[] resolvedDependencies => null;

        public PackageInfo packageInfo => null;

        public bool isInstalled => false;

        public bool isFullyFetched => true;

        public bool isUserVisible => true;

        public bool isAvailableOnDisk => m_IsAvailableOnDisk;

        public bool isVersionLocked => true;

        public bool canBeRemoved => false;

        public bool canBeEmbedded => false;

        public bool isDirectDependency => true;

        public string localPath
        {
            get { return m_LocalPath; }
            set
            {
                m_LocalPath = value;
                m_IsAvailableOnDisk = !string.IsNullOrEmpty(m_LocalPath) && File.Exists(m_LocalPath);
            }
        }

        public string versionString
        {
            get { return m_VersionString; }
            set { m_VersionString = value; }
        }

        public string versionId
        {
            get { return m_VersionId; }
            set { m_VersionId = value; }
        }

        public SemVersion supportedVersion => m_SupportedUnityVersion;

        public IEnumerable<SemVersion> supportedVersions => m_SupportedUnityVersions;

        public IEnumerable<PackageImage> images => m_Images;

        public IEnumerable<PackageSizeInfo> sizes => m_SizeInfos;

        public IEnumerable<PackageLink> links => m_Links;

        public bool HasTag(PackageTag tag)
        {
            return (m_Tag & tag) == tag;
        }

        public AssetStorePackageVersion(AssetStorePackageVersion other, SpecificVersionInfo localInfo = null)
        {
            m_PackageUniqueId = other.m_PackageUniqueId;
            m_DisplayName = other.m_DisplayName;
            m_Type = other.m_Type;
            m_Author = other.m_Author;
            m_Description = other.m_Description;
            m_Category = other.m_Category;
            m_Errors = other.m_Errors;
            m_Version = other.m_Version;
            m_PublishedDate = other.m_PublishedDate;
            m_PublisherId = other.m_PublisherId;
            m_IsAvailableOnDisk = other.m_IsAvailableOnDisk;
            m_LocalPath = other.m_LocalPath;
            m_VersionString = other.m_VersionString;
            m_VersionId = other.m_VersionId;
            m_SupportedUnityVersions = other.m_SupportedUnityVersions;
            m_SupportedUnityVersion = other.m_SupportedUnityVersion;
            m_Images = other.m_Images;
            m_SizeInfos = other.m_SizeInfos;
            m_Links = other.m_Links;
            m_Tag = other.m_Tag;

            if (localInfo != null)
            {
                m_VersionString = localInfo.versionString;
                m_VersionId = localInfo.versionId;

                SemVersion semVer;
                if (!SemVersion.TryParse(m_VersionString.Trim(), out semVer))
                {
                    semVer = new SemVersion(0);
                }
                m_Version = semVer;

                m_PublishedDate = DateTime.Parse(localInfo.publishedDate);

                var simpleVersion = Regex.Replace(localInfo.supportedVersion, @"(?<major>\d+)\.(?<minor>\d+).(?<patch>\d+)[abfp].+", "${major}.${minor}.${patch}");
                SemVersion.TryParse(simpleVersion.Trim(), out m_SupportedUnityVersion);
            }
        }

        public AssetStorePackageVersion(string productId, IDictionary<string, object> productDetail, SpecificVersionInfo localInfo = null)
        {
            if (productDetail == null)
            {
                throw new ArgumentNullException(nameof(productDetail));
            }

            m_Errors = new List<Error>();
            m_Type = "assetstore";
            m_Tag = PackageTag.AssetStore;
            m_PackageUniqueId = productId;

            try
            {
                var description = productDetail.ContainsKey("description") ? productDetail["description"] as string : string.Empty;
                m_Description = CleanUpHtml(description);

                var publisher = new Dictionary<string, object>();
                if (productDetail.ContainsKey("productPublisher"))
                {
                    publisher = productDetail["productPublisher"] as Dictionary<string, object>;
                    if (publisher.ContainsKey("url") && publisher["url"] is string && (string)publisher["url"] == "http://unity3d.com")
                        m_Author = "Unity Technologies Inc.";
                    else
                        m_Author = publisher.ContainsKey("name") ? publisher["name"] as string : L10n.Tr("Unknown publisher");

                    m_PublisherId = publisher.ContainsKey("externalRef") ? publisher["externalRef"] as string : string.Empty;
                }
                else
                {
                    m_Author = string.Empty;
                    m_PublisherId = string.Empty;
                }

                m_Category = string.Empty;
                if (productDetail.ContainsKey("category"))
                {
                    var categoryInfo = productDetail["category"] as IDictionary<string, object>;
                    m_Category = categoryInfo["name"] as string;
                }

                if (localInfo != null)
                {
                    m_VersionString = localInfo.versionString;
                    m_VersionId = localInfo.versionId;

                    SemVersion semVer;
                    if (!SemVersion.TryParse(m_VersionString.Trim(), out semVer))
                    {
                        semVer = new SemVersion(0);
                    }
                    m_Version = semVer;

                    m_PublishedDate = DateTime.Parse(localInfo.publishedDate);
                }
                else if (productDetail.ContainsKey("version"))
                {
                    var versionInfo = productDetail["version"] as IDictionary<string, object>;
                    m_VersionString = versionInfo["name"] as string;
                    m_VersionId = versionInfo["id"] as string;
                    SemVersion semVer;
                    if (!SemVersion.TryParse(m_VersionString.Trim(), out semVer))
                    {
                        semVer = new SemVersion(0);
                    }
                    m_Version = semVer;

                    if (versionInfo.ContainsKey("publishedDate"))
                    {
                        var date = versionInfo["publishedDate"] as string;
                        m_PublishedDate = DateTime.Parse(date);
                    }
                    else
                    {
                        m_PublishedDate = new DateTime();
                    }
                }
                else
                {
                    m_VersionString = string.Empty;
                    m_VersionId = string.Empty;
                    m_Version = new SemVersion(0);
                }

                m_DisplayName = productDetail.ContainsKey("displayName") ? productDetail["displayName"] as string : $"Package {m_PackageUniqueId}@{m_VersionId}";

                m_SupportedUnityVersions = new List<SemVersion>();
                if (productDetail.ContainsKey("supportedUnityVersions"))
                {
                    var supportedVersions = productDetail["supportedUnityVersions"] as IList<object>;
                    foreach (var supportedVersion in supportedVersions.Where(v => v is string))
                    {
                        SemVersion version;
                        if (SemVersion.TryParse(supportedVersion as string, out version))
                            m_SupportedUnityVersions.Add(version);
                    }

                    m_SupportedUnityVersions.Sort((left, right) => left.CompareByPrecedence(right));
                }

                if (localInfo != null)
                {
                    var simpleVersion = Regex.Replace(localInfo.supportedVersion, @"(?<major>\d+)\.(?<minor>\d+).(?<patch>\d+)[abfp].+", "${major}.${minor}.${patch}");
                    SemVersion.TryParse(simpleVersion.Trim(), out m_SupportedUnityVersion);
                }
                else
                {
                    m_SupportedUnityVersion = m_SupportedUnityVersions.LastOrDefault();
                }

                m_Images = new List<PackageImage>();
                if (productDetail.ContainsKey("mainImage"))
                {
                    var mainImage = productDetail["mainImage"] as IDictionary<string, object>;
                    var thumbnailUrl = mainImage["url"] as string;
                    thumbnailUrl = thumbnailUrl.Replace("//d2ujflorbtfzji.cloudfront.net/", "//assetstorev1-prd-cdn.unity3d.com/");
                    m_Images.Add(new PackageImage
                    {
                        type = PackageImage.ImageType.Main,
                        thumbnailUrl = "http:" + thumbnailUrl,
                        url = string.Empty
                    });
                }

                if (productDetail.ContainsKey("images"))
                {
                    var images = productDetail["images"] as IList<object>;
                    foreach (var image in images)
                    {
                        var imageInfo = image as IDictionary<string, object>;
                        var type = imageInfo["type"] as string;
                        if (string.IsNullOrEmpty(type))
                            continue;

                        var imageType = PackageImage.ImageType.Screenshot;
                        var thumbnailUrl = imageInfo["thumbnailUrl"] as string;
                        thumbnailUrl = thumbnailUrl.Replace("//d2ujflorbtfzji.cloudfront.net/", "//assetstorev1-prd-cdn.unity3d.com/");

                        if (type == "sketchfab")
                            imageType = PackageImage.ImageType.Sketchfab;
                        else if (type == "youtube")
                            imageType = PackageImage.ImageType.Youtube;

                        var imageUrl = imageInfo["imageUrl"] as string;
                        if (imageType == PackageImage.ImageType.Screenshot)
                            imageUrl = "http:" + imageUrl;

                        m_Images.Add(new PackageImage
                        {
                            type = imageType,
                            thumbnailUrl = "http:" + thumbnailUrl,
                            url = imageUrl
                        });
                    }
                }

                m_SizeInfos = new List<PackageSizeInfo>();
                if (productDetail.ContainsKey("uploads"))
                {
                    var uploads = productDetail["uploads"] as IDictionary<string, object>;
                    foreach (var key in uploads.Keys)
                    {
                        var simpleVersion = Regex.Replace(key, @"(?<major>\d+)\.(?<minor>\d+).(?<patch>\d+)[abfp].+", "${major}.${minor}.${patch}");
                        SemVersion version;
                        if (SemVersion.TryParse(simpleVersion.Trim(), out version))
                        {
                            var info = uploads[key] as IDictionary<string, object>;
                            var assetCount = info["assetCount"] as string;
                            var downloadSize = info["downloadSize"] as string;

                            m_SizeInfos.Add(new PackageSizeInfo
                            {
                                supportedUnityVersion = version,
                                assetCount = string.IsNullOrEmpty(assetCount) ? 0 : ulong.Parse(assetCount),
                                downloadSize = string.IsNullOrEmpty(downloadSize) ? 0 : ulong.Parse(downloadSize)
                            });
                        }
                    }

                    m_SizeInfos.Sort((left, right) => left.supportedUnityVersion.CompareByPrecedence(right.supportedUnityVersion));
                }

                m_Links = new List<PackageLink>();

                var slug = productDetail.ContainsKey("slug") ? productDetail["slug"] as string : m_PackageUniqueId;
                m_Links.Add(new PackageLink {name = "View in the Asset Store", url = $"/packages/p/{slug}"});

                if (publisher.ContainsKey("url"))
                {
                    var url = publisher["url"] as string;
                    if (!string.IsNullOrEmpty(url) && Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
                        m_Links.Add(new PackageLink {name = "Publisher Web Site", url = url});
                }

                if (publisher.ContainsKey("supportUrl"))
                {
                    var url = publisher["supportUrl"] as string;
                    if (!string.IsNullOrEmpty(url) && Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
                        m_Links.Add(new PackageLink {name = "Publisher Support", url = url});
                }

                if (productDetail.ContainsKey("state"))
                {
                    var state = productDetail["state"] as string;
                    if (state.Equals("published", StringComparison.InvariantCultureIgnoreCase))
                        m_Tag |= PackageTag.Published;
                    else if (state.Equals("deprecated", StringComparison.InvariantCultureIgnoreCase))
                        m_Tag |= PackageTag.Deprecated;
                }

                m_LocalPath = productDetail.ContainsKey("localPath") ? productDetail["localPath"] as string : string.Empty;
                m_IsAvailableOnDisk = !string.IsNullOrEmpty(m_LocalPath) && File.Exists(m_LocalPath);
            }
            catch (Exception e)
            {
                m_Errors.Add(new Error(NativeErrorCode.Unknown, e.Message));
            }
        }

        private static string CleanUpHtml(string source)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            source = source.Replace("<br>", "\n");

            var array = new char[source.Length];
            var arrayIndex = 0;
            var inside = false;

            foreach (var c in source.ToCharArray())
            {
                if (c == '<')
                    inside = true;
                else if (c == '>')
                    inside = false;
                else
                {
                    if (!inside)
                        array[arrayIndex++] = c;
                }
            }

            var text = new string(array, 0, arrayIndex);
            text = Regex.Replace(text, @"&#x?\d+;", "");
            text = text.Replace("&nbsp;", " ");
            text = text.Replace("&lt;", "<");
            text = text.Replace("&gt;", ">");
            text = text.Replace("&amp;", "&");
            text = text.Replace("&quot;", "\"");
            text = text.Replace("&apos;", "'");
            text = Regex.Replace(text, @"[\n\r]+", "\n");
            text = text.Trim(' ', '\r', '\n', '\t');

            return text;
        }
    }
}
