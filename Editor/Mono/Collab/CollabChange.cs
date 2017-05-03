// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;

namespace UnityEditor.Collaboration
{
    // Keep internal and undocumented until we expose more functionality
    //*undocumented
    [StructLayout(LayoutKind.Sequential)]
    internal class Change
    {
        [Flags]
        public enum RevertableStates : ulong
        {
            Revertable                         = 1 << 0,
            NotRevertable                      = 1 << 1,

            Revertable_File                    = 1 << 2,
            Revertable_Folder                  = 1 << 3,
            Revertable_EmptyFolder             = 1 << 4,

            NotRevertable_File                 = 1 << 5,
            NotRevertable_Folder               = 1 << 6,
            NotRevertable_FileAdded            = 1 << 7,
            NotRevertable_FolderAdded          = 1 << 8,
            NotRevertable_FolderContainsAdd    = 1 << 9,

            // do not exceed Javascript Number range
            InvalidRevertableState             = (ulong)1 << 31
        };

        private string m_Path;
        private Collab.CollabStates m_State;
        private Change.RevertableStates m_RevertableState;
        private string m_RelatedTo;
        private string m_LocalStatus;
        private string m_RemoteStatus;
        private string m_ResolveStatus;

        private Change() {}

        public string path { get { return m_Path; } }
        public System.UInt64 state { get { return (System.UInt64)m_State; } }
        public bool isRevertable { get { return (m_RevertableState & Change.RevertableStates.Revertable) == Change.RevertableStates.Revertable; } }
        public System.UInt64 revertableState { get { return (System.UInt64)m_RevertableState; } }
        public string relatedTo { get { return m_RelatedTo; } }

        public bool isMeta { get { return (m_State & Collab.CollabStates.kCollabMetaFile) == Collab.CollabStates.kCollabMetaFile; } }
        public bool isConflict { get { return (m_State & Collab.CollabStates.kCollabConflicted) == Collab.CollabStates.kCollabConflicted || (m_State & Collab.CollabStates.kCollabPendingMerge) == Collab.CollabStates.kCollabPendingMerge; } }
        public bool isFolderMeta { get { return (m_State & Collab.CollabStates.kCollabFolderMetaFile) == Collab.CollabStates.kCollabFolderMetaFile; } }
        public bool isResolved { get { return (m_State & Collab.CollabStates.kCollabUseMine) == Collab.CollabStates.kCollabUseMine || (m_State & Collab.CollabStates.kCollabUseTheir) == Collab.CollabStates.kCollabUseTheir || (m_State & Collab.CollabStates.kCollabMerged) == Collab.CollabStates.kCollabMerged; } }

        public string localStatus { get { return m_LocalStatus; } }
        public string remoteStatus { get { return m_RemoteStatus; } }
        public string resolveStatus { get { return m_ResolveStatus; } }
    }

    internal class PublishInfo
    {
        public Change[] changes;
        public bool filter;
    }
}
