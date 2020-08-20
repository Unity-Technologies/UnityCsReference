// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEngine
{
    [StructLayout(LayoutKind.Sequential)]
    internal sealed partial class RuntimeInitializeMethodInfo
    {
        string      m_FullClassName;
        string      m_MethodName;
        int         m_OrderNumber = 0;
        bool            m_IsUnityClass = false;

        internal string fullClassName { get { return m_FullClassName; } set { m_FullClassName = value; } }
        internal string methodName { get { return m_MethodName; } set { m_MethodName = value; } }
        internal int orderNumber { get { return m_OrderNumber; } set { m_OrderNumber = value; } }
        internal bool isUnityClass { get { return m_IsUnityClass; } set { m_IsUnityClass = value; } }
    }

    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    internal class RuntimeInitializeClassInfo
    {
        public string assemblyName;
        public string nameSpace;
        public string className;
        public string methodName;
        public int loadTypes;
    }

    [NativeHeader("Runtime/Misc/RuntimeInitializeOnLoadManager.h")]
    [StaticAccessorAttribute("GetRuntimeInitializeOnLoadManager()")]
    internal sealed partial class RuntimeInitializeOnLoadManager
    {
        extern internal static string[] dontStripClassNames { get; }
    }
}
