// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace UnityEditor.Licensing.UI.Events.Text;

static class LicenseTrStrings
{
    // Buttons
    public static readonly string BtnCloseProject = L10n.Tr("Quit", null);
    public static readonly string BtnSaveAndQuit = L10n.Tr("Save and Quit", null);
    public static readonly string BtnManageLicense = L10n.Tr("Manage License", null);
    public static readonly string BtnOk = L10n.Tr("OK", null);
    public static readonly string BtnClose = L10n.Tr("Close", null);
    public static readonly string BtnConnect = L10n.Tr("Connect", null);
    public static readonly string BtnOpenUnityHub = L10n.Tr("Open Unity Hub", null);

    // LicenseRevokedWindowContents
    public static readonly string RevokedWindowTitle = L10n.Tr("License revoked", null);
    public static readonly string RevokedDescriptionNoUiEntitlement = L10n.Tr("Your license is revoked and your project will close. Contact your organization manager if you think your license was revoked by mistake.", null);
    public static readonly string RevokedDescriptionWithUiEntitlement = L10n.Tr("You can continue to work in the Unity Editor, but you'll lose access to the features or packages associated with this license. Contact your organization manager if you think your license was revoked by mistake.", null);
    public static readonly string RevokedShortTitleOneLicense = L10n.Tr("Your {0} license has been revoked.", null);
    public static readonly string RevokedShortTitleManyLicenses = L10n.Tr("Your {0} licenses have been revoked.", null);
    public static readonly string RemovedWindowTitle = L10n.Tr("License removed", null);
    public static readonly string RemovedDescriptionOneLicenseNoUiEntitlement = L10n.Tr("Your license for {0} has been removed and your project will close. To reactivate the license, use the Unity Hub to resolve or contact your administrator.", null);
    public static readonly string RemovedDescriptionManyLicensesNoUiEntitlement = L10n.Tr("Your licenses for {0} have been removed and your project will close. To reactivate the license, use the Unity Hub to resolve or contact your administrator.", null);
    public static readonly string RemovedDescriptionOneLicenseWithUiEntitlement = L10n.Tr("Your license for {0} has been removed. You can continue to use the Unity Editor, but some features might be unavailable. To reactivate the license, use the Unity Hub to resolve or contact your administrator.", null);
    public static readonly string RemovedDescriptionManyLicensesWithUiEntitlement = L10n.Tr("Your licenses for {0} have been removed. You can continue to use the Unity Editor, but some features might be unavailable. To reactivate the license, use the Unity Hub to resolve or contact your administrator.", null);

    // LicenseReturnedWindowContents
    public static readonly string ReturnedWindowTitle = L10n.Tr("License returned", null);
    public static readonly string ReturnedDescriptionOneLicenseNoUiEntitlement = L10n.Tr("Your license for {0} has been returned and your project will close. Activate your license again if you want to continue working with the Unity Editor.", null);
    public static readonly string ReturnedDescriptionManyLicensesNoUiEntitlement = L10n.Tr("Your licenses for {0} have been returned and your project will close. Activate your license again if you want to continue working with the Unity Editor.", null);
    public static readonly string ReturnedDescriptionOneLicenseWithUiEntitlement = L10n.Tr("Your license for {0} has been returned. You can continue to use the Unity Editor, but some features might be unavailable. To reactivate the license, use the Unity Hub to resolve or contact your administrator.", null);
    public static readonly string ReturnedDescriptionManyLicensesWithUiEntitlement = L10n.Tr("Your licenses for {0} have been returned. You can continue to use the Unity Editor, but some features might be unavailable. To reactivate the license, use the Unity Hub to resolve or contact your administrator.", null);

