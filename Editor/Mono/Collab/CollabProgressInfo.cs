// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEditor.Collaboration
{
    // Keep internal and undocumented until we expose more functionality
    //*undocumented
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions = CodegenOptions.Custom, Header = "Editor/Src/Collab/CollabProgressInfo.h",
        IntermediateScriptingStructName = "ScriptingCollabProgressInfo")]
    [NativeHeader("Editor/Src/Collab/Collab.bindings.h")]
    [NativeAsStruct]
    internal class ProgressInfo
    {
        public enum ProgressType : uint
        {
            None = 0,
            Count = 1,
            Percent = 2,
            Both = 3
        };

        int m_JobId;
        string m_Title;
        string m_ExtraInfo;
        ProgressType m_ProgressType;
        int m_Percentage;
        int m_CurrentCount;
        int m_TotalCount;
        bool m_Completed;
        bool m_Cancelled;
        bool m_CanCancel;
        string m_LastErrorString;
        ulong m_LastError;

        public int jobId { get { return m_JobId; } }
        public string title { get { return m_Title; } }
        public string extraInfo { get { return m_ExtraInfo; } }
        public int currentCount { get { return m_CurrentCount; } }
        public int totalCount { get { return m_TotalCount; } }
        public bool completed { get { return m_Completed; } }
        public bool cancelled { get { return m_Cancelled; } }
        public bool canCancel { get { return m_CanCancel; } }
        public string lastErrorString { get { return m_LastErrorString; } }
        public ulong lastError { get { return m_LastError; } }

        public int percentComplete
        {
            get
            {
                if (m_ProgressType == ProgressType.Percent || m_ProgressType == ProgressType.Both)
                {
                    return m_Percentage;
                }

                if (m_ProgressType == ProgressType.Count)
                {
                    if (m_TotalCount == 0) return 0;
                    return (m_CurrentCount * 100) / m_TotalCount;
                }
                return 0;
            }
        }

        public bool isProgressTypeCount { get { return (m_ProgressType == ProgressType.Count || m_ProgressType == ProgressType.Both); } }
        public bool isProgressTypePercent { get { return (m_ProgressType == ProgressType.Percent || m_ProgressType == ProgressType.Both); } }
        public bool errorOccured { get { return (m_LastError != 0); } }
    }
}
