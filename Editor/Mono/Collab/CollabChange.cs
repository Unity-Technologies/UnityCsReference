// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEditor.Collaboration
{
    // Keep internal and undocumented until we expose more functionality
    //*undocumented
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions = CodegenOptions.Custom, Header = "Editor/Src/Collab/CollabChange.h",
        IntermediateScriptingStructName = "ScriptingCollabChange")]
    [NativeHeader("Editor/Src/Collab/Collab.bindings.h")]
    [NativeAsStruct]
    internal class Change
    {
        [Flags]
        public enum RevertableStates : uint
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
            InvalidRevertableState             = (uint)1 << 31
        };

        string m_Path;
        Collab.CollabStates m_State;
        RevertableStates m_RevertableState;
        string m_RelatedTo;
        string m_LocalStatus;
        string m_RemoteStatus;
        string m_ResolveStatus;

        Change() {}

        public string path { get { return m_Path; } }
        public Collab.CollabStates state { get { return m_State; } }
        public bool isRevertable { get { return HasRevertableState(RevertableStates.Revertable); } }
        public RevertableStates revertableState { get { return m_RevertableState; } }
        public string relatedTo { get { return m_RelatedTo; } }

        public bool isMeta { get { return HasState(Collab.CollabStates.kCollabMetaFile); } }
        public bool isConflict { get { return HasState(Collab.CollabStates.kCollabConflicted) || HasState(Collab.CollabStates.kCollabPendingMerge); } }
        public bool isFolderMeta { get { return HasState(Collab.CollabStates.kCollabFolderMetaFile); } }
        public bool isResolved { get { return HasState(Collab.CollabStates.kCollabUseMine) || HasState(Collab.CollabStates.kCollabUseTheir) || HasState(Collab.CollabStates.kCollabMerged); } }

        public string localStatus { get { return m_LocalStatus; } }
        public string remoteStatus { get { return m_RemoteStatus; } }
        public string resolveStatus { get { return m_ResolveStatus; } }

        internal bool HasState(Collab.CollabStates states)
        {
            return (m_State & states) != 0;
        }

        internal bool HasRevertableState(RevertableStates revertableStates)
        {
            return (m_RevertableState & revertableStates) != 0;
        }
    }

    internal class PublishInfo
    {
        public Change[] changes;
        public bool filter;
    }
}
