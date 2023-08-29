// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor.Licensing.UI.Data.Events.Base;

namespace UnityEditor.Licensing.UI.Data.Events
{
[Serializable]
class LicenseOfflineValidityEndingNotification : NotificationWithDetails<LicenseOfflineValidityEndingNotificationDetails[]> { }

[Serializable]
class LicenseOfflineValidityEndingNotificationDetails
{
    public string entitlementGroupId;
    public string productName;
    public string endDate;
}
}
