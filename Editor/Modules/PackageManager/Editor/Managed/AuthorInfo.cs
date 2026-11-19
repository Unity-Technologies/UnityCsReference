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
    public class AuthorInfo
    {
        [SerializeField]
        [NativeName("name")]
        private string m_Name;

        [SerializeField]
        [NativeName("email")]
        private string m_Email;

        [SerializeField]
        [NativeName("url")]
        private string m_Url;

        internal AuthorInfo() : this("", "", "") {}

        internal AuthorInfo(string name, string email, string url)
        {
            m_Name = name;
            m_Email = email;
            m_Url = url;
        }

        public string name { get { return m_Name;  } }
        public string email { get { return m_Email;  } }
        public string url { get { return m_Url;  } }
    }
}
