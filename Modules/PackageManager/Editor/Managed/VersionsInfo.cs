// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
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
    public class VersionsInfo
    {
        [SerializeField]
        [NativeName("all")]
        private string[] m_All;
        [SerializeField]
        [NativeName("compatible")]
        private string[] m_Compatible;
        [SerializeField]
        [NativeName("recommended")]
        private string m_Recommended;

        private VersionsInfo() {}

        internal VersionsInfo(
            IEnumerable<string> all,
            IEnumerable<string> compatible,
            string recommended)
        {
            m_All = (all ?? new string[] {}).ToArray();
            m_Compatible = (compatible ?? new string[] {}).ToArray();
            m_Recommended = recommended ?? string.Empty;
        }

        public string[] all { get { return m_All; } }
        public string[] compatible { get { return m_Compatible; } }
        public string recommended { get { return m_Recommended; } }

        public string latest
        {
            get
            {
                return (all.LastOrDefault() ?? string.Empty);
            }
        }

        public string latestCompatible
        {
            get
            {
                return (compatible.LastOrDefault() ?? string.Empty);
            }
        }
    }
}

