// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEditor.Experimental.Licensing;
using UnityEngine;
using UnityEngine.Bindings;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.PackageManager
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeAsStruct]
    internal class EntitlementsInfo
    {
        [SerializeField]
        [NativeName("isAllowed")]
        private bool m_IsAllowed;

        [SerializeField]
        [NativeName("licensingModel")]
        private EntitlementLicensingModel m_LicensingModel;

        [SerializeField]
        [NativeName("status")]
        private EntitlementStatus m_Status;

        public bool isAllowed { get { return m_IsAllowed;  } }

        public EntitlementLicensingModel licensingModel { get { return m_LicensingModel;  } }

        public EntitlementStatus status { get { return m_Status;  } }
    }
}
