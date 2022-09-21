// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI.Internal
{
    internal class CheckUpdateInfoArgs
    {
        internal class Entry
        {
            public long productId;
            public long uploadId;
            public long versionId;
            public string versionString;
            public string packagePath;

            public Entry(AssetStoreLocalInfo info)
            {
                productId = info.productId;
                uploadId = info.uploadId;
                versionId = info.versionId;
                versionString = info.versionString;
                packagePath = info.packagePath;
            }

            public Dictionary<string, string> ToDictionary()
            {
                return new Dictionary<string, string>
                {
                    ["local_path"] = packagePath ?? string.Empty,
                    ["id"] = productId.ToString(),
                    ["upload_id"] = uploadId.ToString(),
                    ["version_id"] = versionId.ToString(),
                    ["version"] = versionString ?? string.Empty
                };
            }
        }

        public Entry[] entries;

        public CheckUpdateInfoArgs(IEnumerable<AssetStoreLocalInfo> localInfos)
        {
            entries = localInfos.Select(info => new Entry(info)).ToArray();
        }

        public override string ToString()
        {
            return Json.Serialize(entries.Select(entry => entry?.ToDictionary() ?? new Dictionary<string, string>()).ToList());
        }
    }

    [Serializable]
    internal class AssetStoreUpdateInfo
    {
        internal enum Status
        {
            None = 0,
            CanUpdate,
            CanDowngrade
        }

        public long productId;
        public long uploadId;

        public Status status;
        public bool canUpdateOrDowngrade => status != Status.None;
        public bool canDowngrade => status == Status.CanDowngrade;
        public bool canUpdate => status == Status.CanUpdate;
    }

    internal partial class JsonParser
    {
        public virtual List<AssetStoreUpdateInfo> ParseUpdateInfos(CheckUpdateInfoArgs args, IDictionary<string, object> rawList)
        {
            var resultsList = rawList.GetDictionary("result")?.GetList<IDictionary<string, object>>("results");
            if (resultsList == null)
                return null;

            var entriesByProductId = args.entries.ToDictionary(item => item.productId, item => item);
            var newUpdateInfos = new List<AssetStoreUpdateInfo>();
            foreach (var updateDetail in resultsList)
            {
                var productId = updateDetail.GetStringAsLong("id");
                var uploadId = entriesByProductId.Get(productId)?.uploadId ?? 0;
                entriesByProductId.Remove(productId);

                var status = AssetStoreUpdateInfo.Status.None;
                if (updateDetail.Get("can_update", 0L) != 0)
                {
                    var recommendVersionCompare = updateDetail.Get("recommend_version_compare", 0L);
                    if (recommendVersionCompare < 0)
                        status = AssetStoreUpdateInfo.Status.CanDowngrade;
                    else
                        status = AssetStoreUpdateInfo.Status.CanUpdate;
                }

                var newUpdateInfo = new AssetStoreUpdateInfo
                {
                    productId = productId,
                    uploadId = uploadId,
                    status = status
                };
                newUpdateInfos.Add(newUpdateInfo);
            }

            // If an asset store package is disabled, we won't get properly update info from the server (the id field will be transformed to something else)
            // in the past we consider this case as `updateInfo` not checked and that causes the Package Manager to check update indefinitely.
            // Now we want to mark all packages that we called `CheckUpdate` on as updateInfoFetched to avoid unnecessary calls on disabled packages.
            foreach (var entry in entriesByProductId.Values)
            {
                var newUpdateInfo = new AssetStoreUpdateInfo
                {
                    productId = entry.productId,
                    uploadId = entry.uploadId,
                    status = AssetStoreUpdateInfo.Status.None
                };
                newUpdateInfos.Add(newUpdateInfo);
            }
            return newUpdateInfos;
        }
    }
}
