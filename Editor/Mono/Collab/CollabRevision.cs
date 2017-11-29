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
    [UsedByNativeCode]
    internal struct Revision
    {
        [NativeName("m_CommitterName")]
        private string m_AuthorName;
        [NativeName("m_CommitterEmail")]
        private string m_Author;
        private string m_Comment;
        private string m_RevisionID;
        private string m_Reference;
        private ulong m_TimeStamp;
        // Whether this revision has been obtained by the client
        private bool m_IsObtained;
        private ChangeAction[] m_Entries;
        private CloudBuildStatus[] m_BuildStatuses;

        public string authorName { get { return m_AuthorName;  } }
        public string author { get { return m_Author;  } }
        public string comment { get { return m_Comment;  } }
        public string revisionID { get { return m_RevisionID;  } }
        public string reference { get { return m_Reference;  } }
        public ulong timeStamp { get { return m_TimeStamp;  } }
        public bool isObtained { get { return m_IsObtained;  } }
        public ChangeAction[] entries { get { return m_Entries;  } }
        public CloudBuildStatus[] buildStatuses { get { return m_BuildStatuses;  } }
    }
}
