// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Build.Content
{
    [Serializable]
    [UsedByNativeCode]
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions = CodegenOptions.Custom)]
    [NativeHeader("Modules/BuildPipeline/Editor/Shared/ContentBuildInterface.bindings.h")]
    public class PreloadInfo
    {
        [NativeName("preloadObjects")]
        internal List<ObjectIdentifier> m_PreloadObjects;
        public List<ObjectIdentifier> preloadObjects
        {
            get { return m_PreloadObjects; }
            set { m_PreloadObjects = value; }
        }
    }

    [Serializable]
    [UsedByNativeCode]
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions = CodegenOptions.Custom)]
    [NativeHeader("Modules/BuildPipeline/Editor/Shared/ContentBuildInterface.bindings.h")]
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
    [NativeType(CodegenOptions = CodegenOptions.Custom)]
    [NativeHeader("Modules/BuildPipeline/Editor/Shared/ContentBuildInterface.bindings.h")]
    public class AssetBundleInfo
    {
        [NativeName("bundleName")]
        private string m_BundleName;
        public string bundleName
        {
            get { return m_BundleName; }
            set { m_BundleName = value; }
        }

        [NativeName("bundleAssets")]
        private List<AssetLoadInfo> m_BundleAssets;
        public List<AssetLoadInfo> bundleAssets
        {
            get { return m_BundleAssets; }
            set { m_BundleAssets = value; }
        }
    }


    [Serializable]
    [UsedByNativeCode]
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions = CodegenOptions.Custom)]
    [NativeHeader("Modules/BuildPipeline/Editor/Shared/ContentBuildInterface.bindings.h")]
    public class SceneLoadInfo
    {
        [NativeName("asset")]
        private GUID m_Asset;
        public GUID asset
        {
            get { return m_Asset; }
            set { m_Asset = value; }
        }

        [NativeName("address")]
        private string m_Address;
        public string address
        {
            get { return m_Address; }
            set { m_Address = value; }
        }

        [NativeName("internalName")]
        private string m_InternalName;
        public string internalName
        {
            get { return m_InternalName; }
            set { m_InternalName = value; }
        }
    }

    [Serializable]
    [UsedByNativeCode]
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions = CodegenOptions.Custom)]
    [NativeHeader("Modules/BuildPipeline/Editor/Shared/ContentBuildInterface.bindings.h")]
    public class SceneBundleInfo
    {
        [NativeName("bundleName")]
        private string m_BundleName;
        public string bundleName
        {
            get { return m_BundleName; }
            set { m_BundleName = value; }
        }

        [NativeName("bundleScenes")]
        private List<SceneLoadInfo> m_BundleScenes;
        public List<SceneLoadInfo> bundleScenes
        {
            get { return m_BundleScenes; }
            set { m_BundleScenes = value; }
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
    [NativeHeader("Modules/BuildPipeline/Editor/Shared/ContentBuildInterface.bindings.h")]
    public class WriteCommand
    {
        [NativeName("fileName")]
        private string m_FileName;
        public string fileName
        {
            get { return m_FileName; }
            set { m_FileName = value; }
        }

        [NativeName("internalName")]
        private string m_InternalName;
        public string internalName
        {
            get { return m_InternalName; }
            set { m_InternalName = value; }
        }

        [NativeName("serializeObjects")]
        private List<SerializationInfo> m_SerializeObjects;
        public List<SerializationInfo> serializeObjects
        {
            get { return m_SerializeObjects; }
            set { m_SerializeObjects = value; }
        }
    }
}
