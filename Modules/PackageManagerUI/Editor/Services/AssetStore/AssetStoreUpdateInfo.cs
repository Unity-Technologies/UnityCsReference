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
        public long[] productIds;

        public CheckUpdateInfoArgs(IEnumerable<long> productIds)
        {
            this.productIds = productIds.ToArray();
        }

        public override string ToString()
        {
            return $"?productIds={string.Join(',', productIds)}";
        }
    }

    [Serializable]
    internal class AssetStoreUpdateInfo
    {
        public long productId;
        public string recommendedMinUnityVersion;
        public long recommendedUploadId;
    }

    internal partial class JsonParser
    {
        public virtual List<AssetStoreUpdateInfo> ParseUpdateInfos(IDictionary<string, object> rawList)
        {
            var result = new List<AssetStoreUpdateInfo>();
            foreach(var entry in rawList)
            {
                if(!long.TryParse(entry.Key, out var productId))
                    continue;
                try
                {
                    var data = (IDictionary<string,object>)entry.Value;
                    if (data == null)
                        continue;
                    result.Add(new AssetStoreUpdateInfo
                    {
                        productId = productId,
                        recommendedUploadId = data.GetStringAsLong("recommended_upload_id"),
                        recommendedMinUnityVersion = data.GetString("recommended_min_unity_version")
                    });
                }
                catch (InvalidCastException)
                {
                }
            }
            return result;
        }
    }
}
