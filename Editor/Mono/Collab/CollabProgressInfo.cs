// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace UnityEditor.Collaboration
{
    // Keep internal and undocumented until we expose more functionality
    //*undocumented
    [StructLayout(LayoutKind.Sequential)]
    internal class ProgressInfo
    {
        public enum ProgressType : uint
        {
            None = 0,
            Count = 1,
            Percent = 2,
            Both = 3
        };

        private int m_JobId;
        private string m_Title;
        private string m_ExtraInfo;
        private ProgressType m_ProgressType;
        private int m_Percentage;
        private int m_CurrentCount;
        private int m_TotalCount;
        private int m_Completed;
        private int m_Cancelled;
        private int m_CanCancel;
        private string m_LastErrorString;
        private ulong m_LastError;

        private ProgressInfo() {}

        public int jobId { get { return m_JobId; } }
        public string title { get { return m_Title; } }
        public string extraInfo { get { return m_ExtraInfo; } }
        public int currentCount { get { return m_CurrentCount; } }
        public int totalCount { get { return m_TotalCount; } }
        public bool completed { get { return m_Completed != 0; } }
        public bool cancelled { get { return m_Cancelled != 0; } }
        public bool canCancel { get { return m_CanCancel != 0; } }
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
