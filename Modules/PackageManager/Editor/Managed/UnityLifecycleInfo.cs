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
        private string m_Nextversion;

        public string version => m_Version;
        public string nextVersion => m_Nextversion;

        internal UnityLifecycleInfo() : this("", "") {}

        internal UnityLifecycleInfo(string version, string nextVersion)
        {
            m_Version = version;
            m_Nextversion = nextVersion;
        }
    }
}
