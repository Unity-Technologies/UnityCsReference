// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEditor.Build.Player;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Build.Content
{
    ///<summary>Container for holding a list of preload objects for a Scene to be built.</summary>
    ///<remarks>Note: this class and its members exist to provide low-level support for the **Scriptable Build Pipeline** package. This is intended for internal use only; use the &lt;a href="https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@latest/index.html"&gt;Scriptable Build Pipeline package&lt;/a&gt; to implement a fully featured build pipeline. You can install this via the [Package Manager window](/upm-ui.md).</remarks>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions = CodegenOptions.Custom)]
    [NativeHeader("Modules/ContentBuild/Editor/SBPSupport/ContentBuildInterface.bindings.h")]
    public class PreloadInfo
    {
        [NativeName("preloadObjects")]
        internal List<ObjectIdentifier> m_PreloadObjects;
        ///<summary>List of Objects for a serialized Scene that need to be preloaded.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.PreloadInfo" />.</remarks>
        public List<ObjectIdentifier> preloadObjects
        {
            get { return m_PreloadObjects; }
            set { m_PreloadObjects = value; }
        }
    }

    ///<summary>Container for holding preload information for a given serialized Asset.</summary>
    ///<remarks>Note: this class and its members exist to provide low-level support for the **Scriptable Build Pipeline** package. This is intended for internal use only; use the &lt;a href="https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@latest/index.html"&gt;Scriptable Build Pipeline package&lt;/a&gt; to implement a fully featured build pipeline. You can install this via the [Package Manager window](/upm-ui.md).</remarks>
    [Serializable]
    [UsedByNativeCode]
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions = CodegenOptions.Custom)]
    [NativeHeader("Modules/ContentBuild/Editor/SBPSupport/ContentBuildInterface.bindings.h")]
    public class AssetLoadInfo
    {
        [NativeName("asset")]
        internal GUID m_Asset;
        ///<summary>GUID for the given asset.</summary>
        ///<remarks>Internal use only. See <see cref="AssetLoadInfo" />.</remarks>
        public GUID asset
        {
            get { return m_Asset; }
            set { m_Asset = value; }
        }

        [NativeName("address")]
        internal string m_Address;
        ///<summary>Friendly name used to load the built asset.</summary>
        ///<remarks>Internal use only. See <see cref="AssetLoadInfo" />.</remarks>
        public string address
        {
            get { return m_Address; }
            set { m_Address = value; }
        }

        [NativeName("includedObjects")]
        internal List<ObjectIdentifier> m_IncludedObjects;
        ///<summary>List of objects that an asset contains in its source file.</summary>
        ///<remarks>Internal use only. See <see cref="AssetLoadInfo" />.</remarks>
        public List<ObjectIdentifier> includedObjects
        {
            get { return m_IncludedObjects; }
            set { m_IncludedObjects = value; }
        }

        [NativeName("referencedObjects")]
        internal List<ObjectIdentifier> m_ReferencedObjects;
        ///<summary>List of objects that an asset references.</summary>
        ///<remarks>Internal use only. See <see cref="AssetLoadInfo" />.</remarks>
        public List<ObjectIdentifier> referencedObjects
        {
            get { return m_ReferencedObjects; }
            set { m_ReferencedObjects = value; }
        }
    }

    ///<summary>Container for holding asset loading information for an AssetBundle to be built.</summary>
    ///<remarks>Note: this class and its members exist to provide low-level support for the **Scriptable Build Pipeline** package. This is intended for internal use only; use the &lt;a href="https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@0.0-preview/manual/index.html"&gt;Scriptable Build Pipeline package&lt;/a&gt; to implement a fully featured build pipeline. You can install this via the [Package Manager window](/upm-ui.md).</remarks>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions = CodegenOptions.Custom)]
    [NativeHeader("Modules/ContentBuild/Editor/SBPSupport/ContentBuildInterface.bindings.h")]
    public class AssetBundleInfo
    {
        [NativeName("bundleName")]
        private string m_BundleName;
        ///<summary>Friendly AssetBundle name.</summary>
        ///<remarks>Internal use only. See <see cref="AssetBundleInfo" />.</remarks>
        public string bundleName
        {
            get { return m_BundleName; }
            set { m_BundleName = value; }
        }

        [NativeName("bundleAssets")]
        private List<AssetLoadInfo> m_BundleAssets;
        ///<summary>List of asset loading information for an AssetBundle.</summary>
        ///<remarks>Internal use only. See <see cref="AssetBundleInfo" />.</remarks>
        public List<AssetLoadInfo> bundleAssets
        {
            get { return m_BundleAssets; }
            set { m_BundleAssets = value; }
        }
    }


    ///<summary>Container for holding preload information for a given serialized Scene in an AssetBundle.</summary>
    ///<remarks>Note: this class and its members exist to provide low-level support for the **Scriptable Build Pipeline** package. This is intended for internal use only; use the &lt;a href="https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@latest/index.html"&gt;Scriptable Build Pipeline package&lt;/a&gt; to implement a fully featured build pipeline. You can install this via the [Package Manager window](/upm-ui.md).</remarks>
    [Serializable]
    [UsedByNativeCode]
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions = CodegenOptions.Custom)]
    [NativeHeader("Modules/ContentBuild/Editor/SBPSupport/ContentBuildInterface.bindings.h")]
    public class SceneLoadInfo
    {
        [NativeName("asset")]
        private GUID m_Asset;
        ///<summary>GUID for the given Scene.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.SceneLoadInfo" />.</remarks>
        public GUID asset
        {
            get { return m_Asset; }
            set { m_Asset = value; }
        }

        [NativeName("address")]
        private string m_Address;
        ///<summary>Friendly name used to load the built Scene from an asset bundle.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.SceneLoadInfo" />.</remarks>
        public string address
        {
            get { return m_Address; }
            set { m_Address = value; }
        }

        [NativeName("internalName")]
        private string m_InternalName;
        ///<summary>Internal name used to load the built Scene from an asset bundle.</summary>
        ///<remarks>Internal names are used for loading to avoid collision if Scenes with similar file names are added to the same AssetBundle.
        ///Internal use only. See <see cref="Build.Content.SceneLoadInfo" />.</remarks>
        public string internalName
        {
            get { return m_InternalName; }
            set { m_InternalName = value; }
        }
    }

    ///<summary>Container for holding asset loading information for a streamed Scene AssetBundle to be built.</summary>
    ///<remarks>Note: this class and its members exist to provide low-level support for the **Scriptable Build Pipeline** package. This is intended for internal use only; use the &lt;a href="https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@latest/index.html"&gt;Scriptable Build Pipeline package&lt;/a&gt; to implement a fully featured build pipeline. You can install this via the [Package Manager window](/upm-ui.md).</remarks>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions = CodegenOptions.Custom)]
    [NativeHeader("Modules/ContentBuild/Editor/SBPSupport/ContentBuildInterface.bindings.h")]
    public class SceneBundleInfo
    {
        [NativeName("bundleName")]
        private string m_BundleName;
        ///<summary>Friendly AssetBundle name.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.SceneBundleInfo" />.</remarks>
        public string bundleName
        {
            get { return m_BundleName; }
            set { m_BundleName = value; }
        }

        [NativeName("bundleScenes")]
        private List<SceneLoadInfo> m_BundleScenes;
        ///<summary>List of Scene loading information for an AssetBundle.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.SceneBundleInfo" />.</remarks>
        public List<SceneLoadInfo> bundleScenes
        {
            get { return m_BundleScenes; }
            set { m_BundleScenes = value; }
        }
    }


    ///<summary>Container for holding object serialization order information for a build.</summary>
    ///<remarks>Note: this class and its members exist to provide low-level support for the **Scriptable Build Pipeline** package. This is intended for internal use only; use the &lt;a href="https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@latest/index.html"&gt;Scriptable Build Pipeline package&lt;/a&gt; to implement a fully featured build pipeline. You can install this via the [Package Manager window](/upm-ui.md).</remarks>
    [Serializable]
    [UsedByNativeCode]
    [NativeAsStruct]
    [NativeType(CodegenOptions = CodegenOptions.Custom)]
    [StructLayout(LayoutKind.Sequential)]
    public class SerializationInfo
    {
        [NativeName("serializationObject")]
        internal ObjectIdentifier m_SerializationObject;
        ///<summary>Source object to be serialzied to disk.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.SerializationInfo" />.</remarks>
        public ObjectIdentifier serializationObject
        {
            get { return m_SerializationObject; }
            set { m_SerializationObject = value; }
        }

        [NativeName("serializationIndex")]
        internal long m_SerializationIndex;
        ///<summary>Order in which the object will be serialized to disk.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.SerializationInfo" />.</remarks>
        public long serializationIndex
        {
            get { return m_SerializationIndex; }
            set { m_SerializationIndex = value; }
        }
    }

    ///<summary>Container for holding information about a serialized file to be written.</summary>
    ///<remarks>Note: this class and its members exist to provide low-level support for the **Scriptable Build Pipeline** package. This is intended for internal use only; use the &lt;a href="https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@latest/index.html"&gt;Scriptable Build Pipeline package&lt;/a&gt; to implement a fully featured build pipeline. You can install this via the [Package Manager window](/upm-ui.md).</remarks>
    [Serializable]
    [UsedByNativeCode]
    [NativeAsStruct]
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions = CodegenOptions.Custom)]
    [NativeHeader("Modules/ContentBuild/Editor/SBPSupport/ContentBuildInterface.bindings.h")]
    public class WriteCommand
    {
        [NativeName("fileName")]
        private string m_FileName;
        ///<summary>Final file name on disk of the serialized file.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.WriteCommand" />.</remarks>
        public string fileName
        {
            get { return m_FileName; }
            set { m_FileName = value; }
        }

        [NativeName("internalName")]
        private string m_InternalName;
        ///<summary>Internal name used by the loading system for a serialized file.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.WriteCommand" />.</remarks>
        public string internalName
        {
            get { return m_InternalName; }
            set { m_InternalName = value; }
        }

        [NativeName("serializeObjects")]
        private List<SerializationInfo> m_SerializeObjects;
        ///<summary>List of objects and their order contained inside a serialized file.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.WriteCommand" />.</remarks>
        public List<SerializationInfo> serializeObjects
        {
            get { return m_SerializeObjects; }
            set { m_SerializeObjects = value; }
        }
    }

    ///<summary>This struct collects all the WriteSerializedFile parameters in to a single place.</summary>
    public struct WriteParameters
    {
        ///<summary>The struct of internal file name, list of objects, and order of objects to use when writing the serialized file.</summary>
        public WriteCommand writeCommand;
        ///<summary>The settings to use when writing the serialized file.</summary>
        public BuildSettings settings;
        ///<summary>The global lighting information to use when writing the serialized file.</summary>
        public BuildUsageTagGlobal globalUsage;
        ///<summary>The the texture, material, mesh, and shader usage tags to use when writing the serialized file.</summary>
        public BuildUsageTagSet usageSet;
        ///<summary>The set of external objects that can be referenced by this serialized file.</summary>
        public BuildReferenceMap referenceMap;
        ///<summary>Optional Parameter used when writing a serialized file for an Asset Bundle.</summary>
        public AssetBundleInfo bundleInfo;
        ///<summary>The set of external object dependencies that need to be loaded when loading the resulting serialized file.</summary>
        public PreloadInfo preloadInfo;
    }

    ///<summary>This struct collects all the WriteSceneSerializedFile parameters in to a single place.</summary>
    public struct WriteSceneParameters
    {
        ///<summary>The original scene asset path.</summary>
        public string scenePath;
        ///<summary>The struct of internal file name, list of objects, and order of objects to use when writing the serialized file.</summary>
        public WriteCommand writeCommand;
        ///<summary>The settings to use when writing the serialized file.</summary>
        public BuildSettings settings;
        ///<summary>The global lighting information to use when writing the serialized file.</summary>
        public BuildUsageTagGlobal globalUsage;
        ///<summary>The the texture, material, mesh, and shader usage tags to use when writing the serialized file.</summary>
        public BuildUsageTagSet usageSet;
        ///<summary>The set of external objects that can be referenced by this serialized file.</summary>
        public BuildReferenceMap referenceMap;
        ///<summary>The set of external object dependencies that need to be loaded when loading the resulting serialzied file.</summary>
        public PreloadInfo preloadInfo;
        ///<summary>Optional Parameter used when writing a scene serialized file for an Asset Bundle.</summary>
        public SceneBundleInfo sceneBundleInfo;
    }

    ///<summary>Defines the write parameters for the <see cref="ContentBuildInterface.WriteGameManagersSerializedFile" /> function.</summary>
    public struct WriteManagerParameters
    {
        ///<summary>The settings to use when writing the serialized file.</summary>
        public BuildSettings settings;
        ///<summary>The global lighting information to use when writing the serialized file.</summary>
        public BuildUsageTagGlobal globalUsage;
        ///<summary>The set of external objects that can be referenced by this serialized file.</summary>
        public BuildReferenceMap referenceMap;
    }
}
