// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.Internal
{
    [Serializable]
    internal class AssetStoreLocalInfo
    {
        public long productId;
        public long uploadId;
        public long versionId;
        public string title;
        public string versionString;
        public string publishedDate;
        public string supportedVersion;
        public string packagePath;
        public string publishNotes;
        public string firstPublishedDate;

        public static AssetStoreLocalInfo ParseLocalInfo(UnityEditor.PackageInfo localInfo)
        {
            if (string.IsNullOrEmpty(localInfo.jsonInfo))
                return null;

            try
            {
                var jsonInfo = Json.Deserialize(localInfo.jsonInfo) as Dictionary<string, object>;
                var productId = jsonInfo?.GetStringAsLong("id") ?? 0;
                if(productId <= 0)
                    return null;

                return new AssetStoreLocalInfo
                {
                    productId = productId,
                    packagePath = localInfo.packagePath ?? string.Empty,
                    title = jsonInfo.GetString("title") ?? string.Empty,
                    versionString = jsonInfo.GetString("version") ?? string.Empty,
                    versionId = jsonInfo.GetStringAsLong("version_id"),
                    uploadId = jsonInfo.GetStringAsLong("upload_id"),
                    publishedDate = jsonInfo.GetString("pubdate") ?? string.Empty,
                    supportedVersion = jsonInfo.GetString("unity_version") ?? string.Empty,
                    publishNotes = jsonInfo.GetString("publishnotes") ?? string.Empty
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>
            {
                ["local_path"] = packagePath ?? string.Empty,
                ["id"] = productId.ToString(),
                ["version"] = versionString ?? string.Empty,
                ["version_id"] = versionId.ToString(),
                ["upload_id"] = uploadId.ToString()
            };
        }
    }
}
