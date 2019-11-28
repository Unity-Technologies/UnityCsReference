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
    [NativeType(IntermediateScriptingStructName = "PackageManager_SearchResults")]
    internal sealed class SearchResults
    {
        [SerializeField]
        [NativeName("capabilities")]
        private SearchCapabilities m_Capabilities = new SearchCapabilities();

        [SerializeField]
        [NativeName("packages")]
        private SearchResultEntry[] m_Packages;

        [SerializeField]
        [NativeName("total")]
        private ulong m_Total = 0;

        internal SearchResults() {}

        public ulong total { get { return m_Total;  } }

        public SearchCapabilities capabilities
        {
            get
            {
                return m_Capabilities;
            }
        }

        public SearchResultEntry[] packages
        {
            get
            {
                return m_Packages;
            }
        }
    }
}
