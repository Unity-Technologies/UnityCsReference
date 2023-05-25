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
        public string id;
        public string packageName;
        public string description;
        public string publisherName;
        public string category;
        public string versionString;
        public string versionId;
        public string publishedDate;
        public string displayName;
        public string state;
        public string publishNotes;
        public string firstPublishedDate;

        public string publisherLink;
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
            var publisherId = string.Empty;
            publisherName = string.Empty;
            if (publisher != null)
            {
                if (publisher.GetString("url") == "http://unity3d.com")
                    publisherName = "Unity Technologies Inc.";
                else
                    publisherName = publisher.GetString("name") ?? L10n.Tr("Unknown publisher");
                publisherId = publisher.GetString("externalRef") ?? string.Empty;
            }

            if (!string.IsNullOrEmpty(publisherId))
                publisherLink = $"{m_AssetStoreUtils.assetStoreUrl}/publishers/{publisherId}";

            packageName = productDetail.GetString("packageName") ?? string.Empty;
            category = productDetail.GetDictionary("category")?.GetString("name") ?? string.Empty;
            publishNotes = CleanUpHtml(productDetail.GetString("publishNotes") ?? string.Empty);
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

            // first we remove all end of line, html tags will reformat properly
            var result = source.Replace("\n", "");
            result = result.Replace("\r", "");

            // then we add all \n from html tgs we want to support
            result = Regex.Replace(result, "</?br */?>", "\n", RegexOptions.IgnoreCase);

            // seems like browsers support p tags that never end.. so we need to add </p> to support it too
            result = Regex.Replace(result, "(<p[^>/]*>[^<]*)<p[^>/]*>", "$1</p>", RegexOptions.IgnoreCase);

            // <p> </p> should decorate with a starting \n and ending \n
            result = Regex.Replace(result, "<p[^>/]*>", "\n", RegexOptions.IgnoreCase);
            result = Regex.Replace(result, "</p>", "\n", RegexOptions.IgnoreCase);

            // We add dots to <li>
            result = Regex.Replace(result, "<li[^>/]*>", "• ", RegexOptions.IgnoreCase);

            // We add \n for each <li>
            result = Regex.Replace(result, "</li *>", "\n", RegexOptions.IgnoreCase);

            // Then we strip all tags except the <a>
            result = Regex.Replace(result, "<[^a>]*>", "", RegexOptions.IgnoreCase);

            // Transform the <a> in a readable text
            result = Regex.Replace(result, "<a[^>]*href *= *[\"']{1}([^\"'>]+)[\"'][^>]*>([^<]*)</a>", "$2 ($1)", RegexOptions.IgnoreCase);

            // for href that doesn't have quotes at all
            result = Regex.Replace(result, "<a[^>]*href *= *([^>]*)>([^<]*)</a>", "$2 ($1)", RegexOptions.IgnoreCase);

            // we strip emojis
            result = Regex.Replace(result, @"&#x?\d+;?", "");

            // finally we transform special characters that we want to support
            result = result.Replace("&nbsp;", " ");
            result = result.Replace("&lt;", "<");
            result = result.Replace("&gt;", ">");
            result = result.Replace("&amp;", "&");
            result = result.Replace("&quot;", "\"");
            result = result.Replace("&apos;", "'");

            // final trim
            result = result.Trim(' ', '\r', '\n', '\t');

            return result;
        }

        private string PrependProtocolIfNotPresent(string url)
        {
            if (!url.StartsWith("http:") && !url.StartsWith("https:"))
                return "http:" + url;
            return url;
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

                var mainImageBig = mainImageDictionary.GetString("big");
                var mainImageBig_v2 = mainImageDictionary.GetString("big_v2");
                var imageUrl = !string.IsNullOrEmpty(mainImageBig) ?  mainImageBig : mainImageBig_v2;
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    result.Add(new PackageImage
                    {
                        type = PackageImage.ImageType.Main,
                        thumbnailUrl = PrependProtocolIfNotPresent(mainImageThumbnailUrl),
                        url = PrependProtocolIfNotPresent(imageUrl)
                    });
                    ++imagesLoaded;
                }
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
                if (string.IsNullOrEmpty(imageUrl))
                    continue;

                result.Add(new PackageImage
                {
                    type = imageType,
                    thumbnailUrl = PrependProtocolIfNotPresent(thumbnailUrl),
                    url = imageType == PackageImage.ImageType.Screenshot ? PrependProtocolIfNotPresent(imageUrl) : imageUrl
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
                    result.Add(GetPackageLink(L10n.Tr("Publisher Website"), url, "viewPublisherWebsite"));

                var supportUrl = publisher.GetString("supportUrl");
                if (!string.IsNullOrEmpty(supportUrl) && Uri.IsWellFormedUriString(supportUrl, UriKind.RelativeOrAbsolute))
                    result.Add(GetPackageLink(L10n.Tr("Publisher Support"), supportUrl, "viewPublisherSupport"));
            }
            return result;
        }

        private PackageLink GetAssetStoreLinkFromProductDetails(IDictionary<string, object> productDetail)
        {
            var slug = productDetail.GetString("slug") ?? productDetail.GetString("id");
            var packagePath = $"/packages/p/{slug}";

            return GetPackageLink(L10n.Tr("View in Asset Store"), packagePath, "viewProductInAssetStore");
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

        private PackageLink GetPackageLink(string name, string url, string analyticsEventName)
        {
            if (!url.StartsWith("http:", StringComparison.InvariantCulture) && !url.StartsWith("https:", StringComparison.InvariantCulture))
                url = m_AssetStoreUtils.assetStoreUrl + url;
            return new PackageLink { name = name, url = url, analyticsEventName = analyticsEventName};
        }

        public bool Equals(AssetStoreProductInfo other)
        {
            return other != null && other.id == id && other.versionId == versionId && other.versionString == versionString;
        }
    }
}
