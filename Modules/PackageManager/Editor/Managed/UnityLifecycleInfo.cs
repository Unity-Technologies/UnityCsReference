// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.PackageManager.UnityLifecycle
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeAsStruct]
    internal class UnityLifecycleInfo
    {
        [SerializeField]
        [NativeName("version")]
        private string m_Version;

        [SerializeField]
        [NativeName("nextVersion")]
        private string m_NextVersion;

        [SerializeField]
        [NativeName("recommendedVersion")]
        private string m_RecommendedVersion;

        [SerializeField]
        [NativeName("isDeprecated")]
        private bool m_IsDeprecated;

        [SerializeField]
        [NativeName("deprecationMessage")]
        private string m_DeprecationMessage;

        [SerializeField]
        [NativeName("isDiscoverable")]
        private bool m_IsDiscoverable;

        public string version => m_Version;
        public string nextVersion => m_NextVersion;
        public string recommendedVersion => m_RecommendedVersion;

        public bool isDeprecated => m_IsDeprecated;
        public string deprecationMessage => m_DeprecationMessage;

        public bool isDiscoverable => m_IsDiscoverable;

        internal UnityLifecycleInfo() : this("", "", "", false, "", false) {}

        internal UnityLifecycleInfo(string version, string nextVersion, string recommendedVersion, bool isDeprecated, string deprecationMessage, bool isDiscoverable)
        {
            m_Version = version;
            m_NextVersion = nextVersion;
            m_RecommendedVersion = recommendedVersion;
            m_IsDeprecated = isDeprecated;
            m_DeprecationMessage = deprecationMessage ?? "";
            m_IsDiscoverable = isDiscoverable;
        }
    }
}
