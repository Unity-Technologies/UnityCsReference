// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.InteropServices;
using UnityEngine.Bindings;

namespace UnityEditor.Collaboration
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeType(CodegenOptions = CodegenOptions.Custom, Header = "Editor/Src/Collab/CollabClient.h",
        IntermediateScriptingStructName = "ScriptingCollabChangeItem")]
    [NativeHeader("Editor/Src/Collab/Collab.bindings.h")]
    [NativeAsStruct]
    internal class ChangeItem
    {
        public string Path { get; set; }
        public Change.RevertableStates RevertableState { get; set; }
        public string RelatedTo { get; set; }
        public string RevisionId { get; set; }
        public string Hash { get; set; }
        public Collab.CollabStates State { get; set; }
        public long Size { get; set; }
        public string DownloadPath { get; set; }
        public string FromPath { get; set; }
    }

    internal class PublishInfo_V2
    {
        public ChangeItem[] changes;
        public bool filter;
    }
}
