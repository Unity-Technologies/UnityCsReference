// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Experimental.Build.AssetBundle
{
    [Serializable]
    [UsedByNativeCode]
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions = CodegenOptions.Custom)]
    [NativeHeader("Modules/BuildPipeline/Editor/Shared/AssetBundleBuildInterface.bindings.h")]
    public class AssetLoadInfo
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
        internal List<ObjectIdentifier> m_IncludedObjects;
        public List<ObjectIdentifier> includedObjects
        {
            get { return m_IncludedObjects; }
            set { m_IncludedObjects = value; }
        }

        [NativeName("referencedObjects")]
        internal List<ObjectIdentifier> m_ReferencedObjects;
        public List<ObjectIdentifier> referencedObjects
        {
            get { return m_ReferencedObjects; }
            set { m_ReferencedObjects = value; }
        }
    }


    [Serializable]
    [UsedByNativeCode]
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    public class SerializationInfo
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
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions = CodegenOptions.Custom)]
    [NativeHeader("Modules/BuildPipeline/Editor/Shared/AssetBundleBuildInterface.bindings.h")]
    public class WriteCommand
    {
        [NativeName("assetBundleName")]
        internal string m_AssetBundleName;
        public string assetBundleName
        {
            get { return m_AssetBundleName; }
            set { m_AssetBundleName = value; }
        }

        [NativeName("explicitAssets")]
        internal List<AssetLoadInfo> m_ExplicitAssets;
        public List<AssetLoadInfo> explicitAssets
        {
            get { return m_ExplicitAssets; }
            set { m_ExplicitAssets = value; }
        }

        [NativeName("assetBundleObjects")]
        internal List<SerializationInfo> m_AssetBundleObjects;
        public List<SerializationInfo> assetBundleObjects
        {
            get { return m_AssetBundleObjects; }
            set { m_AssetBundleObjects = value; }
        }

        [NativeName("assetBundleDependencies")]
        internal List<string> m_AssetBundleDependencies;
        public List<string> assetBundleDependencies
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

    [Serializable]
    [UsedByNativeCode]
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions = CodegenOptions.Custom)]
    [NativeHeader("Modules/BuildPipeline/Editor/Shared/AssetBundleBuildInterface.bindings.h")]
    public class BuildCommandSet
    {
        [NativeName("commands")]
        internal List<WriteCommand> m_Commands;
        public List<WriteCommand> commands
        {
            get { return m_Commands; }
            set { m_Commands = value; }
        }
    }
}