    // LicenseExpiredWindowContents
    public static readonly string ExpiredWindowTitle = L10n.Tr("License expired", null);
    public static readonly string ExpiredDescriptionOneLicenseNoUiEntitlement = L10n.Tr("Your license for {0} has been expired and your project will close. To reactivate the license, contact your administrator or use the Unity Hub to resolve.", null);
    public static readonly string ExpiredDescriptionManyLicensesNoUiEntitlement = L10n.Tr("Your licenses for {0} have been expired and your project will close. To reactivate the license, contact your administrator or use the Unity Hub to resolve.", null);
    public static readonly string ExpiredDescriptionOneLicenseWithUiEntitlement = L10n.Tr("Your license for {0} has been expired. You can continue to use the Unity Editor, but some features might be unavailable. To reactivate the license, use the Unity Hub to resolve or contact your administrator.", null);
    public static readonly string ExpiredDescriptionManyLicensesWithUiEntitlement = L10n.Tr("Your licenses for {0} have been expired. You can continue to use the Unity Editor, but some features might be unavailable. To reactivate the license, use the Unity Hub to resolve or contact your administrator.", null);

    // LicenseOfflineValidityEndingWindowContents
    public static readonly string OfflineValidityEndingWindowTitle = L10n.Tr("Are you online?", null);
    public static readonly string OfflineValidityEndingShortTitleOneDay = L10n.Tr("Connect to the internet and confirm your license. Your license will become invalid if you stay offline for more than {0} day.", null);
    public static readonly string OfflineValidityEndingShortTitleManyDays = L10n.Tr("Connect to the internet and confirm your license. Your license will become invalid if you stay offline for more than {0} days.", null);
    public static readonly string OfflineValidityEndingDescriptionOneLicenseOneDay = L10n.Tr("You seem to be offline while using a {0} license. Connect to the internet to confirm your license. Your offline grace period ends in 1 day.", null);
    public static readonly string OfflineValidityEndingDescriptionOneLicenseManyDays = L10n.Tr("You seem to be offline while using a {0} license. Connect to the internet to confirm your license. Your offline grace period ends in {1} days.", null);
    public static readonly string OfflineValidityEndingDescriptionManyLicensesOneDay = L10n.Tr("You seem to be offline while using {0} licenses. Connect to the internet to confirm your license. Your offline grace period ends in 1 day.", null);
    public static readonly string OfflineValidityEndingDescriptionManyLicensesManyDays = L10n.Tr("You seem to be offline while using {0} licenses. Connect to the internet to confirm your license. Your offline grace period ends in {1} days.", null);

    // LicenseOfflineValidityEndedWindowContents
    public static readonly string OfflineValidityEndedWindowTitle = L10n.Tr("Are you online?", null);
    public static readonly string OfflineValidityEndedDescriptionOneLicense = L10n.Tr("You seem to be offline while using a {0} license. Your offline grace period has ended and your project will close unless you connect to the internet and confirm your license.", null);
    public static readonly string OfflineValidityEndedDescriptionManyLicenses = L10n.Tr("You seem to be offline while using {0} licenses. Your offline grace period has ended and your project will close unless you connect to the internet and confirm your licenses.", null);

    // EntitlementGroupAdded
    public static readonly string LicenseAddedTag = L10n.Tr("License added", null);
    public static readonly string LicenseAddedDescription = L10n.Tr("New {0} license has been added.", null);
    public static readonly string LicensesAddedDescription = L10n.Tr("New {0} licenses have been added.", null);

    // EntitlementGroupRemoved
    public static readonly string LicenseRemovedTag = L10n.Tr("License removed", null);

    // EntitlementGroupRevoked
    public static readonly string LicenseRevokedTag = L10n.Tr("License revoked", null);
    public static readonly string LicenseRevokedDescription = L10n.Tr("{0} license has been revoked.", null);
    public static readonly string LicensesRevokedDescription = L10n.Tr("{0} licenses have been revoked.", null);

    // EntitlementGroupReturned
    public static readonly string LicenseReturnedTag = L10n.Tr("License returned", null);

    // LicenseOfflineValidityPeriodEnding
    public static readonly string LicenseOfflineValidityEndingTag = L10n.Tr("License offline grace period ends soon", null);

    // LicenseExpired
    public static readonly string LicenseUpdateDateExpiredTag = L10n.Tr("License offline grace period expired", null);
    public static readonly string LicenseExpiredTag = L10n.Tr("License expired", null);
}
