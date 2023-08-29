// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Scripting.ScriptCompilation;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class AssetStoreProductInfo
    {
        public long productId;
        public long versionId;
        public string packageName;
        public string description;
        public string publisherName;
        public string category;
        public string versionString;
        public string publishedDate;
        public string displayName;
        public string state;
        public string publishNotes;
        public string firstPublishedDate;

        public string assetStoreProductUrl;
        public string assetStorePublisherUrl;
        public string publisherWebsiteUrl;
        public string publisherSupportUrl;

        public List<string> supportedVersions;
        public List<PackageImage> images;
        public List<PackageSizeInfo> sizeInfos;

        public bool Equals(AssetStoreProductInfo other)
        {
            return other != null && other.productId == productId && other.versionId == versionId && other.versionString == versionString;
        }
    }

    internal partial class JsonParser
    {
        private List<PackageImage> GetImagesFromProductDetails(IDictionary<string, object> productDetail)
        {
            int imageLimit = 4;
            int imagesLoaded = 0;

            var result = new List<PackageImage>();
            var mainImageDictionary = productDetail.GetDictionary("mainImage");
            var mainImageThumbnailUrl = mainImageDictionary?.GetString("url");

            if (!string.IsNullOrEmpty(mainImageThumbnailUrl))
            {
                mainImageThumbnailUrl = mainImageThumbnailUrl.Replace("//d2ujflorbtfzji.cloudfront.net/", "//assetstorev1-prd-cdn.unity3d.com/");

                var imageUrl = "http:" + mainImageDictionary.GetString("big");

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

                // for now we only use screenshot types
                var imageUrl = image.GetString("imageUrl");
                if (imageType == PackageImage.ImageType.Screenshot)
                {
                    imageUrl = "http:" + imageUrl;

                    result.Add(new PackageImage
                    {
                        type = imageType,
                        thumbnailUrl = "http:" + thumbnailUrl,
                        url = imageUrl
                    });
                    ++imagesLoaded;
                }
            }
            return result;
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

        public AssetStoreProductInfo ParseProductInfo(string assetStoreUrl, long productId, IDictionary<string, object> productDetail)
        {
            if (productId <= 0 || productDetail == null || !productDetail.Any())
                return null;

            var productInfo = new AssetStoreProductInfo();

            productInfo.productId = productId;
            productInfo.description = CleanUpHtml(productDetail.GetString("description")) ?? string.Empty;

            var publisher = productDetail.GetDictionary("productPublisher");
            var publisherId = string.Empty;
            productInfo.publisherName = string.Empty;
            if (publisher != null)
            {
                if (publisher.GetString("url") == "http://unity3d.com")
                    productInfo.publisherName = "Unity Technologies Inc.";
                else
                    productInfo.publisherName = publisher.GetString("name") ?? L10n.Tr("Unknown publisher");
                publisherId = publisher.GetString("externalRef") ?? string.Empty;
            }

            if (!string.IsNullOrEmpty(publisherId))
                productInfo.assetStorePublisherUrl = $"{assetStoreUrl}/publishers/{publisherId}";

            productInfo.packageName = productDetail.GetString("packageName") ?? string.Empty;
            productInfo.category = productDetail.GetDictionary("category")?.GetString("name") ?? string.Empty;
            productInfo.publishNotes = CleanUpHtml(productDetail.GetString("publishNotes") ?? string.Empty, false);
            productInfo.firstPublishedDate = productDetail.GetDictionary("properties")?.GetString("firstPublishedDate");

            var versionInfo = productDetail.GetDictionary("version");
            if (versionInfo != null)
            {
                productInfo.versionString = versionInfo.GetString("name");
                productInfo.versionId = versionInfo.GetStringAsLong("id");
                productInfo.publishedDate = versionInfo.GetString("publishedDate");
            }

            productInfo.displayName = productDetail.GetString("displayName");

            productInfo.supportedVersions = productDetail.GetList<string>("supportedUnityVersions")?.ToList();

            productInfo.state = productDetail.GetString("state");

            productInfo.images = GetImagesFromProductDetails(productDetail);
            productInfo.sizeInfos = GetSizeInfoFromProductDetails(productDetail);

            productInfo.assetStoreProductUrl = GetProductUrlFromProductDetails(assetStoreUrl, productDetail);
            productInfo.publisherWebsiteUrl = GetPublisherWebsiteUrlFromProductDetails(assetStoreUrl, productDetail);
            productInfo.publisherSupportUrl = GetPublisherSupportUrlFromProductDetails(assetStoreUrl, productDetail);

            return productInfo;
        }

        private string GetProductUrlFromProductDetails(string assetStoreUrl, IDictionary<string, object> productDetail)
        {
            var slug = productDetail.GetString("slug") ?? productDetail.GetString("id");
            return $"{assetStoreUrl}/packages/p/{slug}";
        }

        private string GetPublisherWebsiteUrlFromProductDetails(string assetStoreUrl, IDictionary<string, object> productDetail)
        {
            var url = productDetail.GetDictionary("productPublisher")?.GetString("url");
            return BuildUrl(assetStoreUrl, url);
        }

        private string GetPublisherSupportUrlFromProductDetails(string assetStoreUrl, IDictionary<string, object> productDetail)
        {
            var supportUrl = productDetail.GetDictionary("productPublisher")?.GetString("supportUrl");
            return BuildUrl(assetStoreUrl, supportUrl);
        }

        private string BuildUrl(string assetStoreUrl, string url)
        {
            if (string.IsNullOrEmpty(url) || !Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute))
                return string.Empty;

            if (!url.StartsWith("http:", StringComparison.InvariantCulture) && !url.StartsWith("https:", StringComparison.InvariantCulture))
                return assetStoreUrl + url;

            return url;
        }
    }
}
