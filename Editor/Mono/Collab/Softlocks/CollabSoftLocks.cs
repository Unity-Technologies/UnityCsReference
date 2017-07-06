// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System.Runtime.InteropServices;
using UnityEngine.Scripting;

namespace UnityEditor.Collaboration
{
    // Keep internal and undocumented until we expose more functionality
    //*undocumented
    [StructLayout(LayoutKind.Sequential)]
    internal class SoftLock
    {
        private string m_UserID;
        private string m_MachineID;
        private string m_DisplayName;
        private ulong m_TimeStamp;
        private string m_Hash;

        private SoftLock() {}

        public string userID { get { return m_UserID;  } }
        public string machineID { get { return m_MachineID;  } }
        public string displayName { get { return m_DisplayName;  } }
        public ulong timeStamp { get { return m_TimeStamp;  } }
        public string hash { get { return m_Hash;  } }
    }
}

