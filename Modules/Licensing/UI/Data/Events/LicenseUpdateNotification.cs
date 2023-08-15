// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEditor.Licensing.UI.Data.Events.Base;

namespace UnityEditor.Licensing.UI.Data.Events;

[Serializable]
class LicenseUpdateNotification : NotificationWithDetails<LicenseUpdateNotificationDetails[]>
{
    public bool HasAnyReason(string reason)
    {
        foreach (var detail in details)
        {
            if (detail.reason == reason)
            {
                return true;
            }
        }

        return false;
    }

    public IList<string> GetProductNamesWithReason(string theReason)
    {
        var productNames = new List<string>();
        foreach (var detail in details)
        {
            if (detail.reason == theReason)
            {
                productNames.Add(detail.productName);
            }
        }

        return productNames;
    }
}

[Serializable]
class LicenseUpdateNotificationDetails
{
    public string entitlementGroupId;
    public string productName;
    public string updateType;
    public string reason;
}
