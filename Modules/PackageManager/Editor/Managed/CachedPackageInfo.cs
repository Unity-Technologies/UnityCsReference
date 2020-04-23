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
    internal class CachedPackageInfo
    {
        [SerializeField]
        [NativeName("name")]
        private string m_Name = "";

        [SerializeField]
        [NativeName("displayName")]
        private string m_DisplayName = "";

        [SerializeField]
        [NativeName("description")]
        private string m_Description = "";

        [SerializeField]
        [NativeName("latest")]
        private string m_Latest = "";

        [SerializeField]
        [NativeName("versions")]
        private string[] m_Versions;

        public string name
        {
            get
            {
                return m_Name;
            }
        }

        public string displayName
        {
            get
            {
                return m_DisplayName;
            }
        }

        public string description
        {
            get
            {
                return m_Description;
            }
        }

        public string latest
        {
            get
            {
                return m_Latest;
            }
        }

        public string[] versions
        {
            get
            {
                return m_Versions;
            }
        }
    }
}
