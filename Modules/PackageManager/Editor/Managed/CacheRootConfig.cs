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
    internal class CacheRootConfig
    {
        [SerializeField]
        [NativeName("path")]
        private string m_Path = "";

        [SerializeField]
        [NativeName("source")]
        private ConfigSource m_Source = ConfigSource.Unknown;

        public string path
        {
            get
            {
                return m_Path;
            }
        }

        public ConfigSource source
        {
            get
            {
                return m_Source;
            }
        }
    }
}
