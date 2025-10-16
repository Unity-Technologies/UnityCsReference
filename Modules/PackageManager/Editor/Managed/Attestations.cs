// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.PackageManager
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeAsStruct]
    internal class Attestation
    {
        [SerializeField]
        [NativeName("format")]
        private string m_Format;

        [SerializeField]
        [NativeName("publishingChannel")]
        private string m_PublishingChannel;

        [SerializeField]
        [NativeName("entitlementKeys")]
        private string[] m_EntitlementKeys;

        [SerializeField]
        [NativeName("ownerOrgId")]
        private string m_OwnerOrgId;

        [SerializeField]
        [NativeName("ownerOrgName")]
        private string m_OwnerOrgName;

        [SerializeField]
        [NativeName("publisherName")]
        private string m_PublisherName;

        public string format => m_Format;
        public string publishingChannel => m_PublishingChannel;
        public string[] entitlementKeys => m_EntitlementKeys;
        public string ownerOrgId => m_OwnerOrgId;
        public string ownerOrgName => m_OwnerOrgName;

        public string publisherName => m_PublisherName;

        internal Attestation() { }
    }
}
