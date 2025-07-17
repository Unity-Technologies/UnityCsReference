// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Build.Content
{
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct ResourceFile
    {
        [SerializeField]
        [NativeName("fileName")]
        internal string m_FileName;
        public string fileName { get { return m_FileName; } set { m_FileName = value; } }

        [SerializeField]
        [NativeName("fileAlias")]
        internal string m_FileAlias;
        public string fileAlias { get { return m_FileAlias; }  set { m_FileAlias = value; } }

        [SerializeField]
        [NativeName("serializedFile")]
        internal bool m_SerializedFile;
        public bool serializedFile { get { return m_SerializedFile; }  set { m_SerializedFile = value; } }
    }
}
