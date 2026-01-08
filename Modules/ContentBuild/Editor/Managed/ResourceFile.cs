// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Build.Content
{
    ///<summary>Details about a specific file written by the <see cref="ContentBuildInterface.WriteSerializedFile" /> or <see cref="ContentBuildInterface.WriteSceneSerializedFile" /> APIs.</summary>
    ///<remarks>Note: this struct and its members exist to provide low-level support for the **Scriptable Build Pipeline** package. This is intended for internal use only; use the &lt;a href="https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@latest/index.html"&gt;Scriptable Build Pipeline package&lt;/a&gt; to implement a fully featured build pipeline. You can install this via the [Package Manager window](/upm-ui.md).</remarks>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct ResourceFile
    {
        [SerializeField]
        [NativeName("fileName")]
        internal string m_FileName;
        ///<summary>Path to the resource file on disk.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.ResourceFile" />.</remarks>
        public string fileName { get { return m_FileName; } set { m_FileName = value; } }

        [SerializeField]
        [NativeName("fileAlias")]
        internal string m_FileAlias;
        ///<summary>Internal name used by the loading system for a resource file.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.ResourceFile" />.</remarks>
        public string fileAlias { get { return m_FileAlias; } set { m_FileAlias = value; } }

        [SerializeField]
        [NativeName("serializedFile")]
        internal bool m_SerializedFile;
        ///<summary>Bool to determine if this resource file represents serialized Unity objects (serialized file) or binary resource data.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.ResourceFile" />.</remarks>
        public bool serializedFile { get { return m_SerializedFile; } set { m_SerializedFile = value; } }
    }
}
