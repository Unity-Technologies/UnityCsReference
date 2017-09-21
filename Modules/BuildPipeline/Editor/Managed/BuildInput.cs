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
    [NativeHeader("Modules/BuildPipeline/Editor/Public/AssetBundleBuildInput.h")]
    public struct BuildInput
    {
        [Serializable]
        [UsedByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        public struct AssetIdentifier
        {
            [NativeName("asset")]
            internal GUID m_Asset;
            public GUID asset
            {
                get { return m_Asset; }
                set { m_Asset = value; }
            }

            [NativeName("address")]
            internal string m_Address;
            public string address
            {
                get { return m_Address; }
                set { m_Address = value; }
            }
        }

        [Serializable]
        [UsedByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        public struct Definition
        {
            [NativeName("assetBundleName")]
            internal string m_AssetBundleName;
            public string assetBundleName
            {
                get { return m_AssetBundleName; }
                set { m_AssetBundleName = value; }
            }

            [NativeName("explicitAssets")]
            internal AssetIdentifier[] m_ExplicitAssets;
            public AssetIdentifier[] explicitAssets
            {
                get { return m_ExplicitAssets; }
                set { m_ExplicitAssets = value; }
            }
        }

        [NativeName("definitions")]
        internal Definition[] m_Definitions;
        public Definition[] definitions
        {
            get { return m_Definitions; }
            set { m_Definitions = value; }
        }
    }
}
