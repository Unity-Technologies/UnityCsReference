// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class AssetStoreLocalInfo
    {
        public string id;
        public string versionString;
        public string versionId;
        public string publishedDate;
        public string supportedVersion;
        public string packagePath;
        public string publishNotes;
        public string firstPublishedDate;

        public bool updateInfoFetched;
        public bool canUpdate;

        public static AssetStoreLocalInfo ParseLocalInfo(UnityEditor.PackageInfo localInfo)
        {
            if (string.IsNullOrEmpty(localInfo.jsonInfo))
                return null;

            try
            {
                var jsonInfo = Json.Deserialize(localInfo.jsonInfo) as Dictionary<string, object>;
                var id = jsonInfo?.GetString("id");
                if (string.IsNullOrEmpty(id))
                    return null;

                return new AssetStoreLocalInfo
                {
                    id = id,
                    packagePath = localInfo.packagePath ?? string.Empty,
                    versionString = jsonInfo.GetString("version") ?? string.Empty,
                    versionId = jsonInfo.GetString("version_id") ?? string.Empty,
                    publishedDate = jsonInfo.GetString("pubdate") ?? string.Empty,
                    supportedVersion = jsonInfo.GetString("unity_version") ?? string.Empty,
                    publishNotes = jsonInfo.GetString("publishnotes") ?? string.Empty,
                    updateInfoFetched = false,
                    canUpdate = false
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
                ["id"] = id ?? string.Empty,
                ["version"] = versionString ?? string.Empty,
                ["version_id"] = versionId ?? string.Empty
            };
        }
    }
}
