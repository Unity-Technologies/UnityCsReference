// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEditor.PackageManager
{
    [StructLayout(LayoutKind.Sequential)]
    [RequiredByNativeCode]
    class OperationStatus
    {
        private StatusCode m_Status;
        private string m_Id;
        private string m_Type;
        private UpmPackageInfo[] m_PackageList;
        private float m_Progress;

        private OperationStatus() {}

        public string id { get { return m_Id;  } }
        public StatusCode status { get { return m_Status;  } }
        public string type { get { return m_Type;  } }
        public UpmPackageInfo[] packageList { get { return m_PackageList; } }
        public float progress { get { return m_Progress; } }
    }
}

