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
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    [NativeAsStruct]
    internal class PackageProgress
    {
        [NativeName("name")]
        private string m_Name;

        [NativeName("version")]
        private string m_Version;

        [NativeName("state")]
        private ProgressState m_State;

        [NativeName("currentBytes")]
        private ulong m_CurrentBytes;

        [NativeName("totalBytes")]
        private ulong m_TotalBytes;

        public string version { get { return m_Version;  } }
        public string name { get { return m_Name;  } }
        public ProgressState state { get { return m_State;  } }
        public DownloadProgress? download
        {
            get
            {
                if (m_State == ProgressState.Downloading || m_TotalBytes > 0)
                {
                    return new DownloadProgress(m_CurrentBytes, m_TotalBytes);
                }

                return null;
            }
        }
    }
}
