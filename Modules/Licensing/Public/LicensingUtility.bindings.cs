// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting.APIUpdating;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.Licensing
{
    [MovedFrom("UnityEditor.Experimental.Licensing")]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeAsStruct]
    public class EntitlementGroupInfo
    {
        [SerializeField]
        private string m_Expiration_ts;

        // Milliseconds since the Unix epoch, as sent by the licensing client.
        // Empty or "0" means the entitlement group does not expire.
        public string Expiration_ts { get { return m_Expiration_ts;  } }

        // False when the entitlement group does not expire, or when the value could not be read as a date.
        public bool TryGetExpirationUtc(out DateTime expirationUtc)
        {
            expirationUtc = default;

            if (string.IsNullOrEmpty(m_Expiration_ts) || m_Expiration_ts == "0")
                return false;

            if (!long.TryParse(m_Expiration_ts, NumberStyles.None, CultureInfo.InvariantCulture, out var milliseconds))
                return false;

            if (milliseconds > DateTimeOffset.MaxValue.ToUnixTimeMilliseconds())
                return false;

            expirationUtc = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
            return true;
        }

        [SerializeField]
        string m_EntitlementGroupId;
        public string EntitlementGroupId { get { return m_EntitlementGroupId;  } }

        [SerializeField]
        string m_ProductName;
        public string ProductName { get { return m_ProductName;  } }

        [SerializeField]
        string m_LicenseType;
        public string LicenseType { get { return m_LicenseType;  } }
    }

    [MovedFrom("UnityEditor.Experimental.Licensing")]
    public enum EntitlementStatus
    {
        Unknown = 0,
        Granted = 1,
        NotGranted = 2,
        Free = 3,
    }

    [MovedFrom("UnityEditor.Experimental.Licensing")]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeAsStruct]
    public class EntitlementInfo
    {
        [SerializeField]
        string m_EntitlementId;
        public string EntitlementId { get { return m_EntitlementId;  } }

        [SerializeField]
        EntitlementStatus m_Status;
        public EntitlementStatus Status { get { return m_Status;  } }

        [SerializeField]
        bool m_IsPackage;
        public bool IsPackage { get { return m_IsPackage;  } }

        [SerializeField]
        int m_Count;
        public int Count { get { return m_Count;  } }

        [SerializeField]
        string m_CustomData;
        public string CustomData { get { return m_CustomData;  } }

        [SerializeField]
        EntitlementGroupInfo[] m_EntitlementGroupsData;
        public EntitlementGroupInfo[] EntitlementGroupsData { get { return m_EntitlementGroupsData;  } }
    }

    [MovedFrom("UnityEditor.Experimental.Licensing")]
    [NativeHeader("Modules/Licensing/Public/LicensingUtility.bindings.h")]
    public static partial class LicensingUtility
    {
        [NativeMethod("HasEntitlement", IsThreadSafe = true)]
        public extern static bool HasEntitlement(string entitlement);

        [NativeMethod("HasEntitlements")]
        public extern static string[] HasEntitlements(string[] entitlements);

        [NativeMethod("IsOnPremiseLicensingEnabled")]
        internal extern static bool IsOnPremiseLicensingEnabled();

        [NativeMethod("HasEntitlementsExtended")]
        public extern static EntitlementInfo[] HasEntitlementsExtended(string[] entitlements, bool includeCustomData);

        [NativeMethod("GetEditorLicenseIdentifier")]
        internal extern static string GetEditorLicenseIdentifier(bool displayFormat);

        [NativeMethod("GetAuthToken")]
        public extern static string GetAuthToken();


        public static bool HasPro() { return HasEntitlement(CommonEntitlements.UseLegacyProFlag); }

        public static bool HasEduLicense() { return HasEntitlement(CommonEntitlements.UseEduWatermark); }

        // Mirrors ILicensing::EntitlementResultMap::IsPersonal; a requested entitlement is always
        // present in the native result map, so absent from the granted ids means not granted
        public static bool IsPersonal()
        {
            var granted = HasEntitlements(new[] { CommonEntitlements.UseEditorUI, CommonEntitlements.DisableSplashScreen });

            return Array.IndexOf(granted, CommonEntitlements.UseEditorUI) >= 0
                && Array.IndexOf(granted, CommonEntitlements.DisableSplashScreen) < 0;
        }

        public static extern void InvokeLicenseUpdateCallbacks();

        public static extern bool UpdateLicense();
    }

}
