// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class AssetStoreProductInfo
    {
        public string id;
        public string packageName;
        public string description;
        public string author;
        public string publisherId;
        public string category;
        public string versionString;
        public string versionId;
        public string publishedDate;
        public string displayName;
        public string state;
        public string publishNotes;
        public string firstPublishedDate;
        public PackageLink assetStoreLink;

        public List<string> supportedVersions;
        public List<PackageImage> images;
        public List<PackageLink> links;
        public List<PackageSizeInfo> sizeInfos;

        [NonSerialized]
        private AssetStoreUtils m_AssetStoreUtils;
        public void ResolveDependencies(AssetStoreUtils assetStoreUtils)
        {
            m_AssetStoreUtils = assetStoreUtils;
        }

        private AssetStoreProductInfo()
        {
        }

        private AssetStoreProductInfo(AssetStoreUtils assetStoreUtils, string productId, IDictionary<string, object> productDetail)
        {
            ResolveDependencies(assetStoreUtils);

            id = productId;
            description = CleanUpHtml(productDetail.GetString("description")) ?? string.Empty;

            var publisher = productDetail.GetDictionary("productPublisher");
            if (publisher != null)
            {
                if (publisher.GetString("url") == "http://unity3d.com")
                    author = "Unity Technologies Inc.";
                else
                    author = publisher.GetString("name") ?? L10n.Tr("Unknown publisher");
                publisherId = publisher.GetString("externalRef") ?? string.Empty;
            }
            else
            {
                author = string.Empty;
                publisherId = string.Empty;
            }

            packageName = productDetail.GetString("packageName") ?? string.Empty;
            category = productDetail.GetDictionary("category")?.GetString("name") ?? string.Empty;
            publishNotes = productDetail.GetString("publishNotes") ?? string.Empty;
            firstPublishedDate = productDetail.GetDictionary("properties")?.GetString("firstPublishedDate");

            var versionInfo = productDetail.GetDictionary("version");
            if (versionInfo != null)
            {
                versionString = versionInfo.GetString("name");
                versionId = versionInfo.GetString("id");
                publishedDate = versionInfo.GetString("publishedDate");
            }

            displayName = productDetail.GetString("displayName");

            supportedVersions = productDetail.GetList<string>("supportedUnityVersions")?.ToList();

            state = productDetail.GetString("state");

            images = GetImagesFromProductDetails(productDetail);
            links = GetLinksFromProductDetails(productDetail);
            sizeInfos = GetSizeInfoFromProductDetails(productDetail);

            assetStoreLink = GetAssetStoreLinkFromProductDetails(productDetail);
        }

        public static AssetStoreProductInfo ParseProductInfo(AssetStoreUtils assetStoreUtils, string productId, IDictionary<string, object> productDetail)
        {
            if (string.IsNullOrEmpty(productId) || productDetail == null || !productDetail.Any())
                return null;
            return new AssetStoreProductInfo(assetStoreUtils, productId, productDetail);
        }

        internal static string CleanUpHtml(string source)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            source = source.Replace("<br>", "\n");

            var array = new char[source.Length];
            var arrayIndex = 0;
            var inside = false;

            var result = Regex.Replace(source, "<a .*href=[\"']([^\"']+)[\"'][^>]*>(.+)</a>", "$2 ($1)", RegexOptions.IgnoreCase);

            foreach (var c in result.ToCharArray())
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

        private List<PackageImage> GetImagesFromProductDetails(IDictionary<string, object> productDetail)
        {
            int imageLimit = 3;
            int imagesLoaded = 0;

            var result = new List<PackageImage>();
            var mainImageDictionary = productDetail.GetDictionary("mainImage");
            var mainImageThumbnailUrl = mainImageDictionary?.GetString("url");

            if (!string.IsNullOrEmpty(mainImageThumbnailUrl))
            {
                mainImageThumbnailUrl = mainImageThumbnailUrl.Replace("//d2ujflorbtfzji.cloudfront.net/", "//assetstorev1-prd-cdn.unity3d.com/");

                var imageUrl = "http:" +  mainImageDictionary.GetString("big");

                result.Add(new PackageImage
                {
                    type = PackageImage.ImageType.Main,
                    thumbnailUrl = "http:" + mainImageThumbnailUrl,
                    url = imageUrl
                });
                ++imagesLoaded;
            }

            var images = productDetail.GetList<IDictionary<string, object>>("images") ?? Enumerable.Empty<IDictionary<string, object>>();

            foreach (var image in images)
            {
                if (imagesLoaded >= imageLimit)
                    break;

                var type = image?.GetString("type");
                if (string.IsNullOrEmpty(type))
                    continue;

                var imageType = PackageImage.ImageType.Screenshot;
                var thumbnailUrl = image.GetString("thumbnailUrl");
                thumbnailUrl = thumbnailUrl.Replace("//d2ujflorbtfzji.cloudfront.net/", "//assetstorev1-prd-cdn.unity3d.com/");

                if (type == "sketchfab")
                    imageType = PackageImage.ImageType.Sketchfab;
                else if (type == "youtube")
                    imageType = PackageImage.ImageType.Youtube;
                else if (type == "vimeo")
                    imageType = PackageImage.ImageType.Vimeo;

                var imageUrl = image.GetString("imageUrl");
                if (imageType == PackageImage.ImageType.Screenshot)
                    imageUrl = "http:" + imageUrl;

                result.Add(new PackageImage
                {
                    type = imageType,
                    thumbnailUrl = "http:" + thumbnailUrl,
                    url = imageUrl
                });
                ++imagesLoaded;
            }
            return result;
        }

        private List<PackageLink> GetLinksFromProductDetails(IDictionary<string, object> productDetail)
        {
            var result = new List<PackageLink>();

            result.Add(GetAssetStoreLinkFromProductDetails(productDetail));

            var publisher = productDetail.GetDictionary("productPublisher");
            if (publisher != null)
            {
                var url = publisher.GetString("url");
                if (!string.IsNullOrEmpty(url) && Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
                    result.Add(GetPackageLink("Publisher Website", url));

                var supportUrl = publisher.GetString("supportUrl");
                if (!string.IsNullOrEmpty(supportUrl) && Uri.IsWellFormedUriString(supportUrl, UriKind.RelativeOrAbsolute))
                    result.Add(GetPackageLink("Publisher Support", supportUrl));
            }
            return result;
        }

        private PackageLink GetAssetStoreLinkFromProductDetails(IDictionary<string, object> productDetail)
        {
            var slug = productDetail.GetString("slug") ?? productDetail.GetString("id");
            var packagePath = $"/packages/p/{slug}";

            return GetPackageLink("View in the Asset Store", packagePath);
        }

        private List<PackageSizeInfo> GetSizeInfoFromProductDetails(IDictionary<string, object> productDetail)
        {
            var result = new List<PackageSizeInfo>();
            var uploads = productDetail.GetDictionary("uploads");
            if (uploads != null)
            {
                foreach (var key in uploads.Keys)
                {
                    var simpleVersion = Regex.Replace(key, @"(?<major>\d+)\.(?<minor>\d+).(?<patch>\d+)[abfp].+", "${major}.${minor}.${patch}");

                    SemVersion? version;
                    bool isVersionParsed = SemVersionParser.TryParse(simpleVersion.Trim(), out version);

                    if (isVersionParsed)
                    {
                        var info = uploads.GetDictionary(key);
                        var assetCount = info?.GetString("assetCount") ?? string.Empty;
                        var downloadSize = info?.GetString("downloadSize") ?? string.Empty;

                        result.Add(new PackageSizeInfo
                        {
                            supportedUnityVersion = (SemVersion)version,
                            assetCount = string.IsNullOrEmpty(assetCount) ? 0 : ulong.Parse(assetCount),
                            downloadSize = string.IsNullOrEmpty(downloadSize) ? 0 : ulong.Parse(downloadSize)
                        });
                    }
                }
            }
            return result;
        }

        private PackageLink GetPackageLink(string name, string url)
        {
            if (!url.StartsWith("http:", StringComparison.InvariantCulture) && !url.StartsWith("https:", StringComparison.InvariantCulture))
                url = m_AssetStoreUtils.assetStoreUrl + url;
            return new PackageLink { name = name, url = url };
        }
    }
}
