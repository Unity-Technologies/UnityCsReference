// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace UnityEditor.Collaboration
{
    // Keep internal and undocumented until we expose more functionality
    //*undocumented
    [StructLayout(LayoutKind.Sequential)]
    [UsedByNativeCode]
    internal struct CloudBuildStatus
    {
        private string m_Platform;
        private bool m_Complete;
        private bool m_Successful;

        public string platform { get { return m_Platform; } }
        public bool complete { get { return m_Complete; } }
        public bool success { get { return m_Successful; } }
    }
}
