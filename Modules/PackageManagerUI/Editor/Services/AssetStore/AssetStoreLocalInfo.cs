// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI.AssetStore
{
    [Serializable]
    internal class LocalInfo
    {
        public string id;
        public string versionString;
        public string versionId;
        public string publishedDate;
        public string supportedVersion;
        public string packagePath;

        public bool updateInfoFetched;
        public bool canUpdate;

        public static LocalInfo ParseLocalInfo(UnityEditor.PackageInfo localInfo)
        {
            if (string.IsNullOrEmpty(localInfo.jsonInfo))
                return null;

            var jsonInfo = Json.Deserialize(localInfo.jsonInfo) as Dictionary<string, object>;
            var id = jsonInfo?.GetString("id");
            if (string.IsNullOrEmpty(id))
                return null;

            return new LocalInfo
            {
                id = id,
                packagePath = localInfo.packagePath ?? string.Empty,
                versionString = jsonInfo.GetString("version") ?? string.Empty,
                versionId = jsonInfo.GetString("version_id") ?? string.Empty,
                publishedDate = jsonInfo.GetString("pubdate") ?? string.Empty,
                supportedVersion = jsonInfo.GetString("unity_version") ?? string.Empty,
                updateInfoFetched = false,
                canUpdate = false
            };
        }
    }
}
