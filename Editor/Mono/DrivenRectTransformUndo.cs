// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: Kernel not yet converted
using UnityEngine;
using Unity.Scripting.LifecycleManagement;

namespace UnityEditor
{
    internal partial class DrivenRectTransformUndo
    {
        [OnCodeLoaded]
        static void Initialize()
        {
            Undo.willFlushUndoRecord += ForceUpdateCanvases;
            // After undo or redo performed, the 'driven values' & 'driven properties mask' need to be updated.
            Undo.undoRedoEvent += OnUndoRedoPerformed;
        }

        [OnCodeUnloading]
        static void Shutdown()
        {
            Undo.willFlushUndoRecord -= ForceUpdateCanvases;
            Undo.undoRedoEvent -= OnUndoRedoPerformed;
        }

        static void ForceUpdateCanvases()
        {
            Canvas.ForceUpdateCanvases();
        }

        static void OnUndoRedoPerformed(in UndoRedoInfo info)
        {
            Canvas.ForceUpdateCanvases();
        }
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
