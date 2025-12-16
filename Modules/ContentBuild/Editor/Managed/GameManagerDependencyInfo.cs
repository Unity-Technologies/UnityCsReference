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
    ///<summary>Contains dependency information for internal Unity game manager classes. Call <see cref="ContentBuildInterface.WriteGameManagersSerializedFile" /> or <see cref="ContentBuildInterface.CalculatePlayerDependenciesForGameManagers" /> to get an instance of this class.</summary>
    ///<remarks>Note: this class and its members exist to provide low-level support for the **Scriptable Build Pipeline** package. This is intended for internal use only; use the &lt;a href="https://docs.unity3d.com/Packages/com.unity.scriptablebuildpipeline@latest/index.html"&gt;Scriptable Build Pipeline package&lt;/a&gt; to implement a fully featured build pipeline. You can install this via the [Package Manager window](/upm-ui.md).</remarks>
    [Serializable]
    [UsedByNativeCode]
    [StructLayout(LayoutKind.Sequential)]
    public struct GameManagerDependencyInfo
    {
        [NativeName("managerObjects")]
        internal ObjectIdentifier[] m_ManagerObjects;
        ///<summary>The project-wide identifiers for the game manager classes referenced in this collection of dependency information.</summary>
        ///<remarks>Note: the elements of the <see cref="managerObjects" /> and <see cref="includedTypes" /> lists correspond to each other. An element at a given index in one list corresponds to the same manager class as the element at the same index in the other.</remarks>
        public ReadOnlyCollection<ObjectIdentifier> managerObjects { get { return Array.AsReadOnly(m_ManagerObjects); } }

        [NativeName("referencedObjects")]
        internal ObjectIdentifier[] m_ReferencedObjects;
        ///<summary>The project-wide identifiers for any objects referenced by the manager classes in the <see cref="managerObjects" /> list.</summary>
        public ReadOnlyCollection<ObjectIdentifier> referencedObjects { get { return Array.AsReadOnly(m_ReferencedObjects); } }

        [NativeName("includedTypes")]
        internal Type[] m_IncludedTypes;
        ///<summary>The project-wide identifiers for the game manager classes referenced in this collection of dependency information.</summary>
        ///<remarks>Note: the elements of the <see cref="managerObjects" /> and <see cref="includedTypes" /> lists correspond to each other. An element at a given index in one list corresponds to the same manager class as the element at the same index in the other.</remarks>
        public ReadOnlyCollection<Type> includedTypes { get { return Array.AsReadOnly(m_IncludedTypes); } }
    }
}
