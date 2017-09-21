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
    public struct BuildCommandSet
    {
        [Serializable]
        [UsedByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        public struct AssetLoadInfo
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

            [NativeName("processedScene")]
            internal string m_ProcessedScene;
            public string processedScene
            {
                get { return m_ProcessedScene; }
                set { m_ProcessedScene = value; }
            }

            [NativeName("includedObjects")]
            internal ObjectIdentifier[] m_IncludedObjects;
            public ObjectIdentifier[] includedObjects
            {
                get { return m_IncludedObjects; }
                set { m_IncludedObjects = value; }
            }

            [NativeName("referencedObjects")]
            internal ObjectIdentifier[] m_ReferencedObjects;
            public ObjectIdentifier[] referencedObjects
            {
                get { return m_ReferencedObjects; }
                set { m_ReferencedObjects = value; }
            }
        }

        [Serializable]
        [UsedByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        public struct SerializationInfo
        {
            [NativeName("serializationObject")]
            internal ObjectIdentifier m_SerializationObject;
            public ObjectIdentifier serializationObject
            {
                get { return m_SerializationObject; }
                set { m_SerializationObject = value; }
            }

            [NativeName("serializationIndex")]
            internal long m_SerializationIndex;
            public long serializationIndex
            {
                get { return m_SerializationIndex; }
                set { m_SerializationIndex = value; }
            }
        }

        [Serializable]
        [UsedByNativeCode]
        [StructLayout(LayoutKind.Sequential)]
        public struct Command
        {
            [NativeName("assetBundleName")]
            internal string m_AssetBundleName;
            public string assetBundleName
            {
                get { return m_AssetBundleName; }
                set { m_AssetBundleName = value; }
            }

            [NativeName("explicitAssets")]
            internal AssetLoadInfo[] m_ExplicitAssets;
            public AssetLoadInfo[] explicitAssets
            {
                get { return m_ExplicitAssets; }
                set { m_ExplicitAssets = value; }
            }

            [NativeName("assetBundleObjects")]
            internal SerializationInfo[] m_AssetBundleObjects;
            public SerializationInfo[] assetBundleObjects
            {
                get { return m_AssetBundleObjects; }
                set { m_AssetBundleObjects = value; }
            }

            [NativeName("assetBundleDependencies")]
            internal string[] m_AssetBundleDependencies;
            public string[] assetBundleDependencies
            {
                get { return m_AssetBundleDependencies; }
                set { m_AssetBundleDependencies = value; }
            }

            [NativeName("sceneBundle")]
            internal bool m_SceneBundle;
            public bool sceneBundle
            {
                get { return m_SceneBundle; }
                set { m_SceneBundle = value; }
            }

            [NativeName("globalUsage")]
            internal BuildUsageTagGlobal m_GlobalUsage;
            public BuildUsageTagGlobal globalUsage
            {
                get { return m_GlobalUsage; }
                set { m_GlobalUsage = value; }
            }
        }

        [NativeName("commands")]
        internal Command[] m_Commands;
        public Command[] commands
        {
            get { return m_Commands; }
            set { m_Commands = value; }
        }
    }
}
