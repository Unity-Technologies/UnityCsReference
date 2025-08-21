// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.Multiplayer.PlayMode.Editor
{
    [Serializable]
    struct CloneState
    {
        public bool StreamLogsToMainEditor;

        // We might want to add scenes hierarchy and play mode state here
        // so sync happens in one atomic operation.

        public override string ToString()
        {
            return $"{{StreamLogsToMainEditor: {StreamLogsToMainEditor}}}";
        }
    }
}
