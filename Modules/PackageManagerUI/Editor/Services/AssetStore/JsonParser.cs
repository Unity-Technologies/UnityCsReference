// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal interface IJsonParser : IService
    {
        List<string> ParseLabels(IDictionary<string, object> result);
        string CleanUpHtml(string source, bool removeEndOfLine = true);

        AssetStorePurchaseInfo ParsePurchaseInfo(IDictionary<string, object> rawInfo);
        AssetStorePurchases ParsePurchases(IDictionary<string, object> rawList);
        List<AssetStoreUpdateInfo> ParseUpdateInfos(IDictionary<string, object> rawList);
        AssetStoreProductInfo ParseProductInfo(string assetStoreUrl, long productId, IDictionary<string, object> productDetail);
        AssetStoreDownloadInfo ParseDownloadInfo(IDictionary<string, object> rawInfo);
    }

    internal partial class JsonParser : BaseService<IJsonParser>, IJsonParser
    {
        public List<string> ParseLabels(IDictionary<string, object> result)
        {
            var resultsList = result.GetList<string>("results");
            if (resultsList == null)
                return null;
            var labels = new List<string>(resultsList);
            labels.Remove("#BIN");
            labels.Sort();
            return labels;
        }

        public string CleanUpHtml(string source, bool removeEndOfLine = true)
        {
            if (string.IsNullOrEmpty(source))
                return source;

            var result = source;
            if (removeEndOfLine)
            {
                // first we remove all end of line, html tags will reformat properly
                result = result.Replace("\n", "");
                result = result.Replace("\r", "");
            }

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
    }
}
