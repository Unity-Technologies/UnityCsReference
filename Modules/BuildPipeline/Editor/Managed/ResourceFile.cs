// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Experimental.Build.AssetBundle
{
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeHeader("Modules/BuildPipeline/Editor/Public/AssetBundleBuildInterface.h")]
    public struct ResourceFile
    {
        [NativeName("fileName")]
        internal string m_FileName;
        public string fileName { get { return m_FileName; } }

        [NativeName("fileAlias")]
        internal string m_FileAlias;
        public string fileAlias { get { return m_FileAlias; } }

        [NativeName("serializedFile")]
        internal bool m_SerializedFile;
        public bool serializedFile { get { return m_SerializedFile; } }
    }
}
