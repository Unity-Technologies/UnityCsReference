// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System.Runtime.InteropServices;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEditor.Collaboration
{
    // Keep internal and undocumented until we expose more functionality
    //*undocumented
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions = CodegenOptions.Custom, Header = "Editor/Src/Collab/Softlocks/CollabSoftLock.h",
        IntermediateScriptingStructName = "ScriptingSoftLock")]
    [NativeHeader("Editor/Src/Collab/Collab.bindings.h")]
    [NativeAsStruct]
    internal class SoftLock
    {
        string m_UserID;
        string m_MachineID;
        string m_DisplayName;
        ulong m_TimeStamp;
        string m_Hash;

        SoftLock() {}

        public string userID { get { return m_UserID;  } }
        public string machineID { get { return m_MachineID;  } }
        public string displayName { get { return m_DisplayName;  } }
        public ulong timeStamp { get { return m_TimeStamp;  } }
        public string hash { get { return m_Hash;  } }
    }
}

