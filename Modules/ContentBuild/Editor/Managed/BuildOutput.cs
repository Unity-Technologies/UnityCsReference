// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Build.Content
{
    ///<summary>Struct containing information about where an object was serialized.</summary>
    ///<remarks>Note: this class and its members exist to provide low-level support for the **Scriptable Build Pipeline** package. This is intended for internal use only; use the &lt;a href="https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@latest/index.html"&gt;Scriptable Build Pipeline package&lt;/a&gt; to implement a fully featured build pipeline. You can install this via the [Package Manager window](/upm-ui.md).</remarks>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct SerializedLocation
    {
        [NativeName("fileName")]
        internal string m_FileName;
        ///<summary>File path on disk where the object was serialized.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.SerializedLocation" />.</remarks>
        public string fileName { get { return m_FileName; } }

        [NativeName("offset")]
        internal ulong m_Offset;
        ///<summary>Byte offset for the start of the object's data.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.SerializedLocation" />.</remarks>
        public ulong offset { get { return m_Offset; } }

        [NativeName("size")]
        internal ulong m_Size;
        ///<summary>Size of the object's data.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.SerializedLocation" />.</remarks>
        public ulong size { get { return m_Size; } }
    }


    ///<summary>Struct containing details about how an object was serialized.</summary>
    ///<remarks>Note: this struct and its members exist to provide low-level support for the **Scriptable Build Pipeline** package. This is intended for internal use only; use the &lt;a href="https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@latest/index.html"&gt;Scriptable Build Pipeline package&lt;/a&gt; to implement a fully featured build pipeline. You can install this via the [Package Manager window](/upm-ui.md).</remarks>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct ObjectSerializedInfo
    {
        [NativeName("serializedObject")]
        internal ObjectIdentifier m_SerializedObject;
        ///<summary>Object that was serialized.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.ObjectSerializedInfo" />.</remarks>
        public ObjectIdentifier serializedObject { get { return m_SerializedObject; } }

        [NativeName("header")]
        internal SerializedLocation m_Header;
        ///<summary>Serialized object header information.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.ObjectSerializedInfo" />.</remarks>
        public SerializedLocation header { get { return m_Header; } }

        [NativeName("rawData")]
        internal SerializedLocation m_RawData;
        ///<summary>Raw byte data of the object if it was serialized seperately from the header.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.ObjectSerializedInfo" />.</remarks>
        public SerializedLocation rawData { get { return m_RawData; } }
    }

    ///<summary>Desribes an externally referenced file. This is returned as part of the <see cref="WriteResult" /> when writing a serialized file.</summary>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct ExternalFileReference
    {
        [NativeName("filePath")]
        internal string m_filePath;
        ///<summary>The path of the file that is referenced.</summary>
        public string filePath { get { return m_filePath; } }

        [NativeName("type")]
        internal int m_type;
        ///<summary>The lookup resolution index for the GUID field in the editor. This is used in conjunction with the GUID internally and should not be modified.</summary>
        public int type { get { return m_type; } }

        [NativeName("guid")]
        internal GUID m_guid;
        ///<summary>A GUID that represents the file being referenced. This GUID might be used to locate default editor resources, but generally pathName is used to identify externally referenced files.</summary>
        public GUID guid { get { return m_guid; } }
    }

    ///<summary>Struct containing the results from the <see cref="ContentBuildPipeline.WriteSerialziedFile" /> and <see cref="ContentBuildPipeline.WriteSceneSerialziedFile" /> APIs.</summary>
    ///<remarks>Note: this struct and its members exist to provide low-level support for the **Scriptable Build Pipeline** package. This is intended for internal use only; use the &lt;a href="https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@latest/index.html"&gt;Scriptable Build Pipeline package&lt;/a&gt; to implement a fully featured build pipeline. &gt;UnityYou can install this via the [Package Manager window](/upm-ui.md)..</remarks>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct WriteResult
    {
        [NativeName("serializedObjects")]
        internal ObjectSerializedInfo[] m_SerializedObjects;
        ///<summary>Collection of objects written to the serialized file.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.WriteResult" />.</remarks>
        public ReadOnlyCollection<ObjectSerializedInfo> serializedObjects { get { return Array.AsReadOnly(m_SerializedObjects); } }

        [NativeName("resourceFiles")]
        internal ResourceFile[] m_ResourceFiles;
        ///<summary>Collection of files written by the <see cref="ContentBuildInterface.WriteSerializedFile" /> or <see cref="ContentBuildInterface.WriteSceneSerializedFile" /> APIs.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.WriteResult" />.</remarks>
        public ReadOnlyCollection<ResourceFile> resourceFiles { get { return Array.AsReadOnly(m_ResourceFiles); } }

        [NativeName("includedTypes")]
        internal Type[] m_IncludedTypes;
        ///<summary>Types that were included in the serialized file.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.WriteResult" />.</remarks>
        public ReadOnlyCollection<Type> includedTypes { get { return Array.AsReadOnly(m_IncludedTypes); } }

        [NativeName("includedSerializeReferenceFQN")]
        internal String[] m_IncludedSerializeReferenceFQN;
        ///<summary>SerializeReference instances fully qualified name which were included in the serialized file.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.WriteResult" />.</remarks>
        public ReadOnlyCollection<String> includedSerializeReferenceFQN { get { return Array.AsReadOnly(m_IncludedSerializeReferenceFQN); } }

        [NativeName("externalFileReferences")]
        internal ExternalFileReference[] m_ExternalFileReferences;
        ///<summary>The collection of externally referenced files.</summary>
        public ReadOnlyCollection<ExternalFileReference> externalFileReferences { get { return Array.AsReadOnly(m_ExternalFileReferences); } }
    }
}
