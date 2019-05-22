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
    [RequiredByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeAsStruct]
    public class GitInfo
    {
        [SerializeField]
        [NativeName("hash")]
        private string m_Hash;

        [SerializeField]
        [NativeName("revision")]
        private string m_Revision;

        internal GitInfo() {}

        internal GitInfo(string hash, string revision)
        {
            m_Hash = hash;
            m_Revision = revision;
        }

        public string hash { get { return m_Hash;  } }
        public string revision { get { return m_Revision;  } }
    }
}
