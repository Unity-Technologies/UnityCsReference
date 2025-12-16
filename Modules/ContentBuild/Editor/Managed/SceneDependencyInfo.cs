// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Build.Content
{
    ///<summary>Scene dependency information generated from the <see cref="ContentBuildInterface.CalculatePlayerDependenciesForScene" /> API.</summary>
    ///<remarks>Note: this class and its members exist to provide low-level support for the **Scriptable Build Pipeline** package. This is intended for internal use only; use the &lt;a href="https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@latest/index.html"&gt;Scriptable Build Pipeline package&lt;/a&gt; to implement a fully featured build pipeline. You can install this via the [Package Manager window](/upm-ui.md).</remarks>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct SceneDependencyInfo
    {
        [NativeName("scene")]
        internal string m_Scene;
        ///<summary>Scene's original asset path.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.SceneDependencyInfo" />.</remarks>
        public string scene { get { return m_Scene; } }

        ///<summary>Path to the post processed version of the Scene.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.SceneDependencyInfo" />.</remarks>
        [Obsolete("processedScene has been deprecated, use scene instead.", true)]
        public string processedScene { get { return m_Scene; } }

        [NativeName("referencedObjects")]
        internal ObjectIdentifier[] m_ReferencedObjects;
        ///<summary>List of objects referenced by the Scene.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.SceneDependencyInfo" />.</remarks>
        public ReadOnlyCollection<ObjectIdentifier> referencedObjects { get { return Array.AsReadOnly(m_ReferencedObjects); } }

        [NativeName("includedTypes")]
        internal Type[] m_IncludedTypes;
        ///<summary>Types that are used by scene objects.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.SceneDependencyInfo" />.</remarks>
        public ReadOnlyCollection<Type> includedTypes { get { return Array.AsReadOnly(m_IncludedTypes); } }

        [NativeName("globalUsage")]
        internal BuildUsageTagGlobal m_GlobalUsage;
        ///<summary>Lighting information used by the Scene.</summary>
        ///<remarks>Internal use only. See <see cref="Build.Content.SceneDependencyInfo" />.</remarks>
        public BuildUsageTagGlobal globalUsage { get { return m_GlobalUsage; } }
    }
}
