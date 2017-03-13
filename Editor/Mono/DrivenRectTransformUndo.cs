// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;

namespace UnityEditor
{
    [InitializeOnLoad]
    internal class DrivenRectTransformUndo
    {
        // Static constructor
        static DrivenRectTransformUndo()
        {
            Undo.willFlushUndoRecord += ForceUpdateCanvases;
            // After undo or redo performed, the 'driven values' & 'driven properties mask' need to be updated.
            Undo.undoRedoPerformed += ForceUpdateCanvases;
        }

        static void ForceUpdateCanvases()
        {
            Canvas.ForceUpdateCanvases();
        }
    }
}
