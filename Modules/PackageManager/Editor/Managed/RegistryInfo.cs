// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.PackageManager
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeAsStruct]
    public class RegistryInfo
    {
        [SerializeField]
        [NativeName("id")]
        private string m_Id;

        [SerializeField]
        [NativeName("name")]
        private string m_Name;

        [SerializeField]
        [NativeName("url")]
        private string m_Url;

        [SerializeField]
        [NativeName("scopes")]
        private string[] m_Scopes;

        [SerializeField]
        [NativeName("isDefault")]
        private bool m_IsDefault;

        internal RegistryInfo() : this("", "", "", new string[0], false) {}

        internal RegistryInfo(string id, string name, string url, string[] scopes, bool isDefault)
        {
            m_Id = id;
            m_Name = name;
            m_Url = url;
            m_Scopes = scopes;
            m_IsDefault = isDefault;
        }

        internal string id { get { return m_Id;  } }
        public string name { get { return m_Name;  } }
        public string url { get { return m_Url;  } }
        internal string[] scopes { get { return m_Scopes; } }
        public bool isDefault { get { return m_IsDefault; } }
    }
}
