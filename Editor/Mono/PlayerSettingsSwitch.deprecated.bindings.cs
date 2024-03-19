// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor;

//==== OBSOLETE PLAYER SETTINGS ===//
//These are used ONLY to upgrade a NMETA Player Settings to an NMETA file//

public sealed partial class PlayerSettings
{
    [NativeHeader("Editor/Mono/PlayerSettingsSwitch.bindings.h")]
    public sealed partial class Switch
    {
        const string kPlayerSettingsAreObsoletedWarning = "NMETA Player Settings are obsolete";

        [NativeProperty("switchUpgradedPlayerSettingsToNMETA", TargetType.Field)]
        extern internal static bool HasUpgradedPlayerSettingsToNMETA { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        internal enum Languages
        {
            AmericanEnglish = 0,
            BritishEnglish = 1,
            Japanese = 2,
            French = 3,
            German = 4,
            LatinAmericanSpanish = 5,
            Spanish = 6,
            Italian = 7,
            Dutch = 8,
            CanadianFrench = 9,
            Portuguese = 10,
            Russian = 11,
            SimplifiedChinese = 12,
            TraditionalChinese = 13,
            Korean = 14,
            BrazilianPortuguese = 15,
        }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        internal enum StartupUserAccount
        {
            None = 0,
            Required = 1,
            RequiredWithNetworkServiceAccountAvailable = 2
        }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        internal enum LogoHandling
        {
            Auto = 0,
            Manual = 1
        }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        internal enum LogoType
        {
            LicensedByNintendo = 0,
            [Obsolete("This attribute is no longer available as of NintendoSDK 4.3.", true)]
            DistributedByNintendo = 1,
            Nintendo = 2
        }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        internal enum ApplicationAttribute
        {
            None = 0,
            Demo = 1
        }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        internal enum RatingCategories
        {
            CERO = 0,
            GRACGCRB = 1,
            GSRMR = 2,
            ESRB = 3,
            ClassInd = 4,
            USK = 5,
            PEGI = 6,
            PEGIPortugal = 7,
            PEGIBBFC = 8,
            Russian = 9,
            ACB = 10,
            OFLC = 11,
            IARCGeneric = 12,
        }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchApplicationID", TargetType.Function)]
        extern internal static string applicationID { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        extern internal static string[] titleNames
        {
            [NativeMethod("GetSwitchTitleNames")]
            get;
            [NativeMethod("SetSwitchTitleNames")]
            set;
        }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        extern internal static string[] publisherNames
        {
            [NativeMethod("GetSwitchPublisherNames")]
            get;
            [NativeMethod("SetSwitchPublisherNames")]
            set;
        }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        extern internal static Texture2D[] icons
        {
            [NativeMethod("GetSwitchIcons")]
            get;
            [NativeMethod("SetSwitchIcons")]
            set;
        }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        extern internal static Texture2D[] smallIcons
        {
            [NativeMethod("GetSwitchSmallIcons")]
            get;
            [NativeMethod("SetSwitchSmallIcons")]
            set;
        }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchAllowsVideoCapturing", TargetType.Field)]
        extern internal static bool isVideoCapturingEnabled { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchAllowsRuntimeAddOnContentInstall", TargetType.Field)]
        extern internal static bool isRuntimeAddOnContentInstallEnabled { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchDataLossConfirmation", TargetType.Field)]
        extern internal static bool isDataLossConfirmationEnabled { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchUserAccountLockEnabled", TargetType.Field)]
        extern internal static bool isUserAccountLockEnabled { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchRatingsMask", TargetType.Field)]
        extern internal static int ratingsMask { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        extern internal static string[] localCommunicationIds
        {
            [NativeMethod("GetSwitchLocalCommunicationIds")]
            get;
            [NativeMethod("SetSwitchLocalCommunicationIds")]
            set;
        }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchParentalControl", TargetType.Field)]
        extern internal static bool isUnderParentalControl { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchAllowsScreenshot", TargetType.Field)]
        extern internal static bool isScreenshotEnabled { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        internal static int cardSpecClock
        {
            get { return cardSpecClockInternal; }
            set { cardSpecClockInternal = (value > 0) ? value : -1; }
        }

        [NativeProperty("switchCardSpecClock", TargetType.Field)]
        extern static int cardSpecClockInternal { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        internal static string manualHTMLPath
        {
            get
            {
                string path = manualHTMLPathInternal;

                if (string.IsNullOrEmpty(path))
                    return "";

                string fullPath = path;

                if (!Path.IsPathRooted(fullPath))
                    fullPath = Path.GetFullPath(fullPath);

                if (!Directory.Exists(fullPath))
                    return "";

                return fullPath;
            }
            set { manualHTMLPathInternal = string.IsNullOrEmpty(value) ? "" : value; }
        }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        internal static string accessibleURLPath
        {
            get
            {
                string path = accessibleURLPathInternal;

                if (string.IsNullOrEmpty(path))
                    return "";

                string fullPath = path;

                if (!Path.IsPathRooted(fullPath))
                    fullPath = Path.GetFullPath(fullPath);

                return fullPath;
            }
            set { accessibleURLPathInternal = string.IsNullOrEmpty(value) ? "" : value; }
        }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        internal static string legalInformationPath
        {
            get
            {
                string path = legalInformationPathInternal;

                if (string.IsNullOrEmpty(path))
                    return "";

                if (!Path.IsPathRooted(path))
                    path = Path.GetFullPath(path);

                return path;
            }
            set { legalInformationPathInternal = string.IsNullOrEmpty(value) ? "" : value; }
        }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchManualHTML", TargetType.Function)]
        extern static string manualHTMLPathInternal { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchAccessibleURLs", TargetType.Function)]
        extern static string accessibleURLPathInternal { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchLegalInformation", TargetType.Function)]
        extern static string legalInformationPathInternal { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchMainThreadStackSize", TargetType.Field)]
        extern internal static int mainThreadStackSize { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchPresenceGroupId", TargetType.Function)]
        extern internal static string presenceGroupId { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchLogoHandling", TargetType.Field)]
        extern internal static LogoHandling logoHandling { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        extern internal static string releaseVersion
        {
            [NativeMethod("GetSwitchReleaseVersion")]
            get;
            [NativeMethod("SetSwitchReleaseVersion")]
            set;
        }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchDisplayVersion", TargetType.Function)]
        extern internal static string displayVersion { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchStartupUserAccount", TargetType.Field)]
        extern internal static StartupUserAccount startupUserAccount { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchSupportedLanguagesMask", TargetType.Field)]
        extern internal static int supportedLanguages { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchLogoType", TargetType.Field)]
        extern internal static LogoType logoType { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchApplicationErrorCodeCategory", TargetType.Function)]
        extern internal static string applicationErrorCodeCategory { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchUserAccountSaveDataSize", TargetType.Field)]
        extern internal static int userAccountSaveDataSize { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchUserAccountSaveDataJournalSize", TargetType.Field)]
        extern internal static int userAccountSaveDataJournalSize { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchApplicationAttribute", TargetType.Field)]
        extern internal static ApplicationAttribute applicationAttribute { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        internal static int cardSpecSize
        {
            get { return cardSpecSizeInternal; }
            set { cardSpecSizeInternal = (value > 0) ? value : -1; }
        }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchCardSpecSize", TargetType.Field)]
        extern private static int cardSpecSizeInternal { get; set; }

        // System Memory (used for virtual memory mapping).
        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        [NativeProperty("switchSystemResourceMemory", TargetType.Field)]
        extern internal static int systemResourceMemory { get; set; }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        internal static int GetRatingAge(RatingCategories category)
        {
            return ratingAgeArray[(int)category];
        }

        [Obsolete(kPlayerSettingsAreObsoletedWarning)]
        extern internal static int[] ratingAgeArray
        {
            [NativeMethod("GetSwitchRatingAges")]
            get;
            [NativeMethod("SetSwitchRatingAges")]
            set;
        }

    }
}
