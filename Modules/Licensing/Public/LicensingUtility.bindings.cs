// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEditor.Experimental.Licensing;
using UnityEngine;
using UnityEngine.Bindings;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.Experimental.Licensing
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeAsStruct]
    public class EntitlementGroupInfo
    {
        [SerializeField]
        private string m_Expiration_ts;
        public string Expiration_ts { get { return m_Expiration_ts;  } }

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

    public enum EntitlementStatus
    {
        Unknown,
        Granted,
        NotGranted,
        Free
    }

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

    [NativeHeader("Modules/Licensing/Public/LicensingUtility.bindings.h")]
    public static class LicensingUtility
    {
        [NativeMethod("HasEntitlement")]
        public extern static bool HasEntitlement(string entitlement);

        [NativeMethod("HasEntitlements")]
        public extern static string[] HasEntitlements(string[] entitlements);

        [NativeMethod("IsOnPremiseLicensingEnabled")]
        internal extern static bool IsOnPremiseLicensingEnabled();

        [NativeMethod("HasEntitlementsExtended")]
        public extern static EntitlementInfo[] HasEntitlementsExtended(string[] entitlements, bool includeCustomData);
    }

}
