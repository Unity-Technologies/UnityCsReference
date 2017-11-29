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
    internal struct ChangeAction
    {
        private string m_Path;
        private string m_Action;

        public string path { get { return m_Path; } }
        public string action { get { return m_Action; } }
    }
}
