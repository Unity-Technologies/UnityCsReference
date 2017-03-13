// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;

namespace UnityEditor.Collaboration
{
    // Keep internal and undocumented until we expose more functionality
    //*undocumented
    [StructLayout(LayoutKind.Sequential)]
    internal class Revision
    {
        private string m_AuthorName;
        private string m_Author;
        private string m_Comment;
        private string m_RevisionID;
        private string m_Reference;
        private ulong m_TimeStamp;

        private Revision() {}

        public string authorName { get { return m_AuthorName;  } }
        public string author { get { return m_Author;  } }
        public string comment { get { return m_Comment;  } }
        public string revisionID { get { return m_RevisionID;  } }
        public string reference { get { return m_Reference;  } }
        public ulong timeStamp { get { return m_TimeStamp;  } }
    }
}
