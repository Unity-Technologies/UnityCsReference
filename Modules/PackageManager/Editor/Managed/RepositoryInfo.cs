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
    public class RepositoryInfo
    {
        [SerializeField]
        [NativeName("type")]
        private string m_Type;

        [SerializeField]
        [NativeName("url")]
        private string m_Url;

        [SerializeField]
        [NativeName("revision")]
        private string m_Revision;

        [SerializeField]
        [NativeName("path")]
        private string m_Path;

        internal RepositoryInfo() : this("", "", "", "") {}

        internal RepositoryInfo(string type, string url, string revision, string path)
        {
            m_Type = type;
            m_Url = url;
            m_Revision = revision;
            m_Path = path;
        }

        public string type => m_Type;
        public string url => m_Url;
        public string revision => m_Revision;
        public string path => m_Path;
    }
}
