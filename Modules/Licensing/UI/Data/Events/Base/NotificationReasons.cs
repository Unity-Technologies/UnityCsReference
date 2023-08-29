// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Licensing.UI.Data.Events.Base
{
struct NotificationReasons
{
    // applicable to LicenseUpdateNotification
    public const string k_EntitlementGroupAdded = "EntitlementGroupAdded";
    public const string k_EntitlementGroupRemoved = "EntitlementGroupRemoved";
    public const string k_EntitlementGroupRevoked = "EntitlementGroupRevoked";
    public const string k_EntitlementGroupReturned = "EntitlementGroupReturned";

    // applicable to LicenseExpiredNotification
    public const string k_UpdateDateExpired = "EntitlementGroupUpdateDateExpired";
    public const string k_EndDateExpired = "EntitlementGroupEndDateExpired";
}
}
