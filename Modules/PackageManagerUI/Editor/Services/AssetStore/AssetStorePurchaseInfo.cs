// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.PackageManager.UI
{
    [Serializable]
    internal class AssetStorePurchaseInfo
    {
        public long productId;
        public string purchasedTime;
        public string displayName;
        public List<string> tags;

        public static AssetStorePurchaseInfo ParsePurchaseInfo(IDictionary<string, object> rawInfo)
        {
            if (rawInfo?.Any() != true)
                return null;

            try
            {
                return new AssetStorePurchaseInfo
                {
                    productId = (long)rawInfo["packageId"],
                    purchasedTime = rawInfo.GetString("grantTime"),
                    displayName = rawInfo.GetString("displayName"),
                    tags = rawInfo.GetList<string>("tagging")?.ToList()
                };
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
