// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor.Collaboration
{
    [NativeHeader("Editor/Src/Collab/CollabBindings.h")]
    partial class Collab
    {
        [NativeThrows]
        [FreeFunction("GetRevisions")]
        private static extern Revision[] InternalGetRevisions(
            bool withChanges = false, int startIndex = 0, int numRevisions = -1);

        public Revision[] GetRevisions(bool withChanges = false, int startIndex = 0, int numRevisions = -1)
        {
            return InternalGetRevisions(withChanges, startIndex, numRevisions);
        }

        [NativeThrows]
        [FreeFunction("GetRevisionsData")]
        private static extern RevisionsData InternalGetRevisionsData(
            bool withChanges, int startIndex, int numRevisions);

        public RevisionsData GetRevisionsData(bool withChanges, int startIndex, int numRevisions)
        {
            return InternalGetRevisionsData(withChanges, startIndex, numRevisions);
        }
    }
}
